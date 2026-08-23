## RENAMED Requirements

- FROM: `### Requirement: 当前阶段必须只生成Swing脚垂直Goal`
- TO: `### Requirement: Foot Placement必须按稳定FootPath和Constraint状态生成唯一双脚修正`

## MODIFIED Requirements

### Requirement: Foot Placement必须是唯一Goal事务

唯一`CharacterPoseConstraintRuntime` MUST为每个Actor和表现帧建立匹配Frame、Completion与Rig lineage的Pending根Bank。Foot Placement MUST只消费同帧原生Component Pose、左右Biomechanical Step Read Page、Body Presentation、Locomotion Motion Timeline、正式Future Body Translation、Foot Placement Profile与唯一World Query Seam，并在该Bank内生成唯一`CharacterFootPlacementResult`。

Foot Placement MUST形成一个深`CharacterFootPlacementModule`。其外部Interface MUST只接收同帧不可变`CharacterFootPlacementFrameInput`并发布一个`CharacterFootPlacementResult`；调用方 MUST不知道或编排Landing Prediction、Ground Path、Swing Target、状态转换、Support、Pelvis与Goal编码的内部执行顺序。Module Implementation MUST为左右脚各执行一次`CharacterFootStateMachine`并生成唯一Resolved Foot Pair，再计算Primary Support、Pelvis与Pelvis/LeftFoot/RightFoot三个typed Goal Contribution；不得发布第二Goal Set、第二Pelvis、第二FBBIK或第二Physical Writer。

#### Scenario: 正常生成Foot Placement贡献

- **WHEN** 同一表现帧具有合法Component Pose、Step、Body、World Query、Profile和根Pending Bank
- **THEN** Foot Placement MUST生成同Frame、Completion与Rig lineage的Resolved Foot Pair、Pelvis Result和三个Goal Contribution
- **AND** 调用方 MUST不取得或逐个提交Foot State Context、Ground Path、Constraint、Pelvis或Solver状态

#### Scenario: 重复执行Foot Placement

- **WHEN** 同一Frame与Completion第二次请求Foot Placement Prepare
- **THEN** CharacterPoseConstraintRuntime MUST报告非法调用顺序并阻止整帧发布
- **AND** MUST不复用第一次Pending结果或建立第二Foot Placement事务

### Requirement: Foot Placement必须按稳定FootPath和Constraint状态生成唯一双脚修正

每只脚 MUST按正式运行Result链执行：

```text
Landing Prediction
-> Proposal/Ground Path
-> Swing Path Target
-> CharacterFootStateMachine
-> Resolved Foot Result
```

Swing MUST使用同帧Original Component Pose中的Animated Sole计算`LastCommittedContact -> NextLandingProposal`的水平纵向空间进度，并按同一进度分别采样两个Landing端点之间的Baseline与Ground Envelope。Raw Swing Correction MUST严格为：

```text
ComponentUp * max(
    0,
    EnvelopeSampleAlongUp - BaselineSampleAlongUp)
```

Swing Path Target MUST严格为：

```text
PathTargetCorrection =
    animation.foot-placement-weight * RawSwingCorrection
```

系统 MUST保留动画脚的水平位置、原生抬脚高度、最高点时刻与旋转；MUST不按动画Phase代替空间进度，不得使用`Envelope - AnimatedSole`、实时Landing Height、地形坡度、Current Foot Trace或旧IK Pose重建Swing轨迹。

每脚 MUST只有一个固定布局的`CharacterFootStateContext`，集中保存Constraint State、Active/Consumed Event、Last Contact、Next Landing Proposal、Ground Path identity与固定payload页引用、Path Target、Path Tracking Status、Settled Frame Count、Frozen Patch、唯一Effective Correction/Velocity和Transition事实。Raw Contact、Edge与Envelope固定payload页 MUST与Context同属一个根Bank和同一脚；World Query Adapter与纯Builder只能在State Machine一次Evaluate期间填充各自的Pending payload，payload MUST不保存Constraint、Transition、Correction或跨帧控制状态。`CharacterFootStateMachine` MUST是Context的唯一写入者。系统 MUST不把Path Output、GoalTransition Output、Contact Ownership Output或Transition Output保存成第二份跨帧脚修正。

