## ADDED Requirements

### Requirement: Taco 字段访问器扫描
Taco SHALL（必须）通过单一字段访问器流暴露节点创作字段。该访问器流必须能表达直接声明在 `BaseNode` 上的字段，也必须能表达序列化节点模块内部声明的字段。

#### Scenario: 节点字段保持可发现
- **当** 节点直接声明一个 `PropertyPort` 字段
- **并且** Taco 初始化或刷新该节点
- **则** 字段访问器流必须暴露该字段
- **并且** 该字段的目标对象必须是节点本身

#### Scenario: 模块字段变为可发现
- **当** 节点拥有一个声明了 `PropertyPort` 字段的序列化模块
- **并且** Taco 初始化或刷新该节点
- **则** 字段访问器流必须暴露该模块字段
- **并且** 该字段的目标对象必须是模块本身

### Requirement: Taco 节点模块
Taco SHALL（必须）允许 `BaseNode` 拥有序列化节点模块。模块可以贡献可编辑字段、属性端口、Graph 引用和创作元数据，而不要求每种能力都新增一条节点继承分支。

#### Scenario: 节点组合多个创作能力
- **当** 节点拥有图引用模块等序列化创作模块
- **并且** 节点显示在 Taco 编辑器中
- **则** 这些模块必须通过同一个 Taco 节点视图贡献自己的字段和端口

#### Scenario: 模块字段使用正式序列化路径
- **当** 模块字段显示在编辑器面板中
- **并且** Unity 绑定该字段
- **则** 绑定必须使用模块字段的序列化属性路径
- **并且** 绑定不得假设字段直接存在于节点上

### Requirement: 稳定 PropertyPort 身份
Taco SHALL（必须）通过稳定端口 ID 识别属性端口，用于持久化、映射查找和边恢复。显示文本必须与连接身份分离。

#### Scenario: 不同模块中存在同名字段
- **当** 同一个节点上的两个模块各自声明名为 `m_Input` 的字段
- **并且** Taco 构建属性端口映射
- **则** 每个端口必须拥有不同的稳定端口 ID

#### Scenario: 显示文本变化不破坏连接
- **当** 属性端口的显示文本发生变化
- **并且** Graph 被重新加载
- **则** 已有属性边必须仍然通过稳定端口 ID 恢复

### Requirement: PropertyEdge 使用 PortId
Taco SHALL（必须）使用起点和终点属性端口 ID 持久化并恢复属性边，而不是使用字段名或显示名。

#### Scenario: Graph 重载后恢复边
- **当** 一条属性边连接两个属性端口
- **并且** Tree 资产被重新加载
- **则** `PropertyEdge.Init()` 必须通过每个节点的属性端口 ID 映射恢复起点和终点端口

#### Scenario: 拒绝缺失端口 ID
- **当** 属性边引用了节点上已经不存在的端口 ID
- **并且** Taco 验证或初始化该 Tree
- **则** 该边必须被视为非法
- **并且** 该边必须由现有清理路径移除

### Requirement: 编辑器 UI 使用字段访问器
Taco 编辑器视图 SHALL（必须）从字段访问器生成属性端口、节点面板和输入默认值字段，而不是直接从节点实例读取字段。

#### Scenario: 模块属性端口出现在节点视图
- **当** 模块贡献一个输入属性端口
- **并且** 节点视图被创建
- **则** 该输入端口必须出现在标准 Taco 节点端口容器中

#### Scenario: 模块默认值出现在输入字段容器
- **当** 模块内部的输入属性端口拥有可编辑默认值
- **并且** 该输入端口未连接
- **则** 标准 Taco 输入字段容器必须显示该模块字段值

#### Scenario: 模块面板字段出现在节点面板
- **当** 模块字段被标记为需要在面板显示
- **并且** 节点面板刷新
- **则** 标准 Taco 节点面板必须显示并绑定该模块字段

### Requirement: Taco 原生端口系统保持唯一
Taco SHALL（必须）保持 `PropertyPort` 和 `PropertyEdge` 作为唯一属性端口系统，并且 SHALL NOT（不得）引入 Workbench 专用的并行端口描述符、类型注册表或边协议。

#### Scenario: 新模块贡献端口
- **当** 新节点模块需要一个值输入
- **并且** 该模块声明该值输入
- **则** 它必须声明 Taco `PropertyPort` 字段
- **并且** 它不得声明独立描述符对象

#### Scenario: 不存在并行端口描述符
- **当** 组件化节点创作变更已经实现
- **并且** 代码库被搜索 Workbench 端口描述符类
- **则** 不得存在 Workbench 专用端口描述符路径

### Requirement: 统一嵌套节点创作
Taco SHALL（必须）允许 Timeline 引用节点、StateMachine 节点、Tree 引用节点、值节点和普通逻辑节点在同一个创作图中创建，前提是该 Tree 用于组件化节点创作。

#### Scenario: Timeline 和 StateMachine 节点处于同一创作图
- **当** 创作图被打开
- **并且** 用户调用节点搜索
- **则** Timeline 引用节点和 StateMachine 节点必须都可以在该创作图中创建

#### Scenario: Tree 引用节点处于同一 Graph
- **当** 创作图被打开
- **并且** 用户创建 Tree 引用节点
- **则** 该节点必须通过普通 Taco 字段或模块字段保存被引用 Tree

### Requirement: 嵌套 Graph 语义不是端口语义
Taco SHALL（必须）把 Graph 嵌套、下钻命令、Graph 作用域和循环验证视为节点/模块创作语义，而不是 `PropertyPort` 的职责。

#### Scenario: 打开子 Graph
- **当** 节点拥有子 Graph 引用字段
- **并且** 用户调用打开子 Graph 命令
- **则** Taco 必须从该字段打开被引用 Tree
- **并且** Taco 不得通过属性端口连接来寻找下钻目标

#### Scenario: 循环检测
- **当** Tree A 通过嵌套 Graph 字段引用 Tree B
- **并且** Tree B 通过嵌套 Graph 字段引用 Tree A
- **并且** Taco 验证嵌套 Graph 引用
- **则** 该循环必须被报告为非法

### Requirement: 创建路径不依赖 Workbench
Taco 组件化创作 SHALL NOT（不得）要求 `WorkbenchTree`、`WorkbenchNode`、`WorkbenchPortDescriptor` 或 `OpenWorkbenchTreeWindow` 来创建、显示、连接或嵌套组件化 Taco 节点。

#### Scenario: 仅通过 Taco 创建组件化节点
- **当** Workbench 代码路径不存在
- **并且** 组件化 Taco 节点被创建
- **则** 该节点必须通过 Taco `BaseTree` / `BaseNode` 创作基础设施创建

#### Scenario: 仅通过 Taco 连接属性端口
- **当** Workbench 代码路径不存在
- **并且** 两个组件化 Taco 属性端口被连接
- **则** 该连接必须保存为 Taco `PropertyEdge`
