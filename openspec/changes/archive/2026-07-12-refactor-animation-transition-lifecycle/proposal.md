# Change: 重构动画 Transition 生命周期

## Why

当前状态机逻辑切换已经能够停止 source State，并通过 owner handoff 让表现层继续收尾，但动画切换的职责仍分散在 `StateMachineGraphRuntime`、动画贡献 Registry 和 `CharacterPresentationStage`：

- Registry 同时维护 contribution 生命周期、pending handoff、active handoff 和 outgoing retirement，既是播放实例真相，又承担 Transition 状态机职责。
- `CharacterPresentationStage` 以 runtime id 保存临时 blend session；同一状态机在旧 blend 尚未结束时再次切换，会直接 retire 旧 source，无法从当前最终姿态连续接管。
- internal transition、Exit、父 Tree graceful abort、ForceStop 最终都可能退化成含义不清的 owner release，表现层无法判断应当平滑退出还是立即清理。
- 现有 contribution crossfade 只能混合仍可描述为 source/target contribution 的结果，不能像 UE inertialization 一样在 source 逻辑和 source 播放实例均已结束后，基于最终输出姿态及速度平滑接入新动画。

这会在攻击、闪避、移动状态连续抢占时产生一帧旧姿态、默认姿态或 source 突然消失的跳变。问题不是单个动画配置，而是 Transition 没有独立、可重入、可观测的生命周期。

## What Changes

- 在 `StateMachineGraph` Transition edge 上内联序列化正式 `AnimationTransitionDefinition`，显式选择 `Immediate`、`ContributionCrossFade` 或 `Inertialization`，并保存 duration 与 curve；ConditionRuleGraph 仍只负责 Bool 条件。
- 新增来源无关的 `CharacterAnimationTransitionRuntime`，以稳定 transition instance identity 管理 `Requested -> WaitingTarget -> Capturing -> Running -> Completed -> Retired`，并记录 `Superseded` 终止原因。
- StateMachine runtime 只发布逻辑切换及动画 Transition request，不直接混合；动画贡献 Registry 只维护 playback、contribution、owner membership 和完成/释放，不再保存 pending/active handoff 或 blend elapsed。
- `CharacterPresentationStage` 在同一个表现帧批次中先接收 target ready 与 contribution snapshot，再推进 Transition runtime、LayerRuntime 和最终 adapter，避免 source release 与 target 首帧之间出现空计划。
- `ContributionCrossFade` 使用冻结的 source contribution snapshot 与当前 target contribution plan 做权重混合，不继续 tick source State、Timeline 或 Action。
- `Inertialization` 在 Animancer 最终动画输出之后、后续 IK/程序化姿态之前插入 Unity animation output job，捕获当前最终 local pose 与 pose velocity，并对新 target pose 施加衰减偏移；它不继续求值 source 动画，也不影响逻辑 Transform 或 root motion。
- 同一 StateMachine runtime 新 Transition 到来时，显式 supersede 旧 Transition：crossfade 从当前冻结视觉快照重建 source，inertialization 从当前已经修正后的最终 pose/velocity 重新捕获，禁止 Transition 栈无限叠加。
- 将 internal transition、Transition to Exit、父 Tree graceful stop、ForceStop/deactivate/dispose 映射为明确的 target 或 Empty release request；ForceStop 类原因只允许 `Immediate`，不得使用隐藏默认策略。
- 在 edge Inspector、运行时 debug 和 validator 中暴露策略、实例 identity、生命周期、target ready、进度、supersede/complete/release 原因与惯性姿态摘要。
- 原子迁移现有 Transition edge：原 duration 为 0 的边显式写入 `Immediate`；原非零 duration 的边显式写入 `ContributionCrossFade`；Corin 高频攻击、闪避及其返回移动的指定边显式采用 `Inertialization`，不保留旧字段或兼容读取。
- 删除 Registry 中旧 pending/active owner transition、`Outgoing` 作为 Transition 会话状态、Stage 内旧 `TransitionBlendSession`、含义不清的 `ReleaseOwner` 和直接 retire active source 的重入路径。
- 同步修正 `character-animation-layer-runtime` 中“低优先级 override 不进入计划”的过时要求，使其与当前按实际权重填充剩余层权重的实现和 `character-animation-pipeline` current spec 一致。

## Capabilities

### Modified Capabilities

- `btsmtl-sm-node-authoring`: Transition edge 显式创作动画策略、时长、曲线和 release 语义。
- `character-animation-pipeline`: 将 owner handoff 从 Registry 临时状态重构为正式动画 Transition runtime。
- `character-animation-layer-runtime`: 收敛 Registry 与 LayerRuntime 边界，并修正 override priority fill 规范冲突。
- `character-presentation-interpolation`: 在表现帧推进可重入 Transition，并接入真实输出姿态 inertialization。
- `character-state-interruption-authoring`: 将逻辑 stop barrier 与动画 Transition/Empty release 生命周期明确对接。
- `character-root-motion-curves`: 保证 inertialization job 不读取、生成或修正角色 root motion。

## Impact

- 受影响 runtime：状态机切换发布、动画 contribution Registry、动画层仲裁、表现帧调度、Animancer presenter、角色 host 生命周期与 debug snapshot。
- 受影响 authoring：StateMachine Transition edge Inspector、默认图初始化、资产 validator、agent snapshot/export 与 Corin 状态机资产。
- 这是破坏性迁移：旧 blend duration/curve 字段、Registry handoff/session 数据和隐式 owner release 不再兼容读取。
- 不新增旧动画 SO、一次性 SubTree、fallback clip、隐藏默认 blend 或并行动画播放路径。
- 本 change 不负责动画资源选择、动画曲线重新烘焙、per-bone profile 编辑器、自定义 blend graph、Motion Matching、IK 或网络同步姿态。

## Dependency And Apply Order

当前 active changes `refactor-pipeline-blackboard-owned-scopes` 与 `restore-timeline-treeclip-pipeline-runtime` 会修改 `CharacterGraphContext`、StateMachine scope、Timeline scheduler、Corin RootTree/Timeline 和 `character-animation-pipeline` delta。本 change MUST NOT 与它们并行 apply。

正式顺序固定为：

1. 完成或重新基线化 `refactor-pipeline-blackboard-owned-scopes`。
2. 完成或重新基线化 `restore-timeline-treeclip-pipeline-runtime`。
3. 以两者最终 current spec、runtime 与 Corin 资产为基线 apply 本 change。

该顺序的业务取舍是推迟动画 Transition 重构，但避免同一 StateMachine/Timeline/Corin 资产被三套迁移脚本并发改写，从而产生分裂真相。

