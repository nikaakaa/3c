# Change: 将 Pose Graph 收敛为 BTSMTL Graph Authoring 领域

## Why

项目已经通过`upgrade-character-animation-authoring-workspace`把BTSMTL Tree、AI Graph和Character Pose Graph装进同一个`GraphAuthoringEditorShell`，并提供Toolbar、Navigator、Graph Canvas、Details与Bottom Dock。但是这轮工作只统一了窗口外壳：

- BTSMTL仍使用`BaseTreeView`与`BaseNodeView`，Pose Graph另外实现`PoseGraphView`与`PoseGraphNodeView`。
- Pose Graph另外维护Node Catalog、Port Policy、Clipboard、Mutation、Inspector、StateMachine GraphView change和Diagnostics adapter；单个`CharacterPresentationPoseGraphEditor.cs`已经超过五千行。
- `CharacterPoseNodeDefinition`把Sequence、Blend、IK、FootPlacement、StateMachine、Slot等所有字段放入同一个联合体，导致序列化和内部Inspector天然携带与当前节点无关的数据。
- Pose节点类型、字段、端口、UI、Validator、Compiler、Document context和Runtime operation分别维护映射；新增节点需要横向修改多处大`switch`。
- Agent Authoring Document v2已经建立稀疏Graph、logical port和唯一Capability Catalog，但Presentation仍被固定为只读context，Pose Graph无法进入同一Reconciler、Mutation和事务。
- `refactor-animation-control-boundaries`要求Corin最终资产通过正式Agent Document原子迁移，同时其依赖的Document change又禁止Presentation mutation；剩余资产任务因此没有一条合法闭环。

用户需要的是UE Animation Blueprint式的完整作者体验和底层分层：共用一套成熟Graph UI、Schema、Capability、Mutation和Document，作者只看到当前节点的业务字段；Pose Graph再编译为自己的紧凑Pose Program并由Animation Native Runtime执行。目标不是把Pose节点塞进BTSMTL Gameplay Runtime，也不是在现有共享Shell上继续堆一套动画专用GraphView。

## What Changes

- 在BTSMTL TreeDesigner Editor内建立唯一`Graph Authoring Domain Framework`：
  - 以现有`BaseTreeWindow`、`BaseTreeView`、`BaseNodeView`、`BasePortView`、`PropertyPortView`、`BaseTreeInspectorView`与`GraphDataCatalog`的成熟实现作为唯一提取基线，原地抽出Graph Canvas、Node View基座、typed Pin、搜索、创建菜单、selection、clipboard、Undo、breadcrumb、Details、Navigator、diagnostics和StateMachine画布。
  - 抽象只允许改变依赖方向、类型归属与domain binding，不得替换BTSMTL现有窗口分区、节点信息、黑板变量拖拽、Property Port、节点搜索、框选、复制粘贴、Undo、Inspector、子树下钻和Live Debug交互。
  - 禁止先新写一套简化GraphView再把BTSMTL切过去；共享作者表面必须由现有BTSMTL UI代码演化而来，并在同一个实现中继续服务BTSMTL。
  - BTSMTL Gameplay、AI与Pose Graph只通过domain schema、document adapter、mutation adapter、details provider和可选node presenter表达差异。
  - 删除“只复用Shell、各领域再实现一套GraphView”的旧边界。
- 将现有Agent Capability Catalog提升为全作者链唯一能力目录：
  - 每个stable node kind声明允许Graph kind、作者属性、默认值、logical port、动态port规则、资源引用、UI presentation、Mutation和Compiler lowering入口。
  - 人工编辑器、Document catalog、strict parser、Reconciler、Validator和Compiler读取同一语义描述，不再维护平行Node列表。
- 破坏性拆分Pose作者节点：
  - 删除`CharacterPoseNodeDefinition`大联合体和每个实例重复保存的全部固定port镜像。
  - 使用公共Pose authoring node header加每种节点独立typed payload；固定port来自Capability，动态port使用节点局部稳定identity。
  - Editor/Authoring Node与Runtime Pose Node Descriptor完全分离，运行时不读取显示名、布局、资源路径或字符串骨骼名称。
