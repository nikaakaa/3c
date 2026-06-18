## ADDED Requirements

### Requirement: 动画驱动采样窗口可选择进入回滚权威
系统 MAY 让纯表现动画播放进度保持 Presentation Layer 非确定状态，不要求捕获 normalized time。系统 MUST 将会影响 simulation 输出的动画驱动采样窗口视为可回滚纯数据状态。对于声明使用 `TickSampledMotion`、root motion profile、Motion Warping playback window 或等价 profile-driven 输出的状态/动作，phase、alias key、current normalized time、previous sampled normalized time 和采样有效性 MUST 能通过 snapshot capture/restore 或确定性状态重建，且 MUST NOT 依赖 Animancer runtime object、Animator、AnimationClip、TransitionAsset、Unity frame time 或场景实例引用作为 replay 权威。

#### Scenario: Profile-driven 状态恢复采样窗口
- **GIVEN** 某状态声明使用 profile-driven motion
- **AND** 该状态的 profile delta 或 yaw 由动画 normalized window 采样
- **AND** replay 从该状态中段 tick 恢复
- **WHEN** 下一 tick 使用同一输入推进
- **THEN** sampler MUST 使用恢复后的 current normalized time 和 previous sampled normalized time
- **AND** MUST NOT 把恢复后的中段状态当作新播放段从 0 重新采样

#### Scenario: 首次进入仍从起始进度开始
- **GIVEN** 逻辑状态机真实进入一个新的 profile-driven 状态
- **WHEN** 该状态声明 start normalized time
- **THEN** 采样播放窗口 MUST 从该 start normalized time 开始
- **AND** previous sampled normalized time MUST 按新播放段规则初始化

#### Scenario: 表现层不是回滚权威
- **GIVEN** Animancer 或 Animator 当前视觉播放状态与 snapshot 中的 sampled motion playback window 不同
- **WHEN** replay 恢复并推进需要 profile-driven motion 的状态
- **THEN** simulation MUST 以 snapshot/纯数据 playback state 作为采样权威
- **AND** 表现层 MAY seek 到该进度，但 MUST NOT 反向覆盖 simulation playback state

#### Scenario: 纯表现动画不要求回滚权威
- **GIVEN** 某动画只用于视觉播放、blend、表情、上身表现或 VFX 节奏
- **AND** 该动画播放进度未被声明为 motion facts、warp window、hit/cancel window 或等价 simulation 输出的输入
- **WHEN** rollback replay 恢复角色状态
- **THEN** 系统 MAY 不捕获该动画 normalized time
- **AND** 该动画播放进度 MUST NOT 反向影响 simulation snapshot、motion facts 或 runtime blackboard 权威事实

### Requirement: 采样窗口恢复诊断
系统 MUST 在本地 rollback 诊断中能定位 sampled motion playback window 或 profile sampling window 分叉。字段级 differences 或相关诊断日志 MUST 能区分 current normalized time 差异、previous sampling window 差异、phase/alias 差异和最终 position/yaw 差异。

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
