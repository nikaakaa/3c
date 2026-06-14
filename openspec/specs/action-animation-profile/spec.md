# action-animation-profile Specification

## Purpose
定义 FullBody 动作动画 Profile 的稳定 key、角色级替换边界、Locomotion 配置分离规则和动作表现校验要求。
## Requirements
### Requirement: 动作动画 Profile 数据源
系统 MUST 提供动作动画 Profile 数据源，用于把稳定 action animation key 映射到具体动画表现。动作逻辑 MUST 只输出 action animation key，不得写死具体角色 clip、可琳动画名或 BBB 运行时资源路径。

#### Scenario: Profile 保存动作动画条目
- **WHEN** 设计者配置动作动画 Profile
- **THEN** Profile entry MUST 能保存 action animation key
- **AND** MUST 能保存具体动画引用或等价 Animancer transition 引用
- **AND** MAY 保存 fade 参数和调试名

#### Scenario: Profile 不是 FullBody 状态机
- **WHEN** 动作动画 Profile 配置 `Action.Dodge.Directional` 或 `Action.Dodge.Backstep`
- **THEN** Profile MUST 只表达动作语义 key 到动画表现资源的映射
- **AND** Profile MAY 使用直接 clip、Animancer transition 或等价 transition asset
- **AND** Profile MUST NOT 替代 FullBody 主行为域中的状态注册、进入条件、退出条件或运动权威

#### Scenario: Profile 通过动画绑定入口接入
- **WHEN** 系统提供 FullBody Action 动画绑定集或等价动画配置入口
- **THEN** 动作动画 Profile MAY 作为该绑定入口的子配置或引用存在
- **AND** 设计者 SHOULD 能通过 FullBody 主调度入口追踪到 Directional 和 Backstep 的动画表现资源
- **AND** 动作动画 Profile MUST NOT 被要求成为和动作逻辑入口、动画绑定入口无绑定关系的游离配置

#### Scenario: 动作逻辑不写死 clip
- **WHEN** Shift FullBody 动作请求动画表现
- **THEN** 动作逻辑 MUST 输出 `Action.Dodge.Directional` 或 `Action.Dodge.Backstep` key
- **AND** 动作逻辑 MUST NOT 直接引用具体 `AnimationClip`
- **AND** 动作逻辑 MUST NOT 直接引用具体角色动画资源名

#### Scenario: 角色可替换动画套件
- **GIVEN** 同一个 Shift FullBody 动作逻辑
- **WHEN** 设计者替换动作动画 Profile 中的 Directional 或 Backstep 动画引用
- **THEN** 系统 MUST 使用新的动画表现
- **AND** 不需要修改动作逻辑代码

### Requirement: Shift FullBody 动画 Key
系统 MUST 为 Shift FullBody 动作第一版提供两个稳定动作动画 key：方向冲刺和后闪。key MUST 表达动作语义，而不是表达具体角色、clip 文件名或导入来源。

#### Scenario: 方向冲刺 key
- **WHEN** 动作变体为 `Directional`
- **THEN** 动作动画 key MUST 为 `Action.Dodge.Directional` 或等价稳定 ID
- **AND** 该 key MUST 可由动作动画 Profile 解析

#### Scenario: 后闪 key
- **WHEN** 动作变体为 `Backstep`
- **THEN** 动作动画 key MUST 为 `Action.Dodge.Backstep` 或等价稳定 ID
- **AND** 该 key MUST 可由动作动画 Profile 解析

#### Scenario: key 不绑定可琳
- **WHEN** 系统定义动作动画 key
- **THEN** key MUST NOT 包含可琳、Corin、具体 fbx、具体 clip 文件名或 BBB 路径

### Requirement: 动作动画 Profile 校验
系统 MUST 对动作动画 Profile 提供可测试的校验，帮助设计者发现空 key、重复 key 和缺失动画引用。校验 MUST 不接管动作逻辑、状态仲裁或运动执行。

#### Scenario: 空 key 报错
- **GIVEN** Profile 中存在空 action animation key
- **WHEN** 运行 Profile 校验
- **THEN** 校验结果 MUST 包含错误

