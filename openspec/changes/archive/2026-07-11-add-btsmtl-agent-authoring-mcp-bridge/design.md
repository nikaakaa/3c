# Design: BTSMTL Agent Authoring MCP 桥接

## 设计目标

MCP 层只负责把结构化请求送进 Unity Editor，并把结构化结果送回 Codex。它不理解 BTSMTL 图规则，不直接修改资产，也不复制 compiler。所有 authoring 业务编排集中在一个 editor-only service 中，使窗口和 MCP 只是不同交互界面。

## 模块边界

### `ManageBtsmtlAgentAuthoringMcpTool`

职责：

- 使用 `[McpForUnityTool("manage_btsmtl_agent_authoring")]` 注册到现有 Unity MCP。
- 声明 `action`、`definition_asset_path` 和可选 `patch_json` 参数。
- 把 snake_case MCP 参数转换为强类型 request。
- 调用 `AgentPatchAuthoringService`。
- 使用 MCP package 的结构化成功/错误响应返回 snapshot 或 compile report。

它 MUST NOT：

- 直接调用 `BaseGraph.CreateNode`、`Link` 或 `LinkProperty`。
- 直接修改 `SerializedObject`、Unity YAML 或 asset 内部集合。
- 读取剪贴板、磁盘 Patch 文件或目录扫描结果。
- 启动终端、子进程、Unity batchmode 或第二个 MCP transport。

### `AgentPatchAuthoringService`

职责：

- 校验 Unity Editor 是否处于可操作状态。
- 只通过请求给出的精确 `Assets/...` 路径加载 `CharacterPipelineDefinition`。
- 为四个 action 编排现有 exporter/compiler/validator。
- 统一 MCP 和 EditorWindow 的错误、事务与保存语义。

接口概念：

```csharp
AgentAuthoringResponse Execute(AgentAuthoringRequest request)
```

`AgentAuthoringRequest`：

- `Action`：`ExportSnapshot`、`DryRunPatch`、`ApplyPatch`、`Validate`。
- `DefinitionAssetPath`：明确的项目资产路径。
- `PatchJson`：仅 dry-run/apply 必需。

`AgentAuthoringResponse`：

- action 与目标 definition 路径。
- 是否成功、是否修改资产。
- snapshot 或 `AgentCompileReport`。
- bridge 级错误码与消息。

## Action 契约

### `export_snapshot`

输入：`definition_asset_path`。

处理：加载 definition，调用 `AgentGraphSnapshotExporter.Export`。

输出：紧凑 `AgentGraphSnapshot`。不导出 full debug snapshot，避免 MCP 响应携带完整端口和边 dump。

副作用：无。

### `dry_run_patch`

输入：`definition_asset_path`、`patch_json`。

处理：正式 JSON utility 解析 Patch，导出当前 snapshot，调用 `AgentPatchCompiler.Compile(..., apply: false)`。

输出：包含 planned diff、错误和指标的 `AgentCompileReport`。

副作用：无；不得 dirty 或保存资产。

### `apply_patch`

输入：`definition_asset_path`、`patch_json`。

处理顺序：

1. 解析 Patch。
2. 基于当前资产重新导出 snapshot。
3. 调用 compiler dry-run；有错误立即返回。
4. 收集 definition、RootTree 和所有可达 inline/shared graph 的 serialized owner。
5. 创建单个 Undo group，并对全部 owner 注册完整 Undo。
6. 调用 compiler apply。
7. 调用 `AgentGraphValidator`，把消息合并进同一 report。
8. compiler 或 validator 有错误时回滚当前 Undo group，不保存。
9. 成功时折叠 Undo group并调用 `AssetDatabase.SaveAssets()`。

输出：最终 `AgentCompileReport`，同时明确 `applied` 与是否已保存。

副作用：仅成功时保存正式资产；失败时恢复到调用前状态。

### `validate`

输入：`definition_asset_path`。

处理：调用 `AgentGraphValidator.Validate`。

输出：机器可读验证 report。

