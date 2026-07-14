# character-state-timeline-authoring-loop Specification

## Purpose
定义 Corin 角色资产的 StateMachine + Timeline authoring 闭环：RootTree 只表达主流程层，Locomotion 和 Action 细节下钻到状态机与状态行为内联图，Timeline 负责具体动作/移动表现与事实输出，不再把攻击节点、window、cue 或 loopback result 平铺在 RootTree。
## Requirements

### Requirement: Corin RootTree 必须只表达角色主流程层

Corin RootTree MUST 作为角色每 tick 的主流程编排层。RootTree SHOULD 包含 Runtime Loop、输入/运动入口、Locomotion StateMachine 和 Action StateMachine 等高层节点。RootTree MUST NOT 平铺某个具体攻击的 action activation、Timeline playback、lifecycle、window、cue 或 loopback result 输出节点。

#### Scenario: 打开 Corin RootTree

- **WHEN** 作者打开 Corin RootTree
- **THEN** 作者 SHOULD 看到 Locomotion StateMachine 和 Action StateMachine
- **AND** 具体 `Attack1` 的 Timeline、window、cue 和 lifecycle 细节 MUST 位于 Action StateMachine 的状态下钻图中
- **AND** RootTree MUST NOT 显示 `Activate Attack`、`Play Attack Timeline`、`Submit Attack Window`、`Submit Attack Cue` 或 `Submit Loopback Result`

### Requirement: Corin Locomotion 必须使用 StateMachine + Timeline 编排

Corin locomotion MUST 使用状态机表达基础移动与分层所有权状态。有独立时序内容的状态 MUST 在状态行为内通过 TimelineNode 播放对应 Timeline；没有独立动画资源的状态 MUST 使用 Transition blend 或无表现 ownership state 衔接，不得创建伪 Timeline 或 fallback clip。状态至少包含 `Idle`、`WalkStart`、`WalkLoop`、`WalkEnd`、`RunStart`、`RunLoop`、`RunEnd`、`MovingTurn` 和 `ActionOverride`。所有 start、end、loop、turn 和 ownership 状态 MUST 通过明确 Transition 响应输入与 ownership fact，并复用统一 State source-exit 生命周期。

#### Scenario: WalkStart 输入抢占

- **WHEN** WalkStart root 尚未完成且输入进入 Run 或 Stop 区间
- **THEN** 状态机 MUST 分别允许进入 RunStart 或 WalkEnd
- **AND** source TimelineNode MUST 通过 State root stop 取消

#### Scenario: WalkEnd 恢复移动

- **WHEN** WalkEnd root 尚未完成且输入进入 Walk 或 Run 区间
- **THEN** 状态机 MUST 分别允许进入 WalkStart 或 RunStart
- **AND** 没有 WalkEnd 独立动画时 MUST 使用 Transition blend

#### Scenario: RunStart 输入抢占

- **WHEN** RunStart root 尚未完成且输入进入 Stop 或 Walk 区间
- **THEN** 状态机 MUST 分别允许进入 RunEnd 或 WalkLoop
- **AND** MUST NOT 等待 RunStart Timeline 自然完成

#### Scenario: RunEnd 恢复移动

- **WHEN** RunEnd root 尚未完成且输入进入 Walk 或 Run 区间
- **THEN** 状态机 MUST 分别允许进入 WalkStart 或 RunStart
- **AND** 输入恢复边 MUST 优先于 Completed AND Stop 的 Idle 边

#### Scenario: RunLoop 与 MovingTurn 输入抢占

- **WHEN** RunLoop 或 MovingTurn 的输入进入 Stop、Walk 或有效 Run/Turn 区间
- **THEN** 状态机 MUST 按明确 edge 切换到 RunEnd、WalkLoop、MovingTurn 或 RunLoop
- **AND** 同 source 多条边 MUST 使用稳定 priority

#### Scenario: Dodge 活跃时交出 locomotion 所有权

- **WHEN** 任一普通 locomotion state 读取到 pipeline blackboard `IsDodging=true`
- **THEN** Locomotion StateMachine MUST 以高优先级进入 ActionOverride
- **AND** ActionOverride MUST NOT 播放动画、引用 Dodge Timeline 或提交 motion contribution

