## RENAMED Requirements

- FROM: `### Requirement: Rig、Mask和Pose运输必须使用Rig v3`
- TO: `### Requirement: Rig、Mask和Pose运输必须使用Rig v4`
- FROM: `### Requirement: TwoBoneIK与LegIK必须使用明确且不同的目标合同`
- TO: `### Requirement: Goal Sources与FullBodyIK必须使用统一typed目标合同`

## MODIFIED Requirements

### Requirement: Pose Graph必须唯一表达完整表现拓扑

`CharacterAnimationPresentationProfile`引用的Pose Graph MUST唯一表达`ProgramParameterInput -> PoseStateMachine -> state-local Player -> AnimationSlot -> Local Pose composition -> LocalToComponentPose -> Component Pose扇出到FootGrounding与其它Goal Sources -> （可选PredictiveFootPlacementModifier） -> 全部最终Goal value汇聚到唯一FullBodyIK -> ComponentToLocalPose -> OutputPose`。图 MAY包含`SelectedPosePlayer`、`BlendStack`、`Inertialization`、`BlendPose`、`LayeredBoneBlend`、`AdditivePose`、`PoseParameterResolve`、`ModifyBone`、`PoseSubgraph`、`BlendSpacePlayer`、`SequencePlayer`、`ActionPlaybackInput`、`GraphInput`、`GraphOutput`、`PoseBoneIKGoals`、`FootGrounding`、`PredictiveFootPlacementModifier`、`FullBodyIK`与两个显式空间转换节点。`FootGrounding`、`PredictiveFootPlacementModifier`和`PoseBoneIKGoals` MUST是只读Goal producer或Goal modifier，不得位于Pose backbone或被标为IK solver；只有FullBodyIK MAY写biped Pose。Runtime MUST不在图外补建基础动画、Player、StateMachine、Slot、Blend、IK、Foot Placement、空间转换或第二Output路径；Pose Graph MUST不保存旧`TwoBoneIK`、旧`FootPlacement`、`LegIK`、AnimationSelectionInput、MotionMatchingSelectionInput或MarkerSync节点。

#### Scenario: 检查Corin正式表现链

- **WHEN** 作者打开Corin Pose Graph
- **THEN** 图 MUST能沿typed edge追踪PoseState基础Pose、Action Slot、Local/Component转换、`FootGrounding -> FullBodyIK`、Hand Goals、FullBodyIK结果和最终输出
- **AND** Corin MUST只把FootGrounding Baseline Goal Set连接到FullBodyIK，PredictiveFootPlacementModifier能力保持未接线
- **AND** MUST不显示图外Foot Placement Pass、TwoBoneIK、LegIK或隐藏FinalIK组件

### Requirement: Pose端口必须显式区分空间并允许typed控制目标

Pose Graph MUST使用`pose.local`与`pose.component`两种稳定Pose端口类型，并使用`component.full-body-ik-goals`表达同帧Component空间Goal Set。Sequence、Blend、StateMachine、Slot、Inertialization、Layered、Additive与Root Orientation操作 MUST在Local Pose工作；ModifyBone与FullBodyIK MUST位于Component Pose backbone，PoseBoneIKGoals、FootGrounding与PredictiveFootPlacementModifier MUST通过typed Component Pose/Goal edge形成Goal value分支。FootGrounding MUST在单个节点内按`Lyra Current Grounding -> Stance Stabilization -> Pelvis Resolve`形成Baseline Goals；PredictiveFootPlacementModifier MUST消费Baseline Goals并只改写未被anchor拥有的Swing脚后输出Final Goals；两者都 MUST只输出typed Goals而不输出Pose；FullBodyIK MUST同时消费原始Component Pose和最终Goal Sets。Local与Component Pose只能通过显式转换节点转换；Goals不得通过Pose转换、隐式cast或Skeleton可写IK骨伪装。OutputPose MUST只接收Local Pose。

#### Scenario: 作者只连接FootGrounding Goals

