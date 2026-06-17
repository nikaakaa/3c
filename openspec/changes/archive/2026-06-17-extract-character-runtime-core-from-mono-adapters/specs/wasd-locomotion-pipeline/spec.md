## ADDED Requirements
### Requirement: Locomotion Runtime 迁出 Mono Adapter
Locomotion 的正式运行时状态、frame runtime host、output runtime host、snapshot/restore 和 diagnostics state MUST 由 core-owned Movement/Locomotion runtime module 持有。`PlayerLocomotionController` MAY 作为 Unity adapter 或迁移期 facade 保留，但 MUST NOT 作为正式 Locomotion state owner 或正式 tick owner。

#### Scenario: State Store 归属 Pure Runtime
- **WHEN** Locomotion module 在角色帧内运行
- **THEN** `LocomotionRuntimeStateStore` MUST 由 `CharacterRuntimeCore` 组合的 Locomotion runtime module 持有
- **AND** `PlayerLocomotionController` MUST NOT 成为该 store 的 authoritative owner

#### Scenario: Blackboard 归属 Pure Runtime
- **WHEN** Locomotion facts builder 需要读取 runtime blackboard snapshot
- **THEN** snapshot MUST 来自 core-owned Locomotion runtime module
- **AND** `PlayerLocomotionController` MUST NOT 通过自身字段成为 blackboard authoritative owner

#### Scenario: Mono Controller 只桥接 Unity 依赖
- **GIVEN** Locomotion 需要 Transform、camera basis、motion executor 或 animation presenter
- **WHEN** Unity adapter 装配 Locomotion dependencies
- **THEN** adapter MAY 提供 Unity-facing dependency implementation
- **AND** adapter MUST NOT 直接执行正式 frame decision 或 output application

#### Scenario: Direct Tick 仍非正式
- **WHEN** `PlayerLocomotionController.AutoUpdate`、`LocomotionTickAdapter` 或兼容 direct tick 入口存在
- **THEN** 它们 MUST NOT 作为正式 gameplay 主线
- **AND** 正式 Move、Run、TurnBack、Dodge 压制关系 MUST 经 `CharacterRuntimeCore` 和 `CharacterFramePipeline` 推进

#### Scenario: Snapshot Restore 不依赖 Mono 生命周期
- **WHEN** rollback/replay 或测试对 Locomotion runtime 执行 capture/restore
- **THEN** capture/restore MUST 作用于 core-owned pure runtime state
- **AND** MUST NOT 依赖启用、禁用或重新创建 `PlayerLocomotionController` 才能恢复一致状态
