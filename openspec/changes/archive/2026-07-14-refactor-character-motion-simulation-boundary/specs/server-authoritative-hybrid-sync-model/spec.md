## ADDED Requirements

### Requirement: ServerAuthoritative 权威运动必须拥有独立模拟后端

ServerAuthoritativeHybrid 的权威端 MUST 从已接受的 canonical input、action state、角色配置和当前 canonical body state 生成 motion intent，并调用唯一正式 authoritative simulation backend 产生 canonical pose。Backend MUST 由 model/server composition root 显式装配。系统 MUST NOT 同时运行 Unity backend 与纯 CSharp KCC backend 后选择结果，也 MUST NOT 在 backend 缺失时累加客户端 resolved displacement 作为 fallback。

#### Scenario: 使用 Unity authoritative process

- **WHEN** model definition 选择 Unity authoritative backend
- **THEN** 服务端 MUST 在 Unity process 内独立推进角色 motion semantics
- **AND** MUST 使用正式 Unity Motion Executor 执行 world constraint

#### Scenario: 使用纯 CSharp KCC server

- **WHEN** model definition 选择纯 CSharp KCC backend
- **THEN** 服务端 MUST 在纯 CSharp runtime 内独立推进角色 motion semantics
- **AND** MUST 使用正式 KCC/world query implementation 产生 canonical pose
- **AND** navigation/pathfinding library MUST 不被当作完整碰撞 motor

#### Scenario: backend 缺失

- **WHEN** ServerAuthoritativeHybrid 要求权威运动但没有配置完整 backend
- **THEN** model session 启动 MUST 失败
- **AND** MUST 不回退到 envelope validation 或 client pose acceptance

## MODIFIED Requirements

### Requirement: ServerAuthoritative Adapter 必须是唯一 Packet 映射入口

系统 MUST 使用 model-owned Character adapter 将 canonical Character input/action facts 映射为 ServerAuthoritative outgoing commands，并将 incoming packets 映射为 Character 语义输入。Adapter MUST 复用 model policy resolver，MUST NOT 回读 Graph、Timeline 或 Animation 结构补齐策略。客户端 resolved motion MAY 进入模型 prediction history 或 diagnostics，但 MUST NOT 被映射为服务端唯一 canonical displacement。

#### Scenario: 构造 MotionCommand

- **WHEN** adapter 收到 canonical input frame、相关 action request 和当前 policy
- **THEN** adapter MUST 构造供权威端独立模拟的 ServerAuthoritative MotionCommand
- **AND** MotionCommand MUST 不把客户端 actual displacement 作为服务端唯一运动输入
- **AND** MotionStage MUST 不直接构造 packet

#### Scenario: 记录 prediction result

- **WHEN** adapter 收到同 tick resolved motion fact
- **THEN** model MAY 将其记录为 prediction comparison metadata
- **AND** authority backend MUST 不直接采用该 pose 作为 canonical pose

#### Scenario: 构造 CorrectionAck

- **WHEN** CharacterMotionStage 输出成功的 correction application result
- **THEN** adapter MUST 构造模型 acknowledgement
- **AND** CharacterPipeline MUST 不持有 endpoint 或 packet id
