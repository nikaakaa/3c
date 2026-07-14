# character-input-pipeline Specification

## Purpose
定义角色输入管线：`CharacterInputProfile` 将 Unity InputAction 映射为 gameplay input value 和 action request，输入层负责表现帧采样、逻辑 tick 消费、request buffer、input sequence 和本地输入历史。
## Requirements
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

系统 MUST 让 `CharacterInputStage` 在 pipeline logic tick 中产出当前 `LocalLogicTick` 的 `CharacterInputFrame`。Frame MUST 包含 local logic tick、input sequence、input source、typed input values 和本 tick新产生的 action requests。LocalDevice source MUST 消费表现帧锁存输入；ExternalFacts source MUST 消费显式注入输入；None MUST 不产生控制输入。Frame MUST 不保存具体 Network Model、endpoint 或 server tick 作为本地输入身份。

#### Scenario: 本地设备角色消费锁存输入

- **WHEN** Character input source 是 LocalDevice
- **THEN** InputStage MUST 从本地表现帧锁存快照读取 values 和触发边沿
- **AND** MUST 写入当前 logic tick 的 CharacterInputFrame

#### Scenario: 外部事实角色不采样本地设备

- **WHEN** Character input source 是 ExternalFacts
- **THEN** InputStage MUST 不 Enable 或读取本地 InputAction
- **AND** MUST 只消费正式 external input facts

#### Scenario: 单表现帧补多个逻辑 tick

- **WHEN** 一个表现帧 catch-up 多个 logic tick 且某个动作边沿只触发一次
- **THEN** InputStage MUST 只在一个 logic tick 产生一次 request
- **AND** 后续 tick MAY 继续读取连续 input value

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

系统 MUST 提供有界 `CharacterInputHistory` 保存需要记录的 `CharacterInputFrame`，并支持按 LocalLogicTick 与 InputSequence 查询。History 是 Character 输入记录能力，不得把写入条件硬编码为 ServerAuthoritative LocalPredicted，也不得宣称仅凭输入历史已经实现 Rollback。具体 Network Model MUST 决定是否以及如何使用该 history。

#### Scenario: LocalDevice 输入写入历史

- **WHEN** LocalDevice input source 产出当前 tick frame
- **THEN** InputStage MUST 按正式容量写入 history
- **AND** ServerAuthoritative model MAY 使用 sequence 对齐 correction

#### Scenario: ExternalFacts 输入写入历史

- **WHEN** 后续模型要求记录 external input facts
- **THEN** CharacterInputHistory MUST 能保存对应 frame
- **AND** CharacterPipeline MUST 不依赖 model id 才允许写入

### Requirement: GraphContext 读取同一输入帧和请求缓存
系统 MUST 让 `CharacterGraphContext` 暴露当前 `CharacterInputFrame`、`CharacterInputRequestBuffer` 和输入历史入口。BTSMTL 节点、ConditionRuleGraph 和后续 gameplay 节点 MUST 通过 graph context 读取 input value 或 action request，不得场景搜索、直接读取 transport 或处理 network command。

#### Scenario: InputAction ValueNode 读取 raw 输入
- **WHEN** 现有 InputAction ValueNode 被请求输出值
- **THEN** graph context MUST 通过同一个 InputStage 或输入来源读取 typed value
- **AND** 节点 MUST NOT 搜索 `PlayerInput` 或其它场景对象

#### Scenario: 输入值节点读取 frame
- **WHEN** 输入值节点读取 `MoveAxis`
- **THEN** 节点 MUST 从 graph context 当前 `CharacterInputFrame` 读取 `MoveAxis` input value
- **AND** 节点 MUST NOT 直接解析 Unity InputActionAsset

#### Scenario: 请求查询不消费
- **WHEN** ConditionRuleGraph 查询 `HasRequest(Attack)`
- **THEN** graph context MUST 从 request buffer 返回非消费查询结果
- **AND** 查询 MUST NOT 改变 request consumed 状态

### Requirement: Network Model 必须从正式输入或运动事实构造自己的命令

Character input pipeline MUST 只提供 CharacterInputFrame、request buffer 和 input history。ServerAuthoritative model MAY 结合 resolved motion fact 构造 MotionCommand；未来其它模型 MAY 构造 canonical input bundle。任何模型命令 MUST 在 model-owned adapter 中产生。

#### Scenario: 当前模型构造 MotionCommand

- **WHEN** CharacterPipeline 完成本 tick input 和 motion
- **THEN** ServerAuthoritative adapter MUST 读取正式 input/motion facts
- **AND** MUST 在 Character input pipeline 外构造 MotionCommand

