# Design: BTSMTL AI Controller 分层 Authoring

## Context

当前BTSMTL已经具备一套稳定Graph底座：`BaseTreeAsset`持有`BaseTree`数据，`BaseTreeWindow`拥有GraphView、Inspector、Data Catalog、page stack和Live Debug；具体Graph通过`CanCreateNodeType`限制节点。Character Authoring在打开RootTree时附加`CharacterPipelineAuthoringContext`，并把Graph编译为CharacterSimulationProgram。

缺口不是“没有行为树节点”。Sequence、Selector、Parallel、Decorator、Value和Blackboard基础已经存在。缺口是没有一个独立AI所有权边界：普通BaseTree允许看到过多Character节点，AI没有自己的Definition、Perception、Memory、Program和Input输出合同，也没有可以与Character窗口同时显示的窗口实例。

现有Local输入合同还有一个必须正面处理的限制：`ISimulationInputAdapter.BuildInput`只获得Actor、Tick、Numeric Profile与sequence，`LocalInputIngressPass`只读取Source和Program Runtime，二者都看不到上一轮committed World State。因此AI不能被直接塞进普通Input Adapter后自行查Session、Actor registration、Scene Transform或Presentation sample。本change必须把committed observation建立为正式read port，并由唯一Local Control Input Ingress统一驱动所有Control Source。

## Core Separation

```text
AI Controller Authoring
  AIControllerDefinition
  -> AIControllerTree
  -> AI Semantic IR
  -> Float32 AIIntentProgram
  -> AIControllerState

Committed Session State T-1
  -> CommittedActorObservation Port
  -> Local Control Input Ingress
       -> Player Control Source
       -> Neutral Control Source
       -> AI Control Source
            -> AIPerceptionFrame
            -> AIIntentProgram Evaluate
            -> Candidate AIControllerState
  -> one CanonicalInputBatch

Character Execution Authoring
  CharacterPipelineDefinition
  -> Character RootTree / StateMachine / Timeline
  -> CharacterSimulationProgram
  -> Motion / WorldSolver / Presentation
```

AI只负责回答：

```text
我关注谁？
我要往哪里移动？
我现在要提交哪个动作请求？
这个请求携带哪个目标快照？
```

Character Program继续回答：

```text
这个请求现在能不能执行？
进入哪个状态？
前后摇和打断如何处理？
Timeline、MotionWarp和动画如何运行？
WorldSolver最终移动多少？
```

## UI Options And Tradeoffs

### Option A: 只新增AI Tree，继续使用唯一BaseTreeWindow

做法：创建`AIControllerTree`资产，双击时仍替换当前BaseTreeWindow根页面。

收益：UI改动最少，不新增窗口类型。

代价：现有TreeWindowUtility按类型只保留一个窗口实例；打开AI Tree会覆盖Character Tree当前根。作者无法并排对照“AI输出Attack”与“Character如何执行Attack”，频繁切换还容易丢失当前下钻位置和上下文。

适合：只偶尔编辑一个Tree、无需联合调试的小项目。

### Option B: AI独立可停靠窗口，但复用BaseTreeWindow核心

做法：增加`AIControllerTreeWindow : BaseTreeWindow`与AI Inspector Context。Window类只决定独立Unity窗口身份、标题和AI Inspector扩展；GraphView、导航、页栈、Data Catalog、Undo、selection、authoring mutation与Live Debug全部来自BaseTreeWindow。

收益：Character与AI可真正并排；作者一眼区分当前编辑领域；没有第二套Graph数据或编辑器实现。后续AI Inspector可显示Perception schema、controlled Character contract和AI Program状态，而不会污染Character Inspector。

代价：需要维护一个很薄的窗口facade，并要求公共TreeWindow基础继续保持可复用扩展点。

结论：采用。

### Option C: 在同一个窗口增加Character/AI双画布或永久侧栏

做法：一个BaseTreeWindow同时持有两个GraphView，或在Character窗口侧边嵌入AI Graph。

