## RENAMED Requirements

- FROM: `### Requirement: Rig、Mask和Pose运输必须使用Rig v2`
- TO: `### Requirement: Rig、Mask和Pose运输必须使用Rig v3`

## ADDED Requirements

### Requirement: Pose端口必须显式区分Local与Component空间

Pose Graph MUST使用`pose.local`与`pose.component`两种稳定Pose端口类型。Sequence、Blend、StateMachine、Slot、Inertialization、Layered、Additive与Root Orientation操作 MUST在Local Pose工作；ModifyBone、TwoBoneIK与FootPlacement MUST在Component Pose工作。两种空间只能通过作者图中显式`LocalToComponentPose`与`ComponentToLocalPose`转换，Compiler MUST不静默插入转换或按节点名称猜测空间。OutputPose MUST只接收Local Pose。

#### Scenario: 作者把Sequence直接连接FootPlacement

- **WHEN** Local Pose Sequence输出连接Component Pose FootPlacement输入
- **THEN** Graph Canvas与Validator MUST拒绝该edge
- **AND** MUST要求作者显式插入LocalToComponentPose

#### Scenario: 多个骨骼控制共享一次空间转换

- **WHEN** 作者在LocalToComponentPose与ComponentToLocalPose之间连接ModifyBone、TwoBoneIK和FootPlacement
- **THEN** Compiler MUST保留一个连续Component Pose段
- **AND** MUST不为每个控制节点隐藏插入额外转换

### Requirement: Pose Plan必须按拓扑编译为有序执行阶段

Projection Compiler MUST按typed依赖、Pose空间与execution domain将同一Pose DAG编译为有序`FactAndDemand`、`SourceCapture`、`PurePose`、`WorldAwarePose`与`FinalPublication`stage。一个world-aware stage完成后 MUST允许后续PurePose或另一个world-aware stage继续消费其输出。stage table MUST只属于generated plan，不得写入authoring Graph。每个source每帧 MUST最多capture一次，PlayableGraph MUST最多Evaluate一次，Physical Transform MUST只由final writer写一次。

#### Scenario: FootPlacement之后继续ModifyBone

- **WHEN** 合法Component Pose图把ModifyBone连接在FootPlacement之后
- **THEN** Compiler MUST生成world-aware stage后的后续PurePose stage
- **AND** ModifyBone MUST消费FootPlacement实际输出而不是其输入副本

#### Scenario: World-aware阶段失败

- **WHEN** world context在当前Presentation transaction失效
- **THEN** executor MUST阻止后续stage和FinalPublication
- **AND** 已跨过Animancer Evaluate Barrier的Animation Presentation Runtime MUST进入Faulted
- **AND** MUST不逆序恢复已跨Barrier的状态或Physical Bone快照

## MODIFIED Requirements

### Requirement: Pose Graph必须唯一表达完整表现拓扑

`CharacterAnimationPresentationProfile`引用的Pose Graph MUST唯一表达`ProgramParameterInput -> PoseStateMachine -> state-local Player -> AnimationSlot -> Local Pose composition -> LocalToComponentPose -> Component Pose skeletal controls -> ComponentToLocalPose -> OutputPose`。图 MAY包含`SelectedPosePlayer`、`BlendStack`、`Inertialization`、`BlendPose`、`LayeredBoneBlend`、`AdditivePose`、`PoseParameterResolve`、`ModifyBone`、`PoseSubgraph`、`BlendSpacePlayer`、`SequencePlayer`、`ActionPlaybackInput`、`GraphInput`、`GraphOutput`、`TwoBoneIK`、`FootPlacement`与两个显式空间转换节点。Runtime MUST不在图外补建基础动画、Player、StateMachine、Slot、Blend、IK、FootPlacement、空间转换或第二Output路径；Pose Graph MUST不保存旧AnimationSelectionInput、MotionMatchingSelectionInput或MarkerSync节点。

#### Scenario: 检查Corin正式表现链

- **WHEN** 作者打开Corin Pose Graph
- **THEN** 图 MUST能沿typed edge追踪PoseState基础Pose、Action Slot、Local/Component转换、骨骼控制和最终输出
- **AND** MUST不显示BaseLocomotion Gameplay AnimationChannel或图外FootPlacement Pass

### Requirement: Rig、Mask和Pose运输必须使用Rig v3

