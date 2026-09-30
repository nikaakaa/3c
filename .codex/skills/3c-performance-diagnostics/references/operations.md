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
| `performance.build_player` | `action=start`、`runtime_id`、`instrumentation_mode`，可选 `clean_build_cache` | 构建当前内置资源并发布指定模式的 Player |
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

构建状态包含 `phase`、`elapsed_ms` 和当前消息；磁盘记录为 `Client/Library/Performance/McpJobs/<job>.json`。阶段包括资源来源指纹、`content-build` 内置资源构建、Player 输入指纹、Unity 构建、输入核对、产物整理、产物哈希和发布。`content-build` 使用项目现有 `TEngine.ReleaseTools.BuildContent`，构建 DefaultPackage 并全量复制内置资源；仅构建 exe 不会更新旧 StreamingAssets 包，已实际发生当前 ACL 地址不在旧 manifest 中的启动失败。资源包来源指纹不纳入生成的 StreamingAssets，最终 Player 输入快照仍纳入内置资源。`elapsed_ms` 仅代表上次进度更新时的累计耗时，Unity 原生构建期间看 Unity 进度和日志，不把未更新的时间当作进程停止。Editor 退出后残留的 `running` 需通过原作业状态核对，不直接启动另一轮。

2026-09-30，Windows 下构建在资源指纹阶段因 MCP 作业状态发布的 `File.Replace` 报“无法删除要被替换的文件”；独立临时文件连续替换也在第 7 次复现。现有状态发布入口已统一使用同目录临时文件与 `MoveFileExW(REPLACE_EXISTING | WRITE_THROUGH)`，Unity 内连续 2000 次发布通过，正式构建已越过该阶段。此证据不确认具体系统过滤器或文件占用者。读取运行中的作业 JSON 使用 UTF-8，并以 `FileShare.ReadWrite | FileShare.Delete` 打开，允许发布器替换正在读取的旧版本。

同日资源构建无条件覆盖已被映射的 `mscorlib.dll.bytes` 而失败，已核对源、目标 SHA-256 完全相同。正式 `BuildDLLCommand` 现在仅在 DLL 内容变化时复制，AOT 与热更新 DLL 共用此入口；内容变化仍按正常复制失败处理。不要通过跳过整个 DLL 或资源构建阶段放行旧产物。

`clean_build_cache` 默认 false。2026-09-28 调整 Timeline 编辑器专属序列化字段后，增量 Player 在反序列化阶段发生原生崩溃；同一修改使用 `clean_build_cache=true` 重建后通过该阶段。遇到这种有日志依据的场景数据/脚本缓存问题可以显式清理重建，不将所有构建改成全量，也不据此认定已证明某个 Unity 引擎缺陷。

资源构建缓存位于仓库 `.performance-build/content`，发布资源使用 YooAsset 的 `HashName` 文件名选项，业务资源地址不变。曾在较深的 `Client/Library/Performance/Content` 输出目录遇到 SBP `ArchiveAndCompressBundles` 的 `PathTooLongException`；不要把此异常当成 C# 编译错误或改写业务资源路径。

失败的资源构建可能留下 URP 管线资源 dirty：当前 URP `ShaderBuildPreprocessor` 会更新 `m_Prefilter*` 并执行 `EditorUtility.SetDirty`。2026-09-28 已确认一次失败构建前输入干净、构建后三个 URP 资源 dirty，定向保存这三项后没有序列化内容差异。只有确认属于本次构建产生的状态时，才定向保存并检查 diff；不扩展成自动保存所有资源或忽略 URP 的作者改动。

当前输入快照记录 Unity 已导入资源的依赖指纹和少量配置文件哈希，不递归散列全部 Assets 与 PackageCache。包中的临时 `obj` 文件不是稳定的输入清单来源。插桩配置使用按输入与模式身份确定的路径，同输入构建复用编译宏；不要手工删除其清单再假设增量编译会重新生成。具体快照 schema 与范围以仓库 README 和合同为准，不把 Unity 未导入的外部文件也说成已覆盖。

未保存资源检查区分可保存的原生资源、AssetImporter 设置和导入后生成的对象。已确认字体生成的 Texture2D 可被标记 dirty，但字体导入设置并未修改；这种缓存状态不能阻塞构建，不通过全局 SaveAssets 或清除 dirty 标志绕过检查。新 Scenario 默认取消帧率上限并关闭 VSync；Profile 的 `maximum_presentation_fps` 是预分配容量的估算输入，不是 Player 帧率上限。采集 Agent 在运行环境就绪、发送 READY 前应用帧率配置，因为更早的 BeforeSceneLoad 设置会被 TEngine RootModule.Awake 覆盖；从当次 player.log 的 `Performance frame pacing` 确认实际值，不能仅凭 scenario.json 声称不限帧。

构建输入核对失败时读取 `Client/Library/Performance/BuildDiagnostics/<job>/inputs-before.json` 和 `inputs-after.json`，定位变化后再处理。Unity Performance Testing 在构建前生成、成功构建后删除的两份 `Assets/Resources/PerformanceTestRunInfo.json`、`PerformanceTestRunSettings.json` 已从源码输入快照排除；不要把这个已确认的生成生命周期扩大成忽略所有 Resources 或所有资源变化。

Smoke 的 `Performance Player transport closed` 只是连接关闭。读取该次 Gate 的 `runtime-result.json` 与 `player.log` 确认 Player 初始化原因，不把它直接归为 TCP 故障。2026-09-28 已确认 Controller 的旧场景和 Ready 名称可在 Player 启动前拒绝正式 Fixed 场景；应与发布器、Player 的当前合同统一，不能放宽为接受任意场景。Player 构建完成不等于角色初始化或采集完成。

2026-09-28 首次通过 Smoke 后，Replay 曾因 Player 仍只接受 `/3`、实际录制已经为 `/4` 而在连接前退出。发布与读取应使用同一当前输入合同；当前每帧已包含相机 basis，文档中的初始相机朝向还需要在 READY 前恢复。Controller 已改为在等待连接期间发现 Player 退出时立即读取当次 runtime-result 报错，不再将这种启动失败拖成连接超时。

核对采集边界时检查有效调用：Session 的开始门应位于被测执行方法之外，暂停或回放完成后的空调用不算已执行 Tick；表现帧需要在进入探针前建立当前 RenderFrame 上下文。总样本计数匹配和上下文完整不能仅凭插桩编译成功判定。

业务 Marker 在配置插桩时按指标目录注册，不依赖各业务类型首次初始化。首次 Capture 曾在未执行的 FactProjection 阶段因 Recorder 无法找到 Marker 而失败；未调用阶段应保留无样本语义，注册 Marker 本身不执行 Begin/End、不生成耗时。2026-09-29 已在目标 Editor 核对全部 15 个 Recorder 入口均可创建，随后独立 Player Capture 完成；FactProjection 仍为无样本，Session 样本恰好覆盖 2536 个采集 Tick。

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
