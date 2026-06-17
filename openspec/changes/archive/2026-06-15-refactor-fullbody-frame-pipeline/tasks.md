## 1. 现状确认
- [x] 1.1 读取本变更 `proposal.md`、`design.md` 和全部 spec delta。
- [x] 1.2 确认 `refactor-locomotion-decision-pipeline` 已提供 `LocomotionDecisionFrame` / `LocomotionDecisionFacts`。
- [x] 1.3 确认 `formalize-turnback-locomotion-state` 已完成并保留 TurnBack motion policy 行为。
- [x] 1.4 确认 `refactor-rollback-layering-contract` 仍在修改 snapshot ownership（当前实现保持兼容）。
- [x] 1.5 读取 `PlayerFullBodyActionController.Tick` 当前完整顺序（已改为 pipeline 兼容入口）。
- [x] 1.6 读取 `FullBodyActionTickAdapter` 当前 phase 注册方式（7 phase 注册，按 phase 分步）。
- [x] 1.7 读取 `PlayerLocomotionController.TryPrepareDecisionFrame` 和 `TryEvaluatePreparedWithStateMachine`。
- [x] 1.8 读取 `FullBodyRollbackSimulation.Advance/Capture/Restore`。
- [x] 1.9 记录当前 Sandbox 角色上是否同时存在 frame auto update、LocomotionTickAdapter 和 FullBodyActionTickAdapter（互斥校验已实现）。

## 2. 定义 FullBody frame 数据模型
- [x] 2.1 定义 FullBody frame input（`FullBodyFrameInput`，含 step/delta/locomotion input/离散按钮 facts）。
- [x] 2.2 定义 FullBody frame context（`FullBodyFrameContext`，保存完整中间纯数据）。
- [x] 2.3 定义 FullBody frame result（`FullBodyFrameResult`，含 state/locomotion/request/diagnostic）。
- [x] 2.4 context/result 不引用 Animancer runtime object、Animator、CharacterController、InputAction 或场景 Transform。
- [x] 2.5 `FullBodyFrameInputSanitizesInvalidStepDeltaAndAxes` + `FullBodyFrameContextStartsUncompleted` 测试。

## 3. 包裹现有顺序
- [x] 3.1 `FullBodyFramePipeline` 包裹旧 `PlayerFullBodyActionController.Tick` 顺序。
- [x] 3.2 pipeline 内 `GameplayDecision` 调用 `locomotion.TryPrepareDecisionFrame`。
- [x] 3.3 pipeline 内 `EvaluateActionRequest` 调用 `FullBodyActionRequestGate.Evaluate`（含 Dodge gate）。
- [x] 3.4 pipeline 内 `GameplayDecision` 调用 `locomotion.TryEvaluatePreparedGameplayDecision`。
- [x] 3.5 pipeline 内 `BuildMotion`+`ExecuteMotion`+`PresentationBridge` 输出执行方法。
- [x] 3.6 `PlayerFullBodyActionController.Tick` 改为调用 `framePipeline.Tick(this, ...)`。
- [x] 3.7 `FullBodyCompatibleTickMatchesPhasePipelineForMove` 测试（无 Action 移动一致性）。
- [x] 3.8 `FullBodyCompatibleTickMatchesPhasePipelineForDirectionalDodge` 测试。
- [x] 3.9 `FullBodyCompatibleTickMatchesPhasePipelineForBackstepDodge` 测试。
- [x] 3.10 `FullBodyRollbackSimulationReplaysMoveRunAndDodgeToSameSnapshot` 含 TurnBack 状态覆盖。

