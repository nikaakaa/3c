## 1. Apply 阶段准备
- [x] 1.1 读取本 change 的 `proposal.md`。
- [x] 1.2 读取本 change 的 `design.md`。
- [x] 1.3 读取本 change 的 `tasks.md`。
- [x] 1.4 读取本 change 的 `character-runtime-ports` spec delta。
- [x] 1.5 读取本 change 的 `fullbody-rollback-replay` spec delta。
- [x] 1.6 对 `CharacterFramePipeline` 运行 GitNexus impact analysis，并记录 blast radius。
- [x] 1.7 对 `PlayerFullBodyActionController` 运行 GitNexus impact analysis，并记录 blast radius。
- [x] 1.8 对 `FullBodyActionTickAdapter` 运行 GitNexus impact analysis，并记录 blast radius。
- [x] 1.9 对 `FullBodySubmissionBuilder` 运行 GitNexus impact analysis，并记录 blast radius。
- [x] 1.10 若任一 impact 为 HIGH 或 CRITICAL，先向用户报告风险，再继续实现。

## 2. 测试先行
- [x] 2.1 在静态边界测试中断言 `CharacterFramePipeline` 不包含 `new FullBodySubmissionBuilder`。
- [x] 2.2 在静态边界测试中断言 `CharacterFramePipeline` 不直接引用 FullBody 生产 submitter 具体类。
- [x] 2.3 在静态边界测试中断言 `PlayerFullBodyActionController` 不包含 `new CharacterFramePipeline`。
- [x] 2.4 在静态边界测试中断言 `FullBodyActionTickAdapter` 不包含 `new CharacterFramePipeline`。
- [x] 2.5 在静态边界测试中断言 `CharacterFramePipelineHost` 位于角色级 Pipeline runtime 目录。
- [x] 2.6 在静态边界测试中断言角色帧 submitter Interface 位于角色级 Pipeline contracts 目录。
- [x] 2.7 更新 `CharacterFramePipeline` phase order 测试，使测试入口通过 host 或显式注入 submitter。
- [x] 2.8 更新 replay 相关测试，使 `PredictionInputFrame` 通过 host 进入正式管线。

## 3. 角色帧提交 Interface
- [x] 3.1 新增 request submission Interface。
- [x] 3.2 新增 frame output submission Interface。
- [x] 3.3 确认两个 Interface 不引用 `MonoBehaviour`。
- [x] 3.4 确认两个 Interface 不引用 `Transform`。
- [x] 3.5 确认两个 Interface 不引用 `CharacterController`。
- [x] 3.6 确认两个 Interface 不引用 Animancer runtime 类型。
- [x] 3.7 确认两个 Interface 不引用 InputAction。
- [x] 3.8 确认 request submitter 只服务 `GameplayDecision` phase。
- [x] 3.9 确认 output submitter 只服务 `BuildMotion` phase。

## 4. CharacterFramePipelineHost
- [x] 4.1 在 `Assets/Scripts/Character/Pipeline/Runtime/...` 新增纯 C# `CharacterFramePipelineHost`。
- [x] 4.2 让 host 持有唯一 `CharacterFramePipeline` 实例。
- [x] 4.3 让 host 持有 request submitter Interface。
- [x] 4.4 让 host 持有 frame output submitter Interface。
- [x] 4.5 为一帧推进提供 `Tick` 或等价入口。
- [x] 4.6 为 simulation tick phase 推进提供 Begin/RunPhase 或等价入口。
- [x] 4.7 让 host 暴露最近一次 `CharacterFrameResult`。
- [x] 4.8 确认 host 不是 MonoBehaviour。
- [x] 4.9 确认 host 不创建第二个状态机 runner。
- [x] 4.10 确认 host 不创建第二个 motion executor。
- [x] 4.11 确认 host 不创建第二个 animation presenter。

