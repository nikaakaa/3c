## MODIFIED Requirements

### Requirement: Foot Placement诊断必须只显示正式结果

Runtime Result MUST与Diagnostics严格分型。PoseGraph MUST在表现帧开始冻结具体`CharacterFootIkCaptureInterest`和View固定容量；仅在存在interest时，Foot、Constraint/FBBIK与Final Publication Owner才可把允许观察的Pending Result单向深冻结进各自预分配诊断页。根表现帧成功Seal后，`CharacterPoseDiagnosticsProjector` MUST只按同一Frame、Completion、Program、Projection、Rig、Tuning Generation与Bank lineage组合一份具体`CharacterFootIkCommittedCaptureViewLease`，包含正式Input、Observation、Transition、Target、连续Correction、Hard Constraint、Resolved、Goal、Solved与Physical事实，并由PoseGraph唯一控制其有效期与失效。

Gizmo、Trace与Pose Watch MUST只读取各自允许的Committed页；Foot Sampler MUST只通过同一Generated Program读取该唯一具体View租约。任何诊断路径不得查询世界、修改Foot状态、选择Support、生成Goal、执行FBBIK、读取FBBIK Vendor对象、读取Pending Workspace、反推Physical Transform，或通过Foot事件与Animation Snapshot/Pose Watch二次拼接同一帧。多个Sampler MUST共享该唯一View，不得让Runtime为每个Sampler复制第二套Foot、FBBIK或Physical事实。Diagnostics命名、Attribute、Schema、Sampler数量或布局变化 MUST不改变Runtime Result，也 MUST不进入PoseGraph View合同。

#### Scenario: 捕获正式Foot事实

- **WHEN** Foot、Pelvis、Goal、FBBIK和Final Publication完成同一根表现帧验证并Seal
- **THEN** PoseGraph MUST发布一份同lineage的`CharacterFootIkCommittedCaptureViewLease`
- **AND** Foot Generated Program MUST在该租约内为全部已选Sampler一次读取输入、过程、Resolved、Solved与Physical事实

#### Scenario: 当前帧没有Foot IK diagnostics interest

- **WHEN** 根表现帧开始时没有任何Foot IK Sampler、Pose Watch或Trace interest需要对应详情
- **THEN** Foot、FBBIK与Final Publication MUST不构造Foot IK View诊断payload，PoseGraph MUST不发布该租约
- **AND** 正式Foot、Goal、Solver与Physical结果 MUST按相同输入产生相同结果

#### Scenario: 同时启用多个Foot IK Sampler

- **WHEN** 多个Sampler在Frame开始前共同请求Foot IK committed facts
- **THEN** PoseGraph MUST只冻结和发布一份匹配该Frame的具体View租约
- **AND** MUST不增加Foot查询、Goal Assembly、FBBIK或Physical Writer执行次数
