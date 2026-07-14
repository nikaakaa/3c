# Proposal: BTSMTL Agent Authoring MCP 桥接

## Why

当前项目已经有 `AgentGraphSnapshotExporter -> AgentPatchCompiler -> AgentGraphValidator -> AgentCompileReport` 的 editor-only 生成链路，但 Codex 不能通过现有 Unity MCP 直接调用它。临时加入的剪贴板菜单把目标资产路径塞进 `AgentPatchIR`，并复制了一套 dry-run、apply、validate、Undo 和 Save 流程；它还占用了 Unity 快捷键。这条路径既需要人工搬运 JSON，又让同一 Patch 在窗口、剪贴板和未来 MCP 中拥有不同生命周期，已经形成分裂入口。

项目当前安装的 `com.coplaydev.unity-mcp` 支持通过 `[McpForUnityTool]` 自动发现项目 Editor assembly 中的自定义工具。可以在现有 `unityMCP` 连接内增加一个薄桥接，让 Codex 直接导出当前 snapshot、预检 Patch、事务应用 Patch 和验证结果，不需要新 MCP server、额外终端进程、Unity batchmode、剪贴板或快捷键。

## What Changes

- 新增一个自动注册到现有 `unityMCP` 的 `manage_btsmtl_agent_authoring` 自定义工具。
- 工具提供 `export_snapshot`、`dry_run_patch`、`apply_patch`、`validate` 四个动作。
- 新增唯一的 `AgentPatchAuthoringService`，统一编排 JSON 解析、snapshot、compiler、validator、Undo 事务和资产保存；MCP 与现有 EditorWindow 都调用该 service。
- `apply_patch` 必须先执行无副作用 dry-run；预检通过后才进入 Undo 事务，apply 后再次验证，成功才保存，失败则回滚全部本次变更。
- 目标 `CharacterPipelineDefinition` 由 MCP/Editor 调用上下文明确传入；删除 `AgentPatchIR.definitionAssetPath`，Patch 不再自行选择目标资产。
- 删除剪贴板菜单、其快捷键和重复的 Patch 应用生命周期。
- 直接依赖已安装 MCP package 的公开 Editor 扩展 API，不修改 `Library/PackageCache`，不增加反射兼容层。

## 目标

- 让 Codex 在当前 Unity Editor 会话内直接完成 `snapshot -> patch -> dry-run -> apply -> validate -> report`。
- 保持 `AgentPatchCompiler` 是唯一 Patch compiler，BTSMTL asset 是唯一正式结果。
- 让窗口调用和 MCP 调用拥有相同输入校验、事务边界、错误报告和保存语义。
- 失败时不留下部分修改，不要求作者手动撤销或清理污染资产。

## 非目标

- 不新增或 fork MCP server，不新增常驻终端进程。
- 不通过 MCP 写 C# 源文件、Unity YAML 或 BTSMTL 内部集合。
- 不让 MCP 直接创建任意 BTSMTL 节点；节点范围继续由 Patch IR 和 emitter 白名单限制。
- 不在 runtime、服务端或网络层执行 Agent JSON。
- 不增加目录扫描、同名资产解析、剪贴板、文件 inbox、菜单命令或反射 fallback。
- 不在本变更新增测试，也不运行 Unity batchmode。

## 方案概述

```text
Codex
-> existing unityMCP connection
-> manage_btsmtl_agent_authoring
-> AgentPatchAuthoringService
   -> export_snapshot: AgentGraphSnapshotExporter
   -> dry_run_patch: JSON parser -> AgentPatchCompiler(dry-run)
   -> apply_patch: dry-run -> Undo transaction -> AgentPatchCompiler(apply)
                                      -> AgentGraphValidator
                                      -> SaveAssets or rollback
   -> validate: AgentGraphValidator
-> structured MCP response / AgentCompileReport
-> BTSMTL / CharacterPipelineDefinition assets
```

## 现有能力对比

