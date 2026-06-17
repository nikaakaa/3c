## Context
现在 Locomotion 侧已经出现两个层次：

```text
FullBodySubmissionBuilder
  -> ILocomotionFrameRuntimePort
       -> PlayerLocomotionController
            -> LocomotionFrameBuilder
```

接口边界已经存在，但实现仍集中在 controller。controller 中的 frame runtime 逻辑包括：

- `TryPrepareDecisionFrame`
- `TryEvaluatePreparedGameplayDecision`
- `TryBuildMotionFromStateDecision`
- intent/facts 构建
- camera-relative movement basis
- facing forward resolve
- playback progress/window
- runtime state apply
- runtime blackboard reads/writes

## Goals
- 将 `ILocomotionFrameRuntimePort` 的实现拆成可测试 module。
- 让 `PlayerLocomotionController` 只负责 Unity 引用解析、兼容入口和 runtime module 装配。
- 让 `LocomotionFrameBuilder` 继续保持纯数据 solver。
- 让 frame runtime module 明确拥有 runtime state apply，但不拥有 motion execution。
- 保持 direct tick retired 诊断不变。
- 保持 rollback snapshot/restore 行为不变。

## Non-Goals
- 不创建 `ILocomotionRuntimePort` 这种完整 controller interface。
- 不把 output/apply 逻辑放进 frame runtime module。
- 不让 frame runtime module 直接读取 InputAction、CharacterController、Animancer runtime state。
- 不让 frame runtime module 创建状态机 runner。

## Proposed Shape
建议目录：

```text
Character/Movement/Runtime/
  PlayerLocomotionController.cs
  LocomotionFrameRuntimeAdapter.cs
  LocomotionFrameRuntime.cs
  LocomotionRuntimeStateStore.cs
  LocomotionPrepareFactsProvider.cs
  LocomotionSpatialFactsProvider.cs
  LocomotionMotionFactsProvider.cs
```

职责建议：

- `LocomotionFrameRuntimeAdapter`: 实现 `ILocomotionFrameRuntimePort`，持有 runtime modules。
- `LocomotionFrameRuntime`: 编排 prepare/evaluate/build 三个 frame runtime 步骤。
- `LocomotionRuntimeStateStore`: 管理 current intent、run latch、previous direction、pending turnback intent、phase time。
- `LocomotionPrepareFactsProvider`: 生成 prepare facts、settings、phase facts。
- `LocomotionSpatialFactsProvider`: 消费 camera/facing basis provider 生成 spatial facts。
- `LocomotionMotionFactsProvider`: 解析 TurnBack/baked motion/profile facts。

## Decisions

### Decision: Frame runtime 和 pure builder 分开
`LocomotionFrameBuilder` 继续是纯 solver。需要 Unity runtime state、camera/facing、playback window 的部分留在 frame runtime modules。

理由：builder 的价值是可用纯输入测试；把 Unity runtime state 放进去会回退到浅封装。

### Decision: Runtime state store 可以持有可恢复局部状态
run latch、last moving gait、current intent、pending turnback intent 等可以归 runtime state store，但 snapshot/restore 格式必须保持兼容。

理由：这些状态属于 Locomotion frame runtime，而不是状态机 runner，也不是 output executor。

### Decision: Controller 不直接实现所有端口逻辑
实施后 `PlayerLocomotionController` 可以继续实现 interface 或暴露 adapter，但方法体应委托，而不是继续承载所有实现。

理由：第一阶段为了最小改动可以让 controller 实现 interface；这一阶段要让背后实现真正迁出。

## Migration Plan
1. 先加静态测试锁住 `LocomotionFrameBuilder` 不执行 motion/animation。
2. 抽出 runtime state store，并用现有 snapshot tests 覆盖。
3. 抽出 prepare/spatial/motion facts provider。
4. 抽出 `LocomotionFrameRuntime` 编排三步。
5. 让 controller 的 `ILocomotionFrameRuntimePort` 方法委托给 adapter/runtime。
6. 保持 public debug properties 映射到 state store 或 last result。

## Risks / Trade-offs
- Risk: 拆出 state store 后 restore 行为变化。
  - Mitigation: 先写 capture/restore idempotent tests，再迁移。
