## MODIFIED Requirements

### Requirement: Foot Placement诊断必须只显示正式结果

Runtime Result MUST与Diagnostics严格分型。Constraint Module MAY按Frame开始冻结的interest，从Pending Context、Observation、Resolved Result和Constraint阶段Result单向深冻结Phase Progress、Baseline、Envelope、Swing Correction、Residual、Anchor、Contact Progress、Ownership、Support Eligibility、Support、Pelvis、Goal与Solved结果；这些事实只能进入`CharacterPoseConstraintCommittedResult`。Physical Write与最终Physical Bone结果 MUST只由Final Publication冻结进`CharacterFinalPosePublicationCommittedResult`。

Gizmo、Trace与Pose Watch MUST只读取各自允许的Committed页。Foot采样 MUST在成功Seal后的同步Commit调用栈内，从相同Frame、Completion、Program、Projection、Rig和Actor lineage的Constraint、Final Publication与当前Source已提交状态直接取得Left／Right与公共Fact Root，并以`in`执行一行target-scoped `DiagnosticEvent` partial调用；MUST不先组合或消费`CharacterFootIkCommittedCaptureViewLease`、Runtime Snapshot、Dimension View、Consumer／Binding或第二事实页。帧开始的可选partial Query只在匹配target订阅时要求Physical Writer冻结真实Physical Ankle事实；未订阅和Disabled构建不得执行该读取。`character-foot-ik-diagnostic-sampling`只提供字段分类、Sampler／Program Definitions和Editor workflow；`generated-diagnostic-sampling-framework`只拥有生成程序、typed packet、Capability Session与Writer／Reader。PoseGraph与Constraint不得拥有任一下游编译器、Schema、packet或Host知识。旧Foot单体Analyzer／Publisher、Diagnosis Store、旧CSV与历史兼容Reader直接删除；独立`character-foot-diagnostic-analysis`只在通用Host完成封存后读取生成Artifact并拥有Operator、当前Plan、七维评分和报告，Foot Runtime不维护其输入绑定或结果。固定输入回放 MUST通过同一Artifact Reader读取所需证据。Diagnostics MUST不查询世界、修改Context、选择Support、生成Goal、执行FBBIK、读取未冻结Physical Transform反推结果或把Constraint与Physical事实写回同一业务Bank。

#### Scenario: 捕获正式Foot事实

- **WHEN** Foot、Pelvis、Goal、FBBIK、Pending Pose与Physical Writer均成功提交
- **THEN** Foot采样 MUST从同一lineage的Constraint、Final Publication与当前Source已提交状态直接传入可对账冻结基线的正式事实；Live／Trace Projector MAY独立发布只读View
- **AND** Diagnostics页归属变化 MUST不改变Runtime Result、Final Pose或Physical Writer输入

#### Scenario: Writer失败

- **WHEN** Constraint Result已经完成但Final Publication在Physical Writer前或Writer中失败
- **THEN** Diagnostics MUST不发布本帧Pending Constraint或Physical结果
- **AND** Projector MUST不为Foot Capture借用上一帧或发布第二Snapshot；Live／Pose Watch只能按各自既有合同保留上一Committed事实或正式Actor Fault

#### Scenario: 离线诊断Foot Artifact

- **WHEN** 通用Host已经完成Foot Capability manifest、Schema和CSV封存
- **THEN** 独立Foot诊断器 MAY读取这些不可变Artifact执行当前Plan
- **AND** Foot Runtime、Constraint和Final Publication MUST不持有Analyzer、Plan、评分或报告引用

#### Scenario: 增加响应解释字段

- **WHEN** 仅增加本帧响应原因或前后数值的诊断记录
- **THEN** 运行状态、脚目标、Pelvis、Bend与最终骨骼 MUST保持不变
- **AND** MUST不要求修改Goal Assembler、Solver算法或质量评分政策
