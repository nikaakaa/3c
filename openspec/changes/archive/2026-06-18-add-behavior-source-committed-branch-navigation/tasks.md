## 1. 准备与冲突确认
- [x] 1.1 读取本 change 的 `proposal.md`、`design.md`、`tasks.md` 和 spec delta。
- [x] 1.2 读取 `openspec/specs/character-behavior-editor-adapters/spec.md`。
- [x] 1.3 读取 `openspec/specs/character-behavior-authoring-source-boundary/spec.md`。
- [x] 1.4 读取 `openspec/specs/committed-action-authoring-toolchain/spec.md`。
- [x] 1.5 确认 current specs 仍要求 `Character Behavior Editor` 是唯一节点树入口。
- [x] 1.6 确认 active change `add-committed-action-timeline-scene-preview-binding` 不修改 Behavior Source 到 Branch 的导航边界。
- [x] 1.7 查找 `CharacterBehaviorEditorWindow` 的 mode 切换、asset field 和 branch reload 入口。
- [x] 1.8 查找 `CharacterBehaviorRefPortedGraphView` 的 node selection 和 MouseDown 处理。
- [x] 1.9 对将修改的核心 editor symbol 运行 GitNexus impact 并记录风险。
- [x] 1.10 若 impact 为 HIGH 或 CRITICAL，先说明风险和拆分方案。

## 2. GraphView Open Gesture
- [x] 2.1 为 node view 增加双击或批准等价 open gesture 事件。
- [x] 2.2 保留单击 `NodeSelected` 行为不变。
- [x] 2.3 确保双击只在节点本体触发，不影响空白区域框选。
- [x] 2.4 确保双击不影响端口连线和节点拖拽。
- [x] 2.5 在 GraphView 层公开 stable node id 的 open callback。
- [x] 2.6 open callback 必须使用 stable node id，不使用数组 index。

## 3. Behavior Source 到 Branch 导航
- [x] 3.1 在 `CharacterBehaviorEditorWindow` 订阅 GraphView node open callback。
- [x] 3.2 当前 mode 为 Behavior Source 且节点 kind 为 `CommittedActionLeaf` 时处理导航。
- [x] 3.3 导航时切换到 Committed Branch mode。
- [x] 3.4 导航时保留 Behavior Source asset 引用，避免返回时丢失。
- [x] 3.5 若当前已有 `CharacterActionDefinitionSO`，导航使用该 action definition。
- [x] 3.6 若当前没有 action definition，导航使用正式默认 Dodge action definition。
- [x] 3.7 若正式 action definition 缺失，显示明确 diagnostic，不创建 fallback branch。
- [x] 3.8 导航完成后 populate Branch graph，并选中 Branch Root 或批准等价 branch 起点。

## 4. 反向与工具边界
- [x] 4.1 保持 toolbar 的 `Behavior Source` 和 `Committed Branch` mode 切换可用。
- [x] 4.2 保持 `Open Committed Action Timeline` 只打开独立 Timeline Window。
- [x] 4.3 确认 Branch mode 保存只写 `CharacterActionDefinitionSO`。
- [x] 4.4 确认 Behavior Source mode 保存只写 `CharacterBehaviorAuthoringAsset`。
- [x] 4.5 确认没有新增 Branch 专用窗口、重复菜单或 embedded timeline panel。
- [x] 4.6 确认 runtime 不引用新增 editor navigation 类型。

## 5. 自动测试
- [x] 5.1 添加 GraphView node double-click/open callback 的 EditMode 测试。
- [x] 5.2 添加单击只 selection、不触发 open callback 的 EditMode 测试。
- [x] 5.3 添加 Behavior Source 中双击 `CommittedActionLeaf` 切换到 Committed Branch mode 的 editor adapter 测试。
- [x] 5.4 添加导航后使用当前 action definition 的测试。
- [x] 5.5 添加当前 action definition 为空时定位正式默认 Dodge action definition 的测试。
- [x] 5.6 添加 action definition 缺失时只显示 diagnostic、不生成 fallback branch 的测试。
- [x] 5.7 添加导航后 Behavior Source asset 引用保持不丢失的测试。
- [x] 5.8 添加 Behavior Source 保存不修改 branch/timeline 的边界测试。
- [x] 5.9 添加 Branch 保存不修改 source topology 的边界测试。
- [x] 5.10 添加静态测试确认没有重复 Branch 窗口、重复菜单、embedded timeline panel 或 runtime editor 引用。

## 6. 验证
- [x] 6.1 运行 `openspec validate add-behavior-source-committed-branch-navigation --strict --no-interactive`。
- [x] 6.2 运行 `dotnet build 3cDemo/Client/3C_Client/Assembly-CSharp-Editor.csproj --no-restore`。
- [x] 6.3 通过 Unity MCP 运行 `Tests.Editor.Character.Behavior.EditorAdapters.CharacterBehaviorEditorAdapterTests`。
- [x] 6.4 通过 Unity MCP 运行 `Tests.Editor.Character.Behavior.CharacterBehaviorAuthoringSourceBoundaryTests`。
- [x] 6.5 通过 Unity MCP 运行相关 Branch editor adapter 定向 EditMode 测试。
