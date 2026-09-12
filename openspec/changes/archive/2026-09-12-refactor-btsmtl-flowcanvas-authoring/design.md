## Context

> 2026-09-12 已拆分替代归档。以下设计保留历史上下文，实际实施以[交接记录](split-handoff.md)中的后继change为准，不再从本目录执行apply或同步delta。

2026-09-12 FSM范围更新：本change负责第9—11节原生FSM作者迁移、对应Document v8和编译适配、Corin清理；普通执行图继续用FlowCanvas，状态机子图唯一使用NodeCanvas FSM。共同业务参数/定义消费unify-skill-authoring-data-model成果；既有转移线与旧Step迁移消费add-skill-transfer-connections成果，不重复实施已经完成的工作。此前把这些工作全部交给数据层change的说明已撤销，本次不修改其它窗口文档；跨change剩余旧交接文字列为归并差异。

本变更的正式对象是Character技能，不是AI Controller，也不是PoseGraph本体。Character RootTree已经从正式资产和Definition合同中删除；Character主线由C# ControlModule、编译后的Character Program以及Simulation Pipeline执行。AI自研RootTree已由独立`replace-btsmtl-ai-with-behavior-designer` change退役，Behavior Designer不进入本Skill Document或BTSMTL Graph。

当前角色技能作者源由`CharacterPipelineDefinition.SkillGraphs`和`CharacterSkillAuthoringDefinition.EntryGraphAuthoringId`组成。Skill Graph由FlowCanvas保存，运行时只消费Semantic IR和Numeric Program，不启动FlowCanvas委托、协程或自动Update。Character Program进入`SimulationSessionCompositionDefinition`，再由ProgramRuntime、ExecutionBackend、WorldSolver、SimulationPipeline和SessionSource组成正式Session。

项目已有Float32、Fixed、Server Authority和Deterministic Rollback路径。网络相关行为通过Session Source、Pipeline Pass和Adapter接入，不能把Character authoring或Skill Graph当作网络边界。本文借鉴GAS的职责划分，不引入Unreal对象模型、AbilitySystemComponent或其复制实现。

## Goals / Non-Goals

**Goals:**

- 一个GA式Skill只有一个稳定Entry Graph，SkillDefinition承担激活和业务合同，FlowGraph承担执行体。
- Skill Graph、Macro、State、Condition、Timeline和Blackboard使用同一正式作者拓扑、owner和Mutation事务。
- Skill Timeline保留类似Montage/AbilityTask的有限动作时序，并向Presentation/PoseGraph发出播放合同。
- 将变量按C# Character State、Ability Attribute/GameplayEffect、GameplayTag、Input/TargetData、Skill Local Blackboard、Activation/Frame Scope分层，不建立万能共享Blackboard。
- Character Program与Simulation Pipeline解耦，使Rollback、Server Authority和其他正式网络Adapter可以复用同一编译产物。
- Document v8直接读写正式FlowGraph/FSM闭包，保持稳定身份、owner、hash、回滚和反向导出；当前v7只作为迁移前基线。

**Non-Goals:**

- 不恢复Character RootTree，不把角色主线重新塞回图资产。
- 不把Character ControlModule、Movement Runtime或PoseGraph改造成Skill Graph。
- 不直接移植UE GAS运行时；只采用Ability、Attribute、Tag、Effect、ActivationData和Montage式Timeline的职责划分。
- 不让Skill直接写Velocity、BodyYaw、最终Pose、网络传输对象或任意C#字段；这些必须通过typed contract或正式Command。
- 不在本文实现Behavior Designer任务接入、Pose Graph拓扑迁移、IK算法重写或具体网络传输实现。

## Decisions

### 1. GA外壳与单根执行图

`CharacterSkillAuthoringDefinition`是GA式外壳，保存`SkillId`、`EntryGraphAuthoringId`、ActionProfile、ActionContext、输入、目标、替换和后续技能关系。一个Skill只能有一个Entry Graph。复杂流程通过Macro、State、Condition和Timeline闭包组织，不提供多个技能执行根。

Skill Graph的节点、端口和连接是唯一正式技能拓扑。编译、Document、观察和编辑器都直接读取该拓扑。旧BaseGraph只能作为未迁移其他领域的合法数据源，不能作为Character Skill转换中间层。

