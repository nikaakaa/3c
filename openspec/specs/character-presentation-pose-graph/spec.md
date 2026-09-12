# character-presentation-pose-graph Specification

## Purpose

定义Character Presentation Pose Graph的正式数据模型、编译边界、作者工作区、Preview、Live Debug和Pose Watch。
## Requirements
### Requirement: Pose Graph运行输入必须通过显式Character Binding装配

Pose Graph MUST只表达可复用的表现拓扑和抽象能力合同；Character-specific资源、Rig、Policy、Foot/IK配置、Action producer与资源解析目录 MUST来自显式的`CharacterAnimationPresentationProfile` Binding。`CharacterPresentationPoseGraphAsset` MUST不得反向引用Profile、CharacterPipelineDefinition、Character实例或生成Projection。编译输入必须明确区分`Reusable PoseGraph`与`Character PoseGraph Binding`，不得从资产路径、角色名、当前Selection或默认Profile推断Binding。

#### Scenario: 同一Graph实例化两个Profile

- **WHEN** 同一shared Pose Graph被两个不同Profile引用
- **THEN** Compiler MUST为每个Profile分别创建Projection-local source/resource dense binding
- **AND** Graph revision与Graph topology MUST保持同一份共享输入
- **AND** 任一Profile的资源替换 MUST不回写Graph-owned拓扑或另一个Profile

#### Scenario: Binding缺失

- **WHEN** Graph声明了需要Binding的Source Slot或Resource Slot但Profile没有合法匹配项
- **THEN** Definition Build MUST报告精确Graph/Node/Slot路径
- **AND** MUST不使用角色名匹配、数组位置、默认资源或旧Projection作为fallback

#### Scenario: 未使用的Resource Slot保持可选

- **WHEN** shared Pose Graph声明了Resource Slot，但当前可达Graph拓扑没有任何节点消费该Slot
- **THEN** Profile可以不提供该Slot的Binding，Pose Resource Catalog MUST不因此失败
- **AND** 当可达节点实际消费该Slot时，缺失或类型不匹配的Binding MUST按Graph/Node/Field路径使Build失败

### Requirement: Pose Graph必须唯一表达完整表现拓扑

`CharacterAnimationPresentationProfile`引用的Pose Graph MUST唯一表达`PoseStateMachine -> state-local Player -> AnimationSlot -> Local Pose composition -> LocalToComponentPose -> Component Pose controls -> Goal Contributions -> FullBodyIK -> ComponentToLocalPose -> OutputPose`。FootPlacement与PoseBoneIKGoals MUST从同一Component Pose扇出typed Goal Contribution，Compiler MUST在唯一FullBodyIK前生成唯一Goal Assembler与Goal Set。

Runtime MUST不在图外补建Goal Assembler、Foot Placement、FBBIK、空间转换、第二Goal Set、第二Pose Graph或第二Output路径。

#### Scenario: 查看完整Foot Placement拓扑

- **WHEN** 作者查看包含FootPlacement与PoseBone Goal来源的正式Pose Graph
- **THEN** 图 MUST明确显示两个Goal Contribution进入Control Rig中的FullBodyIK typed输入，Compiler MUST展开唯一Assembler，再进入唯一FullBodyIK
- **AND** MUST不存在多个Goal Set并行汇入FBBIK的隐藏拓扑

### Requirement: Pose端口必须显式区分空间并允许typed控制目标

Pose Graph MUST使用`pose.local`、`pose.component`与`component.full-body-ik-goal-contribution`稳定端口类型；`component.full-body-ik-goals`只属于Compiler生成的内部 operation。FootPlacement与PoseBoneIKGoals只读Component Pose并输出Goal Contribution；Control Rig中的FullBodyIK可接收有序typed Contribution输入，Compiler负责生成唯一Goal Set并输出Component Pose。

Goal Contribution、Goal Set与Pose空间不得隐式cast、复用同一端口或通过Skeleton可写IK骨伪装。Goal Assembler MUST拒绝重复Effector Slot、错误Application、不同Frame/Completion/Rig lineage和超过编译容量的Contribution。

#### Scenario: FootPlacement与PoseBone贡献同一Slot

- **WHEN** Compiler发现FootPlacement与PoseBoneIKGoals可能写入同一Effector Slot
- **THEN** Character Build MUST报告两个producer与冲突Slot并拒绝Projection发布
- **AND** Runtime MUST不依赖Goal连接顺序决定覆盖者

### Requirement: Pose Plan必须按拓扑编译为有序执行阶段

