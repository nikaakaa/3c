## 1. 准备与边界确认
- [x] 1.1 读取本 change 的 `proposal.md`、`design.md` 和 spec delta。
- [x] 1.2 读取 `openspec/specs/committed-action-authoring-toolchain/spec.md`。
- [x] 1.3 读取 `openspec/specs/committed-action-timeline-editor/spec.md`。
- [x] 1.4 读取 `openspec/specs/character-behavior-editor-adapters/spec.md` 中 Ref UI 和 editor-only 边界。
- [x] 1.5 查找当前 `CommittedActionBranchEditorWindow` 的 card/list 伪图实现位置。
- [x] 1.6 查找现有 `CharacterBehaviorEditorWindow` / `CharacterBehaviorRefPortedGraphView` 的 Ref-port 节点编辑器入口。
- [x] 1.7 查找 Ref 节点编辑器中可借鉴的 Window / GraphView / Node / Port / Edge / SearchWindow 结构。
- [x] 1.8 对将修改的核心 editor symbol 运行 GitNexus impact，记录 direct callers、affected processes 和 risk。
- [x] 1.9 若 impact 为 HIGH 或 CRITICAL，先停下说明风险和拆分方案。

## 2. 移除错误 UI 路径
- [x] 2.1 删除或停用 `CommittedActionBranchEditorWindow` 中的 `ScrollView branchGraph` 字段。
- [x] 2.2 删除或替换 `RebuildGraphView()` 的 `Box card` 伪图展示。
- [x] 2.3 删除专用 `CommittedActionBranchGraphView`，不保留第二套 Branch node editor。
- [x] 2.4 删除 `CommittedActionBranchEditorWindow` 菜单 shim，不保留重复 Branch 节点树入口。
- [x] 2.5 保留 asset selection、Save、Validate 和独立 Timeline Editor 跳转的正式入口。

## 3. Ref-port GraphView 复用
- [x] 3.1 扩展 `CharacterBehaviorRefPortedGraphView` 支持 graph adapter 数据源。
- [x] 3.2 复用 `CharacterBehaviorRefPortedNodeView` 渲染 Branch selector / condition / timeline node。
- [x] 3.3 为 Selector node 映射输出 port。
- [x] 3.4 为 Condition node 映射输入 port 和输出 port。
- [x] 3.5 为 Timeline node 映射输入 port 和可选输出 port。
- [x] 3.6 为节点标题显示 node kind，并在摘要中显示 stable node id。
- [x] 3.7 为 Condition node 显示 condition kind、request kind、required fact 或 variant 摘要。
- [x] 3.8 为 Timeline node 显示 timeline id、duration 和主要 animation key 摘要。
- [x] 3.9 支持缩放、拖拽、框选、Frame All 和 Ref 风格 SearchWindow 节点创建。

## 4. Adapter 写回
- [x] 4.1 新增 `CommittedActionBranchRefPortedGraphAdapter`。
- [x] 4.2 GraphView populate 时从 `CommittedActionBranchSerializedAdapter.Capture()` 建立节点。
- [x] 4.3 GraphView populate 时按 `childNodeIds` 建立 edge。
- [x] 4.4 创建 edge 时通过 adapter 写入 parent node 的 `childNodeIds`。
- [x] 4.5 删除 edge 时通过 adapter 移除对应 child id。
- [x] 4.6 拖动节点时通过 adapter 写回 node position。
- [x] 4.7 新增节点时通过 adapter 创建 Selector / Condition / Timeline node。
- [x] 4.8 删除节点时通过 adapter 删除节点并清理其它节点的 child 引用。
- [x] 4.9 重连 edge 后保存，`CharacterActionDefinitionSO.ToDefinition()` MUST 看到同一份 branch 拓扑。
- [x] 4.10 确认 GraphView 不直接 new runtime `CommittedActionBranchDefinition` 作为保存目标。

## 5. Window 集成
- [x] 5.1 `CharacterBehaviorEditorWindow` 增加 Committed Branch mode。
- [x] 5.2 `Tools/3C/Character Behavior Editor` 作为唯一节点树窗口入口，窗口内提供 Committed Branch mode。
- [x] 5.3 GraphView selection 变更时记录 selected branch node 并显示 diagnostics。
- [x] 5.4 GraphView selection 选中 TimelineNode 后通过按钮打开独立 `CommittedActionTimelineEditorWindow`。
- [x] 5.5 Branch 节点树窗口不再内嵌 `CommittedActionRefPortedTimelineView`。
- [x] 5.6 Save 按钮保存 GraphView 对同一 serialized action definition 的修改。
- [x] 5.7 Validate 按钮显示正式 `CharacterActionDefinitionSO.Validate()` diagnostics。
- [x] 5.8 Timeline Editor 菜单直接打开独立 Timeline window，不经 Branch window 绕路。

## 6. Ref UI 迁移边界
- [x] 6.1 只复制或改写 Editor-only GraphView 相关 UI 资源。
- [x] 6.2 不复制 Ref `.meta`。
- [x] 6.3 不保留 `project://database/Assets/Addon/Taco` 或 Ref 绝对路径引用。
- [x] 6.4 不引用 Ref `BaseTree`、`RunnableTree`、`RunnableNode`、`TreeRunner`、`TimelinePlayer` 或 PlayableGraph runner。
- [x] 6.5 菜单和窗口标题只保留 `Character Behavior Editor` 节点树入口，不声明通用 Skill Editor。

## 7. 自动测试
- [x] 7.1 添加 GraphView populate 测试，确认节点数量、node id、node kind 和 edge 数来自 adapter snapshot。
- [x] 7.2 添加 edge 创建写回 `childNodeIds` 的测试。
- [x] 7.3 添加 edge 删除写回 `childNodeIds` 的测试。
- [x] 7.4 添加节点移动写回 position 的测试。
- [x] 7.5 添加节点删除清理 child 引用的测试。
- [x] 7.6 添加选中 TimelineNode 后独立 timeline adapter 指向该 node 的测试或批准等价 adapter 测试。
- [x] 7.7 添加保存后 `CharacterActionDefinitionSO.ToDefinition()` 可编译 GraphView 修改结果的测试。
- [x] 7.8 添加静态测试，确认 runtime 不引用 UnityEditor、GraphView、Ref/Taco runner 或 Branch editor view 类型。
- [x] 7.9 添加静态测试，确认 Branch Editor 不包含 `ScrollView branchGraph` / `Box card` / 专用 Branch GraphView / 重复 Branch 菜单旧路径或批准等价旧路径。
- [x] 7.10 添加静态测试，确认迁移资源不包含 Ref 项目路径引用。

## 8. 验证
- [x] 8.1 运行 `openspec validate refactor-committed-action-branch-editor-graphview --strict --no-interactive`。
- [x] 8.2 运行 `dotnet build 3cDemo/Client/3C_Client/Assembly-CSharp-Editor.csproj --no-restore`。
- [x] 8.3 通过 Unity MCP 运行 `Tests.Editor.Character.Action.Branch.CommittedActionBranchEditorAdapterTests`。
- [x] 8.4 通过 Unity MCP 运行新增 GraphView / writeback / boundary 定向 EditMode 测试。
