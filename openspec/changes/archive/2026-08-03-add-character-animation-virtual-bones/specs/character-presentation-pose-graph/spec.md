## MODIFIED Requirements

### Requirement: Pose Graph工作区必须显式解释完整表现拓扑

Pose Graph MUST唯一声明Presentation Fact Input、PoseStateMachine、SequencePlayer、State内部Motion Matching provider、有限Action Playback Input、MarkerSync、SelectedPosePlayer、BlendSpacePlayer、BlendStack、AnimationSlot、Inertialization、Blend、Layered Blend Per Bone、Additive、Pose Parameter、PoseSubgraph、ModifyBone、TwoBoneIK、FootPlacement与Output topology。`TwoBoneIK` MUST消费并输出普通Pose Value，读取当前Rig中的Physical或Virtual Pose Bone reference，只写由三个Physical Bone组成的chain，并降低为FootPlacement之前的native composition operation。Compiler与Runtime MUST不在AnimationChannel、PoseState、Slot、TwoBoneIK或Output背后自动追加未显示的Player、IK、隐藏Transform target、world-aware pass或第二输出路径。

#### Scenario: 查看完整Corin表现链

- **WHEN** 作者打开最终Corin Pose Graph
- **THEN** 工作区 MUST能沿typed edge追踪`Presentation Fact -> PoseStateMachine -> state-local source -> AnimationSlot -> TwoBoneIK -> FootPlacement -> OutputPose`
- **AND** 每个Virtual Bone与TwoBoneIK MUST能导航到精确Rig、PoseNodeId和compiled operation

#### Scenario: TwoBoneIK连接在Pose链中

- **WHEN** 作者把composition后的Pose连接到TwoBoneIK并把其输出连接到FootPlacement
- **THEN** Compiler MUST把TwoBoneIK降低为显式native composition operation
- **AND** Runtime MUST不创建图外IK pass或隐藏Transform target

### Requirement: Pose节点必须使用有限typed端口与局部discontinuity边界

Pose Graph MUST使用版本化typed端口连接Presentation Fact、Action Playback、Pose Value、Pose Discontinuity、typed Program Parameter与world-aware阶段。正式运行节点 MUST限于`PresentationFactInput`、`PoseStateMachine`、`SequencePlayer`、State内部Motion Matching provider、有限Action Playback Input、`MarkerSync`、`SelectedPosePlayer`、`BlendSpacePlayer`、`BlendStack`、`AnimationSlot`、`Inertialization`、`BlendPose`、`LayeredBoneBlend`、`AdditivePose`、`PoseParameterResolve`、`PoseSubgraph`、`ModifyBone`、`TwoBoneIK`、`FootPlacement`与`OutputPose`；`GraphInput`/`GraphOutput` MUST只用于subgraph编译边界。Inertialization的输入 MUST由Compiler证明来自合法branch-local discontinuity owner；`TwoBoneIK` MUST只接受普通Pose输入和已解析Rig reference，不得消费Gameplay State、Action identity、World target或场景Transform。节点、端口、Policy和Rig identity任一不匹配 MUST使构建失败。

#### Scenario: discontinuity跨composition隐藏传播

- **WHEN** 作者试图让Player discontinuity穿过LayeredBoneBlend、AdditivePose、TwoBoneIK或PoseSubgraph后由全身Inertialization消费
- **THEN** Validator MUST拒绝该连接
- **AND** MUST要求作者把局部Inertialization放在对应分支内

#### Scenario: TwoBoneIK引用场景target

- **WHEN** TwoBoneIK配置尝试使用GameObject、Transform或未进入Rig catalog的BoneId
- **THEN** Validator MUST拒绝该输入并定位PoseNodeId
- **AND** MUST不创建默认Effector、Joint Target或fallback chain
