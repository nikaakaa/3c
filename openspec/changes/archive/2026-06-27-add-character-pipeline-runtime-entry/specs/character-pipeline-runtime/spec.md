# character-pipeline-runtime Specification

## Purpose
定义角色 gameplay runtime 的正式入口：`CharacterPipelineRunner` 统一 tick，`CharacterPipelineHost` 只做 Unity 装配，`CharacterPipeline` 作为纯 C# 管线解释 BTSMTL RootTree、状态机、状态行为 SubTree 和 Timeline 输出，并从第一版保留混合架构的网络边界。该能力不恢复 BBB 代码状态机或旧 SO/config 数据源，也不在本变更中实现真实 transport 或服务端裁决。

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
系统 MUST 使用 `CharacterPipelineHost` 作为每个角色的 Unity 装配点。Host MUST 只负责序列化角色管线定义和 Unity 组件引用、创建 pipeline、注册和释放 pipeline；Host MUST NOT 直接序列化 BTSMTL RootTree 或 BTSMTL component 类型，MUST NOT 写入动作状态判断、状态切换、motion 结算或 combat 裁决逻辑。

#### Scenario: Host 创建 pipeline
- **WHEN** Host 初始化
- **THEN** Host MUST 使用 `CharacterPipelineDefinition`、Animancer、CharacterController 和输入配置创建 `CharacterPipeline`
- **AND** Host MUST NOT 创建 BBB `PlayerBaseState` 或 `PlayerStateRegistry`
- **AND** BTSMTL RootTree MUST 通过 `CharacterPipelineDefinition` 间接进入 pipeline

#### Scenario: Host 不承担业务逻辑
- **WHEN** 一帧 gameplay tick 执行
- **THEN** Host MUST 只作为已创建 pipeline 的持有者
- **AND** 输入处理、图执行、motion 和 presentation MUST 位于 pipeline 或 stage 中

### Requirement: CharacterPipeline 是纯 C# 运行时主体
系统 MUST 将 `CharacterPipeline` 实现为纯 C# 对象。`CharacterPipeline` MUST 通过 runner 传入的 tick context 执行 update phase 和 late phase，MUST NOT 直接读取 Unity `Time.deltaTime`。Tick context MUST 至少表达 deltaTime、frame index、simulation tick、input sequence 和 authority mode。

#### Scenario: Runner 传入 tick context
- **WHEN** runner 调用 pipeline update phase
- **THEN** runner MUST 传入包含 deltaTime、frame index、simulation tick、input sequence 和 authority mode 的 tick context
- **AND** pipeline MUST 使用该 context 推进自己的 stage

#### Scenario: Pipeline 被释放
- **WHEN** Host 销毁或明确释放 pipeline
- **THEN** pipeline MUST 释放 BTSMTL RootTree 运行实例、Graph context 和 stage 缓存
- **AND** pipeline MUST NOT 继续持有场景对象引用

### Requirement: Graph 执行上下文来自 CharacterGraphContext
系统 MUST 使用 `CharacterGraphContext` 作为 BTSMTL RootTree 的 `BaseGraph.User`。该 context MUST 直接提供 Timeline 播放请求服务、InputAction value source、authority mode、network tick context、gameplay facts 和 correction 输入入口，MUST NOT 依赖场景搜索或 fallback 补齐缺失引用。

#### Scenario: TimelineNode 获取 Timeline 播放请求入口
- **WHEN** `TimelineNode` 在角色 pipeline 中被 tick
- **THEN** `TimelineNode` MUST 通过 `BaseGraph.User` 获取 `ITimelinePlaybackService`
- **AND** service MUST 由 `CharacterGraphContext` 暴露给 Graph/BTSMTL

#### Scenario: InputAction ValueNode 读取输入
- **WHEN** InputAction ValueNode 被请求输出值
- **THEN** 节点 MUST 通过 `BaseGraph.User` 获取 `IInputActionValueSource`
- **AND** value source MUST 使用 graph context 当前帧输入来源读取 Button、Float 或 Vector2

#### Scenario: 缺失上下文引用
- **WHEN** graph context 缺少 Timeline 播放请求服务或输入资产
- **THEN** 对应节点 MUST 按现有 BTSMTL 节点规则报告缺失来源
- **AND** graph context MUST NOT 通过 `FindObjectOfType`、`Camera.main`、全局 singleton 或 GameObject 搜索补齐该引用

#### Scenario: Graph 读取网络上下文
- **WHEN** TransitionRuleGraph、状态行为或后续 gameplay 节点需要读取网络 tick、authority mode、confirmed event 或 correction 状态
- **THEN** 它们 MUST 通过 `CharacterGraphContext` 的正式接口读取
- **AND** 它们 MUST NOT 直接读取 transport、Fantasy Session 或服务端对象

