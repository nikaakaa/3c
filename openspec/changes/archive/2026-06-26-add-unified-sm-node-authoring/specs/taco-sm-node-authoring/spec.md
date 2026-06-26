## ADDED Requirements

### Requirement: StateMachineNode 是唯一普通状态节点
系统 MUST 使用 `StateMachineNode` 表达状态机图中的普通状态，以及父级行为图进入下一层 Graph 的递归边界。系统 MUST NOT 使用 Locomotion、Idle、Walk、Timeline State、Animation State 等业务特化节点表达普通状态。

#### Scenario: 在父级行为图中创建 Locomotion
- **WHEN** 用户在父级行为图中创建 Locomotion 状态机入口
- **THEN** 创建结果 MUST 是 `StateMachineNode`
- **AND** 该节点 MUST 通过图引用模块持有下一层 Graph
- **AND** 该节点 MUST NOT 携带 `Root`、`Enter`、`AnyState` 或 `Exit` 控制模块

#### Scenario: 在状态机图中创建 Idle 和 Walk
- **WHEN** 用户在 Locomotion 的 `StateMachineGraph` 中创建 Idle 和 Walk
- **THEN** Idle 和 Walk MUST 都是 `StateMachineNode`
- **AND** Idle 和 Walk MUST 通过各自的图引用模块持有下一层 Graph
- **AND** 系统 MUST NOT 根据 Idle 或 Walk 的业务内容替换 C# 节点类型

#### Scenario: 普通状态不能变成控制节点
- **WHEN** 用户选中普通 `StateMachineNode`
- **THEN** 编辑器 MUST NOT 提供把它切换为 `Root`、`Enter`、`AnyState` 或 `Exit` 的角色菜单
- **AND** 系统 MUST NOT 把控制语义作为模块挂载到普通状态节点上

### Requirement: 状态机控制节点
系统 MUST 使用 `StateMachineControlNode` 基类及其具体控制节点表达 `Root`、`Enter`、`AnyState` 和 `Exit`。`Root` MUST 是行为树进入当前层 `StateMachineGraph` 的入口源，但 MUST NOT 复用 Taco `RootNode`，也 MUST NOT 作为普通状态被 tick。`Enter` MUST 是父级状态下钻进入下一层 `StateMachineGraph` 的入口源，不能兼任图 root，也 MUST NOT 从当前层 `Root` 接收入边。控制节点 MUST 只能存在于 `StateMachineGraph` 中，普通 `BaseTree` MUST NOT 创建状态机控制节点。

#### Scenario: 创建 Root
- **WHEN** 用户在 `StateMachineGraph` 中创建 `Root`
- **THEN** 系统 MUST 创建 `StateMachineRootNode`
- **AND** 该节点 MUST 继承或接入 `StateMachineControlNode` 控制节点基类
- **AND** 该节点 MUST NOT 是普通 `StateMachineNode`
- **AND** 该节点 MUST 作为行为树进入当前层状态机图的入口源

#### Scenario: 创建 Enter
- **WHEN** 用户在 `StateMachineGraph` 中创建 `Enter`
- **THEN** 系统 MUST 创建 `StateMachineEnterNode`
- **AND** 该节点 MUST 继承或接入 `StateMachineControlNode` 控制节点基类
- **AND** 该节点 MUST NOT 是普通 `StateMachineNode`
- **AND** 该节点 MUST 作为父级状态下钻进入下一层状态机图的入口源
- **AND** 该节点 MUST NOT 暴露 input port

#### Scenario: 创建 AnyState
- **WHEN** 用户在 `StateMachineGraph` 中创建 `AnyState`
- **THEN** 系统 MUST 创建 `StateMachineAnyStateNode`
- **AND** 该节点 MUST 继承或接入 `StateMachineControlNode` 控制节点基类
- **AND** 该节点 MUST NOT 被父级行为图 tick 为普通状态

