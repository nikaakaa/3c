# Foot IK 实验记录：输入越界时保留同事件落点换面

候选提交：`3b87221fb`（实验保留输入越界时的同事件落点换面）

输入：`43357ff3cd384e5cba75d2c31175b116`

基线采样包：`Diagnostics/GeneratedFootSampling/20260903-112846-79907153ed7d44158c467b439a5acd3e`

候选采样包：`Diagnostics/GeneratedFootSampling/20260903-122531-dd201bf6c4e84dcbb8c31ee31740db3e`

结果：失败，已回退。

本轮只在 `CaptureNextSwing` 增加一条准入：同一 Landing Event 已有 NextSwingLanding，查询包含 `PredictionInputDistanceExceeded` 且 SurfaceIdentity 改变时，保留旧目标，不写入新的预测目标。

回放与基线按 `sample.sequence + sample.dimension` 对齐，均为 2088 行。

- 基线同事件换面计数：左 153、右 162；候选仍有左 31、右 32，说明规则确实挡住了大量换面。
- 但候选把普通 5cm 输入阈值越界也当成不可见换面。左脚序列 1409 至 1455 等区间保留陈旧目标，目标高度相对基线最大差约 1.80m；右脚序列 1474 至 1494 最大差约 1.98m。
- 与基线相比，最终脚底变化超过 1mm 的行 1225，实际 ankle 变化 1283；最大三维变化约 0.775m。
- 右脚 `1914→1916` 的主跳变仍为约 26.77cm，未改善；左脚 `2067→2069` 约 6.80cm，略差于基线约 6.79cm。
- 还原完整有符号 Plant 间隙后，正侧行 `17→50`，负侧行 `167→136`，正侧最大间隙约 `5.66cm→11.36cm`；这与陈旧 Plant 目标导致的穿透风险一致。
- 离散约束状态由基线 `Landing 447 / Locked 80` 变为候选 `Landing 443 / Locked 84`，没有形成更稳定的 Contact 接管。

失败原因：`PredictionInputDistanceExceeded` 是观测缓存的 5cm 输入累积阈值，不等价于“临近 Contact 的预测不可信”。把它直接当作跨 Surface 冻结条件，会在普通 Swing 期间长期保留旧目标，造成陈旧落点和更大的穿模风险。

结论：不能保留该候选。下一轮只应针对明显异常的大输入距离做隔离，或把“观察结果”和“可见目标换面准入”拆成独立事实；不能冻结所有 128 查询原因。
