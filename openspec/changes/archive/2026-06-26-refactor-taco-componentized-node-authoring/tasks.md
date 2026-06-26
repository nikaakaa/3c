## 1. 清理前置

- [x] 1.1 确认 `Assets/Scripts/Workbench` 不存在。
- [x] 1.2 确认代码中不存在 `WorkbenchPortDescriptor`。
- [x] 1.3 确认代码中不存在 `WorkbenchTypeRegistry`。
- [x] 1.4 确认代码中不存在 `OpenWorkbenchTreeWindow`。
- [x] 1.5 确认本变更不新增 Workbench 并列路径。

## 2. 字段访问器模型

- [x] 2.1 新增 Taco 原生 `NodeFieldAccessor`。
- [x] 2.2 `NodeFieldAccessor` 保存所属节点。
- [x] 2.3 `NodeFieldAccessor` 保存目标对象。
- [x] 2.4 `NodeFieldAccessor` 保存字段信息。
- [x] 2.5 `NodeFieldAccessor` 保存字段键。
- [x] 2.6 `NodeFieldAccessor` 保存序列化属性路径。
- [x] 2.7 `NodeFieldAccessor` 保存模块 ID。
- [x] 2.8 `NodeFieldAccessor` 提供读取字段值接口。
- [x] 2.9 `NodeFieldAccessor` 提供写入字段值接口。
- [x] 2.10 `NodeFieldAccessor` 提供读取特性接口。
- [x] 2.11 `NodeFieldAccessor` 提供属性端口类型判断接口。

## 3. BaseNode 字段扫描入口

- [x] 3.1 给 `BaseNode` 新增 `GetFieldAccessors()`。
- [x] 3.2 `GetFieldAccessors()` 默认返回节点自身字段。
- [x] 3.3 `GetFieldAccessors()` 为节点字段生成节点级序列化属性路径。
- [x] 3.4 保留 `GetAllFields()` 仅作为低层兼容工具。
- [x] 3.5 禁止新编辑器/运行时主链路继续直接遍历 `GetAllFields()`。

## 4. NodeModule 模型

- [x] 4.1 新增 Taco 原生 `NodeModule` 基类或接口。
- [x] 4.2 给 `BaseNode` 增加 `[SerializeReference]` 模块列表。
- [x] 4.3 给 `BaseNode` 提供只读模块访问。
- [x] 4.4 给 `BaseNode` 提供默认模块创建入口。
- [x] 4.5 给 `BaseNode` 提供模块初始化入口。
- [x] 4.6 给模块分配稳定模块 ID。
- [x] 4.7 模块字段进入 `GetFieldAccessors()`。
- [x] 4.8 模块字段生成模块级序列化属性路径。
- [x] 4.9 节点字段和模块字段允许共存。

## 5. BaseNode 初始化链路

- [x] 5.1 改造 `BaseNode.BeforeInit()` 使用 `GetFieldAccessors()`。
- [x] 5.2 `BeforeInit()` 从字段访问器读取属性端口。
- [x] 5.3 `BeforeInit()` 用端口 ID 建立 `PropertyPortMap`。
- [x] 5.4 `BeforeInit()` 处理 `List<PropertyPort>` 字段。
- [x] 5.5 `BeforeInit()` 检测重复端口 ID。
- [x] 5.6 改造 `BaseNode.Init()` 初始化字段访问器发现的属性端口。
- [x] 5.7 改造 `BaseNode.AfterInit()` 使用端口 ID 映射。
- [x] 5.8 改造 `BaseNode.Dispose()` 释放字段访问器发现的属性端口。
- [x] 5.9 改造 `BaseNode.OnAfterDeserialize()` 处理字段访问器发现的属性端口。
- [x] 5.10 改造 `BaseNode.IsConnected()` 使用端口 ID。

## 6. PropertyPort 身份

- [x] 6.1 给 `PropertyPort` 新增 `PortId`。
- [x] 6.2 给 `PropertyPort` 新增 `DisplayName`。
- [x] 6.3 给 `PropertyPort` 新增 `OwnerModuleId`。
- [x] 6.4 给 `PropertyPort` 新增 `FieldKey`。
- [x] 6.5 定义默认端口 ID 生成规则。
- [x] 6.6 节点字段端口 ID 使用节点作用域。
- [x] 6.7 模块字段端口 ID 使用模块作用域。
- [x] 6.8 `PropertyPort.Name` 不再作为持久连接身份。
- [x] 6.9 端口视图显示文本使用 `DisplayName` 或特性标签。

