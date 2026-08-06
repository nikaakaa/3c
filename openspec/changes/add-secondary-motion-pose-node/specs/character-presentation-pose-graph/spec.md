# character-presentation-pose-graph Delta

## MODIFIED Requirements

### Requirement: Pose Graph必须唯一表达完整表现拓扑

`CharacterAnimationPresentationProfile`引用的Pose Graph MUST唯一表达`ProgramParameterInput -> PoseStateMachine -> state-local Player -> AnimationSlot -> Local Pose composition -> LocalToComponentPose -> Component Pose Goal Sources与控制 -> 唯一FullBodyIK -> ComponentToLocalPose -> 可选SecondaryMotion -> OutputPose`。图 MAY包含`SelectedPosePlayer`、`BlendStack`、`Inertialization`、`BlendPose`、`LayeredBoneBlend`、`AdditivePose`、`PoseParameterResolve`、`ModifyBone`、`PoseSubgraph`、`BlendSpacePlayer`、`SequencePlayer`、`ActionPlaybackInput`、`GraphInput`、`GraphOutput`、`PredictiveFootPlacement`、`PoseBoneIKGoals`、`FullBodyIK`、`SecondaryMotion`与两个显式空间转换节点。SecondaryMotion MUST只允许位于root graph最终ComponentToLocalPose与OutputPose之间，且每个root最多一个。Runtime MUST不在图外补建基础动画、Player、StateMachine、Slot、Blend、IK、FootPlacement、Secondary Motion、空间转换或第二Output路径；Pose Graph MUST不保存旧AnimationSelectionInput、MotionMatchingSelectionInput、MarkerSync、TwoBoneIK或LegIK节点。

#### Scenario: 检查Corin正式表现链

- **WHEN** 作者打开Corin Pose Graph
- **THEN** 图 MUST能沿typed edge追踪PoseState基础Pose、Action Slot、Local/Component转换、Goal Sources、FullBodyIK结果、SecondaryMotion和最终输出
- **AND** MUST不显示BaseLocomotion Gameplay AnimationChannel、图外Foot Placement Pass、图外Magica组件或隐藏IK

#### Scenario: root graph重复Secondary Motion

- **WHEN** 作者在同一root Pose Graph创建第二个SecondaryMotion节点
- **THEN** Canvas与Validator MUST拒绝该节点
- **AND** MUST不把两个节点合并或按显示顺序执行两次Magica

### Requirement: Pose端口必须显式区分空间并允许typed控制目标

Pose Graph MUST使用`pose.local`与`pose.component`两种稳定Pose端口类型，并使用`component.full-body-ik-goals`表达同帧Component空间目标。Sequence、Blend、StateMachine、Slot、Inertialization、Layered、Additive、Root Orientation和SecondaryMotion MUST使用Local Pose端口；ModifyBone、Goal Source与FullBodyIK MUST位于Component Pose段。Goal Source MUST读取Component Pose并输出typed Goals；FullBodyIK MUST消费Component Pose与全部Goals并输出Component Pose。Local与Component Pose只能通过显式转换节点转换；Goals不得通过Pose转换、隐式cast或Skeleton可写IK骨伪装。SecondaryMotion MUST接收最终ComponentToLocalPose的Local Pose并直接输出到OutputPose；OutputPose MUST只接收Local Pose。

#### Scenario: 作者把Secondary Motion连接在FullBodyIK前

- **WHEN** SecondaryMotion输出连接到LocalToComponentPose或任一Component Pose控制前
- **THEN** Graph Canvas与Validator MUST拒绝该拓扑
- **AND** MUST要求节点位于最终ComponentToLocalPose之后

#### Scenario: 多个骨骼控制共享一次空间转换

- **WHEN** 作者在LocalToComponentPose与ComponentToLocalPose之间连接Goal Source、FullBodyIK和ModifyBone
- **THEN** Compiler MUST保留一个连续Component Pose段
- **AND** SecondaryMotion MUST只在转回Local Pose后执行

### Requirement: Pose Plan必须按拓扑编译为有序执行阶段

Projection Compiler MUST按typed依赖、Pose空间与execution domain将同一Pose DAG编译为有序`FactAndDemand`、`SourceCapture`、`PureValue`、`WorldAwareValue`、`PurePose`、`ExternalPhysicalPose`与`FinalPublication`stage。Goal Source与FullBodyIK MUST按value/Pose依赖完成，SecondaryMotion MUST生成唯一ExternalPhysicalPose stage，并显式依赖pre-secondary Base Local Pose、Profile、Rig、team与global batch completion。ExternalPhysicalPose stage MUST由Physical Publication Coordinator在全部Actor Base Physical Pose应用后通过一次global manual simulation完成，再把post-secondary完整Rig capture发布为节点Local Pose输出。stage table MUST只属于generated plan，不得写入authoring Graph。每个source每帧 MUST最多capture一次，PlayableGraph MUST最多Evaluate一次，Physical Transform MUST只允许由同一编译Physical Publication Coordinator中的Base Pose Applicator与声明controlled bone的Secondary Motion backend写入。

#### Scenario: Secondary Motion之前的FullBodyIK失败

- **WHEN** FullBodyIK completion、Rig identity或输出Pose失效
- **THEN** executor MUST阻止Base Physical应用、SecondaryMotion与FinalPublication
- **AND** MUST不登记该Actor的Magica team

#### Scenario: Secondary Motion completion失效

- **WHEN** Magica team completion与节点Frame、Profile、Rig或Projection不匹配
- **THEN** executor MUST阻止OutputPose与FinalPublication
- **AND** 已跨过Animancer Evaluate Barrier的Actor Animation Runtime MUST进入Faulted

#### Scenario: 无Secondary Motion的角色参与同一batch

- **WHEN** 角色Pose Graph没有SecondaryMotion节点
- **THEN** 其Pose Plan MUST仍经过统一Prepare与Finalize阶段
- **AND** Physical Publication Coordinator MUST不为该角色登记Magica team或创建第二调度路径
