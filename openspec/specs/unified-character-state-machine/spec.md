# unified-character-state-machine Specification

## Purpose
定义统一层级角色逻辑状态机作为 FullBody base layer 的唯一状态权威，并约束输入、运动和动画 adapter 的外围职责。
## Requirements
### Requirement: 统一层级逻辑状态机权威
系统 MUST 使用一棵统一、可配置、层级化的角色逻辑状态机作为 FullBody base layer 行为的唯一状态权威。`Idle`、`MoveStart`、`MoveLoop`、`MoveStop`、`Dodge` 及后续 Roll、Jump、Attack 等状态 MUST 归属同一种状态节点模型，而不得由 Locomotion 特化状态机、Dodge 特化 runtime 或外层 FullBody 缝合器分别决定。正式运行时 MUST 只允许 FullBody 主调度入口拥有和推进当前角色的 `CharacterStateMachineRunner`；Locomotion adapter、动作 module、动画 Presenter 和 motion executor MUST NOT 创建第二个运行时 runner 或维护第二份 active state。

#### Scenario: 默认状态树可见
- **WHEN** 设计者打开默认角色逻辑状态机配置
- **THEN** 配置 MUST 能显示 `FullBody/Locomotion/Idle`
- **AND** MUST 能显示 `FullBody/Locomotion/MoveStart`
- **AND** MUST 能显示 `FullBody/Locomotion/MoveLoop`
- **AND** MUST 能显示 `FullBody/Locomotion/MoveStop`
- **AND** MUST 能显示 `FullBody/Action/Dodge`

#### Scenario: 不再存在第二状态权威
- **WHEN** 统一状态机接管 FullBody base layer
- **THEN** Locomotion 四阶段 transition MUST 由统一状态机配置决定
- **AND** Dodge 进入和退出 transition MUST 由统一状态机配置决定
- **AND** 系统 MUST NOT 继续通过 `BasicLocomotionStateMachine`、`LocomotionStateGraphConfigSO`、`DodgeActionRuntime`、`DodgeFullBodyActionModule` 或等价特化 runtime 决定另一套状态流转

#### Scenario: 快照来自统一状态机
- **WHEN** 运行时完成一帧状态推进
- **THEN** 当前状态路径、状态时间、当前变体、当前标签和 pending transition MUST 来自统一状态机快照
- **AND** 该快照 MUST NOT 暴露 Animancer state、CharacterController、InputAction、Cinemachine 或 UnityHFSM 内部 state 对象

#### Scenario: 只有 FullBody 入口创建 runner
- **WHEN** 检查当前角色正式运行时代码
- **THEN** `CharacterStateMachineRunner` MUST 只由 FullBody 主调度入口创建和持有
- **AND** `PlayerLocomotionController` MUST NOT 创建或缓存自己的正式运行时 runner
- **AND** Locomotion 相关测试如需推进状态机 MUST 显式传入测试构造的 runner，而不得恢复 Locomotion 自驱 runtime owner

### Requirement: 通用 transition 配置
系统 MUST 将角色状态切换表达为统一状态机中的 transition 配置。移动意图、无移动意图、预输入请求、状态时间、动画可退出事实、优先级、抗性和打断窗口 MUST 作为 transition 条件或 transition policy 配置呈现，而不是藏在 Locomotion 图或 Action 仲裁器外部路径中。

#### Scenario: Locomotion transition 使用通用条件
- **WHEN** 系统表达基础移动四阶段切换
- **THEN** `Idle -> MoveStart` MUST 使用 `HasMoveIntent` 或等价通用条件
- **AND** `MoveLoop -> MoveStop` MUST 使用 `NoMoveIntent` 或等价通用条件
- **AND** `MoveStop -> Idle` MUST 使用 `NoMoveIntent + StateCanExit` 或等价通用条件
- **AND** 这些 transition MUST 与 Dodge transition 存在于同一张状态机配置中

#### Scenario: Dodge transition 使用通用条件
- **WHEN** 系统表达 Shift Dodge 进入
- **THEN** `Locomotion/* -> Dodge` MUST 使用 `HasInputRequest(Dodge)` 或等价通用条件
- **AND** priority、resistance、timing window 或等价打断规则 MUST 作为该 transition 的可见配置
- **AND** 系统 MUST NOT 通过状态图外部 Action-only arbiter 隐式选择 Dodge 目标状态

#### Scenario: 条件 evaluator 保持纯数据
- **WHEN** transition 条件被求值
- **THEN** evaluator MUST 只读取统一状态机 context 中的纯数据 facts
- **AND** MUST NOT 读取 Animancer runtime state
- **AND** MUST NOT 读取 `CharacterController`
- **AND** MUST NOT 读取 `InputAction` 或 `Camera.main`

