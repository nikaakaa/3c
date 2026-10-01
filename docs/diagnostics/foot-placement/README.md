# 脚部诊断

当前未闭环问题与正式 owner 见[脚部进展](../../status/foot-placement.md)，修改、撤回与后续样本见[脚部阶段记录](../../archive/records/foot-placement/)。本目录只保存对应样本的证据，不合并不同版本的测量结果。

| 证据 | 内容与边界 |
| --- | --- |
| [台阶连续性解释器](ik-stair-continuity-explainer-20260930.html) | 采样、源码与失败实验的交互解释；候选回放启动失败不算 A/B 通过 |
| [接触起点候选离线筛选](contact-handoff-offline-20261001.json) | 两包 9,414 条脚记录中的 173 次交接代数比较，以及原录制释放／水平冻结的逐帧核对；候选已拒绝进入 Replay，完整业务运行仍为 0 帧 |
| [下坡响应](corin-downhill-response-20260927.json) | 下坡高度与响应采样 |
| [E 行走接触修正](corin-e-walk-contact-fix-20260927.json) | 当次接触修正证据 |
| [E 行走手动采样](corin-e-walk-manual-sampling-20260927.json) | 手动运行观察数据 |
| [查询边缘图](corin-foot-query-edge-20260927.svg) | 脚部地形查询几何解释 |
| [旧数据抖动定位](corin-jitter-existing-data-20260927.json) | 原姿势、求解输出与身体位移差异 |
| [抖动修正记录](corin-jitter-fix-20260927.json) | 抬脚修复有录制核对；混合旋转修复只有当时编译记录 |
| [脚部连续场景测试](ik-tests/README.md) | 接触交接与 Releasing 历史对照、来源哈希、正式运行结果和逐帧 HTML |

正式采样 CSV、Proof 和分析报告仍位于项目 `Diagnostics/`，通过报告内链接读取；没有改变采样包内部结构或身份。涉及混合旋转的证据由本目录保存唯一原件，并由[动画分类](../animation/README.md)引用。
