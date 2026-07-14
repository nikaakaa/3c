## REMOVED Requirements

### Requirement: LocalGameplaySyncLoopbackPeer 必须复用通用 peer 合同

不存在跨 Network Model 的通用 packet peer；Loopback 必须归属当前 ServerAuthoritative 模型。

#### Scenario: 删除旧 peer 类型

- **WHEN** 本 change 完成
- **THEN** `LocalGameplaySyncLoopbackPeer` MUST 不再存在

### Requirement: Future Fantasy peer 必须替换 loopback 而不替换 gameplay 语义

Fantasy 与 Loopback 的可替换性必须限定为同一 ServerAuthoritative 模型的 endpoint，而不是通用 gameplay peer。

#### Scenario: 删除 future generic peer 口径

- **WHEN** 本 change 完成
- **THEN** 文档 MUST 不再要求 Fantasy 实现通用 `IGameplaySyncPeer`

## MODIFIED Requirements

### Requirement: Loopback 配置必须只作为本地网络调试配置

Loopback settings MUST 只属于 `LocalServerAuthoritativeEndpoint` 或等价模型 endpoint。Settings MAY 配置延迟、confirm/reject、correction 和 snapshot 模拟，但 MUST 不进入 ActionProfile、GameplayBehavior identity、CharacterPipelineDefinition、Graph 或 Timeline。

#### Scenario: 修改 Loopback 延迟

- **WHEN** 作者修改模型 endpoint 的本地延迟
- **THEN** 只有 LocalLoopback endpoint 的 pending 时序 MUST 改变
- **AND** ServerAuthoritative model policy MUST 不被改写

### Requirement: Loopback 必须覆盖最小混合同步闭环

LocalLoopback endpoint MUST 覆盖当前 ServerAuthoritativeHybrid 的最小 Action confirm/reject、Motion correction/snapshot 和 debug 闭环。它 MUST 消费模型 packet 并产出模型 packet，不得直接调用 CharacterPipeline、ActionRuntime、MotionStage 或 Transform。

#### Scenario: 本地动作确认

- **WHEN** model adapter 发送 ActionActivation 且 Loopback 配置为 Confirm
- **THEN** endpoint MUST 产出同一模型的 ActionDecision
- **AND** incoming MUST 经过 model adapter 转为 Character 语义输入

## ADDED Requirements

### Requirement: LocalLoopback 必须是 ServerAuthoritative 模型 Endpoint

系统 MUST 使用模型专属 Loopback endpoint 合同。LocalLoopback MUST 与未来 Fantasy 共享 ServerAuthoritative packet 语义，但 MUST NOT 被描述为 transport 或 Network Model。

#### Scenario: SessionHost 创建 Loopback

- **WHEN** ServerAuthoritative model definition 引用 LocalLoopback EndpointDefinition
- **THEN** model definition MUST 通过 LocalLoopback EndpointDefinition 创建该 endpoint
- **AND** SessionHost MUST 唯一持有创建后的 model session
- **AND** Character binding MUST 不各自创建 Loopback
