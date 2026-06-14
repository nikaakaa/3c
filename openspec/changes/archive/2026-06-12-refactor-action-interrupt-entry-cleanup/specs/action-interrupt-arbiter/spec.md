## ADDED Requirements
### Requirement: FullBody Action 准入上下文收口
系统 MUST 在 FullBody Action 请求进入统一状态机之前构建完整的动作仲裁上下文。该上下文 MUST 包含当前 action state、当前 action elapsed seconds、当前 action resistance 和当前 tick。priority、resistance、force 和 timing window 的裁决 MUST 只发生在动作仲裁入口，不得分散到状态机 transition 条件中。

#### Scenario: Dodge 请求使用配置化 priority 和 resistance
- **GIVEN** 默认角色绑定了 Dodge 动作配置
- **AND** 输入缓冲中存在 Dodge 请求
- **WHEN** FullBody Action 请求门面构建仲裁请求和上下文
- **THEN** 请求 priority MUST 来自 Dodge 动作配置
- **AND** 当前 state 为 `Action.Dodge` 时 context resistance MUST 来自 Dodge 动作配置
- **AND** 当前 state 为 `Action.None` 时 context resistance MUST 为 0

#### Scenario: 状态机不裁决动作请求 priority
- **WHEN** 默认 FullBody Action 入口处理 Dodge 请求
- **THEN** 状态机 transition MUST NOT 使用 `RequestPriorityAtLeast` 或等价条件判断请求 priority
- **AND** `ActionInterruptArbiter` MUST 是该请求 priority、resistance、force 和 timing window 的唯一准入裁决入口

#### Scenario: rejected 请求不生成状态机事实
- **GIVEN** Dodge 请求被当前 resistance、policy min priority 或 timing window 拒绝
- **WHEN** FullBody Action 请求门面完成本帧处理
- **THEN** 系统 MUST NOT 生成可被统一状态机消费的 Dodge input fact
- **AND** 状态机 MUST NOT 因该 rejected 请求进入 `FullBody/Action/Dodge`

### Requirement: Dodge 作为 FullBody Action 管线实例
系统 MUST 将 Dodge 作为 FullBody Action 管线的一个动作实例处理。Dodge 可以拥有自己的实例配置、请求参数、方向/后撤变体、动作位移配置、转向配置、run latch 和返回 Locomotion 规则，但这些差异 MUST 通过同一条 FullBody Action 管线表达。

#### Scenario: Dodge 实例行为仍走同一准入
- **GIVEN** 输入缓冲中存在 Dodge 请求
- **WHEN** 系统处理该请求
- **THEN** 系统 MAY 使用 Dodge 实例逻辑解析 Directional 或 Backstep
- **AND** MAY 使用 Dodge 实例配置决定位移、转向和 resistance
- **BUT** 请求进入统一状态机前 MUST 仍经过 `FullBodyActionInterruptGate` 和 `ActionInterruptArbiter`

#### Scenario: Dodge 输出仍由统一状态机负责
- **GIVEN** Dodge 请求已被仲裁接受
- **WHEN** 统一状态机进入 `FullBody/Action/Dodge`
- **THEN** Dodge 的动作位移、动画请求、输入消费和返回 Locomotion MUST 仍由统一状态机输出及其现有执行边界负责
- **AND** 仲裁器 MUST NOT 直接播放 Dodge 动画或执行 Dodge 位移

### Requirement: 动作准入条件不得回流状态机
系统 MUST 防止动作请求 priority 条件重新成为统一状态机 transition 的一部分。状态机 transition 的 `priority` 字段 MAY 继续用于多个 transition 同时满足时的选择顺序，但 MUST NOT 表达动作请求的准入优先级。

#### Scenario: transition priority 仍用于状态图选边
- **GIVEN** 同一个当前状态存在多条条件已满足的 transition
- **WHEN** 统一状态机 runner 解析 transition
- **THEN** runner MUST 使用 transition 自身 priority 选择要执行的 transition
- **AND** 该 priority MUST NOT 替代动作请求 priority、policy min priority 或 current resistance

#### Scenario: 默认动作入口没有请求优先级条件
- **WHEN** 检查默认状态机定义和默认状态机资产
- **THEN** `Locomotion/* -> FullBody/Action/Dodge` transition MUST 只消费已被仲裁接受的 Dodge input fact
- **AND** MUST NOT 包含 `RequestPriorityAtLeast`、`minPriority` 动作准入条件或等价状态机条件
