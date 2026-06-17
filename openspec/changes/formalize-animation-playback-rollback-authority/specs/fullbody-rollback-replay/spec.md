## ADDED Requirements

### Requirement: Profile 播放窗口回滚收敛
FullBody rollback replay MUST 保证 profile-driven 动画状态从历史中段恢复后，播放时钟和 profile sampling window 与原始运行逐 tick 收敛。恢复同一 tick 后，replay MUST 不把 active state 的动画当作新进入重新归零；最终 snapshot 比较 MUST 覆盖 animation progress、runtime blackboard animation facts、motion executor root pose、position 和 yaw。

#### Scenario: TurnBack 中段恢复不重启动画
- **GIVEN** 原始运行在 tick N 处处于 `Locomotion.TurnBack`
- **AND** tick N 的 locomotion animation normalized time 大于 `0`
- **WHEN** F6 或等价 synctest 从 tick N 恢复并推进 tick N+1
- **THEN** replay 的 animation normalized time MUST 从 tick N 的历史进度继续推进
- **AND** MUST NOT 在 tick N+1 变为 `0` 或 policy start normalized time

#### Scenario: TurnBack 采样窗口一致
- **GIVEN** TurnBack 的 translation 或 yaw 来自 baked profile
- **AND** tick N 的 snapshot 包含 previous motion playback progress
- **WHEN** replay 从 tick N 恢复并推进 tick N+1
- **THEN** profile sampler MUST 使用与原始运行相同的 previous/current normalized window
- **AND** sampled planar delta 和 yaw delta MUST 在测试容差内一致

#### Scenario: Restore 阶段和 Replay 阶段都比较动画事实
- **WHEN** 本地 synctest 从历史 tick 恢复并重放
- **THEN** restore 后捕获的 snapshot MUST 与恢复目标比较 animation progress 和 runtime blackboard animation facts
- **AND** 每个 replay tick 的 first mismatch MUST 能报告 animation progress 分叉，而不是只在 end tick 报告 position/yaw 分叉

### Requirement: 回滚修复不得产生分裂路径
实现 profile playback rollback 收敛时，系统 MUST 继续通过 `FullBodyRollbackSimulation` 或批准的 rollback adapter、`CharacterFrameRuntimeController`、`CharacterRuntimeCore`、runtime blackboard 和正式 motion executor 主线推进。实现 MUST NOT 为 TurnBack 创建 F6 专用重放路径、Presenter 专用运动路径、直接 sampler 测试通道或 Animator root delta fallback。

#### Scenario: Replay 走角色帧主线
- **WHEN** 自动测试验证 TurnBack 中段恢复
- **THEN** 测试 MUST 通过 `FullBodyRollbackSimulation.Advance` 或等价 rollback adapter 推进正式角色帧 runtime
- **AND** MUST NOT 直接调用底层 sampler 或直接写 Transform 制造收敛

#### Scenario: 无 F6 特判
- **WHEN** 实现区分首次进入和 restore resume
- **THEN** 逻辑 MUST 基于正式 playback state、state transition 或 restore state
- **AND** MUST NOT 检查 F6 key、debug runner 名称、synctest flag 或 Unity Console 状态来改变运行时语义
