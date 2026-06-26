# taco-transition-rule-graph-authoring Specification

## ADDED Requirements

### Requirement: TransitionRuleGraph 表达纯条件求值
系统 MUST 使用 `TransitionRuleGraph` 表达状态机 Transition 的纯 Bool 条件。`TransitionRuleGraph` MUST 使用现有 `BaseTree/BaseGraph` 资产和编辑器入口，MUST NOT 继承或模拟 `RunnableTree` 的执行生命周期。

#### Scenario: 创建规则图
- **WHEN** 用户为一条 Transition 创建规则图
- **THEN** 系统 MUST 创建 `TransitionRuleGraph`
- **AND** 该图 MUST 使用现有节点集合、属性边集合、字段访问器和 `PropertyPort`
- **AND** 系统 MUST NOT 创建 Workbench 图或并行端口协议

#### Scenario: 规则图求值
- **WHEN** 状态机 runtime 求值 Transition 条件
- **THEN** runtime MUST 将规则图当作纯 Bool 求值图
- **AND** 规则图 MUST NOT tick Timeline、Action、RunnableNode 或状态行为 `SubTree`

### Requirement: 规则图必须拥有唯一结果节点
系统 MUST 通过唯一 `TransitionRuleResultNode` 表达规则图的最终 Bool 输出。规则图缺失结果节点或存在多个结果节点时 MUST 被校验为非法。

#### Scenario: 新建规则图
- **WHEN** 系统新建 `TransitionRuleGraph`
- **THEN** 新图 MUST 默认包含一个 `TransitionRuleResultNode`
- **AND** 该节点 MUST 暴露一个 Bool 输入作为最终条件

#### Scenario: 结果节点非法
- **WHEN** 规则图没有结果节点或拥有多个结果节点
- **THEN** 校验 MUST 报告规则图非法
- **AND** runtime MUST NOT 将该规则图当作 true 通过

### Requirement: 规则图节点范围受限
`TransitionRuleGraph` MUST 只允许创建纯值、输入、黑板读取、谓词、比较、逻辑组合和结果节点。它 MUST NOT 创建状态机节点、状态节点、状态机控制节点、Timeline 行为节点、Action 行为节点、普通 `RunnableNode` 或 `RootNode`。

#### Scenario: 创建条件节点
- **WHEN** 用户在规则图中创建 InputAction Bool、黑板读取、Compare、And、Or 或 Not 节点
- **THEN** 创建逻辑 MUST 允许该节点
- **AND** 节点 MUST 通过现有 typed `PropertyPort` 参与求值连接

#### Scenario: 拒绝行为节点
- **WHEN** 用户、拖拽、粘贴或脚本路径尝试向规则图创建 `TimelineNode`、`StateMachineNode`、`StateNode`、`RunnableNode` 或 `RootNode`
- **THEN** 创建逻辑 MUST 拒绝该节点
- **AND** 非法节点 MUST NOT 进入正式节点集合

### Requirement: Transition 边引用可选规则图
状态机 Transition MUST 继续表达为 `StateMachineGraph` 中的 `BaseEdge` 语义。Transition 边 MUST 保存优先级和可选 `TransitionRuleGraph` 引用，MUST NOT 通过同层 Bool `PropertyPort` 引用保存条件。

#### Scenario: 无条件 Transition
- **WHEN** `Enter -> StateNode` 或 `StateNode -> StateNode|Exit` Transition 没有规则图
- **THEN** runtime MUST 将该 Transition 视为无条件可通过

#### Scenario: AnyState Transition
- **WHEN** `AnyState -> StateNode|Exit` Transition 没有规则图
- **THEN** 校验 MUST 报告该 Transition 非法
- **AND** runtime MUST NOT 将它视为无条件可通过

#### Scenario: 条件 Transition
- **WHEN** Transition 配置了规则图
- **THEN** runtime MUST 求值该 `TransitionRuleGraph`
- **AND** 只有规则图输出 true 时该 Transition 才可通过

### Requirement: Transition 调度元数据留在边上
Transition 的优先级和同优先级稳定排序 MUST 属于边调度数据。规则图 MUST NOT 负责选择其它 Transition，也 MUST NOT 保存 priority/tag/trigger 的调度排序逻辑。

#### Scenario: 多条 Transition 同时成立
- **WHEN** 同一来源节点存在多条规则图返回 true 的 Transition
- **THEN** runtime MUST 先按 Transition 优先级选择
- **AND** 优先级相同 MUST 再按 flow order 保持稳定顺序

#### Scenario: tag 或 fact 条件
- **WHEN** Transition 需要 tag、fact、输入或黑板变量参与判断
- **THEN** 这些数据 MUST 通过规则图内的读取或谓词节点表达
- **AND** Transition 边 MUST NOT 为每类业务数据新增专用条件字段

### Requirement: 规则图编辑入口属于 Transition 边
编辑器 MUST 允许用户从 Transition 边打开、创建和查看规则图。边视图 MUST 显示优先级、规则图缺失状态和规则摘要。

#### Scenario: 打开规则图
- **WHEN** 用户双击 Transition 边或点击边 Inspector 的规则图命令
- **THEN** 编辑器 MUST 打开该边引用的 `TransitionRuleGraph`
- **AND** 页面栈 MAY 记录来源边，但 MUST NOT 将页面栈写入资产

#### Scenario: 快捷条件
- **WHEN** 编辑器提供 Inspector 快捷条件配置
- **THEN** 快捷配置 MUST 修改同一个 `TransitionRuleGraph`
- **AND** 系统 MUST NOT 在边上保存第二套快捷条件数据

### Requirement: 旧 BoolPort 条件链路必须删除
系统 MUST 删除旧 `TransitionConditionNodeGuid/PortId` 条件链路和同图 Bool port 条件菜单。可迁移的旧条件 MUST 迁移为规则图；不可迁移的旧条件 MUST 被报告为非法结构，不得 fallback。

#### Scenario: 旧条件字段存在
- **WHEN** 旧资产或代码路径仍保存 Transition BoolPort 条件引用
- **THEN** 迁移或清理路径 MUST 将其移除
- **AND** runtime MUST NOT 再读取该旧字段决定 Transition
