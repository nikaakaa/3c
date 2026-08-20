---
name: btsmtl-agent-authoring
description: 通过唯一BTSMTL Agent Authoring Document读取、修改、对账和验证CharacterController与AIController的Graph、StateMachine、Timeline、Blackboard、Perception和Intent关系，并在authoring语义变化时同步Document schema、exporter、reconciler、Mutation、validator和MCP bridge。
---

# BTSMTL Agent Authoring

## 核心边界

AI通过五个生命周期工具管理一个显式Document v4 JSON package，Graph、StateMachine、Timeline与Character Presentation的业务修改直接使用通用文件工具：

```text
btsmtl.checkout_document
  -> 读取manifest与context catalog
  -> 直接修改editable/**/*.json
  -> btsmtl.dry_run_document
  -> btsmtl.apply_document(expected_document_hash)
  -> Character需要产物时显式调用精确Definition Build生命周期
  -> btsmtl.validate
```

Unity资产是正式真相，`.btsmtl/`目录是单一逻辑Document和AI工作副本。Document不在`Assets/`内，不进入Player、Bundle或generated product。禁止使用BTSMTL局部节点/边编辑工具、直接编辑Unity YAML、`execute_code`、反射、剪贴板、临时菜单、文件监听器、Patch inbox或第二套mutation service。

Document不会自动编译或自动apply。Unity树变化和Document变化只计算同步状态；只有显式`apply_document`才修改Unity authoring资产。Character Presentation Profile、Pose Graph与PoseStateMachine属于同一Document目标、Reconciler和资产级apply事务；Character Program、Presentation Projection与Native Pose Program不属于Document事务，必须在apply成功后通过精确Definition的Character Build生命周期显式发布。

修改C#、OpenSpec和Skill文件继续使用Codex文件工具，不通过Unity MCP写代码。

## 资产修改流程

1. 明确`CharacterController`或`AIController` domain，并确定对应Definition的精确`Assets/...`路径。不得按目录、显示名、selection或场景猜root。
2. 确认Unity不在编译、更新AssetDatabase、Play Mode或切换Play Mode。
3. 调用`btsmtl.checkout_document`。读取返回的绝对`packagePath`、`syncState`、`sourceRevision`、`editableHash`和`contextHash`。
4. 先读`manifest.json`以及`context/node-catalog.json`、`context/graph-kinds.json`、`context/asset-catalog.json`、`context/dependencies.json`和`readonly/presentation/linked-pose-interfaces/*/interface.json`，再用通用文件工具修改`editable/**/*.json`。Character Presentation目标位于`editable/presentation/profile.json`、`editable/presentation/pose-graphs/*/{graph,layout}.json`、`editable/presentation/pose-state-machines/*/{state-machine,layout}.json`与`editable/presentation/linked-pose-implementations/*`完整闭包。不得修改`manifest.json`、`.sync.json`、`context/**/*.json`或`readonly/**/*.json`。
   - 新增Pose State Graph或Subgraph时，graph id必须是`local:<meaningful-id>`，并一次创建同目录`graph.json`和`layout.json`。目录segment由完整local id确定：把非字母数字、`-`、`_`字符替换为`-`，去掉首尾`-`，截取前48字符，再追加`-`和完整local id的SHA-256前12位小写十六进制。
   - 新增Graph-owned Inline Timeline时，timeline id、唯一TimelineNode调用点、Track与Clip都必须使用`local:<meaningful-id>`；一次创建同目录`timeline.json`与`curves.json`，目录使用相同canonical segment算法。controller Timeline摘要、Graph节点、callSite和文件对必须指向同一local TimelineNode与Timeline。
   - Graph、Node、Edge等新实体使用`local:*`；`contentRevision`是版本值而不是实体identity，不得使用带冒号的`local:*`，应提交合法非空revision。
   - dry-run只会把合法的local Pose Graph文件对与graph-owned Inline Timeline文件对加入服务端有效manifest。缺少配对、非canonical目录、非local id、非法role、非唯一调用点或其它manifest外文件仍会严格失败。AI不得直接修改manifest。
   - dry-run返回的document hash已经锁定扩展后的完整文件闭包；apply成功后reverse export把local identity替换为stable identity，并由service发布新的canonical manifest。
