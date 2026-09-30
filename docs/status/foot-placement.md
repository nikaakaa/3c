# Foot Placement 进展与证据入口

整理日期：2026-09-30。脚部质量尚未闭环；阶段报告归档不改变其中记录的失败事实，也不把编译、缓存 Apply 或单段修复当作整体行为验收。

## 正式 owner

输入来自原动画姿势、已提交 Body、动画脚步数据和正式世界查询。唯一 Foot Placement 帧事务提交 typed Goal Contribution，经唯一 Goal Assembly、FullBodyIK 和 final writer 输出。合同见[Foot Placement](../../openspec/specs/character-foot-placement-presentation/spec.md)与[动画管线](../../openspec/specs/character-animation-pipeline/spec.md)。剩余实施由[Foot Path 与 Landing 稳定化](../../openspec/changes/stabilize-character-foot-path-and-landing/proposal.md)拥有，具体清单直接读取该 change，不在本页复制。

## 尚未解决的证据

| 问题 | 可复核记录 |
| --- | --- |
| 零权重内部高度异常、恢复输出大跳及停步穿透 | [2dad 采样](../archive/records/foot-placement/corin-foot-2dad-review-20260928.md) |
| 脚尖跨踏面穿透、下坡晚降与末级响应滞后 | [b173 采样](../archive/records/foot-placement/corin-foot-b173-review-20260928.md) |
| 最终接触几何、膝盖突变和悬脚下降 | [65ec 采样](../archive/records/foot-placement/corin-foot-sample-65ec-20260928.md) |
| 当前踏面与下一步包络共同控制高度 | [c75d 行走/攻击采样](../archive/records/foot-placement/corin-foot-c75d-walk-attack-20260928.md) |

这些样本属于不同代码时点，不能混成一份“当前版本通过”报告。各次修改、撤回、限制和编译结果完整保存在[脚部阶段记录](../archive/records/foot-placement/)。

## 数据与参考

报告附带 JSON、查询图与[台阶连续性解释器](../diagnostics/foot-placement/ik-stair-continuity-explainer-20260930.html)已归入[脚部诊断](../diagnostics/foot-placement/README.md)。正式 CSV、Proof 与分析包仍在项目 `Diagnostics/`；原始数据内容、失败事实和采样身份未改。

2026-10-01 补齐新的 full 采样输入（capability 4 / sampler 3）：`CharacterFootPlacementModule` 在本帧求值前将左右脚生命周期状态保存到帧 Bank 的预分配页；诊断视图引用该页与 Bank 中的完整身体预测曲线，原生成采样程序在提交后同步写入 packet。输出包括 `foot/pre-state`、`future-body-trajectory` 和响应来源 UTF-8 表，以及事件时间、来源身份、作者总权重与 PosePlanHash。旧包缺失的轨迹及前态不能反推补造，必须重新采样；冻结、权重、骨盆和腿 IK 算法均未修改。

补字段的 `abfaf3895` 再次引入了大结构嵌套值拷贝。复用历史检查入口，在当前 Unity Mono 中测得 full Capture 实际栈帧为 953,392 字节；改为上述预分配存储引用后为 664,080 字节，减少 289,312 字节。历史修复 `413e931de` 后为 646,528 字节，core Capture 本次保持 205,904 字节。当前改动已通过 Unity 编译与实际 JIT 检查；原入口的 2716 帧诊断回放已请求，但 Editor 卡在进入 Play 的域重载，尚未完成本次采集和保存，不能声明闪退已修复或 IK 行为已改善。检查输出保留在 `tmp/ik-release-sampling-20260930/capture-stack-replay-input-before.json` 与 `capture-stack-replay-input-after.json`。相关执行规则已写入根目录 `AGENTS.md`；本轮未新增单元测试，既有业务测试仍由配套测试窗口维护。

原理阅读见 [GDC 学习文案](../reference/foot-placement/gdc2016-fitting-the-world.md)；历史否决与重复问题见[实现经验](../reference/foot-placement/implementation-lessons.md)。参数和当前运行路径仍由正式内容与现行规格拥有。
