# Design: BTSMTL Graph Authoring Domain与UE式Pose Graph

## Context

现有作者链分成三层：

```text
GraphAuthoringEditorShell
  -> BTSMTL BaseTreeView / BaseNodeView
  -> PoseGraphView / PoseGraphNodeView

BTSMTL Agent Document v2
  -> Gameplay/AI sparse Graph editable
  -> Presentation read-only context

Pose Graph authoring
  -> CharacterPoseNodeDefinition union
  -> Pose Validator/Compiler
  -> fixed Pose Plan
  -> Animation Native Runtime
```

共享Shell解决了布局、工具栏和区域装配，但没有统一Graph编辑内核。Pose Graph继续复制画布、节点、端口、Inspector、Mutation和StateMachine交互；作者模型又使用大联合体。另一方面，BTSMTL Document v2已经具备最接近目标的稀疏Graph、logical port、Capability、Reconciler和事务，却被现行合同禁止写Presentation。

本设计参照UE的核心分层，而不是复制UE类名或运行时：

```text
共享Graph Editor/Schema
  -> 领域Editor Node
  -> 领域Compiler
  -> 领域Generated Program
  -> 领域Runtime Node
```

UE的Gameplay Blueprint、AnimGraph、Control Rig、Material与Niagara共用图和反射基础，但编译到不同Runtime。本项目对应地共用BTSMTL Graph Authoring Domain Framework，Gameplay继续编译到Character Program，Pose Graph继续编译到Presentation Pose Program。

## Goals

- 完整复用BTSMTL Graph UI与作者基础，不只复用Shell。
- 以现有BTSMTL UI的布局、节点表现、黑板变量拖拽、Flow/Property Port、搜索、selection、clipboard、Undo、Inspector、下钻和Live Debug行为作为迁移基线；共享化不得降低这些能力。
- 作者只看到当前业务节点真正拥有的字段与连接。
- 让人工UI与Agent Document读取同一Capability并进入同一Mutation。
- 让Pose节点、StateMachine和Compiler按模块扩展，不横向修改其它节点。
- 保持Gameplay Program与Pose Program、Simulation Tick与Presentation Tick、Timeline Action与state-local Pose source的现有边界。
- 保持显式Build、无fallback、无兼容、无第二写入口。

## Non-Goals

- 不让Pose Graph继承BTSMTL Gameplay runtime节点或执行器。
- 不把所有Graph序列化成同一种Unity对象。
- 不使用字符串property bag代替typed正式作者数据。
- 不重写现有Pose Runtime算法。
- 不引入自动编译、自动Build或后台Document同步。
- 不新增测试任务。

## Decision 1: 共享Graph Authoring Domain Framework，而不是让Pose继承BaseGraph

正式分层：

```text
Graph Authoring Domain Framework
  Graph Canvas
  Node View
  Port View
  Search / Clipboard / Selection / Undo
  Navigator / Details / Diagnostics
  StateMachine Surface
  Capability Schema

Domain adapters
  BTSMTL Gameplay Domain
  BTSMTL AI Domain
  Character Pose Domain
```

Framework只理解稳定Graph/Node/Edge identity、Graph kind、typed property、logical port、selection和mutation request。它不引用`BaseNode`、`CharacterPoseNode`、Blackboard、AnimationChannel或Runtime Trace DTO。

BTSMTL仍以`BaseGraph/BaseNode/BaseEdge/PropertyPort`作为Gameplay正式数据；Pose仍以独立Pose authoring data保存表现拓扑。两者通过adapter投影到共享作者视图模型，不互相继承serialized/runtime类型。

共享Framework的实现来源固定为现有BTSMTL作者UI，而不是一套新写的替代画布：

```text
现有BaseTreeWindow/BaseTreeView/BaseNodeView/BasePortView
  -> 在原实现中识别domain-neutral交互
  -> 抽出document/capability/mutation/presenter边界
  -> BTSMTL通过binding继续使用同一实现
  -> Pose通过另一binding接入同一实现
  -> 最后删除已被抽空的领域专用壳
```

抽象过程中，原BTSMTL窗口分区、节点内容与样式、黑板变量拖拽、Flow/Property Port、节点搜索与创建、框选、复制粘贴、Undo、Inspector、子树/StateMachine下钻和Live Debug是不可丢失的业务行为。类名和程序集归属可以改变，但不得用功能更少的新`GraphView`替换它们。若共享化必须改变布局、信息密度、交互手势或操作入口，实施必须停止并列出同级业务方案，不得由实现者自行重设计。

