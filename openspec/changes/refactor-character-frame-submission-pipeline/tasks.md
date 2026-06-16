## 1. Scope And Baseline
- [x] 1.1 读取本 change 的 `proposal.md`、`design.md` 和 spec delta。
- [x] 1.2 读取 `openspec/specs/fullbody-action-framework/spec.md`。
- [x] 1.3 读取 `openspec/specs/simulation-tick-system/spec.md`。
- [x] 1.4 读取 `openspec/specs/unified-character-state-machine/spec.md`。
- [x] 1.5 读取 active `refactor-locomotion-frame-runtime-modules` 与 `refactor-locomotion-output-runtime-modules`，确认旧 Locomotion 大包已拆分且职责不再冲突。
- [x] 1.6 确认本变更直接将原 FullBody phase owner 迁移为 `FullBodySubmissionBuilder` 或等价提交构建器。
- [x] 1.7 确认本变更直接将原 Locomotion 局部 pipeline 迁移为 `LocomotionFrameBuilder` 或等价局部帧构建器。
- [x] 1.8 确认角色级帧输出提交命名采用 `CharacterFrameSubmission` 或等价 Character 语义。
- [x] 1.9 确认 request submission 与 frame output submission 是两条不同通道。

## 2. Impact Analysis
- [x] 2.1 对原 `FullBodyFramePipeline` / 当前 `FullBodySubmissionBuilder` 执行可用引用扫描。
- [x] 2.2 对 `PlayerFullBodyActionController.Tick` 运行 GitNexus impact analysis。
- [x] 2.3 对 `FullBodyActionTickAdapter.Tick` 运行 GitNexus impact analysis。
- [x] 2.4 对原 `LocomotionFramePipeline` / 当前 `LocomotionFrameBuilder` 执行可用引用扫描。
- [x] 2.5 对 `PlayerLocomotionController.TryPrepareDecisionFrame` 运行 GitNexus impact analysis。
- [x] 2.6 对 `PlayerLocomotionController.TryEvaluatePreparedGameplayDecision` 运行 GitNexus impact analysis。
- [x] 2.7 对 `PlayerLocomotionController.TryBuildMotionFromStateDecision` 运行 GitNexus impact analysis。
- [x] 2.8 若任一 impact 为 HIGH 或 CRITICAL，先向用户报告 blast radius 后再继续实现。

## 3. Request Submission Model
- [x] 3.1 新增 request submission 数据模型。
- [x] 3.2 新增 request provider 来源标识。
- [x] 3.3 将 Dodge 请求候选接入 request submission。
- [x] 3.4 将 TurnBack 请求候选接入 request submission。
- [x] 3.5 为后续 Attack/Jump 请求预留同一 request submission 接口。
- [x] 3.6 支持外部请求候选进入同一 request submission 收集入口。
- [x] 3.7 将 request submission 输入统一请求/打断仲裁。
- [x] 3.8 仲裁 accepted 后生成 `CharacterInputRequestFact` 或等价事实。
- [x] 3.9 仲裁 rejected 后不消费输入、不切状态、不执行输出副作用。
- [x] 3.10 确认 request provider 不调用 motion executor。
- [x] 3.11 确认 request provider 不调用 animation presenter。
- [x] 3.12 确认 request provider 不调用状态切换 API。

## 4. Character Frame Model
- [x] 4.1 新增角色级 `CharacterFrameInput` 模型。
- [x] 4.2 新增角色级 frame context 模型。
- [x] 4.3 新增角色级 frame result 模型。
- [x] 4.4 新增 `CharacterFrameSubmission` 或等价帧输出提交模型。
- [x] 4.5 新增提交来源标识。
- [x] 4.6 新增 movement submission 数据。
- [x] 4.7 新增 animation submission 数据。
- [x] 4.8 新增 input consume submission 数据。
- [x] 4.9 新增 runtime facts submission 数据。
- [x] 4.10 新增 diagnostics submission 数据。
- [x] 4.11 确认 `CharacterFrameSubmission` 不持有 Unity 场景对象。
- [x] 4.12 确认 `CharacterFrameSubmission` 不表达 request priority/resistance/timing 仲裁规则。

