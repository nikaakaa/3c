## ADDED Requirements

### Requirement: Action Transition Policy Matrix 作者视图
系统 MUST 提供 Action Transition Policy Matrix 或批准等价作者视图，用于编辑跨 Action 请求准入关系。Matrix MUST 写回正式 Action interrupt / request policy 数据源，并编译为现有 `ActionInterruptPolicy`、状态请求策略 runtime policy 或批准等价纯 runtime policy。Matrix MUST NOT 成为 Branch graph、状态机 runner、motion executor、animation presenter、blackboard writer 或第二角色帧入口。

#### Scenario: Matrix row 编译为 runtime policy
- **GIVEN** matrix row 配置 from `Action.Block`、to `Action.GuardCounter`、request `Attack`、required fact `window.counter.open`
- **WHEN** policy compiler 编译该 matrix
- **THEN** 输出 runtime policy MUST 包含相同 from / to / request / required fact 语义
- **AND** `ActionInterruptArbiter` MUST 能消费该编译结果

#### Scenario: Matrix 不直接执行跳转
- **WHEN** 设计者保存 matrix
- **THEN** matrix adapter MUST NOT 调用 Action lifecycle 切换
- **AND** MUST NOT 调用 motion executor
- **AND** MUST NOT 调用 animation presenter
- **AND** MUST NOT 写 runtime blackboard

#### Scenario: Matrix 是 policy 数据视图
- **GIVEN** 设计者在 Matrix Editor 中新增一行 policy
- **WHEN** 保存该 matrix
- **THEN** 修改 MUST 写回正式 Action interrupt / request policy 数据源
- **AND** MUST NOT 只保存在 GraphView edge、EditorWindow state 或 preview-only object 中

### Requirement: Matrix Row 字段合同
Matrix row MUST 能表达 from action id、to action id、request kind、required fact id、min priority、force 和 resistance 语义。row MAY 包含 diagnostics label 或 editor display metadata，但该 metadata MUST NOT 参与 runtime 仲裁。row MUST NOT 保存 AnimationClip、Animator、Animancer runtime object、Transform、CharacterController、MonoBehaviour、GraphView edge 或 EditorWindow state。

#### Scenario: Row 字段完整编译
- **GIVEN** matrix row 配置了 from、to、request、required fact、min priority、force 和 resistance
- **WHEN** compiler 编译该 row
- **THEN** runtime policy MUST 保留这些仲裁所需语义
- **AND** runtime policy MUST NOT 保存 editor-only display metadata 作为判断依据

#### Scenario: Row 不包含 Unity 对象引用
- **WHEN** validator 检查 matrix row
- **THEN** row MUST NOT 要求配置 AnimationClip、角色 prefab、Animator、AnimancerState、Transform 或 scene object
- **AND** compiler MUST NOT 将这些对象写入 runtime policy

### Requirement: Matrix Scope 仅覆盖 Action-to-Action
Action Transition Policy Matrix 第一版 MUST 只表达 `Action.* -> Action.*` 或批准等价 action id 之间的跨 Action 准入关系。Matrix authoring、editor、validator 和 tests MUST NOT 将 Locomotion state、TurnBack state、Branch TimelineNode、GraphView node 或 editor lane 当成本 Matrix row 的 from/to。Matrix compiler MAY 映射到现有底层 policy runtime 的 state id 字段，但该底层字段名 MUST NOT 扩大 Matrix 作者视图 scope。

#### Scenario: Action row 合法
- **GIVEN** matrix row 的 from 为 `Action.Block`
- **AND** to 为 `Action.GuardCounter`
- **WHEN** validator 检查该 row
- **THEN** from/to scope MUST 被视为合法 Action-to-Action row

#### Scenario: Locomotion target 被拒绝
- **GIVEN** matrix row 的 from 为 `Action.Attack01`
- **AND** to 为 `Locomotion.TurnBack`
- **WHEN** validator 检查该 row
- **THEN** validator MUST 报告 scope 错误
- **AND** MUST NOT 将该 row 作为 Action Transition Policy Matrix row 编译

#### Scenario: Branch TimelineNode 不能作为 target
- **GIVEN** matrix row 的 to 被配置为 `Action.Block.Loop` 或某个 Branch TimelineNode id
- **WHEN** validator 检查该 row
- **THEN** validator MUST 报告 target 不是 action id
- **AND** MUST NOT 将 Branch 内部节点解释成跨 Action 目标

### Requirement: Matrix Row 校验
系统 MUST 对 matrix row 提供统一校验。校验 MUST 覆盖空 from action id、空 to action id、空 request kind、非 Action scope from/to、负 min priority、缺失 required fact id、重复 row、非法 Branch target 和窗口 timing 重复定义。存在 error 时 compiler MUST NOT 生成可被正式 runtime 消费的半成品 policy。

#### Scenario: 空 from/to/request 报错
- **GIVEN** matrix row 缺少 from action id、to action id 或 request kind
- **WHEN** validator 检查该 row
- **THEN** validator MUST 报告错误

#### Scenario: 负 priority 报错
- **GIVEN** matrix row 的 min priority 小于 0
- **WHEN** validator 检查该 row
- **THEN** validator MUST 报告错误

#### Scenario: 重复 row 可诊断
- **GIVEN** matrix 中存在两条 from、to、request 和 required fact 完全相同的 row
- **WHEN** validator 检查 matrix
- **THEN** validator MUST 报告 warning 或 error
- **AND** MUST NOT 静默忽略其中一条 row

