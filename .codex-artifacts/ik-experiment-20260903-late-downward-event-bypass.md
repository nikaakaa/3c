# Foot IK 实验记录：下坡落点事件旁路旧 Swing 残差

候选提交：`e77a63084`

输入：`43357ff3cd384e5cba75d2c31175b116`

基线采样包：`Diagnostics/GeneratedFootSampling/20260903-131236-17eb1a81169e4bc38e0ca0d1e877f557`

候选采样包：`Diagnostics/GeneratedFootSampling/20260903-152218-5a84f1f9484e4d13ad0877736b03b2e4`

Replay Proof：`Temp/CharacterInputReplayProofs/diagnostic-v1/43357ff3cd384e5cba75d2c31175b116/20260903-232340-710-99f7ccc39ba94224a72c20350dd6745d.json`，`matched:1044`。

## 改动

同一 Landing Event 的 `LandingPointChanged` 在最后两帧下移时，清除旧 `SwingResidual`，并让 AnimationRelativeScalar 直接采用新目标，验证 ZZZ 的事件型旁路行为。GroundPath、落点、Surface、Contact 目标和正式配置不变。

## 数据

- 基线与候选均为 2088 行、1026 列，输入身份一致。
- 四个最后两帧边缘样本（右 1164、2058；左 1199、1559）均由旧输出高于新目标约 14.9–18.8cm，变为直接贴近新目标。
- 四个样本的 `swing-residual-after-decay.y` 均变为 0，响应速率由 `1.5` 旁路为直接采用。
- 四个事件帧的物理 ankle 同步下移约 14.9–18.8cm；后续 1–3 帧的 Plant 输出距离下降。
- 左脚 Plant 输出距离最大值 `16.11cm → 15.46cm`，右脚保持 `56.99cm`；穿透行数和最大穿透均不变（左 `18`、右 `9`）。
- 最终脚底全段最大跳变不变；大于 2cm 的跳变左 `825 → 820`、右 `833 → 832`。事件帧本身的下落更集中，需视觉确认是否可接受。
- 左右腿最大弯曲没有增加；右脚 P95 约增加 `0.26°`，事件帧弯曲因脚不再悬空而下降约 `17–50°`。
- 正式诊断分析仍被当前 Plan JSON 的尾逗号拒绝（Position 175），上述为原始 CSV 对账，不冒充正式评分。

## 结论

代码行为符合预期的事件旁路：没有再把旧世界残差拖过低一级踏面。数据未发现穿透回归，但它将一部分位移提前到事件帧，视觉上是否优于原来的平滑跨台阶，等待人工验收；未验收前不继续叠加下一项参数实验。