唯一Pose Compiler Module MUST通过`Graph Closure -> Typed IR -> Topology -> Symbolic Family Lowering -> Stage Schedule -> Value Lifetime -> Workspace Plan -> Bind Family Payload -> Seal Program Image`固定Pass链，把同一Pose DAG编译为`CharacterPresentationProjection`内部唯一不可变`CharacterPoseProgramImage`。Graph Closure MUST只通过root catalog、PoseState引用与Node Definition Graph dependency投影展开Subgraph/Linked Pose call。Program Image MUST按typed依赖、Pose空间与Execution Domain保存有序`FactAndDemand`、`SourceCapture`、`PurePose`、`WorldAwareValue`、`PureValue`与`FinalPublication`Stage，并使用公共Operation Header、typed Value Reference和分段Operation Family Payload；MUST不保存万能Operation可选字段、Actor State、Frame Pending页或运行时Tuning。Runtime MUST不构造第二语义Program，每个Program Runtime只可建立最多一份同identity、actor-local、只读Execution View。

Goal Contribution收集、编译生成的唯一Goal Assembler、唯一Goal Set、FBBIK和OutputPose MUST进入固定阶段，Projection MUST静态证明每条正式路径最多一个Assembler、一个Goal Set、一个FBBIK、一个OutputPose和一个Final Publication requirement。每个Constraint Family Operation MUST在自己的Stage位置通过typed编译Handle调用Constraint Module一次，Constraint不得扫描Program或维护第二Schedule。Output Family MUST只保存稳定`CharacterFinalPosePublicationLayoutHandle`，不得保存Actor页指针或分配第二Final Pose buffer。具体Final Publication、Physical Bone binding与Writer唯一性 MUST由Runtime Factory和Final Publication构造验证，Compiler不得创建Writer Graph节点。每个source每帧 MUST最多capture一次，每个Operation MUST恰好执行一次，PlayableGraph MUST最多Evaluate一次，Physical Transform MUST只由Final Publication中的唯一Writer写一次。Runtime MUST不重新编译、重排、补执行或解释authoring Graph。

#### Scenario: Foot Placement后执行FullBodyIK

- **WHEN** Foot Placement与其它Goal Source完成同Frame Goal Contribution
- **THEN** Stage Schedule MUST在其后执行编译生成的唯一Goal Assembler和唯一FullBodyIK
- **AND** 后续节点 MUST消费FBBIK输出而不是输入Pose或外层Runtime预先生成的副本

#### Scenario: 编译无Goal贡献的角色

- **WHEN** 某角色的正式Pose Graph没有任何有效Goal Contribution
- **THEN** 唯一Assembler MUST编译为固定容量零贡献并发布`GoalCount=0`
- **AND** Compiler MUST不插入Empty Goal fallback、Goal Set copy或第二Assembler

#### Scenario: Operation被重复调度

- **WHEN** Stage Schedule包含重复Operation index、遗漏可达Operation或不匹配Execution Domain
- **THEN** Program Image Seal MUST拒绝Projection发布
- **AND** Runtime MUST不通过completion检查掩盖非法Schedule

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

`ClipPlayer`、`BlendSpacePlayer`与`SelectedPosePlayer` MUST声明类型匹配的Graph-owned Source Slot，原生资源由Character Presentation Profile Binding提供。Projection Compiler MUST从Graph Slot与Profile Binding生成dense source usage；ClipPlayer MUST保存Source Slot、Play Rate、Initial Time、Loop与Clock Source，不得保存Character资源、Profile Binding或Topology副本。Provider MUST发布带dense source index、generation、Projection revision与frame lease的`PresentationPoseSourceSample`。Pose Graph MUST不保存Sequence包装资产、作者source字符串或Gameplay producer。

#### Scenario: ClipPlayer首次采样Idle

- **WHEN** Idle State的ClipPlayer获得entry relevance
- **THEN** provider MUST从Profile direct Clip Binding发布Ready sample
- **AND** Player MUST不解析Sequence或AssetDatabase

#### Scenario: ClipPlayer提交Loop字段

- **WHEN** 人工Capability或C#作者API为ClipPlayer提供合法Loop策略
- **THEN** Compiler MUST按该Player usage编译Finite或Cyclic时间行为
- **AND** MUST不复制素材曲线或创建Sequence包装资产

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

`AnimationSlot` MUST拥有Source Pose输入、稳定SlotId、Slot Group与AnimationChannelId以及node-local Routing Plan；Action Playback读取由Compiler展开，不要求作者连接Action Playback Input。无Action时Slot MUST透传同帧Source Pose；Action Ready时 MUST插入Action Pose；Action release时 MUST过渡回持续更新的`SourcePoseEndpoint`。Slot MUST不判断Action admission、不推进Timeline、不控制Locomotion PoseState、不拥有Bone Mask，也 MUST不把NoPose和SourcePoseEndpoint混为一谈。

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

