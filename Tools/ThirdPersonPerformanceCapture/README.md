# 性能采集口径

入口仍是 Launcher Performance 区或 `performance.*` 工具，两者调用同一 workflow。按明确模式构建 Player，再依次运行 Smoke、Replay、Capture；没有通过对应 Player 的门禁就不能采集。

## 三种构建模式

| 模式 | 可用数据 | 用途 |
| --- | --- | --- |
| Disabled | Unity Main Thread、GC、帧率、Windows 函数采样和线程等待 | 关闭业务探针的基线 |
| MarkerOnly | 上述数据及业务 RenderFrame 指标、Unity Profiler Marker | 查看阶段成本，不保存单次调用跨度 |
| Span | 上述数据及调用点、Actor、RenderFrame、LogicTick、异常结束记录 | 定位具体方法及慢调用 |

Disabled 关闭的是业务方法织入，Unity Profiler、Recorder 和 WPR 仍有采集开销，不能把它叫作完全没有诊断开销的发布包。MarkerOnly 不具备单 Tick 耗时，不把一帧内多个 Tick 的总耗时写成单 Tick 样本。只有 Span 生成 `instrumentation-spans.bin`。

普通 Baseline 比较要求相同场景、环境、采集工具、指标目录和织入身份，允许代码构建身份变化，因此能比较重构前后。不同探针模式会被拒绝作为普通性能回归比较。要估计探针成本，应在同一代码、同一输入和同一环境下分别采集三种模式，多次观察整体指标；不能把模式变化解释成业务优化。

## 时间代表什么

Span 是同步方法进入和退出时的单调计时差值，包含被系统抢占、等待和子调用时间，不是线程独占 CPU 时间。文件记录采集 Player 的 `Stopwatch.Frequency`，分析器使用该频率换算，不借用分析进程的频率。Windows 函数热点是采样次数，线程等待由 Context Switch 表提供，两者不冒充方法精确耗时。

业务帧指标来自 Recorder 的逐帧汇总；LogicTick 指标来自按 Tick 聚合的 Span；调用点分布来自逐次 Span。没有样本的预算指标列入 `unavailable_budget_metrics`，同时 `budget_evaluated=false`、`budget_passed=false`，不把没测到解释为零开销或预算通过。

父子阶段使用 `parent_inclusive_total_ratio` 表示同次采集总耗时之比，不使用两个 P95 的比值。父阶段已经包含子阶段，不能相加。递归或多次进入同一阶段的累计值也不能理解为互斥的帧时间切片。线程热点按 Exclusive 样本汇总，避免把同一调用栈的各层 Inclusive 样本重复相加。

## Pose 覆盖

`presentation.animation` 包围表现事务，下面分为 Pose Prepare、Evaluate、Commit，以及 Source Barrier。Evaluate 下另有 Foot Placement 和 FullBodyIK 探针。Source Barrier 包含资源验证、回收准备和后端 Evaluate，不是纯骨骼计算时间。没有实际探针的旧阶段从指标目录删除；新增阶段要同时增加所属目录定义和方法声明。

报告使用 summary/3，跨度使用 spans/2 和 layout revision 2。旧文件不会通过新格式检查；重新构建及采集后才能产生当前格式。源码编译通过不代表 Player 构建、真实采集、探针成本或 Gameplay 行为已验证。
