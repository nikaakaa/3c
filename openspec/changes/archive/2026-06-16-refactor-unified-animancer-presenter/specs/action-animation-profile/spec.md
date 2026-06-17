## MODIFIED Requirements
### Requirement: 动作动画 Presenter 边界
系统 MUST 通过统一 FullBody Animancer 表现入口消费动作动画命令并播放动画。该表现入口 MUST 只负责动画表现和只读播放进度，不得决定动作是否允许、不得切换业务状态、不得执行位移。

#### Scenario: Presenter 播放 Profile 动画
- **GIVEN** 动作动画命令包含 `Action.Dodge.Directional`
- **AND** Profile 能解析该 key
- **WHEN** 统一 Animancer 表现入口接收该命令
- **THEN** 表现入口 MUST 播放 Profile 中对应动画
- **AND** 表现入口 MUST 暴露当前 key 和播放进度作为只读调试信息

#### Scenario: Presenter 不做业务仲裁
- **WHEN** 统一 Animancer 表现入口接收动作动画命令
- **THEN** 表现入口 MUST NOT 调用 `ActionInterruptArbiter`
- **AND** MUST NOT 消费 `InputRequestBuffer`
- **AND** MUST NOT 决定 Dodge 是否允许进入

#### Scenario: Presenter 不执行位移
- **WHEN** 统一 Animancer 表现入口播放动作动画
- **THEN** 表现入口 MUST NOT 调用 `CharacterController.Move`
- **AND** MUST NOT 写入角色 Transform
- **AND** MUST NOT 成为 Dodge 运动事实来源

#### Scenario: 不保留动作专用正式播放组件
- **WHEN** 当前角色已经接入统一 Animancer 表现入口
- **THEN** 系统 MUST NOT 要求再挂载独立 `ActionAnimationAnimancerPresenter` 才能播放 `Action.Dodge.Directional` 或 `Action.Dodge.Backstep`
- **AND** 动作播放进度 MUST 来自统一表现入口的只读快照

### Requirement: 与基础移动动画配置分离
系统 MUST 保持动作动画 Profile 与现有基础移动 Walk/Run alias 配置分离。动作动画 Profile 不得替代 `RunLocomotionAnimationConfigSO` 的基础移动职责，统一 Animancer 表现入口也不得通过动作 Profile 决定 Locomotion phase 播放。

#### Scenario: 基础移动仍使用基础移动配置
- **WHEN** 统一 Animancer 表现入口播放 Idle、WalkStart、WalkLoop、WalkEnd、RunStart、RunLoop 或 RunEnd
- **THEN** 它 MUST 继续使用基础移动动画配置或等价基础移动 alias 解析
- **AND** MUST NOT 要求存在动作动画 Profile

#### Scenario: 动作动画 Profile 不接管 Locomotion
- **WHEN** 动作动画 Profile 配置 Shift FullBody 动画
- **THEN** Profile MUST NOT 定义 `Idle / MoveStart / MoveLoop / MoveStop` 状态图规则
- **AND** MUST NOT 决定 `MoveStop -> MoveStart` 或 `MoveStop -> Idle`

#### Scenario: 统一入口不合并配置归属
- **WHEN** 统一 Animancer 表现入口同时支持 Locomotion 和 Action 播放
- **THEN** Locomotion alias、退出策略和 motion profile MUST 仍归基础移动动画配置
- **AND** Action key 到动画表现资源的映射 MUST 仍归动作动画 Profile 或等价动作动画绑定入口

### Requirement: 动作动画 Profile 可测试和可验证
系统 MUST 提供自动测试和验证，证明动作动画 Profile 可配置、可校验、可替换，并且不会污染 Locomotion 和运动权威边界。

#### Scenario: 自动测试覆盖 Profile 行为
- **WHEN** 运行动作动画 Profile EditMode 测试
- **THEN** 测试 MUST 覆盖 key 解析、空 key、重复 key、缺失动画引用、Directional/Backstep 两个 key 和替换动画引用

#### Scenario: 静态边界验证
- **WHEN** 检查动作动画 Profile 和统一 Animancer 表现入口源码
- **THEN** 静态搜索 MUST 能确认它们不引用 `BBBNexus` 命名空间
- **AND** 表现入口源码 MUST 不直接调用 `CharacterController.Move`
- **AND** 当前角色正式 prefab/scene MUST NOT 同时挂载动作专用和基础移动专用两个正式 Animancer Presenter

#### Scenario: 替换动画验证
- **WHEN** 用户替换 Profile 中 `Action.Dodge.Directional` 或 `Action.Dodge.Backstep` 的动画引用
- **THEN** Play Mode 中对应动作表现 MUST 使用替换后的动画
- **AND** 动作方向、输入消费、Run latch 和基础移动恢复规则 MUST 不需要修改代码
