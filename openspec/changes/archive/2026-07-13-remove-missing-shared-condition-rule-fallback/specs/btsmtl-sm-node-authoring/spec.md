## MODIFIED Requirements

### Requirement: Transition 是同层 BaseEdge 语义

系统 MUST 将状态转换表达为 `StateMachineGraph` 内联保存的 `BaseEdge`，MUST NOT 新增 `TransitionNode`，也 MUST NOT 为 Transition 本体创建 asset。Transition MUST 只连接同层 `Enter`、`AnyState`、`StateNode` 和 `Exit`。Transition 条件默认 MUST 是该 edge 内部的 inline `ConditionRuleGraph` 数据；需要复用时才显式绑定 shared `ConditionRuleGraph` asset。每个 edge MUST 保存正式 ConditionRuleGraph ownership，系统 MUST NOT 根据 shared asset 是否可解析来猜测或改写 owner 来源。

#### Scenario: 合法端点

- **WHEN** 用户连接 Transition flow
- **THEN** 合法连接 MUST 是 `Enter -> StateNode`、`AnyState -> StateNode|Exit`、`StateNode -> StateNode|Exit`
- **AND** `RootNode`、`StateMachineNode`、普通 `RunnableNode`、`TimelineNode`、Tree 节点和 `ValueNode` MUST NOT 成为 Transition 端点

#### Scenario: 条件和优先级

- **WHEN** Transition 配置条件
- **THEN** 条件 MUST 通过该 Transition resolved `ConditionRuleGraph` 表达
- **AND** 创建合法 Transition edge 时 MUST 立即创建该 edge 内部的 inline `ConditionRuleGraph`
- **AND** 默认规则图 MUST 是该 edge 内部的 inline graph data
- **AND** 默认规则图在未连接条件时 MUST 允许 Transition 通过
- **AND** `AnyState` Transition MUST 配置规则图条件
- **AND** runtime MUST 按 Transition 优先级选择可通过的边

#### Scenario: Transition 显式复用规则图

- **WHEN** 多条 Transition 需要复用同一套规则
- **THEN** 用户 MUST 显式抽取或分配 shared `ConditionRuleGraph` asset
- **AND** 删除 Transition 时 MUST 只断开 shared 引用，不删除 shared asset
- **AND** 切换到 shared asset 后 MUST 清理该 Transition 的 inline rule graph 真数据

#### Scenario: Shared ConditionRuleGraph asset 被删除

- **WHEN** Transition 配置为 Shared ownership，但其 `ConditionRuleGraph` asset 被删除、类型错误或无法解析
- **THEN** 编辑器、validator 与 runtime MUST 保留该 Shared ownership 错误并报告 edge 与 owner
- **AND** 编辑器 MUST NOT 清理 shared 引用、创建 inline 图或把 Transition 当作无条件边
- **AND** runtime MUST 使该 Transition 条件失败
- **AND** 作者只能显式替换 shared asset 或执行 Use Inline 才能恢复该 Transition

### Requirement: Transition Rule 编辑入口属于 Transition 边

编辑器 MUST 允许用户从 Transition 边打开和查看条件图。边视图 MUST 显示优先级、持久化 ownership、resolved 状态和规则摘要。默认私有规则图 MUST 作为 Transition edge 内部 inline graph data 保存，需要复用时才显式抽取或分配 shared `ConditionRuleGraph` asset。打开、刷新和校验 MUST NOT 改写已落盘 edge 的 ownership 或生成替代规则图。

#### Scenario: 打开 resolved 规则图

- **WHEN** 用户双击 Transition 边或点击边 Inspector 的 `Open Rule`
- **AND** 该 Transition 边拥有与 ownership 匹配的 resolved `ConditionRuleGraph`
- **THEN** 编辑器 MUST 打开该 resolved `ConditionRuleGraph`
- **AND** 页面栈 MAY 记录来源边，但 MUST NOT 将页面栈写入图数据

#### Scenario: 打开 invalid Transition rule

- **WHEN** 用户尝试打开 ownership 为 Unspecified、Shared asset 缺失、类型错误、Inline 数据缺失或 inline/shared 双持有的 Transition
- **THEN** 编辑器 MUST 显示 edge、owner、ownership 和错误原因
- **AND** 编辑器 MUST NOT 创建 inline `ConditionRuleGraph`、清理 shared 引用或把该 Transition 当作无条件边

#### Scenario: 作者显式切换到 Inline

- **WHEN** 作者在 invalid 或 Shared Transition 上执行 `Use Inline Rule`
- **THEN** 编辑器 MUST 创建新的 edge 内部 inline `ConditionRuleGraph`
- **AND** edge MUST 写入 Inline ownership 并清理 shared 真数据
- **AND** 默认规则图 MUST 包含默认通过的规则输出入口

#### Scenario: 作者显式替换 Shared

- **WHEN** 作者为 Shared Transition 选择另一份有效 `ConditionRuleGraph` asset
- **THEN** edge MUST 保持 Shared ownership 并保存新的 shared 引用
- **AND** edge MUST NOT 保留 inline 规则图真数据

#### Scenario: 删除带规则图的 Transition

- **WHEN** 用户删除拥有 inline 规则图的 Transition 边
- **THEN** inline 规则图 MUST 随 Transition 边序列化数据一起删除
- **AND** 系统 MUST NOT 执行 subasset 删除

#### Scenario: 删除引用 shared 规则图的 Transition

- **WHEN** 用户删除引用 shared asset 规则图的 Transition 边
- **THEN** 系统 MUST 只删除 Transition 边并断开引用
- **AND** 系统 MUST NOT 删除 shared asset

### Requirement: Transition Rule 默认图数据初始化

系统 MUST 只在创建合法 Transition 或作者显式执行 `Use Inline Rule` 时初始化一份可立即下钻编辑的 inline `ConditionRuleGraph` 数据。规则图是条件求值图，不是普通行为树。编辑器 MUST NOT 通过打开、刷新、校验或 `CheckInit()` 修复已落盘的 invalid edge。

#### Scenario: 创建默认 Transition rule

- **WHEN** 用户连接合法 Transition
- **THEN** 系统 MUST 创建 inline `ConditionRuleGraph` 数据并写入 Inline ownership
- **AND** 默认规则图 MUST 包含规则输出入口
- **AND** 默认规则图在未连接条件时 MUST 允许 Transition 通过
- **AND** 用户 MUST 能立即下钻编辑条件节点和属性连线
- **AND** 创建流程 MUST NOT 创建 subasset

#### Scenario: 已落盘规则图缺失

- **WHEN** 编辑器打开、刷新或校验一个 ownership 与实际数据不匹配的 Transition
- **THEN** 系统 MUST 保留该 invalid 状态并报告错误
- **AND** 系统 MUST NOT 自动生成 inline 图、复制 shared 图或清除断裂引用
