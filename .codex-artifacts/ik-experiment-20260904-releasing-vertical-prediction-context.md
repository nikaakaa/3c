# Foot IK 实验：Releasing 统一剥离预测竖直抬脚

候选提交：`f73f5fd5c`

输入：`43357ff3cd384e5cba75d2c31175b116`

## 假设

`ee61480f8` 的条件要求旧 Contact anchor 不可用，因此主序列没有触发；但 1910–1914 的 Releasing 仍接受 FutureLanding，且 1914 的预测修正为纯竖直抬脚。即使 Releasing 保留 Contact 上下文，也不应把该预测竖直分量作为可见目标，否则 1916 新 Contact 会继续捕获高位输出。

## 唯一变量

在 `ResolveReleaseTarget` 中，只要 Releasing 的 `SwingMotion` 已接受，就把 `swingCorrection` 投影到 ComponentUp 的切平面；同时让 Releasing SupportTarget 的位置使用同一投影结果。FutureLanding 记录、查询、GroundPath、ContactVerification、PlantWorldResidual、CorrectionResponse、Transition、Pelvis 和 Profile 不变。

## 预期

1914 Releasing 输出不再被预测高踏面竖直抬高，1916 捕获的旧高位残差应降低；相比禁用 FutureLanding，事件上下文与生命周期计数保持。代价是所有 Releasing 预测脚失去竖直提前抬脚，需要检查 Plant 穿透、最终跳变、Bend/Extension、Pelvis 和左右脚。

## 验证结果

正式回放完成 1044/1044 帧，采样目录：`Diagnostics/GeneratedFootSampling/20260903-220854-0f030a86e482401f900de33a502e6c3c`。本轮没有新的 Analyzer report，使用新 CSV 与 184 基线逐帧对账。

GroundPath `1796/292`、Contact Edge `114`、Landing/Release `24/53`、Plant 正穿透 `27`、最大穿透 `0.083242m`、Plant verified `527`、Physical/Pelvis 覆盖与基线一致；右 1916 Plant output distance 由 `0.569929m` 降到 `0.558191m`，residual after-decay Y 由 `0.349391m` 降到 `0.329897m`，但这来自释放时序改变。

右脚 `ReleaseCompleted` 从基线 1914 提前到 1908，1910–1914 已进入 Swing；左脚出现 `0.491246m` 的 final sole/ankle/physical ankle 跳变（基线最大 `0.285683/0.283212/0.272745m`）。右脚最大 sole 跳变也增至 `0.282997m`。这属于明显生命周期和视觉回归。

结论：失败。剥离 Releasing 预测竖直修正会提前完成 Release，不能用残差小幅下降换取大跳变。保留采样和结论，精确恢复原 Releasing 目标与 SupportTarget 位置。
