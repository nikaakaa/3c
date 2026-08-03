# local-simulation-debugging Specification

## ADDED Requirements

### Requirement: 本地调试必须通过正式 Session Debug Control Service 控制

本地调试 MUST 通过 `LocalSimulationDebugControlService` 或等价正式服务控制 Active Session。控制目标 MUST 由 SessionId、HostInstanceId、ProgramCatalogHash、PipelineHash、Source identity、Solver identity 和 capability 精确绑定。Editor UI MUST 只发送 typed command 并读取 status snapshot，MUST NOT 直接调用 Character、WorldSolver、Kernel、Pipeline Pass、Animation runtime、Transform 或 `ISimulationSessionRuntimeHandle.LogicTick`。

#### Scenario: 作者选择一个 Local Fixed Session

- **WHEN** Editor Window 选择一个已注册 Local Fixed Session
- **THEN** Debug Control Service MUST 校验该 Session 的完整 identity 和 debug capability
- **AND** 命令 MUST 进入该 Session 的正式控制端口
- **AND** UI MUST 不直接推进任何 Actor 或 Solver

#### Scenario: 多个 Session 同时存在

- **WHEN** target 条件匹配多个 Active Session
- **THEN** Debug Control Service MUST 拒绝隐式选择
- **AND** UI MUST 显示候选项并等待显式选择

### Requirement: 本地调试必须支持暂停、单 Tick 推进与倍率播放

本地调试 MUST 支持 `Realtime`、`Paused`、`ManualStep` 和 `RatePlayback` drive mode。所有模式 MUST 继续由 `GameplayTickSystem` 产生 LocalLogicTick，且每个 LocalLogicTick MUST 只推进正式 Session logic target 一次。ManualStep MUST 精确推进请求数量的 fixed Tick；RatePlayback MUST 改变 fixed Tick admission rate，MUST NOT 改变 Program fixed delta 或 Kernel step duration。

#### Scenario: 暂停后推进一个 Tick

- **WHEN** Session 处于 Paused 并收到 StepOne 命令
- **THEN** 下一次 FrameUpdate MUST 只生成一个 LocalLogicTick
- **AND** 该 Tick MUST 走正式 Input、Logic target、Pipeline、Commit 和 Presentation 输出

#### Scenario: 四倍速播放

- **WHEN** drive mode 设置为 RatePlayback 4x
- **THEN** GameplayTickSystem MAY 在单个 render frame 内推进多个 fixed LocalLogicTick
- **AND** 每个 LocalLogicTick MUST 使用相同 fixed delta
- **AND** 推进数量 MUST 受正式预算限制

### Requirement: Local Fixed 录制必须使用 canonical input log 与 checkpoint window

Local Fixed 调试录制 MUST 显式开始和停止。录制开启后，系统 MUST 保存每 Tick canonical input batch、SimulationTick/LocalLogicTick/source mapping、state hash、output summary 和 Trace segment key，并按正式 checkpoint interval 保存 Fixed Session checkpoint。录制窗口 MUST 有界且预分配；未开启录制时 MUST 不捕获 per-tick replay snapshot。

#### Scenario: 录制 120 个 Tick

- **WHEN** 作者开始录制并推进 120 个 LocalLogicTick
- **THEN** capture window MUST 包含 120 条 canonical input 记录
- **AND** MUST 至少包含符合 checkpoint interval 的 Fixed checkpoint
- **AND** 每条记录 MUST 可追溯到对应 Trace segment key

#### Scenario: 未开启录制

- **WHEN** Local Fixed Session 正常实时运行
- **THEN** Debug history MUST 不保存 per-tick checkpoint
- **AND** 正常 Commit 与 Presentation MUST 不依赖 Debug history

### Requirement: Local Fixed replay 必须通过现有 restore/replay transaction 执行

Local Fixed replay MUST 从目标 Tick 最近的 checkpoint 恢复，并使用 recorded canonical input 生成 ordered `Replay` steps。Replay MUST 通过现有 `SimulationRestoreDirective`、`SimulationSessionExecutionPlan` 和 Fixed Backend outer transaction 执行。Replay 期间 MUST 不提交中间 Presentation 输出；成功后 MUST 只提交最终连续分支。

#### Scenario: 从 Tick 100 回放到 Tick 130

- **WHEN** capture window 包含 Tick 90 checkpoint 和 Tick 91 到 130 input log
- **THEN** Schedule MUST 生成 Restore 90 和 Replay 91 到 130
- **AND** Backend MUST 在一个 outer transaction 内执行该计划
- **AND** Committer MUST 只发布 Tick 130 后的最终分支

#### Scenario: replay 中间失败

- **WHEN** Replay 118 发生 hash mismatch 或 Pass failure
- **THEN** 整个 outer transaction MUST 失败
- **AND** Session MUST 不发布 Tick 91 到 118 的部分结果

### Requirement: 从历史 Tick 继续必须建立新的本地分支

作者从历史 Tick 恢复并继续 live play 时，系统 MUST 恢复目标 Tick 状态，截断该 Tick 之后的 capture window，并创建新的 recording generation。系统 MUST 不把旧未来输入自动合并进新分支，MUST 不使用平滑纠偏伪装 gameplay state 差异。

#### Scenario: 从 Tick 200 继续

- **WHEN** capture window 当前记录到 Tick 260 且作者选择 ResumeFromTick 200
- **THEN** 系统 MUST 恢复 Tick 200 的正式状态
- **AND** Tick 201 到 260 的旧记录 MUST 从当前 generation 截断
- **AND** 后续输入 MUST 形成新的本地分支

### Requirement: Replay artifact 必须严格锁定 composition identity

Replay artifact MUST 保存 ProgramCatalogHash、PipelineHash、Backend identity、Source identity、Solver identity、WorldRevision、TickRate、roster、snapshot codec、input schema、checkpoint payload、input log 和 expected hash log。导入 artifact 时，任一 identity 与当前 Session 不匹配 MUST 失败关闭，不得迁移、fallback、近似匹配或跳过字段。

#### Scenario: ProgramHash 不匹配

- **WHEN** 导入 artifact 的 ProgramCatalogHash 与当前 Session 不一致
- **THEN** 导入 MUST 失败
- **AND** 系统 MUST 不尝试用当前 Program 重解释旧 input log

### Requirement: 本地调试不得保存动画骨骼或表现 workspace

Local Fixed replay snapshot MUST 只包含 gameplay state、world state、pipeline participant state 和 debug source state。系统 MUST NOT 保存骨骼 Pose、Animancer state、PoseState workspace、Slot weight、BlendStack runtime、PresentationFrame workspace 或 Transform before-image 作为 replay state。Presentation MUST 只消费 replay 后 committed Body、Intent、Action EventId 和 stream reset/replacement。

#### Scenario: 攻击连段回放

- **WHEN** replay 改变 Attack Action EventId 或 Body sample
- **THEN** Presentation MUST 从新的 committed 输出重建 Action selection 和 PoseState fact
- **AND** Replay snapshot MUST 不包含旧骨骼 Pose
