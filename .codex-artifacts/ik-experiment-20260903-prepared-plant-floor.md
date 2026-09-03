# Foot IK 实验记录：PreparedPlant floor

候选提交：`25d80a3f2`（实验应用PreparedPlant安全下界）

输入：`43357ff3cd384e5cba75d2c31175b116`

基线采样包：`Diagnostics/GeneratedFootSampling/20260903-085423-ccccf95de07a4a55a4e10b17752df116`

候选采样包：`Diagnostics/GeneratedFootSampling/20260903-093951-f36c4e4e39104b53b08cbd110ea6995f`

候选分析：`Diagnostics/GeneratedFootSampling/FootAnalysis/20260903-093951-f36c4e4e39104b53b08cbd110ea6995f-20260903-094217-fd11a17627c1444181a8d8fb2f111bf5`

结果：失败，执行 revert。

- safety floor 可用且最终 correction 低于 minimum 的帧：89 → 28。
- PreparedPlantActive 下低于 minimum 的帧：65 → 4。
- 发生变化的 61 帧全部是 Swing、PlantTarget owner、PreparedPlantActive。
- 最终 sole 相邻帧最大跳变：16.4cm → 22.6cm。
- 物理 ankle Y 相邻帧最大跳变：12.1cm → 23.0cm。
- 形式诊断的穿透、接触间隙、Contact 跳变和 Plant 跳变计数没有下降，现有诊断因缺少稳定 Swing target correction 证据没有捕获这类新增跳变。

原因：PreparedPlant 的未来落点 floor 被当成了当前 Swing 的硬输出约束，脚在接触前提前追到未来 PlantTarget，造成楼梯级别的抬脚和腿部姿态变化。该 floor 只能作为证据或在明确的接触拥有阶段应用，不能无条件作用于 PreparedPlantActive 的 Swing。

基线 Swing 观察：恢复包的 1032 个连续 Swing 对中，748 个最终 correction 输出超过 2cm，357 个 correction 本身超过 2cm；大位移多数来自动画 OriginalSole 与 correction 叠加。代表帧 Left 2059→2061 的动画 sole 下移 12.9cm、correction 再下移 2.5cm，最终 sole 下移 15.4cm；2067→2069 的 Path 接受与 Contact 状态切换又使 correction 上跳约 7.6cm。现有 Stable Swing 规则因缺少 path-current-target-correction 证据而未计入，这也是新增测试要补的缺口。
