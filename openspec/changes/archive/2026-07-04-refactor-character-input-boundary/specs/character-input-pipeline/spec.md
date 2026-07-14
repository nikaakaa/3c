# character-input-pipeline Specification Delta

## MODIFIED Requirements

### Requirement: CharacterInputProfile 映射 InputAction 到 gameplay 输入值和动作请求
系统 MUST 使用 `CharacterInputProfile` 表达角色输入配置。Profile MUST 引用正式 `InputActionAsset`，并将 action 稳定身份映射为 gameplay input value 或 action request。Gameplay 逻辑 MUST 使用稳定 gameplay input id 或 request id，不得直接使用 InputAction 显示名。Profile、Graph 和输入 frame 的正式口径 MUST NOT 使用 `signal` 作为连续输入概念。

#### Scenario: 配置连续输入值
- **WHEN** Profile 将 `Player/Move` action 映射为 `MoveAxis` input value
- **THEN** 输入层 MUST 使用 action identity 读取来源
- **AND** gameplay、Tree 和 Motion 模块 MUST 使用 `MoveAxis` input value id
- **AND** 系统 MUST NOT 将该输入称为 signal 或 network command

#### Scenario: 配置动作请求
- **WHEN** Profile 将 `Player/Fire` action 映射为 `Attack` action request
- **THEN** 输入层 MUST 在该 action 触发时产生 `Attack` action request
- **AND** 后续动作管线 MUST 不直接依赖 `Player/Fire` 名字

#### Scenario: 来源 action 缺失
- **WHEN** Profile 中的 action identity 无法在来源 asset 中解析
- **THEN** 输入层 MUST 报告配置错误
- **AND** 输入层 MUST NOT 回退为按显示名查找

### Requirement: CharacterInputStage 每 tick 产出 CharacterInputFrame
系统 MUST 让 `CharacterInputStage` 在 pipeline logic tick 中产出当前 `LocalLogicTick` 的 `CharacterInputFrame`。连续或保持型输入值和动作触发边沿 MUST 先在本地表现帧锁存，logic tick MUST 消费该锁存输入。Frame MUST 包含 local logic tick、input sequence、authority mode、typed input values 和本 tick 新产生的 action requests。Frame MUST NOT 使用服务端 `ServerTick` 作为本地输入帧身份，也 MUST NOT 把 Graph 可读输入值命名为 command。

#### Scenario: 本地预测角色消费锁存输入
- **WHEN** pipeline authority mode 是 `LocalPredicted`
- **THEN** InputStage MUST 从本地表现帧锁存的输入快照读取 input values 和动作触发边沿
- **AND** InputStage MUST 写入当前 `LocalLogicTick` 的 `CharacterInputFrame`

#### Scenario: 单表现帧内补多个本地逻辑 tick
- **WHEN** 一个表现帧内因为 catch-up 推进多个 `LocalLogicTick`
- **AND** 该表现帧中某个动作输入被触发一次
- **THEN** InputStage MUST 只在一个 logic tick 中产生一次对应 action request
- **AND** 后续 catch-up tick MUST 继续读取同一份 input values，但 MUST NOT 重复产生同一触发请求

#### Scenario: 表现帧没有推进本地逻辑 tick
- **WHEN** 当前表现帧采样到了动作触发边沿
- **AND** accumulator 尚未推进新的 `LocalLogicTick`
- **THEN** InputStage MUST 保留该触发边沿
- **AND** 下一次 logic tick MUST 能消费该触发并创建 action request

#### Scenario: 远端代理角色不采样本地输入
- **WHEN** pipeline authority mode 是 `RemoteProxy`
- **THEN** InputStage MUST NOT 从本地 InputAction 产生本地 action request
- **AND** 远端表现 MUST 由 network receive 注入的快照或事件驱动

### Requirement: 连续输入作为 input value 保存且不消费
系统 MUST 将 MoveAxis、LookAxis、AimAxis、SprintHeld 等连续或保持型输入保存为 typed input value。Input value MUST 每 tick 覆盖当前值，MUST NOT 进入 request 消费语义，也 MUST NOT 在 Graph/BTSMTL 中命名为 command。

#### Scenario: 移动输入进入预测
- **WHEN** `MoveAxis` input value 在当前 tick 读取到 Vector2 值
- **THEN** `CharacterInputFrame` MUST 保存该 `MoveAxis` input value
- **AND** Locomotion 或 Motion 模块 MAY 使用该 input value 立即驱动本地表现

#### Scenario: 按住输入不被消费
- **WHEN** `SprintHeld` 在多帧中保持 true
- **THEN** 每个 tick 的 frame MUST 能读取该 input value
- **AND** 读取该 input value MUST NOT 将其标记为 consumed

### Requirement: ClientCommandFrame 来源于 CharacterInputFrame 但不进入 Graph 语义
系统 MUST 从 `CharacterInputFrame` 和 pipeline 输出生成 `ClientCommandFrame` 或等价网络/预测 sync fact。该网络事实 MUST 使用 input values、action requests、`InputSequence` 与 `LocalLogicTick`。`ClientCommandFrame` MUST NOT 直接保存 raw InputAction performed 事件、action 显示名或服务端 `ServerTick` 作为网络协议语义。BTSMTL/Graph MUST NOT 读取、创建或依赖 `ClientCommandFrame`。

#### Scenario: 收集移动输入事实
- **WHEN** 当前 local logic tick 的 frame 包含 `MoveAxis` input value
- **THEN** SyncFacts 或 NetworkSendStage MUST 能收集包含 input sequence、local logic tick 和 `MoveAxis` 摘要的 `ClientCommandFrame`
- **AND** NetworkSendStage MUST 只收集该网络事实，不直接发送 transport 消息

#### Scenario: 收集动作请求事实
- **WHEN** 当前 local logic tick 产生 `Attack` action request
- **THEN** `ClientCommandFrame` MUST 包含该 request id、input sequence 和 local logic tick
- **AND** 网络层 MUST NOT 依赖 Unity InputAction 名称判断攻击语义

#### Scenario: Graph 不处理网络命令
- **WHEN** BTSMTL Graph 需要读取移动或攻击输入
- **THEN** 它 MUST 读取 input value 或 action request buffer
- **AND** 它 MUST NOT 读取 `ClientCommandFrame` 或任何 network command 对象

### Requirement: GraphContext 读取同一输入帧和请求缓存
系统 MUST 让 `CharacterGraphContext` 暴露当前 `CharacterInputFrame`、`CharacterInputRequestBuffer` 和输入历史入口。BTSMTL 节点、TransitionRuleGraph 和后续 gameplay 节点 MUST 通过 graph context 读取 input value 或 action request，不得场景搜索、直接读取 transport 或处理 network command。

#### Scenario: InputAction ValueNode 读取 raw 输入
- **WHEN** 现有 InputAction ValueNode 被请求输出值
- **THEN** graph context MUST 通过同一个 InputStage 或输入来源读取 typed value
- **AND** 节点 MUST NOT 搜索 `PlayerInput` 或其它场景对象

#### Scenario: 输入值节点读取 frame
- **WHEN** 输入值节点读取 `MoveAxis`
- **THEN** 节点 MUST 从 graph context 当前 `CharacterInputFrame` 读取 `MoveAxis` input value
- **AND** 节点 MUST NOT 直接解析 Unity InputActionAsset

#### Scenario: 请求查询不消费
- **WHEN** TransitionRuleGraph 查询 `HasRequest(Attack)`
- **THEN** graph context MUST 从 request buffer 返回非消费查询结果
- **AND** 查询 MUST NOT 改变 request consumed 状态
