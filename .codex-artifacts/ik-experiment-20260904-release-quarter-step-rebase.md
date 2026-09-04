# ReleaseCompleted 后剥离四分之一个下降速率步长

## 假设

半步交接相对原行为降低了右 1916 的 Plant output，但仍增加首帧脚位跳变。将同一旧响应方向的剥离量降为正式下降速率步长的四分之一，可作为剂量对照：若残差收益仍可观而跳变代价接近消失，说明交接需要小幅校正；若收益不可见，则当前半步也不值得保留。

## 改动

- 保留 `ReleaseCompleted → Plant` 的单次交接标记和旧响应方向。
- 只把 `CorrectionResponseDecreaseSpeed * DeltaSeconds` 的剥离上限改为 `0.25` 倍，即固定 Trace 下 `0.00625m`。
- 查询、目标、TargetHeight、PlantWorldResidual 半衰期、Transition、Pelvis、Solver 和 Profile 数值不变。

## 预期

右 1916 residual/output 相对恢复基线只小幅下降，sole/ankle 首帧跳变应低于半步候选；事件、穿透、bend/extension 和左右覆盖保持不变。

## 回放

待固定 Trace 回放完成后补充实际 Samples、关键帧和完整诊断对比。若收益低于可见变化或出现回归，保留样本并精确回退本轮代码。
