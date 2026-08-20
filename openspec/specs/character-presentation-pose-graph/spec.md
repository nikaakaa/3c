# character-presentation-pose-graph Specification

## Purpose

定义Character Presentation Pose Graph的正式数据模型、编译边界、作者工作区、Preview、Live Debug和Pose Watch。
## Requirements
### Requirement: Pose Graph必须唯一表达完整表现拓扑

`CharacterAnimationPresentationProfile`引用的Pose Graph MUST唯一表达`ProgramParameterInput -> PoseStateMachine -> state-local Player -> AnimationSlot -> Local Pose composition -> LocalToComponentPose -> Component Pose controls -> Goal Sources -> FullBodyIK -> ComponentToLocalPose -> OutputPose`。FootPlacement与PoseBoneIKGoals MUST从同一Component Pose扇出typed Goal Set，唯一FullBodyIK MUST消费原始Component Pose与全部不重叠Goal Set。Runtime MUST不在图外补建Player、StateMachine、Slot、Blend、IK、FootPlacement、空间转换或第二Output路径。

#### Scenario: 检查Corin正式表现链

- **WHEN** 作者打开Corin Pose Graph
- **THEN** 图 MUST能沿typed edge追踪PoseState基础Pose、Action Slot、Local/Component转换、FootPlacement Goals、唯一FullBodyIK和最终输出
- **AND** MUST不显示图外Foot Placement、LegIK、TwoBoneIK或第二FullBodyIK

### Requirement: Pose端口必须显式区分空间并允许typed控制目标

Pose Graph MUST使用`pose.local`、`pose.component`与`component.full-body-ik-goals`三种稳定端口类型。FootPlacement与PoseBoneIKGoals只读Component Pose并输出Goal Set；FullBodyIK接收一个Component Pose和稳定动态Goal输入集合，并输出Component Pose。Local与Component Pose只能通过显式转换节点转换；Goal Set不得通过Pose转换、隐式cast或Skeleton可写IK骨伪装。OutputPose MUST只接收Local Pose。

#### Scenario: Foot Placement未连接FullBodyIK

- **WHEN** FootPlacement Goal Set没有连接唯一FullBodyIK
- **THEN** Canvas连接诊断、Validator与Compiler MUST拒绝该图
- **AND** Runtime MUST不隐藏补建IK

#### Scenario: Foot与Hand Goals连接

- **WHEN** FootPlacement与PoseBoneIKGoals读取同一Component Pose并连接FullBodyIK
- **THEN** Compiler MUST保留两个typed Goal Set并汇聚到唯一FullBodyIK
- **AND** MUST拒绝重复effector slot

#### Scenario: 作者把Sequence直接连接FootPlacement

- **WHEN** Local Pose Sequence输出连接Component Pose FootPlacement输入
- **THEN** Graph Canvas与Validator MUST拒绝该edge
- **AND** MUST要求作者显式插入LocalToComponentPose

#### Scenario: Component控制与Goal链共享一次空间转换

- **WHEN** 作者在LocalToComponentPose与ComponentToLocalPose之间连接ModifyBone、Goal Sources和FullBodyIK
- **THEN** Compiler MUST保留一个连续Component Pose段
- **AND** MUST不为每个控制节点隐藏插入额外转换

### Requirement: Pose Plan必须按拓扑编译为有序执行阶段

Projection Compiler MUST按typed依赖、Pose空间与execution domain将同一Pose DAG编译为有序`FactAndDemand`、`SourceCapture`、`PurePose`、`WorldAwareValue`、`PureValue`与`FinalPublication`stage。FootPlacement完成Goal后才能调度唯一FullBodyIK；Value stage不得持有Pose输出或write set。stage table MUST只属于generated plan，不得写入authoring Graph。每个source每帧 MUST最多capture一次，PlayableGraph MUST最多Evaluate一次，Physical Transform MUST只由final writer写一次。

#### Scenario: Foot Placement后执行FullBodyIK

- **WHEN** FootPlacement与其它Goal Source完成同帧Goal
- **THEN** Compiler MUST在其后生成唯一FullBodyIK pure pose stage
- **AND** 后续节点 MUST消费FBBIK输出而不是输入Pose副本

#### Scenario: Goal lineage失效

- **WHEN** Goal Set的Frame、Completion或Rig identity与FullBodyIK Pose输入不匹配
- **THEN** executor MUST阻止FullBodyIK、后续stage和FinalPublication
- **AND** 已跨过Animancer Evaluate Barrier的Animation Presentation Runtime MUST进入Faulted

