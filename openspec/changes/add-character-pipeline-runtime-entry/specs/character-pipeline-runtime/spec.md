# character-pipeline-runtime Specification

## Purpose
定义角色 gameplay runtime 的正式入口：`CharacterPipelineRunner` 统一 tick，`CharacterPipelineHost` 只做 Unity 装配，`CharacterPipeline` 作为纯 C# 管线解释 Taco RootTree、状态机、状态行为 SubTree 和 Timeline 输出。该能力不恢复 BBB 代码状态机或旧 SO/config 数据源。

## ADDED Requirements

### Requirement: CharacterPipelineRunner 是统一 tick 源
系统 MUST 使用 `CharacterPipelineRunner` 作为角色 pipeline 的统一 tick 源。`CharacterPipeline` MUST NOT 自己拥有 Unity `Update`、`LateUpdate`、`FixedUpdate` 或其它自主 tick 来源。

#### Scenario: 多个角色被统一调度
- **WHEN** 场景中存在多个启用的 `CharacterPipelineHost`
- **THEN** 每个 Host 创建的 `CharacterPipeline` MUST 注册到同一个 runner
- **AND** runner MUST 在同一帧阶段按注册列表调度它们
- **AND** 单个 `CharacterPipeline` MUST NOT 自己从 Unity 生命周期拉取 tick

#### Scenario: 角色被禁用
- **WHEN** 某个 `CharacterPipelineHost` 被禁用
- **THEN** 该 Host 的 pipeline MUST 从 runner 反注册
- **AND** 后续 runner tick MUST NOT 再调度该 pipeline

### Requirement: CharacterPipelineHost 只负责装配和注册
系统 MUST 使用 `CharacterPipelineHost` 作为每个角色的 Unity 装配点。Host MUST 只负责序列化 Unity 引用、创建 pipeline、注册和释放 pipeline；Host MUST NOT 写入动作状态判断、状态切换、motion 结算或 combat 裁决逻辑。

#### Scenario: Host 创建 pipeline
- **WHEN** Host 初始化
- **THEN** Host MUST 使用配置的 Taco RootTree、Animator、CharacterController、TimelinePlayer 和输入资产创建 `CharacterPipeline`
- **AND** Host MUST NOT 创建 BBB `PlayerBaseState` 或 `PlayerStateRegistry`

#### Scenario: Host 不承担业务逻辑
- **WHEN** 一帧 gameplay tick 执行
- **THEN** Host MUST 只作为已创建 pipeline 的持有者
- **AND** 输入处理、图执行、motion 和 presentation MUST 位于 pipeline 或 stage 中

### Requirement: CharacterPipeline 是纯 C# 运行时主体
系统 MUST 将 `CharacterPipeline` 实现为纯 C# 对象。`CharacterPipeline` MUST 通过 runner 传入的 tick context 执行 update phase 和 late phase，MUST NOT 直接读取 Unity `Time.deltaTime`。

#### Scenario: Runner 传入 tick context
- **WHEN** runner 调用 pipeline update phase
- **THEN** runner MUST 传入包含 deltaTime 和 frame index 的 tick context
- **AND** pipeline MUST 使用该 context 推进自己的 stage

#### Scenario: Pipeline 被释放
- **WHEN** Host 销毁或明确释放 pipeline
- **THEN** pipeline MUST 释放 Taco RootTree 运行实例、Graph context 和 stage 缓存
- **AND** pipeline MUST NOT 继续持有场景对象引用

### Requirement: Graph 执行上下文来自 CharacterPipelineGraphContext
系统 MUST 使用 `CharacterPipelineGraphContext` 作为 Taco RootTree 的 `BaseGraph.User`。该 context MUST 直接提供 TimelinePlayer provider 和 InputAction value source，MUST NOT 依赖场景搜索或 fallback 补齐缺失引用。

#### Scenario: TimelineNode 获取 TimelinePlayer
- **WHEN** `TimelineNode` 在角色 pipeline 中被 tick
- **THEN** `TimelineNode` MUST 通过 `BaseGraph.User` 获取 `ITimelinePlayerProvider`
- **AND** provider MUST 返回 Host 注入给 graph context 的 TimelinePlayer

#### Scenario: InputAction ValueNode 读取输入
- **WHEN** InputAction ValueNode 被请求输出值
- **THEN** 节点 MUST 通过 `BaseGraph.User` 获取 `IInputActionValueSource`
- **AND** value source MUST 使用 graph context 当前帧输入来源读取 Button、Float 或 Vector2

#### Scenario: 缺失上下文引用
- **WHEN** graph context 缺少 TimelinePlayer 或输入资产
- **THEN** 对应节点 MUST 按现有 Taco 节点规则报告缺失来源
- **AND** graph context MUST NOT 通过 `FindObjectOfType`、`Camera.main`、全局 singleton 或 GameObject 搜索补齐该引用

### Requirement: GraphStage 驱动 Taco RootTree
系统 MUST 使用 `CharacterGraphStage` 驱动 Host 配置的 Taco RootTree 运行实例。GraphStage MUST 保持 Taco 原有解释链路，让 `StateMachineNode`、`StateMachineGraphRuntime`、`StateNode`、`SubTree`、`StateBehaviorSubTree` 和 `TimelineNode` 自己按现有节点语义运行。

#### Scenario: RootTree 被初始化
- **WHEN** pipeline 启动
- **THEN** GraphStage MUST 从 Host 配置的 RootTree 创建独立运行实例
- **AND** GraphStage MUST 使用 `CharacterPipelineGraphContext` 调用 `InitTree(user)`
- **AND** GraphStage MUST 调用 `OnSpawn()`

