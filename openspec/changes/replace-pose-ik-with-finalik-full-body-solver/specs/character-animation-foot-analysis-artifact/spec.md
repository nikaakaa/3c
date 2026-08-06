## MODIFIED Requirements

### Requirement: Animation Foot Analysis必须拥有Editor-only规范产物

Animation Foot Analysis MUST为`AnimationClip imported content + Rig Definition v4 + Sampling Rig prefab + Rig Calibration v4 + Geometry Validation Result + Analysis Settings + Analyzer Version`生成不可变Editor-only规范Artifact。Artifact MUST保存上述输入的stable identity、revision、hash、采样域、每脚连续feature channel与接触Marker候选；artifact identity MUST包含format version、AnimationClip GUID与import dependency、Analysis Source GUID/identity/version、Rig Definition v4 identity/revision/hash、Sampling Rig GUID/dependency、Rig Calibration v4 identity/revision、Geometry Validation identity/hash、sample rate、threshold、reduction与algorithm version。Artifact MUST不保存Preferred Bend、Knee Direction、FullBodyIK Profile或solver result。Artifact MUST写入固定`Library`存储根，不得进入Assets、Player、Addressables、YooAsset、Program、Snapshot或Network产物，也不得写回AnimationClip、Rig、Calibration、Timeline或Profile。相同输入 MUST产生相同artifact identity与规范payload。

#### Scenario: Calibration几何改变

- **WHEN** Heel、Toe、Sole Frame或Calibration Preview输入使Geometry Validation identity改变
- **THEN** 旧artifact MUST变为Stale
- **AND** Analyzer MUST不因Calibration revision字符串仍可解析而继续使用旧feature

#### Scenario: 同一合法输入重复分析

- **WHEN** 相同AnimationClip、Analysis Source和Geometry Validation identity重复构建
- **THEN** 系统 MUST产生相同canonical payload与artifact hash
- **AND** Store MUST解析到同一规范identity

#### Scenario: AnimationClip重新导入

- **WHEN** AnimationClip GUID不变但import dependency改变
- **THEN** expected artifact identity MUST改变并把旧artifact判为Stale
- **AND** MUST不因clip名称、duration或GUID仍相同而继续使用旧数据

#### Scenario: Rig biped语义改变

- **WHEN** Rig v4的solver root、spine、arm、ankle或toe BoneId、revision或content hash改变
- **THEN** 旧artifact MUST变为Stale
- **AND** Analyzer MUST不使用Sampling Rig旧Transform映射继续发布

### Requirement: 单Clip Analyzer不得依赖Tree或Projection

正式Analyzer MUST只接受精确AnimationClip、Rig Definition v4、Sampling Rig、Rig Calibration v4、Analysis Settings与Analyzer Version。它 MUST通过Rig v4 Physical BoneId绑定Sampling Rig Transform并执行独立PlayableGraph sampling，不得读取Tree、StateMachine、Timeline call site、CharacterPipelineDefinition、Profile runtime、FullBodyIK Profile、PresentationProjection、CharacterPipelineHost、当前Scene或Transform名称。Analyzer MUST生成左右脚有限feature curve set，先写完全部采样帧的heel、toe、sole位置与高度，再从完整循环位置序列计算中心差分速度，不得在未来采样帧尚未写入时读取它。Sampling Rig、Rig与Calibration identity/revision/hash不一致 MUST明确失败。

#### Scenario: 从独立Timeline分析Clip

- **WHEN** 作者选择AnimationClip与合法Analysis Source并执行Rebuild Selected Clip
- **THEN** Analyzer MUST只使用Analysis Source提供的Rig v4、Sampling Rig与Calibration v4生成或更新对应artifact
- **AND** MUST不执行Authoring Discovery、Semantic compile、Numeric lowering、FullBodyIK或完整Projection Build

#### Scenario: Sampling Rig Calibration不匹配

- **WHEN** Calibration声明的rig identity与Analysis Source的Rig v4或Sampling Rig不一致
- **THEN** Analyzer MUST拒绝生成Artifact并报告Rig、Sampling Rig与Calibration三方identity/revision/hash
- **AND** MUST不尝试按骨骼名称重绑或搜索其它Prefab补全