#### Scenario: World-aware阶段失败

- **WHEN** world context在当前Presentation transaction失效
- **THEN** executor MUST阻止后续stage和FinalPublication
- **AND** 已跨过Animancer Evaluate Barrier的Animation Presentation Runtime MUST进入Faulted
- **AND** MUST不逆序恢复已跨Barrier的状态或Physical Bone快照

### Requirement: PoseStateMachine必须是纯表现状态机

`PoseStateMachine` MUST拥有稳定Entry、State、Transition、State Alias和MaxTransitionsPerFrame。Transition Rule MUST只读取同帧`CharacterPresentationFactFrame`、TimeInState与StatePoseRemainingTime；MUST不读取Gameplay Blackboard mutable address、ActionInstance、Timeline operation、Unity Transform或World query。State Alias MUST只复用合法source State集合，不得拥有Pose或成为active runtime State。PoseStateMachine MUST只编入Presentation Projection，不得进入Gameplay Semantic IR或Numeric Program。

#### Scenario: Idle进入Locomotion

- **WHEN** typed HorizontalSpeed Fact满足Transition Rule
- **THEN** PoseStateMachine MUST按priority和stable order选择唯一target
- **AND** Gameplay MUST不发送PlayRun事件

#### Scenario: 同帧多个Transition成立

- **WHEN** 多条可达Transition Rule同时为true
- **THEN** Runtime MUST遵守compiled priority、stable order和MaxTransitionsPerFrame
- **AND** MUST不依赖容器遍历顺序

### Requirement: State inline graph必须存入root-owned flat graph catalog

State在作者语义上 MAY拥有inline Pose subgraph，但serialized State MUST只保存稳定PoseGraphId与OutputPoseNodeId。`CharacterPresentationPoseGraphAsset` MUST用root-owned flat catalog保存`PoseGraphId -> CharacterPoseGraphData`，Subgraph call也 MUST只保存GraphId。Validator与Compiler MUST检查GraphId唯一、每个可达State恰有一个Output、call依赖无递归和无悬空引用。Runtime MUST只读取编译后的flat plan，不得动态展开嵌套对象树。

#### Scenario: 打开Locomotion State图

- **WHEN** 作者进入Locomotion State
- **THEN** Editor MUST按State的PoseGraphId导航到catalog记录
- **AND** State serialized data MUST不再嵌套旧`m_InlinePoseGraph`

#### Scenario: Subgraph形成递归

- **WHEN** Graph A和Graph B通过GraphId相互调用
- **THEN** Build MUST失败并报告完整依赖链
- **AND** Runtime MUST不尝试动态递归

### Requirement: State-local source必须由Profile binding和provider解析

`ClipPlayer`、`BlendSpacePlayer`与`SelectedPosePlayer` MUST引用类型匹配的Graph-owned Source Slot对象。Projection Compiler MUST从精确Definition/Profile解析唯一Binding；Clip Binding MUST直接提供AnimationClip。ClipPlayer MUST只保存Source Slot、Play Rate、Initial Time与Clock Source，不得保存Loop或Topology副本；Finite/Cyclic MUST只从AnimationClip正式Loop设置编译。Provider MUST发布带dense source index、generation、Projection revision与frame lease的`PresentationPoseSourceSample`。Pose Graph MUST不保存Sequence、AnimationClip副本、作者source字符串或Gameplay producer。

#### Scenario: ClipPlayer首次采样Idle

- **WHEN** Idle State的ClipPlayer获得entry relevance
- **THEN** provider MUST从Profile direct Clip Binding发布Ready sample
- **AND** Player MUST不解析Sequence或AssetDatabase

#### Scenario: ClipPlayer提交Loop字段

- **WHEN** 人工Capability或Document v4为ClipPlayer提供Loop、Topology或等价override
- **THEN** typed parser或Validator MUST在Compiler前拒绝该字段
- **AND** MUST不覆盖AnimationClip正式Loop设置

### Requirement: PoseState target必须经过readiness barrier

PoseStateMachine MUST先选择候选target并向其provider提交demand。只有Ready target才可提交Transition Routing generation；已有合法source时Pending MUST保持当前source且不启动transition，Entry target Pending MUST不发布Final Pose，Invalid MUST发布typed failure并阻止该帧正式输出。系统 MUST不使用历史sample、bind pose、默认Idle、旧Timeline或Action Pose作为fallback。

#### Scenario: BlendSpace target尚未产生首样本