全部Goal Source MUST发布`CharacterFullBodyIkGoalContribution`，至少携带Frame、Completion、Rig、Producer、Slot、Application、Component空间目标与权重。Foot Placement内部 MUST在既有Pelvis响应、可达观察与原Landing完成判断后才发布最终Resolved Pair；Foot Goal Encoder MUST只读取最终Resolved的目标和权重，Pelvis Goal Encoder MUST只读取唯一Pelvis Result。两者 MUST不读取Foot State、Lock Response、Context、Path、Residual或Diagnostics，不得恢复业务层Reach夹紧或末端夹脚。

唯一Goal Assembler MUST把合法Contribution规范化为一个`CharacterFullBodyIkGoalSet`，继续由现有入口拥有身份、容量、合法性和重复Slot校验，不接管Foot或Pelvis数学，也不为内部请求分型逐层复制相同检查。FBBIK MUST不理解Foot State Context、Contact Patch、Constraint State、Pelvis选择或Diagnostics。

FBBIK腿Effector跨帧稳定策略 MUST由正式FullBodyIK Profile、Rig准备结果和Pending BendHistory决定。Solver MUST不通过搜索FootPlacement SourceKind启用隐藏状态规则；Vendor FinalIK对象内部可变字段不得成为跨帧真相。正式初始化方向只允许来自同一参考姿态准备结果，不增加第二种默认策略。

233436组合中已保留的可靠动画有符号膝向运输 MUST保持，Stable继续保存运输前动画方向，Applied继续保存实际请求，退化分支维持既有政策。结构迁移 MUST不恢复可靠动画半球强翻、SmoothKnee尾段或改变Bend权重来掩盖已知深折叠。

Goal对求解要求权威，Solved Pose对本次Solver输出权威，Physical Result对本次实际写入权威；三者 MUST保留同Completion阶段区别，不得互相代替或反推覆盖源Pose。Solver MUST只写Pending Pose及正式BendHistory，Writer MUST唯一写骨骼；Encoder、Assembler、Root调度和诊断不得取得额外Pose写入权。已有特殊Goal应用数学保持本change范围约定。

#### Scenario: FBBIK消费Foot Placement贡献

- **WHEN** FootPlacement已完成初步请求、Pelvis响应、可达观察和原Landing完成判断，并由Assembler完成唯一Goal Set
- **THEN** FBBIK MUST只按Goal Application、Slot、Profile、正式Rig准备结果与Pending BendHistory执行求解
- **AND** MUST不回调FootPlacement、读取Ground Path或修改Contact ownership

#### Scenario: 编码几何不可达但保持原值的目标

- **WHEN** Foot记录了本腿不可达观察且原流程保持该脚目标与作者权重
- **THEN** Encoder MUST只完成原空间和权重编码，保持最终Resolved与Contribution一致
- **AND** Assembler MUST不新增Reach拦截或夹脚，FBBIK继续原求解数学并保留真实误差

### Requirement: Pose Graph工作区必须准确映射Authoring、Live与References

正式窗口 MUST以FlowCanvas原生`GraphEditor`作为Pose作者宿主，使用原生画布、breadcrumb、节点/连线Inspector、创建菜单、子图下钻、selection、clipboard与编辑器Undo。Pose领域Adapter只向这些扩展点提供Capability、typed字段、端口形状、业务命令和Mutation路由；MUST不通过自定义domain panel替换FlowCanvas原生Inspector，也不得建立第二套节点字段编辑器或独立selection集合。

项目自定义面板 MAY提供Definition-scoped跨Graph检索、Runtime Observation、References、Preview状态、Build/Validate报告和诊断；这些内容不得成为Pose节点作者字段的第二入口。Authoring修改 MUST只通过正式Presentation Mutation修改当前owner字段；Live只读取匹配PoseGraphId、PoseGraphRevision与ProjectionRevision的snapshot；References只读显示直接资源、Action producer、Slot／Group、Rig、Policy和call site。稳定identity、GUID、revision、hash与compiled index MUST默认隐藏。Live Debug模式下mutation MUST禁用，revision不匹配 MUST显示Stale并清空旧值。

#### Scenario: 查看Locomotion State

