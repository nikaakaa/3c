## ADDED Requirements

### Requirement: Motion Matching Database必须复用唯一Foot Analysis Artifact

MM Database Builder MUST按Clip GUID、dependency hash、Rig Calibration、Analysis Source identity与Algorithm Version精确解析现有Animation Foot Analysis Artifact，并把其heel/toe、plant、landing、height与sole speed feature降低为MM sample contact metadata。MM模块 MUST不创建第二个Foot Analyzer、FootPhase Track、Marker或Blackboard foot数据源。

#### Scenario: Walk Clip已有合法Foot Artifact

- **WHEN** MM Builder分析该Walk segment
- **THEN** Builder MUST按sample time重采样现有Foot feature
- **AND** Artifact identity MUST记录被消费的Foot Artifact hash

#### Scenario: Foot Artifact与Clip不匹配

- **WHEN** Foot Artifact属于旧Clip dependency hash或不同Calibration
- **THEN** MM Build MUST失败
- **AND** MUST不现场重算或忽略contact feature

### Requirement: Motion Source Set必须通过显式批量入口准备Foot Analysis Artifact

Source Set Inspector MUST提供显式`Build Source Set Foot Analysis`重操作。执行前 MUST显示Analysis Source、Sampling Rig、Clip总数、Ready/Missing/Stale数量与预计sample数量；作者确认后 MUST按稳定SourceClipId顺序逐Clip调用现有唯一`AnimationFootAnalysisArtifactBuilder`。该入口 MUST不实现第二个Analyzer，不依赖Timeline producer，也不得由asset import、selection、Inspector repaint、OnValidate、普通Character Compile或MM Database Build隐式触发。

#### Scenario: 作者显式准备新导入动画

- **WHEN** 作者确认Build Source Set Foot Analysis
- **THEN** Job MUST只为该Source Set中Missing或Stale的Clip调用正式单ClipBuilder
- **AND** 每个Artifact MUST继续按现有exact identity与原子发布合同生成

#### Scenario: 作者取消批量Foot Analysis

- **WHEN** Job在两个Clip之间收到Cancel
- **THEN** Job MUST停止后续Clip并释放进度状态
- **AND** 已完整发布的单Clip Artifact MAY保持Ready，未开始或未完成的Clip MUST保持原状态

#### Scenario: 只登记或选择Clip

- **WHEN** 作者登记Source Clip、切换Project selection或打开Source Set Inspector
- **THEN** 系统 MUST不实例化Sampling Rig且不调用Foot Analyzer
- **AND** MUST只显示轻量identity与Artifact status

### Requirement: Foot Analysis与MM Contact Protection必须保持语义分层

Foot Analysis Artifact MUST继续只表达动画局部特征；MM MAY据此判断candidate contact compatibility，但 MUST不把该结果写回Foot Artifact或宣称为world contact。Foot Placement MUST继续根据最终pose与world query决定Locked/Sliding/Free。

#### Scenario: MM认为左脚处于protected plant

- **WHEN** query使用Foot Artifact feature拒绝一个candidate
- **THEN** 该拒绝 MUST只影响Presentation pose selection
- **AND** MUST不产生Gameplay Grounded、FinalIK Grounding结果或PredictiveFootPlacement world anchor
