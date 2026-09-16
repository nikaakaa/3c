# character-pipeline-runtime Specification

## Purpose
定义 CharacterPipelineHost的 Unity Actor registration/Presentation边界，以及 portable Program Runtime、compiled Session Pipeline、Execution Backend、WorldSolver、Committer和 Presentation组成的唯一角色运行链。
## Requirements
### Requirement: CharacterPipelineHost 只负责装配和注册

CharacterPipelineHost MUST 只加载并校验 CharacterPipelineDefinition 的正式 Control、Ability、Timeline、Presentation、Input、Body 和 diagnostics binding，建立显式 ActorId、World body binding、可选 local input、模型无关的 Gameplay output change port、Presentation output port 和 diagnostics metadata，并向显式 SimulationSessionHost 提供不可变 Actor registration。Gameplay output port MUST 只记录当前 Tick 已提交的 Publish、Replace 与 Retire 变更，不得解释 history、correction、rollback 或 Network Model 策略。CharacterPipelineHost MUST 不创建 Session Source、WorldSolver、Execution Backend、Pipeline Runtime、Snapshot codec、Committer aggregate 或 Logic target，也 MUST 不选择 Network Model 或 Pipeline。

#### Scenario: 注册单机 Corin

- **WHEN** Sandbox中的 Corin CharacterPipelineHost启用
- **THEN** MUST向显式 SimulationSessionHost提交一个 Actor registration
- **AND** Local Source、标准 Pipeline、Float32 Backend与 Unity Solver MUST只由 Session composition创建

#### Scenario: Egress 提交 Gameplay 纠偏变更

- **WHEN** 当前 Session的 Egress OutputPlan包含 Gameplay Fact Replace或 Retire
- **THEN** Character Gameplay output port MUST记录对应 source EventId、target EventId、ActorId与可选 Fact
- **AND** Character Host MUST不因当前组合是 Local或 Network而拒绝或改写该生命周期变更

### Requirement: Character ActorId 必须由 Host 单点装配

每个 CharacterPipelineHost MUST 显式提供唯一非空 ActorId，并在 Actor registration 中绑定领域 identity、World body 与 Presentation identity。SimulationSessionHost MUST 在 Active 前验证完整 roster 中 ActorId 唯一且各领域 binding 精确匹配；Graph、Timeline、Solver、Pipeline Pass、Session Source 与 Network Model MUST 不生成替代 Actor identity，Active 后 MUST 不修改 ActorId 或 roster binding。

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

Gameplay Timeline logic time MUST 由正式 Timeline Runtime 按 SimulationTick 推进；每个有限 Action Sample command MUST 只表达 committed playback identity、raw visual time、cycle 与 time scale 锚点，MUST 不表达最终骨骼 Pose。有限 Action projected visual time、PoseStateMachine、Clip/BlendSpace/MM source raw clock、source-local Phase endpoint、AnimationSlot、显式 Player、Animancer source sampling 与原生 Pose Graph evaluation MUST 归 PresentationFrame。Pipeline Runtime MUST 通过 committed Body/Intent 构造 Presentation Fact，并通过有限 Action playback identity 连接 Timeline 与 Slot。Presentation MUST 不推进 Gameplay Timeline。系统 MUST 不提供让同一 Timeline 在多个 owner 时钟之间切换的运行模式；有限 Action 与持续 Pose source MUST 由各自正式 owner 进入唯一对应链路。

#### Scenario: 无新 Logic Tick 的 RenderFrame

- **WHEN** PresentationFrame 到达但没有新 SimulationTick
- **THEN** Action visual time projector、PoseState source raw clock、Phase endpoint、Slot transition、Player与Pose Graph MAY按presentation delta继续推进
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

CharacterPipelineDefinition MUST 继续持有 InputProfile authoring identity；正式 Input owner MUST 将 InputId、value type、range、request policy 和量化规则提供给 Input Adapter。Unity Input Adapter MUST 引用同一正式输入合同转换设备输入，Kernel MUST 不读取 InputProfile asset。

#### Scenario: 准备 Move Input

- **WHEN** Definition 引用合法 InputProfile
- **THEN** Input owner MUST 提供对应 portable InputId/catalog

