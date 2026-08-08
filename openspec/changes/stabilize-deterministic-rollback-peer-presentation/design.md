# Design: DeterministicRollback 预测边界与 Action 分支重基

## Context

DeterministicRollback 的正式链路是：

```text
GameplayTickSystem
  -> SimulationSessionHost
  -> Rollback Source Ingress
  -> Rollback Schedule
  -> Fixed Evaluate / WorldSolve / Finalize
  -> Output Disposition
  -> Fixed Unity Presentation Adapter
  -> Character Presentation Runtime
```

Relay 只转发输入、排序 canonical bundle、推进 confirmation 并路由 hash/snapshot。双端同 Tick world/KCC hash 一致，说明 Fixed Gameplay 状态可以收敛。当前故障分布在三个边界：Schedule 没有独立 prediction horizon；Fixed Unity Presentation Adapter 没有 Action rollback branch revision；Presentation Fact 把 Body branch revision 错当成 Pose discontinuity。

## Decision 1: 分离 Prediction Lead 与 Rollback Depth

`MaximumPredictionLeadTicks` 表示 `CompletedTick - LastCanonicalContiguousTick` 允许达到的最大值；`MaximumRollbackDepthTicks` 表示 late input 或 hash mismatch 需要 restore/replay 时的普通回滚深度边界。

Schedule 做 forward prediction 时只使用前者。Schedule 做 restore/replay、History 容量校验和 deep recovery 判定时继续使用后者。

业务取舍：预测窗口越小，双端看到的远端时间线越接近，但 canonical 暂停时快 Peer 更容易 `NoStep`；回滚深度保持较大值，可以在不扩大可见时间差的前提下保留恢复能力。

## Decision 2: 预测边界属于 Model Schedule

`GameplayTickSystem` 继续拥有本地 fixed accumulator 和 PresentationFrame。Rollback Schedule 读取 model policy 与 canonical frontier，达到领先边界时返回现有 `NoStep` execution plan。Ingress 仍然每次运行，使 canonical 到达后可以恢复 forward step。

不向 `GameplayTickSystem` 写网络校时，不让 Relay 成为 Gameplay clock，不新增另一个 Tick runner。

## Decision 3: 预测边界进入正式身份闭包

新字段进入：

- `DeterministicRollbackModelPolicy.ConfigurationHash`
- DeterministicRollback model semantic identity
- `DeterministicRollbackServerManifest`
- Rollback Product manifest 和 Build adapter
- Client/Relay handshake compatibility
- Rollback diagnostics

任一 Peer 或 Relay 使用不同值时，Session MUST拒绝 Active，不运行时协商，不使用默认或 fallback 值。

## Decision 4: Rollback Action terminal 只在 confirmed 后提交

`SelectProducer` 和 `SampleProducer` 继续属于 predictable/reversible output，使 Action 起手、Slot 选择与表现时间不等待 confirmation。`CompleteProducer` 和 `ReleaseProducer` 改为 confirmed-only，因为它们会推动 lifecycle terminal、retirement permission 和 physical source release，不能再被当作普通可逆状态。

业务取舍：选定方案会让动作结束表现最多延后 `ConfirmationDelayTicks`，当前配置为 4 Tick；动作开始和 Sample 仍然即时。另一个有价值的方案是让 terminal 也可逆，可以更早结束画面，但必须同时撤销 Slot usage、backend release request、source release completion 和 continuity identity，将不可逆边界推迟到 Animancer 资源后端。本 change 选择 confirmed-only terminal，保留单一不可逆边界。

## Decision 5: 回滚撤销不再等于业务 Release

Fixed Unity Presentation Adapter 在一个 outer transaction 内先完成 EventId history 的 keep/replace/cancel，再按 AnimationChannel 与 PlaybackId/generation建立当前有效的 selection、sample与terminal结果。replay过程的中间命令不直接驱动Action Runtime。

