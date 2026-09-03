# Foot IK 实验：Pending Release 不消费 FutureLanding 预测

候选提交：待提交

输入：`43357ff3cd384e5cba75d2c31175b116`

## 假设

预测阶段的离散状态在右 1914 仍是 `Locked`，但锁请求已下降；随后 Post-Interpolation 才发布 `ReleaseCompleted`，1916 才出现新的低 `CurrentContact`。上一轮按 `State == Releasing` 禁止预测没有命中，因为预测发生在状态转换前。真正的 Pending Release 边界是 `Locked && !LockRequest.RequestsLock`。

## 唯一变量

仅在预测阶段满足 `Locked && !RequestsLock` 时禁止 FutureLanding 预测；正式 `CurrentContactVerification` 仍优先，普通 Swing、UnlockedSupport、Landing、Locked 持锁和其它查询/记录不变。PlantWorldResidual、CorrectionResponse、Transition、Pelvis 和 Profile 不变。

## 预期

ReleaseCompleted 前不再把即将失效的 FutureLanding 高踏面作为可见 Releasing 目标，可能降低下一 Contact 的旧高位；代价是释放边界脚步少一帧预测抬脚，需要检查事件计数、Plant 穿透、最终脚底/脚踝跳变、Bend/Extension 和左右脚。

## 验证状态

提交后用同一固定 Trace 正式回放并逐帧对账；不做视觉 Pass。若失败，保留采样和结论，精确回退 Runtime 候选并保留本记录。
