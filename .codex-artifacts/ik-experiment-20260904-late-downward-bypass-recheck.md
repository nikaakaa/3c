# Foot IK 实验：复核下坡事件旁路旧 Swing 残差

候选提交：`4d37fa2ea`

输入：`43357ff3cd384e5cba75d2c31175b116`

## 假设

ZZZ 的事件链在同一落点事件最后两帧向下修订时，会跳过旧标量速率追踪；项目历史候选 `e77a63084` 只因当时没有持久正式 Proof，未完成视觉验收。当前基线已保留“清 SwingResidual”但恢复了 1.8/1.5 速率，可能仍让 Releasing/Landing 边缘保留一小段旧目标尾差。

## 唯一变量

仅在现有 `lateDownwardLandingRevision` 为真且执行 AnimationRelativeScalar 的 Swing 响应时，直接采用当帧目标标量；ContactWorldResidual、Release、目标、查询、Pelvis、Profile 和其它响应帧不变。

## 预期

同事件向下修订的最后两帧更快贴近新落点，减少 Releasing/Landing 局部弯曲抖动和残差尾差；右 1916 的跨事件 Contact 残差不应改变。代价是部分位移集中到事件帧，需检查全包最终跳变、Plant 穿透、Bend/Extension 和左右脚差异。

## 验证结果

正式回放完成 1044/1044 帧，采样目录：`Diagnostics/GeneratedFootSampling/20260903-212714-fe39d218df664b00aa71861e8ad49be3`。本轮 Analyzer 未生成新的 diagnoses report，使用新 CSV 与 184 基线逐帧对账。

3 个 `lateDownwardLandingRevision` 事件的位置与基线一致，但旁路将响应速率从基线 `1.5m/s` 变为 `0`。LandingCompleted 从 `24` 增至 `27`，左脚 final correction 最大跳变从 `0.158651m` 增至 `0.195671m`；Plant 正穿透、最大穿透、最大 Plant output distance、最终 sole/ankle 主跳变和右 1916 均未改善。

结论：失败。ZZZ 的事件旁路不能直接替换本项目的 AnimationRelativeScalar 响应链。保留采样和结论，精确回退到 184 基线行为。
