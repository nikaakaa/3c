## Context
当前 `CharacterFramePipelineTypes` 同时包含：

- `CharacterFramePipelineStep`
- `CharacterFrameInput`
- `CharacterFrameContext`
- `CharacterFrameSubmission`
- `CharacterFrameOutput`
- `CharacterFrameResult`

这些类型都属于 frame pipeline，但变化原因不同。input 会受预测/回滚输入影响；submission 会受 FullBody/Locomotion/Action 输出组合影响；result 会受 diagnostics 和 tests 影响；context 是 pipeline 内部 mutable state。放在一个文件里会掩盖职责边界。

## Goals
- 拆分 frame data 文件。
- 明确 mutable context 只供 pipeline 内部使用。
- 明确 submission 是跨 builder/output 的纯数据合同。
- 明确 output 是 apply 阶段消费的结果包装。
- 明确 result 是外部观测和测试合同。
- 防止 future layer 把 scene object 或 executor 直接塞进 frame data。

## Non-Goals
- 不改变 `CharacterFramePipeline` 的 public phase API。
- 不新增 layer submission。
- 不实现 frame event bus。
- 不添加 fallback config。

## Proposed Shape
建议目录：

```text
Character/Pipeline/Model/
  CharacterFramePipelineStep.cs
  CharacterFrameInput.cs
  CharacterFrameContext.cs
  CharacterFrameSubmission.cs
  CharacterFrameOutput.cs
  CharacterFrameResult.cs
  CharacterFrameDiagnosticsSummary.cs
```

`Character/Pipeline/Runtime/CharacterFramePipeline.cs` 继续作为唯一角色级 phase owner；`Action/FullBody` 只保留 FullBody submission builder、runtime port adapter、provider 和领域 resolver，不承载角色级 pipeline data。

职责：

- `Input`: 外部输入快照和 prediction button facts。
- `Context`: pipeline 内部 mutable aggregation。
- `Submission`: builder 产出的 frame submission。
- `Output`: output composer 结果。
- `Result`: Tick/RunPhase 对外可观察结果。
- `DiagnosticsSummary`: 纯字符串/trace 摘要，不提交日志。

## Decisions

### Decision: Context 保持内部可变
不强行把 `CharacterFrameContext` 改成不可变值对象。

理由：pipeline phase 需要逐步聚合结果；本 change 是数据契约拆分，不是函数式重写。

### Decision: Submission 不做副作用
`CharacterFrameSubmission` 不允许持有 executor/presenter/input component。

理由：submission 是 output apply 的输入，不是 runtime service locator。

### Decision: Future layer 通过独立 submission 扩展
UpperBody/HitReaction/Aim 未来不能直接把字段塞进 FullBody submission；应先新增独立 layer submission/result proposal。

理由：否则 frame context 会变成总线对象。

## Migration Plan
1. 先加静态测试锁住禁止类型。
2. 在 `Assets/Scripts/Character/Pipeline/Model/` 下逐个移动 type 到新文件，保持 namespace 和 public API。
3. 删除或瘦身 `CharacterFramePipelineTypes.cs`，但不得把 compat/index 文件留回 `Action/FullBody`。
4. 更新 csproj 显式 compile include。
5. 确认 tests、rollback comparison、diagnostic summary 不变。

## Risks / Trade-offs
- Risk: 文件拆分造成 Unity csproj include 漏项。
  - Mitigation: dotnet build 和 Unity EditMode 编译覆盖。
- Risk: 拆分时顺手改行为。
  - Mitigation: 不改字段语义，不改 phase 顺序。
- Risk: active proposals 同时改 frame fields。
  - Mitigation: 在 implementation 前重新读取 timeline/action motion active changes。

## Open Questions
- `CharacterFrameResult.DiagnosticSummary` 是否应拆成结构化 summary？
- `CharacterFrameContext` 是否应保持 public struct，还是收窄到 internal？
- future layer submission 是否应在角色级 `CharacterFrameSubmission` 中作为 list/registry，还是独立 pipeline result？

## Interface Details
### `CharacterFrameInput`
- Interface: immutable or effectively immutable frame input passed into pipeline entry.
- Invariant: represents the frame caller's input view, not live input source.
- Allowed: delta time, prediction button facts, frame input snapshot ids.
- Forbidden: `InputAction`, input component, scene object references.
- Test surface: forbidden-reference static tests and prediction input equality tests.

### `CharacterFrameContext`
- Interface: pipeline-internal mutable aggregation object.
- Invariant: only pipeline phases mutate it during one frame.
- Allowed: intermediate submission/output/result values and phase status.
- Forbidden: public service locator fields, future layer bus fields, direct output side effects.
- Test surface: phase tests assert context mutation order through public pipeline result.

### `CharacterFrameSubmission`
- Interface: pure submission from builders into output composition.
- Invariant: all values were produced before output apply.
- Allowed: state frame, locomotion frame, action motion result, request facts and trace data.
- Forbidden: motion executor, animation presenter, input buffer, diagnostic sink, Unity objects.
- Test surface: static forbidden-dependency tests and submission equality tests.

### `CharacterFrameOutput`
- Interface: resolved output payload ready for runtime ports.
- Invariant: output represents what to apply, not how to apply it.
- Allowed: selected motion/animation/facts payloads and apply flags.
- Forbidden: direct execution methods or fallback config.
- Test surface: output composition tests.

### `CharacterFrameResult`
- Interface: observable result returned to tick/RunPhase callers and tests.
- Invariant: result is read-only observation after pipeline execution.
- Allowed: active path, owner/phase summary, applied flags, diagnostic summary.
- Forbidden: log submit, movement apply, animation play.
- Test surface: rollback/replay and diagnostic summary comparison tests.

### `CharacterFrameDiagnosticsSummary`
- Interface: pure diagnostic summary carried by result or trace.
- Invariant: summary does not submit itself.
- Allowed: ids, keys, concise strings, trace references that are pure data.
- Forbidden: `RuntimeDiagnosticLog`, sink references, Unity Console calls.
- Test surface: diagnostic summary stability tests.

## Implementation Phasing
1. Add forbidden-reference static tests before moving types.
2. Move one type per file and keep namespace/API stable.
3. Build after every small group of moves to catch csproj include issues.
4. Update tests/imports without changing behavior.
5. Remove or slim `CharacterFramePipelineTypes.cs`.
6. Add future-layer guard tests so new layer fields are not added prematurely.

## Stop Conditions
- Stop if a data type needs to hold an executor, presenter, sink or scene object.
- Stop if split requires changing pipeline phase order.
- Stop if future UpperBody/HitReaction/Aim fields seem necessary.
- Stop if `CharacterFrameContext` becomes a public cross-domain bus.
- Stop if diagnostics summary needs to submit logs directly.

## Validation Evidence
- Forbidden-reference static tests.
- Frame result equality tests.
- Rollback/replay comparison tests.
- Runtime/editor build validation.
- `openspec validate refactor-character-frame-data-contracts --strict --no-interactive`.
