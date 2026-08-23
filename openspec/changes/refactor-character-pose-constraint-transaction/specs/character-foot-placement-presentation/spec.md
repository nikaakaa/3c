## RENAMED Requirements

- FROM: `### Requirement: 当前阶段必须只生成Swing脚垂直Goal`
- TO: `### Requirement: Foot Placement必须按稳定FootPath和Constraint状态生成唯一双脚修正`

## MODIFIED Requirements

### Requirement: Foot Placement必须是唯一Goal事务

唯一`CharacterPoseConstraintRuntime` MUST为每个Actor和表现帧建立匹配Frame、Completion与Rig lineage的Pending根Bank。Foot Placement MUST只消费同帧原生Component Pose、左右Biomechanical Step Read Page、Body Presentation、Locomotion Motion Timeline、正式Future Body Translation、Foot Placement Profile与唯一World Query Seam，并在该Bank内生成唯一`CharacterFootPlacementResult`。

Foot Placement内部 MUST按`Route -> Swing -> Constraint Reducer -> Constraint Resolver -> Resolved Foot`固定顺序分别完成左右脚，再形成唯一Resolved Foot Pair并计算Primary Support与Pelvis。外部Interface MUST不暴露Route、Ground Path、Constraint、Support、Pelvis状态页或执行/提交顺序。Foot Placement Result MUST只向唯一Goal Assembler发布Pelvis、LeftFoot与RightFoot三个typed Goal Contribution；不得自行发布第二Goal Set、第二Pelvis、第二FBBIK或第二Physical Writer。

#### Scenario: 正常生成Foot Placement贡献

- **WHEN** 同一表现帧具有合法Component Pose、Step、Body、World Query、Profile和根Pending Bank
- **THEN** Foot Placement MUST生成同Frame、Completion与Rig lineage的Resolved Foot Pair、Pelvis Result和三个Goal Contribution
- **AND** 调用方 MUST不逐个提交Route、Constraint、Pelvis或Solver状态

#### Scenario: 重复执行Foot Placement

- **WHEN** 同一Frame与Completion第二次请求Foot Placement Prepare
- **THEN** CharacterPoseConstraintRuntime MUST报告非法调用顺序并阻止整帧发布
- **AND** MUST不复用第一次Pending结果或建立第二Foot Placement事务

### Requirement: Foot Placement必须按稳定FootPath和Constraint状态生成唯一双脚修正

每只脚 MUST按正式运行Result链执行：

```text
Landing Prediction
-> Proposal/Ground Path
-> Path Stable/Rebasing
-> Swing Result
-> Constraint State/Transition
-> Resolved Foot Result
```

Swing MUST使用同帧Original Component Pose中的Animated Sole计算`LastCommittedContact -> NextLandingProposal`的水平纵向空间进度，并按同一进度分别采样两个Landing端点之间的Baseline与Ground Envelope。Raw Swing Correction MUST严格为：

```text
ComponentUp * max(
    0,
    EnvelopeSampleAlongUp - BaselineSampleAlongUp)
```

系统 MUST保留动画脚的水平位置、原生抬脚高度、最高点时刻与旋转；MUST不按动画Phase代替空间进度，不得使用`Envelope - AnimatedSole`、实时Landing Height、地形坡度、Current Foot Trace或旧IK Pose重建Swing轨迹。

Route MUST保存Path Target、Output、Velocity和Settled Frame Count。同Event Path Target变化时 MUST保留上一Committed Output与Velocity并只替换Target；Path MUST仅在输出误差、速度和连续帧数同时满足正式门槛时成为Stable。Rebasing Output MAY继续驱动Swing以保持连续，但 MUST不取得锁脚资格，也 MUST不作为Pelvis Stride终点。

每脚Constraint状态 MUST只包含`Swing`、`Landing`、`Locked`、`Releasing`与`UnlockedSupport`。开始落脚表示同一权威Event的Constraint Weight从接近0开始上升；该时刻只有在Path Stable、Proposal/Event匹配、Grounded、Action未占用和目标可达时才能冻结完整Patch并进入Landing。否则 MUST进入UnlockedSupport并消费该Event，不得为了必达提高修正速度、垂直设置到新Path或在落地后晚到重锁。

Landing入口 MUST只捕获一次`CurrentEffectiveCorrection - FrozenContactCorrection`残差，并按动画Biomechanical Constraint Weight的单调上升衰减；Locked MUST严格输出`FrozenAnchor - AnimatedSole`。Locked非零Goal权重 MUST为1，不能再乘小于1的FootPlacement/Contact权重或通过horizontalWeight削弱Anchor。

正常开始抬脚 MUST进入Releasing并只按Constraint Weight下降衰减入口Residual到原生动画脚；Grounded丢失、Contact超距或不可达 MUST使用正式Safety Release时间。Releasing期间Path Revision MUST只更新Next Route，不能改变当前Release目标。完成后根据动画Phase进入Swing或UnlockedSupport，并清除FrozenPatch。

#### Scenario: Swing脚采样台阶FootPath

- **WHEN** Current authoritative Swing Event具有Accepted Ground Envelope，Animated Sole空间进度位于两次Landing之间
- **THEN** Runtime MUST按同一纵向进度采样Baseline与Envelope
- **AND** Corrected Sole与Animated Sole之差 MUST逐值等于非负`Envelope - Baseline`高度增量

#### Scenario: 同Event Path Target变化

- **WHEN** 新Prediction超过更新死区并形成新的合法Ground Path Target
- **THEN** Route MUST进入Rebasing并保留上一Output与Velocity，只替换Target
- **AND** MUST不重新从旧Path起点播放固定Duration插值

#### Scenario: 开始落脚时Path仍在Rebasing

