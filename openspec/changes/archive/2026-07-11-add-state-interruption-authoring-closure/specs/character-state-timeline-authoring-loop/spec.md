## MODIFIED Requirements

### Requirement: Corin Locomotion 必须使用 StateMachine + Timeline 编排

Corin locomotion MUST 使用状态机表达基础移动状态。有独立时序内容的状态 MUST 在状态行为内通过 TimelineNode 播放对应 Timeline；没有独立动画资源的状态 MUST 使用 Transition blend 衔接，不得创建伪 Timeline 或 fallback clip。状态至少包含 `Idle`、`WalkStart`、`WalkLoop`、`WalkEnd`、`RunStart`、`RunLoop`、`RunEnd` 和 `MovingTurn`。所有 start、end、loop 和 turn 状态 MUST 通过明确 Transition 响应适用输入变化，并复用统一 State source-exit 生命周期。

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

#### Scenario: RunLoop 与 MovingTurn

- **WHEN** RunLoop 或 MovingTurn 的输入进入 Stop、Walk 或有效 Run/Turn 区间
- **THEN** 状态机 MUST 按明确 edge 切换到 RunEnd、WalkLoop、MovingTurn 或 RunLoop
- **AND** 同 source 多条边 MUST 使用稳定 priority

### Requirement: Corin 基础连招必须使用 Action StateMachine + Timeline 编排

Corin 基础连招 MUST 使用 Action StateMachine、ActionProfile 和带 Action Context 的 TimelineNode。状态至少包含 `None`、`Attack1` 和 `Attack2`。连段 MUST 使用与 Tree abort 相同的 Runnable stop、State source-exit、Action lifecycle 和 Timeline cancel 分层，不得创建 Action 专用旁路。

#### Scenario: Attack1 进入 Attack2

- **WHEN** Attack1Cancel 在当前 Tick active 且存在 Attack request
- **THEN** Action StateMachine MUST 从 Attack1 抢占到 Attack2
- **AND** source OnExit MUST 提交 `Cancel(ComboWindow)`
- **AND** target activation MUST 消费 request

#### Scenario: Attack2 回到 Attack1

- **WHEN** Attack2Cancel 在当前 Tick active 且存在 Attack request
- **THEN** Action StateMachine MUST 从 Attack2 抢占到 Attack1
- **AND** condition query MUST NOT 消费 request

#### Scenario: 攻击正常结束

- **WHEN** Attack1 或 Attack2 root 正常完成且没有窗口连段
- **THEN** source MUST 提交 Complete 并通过完成边回到 None
- **AND** OnExit MUST 走无操作成功分支，不提交第二条 terminal transition