收益：理论上可以在一个Unity tab里同时观察两个Tree。

代价：selection、Inspector、Undo owner、breadcrumb、快捷键焦点、Live Debug target和保存状态都必须变成双份；窄窗口下可用面积差；AI与Character生命周期会被UI结构暗示为同一资产。实现复杂度远高于两个dockable窗口。

结论：不采用。

### Option D: 把AI作为Character RootTree的SubTree

做法：在Corin RootTree增加AI SubTree，并由角色Program同时执行。

收益：资产数量最少，现有下钻页栈直接可用。

代价：玩家角色也携带AI决策；AI Memory进入Character State；Network Model无法明确AI由Authority还是Client运行；AI可以直接触碰Action/Timeline；输入边界被绕过。这会让“Bot只替换Input Source”失效。

结论：禁止。

## Decision 1: 独立Definition与独立RootTree

`AIControllerDefinition`是AI装配根，至少保存：

```text
ControllerId
AI RootTreeAsset
Controlled CharacterPipelineDefinition
AIPerceptionProfile
Generated AIIntentProgramAsset
Generated artifact identity/status
```

AI RootTree继续由`BaseTreeAsset`持有，Tree数据类型为`AIControllerTree`。不新增`AIBehaviorTreeAsset`，避免第二种Graph asset shell。直接打开孤立AI Tree时可以显示Graph，但依赖Character/Perception的目录项必须明确显示缺失authoring context；正式编译必须从Definition进入。

## Decision 2: AI窗口是薄领域facade

`AIControllerTreeWindow`只提供：

- 独立EditorWindow类型，使TreeWindowUtility可以同时保留Character与AI窗口。
- `AI Controller`标题和当前Controller identity。
- AI Inspector扩展：Definition、Perception、AI Blackboard、Intent Output和Program状态。
- AI Live Debug target选择。

它不得拥有自己的GraphView、Node Search、page stack、breadcrumb、Graph Data Catalog、Undo或序列化写入服务。若AI需要新的通用编辑能力，应扩展BaseTreeWindow公共服务，而不是在AI窗口复制实现。

## Decision 3: Graph Role与Node Capability共同限制节点

只使用NodePath前缀无法安全限制AI，因为现有共享Flow和Value节点都位于`Base/...`，Character副作用节点也位于该根。新增稳定authoring capability元数据：

```text
SharedFlow
SharedPureValue
SharedBlackboard
CharacterExecution
TimelineDecision
AIObservation
AIMemory
AIIntent
EditorOnlyDebug
```

`AIControllerTree.CanCreateNodeType`只接受：

```text
SharedFlow
SharedPureValue
SharedBlackboard（AI declaration）
AIObservation
AIMemory
AIIntent
EditorOnlyDebug（无副作用）
```

Node Search、drag/drop、paste、script create和Compiler Validator必须调用同一Graph policy。未知capability在AI Graph中直接拒绝，不按继承关系或菜单路径猜测。后续Agent v15 emitter和Validator只能消费该policy，不得复制节点名单。

第一版`SharedPureValue`只授予AI Compiler与Float32 runtime已经正式实现的`Compare`、`And`、`Or`、`Not`和Condition Result。现有Math/Vector目录同时包含Random、Curve、SmoothDamp等状态或数值语义不同的节点，不能只因菜单路径看似“纯值”就整体开放；未安装portable operation的Math/Vector节点保持无AI capability，并在Search、paste、script与Compiler全部拒绝。后续扩展必须先增加明确Semantic operation与Numeric Target实现，再授予共享capability。

Character现有Graph不必在本change重写全部创建规则，但所有进入AI Graph的共享节点必须显式声明capability；AI专用节点不得被Character RootTree接受，除非以后另有spec修改角色语义。

## Decision 4: AI Blackboard与Character Blackboard物理分离

AI Blackboard属于`AIControllerState`，第一版scope为：