- **WHEN** Transition Rule选中Locomotion而BlendSpace provider返回Pending
- **THEN** 当前合法State MUST继续输出
- **AND** Transition clock MUST不开始

### Requirement: Pose State transition必须显式编译Routing并从source binding推导同步

每条Transition MUST继续显式配置Rule、Blend Logic、duration、Blend Mode、Custom Curve与Blend Profile。Compiler MUST从两侧State唯一source usage与Profile Locomotion Sync Group推导可选source-to-source Phase relation；Direct Clip与Blend Space MUST先降低为正式`AnimationSourcePhasePlan`。两侧不属于同组时生成None，同组时必须编译合法per-clip Phase、实际秒域coverage与Foot Analysis质量结果，并按clock authority与完整Blend窗口coverage写入固定leader。Transition authoring MUST不保存同步开关、素材同步策略、leader role、leader override或phase容差；Projection relation MUST保存TransitionId，Runtime再与TransitionGeneration组合生命周期身份。

#### Scenario: Turn进入RunLoop

- **WHEN** Turn与RunLoop属于同一Locomotion Sync Group且Transition条件成立
- **THEN** target effective time MUST来自compiled Phase relation
- **AND** Blend Routing MUST独立使用edge-owned Standard Blend计划

### Requirement: AnimationSlot必须是有限Action的唯一Pose插入口

`ActionPlaybackInput` MUST只读取`CharacterActionPlaybackRuntime`发布的有限Action frame。`AnimationSlot` MUST拥有Source Pose输入、Action Playback输入、稳定SlotId与AnimationChannelId以及node-local Routing Plan。无Action时Slot MUST透传同帧Source Pose；Action Ready时 MUST插入Action Pose；Action release时 MUST过渡回持续更新的`SourcePoseEndpoint`。Slot MUST不判断Action admission、不推进Timeline、不控制Locomotion PoseState、不拥有Bone Mask，也 MUST不把NoPose和SourcePoseEndpoint混为一谈。

#### Scenario: FullBodyAction为空

- **WHEN** Slot没有活动Action playback
- **THEN** 输出 MUST与当前PoseState Source Pose一致
- **AND** MUST不创建默认Idle或常驻Stored Pose

#### Scenario: Attack结束时角色已经停下

- **WHEN** Attack期间PoseStateMachine已经从Move切到Idle
- **THEN** Slot release MUST回到当前Idle Source Pose
- **AND** Gameplay MUST不指定恢复到Run或Idle

### Requirement: Pose节点必须显式处理可用性和局部连续性

每个Player和composition节点 MUST声明RequirePose或AllowEmpty。NoPose MUST是typed availability，不得用bind pose、零矩阵或上一帧缓存伪装。`SelectedPosePlayer` MUST发布source discontinuity；显式`BlendStack` MUST唯一拥有自身多source history、clock、Stored Pose和release；`Inertialization` MUST唯一拥有直接上游局部Pose的history、residual与rebase。Compiler与Runtime MUST不在OutputPose前补建全局连续化节点。

#### Scenario: 局部Action分支惯性化

- **WHEN** Action Player连接Inertialization后进入LayeredBoneBlend
- **THEN** residual MUST只影响该Action分支
- **AND** Base Pose分支 MUST不共享其history

#### Scenario: Required Pose缺失

- **WHEN** 唯一Output路径上的Required Pose为Invalid
- **THEN** Pose Plan MUST失败
- **AND** MUST不发布旧FinalAnimationPoseFrame

### Requirement: Pose参数必须通过typed页面和显式解析传播

Pose Graph MUST声明稳定ParameterId、类型、默认值与允许来源。`ProgramParameterInput` MUST只读取committed parameter page；source-local curve参数 MUST随Pose Value传播；`PoseParameterResolve` MUST按显式`Base | Overlay | Weighted | Max | Min`规则合成。节点 MUST不按字符串、GameplayTag或State显示名查找参数。

#### Scenario: Blend权重读取Program参数

- **WHEN** BlendPose权重连接ProgramParameterInput
- **THEN** Compiler MUST校验ParameterId、类型和page layout
- **AND** Runtime MUST不读取Gameplay对象

### Requirement: Rig、Mask和Pose运输必须使用Rig v4

