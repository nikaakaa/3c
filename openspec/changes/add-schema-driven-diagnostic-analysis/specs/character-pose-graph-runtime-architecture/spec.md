## MODIFIED Requirements

### Requirement: Diagnostics必须只投影Committed typed Result

系统 MUST在Frame开始冻结Diagnostics interest和容量，并只在有interest时从Module Pending Result向预分配诊断页深冻结允许观察的数据。成功Seal后，`CharacterPoseDiagnosticsProjector` MUST只读取匹配同一lineage与Tuning Generation的Committed Source、Program、Constraint和Final Publication Result；MUST不持有Runtime Module引用、不读取Pending Workspace、Actor State私有页、Foot Context、FBBIK Vendor对象或Physical Transform反推结果，也 MUST不参与任何运行决定。

Runtime MUST在Frame开始通过可消除的partial Query冻结Foot IK target interest，并只在成功Seal后的同步Commit调用栈内，从Constraint、Final Publication与当前Source的已提交Owner直接取得同lineage Left／Right与公共Fact Root，以`in`调用一行`DiagnosticEvent` partial触发点。采样链 MUST不创建或消费`CharacterFootIkCommittedCaptureViewLease`、Runtime Snapshot、Dimension View、Consumer／Binding或第二事实页；Live、Trace与Gizmo所需的具体View MAY继续由Runtime Projector独立生产，但不得成为采样输入。PoseGraph只拥有业务Event时机和已提交事实，不拥有Diagnostic Capability、字段Attribute、Sampler Definition、Schema Compiler、AOT Generated Capture Program、typed packet、Host Adapter、CSV、geometry、Analyzer、Plan、Operator、Publisher或评分。独立`character-foot-ik-diagnostic-sampling`Program从业务程序集生成的公开Event合同订阅并调用`generated-diagnostic-sampling-framework`生成的Capture；框架Session MUST不索取或保存业务Owner与View。旧Foot单体Analyzer／Publisher、Diagnosis Store与兼容Reader删除，不再形成PoseGraph必须维护的下游合同；新的独立离线Foot诊断器只消费Host封存后的Artifact，也不形成PoseGraph运行合同。Disabled构建的Event与Query调用及参数求值 MUST被编译消除，PoseGraph Runtime MUST不保留Sampling AssemblyRef、Event identity、领域Program、packet、Host、Analyzer或Build类型，也 MUST不为旧Sampler建立表达式／反射路径、兼容DTO、第二Snapshot或临时Adapter。

#### Scenario: 同时观察Player、Foot和FBBIK

- **WHEN** 当前Frame开始时存在对应Pose Watch和detail interest且Frame成功Seal
- **THEN** Projector MUST从同一Committed lineage发布Player Pose、Foot Contribution、Goal Set、FBBIK Pose和Physical结果
- **AND** MUST不重新采样source、执行world query或调用FBBIK

#### Scenario: interest在Frame中途打开

- **WHEN** Editor在当前Frame开始之后增加detail interest
- **THEN** 当前运行结果 MUST保持不变且完整详情 MAY从下一成功Frame开始
- **AND** Projector MUST不读取Pending页补齐半帧Snapshot

#### Scenario: Foot IK AOT Capture消费已提交事实根

- **WHEN** 匹配Sampler Set与Schema identity的Foot IK Capture Session在Frame开始前声明detail interest且当前Frame成功Seal
- **THEN** Runtime MUST在同步Commit调用栈内执行一行Foot `DiagnosticEvent` partial调用并传入target、真实lineage、Left／Right与公共Fact Root
- **AND** generated Program handler MAY调用Editor／IL2CPP AOT Capture Program写入预分配packet，但PoseGraph Runtime MUST不解释Sampler字段、执行生成程序、生成CSV或保留Consumer／Binding／旧事件／View二次join

#### Scenario: 离线诊断不进入PoseGraph

- **WHEN** Host在Runtime之外对已封存Foot Artifact执行当前诊断Plan
- **THEN** PoseGraph MUST不接收Plan、Operator结果、评分或报告
- **AND** 离线诊断失败 MUST不影响PoseGraph运行状态或已封存Capture
