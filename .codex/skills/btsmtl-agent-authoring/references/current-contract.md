# BTSMTL Agent Authoring 当前合同

## 唯一外部合同

Schema：`btsmtl-agent-authoring-document.v3`

Document固定为Unity项目根目录外部工作区中的目录包：

```text
AgentAuthoring/Documents/<domain>/<root-key>.btsmtl/
```

路径只由`domain + root_asset_path + rootIdentity`确定。调用方不能提交Document路径，也不能扫描目录猜root。

```text
<root-key>.btsmtl/
  manifest.json
  .sync.json
  editable/
    controller.json
    blackboard.json
    actions.json
    graphs/<stable-segment>/
      graph.json
      layout.json
    timelines/<stable-segment>/
      timeline.json
      curves.json
    ai/perception.json
    presentation/
      profile.json
      pose-graphs/<stable-segment>/
        graph.json
        layout.json
      pose-state-machines/<stable-segment>/
        state-machine.json
        layout.json
  context/
    node-catalog.json
    graph-kinds.json
    asset-catalog.json
    dependencies.json
```

`manifest.json`声明schema、domain、root identity与精确文件闭包；`.sync.json`保存整包同步基线。两者和`context/`由service拥有。`editable/`是AI唯一可写区域。整个目录仍是一个逻辑Document：hash、同步状态、dry-run、apply和冲突判定都以整包为单位。

新增Pose Graph或Graph-owned Inline Timeline分片不要求也不允许AI编辑manifest。AI必须使用`local:<meaningful-id>`作为graph/timeline id，并在其canonical segment目录中同时创建完整文件对。canonical segment算法为：

```text
readable = local id中非[A-Za-z0-9_-]字符替换为-，trim(-)，截取前48字符
suffix = SHA-256完整local id的前6字节，即前12位小写hex
segment = readable + "-" + suffix
```

Store在整包strict parse前只发现满足以下全部条件的新增文件对：

- 两个文件位于`editable/presentation/pose-graphs/<canonical-segment>/`。
- 文件精确为`graph.json`与`layout.json`，缺一不可。
- graph id为非空`local:*`，layout graphId与其相同。
- Graph、Node、Edge等新增实体使用`local:*`；`contentRevision`是普通版本值，不是local实体identity。
- role为`pose-state-graph`或`pose-subgraph`，不能创建第二个root graph。
- 两个文件都通过各自strict JSON parser。
- Inline Timeline文件对位于`editable/timelines/<canonical-segment>/`，文件精确为`timeline.json`与`curves.json`。
- Timeline id、唯一TimelineNode调用点、Track与Clip全部使用`local:*`，curves timelineId与Timeline id相同。
- controller Timeline摘要、Graph TimelineNode、Timeline callSite与Graph path必须形成同一拥有关系；Track与Clip只能通过现有typed Timeline Mutation创建。

服务用发现结果形成当前请求的有效manifest并计算editable/document hash；磁盘manifest仍由service拥有。任意其它manifest外文件继续返回`document_manifest_file_mismatch`。apply使用dry-run的有效document hash，成功后reverse export把local identity替换为stable identity并原子发布新的canonical manifest；rebase也可正式发布已发现文件闭包，但不会把未apply的目标伪装为Clean。

## MCP生命周期工具

| 工具 | 额外输入 | Unity副作用 | 输出 |
|---|---|---|---|
| `btsmtl.checkout_document` | 无 | 无 | Package绝对路径、同步状态与revision/hash |
| `btsmtl.rebase_document` | `confirm_rebase=true` | 无 | 新基线、保留editable后的同步状态，以及service正式发布的已发现local Pose Graph/Inline Timeline文件闭包 |
| `btsmtl.dry_run_document` | 无 | 无 | 包含service发现文件闭包的精确Document hash、plan hash、完整planned diff、诊断 |
| `btsmtl.apply_document` | 最新dry-run返回的精确`expected_document_hash` | Gameplay、Timeline与Presentation进入同一资产级Undo事务；成功后保存authoring并反向发布stable identity与canonical manifest | applied diff、新revision/hash、`Clean`；失败完整回滚并返回`ApplyFailed` |
| `btsmtl.validate` | 无 | 无 | domain正式Validator报告；Character包含Presentation ownership与Pose Graph约束 |

