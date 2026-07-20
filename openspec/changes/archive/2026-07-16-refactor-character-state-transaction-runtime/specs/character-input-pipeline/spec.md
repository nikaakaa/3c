## MODIFIED Requirements

### Requirement: 离散动作输入进入 request buffer

系统 MUST将Attack、Dodge、Jump、Interact等离散动作输入编译进`CharacterSimulationInput.Requests`，并由Program声明的typed `InputRequestBuffer` state address维护可查询、可消费的committed状态。每个request MUST保存sequence、source tick、expire simulation tick、priority与consumed状态；request id MUST由Program Layout稳定绑定。写入、查询、过期与消费 MUST通过当前Character State Transaction的Input state port完成，不得创建第二个request buffer、opaque bytes镜像或每Tickrequest codec。

#### Scenario: 硬直中预输入攻击

- **WHEN** 玩家在当前状态不可攻击时触发`Attack`
- **THEN** Input Adapter MUST将`Attack`写入`CharacterSimulationInput.Requests`，Program MUST将其写入对应typed request state
- **AND** 该request MUST在配置的buffer时间内保持可查询

#### Scenario: 请求过期

- **WHEN** `Attack` request超过配置的buffer时间仍未被消费
- **THEN** request buffer MUST将该typed request视为不可用
- **AND** 后续查询 MUST NOT返回该过期request

#### Scenario: 请求被消费

- **WHEN** 状态行为或动作管线正式接受`Dodge` request
- **THEN** request buffer MUST在当前State Transaction中将该request标记为consumed
- **AND** 同一request MUST NOT被第二次消费

