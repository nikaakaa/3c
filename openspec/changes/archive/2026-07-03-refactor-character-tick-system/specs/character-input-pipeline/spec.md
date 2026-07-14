## MODIFIED Requirements

### Requirement: CharacterInputStage 每 tick 产出 CharacterInputFrame

系统 MUST 让 `CharacterInputStage` 在 pipeline logic tick 中产出当前 `LocalLogicTick` 的 `CharacterInputFrame`。连续输入值和动作触发边沿 MUST 先在本地表现帧锁存，logic tick MUST 消费该锁存输入。Frame MUST 包含 local logic tick、input sequence、authority mode、连续命令集合和本 tick 新产生的动作请求集合。Frame MUST NOT 使用服务端 `ServerTick` 作为本地输入帧身份。

#### Scenario: 本地预测角色消费锁存输入

- **WHEN** pipeline authority mode 是 `LocalPredicted`
- **THEN** InputStage MUST 从本地表现帧锁存的输入快照读取连续输入值和动作触发边沿
- **AND** InputStage MUST 写入当前 `LocalLogicTick` 的 `CharacterInputFrame`

#### Scenario: 单表现帧内补多个本地逻辑 tick

- **WHEN** 一个表现帧内因为 catch-up 推进多个 `LocalLogicTick`
- **AND** 该表现帧中某个动作输入被触发一次
- **THEN** InputStage MUST 只在一个 logic tick 中产生一次对应 action request
- **AND** 后续 catch-up tick MUST 继续读取同一份连续输入值，但 MUST NOT 重复产生同一触发请求

#### Scenario: 表现帧没有推进本地逻辑 tick

- **WHEN** 当前表现帧采样到了动作触发边沿
- **AND** accumulator 尚未推进新的 `LocalLogicTick`
- **THEN** InputStage MUST 保留该触发边沿
- **AND** 下一次 logic tick MUST 能消费该触发并创建 action request

#### Scenario: 远端代理角色不采样本地输入

- **WHEN** pipeline authority mode 是 `RemoteProxy`
- **THEN** InputStage MUST NOT 从本地 InputAction 产生本地 action request
- **AND** 远端表现 MUST 由 network receive 注入的快照或事件驱动

### Requirement: CharacterInputHistory 保存预测重放所需输入帧

系统 MUST 提供 `CharacterInputHistory` 保存最近若干 `LocalLogicTick` 的 `CharacterInputFrame`。本变更 MAY 不实现完整 correction replay，但输入历史的写入和查询边界 MUST 是正式路径。History MUST 支持按 `LocalLogicTick` 和 `InputSequence` 查询，MUST NOT 使用 `ServerTick` 查询本地输入历史。

#### Scenario: 保存本地输入帧

- **WHEN** `LocalPredicted` pipeline 产出当前 tick input frame
- **THEN** InputStage MUST 将该 frame 写入 input history
- **AND** history MUST 能按 local logic tick 或 input sequence 查询

#### Scenario: 收到校正后准备重放

- **WHEN** NetworkReceiveStage 收到 correction
- **THEN** 后续 prediction correction 逻辑 MUST 从 `CharacterInputHistory` 获取未确认输入
- **AND** 系统 MUST NOT 重新从当前 InputAction 状态推导历史输入

### Requirement: ClientCommand 来源于 CharacterInputFrame

系统 MUST 从 `CharacterInputFrame` 生成 `ClientCommand` 并写入 `NetworkOutput`。`ClientCommand` MUST 使用 gameplay command/request 数据，并携带 `InputSequence` 与 `LocalLogicTick`。`ClientCommand` MUST NOT 直接保存 raw InputAction performed 事件、action 显示名或服务端 `ServerTick` 作为网络协议语义。

#### Scenario: 收集移动命令

- **WHEN** 当前 local logic tick 的 frame 包含 `Move` command
- **THEN** NetworkOutput MUST 能收集包含 input sequence、local logic tick 和 `Move` command 的 `ClientCommand`
- **AND** NetworkSendStage MUST 只收集该 command，不直接发送 transport 消息

#### Scenario: 收集动作请求

- **WHEN** 当前 local logic tick 产生 `Attack` request
- **THEN** `ClientCommand` MUST 包含该 request 的 semantic id、input sequence 和 local logic tick
- **AND** 网络层 MUST NOT 依赖 Unity InputAction 名称判断攻击语义
