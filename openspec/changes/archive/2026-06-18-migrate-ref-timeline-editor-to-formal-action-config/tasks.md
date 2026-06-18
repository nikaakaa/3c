# Tasks

## 0. 前置核对
- [x] 0.1 读取 `AGENTS.md`、`openspec/AGENTS.md`、`openspec/project.md`。
- [x] 0.2 运行 `openspec list` 和 `openspec list --specs`。
- [x] 0.3 确认 Unity 项目根为 `3cDemo/Client/3C_Client`。
- [x] 0.4 重新读取当前 `CommittedActionTimelineEditorWindow.cs`、`CommittedActionRefPortedTimelineView.cs`、`CharacterActionDefinitionSO.cs`、`CommittedActionBranchTimelineAuthoring.cs`。
- [x] 0.5 对将要修改的 editor window、timeline view、adapter、validator、preview 相关类运行 GitNexus `impact`。
- [x] 0.6 记录 HIGH / CRITICAL impact 的风险、直接调用方和受影响流程。

## 1. Ref Timeline 迁移清单
- [x] 1.1 读取 Ref `TimelineEditorWindow.cs` 并列出可迁移 window/toolbar/track hierarchy 功能。
- [x] 1.2 读取 Ref `TimelineFieldView.cs` 并列出 marker、locator、scroll、zoom、selection 功能。
- [x] 1.3 读取 Ref `TimelineTrackHandle.cs`、`TimelineTrackView.cs` 并列出 track 操作功能。
- [x] 1.4 读取 Ref `TimelineClipView.cs`、clip inspector view 并列出 clip 操作功能。
- [x] 1.5 读取 Ref UXML / USS / 图标资源并建立迁移路径映射。
- [x] 1.6 建立禁入清单：`TimelinePlayer`、Taco runtime tree、PlayableGraph、root motion、副作用 runtime。

## 2. Formal ActionDefinition Adapter
- [x] 2.1 新增或重构 editor-only timeline model adapter。
- [x] 2.2 Adapter 以 `CharacterActionDefinitionSO` 为唯一正式输入。
- [x] 2.3 Adapter 支持读取 Dodge `directionalTimeline` 与 `backstepTimeline`。
- [x] 2.4 Adapter 支持读取通用 `committedActionBranchTimeline`。
- [x] 2.5 Adapter 支持 track add / remove / reorder。
- [x] 2.6 Adapter 支持 clip add / remove / move / resize。
- [x] 2.7 Adapter 支持 payload 编辑：AnimationKey、Motion、HitboxWindow、CancelWindow、Cue。
- [x] 2.8 Adapter 使用 `SerializedObject` / `SerializedProperty`、Undo、dirty 和正式 save，不绕过 Unity serialization。

## 3. Ref UI 资源迁移
- [x] 3.1 迁移或替换 Timeline window UXML。
- [x] 3.2 迁移或替换 Timeline field UXML。
- [x] 3.3 迁移或替换 Timeline track handle UXML / USS。
- [x] 3.4 迁移或替换 Timeline track view UXML / USS。
- [x] 3.5 迁移或替换 Timeline clip view UXML / USS。
- [x] 3.6 迁移必要图标和 USS custom properties。
- [x] 3.7 所有迁移资源放入 Editor-only 路径。

## 4. Timeline 编辑交互
- [x] 4.1 实现 add track dropdown，限制为正式 `ActionTimelineTrackKind`。
- [x] 4.2 实现 track selection 和 Delete 删除。
- [x] 4.3 实现 track reorder。
- [x] 4.4 实现 clip add dropdown，限制为 track 允许的 `ActionTimelineClipKind`。
- [x] 4.5 实现 clip move。
- [x] 4.6 实现 clip left / right resize。
- [x] 4.7 实现 clip invalid 视觉状态。
- [x] 4.8 实现 rectangle selector 和多选。
- [x] 4.9 实现 marker click、locator drag、playhead local time / local tick label。
- [x] 4.10 实现 scroll、zoom、中键平移和 F 定位。
- [x] 4.11 实现 inspector 显示与编辑 selected track / clip payload。

## 5. 保存、校验、编译
- [x] 5.1 Save 写回正式 `CharacterActionDefinitionSO`。
- [x] 5.2 Save 后运行 `CharacterActionDefinitionSO.Validate()`。
- [x] 5.3 Timeline editor validator 覆盖非法 seconds / tick 区间、空 payload、非法 track/clip kind、缺 required timeline。
- [x] 5.4 编译路径使用 `CharacterActionDefinitionSO.ToDefinition()`。
- [x] 5.5 编译结果能得到 `CommittedActionBranchDefinition`。
- [x] 5.6 编译结果能得到 Directional / Backstep `ActionTimelineDefinition`。
- [x] 5.7 确认 Dodge runtime motion、animation key、duration ticks、window 和 cue 只来自 selected timeline。
- [x] 5.8 确认旧 Directional / Backstep variant 字段只作为迁移输入或诊断，不作为 runtime fallback。
- [x] 5.9 删除或降级 sample-only authoring / compiled runtime definition 的 editor 入口。

## 6. Preview
- [x] 6.1 新增 editor-only preview adapter。
- [x] 6.2 Preview 当前 local tick 调用 `CommittedActionBranchEvaluator`。
- [x] 6.3 Preview 展示 selected node id。
- [x] 6.4 Preview 展示 animation key。
- [x] 6.5 Preview 展示 motion spec。
- [x] 6.6 Preview 展示 active window facts。
- [x] 6.7 Preview 展示 cue requests。
- [x] 6.8 Preview 支持 Directional / Backstep selector context 切换。
- [x] 6.9 Preview binding 缺失时报正式未绑定状态，不使用 fallback scene object。
- [x] 6.10 如实现视觉预览，视觉预览代码只能位于 Editor assembly。

## 7. Graph Editor 关系收敛
- [x] 7.1 Character Behavior Editor 保留 root / Locomotion leaf / CommittedAction leaf 展示职责。
- [x] 7.2 Graph Editor 不复制 Dodge timeline 数据。
- [x] 7.3 Graph Editor 可定位或打开 Committed Action Timeline Editor。
- [x] 7.4 文档、菜单、窗口标题不使用通用 Skill Editor 命名。

## 8. 自动测试
- [x] 8.1 增加 adapter 读取正式 Dodge asset 测试。
- [x] 8.2 增加 add/remove/reorder track 写回测试。
- [x] 8.3 增加 add/move/resize/delete clip 写回测试。
- [x] 8.4 增加 payload inspector 写回测试。
- [x] 8.5 增加保存后 `ToDefinition()` 编译 Directional / Backstep 测试。
- [x] 8.6 增加非法 timeline 报错测试。
- [x] 8.7 增加 preview adapter 与 `CommittedActionBranchEvaluator` 输出一致测试。
- [x] 8.8 增加 runtime 静态边界测试：不引用 UnityEditor、GraphView、TimelinePlayer、PlayableGraph、Taco runner。
- [x] 8.9 增加命名边界测试：不出现通用 Skill Editor。

## 9. 验证
- [x] 9.1 运行 `openspec validate migrate-ref-timeline-editor-to-formal-action-config --strict --no-interactive`。
- [x] 9.2 通过 Unity MCP 尽量运行相关 EditMode 测试。
- [x] 9.3 Unity MCP 不可用时记录未执行测试名和原因。
- [x] 9.4 运行 `detect_changes({scope:"all"})` 并记录影响范围。
