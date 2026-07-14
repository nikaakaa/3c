## MODIFIED Requirements

### Requirement: ClientCommand 来源于 CharacterInputFrame

系统 MUST 从 `CharacterInputFrame` 生成 `ClientCommand` 并写入 `SyncFacts`。`ClientCommand` MUST 使用 gameplay command/request 数据，并携带 `InputSequence` 与 `LocalLogicTick`。`ClientCommand` MUST NOT 直接保存 raw InputAction performed 事件、action 显示名或服务端 `ServerTick` 作为网络协议语义。

#### Scenario: 收集移动命令

- **WHEN** 当前 local logic tick 的 frame 包含 `Move` command
- **THEN** `SyncFacts` MUST 能收集包含 input sequence、local logic tick 和 `Move` command 的 `ClientCommand`
- **AND** NetworkSendStage MUST 只收集该 command，不直接发送 transport 消息

#### Scenario: 收集动作请求

- **WHEN** 当前 local logic tick 产生 `Attack` request
- **THEN** `ClientCommand` MUST 包含该 request 的 semantic id、input sequence 和 local logic tick
- **AND** 网络层 MUST NOT 依赖 Unity InputAction 名称判断攻击语义
