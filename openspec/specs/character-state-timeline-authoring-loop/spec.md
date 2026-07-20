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

Corin locomotion MUST 使用状态机表达基础移动与分层所有权状态。有独立时序内容的状态 MUST 在状态行为内通过 TimelineNode 播放对应 Timeline；没有独立动画资源的状态 MUST 使用 Transition blend 或无表现 ownership state 衔接，不得创建伪 Timeline 或 fallback clip。状态至少包含 `Idle`、`WalkStart`、`WalkLoop`、`WalkEnd`、`RunStart`、`RunLoop`、`RunEnd`、`MovingTurn` 和 `ActionOverride`。所有 start、end、loop、turn 和 ownership 状态 MUST 通过明确 Transition 响应输入与 `HasActionLocomotionOwnership`，并复用统一 State source-exit 生命周期。RunEnd MUST 只表达 locomotion 从实际 Run 状态停止，不得作为 Action 结束后的通用恢复状态。

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

#### Scenario: Full-body Action 活跃时交出 locomotion 所有权

- **WHEN** 任一普通 locomotion state 读取到 `HasActionLocomotionOwnership=true`
- **THEN** Locomotion StateMachine MUST 以高优先级进入 ActionOverride
- **AND** ActionOverride MUST NOT 播放动画、引用 Action Timeline 或提交 motion contribution

#### Scenario: Action 完成后有输入进入 RunLoop

- **WHEN** ActionOverride 读取到 `HasActionLocomotionOwnership=false`
- **AND** 当前 MoveAxis 大于 stop threshold
- **THEN** Locomotion StateMachine MUST 直接进入 RunLoop
- **AND** MUST NOT 重复进入 RunStart

#### Scenario: Action 完成后无输入进入 Idle

- **WHEN** ActionOverride 读取到 `HasActionLocomotionOwnership=false`
- **AND** 当前 MoveAxis 不大于 stop threshold
- **THEN** Locomotion StateMachine MUST 直接进入 Idle
- **AND** MUST NOT 播放 RunEnd

#### Scenario: ActionOverride 保持单一职责

- **WHEN** 作者下钻 ActionOverride StateNode
- **THEN** inline state body MUST 不包含 request consume、ActionProfile、Timeline、animation 或 motion node
- **AND** 项目 MUST NOT 为 ActionOverride 创建一次性 SubTree asset

#### Scenario: MovingTurn 使用角色朝向误差

- **WHEN** RunLoop 中有效 Run 输入的 camera-relative 期望世界方向与 tick 起点 actor forward 夹角达到 turn threshold
- **THEN** Locomotion StateMachine MUST 进入 MovingTurn
- **AND** 条件 MUST NOT 使用相邻 logic tick 的 MoveAxis 差角替代 actor facing error
- **AND** turn threshold MUST 继续来自可调 ExposedProperty

#### Scenario: ownership edge 优先级

- **WHEN** 同一 source state 同时满足普通 Walk/Run/Turn 条件和 `HasActionLocomotionOwnership=true`
- **THEN** ActionOverride edge MUST 使用稳定更高 priority 获胜
- **AND** 状态机 MUST NOT 创建重复 source-target edge

### Requirement: Corin 基础连招必须使用 Action StateMachine + Timeline 编排

Corin 外层 Action StateMachine MUST 只表达动作大类，并包含 `None`、`Attack` 和 `Dodge`。`Attack1`、`Attack2`、`Attack3`、`Attack4` 与 `Attack5` MUST 位于 Attack StateNode body 内的 inline StateMachineNode；`DodgeBack` 与 `DodgeForward` MUST 位于 Dodge StateNode body 内的 inline StateMachineNode。具体动作 leaf MUST 使用 ActionProfile、独立 Action Context 和带 Action Context 的 inline TimelineNode。连段、恢复取消与外层 replacement MUST 复用普通 ConditionRuleGraph、State edge、Runnable stop、source OnExit、Action lifecycle 和 Timeline cancel，不得创建 Action 专用旁路。没有成功闪避或成功格挡的 Combat Resolution 事实时，系统 MUST NOT 保留 RushAttack、CounterAttack 或按上一状态推导特殊攻击的路由。

#### Scenario: 首次进入 Attack1

- **WHEN** 外层 None 检测到 Attack request 且 `CanActivateAction(Attack)` 为 true
- **THEN** 外层 Action StateMachine MUST 进入 Attack category
- **AND** 外层条件 MUST 只查询而不消费该 request
- **AND** 内层 Attack StateMachine MUST 进入 `Attack1`
- **AND** `Attack1` target activation MUST 消费 request 并创建新的 Action Context

#### Scenario: 五段普通攻击连段

