# Foot IK 实验：FutureLanding 低候选法线支撑度门控

候选提交：`db6f7d85f`

输入：`43357ff3cd384e5cba75d2c31175b116`

## 假设

右脚 1912 的 FutureLanding 命中高面与低面，原距离排序选到高面，已知选中高面支撑度约 `0.600`；上一轮只按低高度替换过宽，Plant 正穿透从 27 行增到 44 行。错误可能不是高度本身，而是把法线较差的侧面/斜面当作可落踏面。只有低候选的法线支撑度高于当前近命中候选时，才值得提前采用。

## 唯一变量

仅对 FutureLanding 多命中结果，在既有有效过滤与距离排序后，允许选择相对首个候选更低、竖直差不超过 Profile `MaximumReachableVerticalEdge` 且 `dot(candidate.normal, supportUp)` 严格更高的候选；比较继续按已排序命中顺序。CurrentContactVerification、GroundPath、目标、插值、Residual、响应、Pelvis 和 Profile 不变。

## 预期

如果低命中是实际踏面而高命中是台阶边/斜面，1912 将提前接近正式 Contact 的平面，同时比无门控低面选择少引入错误候选；如果低命中只是另一层但法线更好，仍可能产生预测提前或穿透，必须看全包数据。

## 验证结果

正式回放完成 1044/1044 帧，采样目录：`Diagnostics/GeneratedFootSampling/20260903-205821-a28529ea986d4ad8b90033a1b626f144`。本轮 Proof 匹配 1044/1044，Analyzer 未生成新报告。

GroundPath `1796/292`、Contact Edge `114`、Landing/Release `24/53`、Plant 正穿透 `27`、最大穿透 `0.083242m`、最大 Plant output distance `0.569929m`、Bend/Extension、Plant verified、Physical/Pelvis 覆盖全部与 184 基线一致。右 1912/1916 的选点、residual 和 output 也完全不变，说明该法线门控在本 Trace 没有选中任何低候选。

结论：无行为变化，不保留。保留采样作为“法线优先没有提供可用候选”的证据，回退三个 Runtime 文件，恢复原距离排序。