### Requirement: 状态输出配置
系统 MUST 允许逻辑状态节点配置进入、更新和退出时的纯数据输出。输出 MAY 包含运动命令、动画转换请求、输入请求消费、Run latch 写入、状态事实写入和诊断事实，但 MUST 由统一状态机先决定当前状态后再产出。TurnBack 这类 animation-driven locomotion transition MUST 通过状态输出声明运动权威策略，而不是散落在 controller 或 presenter 的临时特判中。

#### Scenario: Locomotion 状态输出基础移动
- **WHEN** 当前逻辑状态为 `MoveLoop`
- **THEN** 状态输出 MUST 能根据当前移动意图产出基础移动运动命令
- **AND** MUST 能产出 `MoveLoop` 对应的动画转换请求或持续播放请求
- **AND** MUST NOT 通过独立 Locomotion runtime 绕过统一状态机提交 base layer 动画

#### Scenario: Dodge 状态输出动作位移
- **WHEN** 当前逻辑状态为 `Dodge` 且变体为 `Directional`
- **THEN** 状态输出 MUST 能按配置距离和时长产出动作位移命令
- **AND** MUST 能产出立即转向输出
- **AND** MUST 能在完成时产出 Run latch 写入
- **AND** Locomotion 状态 MUST NOT 同时产出第二份平面位移或 base layer 动画输出

#### Scenario: Backstep 不写 Run latch
- **WHEN** 当前逻辑状态为 `Dodge` 且变体为 `Backstep`
- **THEN** 状态输出 MUST 能按配置距离和时长产出后闪位移命令
- **AND** MUST NOT 在完成时强制写入 Run latch

#### Scenario: TurnBack 状态输出动画驱动策略
- **WHEN** 当前逻辑状态为 `FullBody/Locomotion/TurnBack`
- **THEN** 状态输出 MUST 能声明 `Locomotion.Turn.Back` 动画请求
- **AND** MUST 能声明 TurnBack motion policy
- **AND** motion policy MUST 能引用 baked motion profile 或等价纯数据资产
- **AND** motion policy MUST 能声明默认入口为 `MoveLoop + Run`
- **AND** MUST 能声明普通输入旋转和平面位移抑制
- **AND** MUST NOT 直接调用 Animancer、CharacterController 或 motion executor

### Requirement: 逻辑状态后的动画转换配置
系统 MUST 允许逻辑状态节点或状态变体配置动画语义 key、timeline binding key 或等价稳定 ID，用于产出动画请求和匹配动画播放进度事实。具体动画播放配置 MUST 归属到 Animancer TransitionLibrary、`RunLocomotionAnimationConfigSO`、`ActionAnimationProfileSO` 或等价动画配置入口；逻辑状态机配置 MUST NOT 长期保存具体 `AnimationClip`、`TransitionAsset`、fade、speed、start time 或 Animancer runtime 对象作为状态机权威配置。

#### Scenario: Dodge 变体配置动画语义 key
- **WHEN** 设计者配置 `FullBody/Action/Dodge`
- **THEN** `Directional` 变体 MUST 能配置 `Action.Dodge.Directional` 或等价稳定动画语义 key
- **AND** `Backstep` 变体 MUST 能配置 `Action.Dodge.Backstep` 或等价稳定动画语义 key
- **AND** 具体 clip、transition asset、fade、speed 和 start time MUST 由动作动画 Profile 或等价动画配置解析

#### Scenario: Locomotion 状态配置 timeline binding key
- **WHEN** 设计者配置 `FullBody/Locomotion/TurnBack`
- **THEN** 状态机 MAY 保存 `Locomotion.Turn.Back` 或等价 timeline binding key
- **AND** 该 key MUST 只用于动画请求语义、播放进度事实匹配或 timeline window 采样
- **AND** 具体 Locomotion 动画资源和过渡参数 MUST 由基础移动动画配置或 Animancer TransitionLibrary 解析

#### Scenario: 动画不决定逻辑进入
- **WHEN** 动画外观 adapter 播放某个 Animancer transition
- **THEN** 它 MUST 只消费统一状态机产出的动画语义请求
- **AND** MUST NOT 决定 `Dodge` 是否允许进入
- **AND** MUST NOT 决定 `Dodge` 是否退出到 `MoveLoop` 或 `Idle`

#### Scenario: 动画事实回传为纯数据
- **WHEN** 状态 transition 需要等待动画可退出
- **THEN** 动画外观 adapter MUST 只回传 normalized time、is ended、alias key、action key 或等价纯数据 fact
- **AND** 统一状态机条件 MUST 读取这些 facts
- **AND** 统一状态机 MUST NOT 直接读取 Animancer state

### Requirement: 删除分裂路径
系统 MUST 在统一状态机实现完成后删除、退役或降级现有分裂路径。任何保留类型 MUST 只能作为纯数据模型、迁移工具或外围 adapter 存在，不得继续拥有状态切换、动作进入、base layer 动画选择或平面位移 owner 的权威。

