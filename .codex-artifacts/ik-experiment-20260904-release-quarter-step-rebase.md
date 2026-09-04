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

固定 Trace `43357ff3cd384e5cba75d2c31175b116` 已完整消费 1044/1044 帧并封口：

- Samples：`3cDemo/Client/3C_Client/Diagnostics/GeneratedFootSampling/20260904-001219-e234d749244b4abb96e480249dad93b7/character-foot-ik%2Ffull.csv`
- capturing/finalizing/analyzing 均为 false，编辑器回到 Edit；Console 只有既有 FinalIK serialization-depth 日志。
- GroundPath 1796/292，Contact Edge 114，Landing/Release 24/53，Plant positive 27，最大穿透 0.083242m，Bend 172.9645°，Extension 0.999992/1.19071，Pelvis 753/2088，Physical 2088/2088，均无结构性回归。
- 右 1916 实际剥离 `0.00625000055m`：captured residual Y `0.5135124 → 0.507262468m`，after-decay `0.349391252 → 0.345138848m`，Plant output `0.5699288 → 0.567331851m`。
- 右 1916 final sole/ankle/physical 跳变 `0.270341/0.155208/0.155208m`，恢复基线 `0.267713/0.151899/0.151899m`；仍增加约 `2.6/3.3/3.3mm`。
- 右 1918 只走原有衰减，未发生第二次剥离；左 2069 与基线一致。

结论：四分之一步长确实命中且无穿透或大跳变，但 Plant output 只改善 2.6mm，收益约为半步的一半，仍有可见交接代价，判为失败。保留本样本和记录，精确回退 `73cda14c8`，恢复 `706d83fd1` 半步行为。
