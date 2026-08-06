## MODIFIED Requirements

### Requirement: CharacterSimulationPresentationRuntime必须执行唯一编译Pose Plan

Runtime MUST在同一正式PlayableGraph执行compiled Presentation Fact、PoseStateMachine、Sequence/BlendSpace/MM source、有限Action Slot、Transition Routing、source capture、native composition、TwoBoneIK、world-aware FootPlacement与Output Pose。每个Animation source MUST先采样`PhysicalBoneCount`个真实骨骼，再在同一capture中派生Virtual Bone并形成`PoseBoneCount`长度的完整Pose page；Player、BlendStack、Stored Pose、Inertialization、Mask/Profile与composition MUST只运输该完整Pose，不得从组合后的Physical Target重算Virtual Bone。TwoBoneIK MUST在native composition中读取同一Pose page并只写合法Physical chain；final writer MUST只把`[0, PhysicalBoneCount)`写入`CharacterAnimationRigBinding`的Physical Transform。Runtime MUST不创建图外Virtual Bone缓存、隐藏IK target、第二IK pass、第二PlayableGraph或兼容Rig reader。

#### Scenario: State-local source进入Pose Plan

- **WHEN** Sequence、Blend Space或Motion Matching source完成一份合法Physical pose sample
- **THEN** source capture MUST在previous pose与velocity计算前派生全部Virtual Bone
- **AND** 下游节点 MUST按同一source identity、weight、continuity与lifecycle运输完整Pose page

#### Scenario: TwoBoneIK完成后进入FootPlacement

- **WHEN** 编译Pose Plan把TwoBoneIK连接在composition与FootPlacement之间
- **THEN** Runtime MUST先完成TwoBoneIK native operation再进入唯一world-aware阶段
- **AND** final writer MUST不为Virtual Bone创建Physical Transform binding或写入Animator
