# Foot Placement 进展与证据入口

整理日期：2026-10-01。脚部质量尚未闭环；阶段报告归档不改变其中记录的失败事实，也不把编译、缓存 Apply 或单段修复当作整体行为验收。

当前 Goal 已按用户要求启动：继续动画混合、Landing/Releasing 弯直突变和台阶跳脚，使用已有采样还原正式函数，由已有配套窗口维护多个完整业务测试与各自 HTML；本轮不启动 Replay。

原生混合的帧计算已从 Unity 时钟读取中分离：`AnimationSlotBlendJob.ProcessAnimation` 读取 `AnimationStream.deltaTime`，交给同一个 `EvaluateFrame(float)`；完整校验、混合、Stored 捕获、历史提交及输出发布均由该函数执行。固定采样实验可直接提供记录的帧时间，不需要伪造 AnimationStream 或另写混合算法。函数体除移出时钟读取外逐字一致，Animation 定向编译 0 警告、0 错误（6.97 秒）。这是正式计算入口的整理，Native Slot 业务与 2041 释放效果仍待配套窗口实际验证。

该完整入口的首次 Mono 执行在 `CommitPersistentState` 抛出 `InvalidProgramException: Passing an argument of size '10200'`，未生成有效结果帧。实际 Stored 状态从 9816 增至 10200 字节，History 从 9808 增至 10192 字节；新增 FootMotion 后继续用 `NativeArray<T>` 索引器提交整份状态触及大参数边界。修正改为直接引用已有 Native 元素，逐字段提交 Stored/History/Scratch，原地清零并读取；Workspace 重置同样改为原内存清零，Runtime 的 Stored 身份消费者只返回所需的 `ulong`，不再返回完整状态。未增加每帧容器，也未修改混合公式；Animation 定向编译 0 警告、0 错误（6.64 秒）。

配套窗口已用对应工作区源码 SHA 在 Mono 实际重跑 2035～2046 共 12 帧：正式 ACL 解码、虚拟骨、Native Slot 历史提交和完整 Action Slot 通过。原踝位置最大还原误差 0.000994 mm、旋转误差 0，两版 203 根骨的局部位置全相等，预热后的帧计算为 0 B。2041 动作实际权重 0.531232，旧贡献为 0.282207429；当前选中 Action，正式曲线的脚高为 0.0232933685 m、接触为 0.8675726、锁权重为 0，事件为 `5517395441386351926`，不再用 Run 的 0.457684726 m 脚高。生产修复为 `d6d6ab9f9`，配套窗口将[实际结果](../diagnostics/foot-placement/ik-tests/releasing-action-native.json)、[源码绑定](../diagnostics/foot-placement/ik-tests/releasing-action-native-provenance.json)和[可播放业务说明](../diagnostics/foot-placement/ik-tests/releasing-action-native.html)提交于 `7bbc98bec`。本段未执行 Stored 捕获、FootFeatures 混合、脚端预测、Physics、完整 FBBIK 或 Burst 调度，也未执行 Runtime 的 Stored 身份读取；该消费者仅经定向编译。新动作事件对释放目标的影响仍待完整脚端还原，不能据此宣布拉直已修复。

释放段的骨盆初态有明确重建来源：2034 的正式结果为 Releasing，响应已计算且未完成，目标为 0、输出 −0.00117490336、速度 0.008545625、位置权重 1；PrimarySupport 明确为空。按 `ResolvePelvisRelease` 与 `AdvancePelvisResponse` 的提交赋值，支持侧为 default、事件为 0、坡度 Flat，世界 Goal 为该帧 AnimatedPelvis 加沿 up 的加权输出，HasValue 与 HasGoalWorldPosition 均为 true。2035 的 previous-output、previous-velocity、previous-target 和 previous-slope 与此一致。因此可以用 2034 正式输出种下 2035 连续双脚／骨盆实验的状态，不能用默认 Spring，也不必为这个状态一律回溯到 1768 的零权重帧；仍需历史函数重现后才能比较候选。这不补齐 FBBIK 弯曲历史。