- 让PoseStateMachine复用BTSMTL的StateMachine视觉与交互组件：
  - 共用Entry、State、Alias/Any、Transition edge、下钻、breadcrumb、selection和规则图画布。
  - Gameplay与Pose继续拥有不同数据、Details、Fact/Blackboard输入、Compiler和Runtime，不把动画Blend字段写进Gameplay StateMachine。
- 把Pose Graph、PoseStateMachine和Presentation Profile source binding纳入唯一Agent Document：
  - `btsmtl-agent-authoring-document.v3`新增稀疏Presentation editable分片。
  - Rig、Bone catalog、可选资源、Action producer、generated Program/Projection、Foot Analysis artifact和Runtime状态继续只读。
  - 五个BTSMTL生命周期MCP工具保持不变，Document apply通过同一Application Service、Reconciler、Mutation Plan、Undo、Validator和反向导出处理Presentation owner。
  - 删除v2 reader、converter和双写，不增加Pose节点级MCP或第二Presentation service。
- 模块化Pose Compiler：
  - 每种Pose节点拥有独立Validator与Compiler handler，统一降低为Pose IR，再线性化为现有Native operation和workspace。
  - 作者语义采用UE式`Output Pose -> Pose Link`依赖模型；执行实现继续使用Unity Animation Job适合的紧凑拓扑计划，不在热路径递归虚调用。
  - Presentation Fact/State Update与骨骼Pose Evaluate保持分阶段，Runtime算法和Gameplay/Action职责边界不因UI统一而合并。
- 重做Pose Graph Workspace装配：
  - 使用共享BTSMTL Canvas、Node、Pin、Details和StateMachine UI。
  - 正常作者界面只显示节点业务名、typed port、当前节点字段、动画/骨骼/Fact/参数选择器、状态与迁移。
  - GUID、revision、port identity、provider技术identity、compiler index、operation code、workspace、Document hash和generated payload只允许进入独立只读Diagnostics。
  - Preview、Pose Watch和Live Debug继续读取正式Pose Plan与Trace，不成为第二求值或写入路径。
- 原子迁移并激进清理：
  - 先安装共享Framework、独立Pose节点、Compiler和Document v3，再通过正式Document生命周期迁移Corin。
  - 只有在原BTSMTL操作逐项由原地抽出的唯一共享实现承接、BTSMTL仍保持原有行为且Pose已经接入同一实现后，才删除被抽空的BTSMTL专用壳、Pose专用GraphView/NodeView/通用Inspector分支、旧联合体、旧inline carrier、旧Document Presentation只读摘要结构和旧generated Projection。
  - 不保留旧资产reader、UI开关、adapter、fallback、双写或临时桥接。

## Scope

### In Scope

- BTSMTL、AI与Pose Graph共享的Graph作者UI内核与domain adapter合同。
- 共享Capability Catalog、typed property、logical/dynamic port和Graph kind schema。
- BTSMTL与Pose StateMachine共享视觉/交互基座。
- Pose作者节点typed payload、Mutation、Validator、Compiler handler与Pose IR。
- Character Document v3的Presentation editable分片、Exporter、Package Mapper、Reconciler、Mutation、Validator与五工具桥接。
- Pose Graph Workspace、Navigator、Details、Preview/Live装配和作者信息隐藏。
- Corin Pose Graph/Profile正式迁移、旧产物删除与精确Character Build发布。
- current specs、active change关系、`openspec/project.md`、`btsmtl-agent-authoring`技能与current-contract同步。

### Out of Scope

- 让Pose Graph继承`BaseGraph`、Pose节点继承`BaseNode`或进入BTSMTL Gameplay Runtime。
- 把Gameplay StateMachine、Timeline、Blackboard或Action生命周期迁入Pose Runtime。
- 新增Montage资产、Event Graph、Control Rig、Niagara式VM或UE运行时依赖。
- 让Document、Editor或Preview解释generated Pose Program作为第二作者真相。
- 修改FootPlacement、TwoBoneIK、Blend、Inertialization、Motion Matching或AnimationSlot的业务算法。
- 增加节点级、边级、字段级或PoseGraph专用MCP工具。
- 自动checkout、自动apply、自动compile、自动Build、文件watcher或selection触发重操作。
- 新增测试；用户将自行做端到端验证。

