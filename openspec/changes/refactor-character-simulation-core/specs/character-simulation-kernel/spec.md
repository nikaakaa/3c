# character-simulation-kernel Specification

## ADDED Requirements

### Requirement: SimulationKernel 必须以明确状态执行单 Tick

SimulationKernel.Step MUST只接收 CharacterSimulationProgram、SimulationTick、CharacterSimulationInput、当前 SimulationState 和 ICharacterWorldSolver，并输出新 State 与 SimulationOutput。Kernel MUST不读取 Unity Time、Camera、InputAction、Transport、Network packet 或 Presentation object。

#### Scenario: Local Driver 推进一 Tick

- **WHEN** Local Driver 提交 Tick 和 portable input
- **THEN** Kernel MUST仅根据正式输入和状态执行 Program

### Requirement: SimulationState 必须可完整 Capture 与 Restore

系统 MUST按 Program State Layout 完整序列化和恢复 Actor gameplay state。Restore MUST原子替换 Runnable、StateMachine、Timeline、Blackboard、Action、Effect、Body、RNG 和 sequence，MUST不只恢复 Transform 或部分模块。

#### Scenario: 恢复攻击中的状态

- **WHEN** 系统恢复一个 Attack2 Timeline 正在运行的 snapshot
- **THEN** ActionInstance、StateMachine、Timeline、Window Blackboard 和 BodyState MUST同时恢复

### Requirement: State Hash 必须只覆盖 Gameplay 真值

系统 MUST以 ProgramHash、State Layout 和 canonical state bytes 计算稳定 state hash。Diagnostics、AnimationClip、Animancer、Camera、VFX、UI 和 Unity object MUST不进入 hash。

#### Scenario: 表现帧独立推进

- **WHEN** Animancer fade 在两个 logic tick 之间推进
- **THEN** SimulationState hash MUST不因表现时间变化

### Requirement: Simulation Driver 必须唯一拥有 Tick 与 Commit 策略

Session MUST装配唯一 ISimulationDriver。Driver MUST决定 SimulationTick、有效 input、是否 capture/restore/replay 以及哪些 SimulationOutput 可提交。Program、Kernel、Graph 和 World Solver MUST不选择 Driver 或 Network Model。

#### Scenario: 单机 Session

- **WHEN** Session 装配 LocalSimulationDriver
- **THEN** Driver MUST每个固定 Tick 执行一次 Kernel 并立即提交输出

### Requirement: World Solver 必须只解决世界约束

ICharacterWorldSolver MUST只接收 portable BodyState、MotionRequest、WorldQuery 和 Tick context，并返回 portable WorldSolverResult。Solver MUST不读取 Graph、Action、Timeline、Network Model、server tick、ack 或 correction packet。

#### Scenario: Unity CharacterController 执行运动

- **WHEN** Unity Solver 收到 portable motion request
- **THEN** adapter MAY在内部调用 CharacterController.Move
- **AND** MUST返回量化 portable body result

### Requirement: CharacterSimulationInput 必须与输入设备解耦

Kernel MUST只消费 portable CharacterSimulationInput。Input Adapter MUST在 Kernel 外将 InputAction、Camera-relative 方向或外部命令转换为稳定 InputId、量化值、request、sequence 和 source tick。

#### Scenario: 相机相对移动

- **WHEN** Unity Input Adapter 采样移动与 Camera yaw
- **THEN** Adapter MUST在进入 Kernel 前产生量化世界方向或量化 yaw
- **AND** Graph operation MUST不读取 Camera

### Requirement: SimulationOutput 必须使用稳定 EventId

Gameplay facts 与 presentation commands MUST使用由 ActorId、operation/activation identity、SimulationTick 和 local sequence 组成的稳定 EventId。Kernel MUST不直接播放动画、发送 packet 或触发相机/VFX。

#### Scenario: Timeline 产生 Cue

- **WHEN** Timeline operation 在当前 Tick 产生 Cue command
- **THEN** Kernel MUST输出带 EventId 的 command
- **AND** 只有 Committer MAY触发外部副作用

### Requirement: Portable Core 必须由 Unity 与普通 DotNet 共享源集

Program、State、Input、Output、Kernel 和 World Solver 合同 MUST来自一个 canonical source set，并可由 Unity asmdef 与普通 .NET csproj 编译。系统 MUST不复制一份 server simulation core。

#### Scenario: DotNet 项目引用 Core

- **WHEN** 后续 server csproj 引用 portable core
- **THEN** MUST编译同一份 Kernel 与 Program reader 源码
