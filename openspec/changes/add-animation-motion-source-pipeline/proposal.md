# Change: 新增动画运动源采样管线

## Why

当前 TurnBack 调试说明：`OnAnimatorMove` 能产生 root delta，但该 delta 属于 Unity Animator evaluation 时机。它通过 pending buffer 被 simulation tick 拉取消费时，会因为 Animator evaluation 与项目 simulation tick 不同拍而出现空消费、延迟消费或不稳定消费。

项目后续需要预测、回滚、预测矫正和可测试的动画混合边界，因此 TurnBack 不应再依赖 Animator runtime delta。动画 root motion 必须先变成 tick 可采样的运行时数据，再进入 movement facts 和 motion executor。

## What Changes

- 新增 `animation-motion-source-pipeline` 能力，定义状态可选的 tick 对齐动画运动源、采样窗口、运动权威和诊断边界。
- 将 TurnBack 的默认权威运动来源固定为 `TickSampledMotion`：按 simulation tick 的播放窗口采样 motion profile，并转换为 movement facts。
- 删除 TurnBack 默认/正式路径中的 `OnAnimatorMove -> pending buffer -> simulation tick 拉取消费`。
- 删除基础移动 Presenter 暴露 pending runtime root delta source/rollback provider 的入口。
- 删除表现层当前 `AnimationClip`/`AnimancerState` 作为 TurnBack 运动源的 authored root motion 入口，避免测试环境和运行时行为分裂。
- 保留 Animator runtime delta 诊断日志：可看到 Animator 是否产生 delta，但日志不得改变 movement facts 或 rollback state。
- Generic rootmotion 原动画作为运动母带；由它派生 runtime motion profile 和 cleaned in-place visual clip，避免同一个状态同时由 Animator runtime delta 与 profile 双重驱动。

## Non-Goals

- 不在本变更中实现 `AnimatorRuntimeDirect` 兼容模式。
- 不新增 root motion sink 或第二套角色控制器。
- 不让 Animancer/Animator 直接成为逻辑状态权威。
- 不让动画外观层直接调用 `CharacterController.Move` 或写角色根 Transform。
- 不删除现有 TurnBack 诊断日志。
- 不一次性迁移 Dodge/Attack/HitReact 等其它动作状态。
- 不运行 Unity batchmode。

## Impact

- Affected specs:
  - `animation-motion-source-pipeline`
  - `basic-locomotion-animation`
  - `simulation-tick-locomotion`
  - `unified-character-state-machine`
- Affected code:
  - `Assets/Scripts/Character/Movement/Model/BasicMovementMotionFacts.cs`
  - `Assets/Scripts/Character/Movement/Runtime/PlayerLocomotionController.cs`
  - `Assets/Scripts/Character/Animation/Runtime/BasicLocomotionAnimancerPresenter.cs`
  - `Assets/Scripts/Character/Animation/Model/ILocomotionRootMotionSource.cs`
  - `Assets/Scripts/Character/Animation/Model/ILocomotionAuthoredRootMotionSource.cs`
  - `Assets/Scripts/Character/Animation/Model/LocomotionAuthoredRootMotionDelta.cs`
  - `Assets/Scripts/Character/Animation/Solver/AnimationClipRootMotionSampler.cs`
  - `Assets/Configs/3C/Statemachine/DefaultCharacterStateMachine.asset`
  - `Assets/Configs/3C/Animation/Locomotion/Corin/DefaultRunLocomotionAnimationConfig.asset`
  - `Assets/Tests/Editor`
- Related active changes:
  - Must remain compatible with `refactor-turnback-request-entry` and `refactor-fullbody-frame-pipeline`.
