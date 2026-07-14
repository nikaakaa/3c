## MODIFIED Requirements

### Requirement: Corin 基础连招必须使用 Action StateMachine + Timeline 编排

Corin 外层 Action StateMachine MUST 只表达动作大类，至少包含 `None`、`Attack`、`DodgeBack` 和 `DodgeForward`。具体 `Attack1`、`Attack2` MUST 位于 `Attack` StateNode 的 inline StateBehaviorSubTree Root 所运行的内层 `StateMachineNode` 中，MUST NOT 与 Dodge 状态平铺。内层攻击状态 MUST 继续使用 ActionProfile、独立 Action Context 和带 Action Context 的 inline TimelineNode。连段 MUST 使用与 Tree abort 相同的 Runnable stop、State source-exit、Action lifecycle 和 Timeline cancel 分层，不得创建 Action 专用旁路。

#### Scenario: 首次进入 Attack1

- **WHEN** 外层 `None` 检测到 Attack request
- **THEN** 外层 Action StateMachine MUST 进入 `Attack`
- **AND** 外层条件 MUST 只查询而不消费该 request
- **AND** 内层 Attack StateMachine MUST 进入 `Attack1`
- **AND** `Attack1` target activation MUST 消费 request 并创建新的 Action Context

#### Scenario: Attack1 进入 Attack2

- **WHEN** `Attack1Cancel` 在当前 Tick active 且存在 Attack request
- **THEN** 内层 Attack StateMachine MUST 从 `Attack1` 抢占到 `Attack2`
- **AND** source OnExit MUST 提交 `Cancel(ComboWindow)`
- **AND** target activation MUST 消费 request 并创建新的 Action Context

#### Scenario: Attack2 回到 Attack1

- **WHEN** `Attack2Cancel` 在当前 Tick active 且存在 Attack request
- **THEN** 内层 Attack StateMachine MUST 从 `Attack2` 抢占到 `Attack1`
- **AND** condition query MUST NOT 消费 request

#### Scenario: 攻击正常结束

- **WHEN** `Attack1` 或 `Attack2` root 正常完成且没有窗口连段
- **THEN** leaf source MUST 提交一次 Complete 并进入内层 Exit
- **AND** 外层 Attack root MUST 因嵌套 StateMachineNode 成功而完成
- **AND** 外层 Action StateMachine MUST 通过 `StateRootCompleted` 回到 None
- **AND** 外层 Attack OnExit MUST NOT 提交第二条 Action terminal transition

#### Scenario: 打开外层 Action StateMachine

- **WHEN** 作者打开 Corin Action StateMachine
- **THEN** 作者 MUST 看到 `None`、`Attack`、`DodgeBack` 和 `DodgeForward`
- **AND** 作者 MUST NOT 在该层看到 `Attack1` 或 `Attack2`
- **AND** 作者下钻 `Attack` state body 后 MUST 能继续打开 inline Attack Combo StateMachine

## ADDED Requirements

### Requirement: Corin Attack 迁移必须保持现有攻击事实与资源身份

将 Attack1/Attack2 迁入嵌套 StateMachine 时，系统 MUST 保持两段攻击各自的 ActionProfile、Action Context、Timeline playback mode、AnimationTrack、MotionCurveTrack、Hit/Cancel Decision TreeClip、WindowId、Digest、帧范围和 lifecycle reason。迁移 MUST 移动并重绑唯一 inline 数据，MUST NOT 克隆成父子两份真相或创建一次性 shared asset。

#### Scenario: 迁移 Attack1 Timeline

- **WHEN** Attack1 StateNode 从外层 Action graph 迁入内层 Attack graph
- **THEN** 原 Attack1 inline TimelineData MUST 归属迁移后的 Attack1 State body
- **AND** Hit/Cancel TreeClip 的帧范围、declaration reference 和 ActionWindow projection MUST 保持不变
- **AND** 项目 MUST NOT 新增 Attack1 TimelineAsset 或 Attack1SubTree asset

#### Scenario: 清理外层旧结构

- **WHEN** 嵌套 Attack graph 迁移完成
- **THEN** 外层旧 Attack1/Attack2 StateNode、combo edge、完成 edge 和 orphan rule graph MUST 被删除
- **AND** runtime、Snapshot 和 Validator MUST 只读取嵌套后的唯一结构
