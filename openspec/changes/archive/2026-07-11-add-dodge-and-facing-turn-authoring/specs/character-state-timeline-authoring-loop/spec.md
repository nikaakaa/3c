## MODIFIED Requirements

### Requirement: Corin Locomotion 必须使用 StateMachine + Timeline 编排

Corin locomotion MUST 使用状态机表达基础移动与分层所有权状态。有独立时序内容的状态 MUST 在状态行为内通过 TimelineNode 播放对应 Timeline；没有独立动画资源的状态 MUST 使用 Transition blend 或无表现 ownership state 衔接，不得创建伪 Timeline 或 fallback clip。状态至少包含 `Idle`、`WalkStart`、`WalkLoop`、`WalkEnd`、`RunStart`、`RunLoop`、`RunEnd`、`MovingTurn` 和 `ActionOverride`。所有 start、end、loop、turn 和 ownership 状态 MUST 通过明确 Transition 响应输入与 ownership fact，并复用统一 State source-exit 生命周期。

#### Scenario: WalkStart 输入抢占

- **WHEN** WalkStart root 尚未完成且输入进入 Run 或 Stop 区间
- **THEN** 状态机 MUST 分别允许进入 RunStart 或 WalkEnd
- **AND** source TimelineNode MUST 通过 State root stop 取消

#### Scenario: WalkEnd 恢复移动

- **WHEN** WalkEnd root 尚未完成且输入进入 Walk 或 Run 区间
- **THEN** 状态机 MUST 分别允许进入 WalkStart 或 RunStart
- **AND** 没有 WalkEnd 独立动画时 MUST 使用 Transition blend

#### Scenario: RunStart 输入抢占

- **WHEN** RunStart root 尚未完成且输入进入 Stop 或 Walk 区间
- **THEN** 状态机 MUST 分别允许进入 RunEnd 或 WalkLoop
- **AND** MUST NOT 等待 RunStart Timeline 自然完成

#### Scenario: RunEnd 恢复移动

- **WHEN** RunEnd root 尚未完成且输入进入 Walk 或 Run 区间
- **THEN** 状态机 MUST 分别允许进入 WalkStart 或 RunStart
- **AND** 输入恢复边 MUST 优先于 Completed AND Stop 的 Idle 边

#### Scenario: RunLoop 与 MovingTurn 输入抢占

- **WHEN** RunLoop 或 MovingTurn 的输入进入 Stop、Walk 或有效 Run/Turn 区间
- **THEN** 状态机 MUST 按明确 edge 切换到 RunEnd、WalkLoop、MovingTurn 或 RunLoop
- **AND** 同 source 多条边 MUST 使用稳定 priority

#### Scenario: Dodge 活跃时交出 locomotion 所有权

- **WHEN** 任一普通 locomotion state 读取到 pipeline blackboard `IsDodging=true`
- **THEN** Locomotion StateMachine MUST 以高优先级进入 ActionOverride
- **AND** ActionOverride MUST NOT 播放动画、引用 Dodge Timeline 或提交 motion contribution

#### Scenario: Dodge 完成后有输入进入 RunLoop

- **WHEN** ActionOverride 读取到 `IsDodging=false`
- **AND** 当前 MoveAxis 大于 stop threshold
- **THEN** Locomotion StateMachine MUST 直接进入 RunLoop
- **AND** MUST NOT 重复进入 RunStart

#### Scenario: Dodge 完成后无输入进入 RunEnd

- **WHEN** ActionOverride 读取到 `IsDodging=false`
- **AND** 当前 MoveAxis 不大于 stop threshold
- **THEN** Locomotion StateMachine MUST 进入 RunEnd

#### Scenario: ActionOverride 保持单一职责

- **WHEN** 作者下钻 ActionOverride StateNode
- **THEN** inline state body MUST 不包含 Dodge request consume、ActionProfile、Timeline、animation 或 motion node
- **AND** 项目 MUST NOT 为 ActionOverride 创建一次性 SubTree asset

#### Scenario: MovingTurn 使用角色朝向误差

- **WHEN** RunLoop 中有效 Run 输入的 camera-relative 期望世界方向与 tick 起点 actor forward 夹角达到 turn threshold
- **THEN** Locomotion StateMachine MUST 进入 MovingTurn
- **AND** 条件 MUST NOT 使用相邻 logic tick 的 MoveAxis 差角替代 actor facing error
- **AND** turn threshold MUST 继续来自可调 ExposedProperty

#### Scenario: ownership edge 优先级

- **WHEN** 同一 source state 同时满足普通 Walk/Run/Turn 条件和 `IsDodging=true`
- **THEN** ActionOverride edge MUST 使用稳定更高 priority 获胜
- **AND** 状态机 MUST NOT 创建重复 source-target edge
