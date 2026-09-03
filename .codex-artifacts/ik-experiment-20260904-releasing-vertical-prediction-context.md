# Foot IK 实验：Releasing 统一剥离预测竖直抬脚

候选提交：待提交

输入：`43357ff3cd384e5cba75d2c31175b116`

## 假设

`ee61480f8` 的条件要求旧 Contact anchor 不可用，因此主序列没有触发；但 1910–1914 的 Releasing 仍接受 FutureLanding，且 1914 的预测修正为纯竖直抬脚。即使 Releasing 保留 Contact 上下文，也不应把该预测竖直分量作为可见目标，否则 1916 新 Contact 会继续捕获高位输出。

## 唯一变量

在 `ResolveReleaseTarget` 中，只要 Releasing 的 `SwingMotion` 已接受，就把 `swingCorrection` 投影到 ComponentUp 的切平面；同时让 Releasing SupportTarget 的位置使用同一投影结果。FutureLanding 记录、查询、GroundPath、ContactVerification、PlantWorldResidual、CorrectionResponse、Transition、Pelvis 和 Profile 不变。

## 预期

1914 Releasing 输出不再被预测高踏面竖直抬高，1916 捕获的旧高位残差应降低；相比禁用 FutureLanding，事件上下文与生命周期计数保持。代价是所有 Releasing 预测脚失去竖直提前抬脚，需要检查 Plant 穿透、最终跳变、Bend/Extension、Pelvis 和左右脚。

## 验证状态

提交后用同一固定 Trace 正式回放并逐帧对账；不做视觉 Pass。若失败，保留采样和结论，精确回退 Runtime 候选并保留本记录。