## 5. CharacterFramePipeline
- [x] 5.1 新增 `CharacterFramePipeline` 或等价唯一角色帧管线。
- [x] 5.2 让角色级管线复用 `SimulationTickPhaseOrder`。
- [x] 5.3 实现 `ReadInput` phase 的兼容转发。
- [x] 5.4 实现 `UpdateInputBuffer` phase 的兼容转发。
- [x] 5.5 实现 `CollectRequests` 或等价 request submission 收集步骤。
- [x] 5.6 实现 `ArbitrateRequests` 或等价统一请求/打断仲裁步骤。
- [x] 5.7 实现 `GameplayDecision` phase 的状态提交构建入口。
- [x] 5.8 实现 `BuildMotion` phase 的帧输出提交构建入口。
- [x] 5.9 实现 `ExecuteMotion` phase 的统一 output apply 入口。
- [x] 5.10 实现 `PresentationBridge` phase 的统一表现提交入口。
- [x] 5.11 实现 `WriteSnapshotAndEvents` phase 的统一 commit 入口。
- [x] 5.12 确认角色级管线不创建第二个 `CharacterStateMachineRunner`。

## 6. Direct Renames
- [x] 6.1 将原 `FullBodyFramePipeline` 类改名为 `FullBodySubmissionBuilder` 或等价名称。
- [x] 6.2 将原 `FullBodyFramePipelineTypes` 中正式 pipeline 命名迁移到 Character/Submission 语义。
- [x] 6.3 将正式 pipeline step 使用迁移到角色级 step 或 phase result。
- [x] 6.4 将原 `LocomotionFramePipeline` 类改名为 `LocomotionFrameBuilder` 或等价名称。
- [x] 6.5 更新所有调用点和测试引用。
- [x] 6.6 确认没有正式路径继续引用旧 FullBody pipeline 类名。
- [x] 6.7 确认没有正式路径继续引用旧 Locomotion pipeline 类名。
- [x] 6.8 将角色级 Pipeline runtime/model/contracts 物理迁移到 `Assets/Scripts/Character/Pipeline/...`。

## 7. FullBody Frame Submission
- [x] 7.1 将当前 FullBody gameplay decision 输出包成 `CharacterFrameSubmission`。
- [x] 7.2 将当前 `CharacterStateMachineFrame` 纳入 `CharacterFrameSubmission`。
- [x] 7.3 将当前 `BasicLocomotionFrame` 纳入 `CharacterFrameSubmission`。
- [x] 7.4 将当前 `ActionMotionResolveResult` 纳入 `CharacterFrameSubmission`。
- [x] 7.5 将当前 request consume 意图纳入 `CharacterFrameSubmission`。
- [x] 7.6 将当前 animation request 纳入 `CharacterFrameSubmission`。
- [x] 7.7 将当前 runtime facts 写入意图纳入 `CharacterFrameSubmission`。
- [x] 7.8 将当前 diagnostics trace 纳入 `CharacterFrameSubmission`。
- [x] 7.9 确认 FullBody submitter 不执行 motion executor。
- [x] 7.10 确认 FullBody submitter 不播放 Animancer。
- [x] 7.11 确认 FullBody submitter 不写 runtime blackboard。
- [x] 7.12 确认 FullBody submitter 不消费 input buffer。

## 8. Output Composer And Applier
- [x] 8.1 新增 output composer，第一版只接收 FullBody 一个 `CharacterFrameSubmission` 来源。
- [x] 8.2 composer 选择唯一 movement 输出。
- [x] 8.3 composer 选择唯一 base animation 输出。
- [x] 8.4 composer 选择 input consume 输出。
- [x] 8.5 composer 选择 runtime facts 输出。
- [x] 8.6 composer 选择 snapshot/events 输出。
- [x] 8.7 新增 output applier 或等价提交模块。
- [x] 8.8 将 motion executor 调用迁到 output applier。
- [x] 8.9 将 animation presenter 调用迁到 output applier。
- [x] 8.10 将 input buffer consume 调用迁到 output applier。
- [x] 8.11 将 runtime blackboard 写入迁到 output applier。
- [x] 8.12 将 snapshot/events commit 迁到角色级 commit 阶段。