同Event Path Target变化时，State Machine MUST保留上一Committed Effective Correction/Velocity并只替换Path Target；Swing状态 MUST用同一Effective Correction/Velocity临界阻尼追踪新Target，不得重启固定Duration。Path Tracking Status MUST仅根据Swing状态下的Target误差、Effective Velocity与连续Settled帧数发布`Stable/Rebasing/Unavailable`，不得成为独立状态机或拥有自己的Output。Rebasing中的Effective Correction MAY继续驱动Swing以保持连续，但 MUST不取得锁脚资格，也 MUST不作为Pelvis Stride终点。

每脚Constraint状态 MUST只包含`Swing`、`Landing`、`Locked`、`Releasing`与`UnlockedSupport`。开始落脚表示同一权威Event的Constraint Weight从接近0开始上升；该时刻只有在Path Stable、Proposal/Event匹配、Grounded、Action未占用和目标可达时才能冻结完整Patch并进入Landing。否则 MUST进入UnlockedSupport并消费该Event，不得为了必达提高修正速度、垂直设置到新Path或在落地后晚到重锁。

Landing入口 MUST只捕获一次`CurrentEffectiveCorrection - FrozenContactCorrection`残差，并按动画Biomechanical Constraint Weight的单调上升衰减；Locked MUST严格输出`FrozenAnchor - AnimatedSole`。Locked非零Goal权重 MUST为1，不能再乘小于1的FootPlacement/Contact权重或通过horizontalWeight削弱Anchor。

正常开始抬脚 MUST进入Releasing并只按Constraint Weight下降衰减入口Residual到原生动画脚；Grounded丢失、Contact超距或不可达 MUST使用正式Safety Release时间。Releasing期间新的Prediction、Ground Path或Path Target MUST只更新下一Event事实，不能改变当前Release目标。完成后根据动画Phase进入Swing或UnlockedSupport，并清除FrozenPatch。进入Swing时 MUST继续使用同一个Effective Correction/Velocity，不得恢复、复制或重置另一份Path Output。

#### Scenario: Swing脚采样台阶FootPath

- **WHEN** Current authoritative Swing Event具有Accepted Ground Envelope，Animated Sole空间进度位于两次Landing之间
- **THEN** Runtime MUST按同一纵向进度采样Baseline与Envelope
- **AND** Raw Swing Correction MUST逐值等于非负`Envelope - Baseline`高度增量，Path Target MUST再乘同帧动画Foot Placement Weight
- **AND** Corrected Sole与Animated Sole之差 MUST逐值等于同一State Context的Effective Correction；Rebasing时不得伪称已经等于最新Path Target

#### Scenario: 同Event Path Target变化

- **WHEN** 新Prediction超过更新死区并形成新的合法Ground Path Target
- **THEN** State Machine MUST发布Rebasing并保留上一Effective Correction与Velocity，只替换Path Target
- **AND** MUST不重新从旧Path起点播放固定Duration插值

#### Scenario: 开始落脚时Path仍在Rebasing

- **WHEN** 同一权威Event开始落脚、Constraint Weight开始上升，但Path尚未满足Settled距离、速度与连续帧门槛
- **THEN** 当前Event MUST进入UnlockedSupport并被消费
- **AND** Runtime MUST不冻结Patch、强制追点或在后续Support帧重新锁该Event

#### Scenario: 冻结后Path事实继续变化

- **WHEN** Landing、Locked或Releasing脚收到当前或下一Event的新Prediction/Path Revision
- **THEN** Active FrozenPatch的Event、Path、Surface、Point与Normal MUST保持不变
- **AND** 新事实 MUST只更新下一Event的Prediction、Ground Path与Path Target

#### Scenario: 正常锁入和释放

- **WHEN** Landing脚的Constraint Weight连续上升到1，随后在开始抬脚后连续下降到0
- **THEN** Landing MUST从入口当前输出连续收敛到Frozen Contact并进入Locked
- **AND** Releasing MUST从入口Locked输出连续回到原生动画脚，且两个过程都不得重新捕获起点或叠加第二平滑

