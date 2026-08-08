## Context

`ExposedPropertyNode` 同时承载 Blackboard Get 和 Set：

- Get 没有 flow 输入，`m_Value` 是 Output / Multiple。
- Set 具有 `Input` flow 输入，`m_Value` 是 Input / Single。

现有 Capability 注册先默认构造节点，再把得到的 Property port 写成固定端口。默认 `ExposedPropertyNodeType` 是 Get，因此全部实例都被声明为 Get 形状。Shared Graph adapter 已经能够投影真实节点的动态 Flow 与 Property port，但固定端口会优先遮蔽同 identity 的实例投影。Document 侧 `IsPortAllowed` 又始终把 Property port走静态列表，并在失败后允许当前 Unity snapshot 端口兜底，形成 Canvas、Document 和资产三套判断。

## Goals / Non-Goals

### Goals

- 一个 typed node property 集合只能得到一个确定的端口形状。
- Canvas、Document context、strict parse、Reconciler、Mutation 和 Validator 使用同一个端口形状来源。
- `ExposedPropertyNode` 自己保证 mode 与端口方向一致。
- 现有合法 Corin Get/Set 资产不迁移、不改 identity。
- mode 变化与 edge 变化在同一目标对账中形成完整闭包。

### Non-Goals

- 不改变 Pipeline Blackboard runtime、scope、lifetime 或 compiled address。
- 不拆分 Blackboard declaration 或新增 Blackboard asset。
- 不把 Unity 序列化字段名写入 sparse Graph JSON。
- 不放宽 `BasePortView.ValidateAuthoringShape`。
- 不为旧 node catalog、旧错误端口或不完整 mode change 保留兼容与 fallback。

## Decisions

### Decision: 保留一个 exposed-property capability

`exposed-property` 继续以 `exposedProperty.mode` 表达 Get/Set。Capability 增加机器可读的条件端口变体，每个变体声明 discriminator 条件及完整条件 Flow/Property port 集合；projector 将固定端口与唯一命中的条件集合合并成最终形状。

业务取舍：作者仍可在同一节点上切换 Get/Set，现有资产和 Graph node identity 不迁移。代价是 Capability 和 node catalog 需要正式理解条件端口，而不能只保存一个类型级静态列表。

备选方案是拆成 `blackboard-get` 与 `blackboard-set` 两种 capability。它能让每种 capability 只有固定端口，但需要迁移所有节点 kind、创建入口、Document、Mutation 和资产，也会取消作者原地切换模式的工作流。当前业务已经把 mode 作为正式 typed property，因此不采用拆分。

### Decision: 条件端口由纯 Node Port Shape Projector 计算

端口投影输入只包含 capability identity 和 strict typed properties，输出为完整 Flow/Property descriptor 集合。投影器不读取 selection、GraphView、当前 Unity edge、默认构造节点状态或 runtime Blackboard。

固定端口继续直接存在 Capability 中。条件端口在 node projection 中使用 `GraphAuthoringDynamicPortProjection` 表达，但它不是作者自由新增的列表端口：identity 由 capability 变体固定，方向、容量和 required 由 discriminator 唯一决定，Graph JSON 不保存冗余端口正文。

`ExposedPropertyNode` 的变体为：

| mode | Flow port | Property port |
|---|---|---|
| `Get` | 无 | `m_Value` / Output / Multiple |
| `Set` | `Input` / Input / Single | `m_Value` / Input / Single |

业务取舍：Canvas 与 Agent Document 能共享同一结果，错误在进入 Mutation 前就被拒绝。代价是实现必须把现有 flow-only configurator 提升为完整节点形状投影器，并同步所有消费者。

### Decision: NodeType 与 PropertyPort.Direction 由节点领域入口共同维护

`ExposedPropertyNode.SetNodeType` 同时更新 `m_NodeType` 和 `m_Value.Direction`。选择 declaration 只负责设置端口值类型和 Blackboard reference，不再承担修正 mode 方向。UI、TreeClip 和 Agent Mutation 调用者不再直接写 `Value.Direction`。

反序列化后若 `NodeType` 与 `m_Value.Direction` 不一致，正式 Validator 报告节点 identity、mode、实际方向和期望方向。系统不静默改资产，也不使用默认方向继续打开或导出。

业务取舍：不变量只存在一个实现入口，新增调用者不需要复制顺序规则。非法旧资产会明确失败，需要作者通过正式 authoring mutation 修正，而不是被窗口自动改写。

### Decision: mode change 必须与 edge 目标形成同一对账闭包

Reconciler 先按目标 properties 计算目标端口形状，再规划删除旧形状不允许的边、配置 mode、建立目标边。Mutation preflight 在修改 Unity 对象前验证完整计划；缺少必要删边、引用非法方向或目标边不闭合时整次 apply 失败。

业务取舍：Get 切 Set 不会留下指向错误方向的连接，也不会依赖 UI 先删边。代价是 mode 变化不能作为忽略 edges 的孤立 property mutation 提交。

## Data Contract

`context/node-catalog.json` 对条件端口 capability 使用严格 `portVariants`，每个 variant 至少包含：

```json
{
  "when": {
    "field": "exposedProperty.mode",
    "equals": "Set"
  },
  "flowPorts": [
    { "key": "Input", "direction": "Input" }
  ],
  "propertyPorts": [
    { "key": "m_Value", "direction": "Input", "valueType": "property" }
  ]
}
```

同一 variant 内 port identity 唯一，且不得与同 capability 的固定端口重复；不同 variant 可以复用同一 identity，但相同 typed properties 必须只匹配一个 variant。未知 discriminator、零匹配或多匹配都由 strict parser 报错。没有条件端口的节点继续只使用现有静态 Flow/Property port 列表。

## Unified Flow

```text
strict typed node properties
  -> Capability Node Port Shape Projector
  -> Canvas node projection
  -> service-owned node catalog variant
  -> Package endpoint validation
  -> Reconciler target shape
  -> Mutation preflight
  -> formal Validator
```

当前 Unity snapshot 只用于对账已有实体和生成 diff，不再作为未知 endpoint 的放行来源。

## Risks

- 其它节点可能也存在由配置改变方向或容量的端口。实现时必须盘点所有注册了 dynamic configurator 或运行时替换 PropertyPort 的节点；发现同类节点必须接入同一投影器，不能给 `ExposedPropertyNode` 增加窗口特判。
- mode 变更排序错误会让旧 edge 在节点换向后暂时非法。必须由 immutable plan 和 preflight 先验证目标闭包，再进入单一事务。
- node catalog schema 改变会使已有 `.btsmtl` context hash 变化。重新 checkout 由 service 发布新 context，不读取旧条件端口格式。

## Migration

1. 安装新的 Capability 与 node catalog 合同。
2. 删除 flow-only configurator、默认实例条件端口推断和 snapshot endpoint fallback。
3. 重新 checkout 目标 Document，刷新 service-owned context 与 context hash。
4. 对现有 Unity authoring 运行正式 Validator；合法 Get/Set 资产保持原样，不生成资产迁移。
5. 若发现 mode 与方向不一致的资产，通过同一 Document target 和正式 Mutation 修正，不直接编辑 YAML。