首个动画混合修正将 FootMotion 从 `CharacterPoseSourceModule` 正式采样写入预分配 Source 页，经 `AnimationPrimitivePoseContribution` 传播，在 `AnimationSlotBlendJob` 的历史与 Stored 捕获中保存；`CharacterPoseWorldContextAdapter` 选择实际参与姿态的 Live/Stored 样本，交给原 FootPlacement 链路。它处理零权重 Live Idle 在 Stored 占据全部姿态时错误地提供锁脚请求的问题。Stored 保留当前接触与锁权重，静态姿态不继续预测未来落地；作者总权重和曲线未改。资源名称与曲线元数据在目录初始化，未增加每帧字符串转换。

同一输入链还确认 `CharacterPoseNativeAnimationSlotHandler` 重复乘动作权重：2041 帧骨骼实际使用 0.531232，贡献记录却为其平方 0.282207429，导致下游仍选 Run。修正让动作贡献只计权一次，基础姿态逐骨骼贡献按实际剩余权重计算，左右脚贡献同理；骨骼混合结果不变。问题追溯到 `ce6cdb09c7`。

动画程序集定向编译通过（0 警告、0 错误）。配套窗口先完成 13 帧无接触 Stored 和 70 帧真实接触 Stored 的正式选择／静态样本比较，随后在 Unity 中实际执行同样 83 帧的 `CharacterFootLifecycle`、Goal 与场景脚掌查询。两段旧版脚位和旋转均精确复现录制；两版预热计算均为 0 B。无接触段相对原动画的最大额外转角从 137.5741° 降至 0.000143°；真实接触段保留原锚点、随后正常释放，额外转角从 1.33253° 降至 0.05192°。有最终查询结果的帧未增加超过浮点误差的正穿透；1174 原本没有可用 Goal 或最终命中，净空仍标记为未知。

