# Change: 基础移动动画参数可配置

## Why

当前基础移动动画配置主要提供阶段到动画 key 的映射，状态机的 `MoveStop` 退出仍依赖 `MoveStopMinTime` 这类运动配置时间。这样 `RunEnd / WalkEnd` 的完整播放时长无法由动画配置驱动，也会让后续动作窗口、多层动画、IK 和预测回滚缺少统一的动画时间数据基础。

## What Changes

- 将基础移动动画配置从简单 key 映射扩展为动画 entry 数据，包含 key、淡入时间、播放速度、起始归一化时间和退出时长。
- 为 `WalkEnd / RunEnd` 提供可解析的停止退出时长，使逻辑层读取纯数据结果，而不是查询 Animancer 播放状态。
- 调整 `MoveStop` 无输入回 `Idle` 的判定来源：优先使用当前停止动画解析出的退出时长。
- 保持 `MoveStop` 中重新出现移动输入时立即进入 `MoveStart`。
- 保持动画外观层只负责播放，不负责状态切换和运动执行。
- 保持位移权威在当前运动执行端口，不引入 Root Motion 接管。

## Non-Goals

- 不实现 FullBody / UpperBody / LowerBody 多层状态机。
- 不实现攻击、闪避、受击、死亡等动作状态。
- 不实现动作打断窗口、命中窗口、IK 窗口或 Timeline 编辑器。
- 不实现预测回滚、网络协议或动画快照同步。
- 不引入 BBB 运行时依赖，不复制 BBB 主控或状态内部互跳路径。

## Impact

- Affected specs:
  - `basic-locomotion-animation`
  - `unityhfsm-locomotion`
- Affected code:
  - `Assets/Scripts/Character/Animation/Config/LocomotionAnimationSetSO.cs`
  - `Assets/Scripts/Character/Animation/Model/*`
  - `Assets/Scripts/Character/Animation/Runtime/BasicLocomotionAnimancerPresenter.cs`
  - `Assets/Scripts/Character/Movement/Model/BasicMovementSettings.cs`
  - `Assets/Scripts/Character/Movement/Model/LocomotionStateGraphContext.cs`
  - `Assets/Scripts/Character/Movement/Solver/BasicLocomotionStateMachine.cs`
  - `Assets/Scripts/Character/Movement/Solver/LocomotionStateGraphConditionEvaluator.cs`
  - `Assets/Tests/Editor/PlayerLocomotionControllerTests.cs`

## Open Questions

- 当前阶段先使用 `ExitDurationOverride` 作为运行时逻辑时长来源；如果未来需要自动读取 `AnimationClip.length`，应在后续变更中引入 Clip 引用或编辑器烘焙步骤。
- 现有 `MoveStopMinTime` 是否保留为无动画配置时的兼容 fallback，本 proposal 倾向保留 fallback，但新配置存在时以动画 entry 为准。
