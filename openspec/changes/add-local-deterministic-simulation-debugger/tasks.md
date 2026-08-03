# Tasks

## 1. Tick 驱动策略

- [x] 1.1 增加 `GameplayTickDriveMode` 正式枚举。
- [x] 1.2 增加 `GameplayTickDrivePolicy` 不可变状态。
- [x] 1.3 增加 `GameplayTickDriveCommand` 命令类型。
- [x] 1.4 在 `GameplayTickSystem` 增加 frame-boundary 命令队列。
- [x] 1.5 让 realtime 模式保持当前 accumulator 行为。
- [x] 1.6 让 paused 模式冻结 logic accumulator。
- [x] 1.7 让 manual step 命令精确推进 1 个 LocalLogicTick。
- [x] 1.8 让 multi-step 命令按预算推进 N 个 LocalLogicTick。
- [x] 1.9 让 rate playback 使用固定倍率改变 accumulator admission。
- [x] 1.10 保持每个 LocalLogicTick 只调用一次正式 logic target。
- [x] 1.11 将当前 drive state 暴露为只读 snapshot。

## 2. 表现调试时钟

- [x] 2.1 增加 `GameplayPresentationDebugClockMode`。
- [x] 2.2 在 `GameplayPresentationFrameContext` 增加正式 presentation debug delta。
- [x] 2.3 让 `LivePresentation` 保持当前 render delta。
- [x] 2.4 让 `LogicLockedPresentation` 在 paused 时输出 0 delta。
- [x] 2.5 让 manual step 成功后输出一个 fixed presentation pulse。
- [x] 2.6 让 Presentation target 只读取正式 context，不自行查询 TickSystem 状态。

## 3. Session 调试控制端口

- [x] 3.1 定义 `ISimulationSessionDebugControlPort`。
- [x] 3.2 定义 `SimulationSessionDebugCapabilityDescriptor`。
- [x] 3.3 定义 `SimulationSessionDebugCommand`。
- [x] 3.4 定义 `SimulationSessionDebugStatusSnapshot`。
- [x] 3.5 让 `SimulationSessionHost` 在 Active 后注册 debug control port。
- [x] 3.6 让 `SimulationSessionHost` 在 Dispose/Failed 时注销 debug control port。
- [x] 3.7 按 SessionId、HostInstanceId、PipelineHash 和 ProgramCatalogHash 精确解析 target。
- [x] 3.8 拒绝多 target、ended target 和不支持 capability 的命令。
- [x] 3.9 保持 RuntimeDebugSession 只共享 target identity，不持有 debug control port。

## 4. Local Fixed 录制窗口

- [ ] 4.1 定义 Local Fixed debug capture profile。
- [ ] 4.2 配置 capture tick capacity。
- [ ] 4.3 配置 checkpoint interval。
- [ ] 4.4 在 Local Fixed Source descriptor 声明 debug replay capability。
- [ ] 4.5 增加 Local Fixed debug source runtime state。
- [ ] 4.6 记录每 Tick canonical input batch。
- [ ] 4.7 记录每 Tick SimulationTick、LocalLogicTick 和 source mapping。
- [ ] 4.8 记录每 Tick state hash。
- [ ] 4.9 记录每 Tick output summary。
- [ ] 4.10 记录 Trace segment key。
- [ ] 4.11 预分配 capture ring buffer。
- [ ] 4.12 未开始 capture 时不创建 per-tick snapshot。

## 5. Checkpoint 与 Restore

- [ ] 5.1 复用 Fixed Session snapshot codec 捕获 checkpoint。
- [ ] 5.2 将 Character state 纳入 checkpoint。
- [ ] 5.3 将 World state 纳入 checkpoint。
- [ ] 5.4 将 Pipeline participant state 纳入 checkpoint。
- [ ] 5.5 将 Local Fixed debug source state 纳入 checkpoint。
- [ ] 5.6 为 checkpoint 计算 stable hash。
- [ ] 5.7 在 capture ring 中保存 checkpoint identity。
- [ ] 5.8 增加从最近 checkpoint 恢复到目标 Tick 的 planner。
- [ ] 5.9 restore 前校验完整 composition identity。

## 6. Replay 调度

- [ ] 6.1 扩展 Local Fixed Schedule 支持 Debug Replay directive。
- [ ] 6.2 将 replay range 降低为 `SimulationRestoreDirective`。
- [ ] 6.3 为每个 replay Tick 生成 `Replay` step。
- [ ] 6.4 replay 使用 recorded canonical input。
- [ ] 6.5 replay 不提交中间 Presentation 输出。
- [ ] 6.6 replay 完成后只提交最终连续分支。
- [ ] 6.7 replay hash 与 recorded hash 不一致时 fail-stop。
- [ ] 6.8 从历史 Tick resume 时截断未来 capture window。
- [ ] 6.9 resume 后重新开始 live recording generation。

## 7. Replay Artifact

- [ ] 7.1 定义 replay artifact header。
- [ ] 7.2 写入 ProgramCatalogHash。
- [ ] 7.3 写入 PipelineHash。
- [ ] 7.4 写入 Backend identity。
- [ ] 7.5 写入 Source identity。
- [ ] 7.6 写入 Solver identity 和 WorldRevision。
- [ ] 7.7 写入 TickRate、roster 和 schema identity。
- [ ] 7.8 写入 checkpoint payload。
- [ ] 7.9 写入 canonical input log。
- [ ] 7.10 写入 expected hash log。
- [ ] 7.11 导入时逐项校验 identity。
- [ ] 7.12 identity 不匹配时拒绝加载。

## 8. Editor 调试窗口

- [x] 8.1 增加 Local Simulation Debugger Editor window。
- [x] 8.2 显示可控制 Session 列表。
- [x] 8.3 显示当前 drive mode。
- [x] 8.4 提供 pause/resume。
- [x] 8.5 提供 step 1。
- [x] 8.6 提供 step N。
- [x] 8.7 提供倍率选择。
- [ ] 8.8 提供 start/stop recording。
- [ ] 8.9 提供 history slider。
- [ ] 8.10 提供 replay range。
- [ ] 8.11 提供 resume from tick。
- [ ] 8.12 显示 latest tick、checkpoint、hash 和 failure。
- [x] 8.13 UI 不直接访问 Character、WorldSolver、Animation 或 Transform。

## 9. Diagnostics 对接

- [ ] 9.1 为 debug command 发布 Trace event。
- [ ] 9.2 为 recording start/stop 发布 Trace event。
- [ ] 9.3 为 checkpoint capture 发布 Trace event。
- [ ] 9.4 为 restore/replay 发布 Trace event。
- [ ] 9.5 为 hash mismatch 发布 Trace event。
- [ ] 9.6 在 RuntimeDebugSession view 中显示关联 replay tick。
- [ ] 9.7 保持 Trace capture scrub 不回滚 runtime。

## 10. 规格收口

- [ ] 10.1 对账 current `gameplay-tick-system` spec。
- [ ] 10.2 对账 current `gameplay-simulation-session-composition` spec。
- [ ] 10.3 对账 current `gameplay-simulation-pipeline` spec。
- [ ] 10.4 对账 current `btsmtl-runtime-diagnostics` spec。
- [ ] 10.5 对账 current `character-presentation-interpolation` spec。
- [ ] 10.6 更新 implementation inventory。
- [x] 10.7 运行 `openspec validate add-local-deterministic-simulation-debugger --strict --no-interactive`。