#### Scenario: 创建 Exit
- **WHEN** 用户在 `StateMachineGraph` 中创建 `Exit`
- **THEN** 系统 MUST 创建 `StateMachineExitNode`
- **AND** 该节点 MUST 继承或接入 `StateMachineControlNode` 控制节点基类
- **AND** 该节点 MUST 表达本层 Graph 的完成出口

#### Scenario: 普通图不能创建控制节点
- **WHEN** 用户在普通 `BaseTree` 或 `RunnableTree` 中打开节点搜索
- **THEN** 搜索结果 MUST NOT 暴露 `Root`、`Enter`、`AnyState` 或 `Exit`
- **AND** 普通图验证 MUST NOT 要求这些控制节点存在

### Requirement: RunnableNode 是通用可执行生命周期
系统 MUST 使用现有 `RunnableNode` 生命周期作为可执行节点的统一入口。`StateMachineNode` 若需要被父级行为图 tick，MUST 接入 `RunnableNode` 生命周期。系统 MUST NOT 为通用可执行节点新增额外模块节点。

#### Scenario: SMNode 被父级行为图执行
- **WHEN** 父级行为图 tick 到 `StateMachineNode`
- **THEN** `StateMachineNode` MUST 通过 `RunnableNode` 的 `OnStart/OnUpdate/OnStop/OnReset` 语义执行
- **AND** 系统 MUST NOT 通过额外模块节点的更新函数执行普通状态语义

#### Scenario: 保留模块扫描
- **WHEN** `StateMachineNode` 接入可执行生命周期
- **THEN** 它 MUST 继续使用 `BaseNode` 的模块字段扫描和属性端口映射
- **AND** 接入生命周期 MUST NOT 绕过 `NodeModule` 和 `PropertyPort` 主链路

### Requirement: StateMachineGraph 承载状态机图语义
系统 MUST 保持普通 `BaseTree` 的 Taco 原有图语义。系统 MUST NOT 把 `GraphProfile`、`Behavior`、`InitialStateNodeGuid` 或状态机图级字段序列化到所有 `BaseTree` 上。状态机图 MUST 由 `StateMachineGraph : BaseTree` 或等价专用图资产类型表达。

#### Scenario: Graph 独立打开
- **WHEN** 用户直接打开一个 `StateMachineGraph` 资产
- **THEN** 编辑器 MUST 能通过该资产自己的类型判断创建菜单、视觉表达和验证规则
- **AND** 编辑器 MUST NOT 依赖父节点上下文才能解释该 Graph

#### Scenario: 普通 BaseTree 不被污染
- **WHEN** 项目中存在已有 Taco `BaseTree` 资产
- **THEN** 系统 MUST NOT 要求这些资产新增 `Behavior` profile
- **AND** 系统 MUST NOT 把 `InitialStateNodeGuid` 序列化到普通 `BaseTree`
- **AND** 系统 MUST NOT 因为状态机语义改变普通 `BaseTree` 的创建菜单默认规则

#### Scenario: 新建状态机 Graph
- **WHEN** 用户创建 `StateMachineGraph` 资产
- **THEN** 创建结果 MUST 是状态机图资产
- **AND** 新图 MUST 默认包含一个固定 `Root`、一个 `Enter`、一个 `AnyState` 和一个 `Exit`
- **AND** 新图 MUST NOT 默认连接 `Root -> Enter`
- **AND** 新图 MUST NOT 自动创建第一个普通 `StateMachineNode`
- **AND** 系统 MUST NOT 新增 `StateMachineTree` 特化资产类

#### Scenario: 状态机 Graph 不直接 UpdateTree
- **WHEN** 父级 Graph tick 到引用 `StateMachineGraph` 的 `StateMachineNode`
- **THEN** `StateMachineGraph` MUST 由 `StateMachineGraphRuntime` 解释执行
- **AND** `StateMachineGraph` MUST NOT 为了被 tick 而继承 `RunnableTree`

### Requirement: 复用 Taco 树控制流端口
系统 MUST 保留 Taco 原有 `RunnableTree`、`RootNode`、`CompositeNode`、`DecoratorNode` 和 `RunnableNode` 生命周期控制流。状态机第一阶段 MUST 复用现有 flow port view 和 `BaseEdge` 数据表达 Transition，MUST NOT 新增 `TransitionIn/TransitionOut`、`SMPort`、并行 port registry 或 `IRunnableGraph` 运行入口。