- **WHEN** Attack1..4 的 `ComboAccept` active、存在 Attack request 且下一段 admission 为 true
- **THEN** 内层 Attack StateMachine MUST 按 Attack1→2→3→4→5 抢占
- **AND** source OnExit MUST 提交一次 `Cancel(RecoveryCancel)`
- **AND** target activation MUST 消费 request 并创建新的 Action Context

#### Scenario: Attack5 是有限连段终段

- **WHEN** Attack5 播放期间再次收到 Attack request
- **THEN** 内层 StateMachine MUST NOT 从 Attack5 replacement 到 Attack1
- **AND** Attack5 MUST 只允许有效 Dodge、Move replacement 或 natural complete

#### Scenario: 攻击较早后摇被 Dodge 取消

- **WHEN** 任一 Attack leaf 的 `RecoveryEarly` active
- **AND** Dodge request 与 Dodge admission 成立
- **THEN** 该 source leaf 的内层 Exit edge MUST 优先于 Combo、Move 和 natural complete
- **AND** source lifecycle MUST 在 target Dodge activation 前明确关闭

#### Scenario: 攻击较晚后摇被移动取消

- **WHEN** 任一 Attack leaf 的 `RecoveryLate` active 且当前 MoveAxis 大于 stop threshold
- **AND** 没有更高优先级 Dodge 或 Combo edge 获胜
- **THEN** leaf MUST 先退出 Attack 内层 StateMachine
- **AND** Locomotion MUST 在 ownership 释放后进入 RunLoop

#### Scenario: 攻击完整后摇自然结束

- **WHEN** 当前 Attack leaf 没有有效 Dodge、Combo 或 Move replacement
- **THEN** 其 End clip MUST 完整播放到 Timeline root terminal
- **AND** leaf MUST 提交一次 Complete 并退出到 None
- **AND** 无移动输入时 Locomotion MUST 回 Idle

#### Scenario: Dodge 恢复期接普通 Attack1

- **WHEN** DodgeBack 或 DodgeForward 的 `RecoveryOpen` active
- **AND** Attack request 与 Attack admission 成立
- **THEN** Dodge leaf MUST 先退出内层 Dodge StateMachine
- **AND** 外层 Dodge→Attack edge MUST 只在 `state_root_completed` 后路由
- **AND** 内层 Attack StateMachine MUST 通过普通 Enter 进入 Attack1

#### Scenario: Dodge 恢复期再次闪避或移动

- **WHEN** Dodge `RecoveryOpen` active
- **THEN** Attack edge MUST 高于 Dodge re-entry，Dodge re-entry MUST 高于 Move edge，Move MUST 高于 natural complete
- **AND** 所有 route MUST 使用显式 State transition，不得由 runtime 全局 priority 推导

#### Scenario: Dodge 无输入自然结束

- **WHEN** Dodge Timeline 自然完成且没有有效 Attack、Dodge 或 Move replacement
- **THEN** Dodge leaf MUST 提交一次 Complete 并退出到 None
- **AND** Locomotion MUST 直接回 Idle
- **AND** MUST NOT 经过 RunEnd

#### Scenario: 打开外层 Action StateMachine

- **WHEN** 作者打开 Corin Action StateMachine
- **THEN** 作者 MUST 只看到 `None`、`Attack` 和 `Dodge` 动作大类
- **AND** 作者下钻 Attack 或 Dodge state body 后 MUST 能继续打开对应 inline StateMachine

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

Corin 的 Locomotion 与 Action 状态 Timeline MUST 默认保存为对应 TimelineNode 私有的 inline TimelineData。只有多个节点明确复用同一 Timeline 时，作者 MAY 显式 Extract Shared 或分配 shared TimelineAsset。Compiler MUST 将 inline/shared resolved Timeline 编译为同一不可变 Program/Projection 合同；每个 playback MUST 只在 CharacterSimulationState 中获得独立 activation state，不得创建 TimelineData 或 TimelineRunningTree runtime clone。

#### Scenario: 下钻 Attack1 Timeline

- **WHEN** 作者从 Attack1 State body 打开 Play Attack1 Timeline 节点
- **THEN** 独立 TimelineEditorWindow MUST 绑定 Attack1 inline TimelineData
- **AND** 来源 Graph 窗口 MUST 保持 Attack1 State body 可见
- **AND** TimelineEditorWindow MUST 显示 Animation、Motion 和 Decision Tree tracks
- **AND** 作者从 Hit 或 Cancel TreeClip 下钻时 MUST 在来源 Graph 窗口打开 inline TimelineRunningTree authoring data

#### Scenario: 下钻 locomotion Timeline

