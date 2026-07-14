## MODIFIED Requirements

### Requirement: 不可用模型和 Endpoint 不得出现在 Inspector

Inspector MUST 只显示已经完整实现的 model definition 和 endpoint definition。本 change 完成后，正式 Network Model MUST 仍只有 `ServerAuthoritativeHybrid`；该模型可引用的 endpoint definition MUST 包含 LocalLoopback 与 Fantasy。Fantasy definition MUST 同时拥有真实连接、生成协议、服务端 Room、roster、远端 pose/action 消费链和 health，MUST NOT 只是 enum 或字符串 placeholder。

#### Scenario: 查看当前模型装配

- **WHEN** 作者查看 Sandbox SessionHost/model definition
- **THEN** model MUST 仍是 ServerAuthoritativeHybrid
- **AND** endpoint definition MUST 可显式选择 LocalLoopback 或 Fantasy
- **AND** UI MUST 不显示 Rollback、Lockstep 或其它未实现模型

#### Scenario: Fantasy 连接失败

- **WHEN** 已配置 Fantasy EndpointDefinition 但连接失败
- **THEN** endpoint MUST 进入明确 Faulted/Disconnected
- **AND** MUST 不回退 LocalLoopback 或改写 model definition

## ADDED Requirements

### Requirement: Fantasy 必须通过独立 EndpointDefinition 扩展当前模型

系统 MUST 新增 `FantasyServerAuthoritativeEndpointDefinition` 或等价模型专属定义，由其创建 Fantasy endpoint。新增 Fantasy MUST NOT 修改 `ServerAuthoritativeHybridModelDefinition` 的 endpoint enum/switch，MUST NOT 进入 common `GameplayNetworkModelDefinition`，也 MUST NOT 创建第二 Network Model。

#### Scenario: 创建 Fantasy Session

- **WHEN** ServerAuthoritative model definition 引用 Fantasy EndpointDefinition
- **THEN** model session MUST 使用该 definition 创建唯一 Fantasy endpoint
- **AND** 所有 Character bindings MUST 继续共享同一 model session
