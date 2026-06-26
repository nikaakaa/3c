# Design

## Context

当前 Taco authoring 已经形成几个边界：

- `BaseGraph` 承载图结构，`BaseTree` 是 Unity 资产和编辑器入口。
- `BaseNode` 可以拥有 `NodeModule`，模块字段会被 `NodeFieldAccessor` 扫描。
- `PropertyPort` / `PropertyEdge` 是唯一属性端口系统。
- `StateMachineGraph` 只表达状态结构，但允许 `ValueNode` 作为 Transition 条件计算节点。
- `BaseTreeView` 已经初始化 `DropArea`，但还没有把 Unity object drag 转成节点创建。

InputAction 节点应进入这条主链路，而不是创建 Input 专用树或恢复旧配置路径。

## Decision: 使用 ValueNode + NodeModule

InputAction 输入节点 SHALL 是 `ValueNode`，输入绑定 SHALL 是 `NodeModule`。

业务取舍：

- 这让输入在状态机图和行为图中都表现为“值来源”，符合输入驱动状态/动作决策的展示目标。
- 组件式模块能复用资产引用、绑定显示、action identity 和后续 provider 读取逻辑，不需要每个节点重复一套字段。
- 不继承 `BaseTree` 可以避免把 InputSystem 资产包装成另一种图资产；`.inputactions` 仍由 Unity Input System 负责，Taco 只引用它并把其中 action 映射成节点。
- 代价是会新增几个小节点类型和一个绑定模块，但这是正式模块化成本，不是分裂路径。

## Decision: 绑定使用稳定 action identity

输入绑定模块 SHALL 保存正式来源资产和稳定 action identity。拖入 `InputActionReference` 时，从 reference 解析出同一个 action 绑定；拖入 `InputActionAsset` 时，从 asset 内的 action 生成绑定。

业务取舍：

- action 显示名适合节点标题，但不适合持久化身份；设计时重命名 `Move`、`Look` 不应破坏图节点绑定。
- 不要求拖入 `InputActionAsset` 时额外生成隐藏 `InputActionReference` 资产，避免项目里多出不可见的临时引用资产。
- 如果 action identity 无法解析，创建应失败并报告原因，而不是退回到字符串名 fallback。

## Decision: 第一阶段提供显式类型节点

第一阶段节点族：

- `InputActionButtonNode` 输出 `BoolPropertyPort`
- `InputActionFloatNode` 输出 `FloatPropertyPort`
- `InputActionVector2Node` 输出 `Vector2PropertyPort`

业务取舍：

- 三个显式节点覆盖第三人称动作 demo 的第一批输入：移动、视角、攻击/闪避/跳跃、扳机或轴值。
- 显式节点比一个动态多端口大节点更容易读图，也更适合求职 demo 展示输入如何驱动状态。
- 动态端口节点虽然拖一次更紧凑，但会增加 `List<PropertyPort>` UI、端口重建、批量重命名和连接恢复复杂度；当前项目已经把动态端口 UI 列为待收口问题，不应把第一版输入能力押在这里。
- 不支持的 value type 不创建 object 节点，因为 object fallback 会模糊业务语义并削弱端口类型安全。

## Decision: 拖拽只是创建正式节点

编辑器 SHALL 在 `BaseTreeView` 里使用现有 `DropArea` 接收 Unity object drag。

拖拽流程：

1. `DragValid` 检查 objectReferences 是否包含 `InputActionReference` 或 `InputActionAsset`。
2. `DragPerform` 把拖入对象交给输入节点工厂。
3. 工厂解析 action 类型，选择对应节点类型。
4. 工厂通过 `BaseTreeView.CreateNode(type, position)` 创建节点。
5. 创建成功后写入节点模块绑定。
6. 批量创建时用稳定间距排布节点。

业务取舍：

- 复用 `BaseTreeView.CreateNode()` 可以自然继承 Undo、dirty、节点视图创建和 `CanCreateNodeType()` 规则。
- 不从拖拽处理器直接写 `m_Nodes`，避免形成编辑器旁路。
- 不新增独立 input graph window，避免把用户从 Taco 主编辑器带到另一套创作体验。

## Decision: 输入读取由正式 provider 承担生命周期

InputAction 值节点 SHALL 只读取值，不负责启用/禁用 action，也不全局搜索 `PlayerInput`。

运行时边界：

- 图执行上下文或用户对象提供正式 input value source。
- 节点通过绑定模块的 action identity 向该 source 请求 typed value。
- 缺少 source 或 action 解析失败时，节点必须报告配置错误并输出类型默认值，且不得尝试全局 fallback。

业务取舍：

- 这样可以兼容未来本地玩家、AI、录制回放、网络预测输入等不同输入来源，而节点 authoring 不需要改变。
- 把 action lifecycle 留给正式 provider，避免一个 `ValueNode` 在条件计算时产生隐藏副作用。
- 代价是实现时需要补一个很薄的 input value source 边界，但这是后续 gameplay 输入链路会用到的正式边界，不是临时桥。

## Rejected Alternatives

### 直接继承树

拒绝。InputSystem 不是新的 Taco 图类型，输入 action 是图内值来源。继承 `BaseTree` 会让 `.inputactions`、Taco graph 和输入运行时形成多套 authoring 入口，不符合当前统一链路。

### 一个 InputActionAsset 巨型动态多端口节点

暂不采用。它会让大 asset 的所有 action 堆在一个节点上，图上难以表达“这个条件来自哪个输入意图”。动态端口也会放大当前尚未收口的列表端口 UI 复杂度。

### 字符串 action 名 fallback

拒绝。名字适合显示，不适合持久化引用。重命名 action 后节点必须能继续指向同一 action，否则输入图会变成隐性坏数据。

### 节点内直接 Enable action

拒绝。条件节点可能在一帧内被多次求值，`Enable()`/`Disable()` 属于输入生命周期，不属于值节点求值职责。