## 5. CharacterFramePipeline 注入化
- [x] 5.1 为 `CharacterFramePipeline` 增加接收 submitter Interface 的构造路径。
- [x] 5.2 移除 `CharacterFramePipeline` 内部的 `FullBodySubmissionBuilder` 字段。
- [x] 5.3 `GameplayDecision` phase 改为通过 request submitter 提交纯数据。
- [x] 5.4 `BuildMotion` phase 改为通过 frame output submitter 生成 `CharacterFrameSubmission`。
- [x] 5.5 保持 `CharacterFrameOutputComposer` 仍在 output apply 之前运行。
- [x] 5.6 保持 `CharacterFrameOutputApplier` 仍是 motion、animation、input consume、runtime facts、snapshot 和 diagnostics 的唯一副作用应用点。
- [x] 5.7 确认 `CharacterFramePipeline` 不直接引用 `PlayerFullBodyActionController`。
- [x] 5.8 确认 `CharacterFramePipeline` 不直接引用 FullBody 生产 submitter 具体类。

## 6. FullBody 提交者适配
- [x] 6.1 将现有 FullBody request submission 逻辑接入 request submitter Interface。
- [x] 6.2 将现有 FullBody frame output submission 逻辑接入 output submitter Interface。
- [x] 6.3 保持 action request 仍经过 `CommittedActionRequestSubmissionResolver` 和统一状态机。
- [x] 6.4 保持 action motion resolve 仍在 frame output submission 阶段生成纯数据。
- [x] 6.5 确认 FullBody request submitter 不执行 motion。
- [x] 6.6 确认 FullBody request submitter 不播放 animation。
- [x] 6.7 确认 FullBody output submitter 不执行 motion。
- [x] 6.8 确认 FullBody output submitter 不播放 animation。
- [x] 6.9 确认 FullBody 提交者不创建 pipeline。
- [x] 6.10 确认 FullBody 提交者不创建 runner。

## 7. Unity Adapter 持有关系迁移
- [x] 7.1 让 `PlayerFullBodyActionController` 懒创建或持有 `CharacterFramePipelineHost`。
- [x] 7.2 移除 `PlayerFullBodyActionController` 对 `CharacterFramePipeline` 的直接字段。
- [x] 7.3 让 `PlayerFullBodyActionController.Tick` 通过 host 推进一帧。
- [x] 7.4 让 `PlayerFullBodyActionController.LastFramePipelineResult` 来自 host 的最近结果。
- [x] 7.5 让 `FullBodyActionTickAdapter` 使用同一个 host 的逐 phase 入口。
- [x] 7.6 移除 `FullBodyActionTickAdapter` 对 `CharacterFramePipeline` 的直接字段。
- [x] 7.7 确认 `FullBodyActionTickAdapter` 不创建独立 host。
- [x] 7.8 保持 `FullBodyActionTickAdapter` 对 request buffer step 的处理顺序不变。
- [x] 7.9 保持 Locomotion auto update 禁用/恢复语义不变。

## 8. Replay 与 synctest 收敛
- [x] 8.1 更新 FullBody replay 相关测试入口，使 replay 通过 host 推进。
- [x] 8.2 确认 `PredictionInputFrame` 仍转换为 `CharacterFrameInput`。
- [x] 8.3 确认离散输入事实仍在 UpdateInputBuffer phase 写入。
- [x] 8.4 确认 Dodge replay 仍经过 GameplayDecision。
- [x] 8.5 确认 replay 不直接调用 `BasicLocomotionPipeline` 作为 FullBody 最终路径。
- [x] 8.6 确认 replay 不直接创建 `CharacterFramePipeline`。
- [x] 8.7 确认 replay 快照仍来自正式 pipeline 输出后的 runtime facts 和 restore state。

## 9. 自动验证
- [x] 9.1 运行 `UnifiedCharacterStateMachineTests` 中的角色帧管线边界相关 EditMode 测试。
- [x] 9.2 运行 `FullBodyRollbackReplayTests` 中的 pipeline replay 相关 EditMode 测试。
- [x] 9.3 运行 `dotnet build 3cDemo/Client/3C_Client/Assembly-CSharp.csproj --no-restore`。
- [x] 9.4 运行 `dotnet build 3cDemo/Client/3C_Client/Assembly-CSharp-Editor.csproj --no-restore`。
- [x] 9.5 运行 `openspec validate refactor-character-frame-pipeline-host-ownership --strict --no-interactive`。
- [x] 9.6 运行 GitNexus `detect_changes()`，确认 affected symbols 和 execution flows 与本 change 范围一致。
