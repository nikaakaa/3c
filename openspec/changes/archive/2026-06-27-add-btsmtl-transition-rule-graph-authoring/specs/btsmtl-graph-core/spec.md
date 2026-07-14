# btsmtl-graph-core Specification

## MODIFIED Requirements

### Requirement: 节点创建尊重图类型规则
系统 MUST 让 `BaseGraph.CreateNode(Type)` 尊重当前图的 `CanCreateNodeType(Type)`。节点搜索、拖拽、粘贴和脚本创建 MUST 不绕过该规则。`StateMachineGraph` MUST 只接收状态结构节点；`TransitionRuleGraph` MUST 只接收纯条件求值节点。

#### Scenario: StateMachineGraph 拒绝非法节点
- **WHEN** 创建路径尝试向 `StateMachineGraph` 创建 `StateMachineNode`、`RootNode`、普通 runnable 节点或条件 `ValueNode`
- **THEN** 创建逻辑 MUST 拒绝该节点
- **AND** 系统 MUST NOT 把该节点加入正式节点集合

#### Scenario: TransitionRuleGraph 接受条件节点
- **WHEN** 创建路径尝试向 `TransitionRuleGraph` 创建 InputAction、黑板读取、Value、Compare、Logic 或 `TransitionRuleResultNode`
- **THEN** 创建逻辑 MUST 允许该节点作为规则图求值节点
- **AND** 这些节点 MUST 继续使用正式字段访问器和 typed `PropertyPort`

#### Scenario: TransitionRuleGraph 拒绝行为节点
- **WHEN** 创建路径尝试向 `TransitionRuleGraph` 创建 `RunnableNode`、`TimelineNode`、`StateMachineNode`、`StateNode` 或状态机控制节点
- **THEN** 创建逻辑 MUST 拒绝该节点
- **AND** 系统 MUST NOT 把该节点加入正式节点集合

### Requirement: 不新增 Graph 分裂路径
系统 MUST 保持一套图数据、一套 BTSMTL 原生端口系统和一套编辑器资产入口。系统 MUST NOT 因 `BaseGraph`、`StateMachineGraph` 或 `TransitionRuleGraph` 新增 Workbench 图、并行端口协议、旧数据 fallback 或重复序列化集合。

#### Scenario: 结构链路唯一
- **WHEN** 新 Graph 能力接入 BTSMTL
- **THEN** 它 MUST 使用现有 `BaseGraph` 集合、`PropertyPort` / `PropertyEdge` 和 `BaseTree` 编辑入口
- **AND** 它 MUST NOT 新增并行 Workbench 或 fallback 数据链路

#### Scenario: 规则图链路唯一
- **WHEN** Transition 需要条件求值图
- **THEN** 系统 MUST 使用 `TransitionRuleGraph`
- **AND** 系统 MUST NOT 同时保留旧 BoolPort 条件字段作为第二套运行条件