#### Scenario: Locomotion 特化状态机退役
- **WHEN** 统一状态机实现完成
- **THEN** 运行时代码 MUST NOT 再通过 `BasicLocomotionStateMachine` 推进基础移动阶段
- **AND** MUST NOT 再通过 `LocomotionStateGraphConfigSO` 作为独立基础移动状态图配置
- **AND** 基础移动四阶段 MUST 由统一状态机配置表达

#### Scenario: Dodge 特化 runtime 退役
- **WHEN** 统一状态机实现完成
- **THEN** 运行时代码 MUST NOT 再通过 `DodgeActionRuntime` 或 `DodgeFullBodyActionModule` 决定 Dodge 生命周期
- **AND** Dodge 的进入、更新、完成和退出 MUST 由统一状态机状态、transition 和输出表达

#### Scenario: FullBody 缝合器退役
- **WHEN** 统一状态机实现完成
- **THEN** 运行时代码 MUST NOT 再通过仅包装 Locomotion 和 Action 的 `FullBodyHfsmStateTreeDriver` 或等价缝合器决定 owner
- **AND** FullBody owner MUST 从统一状态机当前状态和输出推导

#### Scenario: Locomotion 自驱入口退役
- **WHEN** 当前角色通过正式 gameplay 路径运行
- **THEN** `PlayerLocomotionController` MUST NOT 独立读取输入后推进统一状态机 runner
- **AND** `PlayerLocomotionController` MUST 只向 FullBody pipeline 提供 Locomotion facts、运动命令构建和动画桥接能力
- **AND** 任何保留的 Locomotion 直接 tick 入口 MUST 输出迁移诊断或仅用于测试，不得参与正式场景装配

### Requirement: 输入、运动和动画 adapter 保持外围
系统 MUST 保留输入读取、运动执行、动画播放和相机处理作为统一状态机外围 adapter。adapter MUST 执行状态机输出或提供纯数据 facts，不得反向拥有逻辑状态切换权威。

#### Scenario: 输入缓冲只记录请求
- **WHEN** 玩家按下 Shift
- **THEN** 输入 adapter MUST 只写入 Dodge 请求或等价输入事实
- **AND** 是否消费该请求 MUST 由统一状态机 transition 和输出决定

#### Scenario: 运动执行只消费命令
- **WHEN** 统一状态机产出运动命令
- **THEN** 运动 adapter MUST 执行该命令
- **AND** 运动 adapter MUST NOT 选择当前逻辑状态
- **AND** 统一状态机 runner MUST NOT 直接调用 `CharacterController.Move`

#### Scenario: 动画外观只消费动画请求
- **WHEN** 统一状态机产出动画转换请求
- **THEN** Animancer adapter MUST 播放对应 transition
- **AND** Animancer adapter MUST NOT 选择当前逻辑状态
- **AND** 统一状态机 runner MUST NOT 直接调用 Animancer 播放 API

### Requirement: 可测试和可验证
系统 MUST 为统一层级角色逻辑状态机提供自动测试、静态边界验证和 Play Mode 手动验证。验证 MUST 证明状态机统一了当前移动和 Dodge 行为，并证明旧分裂路径不再参与运行时状态决策。

#### Scenario: 自动测试覆盖当前行为
- **WHEN** 运行统一状态机 EditMode 测试
- **THEN** 测试 MUST 覆盖 Idle、MoveStart、MoveLoop、MoveStop 的状态流转
- **AND** MUST 覆盖有移动输入时进入 Dodge Directional
- **AND** MUST 覆盖无移动输入时进入 Dodge Backstep
- **AND** MUST 覆盖 Directional 完成后 Run latch
- **AND** MUST 覆盖 Backstep 完成后不写 Run latch

#### Scenario: 静态验证旧路径删除
- **WHEN** 检查运行时代码
- **THEN** 静态验证 MUST 确认旧 Locomotion 特化状态机不再被运行时引用
- **AND** MUST 确认旧 Dodge 特化 runtime 不再被运行时引用
- **AND** MUST 确认旧 FullBody 缝合器不再被运行时引用
- **AND** MUST 确认当前角色正式运行时代码不存在第二个 `CharacterStateMachineRunner` owner

#### Scenario: 用户手动验证
- **WHEN** 用户在 Play Mode 操作可琳角色
- **THEN** 普通 WASD MUST 按统一状态机路径显示 Idle、MoveStart、MoveLoop、MoveStop
- **AND** 有方向按 Shift MUST 显示 Dodge Directional 并向输入方向冲刺
- **AND** 无方向按 Shift MUST 显示 Dodge Backstep 且不强制 Run
- **AND** 用户 MUST 能在同一状态机配置入口看到 Dodge transition 和 Dodge 动画转换配置
- **AND** 诊断日志中当前状态路径 MUST 来自 FullBody 主调度入口持有的唯一 runner

### Requirement: 状态节点能力模块模型
系统 MUST 将角色状态节点表达为统一节点关系加能力模块集合。节点核心 MUST 只表达稳定状态 ID、父节点、路径片段、标签和模块列表；Locomotion phase、动作请求、位移、动画、timeline、输入消费、run latch 和特殊 motion policy 等能力 MUST 通过模块或等价模块数据表达。系统 MUST NOT 长期使用一个包含所有能力字段的万能节点作为正式配置模型。

