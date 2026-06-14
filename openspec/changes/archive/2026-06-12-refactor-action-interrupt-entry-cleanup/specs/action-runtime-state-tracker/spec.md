## ADDED Requirements
### Requirement: Action facts 同步权威
系统 MUST 以统一角色状态机快照作为当前 FullBody Action state 的权威。`ActionRuntimeStateTracker` 或等价 helper MAY 保存当前 Action facts，但 MUST 由统一状态机快照同步或派生，不得独立驱动状态、自动退出、消费输入或决定 transition。

#### Scenario: Locomotion 派生为空 Action
- **GIVEN** 统一状态机当前 owner 为 Locomotion
- **WHEN** FullBody Action 请求门面构建仲裁上下文
- **THEN** 当前 action state MUST 为 `Action.None`
- **AND** current resistance MUST 为 0

#### Scenario: Dodge 派生为 Action.Dodge
- **GIVEN** 统一状态机当前 owner 为 Action
- **AND** 当前 action state 为 `Action.Dodge`
- **AND** Dodge 动作配置 resistance 为 40
- **WHEN** FullBody Action 请求门面构建仲裁上下文
- **THEN** 当前 action state MUST 为 `Action.Dodge`
- **AND** current resistance MUST 为 40

#### Scenario: tracker 不成为第二状态机
- **WHEN** 检查 `ActionRuntimeStateTracker` 或等价 helper 的运行时接入
- **THEN** 它 MUST NOT 调用统一状态机 transition
- **AND** MUST NOT 调用动画播放 API
- **AND** MUST NOT 直接读取或消费输入缓冲
- **AND** MUST NOT 因 duration、动画结束或隐藏规则自动退出当前 action

### Requirement: 当前 resistance 事实来源
系统 MUST 通过动作配置或等价纯数据表解析当前 Action resistance。解析过程 MUST NOT 依赖 Animator、Animancer、AnimationClip、CharacterController、Input System、Cinemachine 或 BBB 运行时类型。

#### Scenario: 已知 action state 返回配置抗性
- **GIVEN** 当前 action state 为 `Action.Dodge`
- **AND** Dodge 动作配置 resistance 为 20
- **WHEN** 系统解析当前 Action resistance
- **THEN** 结果 MUST 为 20

#### Scenario: 未知或空 action state 返回 0
- **GIVEN** 当前 action state 为空、`Action.None` 或当前配置无法识别
- **WHEN** 系统解析当前 Action resistance
- **THEN** 结果 MUST 为 0
