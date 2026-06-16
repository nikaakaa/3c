## ADDED Requirements

### Requirement: Locomotion 动画运动源 Tick 对齐
系统 MUST 在 simulation tick 内对 `TickSampledMotion` Locomotion 动画运动源进行确定性采样，使 sampled 动画运动贡献与 tick delta、播放窗口和状态 timeline 对齐。

#### Scenario: 每个 tick 独立采样
- **GIVEN** Locomotion 状态声明了 `TickSampledMotion` 动画运动源策略
- **WHEN** `UnitySimulationTickDriver` 产生 tick N
- **THEN** Locomotion pipeline MUST 使用 tick N 的播放进度窗口采样动画运动贡献
- **AND** 该贡献 MUST 只影响 tick N 的 movement facts

#### Scenario: 多 tick 同帧
- **GIVEN** 一个 Unity frame 中 accumulator 产生多个 simulation tick
- **WHEN** Locomotion pipeline 连续处理这些 tick
- **THEN** 每个 tick MUST 使用连续且不重叠的动画播放窗口
- **AND** MUST NOT 多次复用同一份 Unity frame runtime root delta

#### Scenario: 不足一个 tick
- **GIVEN** 当前 Unity frame 不足以产生 simulation tick
- **WHEN** Animator 或表现层仍被 Unity 更新
- **THEN** Locomotion pipeline MUST NOT 因表现层更新而提交新的 simulation movement facts
- **AND** 下一次 tick MUST 按 simulation 播放窗口采样 `TickSampledMotion` 动画运动源

#### Scenario: TurnBack 使用 sampled 权威运动
- **GIVEN** 当前 Locomotion 状态为 TurnBack
- **AND** TurnBack 策略选择 `TickSampledMotion`
- **WHEN** simulation tick 构建本 tick movement facts
- **THEN** pipeline MUST 从 TurnBack motion profile 或等价 tick 对齐数据采样 yaw 和 translation
- **AND** MUST 由统一 motion executor 应用该 yaw 和 translation
- **AND** MUST NOT 从 `OnAnimatorMove` pending buffer 消费 runtime root delta
