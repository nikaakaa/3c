# Locomotion Frame 主干 Pipeline 设计

## Context
当前 `PlayerLocomotionController` 已经不再创建 `CharacterStateMachineRunner`，正式 runner owner 是 `PlayerFullBodyActionController`。FullBody 一帧顺序已由 `FullBodyFramePipeline` 管理，Locomotion controller 作为子职责提供 decision facts、state frame 后的 motion frame、基础移动执行和动画提交。

但 `PlayerLocomotionController` 仍同时承担两类职责：

- Runtime Adapter：Unity 生命周期、引用解析、输入源、motion executor、animation presenter、camera、Transform、snapshot/restore 入口。
- Frame 主干编排：prepare decision facts、推进统一状态机、应用状态输出、构建 motion facts、生成 `BasicLocomotionFrame`、更新 phase/gait memory、写 runtime blackboard 和触发诊断。

这导致后续修改 TurnBack、RunEnd、rollback 或 animation motion sampling 时，开发者仍要在一个 Runtime Adapter 文件里理解整条主干。外围拆分已经不够，需要抽出真正的 frame pipeline Module。

## Goals
- 让 `PlayerLocomotionController` 只保留 Runtime Adapter 和正式外围调用职责。
- 让 `LocomotionFramePipeline` 成为 Locomotion 一帧编排的唯一 Module。
- 让 pipeline Interface 以纯数据输入输出为主，避免暴露 Unity scene object。
- 保持 FullBody pipeline 调用形状稳定，使 replay、synctest 和手动 Sandbox 验证继续走正式主线。
- 保持状态机权威、motion executor 权威和 Animancer presenter 权威不变。
- 用 characterization 测试证明拆分前后关键输出一致。

## Non-Goals
- 不迁移 `CaptureSimulationSnapshot` / `RestoreSimulationSnapshot` 的字段语义。
- 不迁移或重写 `RestoreAnimationPlaybackProgress`、`AdvanceAnimationPlaybackProgress`、`ResetMotionPlaybackWindow` 的权威语义。
- 不把 `BasicLocomotionPipeline` 替换为新状态机。
- 不新增 public Contract 给只有一个实现的 helper。
- 不删除现有日志。

## Proposed Shape
```text
Character/Movement/
  Runtime/
    PlayerLocomotionController.cs
    LocomotionRuntimeReferenceResolver.cs
  Model/
    LocomotionFramePipelineInput.cs
    LocomotionFramePipelineResult.cs
    LocomotionFrameRuntimeState.cs
  Solver/
    LocomotionFramePipeline.cs
    Facts/LocomotionFactsBuilder.cs
    TurnBack/TurnBackIntentResolver.cs
    TurnBack/TurnBackMotionResolver.cs
    Motion/LocomotionStateMotionBuilder.cs
  Diagnostics/
    LocomotionDiagnostics.cs
```

实施时可以采用等价命名，但职责必须一致。

## Pipeline Interface 草案
`LocomotionFramePipeline` 的外部 Interface 应该接近：

```text
TryPrepareDecisionFrame(input, runnerSnapshot, runtimeState, currentStep) -> decisionFrame + pipelineStateUpdates
TryEvaluateStateDecision(decisionFrame, runner, inputRequest, blackboardSnapshot, currentStep) -> stateDecision + outputWrites
TryBuildFrame(stateDecision, runtimeState, currentStep) -> BasicLocomotionFrame + motionFacts + memoryUpdates
```

实现阶段可以保留当前三个 public facade 名称，先让 controller 委托给 pipeline，再逐步压缩 controller 内部方法。第一步不要求一次删除所有 helper，但要求主干编排从 controller 中移出。

## Decisions
- Decision: 新增 focused change，而不是继续扩大 `refactor-character-runtime-adapter-layers`。
  - Reason: 现有 change 已覆盖多域外围拆分；主干 frame pipeline 是新的高风险结构调整，需要单独验收。
- Decision: 第一阶段不迁移 playback restore/window。
  - Reason: `formalize-animation-playback-rollback-authority` 仍处于 active 状态，抢先迁移会混淆“真实新播放”和“rollback restore resume”的语义。