上述是 Unity 内独立编译的真实函数实验，不是正式 Test Runner 或完整 FBBIK 验证。它保持录制的混合动画姿态，预测、路径和骨盆可达性裁决仍为固定输入；未执行 Native Slot 历史页捕获及新贡献合成函数。真实接触段固定髋位置的超腿长记录从 4 帧变成 5 帧，两版最大伸展比均为 1.003319；新增的 954 帧约超长 0.54 mm。这项回归已保留，不能宣称伸直改善。结果在 `ik-tests/stored-*-goal.json`，完整边界见[动画来源审计](../diagnostics/foot-placement/ik-stair-continuity-explainer-20260930.html#stored-motion-audit)。实验时共享 Editor 仍报告其它模块编译失败，因此未把磁盘候选当成主程序集已加载；本轮没有启动 Replay。

上述生产修正已独立提交 `18e1f2a4f`，配套窗口维护 [Stored 业务函数比较 HTML](../diagnostics/foot-placement/ik-tests/stored-foot-motion.html)及对应固定输入与结果。另确认未被该提交覆盖的 Live→Live 旋转跳变：新 full 右脚 2206→2207，WalkStart 与 Idle 的贡献由 55.556% / 44.444% 交接为 44.444% / 55.556%，原踝只转 3.228°，有效目标转 34.329°。未经权重的目标旋转只变 0.575°，支撑法线均向上，锁权重却从 0 切到约 1；因此只平滑原始目标四元数不能处理这一触发条件。历史 32946→32947 也确为两条 Live 正常跨过主来源交接点，不是 Stored 或动作重复乘权，见[Live 混合审计](../diagnostics/foot-placement/ik-stair-continuity-explainer-20260930.html#live-switch-motion-audit)。

第一版旋转候选 `15894ee7b` 已完成三个窗口的真实源码函数实验：Live 23 帧、无接触 Stored 13 帧、从关闭帧开始的真实接触 Stored 85 帧。Live 的整窗最大转角从 37.0679° 降到 25.1218°，相对原动画的修正变化从 31.1801° 降到 9.0765°；最终脚掌查询无新增正穿透，三窗均为 0 B。跨版本比较仍判为失败：Live 固定髋的最大伸展比从 0.983891 升至 0.998176，真实接触 Stored 也存在同帧余量下降。2211 原动画膝角为 25.8279°，按同一髋和实际骨段长度计算的目标所需膝角从 20.8593° 降至 6.9297°。后者是几何需求，不是新 FBBIK 的输出。保留[跨版本对照](../diagnostics/foot-placement/ik-tests/rotation-business-comparison.json)，不能把单版执行成功当作业务通过。

旋转修订候选 `8c0e878f8` 随后完成 2192～2249 的 58 帧双脚实验：正式 Evaluate→PrimarySupport→Intent→PreparePelvis→ResolvePelvis→Complete 连续执行，历史版两脚脚位、旋转及骨盆 Goal 重现录制，三版预热计算均为 0 B。修订版右脚 2207 转角从 34.3287° 降至 13.0994°，相对原动画的最大修正变化从 31.1801° 降至 9.9366°；2211 所需几何膝角从 20.8593° 恢复到 24.5224°，原动画为 25.8278°。但左脚 2229 在原动画仍弯曲 44.4257° 时，目标伸展比从 0.981079 增至 1.007762，距离超过实际腿长约 5.40 mm；两版候选都把左脚超长时间从 1 帧延长到 3 帧（16.67→50 ms）。骨盆 Goal 在该窗未因此改变，最终查询无新增正穿透不能抵消新增拉直。完整 FBBIK 仍未执行，这些是实际 Goal 的几何需求。

因此已从运行代码撤回 `15894ee7b` 与 `8c0e878f8` 的两版旋转响应，6 个相关生产文件精确恢复到 `552f13083`；保留 `18e1f2a4f` 的动画来源和动作贡献修正、固定候选源码与失败结果。不能再次只凭单脚、单帧转角收益接受旋转滤波，也不能用调短半衰期或放宽弯曲余量判据掩盖问题。测试窗口继续维护同一[旋转业务说明](../diagnostics/foot-placement/ik-tests/live-rotation-response.html)，后续先验证正式 Native Slot 捕获、动作贡献合成与 2041 释放，冻结和骨盆算法未改。

撤回同时删除候选专用旋转前态及诊断字段，采样结构恢复 capability 4 / full sampler 3；没有运行候选版本的新采集或 Replay。撤回后的 Animation 与 FootIkDiagnosticSampling 定向编译均 0 错误、0 警告，分别用时 6.99 秒和 5.06 秒，日志在 `tmp/ik-release-sampling-20260930/rotation-withdrawal-*-build-20261001.log`。共享 Editor 最后检查仍报告其它模块编译失败；主 Animation 最后核实的 MVID 为 `cd6cec60-06dd-47f0-9dd6-6ad8029c56d7`。独立函数实验与定向编译均不代表主程序集已加载，也不替代完整骨骼验收。

双脚骨盆还原的输入已核对：`rootPos + rootRotation * (leg/leg-pose/original-hip − pelvis-goal/component-position * pelvis-goal/position-weight)` 与直接记录的 `foot/resolved/reach/landing-hip` 在 3305 条可用记录中最大误差为 0.0056 mm。2192 的真实总权重 0 会正式清空脚端、PelvisSpring 和无候选的 PrimarySupport，支持连续复算的明确起点；不能拿骨盆之后的髋直接作为 FootLifecycle 计算前输入。该核对不补齐完整 FBBIK 骨架及弯曲历史。

58 帧窗口中的 2224 最大世界转角约 178.67°，原动画脚踝本身为 178.6725°，表现根为 176.0301°，不能把该峰当作 IK 追加旋转。双脚报告中的原动画骨段使用同一姿态的髋、膝、踝，不能混合新骨盆后的髋、骨盆前膝和旧骨盆后的踝。

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
