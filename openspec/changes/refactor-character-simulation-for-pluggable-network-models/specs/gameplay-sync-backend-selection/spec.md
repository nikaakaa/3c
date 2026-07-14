# gameplay-sync-backend-selection Specification

## MODIFIED Requirements

### Requirement: Network Model 必须通过 SessionHost 装配

系统 MUST在 GameplayNetworkSessionHost 上装配唯一完整 Network Model。完成本 change 后，正式 model definition 类型 MUST为 ServerAuthoritativeHybrid 与 DeterministicRollback。Character authoring、Program、Kernel、World Solver和Character binding MUST不保存 model selection；运行中 MUST不切换 model。

#### Scenario: Sandbox 装配模型

- **WHEN** 作者选择一个完整 model definition
- **THEN** SessionHost MUST创建对应 model session/Driver
- **AND** 所有 actor bindings MUST归属该 Session

### Requirement: Endpoint 选择必须归属当前模型

每个 Network Model MUST通过自己的 EndpointDefinition/协议管理远端；Endpoint配置 MUST不进入 CharacterPipelineDefinition、Graph、Program或通用 SessionHost。ServerAuthoritative 的 LocalLoopback/Fantasy 与 DeterministicRollback endpoint MUST分别实现自己的模型合同，MUST不复用通用 endpoint enum + switch，也 MUST不在连接失败时互相回退。

#### Scenario: ServerAuthoritative 选择 Fantasy

- **WHEN** model definition 引用 Fantasy EndpointDefinition
- **THEN** session MUST创建唯一 Fantasy endpoint
- **AND** endpoint MUST连接当前 server deployment

#### Scenario: 明确断开

- **WHEN** model 没有配置 required endpoint
- **THEN**配置 MUST显示Disconnected或失败
- **AND** MUST不自动创建LocalLoopback

### Requirement: 不可用模型和 Endpoint 不得出现在 Inspector

Inspector MUST只显示runtime、protocol、actor binding、Driver、required solver/host能力和配置全部完整的model/endpoint。完成本 change 后，ServerAuthoritativeHybrid与DeterministicRollback可用；Fantasy endpoint可用于正式双客户端；其它占位模型或endpoint MUST不显示。

#### Scenario: Deterministic KCC 未安装

- **WHEN** Rollback definition 的required solver不可用
- **THEN** Inspector MUST显示明确配置错误或不提供该definition
- **AND** MUST不创建空session