- **WHEN** 同一权威Event开始落脚、Constraint Weight开始上升，但Path尚未满足Settled距离、速度与连续帧门槛
- **THEN** 当前Event MUST进入UnlockedSupport并被消费
- **AND** Runtime MUST不冻结Patch、强制追点或在后续Support帧重新锁该Event

#### Scenario: 冻结后Route继续变化

- **WHEN** Landing、Locked或Releasing脚收到当前或下一Event的新Prediction/Path Revision
- **THEN** Active FrozenPatch的Event、Path、Surface、Point与Normal MUST保持不变
- **AND** 新事实 MUST只更新Next Route Event

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

Diagnostics MUST记录Path State/Identity/Target/Output/Velocity/Settled Frames、Constraint State/Trigger/Transition Cause、Active/Consumed Event、Frozen Patch、Swing/Contact/Effective Correction、Residual/Progress、Resolved Final Sole、Pelvis Reference、Goal Target、FBBIK Solved与最终Physical Position。唯一Physical Writer成功Apply时 MUST把实际Write Completion和最终Physical Bone位置写入同一Pending Diagnostics页；根Bank切换后只发布Committed页。

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

每只脚 MUST按Landing Event identity维护上一成功Locked或正式Accepted Contact与下一Event Prediction。PreSwing或Swing阶段每个有效表现帧 MUST执行一次且仅一次正式Future Landing SphereCast；同一Event合法Prediction按更新死区维护Next Landing与Ground Path Target。事件进入Landing并冻结Patch后，当前Event的新Prediction与Ground Path MUST不再修改Active Contact，但 MAY继续准备不同Next Route Event。

Ground Path MUST使用LastCommittedContact与NextLandingProposal构造查询输入。没有上一Contact时 MUST发布`CurrentLandingUnavailable`；没有合法下一Prediction时 MUST发布`NextLandingUnavailable`。查询失败 MUST发布当前typed rejection，不得读取旧Diagnostics、Animated Sole、默认地面或另一查询路径补事实。

#### Scenario: 当前Event锁定后建立下一Route

- **WHEN** 当前Event已经冻结或Locked且不同Next Event获得合法Prediction
- **THEN** Route MAY为Next Event建立新的Proposal、Ground Path和Rebasing状态
- **AND** Active FrozenPatch与当前脚Effective Correction MUST不读取该Next Route

#### Scenario: Prediction小幅更新

- **WHEN** 同Event新Accepted Landing与当前Proposal距离小于正式更新死区
- **THEN** Runtime MUST复用现有Proposal、Ground Path Target与Path连续性状态
- **AND** MUST继续执行下一表现帧的唯一Landing Prediction

### Requirement: Ground Path模块必须保持抽象与实现分离

Foot Placement核心 MUST只依赖World Query合同、纯Ground Envelope Builder、预分配Ground Path页和typed Route State。纯Builder MUST不引用`PhysicsScene`、Collider、RaycastHit、Transform、FinalIK、Gizmo或Editor类型；Unity Adapter MUST只执行查询、自碰撞过滤和固定容量写入，不选择Step、平滑Path、冻结Patch、计算Constraint、构造Pelvis或写Goal。

Raw Contacts、Edge、Envelope、Path Target/Output/Velocity和Settled状态 MUST属于同一CharacterPoseConstraint根Bank。Route Module是这些状态的唯一写入者，不对外Seal；Swing Resolver只读Route Result并执行纯采样。

#### Scenario: Ground Path成功但整帧Discard

- **WHEN** Pending Ground Path与Rebasing Output已经生成但后续Goal、FBBIK或Writer使整帧Discard
- **THEN** Committed Ground Path、Path Output/Velocity、Constraint、Patch与Pelvis状态 MUST保持上一成功帧
- **AND** 下一帧 MUST不读取被丢弃的Target、Velocity或Raw Contact页

## ADDED Requirements

### Requirement: Foot Constraint必须由typed State Bank而非共享Blackboard驱动

Foot Constraint MUST使用固定typed State Page保存State、Active/Consumed Event、FrozenPatch、TransitionCause、Progress和Residual。系统 MUST不使用运行时字符串Key、共享Dictionary、Gameplay Blackboard或可变Diagnostics保存Foot状态。Constraint Reducer是状态页的唯一业务写入者，Constraint Resolver只消费Reducer结果并生成纯Effective Correction。

#### Scenario: 同帧多个打断成立

- **WHEN** Action占用、Grounded丢失、开始抬脚和Path Revision在同一帧同时成立
- **THEN** Trigger Resolver MUST按正式优先级只向Constraint Reducer提交一个Constraint Trigger
- **AND** Route MAY独立更新Next Event，但 MUST不修改Reducer已经冻结的Active Patch

### Requirement: Pelvis必须只消费Resolved Foot Pair

左右Resolved Foot MUST形成唯一Pair。Primary Support、Stride与Pelvis MUST只读取Pair中的Final Sole、稳定或Frozen Contact Reference、Support Intent、Path Stable状态和Patch lineage。Pelvis MUST不接收Route Result、Prediction Step、NextLanding、Ground Path页或World Query。

Rebasing中的Swing Route Reference MUST标记为UnavailableForStride；Pelvis MUST保持上一合法Stride目标或进入正式Release，不得追随不稳定Path。Pelvis Target MUST先受支撑腿可达区间限制，再由唯一临界阻尼Spring输出。

#### Scenario: Swing Path正在Rebasing

- **WHEN** Swing脚的Path Target已经变化但Output尚未Settled
- **THEN** Resolved Contact Reference MUST标记为UnavailableForStride
- **AND** Pelvis MUST不把该Target当作新Stride终点或改变Locked支撑脚的Patch时刻
