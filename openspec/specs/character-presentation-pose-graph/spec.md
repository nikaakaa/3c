# character-presentation-pose-graph Specification

## Purpose

定义Character Presentation Pose Graph的正式数据模型、编译边界、作者工作区、Preview、Live Debug和Pose Watch。
## Requirements
### Requirement: Pose Graph必须唯一表达完整表现拓扑

`CharacterAnimationPresentationProfile`引用的Pose Graph MUST唯一表达`ProgramParameterInput -> PoseStateMachine -> state-local Player -> AnimationSlot -> Local Pose composition -> LocalToComponentPose -> Component Pose controls -> FootPlacement -> LegIK -> ComponentToLocalPose -> OutputPose`。图 MAY包含`SelectedPosePlayer`、`BlendStack`、`Inertialization`、`BlendPose`、`LayeredBoneBlend`、`AdditivePose`、`PoseParameterResolve`、`ModifyBone`、`PoseSubgraph`、`BlendSpacePlayer`、`SequencePlayer`、`ActionPlaybackInput`、`GraphInput`、`GraphOutput`、`TwoBoneIK`、`FootPlacement`、`LegIK`与两个显式空间转换节点。Runtime MUST不在图外补建基础动画、Player、StateMachine、Slot、Blend、IK、FootPlacement、LegIK、空间转换或第二Output路径；Pose Graph MUST不保存旧AnimationSelectionInput、MotionMatchingSelectionInput或MarkerSync节点。

#### Scenario: 检查Corin正式表现链

- **WHEN** 作者打开Corin Pose Graph
- **THEN** 图 MUST能沿typed edge追踪PoseState基础Pose、Action Slot、Local/Component转换、FootPlacement目标、LegIK结果和最终输出
- **AND** MUST不显示BaseLocomotion Gameplay AnimationChannel、图外FootPlacement Pass或隐藏LegIK

### Requirement: Pose端口必须显式区分空间并允许typed控制目标

Pose Graph MUST使用`pose.local`与`pose.component`两种稳定Pose端口类型，并使用`component.biped-leg-targets`表达同帧Component空间双腿目标。Sequence、Blend、StateMachine、Slot、Inertialization、Layered、Additive与Root Orientation操作 MUST在Local Pose工作；ModifyBone、TwoBoneIK、FootPlacement与LegIK MUST位于Component Pose段。FootPlacement MUST输出Component Pose与typed targets；LegIK MUST同时消费二者。Local与Component Pose只能通过显式转换节点转换；targets不得通过Pose转换、隐式cast或Skeleton可写IK骨伪装。OutputPose MUST只接收Local Pose。

#### Scenario: 作者只连接FootPlacement Pose输出

- **WHEN** FootPlacement Component Pose继续到Output路径但targets没有连接LegIK
- **THEN** Canvas连接诊断与Validator MUST显示未完成的Foot Placement链
- **AND** Compiler MUST拒绝生成隐藏LegIK

#### Scenario: FootPlacement连接LegIK

- **WHEN** 作者把同一FootPlacement的Component Pose与targets连接到LegIK
- **THEN** Compiler MUST保留连续Component Pose段并生成world-aware到pure pose依赖
- **AND** MUST不插入额外Local/Component转换

#### Scenario: 作者把Sequence直接连接FootPlacement

- **WHEN** Local Pose Sequence输出连接Component Pose FootPlacement输入
- **THEN** Graph Canvas与Validator MUST拒绝该edge
- **AND** MUST要求作者显式插入LocalToComponentPose

#### Scenario: 多个骨骼控制共享一次空间转换

- **WHEN** 作者在LocalToComponentPose与ComponentToLocalPose之间连接ModifyBone、TwoBoneIK和FootPlacement
- **THEN** Compiler MUST保留一个连续Component Pose段
- **AND** MUST不为每个控制节点隐藏插入额外转换

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

#### Scenario: FootPlacement之后继续ModifyBone

