# btsmtl-agent-authoring-document-sync Specification

## Purpose
定义BTSMTL Agent Authoring Document v4目录包的分片、规范编码、Gameplay/Timeline/Presentation声明式Mutation、事务apply与反向发布合同。
## Requirements
### Requirement: Agent Authoring Document必须是按需生成的持久化目录包

系统 MUST为每个已有合法`CharacterPipelineDefinition`或`AIControllerDefinition`提供唯一确定性`btsmtl-agent-authoring-document.v4`文档包。文档包 MUST位于Unity项目内、`Assets/`之外的`AgentAuthoring/Documents/<domain>/<root-key>.btsmtl/`，并只在显式checkout时从当前正式Unity authoring创建或刷新。文档包 MUST不成为BTSMTL正式真相、Unity资产、Player内容或runtime输入。

#### Scenario: AI首次编辑现有Character Controller

- **WHEN** Agent对已有合法Character root显式checkout
- **THEN** 系统 MUST从当前正式Graph、StateMachine、Timeline、Presentation与可达Clip Curve生成规范目录包
- **AND** response MUST返回唯一文档包绝对路径
- **AND** 系统 MUST不修改或保存Unity资产

#### Scenario: 普通人工编辑期间没有AI会话

- **WHEN** 作者修改Graph、Timeline或AnimationClip但没有显式checkout
- **THEN** 系统 MUST不创建或刷新文档包
- **AND** MUST不触发reconcile、compile、build或publish

### Requirement: 物理分片不得改变Document整包同步语义

文档包 MUST通过service-owned manifest声明唯一规范文件清单，并 MAY按Graph、Timeline、Timeline Curve、AnimationClip Curve、Presentation、领域配置和只读context拆分JSON。AI MAY只读取和修改相关文件，但checkout、rebase、dry-run、apply、Conflict、hash锁定与反向导出 MUST始终以整个文档包为唯一提交单元。系统 MUST不提供文件级基线、文件级dirty、文件级apply或文件级Conflict。新增允许创建的Graph分片 MUST继续使用完整canonical文件对；AnimationClip分片 MUST只能引用当前Definition闭包中已有原生`.anim`，不得使用`local:*`创建Clip。AI MUST不直接修改manifest。

#### Scenario: AI只修改一个Clip Curve文件

- **WHEN** AI只改动`editable/animation-clips/<clip-segment>/curves.json`
- **THEN** 下一次显式状态查询 MUST把整个文档包判定为DocumentDirty
- **AND** dry-run MUST严格读取整包并生成一个document hash
- **AND** apply MUST不允许只提交该Clip文件

#### Scenario: AI声明未知Clip分片

- **WHEN** AI加入不在manifest且不引用当前Asset Catalog原生Clip的目录
- **THEN** Store MUST拒绝整包
- **AND** MUST不把该目录解释为新建AnimationClip

### Requirement: 文档包必须分离可编辑authoring、只读context与service基线

文档包 MUST包含service-owned `manifest.json`与`.sync.json`、AI可编辑`editable/`和service-owned只读`context/`。`.sync.json` MUST只保存base source revision、base editable hash与base context hash，不得保存业务authoring。Character Presentation的Profile、Pose Graph、PoseStateMachine、direct Clip Binding、Locomotion Sync Group、AnimationSlot与Policy MUST进入`editable/presentation/`；当前Definition可达原生AnimationClip注册Curve MUST进入`editable/animation-clips/`；Rig资源正文、Body Motion、Foot Analysis generated data、runtime state、Projection与Native Program MUST只进入紧凑只读context或完全省略。

#### Scenario: AI读取Character文档包

- **WHEN** checkout导出Character Controller
- **THEN** editable MUST表达Agent正式可写的Graph、StateMachine、Condition、Timeline、Blackboard、Action、Presentation与Clip Curve结构
- **AND** context MUST只读表达Node/Graph schema、可引用asset、dependency与必要能力摘要
- **AND** 文档包 MUST不暴露Unity YAML、managed-reference布局或私有SerializedProperty path

#### Scenario: AI尝试修改只读context

- **WHEN** context文件semantic hash与checkout基线不同
- **THEN** parser或Reconciler MUST返回`readonly_context_modified`
- **AND** MUST不把变化降低为Mutation

