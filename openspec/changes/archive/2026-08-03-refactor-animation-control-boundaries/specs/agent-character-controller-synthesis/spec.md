# agent-character-controller-synthesis Specification

## MODIFIED Requirements

### Requirement: 正式资产必须仍由人类可微调

系统 MUST保持Agent生成后的Gameplay结果为普通BTSMTL Graph、有限Action Timeline与ActionProfile，动画表现结果为CharacterPipelineDefinition引用的CharacterAnimationPresentationProfile与Pose Graph。作者 MUST能在Graph Editor调整Gameplay逻辑，在Timeline Editor调整有限Action clip/time/window/motion，在共享Profile与Pose Graph Workspace调整PoseStateMachine、Pose source、Slot与Policy。Document v3 editable Presentation MUST通过共享Capability与唯一Presentation Mutation修改同一正式owner；Rig资源正文与generated产品仍只读，系统 MUST不形成第二个Presentation写入口。

#### Scenario: 作者微调生成结果

- **WHEN** Agent生成Attack State与Action Timeline
- **THEN** 作者 MUST在Graph Editor调整准入和打断
- **AND** 在Timeline Editor调整Action时序
- **AND** 在Pose Graph Workspace调整Locomotion表现
- **AND** 三个入口 MUST不双写同一字段

### Requirement: Agent Snapshot 与 Validator 必须递归理解嵌套 StateMachine

Agent Snapshot MUST递归输出Gameplay RootTree、Runnable、inline/shared Graph、BTSMTL nested StateMachine、logical transition、Action activation、有限Action Timeline与稳定producer identity。Presentation editable section MUST输出Pose Graph、PoseStateMachine/State/Transition、Pose source binding、AnimationSlot、node-local Policy与Action channel binding，context只读输出Rig identity与generated产品。Validator MUST区分Gameplay StateMachine与PoseStateMachine，MUST不把持续Pose source伪装为Timeline producer，也不得接受旧Patch写入路径。

#### Scenario: Corin Snapshot

- **WHEN** 导出迁移后的Corin compact Snapshot
- **THEN** Gameplay section MUST显示None/Attack/Dodge及其Action Timeline
- **AND** Presentation editable section MUST显示Locomotion PoseStateMachine、Pose source、FullBodyAction Slot与Policy，context MUST只读显示Rig identity
- **AND** Graph Node/Edge MUST不输出Pose transition字段

### Requirement: Agent Document v3 CharacterController 必须通过正式类型化Mutation配置 Animation Channel

CharacterController Document v3 MUST只对有限Action Timeline与AnimationTrack输出`AnimationChannelId`。Reconciler MUST通过正式typed Timeline Mutation修改这些track channel，MUST不为持续Locomotion Pose source创建AnimationChannel或BaseLocomotion producer。Pose source、PoseStateMachine与Slot MUST在Presentation editable section通过共享Capability与唯一Presentation Mutation处理，不得复制schema或建立动画专用事务。

#### Scenario: 配置Attack Animation Channel

- **WHEN** Document目标状态按Timeline/Track identity配置Attack1 channel
- **THEN** handler MUST只修改Attack1 AnimationTrack
- **AND** MUST不修改FullBodyAction Slot topology

#### Scenario: 尝试配置Run Timeline Channel

- **WHEN** Document目标状态尝试为已迁移的Run Pose source创建BaseLocomotion channel
- **THEN** Validator MUST拒绝该操作
- **AND** MUST定位Run source的Presentation owner
