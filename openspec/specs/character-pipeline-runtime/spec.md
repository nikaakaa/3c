# character-pipeline-runtime Specification

## Purpose
定义 CharacterPipelineHost的 Unity Actor registration/Presentation边界，以及 portable Program Runtime、compiled Session Pipeline、Execution Backend、WorldSolver、Committer和 Presentation组成的唯一角色运行链。
## Requirements
### Requirement: CharacterPipelineHost 只负责装配和注册

CharacterPipelineHost MUST只加载并校验 CharacterPipelineDefinition对应的 CharacterSimulationProgramAsset与 Projection，建立显式 ActorId、World body binding、可选 local input、模型无关的 Gameplay output change port、Presentation output port和 diagnostics metadata，并向显式 SimulationSessionHost提供不可变 Actor registration。Gameplay output port MUST只记录当前 Tick 已提交的 Publish、Replace与 Retire变更，不得解释 history、correction、rollback或 Network Model策略。CharacterPipelineHost MUST不创建 ProgramCatalog、Session Source、WorldSolver、Program Runtime、Execution Backend、Pipeline Runtime、Snapshot codec、Committer aggregate或 Logic target，也 MUST不选择 Network Model或 Pipeline。

#### Scenario: 注册单机 Corin

- **WHEN** Sandbox中的 Corin CharacterPipelineHost启用
- **THEN** MUST向显式 SimulationSessionHost提交一个 Actor registration
- **AND** Local Source、标准 Pipeline、Float32 Backend与 Unity Solver MUST只由 Session composition创建

#### Scenario: Egress 提交 Gameplay 纠偏变更

- **WHEN** 当前 Session的 Egress OutputPlan包含 Gameplay Fact Replace或 Retire
- **THEN** Character Gameplay output port MUST记录对应 source EventId、target EventId、ActorId与可选 Fact
- **AND** Character Host MUST不因当前组合是 Local或 Network而拒绝或改写该生命周期变更

### Requirement: Character ActorId 必须由 Host 单点装配

每个 CharacterPipelineHost MUST显式提供唯一非空 ActorId，并在 Actor registration中绑定 ProgramId、ProgramHash、LayoutHash、World body与 Presentation identity。SimulationSessionHost MUST在 Active前验证完整 roster中 ActorId唯一且 binding精确匹配；Program operation、Projection、Solver、Pipeline Pass、Session Source与 Network Model MUST不生成替代 identity，Active后 MUST不修改 ActorId或 roster binding。

#### Scenario: Local Corin 注册

- **WHEN** Corin registration加入 Local Session launch plan
- **THEN** roster MUST使用该显式 ActorId、Program binding与 World body binding
- **AND** Session Host MUST不按 GameObject instance id、名称或数组 index生成 ActorId

### Requirement: 节点和 Timeline 不直接结算最终 Transform

Compiled Node 与 Timeline operation MUST只产生 state mutation、typed fact、MotionContribution 或 WorldRequest。只有 WorldSolver MAY改变 WorldSimulationState body，只有 Presentation adapter MAY写 visual root；两者 MUST不形成可反写的第二逻辑真值。

#### Scenario: Dodge Timeline 位移

- **WHEN** MotionCurve operation 产生 displacement
- **THEN** MUST经统一 WorldRequest batch取得 actual body result

### Requirement: Timeline 和动画 tick 权威归属 pipeline

Gameplay Timeline logic time MUST归Program与CharacterSimulationState并按SimulationTick推进；每个有限Action Sample command MUST只表达committed playback identity、raw visual time、cycle与time scale锚点，MUST不表达最终骨骼Pose或要求Player只在SimulationTick推进。有限Action projected visual time、PoseStateMachine、Sequence/BlendSpace/MM source clock、AnimationSlot、显式Player、Animancer source sampling与Pose Plan evaluation MUST归PresentationFrame。Pipeline Runtime MUST通过committed Body/Intent构造Presentation Fact，并通过有限Action producer/playback identity连接Timeline与Slot。Program MUST不读取PoseState、SequencePlayer、Slot或Pose Graph时间，Presentation MUST不推进Gameplay Timeline。系统 MUST不提供让同一Timeline在Gameplay-owned与Presentation-owned时钟之间切换的运行模式；有限Action与持续Pose source MUST由其正式authoring owner进入唯一对应链路。