### Requirement: Graph JSON必须使用稀疏规范authoring语言

每个`graph.json` MUST通过稳定Graph kind、正式owner、稀疏Node、Flow Edge目标集合、Property Edge目标集合和Graph reference表达Graph。Node MUST只输出稳定`kind`、stable/local identity与当前kind有意义的typed properties；MUST不输出C# type name、namespace、重复port metadata、无关nullable字段或Unity序列化路径。端口 MUST使用authoring capability catalog中的稳定逻辑key。

#### Scenario: AI添加Timeline节点

- **WHEN** AI在合法State body Graph中增加`kind: timeline`节点并连接逻辑port
- **THEN** Reconciler MUST从catalog解析正式Node类型、属性和port
- **AND** AI MUST不提供C#类型名、PropertyPort对象或`create_node`操作

#### Scenario: Node包含无关字段

- **WHEN** timeline Node提交compareType或未知property
- **THEN** strict parser MUST在mutation前拒绝该Graph
- **AND** MUST不忽略字段或转换为SerializedProperty写入

### Requirement: 系统Node必须通过只读anchor参与Graph连接

系统 MUST按Graph kind把Root、Enter、Exit、Any、OnEnter、OnExit、TimelineEnter和ConditionRuleResult等系统Node投影为保留anchor。Anchor MUST只作为Edge endpoint，不得进入editable Node集合，不得拥有layout、properties或可删除identity。每个Graph kind允许的anchor与逻辑port MUST来自同一Graph kind catalog。

#### Scenario: AI连接State body入口

- **WHEN** Graph Flow Edge从`@root.out`连接到新行为Node
- **THEN** Reconciler MUST把anchor解析为当前Graph真实系统Root Node
- **AND** handler MUST通过正式`BaseGraph.Link`创建连接

#### Scenario: AI尝试创建系统Node

- **WHEN** editable nodes包含`kind: root`或把`@root`声明为普通Node
- **THEN** parser MUST拒绝文档包
- **AND** MUST不创建第二个系统Node

### Requirement: 新Graph必须声明正式owner

每个editable Graph MUST拥有`owner.entityId`与`owner.slot`。已有Graph MUST保持stable authoring identity；新Graph MUST使用local identity并引用同文档包内已有或新建owner。系统 MUST不接受无owner Graph、按路径猜owner或以独立Graph asset作为默认私有下钻。

#### Scenario: AI为新State创建body Graph

- **WHEN** AI增加`local:attack-state`并增加owner为该State、slot为`body`的`local:attack-body`
- **THEN** Reconciler MUST先建立State planning symbol再创建inline body Graph
- **AND** apply成功后两者 MUST反向导出为正式stable identity

### Requirement: Graph逻辑与layout必须使用独立分片

`graph.json` MUST只表达业务Graph逻辑，`layout.json` MUST只表达Node位置与正式允许的视觉布局数据。已有Node显式位置 MUST保留；新Node未提供位置时，系统 MUST使用唯一确定性自动布局规则。纯layout变化 MUST进入editable hash，但 MUST不触发Program或Projection发布。

#### Scenario: AI只增加Node而不编辑layout

- **WHEN** 新Node在graph.json中存在且layout.json没有对应位置
- **THEN** apply MUST按Graph kind、拓扑层级与identity稳定排序生成位置
- **AND** MUST不移动未受影响的已有Node

### Requirement: Timeline结构与Curve payload必须分离

每个Timeline目录 MUST使用`timeline.json`表达Timeline、Track、Segment、ownership和直接AnimationClip引用，使用`curves.json`表达Timeline-local完整Curve payload。Timeline JSON MUST不表达Marker、Sequence、Clip注册Curve或Foot Analysis。Curve MUST只保存影响正式Timeline语义的字段；与catalog正式默认值相同的字段 MUST省略。AI修改Curve MUST提交该Curve完整目标状态，不得依赖key级MCP操作。

#### Scenario: AI只修改Timeline weighted curve

- **WHEN** AI替换`curves.json`中Timeline-local registered Channel的完整Curve
- **THEN** Reconciler MUST保留time、value、tangent、必要weight、weighted mode与wrap mode语义
- **AND** MUST不要求AI调用`edit_curve_key`

