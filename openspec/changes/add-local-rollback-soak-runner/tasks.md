## 1. Implementation
- [x] 1.1 读取现有 synctest runner、debug runner、输入历史和快照历史实现。
- [x] 1.2 定义 soak 配置数据：seed、tickCount、rollbackFrames、stopOnFailure、applyReplayResultToScene。
- [x] 1.3 新增确定性输入生成器，覆盖 Move、Look、Run 和离散按钮事实。
- [x] 1.4 新增 soak runner，复用现有历史、simulation adapter 和 `LocalRollbackSynctestRunner`。
- [x] 1.5 新增 Play Mode debug 入口或现有 debug runner 扩展入口，支持按键触发有限 soak。
- [x] 1.6 输出 `ROLLBACK_SOAK_RESULT` 总结日志。
- [x] 1.7 首个失败只输出一条 `ROLLBACK_SOAK_FIRST_MISMATCH` 详情日志。
- [x] 1.8 hidden soak 默认结束后恢复触发前现场、表现和相机表现状态。
- [x] 1.9 新增 Editor.log rollback 关键日志过滤脚本，避免时间/soak 日志刷屏时难以复制。
- [x] 1.10 新增 F6/F8 本地验收说明，明确通过字段和失败时需要复制的日志。
- [x] 1.11 新增 Sandbox F6/F8 rollback 接线静态检查脚本，输出 `ROLLBACK_WIRING_CHECK`。
- [x] 1.12 新增 F8 soak 结果断言脚本，输出 `ROLLBACK_SOAK_ASSERT`。
- [x] 1.13 新增 Editor.log 编译错误扫描脚本，输出 `UNITY_COMPILE_LOG_CHECK`。
- [x] 1.14 新增 Unity MCP 连接诊断脚本，输出 `UNITY_MCP_CHECK`。
- [x] 1.15 新增 F8 soak 人机协作验收脚本，等待并断言 `ROLLBACK_SOAK_RESULT`，输出 `ROLLBACK_SOAK_HITL`。
- [x] 1.16 新增 F6 synctest 结果断言与人机协作验收脚本，输出 `ROLLBACK_SYNCTEST_ASSERT` 和 `ROLLBACK_SYNCTEST_HITL`。
- [x] 1.17 新增 F6+F8 组合本地 demo 人机协作验收脚本，输出 `ROLLBACK_DEMO_HITL`，并用 `visualConfirmed` 显式区分日志通过和人工画面确认。
- [x] 1.18 新增 HITL 脚本自检入口，输出 `ROLLBACK_HITL_SCRIPT_CHECK`，验证快速 F6/F8、人工确认标记和缺失 F8 失败路径。

## 2. Tests
- [x] 2.1 新增输入生成器确定性测试，同 seed 同结果、不同 seed 可产生不同序列。
- [x] 2.2 新增 soak 成功测试，验证多 tick 多窗口复用现有 synctest 收敛。
- [x] 2.3 新增 soak 失败测试，验证首个 mismatch、seed、restore/end tick 和 differences 输出。
- [x] 2.4 新增 hidden soak 恢复测试，验证 source、presentation 和 camera controller 状态不被永久污染。
- [x] 2.5 新增 Sandbox 场景接入静态测试，验证 F6/F8 runner、FullBody simulation 引用、presentation/camera 引用和低噪声日志标记。
- [ ] 2.6 运行最新版 `LocalRollbackSynctestFoundationTests` Unity Test Runner。
- [ ] 2.7 运行相关 full-body rollback replay Unity Test Runner。

## 3. Validation
- [x] 3.1 运行 `openspec validate add-local-rollback-soak-runner --strict --no-interactive`。
- [ ] 3.2 Unity Editor 当前会话 Console 检查 0 个编译错误。
- [x] 3.3 运行 `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal -m:1`。
- [x] 3.4 运行 `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal -m:1`。
- [x] 3.5 使用临时样本验证 `3cDemo/Tools/RollbackDiagnostics/Get-RollbackLog.ps1` 只输出 rollback 关键日志。
- [x] 3.6 运行 `3cDemo/Tools/RollbackDiagnostics/Test-RollbackWiring.ps1`，确认 Sandbox F6/F8 接线、hidden 模式、presentation/camera 引用和日志字段存在。
- [x] 3.7 使用临时 PASS/FAIL 样本验证 `3cDemo/Tools/RollbackDiagnostics/Test-RollbackSoakResult.ps1` 能断言最近一次 soak 结果字段。
- [x] 3.8 运行 `3cDemo/Tools/RollbackDiagnostics/Test-UnityEditorCompileLog.ps1` 扫描真实 Editor.log 最近 5000 行，并用临时失败样本验证编译错误可被检出。
- [x] 3.9 运行 `3cDemo/Tools/RollbackDiagnostics/Test-UnityMcpConnection.ps1`，确认当前阻塞状态为 server healthy、Unity 进程存在但无注册 instance。
- [x] 3.10 使用临时旧日志、追加新日志和空日志样本验证 `3cDemo/Tools/RollbackDiagnostics/Invoke-RollbackSoakHitl.ps1` 只接受启动后的新 `ROLLBACK_SOAK_RESULT`。
- [x] 3.11 使用临时 PASS/FAIL/旧日志、追加新日志和空日志样本验证 `Test-RollbackSynctestResult.ps1` 和 `Invoke-RollbackSynctestHitl.ps1`。
- [x] 3.12 使用临时追加 F6+F8 PASS 样本、快速追加样本、缺失 F8 样本和 `-ConfirmVisualStable` 验证 `Invoke-RollbackDemoHitl.ps1` 的通过、失败和人工视觉确认标记路径。
- [x] 3.13 运行 `Test-RollbackHitlScripts.ps1`，验证组合 HITL 脚本自检输出 `ROLLBACK_HITL_SCRIPT_CHECK result=PASS`，覆盖 `InitialLength` 快速日志路径。
- [ ] 3.14 手动验证：Play Mode 触发 F6 后 Console、过滤脚本、断言脚本或 HITL 脚本可搜索并验证 `[rollback-synctest] PASS`。
- [ ] 3.15 手动验证：Play Mode 触发 F8 soak 后 Console、过滤脚本、断言脚本或 HITL 脚本可搜索并验证 `ROLLBACK_SOAK_RESULT`。
- [ ] 3.16 手动验证：默认 hidden F6/F8 后角色画面和相机视角保持触发前状态。