### Tradeoff

- 直接让Pose继承`BaseGraph/BaseNode`可以复用最多代码，但会把Runnable lifecycle、PropertyPort和Gameplay Graph约束带进Pose领域。
- 只共享Shell改动最小，但已经证明会继续复制GraphView、NodeView和Inspector。
- 新写通用GraphView再替换BTSMTL看似容易获得干净接口，但会丢失成熟交互并把UI重做风险混入数据链，本change明确禁止。
- 从现有BTSMTL UI原地提取共享上层Domain Framework，保留领域数据与Runtime独立，同时消除作者交互重复；本change采用该方案。

## Decision 2: 一个语义Capability Catalog驱动全部作者入口

每个Capability descriptor至少包含：

```text
StableKind
AllowedGraphKinds
DisplayName / Category / Icon / Color
TypedProperties
FixedLogicalPorts
DynamicPortPolicy
CreateDefaults
ReferenceKinds
DetailsPresentation
MutationBinding
ValidationBinding
CompilerBinding
```

Catalog按职责拆成可组合descriptor，但只有一个registration root和一份stable kind真相：

- runtime-neutral schema部分供UI、Document、Parser与Validator读取。
- editor presentation部分供Graph Canvas和Details读取。
- domain mutation部分供人工命令和Document Reconciler读取。
- Pose compiler binding只由Pose Compiler读取，不进入通用UI热路径。

`AgentAuthoringCapabilityCatalog`中已有的稀疏kind/property/port信息迁入该正式catalog。Agent package只导出catalog的AI所需投影，不再拥有另一份手写能力表。

### Tradeoff

- 单个巨型descriptor容易把Editor、Agent和Runtime程序集耦合。
- 多份独立registry会重新产生能力漂移。
- 本设计使用一个registration root组合分层descriptor：语义identity唯一，领域实现保持程序集边界。

## Decision 3: Pose作者节点使用独立typed payload

正式Pose节点结构：

```text
CharacterPoseAuthoringNode
  Stable NodeId
  Stable Kind
  Display Name
  Editor Position
  Typed Payload
```

typed payload示例：

```text
SequencePlayerPayload
BlendSpacePlayerPayload
PoseStateMachinePayload
AnimationSlotPayload
TwoBoneIkPayload
FootPlacementPayload
```

Payload只保存该节点真正拥有的作者字段。固定port来自Capability，不逐节点序列化。多输入Blend等动态port由payload保存局部稳定input identity和顺序，再由Capability生成logical port。

Unity正式资产可以使用明确受控的managed-reference typed payload或等价typed容器；外部Document永远只看到stable kind和稀疏typed properties，不看到C#类型名或managed-reference布局。

`CharacterPoseNodeDefinition`联合体在迁移完成后删除，不保留reader。

### Tradeoff

- 宽property bag最容易与JSON对应，但会把类型和合法性推迟到运行期。
- typed payload需要正式迁移和每类节点代码，但能让错误在作者/编译边界出现，并从根上消除无关字段。
- 本change采用typed payload。

## Decision 4: Editor Node与Runtime Node Descriptor完全分离

对应关系：

```text
UE UAnimGraphNode_*    -> CharacterPoseAuthoringNode + editor presenter
UE FAnimNode_*         -> compiled Pose runtime descriptor/operation
UE FPoseLink           -> compiled Pose link/index
Anim Blueprint Compiler -> Character Presentation Pose Compiler
Generated Anim Class   -> Presentation Projection/Pose Program
```

作者节点保存identity、属性和拓扑。Compiler解析资源、Rig、Bone、Fact、Parameter与source binding，生成只含索引、枚举、常量和workspace offset的运行描述。

Runtime不得读取：

```text
DisplayName
Editor Position
Document identity string
Asset path
Bone path string
Serialized payload type
```

## Decision 5: PoseLink采用UE式作者语义，执行仍线性化

作者图从source指向`Output Pose`，语义上由输出依赖上游Pose：

```text
Output Pose
  <- FootPlacement
  <- TwoBoneIK
  <- AnimationSlot
  <- PoseStateMachine
  <- Sequence/BlendSpace/MM
```

Compiler从唯一输出做可达性与依赖分析，然后生成确定拓扑顺序：

