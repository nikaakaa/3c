# Change: 增加 CommittedActionLeaf 的 Action Catalog 导航

## Why

当前 Character Behavior Editor 的 Behavior Source 主图已经只保留 source 拓扑，`CommittedActionLeaf` 双击可以跳到 Committed Branch mode，但入口仍偏向当前 Dodge 示例。新增 ActionDefinition 后，设计者缺少从主图进入目标 action branch 的正式路径，容易重新引入 Dodge 特例、Branch 节点塞回 Behavior Source 主图，或增加第二个 Branch 编辑窗口。

需要把 `CommittedActionLeaf` 的打开行为接到正式 `CharacterConfigSO -> CharacterActionCatalogSO -> CharacterActionDefinitionSO` 链路：主图仍只表达 Committed Action source，具体 action 的 branch 由 Action Catalog 选择后进入同一个 Committed Branch mode 编辑。

## What Changes

- `CommittedActionLeaf` 打开时通过正式角色配置或明确选择的 Action Catalog 枚举可编辑的 `CharacterActionDefinitionSO`。
- 当 catalog 中只有一个有效 action 时，编辑器可以直接进入该 action 的 Committed Branch mode。
- 当 catalog 中有多个有效 action 时，编辑器在同一个 Character Behavior Editor 内提供 action 选择入口，选择后进入对应 action 的 Committed Branch mode。
- 缺少角色配置、Action Catalog、ActionDefinition 或存在重复 action id 时，只显示明确诊断，不使用 Dodge 硬编码、Resources、sample asset 或隐藏默认 branch。
- Behavior Source 主图不显示 action branch 内部节点；Branch Root、Selector、Condition、TimelineNode 仍只在 Committed Branch mode 中展示。
- 新增 EditMode / 静态边界测试，覆盖 catalog 导航、无 fallback、主图不保存 action branch 数据和新增 action 可发现。

## What Does Not Change

- 不新增第二个 Branch 编辑窗口、第二套 GraphView shell 或重复菜单入口。
- 不把 ActionDefinition 列表保存进 `CharacterBehaviorAuthoringAsset`。
- 不把 Branch Root、Selector、Condition 或 TimelineNode 嵌入 Behavior Source 主图。
- 不修改 runtime Action Catalog 语义，不新增 gameplay fallback 路径。
- 不改变 Timeline 独立窗口；节点树只选择或打开 TimelineNode，Timeline 数据仍在独立 timeline 编辑器中编辑。

## Impact

- 受影响能力：
  - `character-behavior-editor-adapters`
  - `character-behavior-authoring-source-boundary`
  - `character-action-catalog`
- 受影响代码预计集中在 Editor-only adapter / window / tests。
- 运行时主链、`CharacterFramePipeline`、Action lifecycle、motion executor 和 animation presenter 不应被修改。
- 与 active change `refactor-ref-editor-source-port` 的关系：该 change 负责 Ref UI 源码级移植和 shell 体验，本 change 负责 Action Catalog 导航数据合同。若同时实施，catalog 导航必须通过稳定 adapter 接入移植后的同一个 Character Behavior Editor。
