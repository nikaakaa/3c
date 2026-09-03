# Foot IK 实验：FutureLanding 低候选法线支撑度门控

候选提交：待提交

输入：`43357ff3cd384e5cba75d2c31175b116`

## 假设

右脚 1912 的 FutureLanding 命中高面与低面，原距离排序选到高面，已知选中高面支撑度约 `0.600`；上一轮只按低高度替换过宽，Plant 正穿透从 27 行增到 44 行。错误可能不是高度本身，而是把法线较差的侧面/斜面当作可落踏面。只有低候选的法线支撑度高于当前近命中候选时，才值得提前采用。

## 唯一变量

仅对 FutureLanding 多命中结果，在既有有效过滤与距离排序后，允许选择相对首个候选更低、竖直差不超过 Profile `MaximumReachableVerticalEdge` 且 `dot(candidate.normal, supportUp)` 严格更高的候选；比较继续按已排序命中顺序。CurrentContactVerification、GroundPath、目标、插值、Residual、响应、Pelvis 和 Profile 不变。

## 预期

如果低命中是实际踏面而高命中是台阶边/斜面，1912 将提前接近正式 Contact 的平面，同时比无门控低面选择少引入错误候选；如果低命中只是另一层但法线更好，仍可能产生预测提前或穿透，必须看全包数据。

## 验证状态

提交后用同一固定 Trace 正式回放并逐帧对账；不做视觉 Pass。若失败，保留采样和结论，精确回退 Runtime 候选并保留本记录。
