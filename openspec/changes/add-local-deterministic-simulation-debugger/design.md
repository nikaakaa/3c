# Design: 本地确定性调试

## Context

当前正式链路是 `GameplayTickSystem -> SimulationSessionHost -> Session Source -> compiled Pipeline -> Backend transaction -> Committer -> Presentation`。本地调试必须进入这条链路，不能由 Editor、Graph、Timeline、Character 或 Animation runtime 直接推进。

当前 `RuntimeDebugSession` 的职责是 target 附着、interest、Trace Capture、history scrub 和只读 view。它不能保存 world state，也不能拥有暂停、步进或回放控制权。

## Goals

- 作者可以暂停 Local Fixed Session，并精确执行 1 个或 N 个 LocalLogicTick。
- 作者可以选择 0.25x、0.5x、1x、2x、4x 这类正式倍率播放；Tick step duration 仍固定。
- 作者可以显式开始/停止本地录制，并在有界窗口内 scrub、restore、replay、从历史 Tick 分支继续。
- 作者可以导出/导入一段 replay artifact，用于复现动作连段、KCC 接触、AI 输入或战斗窗口。
- 正常运行不保存本地 replay snapshot，不复制骨骼，不增加 Presentation 回滚状态。

## Non-Goals

- 不做网络 Rollback 的第二套实现。
- 不把 RuntimeDebugSession 改成执行控制器。
- 不把 `Time.timeScale = 0` 当成本地调试方案。
- 不让 UI 直接调用 `LogicTick`、Kernel、WorldSolver 或 Animation runtime。
- 不为 Float32 Local 承诺确定性回放。

## Runtime Chain

`Local Simulation Debugger Window`

`-> LocalSimulationDebugControlService`

`-> GameplayTickSystem Debug Drive Policy`

`-> SimulationSessionHost / ISimulationSessionRuntimeHandle`

`-> Local Fixed Source Debug Port`

`-> Standard Fixed Local Pipeline Schedule + History Egress`

`-> Fixed Backend restore/replay transaction`

`-> Fixed Committer`

`-> Character Presentation committed stream`

## Decision 1: Tick 控制放在 GameplayTickSystem

采用正式 Debug Drive Policy，包含 `Realtime`、`Paused`、`ManualStep` 和 `RatePlayback`。Editor 命令只入队，`FrameUpdate` 边界消费命令并决定本帧允许几个 fixed LocalLogicTick。

业务取舍：这样调试时看到的是正式输入、Session、Pipeline、Commit、Presentation 顺序；代价是要给 TickSystem 增加状态机，不能用最短路径直接调 Session。

不采用 `Time.timeScale = 0`：它会影响 Unity scaled delta，但不能表达单 Tick、N Tick、快进预算、replay command，也不能保证所有 gameplay 入口都服从。

不采用 Editor 直接调用 Session：它会绕过 render input、tick hook、catch-up、PresentationFrame 关系，后续 Session 多了以后会形成第二条执行链。

## Decision 2: 执行控制独立于 RuntimeDebugSession

新增 `LocalSimulationDebugControlService`。它按 SessionId、Host identity、PipelineHash、ProgramCatalogHash 和 capability 注册控制端口。`RuntimeDebugSession` 只共享 target identity 和 Trace view，不发送执行命令。

业务取舍：诊断窗口继续便宜且只读，调试控制可以清楚地失败关闭；代价是 Editor 侧需要把“看 Trace 的 target”和“控制 Session 的 target”做一次显式关联。

## Decision 3: 确定性回放只安装在 Local Fixed

Local Fixed 使用 Fixed Program、Fixed KCC、canonical input、Fixed snapshot codec 和 pipeline state snapshot，具备可重复 replay 的基础。Float32 Local 可以暂停、步进和变速，但不导出 deterministic replay artifact，不做 hash 对账承诺。

业务取舍：求职 Demo 的动作/KCC/战斗问题可以在确定性链路重放；代价是 Float32 观察不被包装成可靠回放。

## Decision 4: 录制使用 checkpoint 加 input log

录制窗口保存：

- 每 Tick canonical input batch。
- 每 Tick step hash、output summary、trace segment key。
- 每 K Tick 一个 Fixed Session checkpoint，包含 Character state、World state、Pipeline participant state 和 Source debug state。
- artifact header 中的 Program、Pipeline、Backend、Source、Solver、World、TickRate、roster、schema identity。

业务取舍：只记 Tick 下标不够，因为下一 Tick 结果还依赖黑板、Timeline、Action instance、GameplayEffect、KCC ground/support、AI state、World body 和 Pipeline cursor。每 Tick 全量 checkpoint 查找快但复制成本高。checkpoint 加 input log 在调试窗口内足够快，成本更干净。

## Decision 5: 表现层不进入 replay snapshot

Replay restore 只恢复 gameplay/world/pipeline。提交结果后，Presentation 从 committed Body/Intent/Action EventId 重建可见流。调试面板提供两种表现策略：

- `LivePresentation`：表现帧继续按 render delta 运行，适合观察正常视觉连续性。
- `LogicLockedPresentation`：暂停时 presentation delta 为 0；手动 step 成功提交后只推进一个固定表现采样量，适合逐 Tick 看动作事实和可见姿态。

业务取舍：不保存骨骼和 Animancer 状态，回放成本低；代价是它不是历史画面录屏，而是用正式输出重新投影当前视觉。

## Decision 6: Replay artifact 必须严格匹配身份

导入 replay 时必须匹配 ProgramCatalogHash、PipelineHash、Backend semantic version、SourceId、Solver identity、WorldRevision、TickRate、roster、snapshot codec 和 input schema。任一身份不匹配直接拒绝加载。

业务取舍：失败会更硬，但不会出现“导入成功但回放其实不是同一套角色/世界”的假复现。

## Failure Policy

- 控制命令 target 不唯一或 Session 未声明 debug capability：拒绝命令。
- recording buffer 未开启时请求 scrub/replay：拒绝命令。
- replay 窗口超过最旧 checkpoint：拒绝或只能从当前 live 分支继续，不做隐式平滑纠偏。
- restore/replay 任一步失败：整个 outer transaction 原子失败，Session fail-stop，Trace 记录明确失败。
- Presentation 在 replay 后异常：按现有 Presentation Fault 策略暴露错误，不回滚 gameplay。