### Requirement: BTSMTLPhase 驱动 BTSMTL RootTree 和 Timeline playback
系统 MUST 使用 `CharacterBTSMTLPhase` 作为 BT(BehaviorTree)、SM(StateMachine)、TL(Timeline) 的统一角色逻辑编排 phase。该 phase MUST 内部持有 `BehaviorTreeRuntime` 和 `TimelinePlaybackScheduler`，并保持 BTSMTL 原有解释链路，让 `StateMachineNode`、`StateMachineGraphRuntime`、`StateNode`、`SubTree`、`StateBehaviorSubTree` 和 `TimelineNode` 自己按现有节点语义运行。

#### Scenario: RootTree 被初始化
- **WHEN** pipeline 启动
- **THEN** `BehaviorTreeRuntime` MUST 从 Host 配置的 RootTree 创建独立运行实例
- **AND** `BehaviorTreeRuntime` MUST 使用 `CharacterGraphContext` 调用 `InitTree(user)`
- **AND** `BehaviorTreeRuntime` MUST 调用 `OnSpawn()`

#### Scenario: RootTree 每帧运行
- **WHEN** runner 调用 pipeline update phase
- **THEN** `CharacterBTSMTLPhase` MUST 先使用 tick context 的 deltaTime 调用 RootTree `UpdateTree(deltaTime)`
- **AND** `CharacterBTSMTLPhase` MUST 再推进由节点提交的 active Timeline playback
- **AND** `BehaviorTreeRuntime` MUST NOT 绕过 BTSMTL 节点生命周期直接调用状态或 Timeline 业务

#### Scenario: BTSMTLPhase 释放
- **WHEN** pipeline 被释放
- **THEN** `CharacterBTSMTLPhase` MUST 先取消并释放 active Timeline playback
- **AND** `BehaviorTreeRuntime` MUST 对运行实例调用 `OnUnspawn()`
- **AND** `BehaviorTreeRuntime` MUST 调用 `DisposeTree()`

### Requirement: Pipeline 分阶段处理输入、图、motion、表现和网络边界
系统 MUST 将角色每帧处理拆成明确 phase。第一阶段 MUST 至少包含 network receive、input、BTSMTL、motion resolve、presentation resolve、network send 和 frame end cleanup。Phase MUST 通过 frame/context/output 交换数据，MUST NOT 互相直接控制对方的内部状态。

#### Scenario: Update phase
- **WHEN** pipeline update phase 执行
- **THEN** NetworkReceiveStage MUST 先读取已注入的 server snapshot、confirmed event 或 correction 缓存
- **AND** InputStage MUST 更新当前帧输入快照
- **AND** CharacterBTSMTLPhase MUST 使用当前 frame/context tick BTSMTL RootTree 和 active Timeline playback
- **AND** CharacterBTSMTLPhase 输出的数据 MUST 写入 `CharacterPipelineOutput`

#### Scenario: Late phase
- **WHEN** pipeline late phase 执行
- **THEN** MotionStage MUST 消费 `MotionIntent`、`MotionContribution` 和 motion modifier 数据并产生 `MotionResult`
- **AND** PresentationStage MUST 消费 `AnimationContribution` 或 `PresentationCue`
- **AND** NetworkSendStage MUST 从 `NetworkOutput` 收集 client command、action request、motion snapshot 或 window digest
- **AND** frame transient 数据 MUST 在帧末被清理

### Requirement: Pipeline 输出分为 strict、presentation 和 network
系统 MUST 将 `CharacterPipelineOutput` 分为 `StrictGameplayOutput`、`PresentationOutput` 和 `NetworkOutput`。可同步或可校验的 gameplay 字段 MUST NOT 与 local-only 表现字段混在同一输出层。

#### Scenario: 写入 strict gameplay output
- **WHEN** Graph、Timeline 或 MotionStage 产出 active state、action phase、motion result、gameplay window 或 combat sample
- **THEN** 这些字段 MUST 写入 `StrictGameplayOutput`
- **AND** 后续网络校验、快照或 combat rewind MUST 只读取该层的正式 gameplay 字段

#### Scenario: 写入 presentation output
- **WHEN** Timeline 或 Graph 产出 animation command、VFX、SFX、camera cue、hit stop 或后处理 cue
- **THEN** 这些字段 MUST 写入 `PresentationOutput`
- **AND** 这些字段 MUST NOT 成为服务端权威裁决输入

#### Scenario: 写入 network output
- **WHEN** 本地预测角色产生 input sequence、action request、motion snapshot、gameplay window digest 或 correction acknowledgement
- **THEN** 这些字段 MUST 写入 `NetworkOutput`
- **AND** 本变更 MAY 只收集这些字段而不发送真实网络消息

### Requirement: 节点和 Timeline 不直接结算最终 Transform
系统 MUST 让 BTSMTL 节点和 Timeline 只产出意图、窗口、命令或 cue。最终角色位移 MUST 由 `CharacterMotionStage` 结算。Timeline MUST NOT 直接宣称命中成立、直接扣血或直接改写角色 Transform。

