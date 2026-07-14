# Design: Pipeline Blackboard 所有权、作用域与创作视图

## Context

`BaseGraph` 本来已经内联保存自己的 `m_ExposedProperties`，运行工作副本也会分别初始化这些 declaration。但角色管线编辑器通过 `CharacterPipelineAuthoringContext.GetExposedPropertySource()` 将所有页面的变量来源改成 RootTree，runtime 注册后又把所有 declaration 压平到 `Dictionary<string, ...>`。

因此当前模型出现了三层不一致：

1. 序列化层允许每个 Graph 拥有 declaration。
2. 编辑器层只允许作者看见和创建 RootTree declaration。
3. runtime 层忽略 Graph/State/Action owner，只按字符串 key 存值。

Corin 资产已经体现了这个矛盾：全局阈值适合 RootTree Character scope；`IsDodging` 需要由 Action 写、Locomotion 读，也适合 Character scope；但两个 Dodge body 为了让本 Graph 的 `ExposedPropertyNode` 能解析 GUID，又各自复制了一份同 key declaration。

## Goals / Non-Goals

### Goals

- 让变量的声明位置、可见范围、运行时地址和清理生命周期一致。
- 允许小范围变量留在当前 Graph/State/Action 上，不污染 Character 全局列表。
- 保持一套 declaration 类型、一套引用协议和一套 runtime 黑板。
- 允许并行 StateMachine、同状态重复进入、shared graph 多实例和多个 ActionInstance 正确隔离。
- 让 Graph 与 Transition 创作时都能查找、创建和绑定合法变量。
- 对缺失、歧义、越界和类型错误直接报错，不回退到裸 key、默认零值或重复 declaration。

### Non-Goals

- 不创建按分类拆分的 Blackboard ScriptableObject 文件。
- 不把所有节点字段和端口默认值迁移成 Blackboard variable。
- 不让 Blackboard 直接成为网络复制单位。
- 不实现跨角色共享内存、全局游戏 Blackboard 或服务端变量数据库。
- 不为旧字符串引用保留兼容 resolver。

## Decisions

### Decision: 声明就近归属，但 runtime 仍保持单一服务

Character scope declaration 保存于角色 RootTree。Graph scope declaration 保存于当前 Graph。State、ActionInstance 和 Frame scope declaration也保存于实际使用它们的 inline/shared Graph，并通过运行上下文绑定到对应 State、ActionInstance 或 tick owner。

所有 declaration 最终都注册进同一个 `PipelineBlackboardRuntime`，runtime 使用结构化 address 分桶；不会为每个 scope 创建不同 dictionary service 或 Blackboard asset。

业务取舍：作者在局部页面只承担局部参数的心智负担，同时 Character 级协调值仍有唯一入口。相比 RootTree 全量声明，这能控制大型状态机的变量数量；相比多个 Blackboard asset，它不会引入装配、版本和来源优先级问题。

### Decision: declaration GUID 是身份，key 是作者名称

现有 `BaseExposedProperty.GUID` 继续作为稳定 declaration identity。`BlackboardKey` 只要求在同一 declaration owner 内唯一，用于显示、搜索和配置表达。节点保存显式 variable reference，reference 至少能定位 declaration identity 与声明 owner。

不同局部 owner MAY 使用相同 key，但运行时不按名称自动 shadow 或猜测最近变量。作者从 picker 选择的是一个明确 declaration；引用断裂、不可见或类型不匹配时 validation 和 runtime 必须失败。

业务取舍：允许不同攻击都拥有 `Elapsed`、`ComboIndex` 之类自然名称，同时避免字符串作用域解析产生隐式行为。代价是引用数据比单个字符串更完整，但重命名变量不会破坏绑定。

### Decision: runtime address 必须包含实际 owner identity

runtime value address 使用 declaration identity 加 scope owner：

```text
Character      = CharacterRuntimeId + DeclarationId
Graph          = GraphRuntimeId + DeclarationId
State          = StateMachineRuntimeId + StateId + ActivationGeneration + DeclarationId
ActionInstance = ActionInstanceId + DeclarationId
Frame          = LocalLogicTick + DeclarationId
```

Graph runtime identity 来自运行工作副本，不来自 asset 名称或 serialized path。State 使用现有 `StateMachineExecutionScope`，因此 Locomotion 与 Action 同名状态、同一状态重复进入以及并行状态机不会共用 bucket。shared graph 每个运行实例也拥有独立 Graph bucket。

业务取舍：结构化 address 增加了 runtime context 传递，但清理可以准确命中 owner，不再扫描并误删其它状态或动作的数据。

### Decision: 生命周期与作用域采用合法组合，不允许任意搭配

支持的正式组合为：

| Scope | Lifetime | 语义 |
|---|---|---|
| Character | Config | 角色配置常量，runtime 只读 |
| Character | Spawn | 角色 runtime 创建到销毁 |
| Character | ManualClear | 跨 Graph/State 共享，显式清理 |
| Graph | Config | 当前 Graph owner 的局部配置常量，runtime 只读 |
| Graph | GraphInstance | 当前 Graph 工作副本初始化到销毁 |
| State | StateEnterToExit | 当前 State activation 进入到退出 |
| ActionInstance | ActionInstance | 当前 ActionInstance 创建到 terminal/clear |
| Frame | Frame | 当前 local logic tick |

其它组合 validation 必须拒绝。单个 State body 的只读调参使用 `Graph + Config`；只有每次进入状态都需要独立变化的值才使用 `State + StateEnterToExit`。