- `btsmtl-graph-core` 已要求所有脚本创建都走 `BaseGraph` 正式结构编辑操作；MCP bridge 只调用现有 compiler，不直接写节点、边或 GUID 集合。
- `character-gameplay-pipeline-closure` 已要求 `CharacterPipelineDefinition` 是 authoring 装配入口且不得扫描 fallback；MCP 请求必须明确给出该资产的 `Assets/...` 路径。
- 历史 change `add-agent-character-controller-synthesis` 已定义 Snapshot、Patch IR、Compiler、Validator 和 Report，但对应 current spec 未出现在 `openspec/specs/`。本变更只补充 MCP 调用边界，不重建或复制完整 Agent compiler 规格。
- 当前 `AgentCharacterControllerSynthesisWindow` 的普通窗口入口可以保留，但必须改为复用统一 service；临时剪贴板菜单必须删除。

## Impact

- 修改 Character Pipeline 的 editor-only AgentAuthoring 模块。
- 新增对 `MCPForUnity.Editor` 和 `Newtonsoft.Json.Linq` 的编译期使用；它们来自当前已安装 package，不进入 runtime assembly。
- 新增一个 current capability delta：`btsmtl-agent-authoring-mcp-bridge`。
- 不修改 gameplay runtime、网络同步、Timeline 执行、Motion 或 Presentation 语义。
- 不修改 MCP package 源码，也不新增第二个 MCP transport。

## 已确认决策与 Tradeoff

### 复用现有 MCP server，还是新建独立 server

- 选择复用现有 `unityMCP` 自定义工具发现机制。业务收益是沿用当前已连接的 Unity Editor 会话、实例路由和主线程调度，Codex 不需要管理第二个进程。
- 独立 server 可以完全控制协议和版本，但会引入新的启动、连接、实例选择和生命周期运维，对当前单项目 authoring 工具没有额外业务价值。

### 单一 manage 工具，还是多个零散工具

- 选择一个 `manage_btsmtl_agent_authoring`，用 action 区分 snapshot、dry-run、apply 和 validate。业务上这是一条完整 authoring 生命周期，统一入口更容易让 Agent 正确按顺序执行，也避免工具列表膨胀。
- 多个独立工具的 schema 更短，但更容易出现只 apply 不预检、不同工具返回结构漂移的问题。

### Patch 携带目标路径，还是调用上下文携带目标路径

- 选择由 MCP/Editor 调用显式传入 `definition_asset_path`，并删除 `AgentPatchIR.definitionAssetPath`。业务上同一 Patch 可以安全地对指定定义预检，目标选择不会隐藏在生成内容内部。
- Patch 自带路径更方便剪贴板一键执行，但把部署目标和编辑指令耦合，误贴 Patch 时更容易修改错误角色资产。

### 直接引用 MCP package API，还是反射兼容多个版本

- 选择直接引用当前 package 的 `[McpForUnityTool]`、`ToolParameter`、`SuccessResponse` 和 `ErrorResponse`。package API 变化时会显式编译失败，问题可立即发现。
- 反射兼容能容忍部分 package 变化，但会把 schema、发现和返回契约变成运行时猜测，形成不受控兼容路径。

### 事务回滚，还是只依赖 dry-run

- 选择 dry-run 加 Undo 事务回滚。dry-run 能发现 schema 和引用问题，但不能保证 apply 后的完整 graph validator 一定通过；事务使业务资产不会停在半成品状态。
- 只做 dry-run 实现更少，但 apply 中途异常或后置验证失败时需要作者人工恢复，无法形成可自动重复的 Agent 闭环。

## 文档口径差异

- `openspec/project.md` 的“当前活跃变更”仍停留在旧 change，与 `openspec list` 全部 Complete 的结果不一致。
- 历史 `add-agent-character-controller-synthesis` proposal 声明会新增 current spec，但 `openspec/specs/` 当前没有 `agent-character-controller-synthesis`。本 proposal 依赖其已实现代码和历史规格，不把缺失 current spec 当成另一套实现依据；归档或整理规格时需要单独补齐该文档缺口。

