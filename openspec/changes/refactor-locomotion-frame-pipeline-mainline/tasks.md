## 1. Scope Audit
- [ ] 1.1 读取本变更 `proposal.md`、`design.md` 和全部 spec delta。
- [ ] 1.2 读取 `openspec/changes/refactor-character-runtime-adapter-layers/tasks.md`，确认外围分层已完成到哪些文件。
- [ ] 1.3 读取 `openspec/changes/refactor-locomotion-adapter-modules/tasks.md`，确认 facts / TurnBack / motion / diagnostics 模块可复用。
- [ ] 1.4 读取 `openspec/changes/formalize-animation-playback-rollback-authority/tasks.md`，确认 playback restore/window 是否仍禁止迁移。
- [ ] 1.5 读取 `openspec/changes/refactor-fullbody-frame-pipeline/tasks.md`，确认 FullBody phase order 和 replay 主线。
- [ ] 1.6 统计 `PlayerLocomotionController` 当前 public facade、private helper、snapshot/restore、playback/window 和 diagnostics 职责。
- [ ] 1.7 列出 `TryPrepareDecisionFrame` 的所有调用点。
- [ ] 1.8 列出 `TryEvaluatePreparedGameplayDecision` 的所有调用点。
- [ ] 1.9 列出 `TryBuildMotionFromStateDecision` 的所有调用点。
- [ ] 1.10 标记不在本变更迁移范围内的 playback restore/window 方法。
- [ ] 1.11 标记不在第一阶段迁移范围内的 snapshot/restore 方法。

## 2. Tests First
- [ ] 2.1 新增静态测试：`LocomotionFramePipeline` 不继承 `MonoBehaviour`。
- [ ] 2.2 新增静态测试：`LocomotionFramePipeline` 不引用 `Transform`。
- [ ] 2.3 新增静态测试：`LocomotionFramePipeline` 不引用 `CharacterController`。
- [ ] 2.4 新增静态测试：`LocomotionFramePipeline` 不引用 `InputAction` 或 `UnityEngine.InputSystem`。
- [ ] 2.5 新增静态测试：`LocomotionFramePipeline` 不引用 Animancer runtime。
- [ ] 2.6 新增静态测试：`LocomotionFramePipeline` 不创建 `CharacterStateMachineRunner`。
- [ ] 2.7 新增静态测试：`LocomotionFramePipeline` 不调用 `CharacterController.Move`。
- [ ] 2.8 新增静态测试：`LocomotionFramePipeline` 不调用 `.Present(` 或 Animancer play API。
- [ ] 2.9 新增静态测试：`PlayerLocomotionController` 的三个主干 facade 委托 `LocomotionFramePipeline`。
- [ ] 2.10 新增日志 key 测试：Locomotion / TurnBack 关键 event id 不丢失。
- [ ] 2.11 新增 characterization 测试：同一输入下 decision facts 输出保持。
- [ ] 2.12 新增 characterization 测试：同一 state frame 下 motion facts 输出保持。
- [ ] 2.13 新增 characterization 测试：run latch / last moving gait / move stop gait memory 输出保持。
- [ ] 2.14 新增 FullBody pipeline 回归测试：Move input 仍走正式 FullBody pipeline。
- [ ] 2.15 新增 FullBody pipeline 回归测试：Directional Dodge 仍压制 Locomotion 输出。
- [ ] 2.16 新增 FullBody pipeline 回归测试：Backstep Dodge 仍恢复 Locomotion。

## 3. Model Shape
- [ ] 3.1 创建 `LocomotionFramePipelineInput` 或等价输入模型。
- [ ] 3.2 输入模型包含 `BasicLocomotionInputSnapshot`。
- [ ] 3.3 输入模型包含 current step。
- [ ] 3.4 输入模型包含 runner snapshot 或 runner 读取所需纯数据。
- [ ] 3.5 输入模型包含 input request fact。
- [ ] 3.6 输入模型包含 runtime blackboard snapshot。
- [ ] 3.7 输入模型不包含 Unity scene object。
- [ ] 3.8 创建 `LocomotionFramePipelineResult` 或等价结果模型。
- [ ] 3.9 结果模型包含 `LocomotionDecisionFrame`。
- [ ] 3.10 结果模型包含 `LocomotionStateDecisionFrame`。
- [ ] 3.11 结果模型包含 `CharacterStateMachineFrame`。
- [ ] 3.12 结果模型包含 `BasicLocomotionFrame`。
- [ ] 3.13 结果模型包含需要写回 controller 的 runtime state updates。
- [ ] 3.14 结果模型不包含 motion executor 或 presenter 引用。