5. Graph文件只表达stable capability、typed properties、逻辑port和system anchor。Pose Graph节点必须使用共享Capability提供的typed payload字段、port与role约束。已有实体保持stable authoring identity；新实体使用`local:<meaningful-id>`，不得写C#类型名、序列化field、compiler index、generated payload、冗余port镜像或系统节点正文。
   - 节点端口随typed property变化时，Capability必须声明严格`portVariants`。唯一Node Port Shape Projector把固定端口、唯一命中的条件端口和作者拥有的动态端口合成完整形状；Canvas、Document、Reconciler、Mutation与Validator不得各自判断mode或从默认构造节点推断端口。
6. 调用`btsmtl.dry_run_document`。必须处理机器可读`path/code/message/suggestion`，并确认`plannedDiff`符合业务目标。
7. 仅当dry-run成功时，把其返回的精确`documentHash`原样作为`expected_document_hash`调用`btsmtl.apply_document`；任一editable文件变化后都必须重新dry-run。
8. apply成功必须同时满足`success=true`、`applied=true`、`saved=true`和`syncState=Clean`。Character的Gameplay、Timeline与Presentation owner进入同一资产级事务，Document从最终Unity树反向导出，local identity被真实stable identity替换。任一Mutation、Validator、保存或反向发布失败都必须完整回滚并返回`syncState=ApplyFailed`。
   - AI schema normalization未改变AI authoring语义且受控Character Program已过期时，apply只验证并保存AI authoring，`AIIntentProgram`保持stale；不得加载旧Numeric Target或自动Build Character。
   - AI authoring语义真实变化时仍必须通过当前Character Program的正式AI Compiler校验。Character Program过期时必须先按精确Definition重新发布Character产物，不能用authoring catalog代替generated identity发布AIIntentProgram。
9. Character authoring语义变化且需要正式产物时，显式调用`character.build_float32_products(definition_asset_path)`；需要Fixed wrapper时再调用`character.build_fixed_products(definition_asset_path, wrapper_asset_path)`。两个工具都只接受精确路径，不读取selection、不扫描目录、不自动触发。
10. Character Build后重新checkout刷新generated context，再调用`btsmtl.validate`确认正式authoring与compiler约束。

同步状态：

- `Clean`：树、context和Document正文都与基线一致。
- `TreeDirty`：Unity侧变化，Document正文未变；重新checkout可刷新。
- `DocumentDirty`：Document正文变化，Unity侧未变；可dry-run/apply。
- `Conflict`：Unity侧和Document正文都变化；不得apply。

`btsmtl.rebase_document`只在显式`confirm_rebase=true`时接受当前Unity树和context为新基线，保留Document目标正文，不修改Unity资产，也不发布产物。

## 可写与只读边界

Character Document v4正式可写：

- Blackboard declaration的基础字段，以及可选`inputBinding.inputValueId`和可选`factProjection`。禁止旧变量级网络策略字段、旧mode枚举、旧平铺input/projection字段或AI Character payload。
- Pose Graph-owned typed Source Slot、Profile-owned direct Clip/Blend Space/Motion Matching Binding、Locomotion Sync Group、policy与有限Action producer binding。`editable/animation-clips/**/curves.json`只允许修改当前Definition可达原生AnimationClip的注册表现Curve；Timeline Animation Segment直接引用AnimationClip。
- root-owned Pose Graph catalog中的Graph、layout、parameter、节点typed payload、dynamic port与edge。
- Linked Pose Implementation及其Entry Graph闭包、Profile Group binding、通用selector envelope和Equipment精确mapping；Interface正文只读。
- PoseStateMachine的entry、state、alias、transition、transition rule与blend策略；可选Locomotion Phase relation只从Profile Group与Clip曲线编译，不是Transition可写字段。
- PoseStateMachine同目录layout只稀疏保存Entry、State与Alias的稳定identity和有限二维位置；纯layout apply不修改StateMachine `ContentRevision`，也不触发Build。

以下内容只作为现有资产引用或`context`事实，不能通过Document创建或修改：