- **WHEN** 作者从 Idle、WalkStart、WalkLoop、RunStart、RunLoop、RunEnd 或 MovingTurn 状态 body 打开 TimelineNode
- **THEN** 独立 TimelineEditorWindow MUST 绑定对应节点的 inline TimelineData
- **AND** 来源 Graph 窗口 MUST 保持当前状态行为 Graph 可见
- **AND** 项目 MUST NOT 要求对应一次性 Timeline asset 存在

#### Scenario: 显式复用 Timeline

- **WHEN** 作者决定多个状态或角色复用同一 Timeline
- **THEN** 作者 MUST 通过 Extract Shared 或显式 Shared ownership 创建/选择 TimelineAsset
- **AND** owner inline 真数据 MUST 被清理
- **AND** 每个 playback request MUST 继续拥有独立 Program state address

### Requirement: Corin inline Timeline Window 必须只有 owner-local 事实链

Attack1..5、DodgeForward 和 DodgeBack 的 inline Timeline MUST 以 Decision TreeClip 和 owner-local Bool Frame declaration 表达 Hit、IFrame、ComboAccept、RecoveryEarly、RecoveryLate 与 RecoveryOpen。ActionWindow projection MUST 保留 Action Context、WindowId、Digest、phase 和 frame range；ConditionRuleGraph 与 EndFrame fact MUST 消费同一 candidate。系统 MUST NOT 建 Root-owned per-state window key、WindowTrack、专用 submit node、cache 或 registry。

#### Scenario: Attack 窗口

- **WHEN** 作者打开任一 Attack inline Timeline
- **THEN** Hit、ComboAccept、RecoveryEarly 与 RecoveryLate MUST 位于该 owner
- **AND** projection MUST 指向当前 ActionInstance

#### Scenario: Dodge 窗口

- **WHEN** 作者打开 DodgeForward 或 DodgeBack inline Timeline
- **THEN** IFrame 与 RecoveryOpen MUST 位于该 owner
- **AND** RecoveryOpen MUST 能被同帧 typed WindowType query 读取
### Requirement: Corin Attack 资源与身份必须由 nested leaf 唯一拥有

Attack1..5 leaf MUST 各自唯一拥有 ActionProfile 引用、Action Context slot、inline Timeline、AnimationTrack、MotionCurveTrack、Window declaration 与 lifecycle 节点。外层 Attack category MUST 只拥有嵌套 StateMachine 和 category transition，MUST NOT 复制 leaf 数据或创建一次性 shared asset。

#### Scenario: 下钻 Attack leaf

- **WHEN** 作者下钻 Attack1..5 任一 StateNode
- **THEN** 对应 inline body 与 Timeline MUST 是该动作的唯一可写数据
- **AND** Snapshot、Compiler、Validator 与 runtime MUST 解析同一 owner identity

#### Scenario: 检查外层 Attack

- **WHEN** 作者查看外层 Attack category
- **THEN** MUST NOT 存在平铺 Attack leaf、重复 combo edge 或 orphan rule graph
### Requirement: Corin 必须由逻辑层提交唯一 Base playback selection

Corin MUST保持单一 Base layer，并在 `CharacterAnimationPresentationProfile` 配置 OutputPolicy=RequireOutput。Locomotion、ActionOverride、Dodge、外层 Action 与 nested combo MUST在逻辑层完成状态、打断和所有权决策，然后为 Base 提交唯一 AnimationPlaybackId。AnimationTrack.Priority、Presentation Driver、Tree route 与 Runtime arbitration MUST不参与该选择。

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
- **AND** 没有移动输入时 MUST选择 Idle playback，MUST NOT经过 RunEnd
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

### Requirement: Corin 全部 AnimationTrack 必须显式选择 Marker Sync 策略

Corin每个可达AnimationTrack MUST显式配置为`None`或`MarkerGroup`，不得保留Unspecified。选择 MUST根据该producer真实动画语义、Timeline Once/Loop call site和完整marker coverage作出，不得按Locomotion、Attack、Dodge、Turn等状态名称硬编码。没有AnimationTrack的状态 MUST不创建伪Timeline、伪clip或伪marker。

#### Scenario: 打开Corin完整作者清单

- **WHEN** Compiler或Agent Validator遍历Corin全部RootTree、nested StateMachine与inline/shared Timeline
- **THEN** 每个可达AnimationTrack MUST拥有明确sync mode
- **AND** 任一Unspecified track MUST阻止发布

#### Scenario: WalkEnd没有动画资源

- **WHEN** WalkEnd继续只依赖Animancer transition blend且没有AnimationTrack
- **THEN** 迁移 MUST不为WalkEnd创建一次性Timeline或fallback clip
- **AND** Marker Sync inventory MUST不制造不存在的producer

### Requirement: Corin WalkLoop 与 RunLoop 必须共享 Locomotion.Gait

