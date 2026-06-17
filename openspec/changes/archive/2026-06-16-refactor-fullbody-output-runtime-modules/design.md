## Context
当前主线已经是：

```text
PlayerFullBodyActionController / FullBodyActionTickAdapter
  -> FullBodyRuntimePortAdapter
  -> CharacterFramePipeline
  -> FullBodySubmissionBuilder
  -> CharacterFrameOutputApplier
```

这解决了 pipeline 不接收 concrete controller 的问题。但 `FullBodyRuntimePortAdapter` 仍把 `SetLastFrameOutputs`、`ConsumeStateFrameInputRequest`、`ExecuteStateFrameMotion`、`PresentStateFrameAnimation`、`WriteStateFrameActionFacts`、`UpdateStateSnapshot`、`WriteAnimationRuntimeFacts`、`CompleteLocomotionTick`、`LogDiagnosticTickSnapshots` 转发回 `PlayerFullBodyActionController`。

这不是行为 bug，而是职责迁移未完成：端口只是外壳，真实输出实现仍在 MonoBehaviour host 中。

## Goals
- 让 FullBody output apply 成为独立 runtime module 组合。
- 将 `PlayerFullBodyActionController` 收窄为 Unity host、runner owner、配置/root binding 装配者。
- 保持 `CharacterFramePipeline` 的 phase 顺序不变。
- 保持 motion executor、animation presenter、Locomotion output port 作为正式输出出口。
- 保持 snapshot/diagnostics 的相对顺序：motion/presentation 之后写 snapshot 和日志。
- 让 output modules 可以用 fake port/fake dependencies 在 EditMode 中直接测试。

## Non-Goals
- 不让 output module 创建或恢复 `CharacterStateMachineRunner`。
- 不让 output module 采样 timeline facts。
- 不让 output module 重算 action motion result。
- 不把 Locomotion camera/facing/reference resolve 移入 FullBody output。
- 不把 diagnostics trace 模型完全独立化；该工作由 `refactor-character-diagnostic-adapters` 处理。

## Proposed Shape
建议第一阶段采用这些模块名，实施时可按现有目录微调：

```text
Character/Action/FullBody/Runtime/
  FullBodyRuntimePortAdapter.cs
  FullBodyOutputRuntime.cs
  FullBodyOutputCacheWriter.cs
  FullBodyInputRequestConsumer.cs
  FullBodyMotionOutputApplier.cs
  FullBodyAnimationOutputPresenter.cs
  FullBodyRuntimeFactsWriter.cs
  FullBodySnapshotWriter.cs
```

`FullBodyRuntimePortAdapter` 仍实现 `ICharacterFrameRuntimePort`，但它只做装配和委托：

```text
FullBodyRuntimePortAdapter
  -> FullBodyOutputRuntime
       -> FullBodyInputRequestConsumer
       -> FullBodyMotionOutputApplier
       -> FullBodyAnimationOutputPresenter
       -> FullBodyRuntimeFactsWriter
       -> FullBodySnapshotWriter
```

## Decisions

### Decision: Adapter 仍存在，但不再是实现聚合点
`FullBodyRuntimePortAdapter` 继续作为 pipeline 看到的生产 port。它可以持有 controller，但不应继续包含所有输出细节。

理由：pipeline 已经依赖端口，直接移除 adapter 会把迁移范围扩大到 tick adapter、rollback fixture 和测试装配。先让 adapter 委托更窄模块，可以保持行为稳定。

### Decision: Runner ownership 留在 FullBody host
`PlayerFullBodyActionController` 继续创建和持有唯一 `CharacterStateMachineRunner`。

理由：这是当前 specs 的核心约束。输出拆分只处理 frame result 的副作用执行，不改变状态权威。

### Decision: Output module 不做 gameplay 决策
Output module 只消费 `CharacterFrameOutput / CharacterStateMachineFrame / ActionMotionResolveResult`，不得重新评估请求、transition、timeline window 或 action motion distance。

理由：否则 output layer 会成为第二个 gameplay solver。

### Decision: Locomotion output 经现有端口
FullBody output module 可以调用 Locomotion output port 或现有 controller 暂时委托，但不得绕过当前 motion executor/presenter。

理由：Locomotion output 自身还有后续拆分 proposal，本阶段只拆 FullBody host 背后的 output 实现。

