## ADDED Requirements
### Requirement: 角色帧主线由 Runtime Core 持有
`CharacterFramePipeline` MUST 仍是唯一角色帧 gameplay 输出主线，但其正式 host ownership MUST 位于 `CharacterRuntimeCore` 或批准的等价纯 C# owner。Unity MonoBehaviour MUST 只能作为 tick adapter 或 dependency composition adapter 调用该 core。

#### Scenario: Unity Update 通过 Core 进入主线
- **GIVEN** 正式角色 prefab 已装配 `CharacterFrameRuntimeController`
- **WHEN** Unity Update 或外部 tick driver 推进一帧
- **THEN** Mono adapter MUST 调用同一个 `CharacterRuntimeCore`
- **AND** core MUST 推进同一个 `CharacterFramePipeline`
- **AND** Locomotion、Action、motion、animation 和 diagnostics 输出 MUST 继续经过同一个 `CharacterFramePlan` 或批准的等价计划

#### Scenario: 不新增第二角色帧循环
- **WHEN** 新增或迁移 Locomotion、Action、rollback replay 或测试 fixture
- **THEN** 新代码 MUST NOT new 独立 `CharacterFramePipeline` 作为生产路径
- **AND** MUST NOT 通过额外 MonoBehaviour Update 直接应用正式 gameplay 输出
- **AND** MUST NOT 绕过 core-owned host 执行 motion 或 animation 副作用

#### Scenario: Phase 顺序保持可测试
- **WHEN** EditMode 测试用 fake dependencies 推进 core tick
- **THEN** request submission、frame submission、plan/composition 和 output application 的顺序 MUST 与现有 `CharacterFramePipeline` 合同一致
- **AND** 测试 MUST 不依赖 scene instance 才能验证顺序
