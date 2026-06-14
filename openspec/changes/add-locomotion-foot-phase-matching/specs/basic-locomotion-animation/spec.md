## ADDED Requirements
### Requirement: 基础移动脚相位 Profile 绑定
系统 MUST 允许基础移动动画配置绑定 Locomotion 脚相位 Profile，使 `TurnBack` 和 `RunLoop` 能通过正式配置参与脚相位匹配。该绑定 MUST 归属于现有基础移动动画配置或其明确子配置，不得新增游离全局配置或 Resources 隐式加载路径。

#### Scenario: RunLoop 绑定脚相位 Profile
- **GIVEN** 当前角色基础移动动画配置包含 `RunLoop` alias
- **WHEN** 设计者为 `MoveLoop + Run + RunLoop` 绑定脚相位 Profile
- **THEN** 运行时 MUST 能通过 phase、gait 和 alias key 解析到该 profile

#### Scenario: TurnBack 绑定脚相位 Profile
- **GIVEN** 当前角色基础移动动画配置包含 `Locomotion.Turn.Back` alias
- **WHEN** 设计者为 `TurnBack + Run + Locomotion.Turn.Back` 绑定脚相位 Profile
- **THEN** 运行时 MUST 能通过 phase、gait 和 alias key 解析到该 profile

#### Scenario: 不新增隐式配置路径
- **WHEN** 当前配置未绑定脚相位 Profile
- **THEN** 系统 MUST 报告配置缺失或匹配无效
- **AND** MUST NOT 通过 `Resources.Load`、全局单例或硬编码路径寻找 profile

### Requirement: 移动动画上下文携带相位匹配请求
系统 MUST 扩展移动动画上下文，使其可以携带纯数据脚相位匹配请求或匹配结果。该上下文 MUST 不携带脚相位 Profile、Animancer runtime、AnimationClip、TransitionAsset、Transform、CharacterController 或 InputAction。

#### Scenario: TurnBack 后 RunLoop 上下文携带匹配结果
- **GIVEN** 黑板中存在有效 TurnBack exit foot phase
- **AND** RunLoop profile 解析出有效 start normalized time
- **WHEN** 系统构建 `MoveLoop + Run` 的移动动画上下文
- **THEN** 上下文 MUST 携带有效的 RunLoop start normalized time override

#### Scenario: 普通移动上下文不携带匹配请求
- **GIVEN** 当前不是从 TurnBack 进入 RunLoop
- **WHEN** 系统构建移动动画上下文
- **THEN** 上下文 MUST 标记为没有脚相位匹配 override

#### Scenario: 上下文保持纯数据
- **WHEN** 动画外观层读取移动动画上下文
- **THEN** 它 MUST NOT 能通过上下文访问脚相位 Profile 资产
- **AND** MUST NOT 能访问 Unity 场景对象或 Animancer runtime

### Requirement: Animancer RunLoop 起播相位应用
Animancer 基础移动外观层 MUST 在新进入 `MoveLoop + RunLoop` 时消费脚相位匹配结果，并设置一次目标 state 的 normalized time。外观层 MUST NOT 因脚相位匹配决定逻辑状态、移动命令或 TurnBack 退出。

#### Scenario: 新播放 RunLoop 应用 start override
- **GIVEN** 移动动画上下文阶段为 `MoveLoop`
- **AND** gait 为 `Run`
- **AND** alias key 解析为 `RunLoop`
- **AND** 上下文携带有效 start normalized time override
- **WHEN** Presenter 新播放 RunLoop
- **THEN** Presenter MUST 设置新 state 的 `NormalizedTime` 为该 override
- **AND** MUST 记录诊断说明该 override 已应用

#### Scenario: 相同 RunLoop 不重复应用 start override
- **GIVEN** 当前 Presenter 已经在播放 `MoveLoop + RunLoop`
- **AND** 下一帧收到相同 phase、gait 和 alias key
- **WHEN** 上下文仍携带 start normalized time override
- **THEN** Presenter MUST 保持现有播放进度
- **AND** MUST NOT 每帧重设 `NormalizedTime`

#### Scenario: 无效 override 不改变播放
- **GIVEN** 上下文没有有效 start normalized time override
- **WHEN** Presenter 新播放 RunLoop
- **THEN** Presenter MUST 使用现有 Animancer 播放行为
- **AND** MUST NOT 猜测脚相位起播点

### Requirement: 基础移动脚相位自动测试和手动验证
系统 MUST 为基础移动脚相位匹配提供 EditMode 测试和手动验证步骤，证明 TurnBack 后 RunLoop 起播相位被正确应用，且普通移动动画不受影响。

#### Scenario: 自动测试覆盖 Presenter 起播
- **WHEN** 运行基础移动动画 EditMode 测试
- **THEN** 测试 MUST 覆盖 RunLoop 新进入时应用 start override
- **AND** MUST 覆盖相同 RunLoop 连续帧不重复应用 override

#### Scenario: 手动验证 TurnBack 衔接
- **GIVEN** 用户在 Sandbox 使用当前 Corin 角色
- **AND** Locomotion 与 Animation 诊断日志已启用
- **WHEN** 用户从 RunLoop 触发 TurnBack 并继续移动
- **THEN** TurnBack 退出后 MUST 回到 RunLoop
- **AND** 日志 MUST 能显示 exit foot phase 和 RunLoop matched start normalized time

