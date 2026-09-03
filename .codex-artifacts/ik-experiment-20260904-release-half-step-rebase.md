# ReleaseCompleted 后剥离半个下降速率步长

## 假设

上一轮完整剥离一个下降速率步长使右脚 1916 的 Plant output 距离下降，但交接首帧脚底和踝部跳变增加。若问题主要来自剥离量过大，保留同一交接时机和方向、只剥离半个正式下降步长，应在降低旧响应与控制首帧跳变之间取得更小的代价。

## 改动

- 保留 `ReleaseCompleted → Plant` 的单次交接标记。
- 沿旧 `AnimationRelativeScalar` 响应方向剥离 `CorrectionResponseDecreaseSpeed * DeltaSeconds * 0.5`，即固定 Trace 下的 `0.0125m` 上限。
- 查询、目标高度、PlantWorldResidual 衰减、Transition、Pelvis、Solver 输入和 Profile 数值不变。

## 预期

右脚 1916 的 residual/output 相对恢复基线小幅下降，且 sole/ankle 首帧跳变不应达到完整步长实验的增幅；其它事件、穿透、左右脚和腿部覆盖保持不变。

## 回放

待固定 Trace 回放完成后补充实际 Samples、关键帧和完整诊断对比。若跳变或穿透回归，保留样本并精确回退本轮代码。
