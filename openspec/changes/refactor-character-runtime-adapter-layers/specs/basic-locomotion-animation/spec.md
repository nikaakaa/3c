## ADDED Requirements
### Requirement: 基础移动 Animancer Presenter 分层
系统 MUST 保持 `BasicLocomotionAnimancerPresenter` 作为 Animancer 播放 Runtime Adapter。Presenter MAY 调用 alias resolver、playback progress helper 和 diagnostics Module，但这些 Module MUST 不接管逻辑状态、运动执行或 rollback playback authority。

#### Scenario: Presenter 只消费动画请求
- **WHEN** 基础移动运行时提交 `MovementAnimationContext` 或等价动画请求
- **THEN** Presenter MUST 负责把请求转换为 Animancer 播放调用
- **AND** Presenter MUST NOT 调用状态机 transition API
- **AND** Presenter MUST NOT 调用 motion executor 或写角色 Transform

#### Scenario: Alias resolver 不调用 Animancer
- **WHEN** 基础移动动画 alias 解析逻辑被拆到 Solver
- **THEN** resolver MUST 只使用 phase、gait、alias 配置和纯数据上下文解析 alias key
- **AND** resolver MUST NOT 调用 Animancer play API
- **AND** resolver MUST NOT 读取 Animator runtime state 或 `AnimationClip`

#### Scenario: Playback restore 权威不在本变更中重写
- **WHEN** Presenter 拆分涉及 playback progress 或 restore helper
- **THEN** 实现 MUST 遵守 `formalize-animation-playback-rollback-authority` 定义的 current/previous playback window 语义
- **AND** 本变更 MUST NOT 把 restore 后同 alias 播放重新归零
- **AND** 本变更 MUST NOT 将 Animator runtime delta 恢复为正式 movement facts source

#### Scenario: Presenter diagnostics 不改变行为
- **WHEN** 动画播放、alias、root motion probe 或 playback progress 日志迁移到 Diagnostics Module
- **THEN** 日志 MUST 只记录只读事实
- **AND** 日志开关 MUST NOT 改变动画播放、状态机切换、motion facts 或 rollback state

### Requirement: Motion Executor 拆分不改变位移权威
系统 MUST 允许 `CharacterControllerBasicMotionExecutor` 将平面 delta、动画运动贡献合成和诊断格式拆到 Solver/Diagnostics，但基础移动的正式 Unity 位移执行仍归属 motion executor Runtime Adapter。

#### Scenario: Solver 只输出运动结果
- **WHEN** animation motion facts 或 input movement command 被 Solver 处理
- **THEN** Solver MUST 输出纯数据 delta、yaw 或 suppression 结果
- **AND** Solver MUST NOT 调用 `CharacterController.Move`
- **AND** Solver MUST NOT 写角色 Transform

#### Scenario: Motion executor 仍是正式 Move 调用点
- **WHEN** 角色执行基础移动根运动
- **THEN** `CharacterControllerBasicMotionExecutor` 或等价正式 motion Runtime Adapter MUST 是调用 `CharacterController.Move` 的位置
- **AND** Animation Presenter、FullBody pipeline helper 和 Locomotion Solver MUST NOT 直接移动角色根

#### Scenario: Rollback state helper 不抢权威
- **WHEN** motion executor rollback state 逻辑被拆分
- **THEN** helper MUST 保持纯数据 capture/restore 协作
- **AND** helper MUST NOT 自行推进 simulation tick
- **AND** helper MUST NOT 创建第二套 movement history