### Requirement: CharacterPipelineDefinition 提供正式 Graph authoring context
系统 MUST 允许 editor 从 `CharacterPipelineDefinition` 打开正式 Graph/Ability authoring，并将 definition、Ability grant 和 input profile 作为 editor-only authoring context 传给对应 Graph window。该 context 只服务 authoring UI，不创建 Character RootTree、不拥有运行时状态，也不改变正式 Graph Runtime 语义。

#### Scenario: 从 Definition 打开 Graph
- **WHEN** 用户从 `CharacterPipelineDefinition` editor 打开正式 Graph/Ability authoring
- **THEN** GraphWindow MUST 获得当前 definition、Ability grant 和 `InputProfile`
- **AND** Input authoring 素材区 MUST 使用该 context 展示输入定义

#### Scenario: 多个 Definition 复用 Graph
- **WHEN** 多个 `CharacterPipelineDefinition` 引用同一个正式 Graph
- **THEN** Input authoring 素材区 MUST 使用打开入口传入的 definition
- **AND** 系统 MUST NOT 通过 AssetDatabase 反查猜测唯一 definition

### Requirement: Pipeline 输出事实必须通过 GameplayFacts 边界产生

正式 Gameplay Runtime MUST 保持 `SimulationActorTickResult.GameplayFacts` 作为角色 Gameplay 事实边界。Graph variable MAY 为 Graph Runtime 提供运行上下文；只有显式合法 fact projection 才能产生 `ActionWindow` fact。Action、Effect、Attribute、Cue、Motion 与 State 事实 MUST 由各自正式 owner 生成；Presentation 输出 MUST 写入独立 `PresentationCommands`。Model Pass MUST 只读取正式 Tick result 与 Source products，MUST 不直接读取 Blackboard state。

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

PresentationFrame MUST 消费 committed Body/Intent、构造 typed Presentation Fact，并消费完整有限 Action playback batch 与 Parameter page；随后按正式 Pose Graph、Source、Constraint 和 Final Publication 顺序执行 PoseState selection、State source demand、source-local Phase resolve、source capture、Action playback、AnimationSlot、Transition Routing、Local Pose composition、显式 Local/Component 转换、Component Pose 控制、FootPlacement 与 PoseBone Goal Contribution、唯一 Goal Assembler、唯一 FullBodyIK、后续 Pose stage 与 FinalPublication。只有唯一 OutputPose 及全部必需阶段完成后才可由唯一 final writer 发布 `FinalAnimationPoseFrame` 并推进 Camera；任一 Fact、source、Phase endpoint、Player、Slot、转换、Pose 节点、world query、Goal Contribution、Goal Assembly 或 FullBodyIK 失败 MUST 阻止部分最终结果发布，不得沿用上一帧、只发布 pelvis Pose 或绕过节点。

#### Scenario: Goal Contribution与Component Pose lineage不匹配

- **WHEN** 同帧Goal Contribution的CompletionIdentity或Rig revision与FullBodyIK Component Pose输入不一致
- **THEN** PresentationFrame MUST阻断Goal Assembly、FullBodyIK、后续stage和FinalPublication
- **AND** MUST不使用上一次Goal或按节点顺序猜测配对

#### Scenario: 完整Goal链成功

- **WHEN** FootPlacement与PoseBone目标源发布合法Contribution、唯一Assembler形成Goal Set且FullBodyIK完成求解
- **THEN** FinalAnimationPoseFrame MUST包含唯一FullBodyIK输出及全部后续Pose操作
- **AND** Runtime MUST不保留第二Goal Set、第二FullBodyIK或图外骨骼结果

#### Scenario: Action等待第一Sample

- **WHEN** Program已经选择Action但Presentation尚无合法playback sample
- **THEN** AnimationSlot MUST按compiled pending/availability policy处理
- **AND** Locomotion PoseState MUST继续来自同帧Fact而不是历史BaseLocomotion selection

### Requirement: Simulation Session 必须作为显式 diagnostics target

每个 Active Simulation Session与其 Actor roster MUST注册明确 diagnostics target/session identity，并提供 Program revision、Source Map、BackendId、PipelineId/Hash、compiled Pass order、SourceId、Solver identity、默认关闭的 Live/Capture store和只读 metadata。Editor MUST不持有 runtime Graph、mutable Character/World/Pipeline state、Pass runtime或 Solver object。

#### Scenario: Local Session 激活