#### Scenario: 节点关系保持统一
- **WHEN** 设计者配置 `FullBody/Locomotion/MoveLoop` 和 `FullBody/Action/Dodge`
- **THEN** 两者 MUST 共享同一种节点关系模型
- **AND** MUST 都能通过 `stateId`、`parentStateId`、`pathSegment` 或等价字段表达树关系
- **AND** MUST NOT 需要不同节点类才能参与同一张状态图 transition

#### Scenario: 能力通过模块表达
- **WHEN** 设计者配置 `Dodge`
- **THEN** Dodge 的动作请求、动作位移、动作动画和输入消费 MUST 来自模块或等价模块数据
- **AND** 普通 `MoveLoop` MUST NOT 暴露无效的 Dodge 动作位移字段
- **AND** 分组节点 MUST NOT 暴露无效的 motion 或 animation 配置字段

#### Scenario: 旧万能字段不得成为双权威
- **WHEN** 默认状态机资产完成模块迁移
- **THEN** 运行时 MUST 只读取模块配置作为状态能力来源
- **AND** 旧 `output`、`animation`、`variants` 或等价万能字段 MUST NOT 与模块配置并行决定同一输出

### Requirement: 输出通道替代互斥 owner 分支
系统 MUST 将状态帧输出表达为 motion、animation、input、latch、timeline、runtime facts 等输出通道或等价纯数据结果。`Locomotion / Action` MAY 作为诊断或兼容事实从模块输出派生，但 MUST NOT 作为决定是否执行 motion、是否播放 animation、是否消费输入的互斥运行时分支权威。

#### Scenario: Action 动画由输出通道驱动
- **WHEN** 当前节点通过模块产出动作动画请求
- **THEN** FullBody pipeline MUST 根据 animation output channel 或等价输出播放动作动画
- **AND** MUST NOT 仅通过 `Owner.IsAction` 判断是否播放动作动画

#### Scenario: Locomotion 动画由模块事实驱动
- **WHEN** 当前节点通过 Locomotion phase 模块产出基础移动表现请求
- **THEN** 动画 adapter MUST 使用 phase 与运行时 gait facts 解析具体基础移动动画
- **AND** 状态节点 MUST NOT 直接配置 Walk/Run 作为逻辑子状态

#### Scenario: 兼容 owner 只读派生
- **WHEN** 诊断或旧测试读取当前 owner
- **THEN** owner MAY 从当前节点模块组合派生
- **AND** 派生 owner MUST NOT 反向决定状态图 transition 或输出系统分支

### Requirement: 模块组合校验
系统 MUST 校验状态节点模块组合的合法性，确保每个模块有明确输出或事实用途，并防止同一职责出现多个权威来源。校验 MUST 覆盖默认状态机资产，并对无效组合报告明确错误。

#### Scenario: Dodge 模块组合合法
- **WHEN** 校验默认 `Dodge` 节点
- **THEN** 节点 MUST 包含动作请求、动作位移、动作动画和输入消费能力
- **AND** 每个 Dodge 变体 MUST 能解析到稳定 animation key
- **AND** 动作位移时长和距离 MUST 只有一个正式配置来源

#### Scenario: TurnBack alias 不重复
- **WHEN** 校验默认 `TurnBack` 节点
- **THEN** timeline binding、motion policy 和 animation alias MUST 共享同一正式 alias 来源或明确映射
- **AND** 状态机资产 MUST NOT 同时要求设计者在两个字段重复填写 `Locomotion.Turn.Back`

#### Scenario: 普通 Locomotion 不携带无效动画模块
- **WHEN** 校验 `Idle`、`MoveStart`、`MoveLoop`、`MoveStop`
- **THEN** 这些节点 MUST NOT 要求配置 action animation key
- **AND** MUST NOT 暴露或读取无效的 action movement 模块

### Requirement: 当前 runner 对模块模型的支撑边界
系统 MUST 在现有自研统一状态图 runner 上实现节点模块模型，而不是新增第二套状态机 runtime。现有 runner MAY 继续负责 active state、state time、variant、transition、pending path 和 restore；模块解析、输出聚合和事实采样 MUST 保持纯数据并位于明确 solver 子职责中。

#### Scenario: 保留单一 runner owner
- **WHEN** 模块化节点配置接入运行时
- **THEN** `PlayerFullBodyActionController` 或等价正式入口 MUST 继续是唯一正式 runner owner
- **AND** 系统 MUST NOT 新增 parallel ECS state runner、per-action runner 或独立 Locomotion runner

#### Scenario: Runner 不知道具体模块副作用
- **WHEN** runner 推进一帧状态
- **THEN** runner MUST NOT 直接播放 Animancer
- **AND** MUST NOT 直接执行 movement
- **AND** MUST NOT 直接消费 Unity 输入对象
- **AND** 模块输出 MUST 通过 FullBody pipeline adapter 执行副作用

