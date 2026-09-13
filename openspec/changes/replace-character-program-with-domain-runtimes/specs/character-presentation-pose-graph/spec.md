## MODIFIED Requirements

### Requirement: Pose Graph运行输入必须通过显式Character Binding装配

Pose 图 MUST只表达可复用的表现拓扑及能力要求。角色资源、Rig、Policy、Foot／IK 和有限动作输入 MUST由明确动画配置与实例绑定提供；图不得反向依赖角色实例或生成 Projection。运行创建 MUST区分原生图与角色绑定，不能从角色名、Selection、目录或默认 Profile 推断。

#### Scenario: 同一Graph实例化两个Profile

- **WHEN** 同一shared Pose Graph被两个不同Profile引用
- **THEN** 图校验与绑定 MUST为每个Profile分别创建实例绑定内的 source/resource dense binding
- **AND** Graph revision与Graph topology MUST保持同一份共享输入
- **AND** 任一Profile的资源替换 MUST不回写Graph-owned拓扑或另一个Profile

#### Scenario: Binding缺失

- **WHEN** Graph声明了需要Binding的Source Slot或Resource Slot但Profile没有合法匹配项
- **THEN** Definition Build MUST报告精确Graph/Node/Slot路径
- **AND** MUST不使用角色名匹配、数组位置、默认资源或旧动画绑定作为fallback

#### Scenario: 未使用的Resource Slot保持可选

- **WHEN** shared Pose Graph声明了Resource Slot，但当前可达Graph拓扑没有任何节点消费该Slot
- **THEN** Profile可以不提供该Slot的Binding，Pose Resource Catalog MUST不因此失败
- **AND** 当可达节点实际消费该Slot时，缺失或类型不匹配的Binding MUST按Graph/Node/Field路径使Build失败


### Requirement: Pose Graph必须唯一表达完整表现拓扑

正式原生图 MUST唯一表达 PoseState／Player／Slot、Local Pose 组合、LocalToComponent、Component 控制、Goal 贡献、FBBIK、ComponentToLocal 和 Output。Foot 与目标节点读取同一合法 Component Pose；FBBIK 节点 MUST在 Constraint owner 内按有序 typed 贡献形成唯一 Goal Set，不需要编译器生成作者不可见操作。图外 MUST不补第二 Foot／IK／空间转换／输出路径。

#### Scenario: 查看完整Foot Placement拓扑

- **WHEN** 作者查看包含FootPlacement与PoseBone Goal来源的正式Pose Graph
- **THEN** 图 MUST明确显示两个Goal Contribution进入Control Rig中的FullBodyIK typed输入，图校验与绑定 MUST展开唯一Assembler，再进入唯一FullBodyIK
- **AND** MUST不存在多个Goal Set并行汇入FBBIK的隐藏拓扑


### Requirement: Pose端口必须显式区分空间并允许typed控制目标

原生 Pose 端口 MUST区分 Local Pose、Component Pose 和 typed Goal Contribution。Foot／Goal 节点 MUST只读 Component Pose 并输出贡献；FBBIK 节点内部由唯一 Constraint owner 聚合 Goal Set 后求解。端口 MUST不隐式 cast，不以可写骨骼伪装目标；重复 Effector Slot、错误 Application、Frame／Rig／调用身份不一致或超出实例容量 MUST失败。

#### Scenario: FootPlacement与PoseBone贡献同一Slot

- **WHEN** 图校验发现FootPlacement与PoseBoneIKGoals可能写入同一Effector Slot
- **THEN** Character Build MUST报告两个producer与冲突Slot并拒绝动画绑定发布
- **AND** Runtime MUST不依赖Goal连接顺序决定覆盖者


### Requirement: Pose Plan必须按拓扑编译为有序执行阶段

Pose MUST通过原生图节点及连接执行，不产生 IR、Stage plan 或 Image。宿主 MUST按准备源需求、唯一采样 Barrier、求值和最终提交的明确阶段驱动；每个节点按调用／阶段只执行一次并复用结果。实例绑定 MUST检查类型、递归、唯一输出与写冲突，运行使用复用缓冲；World-aware Constraint MUST在正式节点处执行。

#### Scenario: Foot Placement后执行FullBodyIK

- **WHEN** Foot Placement与其它Goal Source完成同Frame Goal Contribution
- **THEN** 原生图 MUST在贡献节点完成后通过唯一Constraint聚合和FullBodyIK产生结果
- **AND** 后续节点 MUST消费FBBIK输出而不是输入Pose或外层Runtime预先生成的副本

#### Scenario: 编译无Goal贡献的角色

- **WHEN** 某角色的正式Pose Graph没有任何有效Goal Contribution
- **THEN** 唯一Constraint聚合 MUST处理零贡献并发布`GoalCount=0`
- **AND** 图校验与绑定 MUST不插入Empty Goal fallback、Goal Set copy或第二Assembler

