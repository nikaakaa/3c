## RENAMED Requirements

- FROM: `### Requirement: Agent authoring 必须通过现有 Unity MCP 暴露单一桥接工具`
- TO: `### Requirement: Agent authoring 必须通过现有 Unity MCP 暴露固定生命周期工具集`
- FROM: `### Requirement: MCP bridge 必须提供完整且受限的 authoring action 集合`
- TO: `### Requirement: MCP bridge 必须提供完整且受限的生命周期工具集合`
- FROM: `### Requirement: MCP 和 EditorWindow 必须共用唯一 Patch application service`
- TO: `### Requirement: MCP 和 EditorWindow 必须共用唯一 Document application service`
- FROM: `### Requirement: Apply 必须执行预检和资产级事务`
- TO: `### Requirement: Document Apply必须执行hash门禁、预检和资产级事务`
- FROM: `### Requirement: MCP bridge 必须透传同一 v17 Character 与 AI 事务`
- TO: `### Requirement: MCP bridge必须透传同一Document Character与AI事务`

## MODIFIED Requirements

### Requirement: Agent authoring 必须通过现有 Unity MCP 暴露固定生命周期工具集

系统 MUST在现有`unityMCP`连接中注册`btsmtl.checkout_document`、`btsmtl.rebase_document`、`btsmtl.dry_run_document`、`btsmtl.apply_document`与`btsmtl.validate`五个editor-only工具。工具 MUST使用当前MCP package正式发现与分发机制，MUST不启动第二个server、终端常驻进程、文件watcher或Unity batchmode。系统 MUST删除`manage_btsmtl_agent_authoring`及其`action`multiplexer，MUST不注册Node、Edge、Timeline、字段或JSON patch领域工具。

#### Scenario: Unity Editor完成domain reload

- **WHEN** 当前Editor assembly与Unity MCP package成功加载
- **THEN** custom tool discovery MUST发现五个固定生命周期工具
- **AND** MUST不再发现`manage_btsmtl_agent_authoring`

#### Scenario: Agent需要创建Graph节点

- **WHEN** AI需要在文档包中创建Node与Edge
- **THEN** AI MUST直接修改对应JSON目标状态
- **AND** MCP MUST不暴露`create_node`、`link_edge`或等价局部工具

### Requirement: MCP bridge 必须提供完整且受限的生命周期工具集合

五个工具 MUST各自使用独立input schema、output schema与行为annotations。全部schema MUST拒绝额外参数。工具 MUST只接收明确`domain`与`root_asset_path`；rebase额外要求显式`confirm_rebase`，apply额外要求`expected_document_hash`。Document package路径 MUST由service计算并返回，调用方 MUST不提交任意package path、JSON正文、Patch或action字段。

#### Scenario: Checkout当前树

- **WHEN** Codex对合法root调用`btsmtl.checkout_document`
- **THEN** bridge MUST返回确定性package绝对路径、同步状态、source revision与hash摘要
- **AND** MUST不修改Unity资产或触发build

#### Scenario: 预检Document

- **WHEN** Codex调用`btsmtl.dry_run_document`
- **THEN** bridge MUST返回document hash、plan hash、planned diff、messages与metrics
- **AND** MUST不dirty、save、build或publish

#### Scenario: 调用旧工具或提交旧参数

- **WHEN** 调用方使用`manage_btsmtl_agent_authoring`、`patch_json`、`action`或任意document path
- **THEN** MCP MUST返回unknown tool或严格schema错误
- **AND** MUST不转换为v2生命周期请求

### Requirement: MCP 和 EditorWindow 必须共用唯一 Document application service

系统 MUST使用唯一Document application service统一编排checkout、sync state、rebase、strict package parse、reconcile、dry-run、apply、Validator、Undo、保存与反向package发布。五个MCP handler与Agent Controller Window MUST调用同一service，MUST不复制Document Store、Reconciler或Mutation生命周期。

#### Scenario: Window执行Document dry-run

- **WHEN** 作者在Agent Window显式请求dry-run
- **THEN** Window MUST调用统一service
- **AND** 返回语义 MUST与`btsmtl.dry_run_document`一致

#### Scenario: MCP执行Document apply

- **WHEN** Codex调用`btsmtl.apply_document`
- **THEN** bridge MUST把请求交给统一service
- **AND** handler MUST不直接调用BTSMTL结构编辑API

### Requirement: Document Apply必须执行hash门禁、预检和资产级事务

