## MODIFIED Requirements

### Requirement: Animation Foot Analysis必须拥有Editor-only规范产物

Animation Foot Analysis MUST为`AnimationClip imported content + Rig Definition v3 + Sampling Rig prefab + Rig Calibration + Analysis Settings + Analyzer Version`生成Editor-only规范Artifact。Artifact MUST保存上述输入的stable identity、revision、hash、采样域、每脚连续feature channel与接触Marker候选；不得写回AnimationClip、Rig、Calibration、Timeline或Profile。相同输入 MUST产生相同artifact identity与规范payload。

#### Scenario: 同一输入重复分析

- **WHEN** 作者对相同Clip、Rig v3、Sampling Rig、Calibration和Settings重复执行Analysis
- **THEN** Store MUST得到相同artifact key与等价payload
- **AND** MUST不创建重复owner或修改输入资产

#### Scenario: AnimationClip重新导入

- **WHEN** imported content revision改变
- **THEN** 旧artifact MUST变为Stale
- **AND** MUST不因文件名相同继续被Projection消费

#### Scenario: Rig腿链改变

- **WHEN** Rig v3的ankle或toe BoneId revision改变
- **THEN** 旧artifact MUST变为Stale
- **AND** Analyzer MUST不使用Sampling Prefab旧Transform映射继续发布

### Requirement: 单Clip Analyzer不得依赖Tree或Projection

Analyzer MUST只接收精确AnimationClip、Rig Definition v3、Sampling Rig、Rig Calibration、Settings与Analyzer Version。它 MUST通过Rig v3 Physical BoneId绑定Sampling Rig Transform并执行独立PlayableGraph sampling，不得读取Tree、Timeline运行状态、Profile runtime、Projection、CharacterPipelineHost、当前Scene或Transform名称。Sampling Rig、Rig与Calibration identity不一致 MUST明确失败。

#### Scenario: 从独立Timeline分析Clip

- **WHEN** 作者在Timeline Analysis选择Clip和合法Analysis Source
- **THEN** Analyzer MUST只使用Analysis Source提供的Rig v3、Sampling Rig与Calibration
- **AND** MUST不要求Character Definition已Build

#### Scenario: Sampling Rig Calibration不匹配

- **WHEN** Calibration声明的rig identity与Analysis Source的Rig v3或Sampling Rig不一致
- **THEN** Analyzer MUST拒绝生成Artifact
- **AND** MUST不尝试按骨骼名称重绑