#### Scenario: 无新 Logic Tick 的 RenderFrame

- **WHEN** PresentationFrame 到达但没有新 SimulationTick
- **THEN** Action visual time projector、PoseState source、Slot transition、Player与Pose Graph MAY按presentation delta继续推进
- **AND** Timeline Gameplay state与Action lifecycle MUST不改变

### Requirement: 不恢复 BBB 和旧 SO 数据源
系统 MUST NOT 将 BBB 的代码状态机或旧动作 SO/config 作为 `CharacterPipeline` 的数据主源。BBB 只能作为运行时组织参考。

#### Scenario: 参考 BBB
- **WHEN** 实现 `CharacterPipeline`
- **THEN** 系统 MAY 借鉴 BBB 的单入口、输入清洗、分阶段和帧末清理思想
- **AND** 系统 MUST NOT 复制 BBB `PlayerBaseState`、`PlayerStateRegistry`、`PlayerSO` 动作配置或 locomotion 特化状态类作为主链路

#### Scenario: 旧动作配置存在
- **WHEN** 项目中存在旧 locomotion、action、footphase、bodyclaim 或 animation presentation 配置
- **THEN** `CharacterPipeline` MUST NOT 从这些配置读取动作语义
- **AND** 动作语义 MUST 来自 BTSMTL Graph、NodeModule、Timeline 轨道或后续正式 runtime output

### Requirement: 角色管线路径使用 Character 命名
系统 MUST 将新角色 pipeline 代码放在正式 `Character` 命名路径中。系统 MUST NOT 继续扩展旧拼写 `Charactor` 路径。

#### Scenario: 新增 pipeline 文件
- **WHEN** 实现本能力
- **THEN** 新文件 MUST 位于 `Assets/GameScripts/Main/Runtime/Character/Pipeline`
- **AND** 新命名空间和类型名 MUST 使用 `Character` 或 `CharacterPipeline` 语义

#### Scenario: 旧空路径清理
- **WHEN** 旧 `Assets/Scripts` 或旧 `Charactor/Pipeline` 没有有效代码
- **THEN** 实现阶段 MUST 删除该旧路径
- **AND** 系统 MUST NOT 在该路径下新增新 runtime 文件

### Requirement: Simulation Session 必须是 GameplayTickSystem 的 logic target

GameplayTickSystem MUST只注册 SimulationSessionHost/runtime handle作为同一 Session的 Input/Logic target，不得为每个 Character、Pass、Session Source或 Network Model注册独立 LogicTick。Character Presentation target MAY按 Actor独立注册，但 MUST只消费当前 Session Committer发布的 samples/commands，并由 Session composition统一激活和释放。

#### Scenario: Session 包含两个 Actor

- **WHEN** fixed LocalLogicTick到达
- **THEN** GameplayTickSystem MUST只推进一次 Session runtime handle
- **AND** 两个 Character Presentation runtime MUST不各自推进 Gameplay Kernel或 Pipeline

### Requirement: Pipeline 输出必须按 Gameplay、Body、Presentation 与 Trace 分离

`SimulationTickResult` MUST按Actor类型化保存 `SimulationActorTickResult`；每个Actor result MUST分离GameplayFacts、`CharacterBodySample`、PresentationCommands与TraceRecords。Egress Pass与Committer MAY按正式产品/端口消费，MUST不让Presentation output反向改变Gameplay state，也 MUST不把packet或Pipeline私有状态写入结果。

#### Scenario: Attack Tick 输出

