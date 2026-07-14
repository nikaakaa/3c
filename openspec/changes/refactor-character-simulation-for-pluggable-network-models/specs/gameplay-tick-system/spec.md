# gameplay-tick-system Specification

## MODIFIED Requirements

### Requirement: Gameplay Tick 系统必须区分本地逻辑 tick、表现帧和服务端 tick

系统 MUST明确区分 RenderFrame、LocalInputTick、Session SimulationTick、ServerAuthoritativeTick 和 Replay Pass。RenderFrame 只推进表现；LocalInputTick 标记本地输入采集；SimulationTick 是 SimulationKernel state transition 的 canonical 时间；ServerAuthoritativeTick 标记服务端确认来源；Replay Pass 在旧 SimulationTick 上重新执行但 MUST不创建新的 canonical tick identity。系统 MUST不使用单一 tick 同时表达这些含义。

#### Scenario: Rollback 重演历史 Tick

- **WHEN** 当前预测到 Tick 126 且 Driver 重演 Tick 120 至 126
- **THEN** 每次 Step MUST保留原 SimulationTick
- **AND** Replay Pass MUST与首次 Forward pass 可区分
- **AND** RenderFrame MUST不倒退

### Requirement: GameplayTickSystem 必须通过 target 接口调度业务对象

GameplayTickSystem MUST调度 Session Simulation Driver 与 Presentation target，而不是直接把每个 CharacterPipeline 视为唯一 gameplay authority。Local Driver MAY一对一拥有 Actor；Network Model Driver MAY在一个 Session Tick 中按稳定 ActorId推进多个 Actor。GameplayTickSystem MUST不解释 model packet、actor command 或 rollback history。

#### Scenario: 两个 Rollback Actor

- **WHEN** DeterministicRollback session 绑定两个 Actor
- **THEN** TickSystem MUST只推进该 Session Driver
- **AND** Driver MUST在一个 SimulationTick 内按 canonical order推进两个 Actor

### Requirement: GameplayTickSystem 必须每表现帧推进 PresentationFrame

GameplayTickSystem MUST在每个本地 RenderFrame 推进 PresentationFrame。Presentation MUST消费 Driver/Committer 最新 sample 与 interpolation alpha，MAY重采样 Timeline animation pose，但 MUST不创建 simulation input、推进 SimulationTick、restore snapshot 或产生 gameplay fact。

#### Scenario: logic catch-up 与 replay 同帧发生

- **WHEN** 一个 RenderFrame 前执行多个 forward/replay Step
- **THEN** Presentation MUST在这些 Step 完成后执行一次正式 frame transaction
- **AND** MUST只看到最终 command ledger

### Requirement: 服务端 tick 必须只通过网络输入进入角色管线

ServerAuthoritativeTick、canonical input bundle 和 authoritative snapshot MUST只进入对应 Network Model Driver。SimulationKernel MAY按 Driver 指定的 SimulationTick执行，但 MUST不读取 server packet；Character/Graph/Timeline MUST不保存 server tick authoring。Local 单机 Driver MUST不伪造 ServerAuthoritativeTick。

#### Scenario: 收到 authoritative snapshot

- **WHEN** client model 收到 server tick 200 的 snapshot
- **THEN** model Driver MUST对齐自己的 SimulationTick/history
- **AND** Kernel MUST只接收恢复后的 state 与正式 input