- Rig Definition、Bone、Virtual Bone及其绑定和生成数据。
- Body Motion、Foot Analysis、Motion Matching索引与其它算法生成内容。
- generated Character Program、Presentation Projection、Native Pose Program和AIIntentProgram身份与stale状态。
- AI受控Character的Input/Request合同与capability catalog。

Pose Graph-owned Source Slot与Profile-owned Source Binding允许通过同一Document事务创建、重命名、配置和删除；Clip Binding直接引用现有原生AnimationClip，Profile唯一装配Rig、Analysis Source与Locomotion Sync Group。Document只可修改注册Curve，不得创建Clip、修改骨骼曲线、AnimationEvent、import设置、Foot Analysis或generated payload。Linked Pose Interface以readonly context提供identity、revision、signature、Fact contract、Entry和typed ports；Implementation、Entry Graph、Group、selector与Equipment mapping通过同一typed Presentation Mutation和资产事务创建、配置、删除，并支持新对象`local:*`计划identity。Pose Graph必须通过唯一共享Capability表达节点、typed payload、port与Document role，不得增加Pose专用MCP action、直接切换活动runtime Implementation或第二套Reconciler/Mutation入口。

Presentation目标必须把State-local Pose Source与Action AnimationChannel分开：Pose Player的`pose-source-slot`必须是精确Graph-owned typed Slot对象引用，`profile.json.poseSources`必须用精确Slot与Binding子资产对象引用绑定实际资源；不得按名称、路径、数组index或字符串identity猜测。Projection编译后Runtime只按dense source index解析资源；按PlayerNodeId生成的typed provider identity只做帧内路由，不进入Document或资源查找。ActionPlaybackInput与AnimationSlot只引用Timeline目标状态中已存在的Animation Channel，AnimationSlot仍是Action channel唯一consumer；有限Action producer必须引用现有Timeline与Animation track。

PoseStateMachine Transition混合字段固定为`blendLogic`、`durationSeconds`、`blendMode`、条件式`customBlendCurveAssetId`与`blendProfileAssetId`。Curve/Profile必须从只读Asset Catalog解析为强类型资产并提交同一Presentation Mutation；禁止恢复`blendCurveId`、旧`blendProfileId`或GUID文本输入。Custom必须带Curve Asset，非Custom不得保留Curve Asset；BlendStack只作为显式Pose Graph节点存在。

不得恢复旧MotionMatchingSelectionInput、AnimationSelection port、素材Marker/Notify、旧Layer、PoseSlot、TransitionLibrary、presentation第二MCP入口或generated payload写入。Timeline只拥有Action Segment与Timeline-local Curve；素材骨骼和两项注册表现Curve统一由原生AnimationClip拥有。

## 修改相关代码

authoring代码变化只要改变Agent能看到、能写入、能创建、能连接或必须验证的语义，就同步检查：

| 变化 | 必须同步 |
|---|---|
| Graph、Node、Edge、Port、StateMachine、Source Slot/Binding子资产或ownership | Document模型、Exporter、Reconciler、Mutation handler、Validator |
| Timeline、Track、AnimationClip Segment、Timeline-local Curve或MotionWarp | Document投影、Reconciler顺序、Timeline handler、Validator |`n| AnimationClip注册Curve或Profile Locomotion Sync Group | Document v4 Clip分片、Presentation exporter/reconciler、Clip Curve Mutation、Validator |
| Input、ActionProfile、ActionContext或Blackboard identity | editable/context分区、Reconciler、AssetResolver、Validator |
| AI Definition、Perception、Memory、Observation或Intent | AI editable/context、AI Snapshot、Reconciler、AI Compiler |
| Presentation Profile、Pose Graph或PoseStateMachine | Document v4模型、Presentation codec/exporter、唯一Reconciler、typed Presentation Mutation、Validator与五工具说明 |
| Rig、Bone、Virtual Bone、Body Motion、Foot Analysis或generated product | 只读context、context hash与current spec；不得增加Document Mutation |
| MCP生命周期或事务生命周期 | application service、五个MCP薄桥、Editor Window、current spec、此技能 |

正式调用链必须保持：

```text
Package manifest + strict per-file parser
  -> AgentDocumentMutationReconciler
  -> AgentAuthoringPresentationReconcilerV4
  -> immutable AgentMutationPlan
  -> Mutation preflight
  -> one Undo transaction
  -> typed Gameplay/Timeline/Presentation Mutation handlers
  -> formal Validator
  -> save formal authoring
  -> canonical reverse export