副作用：无。

## 目标资产与 Patch 身份

`definition_asset_path` 属于命令上下文，不属于 Patch IR。service 只接受 `Assets/` 下能精确加载为 `CharacterPipelineDefinition` 的路径。路径缺失、类型不符或资产不存在时直接返回错误，不扫描目录、不按名称寻找替代资产。

`AgentPatchIR.definitionAssetPath` 将被删除。Snapshot 可以继续输出 definition asset path，用于 Agent 理解当前上下文，但该字段不是 apply 指令。

## 事务所有者收集

BTSMTL 私有下钻 graph 内联在其 serialized owner 中，shared graph 可能拥有独立 Unity asset owner。事务开始前必须沿 RootTree 的正式 graph reference 遍历所有可达 graph，将每个非空 `SerializedOwner` 去重后连同 definition 和 RootTree asset 注册到同一个 Undo group。

这是事务边界，不是备选配置路径。任一 owner 无法纳入事务时，apply 必须在修改前失败，不允许退化为只记录 RootTree。

## Editor 状态门禁

所有 action 在 Unity 正在编译、更新 asset database 或切换/处于 Play Mode 时直接失败。bridge 不排队、不延迟重试，也不启动额外进程等待状态变化。这样可以避免 domain reload 或运行时实例与 authoring 写入交叠。

## EditorWindow 收敛

现有 `AgentCharacterControllerSynthesisWindow` 保留人工查看 snapshot、输入 Intent/Patch 和阅读 report 的能力，但其 Patch dry-run/apply/validate 必须调用同一个 `AgentPatchAuthoringService`。

Intent 仍可由窗口先经 `AgentMacroLibrary` 展开为 Patch，再交给 service。第一版 MCP 只接收 Patch IR，不在 bridge 中复制 Intent/Macro 选择逻辑。

临时 `Apply Agent Patch From Clipboard` 菜单方法和 `AgentPatchIR.definitionAssetPath` 一并删除，不保留快捷键、隐藏菜单或兼容解析。

## MCP 注册与版本边界

自定义工具代码放在项目 Editor-only 目录，由当前 project editor assembly 编译。`com.coplaydev.unity-mcp` 的 `McpForUnityToolAttribute`、`ToolParameterAttribute` 和 `CommandRegistry` 会自动发现 `public static HandleCommand(JObject)`。

不修改 `Library/PackageCache`，不复制 package 源码。项目升级 MCP package 后若公开 API 发生破坏，编译错误作为明确升级工作处理，不通过反射或条件分支兼容未知版本。

## 错误分层

- bridge 错误：action 不支持、参数缺失、Editor 状态不可写、definition 路径非法。
- parse 错误：Patch JSON 无法解析。
- compiler 错误：operation、引用或 graph 编辑失败。
- validator 错误：apply 后正式 graph 语义不合法。

MCP 返回必须保留 `AgentCompileReport` 的 path、code、message、suggestion、planned/applied diff 和 metrics，不把所有问题压成单个日志字符串。

## 失败语义

- `export_snapshot`、`dry_run_patch`、`validate` 永不修改资产。
- `apply_patch` 只有 dry-run、apply 和 validator 全部成功时才报告成功并保存。
- apply 抛出异常时必须转换为 bridge/report 错误并回滚事务。
- 回滚失败属于硬错误，必须明确报告，不继续保存或尝试其它路径。

## 风险

- MCP package 当前 manifest 指向 `#main`，未来更新可能破坏扩展 API。当前 `packages-lock.json` 会锁定已解析 revision；本变更不额外引入版本兼容层。
- Patch JSON 可能较大。第一版保留单次结构化参数，避免文件路径和 inbox；如果后续真实规模超过 MCP payload 限制，应另起 proposal 设计分块协议，而不是临时读取文件。
- Undo owner 遍历必须覆盖 shared graph。实现时若现有 graph reference API 无法可靠枚举全部 owner，应停止 apply 实现并说明缺口，不能只保护 RootTree 后继续。

