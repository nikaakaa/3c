## 1. 准备与冲突确认
- [x] 1.1 读取本 change 的 `proposal.md`、`design.md` 和 spec delta。
- [x] 1.2 读取 `openspec/specs/committed-action-authoring-toolchain/spec.md`。
- [x] 1.3 读取 `openspec/specs/committed-action-timeline-editor/spec.md`。
- [x] 1.4 读取 `openspec/specs/character-behavior-editor-adapters/spec.md`。
- [x] 1.5 确认 `Timeline Editor 嵌入 Branch TimelineNode` 旧口径已在本 change 中改为独立窗口定位选中 TimelineNode。
- [x] 1.6 查找 `CharacterBehaviorEditorWindow` 的 Committed Branch mode 入口。
- [x] 1.7 查找 `CommittedActionBranchSerializedAdapter` 的 root、add、delete、connect、position 写回能力。
- [x] 1.8 查找 `CommittedActionTimelineEditorWindow` 的 selected TimelineNode 打开能力。
- [x] 1.9 对将修改的核心 editor symbol 运行 GitNexus impact，记录 direct callers、affected processes 和 risk。
- [x] 1.10 若 impact 为 HIGH 或 CRITICAL，先停下说明风险和拆分方案。

## 2. Root 合同与模板初始化
- [x] 2.1 定义 Editor-only root 判定规则，优先使用 branch `rootNodeId` 指向的 stable node。
- [x] 2.2 在 serialized adapter 中增加 root snapshot 标记或批准等价读取面。
- [x] 2.3 增加显式初始化空 branch 的 adapter 方法，创建正式 branch id、root node id 和最小 selector/timeline 模板。
- [x] 2.4 为 Dodge 提供显式模板初始化路径，生成 selector、Directional condition、Backstep condition、Directional timeline 和 Backstep timeline。
- [x] 2.5 缺失 root 时 validator 继续报错，不在 runtime/compiler 中隐藏补齐。
- [x] 2.6 root 删除请求必须被 adapter 拒绝并返回明确 diagnostic。
- [x] 2.7 root 复制、分组、stack 或作为普通 create option 创建必须被 GraphView 层禁止或不暴露。

## 3. Ref 风格 GraphView 体验
- [x] 3.1 GraphView node snapshot 增加 root/protected/canDelete/canCopy 或批准等价 capabilities。
- [x] 3.2 Root node 显示固定入口标记。
- [x] 3.3 Selector / Condition / Timeline node 显示稳定 node id、kind 和摘要。
- [x] 3.4 SearchWindow 只暴露正式可创建节点类型，不暴露 root 类型。
- [x] 3.5 GraphView 删除操作跳过或拒绝 protected root。
- [x] 3.6 结构变化后按 stable node id 保持 selection，而不是依赖数组 index。
- [x] 3.7 保持 `Tools/3C/Character Behavior Editor` 为唯一节点树菜单入口。

## 4. 节点属性面板
- [x] 4.1 为 Committed Branch mode 增加节点属性面板区域或批准等价节点内面板。
- [x] 4.2 选中 root / selector 时显示 root id、branch id、child 顺序和只读保护状态。
- [x] 4.3 选中 condition 时可编辑 condition kind。
- [x] 4.4 选中 condition 时可编辑 request kind。
- [x] 4.5 选中 condition 时可编辑 required fact id。
- [x] 4.6 选中 condition 时可编辑 expected variant。
- [x] 4.7 condition payload 写回后保存到同一 `CharacterActionDefinitionSO`。
- [x] 4.8 选中 timeline 时显示 timeline node id、duration seconds、body claim 和 channels。
- [x] 4.9 选中 timeline 时提供打开独立 Timeline Editor 的入口。
- [x] 4.10 面板绑定必须在节点删除、重排或新增后重新解析 stable node id。

## 5. 独立 Timeline 定位
- [x] 5.1 `Open Timeline` 只打开或聚焦独立 `CommittedActionTimelineEditorWindow`。
- [x] 5.2 Timeline window 使用 selected TimelineNode serialized adapter。
- [x] 5.3 Timeline window 不回调打开 Branch 专用窗口。
- [x] 5.4 Timeline window 不创建独立 Directional / Backstep 保存目标。
- [x] 5.5 Timeline window diagnostics 显示当前 action definition 和 timeline node id。

## 6. 边界清理
- [x] 6.1 确认没有 `Tools/3C/Committed Action Branch Editor` 菜单。
- [x] 6.2 确认没有专用 `CommittedActionBranchGraphView`。
- [x] 6.3 确认没有 Branch 专用 EditorWindow 作为第二节点树入口。
- [x] 6.4 确认 runtime 不引用 UnityEditor、GraphView、TimelinePlayer、RunnableTree、RunnableNode 或 Ref runner。
- [x] 6.5 确认 GraphView、node panel、selection 和 layout 不进入 runtime definition、rollback snapshot 或 lifecycle frame。

## 7. 自动测试
- [x] 7.1 添加 root snapshot 测试，确认 root node 被标记为 protected。
- [x] 7.2 添加空 branch 显式初始化测试，确认生成正式 root/template 并写回 `CharacterActionDefinitionSO`。
- [x] 7.3 添加 Dodge 模板初始化测试，确认生成 selector、两个 condition 和两个 timeline leaf。
- [x] 7.4 添加 root 删除被拒绝测试。
- [x] 7.5 添加删除普通节点会清理 child 引用但不会清理 root 合同的测试。
- [x] 7.6 添加 SearchWindow/create options 不暴露 root 的测试。
- [x] 7.7 添加 condition 属性面板写回 kind/request/fact/variant 的 adapter 或等价 UI 测试。
- [x] 7.8 添加 timeline 节点面板打开独立 Timeline Editor 的 adapter 测试。
- [x] 7.9 添加 selected TimelineNode 保存后只修改目标 timeline 的测试。
- [x] 7.10 添加静态测试，确认没有 Branch 专用窗口、专用 GraphView、重复菜单和嵌入 Timeline panel 旧路径。
- [x] 7.11 添加 runtime 边界静态测试，确认 Ref/Taco runner 和 Editor 类型不进入 runtime。

## 8. 验证
- [x] 8.1 运行 `openspec validate formalize-committed-action-branch-editor-workflow --strict --no-interactive`。
- [x] 8.2 运行 `dotnet build 3cDemo/Client/3C_Client/Assembly-CSharp-Editor.csproj --no-restore`。
- [x] 8.3 通过 Unity MCP 运行 `Tests.Editor.Character.Action.Branch.CommittedActionBranchEditorAdapterTests`。
- [x] 8.4 通过 Unity MCP 运行 `Tests.Editor.Character.Action.Timeline.CommittedActionTimelineEditorAdapterTests`。
- [x] 8.5 通过 Unity MCP 运行相关 Character Behavior editor adapter 定向 EditMode 测试。