#### Scenario: Dodge 完成后有输入进入 RunLoop

- **WHEN** ActionOverride 读取到 `IsDodging=false`
- **AND** 当前 MoveAxis 大于 stop threshold
- **THEN** Locomotion StateMachine MUST 直接进入 RunLoop
- **AND** MUST NOT 重复进入 RunStart

#### Scenario: Dodge 完成后无输入进入 RunEnd

- **WHEN** ActionOverride 读取到 `IsDodging=false`
- **AND** 当前 MoveAxis 不大于 stop threshold
- **THEN** Locomotion StateMachine MUST 进入 RunEnd

#### Scenario: ActionOverride 保持单一职责

- **WHEN** 作者下钻 ActionOverride StateNode
- **THEN** inline state body MUST 不包含 Dodge request consume、ActionProfile、Timeline、animation 或 motion node
- **AND** 项目 MUST NOT 为 ActionOverride 创建一次性 SubTree asset

#### Scenario: MovingTurn 使用角色朝向误差

- **WHEN** RunLoop 中有效 Run 输入的 camera-relative 期望世界方向与 tick 起点 actor forward 夹角达到 turn threshold
- **THEN** Locomotion StateMachine MUST 进入 MovingTurn
- **AND** 条件 MUST NOT 使用相邻 logic tick 的 MoveAxis 差角替代 actor facing error
- **AND** turn threshold MUST 继续来自可调 ExposedProperty

#### Scenario: ownership edge 优先级

- **WHEN** 同一 source state 同时满足普通 Walk/Run/Turn 条件和 `IsDodging=true`
- **THEN** ActionOverride edge MUST 使用稳定更高 priority 获胜
- **AND** 状态机 MUST NOT 创建重复 source-target edge

### Requirement: Corin 基础连招必须使用 Action StateMachine + Timeline 编排

Corin 外层 Action StateMachine MUST 只表达动作大类，至少包含 `None`、`Attack`、`DodgeBack` 和 `DodgeForward`。具体 `Attack1`、`Attack2` MUST 位于 `Attack` StateNode 的 inline StateBehaviorSubTree Root 所运行的内层 `StateMachineNode` 中，MUST NOT 与 Dodge 状态平铺。内层攻击状态 MUST 继续使用 ActionProfile、独立 Action Context 和带 Action Context 的 inline TimelineNode。连段 MUST 使用与 Tree abort 相同的 Runnable stop、State source-exit、Action lifecycle 和 Timeline cancel 分层，不得创建 Action 专用旁路。

#### Scenario: 首次进入 Attack1

- **WHEN** 外层 `None` 检测到 Attack request
- **THEN** 外层 Action StateMachine MUST 进入 `Attack`
- **AND** 外层条件 MUST 只查询而不消费该 request
- **AND** 内层 Attack StateMachine MUST 进入 `Attack1`
- **AND** `Attack1` target activation MUST 消费 request 并创建新的 Action Context

#### Scenario: Attack1 进入 Attack2

- **WHEN** `Attack1Cancel` 在当前 Tick active 且存在 Attack request
- **THEN** 内层 Attack StateMachine MUST 从 `Attack1` 抢占到 `Attack2`
- **AND** source OnExit MUST 提交 `Cancel(ComboWindow)`
- **AND** target activation MUST 消费 request 并创建新的 Action Context

#### Scenario: Attack2 回到 Attack1

- **WHEN** `Attack2Cancel` 在当前 Tick active 且存在 Attack request
- **THEN** 内层 Attack StateMachine MUST 从 `Attack2` 抢占到 `Attack1`
- **AND** condition query MUST NOT 消费 request

#### Scenario: 攻击正常结束

- **WHEN** `Attack1` 或 `Attack2` root 正常完成且没有窗口连段
- **THEN** leaf source MUST 提交一次 Complete 并进入内层 Exit
- **AND** 外层 Attack root MUST 因嵌套 StateMachineNode 成功而完成
- **AND** 外层 Action StateMachine MUST 通过 `StateRootCompleted` 回到 None
- **AND** 外层 Attack OnExit MUST NOT 提交第二条 Action terminal transition

