# Change: 统一状态 Timeline Facts 帧内权威

## Why
当前 Action request resolver、状态机 runner 和 transition 过程会在不同阶段各自采样 timeline facts，导致同一帧请求准入、状态切换和输出解析可能使用不同时间视角。继续接轻攻击、跳跃或受击时，窗口事实会变成隐式多权威。

## What Changes
- 将当前状态的 `StateTimelineWindowFacts` 收口为 Character frame context 中的单一权威事实包。
- Action request submission / interrupt arbitration、transition 条件和状态输出 MUST 消费同一个 current timeline facts。
- transition evaluator 如需预判下一帧时间，MUST 使用显式命名的 projected facts，不得覆盖 current facts。
- transition 发生后，目标状态进入帧 MAY 重新生成目标状态 facts，但必须作为 target facts 显式传递。
- Action request submission / interrupt arbitration MUST 不再读取 `CharacterStateMachineDefinition + CurrentSnapshot` 自行采样 timeline。
- 状态机 runner MUST 不再直接提交 timeline 诊断日志，改为返回 facts trace 或由外围 adapter 提交。
- `StateTimelinePolicy` 仍是窗口数据权威；本变更只收口“谁在一帧里采样并传递 facts”。
- 当前帧 facts 包必须能进入 rollback/replay 对比，避免预测回放时 request submission / interrupt arbitration 与 transition 使用不同窗口。

## Non-Goals
- 不新增完整 timeline 编辑器。
- 不重建 `StateTimelinePolicy` 数据模型。
- 不改变 TurnBack、Dodge、MoveStart、MoveLoop、MoveStop 的当前玩法数值。
- 不新增 fallback 配置。
- 不实现轻攻击、跳跃或受击。

## Impact
- Affected specs:
  - `unified-character-state-machine`
  - `fullbody-action-framework`
  - `action-interrupt-arbiter`
  - `animation-phase-timeline-facts`
  - `runtime-diagnostic-logging`
- Affected code:
  - `Assets/Scripts/Character/Action/FullBody/Runtime/FullBodySubmissionBuilder.cs`
  - `Assets/Scripts/Character/Action/FullBody/Solver/CommittedActionRequestSubmissionResolver.cs`
  - `Assets/Scripts/Character/StateMachine/Solver/Runtime/CharacterStateMachineRunner.cs`
  - `Assets/Scripts/Character/StateMachine/Solver/Timeline/*`
  - `Assets/Scripts/Character/StateMachine/Solver/Transition/*`
  - `Assets/Scripts/Character/StateMachine/Solver/Output/*`
  - `Assets/Scripts/Diagnostics/*`
  - `Assets/Tests/Editor/UnifiedCharacterStateMachineTests.cs`

## Related Changes
- Builds on `add-configurable-state-interrupt-windows`; this change does not replace its window model.
- Coordinates with `refactor-character-hierarchical-state-runtime` and `refactor-character-frame-submission-pipeline`.

## Parallel Implementation Plan
- 本变更可以和 `refactor-transition-condition-evaluators`、`refactor-state-action-motion-output` 并行推进，但它拥有 timeline facts 命名、采样归属和帧内传递语义的最终定义权。
- 实现前必须先稳定 current/projected/target facts 的最终类型名和字段名；其他并行变更只能消费这些字段，不能另建 frame facts/context。
- `CharacterFramePipeline`、`CharacterFrameSubmission` 与 `FullBodySubmissionBuilder` 中 timeline facts 采样与传递由本变更优先落地。
- `CharacterStateMachineRunner.cs` 中 facts trace 与 current/projected/target 语义由本变更先落地；condition evaluator 变更随后接入同一个 condition context。
- `CharacterStateMachineRuntimeTypes.cs` / `CharacterStateMachineFrame` 的公共字段变动必须和 `refactor-state-action-motion-output` 同步，避免同一帧结果模型被两个 proposal 各自扩展。
- `refactor-transition-condition-evaluators` 可以并行做 evaluator collection、adapter 和测试，但 runner 主循环集成必须等本变更的 facts 字段稳定后再接。
- `refactor-state-action-motion-output` 可以并行做纯 `ActionMotionSpec` / resolver / result 和测试，但不得重新采样 timeline，也不得绕过本变更产出的 facts 包。
- 轻攻击、跳跃、受击等新业务状态仍必须等这三个架构接缝通过审批和验证后再接入。

## Stop Conditions
- 如果需要让 Action request submission / interrupt arbitration 自行读取状态机 definition 才能通过测试，必须停止并重新评审 facts ownership。
- 如果需要新增第二套 timeline policy 或 fallback window 配置，必须停止并重新评审与 `add-configurable-state-interrupt-windows` 的关系。
- 如果 current/projected/target 三类 facts 在日志或测试中无法区分，必须停止收口，先补诊断 trace。
- 如果并行实现需要第二个 frame facts/context 包，必须停止并先统一模型。
- 如果三个 proposal 对同一帧字段名、字段归属或采样时机产生冲突，必须停止集成并先做同步决策。
