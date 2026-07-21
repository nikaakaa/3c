# Change: 增加 BTSMTL AI Controller 分层 Authoring

## Why

当前项目已经确定 Bot 只能替换 Character Input Source 或高层行为意图，玩家与 Bot 必须继续复用同一个 Character Program、Action、Timeline、Motion、Combat 和 Presentation 链。但现有 BTSMTL authoring 只有 Character RootTree、StateMachine、ConditionRuleGraph 与 Timeline TreeClip，没有正式的 AI Controller 资产、AI Graph role、AI Blackboard、Perception 输入或 Intent 输出边界。

因此，作者现在如果直接用 Corin RootTree 拼 AI，会把“决定想做什么”和“角色如何执行”写进同一个 Program；如果另写 MonoBehaviour AI，则会绕过 BTSMTL、portable input、Session Tick 与网络输入身份；如果只创建普通 BaseTreeAsset，则节点搜索仍会暴露 Character Action、Timeline、Motion 和 InputAction 节点，无法从 authoring 上阻止分层被破坏。

界面也不能只考虑“能打开”。`TreeWindowUtility`当前按 Window Type只持有一个窗口实例；普通 Character RootTree 与新增 AI Tree若都使用同一个`BaseTreeWindow`，作者只能来回替换根页面，无法把 AI 决策与 Character执行树并排检查。另一方面，重新制作一套 AI GraphView、Inspector或Workbench会复制当前已经稳定的页栈、Blackboard Catalog、Undo、selection与Live Debug基础。

本change建立正式AI核心边界：独立`AIControllerDefinition`拥有独立`AIControllerTree`和AI状态；同一个BTSMTL编辑器核心通过一个薄`AIControllerTreeWindow`提供可并排停靠的AI页面；正式Local Control Input Ingress从上一轮已提交Session状态构造唯一Observation，再驱动玩家、Neutral或AI Control Source冻结同一批`CharacterSimulationInput`；Character Program仍是动作执行的唯一真相。本change只交付通用AI authoring、编译与Local Float32运行能力，不同时修改Agent schema，也不生成Corin训练AI资产。

## What Changes

- 新增`AIControllerDefinition`作为AI authoring与生成产物装配根，显式引用AI RootTree、受控Character Definition、Perception Profile和generated AI Program。
- 新增`AIControllerTree : OneRootTree` Graph类型，继续由`BaseTreeAsset`持有唯一Graph数据，不新增AI专用Graph asset序列化模型。
- 为BTSMTL节点增加可复用的authoring domain/capability元数据，使AI Graph只允许共享Flow/Pure Value节点与AI专用Perception、Memory、Intent节点，发布前拒绝Character Action、Timeline、Motion、Animation、GameplayEffect、InputAction和Transform副作用节点。
- 新增薄`AIControllerTreeWindow : BaseTreeWindow`与AI Inspector Context。它复用同一`BaseTreeView`、Graph Data Catalog、page stack、breadcrumb、Undo、selection和Live Debug，只负责独立窗口实例、AI标题与AI上下文显示。
- AI Controller Definition Inspector提供`Open AI Tree`入口；作者可将Character Tree Window与AI Tree Window作为两个Unity dockable窗口并排放置。
- 不在Character Graph窗口内增加永久AI侧栏、双画布或Character/AI根页签，不把AI Tree放进Character RootTree的SubTree层级。
- 新增AI Blackboard语义：Controller scope保存跨Tick记忆，Tick scope保存当前观察与临时决策；它与Character Pipeline Blackboard分属不同Program State，不互相继承或直接写入。
- 新增portable `CommittedActorObservationSnapshot`与`AIPerceptionFrame`。唯一Local Control Input Ingress从同一Session上一轮已提交roster与Logic Body构造稳定Actor观察；第一版不引入Team、Faction或任意公开Gameplay fact通道，也不读取VisualRoot、Animator、Scene搜索或当前Tick部分结果。
- 新增AI Semantic IR与`AIIntentProgram`。它必须复用BTSMTL稳定Graph/Node/Edge/PropertyPort identity和现有唯一portable Runnable control topology/runtime，只增加独立AI Observation、Memory、Intent operation set、state layout、ProgramHash与codec，不复制Sequence、Selector、Running、activation或abort解释器。
- AI Intent runtime每Logic Tick读取冻结Perception，以candidate `AIControllerState`推进AI Program，并直接产出目标Character Program要求的`CharacterSimulationInput` values与requests；不保存第二个Gameplay request buffer。
- AI Intent输出节点通过受控Character Program/Input catalog选择稳定InputId、RequestId和类型，禁止自由字符串或显示名映射。
- 将当前Local Input Ingress收敛为唯一Local Control Input Ingress。它通过正式Committed Observation read port读取上一轮已提交状态，按稳定ActorId驱动显式Player、Neutral或AI Control Source，并在任何Character Evaluate前冻结完整CanonicalInputBatch；公共Pass、Source和Composer不得按AI具体类型、Actor名称或字符串选择实现。
- 将MotionWarp Demo中玩家显式目标provider从Actor registration Body缓存迁移到同一个Committed Observation Port；Player Target与AI Perception只保留一条跨Actor逻辑Body读取链。
- AI State成为该Input事务的正式state participant。AI Evaluate只产生candidate state；Character Evaluate、WorldSolver、Finalize或后续Pass失败时必须恢复Tick前AI State，只有整个outer transaction成功才提交AI State与request sequence。
- ServerAuthoritative Authority与DeterministicRollback AI执行不在本change实现；未声明AI Control Source capability的组合遇到AI Actor时必须在Session Active前拒绝，不得退化为Neutral或客户端本地AI。
- 保持当前Agent v14 Character Controller schema不变；AI Definition与AI Tree在本change中明确不进入Agent Snapshot、Patch、Validator或MCP写入范围。
- 为后续`extend-agent-authoring-for-ai-controller`暴露稳定AI Definition、Graph capability、Blackboard、Perception与Intent authoring API，不提供YAML、迁移器或临时资产写入旁路。