#### Scenario: 打开外层 Action StateMachine

- **WHEN** 作者打开 Corin Action StateMachine
- **THEN** 作者 MUST 看到 `None`、`Attack`、`DodgeBack` 和 `DodgeForward`
- **AND** 作者 MUST NOT 在该层看到 `Attack1` 或 `Attack2`
- **AND** 作者下钻 `Attack` state body 后 MUST 能继续打开 inline Attack Combo StateMachine

### Requirement: Corin 资产闭环不得创建一次性 SubTree asset

Corin 的 `Locomotion` 状态行为和基础连招状态行为 MUST 默认保存为 StateNode 内部 inline graph data。只有多个状态明确复用同一行为图时，作者 MAY 显式抽取 shared `BaseTreeAsset`。

#### Scenario: Attack1 状态行为

- **WHEN** 作者下钻 `Attack1` StateNode
- **THEN** 编辑器 MUST 打开该 StateNode 的 inline StateBehaviorSubTree
- **AND** 项目 MUST NOT 为这个一次性状态 body 创建 `Attack1SubTree.asset`

#### Scenario: 显式复用

- **WHEN** 作者决定 `WalkLoop` 和其它角色复用同一行为图
- **THEN** 作者 MAY 使用 Extract Shared 或等价显式复用操作创建 shared asset
- **AND** owner inline 真数据 MUST 被清理

### Requirement: Corin 循环 locomotion 状态必须使用 TimelineNode Loop 播放模式

Corin locomotion 中表达持续姿态或持续移动的循环状态 MUST 通过状态 body 内的 `TimelineNode PlaybackMode=Loop` 播放对应 Timeline。状态 body MUST NOT 使用普通 `LoopNode` 包住 `TimelineNode` 来表达动画循环。RootTree 顶层 `Runtime Loop` MAY 保留，用于角色主流程持续运行。

#### Scenario: Idle 循环姿态

- **WHEN** 作者打开 Corin `Idle` 状态 body
- **THEN** body SHOULD 直接包含播放 idle Timeline 的 `TimelineNode`
- **AND** 该 `TimelineNode` 播放模式 MUST 是 `Loop`
- **AND** body MUST NOT 通过普通 `LoopNode` 重启该 `TimelineNode`

#### Scenario: WalkLoop 和 RunLoop 持续移动

- **WHEN** 作者打开 Corin `WalkLoop` 或 `RunLoop` 状态 body
- **THEN** body SHOULD 直接包含对应 locomotion loop Timeline 的 `TimelineNode`
- **AND** 该 `TimelineNode` 播放模式 MUST 是 `Loop`
- **AND** 状态离开 MUST 由同层 Condition rule 决定

#### Scenario: 一次性 locomotion 状态

- **WHEN** 作者打开 `WalkStart`、`WalkEnd`、`RunStart`、`RunEnd` 或 `MovingTurn` 状态 body
- **THEN** 对应 TimelineNode SHOULD 使用 `Once` 播放模式
- **AND** `StateRootCompleted` 或其它正式 Transition 条件 MUST 决定状态离开

### Requirement: Corin TimelineNode 必须默认拥有 inline Timeline

Corin 的 Locomotion 与 Action 状态 Timeline MUST默认保存为对应 TimelineNode 私有的 inline TimelineData。只有多个节点明确复用同一 Timeline 时，作者 MAY显式 Extract Shared 或分配 shared TimelineAsset。Corin 一对一状态 Timeline MUST NOT继续保留为独立一次性 Timeline asset，也 MUST NOT通过 asset fallback 维持旧引用。

#### Scenario: 下钻 Attack1 Timeline

- **WHEN** 作者从 Attack1 State body 打开 Play Attack1 Timeline 节点
- **THEN** 独立 TimelineEditorWindow MUST绑定 Attack1 inline TimelineData
- **AND** 来源 Graph 窗口 MUST保持 Attack1 State body 可见
- **AND** TimelineEditorWindow MUST显示 Animation、Motion 和 Decision Tree tracks
- **AND** 作者从 Hit 或 Cancel TreeClip 下钻时 MUST在来源 Graph 窗口打开 inline TimelineRunningTree

