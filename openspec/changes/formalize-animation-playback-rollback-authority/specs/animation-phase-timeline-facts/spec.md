## ADDED Requirements

### Requirement: 可回滚动画播放时钟
系统 MUST 将会影响 simulation 输出的动画播放进度视为可回滚纯数据状态。对于 `TickSampledMotion` 或等价 profile-driven 状态，phase、alias key、current normalized time、previous sampled normalized time 和播放有效性 MUST 能通过 snapshot capture/restore 或确定性状态重建，且 MUST NOT 依赖 Animancer runtime object、Animator、AnimationClip、TransitionAsset、Unity frame time 或场景实例引用作为 replay 权威。

#### Scenario: Profile-driven 状态恢复播放时钟
- **GIVEN** 某状态的 profile delta 或 yaw 由动画 normalized window 采样
- **AND** replay 从该状态中段 tick 恢复
- **WHEN** 下一 tick 使用同一输入推进
- **THEN** sampler MUST 使用恢复后的 current normalized time 和 previous sampled normalized time
- **AND** MUST NOT 把恢复后的中段状态当作新播放段从 0 重新采样

#### Scenario: 首次进入仍从起始进度开始
- **GIVEN** 逻辑状态机真实进入一个新的 profile-driven 状态
- **WHEN** 该状态声明 start normalized time
- **THEN** 播放时钟 MUST 从该 start normalized time 开始
- **AND** previous sampled normalized time MUST 按新播放段规则初始化

#### Scenario: 表现层不是回滚权威
- **GIVEN** Animancer 或 Animator 当前视觉播放状态与 snapshot 中的播放时钟不同
- **WHEN** replay 恢复并推进需要 profile-driven motion 的状态
- **THEN** simulation MUST 以 snapshot/纯数据 playback state 作为采样权威
- **AND** 表现层 MAY seek 到该进度，但 MUST NOT 反向覆盖 simulation playback state

### Requirement: 播放窗口恢复诊断
系统 MUST 在本地 rollback 诊断中能定位动画播放时钟或 profile sampling window 分叉。字段级 differences 或相关诊断日志 MUST 能区分 current normalized time 差异、previous sampling window 差异、phase/alias 差异和最终 position/yaw 差异。

#### Scenario: Current normalized time 分叉
- **GIVEN** replay 后 current normalized time 与历史快照不同
- **WHEN** 本地 synctest 输出 first mismatch
- **THEN** differences MUST 标记 animation normalized time 或 runtime blackboard animation progress
- **AND** 日志 MUST 能看到 expected/actual 的 phase、alias 和 normalized time

#### Scenario: Sampling window 分叉
- **GIVEN** replay 的 current normalized time 相同但 previous sampled normalized time 不同
- **WHEN** profile delta 或 yaw 因窗口不同而分叉
- **THEN** 诊断 MUST 能输出或推导 previous/current window
- **AND** 不得只输出笼统的 snapshot mismatch
