# Foot IK 实验记录：静止输入使用零运动预测

候选提交：`699d036bd`

输入：`43357ff3cd384e5cba75d2c31175b116`

基线采样包：`Diagnostics/GeneratedFootSampling/20260903-112846-79907153ed7d44158c467b439a5acd3e`

候选采样包：`Diagnostics/GeneratedFootSampling/20260903-131236-17eb1a81169e4bc38e0ca0d1e877f557`

## 改动

当运动时间线暂不可用时，只有当前速度和连续速度都为零才允许共享预测运动运行时继续工作。KCC 仍使用正式 Future Body Translation 源生成零位移轨迹；没有速度输入时不引入第二套预测或几何兜底。

NextLanding 只有在该正式轨迹实际生成后才允许执行 Ground Query，轨迹生成失败仍保持原拒绝路径。

## 对账

- 每个方向 1044 行，按 `sample.sequence + sample.dimension` 对齐。
- `prediction-motion-available=true`：左右均由 `944/1044` 提升到 `1042/1044`。
- 左脚 2059、2061、2063、2065、2067：由 `MotionTimelineUnavailable` 和 GroundPath 拒绝，变为正式 NextLanding、GroundPath Accepted，落点为 `1.62m`、surface `-90170`。
- 左脚 2059→2061 最终脚底跳变由 `15.379cm` 降为 `9.879cm`；同段 goal 跳变由 `12.107cm` 降为 `6.607cm`，physical ankle 跳变由 `12.107cm` 降为 `6.607cm`。
- 左脚全段最终脚底最大跳变由 `15.379cm` 降为 `13.616cm`，Plant 有符号目标差的正侧最大值由 `14.145cm` 降为 `8.324cm`。
- 左脚 Contact 首帧 2069 的 Plant 残差由 `-14.145cm` 降为 `-6.661cm`，最终脚底由 `1.47855m` 提升到 `1.55339m`。
- 左脚 `foot reject=4` 的行数由 6 降为 1，GroundPath 拒绝行数由 167 降为 162；右脚对应由 5 降为 1、134 降为 130。
- 右脚 1914→1916 最终脚底仍为 `16.412cm`，physical ankle 仍为 `11.737cm`；右侧主 Contact 首帧没有变化。
- 左右 `solved-bend-degrees` 的全段最大值和 P95 没有增加，主弯曲/伸直跳变未由本候选引入。

## 结论

候选有效，保留。它修复的是左脚由于时间线缺失而退回 CurrentSupport 的输入链路；没有触及右脚 Contact 首帧的主跳变。下一轮应直接分析并实验 Contact 交接中的旧可见脚位、Verified Anchor 与 PlantWorldResidual 的接管顺序，不再扩大预测换面规则。
