# Change: 重构 Committed Action Branch Editor 为 Ref 风格节点树

## Why
当前旧 Branch 编辑入口只是 `ScrollView + Box card` 的节点摘要列表，不是 Ref 节点编辑器形态。它能证明数据可读写，但不能满足“画节点树、连线、拖拽、按节点编辑”的工具目标，也容易让 Branch Authoring 工具链看起来像临时调试面板。

项目已经存在 `CharacterBehaviorEditorWindow` / `CharacterBehaviorRefPortedGraphView` 这条 Ref port 节点编辑器外壳。Branch 节点树不应再新造一套窗口或 GraphView，而应作为现有 Behavior Editor 的 Committed Branch 数据源接入，同时保持底层只读写本项目自己的 `CharacterActionDefinitionSO -> CommittedActionBranchAuthoring -> CommittedActionBranchDefinition -> CommittedActionBranchEvaluator` 数据管线。

## What Changes
- `Tools/3C/Character Behavior Editor` 是唯一节点树窗口入口，窗口内提供 Committed Branch mode。
- `CharacterBehaviorRefPortedGraphView` 增加 graph adapter 数据源能力，用同一套 Ref port Node / Port / Edge / SearchWindow 外壳展示 Behavior graph 与 Committed Action branch。
- 新增 `CommittedActionBranchRefPortedGraphAdapter`，把 Selector、Condition、Timeline 三类 branch node 映射到现有 Ref port graph shell。
- GraphView 的新增节点、删除节点、连线、断线、拖动位置和选择操作 MUST 通过 `CommittedActionBranchSerializedAdapter` 写回 `CharacterActionDefinitionSO`。
- Timeline 编辑保持独立 `CommittedActionTimelineEditorWindow`；Branch 节点树只负责选择 TimelineNode 并打开/聚焦独立 timeline 窗口，不内嵌 timeline panel。
- 保留 Ref 节点编辑器的交互意图：可缩放、拖拽、框选、端口连线、节点定位、SearchWindow 创建节点和基础布局辅助；不复制 Ref runtime、Taco tree 或 runner。
- 删除或停用 `RebuildGraphView()` 中基于 `Box card` 的伪图形展示，避免并行 UI 路径。
- 删除专用 `CommittedActionBranchGraphView` 和 `Tools/3C/Committed Action Branch Editor` 重复菜单，避免第二套节点编辑器入口。
- 增加 EditMode 测试和静态边界测试，证明 GraphView 只是 Editor-only 视图层，runtime 继续只消费本项目 compiler 输出。

## Non-Goals
- 不实现通用 Skill Editor。
- 不新增正式 Block、Attack、GuardCounter 玩法能力。
- 不把 Ref `BaseTree`、`RunnableTree`、`RunnableNode`、`TreeRunner`、`TimelinePlayer` 或 PlayableGraph 接入 runtime。
- 不新增第二套 Branch 数据结构、sample asset fallback、Resources fallback 或测试专用 runtime model。
- 不修改 `CharacterFramePipeline`、motion executor、animation presenter、blackboard writer 或 action lifecycle 权威边界。

## Impact
- Affected specs:
  - `committed-action-authoring-toolchain`
- Affected code after approval:
  - `Assets/Editor/Character/Graph/CharacterBehaviorEditorWindow.cs`
  - `Assets/Editor/Character/Graph/CharacterBehaviorRefPortedGraphView.cs`
  - `Assets/Editor/Character/Action/Branch/CommittedActionBranchEditorAdapters.cs`
  - `Assets/Editor/Character/Action/Branch/CommittedActionBranchRefPortedGraphAdapter.cs`
  - `Assets/Editor/Character/Action/Timeline/CommittedActionTimelineEditorWindow.cs`
  - `Assets/Tests/Editor/Character/Action/Branch/CommittedActionBranchEditorAdapterTests.cs`

## Validation
- `openspec validate refactor-committed-action-branch-editor-graphview --strict --no-interactive`
- 通过 Unity MCP 运行相关 EditMode 测试：
  - `Tests.Editor.Character.Action.Branch.CommittedActionBranchEditorAdapterTests`
  - 新增的 Ref-port GraphView / writeback / boundary 测试
- `dotnet build 3cDemo/Client/3C_Client/Assembly-CSharp-Editor.csproj --no-restore`