### Requirement: 自研统一分层状态图运行时
系统 MUST 将当前角色 FullBody base layer 的正式状态机定义为项目自研的统一分层状态图运行时。该状态机 MUST 同时满足单一权威和分层路径表达：`FullBody/Locomotion/...` 与 `FullBody/Action/...` MUST 属于同一棵状态树，而不得被描述或实现为两个并列状态机再由外层缝合。UnityHFSM MAY 作为参考资料或未来另行审批的 adapter 方向存在，但当前正式角色主线 MUST NOT 默认迁移到 UnityHFSM。

#### Scenario: 统一和分层同时成立
- **WHEN** 设计者查看默认角色状态机配置
- **THEN** 配置 MUST 表达 `FullBody` 根节点
- **AND** MUST 表达 `FullBody/Locomotion` 子域
- **AND** MUST 表达 `FullBody/Action` 子域
- **AND** Locomotion 和 Action 的叶子状态 MUST 共享同一个 active path、state time、variant 和 snapshot 来源

#### Scenario: UnityHFSM 不是正式主线
- **WHEN** 后续实现角色业务状态机功能
- **THEN** 实现 MUST 继续扩展项目自研状态图运行时
- **AND** MUST NOT 在未审批 proposal 中把 UnityHFSM 接入为正式角色状态机 engine
- **AND** MUST NOT 同时保留 UnityHFSM runtime 和自研 runner 作为双状态权威

#### Scenario: 分层路径不暴露第三方内部对象
- **WHEN** 读取运行时状态快照
- **THEN** 快照 MUST 暴露稳定状态 id、active path、state time、variant、pending transition 或等价纯数据
- **AND** MUST NOT 暴露 UnityHFSM state 对象
- **AND** MUST NOT 暴露 Animancer state、Animator state、CharacterController、InputAction 或 Transform

### Requirement: 状态图运行时职责收窄
系统 MUST 将状态图运行时的核心职责收窄为解释状态图、求值 transition、维护 active state、维护 state time、维护 variant、记录 pending transition 诊断和提供纯数据 snapshot/restore。Timeline facts 采样、状态输出解析、运动命令构建、动画请求构建、输入消费、run latch 写入和诊断提交 MUST 位于明确的外围模块或明确的子职责中，不得继续让 runner 直接成为 FullBody、Locomotion、Action、Animation 和 Motion 的混合实现。

#### Scenario: Runner 只推进状态
- **WHEN** 状态图运行时 tick 一帧
- **THEN** 它 MUST 根据状态图配置和 context facts 选择 transition
- **AND** MUST 更新 active state、state time、variant 和 pending transition 诊断
- **AND** MUST NOT 调用 motion executor
- **AND** MUST NOT 调用 animation presenter
- **AND** MUST NOT 直接消费输入缓冲

#### Scenario: Timeline 采样独立
- **WHEN** Action request submission arbiter 或 transition 条件需要 timeline facts
- **THEN** 系统 MUST 通过独立 sampler 或等价纯数据模块提供 `StateTimelineWindowFacts`
- **AND** Action request submission arbiter MUST NOT 反向依赖 runner 的实现方法采样 timeline
- **AND** sampler MUST NOT 切换状态

#### Scenario: 状态输出独立
- **WHEN** active state 已经确定
- **THEN** 系统 MUST 通过 state output resolver 或等价模块生成运动、动画、输入消费、run latch 和 TurnBack policy 输出
- **AND** output resolver MUST 只返回纯数据
- **AND** output resolver MUST NOT 执行 `CharacterController.Move`
- **AND** output resolver MUST NOT 播放 Animancer 或 Animator

#### Scenario: Restore 只保存可恢复状态推进事实
- **WHEN** 捕获状态图 restore state
- **THEN** restore state MUST 保存重放所需的 active state、state time、variant、pending transition 和必要的状态 payload
- **AND** MUST NOT 保存 Unity 对象
- **AND** MUST NOT 保存可以从配置或 frame context 重新推导的表现层对象

### Requirement: 经典状态生命周期接口
系统 MUST 在自研统一分层状态机运行时内部提供经典 `Enter / Tick / Exit` 或等价生命周期接口。该接口 MUST 只读取纯数据 context、维护可恢复状态 payload、产出纯数据 frame 输出；接口实现 MUST NOT 直接执行运动、播放动画、消费 Unity 输入对象或写 Unity 场景对象。运行时对外仍 MUST 以单次 `Tick(context)` 产出一个 `CharacterStateMachineFrame` 或等价帧结果。

#### Scenario: Enter 产出进入状态的一次性输出
- **WHEN** transition 选择了新的目标状态
- **THEN** 运行时 MUST 调用目标状态的 `Enter` 或等价生命周期
- **AND** Enter MUST 能初始化 state time、variant、方向 payload、动画语义 key 和输入消费意图
- **AND** Enter MUST NOT 直接调用 Animancer、Animator、CharacterController、InputAction 或 Transform

