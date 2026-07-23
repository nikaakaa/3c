## RENAMED Requirements

- FROM: `### Requirement: MCP 和 EditorWindow 必须共用唯一 Patch application service`
- TO: `### Requirement: MCP 和 EditorWindow 必须共用唯一 Document application service`
- FROM: `### Requirement: Apply 必须执行预检和资产级事务`
- TO: `### Requirement: Document Apply必须执行hash门禁、预检和资产级事务`
- FROM: `### Requirement: MCP bridge 必须透传同一 v16 Character 与 AI 事务`
- TO: `### Requirement: MCP bridge必须透传同一Document Character与AI事务`

## MODIFIED Requirements

### Requirement: Agent authoring 必须通过现有 Unity MCP 暴露单一桥接工具

系统 MUST在现有`unityMCP`连接中只注册一个`manage_btsmtl_agent_authoring` editor-only工具。工具 MUST使用当前MCP package正式发现与分发机制，MUST不启动第二个server、终端常驻进程、文件watcher或Unity batchmode。Document action只负责显式同步生命周期，不把每种节点和mutation暴露为独立MCP tool。

#### Scenario: Agent调用bridge

- **WHEN** Codex调用`manage_btsmtl_agent_authoring`
- **THEN** 请求 MUST由当前Unity Editor会话和统一Document application service处理
- **AND** bridge MUST不创建额外transport或业务mutation tool

### Requirement: MCP bridge 必须提供完整且受限的 authoring action 集合

`manage_btsmtl_agent_authoring` MUST只提供`checkout_document`、`rebase_document`、`dry_run_document`、`apply_document`和`validate`五个action。全部action MUST要求明确`domain`与`root_asset_path`；`apply_document`还 MUST要求`expected_document_hash`。Document路径 MUST由service确定并返回，调用方 MUST不提交任意document path或JSON正文。未知domain、action、root类型或缺失参数 MUST在修改资产前返回结构化错误。

#### Scenario: Checkout当前树

- **WHEN** Codex对合法root调用`checkout_document`
- **THEN** bridge MUST返回确定性Document路径、同步状态、source revision和content hash
- **AND** MUST不修改Unity资产或触发build

#### Scenario: 预检Document

- **WHEN** Codex调用`dry_run_document`
- **THEN** bridge MUST返回document hash、plan hash、planned diff、messages和metrics
- **AND** MUST不dirty、save或build

#### Scenario: 调用旧Patch action

- **WHEN** MCP请求携带`export_snapshot`、`dry_run_patch`、`apply_patch`或`bootstrap_ai_controller`
- **THEN** bridge MUST返回unsupported action
- **AND** MUST不转换到Document action

### Requirement: MCP 和 EditorWindow 必须共用唯一 Document application service

系统 MUST使用唯一Document application service统一编排checkout、sync state、rebase、strict parse、reconcile、dry-run、apply、validator、Undo、保存和反向canonical export。MCP handler与Agent Controller Window MUST调用同一service，MUST不复制Document Store、Reconciler或Mutation生命周期。

#### Scenario: Window执行Document dry-run

- **WHEN** 作者在Agent窗口显式请求dry-run
- **THEN** Window MUST调用统一service
- **AND** 返回语义 MUST与MCP `dry_run_document`一致

#### Scenario: MCP执行Document apply

- **WHEN** Codex调用`apply_document`
- **THEN** bridge MUST把请求交给统一service
- **AND** handler MUST不直接调用BTSMTL结构编辑API

### Requirement: Document Apply必须执行hash门禁、预检和资产级事务

`apply_document` MUST先重新读取确定性Document，校验expected document hash、live source revision、root identity和同步状态，再执行无副作用reconcile与preflight。全部门禁成功后，系统 MUST对Definition和全部可达serialized owner建立单一Undo事务，调用Mutation Compiler、domain Validator、save、显式generated product发布与最终Document反向规范化。任一错误或异常 MUST回滚，MUST不保存半成品或报告Clean。

#### Scenario: Document hash变化

- **WHEN** dry-run后Document semantic hash发生变化
- **THEN** apply MUST在建立mutation前失败
- **AND** MUST要求重新dry-run

#### Scenario: Apply后验证失败

- **WHEN** Mutation完成但domain Validator报告错误
- **THEN** service MUST回滚当前Undo group覆盖的全部owner
- **AND** Document MUST保持待修改状态

#### Scenario: Apply完整成功

- **WHEN** hash、revision、preflight、mutation、Validator、save和generated product发布全部成功
- **THEN** service MUST从最终树原子写回canonical Document
- **AND** response MUST明确`applied=true`、`saved=true`和`syncState=Clean`

### Requirement: Bridge 必须复用正式 Agent compiler 与 BTSMTL authoring API

MCP bridge MUST复用canonical Snapshot/Document exporter、Document Reconciler、Mutation Compiler、domain Validator和Compile Report。全部Graph修改 MUST继续由typed handler通过`BaseGraph.CreateNode`、`BaseGraph.Link`、`BaseGraph.LinkProperty`、Timeline与AI正式authoring API执行。Bridge MUST不直接写Unity YAML、节点集合、边集合、GUID映射或建立第二套Graph数据。

#### Scenario: Document新增状态和Transition

