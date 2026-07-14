# character-input-pipeline Specification

## MODIFIED Requirements

### Requirement: CharacterInputStage 每 tick 产出 CharacterInputFrame

Unity input adapter MUST在 RenderFrame 锁存 InputAction，在目标 SimulationTick 前产出本地 `CharacterInputFrame`，再按 compiled input schema 转换为 portable `CharacterSimulationInput`。External model input MUST转换为同一 portable input。SimulationKernel MUST只读取 portable input slot，MUST不读取 InputAction、Camera、model packet 或 CharacterInputSource enum。

#### Scenario: 本地设备生成移动输入

- **WHEN** 玩家在当前 RenderFrame 提供 MoveAxis 与 camera orientation
- **THEN** input adapter MUST生成稳定 sequence 与量化 world move direction 或 camera yaw
- **AND** compiled Graph operation MUST只读取 portable input

#### Scenario: 外部 Actor 输入

- **WHEN** Rollback model 收到另一个 Actor 的 canonical input
- **THEN** model Driver MUST构造相同 schema 的 portable input
- **AND** MUST不创建网络专用 InputNode

### Requirement: CharacterInputHistory 保存预测重放所需输入帧

本地输入适配器 MAY保存有界原始 CharacterInputFrame 供 input edge、诊断和发送 provenance 使用。实际被 ServerAuthoritative 或 DeterministicRollback 接受的 canonical simulation input history MUST由当前 Model Driver 拥有，并按 SimulationTick、ActorId 和 sequence 保存。Character input history MUST不被当作 world rollback history。

#### Scenario: Rollback 保存输入

- **WHEN** Rollback Driver 接受本地或远端 canonical input
- **THEN** Driver MUST把它写入 model-owned history
- **AND** restore/replay MUST从该 history 读取
- **AND** MUST不依赖 CharacterInputStage 私有缓存

### Requirement: GraphContext 读取同一输入帧和请求缓存

BTSMTL authoring MUST继续使用稳定 gameplay input id 和 action request id；Compiler MUST把对应 ValueNode/RequestNode 编译为 Program input slot 和 SimulationState request-buffer operation。正式 runtime MUST不需要 CharacterGraphContext 持有 CharacterInputFrame、Camera snapshot、InputAction 或 request object reference。

#### Scenario: Transition 读取 MoveAxis

- **WHEN** ConditionRuleGraph authoring 读取稳定 MoveAxis id
- **THEN** Compiler MUST解析为 Program input slot
- **AND** runtime MUST读取当前 Actor 的 CharacterSimulationInput

### Requirement: Network Model 必须从正式输入或运动事实构造自己的命令

Network Model MUST从 portable simulation input、SimulationTick、ActorId 和正式 gameplay facts构造自己的 wire command。ServerAuthoritative MAY附带 prediction comparison；DeterministicRollback MUST构造 canonical input bundle。Model MUST不让 Graph 读取 wire command，也 MUST不把客户端 resolved motion 当作服务端 canonical input。

#### Scenario: ServerAuthoritative 发送输入

- **WHEN** Owner Driver 完成本地预测 Tick
- **THEN** adapter MUST发送该 Tick 的 portable input identity和值
- **AND** predicted motion MUST只作为 comparison metadata

