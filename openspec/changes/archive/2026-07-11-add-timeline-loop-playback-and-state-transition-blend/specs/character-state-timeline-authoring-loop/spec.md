## ADDED Requirements

### Requirement: Corin 循环 locomotion 状态必须使用 TimelineNode Loop 播放模式

Corin locomotion 中表达持续姿态或持续移动的循环状态 MUST 通过状态 body 内的 `TimelineNode PlaybackMode=Loop` 播放对应 Timeline。状态 body MUST NOT 使用普通 `LoopNode` 包住 `TimelineNode` 来表达动画循环。RootTree 顶层 `Runtime Loop` MAY 保留，用于角色主流程持续运行。

#### Scenario: Idle 循环姿态

- **WHEN** 作者打开 Corin `Idle` 状态 body
- **THEN** body SHOULD 直接包含播放 idle Timeline 的 `TimelineNode`
- **AND** 该 `TimelineNode` 播放模式 MUST 是 `Loop`
- **AND** body MUST NOT 通过普通 `LoopNode` 重启该 `TimelineNode`

#### Scenario: WalkLoop 和 RunLoop 持续移动

- **WHEN** 作者打开 Corin `WalkLoop` 或 `RunLoop` 状态 body
- **THEN** body SHOULD 直接包含对应 locomotion loop Timeline 的 `TimelineNode`
- **AND** 该 `TimelineNode` 播放模式 MUST 是 `Loop`
- **AND** 状态离开 MUST 由同层 Condition rule 决定

#### Scenario: 一次性 locomotion 状态

- **WHEN** 作者打开 `WalkStart`、`WalkEnd`、`RunStart`、`RunEnd` 或 `MovingTurn` 状态 body
- **THEN** 对应 TimelineNode SHOULD 使用 `Once` 播放模式
- **AND** `StateRootCompleted` 或其它正式 Transition 条件 MUST 决定状态离开

### Requirement: Corin 状态切换动画混合必须配置在 Transition 边

Corin locomotion 和基础 action 的跨状态动画转换 SHOULD 使用 `StateMachineGraph` Transition edge 上的正式动画混合字段表达。Timeline clip 的 start / duration / ease 字段只表达同一 Timeline 内 clip 混合，MUST NOT 被当作跨状态 transition blend 配置。

#### Scenario: Locomotion 循环状态之间切换

- **WHEN** 作者配置 `WalkLoop -> RunLoop` 或 `RunLoop -> WalkLoop`
- **THEN** 动画混合时长 SHOULD 写在对应 Transition edge 上
- **AND** `WalkLoop` 和 `RunLoop` TimelineNode MUST 仍通过各自状态 body 输出动画贡献

#### Scenario: 起步状态切到循环状态

- **WHEN** 作者配置 `WalkStart -> WalkLoop` 或 `RunStart -> RunLoop`
- **THEN** 状态离开条件 MAY 使用 `StateRootCompleted`
- **AND** 视觉衔接 SHOULD 使用 Transition edge 动画混合字段
- **AND** 系统 MUST NOT 通过把 start Timeline 放进 loop Timeline 末尾来伪造跨状态混合
