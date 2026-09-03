# Foot IK 实验记录：延后一帧衰减 Plant 残差

候选提交：`5729e0444`（实验延后一帧衰减Plant残差）

输入：`43357ff3cd384e5cba75d2c31175b116`

基线采样包：`Diagnostics/GeneratedFootSampling/20260903-085423-ccccf95de07a4a55a4e10b17752df116`

候选采样包：`Diagnostics/GeneratedFootSampling/20260903-101754-3d294790ab504675add74a9285ab61f2`

结果：数据为局部改善、整体恶化，暂待视觉验收。

改动只在 `CharacterFootInterpolationRuntime`：进入 `PlantWorldResidual` 的首帧不衰减残差，下一帧恢复原有 half-life。

- 左脚 `2067→2069` 的 Swing→Landing 修正跳变：`7.57cm → 0.93cm`。
- 右脚 `1914→1916` 的 Swing→Landing 修正跳变：`21.63cm → 38.04cm`。
- 候选右脚该帧的 Plant 输出距离：`0.570m → 0.838m`。
- `plant-penetration-depth > 0` 的采样行：`28 → 31`。
- 发生数值变化的完整采样行：`1122 / 2088`；目标修正列没有变化，变化来自残差传递。
- 视觉输出复核：左脚 `final-sole` 跳变 `6.64cm → 0`，但落地首帧低于目标 `14.15cm → 20.76cm`；右脚 `final-sole` 跳变 `16.41cm → 0`，但落地首帧保留在 `2.4935m`，高于目标 `1.9800m` 约 `51.35cm`。
- `final-effective-correction` 是相对动画原脚的修正量，不能单独作为视觉脚位移；视觉验收以 `resolved/core/final-sole` 和实际 ankle 为准。
- 统一分析器未能执行：当前 full Plan JSON 在 dataset 后保留尾逗号，解析在位置 175 失败；以上指标由前后 CSV 以 `sample.sequence + sample.dimension` 对齐计算。

解释：首帧不衰减确实消除了部分落地切换时的“残差先被改写、输出再跳”的现象，但它把完整 Swing 残差带入 Landing。左脚因此继续穿入目标平面，右脚则把上一帧高度完整保留，落地后悬空约半米；这是用不落地换取不跳变，不能作为稳定方案。

视觉验收：用户需要在同一条固定输入回放上检查左脚落地切换改善是否值得接受右脚新的大跳变。未得到验收结论前不把该候选并入稳定链路；确认失败后执行 `git revert --no-commit 5729e0444` 并提交回退。

ZZZ 对照结论：ZZZ 普通无脚锁 Swing 的连续量是当前动画基准上的局部高度标量，使用上一高度历史、上下行不同速率和 `deltaTime` 追踪；没有证据表明它在普通 Swing→Contact 时保存上一帧最终世界脚位并构造完整向量残差。ZZZ 的 LockFoot 是另一条局部锁脚状态，不能替换项目正式 Contact 的 `PlantWorldResidual`。

下一候选：回退本候选后，只在 Swing 高度合同中恢复有符号的上下修正，保持动画 XZ、Ground Envelope、PlantWorldResidual 和目标选择不变；用同一回放验证“目标低于动画脚”时是否能在 Swing 期间提前向下追踪，减少 Landing 才开始追赶造成的台阶跳变。