## Scope

### In Scope

- AI Controller Definition、AI RootTree、AI窗口薄壳与AI authoring context。
- AI Graph节点能力白名单和AI专用Data Catalog。
- AI Controller/Tick Blackboard scope。
- 基于committed Session状态的typed Perception Frame。
- Local Control Input Ingress、Committed Observation Port与Control Source统一准备合同。
- 玩家ActionTarget provider与AI Perception共用的跨Actor committed Body来源。
- AI Semantic IR、Float32 AIIntentProgram、AIState和Local AI Control Source。
- MoveAxis、ActionTargetSnapshot与Action Request三类正式Intent输出。
- AI Program与Character Program双侧diagnostics关联。

### Out of Scope

- NavMesh、DotRecast pathfinding、路径跟随、动态避障或群体行为。
- 视觉/听觉感知射线、遮挡、复杂仇恨、威胁评分和战术位置搜索。
- Team、Faction、敌我关系、动态候选发现与任意公开Gameplay fact投影。
- 完整怪物AI、Boss阶段、技能规划、Utility AI、GOAP或机器学习推理。
- 命中、伤害、受击、死亡和Combat Result闭环。
- ServerAuthoritative Authority Bot与DeterministicRollback Bot运行接入。
- AI直接调用Character Action、StateMachine、Timeline、Motion、Animation、WorldSolver或Transform。
- 第二套GraphView、Workbench、Blackboard窗口或AI专用序列化边/端口。
- Agent AI Controller Snapshot、Patch、Validator、MCP与技能扩展。
- Corin Training AI Definition、AI Tree、Program资产和训练敌人Control Source迁移。

## Impact

- Affected specs:
  - `btsmtl-graph-core`
  - `character-input-pipeline`
  - `gameplay-simulation-pipeline`
  - `gameplay-simulation-session-composition`
  - 新增`btsmtl-ai-controller-authoring`
  - 新增`gameplay-ai-control-source`
- Affected BTSMTL:
  - BaseTree Graph role与Node authoring capability。
  - TreeWindowUtility、BaseTreeWindow薄派生窗口与Graph Data Catalog context。
  - AI Graph、AI nodes、Blackboard declarations和Live Debug target。
- Affected Simulation:
  - numeric-neutral AI Semantic IR与canonical codec。
  - Float32 AIIntentProgram、AIState、runtime workspace与hash。
  - Local Control Input Ingress、committed observation read port、AI State transaction participant与CanonicalInputBatch唯一writer。
- Affected Character:
  - Control Source catalog增加正式AI source。
  - AI output按Character Program input catalog构造portable input。
- Breaking changes:
  - BTSMTL节点必须具有可验证的authoring domain/capability，未知能力不能进入AI Graph。
  - AI Program与AI State使用独立versioned schema，不提供对象解释器或旧Tree runtime兼容入口。

## Current Spec Comparison