- **WHEN** 作者选中Locomotion State的Clip或BlendSpace Player
- **THEN** Authoring MUST显示类型匹配的Source Slot，并在精确Profile上下文中显示其原生AnimationClip或Blend Space Binding
- **AND** References MUST显示实际资源、Slot／Group、owner与Open Resource命令
- **AND** MUST不显示BaseLocomotion Gameplay producer或可编辑Source Id

#### Scenario: Runtime revision不匹配

- **WHEN** snapshot revision与当前文档或Projection不一致
- **THEN** Live MUST显示Stale
- **AND** MUST不从authoring默认值或Animancer state伪造结果

### Requirement: Pose Watch必须只观察已完成Pose与typed目标Value

Editor MUST允许按稳定PoseNodeId与call-site订阅Pose Watch，并允许Goal Contribution、Goal Set与Constraint结果使用只读Target Watch。Watch selection、颜色、显隐和面板状态 MUST只属于Editor view-state。Runtime MUST在Frame开始冻结interest，并在各正式Module完成Pending Result时向固定容量诊断页冻结被订阅的Pose、Value、Contribution、Goal、FBBIK与Physical结果；成功Seal后Watch MUST只读取同Frame、Completion、Program、Projection与Rig lineage的Committed Result。Watch MUST不访问Program内部Workspace、Actor State、Foot Context、FBBIK Vendor对象或Physical Transform反推，也不得重新执行节点、source sample、world query或FBBIK。

#### Scenario: 同时观察FootPlacement和FullBodyIK

- **WHEN** Frame开始时Foot Placement Goal与FullBodyIK Pose都启用Watch且Frame成功Seal
- **THEN** 两者 MUST来自同一Committed Program与Constraint Result lineage
- **AND** Watch MUST不重新执行Foot Placement、Goal Assembly或FBBIK

#### Scenario: Frame中途启用Watch

- **WHEN** 作者在当前Frame已经开始后启用新的Pose Watch
- **THEN** 当前正式结果 MUST保持不变且新详情 MAY从下一成功Frame开始
- **AND** Diagnostics MUST不读取Pending Workspace补齐半帧结果

#### Scenario: 同时观察State Player和FootPlacement

- **WHEN** 两个节点都启用Pose Watch
- **THEN** diagnostics MUST从同一frame lineage发布Local State Player Pose、FootPlacement Goal与FullBodyIK输出Pose
- **AND** MUST不额外Evaluate PlayableGraph或读取Transform反推结果

### Requirement: Preview、Runtime与Live Debug必须复用同一固定Pose Plan

Projection Compiler MUST把Pose Graph降低为`CharacterPresentationProjection`内部唯一不可变`CharacterPoseProgramImage`，并由同一Factory装配actor-local Execution View、`CharacterPoseProgramRuntime`、`CharacterPoseSourceModule`、`CharacterPoseConstraintRuntime`、`CharacterFinalPosePublication`、根Frame Transaction和actor-local Tuning Snapshot。正式Runtime与正式Preview MUST直接读取同一Projection内Program Image并让各自Program Runtime遵守同一Execution View materialization/Dispose规则，不得创建第二语义Program；二者 MUST使用同一Program Image schema、Stage Schedule、Operation Family evaluator、source backend、world-query Adapter、FinalIK Pose Buffer backend、Final Writer和completion语义；Live Debug MUST只读取对应Committed Result。Preview入口 MUST使用正式Preview Fixture/Scene Session，不得由PoseGraph作者窗口创建第二播放时钟、临时Program或简化Executor。每帧每个source、Player、Action lifecycle、Transition、Slot、composition、转换、Goal Source、Assembler、FBBIK和Writer MUST只执行一次正式计划。Graph mutation或Stale Projection时Preview MUST停止并等待显式Build。

#### Scenario: Graph修改后继续Preview

- **WHEN** 作者修改State、Slot、Rig、Pose空间、Node Definition字段或Foot Placement使Projection变为Stale
- **THEN** Preview MUST停止消费旧Program Image
- **AND** MUST不创建临时Program、旧ABI reader、默认空间转换或旧Projection fallback

#### Scenario: Preview缺少world context

- **WHEN** Preview执行同一Program Image到Foot Placement但精确World Context Adapter不可用
- **THEN** Program Runtime MUST发布typed Unavailable并停止该Frame publication
- **AND** Preview MUST不使用简化Constraint或跳过该Operation

### Requirement: Pose authoring必须通过正式Capability和Presentation Mutation