#### Scenario: Timeline提交Marker字段

- **WHEN** `timeline.json`包含SyncMode、SyncGroup、Topology、Role或Marker
- **THEN** strict parser MUST拒绝整包
- **AND** MUST不忽略旧字段

### Requirement: 可编辑能力必须由唯一authoring capability catalog闭合

系统 MUST使用同一authoring capability catalog驱动exporter、strict parser、Reconciler、handler preflight、Validator及只读Node/Graph catalog。每个editable Node kind MUST声明允许Graph kind、typed properties、默认值、逻辑ports、资产引用与create/configure/delete lowering。任何可导出实体若不能完整创建、修改、连接、删除和反向导出，checkout MUST以`authoring_capability_incomplete`失败，不得输出假可编辑结构。

#### Scenario: Exporter发现未登记Node类型

- **WHEN** 当前正式Graph包含一个未形成完整capability descriptor的可写Node
- **THEN** checkout MUST报告Node identity、Graph identity与缺失能力
- **AND** MUST不把C# type name直接写入editable作为绕过

### Requirement: 文档包codec必须严格解析并计算整包规范hash

系统 MUST对manifest、sync及每类JSON分片使用唯一strict parser与canonical writer。Parser MUST拒绝重复属性、未知字段、非法kind、非法identity、缺失manifest文件、非有限数值，以及既不在manifest中也未被Store按完整canonical `local:*` Pose Graph创建合同接纳的文件。Store MUST只接纳同目录完整`graph.json + layout.json`、匹配local graph identity的canonical segment、相同layout graphId和非root正式role；不得按目录前缀放宽其它文件。Writer MUST使用UTF-8无BOM、稳定字段顺序、稳定entity顺序与明确数值格式。`editableHash`与`contextHash` MUST由有效manifest中的规范相对路径和逐文件semantic hash计算，`documentHash` MUST锁定schema、domain、root identity及两项内容hash。

#### Scenario: AI只格式化一个Graph文件

- **WHEN** AI只改变缩进、换行或JSON属性输入顺序
- **THEN** 对应file semantic hash与document hash MUST保持不变
- **AND** 同步状态 MUST不因此成为DocumentDirty

#### Scenario: 文档包出现未接纳JSON文件

- **WHEN** AI在editable目录加入manifest未声明且不满足完整canonical local Pose Graph创建合同的JSON文件
- **THEN** parser MUST拒绝整包
- **AND** MUST不静默忽略该文件

### Requirement: 同步状态必须由live revision和整包hash推导

系统 MUST通过当前live authoring source revision与base source revision比较可写Unity authoring变化，通过current context hash与base context hash比较只读上下文变化，通过当前editable hash与base editable hash比较AI变化。系统 MUST只产生`Clean`、`TreeDirty`、`DocumentDirty`与`Conflict`，MUST不保存可由AI编辑的dirty布尔值。

#### Scenario: 只有AI修改Graph文件

- **WHEN** live source revision与current context未变化且editable hash变化
- **THEN** 状态 MUST为DocumentDirty
- **AND** Unity树与generated product MUST保持不变

#### Scenario: Unity与AI都修改

- **WHEN** Unity侧identity与editable hash都偏离基线
- **THEN** 状态 MUST为Conflict
- **AND** dry-run与apply MUST拒绝继续

### Requirement: Document必须确定性降低为完整Mutation Plan

系统 MUST使用唯一Document Reconciler比较当前规范Unity投影与整个文档包目标状态，并生成immutable typed `AgentMutationPlan`。Reconciler MUST自行决定Graph owner、Node、Flow Edge、Property Edge、Graph reference、Condition、Timeline、Blackboard和领域配置的创建、更新、连接与删除顺序。AI MUST不提交operation数组、handler名称、前序输出或局部工具调用。

#### Scenario: AI重接Property Edge

- **WHEN** property edge保持或更换identity但endpoint目标发生变化
- **THEN** Reconciler MUST降低为旧Property Edge断开与新Property Edge连接
- **AND** handler MUST调用正式Property Edge API

#### Scenario: AI删除Flow Edge

- **WHEN** 现有可写Flow Edge从目标集合移除
- **THEN** Reconciler MUST计划正式断开Mutation
- **AND** MUST不要求AI调用`delete_edge`

