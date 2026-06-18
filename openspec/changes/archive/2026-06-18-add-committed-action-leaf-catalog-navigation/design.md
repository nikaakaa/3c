# Design: CommittedActionLeaf Catalog 导航

## Context

当前正式数据分层已经确定：

- Behavior Source graph 只表达 `BehaviorRoot -> Ordered/Parallel -> LocomotionLeaf + CommittedActionLeaf` 这类 source topology。
- Committed Action branch 数据属于 `CharacterActionDefinitionSO`，由 Committed Branch mode 编辑。
- 角色可用 action 集合属于 `CharacterActionCatalogSO`，由 `CharacterConfigSO` 引用。
- Timeline 数据属于 ActionDefinition 内的 TimelineNode，Timeline Editor 独立打开。

因此主视图里应该有 `CommittedActionLeaf`，但不应该展开每个 action 的 branch 内部节点。`CommittedActionLeaf` 是“Committed Action source”的入口，不是 `Action.Dodge` 的节点。

## Decision

`CommittedActionLeaf` 的双击或打开命令进入一个 Action Catalog 导航流程：

1. Editor 从正式角色配置解析 `CharacterActionCatalogSO`。默认可以使用 Corin formal `CharacterConfigSO`，但实现必须允许用户明确选择同类正式配置或 catalog，避免写死 Dodge。
2. Editor 从 catalog 中建立 action entry 快照。每个 entry 包含稳定 action id、显示名、`CharacterActionDefinitionSO` 引用和诊断状态。
3. 如果只有一个有效 entry，直接切换同一窗口到 Committed Branch mode，并绑定该 action definition。
4. 如果有多个有效 entry，在同一 Character Behavior Editor 中展示选择入口；选择某个 action 后切换到 Committed Branch mode。
5. 如果 catalog 缺失、为空、entry 缺失 definition 或 action id 重复，显示诊断并停止，不使用 fallback。

## Data Boundaries

Behavior Source graph 仍然只保存 source node、edge、editor position 和 schema version。Catalog selection 可以是 editor session state 或窗口状态，但不能写入 Behavior Source authoring asset 作为 action 数据源。

Action branch 修改仍通过 Committed Branch adapter 写回选中的 `CharacterActionDefinitionSO`。保存 Behavior Source graph 不能修改 ActionDefinition，保存 ActionDefinition 也不能修改 Behavior Source topology。

## UI Boundary

Action 选择入口属于 Character Behavior Editor 内的导航能力，不是新的 Branch 编辑器。可以表现为 in-window picker、toolbar dropdown、SearchWindow 或 Ref-port 后的等价选择面，但必须满足：

- 仍使用同一个 Character Behavior Editor window。
- 选择结果进入 Committed Branch mode。
- Branch 内部节点只在 Committed Branch mode 展示。
- Timeline 编辑仍通过独立 Timeline Editor。

## Rejected Alternatives

- 在 Behavior Source 主图下展开每个 action 的 Branch Root：拒绝。它会把 source topology 和 action branch 数据混在一起。
- 给每个 action 新增一个 Behavior Source leaf：拒绝。Committed Action source 是一个 action domain 入口，具体 action 由 catalog / request / lifecycle 决定。
- 保留 Dodge 默认路径作为 fallback：拒绝。新增 action 后会重新形成 Dodge 特例。
- 新建 Action Branch Editor 窗口：拒绝。当前规范要求 Branch mode 收敛在 Character Behavior Editor。

## Test Strategy

- EditMode 测试 catalog entry 快照、唯一 action 直接打开、多 action 选择打开、缺失 catalog 诊断、重复 action id 诊断。
- Editor adapter 测试 `CommittedActionLeaf` open 不再硬编码 Dodge，选择新增 action 后绑定对应 `CharacterActionDefinitionSO`。
- 静态边界测试确认 Behavior Source authoring asset 不新增 ActionDefinition 列表字段，runtime 不引用 Editor / GraphView 类型。
- OpenSpec validate 覆盖本 change 和全量 specs。
