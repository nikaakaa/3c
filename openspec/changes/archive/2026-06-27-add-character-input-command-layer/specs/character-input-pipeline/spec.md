# character-input-pipeline Specification

## Purpose
定义角色输入层的正式运行时模型：从 Unity InputAction 采样到 gameplay semantic input frame，再进入本地预测、动作请求缓存、BTSMTL 图上下文和网络输出。该能力依赖 `character-pipeline-runtime`，不新增输入专用 Graph、Workbench 路径或真实 transport。

## ADDED Requirements

### Requirement: CharacterInputProfile 映射 InputAction 到 gameplay 语义
系统 MUST 使用 `CharacterInputProfile` 表达角色输入配置。Profile MUST 引用正式 `InputActionAsset`，并将 action 稳定身份映射为 gameplay semantic signal 或 action request。Gameplay 逻辑 MUST 使用 semantic id，而不是直接使用 InputAction 显示名。

#### Scenario: 配置连续输入
- **WHEN** Profile 将 `Player/Move` action 映射为 `Move` signal
- **THEN** 输入层 MUST 使用 action identity 读取来源
- **AND** gameplay、Tree 和 network command MUST 使用 `Move` semantic id

#### Scenario: 配置动作请求
- **WHEN** Profile 将 `Player/Fire` action 映射为 `Attack` request
- **THEN** 输入层 MUST 在该 action 触发时产生 `Attack` action request
- **AND** 后续动作管线 MUST 不直接依赖 `Player/Fire` 名字

#### Scenario: 来源 action 缺失
- **WHEN** Profile 中的 action identity 无法在来源 asset 中解析
- **THEN** 输入层 MUST 报告配置错误
- **AND** 输入层 MUST NOT 回退为按显示名查找

### Requirement: CharacterInputStage 每 tick 产出 CharacterInputFrame
系统 MUST 让 `CharacterInputStage` 在 pipeline update phase 中采样输入，并产出当前 tick 的 `CharacterInputFrame`。Frame MUST 包含 simulation tick、input sequence、authority mode、连续命令集合和本 tick 新产生的动作请求集合。

#### Scenario: 本地预测角色采样输入
- **WHEN** pipeline authority mode 是 `LocalPredicted`
- **THEN** InputStage MUST 从 `CharacterInputProfile` 采样当前 InputAction 值
- **AND** InputStage MUST 写入当前 tick 的 `CharacterInputFrame`

#### Scenario: 远端代理角色不采样本地输入
- **WHEN** pipeline authority mode 是 `RemoteProxy`
- **THEN** InputStage MUST NOT 从本地 InputAction 产生本地 action request
- **AND** 远端表现 MUST 由 network receive 注入的快照或事件驱动

### Requirement: 连续输入作为 command 保存且不消费
系统 MUST 将 Move、Look、Aim、SprintHeld 等连续或保持型输入保存为 continuous command。Continuous command MUST 每 tick 覆盖当前值，MUST NOT 进入 request 消费语义。

#### Scenario: 移动输入进入预测
- **WHEN** `Move` signal 在当前 tick 读取到 Vector2 值
- **THEN** `CharacterInputFrame` MUST 保存该 `Move` command
- **AND** Motion 或预测阶段 MAY 使用该 command 立即驱动本地表现

#### Scenario: 按住输入不被消费
- **WHEN** `SprintHeld` 在多帧中保持 true
- **THEN** 每个 tick 的 frame MUST 能读取该 held command
- **AND** 读取该 command MUST NOT 将其标记为 consumed

### Requirement: 离散动作输入进入 request buffer
系统 MUST 将 Attack、Dodge、Jump、Interact 等离散动作输入写入 `CharacterInputRequestBuffer`。每个 request MUST 保存 request id、created tick、input sequence、过期信息、priority 和 consumed 状态。

#### Scenario: 硬直中预输入攻击
- **WHEN** 玩家在当前状态不可攻击时触发 `Attack`
- **THEN** InputStage MUST 将 `Attack` 写入 request buffer
- **AND** 该 request MUST 在配置的 buffer 时间内保持可查询

#### Scenario: 请求过期
- **WHEN** `Attack` request 超过配置的 buffer 时间仍未被消费
- **THEN** request buffer MUST 将该 request 视为不可用
- **AND** 后续查询 MUST NOT 返回该过期 request

#### Scenario: 请求被消费
- **WHEN** 状态行为或动作管线正式接受 `Dodge` request
- **THEN** request buffer MUST 将该 request 标记为 consumed
- **AND** 同一 request MUST NOT 被第二次消费

### Requirement: CharacterInputHistory 保存预测重放所需输入帧
系统 MUST 提供 `CharacterInputHistory` 保存最近若干 tick 的 `CharacterInputFrame`。本变更 MAY 不实现完整 correction replay，但输入历史的写入和查询边界 MUST 是正式路径。

#### Scenario: 保存本地输入帧
- **WHEN** `LocalPredicted` pipeline 产出当前 tick input frame
- **THEN** InputStage MUST 将该 frame 写入 input history
- **AND** history MUST 能按 simulation tick 或 input sequence 查询

#### Scenario: 收到校正后准备重放
- **WHEN** NetworkReceiveStage 收到 correction
- **THEN** 后续 prediction correction 逻辑 MUST 从 `CharacterInputHistory` 获取未确认输入
- **AND** 系统 MUST NOT 重新从当前 InputAction 状态推导历史输入

### Requirement: ClientCommand 来源于 CharacterInputFrame
系统 MUST 从 `CharacterInputFrame` 生成 `ClientCommand` 并写入 `NetworkOutput`。`ClientCommand` MUST 使用 gameplay command/request 数据，MUST NOT 直接保存 raw InputAction performed 事件或 action 显示名作为网络协议语义。

#### Scenario: 收集移动命令
- **WHEN** 当前 tick 的 frame 包含 `Move` command
- **THEN** NetworkOutput MUST 能收集包含 input sequence、simulation tick 和 `Move` command 的 `ClientCommand`
- **AND** NetworkSendStage MUST 只收集该 command，不在本能力中直接发送 transport 消息

#### Scenario: 收集动作请求
- **WHEN** 当前 tick 产生 `Attack` request
- **THEN** `ClientCommand` MUST 包含该 request 的 semantic id 和 input sequence
- **AND** 网络层 MUST NOT 依赖 Unity InputAction 名称判断攻击语义

### Requirement: GraphContext 读取同一输入帧和请求缓存
系统 MUST 让 `CharacterGraphContext` 暴露当前 `CharacterInputFrame`、`CharacterInputRequestBuffer` 和输入历史入口。BTSMTL 节点、TransitionRuleGraph 和后续 gameplay 节点 MUST 通过 graph context 读取输入，不得场景搜索或直接读取 transport。

#### Scenario: InputAction ValueNode 读取 raw 输入
- **WHEN** 现有 InputAction ValueNode 被请求输出值
- **THEN** graph context MUST 通过同一个 InputStage 或输入来源读取 typed value
- **AND** 节点 MUST NOT 搜索 `PlayerInput` 或其它场景对象

#### Scenario: 语义输入节点读取 frame
- **WHEN** 后续语义输入节点读取 `Move`
- **THEN** 节点 MUST 从 graph context 当前 `CharacterInputFrame` 读取 `Move` command
- **AND** 节点 MUST NOT 直接解析 Unity InputActionAsset

#### Scenario: 请求查询不消费
- **WHEN** TransitionRuleGraph 查询 `HasRequest(Attack)`
- **THEN** graph context MUST 从 request buffer 返回非消费查询结果
- **AND** 查询 MUST NOT 改变 request consumed 状态
