## 1. Context Lock
- [x] 1.1 读取并确认 `refactor-character-frame-submission-pipeline` 已完成的正式类型和调用点。
- [x] 1.2 读取并确认 `refactor-locomotion-frame-pipeline-mainline` 对 `LocomotionFrameBuilder` 的职责边界。
- [x] 1.3 读取当前 `PlayerLocomotionController` 的 direct tick、frame builder facade、snapshot、diagnostic/reference/camera/facing 区域。
- [x] 1.4 读取当前 `PlayerFullBodyActionController` 的 tick host、for-pipeline 方法、runner rebuild、reference resolve、interrupt policy cache 区域。
- [x] 1.5 读取 `CharacterFramePipelineTypes`，确认本变更不做模型文件大拆分。
- [x] 1.6 读取 `CharacterStateMachineTypes`，确认本变更不做状态机 model 口径重排。
- [x] 1.7 对 `CharacterFramePipeline` 运行 GitNexus upstream impact analysis。
- [x] 1.8 对 `FullBodySubmissionBuilder` 运行 GitNexus upstream impact analysis。
- [x] 1.9 对 `PlayerFullBodyActionController` 运行 GitNexus upstream impact analysis。
- [x] 1.10 对 `PlayerLocomotionController` 运行 GitNexus upstream impact analysis。
- [x] 1.11 记录 HIGH/CRITICAL 风险并停止实施，直到用户确认继续。

## 2. Character Frame Runtime Port Tests
- [x] 2.1 新增静态测试，证明 `CharacterFramePipeline` 当前不应直接依赖 `PlayerFullBodyActionController`。
- [x] 2.2 新增 fake runtime port 测试，覆盖 `ReadInput -> WriteSnapshotAndEvents` phase 顺序。
- [x] 2.3 新增 fake runtime port 测试，覆盖 `UpdateInputBuffer` 早于 `GameplayDecision`。
- [x] 2.4 新增 fake runtime port 测试，覆盖 `ExecuteMotion` 只在对应 phase 发生。
- [x] 2.5 新增 fake runtime port 测试，覆盖 `PresentationBridge` 晚于 motion apply。

## 3. Character Frame Runtime Port
- [x] 3.1 在 `Character/Pipeline/Contracts` 或等价角色级目录创建角色帧运行时端口。
- [x] 3.2 将 input buffer 写入能力纳入端口。
- [x] 3.3 将 FullBody submission 构建入口纳入端口。
- [x] 3.4 将 output cache/apply/presentation/facts 写入能力纳入端口。
- [x] 3.5 将 diagnostics commit 能力纳入端口。
- [x] 3.6 迁移 `CharacterFramePipeline.Tick` 使用端口。
- [x] 3.7 迁移 `CharacterFramePipeline.RunPhase` 使用端口。
- [x] 3.8 保持 `CharacterFrameContext` 和 `CharacterFrameResult` 纯数据。
- [x] 3.9 确认 `CharacterFramePipeline` 不通过端口回传完整 controller。

## 4. FullBody Runtime Adapter
- [x] 4.1 创建 `FullBodyRuntimePortAdapter` 或等价包装 adapter。
- [x] 4.2 让包装 adapter 持有或引用 `PlayerFullBodyActionController`，但不让 pipeline 接收 concrete controller。
- [x] 4.3 将 `PlayerFullBodyActionController.Tick(float)` 转发到端口化 pipeline。
- [x] 4.4 将 `PlayerFullBodyActionController.Tick(BasicLocomotionInputSnapshot)` 转发到端口化 pipeline。
- [x] 4.5 将 `PlayerFullBodyActionController.Tick(CharacterFrameInput)` 转发到端口化 pipeline。
- [x] 4.6 保持 `PlayerFullBodyActionController` 为唯一正式 runner owner。
- [x] 4.7 保持 config/root/action binding 解析由 FullBody host 装配。
- [x] 4.8 保持现有日志 event id 不删除。
- [x] 4.9 确认 `PlayerFullBodyActionController` 不直接实现所有 pipeline port 后继续膨胀为操作面板。

## 5. FullBody Submission Runtime Port
- [x] 5.1 新增 FullBody submission 所需端口或收窄现有角色端口。
- [x] 5.2 将 state machine runner 访问收敛到端口。
- [x] 5.3 将 current snapshot 访问收敛到端口。
- [x] 5.4 将 input buffer 访问收敛到端口。
- [x] 5.5 将 Dodge config 访问收敛到端口。
- [x] 5.6 将 interrupt policy 访问收敛到端口。
- [x] 5.7 将 current action resistance 访问收敛到端口。
- [x] 5.8 迁移 `FullBodySubmissionBuilder.TryBuildStateSubmission` 不接收 concrete controller。
- [x] 5.9 迁移 `FullBodySubmissionBuilder.TryBuildFrameSubmission` 不接收 concrete controller。
- [x] 5.10 保持 request submission 仍进入统一打断仲裁。
- [x] 5.11 保持 frame submission 不执行副作用。

