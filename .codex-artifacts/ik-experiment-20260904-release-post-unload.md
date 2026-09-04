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

固定 Trace `43357ff3cd384e5cba75d2c31175b116` 已完整消费 1044/1044 帧并封口：

- Samples：`3cDemo/Client/3C_Client/Diagnostics/GeneratedFootSampling/20260904-004344-1b5fa39a7d864a34a44c7156f41deb61/character-foot-ik%2Ffull.csv`
- capturing/finalizing/analyzing 均为 false，编辑器保持 Edit；Console 只有既有 FinalIK serialization-depth 日志。
- 右 1914 Post-Transition 条件命中，旧响应标量 `0.110955238` 被从 EffectiveCorrection 和 PreviousResponseOutputPoint 卸掉；右 1916 captured/after/output 为 `0.402557135/0.273897856/0.527033865m`，相比半步候选 `0.5010123/0.3408863/0.5647549m`。
- 右 1916 target/solved extension `0.8492457/0.8492457`，bend `63.80986°`；sole/ankle/physical 首帧跳变 `0.247567/0.126515/0.126515m`，局部确实改善。
- 但该逻辑对所有 ReleaseCompleted 生效：全局 final sole/ankle/physical 最大跳变达到 `0.388623/0.388555/0.388555m`（左 1313→1315），右另有约 `0.381m` 跳变；Plant positive `27→32`，左 2069 residual/output 和跳变也变差。
- GroundPath 1796/292，Contact Edge 114，Landing/Release 24/53，Bend 172.9645°，Extension 0.999992/1.19071，Pelvis 753/2088，Physical 2088/2088；覆盖未缺失，但存在局部和全局回归。

结论：局部右 1916 有收益，但无条件提前卸载会污染其它 ReleaseCompleted，造成 0.38m 级跳变和 5 个新增 Plant 穿透事件，判为失败。代码已恢复到半步基线，保留本样本和结论。
