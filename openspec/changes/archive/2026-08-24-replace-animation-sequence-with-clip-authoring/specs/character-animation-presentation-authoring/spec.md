## MODIFIED Requirements

### Requirement: Presentation Profile必须唯一绑定Pose source

Pose Graph MUST唯一拥有typed Source Slot，`CharacterAnimationPresentationProfile` MUST为每个Slot拥有唯一类型匹配Binding。Clip Binding MUST直接引用精确AnimationClip；Blend Space与Motion Matching Binding MUST继续引用各自正式资源。Profile MUST唯一引用角色Rig Definition、FullBodyIK Profile、Foot Analysis Source、有限Action producer binding和Locomotion Sync Group。Clip Binding与Action producer binding MUST不保存旧包装资产、Rig副本、Analysis identity副本或素材注册Curve副本。Blend Space与Motion Matching资源内部为各自Artifact保存的Rig/Analysis compatibility identity MAY保留，但只能作为Profile选择的准入约束，不得成为第二角色配置owner。

#### Scenario: ClipPlayer解析RunLoop

- **WHEN** ClipPlayer引用RunLoop Source Slot
- **THEN** Projection Compiler MUST从Profile唯一Clip Binding解析AnimationClip并分配dense source index
- **AND** MUST不经过旧包装资产或作者字符串查找

### Requirement: Animation Clip控制曲线必须作为typed Curve Channel编辑

项目表现控制曲线 MUST由唯一channel catalog注册，并直接保存于可写原生AnimationClip。Unity Animation Window MUST成为人工Curve key编辑入口；Agent Document MUST只读写同一注册Curve。Projection MUST把Curve降低为Runtime canonical plan。Profile、Timeline、Blend Space和Foot Analysis artifact MUST不保存可写Curve副本。

#### Scenario: 修改Foot Placement Weight

- **WHEN** 作者在Animation Window修改Clip的`presentation.foot-placement-weight`
- **THEN** 完整Clip dependency与Registered Curve Hash MUST变化并使Projection stale
- **AND** AnimationClipAnalysisInputHash与匹配Foot Analysis Artifact MUST保持不变
- **AND** Runtime MUST只在显式Build后消费新的Projection curve

### Requirement: 跨资产表现配置必须保持唯一写入口

Pose Graph Workspace、Navigator与Details MAY只读显示Action Timeline Segment、Profile direct Clip Binding、Locomotion Sync Group、Clip注册Curve、Policy、Rig与Analysis状态。修改Action Segment编排 MUST导航到Timeline Editor；修改Clip骨骼或注册Curve MUST打开Unity Animation Window中的精确Clip与Preview Target；修改Profile Binding或Sync Group MUST导航到Profile；修改State transition与Slot Policy MUST导航到Pose Graph/Policy owner。人工入口与Document v4 Reconciler MUST分别调用同一正式Mutation和资产事务，系统 MUST不复制字段、提供第二mutation命令、按窗口类型分叉写链或保留字符串binding镜像。

#### Scenario: 从Pose Graph调整Run Phase

- **WHEN** 作者在State source引用面板选择Open Source Curve
- **THEN** 必须打开RunLoop原生AnimationClip与正式Preview Target
- **AND** Pose Graph节点与Profile Binding MUST保持只读Clip引用摘要

## ADDED Requirements

### Requirement: Locomotion Sync Group必须只装配直接Clip成员

`CharacterAnimationPresentationProfile` MUST唯一保存Locomotion Sync Group的稳定GroupId与精确AnimationClip成员引用。一个Clip MUST最多属于一个Group。Group成员 MUST具有合法Locomotion Phase曲线并通过全部可达relation质量门槛；组外Clip MUST不保留无消费Locomotion Phase曲线。Group MUST不保存Marker、Time Mapping、SyncRole、Topology、pairwise warp或Transition副本。

#### Scenario: Walk与Run加入同一Group

- **WHEN** 作者把WalkLoop与RunLoop加入`Locomotion.Gait`
- **THEN** Compiler MUST从两项Clip的Phase Curve与Loop事实构建可达relation
- **AND** MUST不要求两项Clip复制Group策略或Marker序列

#### Scenario: 有限Clip关系质量失败

- **WHEN** Start或Turn的Landing锚点合法但有限出口与目标Loop不相容
- **THEN** Profile MUST把该Clip保持为组外Direct Clip
- **AND** Clip MUST删除Locomotion Phase且Transition MUST只执行显式Blend

### Requirement: Presentation Projection必须保存per-clip Phase与可达relation计划

Projection Compiler MUST为每个Locomotion Group成员编译固定容量forward/inverse Phase plan，把Direct Clip或Blend Space降低为`AnimationSourcePhasePlan`，并只为PoseState实际可达edge保存source-to-source relation。Direct Clip endpoint MUST引用自身Clip plan；Blend Space endpoint MUST引用显式Phase Reference Sample作为clock carrier和全部Dynamic Sample的per-clip inverse plan。Relation MUST包含RelationIdentity、TransitionId、两侧source plan identity、编译期固定leader、正式clock authority、实际有限秒域coverage与Artifact validation identity。Foot Analysis质量门槛不通过 MUST阻止Projection发布。Projection MUST不保存Editor AnimationCurve、Phase Validation samples、Marker occurrence、pairwise warp knot或Sequence identity。

#### Scenario: MovingTurn实际只播放28帧

- **WHEN** MovingTurn Clip长度为71帧但Gameplay committed clock只覆盖0至28帧
- **THEN** relation compiler MUST只校验和编译0至28帧实际coverage
- **AND** MUST不使用28帧后的Phase或Foot样本证明出口合法

## REMOVED Requirements

### Requirement: Animation Marker Sync 必须由实际source owner唯一拥有

该Requirement被删除，因为正式Locomotion同步只使用Clip Phase Curve与Profile Group，当前Action没有Marker Sync业务。

#### Scenario: 旧Marker字段进入Build

- **WHEN** Binding、Timeline Track、Clip或Document仍包含Marker Sync字段
- **THEN** 新schema MUST将其视为未知旧数据并失败

### Requirement: Marker Group 必须支持 Finite 与 Cyclic 序列

该Requirement被删除；Finite/Cyclic从AnimationClip Loop事实与实际业务coverage解析。

#### Scenario: 旧Topology字段存在

- **WHEN** authoring保留SequenceTopology或Marker topology
- **THEN** Validator MUST拒绝该旧字段

### Requirement: Marker Group 必须显式声明 handoff 同步角色

该Requirement被删除；Phase leader由Transition两侧正式raw clock authority和生命周期编译决定。

#### Scenario: 旧SyncRole字段存在

- **WHEN** source binding保留AlwaysLeader或AlwaysFollower
- **THEN** Validator MUST拒绝该旧字段而不建立兼容映射

### Requirement: Marker Group 必须在 Projection 构建前完整校验

该Requirement被删除并由Locomotion Phase、actual coverage和Foot Analysis关系质量校验取代。

#### Scenario: 旧Marker relation计划进入Projection

- **WHEN** generated payload包含Marker segment或occurrence
- **THEN** Projection Validator MUST拒绝该payload

### Requirement: Presentation Projection 必须保存规范化 Marker Sync 映射

该Requirement被删除；Projection只保存per-clip Phase plan与可达relation引用。

#### Scenario: 旧warp plan进入Runtime

- **WHEN** Projection包含GeneratedFootPhase或Marker fraction映射
- **THEN** Runtime MUST拒绝Projection revision