## 9. Compatibility Entrypoints
- [x] 9.1 让 `PlayerFullBodyActionController.Tick(float)` 调用唯一角色帧管线。
- [x] 9.2 让 `PlayerFullBodyActionController.Tick(BasicLocomotionInputSnapshot)` 调用唯一角色帧管线。
- [x] 9.3 让 `PlayerFullBodyActionController.Tick(CharacterFrameInput)` 调用唯一角色帧管线或兼容 adapter。
- [x] 9.4 让 `FullBodyActionTickAdapter` 的 phase handler 调用唯一角色帧管线。
- [x] 9.5 保持 rollback `FullBodyRollbackSimulation` 通过同一角色帧管线推进。
- [x] 9.6 删除不再正式使用的 FullBody phase owner 路径。
- [x] 9.7 确认 locomotion-only handler 不参与 FullBody demo 正式推进。

## 10. Naming And Static Boundaries
- [x] 10.1 静态测试确认正式最高层只有 `CharacterFramePipeline` 拥有 phase switch。
- [x] 10.2 静态测试确认 FullBody submitter 不调用 `ExecuteActionMovement`。
- [x] 10.3 静态测试确认 FullBody submitter 不调用 `ExecuteLocomotionMotion`。
- [x] 10.4 静态测试确认 FullBody submitter 不调用 `Present`。
- [x] 10.5 静态测试确认 FullBody submitter 不调用 `WriteActionFacts`。
- [x] 10.6 静态测试确认 FullBody submitter 不调用 `WriteAnimationFacts`。
- [x] 10.7 静态测试确认 request provider 不调用 motion executor。
- [x] 10.8 静态测试确认 request provider 不调用 animation presenter。
- [x] 10.9 静态测试确认 request provider 不调用状态切换 API。
- [x] 10.10 静态测试确认角色级 output applier 是 motion executor 调用入口。
- [x] 10.11 静态测试确认角色级 output applier 是 animation presenter 调用入口。
- [x] 10.12 静态测试确认没有新增第二个正式 `CharacterStateMachineRunner` owner。
- [x] 10.13 静态测试确认没有新增未审批 UpperBody/LowerBody runtime。
- [x] 10.14 静态测试确认旧 pipeline / request gate 类名不再作为正式路径。
- [x] 10.15 静态测试确认 `Action/FullBody` 目录不再承载角色级 Pipeline 文件。

## 11. Behavior Tests
- [x] 11.1 新增角色级管线 phase 顺序测试。
- [x] 11.2 新增 request submission 进入统一打断仲裁的测试。
- [x] 11.3 新增 rejected request 不消费输入、不切状态、不输出副作用的测试。
- [x] 11.4 新增 accepted request 进入状态机 context 的测试。
- [x] 11.5 新增 FullBody-only `CharacterFrameSubmission` 到 composer 的测试。
- [x] 11.6 新增 composer 只应用一个 movement 输出的测试。
- [x] 11.7 新增 composer 只应用一个 base animation 输出的测试。
- [x] 11.8 新增 input consume 只在角色级 apply 阶段发生的测试。
- [x] 11.9 新增 runtime facts 只在角色级 commit 阶段写入的测试。
- [x] 11.10 新增 WASD Idle/MoveStart/MoveLoop/MoveStop 行为保持测试。
- [x] 11.11 新增 Directional Dodge 压制基础移动输出测试。
- [x] 11.12 新增 Backstep Dodge 恢复 Locomotion 输出测试。
- [x] 11.13 新增 TurnBack 仍走统一状态机和统一输出提交的测试。

## 12. Validation
- [x] 12.1 运行 `openspec validate refactor-character-frame-submission-pipeline --strict --no-interactive`。
- [x] 12.2 运行相关 C# 项目编译检查。
- [x] 12.3 运行 `UnifiedCharacterStateMachineTests`。
- [x] 12.4 运行 `FullBodyRollbackReplayTests`。
- [x] 12.5 运行 `LocalRollbackSynctestFoundationTests`。
- [x] 12.6 运行新增 Character frame pipeline EditMode 测试。
- [x] 12.7 运行 `node .gitnexus/run.cjs detect_changes`。