#### Scenario: Tick 产出当前状态持续输出
- **WHEN** 当前状态在本帧保持 active
- **THEN** 运行时 MUST 调用 active 状态的 `Tick` 或等价生命周期
- **AND** Tick MUST 能产出当前帧运动、动画请求、timeline 相关输出和诊断事实
- **AND** Tick MUST 只产出纯数据，由 FullBody pipeline 执行副作用

#### Scenario: Exit 产出离开状态的一次性输出
- **WHEN** 当前状态要切换到目标状态
- **THEN** 运行时 MUST 在切换 active state 前调用旧状态的 `Exit` 或等价生命周期
- **AND** Exit MUST 能产出 run latch、清理 action payload、离开 TurnBack payload 或等价一次性输出
- **AND** Exit MUST NOT 直接清理动画 presenter 或执行 movement

#### Scenario: 对外仍是单 frame 输出
- **WHEN** 一帧内发生 transition
- **THEN** Exit、Enter 和 Tick 输出 MUST 合并为同一个 `CharacterStateMachineFrame` 或等价帧结果
- **AND** 调用方 MUST NOT 需要按 Enter/Exit/Tick 三条外部管线分别执行副作用

### Requirement: 状态机文档口径一致
系统 MUST 让项目文档、agent 指南和 OpenSpec 对角色状态机采用同一口径：角色正式主线是项目自研统一分层状态机，输入、运动、动画、相机和诊断为外围 adapter。文档 MUST NOT 继续建议后续角色业务状态机优先使用 UnityHFSM，除非新 proposal 明确批准迁移。

#### Scenario: Agent 指南不误导
- **WHEN** agent 阅读项目根文档和状态机指南
- **THEN** 文档 MUST 明确当前角色主线使用自研统一分层状态机
- **AND** MUST 明确 UnityHFSM 不是当前角色主线优先 engine
- **AND** MUST 明确如需改用 UnityHFSM 必须另开 OpenSpec proposal

#### Scenario: 架构文档不使用旧 BBB 主线
- **WHEN** agent 阅读 `openspec/project.md`
- **THEN** 文档 MUST NOT 把 `BBBCharacterController`、`PlayerStateRegistry`、`PlayerBaseState` 或 BBB `StateMachine` 描述为当前项目正式角色主线
- **AND** MUST 描述当前主线的 FullBody pipeline、自研分层状态机、motion executor 和 Animancer presenter 边界

#### Scenario: 文档保留预测回滚约束
- **WHEN** 文档描述状态机与预测回滚关系
- **THEN** 文档 MUST 说明状态机 restore、snapshot 和 replay 只使用纯数据事实
- **AND** MUST 说明不得为了网络或回滚新建第二套状态机路径

### Requirement: TurnBack 入口只消费仲裁请求事实
统一状态机默认 TurnBack 进入路径 MUST 只消费已经被状态请求仲裁入口接受的 `CharacterInputRequestFact(InputRequestKind.TurnBack)` 或等价 accepted request fact。`LocomotionTurnBackIntent` MAY 作为候选事实存在，但 MUST NOT 直接作为默认 `MoveStart -> TurnBack` 或 `MoveLoop -> TurnBack` transition 的权威条件。

#### Scenario: accepted TurnBack request 进入状态
- **GIVEN** 当前状态为 `FullBody/Locomotion/MoveStart` 或 `FullBody/Locomotion/MoveLoop`
- **AND** locomotion facts 中存在有效 `LocomotionTurnBackIntent`
- **AND** 状态请求仲裁入口接受 TurnBack 请求并生成 accepted TurnBack request fact
- **WHEN** 统一状态机推进本帧
- **THEN** 状态机 MUST 进入 `FullBody/Locomotion/TurnBack`
- **AND** TurnBack 方向 MUST 来自 accepted request fact 的 world direction

#### Scenario: intent-only 不进入状态
- **GIVEN** 当前状态为 `FullBody/Locomotion/MoveStart` 或 `FullBody/Locomotion/MoveLoop`
- **AND** locomotion facts 中存在有效 `LocomotionTurnBackIntent`
- **AND** 本帧没有 accepted TurnBack request fact
- **WHEN** 统一状态机推进本帧
- **THEN** 状态机 MUST NOT 进入 `FullBody/Locomotion/TurnBack`

#### Scenario: rejected TurnBack request 不进入状态
- **GIVEN** 输入方向满足 TurnBack 候选条件
- **AND** 状态请求仲裁入口拒绝 TurnBack 请求
- **WHEN** 统一状态机推进本帧
- **THEN** 状态机 MUST NOT 进入 `FullBody/Locomotion/TurnBack`
- **AND** rejected 请求 MUST NOT 被转换为状态机 accepted request fact

