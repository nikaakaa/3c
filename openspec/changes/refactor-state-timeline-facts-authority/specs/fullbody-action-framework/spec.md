## ADDED Requirements
### Requirement: Character Frame Context 拥有 Current Timeline Facts
Character frame context MUST 在 Action request submission / interrupt arbitration 之前生成当前状态的 current timeline facts，并将该 facts 作为本帧固定输入传递给请求仲裁、统一状态机推进和状态输出解析。

#### Scenario: 请求仲裁前生成 facts
- **WHEN** Character frame pipeline 处理一帧输入
- **THEN** Character frame context MUST 先基于当前状态快照、播放进度和 timeline policy 生成 current timeline facts
- **AND** MUST 再调用 Action request submission / interrupt arbitration

#### Scenario: Resolver 不反向理解状态机结构
- **WHEN** Action request submission resolver 判断 Dodge、Attack 或等价请求
- **THEN** resolver MUST 消费传入的 current timeline facts
- **AND** MUST NOT 接收 `CharacterStateMachineDefinition` 以自行采样当前状态窗口

#### Scenario: 调度顺序可测试
- **WHEN** 运行 Character frame pipeline 静态边界测试
- **THEN** 测试 MUST 能确认 current timeline facts 在 request submission / interrupt arbitration 前准备
- **AND** request submission / interrupt arbitration、state machine runner 和 output resolver 使用同一帧 facts 输入
