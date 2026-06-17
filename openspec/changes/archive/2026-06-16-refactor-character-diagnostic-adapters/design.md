## Context
当前项目已有统一日志出口：

```text
FullBodyDiagnostics / LocomotionDiagnostics
  -> RuntimeDiagnosticLog.Submit
  -> RuntimeDiagnosticLog.Filter
  -> Unity Console / tests
```

问题不是没有统一出口，而是提交时机和提交者仍混在 runtime 核心附近。更稳的分法是：

```text
Runtime core produces trace
  -> Diagnostic adapter formats event
  -> RuntimeDiagnosticLog.Submit
```

## Goals
- 让 runner、pipeline、condition evaluator、timeline sampler 只产生 trace。
- 让 diagnostic adapter 统一提交 runtime diagnostic events。
- 让 tests 可以用 fake sink 检查 trace/event，而不是依赖 Unity Console。
- 保留 `RuntimeDiagnosticLog` filter/channel/macro 行为。
- 保留现有 event id，避免调试工具失效。

## Non-Goals
- 不取消 `FullBodyDiagnostics` / `LocomotionDiagnostics` 名称；可迁移为 adapter/facade。
- 不改变日志输出格式到无法搜索旧 key。
- 不要求一次迁移所有旧工具日志。
- 不把 manual debug log 纳入 gameplay 验收任务。

## Proposed Shape
建议目录：

```text
Character/Diagnostics/
  CharacterFrameDiagnosticTrace.cs
  ICharacterDiagnosticSink.cs
  RuntimeDiagnosticLogCharacterSink.cs

Character/Action/FullBody/Diagnostics/
  FullBodyDiagnosticAdapter.cs
  FullBodyDiagnosticEventFormatter.cs

Character/Movement/Diagnostics/
  LocomotionDiagnosticAdapter.cs
```

核心思路：

- `CharacterFramePipeline` 可以产生 `CharacterFrameDiagnosticTrace`。
- `CharacterStateMachineFrame` 可以携带 condition/timeline trace。
- `FullBodyOutputRuntime` 或 adapter 在 phase 末尾调用 diagnostic adapter。
- adapter 将 trace 格式化为 `RuntimeDiagnosticLogEvent`。

## Decisions

### Decision: 保留现有 event id
迁移不得删除 `fullbody-path-changed`、`locomotion-phase-changed`、`action-accepted` 等现有 key。

理由：用户和测试已经依赖这些 key 搜索链路。

### Decision: Diagnostic adapter 不影响 gameplay
diagnostic sink 失败、过滤关闭或宏关闭，不得改变状态机、motion、animation、input consume 结果。

理由：日志只能观察，不能成为控制流。

### Decision: Trace 是纯数据
trace 不允许保存 MonoBehaviour、Transform、Animancer state、CharacterController 或 InputAction。

理由：后续 rollback/replay 和测试需要纯数据可比较。

## Migration Plan
1. 先加静态测试找出直接 `RuntimeDiagnosticLog.Submit` 调用范围。
2. 定义 diagnostic trace/sink 最小接口。
3. 将 FullBody path/pending/action accepted 日志迁到 adapter。
4. 将 timeline facts trace 日志迁到 adapter。
5. 将 condition trace 日志迁到 adapter。
6. 将 Locomotion tick summary/TurnBack 关键日志接入 adapter 或保留明确 facade。
7. 用过滤器测试证明日志开关不改变行为。

## Risks / Trade-offs
- Risk: 迁移后日志顺序变化。
  - Mitigation: tests 只锁关键 event 相对顺序，不依赖不必要的噪音。
- Risk: adapter 太宽，重新成为日志大类。
  - Mitigation: 按 trace 类型拆 formatter，sink 只负责提交。
- Risk: 旧 diagnostics facade 和新 adapter 并存太久。
  - Mitigation: 静态测试禁止 core 模块直接提交，允许 facade 只做 adapter 包装。

## Open Questions
- `FullBodyDiagnostics` 是否保留为 public facade，还是完全替换为 adapter？
- condition trace 是否跟 timeline trace 共用同一个 frame trace type？
- diagnostics adapter 是否应进入 `ICharacterFrameRuntimePort`，还是由 output runtime 持有？

## Interface Details
### `CharacterFrameDiagnosticTrace`
- Interface: pure data record describing frame phase, state identity, transition probe, timeline facts and output summary.
- Invariant: trace can be compared in EditMode tests without Unity scene objects.
- Output: consumed by formatter/adapter only.
- Forbidden: storing `MonoBehaviour`, `Transform`, `CharacterController`, Animancer state or InputAction.
- Test surface: serialization/value comparison style tests and static forbidden-reference tests.

### `ICharacterDiagnosticSink`
- Interface: accepts formatted diagnostic events or normalized diagnostic records.
- Invariant: sink failure or filter closure cannot affect gameplay.
- Output: production sink submits to `RuntimeDiagnosticLog`; fake sink records values.
- Forbidden: state mutation in runner, frame pipeline or output modules.
- Test surface: fake sink verifies calls without Unity Console dependency.

### `RuntimeDiagnosticLogCharacterSink`
- Interface: production adapter from character diagnostic events to `RuntimeDiagnosticLog.Submit`.
- Invariant: preserves existing event id and channel key names.
- Output: calls the existing logging outlet.
- Forbidden: evaluating gameplay conditions.
- Test surface: event id presence tests and filter tests.

### `FullBodyDiagnosticAdapter`
- Interface: accepts FullBody frame/path/action/timeline/condition traces.
- Invariant: adapter formats, it does not compute state transitions.
- Output: formatted diagnostic events through sink.
- Forbidden: reading live input or advancing state machine runner.
- Test surface: fake trace to event mapping tests.

### `LocomotionDiagnosticAdapter`
- Interface: accepts Locomotion phase/TurnBack/output summary traces.
- Invariant: adapter observes Locomotion facts already produced elsewhere.
- Output: formatted diagnostic events through sink.
- Forbidden: motion execution, animation presentation or frame decision building.
- Test surface: fake trace to event mapping tests.

## Implementation Phasing
1. Inventory current `RuntimeDiagnosticLog.Submit` calls and event ids.
2. Add static tests that express where direct submit is forbidden.
3. Introduce trace and sink interfaces with fake sink tests.
4. Migrate FullBody event families one by one.
5. Migrate timeline and condition probe events after their active proposals expose trace data.
6. Migrate Locomotion event families or wrap existing facade through adapter.
7. Remove duplicate direct submit calls and keep only production sink as submit owner.

## Stop Conditions
- Stop if trace needs to hold a live Unity object.
- Stop if logging filter state is read by gameplay code.
- Stop if event id rename is needed without explicit compatibility mapping.
- Stop if adapter starts sampling timeline facts itself.
- Stop if a fake sink cannot observe an event family in tests.

## Validation Evidence
- Direct-submit static tests.
- Trace forbidden-reference tests.
- Event-id preservation tests.
- Diagnostics on/off behavior equivalence tests.
- `openspec validate refactor-character-diagnostic-adapters --strict --no-interactive`.
