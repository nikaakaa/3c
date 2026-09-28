# 操作与报告参考

下文路径相对当前 3C 仓库；`Client` 指 `3cDemo/Client/3C_Client`。从本次工具响应和当前源码确认实际路径，不从历史聊天复制实例、作业、构建或采集 ID。

## 正式代码与合同

| 内容 | 位置 |
| --- | --- |
| 使用口径与清单示例 | `Tools/ThirdPersonPerformanceCapture/README.md` |
| MCP 工具参数及作业状态 | `Client/Assets/GameScripts/Main/Editor/Performance/ThirdPersonPerformanceCaptureMcpTools.cs` |
| 构建、场景发布、启动和文件入口 | 同目录 `ThirdPersonPerformanceCaptureWorkflow.cs` |
| schema 与报告字段 | `Client/Assets/GameScripts/Main/Runtime/Performance/PerformanceCaptureContracts.cs` |
| Player 采集执行 | `Client/Assets/GameScripts/Main/Runtime/Performance/Unity/ThirdPersonPerformanceCaptureAgent.cs` |
| Controller、发布和分析 | `Tools/ThirdPersonPerformanceCapture/PerformanceCaptureController.cs`、`PerformanceCapturePublisher.cs`、`PerformanceCaptureAnalysis.cs` |

schema 不受本 skill 固定。准备请求前读取当前 `PerformanceCaptureSchemas.AnalysisRequest`；工具拒绝旧证据时保留旧文件，不能手改版本号、哈希或状态绕过校验。

## MCP 操作

先发现当前可调用工具，点号可能被宿主规范化；必要时使用已有 `execute_custom_tool` 路由到正式工具，不虚构新工具。每次 Unity 调用在外层显式绑定目标 `unity_instance`。下表仅列业务参数，不替代实例参数。

| 工具 | 常用参数 | 执行效果 |
| --- | --- | --- |
| `performance.prepare` | `action=status` | 查询准备状态 |
| 同上 | `action=configure_toolchain` | 校验并保存本机工具链配置 |
| 同上 | `action=publish_scenario`、`trace_path`、`runtime_id` | 从明确输入发布 Scenario |
| `performance.build_player` | `action=start`、`runtime_id`、`instrumentation_mode` | 构建并发布指定模式的 Player |
| 同上 | `action=status`、`job_id` | 查询原构建作业；若当前外层 schema 要求模式参数，也传入原模式 |
| `performance.smoke` | `action=start/status/cancel`；查询带 `job_id` | 启动、就绪与正常退出检查 |
| `performance.replay` | `action=start/status/cancel`；查询带 `job_id` | 对应 Player 的固定输入回放检查 |
| `performance.capture` | `action=start/status/cancel`；查询带 `job_id` | 启动采集；Capture 会请求所需提权并使用 WPR |
| 同上 | `action=select_baseline`、`baseline_manifest_path` | 为后续采集选择明确的单次基线 |
| 同上 | `action=clear_baseline` | 清除单次比较选择，不删除采集文件 |
| `performance.analyze` | `action=start`、`request_path` | 构建 Controller 并分析显式多次采集清单 |
| 同上 | `action=status`、`job_id` | 查询原分析作业 |
| `performance.report` | `action=status/gates/gate/list/manifest/summary/comparison/analysis` | 读取状态或已发布结果 |

`manifest`、`summary`、`comparison` 明确传 `manifest_path`；`analysis` 明确传 `analysis_path`。按任务使用 `limit`，被截断的工具输出不代表完整数据只有这些行。列举结果可以用于发现，但最终选择应核对身份，不按目录时间自动选 A/B。

同一构建重复采集可复用仍然匹配的 Smoke/Replay 结果。当前没有一次 `start` 自动连续采集 N 次的合同：每次 Capture 都要完成并登记路径，再启动下一次。

构建状态包含 `phase`、`elapsed_ms` 和当前消息；磁盘记录为 `Client/Library/Performance/McpJobs/<job>.json`。阶段包括资源指纹、Unity 构建、输入核对、产物整理、产物哈希和发布。`elapsed_ms` 仅代表上次进度更新时的累计耗时，Unity 原生构建期间看 Unity 进度和日志，不把未更新的时间当作进程停止。Editor 退出后残留的 `running` 需通过原作业状态核对，不直接启动另一轮。

当前输入快照记录 Unity 已导入资源的依赖指纹和少量配置文件哈希，不递归散列全部 Assets 与 PackageCache。包中的临时 `obj` 文件不是稳定的输入清单来源。插桩配置使用按输入与模式身份确定的路径，同输入构建复用编译宏；不要手工删除其清单再假设增量编译会重新生成。具体快照 schema 与范围以仓库 README 和合同为准，不把 Unity 未导入的外部文件也说成已覆盖。

未保存资源检查区分可保存的原生资源、AssetImporter 设置和导入后生成的对象。已确认字体生成的 Texture2D 可被标记 dirty，但字体导入设置并未修改；这种缓存状态不能阻塞构建，不通过全局 SaveAssets 或清除 dirty 标志绕过检查。新 Scenario 默认取消帧率上限并关闭 VSync；Profile 的 `maximum_presentation_fps` 是预分配容量的估算输入，不是 Player 帧率上限。

构建输入核对失败时读取 `Client/Library/Performance/BuildDiagnostics/<job>/inputs-before.json` 和 `inputs-after.json`，定位变化后再处理。Unity Performance Testing 在构建前生成、成功构建后删除的两份 `Assets/Resources/PerformanceTestRunInfo.json`、`PerformanceTestRunSettings.json` 已从源码输入快照排除；不要把这个已确认的生成生命周期扩大成忽略所有 Resources 或所有资源变化。