#### Scenario: Operation被重复调度

- **WHEN** 原生图绑定存在递归／重复调用身份、遗漏必要贡献或不匹配的空间／阶段
- **THEN** 原生图实例 Seal MUST拒绝动画绑定发布
- **AND** Runtime MUST不通过completion检查掩盖非法Schedule


### Requirement: PoseStateMachine必须是纯表现状态机

`PoseStateMachine` MUST拥有稳定Entry、State、Transition、State Alias和MaxTransitionsPerFrame。Transition Rule MUST只读取同帧`CharacterPresentationFactFrame`、TimeInState与StatePoseRemainingTime；MUST不读取Gameplay Blackboard mutable address、ActionInstance、Timeline operation、Unity Transform或World query。State Alias MUST只复用合法source State集合，不得拥有Pose或成为active runtime State。PoseStateMachine MUST只编入Presentation 动画绑定，不得进入Gameplay Semantic IR或Numeric Program。

#### Scenario: Idle进入Locomotion

- **WHEN** typed HorizontalSpeed Fact满足Transition Rule
- **THEN** PoseStateMachine MUST按priority和stable order选择唯一target
- **AND** Gameplay MUST不发送PlayRun事件

#### Scenario: 同帧多个Transition成立

- **WHEN** 多条可达Transition Rule同时为true
- **THEN** Runtime MUST遵守compiled priority、stable order和MaxTransitionsPerFrame
- **AND** MUST不依赖容器遍历顺序


### Requirement: State inline graph必须存入root-owned flat graph catalog

State在作者语义上 MAY拥有inline Pose subgraph，但serialized State MUST只保存稳定PoseGraphId与OutputPoseNodeId。`CharacterPresentationPoseGraphAsset` MUST用root-owned flat catalog保存`PoseGraphId -> CharacterPoseGraphData`，Subgraph call也 MUST只保存GraphId。Validator与图校验与绑定 MUST检查GraphId唯一、每个可达State恰有一个Output、call依赖无递归和无悬空引用。Runtime MUST只读取编译后的flat plan，不得动态展开嵌套对象树。

#### Scenario: 打开Locomotion State图

- **WHEN** 作者进入Locomotion State
- **THEN** Editor MUST按State的PoseGraphId导航到catalog记录
- **AND** State serialized data MUST不再嵌套旧`m_InlinePoseGraph`

#### Scenario: Subgraph形成递归

- **WHEN** Graph A和Graph B通过GraphId相互调用
- **THEN** Build MUST失败并报告完整依赖链
- **AND** Runtime MUST不尝试动态递归


### Requirement: State-local source必须由Profile binding和provider解析

状态子图中的 Player MUST通过其正式资源引用和明确角色绑定解析 source，运行句柄在实例创建或正式资源准备时建立。采样、生命周期和同步 MUST只归该 state-local source，不得从名称、旧 channel、默认动画或技能目录推断。图状态和源物理资源 MUST分别拥有。

#### Scenario: ClipPlayer首次采样Idle

- **WHEN** Idle State的ClipPlayer获得entry relevance
- **THEN** provider MUST从Profile direct Clip Binding发布Ready sample
- **AND** Player MUST不解析Sequence或AssetDatabase

#### Scenario: ClipPlayer提交Loop字段

- **WHEN** 人工Capability或C#作者API为ClipPlayer提供合法Loop策略
- **THEN** 图校验与绑定 MUST按该Player usage编译Finite或Cyclic时间行为
- **AND** MUST不复制素材曲线或创建Sequence包装资产


### Requirement: Pose State transition必须显式编译Routing并从source binding推导同步

PoseState 转换 MUST继续按正式过渡配置决定 Standard Blend／Inertialization 和 source 同步，保留优先级、时序、可用性、目标准备、generation 与释放行为。运行实例 MUST从明确的状态／资源绑定建立所需关联，不生成全图操作计划；同组 Phase 的资源数据处理不因取消图编译而删除。

#### Scenario: Turn进入RunLoop

- **WHEN** Turn与RunLoop属于同一Locomotion Sync Group且Transition条件成立
- **THEN** target effective time MUST来自compiled Phase relation
- **AND** Blend Routing MUST独立使用edge-owned Standard Blend计划


### Requirement: Pose节点必须显式处理可用性和局部连续性

每个Player和composition节点 MUST声明RequirePose或AllowEmpty。NoPose MUST是typed availability，不得用bind pose、零矩阵或上一帧缓存伪装。`SelectedPosePlayer` MUST发布source discontinuity；显式`BlendStack` MUST唯一拥有自身多source history、clock、Stored Pose和release；`Inertialization` MUST唯一拥有直接上游局部Pose的history、residual与rebase。图校验与Runtime MUST不在OutputPose前补建全局连续化节点。

