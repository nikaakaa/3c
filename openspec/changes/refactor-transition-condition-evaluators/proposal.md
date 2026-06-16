# Change: 拆分 Transition 条件求值器

## Why
`CharacterStateTransitionEvaluator` 目前通过中心枚举直接硬编码 `MoveTurnBackRequested`、`LocomotionAnimationCanExit`、`ActionCanExit` 等业务条件。后续添加轻攻击、跳跃、受击和武器状态时，如果继续改中心 evaluator，会让状态机核心越来越像业务分发器。

## What Changes
- transition 配置保留稳定 condition key 和参数，但条件求值由一组 domain evaluator adapter 提供。
- 统一状态机 runner 只负责选择 transition，不包含 TurnBack、Dodge、ActionCanExit 等业务判断。
- FullBody、Locomotion、Action 分别贡献自己的 facts evaluator。
- 缺失 evaluator、重复 key 或 evaluator 读取禁止对象时必须被测试或配置校验发现。
- 现有条件先迁为内置 adapter，行为保持不变。
- 诊断日志从 evaluator/runner 直接提交迁移为 condition trace，由外围 diagnostics adapter 统一提交。
- 状态图配置仍使用受控 condition key 集合，不允许任意运行时代码或场景对象成为条件实现。

## Non-Goals
- 不引入任意脚本回调。
- 不允许状态配置引用 MonoBehaviour evaluator。
- 不实现通用游戏状态机框架。
- 不改变现有 transition 配置表达能力。
- 不实现轻攻击、跳跃或受击。

## Impact
- Affected specs:
  - `unified-character-state-machine`
  - `fullbody-action-framework`
  - `action-interrupt-arbiter`
  - `locomotion-state-graph-config`
  - `runtime-diagnostic-logging`
- Affected code:
  - `Assets/Scripts/Character/StateMachine/Model/CharacterStateMachineTypes.cs`
  - `Assets/Scripts/Character/StateMachine/Solver/Transition/CharacterStateTransitionEvaluator.cs`
  - `Assets/Scripts/Character/StateMachine/Solver/Runtime/CharacterStateMachineRunner.cs`
  - `Assets/Scripts/Character/Movement/*`
  - `Assets/Scripts/Character/Action/*`
  - `Assets/Tests/Editor/UnifiedCharacterStateMachineTests.cs`

## Related Changes
- Builds on `refactor-character-hierarchical-state-runtime`.
- Coordinates with `add-configurable-state-interrupt-windows` because request/window facts remain external inputs.
- Should run after or alongside `refactor-state-timeline-facts-authority`, because evaluator adapters should consume the same current timeline facts package.

## Parallel Implementation Plan
- 本变更可以和 `refactor-state-timeline-facts-authority`、`refactor-state-action-motion-output` 并行推进，但它只拥有 condition key、domain evaluator adapter、condition result 和 condition trace。
- 可以先并行实现 evaluator collection、core/Locomotion/Animation/Action adapter、validator 和纯测试，不需要等待其他 proposal。
- runner transition 主循环集成必须等待 `refactor-state-timeline-facts-authority` 稳定 current/projected/target facts 字段后再接入，且只能消费同一个 condition context。
- 本变更不拥有 `CharacterStateMachineFrame` 的 action motion 字段；如需读取 action exit/completed，应通过稳定 facts 或 `refactor-state-action-motion-output` 暴露的 result 进入 condition context。
- `CharacterStateMachineRunner.cs` 的改动顺序为：先接入 timeline facts trace，再用 evaluator collection 替换中心业务分支。
- 诊断迁移只输出 condition trace；具体日志提交仍由外围 diagnostics adapter 负责。
- `add-light-attack-combo-action` 只能在现有 TurnBack、Locomotion animation exit、Action exit 都迁入 adapter 并完成回归后，再新增攻击条件 adapter。

## Stop Conditions
- 如果新增条件需要修改 runner transition 选择循环，必须停止并重新评审 adapter seam。
- 如果 evaluator 需要读取 MonoBehaviour、ScriptableObject 策略资产、Animancer state 或 CharacterController，必须停止。
- 如果 condition key 缺失 evaluator 时只能静默返回 false，必须停止并补配置校验。
- 如果为了并行落地而复制一套 timeline facts 或 action motion facts，必须停止并改为消费对应 proposal 的权威结果。
- 如果 runner 中同时保留新 adapter 路径和旧业务分支作为两套可选路径，必须停止并收敛为单一路径。
