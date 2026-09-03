# Foot IK 实验：新 Contact 接管前剥离旧 Release 标量

候选提交：`d7e4a4eb5`

输入：`43357ff3cd384e5cba75d2c31175b116`

## 假设

右脚 1914 的 Releasing 输出同时包含旧事件的完整世界位置和 `AnimationRelativeScalar` 标量修正。1916 变成新 Contact Event 时，当前代码把整个旧输出直接做 `PreviousOutput - NewContactTarget`，使旧动画标量也进入 `PlantWorldResidual`，形成约 0.5135m 的世界残差。完全丢弃旧输出又把最终脚底跳变放大到 0.828m。应该只剥离上一响应历史中的标量部分，保留剩余世界位置连续量。

## 唯一变量

`ReleaseCompleted` 的 Post-Transition 设置一次待重基标记。下一次直接进入 `EvaluatePlant` 且响应域仍为 `AnimationRelativeScalar` 时，从捕获参考点扣除 `AppliedDirection * Scalar`，再按既有完整向量 `PlantWorldResidual` 捕获和衰减；普通 Swing 若先执行一次正常动画响应则清除标记。其它 State、Target、Query、Residual 半衰期、插值速率、Pelvis 和 Profile 不变；同Event `Releasing -> Landing` 不经过 `ReleaseCompleted`，不走此变量。

## 预期

右 1916 的 Plant 残差 Y 由 `0.5135m` 降为约 `0.4026m`，保留 XZ 连续，降低跨一级台阶的悬空而不制造全量清除的瞬移。代价是首帧位移会比基线略大，需检查最终 Sole/Ankle、Plant 穿透、Bend/Extension、Pelvis 和左右脚差异。

## 验证结果

正式回放完成 1044/1044 帧，采样目录：`Diagnostics/GeneratedFootSampling/20260903-204022-d9f6155bb36c45e6a19b565a6e65639a`。本轮未生成新的 Analyzer report，使用新 CSV 与 184 基线逐帧对账。

| 指标 | 候选 | 184 基线 |
|---|---:|---:|
| GroundPath Accepted / Rejected | 1796 / 292 | 1796 / 292 |
| Contact Edge | 114 | 114 |
| LandingCompleted / ReleaseCompleted | 24 / 53 | 24 / 53 |
| Plant 正穿透样本 | 27 | 27 |
| 最大 Plant 穿透 | 0.083242m | 0.083242m |
| Plant verified | 527 | 527 |
| Physical / Pelvis 覆盖 | 2088 / 2088 | 2088 / 2088 |

右脚 1916 仍保留上一输出点，但在构造 Plant residual 前剥离上一 `AnimationRelativeScalar`：residual after decay Y `0.273898m`（基线 `0.349391m`），Plant output distance `0.527034m`（基线 `0.569929m`）；1918 分别为 `0.186359m`（基线 `0.237724m`）和 `0.358593m`（基线 `0.387778m`）。

代价是右脚最大 final sole 跳变 `0.319609m`（基线 `0.267713m`），增加约 `5.2cm`；final ankle/physical ankle 最大值保持 `0.265114m`。左 2067/2069 与基线一致。残差虽下降但主交接未消失，位移跳变恶化。

结论：失败。保留采样和结论，精确回退 Runtime 候选，恢复原有完整 `PreviousOutput - NewTarget` 捕获。