- **WHEN** MCP dry-run读取包含新State与Transition的Document
- **THEN** Reconciler MUST生成受capability和graph kind约束的typed Mutation
- **AND** bridge MUST不解释节点创建顺序

#### Scenario: Document包含未知节点

- **WHEN** Document包含不支持的node type或port
- **THEN** bridge MUST返回Reconciler或Compiler的明确错误
- **AND** MUST不创建placeholder、执行动态代码或写序列化字段

### Requirement: Definition 目标必须由调用上下文显式提供

MCP与Window请求 MUST通过`domain`和`root_asset_path`显式选择已有合法`CharacterPipelineDefinition`或`AIControllerDefinition`。路径 MUST是`Assets/`下能精确解析为对应domain根类型的资产。Document MUST保存root identity和source revision，但Document路径由service从调用上下文确定。系统 MUST不通过selection、目录扫描、同名匹配、场景对象、剪贴板或旧配置寻找root或Document。

#### Scenario: Definition路径合法

- **WHEN** 请求给出匹配domain的精确Definition路径
- **THEN** service MUST以该Definition及正式引用链作为checkout、reconcile和validate上下文
- **AND** Document MUST只作用于该root

#### Scenario: Definition不存在

- **WHEN** root路径缺失、类型错误或资产不存在
- **THEN** bridge MUST在checkout前返回错误
- **AND** MUST不调用已删除的bootstrap或创建临时root

### Requirement: 临时剪贴板和快捷键入口必须删除

系统 MUST不保留Patch clipboard、快捷键、Patch inbox、任意Document path、文件watcher或隐藏菜单作为MCP不可用时的fallback。唯一Agent JSON工作文件 MUST由正式Document Store确定，且只由显式Document action读取或写回。人工authoring继续使用正式Graph、Timeline和Profile入口。

#### Scenario: JSON文件被保存

- **WHEN** AI直接保存正式Document
- **THEN** 文件watcher MUST不自动apply或build
- **AND** 下一次显式action MUST重新读取文件并推导状态

#### Scenario: MCP不可用

- **WHEN** Unity MCP未连接或自定义工具加载失败
- **THEN** 系统 MUST明确报告连接或编译问题
- **AND** MUST不自动改用剪贴板、菜单、临时Patch或文件监视器

### Requirement: Bridge 必须拒绝不安全的 Editor 状态

Bridge MUST在Unity编译、AssetDatabase更新、Play Mode或Play Mode切换期间拒绝全部Document action。系统 MUST返回明确状态错误，MUST不排队、延迟执行或启动额外进程等待。普通selection、Inspector focus和Document保存不得触发action。

#### Scenario: Unity正在编译

- **WHEN** Codex在domain reload或脚本编译期间调用checkout或apply
- **THEN** bridge MUST返回editor busy
- **AND** MUST不读取半加载Graph或写Document

#### Scenario: 用户只选中Definition

- **WHEN** Project selection变为Character或AI root
- **THEN** 系统 MUST不自动checkout、validate或build
- **AND** 必须等待明确action

### Requirement: MCP 返回必须保留机器可读诊断

Bridge response MUST保留action、domain、root path、Document path、sync state、source revision、content/document/plan hash、success、applied、saved，以及Snapshot摘要或Compile Report。Report MUST保留Document entity path、code、severity、message、suggestion、planned/applied diff和metrics，MUST不只返回Console字符串。

#### Scenario: Document reconcile失败

- **WHEN** Reconciler拒绝一个Document entity
- **THEN** response MUST包含entity path、错误code、原因和建议
- **AND** Codex MUST能直接修改同一Document后重新dry-run

#### Scenario: Document apply成功

- **WHEN** `apply_document`完成并反向同步
- **THEN** response MUST包含最终applied diff、validation、新revision和新content hash
- **AND** MUST明确报告Clean

### Requirement: MCP bridge必须透传同一Document Character与AI事务

BTSMTL Agent MCP bridge MUST接受并返回`btsmtl-agent-authoring-document.v1`同步与validation结果，并通过显式domain透传CharacterController或AIController generic事务。Character Document MUST覆盖现有正式State、Action、Timeline、MotionWarp、Marker、Curve等可写语义；AI Document MUST覆盖Definition、Graph、Blackboard、Perception、Observation、Memory与Intent语义。Bridge MUST只调用统一Document Store、Reconciler、Mutation、transaction和Validator，不得新增domain专用action、Patch JSON、SerializedProperty、YAML、反射、任意字段写入或旧schema转换。

#### Scenario: Character Document修改Curve

- **WHEN** Character Document修改registered ChannelId的完整curve
- **THEN** bridge MUST把规范entity交给同一Reconciler与typed Mutation链
- **AND** handler MUST调用唯一Catalog descriptor和MutationAdapter

#### Scenario: AI Document修改Intent

- **WHEN** AI Document增加合法Character input/request binding
- **THEN** bridge MUST把同一Document交给统一service
- **AND** response MUST返回Mutation Plan、事务与Validator产生的机器报告

#### Scenario: bridge收到旧schema

- **WHEN** 调用方提交v15-v17 Snapshot、Patch、operation或`patch_json`
- **THEN** bridge MUST返回unsupported schema或unsupported parameter
- **AND** MUST不转换为Document v1

