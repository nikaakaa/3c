## MODIFIED Requirements

### Requirement: Pose Graph必须唯一表达完整表现拓扑

`CharacterAnimationPresentationProfile`引用的Pose Graph MUST唯一表达`ProgramParameterInput -> PoseStateMachine -> state-local Player -> AnimationSlot -> Local Pose composition -> LocalToComponentPose -> Component Pose controls -> FootPlacement -> LegIK -> ComponentToLocalPose -> OutputPose`。图 MAY包含`SelectedPosePlayer`、`BlendStack`、`Inertialization`、`BlendPose`、`LayeredBoneBlend`、`AdditivePose`、`PoseParameterResolve`、`ModifyBone`、`PoseSubgraph`、`BlendSpacePlayer`、`SequencePlayer`、`ActionPlaybackInput`、`GraphInput`、`GraphOutput`、`TwoBoneIK`、`FootPlacement`、`LegIK`与两个显式空间转换节点。Runtime MUST不在图外补建基础动画、Player、StateMachine、Slot、Blend、IK、FootPlacement、LegIK、空间转换或第二Output路径。

#### Scenario: 检查Corin正式表现链

- **WHEN** 作者打开Corin Pose Graph
- **THEN** 图 MUST能沿typed edge追踪PoseState基础Pose、Action Slot、Local/Component转换、FootPlacement目标、LegIK结果和最终输出
- **AND** MUST不显示图外FootPlacement Pass、隐藏LegIK或旧复合solver

### Requirement: Pose端口必须显式区分空间并允许typed控制目标

Pose Graph MUST使用`pose.local`与`pose.component`两种稳定Pose端口类型，并使用`component.biped-leg-targets`表达同帧Component空间双腿目标。Sequence、Blend、StateMachine、Slot、Inertialization、Layered、Additive与Root Orientation操作 MUST在Local Pose工作；ModifyBone、TwoBoneIK、FootPlacement与LegIK MUST位于Component Pose段。FootPlacement MUST输出Component Pose与typed targets；LegIK MUST同时消费二者。Local与Component Pose只能通过显式转换节点转换；targets不得通过Pose转换、隐式cast或Skeleton可写IK骨伪装。

#### Scenario: 作者只连接FootPlacement Pose输出

- **WHEN** FootPlacement Component Pose继续到Output路径但targets没有连接LegIK
- **THEN** Canvas连接诊断与Validator MUST显示未完成的Foot Placement链
- **AND** Compiler MUST拒绝生成隐藏LegIK

#### Scenario: FootPlacement连接LegIK

- **WHEN** 作者把同一FootPlacement的Component Pose与targets连接到LegIK
- **THEN** Compiler MUST保留连续Component Pose段并生成world-aware到pure pose依赖
- **AND** MUST不插入额外Local/Component转换

### Requirement: Pose Plan必须按拓扑编译为有序执行阶段

Projection Compiler MUST按typed依赖、Pose空间与execution domain将同一Pose DAG编译为有序`FactAndDemand`、`SourceCapture`、`PurePose`、`WorldAwarePose`与`FinalPublication`stage。FootPlacement MUST生成WorldAwarePose stage并发布Component Pose与targets completion；其后LegIK MUST生成PurePose stage并同时消费两项输出。一个world-aware stage完成后 MUST允许后续PurePose或另一个world-aware stage继续消费其输出。stage table MUST只属于generated plan，不得写入authoring Graph。每个source每帧 MUST最多capture一次，PlayableGraph MUST最多Evaluate一次，Physical Transform MUST只由final writer写一次。

#### Scenario: FootPlacement后执行LegIK与ModifyBone

- **WHEN** 合法Component Pose图把LegIK和ModifyBone依次连接在FootPlacement之后
- **THEN** Compiler MUST生成FootPlacement world-aware、LegIK pure pose和后续ModifyBone stage
- **AND** 后续节点 MUST消费真实已求解Pose而不是FootPlacement输入副本

#### Scenario: LegIK targets失效

- **WHEN** targets Frame、Completion或Rig identity与LegIK Pose输入不匹配
- **THEN** executor MUST阻止LegIK、后续stage和FinalPublication
- **AND** 已跨过Animancer Evaluate Barrier的Animation Presentation Runtime MUST进入Faulted

### Requirement: TwoBoneIK与LegIK必须使用明确且不同的目标合同

`TwoBoneIK` MUST接收并输出Component Pose，显式引用Physical end bone、Pose catalog中的effector reference与joint target reference、local offset和end rotation policy。Compiler MUST从Rig v3解析唯一Physical chain并验证reference依赖；Runtime MUST只修改该Physical chain。`LegIK` MUST接收FootPlacement Component Pose与`component.biped-leg-targets`，从Rig v3解析左右Physical腿链，并把BendPlaneNormal转换为KneeDirection后执行保持骨长的双腿求解。TwoBoneIK的joint target direction与LegIK的bend plane normal MUST使用不同字段、ABI与diagnostic名称，不得共用含糊`BendDirection`。Virtual Bone对两者只读，并在Physical写入后按依赖重算。

#### Scenario: 手臂IK使用Virtual effector

- **WHEN** TwoBoneIK的effector引用Virtual Bone
- **THEN** Solver MUST读取其派生Component Pose作为目标
- **AND** MUST只写肩肘腕Physical chain且不读取FootPlacement targets

#### Scenario: LegIK消费FootPlacement目标

- **WHEN** LegIK收到同call-site合法Component Pose与双腿targets
- **THEN** Solver MUST只写Rig v3左右Hip、Knee、Ankle链及其依赖
- **AND** MUST不query world、重新应用pelvis或读取第二Weight

### Requirement: Pose Watch必须只观察已完成Pose与typed目标Value

Editor MUST允许按稳定PoseNodeId与call-site订阅Pose Watch，并允许FootPlacement targets使用只读Target Watch。Watch selection、颜色、显隐和面板状态 MUST只属于editor view-state。Preview或Runtime diagnostics MUST从同一帧已完成workspace复制固定容量的目标Pose、Pose空间、targets与contribution，不得重新执行节点、第二次采样source、修改Player/transition/history或改变FinalAnimationPoseFrame。FootPlacement MUST在pelvis Pose与targets同时完成后发布completion；LegIK MUST在双腿求解完成后发布可观察Pose。

#### Scenario: 同时观察FootPlacement和LegIK

- **WHEN** FootPlacement与LegIK都启用Watch
- **THEN** FootPlacement Watch MUST显示应用pelvis后的Pose与左右目标，LegIK Watch MUST显示最终双腿Pose
- **AND** 两者 MUST共享同一Frame与FootPlacement Completion lineage