Corin WalkLoop与RunLoop AnimationTrack MUST配置为`MarkerGroup/Cyclic`并共享`Locomotion.Gait` SyncGroupId。两者 MUST按各自真实动画frame配置至少覆盖左右支撑两个方向的marker segment，不得假设normalized time或动画长度相同。Locomotion状态transition时刻、motion request和WorldSolver结果 MUST不因Marker Sync改变。

#### Scenario: WalkLoop切换RunLoop

- **WHEN** Corin从WalkLoop进入RunLoop
- **THEN** Base层RunLoop MUST在整个共同可见fade期间持续跟随WalkLoop当前marker segment
- **AND** Gameplay状态与运动 MUST在原logic tick立即切换

#### Scenario: RunLoop切回WalkLoop

- **WHEN** Corin从RunLoop进入WalkLoop
- **THEN** WalkLoop MUST读取RunLoop当帧effective phase
- **AND** MUST不使用上一次WalkLoop activation留下的offset或cycle

### Requirement: Corin 有限动作只能在资源满足时加入 Marker Group

RunStart、RunEnd、MovingTurn、Attack1至Attack5、Dodge及其它one-shot producer MAY配置为`MarkerGroup/Finite`，但仅当真实clip能够从frame 0到DurationFrame提供完整marker coverage，且同Layer同组directed pair契约成立。资源不满足时 MUST显式配置None并保留普通Timeline sample + Animancer fade；不得伪造支撑marker。Attack combo、recovery、cancel、IFrame与damage MUST继续由Action Context、TreeClip window、ConditionRule和State transition决定，不能由Marker Sync代替。

#### Scenario: RunEnd具有完整步态marker

- **WHEN** RunEnd真实动画能够表达Locomotion.Gait全部有向segment并覆盖完整Timeline
- **THEN** 作者 MAY将其配置为`MarkerGroup/Finite`
- **AND** RunLoop进入RunEnd时 MUST使用通用Cyclic到Finite映射

#### Scenario: Attack动画没有共同姿态契约

- **WHEN** Attack1与Attack2是顺序连段动作但没有同组完整marker语义
- **THEN** 两者AnimationTrack MUST显式为None
- **AND** Attack1到Attack2 MUST继续由ComboAccept窗口、State transition与目标Timeline ClipIn控制

#### Scenario: 一组Action变体确实需要同步

- **WHEN** 多个Action producer真实共享同一姿态marker语义与完整coverage
- **THEN** 作者 MAY为它们建立独立Action Marker Group
- **AND** Runtime MUST复用通用MarkerSyncRuntime，不得增加Attack专用matcher

#### Scenario: 动作退出到Locomotion

- **WHEN** Action producer为None并结束到Locomotion
- **THEN** Animation Runtime MUST使用普通Animancer transition与target raw Timeline time
- **AND** MUST不从Action名称或上一状态伪造Locomotion.Gait phase

### Requirement: Corin animation producer 必须绑定 Animancer 原生 transition

Corin 每个正式 Timeline animation producer MUST拥有稳定 presentation key，并通过 `CharacterAnimationPresentationProfile` 绑定到 Animancer transition key/source。Profile Inspector MUST在显式 Corin Definition context 下，按稳定 identity 列出 Locomotion、Action、Attack1..5 与 Dodge producer 的 Layer 与 binding，但 MUST不复制 producer 之间的逻辑关系；Graph/State edge MUST不保存 transition strategy、duration、curve 或 Driver。

#### Scenario: 配置 Attack1..5

- **WHEN** 作者在 Corin Definition context 下的 Profile Inspector 查看 Attack1..5
- **THEN** Inspector MUST显示五个 producer 的 stable key 与 Animancer binding
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

### Requirement: Corin Marker Sync 配置必须通过正式 Agent v14 迁移

Corin AnimationTrack的sync mode、group、topology、SyncRole与marker，以及Timeline Clip已登记的Curve Channel MUST通过v14 `export_snapshot -> dry_run_patch -> apply_patch -> export_snapshot -> validate`流程写入。实现 MUST不直接修改CorinPlayableRootTree或shared Timeline YAML，不创建一次性migrator。迁移完成后 MUST重新生成匹配source revision的CharacterPresentationProjection及Float32/Fixed Program wrapper。

#### Scenario: 迁移Corin资产

- **WHEN** apply流程配置Corin全部AnimationTrack
- **THEN** dry-run与apply MUST消费同一immutable typed command plan
- **AND** 再次导出的Snapshot MUST显示全部可达track不再是Unspecified
- **AND** generated Projection MUST包含canonical group与segment occurrence索引

#### Scenario: generated artifact重建

- **WHEN** marker作者数据改变source revision
- **THEN** Float32/Fixed Program wrapper与Projection MUST通过正式编译流程重建
- **AND** Program Gameplay operation MUST不包含marker sync payload