五个工具各自拥有独立严格输入schema。不存在action multiplexer，不存在BTSMTL节点、边、property或timeline局部编辑MCP。AI使用通用文件读取和编辑工具修改JSON。

Character generated product使用独立生命周期，不混入BTSMTL Document事务。Character `apply_document`只提交正式authoring，不自动Build：

| 工具 | 必填输入 | 正式输出 |
|---|---|---|
| `character.migrate_legacy_pose_state_graphs` | 精确`definition_asset_path` | 旧inline Pose State Graph到GraphCatalog的一次性结构迁移 |
| `character.build_float32_products` | 精确`definition_asset_path` | 同一Definition的Float32 Program wrapper与Presentation Projection |
| `character.build_fixed_products` | 精确`definition_asset_path`、精确`wrapper_asset_path` | 指定destination的Fixed Program wrapper与同一Presentation Projection |

两个Character Build工具不读取selection、不扫描目录、不猜destination、不自动触发。Character `apply_document`成功后generated product保持stale，调用方按目标显式Build；Build完成后重新checkout刷新只读generated context。

旧`manage_btsmtl_agent_authoring`、`bootstrap_ai_controller`、`export_snapshot`、`dry_run_patch`、`apply_patch`、`patch_json`与v1单文件全部无效，不提供alias、reader、converter或双写。

## 稀疏Graph合同

每个`graph.json`只表达业务语义：

- graph stable identity、kind、ownership、显式owner和shared asset。
- editable node的stable identity、stable kind、可选name与该kind允许的typed properties。
- flow/property edge的stable identity、逻辑endpoint与port。
- 系统节点使用`@root`、`@enter`、`@exit`、`@any`、`@onEnter`、`@onExit`、`@timelineEnter`、`@result`等anchor。

`layout.json`只保存可选位置。新节点不写layout时使用确定性排布。Document不暴露C#类型名、serialized field、冗余property port镜像、系统节点对象或不可编辑运行时字段。

`context/node-catalog.json`是kind、允许property和logical port的机器可读能力目录；`context/graph-kinds.json`声明graph kind、owner slot和anchor。两个文件由同一个`AgentAuthoringCapabilityCatalog`按Document domain过滤生成，AIController只公开`BaseTree`与`ConditionRuleGraph`，不会看到Timeline、Action等Character-only capability，不能和Package Mapper、Reconciler或Validator能力分叉。

Character Input节点使用必填`inputId`，Action Request条件节点使用必填`requestId`。Exporter从正式Node binding反向导出，Reconciler生成同一条typed Mutation，preflight按当前Definition检查identity与值类型，apply只调用Node现有的`BindInputValue`或`BindActionRequest`正式接口。

已有实体必须保留导出的stable identity。新实体使用`local:<meaningful-id>`；apply后的反向导出替换为真实stable identity。数组按stable identity规范化；curve key、condition term等业务有序集合保留业务顺序。

## editable与context

Character editable：

- controller/state machine与tree clip关系
- blackboard declaration
- action request与profile
- sparse graph package
- timeline正文与独立curve文件
- Pose Graph-owned typed Source Slot子资产、Presentation Profile-owned typed Source Binding子资产、policy与有限Action producer binding
- root-owned Pose Graph catalog、layout、parameter、typed node payload、显式`pose.local`/`pose.component` dynamic port、转换节点与edge
- PoseStateMachine entry、带`Always Reset on Entry`的state、alias、transition、transition rule与blend策略；同步只由state source binding推导

AI editable：

- AI Definition/Tree/Perception绑定与candidate
- AI Blackboard与AI节点语义
- sparse graph package

Context：

- node/graph能力目录
- Character Input、Action、Timeline与ActionContext asset catalog
- AI受控Character合同
- Presentation可引用的既有AnimationClip、Blend Space、Motion Matching Profile、Timeline/Animation Channel与Capability事实
- Rig Definition v3、Physical/Virtual Bone、pelvis与左右腿chain、Body Motion、Foot Analysis identity/revision、Motion Matching索引与其它算法生成内容
- Float32/Fixed Character Program、Presentation Projection、Native Pose Program与AIIntentProgram的identity/stale状态

## Document v3 Presentation合同

Character Presentation是`editable/`中的正式目标状态：

