# Foot IK 实验：FutureLanding 台阶候选优先低踏面

候选提交：待提交

输入：`43357ff3cd384e5cba75d2c31175b116`

## 假设

右脚序列 1912 的 FutureLanding 球形查询同时命中高踏面与低踏面，但旧排序按射线距离选择较近的高踏面 `2.16m`；序列 1916 正式 Contact 才验证低踏面 `1.98m`，导致 Contact 首帧接管约一级高度。低踏面候选的竖直差为 `0.18m`，在 Profile `MaximumReachableVerticalEdge=0.3m` 内。

## 唯一变量

仅对 FutureLanding 的多候选结果，在既有有效过滤和排序后，允许选择相对首个候选更低且竖直差不超过 `MaximumReachableVerticalEdge` 的候选。CurrentContactVerification、GroundPath、目标选择、Interpolation、Response、Pelvis 和所有阈值不改。

## 预期

预测落点更早采用低踏面，减小 1912→1916 的 Contact 世界残差与落地后平滑跨级；代价是部分多命中查询可能提前采用低面，必须检查全包穿透、Plant 距离、最终脚底/ankle 跳变、腿部弯曲伸直、Pelvis 与左右脚差异。

## 验证状态

提交后使用同一固定 Trace 正式回放并等待完整 Finalizer/Analyzer/Publisher。未完成前不判定视觉效果；失败保留本次采样证据并精确回退。
