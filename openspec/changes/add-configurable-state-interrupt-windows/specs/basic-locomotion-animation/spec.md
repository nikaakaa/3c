## ADDED Requirements
### Requirement: TurnBack 使用状态 Timeline 和烘焙 Motion
TurnBack MUST 使用状态 timeline policy 表达运动窗口、输入锁和退出窗口。TurnBack 的可见动画 SHOULD 使用 inplace 动画资源，角色根 yaw 和 translation MUST 来自配置的 baked motion profile 并通过统一运动输出执行，不得由 Animator Root Motion 直接写角色根。

#### Scenario: RunLoop 才能触发 TurnBack
- **GIVEN** 当前状态为 `FullBody/Locomotion/MoveLoop`
- **AND** 当前 gait 为 Run
- **AND** 输入方向与角色朝向满足反向阈值
- **WHEN** 状态请求仲裁接受 TurnBack 请求
- **THEN** 系统 MUST 进入 `FullBody/Locomotion/TurnBack`

#### Scenario: 非 RunLoop 不触发 TurnBack
- **GIVEN** 当前状态为 Idle、MoveStart、MoveStop 或 Walk MoveLoop
- **WHEN** 玩家输入反向移动
- **THEN** 系统 MUST NOT 进入 TurnBack
- **AND** MUST NOT 播放 TurnBack 动画

#### Scenario: baked motion 贡献位移和 yaw
- **GIVEN** 当前状态为 TurnBack
- **AND** 当前 timeline facts 表示 motion window active
- **AND** 配置存在匹配 baked motion profile
- **WHEN** 系统采样运动 facts
- **THEN** 本帧 yaw delta MUST 来自 baked motion profile
- **AND** 本帧 translation delta MUST 来自 baked motion profile
- **AND** 普通输入旋转和平面位移 MUST 被抑制

#### Scenario: motion window 结束后恢复普通移动
- **GIVEN** 当前 TurnBack 已进入 exit window
- **WHEN** 玩家持续按住目标方向
- **THEN** 系统 MUST 退出 TurnBack 并恢复普通 MoveLoop 输入位移和旋转
- **AND** MUST NOT 继续消费 TurnBack baked motion tail

### Requirement: TurnBack 动画资源和 Motion Profile 分离
TurnBack 的视觉动画绑定 MUST 与 baked motion profile 分离。Animancer TransitionAsset 或 alias 负责播放表现；baked motion profile 负责角色根 yaw/translation 数据。两者 MUST 通过正式配置关联和校验，不能依靠 rootmotion 动画 clip 本身作为运行时位移权威。

#### Scenario: inplace 动画绑定
- **WHEN** 设计者检查 Corin Generic TurnBack 动画配置
- **THEN** TurnBack alias SHOULD 指向 inplace 动画或等价不写根平面位移的表现资源
- **AND** 位移和 yaw 权威 MUST 来自 baked motion profile

#### Scenario: rootmotion 动画不直接驱动角色根
- **GIVEN** TurnBack TransitionAsset 仍误指向带 RootT/RootQ 的 rootmotion clip
- **WHEN** 系统运行配置校验
- **THEN** 校验 MUST 报告风险或错误
- **AND** runtime MUST NOT 因该 clip 直接由 Animator Root Motion 写角色根

#### Scenario: 视觉混合不改变 TurnBack 运动窗口
- **GIVEN** TurnBack 使用同一个 baked motion profile 和 timeline policy
- **AND** 动画表现配置的 fade 从 `0.08` 改为 `0.25`
- **WHEN** 系统在相同状态时间采样 TurnBack 运动输出
- **THEN** motion window、exit window 和本帧 baked motion delta MUST 保持一致

#### Scenario: profile 缺失可诊断
- **GIVEN** TurnBack timeline policy 启用 baked motion
- **AND** baked motion profile 缺失或 profile id 不匹配
- **WHEN** 玩家触发 TurnBack
- **THEN** 系统 MUST 输出配置诊断
- **AND** MUST NOT 静默退回普通输入位移或 Animator Root Motion 位移