- **WHEN** Attack产生 Window、Motion和 animation command
- **THEN** Step Finalize MUST以独立 typed channels保存并共享同一 Event identity
- **AND** Egress MUST只决定外部 EventId disposition

### Requirement: CharacterPipelineDefinition 持有角色输入合同

CharacterPipelineDefinition MUST继续持有 InputProfile authoring identity；Compiler MUST将 InputId、value type、range、request policy 和量化规则写入 Program input catalog。Unity Input Adapter MUST引用同一 catalog转换设备输入，Kernel MUST不读取 InputProfile asset。

#### Scenario: 编译 Move Input

- **WHEN** Definition 引用合法 InputProfile
- **THEN** Program MUST包含对应 portable InputId/catalog

### Requirement: CharacterPipelineDefinition 提供 RootTree authoring context
系统 MUST 允许 editor 从 `CharacterPipelineDefinition` 打开 RootTree，并将 definition 和 input profile 作为 editor-only authoring context 传给 TreeWindow。该 context 只服务 authoring UI，不改变 runtime Graph 执行语义。

#### Scenario: 从 Definition 打开 RootTree
- **WHEN** 用户从 `CharacterPipelineDefinition` editor 打开 RootTree
- **THEN** TreeWindow MUST 获得当前 definition 和 `InputProfile`
- **AND** Input authoring 素材区 MUST 使用该 context 展示输入定义

#### Scenario: 多个 Definition 复用 RootTree
- **WHEN** 多个 `CharacterPipelineDefinition` 引用同一个 RootTree
- **THEN** Input authoring 素材区 MUST 使用打开入口传入的 definition
- **AND** 系统 MUST NOT 通过 AssetDatabase 反查猜测唯一 definition

### Requirement: Pipeline 输出事实必须通过 GameplayFacts 边界产生

Compiled Program MUST保持 `SimulationActorTickResult.GameplayFacts` 作为角色Gameplay事实边界。Blackboard variable MAY为Program operation提供运行上下文；只有显式合法fact projection才能产生 `ActionWindow` fact。Action、Effect、Attribute、Cue、Motion与State事实 MUST由各自正式operation生成；Presentation输出 MUST写入独立 `PresentationCommands`。Model Pass MUST只读取正式Tick result与Source products，MUST不直接读取Blackboard state。

#### Scenario: 投影 Action Window

- **WHEN** Window projection 收到合法 declaration、write provenance 与 Action Context
- **THEN** Finalize MUST生成 ActionWindow fact并写入 Tick result

#### Scenario: 写入 local-only 临时值

- **WHEN** operation 写入 Projection=None 的 Blackboard variable
- **THEN** 该值 MUST不进入 `SimulationActorTickResult.GameplayFacts`

### Requirement: Pipeline Blackboard 生命周期必须进入 frame cleanup

CharacterSimulationState MUST按Program layout为Frame、State、ActionInstance、Graph activation与Character生命周期维护typed owner generation。State、Action和Graph lifecycle operation MUST推进对应generation；Frame generation MUST等于当前SimulationTick。读取owner generation不匹配的Blackboard slot MUST返回declaration default且不物理修改State。生命周期终点 MUST使旧generation不可读、不可投影，MUST不依赖节点手动写null、CharacterGraphContext dictionary clear或遍历全部scope slot执行物理清零。

#### Scenario: Frame scope逻辑失效

- **WHEN** 当前SimulationTick Finalize完成并进入下一Tick
- **THEN** 新Frame generation MUST使上一Tick的Frame value按default读取
- **AND** 未在新Tick写入的Frame group MUST不产生dirty page

### Requirement: 角色管线必须保留跨 logic tick 的动画生命周期命令

SimulationCommitter MUST使用presentation-owned持久队列保存未消费的有限Action producer selection、sample、complete、release与EventId lifecycle。Queue MUST独立于transient Tick result，并按SimulationTick、event sequence与playback generation保序；queue MUST不保存Character/World mutable state。持续Locomotion PoseState、source relevance和transition MUST只存在于Presentation workspace，不得写入该Gameplay command queue。

