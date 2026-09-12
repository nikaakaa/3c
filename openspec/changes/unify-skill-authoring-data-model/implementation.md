# Skill authoring 数据模型统一实施记录

## 当前状态

核心数据模型和代码链已经落地，正式 v8 package 重建尚未完成。当前 Unity 工程有其它任务留下的未提交 Native FSM、Timeline 和生成物改动；正式 checkout 已正确拒绝不完整的当前 authoring 闭包，没有绕过它们生成假包。

## 已完成

### 状态机转移唯一来源

- `BtsmtlSkillFlowConnection` 只序列化 `BtsmtlSkillTransferPayload`。
- Payload集中保存 `condition`、`priority`、`abortPolicy` 和 `order`。
- 状态 Enter、Any、State 和 Exit 使用固定逻辑端点；状态机边从 `Transfer` 到 `StateIn`。
- 同一来源的转移按显式 `order`排序并校验唯一性，不能按UID、位置或字典顺序推断。
- 普通 Sequence、Selector、Parallel 仍从节点 `properties.steps` 读取自身分支顺序；它们不再被误当成状态机转移。

### 统一消费者

以下链路已经改为读取同一 Edge 数据：

`GraphClosure → ClosureIndex → GraphCopy → Document Exporter → Package Validator → Mutation Applier → Skill Occurrence/Compiler`

状态机条件图的 owner 使用 `kind=edge`、`graphId`、`nodeId`、`edgeId` 和 `referenceKey=condition`。旧的 anchor.steps 已从 package DTO、Exporter、Projection、Applier、Validator、Closure 和循环检查中删除。

- 旧的 `AgentPackageSkillFlowStep` DTO、`ExportStep` 和无调用方的 `ValidateSteps` 已删除；普通组合仍由 `BtsmtlSkillStepPort` 和 `properties.steps` 处理。

### Document v8代码合同

- `AgentAuthoringSchema.Version` 已切换为 `btsmtl-agent-authoring-document.v8`。
- Codec、Store、Report、作者窗口、Presentation 错误信息和五个 MCP 生命周期工具说明均已统一到 v8。
- 旧 v7 及更早包不会被兼容读取；Store 会要求重新 checkout。
- 独立 `BtsmtlSkillTransferConnectionMigrator` 已删除，Git提交 `a1738ec56` 保留删除前历史。

### 源码入口与删除边界

| 业务责任 | 正式入口 | 当前处理 |
|---|---|---|
| FlowCanvas拓扑与状态结构 | `BtsmtlSkillFlowGraph.cs`、`BtsmtlSkillFlowNode.cs`、`BtsmtlSkillStructuralFlowNodes.cs` | 保留正式Graph、State和固定逻辑端口 |
| 状态机转移数据 | `BtsmtlSkillFlowConnection.cs` 的 `BtsmtlSkillTransferPayload` | Edge唯一保存condition、priority、abortPolicy和order |
| 节点定义与Port Shape | `BtsmtlSkillCapabilityCatalog.cs`、`BtsmtlSkillGraphAuthoringMetadata.cs` | 由Capability和唯一投影入口提供 |
| 引用闭包与复制 | `BtsmtlSkillGraphClosure.cs`、`BtsmtlSkillGraphClosureIndex.cs`、`BtsmtlSkillGraphCopy.cs` | 沿正式Edge/Step/Node引用闭合；两种条件owner不互读 |
| Document读写与校验 | `AgentSkillFlowDocumentModels.cs`、`AgentSkillFlowDocumentExporter.cs`、`AgentSkillFlowDocumentValidator.cs`、`BtsmtlSkillGraphAuthoringApplier.cs` | 统一v8 Graph/Edge/owner/order链 |
| 编译消费与SourceMap | `BtsmtlSkillGraphOccurrence.cs`、`BtsmtlSkillGraphFlowEmitter.cs`、`BtsmtlSkillGraphCompiler.cs` | 状态机按显式Edge order发射Transfer控制流 |
| 已删除路径 | `BtsmtlSkillTransferConnectionMigrator`、`BtsmtlSkillLegacyMigrationWorkflow`、状态机anchor/State旧steps | 不再有迁移菜单、旁路转换或状态机Step副本 |
| 保留的相似语义 | `BtsmtlSkillCompositeFlowNode.Steps` 与 Document `properties.steps` | 仅服务Sequence/Selector/Parallel，不属于状态机Transition |

### 正式资产处理

之前通过正式 Document JSON checkout/dry-run/apply 已把当前 Corin 状态机的 20 条转移边写成 Edge Payload，并确认旧状态 step 节点为 0、转移端点为 `Transfer → StateIn`。后续 v8切换前已删除被忽略的旧 v4/v5/v7 package目录，准备由正式 checkout重新生成唯一 v8包。

## 验证记录

- Runtime 与 Editor 的 `dotnet build` 已通过；编译使用 `--disable-build-servers /nr:false /p:UseSharedCompilation=false`，随后执行了 `dotnet build-server shutdown`。
- Unity 正确实例为 `3C_Client@e852139597e42532`；没有向并行测试实例 apply。
- v8代码加载后，MCP tool description 已显示 Document v8。
- 当前最新正式 `checkout_document` 没有生成 package，返回真实 authoring错误：当前未提交 Native FSM/相关节点闭包存在缺失，且当前目录有并行 Timeline 改动；这是正确阻断。

## 未完成

1. 等待并行 Native FSM/Timeline 改动形成可编译、可闭包的正式 authoring，不能覆盖或代替其未提交内容。
2. 在 Unity authoring可完整导出后，重新执行 v8 `checkout_document`，核对manifest/sync schema、完整Graph闭包、Edge owner/order和hash。
3. 对新 v8包执行无修改 `dry_run_document`、`validate` 和重新 checkout；必要时才执行同hash `apply_document`。
4. 对本 change 的现行 spec、`openspec/project.md` 和 `btsmtl-agent-authoring` 技能合同完成 v8对账；历史 archive只保留追溯，不作为当前完成证明。
5. 清理当前工作区中明确属于本 change的剩余重复节点定义；不触碰其它 active task 的未提交文件。

本轮复核确认并行 Native FSM 改动中的 `BtsmtlSkillNativeConnection` 已读取共享 `BtsmtlSkillTransferPayload`，未再保留第二份转移字段；未提交的 `BtsmtlSkillLegacyMigrationWorkflow` 及菜单入口已删除。Native FSM 本身仍属于并行未提交改动，不能作为 FlowCanvas 正式拓扑统一完成的证据，也不能在本 change 中继续扩展第二条正式 authoring 路径。
