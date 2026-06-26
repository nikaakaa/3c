# taco-graph-core Specification

## MODIFIED Requirements

### Requirement: BaseTree 保持编辑器资产入口
系统 MUST 保持 `BaseTree : BaseGraph`。现有 Taco 编辑器 UI 第一阶段 MUST 继续以 `BaseTree` 作为打开、显示、Inspector 和节点搜索入口。系统 MUST NOT 为本变更新增 `BaseGraphWindow`。节点搜索和脚本创建 MUST 继续通过当前 `BaseTree` 实例的 `CanCreateNodeType` 过滤。

#### Scenario: 状态机图创建边界
- **WHEN** 当前图是 `StateMachineGraph`
- **THEN** `CanCreateNodeType` MUST 能允许状态机控制节点、`StateNode` 和条件所需的 `ValueNode`
- **AND** `CanCreateNodeType` MUST 能拒绝 `StateMachineNode`、Taco 原生 `RootNode`、`OnEnter`、`OnExit` 和普通 runnable 行为节点

## ADDED Requirements

### Requirement: BaseGraph 创建节点尊重图类型规则
系统 MUST 让底层 `BaseGraph.CreateNode(Type)` 尊重当前图的 `CanCreateNodeType(Type)`。编辑器搜索、粘贴和脚本创建路径 MUST 使用同一条创建规则，避免不同入口创建出互相矛盾的图数据。

#### Scenario: 脚本创建非法节点
- **WHEN** 脚本路径尝试向 `StateMachineGraph` 创建 `StateMachineNode`
- **THEN** 创建逻辑 MUST 拒绝该节点
- **AND** 系统 MUST NOT 把该节点加入正式节点集合

#### Scenario: 编辑器创建合法节点
- **WHEN** 用户在 `StateMachineGraph` 中创建 `StateNode`
- **THEN** 创建逻辑 MUST 允许该节点
- **AND** 新节点 MUST 被加入同一套节点集合和 GUID 映射
