## ADDED Requirements

### Requirement: 动画贡献必须携带循环插值上下文

当动画贡献来自循环 Timeline 或循环 clip 时，贡献 MUST 携带足够的循环上下文，使动画层运行时和表现层能够区分“时间回绕后的前进播放”和“真的反向播放”。该上下文 MAY 使用 loop flag、clip duration、cycle index、连续 clip time 或等价数据表达，但 MUST 进入正式 `AnimationContribution` 到 `AnimationLayerPlaybackPlan` 的链路。

#### Scenario: 循环 clip time 从末尾回到开头

- **WHEN** 上一表现样本的 clip time 接近 clip duration
- **AND** 当前表现样本的 clip time 回到 0 附近
- **AND** 贡献标记为循环播放
- **THEN** `CharacterPresentationStage` MUST 按前进方向跨边界插值
- **AND** 表现层 MUST NOT 对两个 local clip time 做普通反向 lerp

#### Scenario: 非循环 clip 保持现有插值

- **WHEN** 动画贡献未标记为循环播放
- **THEN** 动画层运行时和表现层 MUST 保持现有非循环 clip time 解释
- **AND** 系统 MUST NOT 因新增循环字段改变一次性 Timeline 的完成表现

### Requirement: 状态切换混合必须保留 outgoing 播放计划而非旧状态逻辑

表现层进行状态切换动画混合时，MUST 使用上一帧正式动画层播放计划作为 outgoing pose 来源，并使用新 active state 当前产生的播放计划作为 incoming pose 来源。系统 MUST NOT 为了获得 outgoing pose 继续 tick 旧状态行为图、旧 Timeline 或旧 Action 逻辑。

#### Scenario: WalkLoop 切换到 RunLoop

- **WHEN** Locomotion 状态机从 `WalkLoop` 切换到 `RunLoop`
- **AND** 命中的 Transition edge 配置了动画混合时长
- **THEN** 表现层 MUST 保留上一帧 `WalkLoop` 动画播放计划作为 outgoing
- **AND** 表现层 MUST 使用 `RunLoop` 当前输出作为 incoming
- **AND** 旧 `WalkLoop` 状态行为 MUST NOT 因动画混合继续产出 motion 或其它 gameplay facts

#### Scenario: incoming 状态未产出动画

- **WHEN** transition blend 会话存在
- **AND** 新 active state 没有产出合法动画播放计划
- **THEN** 系统 MUST 暴露为空动画输出或配置错误
- **AND** 表现层 MUST NOT 自动播放隐藏 Idle、旧 locomotion clip 或 adapter fallback
