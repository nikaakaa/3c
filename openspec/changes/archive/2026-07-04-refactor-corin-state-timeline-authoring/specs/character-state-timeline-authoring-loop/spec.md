## ADDED Requirements

### Requirement: Corin RootTree 必须只表达角色主流程层

Corin RootTree MUST 作为角色每 tick 的主流程编排层。RootTree SHOULD 包含 Runtime Loop、输入/运动入口、Locomotion StateMachine 和 Action StateMachine 等高层节点。RootTree MUST NOT 平铺某个具体攻击的 action activation、Timeline playback、lifecycle、window、cue 或 loopback result 输出节点。

#### Scenario: 打开 Corin RootTree

- **WHEN** 作者打开 Corin RootTree
- **THEN** 作者 SHOULD 看到 Locomotion StateMachine 和 Action StateMachine
- **AND** 具体 `Attack1` 的 Timeline、window、cue 和 lifecycle 细节 MUST 位于 Action StateMachine 的状态下钻图中
- **AND** RootTree MUST NOT 显示 `Activate Attack`、`Play Attack Timeline`、`Submit Attack Window`、`Submit Attack Cue` 或 `Submit Loopback Result`

### Requirement: Corin Locomotion 必须使用 StateMachine + Timeline 编排

Corin locomotion 第一阶段 MUST 使用状态机表达基础移动表现状态，并在状态行为内通过 TimelineNode 播放对应 Timeline。第一阶段状态至少包含 `Idle`、`WalkStart`、`WalkLoop`、`WalkEnd`、`RunStart`、`RunLoop`、`RunEnd` 和 `MovingTurn`。

#### Scenario: Walk 起步

- **WHEN** Move input magnitude 超过 walk threshold 且低于 run threshold
- **THEN** Locomotion StateMachine MUST 从 `Idle` 进入 `WalkStart`
- **AND** `WalkStart` 状态行为 MUST 播放 walk start Timeline
- **AND** `WalkStart -> WalkLoop` MUST 能使用 `StateRootCompleted` 条件

#### Scenario: Run 停止

- **WHEN** `RunLoop` 中 Move input magnitude 低于 stop threshold
- **THEN** Locomotion StateMachine MUST 进入 `RunEnd`
- **AND** `RunEnd` 状态行为 MUST 播放 run end Timeline
- **AND** `RunEnd -> Idle` MUST 能使用 `StateRootCompleted` 条件

#### Scenario: 运动中转身

- **WHEN** 移动中方向变化超过 turn threshold
- **THEN** Locomotion StateMachine MUST 能进入 `MovingTurn`
- **AND** `MovingTurn` 状态行为 MUST 播放 turn Timeline
- **AND** 完成后 MUST 回到 walk/run loop 或等价持续移动状态

### Requirement: Corin 基础连招必须使用 Action StateMachine + Timeline 编排

Corin 基础连招第一阶段 MUST 使用 Action StateMachine 表达动作状态，使用 ActionProfile 激活动作事务，使用带 Action Context 的 TimelineNode 播放攻击 Timeline。第一阶段至少包含 `None`、`Attack1` 和 `Attack2`。

#### Scenario: Attack1

- **WHEN** `None` 状态收到 Attack input request
- **THEN** Action StateMachine MUST 进入 `Attack1`
- **AND** `Attack1` OnEnter MUST 激活对应 ActionProfile
- **AND** `Attack1` Root MUST 播放带 Action Context 的 Timeline

#### Scenario: Attack2

- **WHEN** `Attack1` 中满足 combo 条件并收到 Attack input request 或等价 buffer
- **THEN** Action StateMachine MUST 进入 `Attack2`
- **AND** `Attack2` MUST 拥有自己的 Action Context
- **AND** `Attack2` Timeline 输出 MUST 继续使用 ActionProfile 策略解析

### Requirement: Corin 资产闭环不得创建一次性 SubTree asset

Corin 的 `Locomotion` 状态行为和基础连招状态行为 MUST 默认保存为 StateNode 内部 inline graph data。只有多个状态明确复用同一行为图时，作者 MAY 显式抽取 shared `BaseTreeAsset`。

#### Scenario: Attack1 状态行为

- **WHEN** 作者下钻 `Attack1` StateNode
- **THEN** 编辑器 MUST 打开该 StateNode 的 inline StateBehaviorSubTree
- **AND** 项目 MUST NOT 为这个一次性状态 body 创建 `Attack1SubTree.asset`

#### Scenario: 显式复用

- **WHEN** 作者决定 `WalkLoop` 和其它角色复用同一行为图
- **THEN** 作者 MAY 使用 Extract Shared 或等价显式复用操作创建 shared asset
- **AND** owner inline 真数据 MUST 被清理
