# Change: 规范 Committed Action Branch 编辑体验

## Why
当前 Committed Branch 已经接入 Ref 风格 GraphView 并写回正式 `CharacterActionDefinitionSO`，但编辑体验仍缺少 Ref TreeDesigner 的关键约束：固定入口节点、节点属性编辑面板、模板化初始化和资产驱动入口。

同时现行 specs 存在一处口径冲突：`committed-action-authoring-toolchain` 已要求 Timeline 使用独立窗口，而 `committed-action-timeline-editor` 仍残留“嵌入 Branch TimelineNode panel”的旧说法。本变更先统一规格，再规划实现。

## What Changes
- 规定 `Tools/3C/Character Behavior Editor` 仍是唯一节点树入口，Committed Branch 只是其中的 action branch mode。
- 为 Committed Action branch editor 定义固定 root 工作流：每个 branch 必须有一个受保护 root node，root 不可删除、不可复制、不可作为普通节点创建。
- 定义正式模板初始化/修复入口：空 branch 必须通过显式初始化命令生成正式 root/template，不允许隐藏 fallback。
- 定义 Ref 风格节点属性面板：选中 root/selector/condition/timeline node 后在编辑器内直接编辑对应 serialized authoring 字段，不要求用户手翻数组。
- 明确 Timeline 仍是独立窗口：Branch graph 只选择 TimelineNode 并打开/定位 Timeline Editor，Timeline Editor 通过 selected TimelineNode adapter 读写同一份数据。
- 增加自动测试要求，覆盖 root 保护、模板初始化、属性面板写回、selected TimelineNode 定位和 runtime/editor 边界。

## Impact
- Affected specs:
  - `committed-action-authoring-toolchain`
  - `committed-action-timeline-editor`
  - `character-behavior-editor-adapters`
- Affected code:
  - `Assets/Editor/Character/Graph/CharacterBehaviorEditorWindow.cs`
  - `Assets/Editor/Character/Graph/CharacterBehaviorRefPortedGraphView.cs`
  - `Assets/Editor/Character/Action/Branch/CommittedActionBranchEditorAdapters.cs`
  - `Assets/Editor/Character/Action/Branch/CommittedActionBranchRefPortedGraphAdapter.cs`
  - `Assets/Editor/Character/Action/Timeline/CommittedActionTimelineEditorWindow.cs`
  - `Assets/Tests/Editor/Character/Action/Branch/CommittedActionBranchEditorAdapterTests.cs`
  - `Assets/Tests/Editor/Character/Action/Timeline/CommittedActionTimelineEditorAdapterTests.cs`

## Out of Scope
- 不实现通用 Skill Editor。
- 不新增 Branch 专用窗口、Branch 专用 GraphView 或第二节点树菜单。
- 不把 Timeline 嵌回节点树窗口。
- 不让 Ref/Taco runtime、TimelinePlayer、RunnableTree 或 PlayableGraph runner 进入正式 runtime。
- 不修改 `CharacterFramePipeline`、motion executor、Animancer presenter、blackboard writer 或 gameplay slot/claim 权威。