- **WHEN** Corin Session完成创建
- **THEN** diagnostics registry MUST注册 Session/Actor target、ProgramHash与 PipelineHash
- **AND** MUST能显示当前标准 Local Pass顺序

### Requirement: Pipeline domain debug 必须进入统一 Trace

Input、ingress、Graph runtime、StateMachine、Timeline、Blackboard、WorldRequest/Result、Action、Effect、commit、Animation、Foot Placement 和 Camera diagnostics MUST 进入统一 structured Trace/view model。Inspector MUST 不遍历旧 stage、Final IK 组件或 runtime service 私有集合形成平行调试链。Foot Placement trace MUST 只读取正式 Presentation snapshot，不得重新执行地面查询或 solver。

#### Scenario: 查看一次 Dodge Tick

- **WHEN** Debug Session 定位 Dodge EventId
- **THEN** MUST关联 input、operation、world batch 与 committed animation command

#### Scenario: 查看楼梯上的右脚replant

- **WHEN** Foot Placement snapshot记录右脚因超出reach从Locked释放
- **THEN** 统一Trace MUST显示同帧Body、visible producer、surface、constraint reason和pelvis offset
- **AND** Inspector MUST不直接读取Final IK mutable solver状态

### Requirement: Gameplay/Ability Finalize 必须提交逻辑侧唯一动画选择

正式 Gameplay/Ability owner MUST 在 State、Action、interruption 与 Timeline request 处理后，为每个有限 Gameplay-owned `AnimationChannelId` 最多产生一个 selected producer/playback command。持续 BaseLocomotion MUST 不属于有限 Action channel；其表现输入 MUST 来自 committed Body/Intent 的 Presentation Fact。Committer、Slot 与 Pose Graph MUST 不重新仲裁同一 Action channel 候选，Gameplay owner MUST 不读取 PoseStateId、PoseNodeId、Bone Mask、Slot 或 Pose Graph topology 决定 Gameplay winner。

#### Scenario: FullBodyAction所有权冲突

- **WHEN** Gameplay/Ability owner 无法为 FullBodyAction channel 产生唯一 Action selection
- **THEN** 当前 Tick MUST报告明确冲突
- **AND** Slot MUST不选择默认赢家

#### Scenario: Locomotion与Dodge并行

- **WHEN** Body正在移动且FullBodyAction选择Dodge
- **THEN** Gameplay/Ability owner MUST 提交 Dodge command 和普通 Body 结果
- **AND** Presentation MUST先求值Locomotion PoseStateMachine再由Slot组合Dodge

### Requirement: PresentationFrame必须原子提交动画播放与Pose节点生命周期

PresentationFrame MUST 在同一外层事务中提交 Presentation Fact page、PoseStateMachine active/target state、Clip/BlendSpace/MM source usage、Phase relation/effective sample page、AnimationSlot state、BlendStack 状态、Transition Routing capture/release、Inertialization、空间转换、Pose 节点 completion、world-aware plan、Component Pose solver 结果和 final publication。Reset、branch replacement 或 Graph revision replacement MUST 按正式 owner 清理或重建全部 stateful 节点。Animancer Evaluate Barrier 前失败 MUST 只 Discard Pending；stage 失败已经跨过 Barrier 时 MUST 阻断后续 stage 与 final publication 并使同一 Actor Animation Presentation Runtime 进入 Faulted，不得恢复状态或 Physical Bone 快照。任何路径不得只提交 Action playback、FootPlacement plan 或中间 Pose 而保留旧 Output。

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

GameplayEffect owner MUST 唯一拥有 GE catalog/operations，CharacterSimulationState MUST 唯一拥有 GE state。Evaluate MUST 开始并推进当前 Tick GE transaction，Finalize MUST 唯一 drain change journal 并输出 facts；Host、Committer 与 Presentation MUST 不持有第二份 GE runtime/state。

#### Scenario: Local Tick 推进 Effect

- **WHEN** Effect period 在当前 SimulationTick 到期
- **THEN** Effect owner MUST 在当前 Tick 产生唯一 ChangeSet facts

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

Numeric Target domain data MUST 继续由正式 asset/binding、Catalog 与 Session composition 精确校验 NumericProfile、Target ABI 和 State codec。Presentation binding 校验 MUST 不替代或放宽 Gameplay/Ability/Timeline domain 校验。

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
