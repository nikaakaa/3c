## REMOVED Requirements

### Requirement: FullBody 分层 HFSM 状态树

**Reason**: FullBody 不应继续作为角色主状态根或 Locomotion 上级 owner。

**Migration**: 使用 Locomotion module、Action module 和 Character frame pipeline 协调。

## ADDED Requirements

### Requirement: FullBody 只表达身体占用语义

系统 MUST 将 FullBody 表达为 Action domain 对身体输出范围的 body/channel claim、动画表现层命名或只读诊断 view。FullBody MUST NOT 作为 Locomotion 的父状态域、角色帧 owner、状态机 runner owner 或正式配置根。

#### Scenario: FullBody Action 提交 claim
- **GIVEN** Action domain 接受 Dodge、Attack 或等价全身动作
- **WHEN** Action submitter 构建本帧提交
- **THEN** 它 MUST 提交 full-body 或等价 body/channel claim
- **AND** MAY 提交 action motion candidate 和 action animation candidate
- **AND** MUST NOT 直接拥有 Locomotion state

#### Scenario: Locomotion 不属于 FullBody
- **WHEN** Locomotion module 推进基础移动状态
- **THEN** 当前状态 MUST 使用 Locomotion domain state id
- **AND** MUST NOT 使用 FullBody owner 表达移动状态归属
- **AND** MUST NOT 需要 FullBody runner 才能恢复 Locomotion phase

### Requirement: 局部 HFSM 只能作为领域实现

系统 MAY 在 Locomotion module 或单个 Action lifecycle 内使用 FSM/HFSM，但该状态图 MUST 是该领域 module 的 implementation。领域外部 interface MUST 暴露纯数据 facts、domain state id、candidate output 或 body/channel claim，而不是暴露跨领域树路径。

#### Scenario: Locomotion 内部可使用状态图
- **WHEN** Locomotion module 表达 Idle、MoveStart、MoveLoop、MoveStop 或 TurnBack
- **THEN** 它 MAY 使用内部状态图
- **AND** 该状态图 MUST NOT 直接执行 movement
- **AND** 该状态图 MUST NOT 直接播放 animation

#### Scenario: Action 内部可使用生命周期状态
- **WHEN** Dodge 或 Attack 需要表达 startup、active、recovery 或 cancel window
- **THEN** Action module MAY 使用内部 FSM/HFSM、timeline 或 action instance state
- **AND** 对外 MUST 提交 action facts 和 body/channel claim
- **AND** MUST NOT 要求该 action 成为角色级统一树叶子

### Requirement: 旧 FullBody 路径退役

系统 MUST 将 `FullBody/Locomotion/...` 与 `FullBody/Action/...` 视为遗留路径。迁移期 MAY 识别旧路径用于转换或测试，但新增正式配置、正式断言和生产路径 MUST 使用领域 ID。

#### Scenario: 旧路径只用于迁移
- **WHEN** 加载旧配置或旧测试 fixture
- **THEN** 系统 MAY 将旧 FullBody path 转换为领域 ID
- **AND** 转换结果 MUST 使用 `Locomotion.*` 或 `Action.*`
- **AND** runtime MUST NOT 将旧 path 作为正式状态权威

#### Scenario: 新配置不写 FullBody path
- **WHEN** 创建新的 Locomotion 或 Action 配置
- **THEN** 配置 MUST 使用领域 ID
- **AND** MUST NOT 新增 `FullBody/Locomotion` 或 `FullBody/Action` 作为正式路径

### Requirement: 自动验证旧口径退役

系统 MUST 提供自动测试或静态验证，证明旧 FullBody 状态根不再决定 motion、animation、input consume 或 body arbitration。

#### Scenario: 验证领域 ID 和 claim
- **WHEN** 运行相关 EditMode 测试
- **THEN** 测试 MUST 覆盖 `Locomotion.Idle`
- **AND** MUST 覆盖 `Locomotion.MoveLoop`
- **AND** MUST 覆盖 `Action.Dodge`
- **AND** MUST 覆盖 Dodge claim 参与 FramePlan 输出选择

#### Scenario: 静态验证旧路径
- **WHEN** 检查生产源码、测试断言和配置资产
- **THEN** 验证 MUST 确认新增正式路径不依赖 `FullBodyOwnerKind.Locomotion`
- **AND** MUST 确认新增正式配置不使用 `FullBody/Locomotion`
- **AND** MUST 确认 BodyArbiter 不通过 FullBody view 反向选择输出