- Risk: camera/facing facts provider 变成新 Unity 依赖中心。
  - Mitigation: 用窄 provider 传 Vector facts，不把 Transform 暴露给 builder。
- Risk: TurnBack motion facts 迁移影响当前动作手感。
  - Mitigation: characterization tests 比较 motion command、yaw delta、planar delta。

## Open Questions
- `LocomotionFrameRuntimeAdapter` 是否由 controller 持有一个实例，还是每次 lazy 创建？
- runtime state store 是否应该单独暴露 read-only snapshot 给 diagnostics？
- TurnBack motion facts provider 是否和后续 animation motion source proposal 合并？

## Interface Details
### `LocomotionFrameRuntimeAdapter`
- Interface: implements `ILocomotionFrameRuntimePort` for production callers.
- Invariant: caller is the unified character frame submission path, not a direct Locomotion tick.
- Output: delegates to `LocomotionFrameRuntime` and returns the same result shape as current port methods.
- Forbidden: it must not rebuild Locomotion decisions itself.
- Test surface: static test verifies FullBody submission builder depends on port only.

### `LocomotionFrameRuntime`
- Interface: prepare, evaluate and build one Locomotion frame from runtime providers.
- Invariant: all Unity references are already adapted into plain facts before pure builder calls.
- Output: prepared decision frame, gameplay decision and motion frame.
- Forbidden: movement execution, animation presentation, state machine runner creation.
- Test surface: fake providers verify call order and returned result identity.

### `LocomotionRuntimeStateStore`
- Interface: read/write restorable Locomotion runtime state.
- Invariant: state is local to Locomotion frame preparation, not global character state.
- Output: capture/restore compatible fields for rollback and replay.
- Forbidden: direct scene object references, action state machine snapshots.
- Test surface: idempotent capture/restore tests for run latch, gait, previous direction and pending TurnBack intent.

### `LocomotionPrepareFactsProvider`
- Interface: builds input intent and phase/preparation facts from formal inputs.
- Invariant: it reads facts, it does not consume one-shot action requests.
- Output: pure facts consumed by `LocomotionFrameRuntime`.
- Forbidden: motion execution or animation presentation.
- Test surface: current behavior fixtures for idle, walk, run and no-input frames.

### `LocomotionSpatialFactsProvider`
- Interface: resolves camera-relative and facing-relative vectors.
- Invariant: Transform/Camera details stay behind provider, while builder receives plain vector facts.
- Output: normalized movement basis, facing forward and reference plane facts.
- Forbidden: storing gameplay state or writing blackboard output.
- Test surface: fake camera/facing inputs produce deterministic planar vectors.

### `LocomotionMotionFactsProvider`
- Interface: resolves motion profile facts needed to build a motion frame.
- Invariant: profile selection and baked motion facts are inputs to builder, not output side effects.
- Output: TurnBack, gait and planar movement facts.
- Forbidden: applying motion to controller or writing animation runtime facts.
- Test surface: TurnBack/yaw/planar delta fixtures.

## Implementation Phasing
1. Lock current behavior with pure and integration tests.
2. Extract runtime state store first because multiple providers need it.
3. Extract facts providers while keeping controller method bodies delegating.
4. Introduce `LocomotionFrameRuntime` as the only coordinator.
5. Move `ILocomotionFrameRuntimePort` implementation to adapter or thin controller delegation.
6. Remove duplicate prepare/evaluate/build logic from controller after tests pass.

## Stop Conditions
- Stop if a provider needs to call a motion executor.
- Stop if `LocomotionFrameBuilder` needs a Unity runtime type.
- Stop if FullBody submission builder needs concrete controller access.
- Stop if migration requires a second Locomotion state machine.
- Stop if rollback state shape must change without a separate data-contract proposal.

## Validation Evidence
- Static tests for pure builder dependencies.
- Decision equivalence tests for prepare/evaluate/build.
- TurnBack and run latch behavior tests.
- Capture/restore rollback tests.
- `openspec validate refactor-locomotion-frame-runtime-modules --strict --no-interactive`.