- `btsmtl-graph-core`要求只有一套Graph数据和编辑器资产入口，并禁止并行BaseGraphWindow。推荐方案继续使用`BaseTreeAsset`与`BaseTreeWindow`核心；`AIControllerTreeWindow`只是用于第二个可停靠实例的领域薄壳，不拥有独立GraphView、导航或序列化模型。
- `btsmtl-graph-core`当前只要求StateMachine与ConditionRuleGraph过滤节点。AI Graph需要把过滤提升为共享Node capability合同，确保搜索、拖拽、粘贴、脚本与Compiler都走同一`CanCreateNodeType`结果；后续Agent change必须消费同一policy。
- `character-input-pipeline`当前由Unity Input Adapter产生玩家input，并允许Network Model从portable input构造历史。AI source将成为另一种正式input producer，但输出合同仍是同一个`CharacterSimulationInput`。
- `add-corin-targeted-motion-warp-demo`为首个目标闭环建立Actor registration Body provider；本change安装Session级Committed Observation Port后必须迁移玩家provider并删除旧Body读取入口，不能让Player Target和AI Perception长期读取两份逻辑观察。
- `gameplay-simulation-pipeline`要求Standard Local Pipeline只有一个Local Input Ingress和外层原子事务。本change不增加第二个input writer；它把现有Ingress提升为通用Control Source composition owner，并把AI State checkpoint/restore纳入同一outer transaction。
- `gameplay-simulation-session-composition`要求Launch Plan完整且不可变。本change要求Preparation显式锁定每个Actor的Control Source identity、所需Observation capability和AI Program binding，不允许Active后猜测或替换source。
- `character-pipeline-blackboard`只属于Character Program。AI记忆不能伪装成Character scope变量，因此本change新增独立AI State/Blackboard，不修改Character Blackboard所有权。
- `agent-character-controller-synthesis`、代码、MCP bridge与技能当前已经统一为v14。本change不修改该schema，也不让v14误认自己支持AI Controller；后续`extend-agent-authoring-for-ai-controller`将在本change稳定authoring API上原子提升v15。
- `2v2ve-gameplay-client-demo.md`已经要求Bot只替换Input Source或高层意图。本change首次把该原则落实到可编辑AI Tree、Program与Local运行闭环。
- 当前`Decision Graph`只服务Timeline TreeClip纯决策，不能被重新命名或扩展成AI Tree；两者拥有不同owner、输入、生命周期和输出。

## Dependencies And Sequencing

- 依赖已经完成的`add-corin-targeted-motion-warp-demo`提供Control Source factory与typed ActionTargetSnapshot input；本change只安装通用AI Control Source能力，不修改训练敌人的Neutral绑定。
- 依赖现有compiled BTSMTL Runnable/Value operation与Local Session batch主链，不恢复通用RunnableTree作为Character或AI正式运行时。
- 不依赖Timeline Marker或Agent schema实施；它可以与只修改Timeline Marker/Curve和Agent v14的change并行，但不得与同时修改Control Source、CharacterInput ABI、Local Pipeline或BTSMTL Graph policy的change并行。
- `extend-agent-authoring-for-ai-controller`必须在本change完成后把v14原子提升v15；`add-corin-training-ai-demo`必须再依赖AI核心与Agent v15，通过正式Agent工具创建资产。
- 本change只安装Local Float32 AI。后续Authority AI与Fixed AI必须分别通过独立change声明Session Source、Numeric Target与网络所有权。

## Success Criteria

- 作者从AIControllerDefinition打开一个独立AI Tree Window，并可与Character Graph Window并排查看。
- AI窗口与Character窗口复用同一BaseTreeView、Inspector基础、Graph Data Catalog、页栈、Undo和节点authoring API，没有第二套Workbench。
- AI RootTree只能创建共享纯Flow/Value节点和AI专用Perception、Memory、Intent节点；Character Action、Timeline、Motion和表现节点在搜索、粘贴、脚本与Compiler路径全部被拒绝。
- AI Controller Definition、AI Tree、AI Blackboard和AI Program拥有稳定identity与source revision。
- 每个AI Tick只读取同一份上一轮committed Session observation，不读取Scene Transform或当前Tick部分Actor结果。
- Local Pipeline只有一个CanonicalInputBatch writer；玩家、Neutral与AI输入都经过同一Control Input Ingress，不存在AI专用Session旁路。
- Corin玩家显式ActionTarget迁移到CommittedActorObservationSnapshot，不再存在Actor registration Body provider旁路；后续AI Perception必须消费同一read port。
- 任一后续Character或World步骤失败时，candidate AI State与request sequence不提交；成功时AI、Character与World状态属于同一outer transaction结果。
- AI Program只输出portable CharacterSimulationInput；Character Program继续唯一执行Locomotion、Action、Timeline、Motion、Combat与Presentation命令。
- 不支持AI的Network Composition在Active前明确拒绝AI actor，不回退Neutral、不在客户端偷偷运行AI。
- Agent v14继续只表达Character Controller，不输出、不写入也不验证AI Controller；本change不增加临时资产生成器。