### Requirement: dry-run与apply必须锁定同一整包语义

dry-run MUST重新加载完整文档包、计算live revision、推导同步状态、严格解析、reconcile并执行无副作用preflight。成功结果 MUST包含canonical document hash、plan hash、planned diff、metrics与跨文件entity诊断。Apply MUST要求同一expected document hash，并在mutation前重新确认package、root、revision、context和状态未变化。

#### Scenario: dry-run后另一个文件被修改

- **WHEN** dry-run后任一editable文件semantic hash变化
- **THEN** apply MUST在mutation前返回`document_hash_changed`
- **AND** MUST要求重新dry-run

### Requirement: apply成功后必须从最终Unity树反向发布整个文档包

Apply MUST在唯一资产事务内调用正式handler、Validator、dirty与Save。成功后系统 MUST从最终正式树重新导出完整规范文档包，将local identity替换为stable identity，更新sync基线并通过目录级staging原子发布。AI domain MAY在同一事务内发布AIIntentProgram；Character domain MUST不在Document apply内构建Program或Projection。任一Mutation、Validator、AI generated product或package发布失败 MUST不留下半成品或报告Clean。

#### Scenario: Apply完整成功

- **WHEN** 同一document hash通过全部门禁与事务
- **THEN** 系统 MUST保存正式Unity资产
- **AND** MUST从最终Unity树反向发布完整文档包
- **AND** 文档包与Unity树 MUST回到Clean

#### Scenario: 最终package发布失败

- **WHEN** Unity Mutation成功但最终目录包无法原子切换
- **THEN** service MUST不报告完整成功或Clean
- **AND** MUST在正式可回滚边界内恢复上一份Unity资产与文档包

### Requirement: Conflict必须通过显式rebase处理

系统 MUST在Conflict时拒绝apply，并提供显式rebase。Rebase MUST以当前Unity可写authoring与只读context为新整包基线，刷新context分片，保留AI editable分片，不修改Unity资产、不build、不自动merge。Rebase完成后 MUST重新推导DocumentDirty并要求新的dry-run。

#### Scenario: AI接受当前人工树为新基线

- **WHEN** AI已把需要保留的人工变化合入editable并显式rebase
- **THEN** service MUST更新三项base identity
- **AND** MUST保留AI editable目标状态
- **AND** MUST刷新当前Unity context分片

### Requirement: 文件与Editor事件不得自动触发重操作

Graph编辑、Timeline编辑、Inspector修改、JSON保存、selection变化、窗口focus、AssetDatabase refresh和domain reload MUST不自动执行checkout、reconcile、dry-run、apply、validate、compile、Program build、Projection build或AI Program build。AI Program只可由显式AI apply发布；Character Program与Projection只可由apply后的精确Definition独立Build生命周期发布。

#### Scenario: AI保存多个JSON文件

- **WHEN** AI完成一轮直接文件编辑
- **THEN** 系统 MUST只在下一次显式生命周期调用时重新计算状态
- **AND** MUST不启动文件watcher、Unity compile或asset mutation

### Requirement: Presentation分片必须保持整包同步与稳定owner

Document v4 MUST使用`editable/presentation/profile.json`、`editable/presentation/pose-graphs/<graph-id>/graph.json`、对应`layout.json`，以及`editable/presentation/pose-state-machines/<state-machine-id>/state-machine.json`与对应`layout.json`表达Presentation目标状态。Profile MUST表达direct Clip Binding、Blend Space/MM Binding、有限Action producer binding与Locomotion Sync Group；Pose StateMachine MUST只表达Entry、State、Alias、Transition、Rule与Blend，不保存Marker或同步override。Profile binding、Pose Graph Source Slot与AnimationClip MUST通过包含asset GUID、有符号且非零local file id和一致asset path的结构化对象引用表达。新建子资产 MAY使用`local:*`，AnimationClip MUST不允许local identity。分片 MUST通过稳定owner identity互相引用，并继续服从整包checkout、hash、dry-run、apply、Conflict与反向导出语义；不得提供文件级apply、旧单文件reader、按显示名解析或缺失local file id fallback。

#### Scenario: AI只修改一个Clip Binding

