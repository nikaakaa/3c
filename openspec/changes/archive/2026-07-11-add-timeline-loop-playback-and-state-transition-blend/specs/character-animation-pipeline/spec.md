## ADDED Requirements

### Requirement: TimelinePlaybackScheduler 必须支持回绕稳定的循环播放

`TimelinePlaybackScheduler` MUST 支持由 `TimelineNode` 请求的 `Loop` 播放模式。循环 request MUST 使用同一个 active record 持续推进，播放相位到达 Timeline duration 后回绕到开头，request handle、source identity 和 request 状态 MUST 保持稳定。

#### Scenario: 循环 request 到达 duration

- **WHEN** `TimelinePlaybackScheduler` 推进 `Loop` request
- **AND** 当前播放时间越过 Timeline duration
- **THEN** scheduler MUST 回绕 Timeline 本地播放相位
- **AND** request 状态 MUST 保持 `Running`
- **AND** scheduler MUST NOT 将该 request 标记为 `Succeeded`

#### Scenario: 循环 request 被取消

- **WHEN** 持有 request 的 `TimelineNode` 被 stop 或 reset
- **THEN** scheduler MUST 取消对应 active record
- **AND** 后续 tick MUST NOT 再从该 record 采样动画、motion、window 或 cue

### Requirement: Timeline 回绕采样必须覆盖边界两侧

循环 Timeline 的轨道采样 MUST 能表达本帧采样区间是否跨过 Timeline duration 边界。跨边界时，scheduler MUST 将采样拆成尾段和头段，确保边界附近的动画、motion、window、cue 和其它轨道事实不丢失、不重复。

#### Scenario: 动画采样跨过循环边界

- **WHEN** 上一采样相位位于 Timeline 末尾
- **AND** 当前采样相位回到 Timeline 开头
- **THEN** scheduler MUST 采样末尾区间和开头区间
- **AND** 输出动画贡献 MUST 表达这是同一个循环 request 的连续播放

#### Scenario: 动作窗口跨过循环边界

- **WHEN** loop Timeline 中存在 action window 或 cue 轨道
- **AND** 本帧采样跨过 duration 边界
- **THEN** scheduler MUST 按正式轨道语义采样尾段和头段
- **AND** 同一边界上的 window 或 cue MUST NOT 重复输出
- **AND** 缺少 Action Context 时 MUST NOT 伪造动作归属

#### Scenario: Motion 曲线跨过循环边界

- **WHEN** loop Timeline 中存在 motion 曲线或等价 motion 轨道
- **AND** 本帧采样跨过 duration 边界
- **THEN** scheduler MUST 按尾段 delta 加头段 delta 计算 motion 输出
- **AND** motion 输出 MUST 继续进入正式 motion pipeline

### Requirement: 状态切换动画混合必须由表现层消费正式切换事实

系统 MUST 通过正式 pipeline 输出表达状态切换动画混合事实，并由 `CharacterPresentationStage` 消费。状态机 runtime 和 Timeline scheduler MUST NOT 直接写 Animator、Animancer 或 PlayableGraph；Animancer adapter MUST NOT 自行决定状态切换、blend 时长或 blend 曲线。

#### Scenario: 状态机发生带 blend 的切换

- **WHEN** `StateMachineGraphRuntime` 命中带动画混合元数据的 Transition edge
- **THEN** runtime MUST 将切换源、目标和混合元数据写入正式 pipeline presentation 输出
- **AND** `CharacterPresentationStage` MUST 使用该输出创建表现层 blend
- **AND** Animancer adapter MUST 只应用最终动画层播放计划

#### Scenario: 状态切换没有 blend 时长

- **WHEN** 命中的 Transition edge 动画混合时长为 0
- **THEN** 表现层 MUST 允许即时切换到新 active state 的动画计划
- **AND** 系统 MUST NOT 通过隐藏 CrossFade 或 fallback clip 伪造混合
