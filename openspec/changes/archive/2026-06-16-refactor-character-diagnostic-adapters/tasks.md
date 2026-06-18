## 1. Context Lock
- [x] 1.1 读取 `runtime-diagnostic-logging` 当前 spec。
- [x] 1.2 读取 `CharacterFrameDiagnostics` 所有 event id。
- [x] 1.3 读取 `LocomotionDiagnostics` 所有 event id。
- [x] 1.4 搜索 `RuntimeDiagnosticLog.Submit` 当前调用点。
- [x] 1.5 读取 active timeline facts 和 condition evaluator proposals 的 trace 约束。
- [x] 1.6 对 `CharacterFrameDiagnostics` 运行 GitNexus impact analysis。
- [x] 1.7 对 `RuntimeDiagnosticLog` 运行 GitNexus impact analysis。
- [x] 1.8 记录 HIGH/CRITICAL 风险并等待用户确认后再实施。

## 2. Boundary Tests
- [x] 2.1 静态测试：runner 不直接调用 `RuntimeDiagnosticLog.Submit`。
- [x] 2.2 静态测试：transition evaluator 不直接调用 `RuntimeDiagnosticLog.Submit`。
- [x] 2.3 静态测试：timeline sampler 不直接调用 `RuntimeDiagnosticLog.Submit`。
- [x] 2.4 静态测试：Character frame pipeline 不直接调用 `RuntimeDiagnosticLog.Submit`。
- [x] 2.5 静态测试：diagnostic trace 不引用 Unity scene object。
- [x] 2.6 行为测试：日志过滤关闭不改变 active path。
- [x] 2.7 行为测试：日志过滤关闭不改变 input consume。
- [x] 2.8 行为测试：日志过滤关闭不改变 motion execution。
- [x] 2.9 行为测试：关键 event id 仍可被观察到。

## 3. Trace And Sink Model
- [x] 3.1 创建角色帧 diagnostic trace 类型。
- [x] 3.2 创建 transition condition trace 输入模型或复用已有 trace。
- [x] 3.3 创建 timeline facts trace 输入模型或复用已有 trace。
- [x] 3.4 创建 `ICharacterDiagnosticSink` 或等价 sink。
- [x] 3.5 创建 RuntimeDiagnosticLog sink。
- [x] 3.6 创建 fake diagnostic sink for tests。
- [x] 3.7 确认 trace 不保存 Unity 对象引用。

## 4. Adapter Migration
- [x] 4.1 迁移 FullBody path changed 日志。
- [x] 4.2 迁移 pending transition changed 日志。
- [x] 4.3 迁移 action accepted/rejected 日志。
- [x] 4.4 迁移 timeline facts trace 日志。
- [x] 4.5 迁移 transition condition trace 日志。
- [x] 4.6 迁移 frame pipeline snapshot summary 日志。
- [x] 4.7 迁移 Locomotion phase changed 日志。
- [x] 4.8 迁移 TurnBack 关键诊断日志或明确保留 facade 包装。
- [x] 4.9 保持现有 event id 和 channel key。

## 5. Validation
- [x] 5.1 运行相关 Unity EditMode 定向测试。
- [x] 5.2 运行 `dotnet build 3cDemo\Client\3C_Client\Assembly-CSharp.csproj --no-restore`。
- [x] 5.3 运行 `dotnet build 3cDemo\Client\3C_Client\Assembly-CSharp-Editor.csproj --no-restore`。
- [x] 5.4 运行 `openspec validate refactor-character-diagnostic-adapters --strict --no-interactive`。
- [x] 5.5 运行 GitNexus `detect-changes` 并记录影响范围。

## 6. Scope Gates
- [x] 6.1 搜索 runner 文件，确认没有直接 `RuntimeDiagnosticLog.Submit`。
- [x] 6.2 搜索 transition evaluator 文件，确认没有直接 `RuntimeDiagnosticLog.Submit`。
- [x] 6.3 搜索 timeline sampler 文件，确认没有直接 `RuntimeDiagnosticLog.Submit`。
- [x] 6.4 搜索 frame pipeline 文件，确认没有直接 `RuntimeDiagnosticLog.Submit`。
- [x] 6.5 搜索 output runtime module 文件，确认没有 direct submit，除非该文件就是 diagnostic adapter/sink。
- [x] 6.6 搜索 trace 类型，确认没有 Unity scene object 字段。
- [x] 6.7 搜索 diagnostic adapter，确认没有调用 request resolver。
- [x] 6.8 搜索 diagnostic adapter，确认没有调用 transition evaluator。

## 7. Fine-Grained Completion Checks
- [x] 7.1 列出迁移前所有 event id 和 channel key。
- [x] 7.2 为每个 event family 指定唯一 adapter/formatter owner。
- [x] 7.3 fake sink 覆盖 FullBody path changed event。
- [x] 7.4 fake sink 覆盖 action accepted/rejected event。
- [x] 7.5 fake sink 覆盖 transition condition probe event。
- [x] 7.6 fake sink 覆盖 timeline facts probe event。
- [x] 7.7 fake sink 覆盖 Locomotion phase changed event。
- [x] 7.8 fake sink 覆盖 TurnBack probe event。
- [x] 7.9 diagnostics filter on/off 行为测试比较 active path、input consume、motion execute。
- [x] 7.10 删除迁移后不再使用的 direct submit helper。