## Migration Plan
1. 先加 characterization/static tests，锁住当前 output order。
2. 新建 FullBody output runtime module，并让 adapter 委托它。
3. 将 controller 中 output `ForPipeline` 方法逐个改为薄转发或删除。
4. 保留 public/debug 属性如 `LastStateFrame`、`LastLocomotionFrame`、`LastActionMotionResult`。
5. 确认 rollback/replay fixture 不需要新路径。
6. 清理 adapter 中重复 null handling，统一由 output module 返回明确结果。

## Risks / Trade-offs
- Risk: 模块太多但没有替换点。
  - Mitigation: 每个 module 必须被 production adapter 和至少一个 EditMode fake/characterization test 使用。
- Risk: Snapshot writer 拆出后顺序变动。
  - Mitigation: 行为测试明确验证 snapshot update 晚于 motion 和 presentation。
- Risk: Controller 变薄时丢失 debug 字段。
  - Mitigation: 保留字段所有权或通过只读 snapshot 暴露，不改 Inspector/debug 语义。
- Risk: 和 diagnostics adapter proposal 重叠。
  - Mitigation: 本 change 只把 diagnostics 调用移动到 output runtime 的明确子职责，不重定义 trace 模型。

## Open Questions
- `LastFramePipelineResult` 是否继续在 `PlayerFullBodyActionController` 存储，还是未来进入 frame history/debug adapter？
- `FullBodyOutputRuntime` 是否需要一个 aggregate result 类型，还是沿用 `CharacterFrameContext` 标记即可？
- `FullBodyInputRequestConsumer` 是否应保留在 FullBody output，还是未来跟 input buffer adapter 合并？

## Interface Details
### `FullBodyOutputRuntime`
- Interface: caller provides a complete frame context, resolved state machine frame, resolved action motion result and runtime ports.
- Invariant: the frame has already passed request arbitration, transition evaluation and action motion resolution.
- Output: child modules execute side effects and update the context/result fields already owned by the frame pipeline.
- Error mode: missing required production dependency should fail fast during construction or explicit initialization, not silently skip output.
- Test surface: fake child modules record ordered calls for phase-order tests.

### `FullBodyMotionOutputApplier`
- Interface: caller provides resolved movement output only.
- Invariant: movement distance, direction and completion state are already resolved.
- Output: delegates to existing movement executor and returns or writes execution facts.
- Forbidden: it must not sample timeline windows, derive action variants or call Unity movement primitives directly.
- Test surface: fake action movement executor verifies command payload and call count.

### `FullBodyAnimationOutputPresenter`
- Interface: caller provides resolved animation request/presentation data.
- Invariant: selected clip/state/variant was already chosen before presentation.
- Output: delegates to current animation presenter path and records presentation facts.
- Forbidden: it must not decide whether an action can exit or whether a locomotion animation can exit.
- Test surface: fake presenter verifies animation request identity and ordering after motion.

### `FullBodyRuntimeFactsWriter`
- Interface: caller provides accepted frame output and post-motion execution result.
- Invariant: facts written here are observations, not decisions.
- Output: updates action facts, animation runtime facts and completion facts used by later frames.
- Forbidden: it must not call transition evaluator or mutate graph definition.
- Test surface: facts fixture asserts expected values for Dodge, TurnBack and idle/move frames.

### `FullBodySnapshotWriter`
- Interface: caller provides runner-produced snapshot and frame diagnostics summary.
- Invariant: snapshot commit is the final state identity write for the frame.
- Output: updates current snapshot/debug fields.
- Forbidden: it must not create, restore or advance the runner.
- Test surface: rollback fixture compares pre/post snapshot identities and state time.

## Implementation Phasing
1. Add tests that describe current behavior without moving code.
2. Create module classes with production dependencies injected through constructors or explicit init methods.
3. Move one output responsibility at a time from controller to module.
4. After each move, keep adapter delegation stable and run focused tests.
5. Remove or internalize obsolete controller methods only after all call sites use modules.
6. Leave diagnostics trace model unchanged until `refactor-character-diagnostic-adapters`.

## Stop Conditions
- Stop if a proposed module needs to know `CharacterStateMachineDefinition` to do its job.
- Stop if output logic needs to call request gate or transition evaluator.
- Stop if a new movement executor or animation presenter path appears.
- Stop if a module cannot be tested without a full `PlayerFullBodyActionController`.
- Stop if keeping behavior requires changing timeline facts authority.

## Validation Evidence
- Static dependency tests for pipeline/controller references.
- EditMode behavior tests for output phase order.
- Replay/rollback tests for snapshot and action facts stability.
- Build validation for runtime and editor assemblies.
- `openspec validate refactor-fullbody-output-runtime-modules --strict --no-interactive`.
