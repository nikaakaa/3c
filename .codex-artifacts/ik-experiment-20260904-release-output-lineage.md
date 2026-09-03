# Foot IK 实验：ReleaseCompleted 丢弃旧事件可见输出点

候选提交：待提交

输入：`43357ff3cd384e5cba75d2c31175b116`

## 假设

右脚 1914 的 `ReleaseCompleted` 清理了旧接触状态，但 Post-Transition 仍把旧事件的 `PreviousResponseOutputPoint` 带到 1916。1916 是新的 Contact Event，PlantWorldResidual 因此从旧事件高位输出 `2.493512m` 捕获到新目标 `1.97999978m`，首帧保留 `0.513512m` 世界残差并平滑跨过一级台阶。ZZZ 的接触纪元证据是旧位置在新纪元硬覆盖或丢弃，不在新事件上继续混合。

## 唯一变量

只在 `CharacterFootInterpolationRuntime.ApplyPostTransition` 遇到 `ReleaseCompleted` 时清除 `HasPreviousResponseOutputPoint` 与点值；保留已有 CorrectionResponse 标量、Applied Direction、lineage 和 EffectiveCorrection。其它 State、Target、Query、PlantWorldResidual 捕获/衰减、插值速率、Pelvis 和 Profile 不变。这样同Event `Releasing -> Landing` 不经过 `ReleaseCompleted`，仍保留原连续入口。

## 预期

新 Contact Event 不再从旧 Releasing 可见世界位置构造完整残差，减少 1916 的跨级悬空；代价是新事件接管可能产生更大的首帧下降或腿部伸展，必须检查全包最终 Sole/Ankle 跳变、Plant 穿透、Bend/Extension、Pelvis、左右脚以及 Releasing/Swing 稳定性。

## 验证状态

提交后用同一固定 Trace 正式回放，等待 1044 帧封口和诊断对账；不做视觉 Pass。若失败，保留采样和数据结论，精确回退 Runtime 候选并保留本记录。
