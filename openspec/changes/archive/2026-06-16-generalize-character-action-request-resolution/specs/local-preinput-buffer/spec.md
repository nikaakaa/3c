## ADDED Requirements

### Requirement: 输入请求种类只作为缓冲键
`InputRequestKind` MUST 只表示输入缓冲中的请求键。系统 MUST NOT 将 `InputRequestKind.Attack`、`InputRequestKind.Dodge`、`InputRequestKind.Jump` 或其它输入请求种类直接当作目标 action state、动画 key、motion spec 或连段阶段。动作语义 MUST 由后续 action request resolver 基于正式配置和当前上下文解析。

#### Scenario: Attack 输入不携带连段阶段
- **GIVEN** 玩家按下 Attack
- **WHEN** 输入缓冲记录该输入
- **THEN** 缓冲记录 MUST 只包含 `InputRequestKind.Attack`、origin step、expire step 和消费状态
- **AND** 缓冲记录 MUST NOT 包含 `Action.Attack01`、`Action.Attack02`、`Action.Attack03` 或动画 key

#### Scenario: Dodge 输入不携带最终动作输出
- **GIVEN** 玩家按下 Dodge
- **WHEN** 输入缓冲记录该输入
- **THEN** 缓冲记录 MUST 只包含 `InputRequestKind.Dodge`、origin step、expire step 和消费状态
- **AND** directional/backstep、target state、motion seed 和 animation seed MUST 由后续 resolver 决定

#### Scenario: Rejected 请求保持输入语义
- **GIVEN** 某个 buffered input request 被 action resolver 或 arbiter 拒绝
- **WHEN** 请求仍未过期
- **THEN** 输入缓冲 MAY 保留该请求供后续帧重新评估
- **AND** 保留的数据 MUST 仍是输入请求键而不是 resolved action
