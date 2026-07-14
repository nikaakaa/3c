# gameplay-sync-backend-selection Specification

## Purpose
定义角色 gameplay sync 后端选择的正式 Unity 装配口径：`CharacterGameplaySyncDriver` 负责选择 `None` 或 `LocalLoopback` 后端、持有 actor identity、连接 `GameplaySyncRuntime` 与 `IGameplaySyncPeer`，并在 `GameplayTickSystem` 前后完成 incoming/outgoing 注入。Loopback 是本地调试 peer，不再作为角色同步主入口命名。
## Requirements
### Requirement: Network Model 必须通过 SessionHost 装配

系统 MUST 在 SessionHost 上装配唯一完整 Network Model。当前 model definition MUST 是 `ServerAuthoritativeHybrid`，CharacterPipeline 和 Character binding MUST 不保存 model selection。

#### Scenario: Sandbox 装配模型

- **WHEN** 作者查看 Sandbox SessionHost
- **THEN** MUST 能看到当前 model identity
- **AND** Character 对象 MUST 不重复保存 model mode

### Requirement: Endpoint 选择必须归属当前模型

`ServerAuthoritativeHybrid` MUST 通过模型专属 EndpointDefinition 引用管理 endpoint。未引用表示 disconnected；LocalLoopback 和未来 Fantasy MUST 分别提供自己的 EndpointDefinition。Endpoint 配置 MUST 不进入 GameplayNetworkModelDefinition 公共基类、CharacterPipelineDefinition、ActionProfile 或 Graph，模型核心 MUST 不使用 endpoint enum + switch factory。

#### Scenario: 选择断开

- **WHEN** 当前模型不引用 EndpointDefinition
- **THEN** Character gameplay MUST 继续本地运行
- **AND** model session MUST 不使用 Loopback 作为 fallback

#### Scenario: 选择 LocalLoopback

- **WHEN** 当前模型引用 LocalLoopback EndpointDefinition
- **THEN** model session MUST 使用唯一模型专属 Loopback endpoint
- **AND** 所有 actor bindings MUST 共享该 session

### Requirement: 不可用模型和 Endpoint 不得出现在 Inspector

Inspector MUST 只显示已经完整实现的 model definition 和 endpoint。当前 change 完成时 MUST 不显示 Rollback 或 Fantasy。

#### Scenario: 查看当前选项

- **WHEN** 作者查看 SessionHost
- **THEN** model MUST 只有 ServerAuthoritativeHybrid
- **AND** 当前可创建 endpoint definition MUST 只有 LocalLoopback
- **AND** 未配置 endpoint MUST 显示为 Disconnected

