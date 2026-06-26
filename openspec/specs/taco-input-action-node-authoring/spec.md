# taco-input-action-node-authoring Specification

## Purpose
定义 Unity Input System 进入 Taco 图的正式创作链路：InputAction 输入以 `ValueNode` 表达，绑定数据放在 `NodeModule`，运行时只从正式 input value source 读取值，不新增输入专用 Graph、Workbench 路径或 object fallback。

## Requirements
### Requirement: InputAction 输入以 Taco ValueNode 表达
系统 MUST 使用 Taco `ValueNode` 表达 InputAction 的图内输入值来源。系统 MUST NOT 为 InputSystem 新增 `BaseTree` 子类、并行 graph window 或独立端口系统。

#### Scenario: 创建输入值节点
- **WHEN** 用户在 Taco 图中创建 InputAction 输入节点
- **THEN** 创建结果 MUST 是 `ValueNode`
- **AND** 该节点 MUST 通过现有 `PropertyPort` 输出输入值

### Requirement: InputAction 绑定使用节点模块和稳定身份
系统 MUST 使用 `NodeModule` 保存 InputAction 绑定数据。绑定模块 MUST 保存正式来源资产和稳定 action identity，MUST NOT 使用 action 显示名作为唯一持久化身份。

#### Scenario: action 被重命名
- **WHEN** 已绑定 action 的显示名发生变化
- **AND** action identity 没有变化
- **THEN** 输入节点 MUST 继续绑定同一 action
- **AND** 已有 Taco `PropertyEdge` 连接 MUST NOT 因显示名变化而断开

#### Scenario: 绑定来源缺失
- **WHEN** 输入节点保存的来源资产或 action identity 无法解析
- **THEN** 节点 MUST 报告配置错误
- **AND** 节点 MUST NOT 回退为字符串名查找

### Requirement: 输入节点使用现有 typed PropertyPort
系统 MUST 使用现有 Taco `PropertyPort` 类型输出 InputAction 值。第一阶段 MUST 支持 Bool/Button、Float 和 Vector2，MUST NOT 为不支持类型创建 object fallback 输出。

#### Scenario: 支持类型
- **WHEN** InputAction 表达 Button、Float 或 Vector2
- **THEN** 系统 MUST 创建对应 typed 输入节点
- **AND** 节点 MUST 分别通过 `BoolPropertyPort`、`FloatPropertyPort` 或 `Vector2PropertyPort` 输出值

#### Scenario: 不支持类型
- **WHEN** InputAction 的 value type 不是第一阶段支持类型
- **THEN** 系统 MUST 报告该 action 暂不支持
- **AND** 系统 MUST NOT 创建 object 类型输入节点

### Requirement: 输入节点只读取正式输入来源
InputAction 输入节点 MUST 只读取输入值。节点 MUST NOT 在求值时启用、禁用 action，也 MUST NOT 全局搜索 `PlayerInput` 或其它输入对象。输入生命周期和本地玩家输入来源 MUST 由正式 input value source 或图执行上下文提供。

#### Scenario: 正式 input value source 存在
- **WHEN** 图执行上下文提供 input value source
- **AND** 输入节点被请求输出值
- **THEN** 节点 MUST 使用绑定 action identity 从该 source 读取 typed value

#### Scenario: 缺少 input value source
- **WHEN** 图执行上下文没有提供 input value source
- **THEN** 节点 MUST 报告缺少输入来源
- **AND** 节点 MUST 输出该类型默认值
- **AND** 节点 MUST NOT 尝试全局 fallback

### Requirement: 拖拽创建和状态机条件复用正式链路
Taco 编辑器 MUST 支持从 `InputActionReference` 或 `InputActionAsset` 拖拽创建输入节点。拖拽创建 MUST 复用 `BaseTreeView.CreateNode()` 和当前图 `CanCreateNodeType()`。InputAction Bool 输入节点 MUST 能作为 `StateMachineGraph` Transition 条件来源。

#### Scenario: 拖拽创建
- **WHEN** 用户把 InputSystem 资产拖入 Taco 图
- **THEN** 编辑器 MUST 为支持的 action 创建对应 typed 输入节点
- **AND** 创建过程 MUST 通过当前图的节点创建规则
- **AND** 不支持的 action MUST 被报告且不得创建 fallback 节点

#### Scenario: Transition 条件
- **WHEN** 用户把 InputAction Bool 节点输出设为 Transition 条件
- **THEN** 状态机 runtime MUST 通过现有条件读取路径请求该节点输出值
- **AND** 输入节点 MUST NOT 成为合法 Transition flow 端点