取舍：单根让网络请求、ActionInstance、generation、SourceMap和观察入口有唯一根；多入口虽然能减少某些图跳转，但会使激活身份、停止传播和网络重演出现第二条解释路径，因此拒绝多执行根。

### 2. 变量采用GAS式职责分层

Blackboard面板是多个正式provider的统一视图，不是一个物理万能字典。

| Provider | 内容 | Skill访问方式 |
|---|---|---|
| Character State | C# ControlModule和Movement Runtime事实 | 只读Get；修改走Control Command |
| Ability Attribute | 会被效果修改的数值资源、层数、冷却 | Get；Set通过GameplayEffect/typed effect command |
| GameplayTag | 激活、阻断、取消和状态标记 | Tag Query/Tag Command |
| Input/TargetData | 输入值、Action Request、Target Snapshot | 只读Get |
| Skill Local Blackboard | 当前Skill私有值 | Get/Set |
| State/ActionInstance | 当前状态或一次激活的局部值 | 按生命周期Get/Set |
| Frame Fact | Hit、IFrame、ComboAccept、Recovery窗口 | Timeline写，Skill读 |
| Presentation | Animation Playback/Producer Request | Emit，不直接写Pose |

每个可见字段必须具有owner、稳定ID、类型、读写权限、生命周期、预测和回滚合同。节点保存`ownerId + declarationId`，不得按显示名、路径扫描或反射猜测变量。

C#字段的编辑器投影只声明typed contract；它不是把C#字段复制到另一个Blackboard。Character State默认只读，Skill需要影响角色主线时提交`SubmitMotion`、`SubmitSkill`、`SubmitSkillStop`或其他正式Command。

跨GA共享数据优先使用Attribute、GameplayTag、GameplayEffect和ActivationData。若确实存在无法归类的复杂共享结构，必须新增明确的typed Ability State provider，不能把它藏进某个Skill Graph，也不能建立任意key-value共享黑板。

### 3. Character Movement保持独立

C# ControlModule负责移动规则、状态转换、输入解释和技能请求；Simulation Kernel、KCC和Motion Runtime负责速度、重力、碰撞、朝向和最终身体结果。它们共同承担类似UE CharacterMovementComponent的职责，但分成策略层和确定性求解层。

Skill可以读取Velocity、BodyYaw、Grounded、MovementMode和CurrentMoveDirection等事实。Skill不能直接设置Velocity或BodyYaw；需要改变Movement时必须提交正式Command，进入同一Tick、Program State、Rollback和Authority规则。

只有真正会被GameplayEffect修改的数值才进入Ability Attribute。基础MoveSpeed、TurnRate等控制配置仍属于ControlModule或Pipeline Config；当前帧方向、身体朝向和实际速度属于Movement Runtime事实，不作为普通Attribute或Skill Local Blackboard变量。

### 4. Timeline是Montage式动作合同

Skill Timeline可以拥有AnimationTrack、AnimationClip、Action Slot、播放模式、Blend In/Out请求、动作窗口和取消/停止时序。这是GA内部的有限动作时序合同，职责类似UE AbilityTask播放Montage并等待完成、混出、打断或取消；它不是第二个PoseGraph或通用Sequencer，不拥有Locomotion、IK或最终Pose混合。AnimationTrack和AnimationClip是动作播放请求的作者输入，不能被解释为Skill直接写骨骼。

Timeline编译为Program Operation和Animation Producer/Playback Request。Presentation Projection和PoseGraph负责Locomotion混合、Animation Layer、Mask、IK和最终Output Pose。Skill不得直接执行Pose混合或写骨骼。

作者层继续使用原生Timeline编辑器维护Timeline、Track、Clip和Producer的稳定身份；编译层使用TimelineAuthoringId、Track/Clip identity、Producer identity和Program SourceMap，不额外制造没有消费者的播放ID。GraphEditor只保存和打开Timeline引用，Timeline页面不进入Graph breadcrumb。

### 5. 直接编译和运行隔离

编译链为：

```text
Skill FlowGraph / nested native FSM
  -> read-only occurrence
  -> Semantic IR
  -> Float32/Fixed Numeric Program
  -> Simulation Session
```

`BtsmtlSkillGraphCompiler`负责节点、边、Macro、State、Timeline和TreeClip的语义发射。Blackboard声明进入统一Program State Layout；ActionInstance、GraphInvocation、generation和父调用代次进入同一运行身份链。

