# gameplay-sync-backend-selection Specification

## MODIFIED Requirements

### Requirement: Network Model 必须通过 SessionHost 装配

GameplayNetworkSessionHost MUST显式引用唯一完整 GameplayNetworkModelDefinition。Host/Inspector MUST通过公共 capability contract 创建 model session 与 Simulation Driver composition，MUST不硬编码 ServerAuthoritative、Rollback 或未来 model 分支。LocalSimulationDriver MUST由 Local Simulation Session 直接装配，不作为 Network Model 选项。

#### Scenario: 没有完整 Network Model

- **WHEN** 当前安装的 ModelDefinition 都缺少正式 Driver composition
- **THEN** SessionHost Inspector MUST不显示可运行 model
- **AND** 单机 Local Session MUST不自动创建 Network SessionHost

### Requirement: Endpoint 选择必须归属当前模型

EndpointDefinition 的类型、配置、protocol 和创建 MUST归当前 ModelDefinition。Common Host、CharacterPipelineDefinition、Graph、Program 和 Action profile MUST不保存 endpoint enum/switch 或 fallback endpoint。

#### Scenario: Endpoint 不兼容

- **WHEN** ModelDefinition 引用不符合其 endpoint capability 的资产
- **THEN** 创建 MUST失败并报告具体不兼容原因

### Requirement: 不可用模型和 Endpoint 不得出现在 Inspector

Inspector MUST从已安装 ModelDefinition、Driver factory、WorldSolver、EndpointDefinition 与 capability validation 生成可选项。不完整、不兼容或无法创建的组合 MUST不可选，MUST不通过旧 adapter、默认 Driver 或 fallback endpoint 伪装可用。

#### Scenario: 只安装了旧 LocalLoopback Endpoint

- **WHEN** ServerAuthoritative packet/session/endpoint 存在但新 Driver adapter 缺失
- **THEN** Inspector MUST不显示该组合为可运行