- **WHEN** 合法Component Pose图把ModifyBone连接在FootPlacement之后
- **THEN** Compiler MUST生成world-aware stage后的后续PurePose stage
- **AND** ModifyBone MUST消费FootPlacement实际输出而不是其输入副本

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

`SequencePlayer`、`BlendSpacePlayer`与Motion Matching `SelectedPosePlayer` MUST引用类型匹配的Graph-owned`CharacterPresentationPoseSourceSlot`对象。Projection Compiler MUST从精确Definition/Profile上下文为每个可达Slot解析唯一Profile-owned typed binding子资产，并将其降低为Projection-local dense source index、typed resource plan与只读source map。Provider MUST发布`PresentationPoseSourceSample`的Pending、Ready或Invalid；Player只消费匹配自身Player identity、dense source index、generation、Projection revision与frame lease的sample。Pose Graph MUST不保存作者可编辑Source Id、Provider Id、AnimationClip、Profile binding副本，也不得把state-local source包装成Gameplay producer、AnimationChannel或PlaybackId。

#### Scenario: Idle SequencePlayer首次采样

- **WHEN** Idle State进入relevant且Source Slot对应的Profile binding合法
- **THEN** Sequence provider MUST向Idle Player发布带正确dense source index的Ready sample
- **AND** CharacterActionPlaybackRuntime MUST不登记该source

#### Scenario: Motion Matching sample投递到错误Player

- **WHEN** sample的Player identity、dense source index或Projection revision与当前demand不匹配
- **THEN** Runtime MUST拒绝该sample
- **AND** MUST不按Source Slot名称、资源名或旧Source Id猜测目标Player

### Requirement: PoseState target必须经过readiness barrier

PoseStateMachine MUST先选择候选target并向其provider提交demand。只有Ready target才可提交Transition Routing generation；已有合法source时Pending MUST保持当前source且不启动transition，Entry target Pending MUST不发布Final Pose，Invalid MUST发布typed failure并阻止该帧正式输出。系统 MUST不使用历史sample、bind pose、默认Idle、旧Timeline或Action Pose作为fallback。

#### Scenario: BlendSpace target尚未产生首样本

- **WHEN** Transition Rule选中Locomotion而BlendSpace provider返回Pending
- **THEN** 当前合法State MUST继续输出
- **AND** Transition clock MUST不开始

### Requirement: Pose State transition必须显式编译Routing并从source binding推导同步

每条Transition MUST显式配置source、target、priority、Rule、`Standard Blend | Inertialization`、duration、`Linear | EaseIn | EaseOut | EaseInOut | Custom` Blend Mode、条件式强类型Custom Curve Asset与强类型Blend Profile，MUST不保存target reset或SourceSyncMode。Custom MUST引用合法Curve Asset，非Custom MUST不保存Custom引用，Standard Blend的零duration MUST表示Hard Cut，Inertialization MUST使用正duration。每个State MUST显式配置`Always Reset on Entry`，并由StateMachine在该State provider获得entry relevance之前统一执行或跳过重置；Sequence Player MUST不拥有第二份重进配置。Projection Compiler MUST把Blend Mode降低为canonical curve index、把Blend Profile降低为匹配同一Rig的dense profile index，并为transition生成固定Routing Plan、workspace、generation、capture/release layout。Compiler MUST检查两侧State唯一的Sequence或BlendSpace provider；只有两侧source binding共享同一canonical MarkerGroup时才生成Source Sync Plan，无共同组时生成None，多于一个同步候选、角色冲突或同组topology不兼容 MUST失败。Marker topology和effective sample映射 MUST属于该Transition的source-local plan，Pose Graph MUST不创建MarkerSync节点。Runtime与Preview MUST只执行匹配Projection revision的计划，不得现场重新编译。

#### Scenario: Walk到Run启用MarkerGroup

- **WHEN** Transition两侧唯一source binding共享canonical SyncGroup
- **THEN** Source Sync Plan MUST在共同可见期间持续映射marker segment fraction
- **AND** MUST不创建BaseLocomotion Animation Selection

