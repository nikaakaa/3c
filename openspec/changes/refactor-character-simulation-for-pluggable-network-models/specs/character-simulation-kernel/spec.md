# character-simulation-kernel Specification

## ADDED Requirements

### Requirement: CharacterSimulationKernel 必须是无网络和无表现副作用的纯模拟入口

系统 MUST 使用 `CharacterSimulationKernel.Step` 或等价入口，根据 immutable Program、旧 SimulationState、SimulationTick input 和 World Solver result 产生新状态、gameplay facts、motion result 与待提交 command。Kernel MUST NOT读取 transport、model packet、Unity Time、InputAction、Camera、Transform、Animancer 或 scene object，也 MUST NOT直接发送网络、播放动画、移动 visual root 或发布不可撤销外部副作用。

#### Scenario: Local Driver 推进角色

- **WHEN** Local Driver 提交当前 Tick 输入
- **THEN** Kernel MUST执行一次完整 gameplay Step
- **AND** 输出 MUST由 Driver/Committer 在 Kernel 返回后提交

#### Scenario: Rollback Driver 重演 Tick

- **WHEN** Driver 用相同 Program、旧 state、input 和 deterministic world state 重演同一 Tick
- **THEN** Kernel MUST产生相同新 state、facts、EventId 和 state hash
- **AND** MUST不在重演过程中直接重复外部副作用

### Requirement: SimulationState 必须完整拥有未来 Tick 所需状态

SimulationState MUST覆盖 Runnable lifecycle、StateMachine、Timeline、Blackboard、Action、GameplayEffect、request buffer、body、RNG、counter、handle allocator 和 scope generation。任何影响未来 Tick 的 gameplay 值 MUST存在于显式 state layout；Kernel collaborator MUST NOT保留未被 capture 的隐藏 mutable state。

#### Scenario: 恢复攻击连段窗口

- **WHEN** snapshot 在 Attack1 combo window active 时捕获并恢复
- **THEN** Timeline time、TreeClip membership、Frame/Action scope、request buffer 和 ActionInstance MUST恢复到同一状态
- **AND** 后续相同 Attack 输入 MUST得到相同 Attack2 transition

### Requirement: SimulationWorld 必须按稳定 Actor 顺序统一推进

Session simulation MUST使用 `SimulationWorldState` 保存 SimulationTick、稳定 ActorId 到 Program/State 的绑定、World Solver state 和 command cursor。多 Actor Step、world query 和冲突处理 MUST按稳定 ActorId 或明确 canonical order 运行，MUST NOT依赖 scene registration、dictionary iteration 或 network arrival order。

#### Scenario: 两名角色同 Tick 移动

- **WHEN** 两个 Actor 在同一 SimulationTick 都有输入
- **THEN** Driver MUST先形成 canonical input set
- **AND** Kernel/World Solver MUST按稳定 order 推进
- **AND** 两端相反的 packet 到达顺序 MUST不改变 deterministic model 结果

### Requirement: World Solver 必须只执行世界约束

`ICharacterWorldSolver` MUST只接收 portable body state、motion request、SimulationTick 和必要 world state，并返回 portable body/motion result。Solver MUST不读取 Graph、StateMachine、Timeline、Action、GameplayEffect、Network Model packet、prediction policy 或 Presentation。Solver definition MUST显式声明 Portable、Deterministic、Snapshotable、NavigationSurface 和 CharacterCapsuleCollision 等能力。

#### Scenario: Unity CharacterController solver

- **WHEN** Local 或 Unity authoritative Driver 使用 Unity solver
- **THEN** adapter MAY内部使用 Unity float 和 CharacterController
- **AND** MUST把结果量化为 portable result
- **AND** MUST不声明 Deterministic capability

#### Scenario: Model 要求确定性 solver

- **WHEN** DeterministicRollback definition 与非 Deterministic solver 组合
- **THEN** Session 创建 MUST失败
- **AND** MUST不切换到其它 solver

### Requirement: Simulation Driver 必须唯一拥有调度、历史和提交策略

每个 gameplay Session MUST在启动前装配恰好一个 `ISimulationDriver`。Driver MUST决定 Tick source、actor binding、canonical input、prediction、authority、history、restore/replay 和 output commit；Kernel、Graph 和 World Solver MUST不按 Driver/model id 分支。Local、ServerAuthoritative 和 DeterministicRollback MUST使用不同完整 Driver implementation，而不是一个 enum switch 穿透到节点。

#### Scenario: 单机 Session

- **WHEN** Session 使用 Local Driver
- **THEN** Driver MUST每 Tick推进一次并立即提交结果
- **AND** MUST不创建网络 history 或 rollback replay

#### Scenario: 运行中更换 Driver

- **WHEN** Session 已绑定 Actor 或开始 Tick
- **THEN** 系统 MUST拒绝更换 Driver、Program 或 World Solver
- **AND** MUST不迁移 state 到另一策略

### Requirement: Simulation output 必须通过稳定 EventId 提交表现

Kernel 产生的 animation、camera、cue、VFX 和 UI 请求 MUST是带稳定 EventId 的 presentation command。Committer MUST支持预测记录、确认、替换、撤销和 replay 去重；Presentation command MUST不进入 gameplay state hash，也 MUST不反向改变 SimulationState。

#### Scenario: Replay 重建同一 Cue

- **WHEN** rollback 重演产生与旧预测相同 EventId 的 Cue
- **THEN** Committer MUST识别为同一业务事件
- **AND** MUST不重复播放一次性 Cue

#### Scenario: 权威结果取消预测动作

- **WHEN** Driver 确认预测 Action 不成立
- **THEN** Committer MAY撤销或替换尚未确认的表现记录
- **AND** Graph/Action gameplay state MUST只通过 restored/replayed state 改变