```text
Sample source
Evaluate state blend
Evaluate slot
Compose pose
Evaluate IK
Publish output
```

Runtime继续使用连续数组、operation code和固定workspace，不进行递归虚调用。这同时保留UE清晰的作者心智和Unity Animation Job需要的执行布局。

## Decision 6: 共用StateMachine Surface，不共用领域状态机语义

共享StateMachine Surface拥有：

```text
Entry/Any/Alias/Exit视觉
State视觉
Transition edge
连接手势
priority badge
rule摘要
双击下钻
breadcrumb
selection和context menu
```

Domain schema决定：

| 领域 | State内容 | Transition输入 | Transition拥有内容 | Compiler |
|---|---|---|---|---|
| Gameplay | StateBehavior Graph | Input/Blackboard/Gameplay condition | priority、rule、interruption | Character Program |
| Pose | State Pose Graph | Presentation Fact/Time | priority、rule、Blend、sync、reset | Pose Program |

Gameplay Details永远不显示Blend duration、curve、source sync或Animation Slot。Pose Details永远不显示Action admission、Gameplay interruption或Blackboard mutable state。

## Decision 7: Details只由当前Capability生成作者字段

Details固定分为：

```text
Authoring
Live
References
```

Authoring只请求当前selection capability的字段presentation。普通字段使用typed controls；Bone、Fact、Pose source、Action channel、Policy和Graph reference使用正式catalog picker。Capability未声明的字段无法出现在UI，也无法进入Document。

以下信息不进入正常Authoring面板：

```text
Node/Port GUID
ContentRevision
Provider内部index
Compiler operation/index
Native workspace offset
Document hash
Generated Projection payload
Runtime release generation
```

它们只允许在显式Diagnostics页只读显示，并且不得被复制成作者配置。

## Decision 8: Navigator复用共享Data Catalog外壳

共享Navigator/Data Catalog提供搜索、分组、entry visual、capability action、owner导航和拖拽请求。领域source不同：

- Gameplay：Input、Blackboard、Action、Gameplay Effect。
- AI：AI Blackboard、Perception、Character input/request capability。
- Pose：Graph/Subgraph、Animation Parameter、Presentation Fact、Pose source、Action channel、Rig Bone、Policy和只读generated状态。

Catalog条目只引用正式owner，不保存第二份业务数据。Pose source的资源/marker/curve修改进入Profile source owner；Action producer修改导航到Timeline；Pose topology修改进入Pose Graph owner。

## Decision 9: 人工UI与Agent Document共用Typed Mutation

正式真相仍是Unity authoring asset：

```text
Human Graph UI
  -> Domain Mutation Request
  -> Typed Presentation Mutation
  -> Unity authoring owner

Agent Document v3
  -> Reconciler
  -> immutable Mutation Plan
  -> 同一Typed Presentation Mutation
  -> Unity authoring owner
```

UI可以把连续鼠标操作按标准Undo粒度提交；Document apply在Application Service拥有的单一Undo事务中批量提交。两者调用同一字段/结构Mutation和Validator，不各自直接写SerializedProperty集合。

Mutation handler不拥有Undo、SaveAssets、Document回写或Build。Application Service继续唯一拥有Document事务；人工Editor Window继续使用Unity标准Undo和dirty owner协调。

Document apply的正式事务顺序固定为：

```text
校验expected document hash与同步状态
  -> Reconciler生成不可变计划
  -> preflight解析全部命令、引用与touched owner
  -> 收集Definition、Gameplay Graph、Timeline、Profile、Pose Graph、StateMachine页面与layout owner
  -> 在首次写入前注册一个完整Undo group
  -> 执行Gameplay、Timeline与Presentation typed Mutation
  -> 运行全部domain Validator
  -> 标记dirty并保存Unity authoring
  -> 从最终Unity树反向导出完整Document v3 staging package
  -> 校验staging package与hash
  -> 原子替换正式package并返回Clean
```

Mutation、Validator、SaveAssets、最终Exporter、staging校验或package替换任一步失败时，Application Service必须回滚同一Undo group、再次保存恢复后的Unity owner，并保留上一份正式Document package。失败响应固定为`applied=false`、`saved=false`且不得报告`Clean`。Character Document apply不发布Program、Projection或Native Pose Program；这些生成物只允许由后续精确Definition Build显式发布。

## Decision 10: Character Document破坏性升级为v3

正式schema：

