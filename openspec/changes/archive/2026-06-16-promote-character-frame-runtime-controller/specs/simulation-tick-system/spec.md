## ADDED Requirements
### Requirement: Simulation Tick 使用 Character Runtime 入口
simulation tick system MUST 通过 Character 级 runtime tick adapter 推进正式角色 gameplay。FullBody tick adapter、Locomotion tick adapter 或 per-action tick adapter MUST NOT 作为 Corin 正式 playable 主线的最高 tick registration owner。

#### Scenario: Tick driver 调用 CharacterFrameRuntimeController
- **GIVEN** 场景启用了 `UnitySimulationTickDriver`
- **WHEN** tick runner 执行角色 gameplay phases
- **THEN** phase handler MUST 调用 `CharacterFrameRuntimeController` 或等价角色级 runtime controller
- **AND** MUST 使用同一个 `CharacterFrameRuntimeHost`
- **AND** MUST NOT 通过 `PlayerFullBodyActionController.FramePipelineHost` 作为正式路径推进

#### Scenario: FullBody tick adapter 退役
- **WHEN** 检查正式 Corin simulation tick 装配
- **THEN** `FullBodyActionTickAdapter` MUST 不作为正式注册者
- **AND** 它 MAY 被删除、标记 obsolete 或转发到角色级 tick adapter
- **AND** 它 MUST NOT 创建独立 frame context 或 runtime host

#### Scenario: Locomotion tick adapter 不竞争 gameplay
- **WHEN** 同一角色存在 Locomotion tick adapter 或诊断 tick adapter
- **THEN** 该 adapter MUST NOT 与 Character runtime tick adapter 同时推进 gameplay
- **AND** 冲突 MUST 被装配校验或自动测试捕获
- **AND** 系统 MUST NOT 依赖运行时互相压制来维持长期正确性
