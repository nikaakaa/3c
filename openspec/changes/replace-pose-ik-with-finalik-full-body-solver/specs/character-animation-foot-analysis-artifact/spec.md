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

## ADDED Requirements

### Requirement: Plant Confidence必须只表达源动画接触意图

每脚`PlantConfidence` MUST由单AnimationClip的校准鞋底高度与垂直速度分析生成，并 MUST继续作为源动画接触意图、稳定Landing候选与Motion Matching脚特征。`0.5` MUST只表示单Clip分析中的Planted/Unplanted语义边界。每脚`SoleLocalVelocity` MUST由该Clip左右Heel/Toe独立采样得到，并 MUST随最终Pose contribution的source权重与visual time scale混合。Runtime MAY使用混合后的`PlantConfidence`与`SoleLocalVelocity.magnitude`维护Plant Contact迟滞；Runtime MUST不把二者通过连续乘法直接变成普通Foot Goal Position/Rotation Weight，也 MUST不把`SoleLocalVelocity`与Body世界平移、可见速度或yaw点速度拼接。普通Current Grounding Goal的总alpha MUST只由Foot Placement总权重应用一次；Body Grounded与trace hit只按正式诊断及Lyra未命中分支参与，不得成为关闭普通Goal的gate。

#### Scenario: Run过渡混合两个源动画

- **WHEN** 左脚最终`PlantConfidence`因源动画混合得到`0.65`
- **THEN** Runtime MAY把它作为进入或维持接触意图的证据
- **AND** MUST不把它重映射为`0.3`并直接降低左脚Foot Goal或Pelvis支撑权重

#### Scenario: 左右脚烘焙数据来源

- **WHEN** 分析器处理同一个AnimationClip
- **THEN** 左右脚MUST分别从各自校准Heel/Toe轨迹生成`SoleLocalVelocity`、`SoleHeight`与`PlantConfidence`
- **AND** Runtime MUST消费最终贡献混合后的左右独立样本，不得把单脚曲线复制给另一脚或把actor运行速度伪装成烘焙脚速
