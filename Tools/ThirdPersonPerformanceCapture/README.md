# 性能采集口径

入口仍是 Launcher Performance 区或 `performance.*` 工具，两者调用同一 workflow。按明确模式构建 Player，再依次运行 Smoke、Replay、Capture；没有通过对应 Player 的门禁就不能采集。

## 三种构建模式

| 模式 | 可用数据 | 用途 |
| --- | --- | --- |
| Disabled | Unity Main Thread、GC、帧率、Windows 函数采样和线程等待 | 关闭业务探针的基线 |
| MarkerOnly | 上述数据及业务 RenderFrame 指标、Unity Profiler Marker | 查看阶段成本，不保存单次调用跨度 |
| Span | 上述数据及调用点、Actor、RenderFrame、LogicTick、异常结束记录 | 定位具体方法及慢调用 |

Disabled 关闭的是业务方法织入，Unity Profiler、Recorder 和 WPR 仍有采集开销，不能把它叫作完全没有诊断开销的发布包。当前 MarkerOnly 的 JSON 摘要按 RenderFrame 汇总，不把一帧内多个 Tick 的总耗时写成单 Tick 样本；Unity Profiler 原始 Marker 仍能记录每次调用，逐 Tick 汇总缺失是当前报告实现的限制。只有 Span 生成 `instrumentation-spans.bin`。

新发布的 Scenario 使用 `target_frame_rate=-1`、`v_sync_count=0`，不限制 Player 帧率。Profile 的 `maximum_presentation_fps=1024` 只用于预分配 Recorder 容量，不传给帧率控制；Span 预分配 1048576 条记录。缓冲不足仍按正式采集错误处理，不在采样热路径扩容，也不通过限帧避免溢出。旧 Scenario 保留原配置，新配置使用独立的 `fixed.r5` 身份。

普通 Baseline 比较要求相同场景、环境、采集工具、指标目录和织入身份，允许代码构建身份变化，因此能比较重构前后。不同探针模式会被拒绝作为普通性能回归比较。探针开销使用同一分析入口的 `InstrumentationOverhead` 类型，要求构建输入快照一致，多次观察共同的整体指标；不能把模式变化解释成业务优化。

## 时间代表什么

Span 是同步方法进入和退出时的单调计时差值，包含被系统抢占、等待和子调用时间，不是线程独占 CPU 时间。文件记录采集 Player 的 `Stopwatch.Frequency`，分析器使用该频率换算，不借用分析进程的频率。Windows 函数热点是采样次数，线程等待由 Context Switch 表提供，两者不冒充方法精确耗时。

业务帧指标来自 Recorder 的逐帧汇总；LogicTick 指标来自按 Tick 聚合的 Span；调用点分布来自逐次 Span。没有样本的预算指标列入 `unavailable_budget_metrics`，同时 `budget_evaluated=false`、`budget_passed=false`，不把没测到解释为零开销或预算通过。

父子阶段使用 `parent_inclusive_total_ratio` 表示同次采集总耗时之比，不使用两个 P95 的比值。父阶段已经包含子阶段，不能相加。递归或多次进入同一阶段的累计值也不能理解为互斥的帧时间切片。线程热点按 Exclusive 样本汇总，避免把同一调用栈的各层 Inclusive 样本重复相加。

## Pose 覆盖

业务覆盖与旧采集完整指标见 [性能探针覆盖记录](../../docs/diagnostics/performance-coverage-20260929.md)。2026-09-29 新增 EventGraph、Timeline 采样/提交、Camera、装备表现、两域角色评估/GE 周期推进/世界快照，以及 KCC 批求解探针声明；KCC 同步进入织入程序集名单。这些新增入口尚未经过构建或采样验证，旧报告没有对应数据。Span 当前仅记录耗时，GC 仍是整帧计数，不能按业务阶段归因。

报告应列出 Summary 的全部指标及调用点无样本范围，同时列明尚未插桩的模块。新增目录将改变探针身份，必须由新 Player 的 instrumentation manifest 确认实际覆盖；不同探针身份不绕过正式比较门禁。

Completed Capture 会从同一份 `summary.json` 渲染 `summary.md`，先提供总览、全部指标、已采样/未采样调用点摘要、CPU 热点和线程等待表；它不引入新的统计 schema，也不替代完整 JSON、CSV、ETL 或 Span 证据。完整函数热点仍在 CSV 中，Markdown 只保留 exclusive 最高的前 100 行，避免把数万行函数表变成不可读报告。Span 表是经过时间，父子阶段重叠；CPU 表是采样计数，未解析样本不能归因到函数。

