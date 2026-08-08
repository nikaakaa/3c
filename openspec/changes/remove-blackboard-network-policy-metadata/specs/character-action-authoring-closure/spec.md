## MODIFIED Requirements

### Requirement: Runtime Debug 必须展示配置和运行事实的差异

Runtime Debug MUST按 `ActionInstance` 展示GameplayFact、PresentationCommand与incoming ingress。Model Debug MUST按actor、input sequence、server tick、fact kind与ProducerId展示packet、过滤原因、reconciliation和ack。Debug MUST区分动作输出缺失、模型coverage不支持与网络运行错误，并 MUST使用实际typed fact名称，不得把Blackboard SyncPolicy描述为运行事实。

#### Scenario: Window 没有发送

- **WHEN** 作者预期 HitWindow 会同步但运行时没有 outgoing packet
- **THEN** Debug MUST能显示该 ActionInstance 是否产生了 `ActionWindowFact`
- **AND** MUST显示Model是否正式支持ActionWindow fact kind与对应ProducerId

#### Scenario: 服务端纠正动作

- **WHEN** 收到 ActionInstance Correct 或 Reject decision
- **THEN** Debug MUST显示对应 ActionProfile、ActionInstance、prediction key、incoming transition 和 reason
- **AND** 同tick存在body correction时Model Debug MUST记录restore/replay与ack