- `editable/presentation/profile.json`保存Profile稳定owner、policy、Pose Source Binding子资产、对应Graph-owned Source Slot、实际动画资源的结构化对象引用与有限Action producer binding。正式对象引用固定包含`assetPath + assetGuid + signed non-zero localFileId`；负`localFileId`是合法Unity子资产身份，只有0非法。新建Slot或Binding只在dry-run目标中使用`local:*`，成功apply后的reverse export必须替换为正式对象引用。
- `editable/presentation/pose-graphs/<stable-segment>/graph.json`保存Graph role、parameter、Capability节点typed properties、dynamic port与edge。
- 同目录`layout.json`只保存节点位置，目录segment、Graph id与layout graphId必须一致。
- `editable/presentation/pose-state-machines/<stable-segment>/state-machine.json`保存entry、显式`alwaysResetOnEntry` state、alias、transition、规则图与blend策略。Transition禁止`targetResetPolicy`和`sourceSyncMode`；Sequence Player properties禁止`reset-on-entry`。
- 同目录`layout.json`只稀疏保存Entry、State与Alias的稳定identity和有限二维位置。缺失位置由稳定identity确定性排布；纯layout变化进入同一Presentation Mutation、Undo、hash与冲突判定，但不修改StateMachine `ContentRevision`，不使Projection stale，也不触发Build。

Pose Graph节点、字段、execution domain与port必须来自唯一共享`CharacterPoseGraphAuthoringCapabilities`，不得在Document、MCP或UI重复登记。Pose端口只允许`pose.local`或`pose.component`，不同空间只能通过显式`LocalToComponentPose`或`ComponentToLocalPose`连接；Graph Input/Output动态端口也必须保存精确空间，`OutputPose`只接受Local Pose。PoseStateMachine节点通过`childDocumentId`唯一引用同包StateMachine；State通过`poseGraphId + outputPoseNodeId`引用同包State Pose Graph输出。

Pose Transition混合JSON固定使用：

```json
{
  "blendLogic": "StandardBlend",
  "durationSeconds": 0.18,
  "blendMode": "EaseInOut",
  "blendProfileAssetId": "corin.animation-rig.locomotion-inertial-profile"
}
```

`blendMode`只接受`Linear`、`EaseIn`、`EaseOut`、`EaseInOut`与`Custom`。Custom额外要求`customBlendCurveAssetId`，非Custom禁止该字段；Curve/Profile identity必须存在于`context/asset-catalog.json`并匹配`CharacterAnimationBlendCurveAsset`或`CharacterAnimationBlendProfile`类型。旧`blendCurveId`、旧`blendProfileId`、任意GUID文本和缺失引用均严格拒绝。人工Details与Document Reconciler都提交同一种typed Presentation Mutation，修改只使Projection stale，不自动Build。

Pose State JSON必须显式保存`alwaysResetOnEntry`。StateMachine在State provider获得entry relevance前统一执行该语义。PoseState Compiler从Transition两侧State唯一的Sequence或BlendSpace provider读取Profile source binding；两侧共享同一canonical MarkerGroup才生成同步计划，无共同组生成None，多候选、角色冲突或同组topology不兼容直接失败。

Pose Graph-owned Source Slot与Profile-owned Source Binding可由Document通过同一typed Presentation Mutation创建、重命名、配置和删除。Sequence Binding完整保存`SyncMode/MarkerGroupId/Topology/SyncRole/Markers`和typed Foot Placement Weight curve；Marker writable owner与有限Action Timeline Track分离，不能互相复制。Binding中的Rig、source asset、Motion Matching Profile与Foot Analysis字段只能引用正式目录允许的既有资产；Document不能创建或修改Rig Definition v3、Bone、Virtual Bone、腿链、Foot Analysis、Motion Matching索引或generated payload。

Presentation目标使用两类分离的业务来源：

- Sequence、Blend Space与Selected Pose Player的`pose-source-slot`属性必须引用精确Graph-owned typed Slot对象；`profile.json.poseSources`必须用精确Slot与Binding子资产引用绑定实际资源。Compiler按对象引用解析后生成Projection-local dense source index，Runtime不得保留作者Source字符串；按PlayerNodeId生成的typed provider identity只做帧内路由。
- ActionPlaybackInput与AnimationSlot只引用同一Document Timeline目标状态中已存在的Animation Channel，AnimationSlot是唯一consumer。
- `profile.json.actionProducers`只允许引用同一Document中已经存在的Timeline与Animation track，不通过Presentation Mutation创建Timeline owner。