Pose Graph、PoseStateMachine、Node、Port与Edge MUST继续使用共享typed domain authoring。每个正式Node Kind MUST通过唯一`CharacterPoseNodeDefinition` Adapter声明Payload字段、固定端口、条件`portVariants`、动态端口政策、Graph Role、Execution Domain、Operation Family、Graph dependency与typed lowering。Definition MUST先投影共享`GraphAuthoringCapabilityCatalog`，再由唯一`GraphAuthoringNodePortShapeProjector`把完整端口形状提供给Canvas、C#作者API、Clipboard、Mutation preflight与局部Validator；Compiler MUST只从同一Definition读取Graph dependency、typed lowering与Source Map。系统 MUST不保留第二节点目录、重复字段switch、`ICharacterPoseCompilerHandler`布尔能力矩阵或独立Compiler binding真相。跨节点拓扑规则 MUST只属于唯一Topology Pass。

#### Scenario: 新增Pose节点能力

- **WHEN** 新Pose节点注册唯一Definition Adapter
- **THEN** 人工创建菜单、C#作者API、Clipboard、统一Port Shape、Validator、Graph Closure和Compiler MUST识别同一Capability与Payload合同
- **AND** MUST不要求在多个Catalog、Handler或NodeKind switch中重复声明同一字段和端口

#### Scenario: Node Definition缺少C#作者API投影

- **WHEN** 一个Definition无法为正式C#作者API/Mutation合同提供完整typed字段、条件端口或Graph dependency
- **THEN** Definition目录或Character Build MUST失败并定位Node Kind
- **AND** C#作者API MUST不使用通用SerializedProperty或自由文本绕过

### Requirement: Pose Graph UI必须保留准确术语和serialized identity

UI MUST使用Clip Player、Blend Space Player、Selected Pose Player、Animation State Machine、Slot、Layered Blend Per Bone、Inertialization、Locomotion Phase Group、Pose Watch和Output Pose等准确术语。序列化、C#作者API、Mutation、Compiler source map和Diagnostics MUST使用同一Clip命名；MUST不保留Clip Player显示名或旧node kind alias。

#### Scenario: 作者添加单Clip播放器

- **WHEN** 作者在Pose Graph添加单Clip state-local player并选择类型匹配的Source Slot
- **THEN** Capability、节点标题、C#作者API kind和编译诊断 MUST统一显示Clip Player
- **AND** MUST不存在Clip Player兼容名称

### Requirement: Pose Graph作者资产必须与Character Presentation Binding分离

Pose Graph作者资产 MUST 可被多个Character Presentation Profile引用。Graph MUST只拥有稳定Graph／Node／Port／Edge身份、Pose参数、StateMachine、Transition、Graph Role、节点拓扑和抽象能力合同；MUST不直接拥有CharacterPipelineDefinition、AnimationClip、BlendSpace、MotionMatching Profile、Rig、Bone Mask、FullBodyIK、Foot Placement、Character Blend Policy、Action producer或角色专属Pose Source Binding。

Character Presentation Profile或独立Binding MUST按稳定Graph／Node／Field identity提供上述角色资源和能力映射。正式Projection／Program MUST由Reusable Pose Graph、Binding、Rig和精确Definition共同生成；不同Character复用同一Graph时，Binding缺失或不兼容 MUST以稳定Graph／Node／Field路径失败，不得按名称、默认资源、数组位置或角色上下文猜测。

FlowCanvas GraphEditor打开可复用Pose Graph时 MUST不要求Character Runtime上下文；Binding编辑、正式Preview和Character Build才要求精确Profile、Rig和Definition上下文。

#### Scenario: 两个Character复用同一Pose Graph

- **WHEN** 两个Character Presentation Profile引用同一个可复用Pose Graph但提供不同的Rig和资源Binding
- **THEN** 系统 MUST按各自稳定Graph／Node／Field identity解析Binding并生成各自Presentation Projection
- **AND** Pose Graph资产 MUST不写入任一Character专属AnimationClip、Rig、Mask或Pose Source Binding

### Requirement: Pose StateMachine layout必须是独立纯作者数据

每个root-owned PoseStateMachine MUST在`CharacterPresentationPoseGraphAsset`中拥有按稳定`PoseStateMachineId`索引的唯一layout owner。Layout MAY稀疏保存Entry、State与Alias的显式二维位置；缺少显式位置时 MUST按元素类型和稳定identity使用唯一确定性排布。Layout MUST拒绝重复identity、未知元素和非有限坐标，且 MUST不保存Transition edge位置。Layout变化 MUST进入typed Presentation Mutation、Undo、dirty、保存，并由正式C#作者API与人工UI使用同一语义，但 MUST不修改PoseStateMachine `ContentRevision`、不得使Presentation Projection变为Stale，也不得触发Compile或Build。Compiler与Runtime MUST不读取layout。

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
