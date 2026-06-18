# Change: 正式化 Committed Action 作者工具链

## Why
当前运行时已经有 `CommittedActionBranchDefinition`、selector / condition / timeline 评估和 `CharacterActionDefinitionSO` 数据入口，但工具链没有闭环：行为图编辑器仍在编辑 behavior source 拓扑，Timeline Editor 只编辑 Dodge Directional / Backstep clip，Dodge 运行时配置仍通过 `dodgeCommittedActionBranch` 特例生成固定节点树。设计者无法从一个正式 Action Definition 中看到、编辑、验证并导航 Committed Action branch 节点树，也无法确信编辑器保存的数据就是运行时评估的数据。

## What Changes
- 新增正式 Committed Action Branch Authoring 工具链：以 `CharacterActionDefinitionSO` 为入口，保存 selector、condition、timeline 节点树和节点布局，编译为现有 `CommittedActionBranchDefinition`。
- 新增或正式化 Committed Action Branch Editor：显示 Action branch 节点树，支持 selector / condition / timeline 节点的最小 CRUD、稳定 child 顺序和选中节点 inspector。
- 将 Timeline Editor 收敛为选中 TimelineNode 的 timeline panel / adapter；独立菜单可保留为快捷入口，但不能成为第二数据权威。
- **BREAKING** 将 `Action.Dodge` 从 Dodge 专用 `DodgeCommittedActionBranchAuthoring` 正式迁移到通用 branch authoring；不会保留隐藏 fallback 或并行正式路径。
- 明确 Character Behavior Editor 只编辑 behavior source 拓扑，不复制或持有 Action branch / timeline payload。
- 增加 EditMode 自动测试和静态边界验证，覆盖 editor adapter -> serialized asset -> runtime definition -> evaluator 的完整数据闭环。

## Impact
- Affected specs: `committed-action-authoring-toolchain`、`committed-action-timeline-editor`、`character-action-catalog`、`dodge-action`、`character-behavior-editor-adapters`
- Affected code: `3cDemo/Client/3C_Client/Assets/Scripts/Character/Action/Config/CharacterActionDefinitionSO.cs`、`3cDemo/Client/3C_Client/Assets/Scripts/Character/Action/Timeline/Config/CommittedActionBranchTimelineAuthoring.cs`、`3cDemo/Client/3C_Client/Assets/Scripts/Character/Action/Branch/Model/CommittedActionBranchDefinition.cs`、`3cDemo/Client/3C_Client/Assets/Editor/Character/Action/Timeline/CommittedActionTimelineEditorWindow.cs`、`3cDemo/Client/3C_Client/Assets/Editor/Character/Action/Timeline/CommittedActionTimelineEditorAdapters.cs`、`3cDemo/Client/3C_Client/Assets/Editor/Character/Graph/CharacterBehaviorEditorWindow.cs`
- Dependencies: `refactor-action-timeline-time-authority` 应先归档或在实现时按其 delta 执行，因为当前 active change 已把 ActionTimeline 改为 seconds authoring -> deterministic tick runtime。
- 不做实现代码变更；本 change 只规划正式 authoring 工具链和验收标准。
