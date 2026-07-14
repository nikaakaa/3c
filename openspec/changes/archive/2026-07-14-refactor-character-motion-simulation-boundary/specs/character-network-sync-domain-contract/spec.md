## MODIFIED Requirements

### Requirement: MotionSyncDomain 必须处理连续运动同步

MotionSyncDomain MUST 表达 canonical input frame identity、本地 prediction result、external pose input 和 correction application result 等连续运动语义。CharacterPipeline MUST 不生成 model packet、ClientCommandFrame、MotionCommand 或 CorrectionAck；具体模型 adapter MUST 选择所需事实并构造自己的命令和 acknowledgement。`ResolvedCharacterMotionFact` MUST 表达本地已经发生的运动结果，MUST NOT 被通用合同定义为服务端 canonical motion intent。

#### Scenario: 本地运动完成

- **WHEN** CharacterMotionStage 完成本 tick LocalSolver 结算
- **THEN** MUST 产生 resolved motion fact
- **AND** ServerAuthoritative adapter MAY 将它用于 prediction comparison、diagnostics 或 correction provenance
- **AND** 服务端权威模拟 MUST 从 canonical input/action state 独立生成 motion intent

#### Scenario: 未来模型消费 canonical input

- **WHEN** Network Model 需要在远端重演或独立模拟角色运动
- **THEN** model adapter MUST 从正式 input/action facts 构造模型命令
- **AND** MUST 不把客户端 actual displacement 当作唯一权威输入

### Requirement: NetworkSendStage 必须按 SyncDomain 和 policy 打包

CharacterNetworkSendStage 或等价输出 stage MUST 只收集 CharacterInputFrame、resolved motion 和 SyncFacts，并保留 BehaviorId、ActionId、SyncDomain 与稳定 identity。它 MUST 不解析 model policy 或构造 packet。Model-owned adapter MUST 使用当前 model profile 决定过滤、history 和 packet 映射，并 MUST 区分 canonical command input 与本地 prediction result。

#### Scenario: 本地预测角色输出一帧事实

- **WHEN** 本 tick 产生 input、resolved motion、action activation 和 window facts
- **THEN** output stage MUST 原样暴露对应 gameplay facts
- **AND** ServerAuthoritative adapter MUST 从 canonical input/action facts 构造权威端命令
- **AND** resolved motion MAY 作为 prediction comparison metadata，但 MUST 不替代权威端模拟

#### Scenario: 没有 Network Model

- **WHEN** CharacterPipeline 以单机方式运行且没有 model session 消费 facts
- **THEN** Pipeline MUST 继续正常执行
- **AND** facts MAY 只供 debug 或 recording 使用