#### Scenario: Locked接触超距

- **WHEN** Locked脚水平误差超过正式ReleaseDistance
- **THEN** Constraint MUST进入Safety Releasing并保持Anchor不更新直到释放完成
- **AND** MUST不通过Sliding权重移动或削弱Anchor

### Requirement: Foot Placement诊断必须只显示当前事实

Foot Placement、Ground Path、Path连续性、Constraint、Pelvis、FBBIK与Physical Writer的Runtime Result MUST与Diagnostics严格分型。运行方法签名 MUST不出现`*Diagnostics`、`*Snapshot`、Gizmo或CSV类型。Diagnostics interest与固定容量 MUST在BeginFrame冻结并预验证；Diagnostics Projector MUST在全部Pending Runtime Result完成、Physical Writer执行前，从同一Pending Bank无查询、无业务决策、固定容量地深冻结Diagnostics页。

Diagnostics MUST记录Path Tracking Status/Identity/Target/Settled Frames、Constraint State/Trigger/Transition Cause、Active/Consumed Event、Frozen Patch、唯一Effective Correction/Velocity、Residual/Progress、Resolved Final Sole、Pelvis Reference、Goal Target、FBBIK Solved与最终Physical Position。唯一Physical Writer成功Apply时 MUST把实际Write Completion和最终Physical Bone位置写入同一Pending Diagnostics页；根Bank切换后只发布Committed页。

Gizmo、CSV、Trace与Pose Watch MUST只读取相同Frame、Completion、Rig和Bank identity的Committed深冻结页。Diagnostics MUST不查询世界、重新采样Pose、计算Path、修改Constraint、选择Support、生成Goal或执行FBBIK。旧LandingPreparation、OwnershipHalfLife、CurrentTrace、Plant State、SupportLock、GoalTransition与兼容列 MUST删除。

#### Scenario: 捕获成功提交帧

- **WHEN** 当前帧具有Capture interest且Foot、Pelvis、Goal、FBBIK和Pending Pose已经完成全部验证
- **THEN** Diagnostics Projector MUST在Physical Writer前深冻结Runtime事实，Writer成功时补入Physical结果
- **AND** CSV MUST只读取随根Bank提交的Committed页，不得引用后续复用的Ground Path或Pose页

#### Scenario: 没有Diagnostics interest

- **WHEN** 当前Actor没有Live、Capture、Pose Watch或detail interest
- **THEN** Foot Placement、Goal、FBBIK与Physical Writer MUST照常执行
- **AND** Runtime MUST不复制Ground Path大页、逐腿Pose或字符串身份

### Requirement: Ground Path必须使用上一已提交落点与下一事件落点

每只脚 MUST在同一`CharacterFootStateContext`中按Landing Event identity维护上一成功Locked或正式Accepted Contact与下一Event Prediction。PreSwing或Swing阶段每个有效表现帧 MUST执行一次且仅一次正式Future Landing SphereCast；同一Event合法Prediction按更新死区维护Next Landing与Ground Path Target。事件进入Landing并冻结Patch后，当前Event的新Prediction与Ground Path MUST不再修改Active Contact，但 MAY继续准备不同Next Swing Event事实。

Ground Path MUST使用LastCommittedContact与NextLandingProposal构造查询输入。没有上一Contact时 MUST发布`CurrentLandingUnavailable`；没有合法下一Prediction时 MUST发布`NextLandingUnavailable`。查询失败 MUST发布当前typed rejection，不得读取旧Diagnostics、Animated Sole、默认地面或另一查询路径补事实。

#### Scenario: 当前Event锁定后准备下一Swing Path

- **WHEN** 当前Event已经冻结或Locked且不同Next Event获得合法Prediction
- **THEN** State Machine MAY为Next Event写入新的Proposal、Ground Path、Path Target与Tracking Status
- **AND** Active FrozenPatch与当前脚Effective Correction MUST不读取该Next Event的Path Target

#### Scenario: Prediction小幅更新