## 4. Prepare Decision Frame 迁移
- [ ] 4.1 创建 `LocomotionFramePipeline`。
- [ ] 4.2 将 movement config 缺失诊断保持在 Runtime Adapter 或 Diagnostics，不新增 fallback。
- [ ] 4.3 将 `ResolveMovementIntent` 主干迁入 pipeline 或其内部协作方法。
- [ ] 4.4 将 `ResolveFrameGait` 主干迁入 pipeline。
- [ ] 4.5 将 `ResolveMovementSettings` 调用边界接入 pipeline。
- [ ] 4.6 将 `ResolvePhaseFacts` 调用边界接入 pipeline。
- [ ] 4.7 将 `LocomotionFactsBuilder.ResolveInput` 调用接入 pipeline。
- [ ] 4.8 将 spatial facts 输入从 Runtime Adapter 以纯数据形式传入 pipeline。
- [ ] 4.9 保持 camera look 应用和 rollback camera basis provider 在 Runtime Adapter。
- [ ] 4.10 保持 `AdvanceAnimationPlaybackProgress` 语义不变。
- [ ] 4.11 `PlayerLocomotionController.TryPrepareDecisionFrame` 改为委托 pipeline。
- [ ] 4.12 运行 prepare decision characterization 测试。

## 5. State Decision 迁移
- [ ] 5.1 将 `BuildStateMachineContext` 调用接入 pipeline。
- [ ] 5.2 Pipeline 接收外部提供的 `CharacterStateMachineRunner`。
- [ ] 5.3 Pipeline 不创建 runner。
- [ ] 5.4 Pipeline 调用 runner tick 后返回 `CharacterStateMachineFrame`。
- [ ] 5.5 将 `ConsumeTurnBackIntentIfEntered` 的纯状态更新结果从 pipeline 返回。
- [ ] 5.6 将 `ApplyStateMachineOutputs` 的 run latch 更新结果从 pipeline 返回。
- [ ] 5.7 Runtime Adapter 应用 pipeline 返回的 run latch 更新。
- [ ] 5.8 保持 runtime blackboard write 顺序不变。
- [ ] 5.9 `PlayerLocomotionController.TryEvaluatePreparedGameplayDecision` 改为委托 pipeline。
- [ ] 5.10 运行 state decision characterization 测试。

## 6. Motion Frame 迁移
- [ ] 6.1 将 `ResolveMotionFacts` 调用接入 pipeline，但不改变 TurnBack motion source。
- [ ] 6.2 将 `ResolveMotionDecisionFacts` 调用接入 pipeline。
- [ ] 6.3 将 `LocomotionStateMotionBuilder.BuildFrame` 调用接入 pipeline。
- [ ] 6.4 Pipeline 返回 `BasicLocomotionFrame`。
- [ ] 6.5 Pipeline 返回 active state path update。
- [ ] 6.6 Pipeline 返回 current phase time update。
- [ ] 6.7 Pipeline 返回 current intent update。
- [ ] 6.8 Pipeline 返回 `lastMovingGait` update。
- [ ] 6.9 Pipeline 返回 `hasActiveMoveStopGait` update。
- [ ] 6.10 Pipeline 返回 `activeMoveStopGait` update。
- [ ] 6.11 Pipeline 触发或返回 state output probe 诊断所需纯数据。
- [ ] 6.12 `PlayerLocomotionController.TryBuildMotionFromStateDecision` 改为委托 pipeline。
- [ ] 6.13 运行 motion frame characterization 测试。

## 7. Runtime Adapter 收口
- [ ] 7.1 保持 `ExecuteLocomotionMotion` 留在 `PlayerLocomotionController`。
- [ ] 7.2 保持 `PresentLocomotionAnimation` 留在 `PlayerLocomotionController`。
- [ ] 7.3 保持 `CompleteLocomotionTick` 留在 `PlayerLocomotionController`。
- [ ] 7.4 保持 `TryReadInput` 留在 `PlayerLocomotionController`。
- [ ] 7.5 保持 `ResolveSpatialFacts` 中的 Unity camera 操作留在 Runtime Adapter 或转成纯数据输入。
- [ ] 7.6 保持 `CaptureSimulationSnapshot` 第一阶段不迁移。
- [ ] 7.7 保持 `RestoreSimulationSnapshot` 第一阶段不迁移。
- [ ] 7.8 保持 `RestoreAnimationPlaybackProgress` 语义不变。
- [ ] 7.9 保持 `AdvanceAnimationPlaybackProgress` 语义不变。
- [ ] 7.10 保持 `ResetMotionPlaybackWindow` 语义不变。
- [ ] 7.11 删除或压缩 controller 中已经委托给 pipeline 的 private helper。
- [ ] 7.12 确认 controller 不再持有 frame 主干中间步骤细节。