#### Scenario: 局部Action分支惯性化

- **WHEN** Action Player连接Inertialization后进入LayeredBoneBlend
- **THEN** residual MUST只影响该Action分支
- **AND** Base Pose分支 MUST不共享其history

#### Scenario: Required Pose缺失

- **WHEN** 唯一Output路径上的Required Pose为Invalid
- **THEN** 原生Pose图 MUST失败
- **AND** MUST不发布旧FinalAnimationPoseFrame


### Requirement: Pose参数必须通过typed页面和显式解析传播

Pose MUST只读事件图成功发布的 typed 动画变量 Frame，以及明确的 Fact／World／子图公开参数／source-local 曲线输入。原生输入端口 MUST保持类型与来源身份，按调用实例传播，不复制可写声明或用名称猜测。控制与混合节点 MUST消费该帧一致值，不反写 Gameplay 或动画共享变量。

#### Scenario: Blend权重读取Program参数

- **WHEN** BlendPose权重连接ProgramParameterInput
- **THEN** 图校验与绑定 MUST校验ParameterId、类型和page layout
- **AND** Runtime MUST不读取Gameplay对象


### Requirement: Rig、Mask和Pose运输必须使用Rig v4

Rig v4 MUST以Physical Bones与Virtual Bones组成唯一Pose catalog，并显式声明Solver Root、Pelvis、Spine、Arm与Leg chain。所有source capture、Pose workspace、空间转换、Mask、Blend Profile、composition和IK MUST按PoseBoneCount运输；Animator binding与final writer MUST只读写PhysicalBoneCount。Virtual Bone MUST由已采样Physical Pose按编译依赖顺序派生，不得绑定Transform或直接写Animator。未知BoneId、跨Rig引用、重复写冲突、非法FBBIK chain或非法Virtual依赖 MUST使Build失败。

#### Scenario: source capture包含Virtual Bone

- **WHEN** Physical source Pose采样完成
- **THEN** capture阶段 MUST派生全部Virtual Bone并形成完整PoseBoneCount
- **AND** source backend MUST不查找Virtual Transform

#### Scenario: Mask遗漏Virtual Bone

- **WHEN** dense Mask或Blend Profile未覆盖全部Pose slot
- **THEN** 动画绑定 Build MUST失败
- **AND** Runtime MUST不补默认权重

#### Scenario: FBBIK binding来自旧Prefab组件

- **WHEN** Runtime Prefab仍依赖FinalIK BipedReferences或第二份骨骼映射
- **THEN** Definition validation MUST失败
- **AND** MUST不从该组件或Transform名称迁回Rig v4


### Requirement: Pose Graph工作区必须准确映射Authoring、Live与References

工作区 MUST继续区分 Authoring、Live 与 References，并通过稳定图／节点／端口／调用实例映射到原生运行结果。作者编辑只经正式 Mutation，Live 只读已完成状态；轻量校验与明确实例刷新分开，不能 selection 时编译、运行图或创建预览角色。

#### Scenario: 查看Locomotion State

- **WHEN** 作者选中Locomotion State的Clip或BlendSpace Player
- **THEN** Authoring MUST显示类型匹配的Source Slot，并在精确Profile上下文中显示其原生AnimationClip或Blend Space Binding
- **AND** References MUST显示实际资源、Slot／Group、owner与Open Resource命令
- **AND** MUST不显示BaseLocomotion Gameplay producer或可编辑Source Id

#### Scenario: Runtime revision不匹配

- **WHEN** snapshot revision与当前文档或动画绑定不一致
- **THEN** Live MUST显示Stale
- **AND** MUST不从authoring默认值或Animancer state伪造结果


### Requirement: Pose Watch必须只观察已完成Pose与typed目标Value

Pose Watch MUST只读取当前原生调用实例已完成的 Pose／typed 目标结果，保留坐标空间、Rig、source 和帧身份。无观察需求时 MUST不额外复制大页；观察 MUST不重新执行节点、不读 Pending 私有页或从 Transform 反推结果。

#### Scenario: 同时观察FootPlacement和FullBodyIK

- **WHEN** Frame开始时Foot Placement Goal与FullBodyIK Pose都启用Watch且Frame成功Seal
- **THEN** 两者 MUST来自同一Committed Graph与Constraint Result lineage
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

Pose Preview、正式角色和 Live Debug MUST使用同一个原生图 Factory、节点执行、资源与完成合同。适配器仅提供各自明确 Fact／World／动作输入；预览不得构造旧 Image、简化图或第二播放器。观察 MUST定位原生节点、端口、调用实例及已完成结果，不再次求值。

#### Scenario: Graph修改后继续Preview