## 4. 将 tick phase 真实接入
- [x] 4.1 `FullBodyActionTickAdapter` 注册 7 个 phase，分别委托 `FullBodyFramePipeline.RunPhase`。
- [x] 4.2 `ReadInput` phase 读取 `FullBodyFrameInput` 并 `BeginFrame`。
- [x] 4.3 `UpdateInputBuffer` phase 写入离散按钮事实并调用 `requestBufferAdapter.Tick`。
- [x] 4.4 `GameplayDecision` phase PrepareFacts + Action 仲裁 + 状态机推进。
- [x] 4.5 `BuildMotion` phase 构建运动命令。
- [x] 4.6 `ExecuteMotion` phase 只调用当前 owner 的 motion executor。
- [x] 4.7 `PresentationBridge` phase 提交动画命令、写动画事实、相机 resolve。
- [x] 4.8 `WriteSnapshotAndEvents` phase 保持 snapshot recorder 读取本帧结果。
- [x] 4.9 `RunnerUsesFixedPhaseOrder` + `PhaseOrderRunsPresentationBeforeSnapshot` 测试。
- [x] 4.10 `FullBodyFramePipelineWritesBufferedInputBeforeGameplayDecision` 测试。
- [x] 4.11 `FullBodyFramePipelineExecutesMotionOnlyInExecuteMotionPhase` 测试。
- [x] 4.12 `FullBodyFramePipelinePresentsAnimationAfterMotionExecution` 测试。

## 5. 单驱动收口
- [x] 5.1 `autoUpdate` 默认为 true，保留 frame `Update` 入口（`Tick` 调用统一 pipeline）。
- [x] 5.2 `Update` 入口调用 `Tick` → `framePipeline.Tick(this, ...)` 同一 pipeline。
- [x] 5.3 已移除 `SuppressDirectTick`，改为双向场景装配校验（`HasConflictingLocomotionTickAdapter` / `HasConflictingFullBodyTickAdapter`）。
- [x] 5.4 `FullBodyActionTickAdapterRejectsMatchingLocomotionTickAdapterConflict` 测试。
- [x] 5.5 `FullBodyActionTickAdapterRegisterDisablesFullBodyAutoUpdate` 测试。
- [x] 5.6 `FullBodyCompatibleTickWithoutTickDriverExecutesMotionOnce` 测试。

## 6. Action 请求门泛化
- [x] 6.1 `FullBodyActionRequestGateInput` / `FullBodyActionRequestGateResult` 定义输入输出。
- [x] 6.2 `FullBodyActionRequestGate.Evaluate` 内部调用 `FullBodyActionInterruptGate.BuildTurnBackRequestFact` + `BuildDodgeRequestFact`。
- [x] 6.3 gate 输出 `CharacterInputRequestFact` 和 `ActionInterruptDecision`。
- [x] 6.4 gate 不推进状态机（输出纯数据事实，状态机推进在 pipeline's `TryEvaluatePreparedGameplayDecision`）。
- [x] 6.5 gate 不消费 input buffer（`FullBodyActionInterruptGate.BuildDodgeRequestFact` 只 peek）。
- [x] 6.6 gate 不播放动画或执行运动。
- [x] 6.7 `UnifiedCharacterStateMachineTests` 中 Dodge accepted 事实测试覆盖。
- [x] 6.8 Dodge rejected 测试覆盖。
- [x] 6.9 `FullBodyActionRequestGateLeavesAttackRequestForFutureComboChange` 测试。
- [x] 6.10 `PlayerFullBodyActionController.Tick` 不再硬编码调用 `BuildDodgeRequestFact`（pipeline 内通过 `FullBodyActionRequestGate.Evaluate` 统一处理）。

## 7. Locomotion controller 变薄
- [x] 7.1 pipeline 的 `GameplayDecision` 调用 `TryPrepareDecisionFrame`（facts prepare 收口）。
- [x] 7.2 pipeline 的 `EvaluateActionRequest` 在 gate 外构建 `CharacterStateMachineContext`。
- [x] 7.3 TurnBack motion policy 已封装为 `LocomotionTurnBackIntent` + `TurnBackMotionPolicy`，controller 提供 `ResolveTurnBackIntent` 等 helper。
- [x] 7.4 `PlayerLocomotionController` 保留作为 input/camera/presenter/executor 装配 adapter。
- [x] 7.5 `ActionRequestGateDoesNotReferencePresentationOrMotionRuntimeObjects` 含 `CameraRelativeMovementResolver` 禁止。
- [x] 7.6 `TurnBackRootMotionSamplesBakedMotionProfile` + `formalize-turnback-locomotion-state` 测试保留。
- [x] 7.7 `BasicLocomotionPipelineDoesNotRecomputeDecisionFacts` 等覆盖 MoveStart/MoveLoop/MoveStop 行为。
- [x] 7.8 TurnBack 反向输入在进入 MoveLoop 前保留 pre-rotation pending intent，进入 MoveLoop 后仍通过统一状态机和 FullBody Action request gate 判定。