运行时不读取作者图、不调用FlowCanvas getter、不启动Graph、不复制作者对象。编辑器观察只消费正式运行诊断、SourceMap和只读快照。

### 6. Character Program与Simulation Pipeline分离

Character Pipeline负责从Definition和Skill Graph生成Program与Actor Registration，不负责网络传输。

`SimulationSessionCompositionDefinition`选择：

- ProgramRuntime
- ExecutionBackend
- SimulationPipeline
- SessionSource
- WorldSolver

`SimulationPipelineDefinition`按照Ingress、Schedule、Step、Egress组织Pass。Pass由`SimulationPipelineCompiler`检查Program、Backend、WorldSolver、SnapshotCodec、ExecutionSupport、SourcePort和Product Contract的兼容性后形成Compiled Pipeline Plan。

网络适配发生在SessionSource、Pipeline Pass和Runtime Adapter层。Rollback使用InputIngress、Schedule、History、HashEgress、Snapshot和OutputDisposition等Pass；Server Authority使用自己的Authority、Prediction、Correction和Egress Pass。Skill和Character Program不绑定具体Transport或UE网络对象。

正式兼容检查沿用同一`SimulationSessionCompositionCompatibility.Evaluate`。`btsmtl.validate`可选接受一个精确`composition_asset_path`，把ProgramRuntime、ExecutionBackend、Pipeline、SessionSource、WorldSolver、Required Pass/Source Port及Network Model identity的结果加入只读报告；该参数只服务校验，不扫描目录、不修改Composition、不Build或Play。

### 7. 网络同步策略

网络同步输入、Canonical Request、Prediction身份、确定性Program State、State Hash和Snapshot，不复制FlowCanvas图、Blackboard名称、Timeline对象、AnimationClip对象或最终Pose。

静态Definition、Program和Presentation Projection通过Program/World/Pipeline identity和hash握手确认兼容。Skill激活由ActionInstance、PredictionKey和generation标识；可写Attribute、Tag、Effect、State和Local Blackboard值必须进入相应Program State、Snapshot和Hash路径。

因此网络可替换指更换SessionSource和Pass/Adapter，而不是允许每种网络拥有一套不同的Skill执行语义。Rollback和Server Authority共享Program合同，但各自拥有不同的输入确认、纠正和恢复策略。

### 8. Document、编辑器和事务

Skill Graph、原生FSM、Macro、Timeline、TreeClip、Blackboard declaration、Attribute/Tag provider引用和调用闭包均进入Document v8的稳定owner、hash和Mutation合同。人工编辑和Document apply必须使用同一能力目录、类型校验、真实owner、Undo和失败回滚。

#### 8.1 Skill Graph使用FlowCanvas原生作者表面

Skill Graph的唯一UI宿主是FlowCanvas原生`GraphEditor`。原生canvas、Toolbar、Blackboard、节点/连线Inspector、创建菜单、变量拖拽、selection、clipboard、Undo和Graph下钻保持启用；Skill domain adapter只向这些原生扩展点提供provider目录、typed字段、业务命令和只读诊断。adapter不得通过旁路UI重新实现Blackboard、Inspector、selection或创建流程，也不得设置自定义domain panel使原生Panels提前退出。

Blackboard和Inspector中的provider分组必须来自同一Capability与provider schema。当前图内创建变量只创建Skill Local declaration；Character State、Attribute、Tag、Input和Target provider由其正式schema提供，Skill只能引用或提交明确Command。拖拽变量创建Get/Set时必须写入ownerId和declarationId，不得只写显示名。该UI决策沿用真实owner、Mutation和Undo，FSM适配遵守同一规则。

### 9. 原生FSM作为状态机子图的唯一作者资产

本节是用户确认后的待实施目标，不表示当前代码已经使用NodeCanvas FSM。普通Skill根、StateBody、ConditionRule、Macro与TimelineBody继续使用FlowCanvas；只有StateMachine子图改为NodeCanvas原生`FSM`作者资产，State与Transition分别由`FSMState`和`FSMConnection`的正式领域适配承载。所有页面仍在同一CanvasCore原生`GraphEditor`中导航，不另建状态机窗口，也不保存同一状态机的FlowGraph镜像。