- **WHEN** 同Event新Accepted Landing与当前Proposal距离小于正式更新死区
- **THEN** Runtime MUST复用现有Proposal与Ground Path Target，State Machine MUST保持当前Effective Correction/Velocity
- **AND** MUST继续执行下一表现帧的唯一Landing Prediction

### Requirement: Ground Path模块必须保持抽象与实现分离

`CharacterFootPlacementModule` Implementation MUST只依赖World Query合同、纯Ground Envelope Builder和预分配Ground Path页。纯Builder MUST不引用`PhysicsScene`、Collider、RaycastHit、Transform、FinalIK、Gizmo或Editor类型；Unity Adapter MUST只执行查询、自碰撞过滤和固定容量写入，不选择Step、保存状态、平滑Correction、冻结Patch、计算Constraint、构造Pelvis或写Goal。

Raw Contacts、Edge、Envelope、Path Target、Path Tracking Status、Settled Frame Count、Constraint、Frozen Patch与唯一Effective Correction/Velocity MUST属于同一CharacterPoseConstraint根Bank内的左右`CharacterFootStateContext`及其固定容量Ground Path页。只有`CharacterFootStateMachine`可以写Foot State Context；Ground Envelope Builder、Swing Target Calculator、Trigger Resolver和Constraint数学 MUST只返回不可变中间值，不能保存自己的Pending/Committed或跨帧输出。

#### Scenario: Ground Path成功但整帧Discard

- **WHEN** Pending Ground Path与Rebasing Output已经生成但后续Goal、FBBIK或Writer使整帧Discard
- **THEN** Committed Ground Path、Foot State Context、Effective Correction/Velocity、Patch与Pelvis状态 MUST保持上一成功帧
- **AND** 下一帧 MUST不读取被丢弃的Path Target、Context或Raw Contact页

## ADDED Requirements

### Requirement: Foot Constraint必须由显式typed State Context驱动

每只脚 MUST使用一个固定布局的`CharacterFootStateContext`作为显式状态机上下文，集中保存State、Active/Consumed Event、Last Contact、Next Landing Proposal、Ground Path identity、Path Target/Tracking、FrozenPatch、唯一Effective Correction/Velocity、TransitionCause、Progress和Residual。系统 MUST不使用运行时字符串Key、共享Dictionary、Gameplay Blackboard、动态字段或可变Diagnostics保存Foot状态。

`CharacterFootStateMachine` MUST是整个Context的唯一写入者，并在一次Evaluate中从上一Committed Context与同帧不可变Input生成Pending Context和`CharacterResolvedFootResult`。内部Ground Path、Swing Target、Trigger和Constraint计算 MUST不拥有第二状态页或第二Correction。调用方、Pelvis、Goal、Diagnostics与未来Reactive输入 MUST不能直接写Context。

#### Scenario: 同帧多个打断成立

- **WHEN** Action占用、Grounded丢失、开始抬脚和Path Revision在同一帧同时成立
- **THEN** State Machine内部Trigger Resolver MUST按正式优先级只产生一个Constraint Trigger，State Machine MUST最多执行一次状态转换
- **AND** 同帧下一Event事实 MAY更新，但 MUST由同一State Machine写入Context且不能修改已经冻结的Active Patch

### Requirement: Pelvis必须只消费Resolved Foot Pair

左右Resolved Foot MUST形成唯一Pair。Primary Support、Stride与Pelvis MUST只读取Pair中的Final Sole、稳定或Frozen Contact Reference、Support Intent、Path Stable状态和Patch lineage。Pelvis MUST不接收Foot State Context、Prediction Step、NextLanding、Ground Path页或World Query。

Rebasing中的Swing Path Reference MUST标记为UnavailableForStride；Pelvis MUST保持上一合法Stride目标或进入正式Release，不得追随不稳定Path。Pelvis Target MUST先受支撑腿可达区间限制，再由唯一临界阻尼Spring输出。

#### Scenario: Swing Path正在Rebasing

- **WHEN** Swing脚的Path Target已经变化但Output尚未Settled
- **THEN** Resolved Contact Reference MUST标记为UnavailableForStride
- **AND** Pelvis MUST不把该Target当作新Stride终点或改变Locked支撑脚的Patch时刻