## 7. PropertyEdge 链接协议

- [x] 7.1 改造 `PropertyEdge` 保存起点端口 ID。
- [x] 7.2 改造 `PropertyEdge` 保存终点端口 ID。
- [x] 7.3 改造 `PropertyEdge.Init()` 通过端口 ID 恢复起点端口。
- [x] 7.4 改造 `PropertyEdge.Init()` 通过端口 ID 恢复终点端口。
- [x] 7.5 改造 `BaseTree.LinkProperty()` 用端口 ID 去重。
- [x] 7.6 改造 `BaseTree.LinkProperty()` 创建基于端口 ID 的属性边。
- [x] 7.7 改造 unlink 回调保持原有调用顺序。
- [x] 7.8 不保留字段名边兜底兼容。

## 8. 动态属性端口 API

- [x] 8.1 改造 `BaseNode.SetPropertyPort()` 使用字段访问器。
- [x] 8.2 改造 `BaseNode.SetPropertyPort()` 更新端口 ID。
- [x] 8.3 改造 `BaseNode.AddPropertyPort()` 支持模块列表字段。
- [x] 8.4 改造 `BaseNode.AddPropertyPort()` 使用端口 ID 写入映射。
- [x] 8.5 改造 `BaseNode.RemovePropertyPort()` 使用端口 ID 从映射移除。
- [x] 8.6 检查 `ForNode` 动态端口逻辑是否继续使用字段名判断。
- [x] 8.7 检查 `ToListNode` 动态端口逻辑是否继续使用字段名判断。
- [x] 8.8 将动态端口回调内部查找统一到端口 ID 或字段键。

## 9. BaseNodeView 端口 UI

- [x] 9.1 改造 `BaseNodeView.GeneratePropertyPorts()` 使用字段访问器。
- [x] 9.2 `GeneratePropertyPorts()` 从字段访问器读取属性端口特性。
- [x] 9.3 `GeneratePropertyPorts()` 从字段访问器读取属性端口实例。
- [x] 9.4 `GeneratePropertyPorts()` 用端口 ID 注册属性端口视图。
- [x] 9.5 改造 `BaseNodeView.RefreshPropertyPorts()` 使用字段访问器。
- [x] 9.6 `RefreshPropertyPorts()` 用端口 ID 判断已存在端口。
- [x] 9.7 `RefreshPropertyPorts()` 隐藏/显示时不依赖字段名。

## 10. NodePanelView 字段 UI

- [x] 10.1 改造 `NodePanelView.Refresh()` 使用字段访问器。
- [x] 10.2 属性端口字段从字段访问器读取序列化属性。
- [x] 10.3 面板显示字段从字段访问器读取序列化属性。
- [x] 10.4 枚举菜单字段从字段访问器读取和写入目标对象。
- [x] 10.5 开关字段从字段访问器读取和写入目标对象。
- [x] 10.6 值变化回调用字段目标对象执行。
- [x] 10.7 字段映射键切换为字段访问器字段键。
- [x] 10.8 属性端口字段启用状态使用端口 ID。
- [x] 10.9 改造 `NodePanelView.Rebind()` 使用字段访问器序列化属性路径。
- [x] 10.10 改造 `AddBaseField()` 支持字段访问器。
- [x] 10.11 改造 `AddPropertyPortField()` 使用端口 ID 作为 UI 键。

## 11. NodeInputFieldContainerView 字段 UI

- [x] 11.1 改造 `NodeInputFieldContainerView.Refresh()` 使用字段访问器。
- [x] 11.2 输入属性端口字段查找使用端口 ID。
- [x] 11.3 输入属性端口默认值绑定使用字段访问器序列化属性。
- [x] 11.4 空字段容器键使用端口 ID。
- [x] 11.5 改造 `NodeInputFieldContainerView.Rebind()` 使用字段访问器序列化属性路径。
- [x] 11.6 改造 `SetPropertyPortFieldEnable()` 使用端口 ID。
- [x] 11.7 排序逻辑使用属性端口视图的端口 ID 顺序。