当前源码仍使用`BtsmtlSkillFlowGraph`的StateMachine role及自定义State节点。当前Corin包的Attack状态机已有5个状态、20条边，其中15条指向同一`@exit`；边已保存condition、priority、abortPolicy和order，状态节点已没有转移steps。包manifest仍标v7。此证据说明已有连接迁移工作，不能再从旧截图恢复steps，也不能据此认定原生FSM或新协议已经完成。

业务取舍：原生FSM让作者直接使用成熟的状态与连线交互；保留自定义FlowCanvas状态图也能表达同样业务，迁移成本较低。本次选择原生FSM，承担插件扩展、资产类型和编译读取适配成本，换取状态机作者入口统一。普通组合流程不迁成FSM状态，因为等待、顺序和并行不是状态转移的同义表达。

#### 9.1 唯一owner与四层数据方向

| 层 | 正式输入与输出 | 唯一职责 |
|---|---|---|
| 作者 | GraphEditor修改Unity正式FlowGraph/FSM/Timeline资产 | 每个图只保存一份拓扑，每个实例只保存一份业务参数 |
| Document | checkout从资产导出；apply将目标经正式Mutation写回资产 | 工作副本、完整闭包、hash、Undo、失败回滚与canonical reverse export |
| 编译 | 显式Build读取同一正式作者资产，生成Semantic IR与Numeric Program | 读取业务定义、状态/转移、生命周期与来源身份，不调用插件执行器 |
| 运行 | Program进入现有Simulation Pipeline和WorldSolver | 状态、Action、Timeline和运动仍由现有Program/Session合同执行 |

Skill根保持独立FlowGraph主资产。私有FSM由调用它的state-machine节点唯一拥有，保存于该Skill或共享Macro实际所属文件；FSMState唯一拥有StateBody引用，FSMConnection唯一拥有私有ConditionRule引用。删除、复制、语义hash与事务owner收集使用同一正式引用合同。原生图类型不同不产生另一套作者真相；Document更不是第二份正式图，也不新增JSON直编入口。

#### 9.2 原生对象必须显式映射业务语义

| 原生对象或能力 | BTSMTL映射 | 必须处理的差异 |
|---|---|---|
| FSM | `state-machine`子图、稳定graph identity及调用owner | 禁止StartGraph、自动Update和插件GraphOwner运行 |
| FSMState | `state` identity与唯一StateBody引用 | 状态执行体仍编译为现有进入、主体、退出与stop合同 |
| FSMConnection | 稳定edge identity、端点和唯一Transfer参数 | condition、priority、abortPolicy、order不得再存于State steps或图外列表 |
| Prime State / Entry | 默认入口或显式入口路由 | Prime只够表达单一默认状态；带条件/多分支入口保留正式路由和顺序，不额外存第二份默认目标 |
| Any / Exit | `@any`路由与同一FSM的`@exit`锚点 | 任意状态无条件转移继续拒绝；Exit是退出当前调用，不是一个执行Action的普通状态 |
| OnFSMEnter / OnFSMExit | FSM整体进入/退出钩子的作者表达 | 它们不是入口/出口转移端点，也不是每个FSMState的OnEnter/OnExit；没有业务钩子时不生成空包装 |
| ActionList / ConditionTask | 仅接受有正式定义、typed读取和编译binding的作者任务 | 未登记插件任务拒绝创建/导入/编译，不由插件代执行；引用Body/Condition图时任务只保存该引用，不另存同义动作/条件列表 |
| Sequence / Selector / Parallel | FlowCanvas执行体中的现有组合能力 | 保留步骤条件、顺序、并行更新和中止，不改成原生FSM执行规则 |

原生`FSMState.CheckTransitions`把没有ConditionTask的边解释为状态完成后转移，不能套用到BTSMTL无条件边；`state-root-completed`继续作为明确条件。原生Stacked/Clean call mode不等同于BTSMTL abortPolicy，不开放未登记的栈调用语义。进入目标前仍按BTSMTL既有顺序停止源State主体、执行OnExit及Action/Timeline清理；外层取消必须保留StateExitContext和停止传播。

原生`OnFSMEnter`没有入出连线且会执行ConditionList/ActionList，不能拿来替换`@enter`锚点。若提供该原生生命周期表面，只通过正式领域任务引用相应执行体并编译；同一个钩子不得既保存在Task列表又保存在另一份可写Graph配置中。

