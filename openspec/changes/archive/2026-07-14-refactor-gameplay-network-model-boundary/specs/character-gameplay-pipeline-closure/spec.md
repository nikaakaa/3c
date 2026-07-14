## MODIFIED Requirements

### Requirement: SyncFacts 必须成为 demo 同步和 debug 的唯一事实出口

系统 MUST 使用 `SyncFacts` 作为本 tick 已发生 gameplay facts 的唯一模型外输出。Character fact stage MUST 收集 facts；model-owned adapter/resolver MUST 按当前 ModelId 解析、记录并构造 model packets。CharacterPipeline、Graph 和 Timeline MUST 不引用 ServerAuthoritative runtime、packet、endpoint 或 policy，也 MUST 不恢复旧 NetworkOutput。

#### Scenario: ActionWindow 进入当前模型

- **WHEN** Timeline projection 产生 ActionWindow fact
- **THEN** fact MUST 先进入 SyncFacts
- **AND** ServerAuthoritative adapter MUST 从 model profile 解析 packet/history policy

### Requirement: 第一阶段网络后端只覆盖 None 和 LocalLoopback

第一阶段唯一完整 Network Model MUST 是 `ServerAuthoritativeHybrid`。未引用 EndpointDefinition MUST 表达明确断开；当前唯一可创建的 endpoint definition MUST 是 LocalLoopback。断开/LocalLoopback MUST 不再称为两个 Network Model，且系统 MUST 不显示未实现的 Fantasy 或 Rollback。

#### Scenario: Sandbox 使用 LocalLoopback

- **WHEN** SessionHost model 是 ServerAuthoritativeHybrid 且 endpoint 是 LocalLoopback
- **THEN** Character gameplay MUST 通过 model-owned adapter 和 endpoint 闭环
- **AND** MUST 不存在 per-character backend ownership

### Requirement: 2v2vE demo 第一阶段只实现最小业务压力事实

第一阶段 MUST 继续只实现输入、动作事务、motion、window、result、state、cue 和本地 ServerAuthoritative Loopback 压力事实。本 change 只隔离模型边界，不实现真实双客户端、PvE、Objective、完整 Rollback、命中伤害或 Fantasy server slice。

#### Scenario: 查看当前网络能力

- **WHEN** 作者查看 Runtime Debug
- **THEN** MUST 能识别当前 ServerAuthoritativeHybrid + LocalLoopback
- **AND** MUST 不宣称已经实现 RemoteProxy 或真实服务端权威
