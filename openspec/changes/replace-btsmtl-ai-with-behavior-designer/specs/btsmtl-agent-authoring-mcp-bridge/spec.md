## RENAMED Requirements

- FROM: `### Requirement: MCP bridge必须透传同一Document Character与AI事务`
- TO: `### Requirement: MCP bridge必须透传同一Document整包事务`

## MODIFIED Requirements

### Requirement: Definition 目标必须由调用上下文显式提供

MCP与Window请求 MUST通过`domain`和`root_asset_path`显式选择已有合法 CharacterController 或其它正式登记的非游戏AI领域根。路径 MUST是`Assets/`下能精确解析为对应domain根类型的资产。文档包路径 MUST由service从调用上下文确定。系统 MUST不通过selection、目录扫描、同名匹配、场景对象、剪贴板或旧配置寻找root或文档包。

#### Scenario: Definition路径合法

- **WHEN** 请求给出匹配domain的精确Definition路径
- **THEN** service MUST以该Definition及正式引用链作为checkout、reconcile和validate上下文
- **AND** 文档包 MUST只作用于该root

#### Scenario: Definition不存在

- **WHEN** root路径缺失、类型错误或资产不存在
- **THEN** bridge MUST在checkout前返回错误
- **AND** MUST不创建临时root或调用已删除bootstrap

#### Scenario: 指定已删除的游戏AI领域

- **WHEN** 请求使用 AIController domain 或旧 AI Definition 路径
- **THEN** bridge MUST返回不支持的领域而不创建任何作者输出
- **AND** MUST不通过插件图或另一 domain 代替该根

### Requirement: Document Apply必须执行hash门禁、预检和资产级事务

`btsmtl.apply_document` MUST重新读取确定性文档包，校验expected document hash、live source revision、current context hash、root identity和同步状态，再执行无副作用reconcile与preflight。全部门禁成功后，系统 MUST对Definition和全部可达serialized owner建立单一Undo事务，调用Mutation Compiler、domain Validator、save与最终文档包反向发布。Document apply MUST不执行 Program/Projection/插件行为产物 Build；各领域继续使用正式显式 Build 入口。任一错误或异常 MUST回滚，MUST不保存半成品或报告Clean。

#### Scenario: Document hash变化

- **WHEN** dry-run后任一editable文件semantic hash变化
- **THEN** apply MUST在mutation前失败
- **AND** MUST要求重新dry-run

#### Scenario: Apply后验证失败

- **WHEN** Mutation完成但domain Validator报告错误
- **THEN** service MUST回滚当前Undo group覆盖的全部owner
- **AND** 文档包 MUST保持待修改状态

#### Scenario: Apply完整成功

- **WHEN** hash、revision、preflight、Mutation、Validator、save与package发布全部成功
- **THEN** response MUST明确`applied=true`、`saved=true`与`syncState=Clean`
- **AND** 最终package MUST来自最终正式Unity树

### Requirement: MCP bridge必须透传同一Document整包事务

五个 BTSMTL lifecycle tool MUST接受并返回唯一 Document v5 同步和 validation 结果，并通过显式 domain 透传已登记的非游戏AI领域整包事务。Character package MUST覆盖控制 binding/参数、技能定义及局部结构、ActionProfile、Timeline、Timeline/Clip 曲线、直接 Clip binding、Locomotion Sync Group 和 Presentation 的正式可写语义；独立 Timeline 只按其正式 domain 合同处理。游戏 AIController package MUST不再被解析或发布。Bridge MUST只调用唯一 Store、Reconciler、Mutation、transaction 与 Validator，不新增插件 AI 专用工具、节点级工具、任意字段写入或旧 schema 转换。

#### Scenario: dry-run发现Clip Curve分片

- **WHEN** Character Document 包含 manifest 声明且引用已有原生 Clip 的 Curve 分片
- **THEN** dry-run MUST返回锁定整包的 exact document hash 与正式 Curve Mutation
- **AND** apply MUST只接受该 hash 并在成功反向导出后发布规范包
- **AND** Bridge MUST不增加 Clip/key 级写参数

#### Scenario: AI Document修改Intent binding

- **WHEN** 调用方提交旧 AIController package 或其 Intent 正文
- **THEN** bridge MUST拒绝已删除的领域
- **AND** MUST不调用 AI Compiler、创建插件资产或产生部分 Mutation

#### Scenario: bridge收到旧schema

- **WHEN** 调用方提交不受支持的旧 schema、Snapshot/Patch、operation 或 patch_json
- **THEN** bridge MUST返回明确的不支持结果
- **AND** MUST不转换为当前文档包或恢复兼容 reader