#### Scenario: 父级树 tick StateMachineNode
- **WHEN** `StateMachineNode` 位于普通 `RunnableTree` 中
- **THEN** 它 MUST 只暴露 `Input` flow port
- **AND** 父级树 MUST 只接收该节点的 `Running/Success/Failure`
- **AND** 父级后续执行 MUST 由 Taco 父级控制节点负责，MUST NOT 通过该 `StateMachineNode` 的外层 `Output` 端口表达

#### Scenario: 状态机图内复用 edge
- **WHEN** `StateMachineNode` 位于 `StateMachineGraph` 中
- **THEN** 系统 MUST 将同一套 `Input/Output` edge 数据解释为状态机 Transition
- **AND** 系统 MUST NOT 要求用户连接另一套 transition 专用端口

#### Scenario: 不引入图运行接口
- **WHEN** `StateMachineNode` 引用 `StateMachineGraph`
- **THEN** 系统 MUST 通过 `StateMachineGraphRuntime` 解释该图
- **AND** 系统 MUST NOT 为了当前测试闭环新增统一 `IRunnableGraph` 或让 `StateMachineGraph` 继承 `RunnableTree`

### Requirement: Flow port 声明支持 Graph 上下文
系统 MUST 允许节点根据所在 `BaseGraph` 生成 flow port 声明。默认节点 MUST 继续从现有 `InputAttribute` 和 `OutputAttribute` 生成声明。该机制 MUST 只改变 flow port 声明来源，MUST NOT 改变 `PropertyPort` 值口链路、`PropertyEdge` 序列化链路或 `BaseEdge` 数据模型。

#### Scenario: 默认节点使用 class attribute
- **WHEN** 普通 Taco 节点没有覆写 flow port 声明
- **THEN** 编辑器 MUST 按该节点类型上的 `InputAttribute` 和 `OutputAttribute` 生成 flow port
- **AND** 现有普通树节点的端口表现 MUST 保持不变

#### Scenario: StateMachineNode 在普通行为图中
- **WHEN** `StateMachineNode` 的 owner graph 不是 `StateMachineGraph`
- **THEN** 编辑器 MUST 只为它生成 `Input` flow port
- **AND** 编辑器 MUST NOT 为它生成外层 `Output` flow port

#### Scenario: StateMachineNode 在 StateMachineGraph 中
- **WHEN** `StateMachineNode` 的 owner graph 是 `StateMachineGraph`
- **THEN** 编辑器 MUST 为它生成 `Input` flow port
- **AND** 编辑器 MUST 为它生成 `Output` flow port
- **AND** 该 `Output` flow port MUST 使用 Taco 原生 edge 连接到同层 `StateMachineNode` 或 `Exit`

#### Scenario: 不影响值口
- **WHEN** 节点或模块字段声明了 `PropertyPort`
- **THEN** 该值口 MUST 继续通过 `NodeFieldAccessor`、`PropertyPort` 和 `PropertyEdge` 链路生成
- **AND** flow port 上下文声明 MUST NOT 成为新的值口注册系统

### Requirement: 状态机图边界完整性
每一层 `StateMachineGraph` MUST 且只能包含一个 `Root`、一个 `Enter`、一个 `AnyState` 和一个 `Exit`。`Root` MUST 表达行为树进入当前层状态机图的入口源，`Enter` MUST 表达父级状态下钻进入下一层状态机图的入口源，`AnyState` MUST 表达全局跳转源，`Exit` MUST 表达本层完成出口。

#### Scenario: 缺少 Root
- **WHEN** 状态机 Graph 没有 `Root`
- **THEN** 验证结果 MUST 报告缺少 Root

#### Scenario: 重复 Root
- **WHEN** 状态机 Graph 存在多个 `Root`
- **THEN** 验证结果 MUST 报告重复 Root

