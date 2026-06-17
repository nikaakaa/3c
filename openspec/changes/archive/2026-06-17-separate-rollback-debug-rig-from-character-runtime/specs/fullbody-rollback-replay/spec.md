## ADDED Requirements
### Requirement: FullBody Replay Adapter 属于 Debug Rig
`FullBodyRollbackSimulation` 或等价 `ILocalRollbackSynctestSimulation` Unity adapter MUST 作为独立 `RollbackDebugRig` prefab 的 simulation adapter 存在。该 adapter MUST 通过显式目标角色引用调用当前 `CharacterFrameRuntimeController`、`CharacterFramePipelineHost` 或等价正式角色帧入口推进 replay。正式角色 prefab MUST NOT 依赖该 adapter 作为 gameplay runtime 组件，也 MUST NOT 因该 adapter 缺失而影响正常 Play Mode 移动、动作或动画输出。

#### Scenario: Adapter 推进正式角色帧主线
- **GIVEN** `RollbackDebugRig` prefab 实例中的 FullBody replay adapter 已显式引用目标角色 runtime
- **WHEN** replay adapter 收到 tick N 的 `PredictionInputFrame`
- **THEN** adapter MUST 构造正式角色帧输入
- **AND** MUST 通过目标角色的 `CharacterFrameRuntimeController`、`CharacterFramePipelineHost` 或等价角色级主入口推进
- **AND** MUST NOT 直接创建第二个 `CharacterFramePipeline`

#### Scenario: 正式角色缺少 Adapter 不影响 gameplay
- **WHEN** 正式 Corin 角色 prefab 或正式场景实例未挂载 FullBody replay adapter
- **THEN** 角色正式 gameplay MUST 仍通过 `CharacterFrameRuntimeController` 推进
- **AND** Move、Run、TurnBack、Dodge 或后续 Action 不得依赖 replay adapter 才能运行

#### Scenario: Adapter 不创建分裂持有者
- **WHEN** FullBody replay adapter 执行 capture、restore 或 advance
- **THEN** adapter MUST 复用目标角色已有 runtime、state machine runner、motion executor 和 animation presenter
- **AND** MUST NOT new 第二套 runtime host、状态机 runner、motion executor 或 animation presenter
- **AND** MUST NOT 通过 fallback 配置补齐缺失目标角色引用

#### Scenario: 测试可临时创建 Adapter
- **WHEN** EditMode 测试需要验证 FullBody replay
- **THEN** 测试 MAY 在 fixture 中临时创建 FullBody replay adapter
- **AND** fixture MUST 显式注入目标角色 runtime 依赖
- **AND** fixture MUST NOT 替代 `RollbackDebugRig` prefab 作为 Play Mode 工具入口
- **AND** 测试 MUST 证明 replay 仍走同一角色帧主线