#### Scenario: 一个 PresentationFrame 前多个 SimulationTick

- **WHEN** Committer 连续提交多个 generation
- **THEN** queue MUST保留Complete与Release顺序直到Presentation acknowledge
- **AND** MUST不为Body速度变化追加Run或Idle animation command

### Requirement: PresentationFrame必须输出完整最终Pose Plan结果

PresentationFrame MUST消费committed Body/Intent、构造typed Presentation Fact，并消费完整有限Action Selection batch与Parameter page；随后按Projection编译的ordered stage table执行PoseState selection、State source demand/capture、Action playback、Marker time resolve、AnimationSlot、Transition Routing、Local Pose composition、显式Local/Component转换、Component Pose骨骼控制、world-aware FootPlacement规划与pelvis输出、typed双腿targets、pure pose LegIK、后续Pose stage与FinalPublication。只有唯一OutputPose及全部必需stage完成后才可由唯一final writer发布`FinalAnimationPoseFrame`并推进Camera；任一Fact、source、MarkerSync、Player、Slot、转换、Pose operation、world query、Planner、targets validation或LegIK solver失败 MUST阻止部分最终结果发布，不得沿用上一帧、只发布pelvis Pose或绕过节点。

#### Scenario: FootPlacement targets与LegIK Pose不匹配

- **WHEN** 同帧targets CompletionIdentity或Rig revision与LegIK Component Pose输入不一致
- **THEN** PresentationFrame MUST阻断LegIK、后续stage和FinalPublication
- **AND** MUST不使用上一次targets或按节点顺序猜测配对

#### Scenario: 完整Foot Placement链成功

- **WHEN** FootPlacement发布合法pelvis Pose与targets且LegIK完成左右腿求解
- **THEN** FinalAnimationPoseFrame MUST包含LegIK输出及全部后续Pose操作
- **AND** Runtime MUST不保留第二Foot Placement或图外Leg IK结果

#### Scenario: Action等待第一Selection sample

- **WHEN** Program已经选择Action但Presentation尚无合法Selection sample
- **THEN** AnimationSlot MUST按compiled pending/availability policy处理
- **AND** Locomotion PoseState MUST继续来自同帧Fact而不是历史BaseLocomotion selection

### Requirement: Simulation Session 必须作为显式 diagnostics target

每个 Active Simulation Session与其 Actor roster MUST注册明确 diagnostics target/session identity，并提供 Program revision、Source Map、BackendId、PipelineId/Hash、compiled Pass order、SourceId、Solver identity、默认关闭的 Live/Capture store和只读 metadata。Editor MUST不持有 runtime Graph、mutable Character/World/Pipeline state、Pass runtime或 Solver object。

#### Scenario: Local Session 激活

- **WHEN** Corin Session完成创建
- **THEN** diagnostics registry MUST注册 Session/Actor target、ProgramHash与 PipelineHash
- **AND** MUST能显示当前标准 Local Pass顺序

### Requirement: Pipeline domain debug 必须进入统一 Trace

Input、ingress、Program operation、StateMachine、Timeline、Blackboard、WorldRequest/Result、Action、Effect、commit、Animation、Foot Placement和Camera diagnostics MUST进入统一 structured Trace/view model。Inspector MUST不遍历旧stage、Final IK组件或runtime service私有集合形成平行调试链。Foot Placement trace MUST只读取正式Presentation snapshot，不得重新执行地面查询或solver。

#### Scenario: 查看一次 Dodge Tick

- **WHEN** Debug Session 定位 Dodge EventId
- **THEN** MUST关联 input、operation、world batch 与 committed animation command

#### Scenario: 查看楼梯上的右脚replant

- **WHEN** Foot Placement snapshot记录右脚因超出reach从Locked释放
- **THEN** 统一Trace MUST显示同帧Body、visible producer、surface、constraint reason和pelvis offset
- **AND** Inspector MUST不直接读取Final IK mutable solver状态

