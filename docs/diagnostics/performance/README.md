# 性能诊断

| 报告 | 内容与边界 |
| --- | --- |
| [9 月 29 日采集](performance-run-20260929.md) | 当次 Player 版本、构建/Smoke 失败、正式 Span Capture 与热点初读；没有 A/B 优化收益结论 |
| [探针覆盖与旧采集指标](performance-coverage-20260929.md) | 源码声明与旧 Player 的 27 项指标；新增声明不等于实际采样 |
| [9 月 30 日性能与 GC 核查](../../../.performance-build/reports/20260930-performance-gc-review.md) | 旧/最近 Player 的计数与 CPU 探针热点、采集 Agent 大缓冲、回放状态字符串修复、Fixed 求值结果与 Timeline 请求复用；完整 Client 编译 0 错误，0 GC 未完成 |
| [9 月 30 日 GC 父节点原始汇总](gc-parent-samples-20260930.json) | 从当时的 MCP 返回恢复完整节点名称、查询和 300 帧局部汇总；没有启动新采集 |

正式采集、报告读取和比较口径由[性能采集工具](../../../Tools/ThirdPersonPerformanceCapture/README.md)拥有。Player、manifest、原始 Capture 与失败 Gates 仍由项目 `Library/Performance/` 保存，具体身份从对应报告读取；本次没有移动这些产物或重新采集。
