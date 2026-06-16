# Change: 移动 TurnBack 改为逻辑状态和 Root Motion

## Why

旧的 `TurnInPlace`、`MovingPivotTurn` 和 baked yaw 方案把原地转身、移动急转、代码旋转和烘焙曲线混在同一条移动链路里。Sandbox 里已经出现过转、瞬切、偏移和左右不一致，说明继续修补这套方案会让运动权威更混乱。

## What Changes

- 删除 `TurnInPlace` 原地转身运行链路、配置类型、selector、状态机节点和测试。
- 删除 `MovingPivotTurn` 运行链路、配置类型、selector、baked yaw 采样、计划生命周期和测试。
- 删除旧 TurnInPlace/MovingPivot 配置 asset 和一次性 bake/configure 工具。
- 保留普通 WASD locomotion、run/walk 动画、dodge/action 状态和统一 movement executor。
- 保留动画 motion 诊断日志能力，但不再使用 `moving-pivot-turn` 作为运行概念。
- 下一步 TurnBack SHALL 作为明确逻辑状态处理，窗口内由 Animator/Animancer root motion delta 进入统一 motion executor，而不是继续使用 baked yaw/profile 补丁。

## Non-Goals

- 本阶段不删除 TurnBack 动画 clip、TransitionAsset 或可复用动画资源。
- 本阶段不把移动急转临时塞回普通 MoveLoop 旋转逻辑。
- 本阶段不新增第二套角色控制器或绕过 `MovementCommand`/motion executor 的位移出口。
- 本阶段不运行 Unity batchmode。

## Impact

- Affected specs: `locomotion-turnback-root-motion`
- Affected code:
  - `Assets/Scripts/Character/Movement`
  - `Assets/Scripts/Character/Animation`
  - `Assets/Scripts/Character/StateMachine`
  - `Assets/Scripts/Character/Config`
  - `Assets/Scripts/Simulation/Rollback`
  - `Assets/Editor`
  - `Assets/Tests/Editor`