- **WHEN** FootGrounding Baseline Goals存在但没有连接到到达Output路径的FullBodyIK
- **THEN** Canvas连接诊断与Validator MUST显示未完成的Foot Grounding链
- **AND** Compiler MUST拒绝忽略Goals或生成隐藏solver

#### Scenario: FootGrounding与Hand Goals连接FullBodyIK

- **WHEN** 作者把FootGrounding与PoseBoneIKGoals分别连接到FullBodyIK动态Goals端口
- **THEN** Compiler MUST保留两个typed value依赖并生成唯一FullBodyIK stage
- **AND** MUST不插入额外Local/Component转换或隐式Goal Merge节点

#### Scenario: PredictiveFootPlacementModifier改写Foot Goals

- **WHEN** 作者把FootGrounding连接到PredictiveFootPlacementModifier，再把Modifier输出与PoseBoneIKGoals连接到FullBodyIK
- **THEN** Compiler MUST保留Baseline到Modifier的typed Goal dependency并生成唯一FullBodyIK stage
- **AND** FullBodyIK MUST只消费Modifier的最终Foot slots
- **AND** MUST不把Baseline和Modifier输出作为两个同slot Goal Set直接合并

#### Scenario: 作者把Sequence直接连接FullBodyIK

- **WHEN** Local Pose Sequence输出连接Component Pose FullBodyIK输入
- **THEN** Graph Canvas与Validator MUST拒绝该edge
- **AND** MUST要求作者显式插入LocalToComponentPose

#### Scenario: 多个骨骼控制共享一次空间转换

- **WHEN** 作者在LocalToComponentPose与ComponentToLocalPose之间连接ModifyBone、Goal Sources和FullBodyIK
- **THEN** Compiler MUST保留一个连续Component Pose段
- **AND** MUST不为每个控制节点隐藏插入额外转换

### Requirement: Pose Plan必须按拓扑编译为有序执行阶段

Projection Compiler MUST按typed依赖、Pose空间与execution domain将同一Pose/Value DAG编译为有序`FactAndDemand`、`SourceCapture`、`PureValue`、`WorldAwareValue`、`PurePose`、`WorldAwarePose`与`FinalPublication`stage。FootGrounding MUST从原始Component Pose分支生成Lyra Foot Plant等价的WorldAwareValue Baseline Goal stage；如果图中存在PredictiveFootPlacementModifier，它 MUST消费FootGrounding Goal并生成只改写Swing脚的WorldAwareValue Final Goal stage；PoseBoneIKGoals MUST从同一Component Pose分支生成PureValue stage；FullBodyIK MUST在最终Foot Goals与Hand Goals completion都可用后生成唯一PurePose solver stage。Goal stage在generated stage table中的先后只表示typed value依赖调度，MUST不表示多个IK串行，也 MUST不把任一Goal stage的骨骼结果传给另一Goal stage。Value stage MUST不持有Pose输出page或Pose write set。stage table MUST只属于generated plan，不得写入authoring Graph。每个source每帧 MUST最多capture一次，PlayableGraph MUST最多Evaluate一次，Physical Transform MUST只由final writer写一次。

#### Scenario: FootGrounding与Modifier后执行FullBodyIK与ModifyBone

- **WHEN** 合法Component Pose图把FootGrounding、可选PredictiveFootPlacementModifier与Hand Goals连接到FullBodyIK并在其后连接ModifyBone
- **THEN** Compiler MUST生成FootGrounding WorldAwareValue、可选Modifier WorldAwareValue、PoseBoneIKGoals PureValue、FullBodyIK PurePose和后续ModifyBone stage
- **AND** 后续节点 MUST消费真实FullBodyIK solved Pose

#### Scenario: FullBodyIK Goals失效

- **WHEN** Goal Set Frame、Completion或Rig identity与FullBodyIK Pose输入不匹配
- **THEN** executor MUST阻止FullBodyIK、后续stage和FinalPublication
- **AND** 已跨过Animancer Evaluate Barrier的Animation Presentation Runtime MUST进入Faulted

