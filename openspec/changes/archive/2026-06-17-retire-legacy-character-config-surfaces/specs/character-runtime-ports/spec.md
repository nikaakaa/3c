## ADDED Requirements

### Requirement: CharacterFrame runtime 必须是唯一正式驱动入口

正式角色运行时 MUST 由 `CharacterFrameRuntimeController` 及其正式 tick adapter 驱动 Locomotion 与 FullBody Action。旧 tick adapter 不得注册、推进或旁路驱动正式 runtime。

#### Scenario: 正式 prefab 只有 CharacterFrame 驱动链

- **GIVEN** Corin 正式 prefab 被加载或静态扫描
- **WHEN** 测试检查 runtime 驱动组件
- **THEN** prefab 存在且只存在一条 `CharacterFrameRuntimeController` 正式驱动链
- **AND** `FullBodyActionTickAdapter` 与 `LocomotionTickAdapter` 不作为可驱动组件挂载

#### Scenario: 退役 adapter 不参与运行时推进

- **GIVEN** 项目仍保留退役 adapter 类型用于迁移或诊断
- **WHEN** runtime 初始化与 frame tick 执行
- **THEN** 退役 adapter 不注册到正式 tick 流
- **AND** 退役 adapter 不调用 Locomotion 或 FullBody Action 的推进 API

### Requirement: 运行时端口不得依赖旧 Host Adapter

正式 FullBody Action 运行时端口 MUST 不依赖 `PlayerFullBodyActionController` 或旧 Host Adapter。状态、请求、动画播放和诊断数据必须通过当前正式端口与视图暴露。

#### Scenario: FullBody Action 不需要旧 Host Adapter

- **GIVEN** FullBody Action runtime 被构建
- **WHEN** Action 请求、状态推进和动画状态被访问
- **THEN** 调用链不需要 `PlayerFullBodyActionController`
- **AND** 不需要从旧 Host Adapter 读取配置或状态

#### Scenario: 冲突诊断不要求保留旧驱动组件

- **GIVEN** 测试需要发现 prefab/scene 中的重复驱动或旧组件
- **WHEN** 静态扫描执行
- **THEN** 扫描可以识别旧组件名或旧字段名
- **AND** 不要求旧组件作为可挂载正式 runtime 类继续存在
