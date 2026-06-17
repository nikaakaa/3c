# Change: 拆分 Locomotion Frame Runtime 模块

## Why
`ILocomotionFrameRuntimePort` 已经把 FullBody submission builder 和 `PlayerLocomotionController` 的 concrete 类型隔开，但真正的 prepare decision、prepared gameplay decision 和 state decision 到 motion frame 构建仍由 `PlayerLocomotionController` 直接实现。当前 controller 同时承担 Unity host、input source、frame builder facade、camera/facing facts、playback window、runtime blackboard 和 rollback restore，职责仍然过宽。

下一步要把 `ILocomotionFrameRuntimePort` 背后的 frame runtime 实现拆出来，让 controller 只装配依赖并保持兼容入口。

## What Changes
- 将 Locomotion frame prepare/evaluate/build 逻辑从 `PlayerLocomotionController` 背后拆到明确 runtime modules。
- 保持 `ILocomotionFrameRuntimePort` 作为 FullBody submission builder 的唯一 Locomotion frame 入口。
- 将 input intent、spatial facts、phase facts、motion facts、playback window 读取、runtime state apply 拆成明确子职责。
- 保持 camera/facing resolve 作为外围 facts provider，不移入 `LocomotionFrameBuilder`。
- 保持 rollback snapshot capture/restore 语义不变。
- 保持 Locomotion direct tick 为退役/诊断或测试路径，不恢复正式自驱主线。

## Non-Goals
- 不拆 Locomotion output side effects；该部分由 `refactor-locomotion-output-runtime-modules` 承担。
- 不改变 FullBody owner 选择或统一状态机 runner ownership。
- 不新增 Locomotion 状态机。
- 不改变 TurnBack、RunLatch、MoveStart、MoveLoop、MoveStop 行为数值。
- 不拆 `CharacterFramePipelineTypes` 或状态机 model。

## Impact
- Affected specs:
  - `wasd-locomotion-pipeline`
  - `fullbody-action-framework`
  - `unified-character-state-machine`
  - `local-rollback-synctest-foundation`
- Affected code:
  - `Assets/Scripts/Character/Movement/Runtime/PlayerLocomotionController.cs`
  - `Assets/Scripts/Character/Movement/Contracts/ILocomotionFrameRuntimePort.cs`
  - `Assets/Scripts/Character/Movement/Solver/LocomotionFrameBuilder.cs`
  - `Assets/Scripts/Character/Movement/Solver/Facts/*`
  - `Assets/Scripts/Character/Movement/Solver/Motion/*`
  - `Assets/Scripts/Character/Movement/Model/*`
  - `Assets/Tests/Editor/UnifiedCharacterStateMachineTests.cs`
  - `Assets/Tests/Editor/Simulation/FullBodyRollbackReplayTests.cs`

## Dependencies
- Builds on `refactor-character-runtime-ports`.
- Should land before `refactor-locomotion-output-runtime-modules`, because output modules should consume a stable frame runtime state/result boundary.
- Must coordinate with `refactor-state-timeline-facts-authority`; this change consumes current timeline facts but does not define their authority.
- Must coordinate with `refactor-state-action-motion-output`; action motion remains outside Locomotion frame runtime.

## Success Criteria
- FullBody submission builder only sees `ILocomotionFrameRuntimePort`.
- `PlayerLocomotionController` no longer directly contains the full prepare/evaluate/build implementation.
- `LocomotionFrameBuilder` remains pure and does not execute movement or animation.
- camera/facing/reference resolve do not move into pure builder.
- rollback capture/restore tests remain deterministic.

## Detailed Scope Partition
| Area | This change owns | This change must not own | Completion evidence |
| --- | --- | --- | --- |
| Frame prepare | Build locomotion input intent, prepare facts, phase facts and spatial facts for the current frame. | Consuming input requests or presenting animation. | Tests compare prepared facts before and after migration. |
| Frame evaluate | Convert prepared facts into gameplay decision and state decision inputs. | Running FullBody transition evaluator or action request gate. | Tests assert same decision result for idle, move, run latch and TurnBack cases. |
| Motion frame build | Call pure `LocomotionFrameBuilder` with stable data inputs. | Executing motion or writing final animation output. | Static tests keep builder free of Unity side effects. |
| Runtime state | Own current locomotion intent, previous direction, run latch, gait memory, pending TurnBack intent and phase time. | Owning FullBody state machine snapshot or action state. | Snapshot/restore tests prove state equivalence. |
| Spatial facts | Resolve camera/facing/reference facts through narrow providers. | Moving Transform/Camera dependencies into pure solver. | Provider tests use fake basis inputs and compare vectors. |
| Controller role | Keep `PlayerLocomotionController` as Unity host and adapter owner. | Letting controller remain the implementation of all frame runtime behavior. | Controller methods delegate to runtime module; no new broad methods appear. |

## Runtime Data Flow
The intended flow after this change is:

```text
FullBodySubmissionBuilder
  -> ILocomotionFrameRuntimePort
       -> LocomotionFrameRuntimeAdapter
            -> LocomotionFrameRuntime
                 -> LocomotionRuntimeStateStore
                 -> LocomotionPrepareFactsProvider
                 -> LocomotionSpatialFactsProvider
                 -> LocomotionMotionFactsProvider
                 -> LocomotionFrameBuilder
```

The flow MUST remain submission-oriented. Locomotion frame runtime prepares information for the single character frame pipeline; it must not become a second tick pipeline.

## Formal Ownership Rules
- `LocomotionFrameRuntime` owns frame preparation order.
- `LocomotionRuntimeStateStore` owns restorable local Locomotion state.
- `LocomotionFrameBuilder` owns pure data construction only.
- `PlayerLocomotionController` owns Unity references, inspector config binding and adapter lifetime.
- `FullBodySubmissionBuilder` owns when Locomotion is asked to submit data to the character frame.
- No module in this change owns movement execution, animation presentation or state machine runner advancement.

## User Verification
用户可以通过这些方式确认 change 完成：

- 跑 Locomotion decision/motion frame 相关 EditMode 测试，确认 idle/move/run/TurnBack 输出一致。
- 跑 rollback/replay 测试，确认 capture/restore 后同输入序列稳定。
- 搜索 `FullBodySubmissionBuilder`，确认它只依赖 `ILocomotionFrameRuntimePort`。
- 搜索 `LocomotionFrameBuilder`，确认它仍不引用 Unity runtime 类型、motion executor 或 animation presenter。
- 搜索 `PlayerLocomotionController`，确认 frame prepare/evaluate/build 方法不再承载完整实现。