插件`FSMNode.outConnectionType`固定为FSMConnection，FSM也限制变量拖拽。实施需在原生扩展点提供正式连接参数、能力过滤、typed Inspector和provider接入，并登记必要插件补丁；不能用图外可写字典绕过连接限制，不能重建旁路Blackboard/Inspector。禁止在OnInspectorGUI里执行闭包迁移、Build或其它重操作。

#### 9.3 Document v8接入

本change为FSM最终合同承担v8原子切换：保留Skill根与普通图分片，FSM仍通过稳定业务role、kind和逻辑端点表示，禁止暴露插件C#类型/委托/运行状态。条件图owner明确为`kind=edge`、`graphId`、`edgeId`、`referenceKey=condition`；StateBody由State节点拥有，FSM整体生命周期执行体由唯一系统钩子节点拥有。新实体继续使用local identity并由同一apply反向导出为稳定identity。

Graph类型、引用与钩子能力均来自正式领域定义和graph catalog，不在Agent内重写一份FSM schema规则。非转移边不得携带Transfer参数；同一来源order唯一；导出、创建、编辑、复制、删除、校验与编译必须闭合后才开放能力。五个生命周期入口一次切换到v8，拒绝v7及更早包；旧DocumentDirty/Conflict先保留差异并裁决，再重新checkout，不静默覆盖。旧资产转换只在正式事务的显式迁移计划中使用，完成后删除，不成为正常reader。

业务取舍：内部改基类本可不升版本，但本目标同时明确edge owner/order和FSM钩子合同，升级整包使非法旧目标能明确拒绝；代价是旧工作包需重新checkout。不能把“manifest仍为v7的新字段包”当作兼容格式继续接受。

#### 9.4 编译、观察与迁移合同

编译读取器须从目前全量强转FlowNode/BinderConnection改为读取共同业务定义和正式图引用。FlowGraph与FSM适配分别读取各自真实资产，汇入同一只读编译输入与既有lowering；不得先重建旧自定义FlowGraph再编译。运行观察只读取真实Program诊断，映射至原生State/Connection，不启动FSM获取状态或向作者资产写执行结果。

迁移保留可保留的graph/node/edge identity、调用路径、条件、priority、abortPolicy、显式order及SourceMap来源；原生临时数组顺序、位置或随机UID不重新决定转移顺序。必须保留逻辑端点合同并登记到原生连接的映射，不能复制一份FlowCanvas端口对象到FSM资产。仅当Unity资产对象身份确实不能保留时，在同一事务中完整重映射引用并报告前后关系，不能按名称修复。

已有Edge目标是迁移来源，旧Step只允许在真实旧资产尚未迁移时通过一次性转换读取。两者矛盾必须报告并裁决，不以本计划覆盖其它窗口正确改动。成功后删除Skill专用旧StateMachine role实现、自定义State/Entry/Any/Exit存储、旧连接适配与一次性转换入口；仍被普通FlowGraph或合法非Skill领域使用的类型不能整文件误删。正常入口只接受新正式状态机，不保留双读、双写或兼容开关。

### 10. Corin Skill残余清理与变量owner

本节按精确Corin Definition闭包执行，不按节点名称全局删除。当前磁盘包是只读盘点证据，正式实施前重新核对Unity与Document同步状态；历史Clean/Build记录不证明这次目标已应用。

| 对象 | 目标 | 保留的业务 |
|---|---|---|
| Attack Startup Branches、OnEnter Action Setup、Clear Directional Dodge Run Intent | 删除三个实例、相关连线和失去引用的私有空条件图/layout；Root直接连接Attack Combo StateMachine | 攻击主体与原状态机identity |
| Attack图内HasDirectionalDodgeRunIntent声明 | 与唯一Set一起删除；它不是ControlModule字段 | C#控制状态仍由正式Program State拥有 |
| Attack1至Attack5及15条退出边、4条连段边 | 保留条件、目标、优先级、中止和顺序，改善连接摘要 | 自然完成、恢复前段闪避取消、恢复后段移动取消及连段准入 |
| Dodge的Startup Branches | 保留Timeline Body与Exit Monitor的有效并行；仅清理无消费Setup支路 | Timeline执行期间仍能监控取消/完成，不能整体换成Sequence |
| Dodge Enter/Set与图内同名意图声明 | 对所有typed引用确认只有写入后删除私有闭包 | 不移动ControlModule中DodgeForward完成后置位的时机 |
| Hit/IFrame/Recovery窗口 | 保留Timeline、Frame Fact和ActionContext消费 | 没有Get节点不等于无消费，需覆盖Timeline引用 |
| ActionTarget | 保留Attack实际targetSnapshot消费者；Dodge声明先核对输入绑定和编译消费 | 目标属于正式输入/激活数据，不降成任意Skill Local值 |