- `Controller`：跨Logic Tick保存当前目标、冷却、记忆与决策状态。
- `Tick`：只在一次AI Evaluate内保存Perception投影和临时计算。
- `Graph`：保持现有inline/shared Graph局部所有权。

Character Pipeline Blackboard继续属于`CharacterSimulationState`。AI节点不能获得Character Blackboard resolver；Character节点也不能读取AI Memory。唯一跨界数据是最终`CharacterSimulationInput`。

业务上，这避免AI把“我想攻击谁”和Character把“当前Attack ActionInstance捕获了谁”混为一份可变状态。

## Decision 5: Observation是正式Session只读端口

每个Logic Tick开始时，Execution Backend从outer transaction开始前的committed state暴露唯一`CommittedActorObservationSnapshot`：

```text
ObservationTick
Locked Roster identity
ActorId
每个Actor最近committed Logic Body
```

Snapshot必须按稳定ActorId排序并保持不可变。`AIPerceptionProfile`只把该Snapshot降低为每个AI Actor的Self与显式候选Actor视图；第一版候选由Definition/Profile保存的稳定ActorId绑定产生，不引入Team、Faction、敌我推断或任意公开Gameplay fact通道。

所有AI Actor读取同一Snapshot，然后一次冻结各自CharacterSimulationInput，之后才开始Character batch。这意味着AI最多看到上一个已提交Tick，但结果与Actor注册顺序无关。AI不得读取：

- 当前Tick某个Actor已经WorldSolve后的部分结果。
- CharacterSimulationState私有Blackboard、Action或Timeline状态。
- VisualRoot或动画root。
- Scene中的Collider/Transform扫描。
- Camera可见性。
- Network packet或Presentation command。

如果后续2v2vE需要阵营与可公开Gameplay fact，必须先建立独立versioned Actor Affiliation/Observation capability并由Session Commit投影；不得在本change用Tag、名称、ActorId前缀或AI节点硬编码敌我。

## Decision 6: 唯一Local Control Input Ingress拥有输入组合

当前`LocalInputIngressPass`提升为通用Local Control Input Ingress，继续是`CanonicalInputBatch`唯一writer。它显式读取：

```text
Locked Program Runtime roster
CommittedActorObservation read port
Prepared Control Source roster
Local Logic Tick identity
```

Session Preparation必须为每个Actor锁定一个`ICharacterControlSourceRuntime`及其identity、Numeric ABI、所需capability和Character Program binding。公共Ingress只按稳定ActorId调用统一合同，不通过`is AI`、具体类型、Actor名称、Tag或fallback选择实现：

- Player Source消费已经锁存的设备输入；需要ActionTargetSnapshot时，其显式target selector通过同一Observation解析ActorId，不再读取Actor registration Body缓存。
- Neutral Source按Character input catalog生成typed neutral values和空request。
- AI Source消费对应AIPerceptionFrame并运行AIIntentProgram。

三个来源最终都返回同一种prepared `CharacterSimulationInput`。全部Actor输入验证完成后，Ingress才一次写入CanonicalInputBatch。AI不能另建第二个Ingress、直接写Kernel input或在Character Evaluate期间补交request。

Character Program的InputRequest catalog必须显式保存`TimingClass`，AI Inspector显示该值，AI Compiler也要求该字段存在且有效。TimingClass继续属于Character Input合同；AI节点只选择正式RequestId并配置自身提交窗口、优先级与repeat policy，不复制InputAction名称或按RequestId字符串猜测语义。

## Decision 7: AI State随outer transaction原子提交

AI Source不能在Evaluate时直接覆盖正式`AIControllerState`。每个AI Actor在Tick开始前捕获正式state checkpoint，并产生：

```text
Candidate AIControllerState
Prepared CharacterSimulationInput
AI diagnostics candidate
```

拥有AI State的Local Control Input Ingress必须成为正式Pipeline state participant，并按稳定ControllerId/ActorId聚合state schema、hash、checkpoint与restore。其事务语义为：

