## ADDED Requirements

### Requirement: Pose Source Selection必须独立于Program Playback生命周期

`PoseStateRelevanceGeneration` MUST标识MM source需求，`PoseSelectionGeneration` MUST标识该需求内部source pose selection。State内部Selection的完整source identity MUST同时包含relevance与Pose Selection identity。同relevance同Selection的sample更新 MUST为continuation；同relevance新Selection MUST产生新source identity。显式Player MUST按自身语义处理新identity。

#### Scenario: MM playback内部跳转

- **WHEN** Locomotion PoseState relevance未变但MM选择不同Database sample
- **THEN** Runtime MUST提升PoseSelectionGeneration
- **AND** 显式Player MUST按provider、player、Presentation source与新generation识别discontinuity，不得读取Gameplay PlaybackId

#### Scenario: Timeline producer连续采样

- **WHEN** Timeline producer在同一Playback和Pose Selection中更新时间
- **THEN** Runtime MUST继续更新同一Player source
- **AND** 现有Timeline lifecycle MUST不被MM identity拆成逐帧entry

### Requirement: Motion Matching jump必须复用显式Player语义

MM Runtime MUST只提交新的Selection source identity，不提交临时blend duration或weight。若Selection连接`BlendStack`，该节点的Blend Policy MUST包含所有可达MM source pair的exact CrossFade rule，并唯一决定duration、curve、per-bone profile、Stored Pose与retirement；若连接`SelectedPosePlayer -> Inertialization`，则局部Inertialization Policy MUST决定HardCut或Inertialize。MM不得拥有任一种连续性状态。

#### Scenario: Run sample跳到Stop sample

- **WHEN** Search选择同MM provider内的新Stop sample
- **THEN** 显式Blend Stack节点 MUST查找exact transition并创建entry
- **AND** MM MUST不传入临时blend duration或直接修改entry weight

### Requirement: Motion Matching source不得加入Action Timeline Marker Sync relation

MM source的contact连续性 MUST来自Database Foot Feature、candidate admission与plan；Action Marker Sync MUST只处理正式Timeline producer关系。PoseState从Sequence/BlendSpace切换到MM时 MUST只通过State transition、State Source Sync Plan和显式Player处理，不得为MM伪造Timeline marker、cycle或playback relation。

#### Scenario: Airborne Timeline落地到Grounded MM

- **WHEN** PoseState从普通Airborne source切换到Grounded MM source
- **THEN** State relevance MUST提交新的Selection source identity
- **AND** Marker Sync MUST不创建跨source-kind relation
