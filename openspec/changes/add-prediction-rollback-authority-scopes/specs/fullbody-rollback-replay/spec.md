## ADDED Requirements

### Requirement: FullBody replay 使用权威域比较
FullBody rollback replay MUST 使用预测回滚权威域和比较域判断 replay 结果。FullBody 状态机、Action gameplay facts、Locomotion gameplay facts、motion executor state 和 profile-driven motion facts MUST 属于 strict gameplay；纯视觉动画播放漂移 MUST 可诊断但 MUST NOT 单独导致 FullBody replay 失败。

#### Scenario: FullBody gameplay mismatch 仍失败
- **GIVEN** FullBody replay 后 action state 或 locomotion state 不一致
- **WHEN** 本地 synctest 比较快照
- **THEN** 结果 MUST 包含 strict differences
- **AND** FullBody replay MUST 失败

#### Scenario: 视觉 animation drift 不阻塞 FullBody replay
- **GIVEN** FullBody replay 后只有 Action animation normalized time 或 MoveLoop visual playback time 不一致
- **WHEN** 本地 synctest 比较快照
- **THEN** 结果 MUST 保留 presentation differences
- **AND** FullBody replay MAY 成功

#### Scenario: Profile-driven motion 仍严格
- **GIVEN** FullBody replay 期间处于 TurnBack 或等价 profile-driven motion 状态
- **WHEN** playback window、profile delta、root position 或 yaw 不一致
- **THEN** 结果 MUST 包含 strict differences
- **AND** FullBody replay MUST 失败

### Requirement: FullBody 不写业务类型特判
FullBody replay MUST NOT 因项目暂时偏向 MOBA/MMO 或格斗而写死一套业务类型分支。业务差异 MUST 通过状态 policy、timeline facts、motion source 或 compare scope 声明表达。

#### Scenario: MOBA/MMO 风格状态
- **WHEN** 某技能状态声明逻辑窗口由 simulation tick 掌权，动画只表现
- **THEN** hit/cancel/recovery facts MUST 能 strict 比较
- **AND** animation visual playback drift MUST 能只作为 presentation differences

#### Scenario: 格斗风格状态
- **WHEN** 某攻击状态声明动画播放帧直接驱动 hitbox 或取消窗口
- **THEN** 该播放时钟 MUST 被标记为 strict gameplay
- **AND** replay 差异 MUST 导致 strict failure

#### Scenario: 同一主线支持不同策略
- **WHEN** 两个状态使用不同 compare scope
- **THEN** 它们 MUST 仍通过同一 FullBody replay adapter 推进
- **AND** MUST NOT 为某个业务类型创建第二套 replay 主线
