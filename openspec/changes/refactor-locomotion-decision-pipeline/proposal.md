# Change: 统一 Locomotion 决策管线

## Why

当前 Locomotion 每帧处理顺序虽然大体存在，但输入快照、移动意图、相机相对方向、人物朝向、phase facts、TurnBack 条件、状态机 context、motion facts 和动画反馈都挤在 `PlayerLocomotionController.TryEvaluateWithStateMachine` 的隐式流程里。结果是 TurnBack 这类移动派生逻辑容易被写进 transition evaluator 或专用补丁，后续攻击连招、跳跃、翻滚等也会继续诱发分裂路径。

## What Changes

- 将现有 Locomotion 主链明确整理为一个统一的 decision pipeline，而不是新增第二套 controller 或第二条 TurnBack 路径。
- 统一每帧阶段：读取输入快照、解析移动意图、解析空间事实、派生 Locomotion 决策事实、推进统一状态机、构建 motion facts/command、执行运动、提交动画、回写动画事实。
- 新增或整理 `LocomotionDecisionFacts` 或等价纯数据模型，集中承载 has move、world move direction、facing forward、gait candidate、phase facts、TurnBack intent 等 Locomotion 决策事实。
- TurnBack 不再作为独立补丁；它作为统一 Locomotion decision pipeline 的第一个派生事实进入 `CharacterStateMachineContext`。
- 状态机 transition 只消费 context facts；transition evaluator 不再临时执行空间解析、相机解析或运动权威逻辑。
- 保持动作按钮预输入缓冲的边界：Attack/Dodge/Jump/Interact 仍走 `InputRequestBuffer`，但最终同样作为纯数据 facts 进入统一状态机 context。
- 保持运动权威和动画边界：运动仍只通过 `MovementCommand`/motion executor，Animancer presenter 只播放状态机结果并回传动画事实。

## Non-Goals

- 不恢复 `TurnInPlace`、`MovingPivotTurn`、baked yaw/profile 或独立 TurnBack 运行路径。
- 不把移动轴输入塞进按钮 `InputRequestBuffer`。
- 不一次性实现攻击连招、跳跃、翻滚或完整 Action combo 系统。
- 不重写整个 FullBody action framework。
- 不新增绕过 `PlayerLocomotionController`、统一状态机、Animancer presenter 或 motion executor 的路径。
- 不运行 Unity batchmode。

## Impact

- Affected specs:
  - `wasd-locomotion-pipeline`
  - `simulation-tick-locomotion`
  - `unified-character-state-machine`
  - `locomotion-turnback-root-motion`
- Affected code:
  - `Assets/Scripts/Character/Movement/Model`
  - `Assets/Scripts/Character/Movement/Solver`
  - `Assets/Scripts/Character/Movement/Runtime/PlayerLocomotionController.cs`
  - `Assets/Scripts/Character/StateMachine/Model`
  - `Assets/Scripts/Character/StateMachine/Solver`
  - `Assets/Scripts/Simulation/Rollback`
  - `Assets/Tests/Editor`
  - `docs/agents/turnback-rootmotion-debug-log.md`

## Relationship To Existing Changes

- `add-moving-pivot-turn` 清理了旧 TurnInPlace/MovingPivot 路线并接入 TurnBack root motion，本变更不恢复旧路线。
- 本变更取代之前单独规划的 `refactor-turnback-intent-capture` 思路：TurnBack 仍要早期捕获，但必须作为统一 Locomotion decision pipeline 的一个事实，而不是独立专项路径。
