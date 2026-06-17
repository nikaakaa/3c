## ADDED Requirements
### Requirement: Character Runtime Core 纯 C# Owner
系统 MUST 提供 `CharacterRuntimeCore` 或批准的等价纯 C# 对象作为正式角色运行时 owner。该 owner MUST 组合正式 `CharacterFrameRuntimeHost`、正式 runtime port、Locomotion runtime module、Action runtime module、snapshot/restore 和 diagnostics 状态。MonoBehaviour MAY 创建、配置或持有该 core，但 MUST NOT 自身成为正式 runtime state、runner 或 lifecycle 的 owner。

#### Scenario: Core 无 Unity 对象构造
- **WHEN** EditMode 测试使用纯 C# fixture 构造正式角色 runtime core
- **THEN** core MUST 不要求 GameObject、Transform、MonoBehaviour 或 scene instance 才能创建
- **AND** core MUST 能持有一个正式 `CharacterFrameRuntimeHost`
- **AND** core MUST 能暴露正式 runtime port

#### Scenario: Mono Adapter 只拼装依赖
- **GIVEN** `CharacterFrameRuntimeController` 或批准的等价 Mono adapter 已显式绑定 config、input、motion executor 和 animation presenter
- **WHEN** adapter 初始化正式角色 runtime
- **THEN** adapter MUST 创建或接收一个 `CharacterRuntimeCore`
- **AND** MUST 将 Unity-facing dependencies 注入 core
- **AND** MUST NOT 创建第二个正式 `CharacterFramePipeline`、状态机 runner 或 lifecycle runtime

#### Scenario: Runtime Port 不反查 Mono Owner
- **WHEN** `CharacterFramePipeline` 通过正式 runtime port 运行 phase
- **THEN** port MUST 由 `CharacterRuntimeCore` 或 core-owned adapter 提供
- **AND** MUST NOT 通过 `CharacterFrameRuntimeController` 再查找 `PlayerLocomotionController` 或 `FullBodyActionRuntime` 来获得正式状态

#### Scenario: Replay 复用同一 Core
- **GIVEN** 独立 Rollback Debug Rig 的 replay adapter 已显式引用目标角色 runtime
- **WHEN** replay 执行 capture、restore 或 tick
- **THEN** replay MUST 复用目标角色的 `CharacterRuntimeCore` 或等价正式 owner
- **AND** MUST NOT 创建第二个 core、第二个 runner、第二个 motion executor 或第二个 animation presenter

### Requirement: Mono Adapter 运行时状态禁入
正式角色 runtime 状态 MUST 从 MonoBehaviour 字段迁出。`CharacterFrameRuntimeController`、`PlayerLocomotionController`、`FullBodyActionRuntime` 或批准的等价 Mono adapter MAY 保留序列化引用、Unity 生命周期入口和兼容 facade，但 MUST NOT 持有正式 `LocomotionRuntimeStateStore`、`CharacterRuntimeBlackboard`、`CharacterStateMachineRuntime`、`ActionLifecycleRuntime` 或 `CharacterFrameRuntimeHost` 作为 authoritative state。

#### Scenario: Locomotion 状态不由 Controller 持有
- **WHEN** 自动静态测试扫描正式 production runtime 代码
- **THEN** `PlayerLocomotionController` MUST NOT new 或持有正式 `LocomotionRuntimeStateStore`
- **AND** MUST NOT new 或持有正式 `CharacterRuntimeBlackboard`
- **AND** Locomotion state MUST 由 core-owned Movement/Locomotion runtime module 持有

#### Scenario: Action 状态不由 Mono Runtime 持有
- **WHEN** 自动静态测试扫描正式 production runtime 代码
- **THEN** `FullBodyActionRuntime` MUST NOT new 或持有正式 `CharacterStateMachineRuntime`
- **AND** MUST NOT new 或持有正式 `ActionLifecycleRuntime`
- **AND** Action state MUST 由 core-owned Action runtime module 持有

#### Scenario: Controller 不持有正式 Host
- **WHEN** `CharacterFrameRuntimeController` 在 Play Mode 初始化
- **THEN** 它 MUST 通过 `CharacterRuntimeCore` 推进正式 tick
- **AND** MUST NOT 自身持有 authoritative `CharacterFrameRuntimeHost`
- **AND** MUST NOT 直接 new 第二个 pipeline host 作为 fallback
