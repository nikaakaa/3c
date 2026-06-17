## 1. Context Lock
- [x] 1.1 读取 `CharacterFramePipelineTypes.cs` 当前全部类型。
- [x] 1.2 读取 `CharacterFramePipeline` 对 context/submission/output/result 的使用。
- [x] 1.3 读取 `FullBodySubmissionBuilder` 对 submission/context 的使用。
- [x] 1.4 读取 active `refactor-state-timeline-facts-authority` docs，确认 facts 字段归属。
- [x] 1.5 读取 active `refactor-state-action-motion-output` docs，确认 action motion result 字段归属。
- [x] 1.6 对 `CharacterFramePipelineTypes` 运行 GitNexus upstream impact analysis。
- [x] 1.7 记录 HIGH/CRITICAL 风险并等待用户确认后再实施。
- [x] 1.8 确认 frame data 正式源路径为 `Assets/Scripts/Character/Pipeline/Model/`，不是 `Assets/Scripts/Character/Action/FullBody/Model/`。

## 2. Boundary Tests
- [x] 2.1 静态测试：frame data types 不引用 `MonoBehaviour`。
- [x] 2.2 静态测试：frame data types 不引用 `Transform`。
- [x] 2.3 静态测试：frame data types 不引用 `CharacterController`。
- [x] 2.4 静态测试：frame data types 不引用 Animancer runtime types。
- [x] 2.5 静态测试：frame data types 不引用 InputAction。
- [x] 2.6 静态测试：submission 不引用 executor/presenter interfaces。
- [x] 2.7 行为测试：`CharacterFrameResult` diagnostic summary 保持稳定。
- [x] 2.8 行为测试：RunPhase 逐阶段 result 字段保持稳定。

## 3. Type Split
- [x] 3.1 在 `Assets/Scripts/Character/Pipeline/Model/` 创建 `CharacterFramePipelineStep.cs`。
- [x] 3.2 在 `Assets/Scripts/Character/Pipeline/Model/` 创建 `CharacterFrameInput.cs`。
- [x] 3.3 在 `Assets/Scripts/Character/Pipeline/Model/` 创建 `CharacterFrameContext.cs`。
- [x] 3.4 在 `Assets/Scripts/Character/Pipeline/Model/` 创建 `CharacterFrameSubmission.cs`。
- [x] 3.5 在 `Assets/Scripts/Character/Pipeline/Model/` 创建 `CharacterFrameOutput.cs`。
- [x] 3.6 在 `Assets/Scripts/Character/Pipeline/Model/` 创建 `CharacterFrameResult.cs`。
- [x] 3.7 创建 diagnostics summary helper 或保留在 result 文件。
- [x] 3.8 保持 namespace 不变。
- [x] 3.9 更新 csproj include。
- [x] 3.10 删除或瘦身旧聚合文件。
- [x] 3.11 确认没有在 `Action/FullBody/Model/` 保留角色级 frame data compat 文件。

## 4. Contract Checks
- [x] 4.1 确认 context 只由 pipeline phase mutate。
- [x] 4.2 确认 submission 由 builder 产出。
- [x] 4.3 确认 output 由 composer 产出。
- [x] 4.4 确认 result 只读对外观测。
- [x] 4.5 确认没有新增 future layer 字段。
- [x] 4.6 确认没有把 diagnostics submit 放入 data type。

## 5. Validation
- [x] 5.1 运行相关 Unity EditMode 定向测试。
- [x] 5.2 运行 `dotnet build 3cDemo\Client\3C_Client\Assembly-CSharp.csproj --no-restore`。
- [x] 5.3 运行 `dotnet build 3cDemo\Client\3C_Client\Assembly-CSharp-Editor.csproj --no-restore`。
- [x] 5.4 运行 `openspec validate refactor-character-frame-data-contracts --strict --no-interactive`。
- [x] 5.5 运行 GitNexus `detect-changes` 并记录影响范围。

## 6. Scope Gates
- [x] 6.1 搜索 frame data model，确认没有 `MonoBehaviour`。
- [x] 6.2 搜索 frame data model，确认没有 `Transform`。
- [x] 6.3 搜索 frame data model，确认没有 `CharacterController`。
- [x] 6.4 搜索 frame data model，确认没有 Animancer runtime type。
- [x] 6.5 搜索 frame data model，确认没有 InputAction。
- [x] 6.6 搜索 frame data model，确认没有 motion executor interface。
- [x] 6.7 搜索 frame data model，确认没有 animation presenter interface。
- [x] 6.8 搜索 frame data model，确认没有 diagnostic sink 或 `RuntimeDiagnosticLog`。
- [x] 6.9 搜索 `Action/FullBody`，确认没有角色级 frame data compat 文件。
- [x] 6.10 搜索 future layer 字段，确认没有提前新增 UpperBody/HitReaction/Aim placeholder。

## 7. Fine-Grained Completion Checks
- [x] 7.1 `CharacterFramePipelineStep` 独立文件只包含 phase identity。
- [x] 7.2 `CharacterFrameInput` 独立文件只包含输入快照和预测 facts。
- [x] 7.3 `CharacterFrameContext` 独立文件标明 pipeline-internal 使用。
- [x] 7.4 `CharacterFrameSubmission` 独立文件不包含任何执行依赖。
- [x] 7.5 `CharacterFrameOutput` 独立文件不包含执行方法。
- [x] 7.6 `CharacterFrameResult` 独立文件不包含副作用方法。
- [x] 7.7 diagnostics summary 独立或同 result 文件，但保持纯数据。
- [x] 7.8 csproj include 覆盖所有新 model 文件。
- [x] 7.9 旧聚合文件删除或只保留明确兼容说明。
- [x] 7.10 所有调用点只因文件拆分/using 更新而改变，不改变字段语义。
