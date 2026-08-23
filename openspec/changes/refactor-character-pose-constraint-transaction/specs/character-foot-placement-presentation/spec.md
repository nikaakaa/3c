## RENAMED Requirements

- FROM: `### Requirement: 当前阶段必须只生成Swing脚垂直Goal`
- TO: `### Requirement: Foot Placement必须按FootPath、Landing Patch和Committed Anchor生成唯一双脚修正`

## MODIFIED Requirements

### Requirement: Foot Placement必须是唯一Goal事务

唯一`CharacterPoseConstraintRuntime` MUST为每个Actor和表现帧建立匹配Frame、Completion与Rig lineage的Pending根Bank。Foot Placement MUST形成一个深`CharacterFootPlacementModule`，其外部Interface只接收同帧不可变Frame Input并发布一个`CharacterFootPlacementResult`。调用方 MUST不知道或编排Landing Prediction、Ground Path、状态转换、Support、Pelvis与Goal编码顺序。

Module Implementation MUST为左右脚各执行一次`CharacterFootStateMachine`并生成唯一Resolved Foot Pair，再计算Primary Support、Pelvis与三个typed Goal Contribution；不得发布第二Goal Set、第二Pelvis、第二FBBIK或第二Physical Writer。

#### Scenario: 正常生成Foot Placement贡献

- **WHEN** 同一表现帧具有合法Component Pose、Step、Body、World Query、Profile和根Pending Bank
- **THEN** Foot Placement MUST生成同Frame、Completion与Rig lineage的Resolved Foot Pair、Pelvis Result和三个Goal Contribution
- **AND** 调用方 MUST不取得或逐个提交Foot Context、Ground Path、Pelvis或Solver状态

#### Scenario: 重复执行Foot Placement

- **WHEN** 同一Frame与Completion第二次请求Foot Placement Prepare
- **THEN** 根Runtime MUST报告非法调用顺序并阻止整帧发布
- **AND** MUST不建立第二Foot Placement事务

### Requirement: Foot Placement必须按FootPath、Landing Patch和Committed Anchor生成唯一双脚修正

Foot Placement MUST让每只脚只经过唯一Swing Path Target、唯一State Machine、唯一Effective Correction和唯一Committed Anchor链路。

```text
Landing Prediction
-> Proposal/Ground Path
-> Swing Path Target
-> CharacterFootStateMachine
-> Resolved Foot Result
```

Swing MUST用Animated Sole在`LastCommittedContact -> NextLandingProposal`方向上的空间投影计算进度，并按同一进度采样Baseline与Ground Envelope：

```text
RawSwingCorrection =
    ComponentUp * max(0, EnvelopeHeight - BaselineHeight)

PathTargetCorrection =
    animation.foot-placement-weight * RawSwingCorrection
```

系统 MUST保留动画脚XZ、下降轨迹、最高点与旋转；MUST不按动画Phase代替空间进度，不得使用`Envelope - AnimatedSole`、`Baseline - AnimatedSole`、未来Landing Height、实时Path硬地面下限、Current Foot Trace或旧IK Pose重建Swing轨迹。

同Event Path Target变化时，State Machine MUST保留上一Committed Effective Correction/Velocity并只替换Target。Path Tracking Status MUST发布`Stable/Rebasing/Unavailable`，但 MUST不成为Landing准入、Contact状态或第二Output Owner。

每脚Constraint状态 MUST只包含`Swing`、`Landing`、`Locked`、`Releasing`与`UnlockedSupport`：

- `ApproachStarted`只属于动画分析事实，不触发状态或脚Goal。
- `LandingStarted` MUST是Projection发布的唯一晚期高度交接起点，只触发`Swing -> Landing`。
- `LandingHeightProgress` MUST由Projection发布并在LandingStarted到PlantStarted之间从0单调覆盖到1；Runtime不得用固定Duration、Constraint原值或PlantConfidence原值补造。
- `PlantStarted` MUST是Projection发布的唯一Plant onset，只触发`Landing -> Locked`。
- `Locked` MUST拥有唯一Committed Anchor。
- `Releasing` MUST从当前Effective Correction返回动画脚。
- `UnlockedSupport` MUST表达该Event没有合法Anchor。