Rig v4 MUST以Physical Bones与Virtual Bones组成唯一Pose catalog，并显式声明Solver Root、Pelvis、Spine、Arm与Leg chain。所有source capture、Pose workspace、空间转换、Mask、Blend Profile、composition和IK MUST按PoseBoneCount运输；Animator binding与final writer MUST只读写PhysicalBoneCount。Virtual Bone MUST由已采样Physical Pose按编译依赖顺序派生，不得绑定Transform或直接写Animator。未知BoneId、跨Rig引用、重复写冲突、非法FBBIK chain或非法Virtual依赖 MUST使Build失败。

#### Scenario: source capture包含Virtual Bone

- **WHEN** Physical source Pose采样完成
- **THEN** capture阶段 MUST派生全部Virtual Bone并形成完整PoseBoneCount
- **AND** source backend MUST不查找Virtual Transform

#### Scenario: Mask遗漏Virtual Bone

- **WHEN** dense Mask或Blend Profile未覆盖全部Pose slot
- **THEN** Projection Build MUST失败
- **AND** Runtime MUST不补默认权重

#### Scenario: FBBIK binding来自旧Prefab组件

- **WHEN** Runtime Prefab仍依赖FinalIK BipedReferences或第二份骨骼映射
- **THEN** Definition validation MUST失败
- **AND** MUST不从该组件或Transform名称迁回Rig v4

### Requirement: Goal Sources与FullBodyIK必须使用统一typed目标合同

FootPlacement MUST通过单次Frame事务输出Pelvis与双脚Goal；PoseBoneIKGoals MUST输出其它effector Goal。FullBodyIK MUST消费原始Component Pose与全部不重叠Goal Set，并通过项目Pose Buffer backend调用唯一FinalIK FBBIK，只修改Rig v4 Physical biped。系统 MUST不保留Predictive Modifier ABI、Grounding覆盖协议、LegIK、TwoBoneIK或第二腿目标ABI。

#### Scenario: 同帧Goal完成

- **WHEN** Foot与Hand Goal具有相同Frame、Completion和Rig
- **THEN** 唯一FBBIK MUST一次验证并求解全部有效目标
- **AND** MUST拒绝重复effector slot

#### Scenario: Foot Placement Goal权重为零

- **WHEN** FootPlacement发布三个合法零权重Goal
- **THEN** FullBodyIK MUST验证Goal lineage后跳过FBBIK Update
- **AND** 输出Pose MUST保持输入Pose不变

#### Scenario: Preview缺少world context

- **WHEN** Pose Plan到达FootPlacement但Preview没有合法world context
- **THEN** world-aware阶段 MUST报告Unavailable
- **AND** MUST不伪造FootPlacement输出或FinalAnimationPoseFrame

### Requirement: Pose Graph工作区必须准确映射Authoring、Live与References

正式窗口 MUST提供Definition-scoped Navigator、唯一`GraphAuthoringCanvasView`、Details和可折叠Bottom Dock。Details MUST分离Authoring、Live与References：Authoring只通过正式Presentation Mutation修改当前owner字段；Live只读取匹配PoseGraphId、PoseGraphRevision与ProjectionRevision的snapshot；References只读显示Source Slot、Profile binding子资产、实际资源对象、source map、Action producer、Rig、Policy和call site。稳定identity、GUID、revision、hash与compiled index MUST默认隐藏。Live Debug模式下mutation MUST禁用，revision不匹配 MUST显示Stale并清空旧值。

#### Scenario: 查看Locomotion State

- **WHEN** 作者选中Locomotion State的Clip或BlendSpace Player
- **THEN** Authoring MUST显示类型匹配的Source Slot对象选择器
- **AND** References MUST显示解析后的Profile binding、实际资源、owner与Open Source命令
- **AND** MUST不显示BaseLocomotion Gameplay producer或可编辑Source Id

#### Scenario: Runtime revision不匹配

- **WHEN** snapshot revision与当前文档或Projection不一致
- **THEN** Live MUST显示Stale
- **AND** MUST不从authoring默认值或Animancer state伪造结果

### Requirement: Pose Watch必须只观察已完成Pose与typed目标Value

Editor MUST允许按稳定PoseNodeId与call-site订阅Pose Watch，并允许Goal Set使用只读Target Watch。Watch selection、颜色、显隐和面板状态 MUST只属于editor view-state。Preview或Runtime diagnostics MUST从同一帧已完成workspace复制固定容量的Goal、Pose与contribution，不得重新执行节点、第二次采样source、修改Player/transition/history或改变FinalAnimationPoseFrame。

#### Scenario: 同时观察FootPlacement和FullBodyIK

