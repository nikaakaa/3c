# Foot IK 实验：ReleaseCompleted 丢弃旧事件可见输出点

候选提交：`a3d1bda14`

输入：`43357ff3cd384e5cba75d2c31175b116`

## 假设

右脚 1914 的 `ReleaseCompleted` 清理了旧接触状态，但 Post-Transition 仍把旧事件的 `PreviousResponseOutputPoint` 带到 1916。1916 是新的 Contact Event，PlantWorldResidual 因此从旧事件高位输出 `2.493512m` 捕获到新目标 `1.97999978m`，首帧保留 `0.513512m` 世界残差并平滑跨过一级台阶。ZZZ 的接触纪元证据是旧位置在新纪元硬覆盖或丢弃，不在新事件上继续混合。

## 唯一变量

只在 `CharacterFootInterpolationRuntime.ApplyPostTransition` 遇到 `ReleaseCompleted` 时清除 `HasPreviousResponseOutputPoint` 与点值；保留已有 CorrectionResponse 标量、Applied Direction、lineage 和 EffectiveCorrection。其它 State、Target、Query、PlantWorldResidual 捕获/衰减、插值速率、Pelvis 和 Profile 不变。这样同Event `Releasing -> Landing` 不经过 `ReleaseCompleted`，仍保留原连续入口。

## 预期

新 Contact Event 不再从旧 Releasing 可见世界位置构造完整残差，减少 1916 的跨级悬空；代价是新事件接管可能产生更大的首帧下降或腿部伸展，必须检查全包最终 Sole/Ankle 跳变、Plant 穿透、Bend/Extension、Pelvis、左右脚以及 Releasing/Swing 稳定性。

## 验证结果

正式回放已完成 1044/1044 帧，采样目录：`Diagnostics/GeneratedFootSampling/20260903-200453-edb0e4cccc8e4ddca9aa1f6a022b71d5`。本轮没有生成新的 Analyzer report，使用新 CSV 与 184 基线逐帧对账。

| 指标 | 候选 | 184 基线 |
|---|---:|---:|
| GroundPath Accepted / Rejected | 1796 / 292 | 1796 / 292 |
| Contact Edge 行 | 114 | 114 |
| LandingCompleted / ReleaseCompleted | 24 / 53 | 24 / 53 |
| Plant 正穿透样本 | 34 | 27 |
| 最大 Plant output distance | 0.338173m | 0.569929m |
| Physical / Pelvis 覆盖 | 2088 / 2088 | 2088 / 2088 |

右脚 1916 的 `PreviousResponseOutputPoint` 已为空，Plant residual 的 Y 从基线 `0.349391m` 降至 `0.015070m`，Plant output distance 从 `0.569929m` 降至 `0.015074m`，说明旧事件残差确实被切断；但最终 sole 最大跳变从 `0.267713m` 放大至 `0.828288m`，最终 ankle 从 `0.265114m` 放大至 `0.705738m`，physical ankle 最大跳变 `0.705117m`。右脚正穿透由 9 行降至 6 行，但左脚与总穿透增加，且主交接变成瞬移。

左脚 2067/2069 与基线一致，说明变量只影响 ReleaseCompleted 后的跨事件接管。

结论：失败。清除旧可见输出点能消掉错误残差，却没有提供连续的接管轨迹，产生更大的最终脚/脚踝跳变。保留本轮采样作为失败证据，精确回退 Runtime 候选，恢复原有 Post-Transition 输出保留行为。
