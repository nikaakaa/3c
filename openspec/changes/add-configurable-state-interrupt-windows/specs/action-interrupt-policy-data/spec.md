## ADDED Requirements
### Requirement: 状态请求策略不重复定义窗口时间
系统 MUST 让状态请求策略只描述从当前状态到目标状态的准入关系、最小请求优先级、force 和 required fact id。新增状态请求策略 MUST NOT 重新定义同一个窗口的 start/end timing；窗口 timing MUST 来自 `StateTimelinePolicy`，并由 sampler 产出 active facts。旧 `ActionInterruptPolicy` 的 elapsed timing rule MAY 在迁移期保留，但不得作为 Attack combo、TurnBack 或后续 HitReact 新窗口的首选表达。

#### Scenario: 新 Attack 策略只引用 combo window
- **GIVEN** Attack01 的 timeline policy 定义了 `attack01-combo` window
- **WHEN** 设计者配置 Attack01 到 Attack02 的请求策略
- **THEN** 策略 MUST 能引用 `ComboInputOpen` 或等价 required fact id
- **AND** 策略 MUST 配置 min priority 或 force
- **AND** 策略 MUST NOT 配置另一份 combo window start/end

#### Scenario: 旧 Dodge timing rule 是迁移兼容
- **GIVEN** 现有 Dodge 策略使用 elapsed time timing rule
- **WHEN** 本变更迁移策略数据源
- **THEN** 系统 MAY 保留该规则以保护旧行为
- **AND** 新增状态请求规范 MUST 推荐使用 required fact id

### Requirement: 状态请求策略数据源
系统 MUST 提供可配置的状态请求策略数据源，用于描述从当前状态到目标状态的请求准入规则。该数据源 MUST 能覆盖现有 ActionInterruptPolicy 的 priority、resistance、force 和 timing 语义，并 MUST 能引用或关联状态 timeline fact id。

#### Scenario: TurnBack 策略引用窗口
- **GIVEN** 策略 from state 为 `FullBody/Locomotion/MoveLoop`
- **AND** target state 为 `FullBody/Locomotion/TurnBack`
- **WHEN** 设计者配置策略
- **THEN** 策略 MUST 能引用 TurnBack 允许进入事实或等价 fact id
- **AND** MUST 能配置 min priority 和 force

#### Scenario: Dodge 现有策略可迁移
- **GIVEN** 当前已有 Dodge action interrupt policy
- **WHEN** 系统迁移到状态请求策略数据源
- **THEN** 现有 Dodge priority、timing rule 和 force 语义 MUST 能保持
- **AND** 不需要状态机 transition 条件重新判断请求 priority

### Requirement: 策略数据编译到纯 runtime 数据
状态请求策略数据源 MUST 编译为纯 runtime policy 列表。编译器 MUST 只做数据转换和校验，不得调用状态机、Animancer、Animator、motion executor、CharacterController 或 Transform。

#### Scenario: 编译 TurnBack 策略
- **GIVEN** 一个 TurnBack 状态请求策略定义
- **WHEN** 系统编译策略集合
- **THEN** 输出 runtime policy MUST 包含 from state、target state、min priority、force 和 required fact id
- **AND** 输出 policy MUST 不包含 Unity 对象引用

#### Scenario: 缺失 fact 报告错误
- **GIVEN** 策略引用了不存在的 required fact id
- **WHEN** 系统校验策略集合
- **THEN** 校验结果 MUST 包含错误

### Requirement: 策略配置入口不污染 Locomotion
状态请求策略配置 MAY 由角色 FullBody 配置、状态机配置或等价正式装配点引用，但 Locomotion movement pipeline、Animancer presenter 和 motion executor MUST NOT 直接读取策略 SO。

#### Scenario: Presenter 不读取策略
- **WHEN** 基础移动动画 Presenter 播放 TurnBack 或 MoveLoop 动画
- **THEN** Presenter MUST NOT 读取状态请求策略资产
- **AND** MUST NOT 由策略资产决定是否切换状态
