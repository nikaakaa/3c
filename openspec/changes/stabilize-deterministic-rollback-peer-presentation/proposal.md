# Change: 稳定 DeterministicRollback 双端预测与表现分支

## Why

当前 DeterministicRollback 双端包含三个相互独立的故障。

第一个故障是 Peer 预测推进没有独立于回滚深度的边界。两个 Unity Player 使用各自的 fixed accumulator，Rollback Schedule 又把 `MaximumRollbackDepthTicks=90` 同时当作预测领先上限。在 `20260807-170109` 双端运行中，Relay 记录的 `Peer B explicit frontier - Peer A explicit frontier` 从接近 0 漂移到最高 31，后续又回到约 2。同 Tick world/KCC hash 一致且 Relay `invalid=0`、`dropped=0`，说明这是本地推进节奏漂移，不是 Fixed 状态分歧或丢包。时间领先会让快 Peer 持续预测慢 Peer 的远端输入，形成可变方向的位置卡顿和单向不同步。

第二个故障是有限 Action 表现生命周期不具备 rollback branch revision 语义。`FixedUnityPresentationOutputAdapter` 把 Sample 与 terminal 分成独立 state key，并把已提交 Select/Sample/terminal 的回滚撤销统一降低为 `Retire`；`ActionPlaybackCommandInbox.Retire` 在目标已被 PresentationFrame 消费后会生成业务 `Release`。这使“撤销未确认的预测分支”被错误解释为“该 generation 已不可逆结束”。后续最终分支再次提交同 generation Sample 时，Lifecycle Registry 报告 `Action playback Sample follows a terminal command`，表现帧在 Evaluate Barrier 前持续失败。该旧异常发生时 Relay 前沿只相差约 4–5 Tick，因此缩小 prediction lead 不能修复骨骼异常。

第三个故障是 Body branch sequence 被直接当成 Presentation Fact 的 Pose discontinuity generation。Committed Body history 因迟到输入发生普通分支替换时，`CharacterPoseStateMachineRuntime` 会把 generation 变化解释为 owner generation 更换并重置 Locomotion StateMachine、Sequence Player 和 transition clock；Foot Placement 也会清空 contact、surface anchor 与 pelvis 连续状态。远端角色比本地 owner 更频繁发生这种分支替换，因此 Walk/Run 会反复从源头重启，而有限 Action 仍沿独立 committed sample lifecycle 连续播放。

## What Changes

- 在 DeterministicRollback policy 中增加独立 `MaximumPredictionLeadTicks`，不再使用 `MaximumRollbackDepthTicks` 决定 forward prediction horizon。
- Rollback Schedule 达到预测领先上限时使用现有 `NoStep`；Ingress 继续收取 explicit、canonical 和 confirmation，Relay 仍然不执行 Gameplay。
- 将新预测窗口纳入 policy hash、model identity、Server Manifest、Build 产物和握手一致性校验。
- Rollback Output Disposition 将 `CompleteProducer` 和 `ReleaseProducer` 归为 confirmed-only；Select 与 Sample 仍可预测提交，保留动作起手和播放时间响应。
- 把 Fixed Rollback 的动画提交从“按独立 state key 立即 Retire”收口为“按 outer transaction 先合并最终 Action 分支，再延迟撤销”。回滚撤销未确认 Select/Sample MUST不再合成业务 Release。
- Action Playback Runtime 继续只通过现有 PresentationFrame 预分配事务消费最终有效命令；Adapter保存未确认动画撤销，分支恢复时取消撤销，只有撤销所属 Tick confirmed 后才进入既有 Retire/Release 清理。
- Confirmed terminal 一旦提交仍然保持单调；其后同 generation Sample 是真实不变量违反，继续进入正式 Faulted 路径。
- 将 Body branch sequence 与 Pose discontinuity generation 分离。Committed branch replacement 只重基 Body/Intent history并重定向 follower、Foot Placement 与 Motion Matching trajectory；只有 Initialization 和显式 Selected Stream Reset 才推进 Pose discontinuity generation并重置 Locomotion连续状态。
- 将 prediction lead、paced NoStep、Peer explicit frontier gap、predicted fallback 和本地 dropped logic tick 分开诊断。

## Impact

这是 DeterministicRollback 模型调度和 Fixed Rollback -> Character Presentation 提交边界的架构修正。它不改变 Fixed Program、KCC、World Solver、Body Presentation Clock、Pose Graph 或 Relay-only Server 职责。

本地双端 Demo 建议将 `MaximumPredictionLeadTicks` 配为 8，将可见的方向性时间差限制在约 133ms 内；`MaximumRollbackDepthTicks` 继续保持 90，以保留深度恢复能力。`Complete/Release` 最多延迟当前 `ConfirmationDelayTicks=4`，即60Hz下约67ms；换来的是不再撤销已触发资源释放的 terminal。

## Current Spec Comparison

- `deterministic-rollback-network-model` 当前把 `MaximumRollbackDepthTicks` 同时作为预测领先上限；本 change 将两者分离。
- `deterministic-rollback-network-model` 当前要求 outer transaction 只提交最终表现净分支，但实现仍会把回滚撤销降低为业务 Release；本 change 补齐这个实现缺口。
- `gameplay-tick-system` 允许一个 LocalLogicTick 对应零个、一个或多个 SimulationTick；本 change 继续由 `GameplayTickSystem` 拥有外层 Tick，只让 Schedule 返回 `NoStep`。
- `character-animation-pipeline` 当前要求 command 由 PresentationFrame 原子消费，但没有定义已消费预测分支的回滚撤销边界。本 change 补充未确认撤销与 confirmed terminal 的生命周期语义。
- `character-presentation-interpolation` 当前要求 Rollback 仅提交 replay 后最终 Body/动画净分支；本 change 不建立 confirmed 表现缓冲，只将不可逆 Action terminal 分类为 confirmed-only。
- `character-presentation-interpolation` 当前要求连续 branch revision 从当前 visible 状态重新定向，但 Body reset sequence 仍会间接重置 Locomotion Pose；本 change 补齐 branch revision 与真正 Pose discontinuity 的边界。
- active `close-deterministic-rollback-character-pipeline` 明确将新网络同步算法列为范围外。本 change 单独拥有预测领先边界和 Action branch revision，不修改该 active change。

## Out of Scope

- 不让 Dedicated Relay Server 执行 Fixed Program、KCC、World Solver 或动画。
- 不新增第三个 Peer、Canonical Host、Client Host 或 LocalLoopback fallback。
- 不使用 Transform teleport、confirmed Body 表现缓冲、骨骼快照恢复或吞掉 Animation Runtime 异常掩盖问题。
- 不修改 Character Program、KCC movement policy、Collision Artifact 或网络输入协议的业务语义。
- 不自动触发 Unity Build、Asset 选中编译或运行时修复配置。
