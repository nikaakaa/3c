# btsmtl-graph-core Specification

## ADDED Requirements

### Requirement: Graph 必须拥有统一稳定 authoring identity

每个 `BaseGraph` MUST 持有稳定 `GraphAuthoringId`，Node 和 Edge MUST 继续持有各自稳定 authoring GUID。Graph runtime clone MUST 保留这些 source identities，但 MUST 使用独立 runtime instance identity。Pipeline Blackboard declaration owner、Agent Snapshot、Debug Source Map 和 editor navigation MUST 引用同一个 Graph authoring identity。

#### Scenario: 创建 inline Graph

- **WHEN** owner 创建新的 inline Graph
- **THEN** Graph MUST 获得新的稳定 `GraphAuthoringId`
- **AND** Graph 内 Node/Edge MUST 获得各自稳定 identity

#### Scenario: 创建 runtime clone

- **WHEN** runtime 从 authoring Graph 创建工作副本
- **THEN** clone MUST 保留 Graph/Node/Edge authoring identity
- **AND** clone MUST 获得新的 runtime instance identity

#### Scenario: 迁移 Blackboard owner identity

- **WHEN** 实现将旧 `BlackboardOwnerId` 提升为 `GraphAuthoringId`
- **THEN** 现有 declaration owner reference MUST 一次性迁移到同一 identity value
- **AND** 旧字段、旧 API 和第二份 debug Graph id MUST 删除

### Requirement: TreeWindow runtime 状态必须通过只读 diagnostics overlay 表达

`BaseTreeWindow` MUST 继续绑定 authoring Graph，并通过 `RuntimeDebugSession` 和 source identity 显示选中 runtime instance 的 Node、Edge、StateMachine 和生命周期状态。TreeWindow MUST NOT 打开 runtime clone 作为 authoring page，也 MUST NOT 直接读取 authoring Node 的 runtime `State` 字段。

#### Scenario: Live Debug 高亮运行节点

- **WHEN** Session 为当前 Graph source 提供匹配 revision 的 Node execution snapshot
- **THEN** 对应 NodeView MUST 显示 Running、Success、Failure、Stopping 或其它正式 debug 状态
- **AND** authoring Node 数据 MUST 不被修改

#### Scenario: 下钻运行中的 inline Graph

- **WHEN** 用户从 authoring Graph 下钻 StateMachine、State body、ConditionRuleGraph 或 TreeClip Graph
- **THEN** 页面栈 MUST 继续打开对应 authoring Graph
- **AND** overlay MUST 使用当前 Session 选中的 runtime child instance
- **AND** 页面栈 MUST NOT 保存 runtime object reference

#### Scenario: 旧 direct-state 高亮

- **WHEN** 新 diagnostics overlay 接管 NodeView runtime 状态
- **THEN** `BaseNodeView` 直接读取 `RunnableNode.State` 的旧高亮路径 MUST 删除
- **AND** 窗口 MUST NOT 保留两套节点运行状态来源