### Requirement: Program Finalize 必须提交逻辑侧唯一动画选择

Program Finalize MUST在State、Action、interruption与Timeline request处理后，为每个有限Gameplay-owned `AnimationChannelId`最多产生一个selected producer/playback command。持续BaseLocomotion MUST不再是Program animation channel；其表现输入 MUST来自committed Body/Intent的Presentation Fact。Committer、Projection、Slot与Pose Graph MUST不重新仲裁同一Action channel候选，Program MUST不读取PoseStateId、PoseNodeId、Bone Mask、Slot或Pose Graph topology决定winner。

#### Scenario: FullBodyAction所有权冲突

- **WHEN** Program无法为FullBodyAction channel产生唯一Action selection
- **THEN** 当前 Tick MUST报告明确冲突
- **AND** Slot MUST不选择默认赢家

#### Scenario: Locomotion与Dodge并行

- **WHEN** Body正在移动且FullBodyAction选择Dodge
- **THEN** Program MUST提交Dodge command和普通Body结果
- **AND** Presentation MUST先求值Locomotion PoseStateMachine再由Slot组合Dodge

### Requirement: PresentationFrame必须原子提交动画播放与Pose节点生命周期

PresentationFrame MUST在同一外层事务中提交Presentation Fact page、PoseStateMachine active/target state、Sequence/Selection source usage、Marker relation/effective sample page、AnimationSlot state、BlendStack状态、Transition Routing capture/release、Inertialization、空间转换、Pose operation completion、world-aware plan、Component Pose solver结果和final publication。Reset、branch replacement或Projection replacement MUST按compiled stage与operation清理或重建全部stateful节点。Animancer Evaluate Barrier前失败 MUST只Discard Pending；stage失败已经跨过Barrier时 MUST阻断后续stage与final publication并使同一Actor Animation Presentation Runtime进入Faulted，不得恢复状态或Physical Bone快照。任何路径不得只提交Action playback、FootPlacement plan或中间Pose而保留旧Output。

#### Scenario: Action Selection与首个Sample同批

- **WHEN** 新Selection与首份合法source sample在同一PresentationFrame到达
- **THEN** Slot MUST原子初始化并参与本帧Pose Plan
- **AND** FinalAnimationPoseFrame MUST只反映该次完整事务结果

#### Scenario: World context在求解前失效

- **WHEN** FootPlacement query前精确PhysicsScene或world binding失效
- **THEN** transaction MUST阻断后续stage并保持final publication不可用
- **AND** 当前Actor Animation Presentation Runtime MUST进入Faulted
- **AND** MUST不发布上游未放脚Pose作为替代最终结果

### Requirement: Compiled Program 必须编排唯一 Gameplay Effect 阶段

Compiled Program MUST唯一拥有 GE catalog/operations，CharacterSimulationState MUST唯一拥有 GE state。Evaluate MUST开始并推进当前 Tick GE transaction，Finalize MUST唯一 drain change journal 并输出 facts；Host、Committer 与 Presentation MUST不持有第二份 GE runtime/state                              。

#### Scenario: Local Tick 推进 Effect

- **WHEN** Effect period 在当前 SimulationTick 到期
- **THEN** Program MUST在当前 Tick产生唯一 ChangeSet facts

### Requirement: Program Operation Execution Context 必须是唯一角色逻辑上下文

Kernel MUST为 operation提供只读 Program、SimulationTick、Actor input、SimulationIngress、Character state accessor、上一 body observation、typed output writer和 Source Map identity。Operation MUST不获得 Host、GameObject、Session Source、Pipeline Runtime/Pass、Execution Backend、WorldSolver、Presentation或 model session reference。

#### Scenario: Condition operation 读取输入与 Blackboard

- **WHEN** operation求值移动状态条件
- **THEN** MUST只通过 execution context的 portable input/state accessor读取

### Requirement: Program Runtime 与 Execution Backend 必须形成唯一纯 CSharp 运行主体

