## ADDED Requirements
### Requirement: 本地回滚 Soak 长跑验证
系统 MUST 提供有限时长的本地 rollback soak 验证能力，用固定 seed 生成输入流并重复执行现有 restore/replay/compare 管线。Soak MUST 复用 `PredictionInputHistory`、`PredictionSnapshotHistory`、`ILocalRollbackSynctestSimulation` 和 `LocalRollbackSynctestRunner`，不得直接调用 `BasicLocomotionPipeline`、`CharacterController.Move` 或新增第二套角色推进路径。

#### Scenario: 固定 seed 可复现
- **GIVEN** soak 配置包含 seed、tickCount 和 rollbackFrames
- **WHEN** 使用同一配置运行两次输入生成
- **THEN** 两次生成的 `PredictionInputFrame` 序列 MUST 完全一致

#### Scenario: 长跑成功输出单条总结
- **GIVEN** soak 在所有窗口中 restore/replay/compare 均通过
- **WHEN** soak 运行结束
- **THEN** Console MUST 输出包含 `ROLLBACK_SOAK_RESULT` 的总结日志
- **AND** 日志 MUST 包含 seed、tickCount、rollbackFrames、checkedWindows 和 result=PASS

#### Scenario: 首个失败低噪声诊断
- **GIVEN** soak 的某个窗口出现 snapshot mismatch
- **WHEN** stopOnFailure 为 true
- **THEN** soak MUST 停止在首个失败窗口
- **AND** Console MUST 输出一条包含 `ROLLBACK_SOAK_FIRST_MISMATCH` 的详情日志
- **AND** 详情 MUST 包含 seed、restore tick、end tick、first mismatch tick 和 differences

#### Scenario: 刷屏时过滤 rollback 关键日志
- **GIVEN** Unity Console 或 Editor.log 存在大量非 rollback 日志
- **WHEN** 开发者需要收集本地 rollback 验证证据
- **THEN** 系统 MUST 提供本地过滤方式，只输出 `ROLLBACK_SOAK_RESULT`、`ROLLBACK_SOAK_FIRST_MISMATCH`、`ROLLBACK_TIMING_PROBE` 或 `[rollback-synctest]` 关键行

#### Scenario: Sandbox 接线可静态验证
- **GIVEN** Unity Editor 当前会话不可用或无法运行 Unity Test Runner
- **WHEN** 开发者需要确认本地 rollback debug 入口没有断线
- **THEN** 系统 MUST 提供本地静态检查方式，确认 Sandbox 中 F6 和 F8 runner 处于 hidden 模式，并引用 FullBody simulation、presentation interpolator 和 camera controller
- **AND** 检查结果 MUST 输出包含 `ROLLBACK_WIRING_CHECK` 的单行结果

#### Scenario: F8 soak 结果可本地断言
- **GIVEN** Play Mode 已触发 F8 soak 并写入 `ROLLBACK_SOAK_RESULT`
- **WHEN** 开发者需要确认最近一次 F8 soak 是否满足 hidden restore 要求
- **THEN** 系统 MUST 提供本地断言方式，检查最近一条结果包含 `result=PASS`、`applyReplay=False`、`sourceRestored=True`、`visualRestored=True`、`cameraRestored=True`、`visualChecked=True` 和 `cameraChecked=True`
- **AND** 断言结果 MUST 输出包含 `ROLLBACK_SOAK_ASSERT` 的单行结果

#### Scenario: F8 soak 可人机协作验收
- **GIVEN** Unity MCP 当前会话不可用且开发者可以手动操作 Unity Editor
- **WHEN** 开发者启动本地 HITL 验收并在 Play Mode 按 F8
- **THEN** 系统 MUST 等待 `ROLLBACK_SOAK_RESULT` 出现并复用本地断言方式检查最近一次结果
- **AND** HITL 验收 MUST 输出包含 `ROLLBACK_SOAK_HITL` 的低噪声结果

#### Scenario: F6 synctest 可人机协作验收
- **GIVEN** Unity MCP 当前会话不可用且开发者可以手动操作 Unity Editor
- **WHEN** 开发者启动本地 HITL 验收并在 Play Mode 按 F6
- **THEN** 系统 MUST 等待 `[rollback-synctest]` 结果出现并检查最近一次结果为 PASS
- **AND** HITL 验收 MUST 输出包含 `ROLLBACK_SYNCTEST_HITL` 的低噪声结果

#### Scenario: 本地回滚 demo 可组合验收
- **GIVEN** Unity MCP 当前会话不可用且开发者可以手动操作 Unity Editor
- **WHEN** 开发者启动组合 HITL 验收并依次在 Play Mode 触发 F6 与 F8
- **THEN** 系统 MUST 先验证 F6 synctest PASS，再验证 F8 soak PASS
- **AND** 组合验收 MUST 输出包含 `ROLLBACK_DEMO_HITL` 的低噪声结果
- **AND** 组合验收 MUST 显式输出人工画面稳定确认状态，不得把日志通过自动当作画面稳定已确认

#### Scenario: HITL 脚本可自检
- **GIVEN** 开发者需要确认本地 HITL 验收脚本自身没有误判旧日志或丢失快速按键日志
- **WHEN** 运行 HITL 脚本自检
- **THEN** 系统 MUST 使用临时日志样本验证 F6+F8 通过、人工视觉确认标记通过和缺失 F8 失败路径
- **AND** 自检 MUST 输出包含 `ROLLBACK_HITL_SCRIPT_CHECK` 的单行结果

#### Scenario: Editor.log 编译错误可低噪声扫描
- **GIVEN** Unity MCP 当前会话不可用或无法读取 Console
- **WHEN** 开发者需要辅助确认最近 Editor.log 是否包含 C# 编译错误
- **THEN** 系统 MUST 提供本地扫描方式，检查最近日志中的 `error CS` 或 Unity 编译失败标记
- **AND** 扫描结果 MUST 输出包含 `UNITY_COMPILE_LOG_CHECK` 的单行结果

#### Scenario: Unity MCP 连接状态可低噪声诊断
- **GIVEN** 本地 Unity Test Runner 无法通过 MCP 启动
- **WHEN** 开发者需要区分 server、Unity 进程和 instance 注册状态
- **THEN** 系统 MUST 提供本地诊断方式，检查 MCP server health、Unity 进程和 `/api/instances`
- **AND** 诊断结果 MUST 输出包含 `UNITY_MCP_CHECK` 的单行结果

#### Scenario: Hidden soak 不污染当前现场
- **GIVEN** soak 未启用应用 replay 结果到场景
- **WHEN** soak 触发后内部执行多次 restore/replay
- **THEN** 结束后真实模拟根 MUST 恢复到触发前现场
- **AND** 已配置的表现插值状态和相机 controller 表现状态 MUST 恢复到触发前状态
