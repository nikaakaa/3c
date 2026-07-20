# character-state-timeline-authoring-loop Specification Delta

## MODIFIED Requirements

### Requirement: Corin Locomotion 必须使用 StateMachine + Timeline 编排

Corin locomotion MUST 使用状态机表达基础移动与分层所有权状态。有独立时序内容的状态 MUST 在状态行为内通过 TimelineNode 播放对应 Timeline；没有独立动画资源的状态 MUST 使用 Transition blend 或无表现 ownership state 衔接，不得创建伪 Timeline 或 fallback clip。状态至少包含 `Idle`、`WalkStart`、`WalkLoop`、`WalkEnd`、`RunStart`、`RunLoop`、`RunEnd`、`MovingTurn` 和 `ActionOverride`。所有 start、end、loop、turn 和 ownership 状态 MUST 通过明确 Transition 响应输入与 root-owned pipeline blackboard ownership fact，并复用统一 State source-exit 生命周期。

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

#### Scenario: 全身 Action 活跃时交出 locomotion ownership

- **WHEN** 任一普通 locomotion state 读取到 pipeline blackboard `HasActionLocomotionOwnership=true`
- **THEN** Locomotion StateMachine MUST 以高优先级进入 ActionOverride
- **AND** ActionOverride MUST NOT 播放动画、引用 Action Timeline 或提交 motion contribution
- **AND** 原 locomotion Timeline MUST 通过 State source-exit 停止，不能在隐藏状态继续推进 producer

#### Scenario: Action 完成后有输入恢复 RunLoop

- **WHEN** ActionOverride 读取到 `HasActionLocomotionOwnership=false`
- **AND** 当前 MoveAxis 大于 stop threshold
- **THEN** Locomotion StateMachine MUST 直接进入 RunLoop
- **AND** MUST NOT 重复进入 RunStart

#### Scenario: Dodge 完成后无输入进入 RunEnd

- **WHEN** ActionOverride 读取到 `HasActionLocomotionOwnership=false`
- **AND** 当前 MoveAxis 不大于 stop threshold
- **AND** `ResumeLocomotionThroughRunEnd=true`
- **THEN** Locomotion StateMachine MUST 进入 RunEnd

#### Scenario: Attack 完成后无输入进入 Idle

- **WHEN** ActionOverride 读取到 `HasActionLocomotionOwnership=false`
- **AND** 当前 MoveAxis 不大于 stop threshold
- **AND** `ResumeLocomotionThroughRunEnd=false`
- **THEN** Locomotion StateMachine MUST 直接进入 Idle
- **AND** MUST NOT 暴露此前已推进的 RunEnd producer

#### Scenario: ActionOverride 保持单一职责

- **WHEN** 作者下钻 ActionOverride StateNode
- **THEN** inline state body MUST 不包含 Action request consume、ActionProfile、Timeline、animation 或 motion node
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

Corin 外层 Action StateMachine MUST 只表达动作大类并只包含 `None`、`Attack` 和 `Dodge`。具体 `Attack1`、`Attack2`、`Attack3`、`Attack4`、`Attack5` 与 `RushAttack` MUST 位于 `Attack` StateNode 的 inline StateBehaviorSubTree 所运行的 `Attack Combo StateMachine` 中；具体 `DodgeBack`、`DodgeForward` MUST 位于 `Dodge` StateNode 的 inline StateBehaviorSubTree 所运行的 `Dodge Direction StateMachine` 中。Leaf action state MUST 继续使用 ActionProfile、独立 Action Context 和带 Action Context 的 inline TimelineNode。连段、移动取消、Dodge 后摇重入与方向选择 MUST 使用与 Tree abort 相同的 Runnable stop、State source-exit、Action lifecycle 和 Timeline cancel 分层，不得创建 Action 专用旁路。

#### Scenario: 首次进入 Attack1

- **WHEN** 外层 `None` 检测到 Attack request
- **THEN** 外层 Action StateMachine MUST 进入 `Attack`
- **AND** 外层条件 MUST 只查询而不消费该 request
- **AND** 内层 Attack StateMachine MUST 进入 `Attack1`
- **AND** `Attack1` target activation MUST 消费 request 并创建新的 Action Context

#### Scenario: Attack1 进入 Attack2

