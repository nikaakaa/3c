# character-motion-semantics Specification

## ADDED Requirements

### Requirement: MotionContribution 必须区分位移 Delta 与低层 Channel 占用

MotionContribution 与 TimelineMotionCurveContribution MUST分别表达本 tick 是否包含位移 delta，以及是否通过 Override + ConsumeLowerChannels 占用并消费低层 channel。零 delta Override claim MUST可以成为当前 channel winner 并清空已累计低层 motion；零 delta Additive 或 WeightedBlend MUST不产生 channel claim。

#### Scenario: 攻击 Recovery 保持原地

- **WHEN** Attack MotionCurve 已到达累计曲线终点
- **AND** MotionCurveClip 仍在正式占权区间且配置 ConsumeLowerChannels
- **THEN** Timeline MUST提交零 delta Action channel claim
- **AND** MotionResolver MUST阻止 Locomotion contribution 在该 tick 生效

#### Scenario: 零 Additive Contribution

- **WHEN** Additive 或 WeightedBlend contribution 的 displacement 与 yaw 都为零
- **THEN** MotionResolver MUST忽略该 contribution
- **AND** MUST不消费低层 channel

### Requirement: MotionCurveClip 必须分开曲线结束与占权结束

MotionCurveClip MUST显式保存满足 `StartFrame < CurveEndFrame <= EndFrame` 的 CurveEndFrame。累计位置与 yaw 曲线 MUST在 StartFrame 到 CurveEndFrame 之间采样；CurveEndFrame 到 EndFrame 之间 MUST保持曲线终值，并按 Override/ConsumeLowerChannels 配置继续提交零 delta claim。缺失或非法 CurveEndFrame MUST作为配置错误，系统 MUST不按 EndFrame 猜测或兼容补齐。

#### Scenario: Corin Attack 曲线早于 Recovery 结束

- **WHEN** Attack1/Attack2 的位移曲线分别在 49/48 帧结束
- **AND** 动作 recovery 在 80 帧结束
- **THEN** 曲线 delta MUST保持原有 49/48 帧时序
- **AND** Action channel claim MUST持续到 80 帧
