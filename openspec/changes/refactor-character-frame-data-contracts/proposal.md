# Change: 拆分 Character Frame 数据契约

## Why
`CharacterFramePipelineTypes` 当前聚合了 frame input、context、submission、output、result 和 pipeline step。它现在还能工作，但随着 FullBody output、Locomotion runtime、diagnostics trace、UpperBody/HitReaction/Aim layer 规划推进，这个文件和数据对象会越来越像“角色帧总线”。

本阶段要拆数据契约，不改变行为：让 input/context/submission/output/result 各自成为稳定纯数据边界，并防止 future layer 把 Unity 对象或副作用字段塞进 frame context。

## What Changes
- 将 `CharacterFramePipelineTypes` 拆分为多个 focused model 文件。
- 明确 `CharacterFrameInput`、`CharacterFrameContext`、`CharacterFrameSubmission`、`CharacterFrameOutput`、`CharacterFrameResult` 的职责。
- 保持所有 frame data 为纯数据，不引用 Unity scene object、Animancer runtime object 或 input runtime object。
- 将 diagnostics summary/trace 字段保持纯数据，不直接提交日志。
- 为 future layer submission 预留扩展方式，但不实现 UpperBody/HitReaction/Aim。
- 保持 serialization/rollback comparison 可测试。

## Non-Goals
- 不改变 frame phase 顺序。
- 不改变 FullBody output runtime 实现。
- 不改变状态机 runner、transition evaluator 或 timeline facts authority。
- 不实现并行状态机或多 layer blending。
- 不移动 gameplay rule 到数据对象。

## Impact
- Affected specs:
  - `fullbody-action-framework`
  - `simulation-tick-system`
  - `fullbody-rollback-replay`
  - `project-structure`
- Affected code:
  - `Assets/Scripts/Character/Pipeline/Model/CharacterFramePipelineTypes.cs`
  - `Assets/Scripts/Character/Pipeline/Runtime/CharacterFramePipeline.cs`
  - `Assets/Scripts/Character/Pipeline/Contracts/ICharacterFrameRuntimePort.cs`
  - `Assets/Scripts/Character/Action/FullBody/Runtime/FullBodySubmissionBuilder.cs`
  - `Assets/Scripts/Character/Action/FullBody/Runtime/*`
  - `Assets/Tests/Editor/Simulation/FullBodyRollbackReplayTests.cs`
  - `Assets/Tests/Editor/SimulationTickSystemTests.cs`

## Dependencies
- Should land after `refactor-fullbody-output-runtime-modules`, so output behavior is no longer hidden in controller methods.
- Should land after or coordinate with active `refactor-state-timeline-facts-authority`, because current/projected/target facts fields must be stable before frame data split.
- Should coordinate with active `refactor-state-action-motion-output`, because action motion result fields are part of frame submission/result.

## Success Criteria
- `CharacterFramePipelineTypes.cs` is split or reduced to an index/compat file.
- Each frame data type lives in a focused file with one clear reason to change.
- Static tests prove frame data types contain no Unity scene object references.
- Behavior tests prove frame result equality/diagnostic summaries remain stable.

## Detailed Scope Partition
| Area | This change owns | This change must not own | Completion evidence |
| --- | --- | --- | --- |
| File split | Move frame data types into focused files under `Character/Pipeline/Model`. | Moving runtime behavior or gameplay rules into model files. | Old aggregate file is removed or reduced to compatibility only. |
| Input contract | Define external per-frame input snapshot and prediction facts. | Holding input runtime objects or consuming input buffers. | Static tests forbid InputAction/runtime input references. |
| Context contract | Keep pipeline-internal mutable aggregation private to phase execution. | Becoming a public event bus for all domains. | Context mutation call sites remain inside pipeline/runtime assembly area. |
| Submission contract | Carry builder-produced pure data into output composition. | Holding executors, presenters, sinks or Unity scene objects. | Static tests forbid runtime service references in submission. |
| Output contract | Represent output composer result before apply. | Executing movement, animation or logs. | Behavior tests compare output/result fields. |
| Result contract | Expose stable observable frame result for tests/diagnostics. | Triggering side effects or submitting logs. | Result tests compare without requiring scene mutation. |
| Future layer extension | Document that future UpperBody/HitReaction/Aim need their own submission/result proposal. | Preemptively adding unused layer fields. | No future-layer placeholder fields are added in this change. |

## Data Contract Ownership Map
| Type | Owner | Allowed content | Forbidden content |
| --- | --- | --- | --- |
| `CharacterFramePipelineStep` | Pipeline model | Named phase enum/identifier. | Runtime behavior. |
| `CharacterFrameInput` | External tick caller / prediction input assembly. | Delta time, input snapshot, prediction button facts. | InputAction, input component references. |
| `CharacterFrameContext` | `CharacterFramePipeline` only. | Mutable per-phase aggregation and intermediate results. | Public domain event bus, scene objects. |
| `CharacterFrameSubmission` | Submission builders. | State frame, locomotion frame, action motion result, request/facts trace. | Executors, presenters, diagnostic sinks. |
| `CharacterFrameOutput` | Output composer. | Resolved output payload for apply phase. | Direct movement/animation/log execution. |
| `CharacterFrameResult` | Tick/RunPhase caller and tests. | Observed path, phase, result flags, diagnostic summary. | Side-effect methods or mutable service references. |
| `CharacterFrameDiagnosticsSummary` | Diagnostics adapter/tests. | Pure summary strings/ids/traces. | `RuntimeDiagnosticLog.Submit`. |

## User Verification
用户可以通过这些方式确认 change 完成：

- 跑 frame pipeline、rollback/replay 和 simulation tick 相关 EditMode 测试。
- 搜索 `CharacterFramePipelineTypes.cs`，确认它不再是大聚合文件。
- 搜索 `Character/Pipeline/Model`，确认 frame data 类型没有 Unity scene object、InputAction、executor、presenter、diagnostic sink。
- 搜索 `Action/FullBody`，确认没有承载角色级 frame data compat 文件。
- 搜索 future layer 相关字段，确认没有提前塞入未实现的 UpperBody/HitReaction/Aim 数据。