## Impact

### Specs

- 新增`graph-authoring-domain-framework`。
- 修改`graph-authoring-editor-shell`。
- 修改`btsmtl-graph-core`。
- 修改`btsmtl-sm-node-authoring`。
- 修改`character-presentation-pose-graph`。
- 修改`character-animation-presentation-authoring`。
- 在`btsmtl-agent-authoring-document-sync`安装后将其破坏性升级为v3 Presentation editable合同。
- 修改`agent-character-controller-synthesis`。
- 扩展`btsmtl-agent-authoring-mcp-bridge`的Character Document事务owner，不增加工具。

### Code

- `GraphAuthoringEditorShell`、共享GraphView/NodeView/Port/StateMachine/Details/Catalog模块。
- `BaseTreeView`、`BaseNodeView`与BTSMTL Tree/AI domain adapter。
- `CharacterPresentationPoseGraphEditor`及其Pose专用View、Catalog、Port、Mutation和Inspector实现。
- `CharacterPoseAuthoringContracts`、Pose节点定义、Graph/StateMachine数据与迁移服务。
- `CharacterPresentationPoseGraphValidator`、`CharacterPresentationPoseGraphCompiler`与Pose Plan schema。
- `AgentAuthoringCapabilityCatalog`及其程序集归属。
- Document Models、Codec、Package Mapper、Exporter、Reconciler、Mutation Planner/Compiler/Handler、Application Service与MCP bridge。
- `btsmtl-agent-authoring`技能、current-contract与Character Definition Build上下文。
- Corin Presentation Profile、Pose Graph和generated Presentation Projection。

### Active Change关系

- 当前工作区唯一串行顺序、阶段门槛与跨change任务归属以`openspec/character-pipeline-serial-execution.md`为准。本change不得把已完成的动画算法重新实现，也不得在共享authoring尚未闭合时提前迁移Corin资产。
- `refactor-agent-authoring-to-synced-json-document`必须先完成v2 Store、strict package、Reconciler和五工具生命周期基础。本change随后一次性升级v3并删除v2，不复制其基础设施。
- `refactor-animation-control-boundaries`的代码合同继续保留；其20–23、27.1–27.5、27.13–27.15、28与26.6–26.11描述的Corin业务迁移和发布必须由本change安装的Document v3、Presentation Mutation、迁移器和显式Build一次完成，不得分别执行两次资产写入。
- `complete-composable-pose-graph-editor-workflow`已经拥有Rig v3、Virtual Bone、空间化TwoBoneIK与FootPlacement、ordered staged Pose Plan及Corin业务配置。本change只负责把相关Bone引用迁入唯一Capability、typed payload、Document v3、共享UI与Pose IR，不得建立第二solver、source capture或IK pass。
- `add-character-presentation-blend-space`与`add-character-motion-matching-pose-source`已经拥有各自领域算法和state-local source ABI。本change必须把BlendSpacePlayer与MM provider接入同一Pose domain catalog、共享UI、Document和Compiler边界；两项change剩余的独立演示内容不阻塞Corin与Rollback关键路径。
- `add-action-animation-authoring-workspace`必须在共享Canvas、Details、Navigator、Document v3与Presentation Mutation安装后、Corin资产迁移前实施。它复用正式Timeline、Profile和Pose owner，不得等待`refactor-animation-control-boundaries`资产迁移或归档，也不得把旧`PoseGraphMutationAdapter`固化为跨owner路由。
- `close-deterministic-rollback-character-pipeline`只在本change完成Corin迁移和Fixed产品发布后执行，并只把新Program/Projection接回既有Rollback Composition、KCC、Collision、Relay与Peer产品。

## Breaking Changes

- `btsmtl-agent-authoring-document.v2`被v3替换，不提供reader、converter或双写。
- Presentation不再是Character Document的只读摘要；正式可写Pose/Profile内容进入editable，generated与资源目录继续只读。
- `CharacterPoseNodeDefinition`大联合体被独立typed节点payload替换。
- Pose节点固定port不再逐实例序列化；Edge只保存Capability定义的logical port或节点局部dynamic port identity。
- `PoseGraphView`、`PoseGraphNodeView`及重复Catalog/Port/Inspector基础被删除。
- BTSMTL与Pose StateMachine视觉组件改为共享实现，不保留旧画布开关。
- 旧Pose authoring资产必须一次性迁移；未迁移资产明确失败，不回退读取旧字段。

