# Foot IK 实验：Releasing 不消费 FutureLanding 预测

候选提交：待提交

输入：`43357ff3cd384e5cba75d2c31175b116`

## 假设

右脚 1912–1914 的 FutureLanding 预测事件是 `9009312538774855365`、高踏面 `2.16m`；1916 实际 CurrentContact 却变成另一事件 `8196565222736661925`、低踏面 `1.98m`。Releasing 仍消费预测目标，使 1914 的可见输出保持高位，Contact 首帧再用旧高位捕获世界残差。现有 StateTarget 合同定义 Releasing 应回到原始动画 Swing 目标，因此不应继续把 FutureLanding 当成 Releasing 的可见目标。

## 唯一变量

`PredictFootPair` 只在当前离散状态为 `Releasing` 时禁止 FutureLanding 预测；`plantVerificationRequired` 仍优先执行正式 CurrentContactVerification，Swing、Landing、Locked 和 UnlockedSupport 不变。Releasing 的目标、Contact、GroundPath、PlantWorldResidual、CorrectionResponse、Pelvis 和 Profile 不变。

## 预期

1914 Releasing 将不再保持预测高踏面，原始动画脚下降后，1916 新 Contact 捕获的旧输出高度更低，从而减少一级悬空与跨级残差。代价是 Releasing 期间失去提前抬脚/抬高保护，其他脚步可能更早接近地面，必须检查穿透、最终跳变、Bend/Extension、Pelvis 和左右脚差异。

## 验证状态

提交后用同一固定 Trace 正式回放并逐帧对账；不做视觉 Pass。若失败，保留采样和结论，精确回退 Runtime 候选并保留本记录。