`btsmtl.apply_document` MUST重新读取确定性文档包，校验expected document hash、live source revision、current context hash、root identity和同步状态，再执行无副作用reconcile与preflight。全部门禁成功后，系统 MUST对Definition和全部可达serialized owner建立单一Undo事务，调用Mutation Compiler、domain Validator、save与最终文档包反向发布。AI domain MAY在事务内发布AIIntentProgram；Character domain MUST不在Document apply内Build。任一错误或异常 MUST回滚，MUST不保存半成品或报告Clean。

#### Scenario: Document hash变化

- **WHEN** dry-run后任一editable文件semantic hash变化
- **THEN** apply MUST在mutation前失败
- **AND** MUST要求重新dry-run

#### Scenario: Apply后验证失败

- **WHEN** Mutation完成但domain Validator报告错误
- **THEN** service MUST回滚当前Undo group覆盖的全部owner
- **AND** 文档包 MUST保持待修改状态

#### Scenario: Apply完整成功

- **WHEN** hash、revision、preflight、Mutation、Validator、save、该domain事务内产物与package发布全部成功
- **THEN** response MUST明确`applied=true`、`saved=true`与`syncState=Clean`
- **AND** 最终package MUST来自最终正式Unity树

### Requirement: Character generated product必须使用精确独立Build生命周期

系统 MUST在同一Unity MCP连接中提供`character.build_float32_products`与`character.build_fixed_products`。Float32工具 MUST只接收精确`definition_asset_path`并原子发布该Definition的Float32 wrapper与Presentation Projection。Fixed工具 MUST只接收精确`definition_asset_path`和精确`wrapper_asset_path`并原子发布指定Fixed wrapper与同一Projection。两个工具 MUST拒绝未知参数，MUST不读取selection、扫描目录、猜测Definition或自动触发。

#### Scenario: Apply后显式重建Corin产物

- **WHEN** Character Document apply成功且调用方传入精确Corin Definition路径
- **THEN** Float32工具 MUST发布该Definition的Float32 wrapper与Projection
- **AND** Fixed工具 MUST只把Fixed wrapper发布到调用方指定destination
- **AND** response MUST返回Program、Projection、Numeric ABI、hash与编译诊断

### Requirement: 旧Pose State Graph必须使用精确一次性迁移生命周期

系统 MUST在同一Unity MCP连接中提供`character.migrate_legacy_pose_state_graphs`。工具 MUST只接收精确`definition_asset_path`，调用唯一`CharacterPresentationPoseGraphMigrationService`把旧inline Pose Graph迁入GraphCatalog并替换Pose State引用。工具 MUST不读取selection、不扫描资产、不Build、不修改Pose Runtime或Compiler。成功结果 MUST返回`success/applied/saved`、迁移State与Graph数量、Definition与Presentation路径、GUID和revision；失败结果 MUST返回typed code、path、message与remediation。

#### Scenario: Checkout被旧inline Pose Graph阻塞

- **WHEN** checkout返回`presentation_pose_state_graph_migration_required`
- **AND** 调用方把同一精确Definition路径传给迁移工具
- **THEN** 工具 MUST只执行一次性Presentation结构迁移并保存正式Presentation资产
- **AND** MUST不触发Program或Projection Build
- **AND** 迁移后调用方 MAY重新执行checkout

### Requirement: Bridge 必须复用正式 Agent compiler 与 BTSMTL authoring API

MCP bridge MUST复用v2 package exporter、Document Reconciler、Mutation Compiler、domain Validator和Compile Report。全部Graph修改 MUST继续由typed handler通过`BaseGraph.CreateNode`、`BaseGraph.Link`、`BaseGraph.UnLink`、`BaseGraph.LinkProperty`、正式Property Edge断开、Timeline与AI authoring API执行。Bridge MUST不直接写Unity YAML、Node集合、Edge集合、GUID映射或建立第二套Graph数据。

#### Scenario: Document新增状态和Transition

- **WHEN** dry-run读取包含新State、body Graph、Node与Transition的package
- **THEN** Reconciler MUST生成受capability与Graph kind约束的typed Mutation
- **AND** bridge MUST不解释创建顺序

#### Scenario: Document包含未知Node或port

- **WHEN** package包含catalog不支持的kind或逻辑port
- **THEN** bridge MUST返回Reconciler或Compiler明确错误
- **AND** MUST不创建placeholder、执行动态代码或写SerializedProperty

### Requirement: Definition 目标必须由调用上下文显式提供

MCP与Window请求 MUST通过`domain`和`root_asset_path`显式选择已有合法`CharacterPipelineDefinition`或`AIControllerDefinition`。路径 MUST是`Assets/`下能精确解析为对应domain根类型的资产。文档包路径 MUST由service从调用上下文确定。系统 MUST不通过selection、目录扫描、同名匹配、场景对象、剪贴板或旧配置寻找root或文档包。