#### Scenario: Timeline 产出移动意图
- **WHEN** Timeline 轨道或节点表达某段动作位移
- **THEN** 该输出 MUST 进入 `MotionContribution` 或正式 `MotionIntent`
- **AND** `CharacterMotionStage` MUST 决定最终 `MotionResult`

#### Scenario: Timeline 产出 gameplay window
- **WHEN** Timeline 表达攻击、无敌或取消窗口
- **THEN** 该输出 MUST 进入 pipeline output 的 window facts
- **AND** 命中、伤害和目标归属 MUST 留给后续 gameplay solver 或服务端裁决

### Requirement: CharacterPipeline 支持混合架构 authority mode
系统 MUST 明确区分角色 pipeline 的 authority mode。第一阶段 MUST 至少定义 `LocalPredicted`、`RemoteProxy` 和 `PresentationOnly`。不同 mode MUST 使用同一 `CharacterPipeline` 主线，不得新增第二套角色控制器路径。

#### Scenario: 本地预测角色
- **WHEN** pipeline 处于 `LocalPredicted`
- **THEN** pipeline MUST 允许本地输入立即驱动 Graph、Timeline、Motion 和 Presentation
- **AND** pipeline MUST 通过 `NetworkOutput` 暴露后续服务端确认需要的 action request、input sequence 和 motion snapshot

#### Scenario: 远端代理角色
- **WHEN** pipeline 处于 `RemoteProxy`
- **THEN** pipeline MUST 允许 server snapshot 和 interpolation 数据驱动表现
- **AND** pipeline MUST NOT 要求远端角色完整重放本地输入 Graph

#### Scenario: 表现专用角色
- **WHEN** pipeline 处于 `PresentationOnly`
- **THEN** pipeline MUST 只消费表现输入或快照
- **AND** pipeline MUST NOT 产生本地 action request

### Requirement: NetworkStage 是正式边界但不实现真实 transport
系统 MUST 在 `CharacterPipeline` 中保留 `NetworkReceiveStage` 和 `NetworkSendStage`。本变更 MUST NOT 实现 Fantasy transport、服务端 handler 或完整网络裁决；真实 transport MUST 在后续 network change 中接入。

#### Scenario: 接收网络输入缓存
- **WHEN** 本帧开始时存在已注入的 `ServerSnapshot`、`ConfirmedEvent` 或 `Correction`
- **THEN** NetworkReceiveStage MUST 将它们放入 `CharacterPipelineFrame` 或 graph context 的正式位置
- **AND** NetworkReceiveStage MUST NOT 直接修改 Transform 或 BTSMTL 节点状态

#### Scenario: 收集网络输出
- **WHEN** 本帧产生 `NetworkOutput`
- **THEN** NetworkSendStage MUST 收集这些输出
- **AND** NetworkSendStage MUST NOT 在本变更中直接发送 Fantasy 消息

### Requirement: Timeline 和动画 tick 权威归属 pipeline
系统 MUST 让 `CharacterPipelineRunner` 成为角色 pipeline 模式下的 Timeline 和动画图推进权威。Timeline 播放请求 MUST 由 `CharacterBTSMTLPhase` 内部的 `TimelinePlaybackScheduler` 推进。`TimelinePlayer` 或等价 PlayableGraph adapter MAY 位于表现层边界内，MUST NOT 与 `TimelineNode` 在同一帧重复推进同一 Timeline。

#### Scenario: TimelineNode 提交 Timeline 请求
- **WHEN** `TimelineNode` 在 CharacterBTSMTLPhase 内执行
- **THEN** `TimelineNode` MUST 提交 Timeline 播放请求
- **AND** `TimelinePlaybackScheduler` MUST 使用 pipeline tick context 推进该请求
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
- **AND** 动作语义 MUST 来自 BTSMTL Graph、NodeModule、Timeline 轨道或后续正式 runtime output

### Requirement: 角色管线路径使用 Character 命名
系统 MUST 将新角色 pipeline 代码放在正式 `Character` 命名路径中。系统 MUST NOT 继续扩展旧拼写 `Charactor` 路径。

#### Scenario: 新增 pipeline 文件
- **WHEN** 实现本能力
- **THEN** 新文件 MUST 位于 `Assets/GameScripts/Main/Runtime/Character/Pipeline`
- **AND** 新命名空间和类型名 MUST 使用 `Character` 或 `CharacterPipeline` 语义

#### Scenario: 旧空路径清理
- **WHEN** 旧 `Assets/Scripts` 或旧 `Charactor/Pipeline` 没有有效代码
- **THEN** 实现阶段 MUST 删除该旧路径
- **AND** 系统 MUST NOT 在该路径下新增新 runtime 文件