## 12. NodePortContainerView

- [x] 12.1 属性端口视图映射键切换为端口 ID。
- [x] 12.2 `AddPropertyPort()` 使用端口 ID 注册。
- [x] 12.3 `RemovePropertyPort()` 使用端口 ID 移除。
- [x] 12.4 `AddVariablePropertyPort()` 使用端口 ID 注册。
- [x] 12.5 拖拽排序写回属性端口索引。
- [x] 12.6 排序时不依赖字段名。

## 13. 统一节点创建范围

- [x] 13.1 梳理当前 `AcceptableNodePaths` 对节点创建的限制。
- [x] 13.2 明确路径只作为显示分类。
- [x] 13.3 允许同一创作图创建普通逻辑节点。
- [x] 13.4 允许同一创作图创建值节点。
- [x] 13.5 允许同一创作图创建 Timeline 引用节点。
- [x] 13.6 允许同一创作图创建 StateMachine 节点。
- [x] 13.7 允许同一创作图创建 Tree 引用节点。
- [x] 13.8 节点创建不依赖 Workbench 图类型。

## 14. 嵌套节点模型

- [x] 14.1 新增 Timeline 引用模块。
- [x] 14.2 新增带作用域的 Graph 引用模块。
- [x] 14.3 新增 Tree 引用模块。
- [x] 14.4 删除 StateMachine 专名 Graph 模块，避免假装已有正式 StateMachineGraph 类型。
- [x] 14.5 删除流程/值端口模块，不给引用节点生成默认假值端口。
- [x] 14.6 Timeline 引用节点组合 Timeline 引用模块。
- [x] 14.7 StateMachine 节点组合带作用域的 Graph 引用模块。
- [x] 14.8 Tree 引用节点组合 Tree 引用模块。
- [x] 14.9 嵌套引用字段显示在 Taco 原面板链路。
- [x] 14.10 嵌套引用字段不通过新端口描述符表达。

## 15. 下钻与作用域

- [x] 15.1 为含子 Graph 的节点提供打开子图命令。
- [x] 15.2 打开子图命令读取模块字段。
- [x] 15.3 子图打开不依赖专用 Workbench 窗口。
- [x] 15.4 定义 Graph 作用域 ID。
- [x] 15.5 StateMachine 节点拥有自己的作用域。
- [x] 15.6 Timeline 引用节点不把 Timeline 轨道编辑复制到 Graph 视图。
- [x] 15.7 Tree 引用节点打开被引用 Tree。

## 16. 嵌套图校验

- [x] 16.1 新增嵌套引用收集入口。
- [x] 16.2 收集节点字段内的 Tree 引用。
- [x] 16.3 收集模块字段内的 Tree 引用。
- [x] 16.4 检测 Tree 引用循环。
- [x] 16.5 检测缺失子 Graph 引用。
- [x] 16.6 检测跨作用域边。
- [x] 16.7 检测属性边指向不存在的端口 ID。

## 17. 删除分裂路径约束

- [x] 17.1 不新增 `WorkbenchPortDescriptor`。
- [x] 17.2 不新增 `WorkbenchFieldAccessor`。
- [x] 17.3 不新增 `WorkbenchTypeRegistry`。
- [x] 17.4 不新增 `WorkbenchTree`。
- [x] 17.5 不新增 Workbench 编辑器窗口。
- [x] 17.6 不新增并列节点搜索窗口。
- [x] 17.7 不新增兜底兼容配置。

## 18. 工具校验

- [x] 18.1 运行 `openspec validate refactor-taco-componentized-node-authoring --strict --no-interactive`。
- [x] 18.2 用 `rg` 确认代码中没有 `WorkbenchPortDescriptor`。
- [x] 18.3 用 `rg` 确认代码中没有 `WorkbenchTypeRegistry`。
- [x] 18.4 用 `rg` 确认代码中没有 `OpenWorkbenchTreeWindow`。
- [x] 18.5 用 `rg` 确认新的属性端口链接入口使用端口 ID。