Landing入口 MUST冻结`Event/Path/Surface/PlanePoint/Normal`，不得冻结Prediction Point为Anchor。入口 MUST捕获一次`CurrentEffectiveCorrection - CurrentLandingCorrection`残差，并只按单调`LandingHeightProgress`衰减。Path是否Settled不得阻止有效Landing事件；实时Path Revision只能更新下一Event。

PlantStarted当帧 MUST使用`CurrentEffectiveSole`沿Component Up投影Frozen Patch生成Committed Anchor。Anchor XZ MUST来自当帧Effective Sole。只有当LandingHeightProgress完整、Patch/投影有效且入口差异处于正式容差内时，State Machine才 MAY原子执行`Landing -> Locked`；否则 MUST进入UnlockedSupport。系统 MUST不增加Planting/Acquiring状态、隐藏`HasAnchor`子状态、Plant后的固定Duration Acquire或第二Plant来源。

Locked MUST严格输出`CommittedAnchor - AnimatedSole`且非零Goal权重为1；不得通过FootPlacement/Contact权重、horizontalWeight、Sliding、Anchor移动或实时Path Clamp削弱它。Anchor超距或不可达 MUST进入Releasing。

Release MUST按Projection发布的单调Release Progress衰减入口Residual；Grounded丢失、Anchor超距或不可达才使用Safety Release。完成后 MUST继续使用同一个Effective Correction/Velocity进入Swing。

#### Scenario: Swing脚采样FootPath

- **WHEN** Swing Event具有Accepted Ground Envelope
- **THEN** Raw Swing Correction MUST逐值等于非负`Envelope - Baseline`高度增量
- **AND** Swing Correction沿Component Up MUST永远不小于0

#### Scenario: 同Event Path Target变化

- **WHEN** 新Prediction形成新的合法Path Target
- **THEN** State Machine MUST保留上一Effective Correction与Velocity，只替换Target
- **AND** 实时Target MUST不通过硬地面下限、Goal Encoder或第二平滑直接设置脚高

#### Scenario: Path仍在Rebasing时开始Landing

- **WHEN** Projection发布LandingStarted且具有合法匹配Patch，但Path尚未Settled
- **THEN** State Machine MUST进入Landing、冻结当前Patch并捕获入口Residual
- **AND** MUST不因为Path未Settled拒绝Landing，也不得冻结Prediction Point为Anchor

#### Scenario: Landing Height计划不完整

- **WHEN** PlantStarted到达但LandingHeightProgress未单调覆盖到1
- **THEN** 当前Event MUST进入UnlockedSupport并被消费
- **AND** Runtime MUST不强制追点、增加Acquiring状态或晚到重锁

#### Scenario: Plant时创建Anchor

- **WHEN** LandingHeightProgress完整、Frozen Patch有效且CurrentEffectiveSole可在容差内投影
- **THEN** Committed Anchor MUST使用投影后的当帧Effective Sole
- **AND** State Machine MUST原子执行`Landing -> Locked`且Final Sole差异不超过几何容差

#### Scenario: 冻结后Path继续变化

- **WHEN** Landing、Locked或Releasing收到新Prediction/Path Revision
- **THEN** 当前Frozen Patch和Committed Anchor MUST不变
- **AND** 新事实 MUST只更新下一Event

#### Scenario: Locked接触超距

- **WHEN** Locked脚水平误差超过ReleaseDistance
- **THEN** Constraint MUST进入Safety Releasing并保持Anchor不变
- **AND** MUST不通过Sliding权重移动或削弱Anchor

### Requirement: Foot Placement诊断必须只显示当前事实

Runtime Result MUST与Diagnostics严格分型。Diagnostics MUST记录Path Tracking、LandingStarted/LandingHeightProgress/PlantStarted/ReleaseStarted、状态与Cause、Active/Consumed Event、Frozen Patch、Committed Anchor、Effective Correction/Velocity、Landing/Release Residual、Support Intent、Resolved Sole、Pelvis Reference、Goal、FBBIK Solved与Physical Position。

