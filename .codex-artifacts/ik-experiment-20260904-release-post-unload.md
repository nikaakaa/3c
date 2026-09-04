# ReleaseCompleted Post-Transition 卸载旧标量

## 假设

当前半步实验在新 Contact 的 1916 帧才剥离旧 `AnimationRelativeScalar`，因此旧 Release 输出仍完整存在于 1914，Contact 首帧仍承担主要变化。若在 `ReleaseCompleted` 的 Post-Transition 先沿旧响应方向卸掉完整旧标量，同时保留世界 XZ 输出连续性，并让下一次 Plant 不再重复剥离，交接变化可能被提前分布到 Release 帧，减少 1916 的脚位跳变。

## 改动

- 仅在 `ReleaseCompleted` Post-Transition、已有 AnimationRelativeScalar 历史时，从 `EffectiveCorrection` 与 `PreviousResponseOutputPoint` 卸掉该历史标量。
- 保留响应方向和世界输出点的 XZ 连续性；将标量历史置零，避免下一次 Plant 再做半步剥离。
- 不改 Contact 查询、目标、PlantWorldResidual 公式/半衰期、状态转换、Pelvis、Solver 或 Profile。

## 预期

右 1914 的 Release 输出会提前向动画基线移动，右 1916 的 Plant 捕获残差和首帧 sole/ankle 跳变应同时下降；必须检查 Release/Landing 事件、腿部 bend/extension、穿透和左右覆盖。

## 回放

待固定 Trace 回放完成后补充实际 Samples、关键帧和完整诊断对比。若 Release 帧提前跳变、Contact 残差方向异常或穿透增加，保留样本并精确回退本轮代码。