- **WHEN** 一个Pose Graph ClipPlayer改为引用另一个既有Source Slot且Profile Binding引用另一个原生Clip
- **THEN** dry-run与apply MUST锁定整个Document包及精确Profile/Pose Graph/Clip owner
- **AND** 反向导出 MUST更新整包基线与规范对象引用

#### Scenario: AI创建Profile binding子资产

- **WHEN** editable使用`local:*`声明新的Profile-owned binding并引用既有Source Slot与原生Clip
- **THEN** Reconciler MUST生成typed子资产创建、Profile数组更新和资源配置Mutation
- **AND** apply成功后reverse export MUST发布正式GUID与local file id引用

### Requirement: Presentation JSON必须由共享Capability生成稀疏typed字段

Pose Graph node、port、field、StateMachine页面、Source Slot、Profile binding与Locomotion Sync Group的JSON合同 MUST来自Graph Authoring Domain Framework的同一Authoring Capability Catalog。每个node MUST只包含当前capability有意义的typed payload与node-local动态port；Source关系 MUST使用结构化对象引用。ClipPlayer MUST只引用类型匹配Source Slot，Profile Clip Binding MUST直接引用AnimationClip；JSON MUST不输出作者Source Id、Provider Id、Sequence、Marker、C#类型、SerializedProperty path、runtime枚举载荷或联合体空字段。

#### Scenario: Clip Player JSON包含Source Id字符串

- **WHEN** Clip Player payload包含`pose-source-id`、`provider-id`或任意字符串资源引用
- **THEN** strict parser MUST在Reconciler前拒绝该分片
- **AND** MUST要求类型匹配的Source Slot对象引用

#### Scenario: Clip Player JSON包含Sequence字段

- **WHEN** Clip Player或Binding payload包含Sequence引用、Marker或IK字段
- **THEN** strict parser MUST在Reconciler前拒绝该分片
- **AND** MUST不忽略字段或保留扩展字典

### Requirement: Pose Transition JSON必须保存可解析混合资产引用

Pose Transition JSON MUST使用`blendLogic`、`durationSeconds`、`blendMode`、条件式`customBlendCurveAssetId`与`blendProfileAssetId`表达混合设置。`blendMode` MUST只接受`Linear | EaseIn | EaseOut | EaseInOut | Custom`；Custom MUST引用只读Asset Catalog中的`CharacterAnimationBlendCurveAsset`，非Custom MUST不携带该字段；非Hard Cut MUST引用匹配类型的`CharacterAnimationBlendProfile`。旧`blendCurveId`、旧`blendProfileId`与无法解析的自由文本identity MUST在Reconciler前失败。

#### Scenario: Document修改一条Custom Transition

- **WHEN** AI把Transition的blendMode改为Custom并写入Catalog中合法Curve/Profile asset identity
- **THEN** Reconciler MUST降低为与人工Details相同的typed Presentation Mutation
- **AND** apply MUST只修改authoring并保持Projection stale，MUST不自动Build

### Requirement: Presentation Reconciler必须调用唯一Presentation Mutation

Document v4 Reconciler MUST按owner依赖生成类型化Presentation Mutation计划，并与人工编辑共用validator、资产级transaction、子资产identity allocator、dirty owner与诊断。Source Slot、direct Clip/Blend Space/MM Binding、Locomotion Sync Group、Pose Graph和PoseStateMachine的创建、修改、引用与删除 MUST在同一个正式资产事务中处理；Reconciler MUST不直接写Unity YAML、SerializedObject path、generated Projection或第二份字符串binding。

#### Scenario: apply新增Clip Source Slot与binding

- **WHEN** 文档目标状态新增Graph-owned Source Slot、Profile-owned direct Clip Binding并让ClipPlayer引用该Slot
- **THEN** Reconciler MUST按子资产创建、binding配置、Player引用与owner保存顺序生成类型化Mutation
- **AND** 任一失败 MUST回滚全部子资产、数组、节点引用、Gameplay、Timeline、Clip与Presentation变化

#### Scenario: apply修改Locomotion Sync Group

- **WHEN** 文档目标状态调整Group中的原生AnimationClip成员
- **THEN** Reconciler MUST使用结构化Clip引用生成Profile Mutation并校验成员唯一性
- **AND** MUST不修改Clip Curve或自动Build Projection