## Current Spec Comparison

- current `graph-authoring-editor-shell`要求同一Shell，但仍允许BTSMTL GraphView与Pose GraphView由不同domain adapter提供。本change把复用边界下沉到Canvas、Node、Pin、Details、StateMachine和Capability，不再只共享窗口壳。
- current `btsmtl-graph-core`明确规定Pose Graph“只能复用通用Shell”。该约束与用户要求冲突，本change将其替换为“共享作者领域Framework、保持serialized/runtime语义独立”。
- current `character-presentation-pose-graph`已经要求UE术语、Workspace、Preview和固定Pose Plan，但没有约束节点定义必须模块化，也没有禁止Pose专用GraphView。本change补齐Editor Node/Runtime Node、PoseLink、Compiler handler和作者字段可见性。
- current `character-animation-presentation-authoring`把Profile Inspector和Pose Workspace定义为人工唯一入口；本change保留正式owner，同时让人工UI与Agent Document调用同一Typed Presentation Mutation，避免第二写链。
- active `refactor-agent-authoring-to-synced-json-document`明确把Presentation排除在editable之外；本change在其v2基础设施安装后升级v3，不保留v2只读Presentation作为兼容模式。
- active `refactor-animation-control-boundaries`一方面要求Agent Document保持Presentation只读，另一方面任务28.14又要求在同一Document mutation中完成Pose Graph/Profile资产迁移。两者无法同时成立；本change以v3 Presentation editable解决该矛盾。
- archived `upgrade-character-animation-authoring-workspace`明确只升级共享Shell并保持domain adapter职责不变。它已经完成自己的范围；本change破坏性取代“Shell共享即完成UI复用”的旧结论。
- current `btsmtl-componentized-node-authoring`继续只约束BTSMTL`BaseNode/NodeModule`领域。Pose节点不继承这些runtime/serialized类型，只复用更上层的Graph Authoring Domain Framework。
- current `btsmtl-sm-node-authoring`禁止Gameplay StateMachine携带动画表现字段。本change保留该禁令，只共用StateMachine视觉与交互，不共用领域Details和Compiler。

## Success Criteria

- BTSMTL、AI与Pose Graph的画布交互只由一个共享Canvas/Node/Pin/selection/clipboard/Undo实现拥有。
- 唯一共享实现必须从现有BTSMTL作者UI原地提取；BTSMTL的黑板变量拖拽、Flow/Property Port、节点搜索与创建、框选、复制粘贴、Undo、Inspector、子树/StateMachine下钻、窗口布局和Live Debug保持原有业务行为。
- 不存在用于替换BTSMTL的第二套`GraphAuthoringCanvas`、通用Node/Port或Details视觉实现；若抽象需要改变现有布局、信息密度或操作入口，实施必须停止并由用户选择业务取舍。
- Pose Graph不再定义自己的GraphView和基础Node View，领域差异只通过Schema、Presenter和Mutation表达。
- 作者选择任一Pose节点时，只能看到该kind正式拥有的字段；无关字段在正式数据模型中也不存在。
- UI、Document、Reconciler和Validator读取同一stable kind、typed property和logical port目录。
- Pose节点增加或删除时，不需要修改其它节点payload、通用Inspector或多个平行节点列表。
- Gameplay与Pose StateMachine使用同一视觉/导航组件，但各自数据、条件、Blend、Compiler和Runtime保持严格分离。
- Character Document v3能完整往返Pose Graph、PoseStateMachine、Transition、Pose source binding和AnimationSlot配置。
- 人工编辑与Document apply最终调用同一Presentation Mutation和Validator，不存在第二事务或YAML写入。
- Pose authoring仍只编译为Presentation Projection/Pose Program，BTSMTL Gameplay Program不包含Pose operation。
- Corin迁移后旧联合体、旧GraphView、旧inline carrier、v2 Document和旧Projection全部删除。
- 打开窗口、选择节点、修改字段、保存JSON、Preview或AssetDatabase事件均不自动Build。
