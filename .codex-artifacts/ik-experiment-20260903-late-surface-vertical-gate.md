# Foot IK 实验记录：异常换面按竖直差保留

候选提交：`0cb7a6aab`、`0e7726466`

输入：`43357ff3cd384e5cba75d2c31175b116`

基线采样包：`Diagnostics/GeneratedFootSampling/20260903-112846-79907153ed7d44158c467b439a5acd3e`

候选采样包：`Diagnostics/GeneratedFootSampling/20260903-124129-0e8e3dfcf297486abe15e1d59e30f573`

结果：无效，已回退。

本轮先以 1m 查询输入距离和三维落点差做门控，未命中目标帧；随后改为沿 `CandidateComponentUp` 的高度差不超过现有 `TargetHeightForceRefreshDistance`，命中右脚序列 1912 的 18cm 跨面预测。

完整回放按 `sample.sequence + sample.dimension` 对齐，均为 2088 行。

- 右脚 1912 的 `NextSwingLanding` 从基线 `2.16m` 保持为前一目标 `1.98m`，1914 仍保持该目标，说明门控实际执行。
- 目标高度变化共 16 行（左 7、右 9），没有改写普通输入越界换面；候选最终脚底变化 54 行，最大三维差约 `1.92cm`，实际 ankle 的最大差也约 `1.92cm`。
- 关键右脚 `1914→1916` 最终脚底跳变仍约 `26.77cm`，ankle 跳变仍约 `15.19cm`；左脚 `2067→2069` 仍约 `6.79cm` 与 `6.43cm`。因此“踩不到后平滑过去”的主跳变没有被触及。
- Plant 有符号间隙仍为 `17` 个正侧、`167` 个负侧、`343` 个零值；约束状态仍为 `Landing 447 / Locked 80`，与基线一致。
- 腿部只产生小范围伴随变化：`solved-bend-degrees` 最大约 `2.33°`，不是主弯曲/伸直来源。

关键归因：右脚 1914→1916 在 Contact 首帧发生的是动画 `OriginalSole` 约 `(+56cm, -38cm, -35cm)` 的正式事件切换；随后 `CharacterFootStateTargetResolver.ResolveContactPlant` 改用 Verified Anchor，`CharacterFootInterpolationRuntime.EvaluatePlant` 以新旧可见输出捕获 `PlantWorldResidual`，再由 Landing 的锁权重逐步接管。NextSwingLanding 是否在 1912 换面不会消除这条 Contact 首帧链路。

结论：该候选不能作为修复，回退后继续实验 Contact 交接本身；不再扩大 NextSwingLanding 冻结规则。
