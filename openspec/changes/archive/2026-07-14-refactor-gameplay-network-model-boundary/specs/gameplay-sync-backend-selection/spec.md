## REMOVED Requirements

### Requirement: Gameplay Sync 后端选择必须是正式装配语义

旧装配把 model、endpoint 和 per-character ownership 混为 backend，必须由 Session-level model composition 取代。

#### Scenario: 删除旧装配

- **WHEN** 本 change 完成
- **THEN** Character component MUST 不再持有 gameplay sync backend mode

### Requirement: 第一阶段后端必须只包含 None 和 LocalLoopback

断开状态和 LocalLoopback 不是 network models，而是当前模型是否引用具体 EndpointDefinition 的结果。

#### Scenario: 删除旧 backend 枚举语义

- **WHEN** 本 change 完成
- **THEN** 系统 MUST 不再把 None/LocalLoopback 描述为 Network Model

### Requirement: Backend driver 必须服从 GameplayTickSystem

per-character backend driver 将由 SessionHost + model-owned binding 取代。

#### Scenario: 删除旧 driver

- **WHEN** 本 change 完成
- **THEN** 旧 backend driver MUST 不再注册 tick hook

### Requirement: None 后端必须是正式关闭同步模式

None 的旧 backend 枚举被删除；关闭语义迁移为 ServerAuthoritative model 未引用 EndpointDefinition。

#### Scenario: 迁移 None

- **WHEN** 当前模型未引用 EndpointDefinition
- **THEN** model session MUST 不创建 endpoint

### Requirement: LocalLoopback 后端必须只创建本地调试 peer

LocalLoopback 迁移为 ServerAuthoritative model endpoint，不再是通用 peer backend。

#### Scenario: 迁移 Loopback

- **WHEN** 当前模型引用 LocalLoopback EndpointDefinition
- **THEN** SessionHost MUST 唯一创建模型专属 endpoint

### Requirement: 旧 LoopbackDriver 入口必须清理

该清理继续有效，并扩张为删除 per-character GameplaySyncDriver。

#### Scenario: 清理角色 driver

- **WHEN** 本 change 完成
- **THEN** `CharacterGameplaySyncDriver` MUST 不再存在

## ADDED Requirements

### Requirement: Network Model 必须通过 SessionHost 装配

系统 MUST 在 SessionHost 上装配唯一完整 Network Model。当前 model definition MUST 是 `ServerAuthoritativeHybrid`，CharacterPipeline 和 Character binding MUST 不保存 model selection。

#### Scenario: Sandbox 装配模型

- **WHEN** 作者查看 Sandbox SessionHost
- **THEN** MUST 能看到当前 model identity
- **AND** Character 对象 MUST 不重复保存 model mode

### Requirement: Endpoint 选择必须归属当前模型

`ServerAuthoritativeHybrid` MUST 通过模型专属 EndpointDefinition 引用管理 endpoint。未引用表示 disconnected；LocalLoopback 和未来 Fantasy MUST 分别提供自己的 EndpointDefinition。Endpoint 配置 MUST 不进入 GameplayNetworkModelDefinition 公共基类、CharacterPipelineDefinition、ActionProfile 或 Graph，模型核心 MUST 不使用 endpoint enum + switch factory。

#### Scenario: 选择断开

- **WHEN** 当前模型不引用 EndpointDefinition
- **THEN** Character gameplay MUST 继续本地运行
- **AND** model session MUST 不使用 Loopback 作为 fallback

#### Scenario: 选择 LocalLoopback

- **WHEN** 当前模型引用 LocalLoopback EndpointDefinition
- **THEN** model session MUST 使用唯一模型专属 Loopback endpoint
- **AND** 所有 actor bindings MUST 共享该 session

### Requirement: 不可用模型和 Endpoint 不得出现在 Inspector

Inspector MUST 只显示已经完整实现的 model definition 和 endpoint。当前 change 完成时 MUST 不显示 Rollback 或 Fantasy。

#### Scenario: 查看当前选项

- **WHEN** 作者查看 SessionHost
- **THEN** model MUST 只有 ServerAuthoritativeHybrid
- **AND** 当前可创建 endpoint definition MUST 只有 LocalLoopback
- **AND** 未配置 endpoint MUST 显示为 Disconnected
