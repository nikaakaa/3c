## MODIFIED Requirements

### Requirement: Loopback 必须覆盖最小混合同步闭环

LocalLoopback endpoint MAY继续作为 ServerAuthoritative模型专属 packet endpoint存在，但只有完整 ServerAuthoritative Session Source、actor binding、Prediction/Correction Pipeline及其 Pass factory全部安装时才 MUST形成 Gameplay闭环。仅有 packet/session/endpoint而缺少 Source或 Pipeline时，Loopback MUST不直接调用 Character Core、SimulationKernel、WorldSolver、Pipeline Backend或 Transform。

#### Scenario: 核心完成而模型 Pipeline 未完成

- **WHEN** LocalLoopback EndpointDefinition仍存在
- **AND** ServerAuthoritative Source、Prediction Pipeline或 required Pass capability缺失
- **THEN** 该 endpoint MUST不可作为可运行 Gameplay组合创建
- **AND** MUST不回退旧 Character adapter或 Standard Local Pipeline

### Requirement: LocalLoopback 必须是 ServerAuthoritative 模型 Endpoint

LocalLoopback MUST继续只属于 ServerAuthoritative packet/protocol语义，不得被描述为 Network Model、Local Session Source、Local Pipeline或 transport fallback。SimulationSessionHost MUST只在完整 ModelDefinition、Source、Pipeline、Backend与 Solver验证通过后由该模型创建 Loopback endpoint。

#### Scenario: Model 组合不完整

- **WHEN** ModelDefinition缺少 Source、Pipeline、Pass factory、Actor binding或 required WorldSolver
- **THEN** SessionHost MUST不因配置了 LocalLoopback而放宽 capability校验