退出连接摘要按真实条件与目标显示，例如“主体完成”“恢复前段：可闪避且收到闪避请求”“恢复后段：移动输入超过阈值”“连击窗口：进入Attack2”。同一`@exit`不复制为多个状态；显示改进不引入第二份条件、排序或退出原因运行参数，也不删除CanActivateAction准入。

StopThreshold目前由Attack根声明，12处条件引用中包含两个Dodge取消条件，Dodge自己的同名声明无直接引用。该现状不允许直接删除Attack声明。配置归属属于尚未授权改变的业务：统一ControlModule阈值可让停步和动作取消一致；各Skill私有取消阈值可独立调手感。此次迁移保持现有值和判定，不替用户选择；完整删除这些配置副本前必须明确归属、重定向全部typed消费者，未裁决部分明确保持未完成，不能写临时provider或默认fallback。

正式DirectionalDodgeRunIntent当前在Idle进入时清理，在观察到新的DodgeForward完成实例时置位。删除图内无人读取的副本不等于增加“攻击开始清理正式意图”；后者是控制行为变化，不在本次清理中实施。不得覆盖CorinCharacterControlModule中其它窗口的MovingTurn来源或60Hz时长改动。

`m_Name`重复序列化错误单独记录为作者层缺陷，不能归因于这些节点多余。当前命中的SkillStepPort字段属于普通可序列化类，尚无证据确定报错MonoBehaviour类型；后续先取得完整错误上下文和实际继承链，再决定修复。不得全局改名、清缓存或用图迁移成功冒充该问题解决。

### 11. 实施范围与现行规范对账

| 范围 | 正式职责与依赖 |
|---|---|
| Runtime/Character/Control/Authoring/FlowGraphs及原生FSM领域适配 | 使用共同业务定义，切换状态机子图、连接、导航、provider和观察；删除旧Skill状态机承载 |
| Editor/CharacterPipeline/Authoring/SkillDocument及AgentAuthoring事务层 | 更新Exporter/Mapper/Validator/OwnerCollector/Applier和正式Mutation，FSM/StateBody/Edge条件纳入同一闭包和事务 |
| Editor/CharacterSimulation/Compilation/Skills | 去除全图FlowNode/BinderConnection假设，接入同一IR/Program和SourceMap |
| Corin精确Skill作者闭包 | 迁移原生FSM与清理无消费实例、声明、私有图；不直接修改YAML，不运行第二迁移服务 |
| Simulation与Presentation | 不新增执行器、Motion Stage或MovementComponent；MovingTurn只保留Gameplay Motion，Attack Timeline保留有限动作AnimationTrack/Slot |

执行依赖为：冻结实际来源与冲突裁决 → 共同业务定义/原生FSM适配 → Document与编译完整支持 → 单次正式资产事务迁移和清理 → 精确Definition正式产物发布。代码支持未完整时不开放作者目录，也不先发布只能编辑不能编译的FSM资产。此次只更新规划，上述执行与验证均未运行。