业务取舍：限制组合会减少“看似灵活”的配置，但作者不再需要猜 `State + ManualClear` 或 `Frame + Spawn` 到底何时销毁。

### Decision: Graph 访问上下文负责解析 owner，不让节点手工拼地址

`BaseGraph` 初始化、ConditionRuleGraph evaluation、State scope push、ActionContext 和 logic tick 共同形成有效 blackboard access context。变量节点只提交显式 declaration reference 和读写意图，由统一 runtime resolver 根据 declaration scope 生成 address。

ConditionRuleGraph 保持纯求值：它可以读取当前上下文中可见的 Character、Graph、active State、ActionInstance 或 Frame declaration，但不能通过 runnable setter 写值。缺少有效 owner 时求值失败，不写零值让 Compare/And/Or 继续运行。

业务取舍：节点 API 更简单，owner 规则集中；代价是 Graph runtime 和 evaluation context 必须完整传递 scope，但这正是状态、动作和 frame 生命周期闭环所需的信息。

### Decision: 创作面板使用上下文、scope 与分类视图，不使用分类文件

Pipeline Blackboard 面板在 Graph tab 和 Transition selection 中持续可访问，包含：

- scope 筛选：`All / Character / Graph / State / Action / Frame`
- 上下文筛选：`Current Context / All Visible`
- 层级分类：由 `CategoryPath` 按 `/` 形成 foldout，例如 `Locomotion/Thresholds`
- 搜索：按 key、display name、类型和 owner 查找
- 来源标记：`Local` 或 `Inherited`，并显示声明 owner

在当前 Graph 创建变量时，UI 只提供该上下文合法的 scope/lifetime 组合。Character declaration 明确写入 RootTree；局部 declaration 写入当前 Graph。拖拽或 picker 绑定 inherited declaration 时只创建 reference，不复制 declaration。

业务取舍：这满足“分栏分类与作用域”的创作需求，但资产仍按角色 Graph ownership 保存。相比每个分类一个文件，局部数据随 owner 删除、复制和抽取 shared graph，生命周期更自然。

### Decision: 小常量优先留在节点与端口

只被一个节点使用的常量使用节点字段或 `PropertyPort` 默认值；只在一次求值链中传播的中间值使用 ValueNode + PropertyEdge；需要跨节点、跨 tick、跨状态、跨 Graph、调试观察或作者集中调参时才声明 Blackboard variable。

业务取舍：Blackboard 不会退化为所有数值的垃圾桶，同时共享状态仍有正式、可观察的身份。

### Decision: Blackboard 元数据不改变网络边界

`Authority` 和 `SyncPolicy` 继续描述变量如何参与本地预测、配置身份或事实转换，但 Blackboard value 不直接序列化为网络 key/value。需要同步的业务输出必须由 resolver 转换为正式 SyncFacts，再进入对应 SyncDomain。

业务取舍：局部变量可以自由重命名和重组，不会意外改变协议；代价是需要同步的事实仍要显式建模，这是可追踪网络行为所必需的。

## Alternatives

### Alternative: 所有 declaration 继续放 RootTree，只增加文件夹和筛选

优点是改动最小，现有跨图字符串读取基本不变。缺点是 scope 仍只有标签意义，局部 Graph 被复制或 shared 时变量不会随 owner 迁移，State/Action 清理漏洞也无法由 UI 解决。

### Alternative: 每个分类或 scope 使用独立 Blackboard asset

优点是可以跨角色复用整组参数。缺点是需要定义 asset 装配顺序、覆盖规则、版本和 ownership，容易重新形成多数据源；本项目当前没有跨角色参数库的业务压力，因此不采用。

### Alternative: 按 key 使用词法 shadowing 自动解析最近声明

优点是节点只保存短 key。缺点是移动节点、抽取 shared graph 或新增同名局部变量都可能静默改变绑定；调试时也难以判断读到了哪一个 owner，因此不采用。

## Migration Plan

1. 完成并归档/关闭 active change `fix-corin-action-lifecycle-and-dodge-interruption`，冻结其 Corin RootTree 结果。
2. 扩展 declaration metadata、variable reference 和 scope/lifetime validation，删除旧分类字段命名。
3. 将 runtime declaration/value store 迁移到结构化 address，并接入 Graph、State、ActionInstance 和 Frame owner 生命周期。
4. 迁移变量节点与 ConditionRuleGraph 读取，不保留裸 key fallback。
5. 重构 Pipeline Blackboard authoring panel 与 picker。
6. 迁移 Corin RootTree：保留五个 Character declaration，重绑 Dodge body 引用并删除两个重复 `IsDodging` declaration。
7. 扫描全部角色 pipeline 资产，报告并修复重复 declaration、非法 scope/lifetime、断裂引用和裸 key 节点。
8. 删除旧接口、旧字段和兼容 resolver，执行编译与 OpenSpec strict validation。

## Risks / Trade-offs

- 变量引用和 runtime address 是破坏性迁移；未迁移资产应明确报错，不能继续依赖字符串 fallback。
- inline graph 的 owner 链必须在编辑器、克隆和 runtime evaluation 中保持稳定；缺失 owner context 时局部变量不可读。
- shared graph 的 Graph declaration 会按每个 runtime instance 隔离；若业务真正需要跨实例共享，必须显式改为 Character declaration，而不是依赖 shared asset 身份。
- Corin active change 与本 change 都触碰 RootTree，必须串行实施。

