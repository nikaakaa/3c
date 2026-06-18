## ADDED Requirements

### Requirement: 采样型播放恢复与首次进入分离
当基础移动状态声明使用 `TickSampledMotion`、root motion profile 或等价 animation-driven sampled motion 时，基础移动动画外观层 MUST 区分“首次进入或真实新播放”与“从 rollback snapshot 恢复后继续播放”。`RestorePlaybackProgress` 或等价恢复入口 MUST 将外观层 seek 到给定 phase/alias/normalized time，并建立后续同 alias `Present` 可识别的恢复播放段；同一播放段的后续 `Present` MUST NOT 执行 one-shot restart 或将 normalized time 重置为 start normalized time。纯表现动画 MAY 不执行该恢复流程。

#### Scenario: Restore 后同 alias 不归零
- **GIVEN** `TurnBack` 声明使用 sampled motion
- **AND** 外观层已通过 restore 恢复到 `TurnBack` alias 的 normalized time `0.35`
- **WHEN** 下一次 `Present` 收到相同 phase、gait 和 alias
- **THEN** 外观层 MUST 保持恢复后的播放段
- **AND** MUST NOT 将 normalized time 重置为 `0`
- **AND** MUST NOT 将该状态当作首次进入 TurnBack

#### Scenario: 真实新进入仍归零
- **GIVEN** 角色从非 TurnBack 状态真实进入 TurnBack
- **WHEN** 外观层播放 TurnBack alias
- **THEN** one-shot restart MAY 将 normalized time 设置为 policy start normalized time
- **AND** 该行为 MUST 不依赖 rollback debug runner 或 F6 特判

#### Scenario: 恢复入口不泄漏 Animancer 对象
- **WHEN** 逻辑层请求恢复基础移动播放进度
- **THEN** 请求 MUST 使用纯数据 playback progress
- **AND** 逻辑层 MUST NOT 读取或保存 `AnimancerState`、`AnimationClip`、`TransitionAsset` 或 Animator 引用

#### Scenario: 纯表现动画不强制恢复
- **GIVEN** 某基础移动动画只负责视觉 blend 或过渡表现
- **AND** 该动画播放进度未被 motion source 声明为 sampled motion 输入
- **WHEN** rollback restore 恢复角色 simulation 状态
- **THEN** 外观层 MAY 使用自身表现策略继续播放
- **AND** 该播放进度 MUST NOT 覆盖 sampled motion playback window、motion facts 或 runtime blackboard 权威事实

### Requirement: 基础移动外观层不覆盖 TickSampledMotion 权威
当基础移动状态声明使用 `TickSampledMotion` 或等价 profile-driven motion 时，外观层 MUST 只表现 simulation 提供的 sampled motion playback window。外观层 MAY 暴露只读播放进度给事实采样器，但不得在 rollback restore/replay 后用自身播放起点覆盖 simulation 的 playback progress 或 sampling window。

#### Scenario: Simulation 恢复进度后外观层跟随
- **GIVEN** simulation restore state 指定 phase、alias 和 normalized time
- **WHEN** 外观层恢复播放状态
- **THEN** 外观层 MUST seek 到该 normalized time
- **AND** 后续同 tick 的 animation facts MUST 与恢复进度一致

#### Scenario: 外观层不成为运动 source
- **GIVEN** 当前状态的位移或 yaw 来自 profile sampling
- **WHEN** 外观层播放对应视觉动画
- **THEN** 外观层 MUST NOT 通过 `OnAnimatorMove`、pending delta、Transform 写入或 motion executor 调用贡献 simulation movement facts
- **AND** profile sampling window MUST 来自 simulation playback state
