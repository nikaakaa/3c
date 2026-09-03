# Foot IK 实验：FutureLanding 台阶候选优先低踏面

候选提交：`39bc9dec8`

输入：`43357ff3cd384e5cba75d2c31175b116`

## 假设

右脚序列 1912 的 FutureLanding 球形查询同时命中高踏面与低踏面，但旧排序按射线距离选择较近的高踏面 `2.16m`；序列 1916 正式 Contact 才验证低踏面 `1.98m`，导致 Contact 首帧接管约一级高度。低踏面候选的竖直差为 `0.18m`，在 Profile `MaximumReachableVerticalEdge=0.3m` 内。

## 唯一变量

仅对 FutureLanding 的多候选结果，在既有有效过滤和排序后，允许选择相对首个候选更低且竖直差不超过 `MaximumReachableVerticalEdge` 的候选。CurrentContactVerification、GroundPath、目标选择、Interpolation、Response、Pelvis 和所有阈值不改。

## 预期

预测落点更早采用低踏面，减小 1912→1916 的 Contact 世界残差与落地后平滑跨级；代价是部分多命中查询可能提前采用低面，必须检查全包穿透、Plant 距离、最终脚底/ankle 跳变、腿部弯曲伸直、Pelvis 与左右脚差异。

## 验证结果

正式回放已完成 1044/1044 帧，采样目录：`Diagnostics/GeneratedFootSampling/20260903-193655-3f9d8a5521214a73ab9ebe44cdb6b99e`。本轮没有生成新的 Analyzer report，因此以下以新 CSV 和 184 基线逐帧对账。

| 指标 | 候选 | 184 基线 |
|---|---:|---:|
| GroundPath Accepted / Rejected | 1796 / 292 | 1796 / 292 |
| Contact Edge 行 | 114 | 114 |
| LandingCompleted / ReleaseCompleted | 24 / 53 | 24 / 53 |
| Plant 正穿透样本 | 44 | 27 |
| 最大 Plant 穿透 | 0.083232m | 0.083242m |
| 右脚正穿透 | 17 | 9 |
| 右脚最大穿透 | 0.067620m | 0.007593m |
| 右脚最大最终修正跳变 | 0.499072m | 0.499072m |

1912 右脚 FutureLanding 的 selected distance 从 `0.176999` 变为 `0.325`，但 1916 Contact 的目标、Plant output distance 和主跳变均未改变。候选只改变了部分落点身份和事件时序，没有消除一级平滑跨级。

结论：失败。低踏面优先扩大了多命中场景的落点变更，导致穿透显著增加且主问题未改善。保留上述采样作为失败证据，回退 `39bc9dec8` 的三个 Runtime 文件；该文档随回退提交保留。