#### Scenario: 缺少 Enter
- **WHEN** 状态机 Graph 没有 `Enter`
- **THEN** 验证结果 MUST 报告缺少 Enter

#### Scenario: 重复 Enter
- **WHEN** 状态机 Graph 存在多个 `Enter`
- **THEN** 验证结果 MUST 报告重复 Enter

#### Scenario: 缺少 AnyState
- **WHEN** 状态机 Graph 没有 `AnyState`
- **THEN** 验证结果 MUST 报告缺少 AnyState

#### Scenario: 重复 AnyState
- **WHEN** 状态机 Graph 存在多个 `AnyState`
- **THEN** 验证结果 MUST 报告重复 AnyState

#### Scenario: 缺少 Exit
- **WHEN** 状态机 Graph 没有 `Exit`
- **THEN** 验证结果 MUST 报告缺少 Exit

#### Scenario: 重复 Exit
- **WHEN** 状态机 Graph 存在多个 `Exit`
- **THEN** 验证结果 MUST 报告重复 Exit

#### Scenario: Root 边界
- **WHEN** 状态机 Graph 存在 `Root`
- **THEN** `Root` MUST NOT 有入边
- **AND** `Root` MUST 至少有一条指向同层 `StateMachineNode` 的出边

#### Scenario: Enter 边界
- **WHEN** 状态机 Graph 存在 `Enter`
- **THEN** `Enter` MUST NOT 有入边
- **AND** `Enter` MUST 至少有一条指向同层 `StateMachineNode` 的出边

#### Scenario: 缺失控制节点恢复
- **WHEN** 状态机 Graph 缺少 `Root`、`Enter`、`AnyState` 或 `Exit`
- **THEN** 节点搜索 MUST 重新暴露缺失的控制节点创建项
- **AND** 节点搜索 MUST NOT 暴露已经存在且唯一的控制节点创建项

### Requirement: SMNode 支持递归下钻
系统 MUST 允许每个 `StateMachineNode` 持有下一层 Graph 引用。该 Graph MAY 是 `StateMachineGraph`，也 MAY 是普通 `BaseTree` 或 `RunnableTree`。被引用 Graph MUST 用自己的实际资产类型表达语义，引用模块 MUST NOT 复制保存子 Graph 类型声明。

#### Scenario: 多个 SMNode 复用同一个 Graph
- **WHEN** 多个 `StateMachineNode` 引用同一个 Graph
- **THEN** 该 Graph 的语义 MUST 来自它自己的实际资产类型
- **AND** 引用它的父节点 MUST NOT 各自保存互相冲突的子 Graph 语义声明

#### Scenario: Locomotion 下钻到状态机图
- **WHEN** 用户打开 Locomotion `StateMachineNode`
- **THEN** 编辑器 MUST 打开 Locomotion 引用的 `StateMachineGraph`
- **AND** 该 Graph MUST 能包含 `Root`、`Enter`、`AnyState`、`Exit` 和 Idle、Walk 等同层 `StateMachineNode`
- **AND** 该 Graph 的状态机语义 MUST 来自 LocomotionGraph 自己的 `StateMachineGraph` 类型

#### Scenario: Idle 下钻到具体行为图
- **WHEN** 用户打开 Idle `StateMachineNode`
- **THEN** 编辑器 MUST 打开 Idle 引用的普通 `BaseTree` 或 `RunnableTree`
- **AND** 用户 MUST 能在该 Graph 中创建 Timeline 引用、Action、Value、BT 子图引用或另一个 `StateMachineNode`
- **AND** 该 Graph MUST NOT 为了表达普通行为语义而保存 `Behavior` profile

#### Scenario: 递归状态机
- **WHEN** 用户在某个行为 Graph 内创建新的 `StateMachineNode`
- **THEN** 系统 MUST 允许该节点继续引用下一层状态机 Graph
- **AND** 嵌套验证 MUST 检测 Graph 引用循环