- **WHEN** FootPlacement Goal与FullBodyIK Pose都启用Watch
- **THEN** 两者 MUST共享同一Frame、Completion和Rig lineage
- **AND** Watch MUST不重新执行world query或FBBIK

#### Scenario: 同时观察State Player和FootPlacement

- **WHEN** 两个节点都启用Pose Watch
- **THEN** diagnostics MUST从同一frame lineage发布Local State Player Pose、FootPlacement Goal与FullBodyIK输出Pose
- **AND** MUST不额外Evaluate PlayableGraph或读取Transform反推结果

### Requirement: Preview、Runtime与Live Debug必须复用同一固定Pose Plan

Projection Compiler MUST把Pose Graph降低为固定Fact/parameter、source demand/capture、空间化PurePose、world-aware value、FullBodyIK与final publication stage table。每帧每个source、Player、transition、Slot、composition、转换、Goal Source、FBBIK和writer MUST只执行一次正式计划，所有source合计只进行一次正式PlayableGraph Evaluate。Preview、Live Debug与正式Runtime MUST使用同一Projection revision、stage table、world-query backend、FinalIK Pose Buffer backend和completion语义；精确Host world context不完整时 MUST报告typed Unavailable。Graph mutation或Stale Projection时Preview MUST停止并等待显式Build。

#### Scenario: Graph修改后继续Preview

- **WHEN** 作者修改State、Slot、Rig、Pose空间或FootPlacement使Projection变为Stale
- **THEN** Preview MUST停止消费旧Plan
- **AND** MUST不创建临时Plan、默认空间转换或旧Projection fallback

### Requirement: Pose authoring必须使用共享Capability与类型化Presentation Mutation

Pose Graph、PoseStateMachine、Node、Port与Edge MUST使用共享typed domain document。

#### Scenario: 新增Pose节点能力

- **WHEN** 新Pose节点注册typed payload与compiler handler
- **THEN** 人工创建菜单、Document v4、Validator和Compiler MUST识别同一Capability
- **AND** MUST不复制Node/Port View或第二Compiler入口

### Requirement: Pose Graph UI必须保留准确术语和serialized identity

UI MUST使用Clip Player、Blend Space Player、Selected Pose Player、Animation State Machine、Slot、Layered Blend Per Bone、Inertialization、Locomotion Phase Group、Pose Watch和Output Pose等准确术语。序列化、Document、Mutation、Compiler source map和Diagnostics MUST使用同一Clip命名；MUST不保留Clip Player显示名或旧node kind alias。

#### Scenario: 作者添加单Clip播放器

- **WHEN** 作者在Pose Graph添加单AnimationClip state-local player
- **THEN** Capability、节点标题、Document kind和编译诊断 MUST统一显示Clip Player
- **AND** MUST不存在Clip Player兼容名称

### Requirement: Pose StateMachine layout必须是独立纯作者数据

每个root-owned PoseStateMachine MUST在`CharacterPresentationPoseGraphAsset`中拥有按稳定`PoseStateMachineId`索引的唯一layout owner。Layout MAY稀疏保存Entry、State与Alias的显式二维位置；缺少显式位置时 MUST按元素类型和稳定identity使用唯一确定性排布。Layout MUST拒绝重复identity、未知元素和非有限坐标，且 MUST不保存Transition edge位置。Layout变化 MUST进入typed Presentation Mutation、Undo、dirty、保存与Document同步，但 MUST不修改PoseStateMachine `ContentRevision`、不得使Presentation Projection变为Stale，也不得触发Compile或Build。Compiler与Runtime MUST不读取layout。

#### Scenario: 作者拖动Locomotion State

- **WHEN** 作者把Pose StateMachine中的Locomotion State拖到新位置
- **THEN** 系统 MUST通过Pose StateMachine layout Mutation保存该State的稳定identity与位置
- **AND** 重新打开工作区后 MUST从同一layout owner恢复位置
- **AND** Pose StateMachine运行语义与Projection revision MUST保持不变

#### Scenario: 现有State没有显式位置

- **WHEN** 现有Pose StateMachine layout没有某个State的显式位置
- **THEN** 工作区 MUST按稳定identity使用唯一确定性位置
- **AND** MUST不在打开窗口、selection变化或AssetDatabase刷新时自动保存生成位置

#### Scenario: layout引用已删除State

- **WHEN** layout包含当前Pose StateMachine中不存在的State identity
- **THEN** Validator MUST报告悬空layout元素并拒绝正式提交
- **AND** MUST不忽略该元素或按显示名重绑定
