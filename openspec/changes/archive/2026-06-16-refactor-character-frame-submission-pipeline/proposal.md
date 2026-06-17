# Change: 统一角色帧管线与提交模型

## Why
当前最高帧调度已经收束到 `CharacterFramePipeline`，FullBody 行为域通过 `FullBodySubmissionBuilder` 提交结果。问题从“怎么迁移旧 FullBody pipeline”转为“如何防止 FullBody、Locomotion、Action 或未来 UpperBody/LowerBody 重新拥有自己的 phase owner”。

本变更固定最高调度权只属于唯一 `CharacterFramePipeline`，并将 FullBody、Locomotion、Action 等域降级为提交者。提交分为两类：状态机前的 request submission 进入统一请求/打断仲裁，状态机后的 frame output submission 进入统一输出合成。最终副作用只能由唯一管线统一应用和提交。

## What Changes
- 新增 `character-frame-pipeline` 能力，定义唯一角色帧管线、提交模型、输出合成和输出应用边界。
- 将现有 FullBody 口径从“FullBody frame pipeline 是最大管线”调整为“FullBody 是当前唯一身体域提交者”。
- 将 request candidate 收集上移到 Character frame pipeline，外部请求、Dodge、TurnBack 和后续 Attack/Jump 必须通过统一 request submission 进入请求/打断仲裁。
- 将 `ExecuteMotion`、`PresentationBridge`、input consume、runtime facts 写入和 snapshot/events commit 规划为唯一 Character 管线的提交阶段。
- 保留现有 FullBody 行为作为第一阶段唯一提交来源，不在本变更中实现 UpperBody、LowerBody、AvatarMask layer 或并行状态机。
- 明确原 FullBody phase owner 已直接迁移为 `FullBodySubmissionBuilder`，原 Locomotion 局部 pipeline 已迁移为 `LocomotionFrameBuilder`；迁移后不得保留正式 pipeline 外壳作为 phase owner。
- 明确角色级管线的物理归属为 `Assets/Scripts/Character/Pipeline/...`，不得继续放在 `Action/FullBody` 目录下伪装成 FullBody 私有实现。
- 明确角色级提交结果命名为 `CharacterFrameSubmission` 或等价 Character 语义，而不是 `BodyFrameSubmission`。

## Impact
- Affected specs:
  - `character-frame-pipeline`
  - `fullbody-action-framework`
  - `simulation-tick-system`
- Affected code:
  - `Assets/Scripts/Character/Pipeline/Runtime/CharacterFramePipeline.cs`
  - `Assets/Scripts/Character/Pipeline/Contracts/ICharacterFrameRuntimePort.cs`
  - `Assets/Scripts/Character/Action/FullBody/Runtime/FullBodySubmissionBuilder.cs`
  - `Assets/Scripts/Character/Pipeline/Model/CharacterFramePipelineTypes.cs`
  - `Assets/Scripts/Character/Action/FullBody/Runtime/PlayerFullBodyActionController.cs`
  - `Assets/Scripts/Character/Action/FullBody/Runtime/FullBodyActionTickAdapter.cs`
  - `Assets/Scripts/Character/Movement/Solver/LocomotionFrameBuilder.cs`
  - `Assets/Scripts/Character/Movement/Runtime/PlayerLocomotionController.cs`
  - `Assets/Scripts/Character/StateMachine/Solver/Output/CharacterStateOutputResolver.cs`
  - `Assets/Scripts/Simulation/Core/SimulationTickPhaseOrder.cs`
- Coordination:
  - 原 `refactor-locomotion-frame-pipeline-mainline` 已拆分为 `refactor-locomotion-frame-runtime-modules` 与 `refactor-locomotion-output-runtime-modules`；本变更只固定 Locomotion 侧正式类型为 `LocomotionFrameBuilder` 或等价 builder，不再保留旧大包 change。
  - 与 `refactor-state-timeline-facts-authority`、`refactor-state-action-motion-output`、`refactor-transition-condition-evaluators` 共享状态机外围职责收窄目标；本变更不得恢复 runner、resolver 或 controller 的混合大职责。
