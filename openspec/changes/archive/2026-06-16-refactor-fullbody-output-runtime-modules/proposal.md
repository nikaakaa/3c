# Change: 拆分 FullBody 输出运行时模块

## Why
`refactor-character-runtime-ports` 已经让 `CharacterFramePipeline` 不再直接接收 `PlayerFullBodyActionController`，但 `FullBodyRuntimePortAdapter` 背后仍把输出副作用转回 `PlayerFullBodyActionController` 的一组 `ForPipeline` 方法。也就是说调用边界已经端口化，真实输出实现职责还没有迁走。

如果继续在 `PlayerFullBodyActionController` 内承载 motion apply、animation presentation、action facts、snapshot writer 和 diagnostics，后续 UpperBody、HitReaction、轻攻击和回滚诊断都会把 FullBody host 重新撑成操作面板。

## What Changes
- 将 FullBody 输出副作用拆为明确的 runtime modules，而不是继续堆在 `PlayerFullBodyActionController`。
- 保持 `CharacterFramePipeline` 只依赖 `ICharacterFrameRuntimePort / IFullBodyOutputRuntimePort`。
- 保持 `FullBodyRuntimePortAdapter` 作为生产装配 adapter，但它应委托给更窄的 output modules。
- 将动作运动执行、动作动画表现、状态快照写入、runtime facts 写入和 diagnostics 提交分成可测试子职责。
- 保持 `PlayerFullBodyActionController` 为唯一 runner owner 和 Unity host，不迁移 runner ownership。
- 保持现有 `IActionMovementExecutor`、Action Animation Presenter 和 Locomotion output port 作为正式执行出口。
- 不新增第二条 motion executor、第二条 animation presenter 或第二条 frame pipeline。

## Non-Goals
- 不改变 Dodge、TurnBack、MoveStart、MoveLoop、MoveStop 的玩法数值。
- 不实现 UpperBody、HitReaction、Aim layer 或轻攻击。
- 不迁移 Locomotion 内部 frame/output 实现；该部分由后续 change 承担。
- 不拆 `CharacterFramePipelineTypes` 数据模型。
- 不改变 timeline facts authority、transition condition evaluator 或 action motion resolver 的已存在 proposal 职责。

## Impact
- Affected specs:
  - `fullbody-action-framework`
  - `unified-character-state-machine`
  - `runtime-diagnostic-logging`
- Affected code:
  - `Assets/Scripts/Character/Action/FullBody/Runtime/FullBodyRuntimePortAdapter.cs`
  - `Assets/Scripts/Character/Action/FullBody/Runtime/PlayerFullBodyActionController.cs`
  - `Assets/Scripts/Character/Pipeline/Runtime/CharacterFramePipeline.cs`
  - `Assets/Scripts/Character/Action/FullBody/Runtime/FullBodyActionTickAdapter.cs`
  - `Assets/Scripts/Character/Action/FullBody/Diagnostics/FullBodyDiagnostics.cs`
  - `Assets/Tests/Editor/Simulation/FullBodyRollbackReplayTests.cs`
  - `Assets/Tests/Editor/UnifiedCharacterStateMachineTests.cs`

## Dependencies
- Must build on `refactor-character-runtime-ports`.
- Should land before `refactor-character-frame-data-contracts`, because frame result fields should not be split while output ownership is still hidden behind the controller.
- Must coordinate with active `refactor-state-action-motion-output`; action motion result remains its responsibility.
- Must not conflict with active `refactor-state-timeline-facts-authority`; this change consumes timeline/result facts but does not redefine them.

## Success Criteria
- `CharacterFramePipeline` still has no direct `PlayerFullBodyActionController` reference.
- `FullBodyRuntimePortAdapter` no longer directly implements every output operation itself; it delegates to narrower output modules.
- `PlayerFullBodyActionController` no longer exposes a growing list of output `ForPipeline` methods as the primary implementation surface.
- Static tests prove only the FullBody host creates `CharacterStateMachineRunner`.
- Behavior tests prove input consume, motion execute, animation presentation, facts write, snapshot update and diagnostics order remain unchanged.