#### Scenario: State选择重进归零

- **WHEN** `Always Reset on Entry`为true的State再次获得entry relevance
- **THEN** StateMachine MUST在采样前重置该State的全部provider
- **AND** Transition与Sequence Player MUST不参与决定是否重置

#### Scenario: Target选择Inertialization

- **WHEN** target Ready且compiled route为Inertialization
- **THEN** transition owner MUST提交typed capture/release request
- **AND** branch-local consumer MUST完成rebase与completion

#### Scenario: Standard Blend使用每骨骼Profile

- **WHEN** Transition的Blend Profile为不同Pose Bone配置不同duration multiplier
- **THEN** Native Pose evaluator MUST对每根Physical与Virtual Bone使用同一canonical curve和各自duration求值
- **AND** Pose Parameter MUST使用全局包络，左右脚Feature MUST使用对应脚骨骼包络

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

### Requirement: TwoBoneIK与LegIK必须使用明确且不同的目标合同

`TwoBoneIK` MUST接收并输出Component Pose，显式引用Physical end bone、Pose catalog中的effector reference与joint target reference、local offset和end rotation policy。Compiler MUST从Rig v3解析唯一Physical chain并验证reference依赖；Runtime MUST只修改该Physical chain。`LegIK` MUST接收FootPlacement Component Pose与`component.biped-leg-targets`，从Rig v3解析左右Physical腿链，并把BendPlaneNormal转换为KneeDirection后执行保持骨长的双腿求解。TwoBoneIK的joint target direction与LegIK的bend plane normal MUST使用不同字段、ABI与diagnostic名称，不得共用含糊`BendDirection`。Virtual Bone对两者只读，并在Physical写入后按依赖重算。

#### Scenario: 手臂IK使用Virtual effector

- **WHEN** TwoBoneIK的effector引用Virtual Bone
- **THEN** Solver MUST读取其派生Component Pose作为目标
- **AND** MUST只写肩肘腕Physical chain

#### Scenario: LegIK消费FootPlacement目标

- **WHEN** LegIK收到同call-site合法Component Pose与双腿targets
- **THEN** Solver MUST只写Rig v3左右Hip、Knee、Ankle链及其依赖
- **AND** MUST不query world、重新应用pelvis或读取第二Weight

#### Scenario: Preview缺少world context

- **WHEN** Pose Plan到达FootPlacement但Preview没有合法world context
- **THEN** world-aware阶段 MUST报告Unavailable
- **AND** MUST不伪造FootPlacement输出或FinalAnimationPoseFrame

### Requirement: Pose Graph工作区必须准确映射Authoring、Live与References

正式窗口 MUST提供Definition-scoped Navigator、唯一`GraphAuthoringCanvasView`、Details和可折叠Bottom Dock。Details MUST分离Authoring、Live与References：Authoring只通过正式Presentation Mutation修改当前owner字段；Live只读取匹配PoseGraphId、PoseGraphRevision与ProjectionRevision的snapshot；References只读显示Source Slot、Profile binding子资产、实际资源对象、source map、Action producer、Rig、Policy和call site。稳定identity、GUID、revision、hash与compiled index MUST默认隐藏。Live Debug模式下mutation MUST禁用，revision不匹配 MUST显示Stale并清空旧值。

#### Scenario: 查看Locomotion State

- **WHEN** 作者选中Locomotion State的Sequence或BlendSpace Player
- **THEN** Authoring MUST显示类型匹配的Source Slot对象选择器
- **AND** References MUST显示解析后的Profile binding、实际资源、owner与Open Source命令
- **AND** MUST不显示BaseLocomotion Gameplay producer或可编辑Source Id

#### Scenario: Runtime revision不匹配

- **WHEN** snapshot revision与当前文档或Projection不一致
- **THEN** Live MUST显示Stale
- **AND** MUST不从authoring默认值或Animancer state伪造结果

### Requirement: Pose Watch必须只观察已完成Pose与typed目标Value

