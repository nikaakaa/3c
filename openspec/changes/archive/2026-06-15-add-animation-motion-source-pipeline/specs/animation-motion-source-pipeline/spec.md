## ADDED Requirements

### Requirement: 管线可选动画运动源
系统 MUST 提供通用的动画运动源管线，使逻辑状态可以通过纯数据策略选择是否让动画为本状态贡献 yaw 或 translation。第一版正式运动模式 MUST 为 `TickSampledMotion`，该策略能力 MUST 不依赖 TurnBack 专用分支，也 MUST 不携带 Animator、AnimancerState、Transform 或 CharacterController 引用。

#### Scenario: 状态选择动画运动源
- **GIVEN** 当前逻辑状态声明了动画运动源策略
- **WHEN** 状态机产出本 tick 的状态帧
- **THEN** 输出 MUST 携带该策略的纯数据描述
- **AND** 后续 locomotion/motion pipeline MUST 能根据该策略选择 yaw source、translation source 和 source id

#### Scenario: 状态未启用动画运动源
- **GIVEN** 当前逻辑状态未声明动画运动源策略
- **WHEN** pipeline 构建本 tick movement facts
- **THEN** 动画运动源 MUST 不贡献 yaw 或 translation
- **AND** 现有输入驱动或配置动作运动行为 MUST 保持不变

### Requirement: TickSampledMotion 对齐采样
系统 MUST 支持 `TickSampledMotion`，将动画运动源按 simulation tick 的播放进度窗口采样，并转换为 movement facts。需要客户端预测、回滚或预测矫正的状态 MUST 使用该模式作为动画运动权威来源。

#### Scenario: 采样本 tick 播放窗口
- **GIVEN** 当前动画播放进度从上一 tick normalized time 推进到当前 tick normalized time
- **WHEN** `TickSampledMotion` 动画运动源被采样
- **THEN** sampler MUST 使用该 normalized window 输出本 tick planar delta
- **AND** MUST 输出本 tick yaw delta

#### Scenario: 多 tick 不依赖 Unity Animator 回调次数
- **GIVEN** 某个 Unity frame 产生多个 simulation tick
- **WHEN** 每个 tick 都需要 `TickSampledMotion` 动画运动贡献
- **THEN** 每个 tick MUST 按各自播放窗口采样
- **AND** MUST NOT 要求 `OnAnimatorMove` 与 simulation tick 一一对应

#### Scenario: TurnBack 默认使用 TickSampledMotion
- **GIVEN** 当前状态为 `FullBody/Locomotion/TurnBack`
- **AND** TurnBack 需要兼容后续预测、回滚和预测矫正
- **WHEN** pipeline 解析 TurnBack 动画运动源策略
- **THEN** TurnBack 默认 MUST 使用 `TickSampledMotion`
- **AND** MUST NOT 默认消费 `OnAnimatorMove` pending runtime root delta

### Requirement: Rootmotion 母带派生资产
系统 MUST 支持从 Generic rootmotion 原动画派生 runtime motion profile 和 cleaned in-place visual clip。rootmotion 原动画 MUST 被视为运动母带；profile MUST 用于 `TickSampledMotion` 权威采样；cleaned in-place visual clip MUST 用于视觉播放。

#### Scenario: 从同一母带派生 profile 和视觉 clip
- **GIVEN** 设计者为 TurnBack 选择 Generic rootmotion 原动画作为母带
- **WHEN** 构建 `TickSampledMotion` 所需资产
- **THEN** 系统 MUST 能从该母带生成或绑定 runtime motion profile
- **AND** MUST 能从该母带生成或绑定 cleaned in-place visual clip

#### Scenario: 不双重应用 root motion
- **GIVEN** 某状态默认使用 `TickSampledMotion`
- **WHEN** 该状态播放视觉动画
- **THEN** 视觉动画 MUST NOT 再通过 Animator runtime root delta 推动角色根
- **AND** 角色根运动 MUST 来自 sampled profile 或等价 tick 对齐数据

### Requirement: 采样窗口重置规则
系统 MUST 在动画运动源的 phase、alias、profile 或播放进度不连续时重置采样窗口，防止上一段动画的累计位移污染新状态。

#### Scenario: phase 或 alias 变化
- **GIVEN** 上一 tick 采样的是旧 phase 或旧 alias
- **WHEN** 当前 tick 进入新 phase 或新 alias
- **THEN** sampler MUST 重置上一采样窗口
- **AND** MUST NOT 输出跨状态累计 delta

