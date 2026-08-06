## MODIFIED Requirements

### Requirement: Animation Foot Analysis必须拥有Editor-only规范产物

Animation Foot Analysis MUST为`AnimationClip imported content + Rig Definition v3 + Sampling Rig prefab + Rig Calibration + Geometry Validation Result + Analysis Settings + Analyzer Version`生成不可变Editor-only规范Artifact。Artifact MUST保存上述输入的stable identity、revision、hash、采样域、每脚连续feature channel与接触Marker候选；artifact identity MUST包含format version、AnimationClip GUID与import dependency、Analysis Source GUID/identity/version、Rig Definition v3 identity/revision/hash、Sampling Rig GUID/dependency、Rig Calibration identity/revision、Geometry Validation identity/hash、sample rate、threshold、reduction与algorithm version。Artifact MUST写入固定`Library`存储根，不得进入Assets、Player、Addressables、YooAsset、Program、Snapshot或Network产物，也不得写回AnimationClip、Rig、Calibration、Timeline或Profile。相同输入 MUST产生相同artifact identity与规范payload。

#### Scenario: Calibration几何改变

- **WHEN** Heel、Toe、Sole Frame、Preferred Bend或Calibration Preview输入使Geometry Validation identity改变
- **THEN** 旧artifact MUST变为Stale
- **AND** Analyzer MUST不因Calibration revision字符串仍可解析而继续使用旧feature

#### Scenario: 同一合法输入重复分析

- **WHEN** 相同AnimationClip、Analysis Source和Geometry Validation identity重复构建
- **THEN** 系统 MUST产生相同canonical payload与artifact hash
- **AND** Store MUST解析到同一规范identity

### Requirement: Definition Build必须精确消费Artifact并发布Projection

Definition Build MUST收集全部可达持续Pose Source与有限Action Animation Clip binding，按精确`AnimationClip + Analysis Source + Geometry Validation` identity校验或生成artifact，再把feature按每个stable source binding嵌入CharacterPresentationProjection。相同AnimationClip MAY复用一次artifact读取，但每个binding MUST保持独立source identity。任一artifact缺失、损坏、Calibration revision不匹配或Geometry Validation identity过期 MUST阻止本次Program/Projection发布。Projection MUST发布Runtime可核对的Rig、Calibration、Artifact与Geometry Validation identity，不得发布Sampling Rig或Preview Clip对象。

#### Scenario: Artifact Ready但几何验证过期

- **WHEN** Artifact的Calibration revision匹配但Geometry Validation identity不匹配当前Sampling Rig与Preview Pose
- **THEN** Definition Build MUST把Artifact判为Stale并阻止发布
- **AND** MUST不只调用数值级Calibration验证继续Build

#### Scenario: Artifact完整匹配

- **WHEN** Artifact payload、Calibration revision与Geometry Validation identity全部匹配
- **THEN** Definition Build MAY复用该payload而不重新采样AnimationClip
- **AND** Projection MUST发布精确validation identity供Runtime create核对