#### Scenario: 重复 key 报告错误或 warning
- **GIVEN** Profile 中存在重复 action animation key
- **WHEN** 运行 Profile 校验
- **THEN** 校验结果 MUST 报告重复 key
- **AND** 重复项 MUST NOT 被静默忽略

#### Scenario: 缺失动画引用报错
- **GIVEN** Profile entry 没有可播放动画引用
- **WHEN** 运行 Profile 校验
- **THEN** 校验结果 MUST 包含错误

### Requirement: 动作动画 Presenter 边界
系统 MUST 提供或扩展动作动画 Presenter，使其消费动作动画命令并通过 Profile 播放动画。Presenter MUST 只负责动画表现和只读播放进度，不得决定动作是否允许、不得切换业务状态、不得执行位移。

#### Scenario: Presenter 播放 Profile 动画
- **GIVEN** 动作动画命令包含 `Action.Dodge.Directional`
- **AND** Profile 能解析该 key
- **WHEN** Presenter 接收该命令
- **THEN** Presenter MUST 播放 Profile 中对应动画
- **AND** Presenter MUST 暴露当前 key 和播放进度作为只读调试信息

#### Scenario: Presenter 不做业务仲裁
- **WHEN** Presenter 接收动作动画命令
- **THEN** Presenter MUST NOT 调用 `ActionInterruptArbiter`
- **AND** MUST NOT 消费 `InputRequestBuffer`
- **AND** MUST NOT 决定 Dodge 是否允许进入

#### Scenario: Presenter 不执行位移
- **WHEN** Presenter 播放动作动画
- **THEN** Presenter MUST NOT 调用 `CharacterController.Move`
- **AND** MUST NOT 写入角色 Transform
- **AND** MUST NOT 成为 Dodge 运动事实来源

### Requirement: 与基础移动动画配置分离
系统 MUST 保持动作动画 Profile 与现有基础移动 Walk/Run alias 配置分离。动作动画 Profile 不得替代 `RunLocomotionAnimationConfigSO` 的基础移动职责，基础移动 Presenter 也不得通过动作 Profile 决定 Locomotion phase 播放。

#### Scenario: 基础移动仍使用基础移动配置
- **WHEN** 基础移动动画 Presenter 播放 Idle、WalkStart、WalkLoop、WalkEnd、RunStart、RunLoop 或 RunEnd
- **THEN** 它 MUST 继续使用基础移动动画配置或等价基础移动 alias 解析
- **AND** MUST NOT 要求存在动作动画 Profile

#### Scenario: 动作动画 Profile 不接管 Locomotion
- **WHEN** 动作动画 Profile 配置 Shift FullBody 动画
- **THEN** Profile MUST NOT 定义 `Idle / MoveStart / MoveLoop / MoveStop` 状态图规则
- **AND** MUST NOT 决定 `MoveStop -> MoveStart` 或 `MoveStop -> Idle`

### Requirement: 动作动画 Profile 可测试和可验证
系统 MUST 提供自动测试和手动验证，证明动作动画 Profile 可配置、可校验、可替换，并且不会污染 Locomotion 和运动权威边界。

#### Scenario: 自动测试覆盖 Profile 行为
- **WHEN** 运行动作动画 Profile EditMode 测试
- **THEN** 测试 MUST 覆盖 key 解析、空 key、重复 key、缺失动画引用、Directional/Backstep 两个 key 和替换动画引用

#### Scenario: 静态边界验证
- **WHEN** 检查动作动画 Profile 和 Presenter 源码
- **THEN** 静态搜索 MUST 能确认它们不引用 `BBBNexus` 命名空间
- **AND** Presenter 源码 MUST 不直接调用 `CharacterController.Move`

#### Scenario: 手动替换动画验证
- **WHEN** 用户替换 Profile 中 `Action.Dodge.Directional` 或 `Action.Dodge.Backstep` 的动画引用
- **THEN** Play Mode 中对应动作表现 MUST 使用替换后的动画
- **AND** 动作方向、输入消费、Run latch 和基础移动恢复规则 MUST 不需要修改代码
