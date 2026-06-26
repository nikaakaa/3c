# taco-input-action-node-authoring Specification

## MODIFIED Requirements

### Requirement: 拖拽创建和状态机条件复用正式链路
Taco 编辑器 MUST 支持从 `InputActionReference` 或 `InputActionAsset` 拖拽创建输入节点。拖拽创建 MUST 复用 `BaseTreeView.CreateNode()` 和当前图 `CanCreateNodeType()`。InputAction Bool 输入节点 MUST 能在 `TransitionRuleGraph` 中作为 Transition 条件输入来源，MUST NOT 直接作为 `StateMachineGraph` 同层 Transition 条件字段。

#### Scenario: 拖拽创建
- **WHEN** 用户把 InputSystem 资产拖入 Taco 图
- **THEN** 编辑器 MUST 为支持的 action 创建对应 typed 输入节点
- **AND** 创建过程 MUST 通过当前图的节点创建规则
- **AND** 不支持的 action MUST 被报告且不得创建 fallback 节点

#### Scenario: Transition 条件
- **WHEN** 用户把 InputAction Bool 用作 Transition 条件
- **THEN** 用户 MUST 在该 Transition 的 `TransitionRuleGraph` 中创建或引用 InputAction Bool 输入节点
- **AND** 状态机 runtime MUST 通过规则图求值路径读取该输入节点
- **AND** 输入节点 MUST NOT 成为 `StateMachineGraph` 本层合法 Transition flow 端点
