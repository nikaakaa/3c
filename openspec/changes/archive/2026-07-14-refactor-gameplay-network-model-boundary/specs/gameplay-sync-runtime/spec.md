## REMOVED Requirements

### Requirement: GameplaySyncRuntime 必须作为通用同步运行时

带有 correction、snapshot 和 action decision 的 Runtime 不是 model-neutral 通用运行时，必须迁移为 ServerAuthoritative model session。

#### Scenario: 删除错误通用口径

- **WHEN** 本 change 完成
- **THEN** 系统 MUST 不再存在通用 `GameplaySyncRuntime` 类型

### Requirement: GameplaySyncPacket 必须使用 SyncDomain 和稳定身份

带有固定 packet kind 和 payload 的合同属于具体 Network Model，不得继续作为所有模型的公共 packet。

#### Scenario: 删除通用 packet

- **WHEN** 本 change 完成
- **THEN** 系统 MUST 不再存在通用 `GameplaySyncPacket` 类型

### Requirement: GameplaySyncPeer 必须是通用 peer 合同

消费具体 GameplaySyncPacket 的 Peer 属于当前模型 endpoint，不是 transport-neutral 或 model-neutral peer。

#### Scenario: 删除通用 peer

- **WHEN** 本 change 完成
- **THEN** 系统 MUST 不再存在通用 `IGameplaySyncPeer` 类型

### Requirement: History 必须按 actor、SyncDomain 和 policy 记录

History 的内容和恢复语义由 Network Model 决定，不得由 common runtime 假定所有模型共享 correction/snapshot history。

#### Scenario: 迁移 history

- **WHEN** 本 change 完成
- **THEN** 现有 history MUST 归属 ServerAuthoritative model session

### Requirement: Runtime Debug 必须按 SyncDomain 展示同步链路

Packet、decision、correction 和 pending queue debug 必须带有明确 model identity，不得继续作为无模型归属的 Runtime Debug。

#### Scenario: 迁移 runtime debug

- **WHEN** 本 change 完成
- **THEN** 现有 packet debug MUST 显示 ServerAuthoritative model identity

## ADDED Requirements

### Requirement: Common Network Session 必须只管理模型生命周期

系统 MUST 提供 model-neutral Session composition boundary，用于持有唯一 model definition、创建 model session 并管理 dispose。该 boundary MUST NOT 定义 packet kind、history 内容、prediction、correction 或 snapshot 语义。

#### Scenario: 创建当前模型

- **WHEN** SessionHost 读取 ServerAuthoritative model definition
- **THEN** 它 MUST 创建对应 model session
- **AND** common host MUST 不读取模型 packet

### Requirement: 同步 Runtime、Packet、History 和 Debug 必须声明模型归属

任何管理 gameplay 网络 packet、history、queue 和 debug 的 runtime MUST 属于一个明确 Network Model。系统 MUST NOT 再用无模型限定的 `GameplaySync*` 类型承载 ServerAuthoritative 专用语义。

#### Scenario: 搜索通用类型

- **WHEN** 实现完成后搜索正式运行时代码
- **THEN** `GameplaySyncRuntime`、`GameplaySyncPacket`、`IGameplaySyncPeer` 和通用 GameplaySyncHistory MUST 为零定义
- **AND** 对应能力 MUST 只存在于 ServerAuthoritative model 模块