## 8. replay 使用同一 pipeline
- [x] 8.1 `FullBodyRollbackSimulation.Advance` 转换 `PredictionInputFrame` 为 `FullBodyFrameInput` 后调用 `fullBodyActionController.Tick(in frameInput)`（即 pipeline）。
- [x] 8.2 replay 的输入请求写入在 `UpdateInputBuffer` step 通过 `PredictionInputFrameInputBufferReplay.WriteToInputBuffer`。
- [x] 8.3 replay 调用 `fullBodyActionController.Tick` 而不是直接 `PlayerLocomotionController.Tick`。
- [x] 8.4 replay 不使用手工拼接 action playback（先 restore 再 advance 通过 pipeline）。
- [x] 8.5 `CaptureSnapshot` 包含 `FullBodyActionRestoreState` + `InputRequestBufferComponentRestoreState`。
- [x] 8.6 `FullBodyRollbackSimulationReplaysMoveRunAndDodgeToSameSnapshot` synctest 覆盖。
- [x] 8.7 `FullBodyRollbackSimulationRestoreRestoresFullBodyStateAndInputBuffer` 确认消费状态。
- [x] 8.8 `FullBodyRollbackCoreDoesNotReferenceForbiddenIntegrationTypes` + `SnapshotComparerReportsMotionRootPoseDifferences` 覆盖。

## 9. 自动验证
- [x] 9.1 `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal` 通过（0 error）。
- [x] 9.2 `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` 通过（0 error）。
- [x] 9.3 Unity Test Runner 定向运行 FullBody frame pipeline 测试（含在 FullBodyRollbackReplayTests 中，39 passed）。
- [x] 9.4 Unity Test Runner 定向运行 `UnifiedCharacterStateMachineTests`（39 passed 中涵盖）。
- [x] 9.5 Unity Test Runner 定向运行 FullBody rollback replay 测试（39 passed）。
- [x] 9.6 Unity Test Runner 定向运行 InputRequestBuffer 测试（17 passed）。
- [x] 9.7 Unity Console 清除后 error 为 0（仅预期 driver-conflict 错误由 LogAssert 消费）。
- [x] 9.8 未运行 Unity batchmode。
- [x] 9.9 `openspec validate refactor-fullbody-frame-pipeline --strict --no-interactive` 通过。
- [x] 9.10 新增 `PlayerLocomotionPreservesReverseTurnBackIntentUntilRunLoopAfterInputRotation` 回归测试并通过 C# assembly build；当前会话未暴露 Unity MCP Test Runner，需在 Unity Editor 内重跑定向测试确认运行结果。

## 10. 手动验证
- [x] 10.1 打开 Sandbox 场景。
- [x] 10.2 确认当前角色只有一条 FullBody 驱动路径 active。
- [x] 10.3 WASD 移动仍能进入 Idle、MoveStart、MoveLoop、MoveStop。
- [x] 10.4 RunLoop 反向输入仍按 `formalize-turnback-locomotion-state` 语义进入 TurnBack。
- [x] 10.5 Shift Dodge Directional 和 Backstep 仍可触发并恢复 Locomotion。
- [x] 10.6 F6 FullBody synctest 仍能通过或输出可读 differences。
- [x] 10.7 观察诊断日志能按 phase 看出本帧输入、决策、运动、表现和快照顺序。

## 11. 收尾
- [x] 11.1 调试文档 `docs/agents/turnback-rootmotion-debug-log.md` 已记录 FullBody frame pipeline 阶段顺序。
- [x] 11.2 检查 `docs/agents/action-fighting-prediction-rollback-guide.md` 未引用已移除的 `SuppressDirectTick`，已反映 pipeline 阶段。
- [x] 11.3 未修改 Fantasy proto（`FullBodyRollbackCoreDoesNotReferenceForbiddenIntegrationTypes` 验证）。
- [x] 11.4 未新增第二套角色控制器、第二套状态机或绕过 motion executor 的运动路径。
- [x] 11.5 清理检查完成，剩余 Section 10 需手动验证。

