# ReleaseCompleted 交接把一个下降步长分到两帧

## 假设

半步候选把右脚 1916 的首帧代价降到完整步长实验的一半，但只减少了一半旧响应。若首帧跳变来自交接量集中在同一帧，把一个正式下降速率步长拆成连续两帧，可能同时保留总的旧响应剥离和更平滑的腿部变化。

## 改动

- `ReleaseCompleted → Plant` 首帧沿旧响应方向剥离半个 `CorrectionResponseDecreaseSpeed * DeltaSeconds`。
- 下一帧从仍在衰减的 `PlantWorldResidual` 再剥离同样半步，然后清除待处理量。
- 不改目标、查询、TargetHeight、Residual 半衰期、Transition、Pelvis、Solver 输入或 Profile；若中间先回到普通 Swing，待处理量清除。

## 预期

总剥离量与完整步长候选相同，但右 1916 首帧 sole/ankle 跳变接近半步候选，右 1918 之后的 residual/output 尾巴进一步缩短；其它事件、穿透、左右脚和腿部覆盖不变。

## 回放

待固定 Trace 回放完成后补充实际 Samples、关键帧和完整诊断对比。若第二帧出现新的脚位跳变或穿透，保留样本并精确回退本轮代码。