未确认的动画 Select/Sample撤销只进入Adapter的deferred retirement表。最终分支恢复同一generation时，Adapter移除对应deferred记录并通过既有Publish/Replace命令恢复当前结果；分支没有恢复时，也必须等该命令所属Tick进入confirmed horizon，才允许调用既有Retire清理。这样`ActionPlaybackCommandInbox`的“已消费command -> 合成Release”只发生在确认后的正式清理边界。

Action Runtime仍然只在现有PresentationFrame Evaluate Barrier前事务中消费Adapter提交的最终有效命令，继续由已有lifecycle、sample history、Slot usage、source continuity与release ownership事务共同Seal。不存在第二套绕过Runtime的回滚路径。

## Decision 6: Confirmed terminal 继续保持单调

Confirmed terminal 在 Runtime 提交成功后，Adapter再裁剪该generation的sample/terminal rollback history，并保留generation级confirmed terminal tombstone。此时同generation的Select/Sample不再可恢复；如果出现，说明已确认Output history被改写，Runtime MUST报告结构化错误并进入正式Faulted。

`PruneConfirmed` 只负责裁剪 Adapter 回滚历史，不再承担修正 Runtime lifecycle 的职责。

## Decision 7: Body 分支和 Action 分支保持各自单一 owner

Body branch replacement 继续由 `CharacterBodyPresentationRuntime` 按 Tick 区间和显式 stream reset 处理。Action branch revision 只更新有限 Action lifecycle 与相关资源所有权，不重置 Body target、PoseStateMachine、整 Rig 骨骼或 Presentation clock。

Body Runtime 的 branch sequence 表达 committed history 版本，Presentation Fact 的 Pose discontinuity generation 只表达必须重建 Locomotion连续状态的硬边界。普通 `CommittedBranchReplacement` 清理并重建对应 Body/Intent history，但保持 Pose discontinuity generation、Presentation clock、PoseStateMachine、Sequence Player、Root Orientation Warp、Foot contact/anchor 和 pelvis连续状态；Foot Placement与Motion Matching只接收新branch sequence完成重定向。`Initialization`与显式`SelectedStreamReset`才同时推进两种身份并走既有硬重置。

业务取舍：保留Locomotion连续状态可以避免远端角色每次迟到输入都重播Walk/Run起始帧，代价是Foot anchor会跨普通Body修订继续存在；anchor本来就绑定实际surface局部空间，visible root再通过同一follower有界收敛，因此这正是屏幕连续性的正式语义。每次branch replacement都清空整套Pose虽然实现更简单，但会把网络历史修订直接泄漏成骨骼闪断，不采用。

## Decision 8: 诊断必须分开节奏与生命周期

诊断分别暴露：

- `predictionLead`：当前 completed Tick 相对 canonical frontier 的领先量。
- `peerExplicitFrontierGap`：Relay 视角两个 Peer 显式输入前沿差。
- `pacedNoStep`：因 prediction lead 达到上限而停止 forward step 的次数。
- `predictedFallback`：目标 Tick 缺少远端显式输入的次数。
- `DroppedLocalLogicTicks`：GameplayTickSystem 本地 catch-up 上限造成的丢 Tick。

这些数值 MUST不合并为一个 sync 状态，否则无法区分网络输入、本地推进和动画生命周期故障。

## Failure Model

- Policy、Manifest 或 Handshake 的 `MaximumPredictionLeadTicks` 不一致：Session 拒绝 Active。
- Prediction lead 达到上限：Schedule 返回 `NoStep`，继续收取输入与 canonical，不新增 predicted history。
- Late input 位于 history 内：继续使用原有 restore/replay，只将 outer transaction 合并后的最终 Action 分支送入 Presentation。
- Committed Body history 被替换：重基 Body/Intent branch并保持 Pose discontinuity generation，不重置Locomotion或Foot Placement连续状态。
- 显式 Selected Stream Reset：推进 Pose discontinuity generation并执行既有Pose与Foot Placement硬重置。
- 未确认 Select/Sample 被撤销：重基预测分支，不合成 Release。
- Confirmed terminal 后出现同 generation Sample：拒绝提交并进入 Faulted。
- 任何修正 MUST不直接写 WorldState、Transform、Physical Bones 或绕过 Output Disposition。