- **WHEN** `Attack1Cancel` 在当前 Tick active 且存在 Attack request
- **THEN** 内层 Attack StateMachine MUST 从 `Attack1` 抢占到 `Attack2`
- **AND** source OnExit MUST 通过唯一取消分支提交 `Cancel(RecoveryCancel)`
- **AND** target activation MUST 消费 request 并创建新的 Action Context

#### Scenario: 中间段进入下一段并由末段循环

- **WHEN** `Attack2Cancel`、`Attack3Cancel`、`Attack4Cancel` 或 `Attack5Cancel` 在当前 Tick active 且存在 Attack request
- **THEN** 内层 Attack StateMachine MUST 分别从 `Attack2` 进入 `Attack3`、从 `Attack3` 进入 `Attack4`、从 `Attack4` 进入 `Attack5` 或从 `Attack5` 进入 `Attack1`
- **AND** condition query MUST NOT 消费 request
- **AND** target activation MUST 唯一消费 request 并创建新的 Action Context
- **AND** Attack request transition MUST稳定优先于同source MoveCancel transition

#### Scenario: 移动取消攻击后摇

- **WHEN** `AttackNMoveCancel` active、MoveAxis大于stop threshold且当前Tick没有Attack request
- **THEN** 内层Attack StateMachine MUST从当前AttackN进入Exit
- **AND** source leaf MUST提交唯一`Cancel(RecoveryCancel)`
- **AND** outer Attack释放ownership后Locomotion MUST进入RunLoop

#### Scenario: 每段攻击没有继续输入时播放后摇

- **WHEN** 当前ComboCancel或MoveCancel已到达但没有匹配的有效请求
- **THEN** 当前 leaf MUST 保持 active 并继续播放同一 Timeline 中对应的 End animation clip
- **AND** MUST NOT 通过 Hold 主攻击末帧、隐藏 Idle 或 RunEnd producer代替后摇

#### Scenario: 攻击正常结束

- **WHEN** Attack1、Attack2、Attack3、Attack4 或 Attack5 的 End animation 已播放到 Timeline 自然完成且没有窗口连段
- **THEN** leaf source MUST 提交一次 Complete 并进入内层 Exit
- **AND** 外层 Attack root MUST 因嵌套 StateMachineNode 成功而完成
- **AND** 外层 Action StateMachine MUST 通过 `StateRootCompleted` 回到 None
- **AND** 外层 Attack OnExit MUST NOT 提交第二条 Action terminal transition

#### Scenario: 首次进入 Dodge direction

- **WHEN** 外层 None 检测到 Dodge request
- **THEN** 外层 Action StateMachine MUST 进入 Dodge
- **AND** 外层条件 MUST 只查询而不消费该 request
- **AND** 内层 Dodge Direction StateMachine MUST 根据正式输入条件选择 DodgeBack 或 DodgeForward
- **AND** 内层 Entry transition MUST NOT 再次查询 Dodge request
- **AND** 目标 direction leaf MUST 消费 request 并创建新的 Action Context

#### Scenario: Dodge后摇可被攻击、再次Dodge或移动取消

- **WHEN** `DodgeRecoveryCancel` active
- **THEN** Attack request MUST使outer Dodge进入Attack并由RushAttack target消费request
- **AND** Dodge request MUST按MoveAxis使当前Dodge leaf进入新的DodgeBack或DodgeForward并由target消费request
- **AND** 仅有有效移动输入时 MUST进入inner Exit并释放给Locomotion
- **AND** 同Tick优先级 MUST为Attack、Dodge、移动、自然完成

#### Scenario: RushAttack进入普通连段或移动退出

- **WHEN** Dodge recovery中的Attack request进入RushAttack
- **THEN** RushAttack MUST播放主段与End后摇并持有同一Attack category ownership
- **AND** RushAttackCancel active且存在Attack request时 MUST进入Attack1
- **AND** RushAttackMoveCancel active、有移动且没有Attack request时 MUST进入Exit

#### Scenario: Dodge direction 正常结束

- **WHEN** DodgeBack 或 DodgeForward root 正常完成或命中正式 recovery-cancel transition
- **THEN** direction leaf MUST 提交匹配的唯一 terminal lifecycle并进入内层 Exit
- **AND** 外层 Dodge root MUST 完成并回到 None
- **AND** 外层 Dodge OnExit MUST NOT 提交第二条 Action terminal transition

