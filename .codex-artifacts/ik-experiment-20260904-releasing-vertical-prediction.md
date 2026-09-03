# Foot IK 实验：Releasing 不带入预测竖直抬脚

候选提交：待提交

输入：`43357ff3cd384e5cba75d2c31175b116`

## 假设

右脚 1912–1914 的 FutureLanding 记录仍是高踏面预测，Releasing 的可见修正为纯竖直 `+0.1247m`；1916 实际 CurrentContact 换成另一低踏面后，上一 Releasing 输出被完整带入 PlantWorldResidual。事件分类显示这是 Releasing 后新 Contact 的异常，不应因此改变 FutureLanding 记录或事件上下文。

## 唯一变量

只在 `ResolveReleaseTarget` 且旧 Contact 已释放、FutureLanding Swing 已接受时，把预测 `swingCorrection` 投影到 ComponentUp 的切平面；当前样例纯竖直预测因此不再抬脚，水平分量保持。FutureLanding 查询/记录、GroundPath、ContactVerification、PlantWorldResidual、CorrectionResponse、Transition、Pelvis 和 Profile 不变。

## 预期

1914 Releasing 输出更接近动画基准，1916 新 Contact 捕获的旧高位残差变小，减少下坡跨级悬空；相比禁用 FutureLanding，事件记录与 Landing/Release 生命周期应保持不变。代价是 Releasing 失去预测竖直保护，需检查全包 Plant 穿透、最终跳变、Bend/Extension 与左右脚差异。

## 验证状态

提交后用同一固定 Trace 正式回放并逐帧对账；不做视觉 Pass。若失败，保留采样和结论，精确回退 Runtime 候选并保留本记录。
