## ADDED Requirements
### Requirement: Transition 条件由 Evaluator Adapter 求值
统一状态机 MUST 将 transition condition 的配置表达和求值实现分离。状态图配置 MUST 保存稳定 condition key 和参数；运行时 MUST 通过正式 evaluator adapter collection 求值，而不得在 runner 或中心 evaluator 中硬编码新增业务条件。

#### Scenario: Runner 不知道业务条件
- **WHEN** runner 选择 transition
- **THEN** runner MUST 将 condition 交给 evaluator collection 求值
- **AND** runner MUST NOT 直接包含 TurnBack、Dodge、Attack、Jump、HitReact 或等价业务条件分支

#### Scenario: 缺失 evaluator 配置失败
- **GIVEN** 状态机配置包含一个 condition key
- **AND** 默认 evaluator collection 没有对应 evaluator
- **WHEN** 状态机配置被校验或 runner 初始化
- **THEN** 系统 MUST 报告配置错误
- **AND** MUST NOT 静默将该条件视为 false

#### Scenario: Evaluator 只读取纯数据 facts
- **WHEN** 任一 transition condition evaluator 求值
- **THEN** evaluator MUST 只读取状态机 context、timeline facts、runtime blackboard facts 或等价纯数据输入
- **AND** MUST NOT 读取 Animancer runtime、Animator、CharacterController、InputAction、Transform 或 Camera

#### Scenario: 重复 condition key 配置失败
- **GIVEN** 两个 evaluator adapter 声明支持同一个 condition key
- **WHEN** 默认 evaluator collection 被构建或校验
- **THEN** 系统 MUST 报告重复 key 错误
- **AND** MUST NOT 使用注册顺序隐式选择其中一个 evaluator

### Requirement: Domain 条件通过 Adapter 扩展
FullBody、Locomotion、Action 和 Animation 领域条件 MUST 通过各自 evaluator adapter 扩展。新增动作状态需要新的业务条件时，MUST 新增或复用对应 adapter，而不是修改 runner 的 transition 选择核心。

#### Scenario: TurnBack 条件归 Locomotion evaluator
- **WHEN** `MoveTurnBackRequested` 或等价 condition key 被求值
- **THEN** 求值逻辑 MUST 位于 Locomotion evaluator adapter
- **AND** MUST 读取已派生的 Locomotion facts
- **AND** MUST NOT 在 runner 中重新计算空间夹角

#### Scenario: Action 退出条件归 Action evaluator
- **WHEN** `ActionCanExit` 或等价 condition key 被求值
- **THEN** 求值逻辑 MUST 位于 Action 或 Animation evaluator adapter
- **AND** MUST 读取 action playback facts 或 timeline facts
- **AND** MUST NOT 读取 Animancer state

#### Scenario: 新业务条件不修改核心 runner
- **WHEN** 后续新增 Attack、Jump 或 HitReact transition condition
- **THEN** 实现 MUST 新增或扩展 domain evaluator adapter
- **AND** MUST NOT 修改 runner 的 transition 选择循环

### Requirement: Transition 条件诊断由 Trace 输出
Transition condition evaluator MUST 返回纯数据 trace，说明 condition key、输入摘要、结果和失败原因。运行时日志 MUST 由 diagnostics adapter 消费 trace 后提交，而不是由 evaluator 或 runner 直接提交。

#### Scenario: TurnBack 条件日志来自 trace
- **WHEN** `MoveTurnBackRequested` 或等价 condition 被求值
- **THEN** evaluator MUST 返回包含 TurnBack intent、角度、阈值和 passed 结果的 trace
- **AND** diagnostics adapter MUST 能用该 trace 输出现有等价诊断日志

#### Scenario: Evaluator 不提交日志
- **WHEN** 检查 transition condition evaluator 源码
- **THEN** evaluator MUST NOT 直接调用 `RuntimeDiagnosticLog.Submit`
- **AND** runner transition 选择循环 MUST NOT 为单个业务条件直接提交日志
