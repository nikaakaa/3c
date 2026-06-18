# Change: 拆分角色诊断 Adapter

## Why
当前 diagnostics 已经通过 `CharacterFrameDiagnostics` 和 `RuntimeDiagnosticLog` 统一输出，但触发点仍分散在 runner、pipeline、FullBody host 和 Locomotion controller 附近。随着 timeline facts trace、condition trace、frame output trace 和 rollback trace 增加，如果各模块继续直接提交日志，会让诊断系统重新绑定运行时核心。

本阶段要把“产生 trace”和“提交日志”分开：runner/pipeline/output modules 只产出纯数据 trace 或调用窄 diagnostic port，外围 diagnostic adapter 负责格式化和提交 `RuntimeDiagnosticLog`。

## What Changes
- 建立角色帧诊断 adapter 边界。
- 将 transition condition trace、timeline facts trace、frame phase summary、snapshot change、action accepted/rejected 等日志提交集中到 adapter。
- runner、condition evaluator、timeline sampler、frame pipeline 和 output runtime MUST 不直接依赖 `RuntimeDiagnosticLog.Submit`。
- 保留现有 event id 和 channel key，不删除既有日志语义。
- 支持 tests 使用 fake diagnostic sink 观察 trace，不需要 Unity Console。
- 保持 `RuntimeDiagnosticLog` 作为统一日志出口。

## Non-Goals
- 不删除现有日志。
- 不改变日志宏、过滤器或 Inspector channel toggle 的既有规则。
- 不重新设计 RuntimeDiagnosticLog 数据模型。
- 不把 diagnostics 变成状态权威。
- 不改变 gameplay 行为以满足日志需求。

## Impact
- Affected specs:
  - `runtime-diagnostic-logging`
  - `fullbody-action-framework`
  - `unified-character-state-machine`
  - `simulation-tick-system`
- Affected code:
  - `Assets/Scripts/Character/Action/FullBody/Diagnostics/CharacterFrameDiagnostics.cs`
  - `Assets/Scripts/Diagnostics/*`
  - `Assets/Scripts/Character/Action/FullBody/Runtime/*`
  - `Assets/Scripts/Character/StateMachine/Solver/Runtime/*`
  - `Assets/Scripts/Character/StateMachine/Solver/Transition/*`
  - `Assets/Scripts/Character/Movement/Diagnostics/*`
  - `Assets/Tests/Editor/RuntimeDiagnosticLogTests.cs`
  - `Assets/Tests/Editor/UnifiedCharacterStateMachineTests.cs`

## Dependencies
- Can run after `refactor-fullbody-output-runtime-modules` so output diagnostics have a stable module boundary.
- Coordinates with active `refactor-state-timeline-facts-authority` and `refactor-transition-condition-evaluators`, because they define facts trace and condition trace.
- Should land before large new action types, otherwise each action may add direct logging paths.

## Success Criteria
- Runtime core modules return trace or use diagnostic port, not direct `RuntimeDiagnosticLog.Submit`.
- Existing event ids remain present.
- Static tests prove runner/pipeline/evaluator do not directly submit logs.
- Behavior tests prove logging filter changes do not alter owner/phase/active path.

## Detailed Scope Partition
| Area | This change owns | This change must not own | Completion evidence |
| --- | --- | --- | --- |
| Trace production | Define pure trace data produced by runner, transition evaluator, timeline sampler, pipeline and output modules. | Formatting Unity Console messages inside core modules. | Static tests prove trace types contain no Unity scene object references. |
| Event formatting | Convert trace data into existing diagnostic event ids and payloads. | Changing gameplay decisions to make logs easier. | Event-id tests prove old keys still exist. |
| Log submission | Submit formatted events through `RuntimeDiagnosticLog` via sink. | Direct `RuntimeDiagnosticLog.Submit` from core runtime. | Static tests forbid direct submit in runner/pipeline/evaluator/sampler. |
| Test sink | Provide fake sink for EditMode tests. | Requiring Unity Console text as the only assertion source. | Tests assert captured trace/event records. |
| Existing facades | Keep or wrap `CharacterFrameDiagnostics` and `LocomotionDiagnostics` as adapters/facades. | Duplicating event ids in multiple submission paths. | Search shows one formal submit path per event family. |
| Filtering | Preserve macro/filter/channel behavior. | Letting filter state affect gameplay state. | Behavior tests compare with diagnostics on/off. |

## Diagnostic Flow Contract
The intended flow is:

```text
Runtime core
  -> pure trace
  -> diagnostic adapter / formatter
  -> ICharacterDiagnosticSink
  -> RuntimeDiagnosticLog
```

The reverse direction is forbidden: `RuntimeDiagnosticLog`, channel filters and debug UI MUST NOT feed state back into request arbitration, transition evaluation, timeline facts, motion output or animation output.

## Event Families To Preserve
- FullBody active path changed.
- Pending transition changed.
- Action accepted/rejected.
- Transition condition probe.
- Timeline facts probe.
- Frame phase summary.
- Snapshot change summary.
- Locomotion phase changed.
- TurnBack diagnostic probe.
- FullBody / Locomotion driver conflict guard.
- Retired direct driver guard.

If an implementation discovers another current event family, it must add it to this list before migration so the change remains auditable.

## User Verification
用户可以通过这些方式确认 change 完成：

- 跑 runtime diagnostic log 相关 EditMode 测试，确认旧 event id 仍可观察。
- 跑统一状态机/FullBody frame 测试，确认 diagnostics on/off 行为一致。
- 搜索 runner、pipeline、condition evaluator、timeline sampler，确认没有直接 `RuntimeDiagnosticLog.Submit`。
- 搜索 trace 类型，确认没有保存 `MonoBehaviour`、`Transform`、`CharacterController`、Animancer state 或 InputAction。