### Requirement: StateMachineNode 在状态机图中支持 Transition 输出
系统 MUST 允许 `StateMachineNode` 在 `StateMachineGraph` 中提供 flow output，用于状态机同层 Transition。该 output MUST 与现有 edge 序列化链路兼容，不新增并行 transition port 系统。系统 MUST NOT 在普通行为图中为 `StateMachineNode` 暴露外层 flow output。

#### Scenario: 状态机同层状态转换
- **WHEN** 用户在 `StateMachineGraph` 中从 Idle `StateMachineNode` 连接到 Walk `StateMachineNode`
- **THEN** Idle MUST 暴露可连接的 `Output` flow port
- **AND** 系统 MUST 将该 edge 保存为普通 `BaseEdge`
- **AND** runtime MUST 能按该 edge 执行 Transition

#### Scenario: 普通行为图不暴露输出
- **WHEN** 用户在普通行为图中查看 `StateMachineNode`
- **THEN** 该节点 MUST NOT 显示 `Output` flow port
- **AND** 用户 MUST NOT 能从该节点创建外层 flow edge 到后继节点

#### Scenario: 普通行为图残留输出边
- **WHEN** 普通行为图中存在从 `StateMachineNode.Output` 发出的旧 edge
- **THEN** 验证结果 MUST 报告该 edge 非法
- **AND** 系统 MUST NOT 通过该 edge 串联后继 runnable 节点

### Requirement: Transition 是同层边语义
系统 MUST 将状态转换表达为 `StateMachineGraph` 内的 edge 语义。Transition MUST NOT 表达为单独节点。

#### Scenario: Root 进入当前层状态
- **WHEN** 用户连接 `Root -> Idle`
- **THEN** 系统 MUST 将该边解释为行为树进入当前层状态机图时的初始 Transition
- **AND** Idle MUST 是同层 `StateMachineNode`

#### Scenario: Enter 进入 Idle
- **WHEN** 用户连接 `Enter -> Idle`
- **THEN** 系统 MUST 将该边解释为父级状态下钻进入下一层状态机图时的初始 Transition
- **AND** Idle MUST 是同层 `StateMachineNode`

#### Scenario: Idle 转到 Walk
- **WHEN** 用户连接 Idle `StateMachineNode` 到 Walk `StateMachineNode`
- **THEN** 系统 MUST 能在该边上记录 Transition 语义
- **AND** Transition MUST 属于 Locomotion 这一层状态机 Graph
- **AND** 创建菜单 MUST NOT 暴露 Transition 特化节点

#### Scenario: Walk 转到 Exit
- **WHEN** 用户连接 Walk `StateMachineNode` 到 `Exit`
- **THEN** 系统 MUST 将该边解释为本层状态机完成 Transition
- **AND** Transition 命中后本层 `StateMachineGraph` MUST 返回 Success

#### Scenario: AnyState 全局跳转
- **WHEN** 用户连接 `AnyState -> Walk`
- **THEN** 系统 MUST 将该边解释为本层全局 Transition
- **AND** 该 Transition MUST 配置 Bool 条件

#### Scenario: 禁止跨层转换
- **WHEN** 用户尝试从 LocomotionGraph 的 Idle 直接连接到 IdleGraph 内部节点
- **THEN** 系统 MUST 拒绝该 Transition 或在验证中报告非法跨层转换
- **AND** Transition MUST 只能连接同一个状态机 Graph 内的合法端点

### Requirement: Transition 端点规则
系统 MUST 验证 Transition 的起点和终点。合法连接 MUST 是 `Root -> StateMachineNode`、`Enter -> StateMachineNode`、`AnyState -> StateMachineNode|Exit`、`StateMachineNode -> StateMachineNode|Exit`。

#### Scenario: Root 只能进入当前层 State
- **WHEN** Transition 从 `Root` 指向非 `StateMachineNode` 节点
- **THEN** 验证结果 MUST 报告非法 Transition 终点

#### Scenario: Exit 不能出边
- **WHEN** Transition 从 `Exit` 开始
- **THEN** 验证结果 MUST 报告非法 Transition 起点

#### Scenario: Enter 不能有入边
- **WHEN** Transition 指向 `Enter`
- **THEN** 验证结果 MUST 报告非法 Transition 终点