#### Scenario: 下钻 locomotion Timeline

- **WHEN** 作者从 Idle、WalkStart、WalkLoop、RunStart、RunLoop、RunEnd 或 MovingTurn 状态 body 打开 TimelineNode
- **THEN** 独立 TimelineEditorWindow MUST绑定对应节点的 inline TimelineData
- **AND** 来源 Graph 窗口 MUST保持当前状态行为 Graph 可见
- **AND** 项目 MUST NOT要求对应一次性 Timeline asset 存在

#### Scenario: 显式复用 Timeline

- **WHEN** 作者决定多个状态或角色复用同一 Timeline
- **THEN** 作者 MUST通过 Extract Shared 或显式 Shared ownership 创建/选择 TimelineAsset
- **AND** owner inline 真数据 MUST被清理
- **AND** 每个 playback request MUST继续拥有独立 runtime clone

### Requirement: Corin inline Timeline 迁移必须保留 TreeClip 事实链路

Corin Attack1、Attack2、DodgeForward 和 DodgeBack Timeline 迁入 TimelineNode 后 MUST完整保留现有八个 Decision TreeClip。Hit、Cancel、IFrame 和 MoveCancel 的 frame range、phase、inline TimelineRunningTree、Blackboard declaration reference、fact projection 和 Action Context provenance MUST保持不变。迁移 MUST NOT恢复 ActionWindowTrack、ActionWindowClip、专用 Window reader 或 SubmitActionWindowSampleNode。

#### Scenario: 迁移 Attack TreeClip

- **WHEN** Attack1 和 Attack2 Timeline 从外部 asset 迁入 TimelineNode
- **THEN** Attack1Hit、Attack1Cancel、Attack2Hit 和 Attack2Cancel TreeClip MUST仍位于各自 resolved TimelineData
- **AND** TreeClip MUST继续写入同一 Root-owned Frame declarations
- **AND** WindowFactProjection MUST继续生成同一 ActionWindowSample identity

#### Scenario: 迁移 Dodge TreeClip

- **WHEN** DodgeForward 和 DodgeBack Timeline 从外部 asset 迁入 TimelineNode
- **THEN** 两个 CanDodgeMoveCancel 与两个 IFrame TreeClip MUST完整保留
- **AND** CanDodgeMoveCancel MUST保持 Projection=None
- **AND** IFrame declarations MUST保持 ActionWindow projection

#### Scenario: 删除旧 Timeline assets

- **WHEN** 11 个 Corin Timeline 的 tracks、clips、引用和 playback mode 已迁入对应 TimelineNode
- **THEN** 项目 MUST确认不存在剩余引用后删除 11 个旧 Timeline assets
- **AND** 系统 MUST NOT保留旧字段反序列化、兼容 wrapper 或 asset fallback

### Requirement: Corin Attack 迁移必须保持现有攻击事实与资源身份

将 Attack1/Attack2 迁入嵌套 StateMachine 时，系统 MUST 保持两段攻击各自的 ActionProfile、Action Context、Timeline playback mode、AnimationTrack、MotionCurveTrack、Hit/Cancel Decision TreeClip、WindowId、Digest、帧范围和 lifecycle reason。迁移 MUST 移动并重绑唯一 inline 数据，MUST NOT 克隆成父子两份真相或创建一次性 shared asset。

#### Scenario: 迁移 Attack1 Timeline

- **WHEN** Attack1 StateNode 从外层 Action graph 迁入内层 Attack graph
- **THEN** 原 Attack1 inline TimelineData MUST 归属迁移后的 Attack1 State body
- **AND** Hit/Cancel TreeClip 的帧范围、declaration reference 和 ActionWindow projection MUST 保持不变
- **AND** 项目 MUST NOT 新增 Attack1 TimelineAsset 或 Attack1SubTree asset

#### Scenario: 清理外层旧结构

- **WHEN** 嵌套 Attack graph 迁移完成
- **THEN** 外层旧 Attack1/Attack2 StateNode、combo edge、完成 edge 和 orphan rule graph MUST 被删除
- **AND** runtime、Snapshot 和 Validator MUST 只读取嵌套后的唯一结构