`presentation.animation` 包围表现事务，下面分为 Pose Prepare、Evaluate、Commit，以及 Source Barrier。Evaluate 下另有 Foot Placement 和 FullBodyIK 探针。Source Barrier 包含资源验证、回收准备和后端 Evaluate，不是纯骨骼计算时间。没有实际探针的旧阶段从指标目录删除；新增阶段要同时增加所属目录定义和方法声明。

当前合同为 player/3、capture/3、summary/4、comparison/2、analysis-request/2、analysis/2；跨度使用 spans/2 和 layout revision 2。旧报告不进入当前比较，需要重新构建 Player 并使用当前 Controller 采集；旧证据保留原样。源码编译通过不代表 Player 构建、真实采集、探针成本或 Gameplay 行为已验证。

## 单次比较的结果口径

Controller 先校验候选 Summary，再读取基线 manifest 中声明的 Summary。基线必须 Completed，产物路径不能越界或重复，要求的文件角色必须齐全，文件大小和 SHA-256 必须匹配；Summary、Runtime Result、Capture 身份与请求哈希也必须对应。读取失败或环境身份冲突写入 `Rejected`，不会使已经成功采集的候选数据丢失。

每个指标都记录两侧样本数。只在一侧出现的指标保留为 `MissingBaseline` / `MissingCandidate`，单位、采样范围或调用点归属不同为 `DefinitionMismatch`，整个比较标记 `Incomplete`。调用点注册了但没有被调用时保留 `NoSamples`；只在一侧没有调用则为 `NoBaselineSamples` / `NoCandidateSamples`，整个比较也标记 `Incomplete`。它们不能按数值字段的默认零解释成零开销。

基线为零时，变化百分比在数学上没有定义：`percent_delta_available=false`，状态为 `BaselineZero` 或 `BothZero`。读取者必须先检查状态与可用性，不能直接显示 `percent_delta` 的占位值。正常的绝对差仍然保留，例如 0 → 0.2 ms 的绝对差为 +0.2 ms。预算也分开提供 `budget_evaluated` 与 `budget_passed`；只有两侧均完整评估时 `budget_delta_available=true`。

Summary 保存全部函数热点，线程 Exclusive 汇总不受前 200 条展示限制影响。未解析的地址和符号不会被静默丢弃，报告保存 `unresolved_exclusive_samples` 与 `total_exclusive_samples`；无法识别格式的导出行直接报错。热点比较保留原始样本数，但 `absolute_delta` 和百分比按每秒样本数计算，分母为 Player 的 `capture_seconds`，不是 CPU 毫秒。ETW 的 Controller 起止标记还包围通信握手边界，并非与 Player 第一帧、最后一帧精确对齐；据此定位热点后，应结合原始 ETL 分析，不能把比率当作方法精确耗时。

Smoke、Replay、Capture 都在证据整理完成后才写 Completed 状态，并在 manifest 和目录原子发布后作为正式结果。MCP 在 Controller 尚存活、正在发布时继续等待，避免把“状态已完成、manifest 尚未落盘”的间隙误判为失败。

## 多次采集分析

仍使用同一个 Controller。先按正常入口分别采集基线版本和候选版本，再明确列出参加比较的 manifest；不会扫描目录自动挑选结果，不会自动剔除离群运行。每组必须来自同一个 Player 构建，两组之间允许构建变化；场景、预算、画质、硬件、工具链、指标目录和探针模式等比较条件仍必须一致。重复路径、复制出来的相同 Capture ID，以及同一次采集同时进入两组都会被拒绝。

分析清单示例（路径替换成真实的 Completed Capture manifest）：

```json
{
  "schema": "third-person-performance-analysis-request/2",
  "comparison_kind": "Regression",
  "baseline_manifest_paths": [
    "D:/Performance/baseline-01/manifest.json",
    "D:/Performance/baseline-02/manifest.json",
    "D:/Performance/baseline-03/manifest.json"
  ],
  "candidate_manifest_paths": [
    "D:/Performance/candidate-01/manifest.json",
    "D:/Performance/candidate-02/manifest.json",
    "D:/Performance/candidate-03/manifest.json"
  ]
}
```

- Launcher → Performance → **分析重复采集…**：选择清单。分析不启动 Player、WPR 或 Unity 构建，会先构建 Controller；随后打开 Markdown 报告。操作在按钮回调中调度，不在 Inspector 绘制过程中扫描和分析。
- MCP `performance.analyze`：`action=start`、`request_path`，再以 `action=status`、`job_id` 查询任务；`performance.report` 的 `action=analysis`、`analysis_path` 读取结果。省略报告路径时使用最近一次显式分析。报告读取支持 `limit`，完整数据仍保存在文件中。
- 已构建的 Controller CLI：`ThirdPersonPerformanceCapture.Controller.exe --analysis-request="D:/Performance/repeats.json" --analysis-output="D:/Performance/analysis-01"`。输出目录必须不存在，不覆盖历史结果；此命令只分析文件。

