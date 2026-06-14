## MODIFIED Requirements
### Requirement: 预输入消费边界
系统 MUST 将预输入定义为输入请求在短窗口内等待玩法消费层消费，而不是输入层提前决定未来动作结果。FullBody Action 请求 MUST 只有在动作打断仲裁 accepted 后才被消费；rejected 请求 MUST 保留到过期或后续合法消费。

#### Scenario: 按下时不确定未来动作 step
- **WHEN** 玩家在 step N 提前按下 Attack
- **THEN** 输入缓冲 MUST 只记录 Attack 请求从 step N 起有效
- **AND** MUST NOT 记录未来某个 step 必定触发 Attack 动作

#### Scenario: 状态不允许时保留请求
- **WHEN** 请求仍在有效窗口内
- **AND** 当前状态或仲裁规则不允许消费该请求
- **THEN** 输入缓冲 MUST 保留该请求直到过期或被合法消费

#### Scenario: 只有玩法层消费请求
- **WHEN** 输入请求可被消费
- **THEN** 只有状态机、ActionArbiter 或等价玩法仲裁层 MUST 决定是否消费
- **AND** Input System adapter MUST NOT 直接消费请求
- **AND** Locomotion 输入读取 MUST NOT 直接消费 Attack、Dodge、Jump 或 Interact 请求

#### Scenario: FullBody Action accepted 后消费
- **GIVEN** 输入缓冲中存在未过期 Dodge 请求
- **AND** 动作打断仲裁入口返回 accepted decision
- **WHEN** FullBody Action 请求门面把该请求转为状态机输入事实
- **THEN** 对应输入请求 MUST 被消费

#### Scenario: FullBody Action rejected 后保留
- **GIVEN** 输入缓冲中存在未过期 Dodge 请求
- **AND** 动作打断仲裁入口返回 rejected decision
- **WHEN** FullBody Action 请求门面处理本帧输入
- **THEN** 对应输入请求 MUST NOT 被消费
- **AND** 后续帧在请求过期前 MAY 再次参与仲裁
