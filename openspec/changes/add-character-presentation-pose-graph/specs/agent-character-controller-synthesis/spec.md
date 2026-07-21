## MODIFIED Requirements

### Requirement: 正式资产必须仍由人类可微调

系统 MUST保持Agent生成后的正式结果为普通BTSMTL Graph、Timeline、ActionProfile，以及由CharacterPipelineDefinition引用的CharacterAnimationPresentationProfile。作者 MUST能在BTSMTL Graph Editor调整逻辑，在Timeline Editor调整clip/time与AnimationChannelId，在Profile/Pose Graph Editor调整Pose Slot、Bone Mask、Pose Parameter和composition，在Blend Library Inspector调整transition。Agent Snapshot MAY只读理解Profile、Pose Graph与Presentation identity，但本capability的Agent Patch MUST不形成第二个Presentation写入口。

#### Scenario: 作者微调生成结果

- **WHEN** Agent生成普通Tree branch、Attack State与Timeline
- **THEN** 作者 MUST在BTSMTL Graph Editor调整logic rule
- **AND** 在Timeline Editor调整clip/time/channel
- **AND** 在Profile/Pose Graph/Blend Library正式入口调整表现composition
- **AND** 这些入口 MUST不双写同一字段

#### Scenario: Agent继续修改Gameplay

- **WHEN** 作者微调Pose Graph后再次请求Agent增加dodge cancel
- **THEN** Agent MUST基于新的BTSMTL Graph、Timeline与只读Presentation identity生成增量Patch
- **AND** MUST不覆盖Pose Graph、Blend Library、Rig或Profile修改

### Requirement: Agent Snapshot 与 Validator 必须递归理解嵌套 StateMachine

Agent Snapshot MUST递归输出完整RootTree authoring routes、RunnableNode、flow edges、inline/shared Graph、nested StateMachine、logical transitions、Action activation、Timeline与稳定animation producer identity。Presentation section MUST只读输出Animation Channel catalog、PoseSlot binding、PoseGraph identity/revision、Blend Library identity、Rig identity与producer resource binding。Validator MUST检查Graph topology、route identity、Timeline identity与Timeline AnimationChannelId，但 MUST不校验或写入Pose Graph topology、Blend transition、Rig、runtime Stack或PoseGraph lifecycle。

#### Scenario: Corin Snapshot

- **WHEN** 导出Corin compact Snapshot
- **THEN** Graph section MUST显示Root、Locomotion、外层Action和nested Attack route
- **AND** Presentation section MUST只读显示BaseLocomotion/FullBodyAction channel、两个Pose Slot及Profile binding identity
- **AND** BTSMTL Graph Node/Edge MUST不输出Bone Mask或Pose composition字段

#### Scenario: Timeline channel identity断裂

- **WHEN** 可达AnimationTrack缺失或引用未知AnimationChannelId
- **THEN** Validator MUST输出对应Graph/Timeline source错误
- **AND** Compiler transaction MUST回滚

### Requirement: Agent 不得形成第二个动画表现 authoring 入口

Agent Patch compiler MUST只编辑正式BTSMTL Graph、StateMachine、Timeline与Blackboard authoring。它 MUST不创建或修改CharacterAnimationPresentationProfile、CharacterPresentationPoseGraphAsset、Blend Library、Rig Definition、Pose Slot、Bone Mask、Pose Parameter、producer resource binding或Presentation transition。若未来需要Agent编辑Pose Graph，必须由独立capability定义唯一Pose Graph authoring service、Patch schema、lowerer、handler与validator。

#### Scenario: Patch请求配置Pose Slot

- **WHEN** Agent Patch包含`configure_pose_slot`、`configure_animation_layer`或Pose Graph payload
- **THEN** schema/compiler MUST作为未知操作拒绝
- **AND** MUST不转换成Timeline channel、默认mask或Blend Library字段

#### Scenario: Patch修改AnimationTrack channel

- **WHEN** 已安装的Timeline Patch命令显式修改AnimationChannelId
- **THEN** Agent MAY通过Timeline正式authoring API修改该字段
- **AND** MUST不同时修改PoseSlot binding来掩盖Projection错误