- AI Evaluate失败：恢复全部已推进AI State，不发布CanonicalInputBatch。
- 后续Character Evaluate、WorldSolver、Finalize或Egress失败：恢复Tick前AI State与request sequence。
- outer state publish成功：提交全部candidate AI State；之后才允许外部Committer发布结果。
- Session snapshot/restore包含AI State participant；每个Session Source Requirements显式声明允许的Local Control Source capability，不支持`TransactionalState`的Composition在Active前按ActorId拒绝该配置。

Tick workspace、Perception缓存和output builder仍然是transient，不进入AI State或hash。这样同一request不会因为Character失败而消耗AI sequence，也不会在下一Tick重复或丢失。

## Decision 8: AI独立Program必须复用唯一portable控制运行时

AI不能使用通用`RunnableTree`对象解释器作为正式运行路径，因为它会引入Unity对象clone、不可审查状态和未来网络身份问题。AI Frontend从同一Graph结构产生独立`AI Semantic IR`，但Sequence、Selector、Parallel、Decorator、Running、activation、abort、value edge和control topology必须复用现有portable Core合同与实现。若当前Character frontend/runtime的API仍带Character具体类型，应先把控制基础提取为共享Core模块，再由Character与AI operation adapter消费；不得复制第二套control evaluator。

AI只增加：

```text
ReadSelfObservation
EnumerateConfiguredCandidates
SelectNearestCandidate
Read/WriteAIMemory
WriteContinuousInput
WriteActionTargetSnapshot
SubmitActionRequest
```

生成的`AIIntentProgram`拥有独立ProgramHash、LayoutHash、OperationSetVersion和AIState codec。它不是第二个Character gameplay evaluator：AI Program只能生成input，不能执行Character operation。

第一版只安装Float32 AI Target。Fixed Target、Authority Host与Rollback AI都必须显式实现匹配capability后才能使用该AI Definition。

## Decision 9: AI直接产出CharacterSimulationInput

不增加长期存在的`AICommand`、`BotAction`或第二request buffer。AI Evaluate内部可以使用短生命周期output builder，Finalize时直接冻结：

```text
CharacterSimulationInput.Values
CharacterSimulationInput.Requests
SourceTick
InputSequence
```

Intent节点只能从受控Character Program/Input catalog选择合法InputId与RequestId。编译时验证value kind、request timing class和目标Character Program identity。AI request sequence保存在candidate AI State中；同一个Intent节点持续Running时不得每Tick重复提交离散request，只有新的节点activation或显式repeat策略才能产生新request。

## Runtime Order

```text
Logic Tick T begins
  -> Capture Control Input participant checkpoints
  -> Read committed observation from T-1
  -> Build PerceptionFrame for every AI actor
  -> Evaluate all AIIntentPrograms into candidate AI states
  -> Prepare all player/neutral/AI CharacterSimulationInputs
  -> Write one CanonicalInputBatch
  -> Character Session Schedule
  -> Evaluate all Character Programs
  -> ResolveBatch
  -> Finalize
  -> Atomic state publish
       -> Character State
       -> World State
       -> AI Controller State
  -> Observation for next tick becomes available
```

任一AI Evaluate或后续Pipeline步骤失败时，本次Session Tick不得悄悄使用Neutral input。Ingress participant必须恢复Tick前AI State，正式Session failure policy拒绝发布当前batch，并输出ControllerId、AI node、ActorId和Tick。

## Network Ownership

本change只接Local Session：

- Local Demo：AI Program与Character Program都在同一进程运行。
- ServerAuthoritative：未来只允许Authority侧运行AI，客户端把Bot作为remote actor观察。
- DeterministicRollback：未来选择Fixed AI Program在所有Peer运行，或由明确Authority/Relay输入Bot；不能在本change猜方案。

Network Model不逐节点同步AI Tree。它只消费AI最终生成的正式CharacterSimulationInput或Authority产生的Character结果。

## Diagnostics

AI Live Debug需要沿同一因果链显示：

