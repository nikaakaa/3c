## ADDED Requirements
### Requirement: FullBody Action 抢占只提交 Locomotion preemption candidate
FullBody Action framework MUST 在 full-body action 开始并占用 base layer 时提交 Locomotion preemption candidate 或等价纯数据输出。FullBody Action framework MUST NOT 直接拥有、重置或推进 Locomotion runtime；Locomotion 的状态退出和残留清理 MUST 由 Locomotion module 消费 preemption fact 后完成。

#### Scenario: Dodge 开始时提交抢占候选
- **GIVEN** 当前 Action lifecycle 从 `Action.None` 开始 `Action.Dodge`
- **AND** Dodge action claim 为 full-body motion/animation claim
- **AND** 当前 Locomotion frame 表示可抢占 transient motion source
- **WHEN** FullBody Action submitter 构建本帧输出
- **THEN** submitter MUST 提交 Locomotion preemption candidate
- **AND** candidate MUST 保留 `Action.Dodge` 作为 source action id
- **AND** submitter MUST NOT 直接调用 Locomotion runtime 私有清理方法

#### Scenario: Dodge 持续帧不重复提交同一抢占
- **GIVEN** Action lifecycle 已经 active `Action.Dodge`
- **AND** 本帧不是 Dodge started frame
- **WHEN** FullBody Action submitter 构建本帧输出
- **THEN** submitter MUST NOT 重复提交同一次 TurnBack 抢占 candidate
- **AND** Locomotion preemption fact MUST 保持一次性消费语义

#### Scenario: 后续 full-body action 复用同一契约
- **WHEN** 后续新增 HitReact、Knockback、Attack 或等价 full-body action
- **THEN** 这些 action MAY 通过同一 preemption candidate contract 抢占 Locomotion transient
- **AND** MUST NOT 为每个 action 新增直接修改 Locomotion state 的专用路径