#### Scenario: Definition路径合法

- **WHEN** 请求给出匹配domain的精确Definition路径
- **THEN** service MUST以该Definition及正式引用链作为checkout、reconcile和validate上下文
- **AND** 文档包 MUST只作用于该root

#### Scenario: Definition不存在

- **WHEN** root路径缺失、类型错误或资产不存在
- **THEN** bridge MUST在checkout前返回错误
- **AND** MUST不创建临时root或调用已删除bootstrap

### Requirement: 临时剪贴板和快捷键入口必须删除

系统 MUST不保留Patch clipboard、快捷键、Patch inbox、任意Document path、文件watcher或隐藏菜单作为MCP不可用时的fallback。AI MUST通过宿主已有通用文件能力直接编辑正式文档包；BTSMTL MCP MUST只负责生命周期。人工authoring继续使用正式Graph、Timeline和Profile入口。

#### Scenario: JSON文件被保存

- **WHEN** AI直接保存文档包文件
- **THEN** 文件watcher MUST不自动apply、validate或build
- **AND** 下一次显式生命周期工具 MUST重新读取整包并推导状态

#### Scenario: MCP不可用

- **WHEN** Unity MCP未连接或custom tool加载失败
- **THEN** 系统 MUST明确报告连接或编译问题
- **AND** MUST不自动改用剪贴板、菜单、临时Patch或文件监视器

### Requirement: Bridge 必须拒绝不安全的 Editor 状态

Bridge MUST在Unity编译、AssetDatabase更新、Play Mode或Play Mode切换期间拒绝全部生命周期工具。系统 MUST返回明确状态错误，MUST不排队、延迟执行或启动额外进程等待。普通selection、Inspector focus和JSON保存不得触发任何工具。

#### Scenario: Unity正在编译

- **WHEN** Codex在domain reload或脚本编译期间调用checkout或apply
- **THEN** bridge MUST返回editor busy
- **AND** MUST不读取半加载Graph或写文档包

#### Scenario: 用户只选中Definition

- **WHEN** Project selection变为Character或AI root
- **THEN** 系统 MUST不自动checkout、validate、compile或build
- **AND** MUST等待明确工具调用

### Requirement: MCP 返回必须保留机器可读诊断

每个工具 MUST声明结构化output schema。成功结果 MUST保留tool、domain、root path、package path、sync state、source revision、editable/context/document/plan hash、success、applied与saved等适用字段。Report MUST保留跨文件entity path、code、severity、message、suggestion、planned/applied diff与metrics，MUST不只返回Console字符串，也 MUST不嵌入完整文档包JSON。

#### Scenario: Document reconcile失败

- **WHEN** Reconciler拒绝一个Graph entity
- **THEN** tool execution error MUST包含文件路径、entity path、错误code、原因和建议
- **AND** Codex MUST能直接修改同一文件后重新dry-run

#### Scenario: Document apply成功

- **WHEN** apply完成并反向同步
- **THEN** response MUST包含最终applied diff、validation、新revision和新document hash
- **AND** MUST明确报告Clean

### Requirement: MCP bridge必须透传同一Document Character与AI事务

五个BTSMTL lifecycle tool MUST接受并返回`btsmtl-agent-authoring-document.v2`同步与validation结果，并通过显式domain透传CharacterController或AIController generic事务。Character package MUST覆盖State、Action、Timeline、MotionWarp、Marker、Curve、Node与Edge可写语义；AI package MUST覆盖Definition、Graph、Blackboard、Perception、Observation、Memory与Character input/request intent binding。Bridge MUST只调用统一Store、Reconciler、Mutation、transaction和Validator，不得新增domain专用action、Node级tool、Patch JSON、YAML、反射、任意字段写入或旧schema转换。

#### Scenario: Character Document修改Property Edge

- **WHEN** Character package增加、删除或重接Property Edge目标状态
- **THEN** bridge MUST把整包交给同一Reconciler与typed Mutation链
- **AND** handler MUST调用正式Property Edge authoring API

#### Scenario: AI Document修改Intent binding

- **WHEN** AI package增加合法Character input/request binding
- **THEN** bridge MUST把同一整包交给统一service
- **AND** response MUST返回Mutation Plan、事务与Validator机器报告

#### Scenario: bridge收到旧schema

- **WHEN** 调用方提交v1单文件、v15-v17 Snapshot/Patch、operation或`patch_json`
- **THEN** bridge MUST返回unsupported schema或unsupported parameter
- **AND** MUST不转换为v2文档包
