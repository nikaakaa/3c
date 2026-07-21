## ADDED Requirements

### Requirement: Pose Source Selection必须独立于Program Playback生命周期

`AnimationPlaybackId` MUST只标识Program producer activation；`PoseSelectionGeneration` MUST标识该activation内部source pose selection。`AnimationBlendEntryId` MUST同时包含PoseSlot、Playback与Pose Selection identity。同Playback同Selection的sample更新 MUST为continuation；同Playback新Selection MUST创建新entry并使用该Slot exact transition matrix。

#### Scenario: MM playback内部跳转

- **WHEN** BaseLocomotion MM producer仍为同一Playback但选择不同Database sample
- **THEN** Runtime MUST提升PoseSelectionGeneration
- **AND** Stack MUST不能只按PlaybackId将其视为continuation

#### Scenario: Timeline producer连续采样

- **WHEN** Timeline producer在同一Playback和Pose Selection中更新时间
- **THEN** Runtime MUST继续更新同一entry
- **AND** 现有Timeline lifecycle MUST不被MM identity拆成逐帧entry

### Requirement: Motion Matching jump必须复用Pose Slot唯一Transition Matrix

每个绑定MM producer的PoseSlot MUST在编译Transition Matrix中包含该producer到自身的exact jump rule。MM Runtime MUST只提交source/target identity；Blend Stack MUST唯一决定CrossFade或Inertial、duration、curve、per-bone profile、Stored Pose与retirement。

#### Scenario: Run sample跳到Stop sample

- **WHEN** Search选择同MM producer内的新Stop sample
- **THEN** Stack MUST查找MM self-pair transition并创建entry
- **AND** MM MUST不传入临时blend duration或直接修改entry weight

### Requirement: Motion Matching source不得加入Timeline Marker Sync relation

MM source的contact连续性 MUST来自Database Foot Feature、candidate admission与plan；Marker Sync Runtime MUST只处理正式Timeline producer关系。Timeline与MM在同Channel发生handoff时 MUST只通过PoseSlot Blend Stack过渡，不得为MM伪造Timeline marker、cycle或relation。

#### Scenario: Airborne Timeline落地到Grounded MM

- **WHEN** BaseLocomotion Channel从普通Airborne producer切换到Grounded MM producer
- **THEN** Lifecycle MUST提交新的Pose Source selection和Slot transition
- **AND** Marker Sync MUST不创建跨source-kind relation
