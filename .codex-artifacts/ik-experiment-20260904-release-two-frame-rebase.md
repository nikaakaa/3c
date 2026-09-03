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

固定 Trace `43357ff3cd384e5cba75d2c31175b116` 已完整消费 1044/1044 帧并封口：

- Samples：`3cDemo/Client/3C_Client/Diagnostics/GeneratedFootSampling/20260903-232536-cabbdfa869504328a9447adaecd7afa6/character-foot-ik%2Ffull.csv`
- capturing/finalizing/analyzing 均为 false，编辑器回到 Edit；Console 只有既有 FinalIK serialization-depth 日志。
- rows=2088，GroundPath 1796/292，Contact Edge 114，Landing/Release 24/53，Plant positive 27，最大穿透 0.083242m，Verified 527，均与恢复基线和半步候选一致。
- 右 1916 首帧与半步候选一致：captured residual Y `0.5010123m`，after-decay `0.3408863m`，Plant output `0.5647549m`，target/solved extension `0.7801122/0.7801122`，solved bend `77.54771°`。
- 右 1918 实际发生第二次半步：captured residual Y `0.3283863m`，after-decay `0.2234324m`，Plant output `0.379184932m`；相比半步候选再次减少 `0.0125m`。
- 右 1918 第二帧 final sole/ankle/physical 跳变为 `0.185757/0.183720/0.183720m`，恢复基线为 `0.182153/0.180075/0.180075m`，新增约 `3.6mm`；左 2069 与基线一致。
- 全局 Plant 最大 output 仍为 `0.564755m`，与半步候选相同；Bend `172.9645°`、Extension `0.999992/1.19071`、Pelvis 753/2088、Physical 2088/2088 不变。

结论：两帧分摊条件和第二次命中均成立，但没有比半步候选提供额外最大收益，并引入第二帧小跳变，判为失败。保留本样本和记录，精确回退 `de58a6d5e`，恢复 `706d83fd1` 半步行为。