Rig v3 MUST以Physical Bones与Virtual Bones组成唯一Pose catalog，并显式声明Pelvis与左右`Hip -> Knee -> Ankle -> Toe`Physical Bone chain。所有source capture、Pose workspace、空间转换、Mask、Blend Profile、composition和IK MUST按PoseBoneCount运输；Animator binding与final writer MUST只读写PhysicalBoneCount。Virtual Bone MUST由已采样Physical Pose按编译依赖顺序派生，不得绑定Transform或直接写Animator。Bone Mask、Additive、ModifyBone、TwoBoneIK和FootPlacement引用 MUST匹配同一RigId与revision，未知BoneId、跨Rig引用、重复写冲突、非法腿链或非法Virtual依赖 MUST使Build失败。

#### Scenario: source capture包含Virtual Bone

- **WHEN** Physical source Pose采样完成
- **THEN** capture阶段 MUST派生全部Virtual Bone并形成完整PoseBoneCount
- **AND** source backend MUST不查找Virtual Transform

#### Scenario: Mask遗漏Virtual Bone

- **WHEN** dense Mask或Blend Profile未覆盖全部Pose slot
- **THEN** Projection Build MUST失败
- **AND** Runtime MUST不补默认权重

#### Scenario: FootPlacement腿链来自旧Prefab组件

- **WHEN** Runtime Prefab仍保存第二份Foot Placement hip、knee、ankle或toe引用
- **THEN** Definition validation MUST失败
- **AND** MUST不从该组件或Transform名称迁回Rig v3

### Requirement: TwoBoneIK必须使用Physical chain和显式Pose reference

`TwoBoneIK` MUST接收并输出Component Pose，显式引用Physical end bone、Pose catalog中的effector reference与joint target reference、local offset和end rotation policy。Compiler MUST从Rig v3解析唯一Physical chain并验证reference依赖；Runtime MUST只修改该Physical chain，对Virtual Bone只读，并在写入后重算受影响的descendant与Virtual依赖。`FootPlacement` MUST是独立Component Pose world-aware skeletal control，复用正式Planner与解析式Pose solver，不得由TwoBoneIK节点或图外Transform pass代替。

#### Scenario: 手臂IK使用Virtual effector

- **WHEN** TwoBoneIK的effector引用Virtual Bone
- **THEN** Solver MUST读取其派生Component Pose作为目标
- **AND** MUST只写肩肘腕Physical chain

#### Scenario: Preview缺少world context

- **WHEN** Pose Plan到达FootPlacement但Preview没有合法world context
- **THEN** world-aware stage MUST报告Unavailable
- **AND** MUST不伪造FootPlacement输出或FinalAnimationPoseFrame

### Requirement: Pose Watch必须只观察已完成Pose Value

Editor MUST允许按稳定PoseNodeId与call-site订阅Pose Watch。Watch selection、颜色、显隐和面板状态 MUST只属于editor view-state。Preview或Runtime diagnostics MUST从同一帧已完成Pose workspace复制固定容量的目标Pose、Pose空间与contribution，不得重新执行节点、第二次采样source、修改Player/transition/history或改变FinalAnimationPoseFrame。world-aware节点 MUST在Planner与solver均完成后才发布可观察输出。

#### Scenario: 同时观察State Player和FootPlacement

- **WHEN** 两个节点都启用Pose Watch
- **THEN** diagnostics MUST从同一frame completion发布Local State Player Pose与已求解Component FootPlacement Pose
- **AND** MUST不额外Evaluate PlayableGraph或读取Final IK Transform

### Requirement: Preview、Runtime与Live Debug必须复用同一固定Pose Plan

Projection Compiler MUST把Pose Graph降低为固定Fact/parameter、source demand/capture、空间化PurePose、world-aware Pose与final publication stage table。每帧每个source、Player、transition、Slot、composition、转换、IK、world-aware control和writer MUST只执行一次正式计划，所有source合计只进行一次正式PlayableGraph Evaluate。Action Timeline Preview、Pose Graph Fact Preview、MM Query Fixture、Live Debug与正式Runtime MUST使用同一Projection revision、Routing Plan、stage table、source backend和completion语义；精确Host world context完整时Preview MUST执行FootPlacement真实stage，不完整时 MUST报告typed Unavailable。Graph mutation或Stale Projection时Preview MUST停止并等待显式Build。

#### Scenario: Graph修改后继续Preview

- **WHEN** 作者修改State、Slot、Rig、Pose空间或FootPlacement使Projection变为Stale
- **THEN** Preview MUST停止消费旧Plan
- **AND** MUST不创建临时Plan、默认空间转换或旧Projection fallback
