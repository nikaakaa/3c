# 性能诊断

| 报告 | 内容与边界 |
| --- | --- |
| [9 月 29 日采集](performance-run-20260929.md) | 当次 Player 版本、构建/Smoke 失败、正式 Span Capture 与热点初读；没有 A/B 优化收益结论 |
| [探针覆盖与旧采集指标](performance-coverage-20260929.md) | 源码声明与旧 Player 的 27 项指标；新增声明不等于实际采样 |
| [9 月 30 日性能与 GC 核查](../../../.performance-build/reports/20260930-performance-gc-review.md) | 旧/最近 Player 的计数与 CPU 探针热点、采集 Agent 大缓冲、回放状态字符串修复、Fixed 求值结果与 Timeline 请求复用、Control 私有可写数组和 hash 格式化修复；Client / Core 编译 0 错误，0 GC 未完成 |
| [9 月 30 日 GC 父节点原始汇总](gc-parent-samples-20260930.json) | 从当时的 MCP 返回恢复完整节点名称、查询和 300 帧局部汇总；没有启动新采集 |

当前静态清理沿固定输入的 Fixed 求值和 Timeline 提交链推进：播放器与求值输出 bank 按正式释放边界复用，Timeline 快照一次合并提交，Fixed 哈希编码复用排序工作区；GE 编码直接读取已提交的有序快照数组，预测属性按索引访问，删除编码阶段的深复制及 `ToArray / OrderBy`。此前数组二次复制的错误判断已纠正。2026-10-01 的 Fixed / Float32 编译通过，最终完整 Client 为 25 个既有警告、0 个错误；此前失败日志保留。最终状态、Timeline 嵌套快照、GE 变化快照和可写工作集合、请求字符串仍有分配。完整调用链、剩余项和检查日志见[性能运行期分配清理](../../路径/性能运行期分配清理.md)；尚未进行新 Player 或重新采集。

正式采集、报告读取和比较口径由[性能采集工具](../../../Tools/ThirdPersonPerformanceCapture/README.md)拥有。Player、manifest、原始 Capture 与失败 Gates 仍由项目 `Library/Performance/` 保存，具体身份从对应报告读取；本次没有移动这些产物或重新采集。