Launcher/MCP 产物放在 `Library/Performance/Analyses/<独立编号>/`，包含输入清单快照 `request.json`、正式结果 `analysis.json` 和同源展示 `analysis.md`。报告保存清单哈希、每份源 manifest 的路径、哈希、Capture ID、Build ID、采集时长与预算状态。输入验证失败生成 `Rejected` 报告，CLI 返回 2；无法读取请求或写入结果等执行错误也返回 2，不能按“进程结束”判断比较有效。文件分析时禁止同时进行当前 Launcher 拥有的采集、Play 或编译，避免干扰测量。

每次运行的 P95/P99 分别作为一次观测，调用点使用每次运行的 P95；报告还包含整体 FPS 和丢弃 Tick 数。每组保留全部观测值，计算中位数、最小/最大值及中位绝对偏差（MAD），组间绝对差和百分比比较的是两组中位数。不会合并不同运行的帧，也不会让采集帧数更多的一次运行获得更大权重。

| 状态 | 含义 |
| --- | --- |
| Rejected | 身份或证据不符合比较条件，不产生性能结论 |
| Incomplete | 某些运行缺失对应指标或定义不一致；记录具体 Capture ID，不把缺失补成零 |
| InsufficientRepeats | 某组少于 3 次独立采集；可查看差值，重复证据不足 |
| ObservedRangesOverlap | 两组观测范围重叠 |
| CandidateRangeLower / CandidateRangeHigher | 候选的整个观测范围低于 / 高于基线 |
| IdenticalObservedValues | 所有观测值一致 |
| NotObserved | 调用点在所有运行中均未被调用，不计算耗时差，也不据此声称零开销 |

后四类为指标行状态。总报告 `Comparable` 只表示可以按当前口径比较，不等于优化通过。3 次是报告的最低重复数，不是统计功效保证；观测范围分离也不是置信区间或显著性检验。耗时和分配下降通常是目标，但 FPS 下降不是改善。建议交错采集 A/B、保持热身和后台负载一致，完整保留异常运行及其原因；本工具不会以排序或删数据掩盖漂移。

## 验证边界

早期工具完善只通过语法解析和差异静态检查。2026-09-28 已实际完成 Windows IL2CPP Development Player 构建、产物发布和 Controller 编译。Timeline 仍为纯数据，TreeClip/Marker 绑定已有技能编译程序的调用来源记录；22 个播放 Timeline 的 61 个 TreeClip、1 个 Marker 已检查，清除作者图引用的克隆仍可解析 139 项依赖。接入当前内置资源构建后，Player `ef114304062e17c8814a9351` 的 Smoke 已完成，实际日志确认不限帧且 VSync 为 0。随后 Replay 暴露了性能入口仍接收旧 `/3` 输入的问题，已与发布入口统一为当前 `/4`，并恢复录制相机初态。集中检查还修正了暂停空 Tick 进入 Session 统计、表现阶段缺少帧上下文，以及 Player 提前退出仍等待连接超时的问题。

上述修改已通过 Controller 与 Editor 编译，Editor 普通回放完整执行 `757f243033414fc7b123c97e2fcb0d70` 的 2716 帧，生成当前版本基线 Proof。2026-09-29，Player `fe981685443dab7200db321d` 已完成 Smoke、Replay 和正式 Capture `capture.20260928-161732.1d0ec10f9dd94ede8de4ab5b205bc7f2`；最终 manifest 为 Completed，Profiler、Span、ETL、函数热点、摘要均已发布。Capture 包含 2536 个逻辑 Tick、4304 个表现帧，持续 42.407 秒；期间丢弃 9 Tick，预算未通过。首次 Capture 曾因未执行的 FactProjection 类型没有注册 Marker 而失败，现按目录预先注册业务 Marker，未执行阶段仍无样本。

该包普通回放约 151.70 FPS，完整诊断运行约 101.49 FPS；这些是单次观测，不能直接当成校准后的工具开销。首次整体报告位于 `Client/Library/Performance/Reports/overall-performance-20260929.md`，原始证据位于 `Client/Library/Performance/Captures/<CaptureId>/`。没有优化前同条件基线、探针开销校准或重复采集证据，不宣称优化收益或完整 0 GC；没有新增测试。