#### Scenario: 打开外层 Action StateMachine

- **WHEN** 作者打开 Corin Action StateMachine
- **THEN** 作者 MUST 只看到 `None`、`Attack` 和 `Dodge`
- **AND** 作者 MUST NOT 在该层看到 `Attack1`、`Attack2`、`Attack3`、`Attack4`、`Attack5`、`RushAttack`、`DodgeBack` 或 `DodgeForward`
- **AND** 作者下钻 Attack 或 Dodge state body 后 MUST 能继续打开对应 inline nested StateMachine

## ADDED Requirements

### Requirement: Corin Action 与 Locomotion 必须在同 tick 完成 Base ownership 交接

Corin Gameplay Parallel MUST 以稳定 flow order 先执行 Action StateMachine，再执行 Locomotion StateMachine。外层 Attack/Dodge MUST通过 root-owned `HasActionLocomotionOwnership` 与 `ResumeLocomotionThroughRunEnd` 表达 locomotion ownership 和释放策略。Locomotion MUST只读取这些事实，不得读取具体 leaf ActionProfile、Timeline、producer 或 ActionInstance。项目 MUST删除 `IsDodging` 专用 ownership 路径，并不得让 active Action 与普通 Locomotion Timeline 在同一 tick 为 Base 提交不同 selected producer。

#### Scenario: Attack 激活 tick

- **WHEN** Action StateMachine 在本 tick 从 None 进入 Attack
- **THEN** Attack OnEnter MUST先写入 `HasActionLocomotionOwnership=true`
- **AND** 随后执行的 Locomotion StateMachine MUST在同 tick 进入 ActionOverride
- **AND** Base MUST只选择 Attack leaf Timeline producer

#### Scenario: Attack 完成 tick

- **WHEN** Attack nested root 在本 tick 自然完成
- **THEN** outer Attack OnExit MUST先写入 `HasActionLocomotionOwnership=false`
- **AND** 随后执行的 Locomotion StateMachine MUST在同 tick 选择正式恢复状态
- **AND** Base MUST不出现没有 target producer 的中间 tick

#### Scenario: Dodge 与 Attack 使用不同返回策略

- **WHEN** outer Attack 或 Dodge 获得 ownership
- **THEN** Attack MUST写入 `ResumeLocomotionThroughRunEnd=false`
- **AND** Dodge MUST写入 `ResumeLocomotionThroughRunEnd=true`
- **AND** Animation layer MUST不解释该值或比较动作 priority

### Requirement: Corin 每段攻击 Timeline 必须包含唯一主攻击与后摇动画段

Attack1、Attack2、Attack3、Attack4、Attack5 与 RushAttack 的 inline Timeline MUST 在各自唯一 AnimationTrack 中包含主攻击 clip 和对应 End 后摇 clip。主攻击 clip MUST 使用 `ExtraPolationMode=None`，主攻击与 End clip MUST 使用明确且有限的 overlap/ease，End clip MUST 使用完整非循环 inplace 资源时长。Hit、ComboCancel、MoveCancel、Cue 和 MotionCurve MUST 位于同一 Timeline；End clip 超出 MotionCurve 区间的部分 MUST 不产生隐式 motion。

#### Scenario: 查看任一普通攻击 Timeline

- **WHEN** 作者打开 Attack1、Attack2、Attack3、Attack4、Attack5 或 RushAttack inline Timeline
- **THEN** AnimationTrack MUST 显示对应段主攻击与 End 两个正式 clip
- **AND** Hit、Cancel、Cue、MotionCurve MUST保持可见并与完整动画对齐

#### Scenario: 特殊攻击资源不自动进入普通五连

- **WHEN** `Normal_03_Explode` 与 `Normal_05_B` 尚未拥有已批准的输入、条件与状态语义
- **THEN** 普通 Attack1 至 Attack5 Timeline MUST NOT 自动引用该资源
- **AND** 项目 MUST NOT 为该资源创建猜测性 transition 或 fallback clip

#### Scenario: 连段命中 Cancel window

- **WHEN** 当前攻击 End clip 已开始且 Cancel TreeClip active，并收到 Attack request
- **THEN** State transition MUST停止当前完整 Timeline并进入下一段攻击
- **AND** AnimationPlaybackLifecycle MUST只消费 source release 与 target selection，不得自行决定连段
