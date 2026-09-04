# Releasing 等待下一 Swing 事件的接触请求

## 假设

右脚 `1914` 已有新的 Swing landing event，但 LockRequest 仍是旧 Contact，`1916` 才出现新 Contact 上升沿。当前 Releasing 在 `1914` 先 `ReleaseCompleted`，下一帧才建立新 Anchor；这可能让旧 Release 输出和新 Contact 之间出现不必要的交接断层。本轮只延后一帧 Release 完成，让已有下一 Swing event 的 Releasing 保持原状态，直到新 LockRequest 到达。

## 改动

- 仅当当前仍持有旧 Contact、LockRequest 仍指向旧事件且下一 Swing event 已接受时，Post-Interpolation 不发布 `ReleaseCompleted`。
- 新 LockRequest 到达或没有可接受下一事件时，沿原 Release 完成路径处理。
- 不改 Foot 查询、Anchor 点、目标、TargetHeight、PlantWorldResidual、响应速率、Pelvis、Solver 或 Profile。

## 预期

右 `1914 → 1916` 应保持旧接触交接语义，减少提前释放导致的首帧残差/腿部抖动；需要检查 Release/Landing 事件计数、右 1916/左 2069 脚位、bend/extension、穿透和 Pelvis 覆盖。

## 回放

固定 Trace `43357ff3cd384e5cba75d2c31175b116` 已完整消费 1044/1044 帧并封口：

- Samples：`3cDemo/Client/3C_Client/Diagnostics/GeneratedFootSampling/20260904-002452-4e250168e7f94dfbba2139ee3f5fb0ce/character-foot-ik%2Ffull.csv`
- capturing/finalizing/analyzing 均为 false，编辑器回到 Edit；Console 只有既有 FinalIK serialization-depth 日志。
- 右 1914 不再发布 `ReleaseCompleted`，右 1916 从 `ContactAcquired(6)` 改为 `NewEventContactAcquired(13)`；右 1926 再次发生新接触，状态链发生重排。
- 右 1916 Plant residual 没有减少：captured/after/output 仍为 `0.5135124/0.349391252/0.5699288`，没有 Plant output 收益。
- 全局 GroundPath 1796/292、Contact Edge 114、Plant positive 27，但 Landing/Release 从 `24/53` 变为 `21/6`，丢失 47 个 Release 事件；右脚跳变回到原基线而不是改善。
- Bend `172.9645°`、Extension `0.999992/1.19071`、Pelvis 753/2088、Physical 2088/2088 保持数值覆盖；左 2069 只有微小漂移。

结论：延后 Release 完成造成生命周期重排，未解决脚位问题，判为失败。保留本样本和记录，精确回退 `2106f0b8e`，恢复 `706d83fd1` 半步行为。