```text
Observation Tick
-> candidate actors
-> selected target
-> active AI node path
-> AI Blackboard writes
-> emitted input values/requests
-> Character Actor/Tick consumption
```

Character Debug仍从input开始显示Character Program。两者通过ActorId、InputSequence和SourceTick关联，不共享mutable debug状态。

## Deferred Agent Boundary

当前唯一Agent schema已经由Animation Marker change收口为`agent-character-controller-synthesis.v14`。本change不修改Snapshot、Patch、typed command、Validator、MCP bridge或技能，也不让v14通过未知domain访问AI资产。AI Definition、Graph policy、Blackboard、Perception与Intent必须先形成稳定正式authoring API；后续`extend-agent-authoring-for-ai-controller`再基于这些API把唯一schema原子提升为v15。

这不是兼容期：v14始终只表示Character Controller，v15安装后直接替代v14。两个change之间不得出现AI YAML writer、临时菜单、反射或第二套宽DTO。

## Migration And Cleanup

1. 确认MotionWarp Demo已经提供typed target input与Control Source factory。
2. 完成AI Graph role、Node capability与薄窗口。
3. 完成AI Definition与Inspector Context，但保持Agent v14边界不变。
4. 提取并复用唯一portable control runtime，完成AI Semantic IR、Float32 Program与State。
5. 完成Committed Observation Port、Local Control Input Ingress与AI State outer transaction。
6. 将玩家ActionTarget provider迁移到同一Committed Observation并删除Actor registration Body读取入口。
7. 删除任何试验MonoBehaviour AI、AI Transform读取、通用RunnableTree正式执行和AI输入旁路。
8. 由后续Agent v15 change接入自动authoring，再由Corin demo change创建具体AI资产并迁移训练敌人。

不提供普通BaseTree解释执行AI、Neutral失败回退、Character SubTree AI或自由字符串Intent映射。

## Risks And Tradeoffs

### 独立AI Program增加一套artifact

代价是真实存在AI IR、Program与State codec；收益是AI Memory、网络运行位置和失败边界可独立审查。Sequence、Selector、Running与abort仍必须复用唯一portable control实现，因此独立artifact不等于第二个控制解释器。把AI操作塞进Character Program虽然文件少，但会让玩家角色也携带AI逻辑并破坏Input Source边界。

### Local Input Ingress承担更多正式责任

代价是Local Input Ingress需要读取committed observation并参与AI State checkpoint/restore，不再只是遍历无状态Adapter。收益是玩家、Neutral与AI仍共享一个CanonicalInputBatch writer，AI不需要Session查询、Scene registry或第二套Driver。把AI写成普通Adapter实现更快，但它拿不到正式World状态，只能通过旁路获取目标，因此不采用。

### 第一版不建立Team和公开Fact系统

代价是首版AI只能从显式ActorId候选中选择目标，暂时不能自动识别2v2vE敌我。收益是Observation字段都有正式所有者，不会把Tag、名称或Character私有状态伪装成公共感知。阵营和跨角色公开事实应在有明确战斗规则时作为独立Session capability加入。

### 先做直接接近而不做寻路

直接接近型AI会被障碍挡住，但足以验证Tree、Perception、Memory、Intent与Character执行。提前引入NavMesh会把AI authoring与World navigation两个大问题绑在一起。

### 只安装Local Float32

它不能立刻进入三个网络产品，但不会污染Network Model。Network Source Requirements把允许的Control Source capability纳入identity；ServerAuthoritative客户端与Authority当前只允许`CommittedObservation`，因此统一Preparation会在Active前拒绝`TransactionalState`，不在具体Prediction/Authority runtime重复识别AI。Authority与Fixed AI可以在同一AI Semantic IR上分别增加正式Target/Host。

### 节点Capability迁移有一定范围

AI Graph必须可靠拒绝Character节点，因此共享节点需要正式能力元数据。只在AI Graph里维护Type名单实现更快，但每次新增节点都容易漏配，Agent与编辑器也会分裂判断。
