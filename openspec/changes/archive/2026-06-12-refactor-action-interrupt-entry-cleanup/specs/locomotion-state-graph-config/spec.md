## ADDED Requirements
### Requirement: 状态机条件不得承载 FullBody Action 请求准入
系统 MUST 保持统一状态机 transition 条件集合的职责边界：条件可以读取移动意图、状态可退出、输入事实是否存在、状态 elapsed time 和状态 tag，但 MUST NOT 在默认 FullBody Action 入口中直接判断动作请求 priority、policy min priority、resistance、force 或 timing window。

#### Scenario: 默认 Dodge 入口只消费 accepted fact
- **GIVEN** 默认统一状态机配置
- **WHEN** 设计者查看 `Locomotion/* -> FullBody/Action/Dodge` transition
- **THEN** transition MUST 包含 `HasInputRequest(Dodge)` 或等价已接受请求事实条件
- **AND** transition MUST NOT 包含动作请求 priority 准入条件
- **AND** transition MUST NOT 读取动作策略集合

#### Scenario: transition evaluator 不读取动作策略
- **WHEN** transition evaluator 求值任意条件
- **THEN** evaluator MUST NOT 调用 `ActionInterruptArbiter`
- **AND** MUST NOT 读取 `ActionInterruptPolicySetSO`
- **AND** MUST NOT 执行 action policy matching

#### Scenario: 状态图 priority 不等于动作请求 priority
- **GIVEN** transition 定义包含 priority 字段
- **WHEN** runner 选择多条已满足 transition 中的一条
- **THEN** 该 priority MUST 只决定状态图 transition 选择顺序
- **AND** MUST NOT 被解释为动作请求 priority

### Requirement: RequestPriorityAtLeast 迁移清理
系统 SHOULD 删除或明确废弃 `RequestPriorityAtLeast` 状态机条件，除非实施阶段发现非动作场景存在已审批的真实依赖。若保留该条件，默认 FullBody Action 入口仍 MUST NOT 使用它。

#### Scenario: 无真实依赖时删除条件
- **GIVEN** 静态搜索确认没有非动作场景依赖 `RequestPriorityAtLeast`
- **WHEN** 实施清理
- **THEN** 系统 SHOULD 删除 `RequestPriorityAtLeast` enum、factory、evaluator 分支和默认测试引用
- **AND** MUST 保持已有资产条件 kind 的序列化含义不被误读

#### Scenario: 发现真实依赖时暂停扩大实现
- **GIVEN** 实施阶段发现非动作场景依赖 `RequestPriorityAtLeast`
- **WHEN** 该依赖不在本 proposal 已审批范围内
- **THEN** 实施 MUST 暂停删除该条件
- **AND** MUST 更新 proposal 或回到用户确认
- **AND** MUST NOT 将该条件用于默认 FullBody Action 入口