正式 Character gameplay runtime MUST由 portable Program Runtime、compiled Pipeline plan、Execution Backend runtime handle、Character/World/Pipeline state与明确 ports构成。可变 Gameplay state MUST不隐藏在 CharacterPipeline stage、RunnableNode clone、Timeline scheduler、GraphContext、Pass Definition或 Unity component内。Unity Host/Adapter MUST留在 composition boundary。

#### Scenario: 普通 DotNet Host 编译 Runtime

- **WHEN** 后续普通 .NET Host引用 Program Runtime与兼容 Execution Backend源码
- **THEN** MUST不需要 CharacterPipelineHost、ScriptableObject或 UnityEngine执行 Gameplay Program
- **AND** MUST使用同一 Pipeline descriptor与 Session transaction合同

### Requirement: Character Pipeline 必须通过可组合 Session Pipeline 执行

正式逻辑链 MUST收口为 `Ingress Passes -> one Schedule plan -> zero or more Step sequences -> Egress Passes -> atomic state publish -> Committer`。标准 Step Pass MUST调用唯一 Program/Kernel Evaluate、WorldSolver ResolveBatch与 Kernel Finalize；Graph、StateMachine、Timeline、Action、Effect和 Motion resolve MUST属于 Program/Kernel，world mutation MUST属于 WorldSolver，Network Model与 Presentation MUST位于正式 Source/Egress/Commit端口。Egress disposition MUST不决定 staged Gameplay state是否生效。

#### Scenario: 一个 Local Tick

- **WHEN** Standard Local Pipeline推进一个 SimulationTick
- **THEN** Step Pass MUST完成全部 Actor logic和一个 world batch
- **AND** Committer MUST只在 outer Pipeline transaction原子成功后处理副作用

#### Scenario: 一次纠偏执行多个内部 Tick

- **WHEN** 后续 Prediction Pipeline生成 restore和多个 replay/current step
- **THEN** 每个 step MUST复用相同 Kernel/Solver/Finalize Pass合同
- **AND** Replay中间输出 MUST不绕过 Egress与 Commit事务

### Requirement: Character Presentation 装配必须使用唯一 Target-Neutral Contract

每个Character Presentation Host或Remote Presentation Adapter MUST先严格加载所属Numeric Target Program或正式semantic producer manifest，再通过对应Adapter生成不可变`CharacterPresentationSemanticContract`。`CharacterPresentationProjectionAsset` MUST只提供一个按该contract加载Projection的Interface，并 MUST精确校验ProgramId、Gameplay SourceRevision、SemanticHash、ContractHash与ordered producer contract。Float32、Fixed、Rollback、Preview与Remote Presentation MUST不维护不同Projection匹配规则，也 MUST不按ProgramHash、NumericProfile或ABI选择Presentation资源。

Numeric Target Program MUST继续由ProgramAsset、Catalog与Session composition精确校验ProgramHash、LayoutHash、NumericProfile、Target ABI和State codec。Presentation contract校验 MUST不替代或放宽该Program校验。

#### Scenario: Float32 Host创建Presentation

- **WHEN** Float32 Host已严格加载Float32 Program
- **THEN** Float32 Adapter MUST生成Presentation contract并通过唯一Projection Load Interface创建Presentation
- **AND** Projection MUST不读取Float32 ProgramHash或ABI

#### Scenario: Fixed Host创建Presentation

- **WHEN** Fixed Host已严格加载Fixed Program
- **THEN** Fixed Adapter MUST生成与Frontend相同的Presentation contract并通过同一Projection Load Interface创建Presentation
- **AND** Host MUST不手工拼接producer identity数组或调用较弱校验分支

#### Scenario: Target Program producer contract不一致

- **WHEN** 任一Target Program的ordered producer contract与Projection ContractHash不一致
- **THEN** Character Host MUST在创建Presentation和注册Actor之前失败
- **AND** MUST不按ProgramId、名称、旧Projection或部分producer集合继续运行