```

Character需要产物时，必须在上述Document事务完成后另行调用精确Definition的Build生命周期。

Reconciler只计算差异，不修改Unity对象。Mutation compiler/handler不拥有Undo、rollback、dirty、SaveAssets或Document写回。Document Transaction Service唯一拥有事务生命周期，并以同目录staging校验、package内容镜像与rollback副本完成Windows安全发布。

如果Document实体无法映射到正式Mutation，必须返回明确错误并扩展唯一Reconciler/handler链，不能手改YAML或添加fallback。

完整字段和代码地图见[当前合同](references/current-contract.md)。

## 完成门槛

- 外部不存在Intent、Macro、Patch IR、operation catalog、bootstrap action或旧action alias。
- BTSMTL MCP和Window只暴露`btsmtl.checkout_document`、`btsmtl.rebase_document`、`btsmtl.dry_run_document`、`btsmtl.apply_document`、`btsmtl.validate`。
- 旧Pose State inline Graph只通过`character.migrate_legacy_pose_state_graphs(definition_asset_path)`一次性迁入GraphCatalog；该工具不读取selection、不扫描、不build。
- Character generated product通过独立`character.build_float32_products`与`character.build_fixed_products`生命周期发布；它们不是BTSMTL局部编辑工具。
- 不存在BTSMTL局部节点/边/属性修改工具；AI直接修改package文件。
- Package严格拒绝清单外文件、未知字段、重复属性、非法数值、`.sync.json`语义改动和read-only context改动；Character必须包含完整Presentation目标文件闭包，每个Pose StateMachine必须同时具有`state-machine.json`与`layout.json`。
- manifest外只允许由服务发现完整canonical `local:*` Pose State Graph/Subgraph的`graph.json + layout.json`创建对，以及graph-owned Inline Timeline的`timeline.json + curves.json`创建对；两者都属于同一Store、hash和apply生命周期，不是未知文件fallback。
- dry-run不dirty、不保存、不build；apply使用同一Document hash，Character apply不build。
- apply失败必须同时恢复Unity owner与正式package并返回`ApplyFailed`；成功后Document从最终树规范化并回到`Clean`。
- 没有watcher、selection/focus自动执行、第二套graph/timeline/AI mutation service。
- 不运行Unity batchmode，不新增测试，除非用户明确要求。
- dotnet build使用`--disable-build-servers /nr:false /p:UseSharedCompilation=false`，随后立即`dotnet build-server shutdown`。

## 故障处理

- checkout返回`presentation_pose_state_graph_migration_required`时，不得直接改YAML或读取inline数据继续导出。调用`character.migrate_legacy_pose_state_graphs`并传入报错root的精确`definition_asset_path`，完成GraphCatalog结构迁移后重试checkout。该迁移不build。

- `play_mode_active`：退出Play Mode。
- `editor_busy`：等待当前编译或导入结束。
- `DocumentDirty`：正常执行`btsmtl.dry_run_document`。
- `TreeDirty`：Document未编辑时重新checkout；需要保留目标正文时显式rebase。
- `Conflict`：先决定以哪侧为基线，再显式rebase或人工整理Document；禁止自动合并。
- `ApplyFailed`：资产级事务已经回滚；读取机器诊断，修复同一Document目标后重新dry-run，不得复用旧hash。
- `readonly_context_modified`或`sync_header_modified`：恢复这些service-owned字段，只编辑`editable`。
- `document_hash_changed`：重新调用`btsmtl.dry_run_document`，不复用旧hash。
- `transaction_owner_*`：修复ownership，使所有正式与generated owner进入同一事务，禁止缩小Undo范围。
- MCP transport错误：修复连接或Editor编译状态，禁止改走YAML、`execute_code`或旧Patch入口。