```text
btsmtl-agent-authoring-document.v3
```

Character package增加：

```text
editable/
  presentation/
    profile.json
    pose-graphs/<graph-id>/graph.json
    pose-graphs/<graph-id>/layout.json
    pose-state-machines/<machine-id>/state-machine.json
```

Transition Rule继续作为`pose-transition-rule` Graph kind存在于`pose-graphs`目录，由Transition owner/slot引用。State Pose Graph同样使用owner/slot，不通过递归JSON内联。

`profile.json`只表达可写的：

```text
Pose Graph引用
Presentation Pose source binding
有限Action producer binding引用
node-local Policy引用
source-local marker/curve/analysis引用
```

只读context继续表达：

```text
Rig/Bone/Virtual Bone catalog
可选Animation资源
Action Timeline producer
Action Context/Channel
Policy/Mask/Calibration asset catalog
Foot Analysis artifact状态
Program/Projection identity与Stale状态
Runtime capability
```

Rig定义、generated artifact和Program/Projection payload不进入editable。

v3替换v2，不提供converter、alias、双写或兼容reader。五个MCP工具及整包hash/Conflict语义保持不变。

## Decision 11: Presentation Reconciler使用目标状态和正式owner

Reconciler按以下顺序建立计划：

1. Profile和root Pose Graph identity索引。
2. Pose Graph catalog owner与Graph planning symbol。
3. PoseStateMachine、State、Alias和Transition identity。
4. State Pose Graph与Transition Rule Graph owner关系。
5. Pose Node typed payload。
6. Pose Edge和dynamic port。
7. Profile Pose source binding与Policy引用。
8. Action channel到ActionPlaybackInput/AnimationSlot引用。
9. 删除顺序与全部touched owner。

已有identity保持；新实体使用`local:<meaningful-id>`，apply后反向导出stable identity。Graph/Node kind不可原地改变，必须删除并重建。未知kind/property/port/reference在Mutation前失败。

## Decision 12: Pose Compiler使用handler catalog和两级IR

编译链：

```text
Typed Pose Authoring Graph
  -> Graph/Node semantic validation
  -> Pose Link dependency graph
  -> Node Compiler Handler
  -> target-neutral Pose IR
  -> Rig/source/parameter binding
  -> native Pose operation plan
  -> Presentation Projection
```

每个节点handler只负责自己的payload和输出，不拥有全局拓扑、发布事务或Runtime状态。全局Compiler负责identity、拓扑、可达性、唯一输出、阶段约束、workspace分配和source map。

Runtime operation code仍可使用固定enum/switch；这是编译产物的性能分派，不是作者扩展入口。新增节点不得要求修改其它节点payload或通用Details。

## Decision 13: Preview和Live Debug继续只消费正式计划

共享UI只提供宿主：

- Preview adapter显式选择Definition和target，执行匹配revision的正式Pose Plan。
- Pose Watch从已完成workspace复制有界只读结果。
- Live Debug读取`RuntimeDebugSession`正式Trace。
- Graph mutation立即标记Projection Stale并停止旧Preview。

UI不得从authoring拓扑推断weight、重新采样source、创建临时Player或在没有Projection时现场编译。

## Decision 14: 所有重操作显式触发

以下事件只允许刷新轻量作者状态：

```text
selection
Inspector focus
Graph mutation
JSON保存
窗口恢复
domain reload
AssetDatabase refresh
Preview target变化
```

它们不得触发checkout、dry-run、apply、compile、Program/Projection Build、Foot Analysis或Motion Matching Database Build。

人工工具栏的Validate/Compile Preview/Build和五个Document生命周期必须由作者或Agent明确调用。`OnInspectorGUI`只绘制轻量字段，不执行扫描、编译或Build。

## Migration

完整跨change阶段和完成门槛见`openspec/character-pipeline-serial-execution.md`。本change内部必须按以下顺序推进：

