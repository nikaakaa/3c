## 1. 输入绑定模块
- [x] 1.1 新增 InputAction 绑定模块，保存正式来源资产和稳定 action identity。
- [x] 1.2 绑定模块暴露 action 显示名、action map 显示名和来源资产引用。
- [x] 1.3 绑定模块实现 Taco 资产引用枚举，让节点右键引用动作可以定位来源资产。
- [x] 1.4 绑定模块在 action identity 缺失或来源资产缺失时报告配置错误。

## 2. 输入值节点
- [x] 2.1 新增 InputAction 值节点基类，继承 `ValueNode` 并组合输入绑定模块。
- [x] 2.2 新增 `InputActionButtonNode`，输出 `BoolPropertyPort`。
- [x] 2.3 新增 `InputActionFloatNode`，输出 `FloatPropertyPort`。
- [x] 2.4 新增 `InputActionVector2Node`，输出 `Vector2PropertyPort`。
- [x] 2.5 为输入节点设置清晰的 `NodePath` 和 `NodeName`。
- [x] 2.6 确认输入节点在 `StateMachineGraph` 中作为 `ValueNode` 可创建，但不能成为 Transition flow 端点。

## 3. 输入值读取边界
- [x] 3.1 定义正式 input value source 边界，用 action identity 读取 typed value。
- [x] 3.2 输入节点通过图执行上下文或用户对象获取 input value source。
- [x] 3.3 输入节点缺少 input value source 时报告错误并输出类型默认值。
- [x] 3.4 输入节点不得启用、禁用 action，也不得全局查找 `PlayerInput`。

## 4. 拖拽创建入口
- [x] 4.1 新增 InputAction 拖拽节点工厂，负责识别 `InputActionReference` 和 `InputActionAsset`。
- [x] 4.2 工厂将单个 `InputActionReference` 解析为一个 typed 输入节点。
- [x] 4.3 工厂将 `InputActionAsset` 中支持的 action 批量解析为 typed 输入节点。
- [x] 4.4 工厂对不支持的 action value type 报告原因，不创建 object fallback 节点。
- [x] 4.5 在 `BaseTreeView` 中接入 `DropArea.DragValid` 和 `DropArea.onDragPerformEvent`。
- [x] 4.6 拖拽创建必须调用 `BaseTreeView.CreateNode()`，不得直接写入图节点集合。
- [x] 4.7 批量创建节点时按 action map 和 action 顺序排布，避免节点重叠。

## 5. 编辑器集成收口
- [x] 5.1 节点搜索中可找到输入节点。
- [x] 5.2 节点面板能显示输入绑定模块字段。
- [x] 5.3 输入节点输出端口能连接到 Bool、Float、Vector2 对应属性输入。
- [x] 5.4 Bool 输入节点输出可被状态机 Transition 条件引用。
- [x] 5.5 拖入不合法对象时不修改图数据。

## 6. 工具校验
- [x] 6.1 运行 `openspec validate add-taco-input-action-value-nodes --strict --no-interactive`。
- [x] 6.2 检查 Unity 编辑器编译反馈并修复输入节点相关编译错误，不运行 Unity batchmode。
