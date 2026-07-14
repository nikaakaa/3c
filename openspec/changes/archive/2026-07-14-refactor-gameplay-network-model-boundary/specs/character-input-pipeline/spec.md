## MODIFIED Requirements

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

## REMOVED Requirements

### Requirement: ClientCommandFrame 来源于 CharacterInputFrame 但不进入 Graph 语义

ClientCommandFrame 是旧 ServerAuthoritative wire contract，不能继续作为 Character input pipeline 输出。

#### Scenario: 删除 ClientCommandFrame

- **WHEN** 本 change 完成
- **THEN** CharacterInputStage、SyncFacts 和 NetworkSendStage MUST 不再创建 `ClientCommandFrame`
- **AND** Graph MUST 继续只读取 CharacterInputFrame 和 request buffer

## ADDED Requirements

### Requirement: Network Model 必须从正式输入或运动事实构造自己的命令

Character input pipeline MUST 只提供 CharacterInputFrame、request buffer 和 input history。ServerAuthoritative model MAY 结合 resolved motion fact 构造 MotionCommand；未来其它模型 MAY 构造 canonical input bundle。任何模型命令 MUST 在 model-owned adapter 中产生。

#### Scenario: 当前模型构造 MotionCommand

- **WHEN** CharacterPipeline 完成本 tick input 和 motion
- **THEN** ServerAuthoritative adapter MUST 读取正式 input/motion facts
- **AND** MUST 在 Character input pipeline 外构造 MotionCommand