#### Scenario: transition evaluator 不重复裁决 TurnBack
- **WHEN** 检查默认 TurnBack 进入 transition
- **THEN** 该 transition MUST 使用 `HasInputRequest(InputRequestKind.TurnBack)` 或等价 accepted request fact 条件
- **AND** MUST NOT 使用 `MoveTurnBackRequested` 或等价 intent 直读条件作为进入权威
- **AND** transition evaluator MUST NOT 重新计算 TurnBack priority、resistance 或 window policy

### Requirement: 状态输出声明动画运动源策略
统一状态机 MUST 允许逻辑状态输出声明通用动画运动源策略。该策略 MUST 是纯数据输出，由后续 locomotion/motion pipeline 消费，不得让状态机 runner 直接调用 Animator、Animancer、CharacterController 或 motion executor。

#### Scenario: 状态输出携带策略
- **GIVEN** 设计者在状态配置中为某个状态启用动画运动源
- **WHEN** 统一状态机产出该状态的状态帧
- **THEN** 状态帧 MUST 携带该动画运动源策略
- **AND** 策略 MUST 能表达 yaw source、translation source、source id 和输入抑制语义

#### Scenario: Runner 保持纯数据边界
- **WHEN** 统一状态机 runner 构建状态帧
- **THEN** runner MUST NOT 采样 AnimationClip
- **AND** MUST NOT 读取 Animancer runtime state
- **AND** MUST NOT 调用 `CharacterController.Move`

#### Scenario: TurnBack 使用通用策略
- **GIVEN** 当前状态为 `FullBody/Locomotion/TurnBack`
- **WHEN** 状态输出声明 TurnBack 动画运动源策略
- **THEN** 后续管线 MUST 按通用动画运动源能力处理 TurnBack yaw 和 translation
- **AND** MUST NOT 在状态机 runner 内写入 TurnBack 专用运动逻辑

### Requirement: TurnBack Locomotion 正式状态契约
系统 MUST 将移动反向急转表达为 `FullBody/Locomotion/TurnBack` 正式逻辑状态，并由该状态声明本次转身的动画请求、目标朝向、运动权威策略、输入抑制、动画进入时间和退出窗口。默认 TurnBack 动画只允许从 `FullBody/Locomotion/MoveLoop` 且当前 gait 为 Run 时进入；Walk、MoveStart、MoveStop 和 Idle MUST NOT 直接触发该 TurnBack 动画。TurnBack MUST 仍由统一状态机 transition 进入，MUST NOT 由动画外观层、motion executor 或 controller 特判直接切换状态。

#### Scenario: TurnBack 由统一状态机进入
- **GIVEN** 当前状态为 `FullBody/Locomotion/MoveLoop`
- **AND** 当前 gait 为 Run
- **AND** 统一 Locomotion 决策事实中存在有效 TurnBack intent
- **WHEN** `MoveTurnBackRequested` 或等价 transition 条件通过
- **THEN** 统一状态机 MUST 进入 `FullBody/Locomotion/TurnBack`
- **AND** 进入行为 MUST 锁定本次目标朝向或目标方向
- **AND** 动画外观层 MUST NOT 直接调用状态切换 API

#### Scenario: WalkLoop 不直接进入 TurnBack
- **GIVEN** 当前状态为 `FullBody/Locomotion/MoveLoop`
- **AND** 当前 gait 为 Walk
- **AND** 统一 Locomotion 决策事实中存在有效 TurnBack intent
- **WHEN** 统一状态机评估本帧 transition
- **THEN** 状态机 MUST NOT 进入 `FullBody/Locomotion/TurnBack`

#### Scenario: MoveStart 和 MoveStop 不直接进入 TurnBack
- **GIVEN** 当前状态为 `FullBody/Locomotion/MoveStart` 或 `FullBody/Locomotion/MoveStop`
- **AND** 统一 Locomotion 决策事实中存在有效 TurnBack intent
- **WHEN** 统一状态机评估本帧 transition
- **THEN** 状态机 MUST NOT 进入 `FullBody/Locomotion/TurnBack`

#### Scenario: Idle 不直接进入 TurnBack
- **GIVEN** 当前状态为 `FullBody/Locomotion/Idle`
- **AND** 统一 Locomotion 决策事实中存在有效 TurnBack intent
- **WHEN** 统一状态机评估本帧 transition
- **THEN** 状态机 MUST NOT 进入 `FullBody/Locomotion/TurnBack`

#### Scenario: TurnBack 锁定目标不被相机抖动覆盖
- **GIVEN** 角色已经进入 `FullBody/Locomotion/TurnBack`
- **AND** 本次 TurnBack 已锁定目标朝向
- **WHEN** 后续帧相机朝向或输入基准发生变化
- **THEN** TurnBack 状态 MUST 继续使用进入时锁定的目标朝向
- **AND** MUST NOT 每帧重新用相机基准改写本次转身目标

