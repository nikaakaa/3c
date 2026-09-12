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

### Document v8代码合同

- `AgentAuthoringSchema.Version` 已切换为 `btsmtl-agent-authoring-document.v8`。
- Codec、Store、Report、作者窗口、Presentation 错误信息和五个 MCP 生命周期工具说明均已统一到 v8。
- 旧 v7 及更早包不会被兼容读取；Store 会要求重新 checkout。
- 独立 `BtsmtlSkillTransferConnectionMigrator` 已删除，Git提交 `a1738ec56` 保留删除前历史。

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