#### Scenario: AnyState 不能入边
- **WHEN** Transition 指向 `AnyState`
- **THEN** 验证结果 MUST 报告非法 Transition 终点

#### Scenario: 非法转换端点
- **WHEN** Transition 连接到非状态机控制节点、非 `StateMachineNode` 或跨层节点
- **THEN** 验证结果 MUST 报告非法转换端点

#### Scenario: 拖线阶段过滤非法端点
- **WHEN** 用户在 `StateMachineGraph` 中从 flow port 拖拽创建 Transition
- **THEN** 编辑器 MUST 只把合法 Transition 端点作为兼容 port 候选
- **AND** `Root` 的输出 MUST NOT 能连接到非 `StateMachineNode` 的输入
- **AND** `Enter` 的输出 MUST NOT 能连接到非 `StateMachineNode` 的输入
- **AND** 该过滤 MUST NOT 影响 `PropertyPort` 值口连接

### Requirement: Transition 条件
Transition 第一阶段 MUST 支持优先级和一个可选 Bool 条件引用。条件数据 MUST 通过现有端口/属性主链路表达。复杂条件 MUST 先由同层 Graph 中的普通节点或模块计算成 Bool。

#### Scenario: 无条件转换
- **WHEN** 普通 State 的 Transition 没有配置条件
- **THEN** 运行时 MUST 将该 Transition 视为始终允许

#### Scenario: 条件驱动转换
- **WHEN** Transition 配置了 Bool 条件输入
- **THEN** 运行时 MUST 在条件为 true 时允许转换
- **AND** 条件端口 MUST 是现有 property port 中的 Bool 输出

#### Scenario: AnyState 必须有条件
- **WHEN** `AnyState` 的 Transition 没有配置 Bool 条件
- **THEN** 验证结果 MUST 报告该 Transition 非法

#### Scenario: 复杂条件
- **WHEN** 用户需要表达多个条件组合
- **THEN** 用户 MUST 先在同层 Graph 内用普通节点计算出 Bool 结果
- **AND** Transition MUST 只引用该 Bool 结果作为条件

#### Scenario: 缺失动态端口
- **WHEN** Transition 引用的端口 ID 不存在
- **THEN** 验证结果 MUST 报告具体节点、模块和端口 ID

### Requirement: Timeline 通过状态行为 Graph 接入
系统 MUST NOT 将 `TimelineNode` 作为 `StateMachineGraph` 同层状态节点创建。Timeline MUST 通过 `StateMachineNode` 下钻到状态行为 Graph 后接入；承载 `TimelineNode` 的状态行为 Graph MUST 能提供 `RunnableTree` 生命周期和 deltaTime。

#### Scenario: Idle 状态播放 Timeline
- **WHEN** Idle `StateMachineNode` 引用一个状态行为 Graph
- **THEN** 用户 MAY 在该状态行为 Graph 中创建 `TimelineNode`
- **AND** 该状态行为 Graph MUST 是 `RunnableTree` 体系或提供等价 `UpdateTree(deltaTime)` 生命周期
- **AND** `TimelineNode` MUST 从该运行图获得 deltaTime

#### Scenario: 状态机图不直接创建 TimelineNode
- **WHEN** 用户在 `StateMachineGraph` 的节点搜索中查找 Timeline 节点
- **THEN** 系统 MUST NOT 将 `TimelineNode` 暴露为同层可创建状态节点
- **AND** 用户 MUST 通过普通 `StateMachineNode` 的下钻 Graph 创建 Timeline 行为

#### Scenario: Timeline 行为串联
- **WHEN** 用户需要 Timeline 播放结束后继续执行同一状态行为 Graph 内的其它 runnable 节点
- **THEN** `TimelineNode` MUST 提供可连接的 `Output` flow port
- **AND** 系统 MUST NOT 为 Timeline 串联新增并行端口协议

### Requirement: 状态机运行时解释
系统 MUST 让父级 Graph tick 到 `StateMachineNode` 时，由该 SMNode 负责进入并驱动自己引用的下一层 Graph，再把结果以 `Running/Success/Failure` 返回给父级 Graph。

