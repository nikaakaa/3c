# Foot IK 实验：新 Contact 接管前剥离旧 Release 标量

候选提交：待提交

输入：`43357ff3cd384e5cba75d2c31175b116`

## 假设

右脚 1914 的 Releasing 输出同时包含旧事件的完整世界位置和 `AnimationRelativeScalar` 标量修正。1916 变成新 Contact Event 时，当前代码把整个旧输出直接做 `PreviousOutput - NewContactTarget`，使旧动画标量也进入 `PlantWorldResidual`，形成约 0.5135m 的世界残差。完全丢弃旧输出又把最终脚底跳变放大到 0.828m。应该只剥离上一响应历史中的标量部分，保留剩余世界位置连续量。

## 唯一变量

`ReleaseCompleted` 的 Post-Transition 设置一次待重基标记。下一次直接进入 `EvaluatePlant` 且响应域仍为 `AnimationRelativeScalar` 时，从捕获参考点扣除 `AppliedDirection * Scalar`，再按既有完整向量 `PlantWorldResidual` 捕获和衰减；普通 Swing 若先执行一次正常动画响应则清除标记。其它 State、Target、Query、Residual 半衰期、插值速率、Pelvis 和 Profile 不变；同Event `Releasing -> Landing` 不经过 `ReleaseCompleted`，不走此变量。

## 预期

右 1916 的 Plant 残差 Y 由 `0.5135m` 降为约 `0.4026m`，保留 XZ 连续，降低跨一级台阶的悬空而不制造全量清除的瞬移。代价是首帧位移会比基线略大，需检查最终 Sole/Ankle、Plant 穿透、Bend/Extension、Pelvis 和左右脚差异。

## 验证状态

提交后用同一固定 Trace 正式回放并逐帧对账；不做视觉 Pass。若失败，保留采样和结论，精确回退 Runtime 候选并保留本记录。