| 当前文档或现行规范 | 与目标的差异 | 处理边界 |
|---|---|---|
| 本change旧FlowCanvas唯一拓扑表述 | 全部StateMachine仍被理解为FlowGraph | 本节明确普通执行图用FlowCanvas、状态机用原生FSM，同一对象不双存 |
| current btsmtl-agent-authoring-document-sync与技能说明 | 当前v7；在途设计有edge owner/order及v8目标 | 本change第9.3节负责FSM最终v8切换；其它change只交付其独有业务增量，不能再安装旧v7版本要求；本次不提前改current或技能 |
| unify-skill-authoring-data-model及add-skill-transfer-connections旧交接段 | 数据层proposal已缩小范围，其它工件仍在另窗口调整 | 本窗口不覆盖；共同定义归数据层，既有转移成果归转移专项，原生FSM与最终协议/闭包迁移归本change，合并前重读最新工件 |
| character-state-timeline-authoring-loop的外层None/Attack/Dodge状态机要求 | 与当前C# ControlModule激活独立Skill矛盾 | 合并规范时删除过时外层要求，保留Skill内连段，不恢复Character RootTree |
| 同一spec的Document不得包含Sequence表述 | 与有效Skill组合流程矛盾 | 限定为已退役的旧表现Sequence，不能禁用通用组合节点 |
| btsmtl-sm-node-authoring的BTSMTL/Pose共用状态机View要求 | 与原生FSM和Pose独立作者模型不完全一致 | 对齐共用GraphEditor交互，分别保留领域schema/编译合同，不保留旧BTSMTL View |
| character-motion-simulation-boundary | 唯一Motion accumulator、Body Motion和WorldSolver | 完整保留，不增加插件FSM运动执行或第二位移链 |

这些current spec差异是后续规范归并的明确待办，不在提案阶段把目标写成已实现。已有change的历史任务勾选与运行记录保持原样，不能作为原生FSM迁移完成证据。

## Risks / Trade-offs

- [Skill作者想直接改主线状态] -> 只开放Character State Get和正式Control Command，拒绝直接字段写入。
- [Ability共享状态变成新的万能黑板] -> 先按Attribute、Tag、Effect、ActivationData分类，复杂结构必须是显式typed provider。
- [Rollback和Server Authority共用一套状态但确认时机不同] -> 将确认、Restore、Hash和Egress放进各自Pipeline Pass，Program语义保持唯一。
- [Timeline动画与PoseGraph职责重叠] -> Timeline只通过原生Timeline编辑器维护类似Montage的有限动作播放/窗口合同，最终混合、IK和Pose输出只由Presentation/PoseGraph处理。
- [原生FlowCanvas不理解Skill provider] -> 由Skill domain adapter向原生Blackboard、Inspector和菜单注入provider目录与typed Mutation，不保留第二个Skill专用面板。
- [C#投影依赖反射导致网络不可重放] -> 使用稳定typed descriptor、State Layout、Serialize/Hash合同，不允许任意字段反射读写。
- [旧文档或旧测试继续引用Character RootTree] -> 正式checkout和作者入口拒绝旧Character RootTree；合法非Skill领域保持各自边界。
- [多网络Adapter产生分裂行为] -> 通过SimulationPipelineCompiler统一检查Program、Backend、Solver、Snapshot和Pass Contract，Adapter只实现接入策略。

## Migration Plan

1. 保留CharacterPipelineDefinition作为角色Program装配边界，删除Character RootTree字段、资产、入口、旧测试和零消费者Character路径；AI自研RootTree不保留，Behavior Designer接入由独立change管理。
2. 将C# ControlModule、Movement Runtime、Skill Program、GameplayEffect、GameplayTag和Presentation Projection的依赖整理为正式Program State和SourceMap合同。
3. 建立provider化Blackboard合同：Character State只读投影、Ability Attribute/GameplayEffect、GameplayTag、Input/TargetData、Skill Local Blackboard、State/ActionInstance/Frame Scope。
4. 在FlowCanvas原生GraphEditor的Blackboard、Inspector和创建菜单扩展点中提供provider分组、变量创建、Get/Set拖拽和外部provider显式引用；删除旁路Skill面板及其对原生Panels的替换，所有写入复用真实owner Mutation和Undo。
5. 将Skill Timeline的动作AnimationTrack/Clip、Action Window、停止和混出语义编译为Presentation Producer/Playback合同，PoseGraph继续拥有最终混合和IK。
6. 让Character Program通过Session Composition进入SimulationPipeline；Rollback、Server Authority和本地Pipeline分别接入正式Pass/Adapter，不能新增Skill专用网络分支。
7. 完成第9节原生FSM与Document v8支持后，经唯一事务迁移精确Skill闭包并执行第10节清理；保留转移业务和完整owner回滚，旧Character RootTree不恢复。
8. 最后执行正式Character Build、网络产品闭包核对、Skill运行/中断/Timeline证据、Hash/Snapshot对账和OpenSpec严格校验。
