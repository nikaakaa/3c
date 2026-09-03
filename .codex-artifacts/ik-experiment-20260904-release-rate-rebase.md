# ReleaseCompleted 后按现有下降速率剥离一步旧响应

## 假设

固定 Trace `43357ff3cd384e5cba75d2c31175b116` 中，ReleaseCompleted 后紧接新 Contact 时，旧 AnimationRelativeScalar 仍完整进入 PlantWorldResidual。整段剥离会把跳变推迟到脚底输出，因此本轮只剥离 Profile 的 `CorrectionResponseDecreaseSpeed * DeltaSeconds` 一步，保留其余世界位置连续性。

## 改动

- 仅在 `ReleaseCompleted → Plant` 的直接交接设置一次待处理标记。
- 下一次 Plant 捕获连续性残差前，沿旧响应方向扣除一个现有下降速率步长。
- 普通 Swing 先发生时清除标记；查询、目标、Residual 衰减、状态转换和 Profile 不变。

## 预期

右脚 1916 一类的旧 Release 残差应小幅下降，避免整段重基造成的新跳变；Landing/Release 事件集合和非交接帧保持不变。

## 回放

固定 Trace `43357ff3cd384e5cba75d2c31175b116` 已完整消费 1044/1044 帧并封口：

- Samples：`3cDemo/Client/3C_Client/Diagnostics/GeneratedFootSampling/20260903-225853-f9566fdd753c4c1cab5e5d473bd59ec0/character-foot-ik%2Ffull.csv`
- capturing/finalizing/analyzing 均为 false，未新增编译或诊断错误；编辑器保持 Edit。
- GroundPath 1796/292，Contact Edge 114，Landing/Release 24/53，Plant positive 27，Verified 527，均与恢复基线一致。
- Plant 最大穿透 0.083242m 不变；Plant 最大 output distance `0.569929m → 0.559662m`。
- Bend `172.9645°`、solved/target extension `0.999992/1.19071`、Pelvis 753/2088、Physical 2088/2088 均不变。

实际只命中右脚 `ReleaseCompleted seq1914 → Plant seq1916`：下降速率 1.5m/s、固定 dt 1/60，剥离步长 0.025m。右 seq1916 的 captured residual Y `0.5135124 → 0.488512278`，after-decay Y `0.349391252 → 0.3323813`，Plant output `0.569929m → 0.559662m`。影响沿 residual 衰减到 seq1938 后消失。

代价是右 final correction 最大值 `0.499075 → 0.491942`，右 final sole 最大跳变 `0.267713 → 0.278466m`，右 seq1916 ankle/physical 跳变 `0.151899 → 0.165396m`；左 seq2069、全局事件集合、穿透和覆盖不变。结论是数据上局部减少旧响应，但交接首帧脚底跳变略增，暂保留候选供视觉验收，不宣称整体通过。