1. 完成Document v2仍被v3复用的Store、strict package、Reconciler、Application Service与五工具基础，停止扩展Presentation只读模型。
2. 冻结`refactor-animation-control-boundaries`已经安装的Action/state-local source、PoseState、Slot、Transition Routing、Pose事务与Runtime边界，不提前执行其Corin资产任务。
3. 安装Graph Authoring Domain Framework、共享Capability registration root、独立Pose typed payload、Presentation Mutation、Validator、Pose IR handler和Native Plan builder。
4. 安装Document v3 Presentation分片、Exporter、Reconciler、资产级事务、反向导出和skill合同。
5. 先恢复并固定现有BTSMTL UI操作基线，再从`BaseTreeWindow/BaseTreeView/BaseNodeView/BasePortView/BaseTreeInspectorView/GraphDataCatalog`原地提取共享Canvas、Node、Pin、Details、Navigator和StateMachine Surface；BTSMTL先通过binding继续使用同一实现且保持全部原操作，随后接入Pose Workspace，最后才删除被抽空的领域专用壳。禁止新写替代式GraphView。
6. 实施`add-action-animation-authoring-workspace`，让有限Action作者工作面复用同一Capability、typed Mutation和正式owner导航。
7. 对账Virtual Bone、TwoBoneIK、FootPlacement、BlendSpace、Motion Matching、AnimationSlot、BlendStack、Inertialization、Layer/Additive/Mask与Transition Routing全部进入唯一authoring和compiler链，不重写其既有算法。
8. 提供一次性旧Pose联合体到typed payload迁移；迁移工具只接受精确Definition路径，不扫描、不Build。
9. 通过一次Document v3 checkout/dry-run/apply同时完成本change与`refactor-animation-control-boundaries`定义的Corin Profile、Pose Graph、PoseStateMachine、Slot、Rig/Mask/Policy引用和旧Gameplay表现数据迁移。
10. 完成canonical reverse export并回到Clean后，通过精确Character Build发布Float32/Fixed Program、Presentation Projection与Native Pose Program。
11. 删除v2、旧联合体、旧GraphView、旧adapter、legacy inline carrier、旧Projection和全部兼容路径。
12. 交给`close-deterministic-rollback-character-pipeline`把新产物重新装配到既有Rollback产品；本change不修改KCC、Collision或网络模型。

迁移结束只允许：

```text
Human UI -> shared domain mutation -> formal Unity authoring
Agent v3 package -> Reconciler -> same domain mutation -> formal Unity authoring
formal Unity authoring -> Pose Compiler -> Projection -> Native Runtime
```

## Failure Model

- capability缺失或不闭合：打开/checkout失败并定位kind，不显示或导出假可编辑节点。
- domain schema与正式payload不匹配：Mutation前失败。
- unknown property/port/dynamic port identity：连接或dry-run失败。
- Presentation owner无法纳入事务：apply前失败。
- Profile source、Rig、Bone、Fact、Action channel或Policy无法精确解析：Validator/Build失败。
- v2 package：明确unsupported schema，不转换。
- 旧Pose联合体资产：明确migration required，不双读。
- Projection Stale：Preview停止，Runtime preparation拒绝不匹配组合。
- Build失败：保留已保存authoring与Stale产物，不伪装apply失败，也不恢复旧Projection。
- BTSMTL任一既有操作没有明确映射到共享实现：停止UI迁移，不切换`BaseTreeWindow`，不删除原View。
- 共享化要求改变BTSMTL布局、节点信息密度、黑板拖拽或操作入口：停止实施并等待用户确认业务取舍。

## Tradeoffs

### 共享UI内核与继续使用domain GraphView

共享UI内核会要求BTSMTL现有TreeView也进行内部迁移，改动比只重写Pose窗口大。但它真正消除选择、端口、复制、搜索、状态机和Inspector的重复，后续BlendSpace、MM和其它图不再各建一套画布。

### Document v3可写Presentation与继续只读

保持只读最安全，但无法用唯一Document完成当前Corin迁移，也让人工UI与Agent长期使用不同能力边界。v3需要扩大Exporter/Reconciler/Mutation和事务owner，但能形成一条正式闭环。

### Typed payload与稀疏property bag

property bag更接近JSON，但会把类型错误和默认值解释集中到运行时/Compiler。typed payload让Unity作者资产和C#编译边界保持强类型；Document仍通过Capability投影为稀疏JSON。

### v3替换v2与原地扩展v2

strict package增加可写Presentation后，原外部合同、文件闭包和权限边界已经改变。继续称为v2会让旧工具误判只读字段。v3明确破坏边界，并按项目原则删除v2兼容。

### UE式递归PoseLink与线性Native计划

运行时递归节点对象最接近UE源码结构，但不适合当前Unity Job和固定workspace。作者与Compiler采用PoseLink依赖语义，Runtime线性化执行，可以同时得到清晰作者模型和现有性能边界。