Editor MUST允许按稳定PoseNodeId与call-site订阅Pose Watch，并允许FootPlacement targets使用只读Target Watch。Watch selection、颜色、显隐和面板状态 MUST只属于editor view-state。Preview或Runtime diagnostics MUST从同一帧已完成workspace复制固定容量的目标Pose、Pose空间、targets与contribution，不得重新执行节点、第二次采样source、修改Player/transition/history或改变FinalAnimationPoseFrame。FootPlacement MUST在pelvis Pose与targets同时完成后发布completion；LegIK MUST在双腿求解完成后发布可观察Pose。

#### Scenario: 同时观察FootPlacement和LegIK

- **WHEN** FootPlacement与LegIK都启用Watch
- **THEN** FootPlacement Watch MUST显示应用pelvis后的Pose与左右目标，LegIK Watch MUST显示最终双腿Pose
- **AND** 两者 MUST共享同一Frame与FootPlacement Completion lineage

#### Scenario: 同时观察State Player和FootPlacement

- **WHEN** 两个节点都启用Pose Watch
- **THEN** diagnostics MUST从同一frame lineage发布Local State Player Pose、应用pelvis后的FootPlacement Pose与LegIK已求解Pose
- **AND** MUST不额外Evaluate PlayableGraph或读取Transform反推结果

### Requirement: Preview、Runtime与Live Debug必须复用同一固定Pose Plan

Projection Compiler MUST把Pose Graph降低为固定Fact/parameter、source demand/capture、空间化PurePose、world-aware Pose与final publication stage table。每帧每个source、Player、transition、Slot、composition、转换、IK、world-aware control和writer MUST只执行一次正式计划，所有source合计只进行一次正式PlayableGraph Evaluate。Action Timeline Preview、Pose Graph Fact Preview、MM Query Fixture、Live Debug与正式Runtime MUST使用同一Projection revision、Routing Plan、stage table、source backend和completion语义；精确Host world context完整时Preview MUST执行FootPlacement真实stage，不完整时 MUST报告typed Unavailable。Graph mutation或Stale Projection时Preview MUST停止并等待显式Build。

#### Scenario: Graph修改后继续Preview

- **WHEN** 作者修改State、Slot、Rig、Pose空间或FootPlacement使Projection变为Stale
- **THEN** Preview MUST停止消费旧Plan
- **AND** MUST不创建临时Plan、默认空间转换或旧Projection fallback

### Requirement: Pose authoring必须使用共享Capability与类型化Presentation Mutation

Pose Graph、PoseStateMachine、Node、Port与Edge MUST使用共享typed domain document。

#### Scenario: 新增Pose节点能力

- **WHEN** 新Pose节点注册typed payload与compiler handler
- **THEN** 人工创建菜单、Document v3、Validator和Compiler MUST识别同一Capability
- **AND** MUST不复制Node/Port View或第二Compiler入口

### Requirement: Pose Graph UI必须保留准确术语和serialized identity

UI MAY把正式`PoseStateMachine`显示为Animation State Machine、把`AnimationSlot`显示为Slot，并使用Anim Graph、Sequence Player、Transition Rule、State Alias、Layered Blend Per Bone、Inertialization、Sync Group、Pose Watch和Output Pose。系统 MUST在序列化、Undo、clipboard、Document、compiler source map与Diagnostics中保留项目serialized node kind和stable identity，但人工UI MUST默认使用业务显示名和Unity资源对象，不得把identity、GUID、hash或compiled index作为节点标题、Navigator项目、breadcrumb或可编辑字段。AnimationChannel仍是有限Action arbitration identity，BTSMTL Action Timeline职责近似Montage但不得伪装成Montage资产。

#### Scenario: 显示FullBodyAction

- **WHEN** Navigator选中FullBodyAction Slot
- **THEN** UI MUST显示Slot业务名与绑定Action AnimationChannel的业务名
- **AND** 原始SlotId与AnimationChannelId MUST只在显式Diagnostics中只读出现
- **AND** MUST不把AnimationChannel本身序列化为Slot

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
