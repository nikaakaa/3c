# gameplay-simulation-session-composition Specification

## ADDED Requirements

### Requirement: Session Host 必须注册显式本地调试控制端口

支持本地调试的 Session Host MUST 在 Active 后向正式 `LocalSimulationDebugControlService` 或等价服务注册 `ISimulationSessionDebugControlPort`。该端口 MUST 暴露只读 identity、capability、drive status、recording status、current tick、history window 和 failure。Host 在 Failed、Disposed 或 target 注销时 MUST 注销端口。公共 Host MUST 不解释具体 Fixed state、Pipeline Pass 或 replay artifact payload。

#### Scenario: Local Fixed Session Active

- **WHEN** Local Fixed Session 进入 Active
- **THEN** Session Host MUST 注册一个带完整 composition identity 的 debug control port
- **AND** Editor MUST 能通过该端口读取 current tick 和 capability

#### Scenario: Session Failed

- **WHEN** Active Session 因 Pipeline failure 进入 Failed
- **THEN** Host MUST 注销 debug control port
- **AND** 已打开的 Editor Window MUST 只能显示 frozen status 和 failure
- **AND** 系统 MUST 不保留可继续推进的悬空 runtime

### Requirement: 本地调试命令必须在正式 Tick 边界消费

Debug control port MUST 将 Pause、Resume、Step、Rate、Record、Replay、Scrub 和 ResumeFromTick 命令排入正式命令队列，并只在 GameplayTickSystem 或 Session runtime 的合法 boundary 消费。命令 MUST 拥有单调 command sequence 和明确结果状态。命令处理 MUST 不创建私有 Update、协程、Task loop 或第二 runtime handle。

#### Scenario: Editor 连续发送两个 Step 命令

- **WHEN** Editor 在同一 render frame 发送两个 StepOne 命令
- **THEN** Debug control port MUST 保留命令顺序
- **AND** GameplayTickSystem MUST 在正式 budget 内按序消费
- **AND** 每个成功 Step MUST 对应一个 LocalLogicTick

#### Scenario: Replay 命令到达错误 Session

- **WHEN** 命令携带的 SessionId 或 PipelineHash 与端口当前 identity 不一致
- **THEN** 端口 MUST 拒绝命令
- **AND** MUST 不尝试转发给其它 Session