#### Scenario: 父级行为图 tick Locomotion
- **WHEN** 父级行为图 tick 到 Locomotion `StateMachineNode`
- **THEN** Locomotion MUST 进入自己引用的 LocomotionGraph
- **AND** LocomotionGraph 如果是 `StateMachineGraph` 且进入来源是普通行为图，MUST 从 `Root` 开始解释
- **AND** 父级行为图 MUST NOT 直接 tick IdleGraph 或 WalkGraph 内部节点

#### Scenario: 状态下钻进入下一层状态机
- **WHEN** 当前 `StateMachineGraph` 的 active `StateMachineNode` 引用下一层 `StateMachineGraph`
- **THEN** 下一层 `StateMachineGraph` MUST 从 `Enter` 开始解释
- **AND** 系统 MUST NOT 通过当前层内部 edge 连接到下一层 `Enter`

#### Scenario: 状态机图 tick active state
- **WHEN** LocomotionGraph 当前 active state 是 Idle
- **THEN** 本帧 MUST 只 tick Idle 及其下钻 Graph
- **AND** Walk MUST NOT 被 tick，除非 Transition 切换 active state 到 Walk

#### Scenario: Root 和 Enter 不持续 tick
- **WHEN** LocomotionGraph 已经通过入口源激活了 Idle
- **THEN** 后续帧 MUST NOT tick `Root` 或 `Enter`
- **AND** 系统 MUST 只把 `Root` 作为行为树进入当前层时没有 active state 的图解释入口
- **AND** 系统 MUST 只把 `Enter` 作为父状态下钻进入下一层时没有 active state 的图解释入口

#### Scenario: AnyState 优先检查
- **WHEN** 状态机 Graph 已经存在 active state
- **THEN** 运行时 MUST 在 tick active state 前检查 `AnyState` Transition
- **AND** 命中后 MUST stop 当前 active state 并切换到目标状态或 Exit

#### Scenario: Transition 切换 active state
- **WHEN** Idle 到 Walk 的 Transition 条件成立
- **THEN** 系统 MUST stop Idle 当前执行链路
- **AND** 系统 MUST 将 active state 切换为 Walk
- **AND** 下一次状态执行 MUST 从 Walk 引用的 Graph 开始

#### Scenario: Active state 完成但没有转换
- **WHEN** 当前 active state 的下钻 Graph 返回 `Success`
- **AND** 该 active state 没有命中任何 Transition
- **THEN** 本层 `StateMachineGraph` MUST 保持 `Running`
- **AND** 当前 active state MUST 保持不变，直到后续 Transition 或 Exit 命中

#### Scenario: Exit 完成本层 Graph
- **WHEN** active state 命中指向 `Exit` 的 Transition
- **THEN** 本层 `StateMachineGraph` MUST 返回 Success
- **AND** 父级 `StateMachineNode` MUST 以该结果继续自己的父级生命周期

#### Scenario: Stop 和 Reset 传播
- **WHEN** 父级 Graph stop 或 reset Locomotion `StateMachineNode`
- **THEN** Locomotion MUST stop 或 reset 当前 active state 链路
- **AND** 当前 active state MUST stop 或 reset 自己的下钻 Graph

### Requirement: 不保留旧特化数据路径
系统 MUST 不依赖旧 Locomotion、Action、FootPhase SO/config 数据来表达状态机创作语义。

#### Scenario: 旧 Locomotion 数据存在
- **WHEN** 项目中发现旧 Locomotion 状态配置或动画状态 SO
- **THEN** 本能力 MUST NOT 读取该数据
- **AND** 该数据 MUST 被迁移为新 Graph 结构或直接删除

#### Scenario: FootPhase 创作
- **WHEN** 用户需要编辑 FootPhase
- **THEN** FootPhase MUST 作为 Timeline 或状态行为 Graph 里的轨道或模块数据表达
- **AND** 系统 MUST NOT 为 FootPhase 保留独立状态机配置入口
