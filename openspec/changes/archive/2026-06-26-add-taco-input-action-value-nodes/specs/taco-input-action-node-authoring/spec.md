# taco-input-action-node-authoring Specification

## ADDED Requirements

### Requirement: InputAction 输入以 Taco ValueNode 表达
系统 MUST 使用 Taco `ValueNode` 表达 Unity Input System action 的图内输入值来源。系统 MUST NOT 为 InputSystem 新增 `BaseTree` 子类、并行 graph window、Workbench 路径或独立端口系统。

#### Scenario: 创建输入值节点
- **WHEN** 用户在 Taco 图中创建 InputAction 输入节点
- **THEN** 创建结果 MUST 是 `ValueNode`
- **AND** 该节点 MUST 通过现有 `PropertyPort` 输出输入值
- **AND** 系统 MUST NOT 创建 InputSystem 专用 Tree 资产

#### Scenario: 状态机图使用输入条件
- **WHEN** 用户在 `StateMachineGraph` 中创建 InputAction Bool 输入节点
- **THEN** 该节点 MUST 作为条件用 `ValueNode` 存在于同层状态机图
- **AND** 该节点 MUST NOT 成为 Transition flow 起点或终点

### Requirement: InputAction 绑定使用节点模块
系统 MUST 使用 `NodeModule` 保存 InputAction 绑定数据。绑定模块 MUST 保存正式来源资产和稳定 action identity。绑定模块 MUST NOT 使用 action 显示名作为唯一持久化身份。

#### Scenario: 模块保存绑定
- **WHEN** 输入节点绑定到一个 InputAction
- **THEN** 节点 MUST 通过序列化 `NodeModule` 保存该绑定
- **AND** 绑定 MUST 能在 Tree 重载后重新解析到同一 action

#### Scenario: action 被重命名
- **WHEN** 已绑定 action 的显示名发生变化
- **AND** action identity 没有变化
- **THEN** 输入节点 MUST 继续绑定同一 action
- **AND** 已有 Taco `PropertyEdge` 连接 MUST NOT 因显示名变化而断开

#### Scenario: 绑定来源缺失
- **WHEN** 输入节点保存的来源资产或 action identity 无法解析
- **THEN** 节点 MUST 报告配置错误
- **AND** 节点 MUST NOT 回退为字符串名查找

### Requirement: InputAction 节点输出使用现有 PropertyPort
系统 MUST 使用现有 Taco `PropertyPort` 类型输出 InputAction 值。第一阶段 MUST 支持 Bool/Button、Float 和 Vector2 三类输出。系统 MUST NOT 为不支持的 action value type 创建 object fallback 输出。

#### Scenario: Button action
- **WHEN** InputAction 表达 button 或 pressed 输入
- **THEN** 系统 MUST 创建 Bool 输入节点
- **AND** 该节点 MUST 通过 `BoolPropertyPort` 输出当前按下状态

#### Scenario: Float action
- **WHEN** InputAction 表达 float 输入
- **THEN** 系统 MUST 创建 Float 输入节点
- **AND** 该节点 MUST 通过 `FloatPropertyPort` 输出当前数值

#### Scenario: Vector2 action
- **WHEN** InputAction 表达 Vector2 输入
- **THEN** 系统 MUST 创建 Vector2 输入节点
- **AND** 该节点 MUST 通过 `Vector2PropertyPort` 输出当前向量

#### Scenario: Unsupported action type
- **WHEN** InputAction 的 value type 不是第一阶段支持类型
- **THEN** 系统 MUST 报告该 action 暂不支持
- **AND** 系统 MUST NOT 创建 object 类型输入节点

### Requirement: 拖拽 InputSystem 资产创建正式节点
Taco 编辑器 MUST 支持从 Unity object drag 创建 InputAction 输入节点。拖拽创建 MUST 复用当前 `BaseTreeView.CreateNode()` 和当前图的 `CanCreateNodeType()` 规则。拖拽处理器 MUST NOT 直接写入图节点集合。

#### Scenario: 拖入 InputActionReference
- **WHEN** 用户把 `InputActionReference` 拖入 Taco 图
- **THEN** 编辑器 MUST 根据该 action 类型创建一个对应 typed 输入节点
- **AND** 新节点 MUST 保存该 action 的正式绑定
- **AND** 创建过程 MUST 通过当前图的节点创建规则

#### Scenario: 拖入 InputActionAsset
- **WHEN** 用户把 `InputActionAsset` 拖入 Taco 图
- **THEN** 编辑器 MUST 为其中第一阶段支持的 action 创建对应 typed 输入节点
- **AND** 节点 MUST 按 action map 和 action 顺序稳定排布
- **AND** 不支持的 action MUST 被报告且不得创建 fallback 节点

#### Scenario: 当前图拒绝输入节点
- **WHEN** 当前图的 `CanCreateNodeType()` 拒绝目标输入节点类型
- **THEN** 拖拽创建 MUST 失败
- **AND** 图数据 MUST 保持不变

### Requirement: 输入节点读取值不拥有输入生命周期
InputAction 输入节点 MUST 只读取输入值。节点 MUST NOT 在求值时启用、禁用 action，也 MUST NOT 全局搜索 `PlayerInput` 或其它输入对象。输入生命周期和本地玩家输入来源 MUST 由正式 input value source 或图执行上下文提供。

#### Scenario: 正式 input value source 存在
- **WHEN** 图执行上下文提供正式 input value source
- **AND** 输入节点被请求输出值
- **THEN** 节点 MUST 使用绑定的 action identity 从该 source 读取 typed value
- **AND** 节点 MUST 将结果写入自己的输出 `PropertyPort`

#### Scenario: 缺少 input value source
- **WHEN** 输入节点被请求输出值
- **AND** 图执行上下文没有提供正式 input value source
- **THEN** 节点 MUST 报告缺少输入来源
- **AND** 节点 MUST 输出该类型默认值
- **AND** 节点 MUST NOT 尝试全局 fallback

### Requirement: 输入节点可作为状态机 Transition 条件来源
InputAction Bool 输入节点 MUST 能作为 `StateMachineGraph` Transition 条件来源。系统 MUST 继续通过现有 Transition 条件节点 GUID 和条件端口 ID 读取 Bool 输出，不得新增 TransitionNode 或并行条件系统。

#### Scenario: AttackPressed 驱动 Attack Transition
- **WHEN** 用户创建 `InputActionButtonNode` 并绑定 Attack action
- **AND** 用户把该节点的 Bool 输出设为 Idle 到 Attack 的 Transition 条件
- **THEN** 状态机 runtime MUST 通过现有条件读取路径请求该节点输出值
- **AND** 当 Bool 输出为 true 时该 Transition MUST 可以通过

#### Scenario: 输入节点不是 Transition 端点
- **WHEN** 用户在 `StateMachineGraph` 中连接 Transition flow
- **THEN** 输入节点 MUST NOT 出现在合法 Transition flow 端点候选中
- **AND** 输入节点的 `PropertyPort` 值连接能力 MUST 不受影响