Gizmo、CSV、Trace与Pose Watch MUST只读取相同Frame、Completion、Rig和Bank identity的Committed深冻结页。Diagnostics MUST不查询世界、修改状态、选择Support、生成Goal或执行FBBIK。旧LandingPreparation、PlantConfidence Ownership、Sliding、SupportLock、GoalTransition与兼容列 MUST删除。

#### Scenario: 捕获成功提交帧

- **WHEN** Foot、Pelvis、Goal、FBBIK和Pending Pose完成验证
- **THEN** Diagnostics Projector MUST深冻结Runtime事实，Writer成功时补入Physical结果
- **AND** CSV MUST只读取Committed页

### Requirement: Ground Path必须使用上一已提交落点与下一事件落点

每只脚 MUST在同一Foot Context中维护上一Locked Contact与下一Event Prediction。Swing阶段每个有效表现帧 MUST执行一次且仅一次Future Landing查询；同Event合法Prediction按更新死区维护Proposal和Path Target。进入Landing并冻结Patch后，当前Event的新Prediction不得修改Frozen Patch或后续Anchor，但 MAY准备不同Next Event。

查询失败 MUST发布typed rejection，不得读取旧Diagnostics、Animated Sole、默认地面或另一查询路径补事实。

#### Scenario: Landing后准备下一Path

- **WHEN** 当前Event已冻结Patch且不同Next Event获得合法Prediction
- **THEN** State Machine MAY更新Next Event的Proposal、Path Target与Tracking Status
- **AND** 当前Patch、Anchor与Effective Correction MUST不读取该Target

### Requirement: Ground Path模块必须保持抽象与实现分离

Foot Placement核心 MUST只依赖World Query合同、纯Ground Envelope Builder和预分配页。Unity Adapter只执行查询与固定容量写入；不得选择Step、保存状态、平滑Correction、冻结Patch、创建Anchor、构造Pelvis或写Goal。

Ground Path payload、Foot Context、Patch、Anchor与唯一Effective Correction/Velocity MUST属于同一根Bank。只有State Machine可以写Foot Context。

#### Scenario: 整帧Discard

- **WHEN** Pending Ground Path与Foot Context已生成但后续阶段失败
- **THEN** Committed Context、Patch、Anchor、Correction与Pelvis状态 MUST保持上一成功帧
- **AND** 下一帧 MUST不读取被丢弃的事实

## ADDED Requirements

### Requirement: Foot Constraint必须由显式typed State Context驱动

每只脚 MUST使用一个固定布局的`CharacterFootStateContext`，集中保存State、Event、Path Target/Tracking、Frozen Patch、Committed Anchor、唯一Effective Correction/Velocity、Landing/Release Progress和Residual。系统 MUST不使用字符串Key、共享Dictionary、Gameplay Blackboard、动态字段或可变Diagnostics保存Foot状态。

State Machine MUST是Context唯一写入者，并在一次Evaluate中生成Pending Context和Resolved Foot。调用方、Pelvis、Goal、Diagnostics与Reactive输入 MUST不能直接写Context。

#### Scenario: 同帧多个打断

- **WHEN** Action、Grounded丢失、ReleaseStarted、PlantStarted和Path Revision同帧成立
- **THEN** Trigger Resolver MUST只产生一个Constraint Trigger且最多转换一次状态
- **AND** Path Observation MUST不能修改已冻结Patch或Anchor

### Requirement: Pelvis必须只消费Resolved Foot Pair

Primary Support、Stride与Pelvis MUST只读取Resolved Pair中的Final Sole、Patch/Anchor Reference、Support Intent、Path Tracking和lineage。Support Intent MUST与Contact Ownership分离；Landing尚未Locked时也 MAY提供Support Intent和腿可达区间。Pelvis MUST同时保证上一支撑腿与Landing腿可达，Rebasing实时Proposal不得成为Stride终点。

#### Scenario: Landing腿尚未Locked

- **WHEN** 一只脚处于Landing且Support Intent非零
- **THEN** Resolved Foot MUST发布Frozen Patch、Support Intent与Landing腿可达区间
- **AND** Pelvis MUST不等待Locked帧才接入该腿，也不得伪造Anchor
