## ADDED Requirements

### Requirement: 状态进入运动基准事实
统一状态机 MUST 能在进入需要 `EntryLocal` 动画运动的状态时捕获纯数据 entry planar basis，并通过状态帧和 restore state 传递给后续 locomotion/motion pipeline。该基准 MUST 与输入锁定方向、目标方向和当前 root transform 保持语义区分。

#### Scenario: TurnBack 捕获进入 facing
- **GIVEN** transition 进入 `FullBody/Locomotion/TurnBack`
- **WHEN** 状态机应用该 transition
- **THEN** 状态机 MUST 捕获进入 TurnBack 时的角色平面 facing 作为 entry basis
- **AND** 状态帧 MUST 携带该 basis 给 movement facts 构建阶段

#### Scenario: 输入方向不替代 entry basis
- **GIVEN** TurnBack request 携带反向移动输入方向
- **WHEN** 状态机进入 TurnBack
- **THEN** 系统 MAY 继续保存该输入方向用于 desired facing 或锁定方向事实
- **AND** MUST NOT 将该输入方向误用为 `EntryLocal` translation basis

#### Scenario: Restore 后基准稳定
- **GIVEN** 状态机 restore state 捕获了 TurnBack entry basis
- **WHEN** rollback restore 该状态机
- **THEN** restore 后的状态帧 MUST 继续输出同一 entry basis
- **AND** MUST NOT 从 restore 后的当前 Transform 临时重新计算 basis