GPU、整机内存峰值、长期泄漏与多硬件基准不在当前 Windows CPU/托管分配工具的报告范围内。

## 探针开销校准

输入快照排除 `Assets/Resources/PerformanceTestRunInfo.json` 和 `PerformanceTestRunSettings.json`：它们由 Unity Performance Testing 的构建回调生成并在构建结束删除，属于生成产物。其他输入继续比较；不一致时将前后快照保存到 `Library/Performance/BuildDiagnostics/<job>/inputs-before.json` 和 `inputs-after.json`，供定位实际变化，不发布该次 Player。

同一份分析清单将 `comparison_kind` 改为 `InstrumentationOverhead`，其余操作入口和报告格式不变。支持 Disabled → MarkerOnly、Disabled → Span、MarkerOnly → Span；每组仍必须使用同一个明确构建、至少 3 次采集才满足最低重复数。普通 `Regression` 继续严格禁止跨模式。

Player 构建输入使用 `build-inputs/2`：Assets 和 Packages 中 Unity 已导入资源的身份来自 `AssetDatabase.GetAssetDependencyHash`，不再递归读取全部美术资源、包缓存或包内的 `obj`、`Source~` 等目录。ProjectSettings、包清单与锁文件继续记录文件 SHA-256，同时记录 Unity 版本、目标、后端、构建选项、场景和共同的额外编译定义。构建前后比较这份指纹；开始时要求资源与场景已保存，不替用户保存。资源身份依赖 Unity 的导入数据库，不声称覆盖 Unity 未导入、构建回调私自读取的外部文件。旧版输入快照不再作为新版工具的有效构建证据，需要重新构建。

插桩输入与编译清单存放在仓库 `.performance-build/inputs/<输入快照哈希>/<插桩身份>/`。相同输入与模式复用路径及清单，内容未变时不重写输入文件；编译宏不再包含随机作业 ID。输入或模式变化会使用另一份明确输入，避免读取旧输入的插桩清单。Player 发布目录仍按本次产物身份保存，产物文件哈希仍计算一次。

构建先执行 `content-inputs`、`content-build`，使用现有 `TEngine.ReleaseTools.BuildContent` 构建并全量安装 DefaultPackage 内置资源；资源包来源身份不包含生成的 StreamingAssets，随后 `input-snapshot` 会将实际内置资源纳入 Player 输入。之后显示 `unity-build`、`input-verification`、`artifacts`、`artifact-hashes`、`publication` 阶段，Console 记录阶段耗时与总耗时。资源指纹阶段显示计数，产物阶段显示当前文件；这些阶段的进度窗口支持取消，Unity 原生构建阶段使用 Unity 自身的进度。MCP `status` 和 `Library/Performance/McpJobs/<job>.json` 返回 `phase`、`elapsed_ms` 与当前消息。`elapsed_ms` 是最近一次进度更新时的累计耗时，原生构建期间不伪造心跳或百分比。进程退出后遗留的 `running` 文件不代表构建仍在运行，应核对原作业和目标 Editor；不据此自动重试构建。

`performance.build_player` 的 `clean_build_cache` 默认 false；需要明确清理重建时传 true，同时作用于资源包与 Unity Player 缓存。Timeline 编辑器专属序列化字段变化后，曾出现增量 Player 反序列化崩溃，清理重建后通过该阶段；这不要求日常重复采集重新构建。采集帧率配置在运行环境就绪后、发送 READY 前应用，避免被后启动的 TEngine RootModule 覆盖；当次日志应记录 `Performance frame pacing: targetFrameRate=-1, vSyncCount=0.`。

快照 `build-inputs.json` 纳入 Player 文件闭包，其哈希随 Player 和 Capture manifest 保存；Capture 发布时复制这份证据并再次校验。校准除常规环境身份外，还要求两组 `build_inputs_hash`、内容、Pipeline、Pose Graph 与 Solver 身份一致。普通回归允许源码改变；校准不允许把源码差异与探针模式差异混在一起。快照覆盖的是上述已记录输入，不能据此消除操作系统调度、温度或外部工具链状态的波动。

校准只输出所有模式共同具备的 Unity Main Thread、帧内 GC 分配、整体 FPS 和丢弃 Tick 数，不将只有 Span 才有的调用点与 Disabled 的“缺失”相减。结果表示切换模式对这些整体指标的观测影响，包括调度等交互；不是每次 Enter/Exit 的精确成本，也不是业务代码优化收益。Disabled 仍启用 Recorder、Profiler 与 WPR，报告不会称它为零诊断开销。当前只实现校准链路，尚无此次实测的探针开销数字。
