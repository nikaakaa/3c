## ADDED Requirements

### Requirement: ServerAuthoritative Binding 必须从控制模式确定收发资格

`CharacterServerAuthoritativeBinding` MUST 继续只保存 SessionHost、CharacterPipelineHost、SubjectActorId 和模型 profile。Binding MUST 根据 Character 的 InputSource/MotionAuthority 注册收发资格：LocalDevice + LocalSolver MAY 发送并接收 Owner 结果；ExternalFacts + ExternalPose MUST 只接收远端事实。系统 MUST NOT 新增 LocalPredicted/RemoteProxy 总控枚举、authority role 字段或按对象名称判断。

#### Scenario: 本地 Owner 完成 tick

- **WHEN** LocalDevice + LocalSolver Character 完成本 tick
- **THEN** binding MUST 让现有 adapter 收集 canonical input/action facts 与 resolved prediction result
- **AND** adapter MUST 只使用 canonical input/action facts 构造 authority command
- **AND** resolved prediction result MUST 只进入 prediction comparison metadata
- **AND** outgoing packet MUST 使用该 binding 的 SubjectActorId

#### Scenario: 远端 Character 运行 Timeline

- **WHEN** ExternalFacts + ExternalPose Character 产生 window、cue 或 motion facts
- **THEN** binding MUST 不把这些派生事实交给 outgoing adapter
- **AND** MUST 不形成网络 echo

### Requirement: Remote Packet 必须先转换为 Character 语义输入

ServerAuthoritative adapter MUST 将 MotionSnapshot 转交模型 snapshot buffer，将 ActionReplication activation 转换为 `ExternalActionActivation`，并将 terminal replication 转换为既有 `ActionLifecycleTransition`。Adapter MUST NOT 把 model packet、Fantasy message、server tick buffer 或 endpoint 放进 CharacterPipeline，也 MUST NOT 直接调用 Graph、Timeline、MotionStage、PresentationStage 或 Animancer。

#### Scenario: 收到远端动作激活

- **WHEN** binding drain 到当前 SubjectActorId 的 ActionReplication activation
- **THEN** adapter MUST 产生带服务端 ActionInstanceId 的 ExternalActionActivation
- **AND** 正式 Character action 输入阶段 MUST 消费该语义输入

#### Scenario: 收到远端动作结束

- **WHEN** binding drain 到同一 ActionInstanceId 的 terminal replication
- **THEN** adapter MUST 产生既有 ActionLifecycleTransition
- **AND** Character Runtime MUST 不保存原始 replication payload

### Requirement: Fantasy Endpoint 不得改变 Character Adapter 合同

LocalLoopback 与 Fantasy Endpoint MUST 消费同一 ServerAuthoritative packet 合同。Character adapter、CharacterNetworkSendStage 和 CharacterNetworkReceiveStage MUST 不认识生成的 Fantasy C2G/G2C message，也 MUST 不因 endpoint 切换而更换映射入口。

#### Scenario: Model 使用 Fantasy EndpointDefinition

- **WHEN** SessionHost 创建带 Fantasy endpoint 的同一 ServerAuthoritativeHybrid model
- **THEN** Character binding/adapter MUST 继续使用现有 model session API
- **AND** generated message mapping MUST 只发生在 Fantasy endpoint 内
