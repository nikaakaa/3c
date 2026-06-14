# Change: 正式化 TurnBack Locomotion 状态

## Why

当前 TurnBack 已经能通过统一管线进入 `FullBody/Locomotion/TurnBack`，但运动权威、动画 root motion、输入抑制和退出窗口仍散落在 controller、presenter、motion facts 和临时采样逻辑里。结果是动画能转、角色也能转，但手感仍受动画尾巴、RootT 基线、运行时 root motion 采样差异和普通移动回接时机影响。

参考工程能稳定表现 TurnRun/TurnBack 的关键不是单纯使用 Animator 状态机，而是 TurnBack/ReturnRun 被当成一个明确的移动逻辑状态：进入后动画接管关键窗口，普通旋转短暂停止，动画事件或状态时间决定回到 Sprint/Idle。本变更把这个状态契约明确落回当前统一状态机和 motion executor 链路，而不是新增第二套角色控制器。

## What Changes

- 将 TurnBack 定义为正式 Locomotion 逻辑状态契约，而不是普通 MoveLoop 内的动画/root motion 特判。
- 默认 TurnBack 只允许从 `MoveLoop + Run` 进入，Walk、MoveStart、MoveStop 和 Idle 不直接触发该转身动画。
- TurnBack 状态进入时锁定目标朝向、动画 alias、运动权威策略和退出窗口。
- TurnBack 活跃期间普通输入旋转和普通输入平面位移必须被抑制。
- TurnBack 的旋转/位移来源必须通过配置化 motion policy 转成统一 `MovementCommand` 运动事实，再由现有 motion executor 执行。
- 第一版面向 Generic/Sandbox：默认使用 TurnBack 转身窗口的烘焙 motion profile 驱动根位移和 yaw，转完后立即回到普通 MoveLoop/Idle，不继续消耗动画后半段跑步尾巴。
- motion policy 必须使用运行时可读的烘焙运动数据入口：编辑器可以从动画 clip 提取 yaw、translation、marker、entry/exit timing 和校验信息，运行时只消费生成后的纯数据资产。
- TurnBack 必须显式表达动画进入时间、锁定窗口、转完点和退出时间，而不是只依赖整段 clip 播放结束。
- 动画外观层仍只负责播放、采样和回传纯数据事实，不直接切状态、不直接移动角色 Transform。
- 保留现有诊断日志，并新增能判断 TurnBack 状态权威、退出窗口和运动来源的日志字段。

## Non-Goals

- 不恢复 `TurnInPlace`、`MovingPivotTurn` 或旧的散落式 baked yaw/profile 修补路线。
- 不新增绕过 `PlayerLocomotionController`、统一状态机、Animancer presenter 或 motion executor 的第二套控制器。
- 不复制参考工程 Animator Controller 或完整角色状态机。
- 不处理 Humanoid 资源，第一版只保证 Generic 可琳/Sandbox。
- 不实现攻击连招、跳跃、翻滚或完整 action motion policy。
- 不在本变更第一步强制实现完整编辑器 UI；但数据结构和任务必须为后续编辑器 authoring 预留边界。
- 不删除现有日志。
- 不运行 Unity batchmode。

## Impact

- Affected specs:
  - `unified-character-state-machine`
  - `basic-locomotion-animation`
  - `wasd-locomotion-pipeline`
- Affected code:
  - `Assets/Scripts/Character/StateMachine/Model`
  - `Assets/Scripts/Character/StateMachine/Solver`
  - `Assets/Scripts/Character/Movement/Model`
  - `Assets/Scripts/Character/Movement/Runtime/PlayerLocomotionController.cs`
  - `Assets/Scripts/Character/Animation/Runtime/BasicLocomotionAnimancerPresenter.cs`
  - `Assets/Scripts/Character/Animation/Solver`
  - `Assets/Tests/Editor`
- Related active changes:
  - Depends on `refactor-locomotion-decision-pipeline` for early TurnBack intent and unified decision facts.
  - Narrows `refactor-locomotion-decision-pipeline` 中较宽的 `MoveStart/MoveStop -> TurnBack` 入口：正式 TurnBack 动画默认只从 RunLoop 触发。
  - Does not reopen `add-moving-pivot-turn`; old moving pivot / turn-in-place behavior remains removed.
