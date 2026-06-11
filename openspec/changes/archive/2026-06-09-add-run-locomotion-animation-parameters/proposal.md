# Change: Run 基础移动动画参数最小配置

## Why

当前基础移动逻辑状态机已经是 `Idle / MoveStart / MoveLoop / MoveStop` 四阶段，但动画播放层仍在代码里写死 `Idle / RunStart / RunLoop / RunEnd` 等 alias key，且 `MoveStop -> Idle` 的等待时间只来自移动配置里的 `moveStopMinTime`。现在需要先把 Run 基础移动这条链路做成可配置、可测试的最小闭环，不引入 Walk、Shift、多层动画或 IK。

## What Changes

- 保持逻辑状态机仍只有 `Idle / MoveStart / MoveLoop / MoveStop` 四阶段。
- 当前版本只支持 Run 基础移动动画，不新增 Walk 状态或 Walk 动画配置。
- 新增 Run-only 基础移动动画参数配置，覆盖 `Idle / RunStart / RunLoop / RunEnd`。
- 动画参数最小包含 alias key、淡入时间、播放速度、归一化起播时间和停止退出时长。
- `BasicLocomotionAnimancerPresenter` 从 Run 动画配置读取播放参数，但仍只负责播放，不切状态、不执行位移。
- `MoveStop -> Idle` 的等待时间优先使用 RunEnd 的停止退出时长；缺失时 fallback 到 `moveStopMinTime`。
- `MoveStop` 中出现移动输入时仍必须立即切回 `MoveStart`，不等待 RunEnd。

## Non-Goals

- 不实现 Walk；Shift 决定 Walk/Run 的 gait 选择另起 proposal。
- 不新增 `WalkStart / WalkLoop / WalkEnd` 配置。
- 不实现攻击、闪避、受击、跳跃、落地或动作打断窗口。
- 不实现 FullBody / UpperBody / LowerBody 多层动画。
- 不实现 IK、Timeline 编辑器、预测回滚或网络动画快照。
- 不新增第二套角色控制器或绕过当前 `PlayerLocomotionController` 主链。

## Impact

- Affected specs:
  - `basic-locomotion-animation`
  - `unityhfsm-locomotion`
- Affected code:
  - `Assets/Scripts/Character/Animation/Runtime/BasicLocomotionAnimancerPresenter.cs`
  - `Assets/Scripts/Character/Animation/Model/*`
  - `Assets/Scripts/Character/Animation/Config/*`
  - `Assets/Scripts/Character/Movement/Model/BasicMovementSettings.cs`
  - `Assets/Scripts/Character/Movement/Runtime/PlayerLocomotionController.cs`
  - `Assets/Scripts/Character/Movement/Solver/LocomotionStateGraphConditionEvaluator.cs`
  - `Assets/Tests/Editor/PlayerLocomotionControllerTests.cs`

## Open Questions

- 当前 proposal 默认 RunEnd 的停止退出时长由手填配置提供，不自动读取 `AnimationClip.length`；如果之后要自动烘焙 clip 长度，需要另起编辑器/导入流程 proposal。