### Requirement: 跨 Action 跳转不写入 Branch 图
跨 Action 跳转 MUST 通过 request provider、interrupt arbiter、action lifecycle 和 policy 数据完成。CommittedActionBranch MUST NOT 直接持有指向另一个 Action root 的跳转边，Branch condition 命中 required fact 时也 MUST NOT 直接启动另一个 Action。

#### Scenario: Block 到 GuardCounter 走 policy
- **GIVEN** `Action.Block` 当前输出 `window.counter.open`
- **AND** 玩家提交 Attack 或 Counter 请求
- **WHEN** policy 允许从 `Action.Block` 到 `Action.GuardCounter`
- **THEN** Action interrupt arbiter MAY accept `Action.GuardCounter`
- **AND** `Action.Block` branch MUST NOT 直接跳到 `Action.GuardCounter` branch root

#### Scenario: Branch 只输出当前 Action outcome
- **WHEN** `Action.Block` branch evaluator 运行
- **THEN** 它 MUST 只输出 `Action.Block` 内部 TimelineNode 的 outcome
- **AND** MUST NOT 创建新的 `Action.GuardCounter` lifecycle state

#### Scenario: Branch target 不允许是另一个 Action
- **GIVEN** 设计者尝试把 Branch child target 配置为 `Action.GuardCounter`
- **WHEN** branch validator 或 matrix validator 检查配置
- **THEN** 系统 MUST 报告配置错误
- **AND** MUST 引导该关系进入 Action Transition Policy Matrix 或批准等价 policy 数据

### Requirement: Matrix 策略引用事实而不重复窗口时间
新增跨 Action policy row MUST 优先引用 required fact id 表达窗口准入，MUST NOT 重新配置同一个窗口的 start/end timing。窗口 timing MUST 来自 Action Timeline、ActionTimeline fact source 或批准等价动作时间源。旧 elapsed timing rule MUST 被迁移为 required fact id、timeline fact source 或明确迁移诊断，不得作为正式 runtime 兼容规则保留。

#### Scenario: Counter policy 引用窗口事实
- **GIVEN** `Action.Block` timeline 声明 `window.counter.open`
- **WHEN** 设计者配置 `Action.Block -> Action.GuardCounter`
- **THEN** policy row MUST 引用 `window.counter.open`
- **AND** policy row MUST NOT 配置另一份 counter window start/end

#### Scenario: 缺失 required fact 报错
- **GIVEN** policy row 引用 `window.counter.open`
- **AND** 当前配置没有任何已声明 fact id 匹配它
- **WHEN** policy validator 运行
- **THEN** validator MUST 报告错误
- **AND** runtime MUST NOT 使用隐藏默认窗口允许跳转

#### Scenario: Matrix 不通过前缀猜测匹配 fact
- **GIVEN** timeline 只声明 `window.counter.open`
- **AND** policy row 引用 `window.counter`
- **WHEN** policy validator 运行
- **THEN** validator MUST 报告缺失或不匹配 fact id
- **AND** MUST NOT 因字符串相似而接受该 policy

#### Scenario: Matrix 使用共享 Fact Resolver
- **GIVEN** condition/fact framework 的共享 compile context 声明了 `window.counter.open`
- **AND** matrix row 引用 `window.counter.open`
- **WHEN** policy validator 校验该 row
- **THEN** validator MUST 通过共享 fact resolver 或批准等价 compile context 解析该 fact
- **AND** MUST NOT 使用 matrix-only 隐藏 fact registry 得出不同结果

### Requirement: Matrix Runtime 仲裁语义
Matrix 编译结果 MUST 由 Action interrupt arbiter 或批准等价仲裁器消费。仲裁器 MUST 同时考虑 current action、request kind、required fact、min priority、force 和 resistance 语义。Matrix 本身 MUST NOT 执行 Action lifecycle 切换；accepted decision MUST 交由正式 Action lifecycle 推进。

#### Scenario: Fact active 且 priority 满足时接受
- **GIVEN** 当前 active action 为 `Action.Block`
- **AND** request kind 为 `Attack`
- **AND** active facts 包含 `window.counter.open`
- **AND** request priority 满足 policy min priority
- **WHEN** Action interrupt arbiter 消费 matrix 编译结果
- **THEN** 仲裁器 MUST 返回 accepted `Action.GuardCounter` 或批准等价 accepted decision

#### Scenario: Fact missing 时拒绝
- **GIVEN** 当前 active action 为 `Action.Block`
- **AND** request kind 为 `Attack`
- **AND** active facts 不包含 `window.counter.open`
- **WHEN** Action interrupt arbiter 消费 matrix 编译结果
- **THEN** 仲裁器 MUST 返回 rejected decision 或明确 diagnostics
- **AND** MUST NOT 因存在 from/to/request 匹配而忽略 required fact

#### Scenario: Accepted decision 交给 lifecycle
- **GIVEN** 仲裁器返回 accepted `Action.GuardCounter`
- **WHEN** 本帧 Action runtime 推进
- **THEN** Action lifecycle MUST 负责进入 `Action.GuardCounter`
- **AND** matrix compiler、matrix adapter 和 Branch evaluator MUST NOT 直接创建 active lifecycle state