## 8. Cross-Layer Verification
- [ ] 8.1 搜索 `LocomotionFramePipeline`，确认没有 `MonoBehaviour`。
- [ ] 8.2 搜索 `LocomotionFramePipeline`，确认没有 `Transform` 字段或属性。
- [ ] 8.3 搜索 `LocomotionFramePipeline`，确认没有 `CharacterController`。
- [ ] 8.4 搜索 `LocomotionFramePipeline`，确认没有 `InputAction`。
- [ ] 8.5 搜索 `LocomotionFramePipeline`，确认没有 `Animancer`。
- [ ] 8.6 搜索 `LocomotionFramePipeline`，确认没有 `new CharacterStateMachineRunner`。
- [ ] 8.7 搜索 `LocomotionFramePipeline`，确认没有 `RegisterTick`。
- [ ] 8.8 搜索 `LocomotionFramePipeline`，确认没有 `.Move(`。
- [ ] 8.9 搜索 `LocomotionFramePipeline`，确认没有 `.Present(`。
- [ ] 8.10 搜索正式路径，确认没有 `Resources.Load`。
- [ ] 8.11 搜索正式路径，确认没有新增 fallback config。

## 9. Automatic Validation
- [ ] 9.1 运行 `dotnet build .\3cDemo\Client\3C_Client\Assembly-CSharp.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [ ] 9.2 运行 `dotnet build .\3cDemo\Client\3C_Client\Assembly-CSharp-Editor.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [ ] 9.3 使用 Unity Test Runner 运行 `Tests.Editor.UnifiedCharacterStateMachineTests`。
- [ ] 9.4 使用 Unity Test Runner 运行 FullBody frame pipeline 定向测试。
- [ ] 9.5 使用 Unity Test Runner 运行基础移动动画相关 EditMode 测试。
- [ ] 9.6 使用 Unity Test Runner 运行 FullBody rollback replay 定向测试；若 playback rollback authority 尚未完成，记录阻塞原因。
- [ ] 9.7 读取 Unity Console，确认编译 error 为 0。
- [ ] 9.8 运行 `openspec validate refactor-locomotion-frame-pipeline-mainline --strict --no-interactive`。
- [ ] 9.9 不运行 Unity batchmode。

## 10. Manual Verification
- [ ] 10.1 打开 Sandbox 场景。
- [ ] 10.2 确认当前角色仍只有 FullBody 正式 driver active。
- [ ] 10.3 WASD 仍进入 Idle。
- [ ] 10.4 WASD 起步仍进入 MoveStart。
- [ ] 10.5 持续输入仍进入 MoveLoop。
- [ ] 10.6 松开输入仍进入 MoveStop。
- [ ] 10.7 RunEnd 仍能无输入播完回 Idle。
- [ ] 10.8 RunEnd 中途重新输入仍立即回移动阶段。
- [ ] 10.9 RunLoop 反向输入仍进入 TurnBack。
- [ ] 10.10 TurnBack motion/input lock 语义不变。
- [ ] 10.11 Shift Dodge Directional 仍可触发并恢复 Locomotion。
- [ ] 10.12 Shift Dodge Backstep 仍可触发并恢复 Locomotion。
- [ ] 10.13 开启诊断日志后确认 Locomotion decision、TurnBack motion 和 FullBody owner 日志仍可定位。
- [ ] 10.14 若 playback rollback authority 已完成，运行 F6/F8 验证无新增 first mismatch。

## 11. Completion
- [ ] 11.1 确认 `PlayerLocomotionController` 不再承载一帧主干编排细节。
- [ ] 11.2 确认 `LocomotionFramePipeline` 是一帧编排 Module，而不是第二状态机。
- [ ] 11.3 确认没有新增第二状态路径。
- [ ] 11.4 确认没有新增第二运动路径。
- [ ] 11.5 确认没有新增第二动画权威。
- [ ] 11.6 确认没有新增 fallback 配置。
- [ ] 11.7 确认没有删除用户未明确要求删除的 log。
- [ ] 11.8 确认 active playback rollback 变更范围没有被本变更抢权。
- [ ] 11.9 全部真实完成后再将 checklist 标为 `- [x]`。