#### Scenario: 播放进度回退
- **GIVEN** 当前 tick 的 normalized time 小于上一采样 normalized time
- **WHEN** sampler 构建播放窗口
- **THEN** sampler MUST 将该情况视为新播放段
- **AND** MUST NOT 产生负向跨段 delta

### Requirement: Motion Window 控制
系统 MUST 允许状态 timeline 或等价事实控制动画运动源的应用窗口。motion window inactive 时不得继续消费 sampled motion 尾巴。

#### Scenario: motion window active
- **GIVEN** 当前状态声明了动画运动源策略
- **AND** timeline facts 表示 motion window active
- **WHEN** pipeline 构建 movement facts
- **THEN** 动画运动源 MAY 为本 tick 贡献 yaw 或 translation

#### Scenario: motion window inactive
- **GIVEN** 当前状态声明了动画运动源策略
- **AND** timeline facts 表示 motion window inactive
- **WHEN** pipeline 构建 movement facts
- **THEN** 动画运动源 MUST 输出无贡献或跳过应用
- **AND** MUST NOT 消费或应用尾部 delta

### Requirement: 运动执行权威保持
系统 MUST 保持角色根 yaw 和 translation 的运行时应用权威在正式 motion executor。动画外观层不能直接移动角色根，也不能把 Animator runtime delta 作为 pending movement facts 交给 simulation tick。

#### Scenario: 动画贡献进入 movement facts
- **GIVEN** `TickSampledMotion` 采样出本 tick yaw 或 translation
- **WHEN** pipeline 构建运动命令
- **THEN** 该贡献 MUST 进入 movement facts 或等价纯数据命令
- **AND** MUST 由正式 motion executor 应用到角色根

#### Scenario: 动画外观层不直接移动
- **WHEN** 动画外观层播放基础移动或 TurnBack 动画
- **THEN** 动画外观层 MUST NOT 直接调用 `CharacterController.Move`
- **AND** 动画外观层 MUST NOT 写入角色根 Transform

#### Scenario: 动画外观层不作为运动 source
- **GIVEN** 当前状态默认使用 `TickSampledMotion`
- **WHEN** locomotion pipeline 构建本 tick movement facts
- **THEN** pipeline MUST NOT 从 Presenter 当前 `AnimancerState` 或 `AnimationClip` 采样角色根运动
- **AND** pipeline MUST 使用配置绑定的 motion profile 或等价运行时可序列化 tick sampled 数据

### Requirement: 无静默 Fallback
系统 MUST 对动画运动源缺失或无效进行明确诊断，并不得静默 fallback 到未声明的 source、普通输入位移或直接 Transform 写入。

#### Scenario: 声明的 source 缺失
- **GIVEN** 当前状态声明了动画运动源策略
- **AND** 对应 profile、alias 或采样数据缺失
- **WHEN** pipeline 构建 movement facts
- **THEN** 系统 MUST 输出诊断日志或校验结果
- **AND** MUST NOT 自动改用其它动画运动源

#### Scenario: source 输出零贡献
- **GIVEN** 当前状态声明了动画运动源策略
- **AND** 采样窗口内 yaw 和 translation 均为零
- **WHEN** pipeline 构建 movement facts
- **THEN** 系统 MUST 输出无动画运动贡献
- **AND** MUST 保持该状态声明的输入抑制语义

### Requirement: Pending Runtime Root Delta 边界
系统 MUST 将 Unity `OnAnimatorMove` 的 runtime root delta 与 simulation tick sampled mode 区分开。runtime root delta MAY 用于诊断日志，但 MUST NOT 通过 source、pending buffer 或 rollback state 成为 simulation tick motion source。

#### Scenario: runtime delta 只用作诊断
- **WHEN** Animator 在 `OnAnimatorMove` 中产生 runtime root delta
- **THEN** 系统 MAY 输出诊断或对比日志
- **AND** 该日志 MUST NOT 直接改变 simulation tick 的 movement facts
- **AND** 该 delta MUST NOT 写入 rollback state

#### Scenario: 禁止 pending buffer 默认路径
- **GIVEN** 当前角色由 simulation tick 驱动
- **WHEN** pipeline 需要 simulation tick 内的动画运动贡献
- **THEN** pipeline MUST 使用 tick 对齐采样数据
- **AND** MUST NOT 依赖 `OnAnimatorMove` pending buffer 与 tick 消费次数一致
