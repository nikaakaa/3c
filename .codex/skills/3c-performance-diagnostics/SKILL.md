---
name: 3c-performance-diagnostics
description: 使用 3C 项目现有 Windows IL2CPP Player 性能工具进行 CPU 与托管分配诊断、读取采集报告、比较优化前后多次运行，或校准 Disabled、MarkerOnly、Span 探针开销。适用于实际性能测量和报告分析；不用于仅凭源码的性能审查、一般编译或功能回放，也不自动进入并行开发流程。
---

# 3C 性能诊断

使用项目已有的 Player、Controller 和分析器，把固定输入、构建身份、原始采样和结论对应起来。先区分用户要定位热点、比较业务优化，还是测量诊断工具自身的开销，再选择对应路径。

## 范围与入口

- 从当前工作区确定仓库根目录；Unity 项目通常为 `3cDemo/Client/3C_Client`。先读取目标项目的规则及 `Tools/ThirdPersonPerformanceCapture/README.md`，需要具体命令时读取 [操作与报告参考](references/operations.md)。schema、工具参数和能力以当前源码及工具发现结果为准。
- 单任务可以直接使用本 skill。仅因性能检测、多个聊天窗口或已有多个 worktree，不调用 `3c-fast-development-validation`，不创建 worktree、不启动其他 Agent。已经明确进入并行开发时，才按该工作流关联证据；同机性能采集顺序执行。
- 沿用 `performance.*` 工具和 `ThirdPersonPerformanceCapture.Controller`，不另写采样器、统计器或绕过检查的启动脚本。MCP 与 Launcher 调用同一 workflow。
- 用户只问用法、位置或要求读取报告时，执行只读工作。要求实际采集时沿用已有执行授权；会话中禁止启动进程、构建或采样的限制优先，不因调用 skill 自动解除。允许的工作直接完成，不反复确认。
- 真正需要操作 Unity MCP 时，读取 [3C Unity 操作](../3c-unity-mcp/SKILL.md)，由该入口衔接通用连接流程和本项目执行规则。

## 根据任务选择路径

| 用户目标 | 执行路径 |
| --- | --- |
| 解释已有报告或找热点 | 读取明确的 Capture manifest、Summary 和必要的原始证据，不重新采集 |
| 对已有多次结果做比较 | 构造显式清单，使用正式离线分析器；不启动 Player |
| 获取当前版本性能数据 | 固定输入与 Scenario → 选模式构建 Player → Smoke → Replay → Capture |
| 验证优化收益 | 优化前后各保留相同模式、同条件的多次采集，使用 `Regression` |
| 测探针自身开销 | 保持记录的构建输入和环境一致，分模式采集，使用 `InstrumentationOverhead` |

离线分析也有执行成本：Launcher/MCP 的分析入口会先构建 Controller，再启动分析进程；已有 Controller 的 CLI 不构建 Player，但仍启动分析进程。禁止起进程时只读取已有产物，不把“离线”误解为无进程。

## 采集闭环

1. 发现已有固定输入、Scenario、工具链和 Player，记录精确路径与身份。优先复用匹配产物；不把“最近一次”当成用户指定的基线，不立即要求重新录制输入。
2. 选择探针模式：`MarkerOnly` 用于阶段成本；`Span` 用于方法、Actor、LogicTick 和慢调用；`Disabled` 关闭业务方法织入，用于整体基线或开销校准。Disabled 仍有 Recorder、Profiler 和 WPR 成本。
3. 需要构建时使用正式 Windows x64、IL2CPP、Development Player 路径。未保存输入或构建输入变化时保留失败原因，不代替用户保存全部资产，不删除快照检查。代码、资源、模式或必要合同改变后重新构建；同一构建重复采集不反复打包。
4. 对选定 Player 顺序执行 Smoke、Replay、Capture。前两步检查启动与固定输入回放，本身不是性能采集。只有与当前 Player、Scenario 匹配的 Completed 检查结果才能放行 Capture，不手改门禁或 manifest。
5. 启动后保存 `job_id`，查询同一作业直到结束。构建耗时长时先读 `phase`、`elapsed_ms`、当前消息与目标 Editor 日志；没有编译阶段证据时，不把等待归因于 IL2CPP。超时或域重载先核对原作业，不重复启动；仅取消本任务拥有的运行。确认最终 manifest 和所需产物已正式发布，不能只看进程退出、状态字或旧路径。
6. 保留本次输出及失败证据。读取 Summary 后按问题深入方法样本、Unity Profiler 或 WPA 数据；不自动打开外部应用，不自动压包、迁移或提交采样产物。

## 前后比较与校准

### 业务优化：Regression

- 每组来自同一个确切 Player 构建；组间允许代码改变，探针模式、场景、输入、环境和采集配置必须满足正式比较器的条件。
- 需要重复性证据时，每组至少 3 次独立采集。保留每次结果，不复制一次采集凑次数，不自动删离群点或挑最快结果。明确记录不同组的采集顺序与可能的后台负载变化。
- 使用各次运行的 P95/P99 等观测比较组中位数和波动，不把所有运行的帧混成一个大样本。3 次只是最低报告门槛，不是统计显著性保证。
- 用户授权修改代码时，先获取可比较基线。修改已经完成但没有基线时，先找可靠的旧产物；不能用当前采集反推优化前数据，也不擅自切换共享工作区重建旧版本。

### 工具开销：InstrumentationOverhead

- 使用独立的分析类型，不放宽普通回归的模式校验。支持 Disabled → MarkerOnly、Disabled → Span、MarkerOnly → Span。
- 两组记录的构建输入快照、内容、Pipeline、Pose Graph、Solver 与环境必须满足正式比较器的同一性要求。源码改变与探针模式改变不能混在一次校准中。
- 只解释共同的整体指标：Unity Main Thread、帧内 GC 分配、整体 FPS、丢弃 Tick 数。只有 Span 才有的调用点不能与 Disabled 的缺失数据相减。
- 结果是模式变化的整体观测影响，包含调度等交互，不是每次探针 Enter/Exit 的精确耗时，也不是业务优化收益。

两种分析都由 AI 根据已确认的 Capture 路径准备清单，提交已有分析器；不把手写清单交给用户作为默认前置工作。清单、输出路径及状态字段见参考文件。

## 读数与交付

- 先核对报告类型、状态、来源和样本覆盖，再看差值。`Completed` 是采集/作业状态，`Comparable` 只表示满足比较条件，都不等于优化通过。
- `Rejected` 不产生比较结论；`Incomplete`、缺失指标或未调用探针不能按零解释。百分比只有在 `percent_delta_available=true` 时有效；预算未完整评估不能称为通过。
- Span 是经过时间，包含抢占、等待和子调用，不是独占 CPU。父子耗时不可相加，两个 P95 的比值不是父子占比。Windows 热点是采样计数；当前热点差值按 Player 采集秒数归一化，不能叫作方法毫秒差。
- 核对未解析符号样本、丢弃 Tick、异常结束与低样本量提示。观测范围重叠说明结果存在波动；范围分离也不是显著性检验。FPS、耗时、分配各自解释方向，不统一按“负数就是更好”判断。
- 只对已覆盖的版本、输入、时段、模式和平台下结论。性能数据不替代 Gameplay 正确性验证，局部 0 B 不代表整个项目或所有平台零 GC。

交付说明输入与模式、基线/候选身份、采集次数、关键差值与波动、主要热点、证据路径和未验证部分。给出可读报告链接及精确 Capture/Analysis 路径，区分“代码已实现”“编译通过”“实测完成”。仅在用户还授权了实现时继续修改运行时代码。