### Requirement: Document v4必须原子替代v3

系统 MUST删除v3及更早schema、reader、writer、manifest识别、文档包兼容与升级器，只接受v4 Document。已有v3工作目录 MUST要求显式重新checkout生成v4，不得静默迁移、fallback读取或并存两种apply路径。五个生命周期工具及其事务语义 MUST保持不变。

#### Scenario: 读取v3文档包

- **WHEN** service发现schema为`btsmtl-agent-authoring-document.v3`
- **THEN** dry-run与apply MUST拒绝该文档且不修改资产
- **AND** 调用方 MUST显式重新checkout

### Requirement: Document v4失败恢复必须同时覆盖Unity owner与正式package

Application Service MUST在首次Mutation前解析并锁定全部Gameplay、Timeline、AnimationClip与Presentation serialized owner，并注册一个完整Undo事务。只有Mutation、全域Validator、Unity authoring保存、最终树反向导出、staging重读与hash校验、正式package原子替换全部成功后，apply才可返回`applied=true`、`saved=true`与`Clean`。任一步失败 MUST恢复全部Unity owner并保留上一份正式package；Character apply MUST不发布Foot Analysis、Program、Projection或Native Pose Program。Clip registered Curve Mutation MUST只改变完整dependency baseline与Registered Curve Hash并使相关Projection stale，不得修改`AnimationClipAnalysisInputHash`或把匹配Artifact标记为stale。

#### Scenario: Clip Curve Validator失败

- **WHEN** Gameplay和Timeline mutation已经执行，但Clip Curve Validator发现Phase非单调
- **THEN** Application Service MUST回滚同一事务内全部Gameplay、Timeline、Clip与Presentation owner
- **AND** 正式Document package MUST保持apply前内容且响应不得报告`Clean`

### Requirement: AnimationClip注册Curve必须使用独立严格分片

Document v4 MUST只为当前Definition闭包中实际可达且位于可写原生`.anim`的AnimationClip输出`editable/animation-clips/<stable-segment>/curves.json`。分片 MUST包含结构化Clip对象引用、完整dependency baseline、只读`AnimationClipAnalysisInputHash`和Clip Curve catalog允许的秒域完整canonical Curve目标集合；从目标集合省略已有channel MUST表达删除。可达Clip的Foot Weight删除和仍为Locomotion Sync Group成员的Phase删除 MUST被Validator拒绝。分片 MUST不包含骨骼Curve、AnimationEvent、import设置、Rig、Foot Analysis Artifact、Phase Validation samples、Group或generated plan。Exporter、strict parser、Reconciler、handler、Validator与reverse exporter MUST复用同一Clip Curve capability，并按完整`EditorCurveBinding(path + type + property)`识别channel，不得只比较propertyName。每项替换或删除 MUST进入planned/applied diff、同一AnimationClip Undo owner与最终reverse export。

#### Scenario: checkout导出RunLoop Curve

- **WHEN** RunLoop原生Clip被当前Profile或Blend Space可达引用
- **THEN** exporter MUST输出其允许的注册Curve与dependency baseline
- **AND** MUST不导出骨骼曲线或Analysis payload

#### Scenario: Document修改Foot Weight Curve

- **WHEN** AI在dependency baseline与Analysis Input Hash仍匹配时替换Foot Placement Weight完整秒域Curve
- **THEN** preflight MUST通过唯一Clip Curve Mutation校验完整binding、秒域、值域与Registered Curve Hash
- **AND** apply成功后 MUST只使Projection stale，匹配Analysis Input Hash的Foot Analysis Artifact MUST继续Ready

#### Scenario: 组外Clip删除Locomotion Phase

- **WHEN** Document从非Group成员Clip的Curve目标集合移除`presentation.locomotion-phase`
- **THEN** dry-run MUST计划Clip Curve删除并锁定该AnimationClip owner
- **AND** apply MUST删除完整EditorCurveBinding且reverse export MUST省略该channel

#### Scenario: Document修改只读导入子Clip

- **WHEN** Clip分片引用ModelImporter子Clip或dependency baseline已变化
- **THEN** preflight MUST拒绝Mutation并定位精确对象
- **AND** MUST不复制或生成替代Clip
