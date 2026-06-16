## 1. Context Lock
- [x] 1.1 读取 `refactor-character-runtime-ports` 的 proposal/design/tasks，确认端口边界已完成。
- [x] 1.2 读取 `CharacterFramePipeline`，记录 output apply phase 顺序。
- [x] 1.3 读取 `FullBodyRuntimePortAdapter`，列出当前直接委托回 controller 的 output 方法。
- [x] 1.4 读取 `PlayerFullBodyActionController` 的 `ForPipeline` output 方法区域。
- [x] 1.5 读取 `FullBodyRollbackReplayTests` 中 input consume、motion、presentation、facts、snapshot 顺序测试。
- [x] 1.6 对 `FullBodyRuntimePortAdapter` 运行 GitNexus upstream impact analysis。
- [x] 1.7 对 `PlayerFullBodyActionController` 运行 GitNexus upstream impact analysis。
- [x] 1.8 记录 HIGH/CRITICAL 风险并等待用户确认后再实施。

## 2. Characterization Tests
- [x] 2.1 新增或更新静态测试，证明 `CharacterFramePipeline` 不接收 concrete controller。
- [x] 2.2 新增静态测试，证明正式代码只有 FullBody host 创建 `CharacterStateMachineRunner`。
- [x] 2.3 新增行为测试，记录 input consume 只在 ExecuteMotion phase 发生。
- [x] 2.4 新增行为测试，记录 action motion executor 只在 ExecuteMotion phase 发生。
- [x] 2.5 新增行为测试，记录 animation presenter 只在 PresentationBridge phase 发生。
- [x] 2.6 新增行为测试，记录 runtime action facts 写入晚于 action motion resolve。
- [x] 2.7 新增行为测试，记录 snapshot update 晚于 motion 和 presentation。
- [x] 2.8 新增静态测试，证明 FullBody output module 不调用 `CharacterController.Move`。
- [x] 2.9 新增静态测试，证明 FullBody output module 不直接调用 Animancer API。

## 3. FullBody Output Modules
- [x] 3.1 创建 `FullBodyOutputRuntime` 或等价模块。
- [x] 3.2 创建 output cache writer 子职责。
- [x] 3.3 创建 input request consumer 子职责。
- [x] 3.4 创建 action/basic motion output applier 子职责。
- [x] 3.5 创建 action/locomotion animation output presenter 子职责。
- [x] 3.6 创建 runtime facts writer 子职责。
- [x] 3.7 创建 state snapshot writer 子职责。
- [x] 3.8 创建 diagnostics submit 子职责或明确保留现有 diagnostics 调用位置。
- [x] 3.9 为每个子职责提供最小构造依赖，不传完整 controller 给纯模块。
- [x] 3.10 保持 `CharacterFrameContext` 和 `CharacterFrameResult` 纯数据。

## 4. Adapter Migration
- [x] 4.1 让 `FullBodyRuntimePortAdapter.SetLastFrameOutputs` 委托 output runtime。
- [x] 4.2 让 `FullBodyRuntimePortAdapter.ConsumeStateFrameInputRequest` 委托 output runtime。
- [x] 4.3 让 `FullBodyRuntimePortAdapter.ExecuteStateFrameMotion` 委托 output runtime。
- [x] 4.4 让 `FullBodyRuntimePortAdapter.PresentStateFrameAnimation` 委托 output runtime。
- [x] 4.5 让 `FullBodyRuntimePortAdapter.WriteStateFrameActionFacts` 委托 output runtime。
- [x] 4.6 让 `FullBodyRuntimePortAdapter.UpdateStateSnapshot` 委托 output runtime。
- [x] 4.7 让 `FullBodyRuntimePortAdapter.WriteAnimationRuntimeFacts` 委托 output runtime。
- [x] 4.8 让 `FullBodyRuntimePortAdapter.CompleteLocomotionTick` 委托 output runtime。
- [x] 4.9 让 `FullBodyRuntimePortAdapter.LogDiagnosticTickSnapshots` 委托 output runtime 或 diagnostics module。

## 5. Controller Narrowing
- [x] 5.1 将 `PlayerFullBodyActionController` 中 output `ForPipeline` 方法改为薄装配或删除。
- [x] 5.2 保留 `RuntimePort` 生产入口。
- [x] 5.3 保留 runner rebuild/restore ownership。
- [x] 5.4 保留 config/root/action binding 解析。
- [x] 5.5 保留 debug fields 的语义。
- [x] 5.6 确认 controller 不新增更宽的 replacement interface。

## 6. Validation
- [x] 6.1 运行相关 Unity EditMode 定向测试。
- [x] 6.2 运行 `dotnet build 3cDemo\Client\3C_Client\Assembly-CSharp.csproj --no-restore`。
- [x] 6.3 运行 `dotnet build 3cDemo\Client\3C_Client\Assembly-CSharp-Editor.csproj --no-restore`。
- [x] 6.4 运行 `openspec validate refactor-fullbody-output-runtime-modules --strict --no-interactive`。
- [x] 6.5 运行 GitNexus `detect-changes` 并记录影响范围。

## 7. Scope Gates
- [x] 7.1 搜索 `CharacterFramePipeline`，确认没有新增 concrete controller 类型引用。
- [x] 7.2 搜索 FullBody output module 文件，确认没有直接引用 `CharacterController.Move`。
- [x] 7.3 搜索 FullBody output module 文件，确认没有直接引用 Animancer 播放入口。
- [x] 7.4 搜索 FullBody output module 文件，确认没有引用 `CharacterStateMachineDefinition`。
- [x] 7.5 搜索 FullBody output module 文件，确认没有调用 transition evaluator。
- [x] 7.6 搜索 FullBody output module 文件，确认没有调用 action request resolver。
- [x] 7.7 搜索 `PlayerFullBodyActionController`，确认 output `ForPipeline` 方法没有继续增加。
- [x] 7.8 搜索正式代码，确认没有第二条 action/basic motion executor 路径。

## 8. Fine-Grained Completion Checks
- [x] 8.1 `FullBodyRuntimePortAdapter` 每个 pipeline-facing 方法都有明确委托目标。
- [x] 8.2 `FullBodyOutputRuntime` 的 constructor/init 依赖不包含完整 controller。
- [x] 8.3 input consume 模块只处理已经接受的 request id/type。
- [x] 8.4 motion applier 模块只处理已经 resolve 的 command/result。
- [x] 8.5 animation presenter 模块只处理已经 resolve 的 presentation request。
- [x] 8.6 facts writer 模块只写本帧事实，不读取下一帧输入。
- [x] 8.7 snapshot writer 模块只提交 runner 输出，不推进 runner。
- [x] 8.8 diagnostics 子职责只提交现有日志，不定义新日志 schema。
- [x] 8.9 rollback fixture 仍通过同一生产 adapter 进入 pipeline。
- [x] 8.10 删除所有仅为迁移临时存在且不被生产路径使用的 wrapper。
