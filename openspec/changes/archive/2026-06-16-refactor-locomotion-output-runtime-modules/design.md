## Context
Locomotion output currently includes:

- `ExecuteLocomotionMotion`
- `PresentLocomotionAnimation`
- `WriteActionFacts`
- `WriteAnimationFacts`
- `CompleteLocomotionTick`
- camera resolve and rollback camera basis sync
- run latch reset after idle

These are output/apply responsibilities, not frame prepare/build responsibilities. They should be behind `ILocomotionOutputRuntimePort` and split into modules that can be tested independently.

## Goals
- Make Locomotion output runtime separate from frame runtime.
- Keep motion executor the only basic movement execution outlet.
- Keep animation presenter read-only with respect to gameplay state.
- Keep runtime blackboard writes explicit and ordered.
- Keep camera resolve as output-complete behavior, not frame prepare behavior.
- Reduce `PlayerLocomotionController` to Unity host and module composition.

## Non-Goals
- No new root motion authority.
- No direct `CharacterController.Move` outside motion executor.
- No direct Animancer state decision in Locomotion output runtime.
- No change to input source or direct tick retirement behavior.

## Proposed Shape
建议目录：

```text
Character/Movement/Runtime/
  LocomotionOutputRuntimeAdapter.cs
  LocomotionMotionOutputApplier.cs
  LocomotionAnimationOutputPresenter.cs
  LocomotionRuntimeBlackboardWriter.cs
  LocomotionOutputCompletion.cs
```

职责建议：

- `LocomotionMotionOutputApplier`: 执行 `MovementCommand`，只通过 `IBasicLocomotionMotionExecutor`。
- `LocomotionAnimationOutputPresenter`: 构建并提交 `MovementAnimationContext`。
- `LocomotionRuntimeBlackboardWriter`: 写 action facts、animation facts、locomotion facts。
- `LocomotionOutputCompletion`: camera resolve、rollback basis sync、run latch idle reset。
- `LocomotionOutputRuntimeAdapter`: 实现或支撑 `ILocomotionOutputRuntimePort`。

## Decisions

### Decision: Output module 不接触状态机 runner
Locomotion output runtime 不允许访问 `CharacterStateMachineRunner`。

理由：状态选择已经完成，output 只消费 frame/result。

### Decision: Camera complete tick 留在 Locomotion output
`CompleteLocomotionTick` 仍属于 Locomotion output，因为它同步 camera basis 和 reset run latch，是本帧 output 结束后的外围状态维护。

理由：把 camera resolve 放进 frame prepare 会污染 facts 构建时机，也会影响 rollback basis。

### Decision: Blackboard writer 独立
runtime blackboard 写入从 controller 移入 writer，但 blackboard storage 可第一阶段仍由 controller 或 state store 持有。

理由：先拆写入职责，再决定存储所有权，风险更低。

## Migration Plan
1. 先用现有 rollback/blackboard tests 锁住 facts 写入。
2. 抽出 motion output applier。
3. 抽出 animation output presenter。
4. 抽出 runtime blackboard writer。
5. 抽出 output completion。
6. 让 controller 的 `ILocomotionOutputRuntimePort` 方法委托给 output runtime adapter。
7. 用静态测试确认没有新增 motion/animation 分裂出口。

## Risks / Trade-offs
- Risk: camera resolve 时机被移动导致回滚差异。
  - Mitigation: rollback camera basis tests 必须覆盖。
- Risk: blackboard writer 拆出后 facts source step 改变。
  - Mitigation: action/animation facts source step tests 必须覆盖。
- Risk: output runtime 误读 frame builder state。
  - Mitigation: output module 只接受 frame/output 参数和 narrow dependencies。

## Open Questions
- runtime blackboard storage 是否在本 change 一并迁出 controller？
- `CompleteLocomotionTick` 是否应拆为 camera sync 和 latch reset 两个 module？
- output runtime 是否需要返回 result 供 diagnostics adapter 使用？

## Interface Details
### `LocomotionOutputRuntimeAdapter`
- Interface: satisfies `ILocomotionOutputRuntimePort` for FullBody output callers.
- Invariant: all incoming data has already been prepared by Locomotion frame runtime and selected by FullBody frame pipeline.
- Output: delegates to output modules without adding gameplay rules.
- Forbidden: frame prepare/evaluate/build, input sampling and state machine transition evaluation.
- Test surface: fake modules verify delegation and phase order.

### `LocomotionMotionOutputApplier`
- Interface: apply a resolved locomotion movement command.
- Invariant: command has already been built and accepted for this frame.
- Output: delegates to `IBasicLocomotionMotionExecutor`.
- Forbidden: direct movement primitive, path around motion executor, gait selection.
- Test surface: fake executor records command payload and call count.

### `LocomotionAnimationOutputPresenter`
- Interface: present a resolved locomotion animation context.
- Invariant: logical state is already selected before this module runs.
- Output: calls existing locomotion animation presenter path.
- Forbidden: transition exit checks, direct Animancer state authority, gameplay state mutation.
- Test surface: fake presenter records context and verifies ordering after motion.

### `LocomotionRuntimeBlackboardWriter`
- Interface: write runtime facts from current output/result.
- Invariant: source step and frame identity come from upstream frame result.
- Output: writes action facts, animation facts and locomotion facts into current blackboard storage.
- Forbidden: choosing new facts by sampling live input or reading future state.
- Test surface: blackboard fixture compares source step and values.

### `LocomotionOutputCompletion`
- Interface: finish non-gameplay maintenance after output.
- Invariant: output for the frame has already been applied/presented.
- Output: camera basis sync, rollback basis sync and run latch idle reset.
- Forbidden: direct tick gameplay advancement.
- Test surface: rollback basis and idle reset tests.

## Implementation Phasing
1. Add tests for current output call order and facts values.
2. Extract motion applier first because it has the clearest executor Interface.
3. Extract animation presenter wrapper next and keep context payload identical.
4. Extract blackboard writer with source-step tests.
5. Extract completion module and lock camera/rollback basis behavior.
6. Make controller delegate all `ILocomotionOutputRuntimePort` methods to adapter/runtime.
7. Remove obsolete output methods after static dependency checks pass.

## Stop Conditions
- Stop if output runtime needs to call `TryPrepareDecisionFrame`.
- Stop if output runtime needs to read raw InputAction.
- Stop if motion apply cannot go through the existing motion executor.
- Stop if animation presentation requires logical state selection.
- Stop if complete tick starts to advance gameplay outside the unified frame pipeline.

## Validation Evidence
- Static tests for no direct movement/input/runner dependencies.
- Behavior tests for executor/presenter call counts.
- Blackboard source-step tests.
- Rollback camera basis tests.
- `openspec validate refactor-locomotion-output-runtime-modules --strict --no-interactive`.