#### Scenario: TurnBack 状态声明输入抑制
- **GIVEN** 当前逻辑状态为 `FullBody/Locomotion/TurnBack`
- **WHEN** 系统构建本帧运动命令
- **THEN** TurnBack 状态输出 MUST 声明普通输入旋转被抑制
- **AND** MUST 声明普通输入平面位移被抑制

#### Scenario: TurnBack 状态声明动画时间窗口
- **GIVEN** 当前逻辑状态为 `FullBody/Locomotion/TurnBack`
- **WHEN** 系统构建状态输出
- **THEN** TurnBack 状态输出 MUST 能携带进入 fade、start normalized time、输入锁定窗口、转完点和退出窗口
- **AND** 这些时间事实 MUST 可由配置或 baked motion profile 提供

#### Scenario: TurnBack 按转完点退出
- **GIVEN** 当前逻辑状态为 `FullBody/Locomotion/TurnBack`
- **AND** TurnBack policy 配置了转完 normalized time 或等价 marker
- **WHEN** 动画播放进度达到转完点
- **AND** 当前仍有移动输入
- **THEN** 状态机 MUST 转入 `FullBody/Locomotion/MoveLoop`
- **WHEN** 动画播放进度达到转完点
- **AND** 当前没有移动输入
- **THEN** 状态机 MUST 转入 `FullBody/Locomotion/Idle`

#### Scenario: TurnBack 不等待跑步尾巴
- **GIVEN** `Locomotion.Turn.Back` 动画包含转身后的跑步尾巴
- **WHEN** TurnBack 已达到转完点
- **THEN** 状态机 MUST 允许退出 TurnBack
- **AND** MUST NOT 要求整段动画播放结束后才能交还普通移动

### Requirement: Runner 状态 payload 通用化
统一状态机 runner MUST 只维护状态图推进所需的通用可恢复事实。Action locked direction、TurnBack locked direction、TurnBack entry basis forward 或后续 Attack/Jump/HitReact payload MUST 通过通用 state payload、状态输出或等价纯数据 carrier 表达，runner MUST NOT 以专用字段或 `CharacterStateIds.*` 特判保存具体业务状态 payload。

#### Scenario: TurnBack payload 不在 runner 专用字段中
- **GIVEN** accepted TurnBack request 进入 `FullBody/Locomotion/TurnBack`
- **WHEN** runner 应用 transition
- **THEN** TurnBack locked direction 和 entry basis forward MUST 写入通用 state payload 或等价输出数据
- **AND** runner MUST NOT 通过 `turnBackWorldDirection`、`turnBackEntryBasisForward` 专用字段保存
- **AND** 行为输出 MUST 仍能使用进入时锁定方向

#### Scenario: Action payload 不在 runner 专用字段中
- **GIVEN** accepted Dodge request 进入 `FullBody/Action/Dodge`
- **WHEN** runner 应用 transition
- **THEN** action locked direction 和 variant MUST 通过通用 state payload 或等价输出数据提供给 state output
- **AND** runner MUST NOT 通过 action 专用 direction 字段保存
- **AND** rollback restore 后 Dodge 方向 MUST 保持确定

#### Scenario: 新状态 payload 不修改 runner 字段
- **WHEN** 后续新增 Attack、Jump 或 HitReact 状态 payload
- **THEN** 新 payload MUST 通过通用 payload carrier 接入
- **AND** MUST NOT 要求在 runner 中新增 `attackPayload`、`jumpPayload`、`hitReactPayload` 或等价专用字段

### Requirement: Snapshot 与 FullBody 解释分离
`CharacterStateMachineSnapshot` MUST 只表达统一状态机身份和恢复诊断事实，包括 active state、active path、state time、variant、pending transition 和 tags。FullBody owner、ActionState、LocomotionPhase、IsAction、IsLocomotion 或等价业务解释 MUST 由外围 FullBody state view/adapter 从 snapshot 和状态定义派生，不能作为 snapshot 的核心职责。

#### Scenario: Snapshot 保持纯状态机身份
- **WHEN** 捕获状态机 snapshot
- **THEN** snapshot MUST 包含 active state、active path、state time、variant、pending transition 和 tags
- **AND** MUST NOT 暴露 FullBody owner 作为核心字段
- **AND** MUST NOT 暴露 Locomotion phase 或 ActionState 作为核心字段

#### Scenario: FullBody view 提供兼容解释
- **WHEN** FullBody pipeline、diagnostics、Locomotion adapter 或 Action facts 需要 owner、Locomotion phase 或 ActionState
- **THEN** 它们 MUST 通过 FullBody state view/adapter 或等价解释入口读取
- **AND** 该 view MUST 从 snapshot 和状态定义派生
- **AND** view MUST NOT 成为第二状态权威

#### Scenario: Snapshot 改名不破坏业务解释
- **WHEN** 状态 path 命名或层级结构调整但状态模块语义不变
- **THEN** FullBody 解释 MUST 优先使用状态定义、模块或受控 tag
- **AND** MUST NOT 仅依赖 `StartsWith("FullBody/Action")` 或最后 path segment 推导业务行为