## Detailed Scope Partition
| Area | This change owns | This change must not own | Completion evidence |
| --- | --- | --- | --- |
| FullBody runtime host | Keep `PlayerFullBodyActionController` as Unity host, config/root binder, runner owner and debug field owner. | Moving runner creation into output modules or frame pipeline. | Static test still finds runner creation only in FullBody host. |
| Pipeline port adapter | Keep `FullBodyRuntimePortAdapter` as production adapter for `ICharacterFrameRuntimePort`. | Using adapter as a new large implementation dump. | Adapter methods become one-hop delegation to named modules. |
| Output side effects | Move input consume, motion execution, animation presentation, facts write and snapshot write into explicit modules. | Re-evaluating requests, transitions, timeline windows or action motion distance. | Behavior tests assert the same output order and same observable result. |
| Motion execution | Route all movement through the existing action/basic movement executor interfaces. | Calling `CharacterController.Move` or new executors from FullBody output modules. | Static tests forbid direct movement calls in FullBody output module files. |
| Animation presentation | Route animation requests through the existing animation presenter path. | Calling Animancer or animation components directly from generic frame pipeline code. | Static tests forbid direct animation library calls from pipeline/output core files. |
| Diagnostics | Keep diagnostic submission reachable through an explicit child module until diagnostic adapter work lands. | Designing a new trace model in this change. | Diagnostic calls are localized and named as a temporary output child responsibility. |

## Module Responsibility Split
| Module | Interface exposed to callers | Implementation responsibility | Forbidden dependency |
| --- | --- | --- | --- |
| `FullBodyOutputRuntime` | Execute named output phases for one `CharacterFrameContext`. | Coordinate child modules in pipeline order. | `CharacterStateMachineRunner` mutation, timeline sampling. |
| `FullBodyOutputCacheWriter` | Store last frame outputs for debug/replay. | Update last state/locomotion/action result fields. | Gameplay decision logic. |
| `FullBodyInputRequestConsumer` | Consume frame input requests that were already accepted. | Consume buffered requests at the correct output phase. | Action request validation or priority arbitration. |
| `FullBodyMotionOutputApplier` | Apply resolved action/basic movement commands. | Call existing movement executors and record execution result. | Direct physics or controller movement calls. |
| `FullBodyAnimationOutputPresenter` | Present resolved animation output. | Call existing presenter/locomotion output adapter. | Direct timeline facts recomputation. |
| `FullBodyRuntimeFactsWriter` | Write runtime facts derived from the accepted frame output. | Update action facts, animation facts and completion flags. | Transition condition evaluation. |
| `FullBodySnapshotWriter` | Commit state machine snapshot after outputs. | Persist current snapshot and debug snapshots in stable order. | Creating or restoring runner state. |

## Execution Order Contract
The implementation MUST keep this order explicit and testable:

1. Cache current frame output.
2. Consume accepted input requests.
3. Apply resolved motion output.
4. Present resolved animation output.
5. Write runtime facts.
6. Update state snapshot.
7. Submit diagnostics through the localized diagnostic child module.

Any implementation step that needs to run outside this order MUST stop and become a separate approved proposal, because it means the frame pipeline contract has changed.

## User Verification
用户可以通过这些方式确认 change 完成：

- 跑相关 Unity EditMode 测试，确认 FullBody rollback/replay、state machine 和 frame pipeline 测试通过。
- 搜索 `CharacterFramePipeline`，确认它没有重新依赖 `PlayerFullBodyActionController`。
- 搜索 `PlayerFullBodyActionController` 的 `ForPipeline` 输出方法，确认它不再作为主要输出实现面板继续膨胀。
- 搜索新建 output modules，确认它们只消费 frame/result/facts，不做 request gate、transition 或 timeline facts 采样。