### Requirement: Corin 必须由逻辑层提交唯一 Base playback selection

Corin MUST保持单一 Base layer，并在 CharacterAnimationPresentationDefinition 配置 OutputPolicy=RequireOutput。Locomotion、ActionOverride、Dodge、外层 Action 与 nested combo MUST在逻辑层完成状态、打断和所有权决策，然后为 Base 提交唯一 AnimationPlaybackId。AnimationTrack.Priority、Presentation Driver、Tree route 与 Runtime arbitration MUST不参与该选择。

#### Scenario: Locomotion 正常运行

- **WHEN** ActionOverride 没有活动动作
- **THEN** Base selection MUST来自当前 Locomotion State 的正式 Timeline playback
- **AND** Idle、WalkStart、WalkLoop、RunStart、RunLoop、RunEnd 与 MovingTurn MUST按状态逻辑切换 selection

#### Scenario: Locomotion 进入 Dodge

- **WHEN** Dodge 获得动作所有权
- **THEN** Action 逻辑 MUST为 Base 选择 Dodge playback
- **AND** Animation 模块 MUST不比较 Dodge 与 Locomotion Priority

#### Scenario: Dodge 返回 Locomotion

- **WHEN** Dodge 完成且当前仍有移动输入
- **THEN** 逻辑层 MUST选择当前正式 Run playback
- **AND** 没有移动输入时 MUST选择 RunEnd、Idle 或其它由 Locomotion 状态确定的正式 playback
- **AND** Animation 模块 MUST不从历史 sample 或表现状态猜测返回目标

#### Scenario: Attack1 进入 Attack2

- **WHEN** nested Attack StateMachine 满足连段条件并切换到 Attack2
- **THEN** Action 逻辑 MUST将 Base selection 更新为 Attack2 playback
- **AND** State transition edge MUST只保存逻辑 condition 与 priority

#### Scenario: 无动画 WalkEnd

- **WHEN** WalkEnd 本身没有 animation producer
- **THEN** 本次逻辑提交 MUST省略 Base 更新以保持当前正式 selection，或直接选择目标状态的正式 producer
- **AND** Animation 模块 MUST不为 WalkEnd 创建 fallback Timeline

#### Scenario: 同 tick 多次状态变化

- **WHEN** 一个 logic tick 内 RunLoop、MovingTurn 与 Action ownership 连续变化
- **THEN** Pipeline MUST只提交最终 Base selection
- **AND** playback generation 的 Complete/Release MUST继续保序

### Requirement: Corin animation producer 必须绑定 Animancer 原生 transition

Corin 每个正式 Timeline animation producer MUST拥有稳定 presentation key，并通过 CharacterAnimationPresentationDefinition 绑定到 Animancer transition key/source。CharacterPipelineDefinition Inspector MUST按稳定 identity 列出 Locomotion、Action、Attack1、Attack2 与 Dodge producer 的 Layer 与 binding，但 MUST不复制 producer 之间的逻辑关系；Graph/State edge MUST不保存 transition strategy、duration、curve 或 Driver。

#### Scenario: 配置 Attack1 与 Attack2

- **WHEN** 作者在 CharacterPipelineDefinition Inspector 查看 Attack1 和 Attack2
- **THEN** Inspector MUST显示两个 producer 的 stable key 与 Animancer binding
- **AND** source-target fade duration MAY由 Animancer TransitionLibrary modifier 配置
- **AND** Pipeline MUST不创建第二张 pair transition 表

#### Scenario: 配置 Locomotion 与 Dodge

- **WHEN** 作者调整 Dodge 的进入或退出表现
- **THEN** 调整 MUST落在 Animancer 原生 transition/library 数据
- **AND** RootTree、Parallel edge 与 StateMachine edge MUST保持纯逻辑

#### Scenario: 缺失 producer binding

- **WHEN** selected Corin producer 没有合法 Animancer transition binding
- **THEN** runtime MUST报告明确配置错误
- **AND** MUST不使用默认 Idle、当前 clip 或 Immediate fallback