不存在`MotionMatchingSelectionInput`、`AnimationSelection` port、Pose Graph `MarkerSync`或对应摘要。有限Action的Marker Sync由`editable/timelines/**/timeline.json`中的AnimationTrack唯一拥有；持续Pose Source的Marker Sync由`editable/presentation/profile.json`中的typed Source Binding唯一拥有。两类owner共用validator与时间编辑模块，但不得共享可写对象。

## 正式调用链

```text
five BTSMTL lifecycle MCP tools
  -> AgentAuthoringDocumentApplicationService
  -> AgentAuthoringDocumentExporter
  -> AgentAuthoringPackageMapper
  -> AgentAuthoringPresentationPackageCodec
  -> AgentAuthoringDocumentCodec + AgentAuthoringDocumentStore
  -> AgentDocumentReconciler
  -> AgentAuthoringPresentationReconciler
  -> AgentMutationPlanner
  -> immutable AgentMutationPlan
  -> AgentMutationSession preflight
  -> AgentDocumentMutationCompiler
  -> typed Gameplay/Timeline/Presentation Mutation handlers
  -> Character/AI Validator
  -> domain transaction owners + one Undo group
  -> SaveAssets
  -> final canonical package export
```

Character generated product发布是上述Document事务之外的显式精确Definition Build生命周期。

## 代码所有权地图

根目录：

`3cDemo/Client/3C_Client/Assets/GameScripts/Main/Editor/CharacterPipeline/AgentAuthoring/`

| 文件 | 所有权 |
|---|---|
| `Mcp/BtsmtlAgentAuthoringMcpTools.cs` | 五个独立生命周期薄桥与严格参数边界 |
| `AgentAuthoringServiceModels.cs` | 内部command与response |
| `AgentAuthoringDocumentApplicationService.cs` | domain dispatch、同步状态、Undo、rollback、save、publish、反向同步 |
| `AgentAuthoringDocumentModels.cs` | manifest、sync、package file与内部target |
| `AgentAuthoringDocumentCodec.cs` | strict parse、canonical write、整包hash |
| `AgentAuthoringDocumentStore.cs` | 确定目录、文件闭包与目录级原子发布 |
| `AgentAuthoringPackageMapper.cs` | 稀疏package与内部完整target双向映射 |
| `AgentAuthoringCapabilityCatalog.cs` | stable node kind、typed property、port与system anchor唯一目录 |
| `AgentAuthoringDocumentExporter.cs` | Character/AI canonical package投影 |
| `AgentAuthoringPresentationDocumentModels.cs` | Document v3 Presentation Profile、Pose Graph与PoseStateMachine模型 |
| `AgentAuthoringPresentationPackageCodec.cs` | Presentation分片路径、strict parse与文件闭包 |
| `AgentAuthoringPresentationExporter.cs` | Presentation正式资产到canonical editable目标的投影 |
| `AgentAuthoringPresentationReconciler.cs` | Presentation完整目标对账与typed Mutation事务规划 |
| `AgentDocumentReconciler.cs` | 完整目标集合对账与最小Mutation计划入口 |
| `AgentMutationPlanner.cs`、`AgentMutations.cs` | typed Mutation lowering与immutable plan |
| `AgentMutationSession.cs` | 单次Index、anchor/reference resolver、symbol、diff、touched owner |
| `AgentGraphLinkMutationHandler.cs` | flow/property edge创建、删除与改接 |
| 其余`Agent*MutationHandler.cs` | 正式typed authoring API适配 |

## 修改示例

先调用`btsmtl.checkout_document`，读取`context/`后修改`editable/actions.json`中的request：

```json
{
  "requestId": "Action.Attack.Light",
  "bufferSeconds": 0.2,
  "priority": 10,
  "timingClass": "Offensive"
}
```

保存文件后调用`btsmtl.dry_run_document`，再把返回的`documentHash`原样传给`btsmtl.apply_document.expected_document_hash`。

修改Presentation时直接编辑Document v3的`editable/presentation/**`目标文件，随后对整个Document执行一次dry-run和同hash apply。不得增加Pose专用MCP action、Presentation专用apply或第二套事务。

完成代码修改时必须说明Agent合同已同步，或说明变化为什么完全不影响package editable/context、identity、ownership、Reconciler和Validator。
