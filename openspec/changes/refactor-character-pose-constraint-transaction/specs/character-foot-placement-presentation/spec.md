## RENAMED Requirements

- FROM: `### Requirement: 当前阶段必须只生成Swing脚垂直Goal`
- TO: `### Requirement: Foot Placement必须通过统一状态机生成双脚修正`

## MODIFIED Requirements

### Requirement: Foot Placement必须是唯一Goal事务

唯一`CharacterPoseConstraintRuntime` MUST为每个Actor和表现帧建立匹配Frame、Completion与Rig lineage的Pending根Bank。根Runtime MUST只管理阶段顺序、lineage、页所有权、Seal/Discard/Invalidate和失败传播，不得实现Foot、Pelvis、Goal或Solver数学。

Foot Placement MUST形成一个深`CharacterFootPlacementModule`，其外部Interface只接收同帧不可变Frame Input并发布一个`CharacterFootPlacementResult`。调用方 MUST不知道或编排Landing Prediction、Ground Path、左右脚状态、Support、Pelvis与Goal编码顺序。

Module Implementation MUST先通过World Query Adapter生成不可变Observation Page，再为左右脚各执行一次`CharacterFootStateMachine`并生成唯一Resolved Foot Pair，最后计算Primary Support、Pelvis与三个typed Goal Contribution。State Machine MUST不直接调用SphereCast、访问Collider或保存Unity查询对象；不得发布第二Goal Set、第二Pelvis、第二FBBIK或第二Physical Writer。

#### Scenario: 正常生成Foot Placement结果

- **WHEN** 同一表现帧具有合法Component Pose、Step、Body、World Query、Profile和根Pending Bank
- **THEN** Foot Placement MUST生成同Frame、Completion与Rig lineage的Resolved Foot Pair、Pelvis Result和三个Goal Contribution
- **AND** 调用方 MUST不取得或逐个提交Foot Context、Ground Path、Pelvis或Solver状态

#### Scenario: 重复执行Foot Placement

- **WHEN** 同一Frame与Completion第二次请求Foot Placement Prepare
- **THEN** 根Runtime MUST报告非法调用顺序并阻止整帧发布
- **AND** MUST不建立第二Foot Placement事务

### Requirement: Foot Placement必须通过统一状态机生成双脚修正

每只脚 MUST只有一个固定typed `CharacterFootStateContext`、一个`CharacterFootStateMachine`和一个Effective Correction Owner。State Machine MUST用以下确定映射重新解释`8fc704a74ed3548c3357eff5c2d45f52d8366a4b`行为：

```text
None且PlantCycle未消费 -> Swing
None且PlantCycle已消费 -> UnlockedSupport
Acquiring -> Landing
Locked -> Locked / FullAnchor Response
Sliding -> Locked / Sliding Response
Releasing -> Releasing
```

Sliding Response MUST只属于Locked内部计算事实，不得拥有独立Event、Anchor、Transition或Output。系统 MUST保持8fc的状态条件、比较顺序、公式、阈值和逐帧结果，不得因新命名改变行为。

Swing MUST继续使用动画Phase、Last Landing、Next Landing与Ground Envelope：

```text
phase = InverseLerp(LiftOffPhase, LandingPhase, EventPhase)
progress = SmoothStep(0, 1, phase)
baseline = Lerp(LastLanding, NextLanding, progress)
envelope = SampleEnvelopeByArcLength(progress)
vertical = max(0, dot(envelope - baseline, ComponentUp))
         + LandingConstraintWeight * dot(baseline - AnimatedSole, ComponentUp)
```

同Event Path Revision MUST继续按LandingUpdateDistance捕获`PreviousOutput - NewSwingCorrection`残差，并按EffectiveCorrectionHalfLife衰减。Swing、Landing与Locked MUST继续执行8fc向上RaiseToFloor。

PlantConfidence首次达到0.5 MUST消费本轮Plant；只有合法Landing且水平误差不超过LockDistance才进入Landing并使用Landing Point创建Anchor。Landing Contact Progress MUST保持历史最大`InverseLerp(0.5, 0.75, PlantConfidence)`；达到1进入Locked。Landing、Locked、Sliding Response与Releasing的准入、释放和完成条件 MUST逐值保持8fc。

Locked FullAnchor Response MUST使用完整Anchor修正。Sliding Response MUST保持8fc的水平权重公式和进入首帧保留Output行为。Releasing MUST保持移动Swing Target残差与HalfLife衰减。Contact Ownership、Support Weight和Goal Position Weight MUST逐值保持8fc。

#### Scenario: Acquiring重新解释为Landing

- **WHEN** PlantConfidence首次达到0.5且Landing与LockDistance准入合法
- **THEN** 新State Machine MUST进入Landing并产生与8fc Acquiring相同的Anchor、Acquire Residual、Progress和Output
- **AND** MUST不引入新的LandingStarted、固定Duration或动画Contact Plan

#### Scenario: Sliding重新解释为Locked Response

- **WHEN** Locked脚水平误差大于LockDistance且不超过SlideDistance
- **THEN** State Machine MUST保持Locked生命周期并使用与8fc Sliding相同的水平修正、首帧保留和HalfLife追踪
- **AND** Sliding Response MUST不形成第二状态机或第二Anchor Owner

#### Scenario: 相同基线输入

- **WHEN** 新Module收到与8fc相同的Frame Input和映射后的上一帧Context
- **THEN** Swing、Anchor、Correction、Ownership、Support、Pelvis与Goal结果 MUST逐帧等价
- **AND** 任一差异 MUST作为重构回归而不是行为优化

### Requirement: Foot Placement诊断必须只显示正式结果

