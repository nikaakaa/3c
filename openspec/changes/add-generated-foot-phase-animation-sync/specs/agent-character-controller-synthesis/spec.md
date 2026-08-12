## MODIFIED Requirements

### Requirement: Agent 必须阻止 Marker Sync 数据分裂

Agent Document MUST只在实际animation source owner保存Marker Sync可写数据：有限Action producer MUST由对应AnimationTrack entity拥有，持续Pose source MUST由Profile中的对应source binding拥有。两类owner的MarkerGroup MUST保存mode、canonical group、topology、SyncRole、ordered marker与明确Time Mapping；不得把这些字段复制到TimelineNode override、Pose StateMachine Transition或Rule、Blackboard、ActionProfile、独立FootPhase、pair table或generated Projection。Target与Profile binding引用 MUST使用stable identity或Document local identity；名称、breadcrumb和index不得成为fallback。Validator MUST继续覆盖None残留、Unspecified Time Mapping、identity、Finite/Cyclic边界、call site、directed pair、relation两侧策略一致性、GeneratedFootPhase artifact readiness和animation output coverage。

#### Scenario: None Track保留Marker

- **WHEN** Document把Track设为None但仍保留Time Mapping、group或Marker
- **THEN** dry-run MUST拒绝整个Document
- **AND** apply MUST不产生部分资产修改

#### Scenario: AI修改持续Pose source同步策略

- **WHEN** Document把Run Profile source binding的Time Mapping改为GeneratedFootPhase
- **THEN** Reconciler MUST只生成该Profile binding的typed Mutation并保持Projection stale
- **AND** MUST不修改Pose Transition、创建Timeline Track或发布generated warp plan

#### Scenario: Transition重复保存同步策略

- **WHEN** Document在Pose Transition或Rule提交SyncMode、SyncGroupId、Time Mapping或warp payload
- **THEN** strict parser MUST在Reconciler前拒绝对应分片
- **AND** MUST要求同步关系由Transition两侧实际source owner推导

### Requirement: Agent Document必须完整读写 Timeline Marker 与 Curve Channel

Character Document package MUST在`timeline.json`按Timeline与Track stable identity表达有限Action Marker Sync mode、group、topology、SyncRole、Time Mapping、call-site playback和每个Marker identity/id/frame，并在`curves.json`按registered Curve Channel表达domain、unit、wrap和完整Keyframe语义。Reconciler MUST为策略修改、Marker创建、移动、删除和完整Curve替换生成typed Mutation；handler MUST只调用Timeline正式authoring API和Curve MutationAdapter。系统 MUST不接受key级MCP操作、旧Patch operation、字段名目标、可编辑generated warp payload或缺省Time Mapping。

#### Scenario: 修改weighted curve

- **WHEN** Document替换registered channel的完整curve
- **THEN** Mutation MUST保留time、value、tangent、weight、WeightedMode和wrap mode
- **AND** unknown channel MUST在mutation前失败

#### Scenario: 新增重复Marker语义

- **WHEN** Document增加第二个相同MarkerId但不同local identity的occurrence
- **THEN** Reconciler MUST接受独立occurrence
- **AND** apply后canonical Document MUST输出不同MarkerAuthoringId

#### Scenario: 修改Action Time Mapping

- **WHEN** Document把合法MarkerGroup AnimationTrack从MarkerSegmentFraction改为GeneratedFootPhase
- **THEN** dry-run MUST报告精确Track的typed策略变化和artifact readiness诊断
- **AND** apply MUST只修改authoring并使Projection stale，不得自动Build或生成warp knot