#### Scenario: World-aware阶段失败

- **WHEN** world context在当前Presentation transaction失效
- **THEN** executor MUST阻止后续stage和FinalPublication
- **AND** 已跨过Animancer Evaluate Barrier的Animation Presentation Runtime MUST进入Faulted
- **AND** MUST不逆序恢复已跨Barrier的状态或Physical Bone快照

### Requirement: Rig、Mask和Pose运输必须使用Rig v4

Rig v4 MUST以Physical Bones与Virtual Bones组成唯一Pose catalog，并显式声明Solver Root、Pelvis、ordered Spine、左右Arm chain、左右`Hip -> Knee -> Ankle -> Toe`Leg chain及可选Head/Clavicle。所有source capture、Pose workspace、空间转换、Mask、Blend Profile、composition和IK MUST按PoseBoneCount运输；Animator binding与final writer MUST只读写PhysicalBoneCount。Virtual Bone MUST由已采样Physical Pose按编译依赖顺序派生，不得绑定Transform或直接写Animator。Bone Mask、Additive、ModifyBone、PoseBoneIKGoals、FootGrounding、PredictiveFootPlacementModifier和FullBodyIK引用 MUST匹配同一RigId与revision；未知BoneId、跨Rig引用、重复语义、非法biped chain、退化reference bend plane、重复写冲突或非法Virtual依赖 MUST使Build失败。

#### Scenario: source capture包含Virtual Bone

- **WHEN** Physical source Pose采样完成
- **THEN** capture阶段 MUST派生全部Virtual Bone并形成完整PoseBoneCount
- **AND** source backend MUST不查找Virtual Transform

#### Scenario: Mask遗漏Virtual Bone

- **WHEN** dense Mask或Blend Profile未覆盖全部Pose slot
- **THEN** Projection Build MUST失败
- **AND** Runtime MUST不补默认权重

#### Scenario: FullBodyIK biped mapping来自旧Prefab组件

- **WHEN** Runtime Prefab仍保存第二份FinalIK references或旧Foot Placement腿骨引用
- **THEN** Definition validation MUST失败
- **AND** MUST不从该组件、Humanoid Avatar或Transform名称迁回Rig v4

### Requirement: Goal Sources与FullBodyIK必须使用统一typed目标合同

`PoseBoneIKGoals` MUST接收只读Component Pose，显式引用Pose catalog中的target reference、Effector Slot、local offset及position/rotation weight，并只输出`component.full-body-ik-goals`。`FootGrounding` MUST接收同一只读Component Pose、节点总weight、Body Grounded只读诊断、Foot Analysis stance证据与当前world context，通过每脚一次Sphere Trace、Lyra offset/normal/pelvis smoothing、唯一contact/anchor稳定和pelvis reach安全输出包含竖直pelvis pre-solve translation与Foot effectors的Baseline Goals；项目 MUST不伪造`UseFootPlacement`、`DisableLegIK`或`GroundDistance` gate。`PredictiveFootPlacementModifier` MUST接收FootGrounding Goals、下一落地Foot Feature与预测world context，只输出同类型的最终Foot Goals，并原样传递stance/anchored Foot、非Swing Foot与Pelvis slots。`FullBodyIK` MUST接收原始Component Pose与最终Goal Sets，从Rig v4建立唯一FinalIK FBBIK binding并只修改其Physical biped范围。Virtual Bone对Goal producers与Modifier只读，并在Physical写入后按依赖重算。系统 MUST不保留TwoBoneIK joint target ABI、LegIK bend plane ABI、`component.biped-leg-targets`、FinalIK Grounding或第二current-grounding result ABI。

#### Scenario: 手臂IK使用Virtual effector

- **WHEN** PoseBoneIKGoals的LeftHand目标引用Virtual Bone
- **THEN** Goal Source MUST读取其派生Component Pose并发布LeftHand Goal
- **AND** 只有后续FullBodyIK MAY写肩肘腕Physical chain

