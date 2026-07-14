# Change: 用内联图数据替换 BTSMTL 私有 subasset 图归属

## Why
当前 BTSMTL authoring 口径和代码实现把 `StateMachineNode`、`StateNode`、Transition rule 的私有下钻图默认做成 Unity owned embedded sub-asset。这和新的创作心智不一致：

- 默认创建节点时，用户不应该再看到“先创建或拖拽一个 Graph asset”的步骤。
- 默认私有图也不应该保存为 subasset；它应该是 owner 节点或边内部的普通 C# 可序列化图数据。
- 只有确实需要复用时，才显式创建独立 ScriptableObject asset。

现在的 `BaseGraph : ScriptableObject`、`BaseTree : BaseGraph`、`EmbeddedGraphOwnershipUtility`、`BaseEdge.m_TransitionRuleGraph` 和 Graph reference module 都把“图数据”和“Unity asset”绑在一起，导致默认私有图只能走 `CreateInstance + AddObjectToAsset`。这会继续产生隐藏 subasset、删除生命周期复杂、打开和复用心智混乱的问题。

## What Changes
- 将 BTSMTL 图核心从 Unity Object 资产身份里拆出，形成普通 C# 可序列化图数据模型。
- `BaseGraph` 从“ScriptableObject 图资产本体”收口为“图数据和结构操作本体”。
- 新增 `BaseTreeAsset : ScriptableObject` 作为独立 reusable/shared asset 外壳；`BaseTree` 继续作为 `BaseGraph` 图数据类型，不再承担 Unity asset 身份。
- `StateMachineNode` 默认内联持有 `StateMachineGraph` 数据；需要复用时才引用独立 `StateMachineGraph` asset。
- `StateNode` 默认内联持有状态行为 `SubTree` / `StateBehaviorSubTree` 图数据；需要复用时才引用独立 tree asset。
- Transition edge 默认内联持有 `TransitionRuleGraph` 数据；需要复用时才引用独立 rule graph asset。
- 删除 owned embedded subasset 创建、绑定、递归删除和校验路径。
- 运行时从内联或 shared 图数据创建运行工作副本，不再用 `Object.Instantiate(ScriptableObject graph)` 作为通用运行实例机制。

## Out of Scope
- 不新增 WorkbenchTree、WorkbenchPortDescriptor 或并行端口协议。
- 不恢复旧 locomotion、action、footphase、bodyclaim、AnimationPresentationPolicy 等 SO/config 数据源。
- 不重做 Timeline 资产模型；Timeline 仍然是天然可复用资产，由 `TimelineNode` 引用。
- 不新增手动验证任务；Unity 端到端验证由用户执行。

## Impact
- 这是破坏性图数据归属重构。
- 现有 project/spec 中关于 owned embedded subasset 的描述必须被本 change 覆盖。
- 现有代码中依赖 `BaseGraph : ScriptableObject`、`BaseTree : BaseGraph`、`AssetDatabase.AddObjectToAsset`、`AssetDatabase.IsSubAsset`、`Object.Instantiate(graph)` 的图归属和运行逻辑都需要调整。
- 默认私有图删除会变简单：删除 owner 节点或边即可删除其内联字段数据；shared asset 明确独立，不随 owner 删除。

## Open Questions
- 无。实现口径收束为 `BaseTreeAsset` 作为 Unity asset 外壳，`BaseTree/BaseGraph` 作为普通 C# 图数据。