Runtime Result MUST与Diagnostics严格分型。Diagnostics MUST从Pending Context、Observation、Resolved Result和后续阶段Result单向深冻结Phase Progress、Baseline、Envelope、Swing Correction、Residual、Anchor、Contact Progress、Ownership、Support Eligibility、Support、Pelvis与Goal/Solved/Physical结果，并可发布新五状态和Lock Response。

Gizmo、CSV、Trace与Pose Watch MUST只读取相同Frame、Completion、Rig和Bank identity的Committed页。Diagnostics MUST不查询世界、修改Context、选择Support、生成Goal或执行FBBIK。

#### Scenario: 捕获重构后基线事实

- **WHEN** Foot、Pelvis、Goal、FBBIK和Pending Pose完成验证
- **THEN** Diagnostics MUST发布可对账8fc的同帧正式事实和新状态映射
- **AND** Diagnostics命名变化 MUST不改变Runtime Result

### Requirement: Ground Path模块必须保持抽象与实现分离

Foot Placement核心 MUST只依赖现有World Query合同、Ground Envelope Builder和预分配Observation页。Unity Adapter只执行8fc已有查询与固定容量写入；不得选择Step、保存Foot状态、平滑Correction、创建Anchor、构造Pelvis或写Goal。State Machine MUST只消费不可变Observation，不得直接访问Unity查询对象。

Landing Lifecycle的Previous/Next Landing、更新死区、晋升、Prediction Error和Constraint Weight MUST迁入Foot Context并保持8fc行为。Ground Path payload与Foot Context MUST属于同一根Bank，只有State Machine可以写Context。

#### Scenario: 整帧Discard

- **WHEN** Pending Ground Path与Foot Context已生成但后续阶段失败
- **THEN** Committed Context、Path、Correction、Anchor与Pelvis状态 MUST保持上一成功帧
- **AND** 下一帧 MUST不读取被丢弃的事实

## ADDED Requirements

### Requirement: Foot Constraint必须由显式typed State Context驱动

每只脚 MUST使用一个固定布局`CharacterFootStateContext`集中保存Landing Event、PlantCycleConsumed、Path Residual、Contact Anchor/Progress、唯一Effective Correction、Acquire/Release Residual、顶层State与Lock Response。系统 MUST不使用字符串Key、共享Dictionary、Gameplay Blackboard、动态字段或可变Diagnostics保存Foot状态。

State Machine MUST是Context唯一写入者，并在一次Evaluate中生成Pending Context和Resolved Foot。调用方、Pelvis、Goal与Diagnostics MUST不能直接写Context。

#### Scenario: Action硬失去脚所有权

- **WHEN** Action占用该脚且当前PlantConfidence不低于0.5
- **THEN** State Machine MUST按8fc清空输出与接触状态，并把本轮Plant保持为已消费的UnlockedSupport映射
- **AND** Action、调用方和Diagnostics MUST不直接修改Context字段

### Requirement: Resolved Foot必须形成紧凑下游合同

`CharacterResolvedFootResult`正式下游合同 MUST包含Frame/Completion/Rig/Side、Final Sole/Ankle、Effective Correction、Goal Weight、Contact Reference/Ownership、Support Eligibility、Support Weight、Support Intent Weight、Support Horizontal Error、Support Event lineage、typed Pelvis Reach Reference与Outcome。Support Eligibility MUST只包含`None`、`RetainOnly`与`AcquireAndRetain`。

State Machine MUST按8fc映射发布Eligibility：Swing、Landing和UnlockedSupport为None，Releasing为RetainOnly，Locked无论FullAnchor或Sliding Response均为AcquireAndRetain。重构阶段Support Intent Weight MUST逐值等于8fc Support Weight；Pelvis Reach Reference MUST只在现有Contact可参与Pelvis时指向与Contact Reference相同的点，其余状态为Unavailable。State、Lock Response、Path、Anchor内部历史、Acquire/Release Residual和其它Context字段 MUST不进入正式Resolved Result；它们 MAY只进入Diagnostics。

Resolved Foot Pair MUST只组合同Frame/Completion/Rig的左右Result，不重新计算状态、Support或Goal，不得成为第二Blackboard。

#### Scenario: Locked使用Sliding Response

- **WHEN** Locked脚当前使用Sliding Response
- **THEN** Resolved Foot MUST发布AcquireAndRetain及与8fc相同的Support Weight、Horizontal Error和Contact Reference
- **AND** MUST不向Primary Support暴露Lock Response

#### Scenario: Releasing只能保留支撑

- **WHEN** 脚处于Releasing
- **THEN** Resolved Foot MUST发布RetainOnly及与8fc相同的残余Support Weight
- **AND** Primary Support MUST不能从无Primary状态新获取该脚

### Requirement: Pelvis必须只消费Resolved Foot Pair并保持基线结果

Primary Support MUST只读取Resolved Pair中的Support Eligibility、Support Weight、Support Horizontal Error、Support Event identity和Contact Reference：AcquireAndRetain可获取并保留，RetainOnly只能保留，None不能参与。Selector MUST不读取Foot State、Lock Response或Context。

Stride与Pelvis MUST只读取Primary Support Result及Resolved Pair中的Final Sole、Pelvis Reach Reference和lineage。Stride端点、支持腿可达区间、Pelvis Target、Handoff与Spring MUST保持8fc逐值结果。本change MUST不读取Foot State/Lock Response、不提前接入Landing腿或读取新的Path Proposal改变Pelvis。

#### Scenario: Sliding Response脚成为Primary Support

- **WHEN** 一只Resolved Foot发布AcquireAndRetain且其内部脚处于Sliding Response
- **THEN** Primary Support MUST按8fc把它视为可获取且可保留候选
- **AND** Primary Support与Pelvis MUST不需要读取Sliding Response并产生与旧Sliding状态相同的结果
