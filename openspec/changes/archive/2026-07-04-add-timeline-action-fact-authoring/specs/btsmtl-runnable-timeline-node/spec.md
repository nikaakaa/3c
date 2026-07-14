## ADDED Requirements

### Requirement: Timeline 动作事实必须来自 Timeline 轨道采样

系统 MUST 支持 Timeline 资产通过正式轨道产出动作时间事实。第一阶段至少 MUST 支持 `ActionWindowTrack` 和 `ActionCueTrack`。这些轨道采样出的事实 MAY 通过 TimelineNode 的 Action Context 关联到 ActionInstance。Timeline 轨道 MUST NOT 保存完整网络策略。

#### Scenario: Attack1 产出 HitWindow

- **WHEN** `Attack1` 状态播放带 Action Context 的攻击 Timeline
- **THEN** Timeline 中的 Hit window clip MUST 采样为 ActionWindow sample
- **AND** sample MUST 携带 Action Context 对应的 ActionInstanceId
- **AND** window authority、history 和 replication policy MUST 从 ActionProfile 解析

#### Scenario: 普通 locomotion Timeline

- **WHEN** `RunLoop` 状态播放不带 Action Context 的 locomotion Timeline
- **THEN** Timeline MAY 产出 animation contribution 或 motion contribution
- **AND** Timeline MUST NOT 自动创建 ActionInstance
- **AND** ActionWindowTrack 或 ActionCueTrack 缺少 Action Context 时 MUST NOT 伪造动作归属

### Requirement: TimelineNode 完成状态必须保持请求语义

系统 MUST 保持 `TimelineNode` 通过正式 Timeline playback request 获取播放状态，并在播放成功时返回 `Success`。TimelineNode MUST NOT 直接驱动 StateMachine transition，也 MUST NOT 在自身内部解释 action lifecycle。

#### Scenario: Timeline 播放完成

- **WHEN** Timeline playback request 返回 `Succeeded`
- **THEN** TimelineNode MUST 返回 `Success`
- **AND** 状态机 transition 是否发生 MUST 由 StateMachine transition rule 决定

#### Scenario: Timeline 被取消

- **WHEN** 状态离开导致 TimelineNode 被 stop 或 reset
- **THEN** TimelineNode MUST 通过正式 playback request 取消未完成 Timeline
- **AND** TimelineNode MUST NOT 提交动作完成 lifecycle transition