## 6. Locomotion Runtime Port
- [x] 6.1 新增 `ILocomotionFrameRuntimePort` 或等价 prepare/build 端口。
- [x] 6.2 新增 `ILocomotionOutputRuntimePort` 或等价 output/apply 端口。
- [x] 6.3 将 prepare decision frame 能力纳入 frame runtime port。
- [x] 6.4 将 prepared gameplay decision 评估能力纳入 frame runtime port。
- [x] 6.5 将 state decision 到 motion frame 构建能力纳入 frame runtime port。
- [x] 6.6 将 runtime blackboard snapshot 读取能力纳入 frame runtime port。
- [x] 6.7 将 runtime action facts 写入能力纳入 output runtime port。
- [x] 6.8 将 animation facts 写入能力纳入 output runtime port。
- [x] 6.9 将 motion executor apply 能力纳入 output runtime port。
- [x] 6.10 将 locomotion animation presentation 能力纳入 output runtime port。
- [x] 6.11 保留 playback/window/snapshot restore 语义不变。
- [x] 6.12 保持 Locomotion 直接 tick 入口只作为迁移诊断或测试工具。
- [x] 6.13 确认 FullBody submission 不再通过完整 `PlayerLocomotionController` 类型访问 Locomotion 子职责。
- [x] 6.14 确认 camera/facing resolve 不进入 `LocomotionFrameBuilder`。
- [x] 6.15 确认 rollback snapshot capture/restore 语义保留在 runtime adapter 或现有 rollback authority，不被 builder 接管。
- [x] 6.16 确认没有新增单个巨大 `ILocomotionRuntimePort` 复制完整 controller Interface。

## 7. Static Boundary Tests
- [x] 7.1 静态测试：`CharacterFramePipeline` 不包含 `PlayerFullBodyActionController` 类型引用。
- [x] 7.2 静态测试：`FullBodySubmissionBuilder` 不包含 `PlayerFullBodyActionController` 类型引用。
- [x] 7.3 静态测试：`FullBodySubmissionBuilder` 不包含 `PlayerLocomotionController` 类型引用。
- [x] 7.4 静态测试：端口契约不引用 `MonoBehaviour`。
- [x] 7.5 静态测试：端口契约不引用 `Transform`。
- [x] 7.6 静态测试：端口契约不引用 `CharacterController`。
- [x] 7.7 静态测试：端口契约不引用 Animancer runtime 类型。
- [x] 7.8 静态测试：端口契约不引用 InputAction。
- [x] 7.9 静态测试：`LocomotionFrameBuilder` 仍不执行 motion 或 animation。
- [x] 7.10 静态测试：正式运行时代码仍只有 FullBody host 创建 `CharacterStateMachineRunner`。
- [x] 7.11 静态测试：端口化后 `CharacterFramePipelineTypes` 不新增 motion executor、animation presenter 或 Unity scene object 字段。
- [x] 7.12 静态测试：本变更不把 `CharacterStateMachineTypes` 的业务 condition/model 口径作为顺手重排内容。
- [x] 7.13 静态测试：FullBody 生产路径存在包装 adapter，pipeline 不直接接收 `PlayerFullBodyActionController`。
- [x] 7.14 静态测试：Locomotion frame runtime port 和 output runtime port 分离，且不存在复制完整 controller Interface 的单一巨型端口。
- [x] 7.15 静态测试：`ICharacterFrameRuntimePort` 不留在 `Action/FullBody` 目录。

## 8. Behavior Regression Tests
- [x] 8.1 更新 FullBody rollback replay 测试以使用端口化 pipeline。
- [x] 8.2 覆盖 Directional Dodge request submission 和 output apply。
- [x] 8.3 覆盖 Backstep Dodge request submission 和 output apply。
- [x] 8.4 覆盖 TurnBack request submission 和 output apply。
- [x] 8.5 覆盖 MoveStart / MoveLoop / MoveStop 基础移动输出不变。
- [x] 8.6 覆盖 runtime blackboard action facts 写入不变。
- [x] 8.7 覆盖 animation runtime facts 写入不变。
- [x] 8.8 覆盖 input consume 仍只在 output apply 阶段发生。
- [x] 8.9 覆盖 snapshot update 仍晚于 motion 和 presentation。

## 9. Build And Spec Validation
- [x] 9.1 运行相关 Unity EditMode 定向测试。
- [x] 9.2 运行 `dotnet build` 验证 C# 编译。
- [x] 9.3 运行 `openspec validate refactor-character-runtime-ports --strict --no-interactive`。
- [x] 9.4 运行 GitNexus `detect_changes()` 或 CLI 等价命令检查影响范围。
- [x] 9.5 确认任务全部完成后更新 checklist。