Smoke 的 `Performance Player transport closed` 只是连接关闭。读取该次 Gate 的 `runtime-result.json` 与 `player.log` 确认 Player 初始化原因，不把它直接归为 TCP 故障。2026-09-28 已确认 Controller 的旧场景和 Ready 名称可在 Player 启动前拒绝正式 Fixed 场景；应与发布器、Player 的当前合同统一，不能放宽为接受任意场景。Player 构建完成不等于角色初始化或采集完成。

## 多次分析清单

使用系统文件工具以 UTF-8 写入请求，保存到明确的性能分析请求位置，避免写进会触发 Unity 导入的 Assets。`schema` 从当前合同读取，路径从已确认的 Completed Capture 获取。结构为：

```json
{
  "schema": "从当前 PerformanceCaptureSchemas.AnalysisRequest 读取",
  "comparison_kind": "Regression",
  "baseline_manifest_paths": [
    "基线第一次的绝对 manifest.json 路径",
    "基线第二次的绝对 manifest.json 路径",
    "基线第三次的绝对 manifest.json 路径"
  ],
  "candidate_manifest_paths": [
    "候选第一次的绝对 manifest.json 路径",
    "候选第二次的绝对 manifest.json 路径",
    "候选第三次的绝对 manifest.json 路径"
  ]
}
```

这是结构说明，提交前替换全部说明文字。校准时将 `comparison_kind` 设为 `InstrumentationOverhead`。不要在两组或组内重复使用相同采集。

已有且对应当前源码合同的 Controller 可以直接离线分析：

```text
ThirdPersonPerformanceCapture.Controller.exe --analysis-request="请求文件的绝对路径" --analysis-output="尚不存在的输出目录"
```

先确认实际可执行文件位置和版本。常规构建输出位于 `Tools/ThirdPersonPerformanceCapture/bin/Release/net8.0-windows/`，不能只凭文件存在就认定它包含最新实现。CLI 不启动 Player，但本身是进程；禁止起进程时不执行。退出码 0 不代表性能改善，仍读取报告状态；拒绝分析返回 2，执行故障也可能返回 2。

## Launcher 对应操作

入口为 `Tools → 3C → Launcher → 性能诊断 / Performance Capture`。

- `Configure Toolchain`、选定输入后 `Publish Scenario`。
- `Build MarkerOnly Player` 或 `Build Span Player`；Disabled 当前通过 MCP `performance.build_player` 指定，不假设界面已有按钮。
- `1. Smoke → 2. Replay → 3. Capture`；选定单次基线后第三个按钮显示 `Capture + Compare`。
- `Reveal Capture` 打开结果目录；`Open Summary`、`Open Comparison`、`Open Unity Profiler`、`Open WPA` 对应本次文件。
- `分析重复采集…` 读取 JSON 清单；`打开重复分析报告` 打开可读 Markdown。

这些按钮用于解释现有操作，不要求 AI 用鼠标自动化；可执行工作沿用正式 MCP/CLI。

## 报告位置与判断

正式产物根目录为 `Client/Library/Performance`，实际运行身份以返回值为准：

| 目录 | 用途 |
| --- | --- |
| `Players/<build>/` | Player、符号、Player manifest、构建输入快照 |
| `Gates/<operation>/<run>/` | Smoke/Replay 的结果和证据 |
| `Captures/<capture>/` | `manifest.json`、`summary.json`、`comparison.json`、原始采样及日志 |
| `Analyses/<analysis>/` | `request.json`、`analysis.json`、`analysis.md` |

CLI 的分析输出可由调用方显式指定，因此不强行改写为默认目录。目录名或 `.staging` 中的临时文件不能代替正式完成证据。Library 中的报告会受项目清理影响；需要清理或重建目录时，先按用户要求保护仍需使用的证据，不默认全量复制或提交 Git。

| 字段或状态 | 应如何解释 |
| --- | --- |
| `NotRequested` | 本次未选择单次比较基线 |
| `Rejected` | 身份、环境或文件证据未通过比较条件 |
| `Incomplete` | 某些对应指标缺失或定义不一致，检查具体 Capture 和指标 |
| `InsufficientRepeats` | 某组不足最低重复数，不声称已有稳定重复证据 |
| `Comparable` | 可以按当前口径比较，不是优化验收结论 |
| `ObservedRangesOverlap` | 两组观测范围重叠 |
| `CandidateRangeLower/Higher` | 候选观测范围整体更低/高；按指标方向解释，不宣称统计显著 |
| `NoSamples`、`NotObserved` | 没有调用或观测，不是测得零耗时 |
| `MissingBaseline/Candidate`、`NoBaselineSamples/NoCandidateSamples` | 一侧缺少对应证据，不能补零相减 |
| `DefinitionMismatch` | 定义、单位、采样范围或归属不符合比较条件 |
| `BaselineZero`、`BothZero` | 百分比无定义；先检查 `percent_delta_available` |
| `budget_evaluated=false` | 预算未完整评估，即使部分指标达标也不是整体预算通过 |

优先读取机器报告及来源引用，再解释 Markdown 展示。对已存在的分析报告核对请求类型、来源 Capture ID/哈希和指标覆盖；需要重新计算时调用正式分析器，不另算一份同名结论替代它。