- Decision: Pipeline 不创建 runner，只接收 runner 或 runner snapshot。
  - Reason: `PlayerFullBodyActionController` 是唯一正式 runner owner。
- Decision: Pipeline 不执行 motion 或 animation。
  - Reason: `CharacterControllerBasicMotionExecutor` 和 `BasicLocomotionAnimancerPresenter` 分别是正式运动和动画 Runtime Adapter。
- Decision: Pipeline 可以调用纯 Solver 和 Diagnostics。
  - Reason: frame 主干需要集中编排 facts、motion facts 和日志触发，但 Diagnostics 只读取已生成事实。

## Sequencing
1. 建立 characterization 和静态边界测试，先锁定当前输出。
2. 定义 pipeline input/result/runtime state 的最小模型。
3. 把 `TryPrepareDecisionFrame` 的 facts prepare 主干委托给 pipeline。
4. 把 `TryEvaluatePreparedGameplayDecision` 的 state machine tick 前后编排委托给 pipeline，但 runner 仍由 caller 提供。
5. 把 `TryBuildMotionFromStateDecision` 的 motion facts + frame build + memory update 委托给 pipeline。
6. 保留 `ExecuteLocomotionMotion`、`PresentLocomotionAnimation`、`CompleteLocomotionTick` 在 Runtime Adapter。
7. 等 playback rollback authority 完成后，再评估是否迁移 snapshot/playback window。

## Risks / Trade-offs
- Risk: 抽 pipeline 时不得不碰 playback window。
  - Mitigation: 第一阶段把 playback progress 作为只读输入或回调结果传递，不改变 restore/window 语义；如做不到，停止。
- Risk: Pipeline Interface 过大，变成另一个胖类。
  - Mitigation: 只让 pipeline 持有一帧编排；Unity 引用、执行端口和 snapshot restore 仍留 Runtime Adapter。
- Risk: 与现有 `BasicLocomotionPipeline` 名称混淆。
  - Mitigation: `BasicLocomotionPipeline` 保持 frame 构建 helper；新 `LocomotionFramePipeline` 表达一帧编排主干。
- Risk: FullBody replay 仍依赖当前 facade 名称。
  - Mitigation: 外部 public facade 先保持，内部委托 pipeline，测试确认 replay 不改入口。

## Validation Strategy
- 自动测试：
  - 静态测试：`LocomotionFramePipeline` 不引用 `MonoBehaviour`、`Transform`、`CharacterController`、`InputAction`、Animancer runtime、`UnityEngine.Object` 场景实例。
  - 静态测试：`LocomotionFramePipeline` 不创建 `CharacterStateMachineRunner`，不注册 tick driver，不调用 `CharacterController.Move`，不调用 Animancer play API。
  - characterization：同一输入、runner state、blackboard snapshot 和 input request 下，拆分前后 `LocomotionDecisionFrame`、`CharacterStateMachineFrame`、`BasicLocomotionFrame`、runtime facts 和关键日志 event id 一致。
  - FullBody pipeline 定向测试：Directional Dodge、Backstep Dodge、MoveStart/MoveLoop/MoveStop/TurnBack 仍通过正式 FullBody pipeline。
  - rollback/replay 定向测试：不新增 replay 路线，若 playback rollback active change 尚未完成，记录阻塞而不是改语义。
- 手动验证：
  - Sandbox 中 WASD、RunEnd、TurnBack、Dodge Directional、Dodge Backstep 行为不变。
  - 诊断日志仍能定位 Locomotion decision、TurnBack motion 和 FullBody owner。
  - 若 playback rollback authority 已完成，再运行 F6/F8 验证无新增 first mismatch。

## Open Questions / Problems
- `formalize-animation-playback-rollback-authority` 未完成：不能在本变更中迁移或重写 playback restore/window。
- `CaptureSimulationSnapshot` / `RestoreSimulationSnapshot` 仍在 `PlayerLocomotionController`：第一阶段建议保留，避免和 rollback ownership active changes 冲突。
- `BasicLocomotionPipeline` 和新 `LocomotionFramePipeline` 命名接近：实施时要明确前者是 frame 构建 helper，后者是一帧编排主干。
- 当前 active changes 很多，实施时必须先跑静态边界测试，否则容易把另一个 proposal 的职责顺手搬进来。