#### Scenario: FullBodyIK消费最终Foot与Hand Goals

- **WHEN** FullBodyIK收到同Frame、同Rig的FootGrounding最终Goals与Hand Goals
- **THEN** Solver MUST在一次FBBIK solve中处理Body、双脚与双手
- **AND** MUST不query world、重新计算pelvis或执行第二solver

#### Scenario: Modifier输入与输出保持Goal lineage

- **WHEN** PredictiveFootPlacementModifier收到同Frame、同Rig的FootGrounding Goals
- **THEN** Modifier MUST发布同Rig、同Foot slot lineage的最终Goals
- **AND** Compiler MUST拒绝绕过Modifier把基础与最终Foot Goals同时送给FullBodyIK

#### Scenario: Preview缺少world context

- **WHEN** Pose Plan到达FootGrounding或PredictiveFootPlacementModifier但Preview没有合法world context
- **THEN** world-aware阶段 MUST报告Unavailable
- **AND** MUST不伪造Foot Goals、FullBodyIK结果或FinalAnimationPoseFrame

### Requirement: Pose Watch必须只观察已完成Pose与typed目标Value

Editor MUST允许按稳定PoseNodeId与call-site订阅Pose Watch，并允许FootGrounding、PredictiveFootPlacementModifier与PoseBoneIKGoals使用只读Target Watch。Watch selection、颜色、显隐和面板状态 MUST只属于editor view-state。Preview或Runtime diagnostics MUST从同一帧已完成workspace复制固定容量的目标Pose、Pose空间、Goal Sets与contribution，不得重新执行节点、第二次采样source、修改Player/transition/history或改变FinalAnimationPoseFrame。Goal producer或Modifier MUST在各自Goal value完成后发布completion；FullBodyIK MUST在一次全身求解完成后发布可观察Pose。

#### Scenario: 同时观察Predictor、Hand Goals和FullBodyIK

- **WHEN** 三个节点都启用Watch
- **THEN** Target Watch MUST显示Current Foot Goals、Modifier最终Foot Goals与Hand Goals，Pose Watch MUST显示FullBodyIK最终Component Pose
- **AND** 三者 MUST共享同一Frame、Rig与Goal lineage

#### Scenario: 同时观察State Player和FullBodyIK

- **WHEN** 两个节点都启用Pose Watch
- **THEN** diagnostics MUST从同一frame lineage发布Local State Player Pose与FullBodyIK solved Pose
- **AND** MUST不额外Evaluate PlayableGraph或读取Transform反推结果

### Requirement: Preview、Runtime与Live Debug必须复用同一固定Pose Plan

Projection Compiler MUST把Pose Graph降低为固定Fact/parameter、source demand/capture、空间化PurePose、world-aware Goal生成与final publication stage table。每帧每个source、Player、transition、Slot、composition、转换、Lyra Foot Plant Goal Source、可选Modifier、FullBodyIK和writer MUST只执行一次正式计划，所有source合计只进行一次正式PlayableGraph Evaluate。Action Timeline Preview、Pose Graph Fact Preview、MM Query Fixture、Live Debug与正式Runtime MUST使用同一Projection revision、Routing Plan、stage table、source backend、world-query backend、FinalIK Pose Buffer backend和completion语义；精确Host world context完整时Preview MUST执行FootGrounding与图中显式存在的PredictiveFootPlacementModifier真实stage，不完整时 MUST报告typed Unavailable。Graph、Rig、Calibration、Foot Placement Profile或FullBodyIK Profile mutation使Projection Stale时Preview MUST停止并等待显式Build。

#### Scenario: Graph修改后继续Preview

- **WHEN** 作者修改Goal binding、Rig v4、FullBodyIK Profile或Goal拓扑使Projection变为Stale
- **THEN** Preview MUST停止执行旧FullBodyIK plan并显示Stale
- **AND** MUST不从旧Projection、Prefab FinalIK组件或旧LegIK结果继续预览
