## MODIFIED Requirements

### Requirement: ActionActivationRequest 必须携带动作事务来源

`ActionActivationRequest` MUST 表达 action id 或 action profile identity、source input request id、input sequence、local logic tick、target key、target snapshot 和 source graph identity。系统 MUST 使用这些字段把输入、Graph 决策、本地预测动作和服务端确认关联起来。服务端确认或拒绝 MAY 额外携带 `ServerTick`，但 `ActionActivationRequest` 的本地来源 tick MUST NOT 使用服务端 tick。

#### Scenario: 从输入 request 启动作

- **WHEN** Graph 使用 `TryConsumeInputRequest("LightAttack")` 后提交攻击激活
- **THEN** `ActionActivationRequest` MUST 携带 source input request id、input sequence 和 local logic tick
- **AND** Debug MUST 能显示该 `ActionInstance` 来自哪次输入 request

#### Scenario: 从非输入条件激活动作

- **WHEN** Graph 因 `ReceivedAttackInParryWindow`、资源条件或 AI 决策激活动作
- **THEN** `ActionActivationRequest` MUST 允许 source input request id 为空
- **AND** MUST 仍携带 source graph identity 和 local logic tick 便于 debug
