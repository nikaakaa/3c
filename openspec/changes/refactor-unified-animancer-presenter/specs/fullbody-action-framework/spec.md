## ADDED Requirements
### Requirement: 统一 Animancer 表现入口
系统 MUST 为当前角色 FullBody base layer 提供单一正式 Animancer 表现入口。Locomotion 和 FullBody Action 的动画播放请求 MUST 进入同一个 Presenter 或等价播放端口；系统 MUST NOT 在同一角色正式运行时中让 `BasicLocomotionAnimancerPresenter` 与 `ActionAnimationAnimancerPresenter` 两个正式播放组件并存作为双播放权威。

#### Scenario: Locomotion 和 Action 共用播放入口
- **WHEN** Character frame pipeline 进入 PresentationBridge 或等价输出阶段
- **THEN** Locomotion 动画上下文 MUST 被转换为统一动画播放请求
- **AND** Action 动画请求 MUST 被转换为统一动画播放请求
- **AND** 二者 MUST 提交给同一个正式 Animancer Presenter 或等价播放端口

#### Scenario: 不保留两个正式播放组件
- **WHEN** 检查当前角色 prefab 或 Sandbox 场景装配
- **THEN** 同一个角色 MUST NOT 同时挂载两个正式 Animancer 播放组件分别处理 Locomotion 和 Action
- **AND** 如果旧 Presenter 类型仍存在，它们 MUST 只能作为迁移桥、测试辅助或废弃兼容层
- **AND** 旧 Presenter 类型 MUST NOT 分别持有正式当前播放状态

#### Scenario: 单一进度事实来源
- **WHEN** Character output applier 写入动画 runtime facts
- **THEN** Locomotion playback progress 和 Action playback progress MUST 来自同一个 Presenter 的只读播放快照或等价统一事实源
- **AND** 该快照 MUST NOT 暴露 Animancer runtime 对象
- **AND** 该快照 MUST NOT 决定业务状态切换

#### Scenario: Presenter 不成为状态或位移权威
- **WHEN** 统一 Presenter 播放任意 Locomotion 或 Action 动画
- **THEN** Presenter MUST NOT 调用状态机切换 API
- **AND** MUST NOT 调用 `CharacterController.Move`
- **AND** MUST NOT 写入角色 Transform
- **AND** MUST NOT 读取 Action 打断策略集合
