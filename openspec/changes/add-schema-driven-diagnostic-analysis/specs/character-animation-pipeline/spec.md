## MODIFIED Requirements

### Requirement: 动画调试只能读取正式Snapshot

系统 MUST在Frame开始冻结Live、Capture、Pose Watch与detail interest及容量。Source、Program、Constraint和Final Publication Module MUST只在有匹配interest时从已完成Pending Result向各自固定诊断页深冻结数据；成功Seal后，唯一Diagnostics Projector MUST从匹配同一lineage与Tuning Generation的Committed Result生成只读Snapshot。Snapshot MAY包含Action lifecycle、source readiness/usage、PoseState、Player、Transition、Slot、Blend、Inertialization、Operation、Pose、Goal Contribution、Goal Set、FBBIK、Final Pose与Physical结果，但 MUST不参与运行计算。

Diagnostics Projector MUST不持有Program Runtime、Source Module、Constraint Module或Final Publication的可变引用，不得读取Pending Workspace、Actor State私有页、Foot Context、FBBIK Vendor对象或Physical Transform反推，也不得从Animancer weight重建事实。没有interest时 MUST跳过对应大页与逐骨骼复制，但正式执行结果不变。

成功Seal后，Runtime Snapshot或具体`CharacterFootIkCommittedCaptureViewLease` MAY继续服务Live、Trace与Gizmo，但 Foot采样 MUST在同步Commit调用栈内从同一lineage的已提交Owner直接取得Left／Right与公共Fact Root并以`in`调用冻结consumer，不得消费Runtime Snapshot、Capture View、万能Committed View或第二事实页。`generated-diagnostic-sampling-framework`唯一拥有通用AOT生成、typed packet、Capability Session、Writer和Host Finalizer合同，`character-foot-ik-diagnostic-sampling`只拥有字段分类、Sampler／Program Definitions、薄GeneratedCapture与Editor workflow。PoseGraph不得认识Schema、Generated Program、packet、Host或字段映射。旧Foot单体Analyzer／Publisher、Diagnosis Store与旧格式兼容路径直接删除；独立Host-only Foot诊断器 MAY在Capability manifest完成后读取生成Artifact并执行当前Plan、Operator、评分与报告，但不得形成动画Runtime、PoseGraph或采样Session的第二报告状态机。

#### Scenario: 导出每帧调试数据

- **WHEN** 当前Frame成功Seal且存在匹配diagnostics interest
- **THEN** Snapshot MUST只表达同一Frame、Completion、Program、Projection与Rig的Committed结果
- **AND** 关闭或打开调试历史 MUST不改变正式播放、Goal或Final Pose

#### Scenario: Module结果未提交

- **WHEN** Program或Constraint Pending Result完成但后续Writer失败
- **THEN** Diagnostics MUST不发布该Pending结果
- **AND** Projector MUST继续只见上一Committed Snapshot或Actor Fault事实

#### Scenario: Diagnostics interest中途变化

- **WHEN** Editor在当前表现帧中途打开Foot Placement或FBBIK detail interest
- **THEN** 本帧运行Result MUST保持不变且完整诊断 MAY从下一成功帧开始
- **AND** Runtime MUST不读取Pending页补齐半帧Snapshot

#### Scenario: Host分析已封存Artifact

- **WHEN** 动画Runtime已经退出本次Capture且通用Host完成Capability封存
- **THEN** 独立Analyzer MAY读取不可变Artifact产生诊断结果
- **AND** Analyzer MUST不回调动画Runtime、重新求值Pose或改变任何Committed页
