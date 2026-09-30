# Foot Placement 进展与证据入口

整理日期：2026-10-01。脚部质量尚未闭环；阶段报告归档不改变其中记录的失败事实，也不把编译、缓存 Apply 或单段修复当作整体行为验收。

当前 Goal 已按用户要求启动：继续动画混合、Landing/Releasing 弯直突变和台阶跳脚，使用已有采样还原正式函数，由已有配套窗口维护多个完整业务测试与各自 HTML；本轮不启动 Replay。

首个动画混合修正将 FootMotion 从 `CharacterPoseSourceModule` 正式采样写入预分配 Source 页，经 `AnimationPrimitivePoseContribution` 传播，在 `AnimationSlotBlendJob` 的历史与 Stored 捕获中保存；`CharacterPoseWorldContextAdapter` 选择实际参与姿态的 Live/Stored 样本，交给原 FootPlacement 链路。它处理零权重 Live Idle 在 Stored 占据全部姿态时错误地提供锁脚请求的问题。Stored 保留当前接触与锁权重，静态姿态不继续预测未来落地；作者总权重和曲线未改。资源名称与曲线元数据在目录初始化，未增加每帧字符串转换。

同一输入链还确认 `CharacterPoseNativeAnimationSlotHandler` 重复乘动作权重：2041 帧骨骼实际使用 0.531232，贡献记录却为其平方 0.282207429，导致下游仍选 Run。修正让动作贡献只计权一次，基础姿态逐骨骼贡献按实际剩余权重计算，左右脚贡献同理；骨骼混合结果不变。问题追溯到 `ce6cdb09c7`。

动画程序集定向编译通过（0 警告、0 错误）。配套窗口已执行 13 帧无接触 Stored 和 70 帧真实接触 Stored 的正式选择／静态样本比较，均通过且预热计算 0 B；结果已发布至 `docs/diagnostics/foot-placement/ik-tests/stored-*-functions.json`，独立构建产物留在项目 `Temp/FootStoredPoseFunctions/`。它们尚未覆盖 Native Slot 历史页捕获、新贡献合成、Goal、Physics 或 FBBIK，不能称为完整动画或 IK 验证。共享 Editor 因网络脚本程序集引用错误仍未成功重编译；本轮没有启动 Replay。具体证据与限制见[动画来源审计](../diagnostics/foot-placement/ik-stair-continuity-explainer-20260930.html#stored-motion-audit)。

上述生产修正已独立提交 `18e1f2a4f`，配套窗口已发布 [Stored 业务函数比较 HTML](../diagnostics/foot-placement/ik-tests/stored-foot-motion.html)及对应固定输入与结果，正在接续真实 Goal 与 Physics。另确认未被该提交覆盖的 Live→Live 旋转跳变：新 full 右脚 2206→2207，WalkStart 与 Idle 的贡献由 55.556% / 44.444% 交接为 44.444% / 55.556%，原踝只转 3.228°，有效目标转 34.329°。历史 32946→32947 也确为两条 Live 正常跨过主来源交接点，不是 Stored 或动作重复乘权。后续仍须保留作者权重和原子事件语义，以真实脚掌净空约束验证最终旋转交接；旋转响应尚未修改，见[Live 混合审计](../diagnostics/foot-placement/ik-stair-continuity-explainer-20260930.html#live-switch-motion-audit)。

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

2026-10-01 补充 full 的脚端复算输入（capability 4 / sampler 3）：`CharacterFootPlacementModule` 在本帧求值前将左右脚生命周期状态保存到帧 Bank 的预分配页；诊断视图引用该页与 Bank 中的完整身体预测曲线，原生成采样程序在提交后同步写入 packet。输出包括 `foot/pre-state`、`future-body-trajectory` 和响应来源 UTF-8 表，以及事件时间、来源身份、作者总权重与 PosePlanHash。旧包缺失的轨迹及前态不能反推补造，必须重新采样；冻结、权重、骨盆和腿 IK 算法均未修改。这里不是完整骨骼 IK 的输入快照：求解前的整份骨骼姿势及完整 `CharacterFullBodyIkBendHistory` 尚未采集。

补字段的 `abfaf3895` 再次引入了大结构嵌套值拷贝。`23b52330c` 改为上述预分配存储引用后，实际 Unity Mono full Capture 栈帧从 953,392 降至 664,080 字节，减少 289,312 字节；历史修复 `413e931de` 后为 646,528 字节，core 本次保持 205,904 字节。编译与 JIT 检查通过，结果保留在 `tmp/ik-release-sampling-20260930/capture-stack-replay-input-before.json` 与 `capture-stack-replay-input-after.json`。执行规则已写入根目录 `AGENTS.md`；既有业务单元测试仍由配套测试窗口维护。

01:30 已完成原入口的完整运行验证：精确输入 `757f243033414fc7b123c97e2fcb0d70` 的 2716 帧全部回放、停止并保存；新 Foot 包为 `20260930-172333-3cb49fba52604669bfae7fe6eda3aabc`，主表 5432 行、1279 列，左右脚各 2716 行。完整身体轨迹表 6348 行，与每行声明的点数全部一致；响应来源字节表 347520 行，计算前脚态包含实际状态变化。正式 Proof 的 `replay_completed` / `capture_completed` 均为 true，错误列表为空，Foot 与 Presentation 清单均已发布；随后已退出本次 Play。该结果证明这条完整采样流程没有再发生启动闪退，不代表 Landing / Releasing 运动质量已修复。自动比较结果为 `baseline-created:2716`，不是候选 A/B 通过。

本次“编译十分钟”的实际记录是进入 Play 的刷新耗时 604.728 秒，其中 `CompileScripts` 仅 1.760 毫秒，`ProcessInitializeOnLoadAttributes` 占 314.061 秒，资源分类占 140.160 秒；片段保留在 `tmp/ik-release-sampling-20260930/editor-play-reload-20261001.log`。之后连接自行恢复，未重启或重复发起刷新。导出期间观察到整机可用内存约 1.2 GB、提交内存占用 93% 并有大量换页；这是当时的资源压力证据，不能据此断言某个初始化器就是根因。初始化器级别的耗时来源尚未定位，未修改其逻辑。

原理阅读见 [GDC 学习文案](../reference/foot-placement/gdc2016-fitting-the-world.md)；历史否决与重复问题见[实现经验](../reference/foot-placement/implementation-lessons.md)。参数和当前运行路径仍由正式内容与现行规格拥有。

2026-10-01 按用户要求核对原动画伸直和弯曲速率，并逐项审计 HTML 实施状态。当前采样与 9 月 30 日同输入基线的 5432 条脚记录在膝角、脚位、权重和末端补高上均一致；局部接触目标修正不等于六项均已完成。包内原动画对照仍显示 IK 额外拉直和输出脚掌命中高面后的硬补高，详见[原动画、角速度与六项状态报告](../diagnostics/foot-placement/ik-stair-continuity-explainer-20260930.html#knee-audit)。本次是已有数据的只读统计，未新增或运行单元测试；输入回放、脚端函数复算和完整 FBBIK 还原的边界在同一报告中明确区分。
