# Change: 清理动作打断准入残留分裂风险

## Why
`integrate-action-interrupt-runtime-gate` 已经把默认 Dodge 准入收束到 `FullBodyActionInterruptGate -> ActionInterruptArbiter -> CharacterInputRequestFact -> unified CharacterStateMachine`。当前默认 Dodge 主线不再分裂。

Dodge 是 FullBody Action 管线的第一个实例。方向/后撤变体、动作位移、结束后回到 Idle/MoveLoop、run latch 继承等只是 Dodge 实例的数据和行为差异，必须通过同一条 FullBody Action 管线表达。

当前仍有三个残留风险会让后续 Attack、HitReact、Death 或连续 Dodge 接入时重新分裂：

- 状态机仍保留 `RequestPriorityAtLeast` 条件类型和 evaluator 分支，后续容易被重新挂回动作入口，和仲裁器的 `minPriority/resistance/force/timing` 重叠。
- `PlayerFullBodyActionController` 仍使用 `DodgeActionConfig.Default`，没有把 `DodgeActionConfigSO` 作为正式运行时配置来源。
- `FullBodyActionInterruptGate` 具备 `currentStateResistance` 入参，但当前控制器传入 `0`，导致运行时 resistance 事实没有正式进入仲裁上下文。

## What Changes
- 删除或降级 `RequestPriorityAtLeast` 作为状态机 transition 条件，保留 transition 自身 `priority` 只用于状态图多边选择。
- 将 Dodge 的 priority、resistance、duration、distance、rotate、方向/后撤变体等实例参数收束到 `DodgeActionConfigSO` 或等价动作实例配置入口。
- 保证 Dodge 作为 FullBody Action 实例走同一套请求构建、仲裁、状态机事实、状态机输出管线。
- 从统一状态机快照和动作配置派生当前 Action 仲裁上下文，给 `ActionInterruptArbiter` 提供真实 `currentStateResistance`。
- 明确 `ActionRuntimeStateTracker` 或等价事实缓存不得成为第二状态权威；当前 Action state 必须由统一状态机快照同步或派生。
- 补齐自动测试、静态边界测试和手动验证步骤，证明默认动作入口没有重新出现状态机优先级准入。

## Impact
- Affected specs:
  - `action-interrupt-arbiter`
  - `action-runtime-state-tracker`
  - `locomotion-state-graph-config`
- Affected code:
  - `Assets/Scripts/Character/StateMachine/Model/CharacterStateMachineTypes.cs`
  - `Assets/Scripts/Character/StateMachine/Solver/CharacterStateTransitionEvaluator.cs`
  - `Assets/Scripts/Character/StateMachine/Model/CharacterStateMachineDefinition.cs`
  - `Assets/Scripts/Character/Action/FullBody/Runtime/PlayerFullBodyActionController.cs`
  - `Assets/Scripts/Character/Action/FullBody/Solver/FullBodyActionInterruptGate.cs`
  - `Assets/Scripts/Character/Action/Config/DodgeActionConfigSO.cs`
  - `Assets/Configs/3C/Action/*`
  - `Assets/Prefabs/Character/可琳.prefab`
  - `Assets/Tests/Editor/UnifiedCharacterStateMachineTests.cs`
  - `Assets/Tests/Editor/ActionRuntimeStateTrackerTests.cs`