#### Scenario: RootTree 每帧运行
- **WHEN** runner 调用 pipeline update phase
- **THEN** GraphStage MUST 使用 tick context 的 deltaTime 调用 RootTree `UpdateTree(deltaTime)`
- **AND** GraphStage MUST NOT 绕过 Taco 节点生命周期直接调用状态或 Timeline 业务

#### Scenario: GraphStage 释放
- **WHEN** pipeline 被释放
- **THEN** GraphStage MUST 对运行实例调用 `OnUnspawn()`
- **AND** GraphStage MUST 调用 `DisposeTree()`

### Requirement: Pipeline 分阶段处理输入、图、motion 和表现
系统 MUST 将角色每帧处理拆成明确 stage。第一阶段 MUST 至少包含 input、graph、motion 和 presentation。Stage MUST 通过 frame/context/output 交换数据，MUST NOT 互相直接控制对方的内部状态。

#### Scenario: Update phase
- **WHEN** pipeline update phase 执行
- **THEN** InputStage MUST 更新当前帧输入快照
- **AND** GraphStage MUST 使用当前 frame/context tick Taco RootTree
- **AND** GraphStage 输出的数据 MUST 写入 `CharacterPipelineOutput`

#### Scenario: Late phase
- **WHEN** pipeline late phase 执行
- **THEN** MotionStage MUST 消费 `MotionProposal` 并产生 `MotionResult`
- **AND** PresentationStage MUST 消费 `AnimationCommand` 或 `PresentationCue`
- **AND** frame transient 数据 MUST 在帧末被清理

### Requirement: 节点和 Timeline 不直接结算最终 Transform
系统 MUST 让 Taco 节点和 Timeline 只产出意图、窗口、命令或 cue。最终角色位移 MUST 由 `CharacterMotionStage` 结算。Timeline MUST NOT 直接宣称命中成立、直接扣血或直接改写角色 Transform。

#### Scenario: Timeline 产出移动意图
- **WHEN** Timeline 轨道或节点表达某段动作位移
- **THEN** 该输出 MUST 进入 `MotionProposal`
- **AND** `CharacterMotionStage` MUST 决定最终 `MotionResult`

#### Scenario: Timeline 产出 gameplay window
- **WHEN** Timeline 表达攻击、无敌或取消窗口
- **THEN** 该输出 MUST 进入 pipeline output 的 window facts
- **AND** 命中、伤害和目标归属 MUST 留给后续 gameplay solver 或服务端裁决

### Requirement: Timeline 和动画 tick 权威归属 pipeline
系统 MUST 让 `CharacterPipelineRunner` 成为角色 pipeline 模式下的 Timeline 和动画图推进权威。`TimelinePlayer` 在该模式下 MUST 作为 provider 和 PlayableGraph adapter 使用，MUST NOT 与 `TimelineNode` 在同一帧重复推进同一 Timeline。

#### Scenario: TimelineNode 评估 Timeline
- **WHEN** `TimelineNode` 在 GraphStage 内执行
- **THEN** `TimelineNode` MUST 使用 GraphStage 提供的 deltaTime 评估 Timeline
- **AND** TimelinePlayer MUST NOT 在自己的自主 tick 中再次推进同一运行实例

#### Scenario: 选择外部 tick 策略
- **WHEN** 项目启用 `CharacterPipeline`
- **THEN** TimelinePlayer 的运行方式 MUST 被收敛为 pipeline 显式 tick
- **AND** 系统 MUST NOT 长期保留 pipeline tick 和 TimelinePlayer autonomous tick 两条权威路径

### Requirement: 不恢复 BBB 和旧 SO 数据源
系统 MUST NOT 将 BBB 的代码状态机或旧动作 SO/config 作为 `CharacterPipeline` 的数据主源。BBB 只能作为运行时组织参考。

#### Scenario: 参考 BBB
- **WHEN** 实现 `CharacterPipeline`
- **THEN** 系统 MAY 借鉴 BBB 的单入口、输入清洗、分阶段和帧末清理思想
- **AND** 系统 MUST NOT 复制 BBB `PlayerBaseState`、`PlayerStateRegistry`、`PlayerSO` 动作配置或 locomotion 特化状态类作为主链路

#### Scenario: 旧动作配置存在
- **WHEN** 项目中存在旧 locomotion、action、footphase、bodyclaim 或 animation presentation 配置
- **THEN** `CharacterPipeline` MUST NOT 从这些配置读取动作语义
- **AND** 动作语义 MUST 来自 Taco Graph、NodeModule、Timeline 轨道或后续正式 runtime output

### Requirement: 角色管线路径使用 Character 命名
系统 MUST 将新角色 pipeline 代码放在正式 `Character` 命名路径中。系统 MUST NOT 继续扩展旧拼写 `Charactor` 路径。

#### Scenario: 新增 pipeline 文件
- **WHEN** 实现本能力
- **THEN** 新文件 MUST 位于 `Assets/Scripts/Character/Pipeline`
- **AND** 新命名空间和类型名 MUST 使用 `Character` 或 `CharacterPipeline` 语义

#### Scenario: 旧空路径清理
- **WHEN** 旧 `Assets/Scripts/Charactor/Pipeline` 没有有效代码
- **THEN** 实现阶段 MUST 删除该旧路径
- **AND** 系统 MUST NOT 在该路径下新增新 runtime 文件
