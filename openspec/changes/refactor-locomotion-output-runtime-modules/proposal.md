# Change: 拆分 Locomotion Output Runtime 模块

## Why
`ILocomotionOutputRuntimePort` 已经把 Locomotion 输出能力和 frame prepare/build 能力分开，但 `PlayerLocomotionController` 仍直接实现 motion execution、animation presentation、runtime blackboard facts 写入、camera resolve、rollback camera basis sync 和 run latch reset。这样 frame runtime 拆完后，output side effects 仍会把 controller 留成大类。

本阶段要把 Locomotion output/apply 从 controller 中拆为明确模块，使 Locomotion frame runtime 和 output runtime 真正分离。

## What Changes
- 将基础移动 motion apply、locomotion animation presentation、runtime action facts 写入、animation facts 写入、complete tick/camera sync 拆成明确 output runtime modules。
- 保持 `ILocomotionOutputRuntimePort` 作为 FullBody output 层访问 Locomotion output 的唯一入口。
- 保持 motion executor 为唯一基础移动位移出口。
- 保持 animation presenter 只消费 animation context，不决定逻辑状态。
- 保持 camera resolve 只在 output complete tick 阶段发生。
- 保持 runtime blackboard facts 写入顺序和 rollback snapshot 语义。

## Non-Goals
- 不拆 Locomotion frame prepare/evaluate/build；该部分由 `refactor-locomotion-frame-runtime-modules` 承担。
- 不改变 `CharacterMotionDriver` 或基础移动运动命令数值。
- 不改变 Animancer presenter 配置解析。
- 不改变 TurnBack motion policy 或 animation motion source。
- 不新增第二个 Locomotion gameplay driver。

## Impact
- Affected specs:
  - `wasd-locomotion-pipeline`
  - `basic-locomotion-animation`
  - `character-runtime-blackboard`
  - `fullbody-action-framework`
- Affected code:
  - `Assets/Scripts/Character/Movement/Runtime/PlayerLocomotionController.cs`
  - `Assets/Scripts/Character/Movement/Contracts/ILocomotionOutputRuntimePort.cs`
  - `Assets/Scripts/Character/Movement/Runtime/CharacterMotionDriver.cs`
  - `Assets/Scripts/Character/Animation/Runtime/BasicLocomotionAnimancerPresenter.cs`
  - `Assets/Scripts/Character/StateMachine/Model/CharacterRuntimeBlackboard.cs`
  - `Assets/Tests/Editor/UnifiedCharacterStateMachineTests.cs`
  - `Assets/Tests/Editor/Simulation/FullBodyRollbackReplayTests.cs`

## Dependencies
- Should run after `refactor-locomotion-frame-runtime-modules`.
- Must build on `refactor-character-runtime-ports`.
- Must coordinate with `refactor-fullbody-output-runtime-modules`, because FullBody output calls Locomotion output port.
- Must not redefine action motion result; that belongs to active `refactor-state-action-motion-output`.

## Success Criteria
- `PlayerLocomotionController` no longer directly owns all output side effects.
- output modules do not read transition config or select state.
- motion execution still uses `IBasicLocomotionMotionExecutor`.
- animation presentation still uses locomotion presenter context.
- runtime blackboard facts remain deterministic in rollback/replay tests.

## Detailed Scope Partition
| Area | This change owns | This change must not own | Completion evidence |
| --- | --- | --- | --- |
| Motion output | Apply already-built basic locomotion motion commands through the formal executor. | Building motion decisions, sampling input, direct `CharacterController.Move`. | Static and behavior tests prove executor-only movement. |
| Animation output | Present already-built locomotion animation context. | Choosing logical state, evaluating transition exit windows, direct Animancer state authority. | Presenter call payload and timing tests remain stable. |
| Runtime facts | Write action, animation and locomotion facts derived from the current frame/result. | Recomputing frame decisions or changing blackboard schema. | Facts source-step tests and rollback tests pass. |
| Output completion | Synchronize camera basis, rollback basis and run latch reset after output. | Starting direct gameplay tick or reading fresh input. | Direct tick remains retired/non-authoritative. |
| Controller role | Keep serialized references, Unity object lifetime and adapter construction. | Continuing as the place where every output rule lives. | Controller output methods become thin delegation or disappear. |
| FullBody integration | Keep FullBody output calling `ILocomotionOutputRuntimePort`. | FullBody output directly manipulating Locomotion controller internals. | Static dependency tests keep FullBody on the port. |

## Output Phase Contract
The output runtime MUST keep these phases separately named and separately testable:

1. Apply locomotion motion command.
2. Present locomotion animation context.
3. Write runtime blackboard facts.
4. Complete output tick side maintenance.

If any implementation needs a fifth phase, it must be named in the code and covered by a test before it becomes production behavior.

## Formal Ownership Rules
- `LocomotionMotionOutputApplier` owns basic locomotion motion apply only.
- `LocomotionAnimationOutputPresenter` owns animation presentation only.
- `LocomotionRuntimeBlackboardWriter` owns runtime facts writes only.
- `LocomotionOutputCompletion` owns post-output camera/basis/latch maintenance only.
- `LocomotionOutputRuntimeAdapter` owns `ILocomotionOutputRuntimePort` production wiring only.
- No output module owns Locomotion frame preparation or FullBody state transition decisions.

## User Verification
用户可以通过这些方式确认 change 完成：

- 跑 Locomotion output、blackboard facts 和 rollback camera basis 相关 EditMode 测试。
- 搜索 output modules，确认没有直接调用 `CharacterController.Move`。
- 搜索 output modules，确认没有直接读取 InputAction 或状态机 transition 配置。
- 搜索 `PlayerLocomotionController`，确认 motion/animation/facts/complete tick 不再集中在 controller 里。
- 搜索 FullBody output，确认仍只通过 `ILocomotionOutputRuntimePort` 访问 Locomotion output。