- **WHEN** 作者修改State、Slot、Rig、Pose空间、Node Definition字段或Foot Placement使动画绑定变为Stale
- **THEN** Preview MUST停止消费旧原生图实例
- **AND** MUST不创建临时Program、旧ABI reader、默认空间转换或旧动画绑定 fallback

#### Scenario: Preview缺少world context

- **WHEN** Preview执行同一原生图实例到Foot Placement但精确World Context Adapter不可用
- **THEN** 原生图实例 MUST发布typed Unavailable并停止该Frame publication
- **AND** Preview MUST不使用简化Constraint或跳过该Operation


### Requirement: Pose authoring必须通过正式Capability和Presentation Mutation

Pose 图、状态、节点、端口和边 MUST继续使用共享 typed domain authoring。正式节点定义 MUST唯一提供业务字段、默认值、固定／动态端口、角色约束、局部规则和子图依赖给原生运行、Canvas、C# API、Clipboard、Mutation 与校验。系统 MUST不再要求 Operation Family、typed lowering 或独立 Compiler binding；跨节点约束由唯一图校验入口处理，运行缓存和内部索引不暴露给作者。

#### Scenario: 新增Pose节点能力

- **WHEN** 新Pose节点注册唯一Definition Adapter
- **THEN** 人工创建菜单、C#作者API、Clipboard、统一Port Shape、Validator、Graph Closure和图校验与绑定 MUST识别同一Capability与Payload合同
- **AND** MUST不要求在多个Catalog、Handler或NodeKind switch中重复声明同一字段和端口

#### Scenario: Node Definition缺少C#作者API投影

- **WHEN** 一个Definition无法为正式C#作者API/Mutation合同提供完整typed字段、条件端口或Graph dependency
- **THEN** Definition目录或Character Build MUST失败并定位Node Kind
- **AND** C#作者API MUST不使用通用SerializedProperty或自由文本绕过


### Requirement: Pose Graph作者资产必须与Character Presentation Binding分离

Pose 图资产 MUST可被多个角色复用，唯一保存作者节点、状态、端口、连接、业务配置与明确资源引用。Player MUST继续允许正式作者能力提供的直接动画资源或显式资源参数，不强制恢复 Source Slot／Profile Binding 两次选择。图 MUST不保存角色实例、World、可变运行状态或生成的程序；Rig 与角色级配置仍由明确 Profile／绑定拥有。

角色实例 MUST按原生图、明确参数、Rig 与资源创建只读运行绑定，不生成 Projection／Program 总包。缺失或不兼容绑定 MUST定位图／节点／字段，不得按名称、默认资源或数组位置猜测。单纯编辑可复用图不要求运行角色；实际预览和运行才要求完整绑定。

#### Scenario: 两个Character复用同一Pose Graph

- **WHEN** 两个Character Presentation Profile引用同一个可复用Pose Graph但提供不同的Rig和资源Binding
- **THEN** 系统 MUST按各自稳定Graph／Node／Field identity解析参数和资源，并建立独立只读实例绑定
- **AND** 共享图资产 MUST不被实例运行改写；角色专属参数和Rig绑定不得覆盖共享图的作者资源


### Requirement: Pose StateMachine layout必须是独立纯作者数据

每个root-owned PoseStateMachine MUST在`CharacterPresentationPoseGraphAsset`中拥有按稳定`PoseStateMachineId`索引的唯一layout owner。Layout MAY稀疏保存Entry、State与Alias的显式二维位置；缺少显式位置时 MUST按元素类型和稳定identity使用唯一确定性排布。Layout MUST拒绝重复identity、未知元素和非有限坐标，且 MUST不保存Transition edge位置。Layout变化 MUST进入typed Presentation Mutation、Undo、dirty、保存，并由正式C#作者API与人工UI使用同一语义，但 MUST不修改PoseStateMachine `ContentRevision`、不得使Presentation 动画绑定变为Stale，也不得触发Compile或Build。图校验与Runtime MUST不读取layout。

#### Scenario: 作者拖动Locomotion State

- **WHEN** 作者把Pose StateMachine中的Locomotion State拖到新位置
- **THEN** 系统 MUST通过Pose StateMachine layout Mutation保存该State的稳定identity与位置
- **AND** 重新打开工作区后 MUST从同一layout owner恢复位置
- **AND** Pose StateMachine运行语义与动画绑定 revision MUST保持不变

#### Scenario: 现有State没有显式位置

- **WHEN** 现有Pose StateMachine layout没有某个State的显式位置
- **THEN** 工作区 MUST按稳定identity使用唯一确定性位置
- **AND** MUST不在打开窗口、selection变化或AssetDatabase刷新时自动保存生成位置

#### Scenario: layout引用已删除State

- **WHEN** layout包含当前Pose StateMachine中不存在的State identity
- **THEN** Validator MUST报告悬空layout元素并拒绝正式提交
- **AND** MUST不忽略该元素或按显示名重绑定
