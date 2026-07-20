# local-gameplay-sync-loopback Specification

## MODIFIED Requirements

### Requirement: Loopback 必须覆盖最小混合同步闭环

LocalLoopback endpoint MAY继续作为 ServerAuthoritative 模型专属 packet endpoint 存在，但只有完整 ServerAuthoritative Simulation Driver 与 actor binding 已安装时才 MUST形成 gameplay 闭环。核心迁移后仅有旧 packet/session/endpoint 而缺少 Driver 时，Loopback MUST不直接调用 Character Core、旧 CharacterPipeline、ActionRuntime、MotionStage、WorldSolver 或 Transform。

#### Scenario: 核心完成而模型 Driver 未完成

- **WHEN** LocalLoopback EndpointDefinition 仍存在
- **AND** ServerAuthoritative Simulation Driver capability 缺失
- **THEN** 该 endpoint MUST不可作为可运行 gameplay 组合创建
- **AND** MUST不回退旧 Character adapter

### Requirement: LocalLoopback 必须是 ServerAuthoritative 模型 Endpoint

LocalLoopback MUST继续只属于 ServerAuthoritative packet/protocol 语义，不得被描述为 Network Model、LocalSimulationDriver 或 transport fallback。SessionHost MUST只在完整 ModelDefinition 验证通过后由该模型创建 Loopback endpoint。

#### Scenario: Model 组合不完整

- **WHEN** ModelDefinition 缺少 Driver、actor binding 或 required WorldSolver
- **THEN** SessionHost MUST不因配置了 LocalLoopback 而放宽能力校验
