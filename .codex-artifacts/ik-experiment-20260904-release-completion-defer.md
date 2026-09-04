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

待固定 Trace 回放完成后补充实际 Samples 与完整对比。若事件时序、脚位跳变或穿透变坏，保留样本并精确回退本轮代码，恢复半步交接候选。
