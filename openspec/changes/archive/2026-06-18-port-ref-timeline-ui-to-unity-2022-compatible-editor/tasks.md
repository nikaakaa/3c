# Tasks

## 0. 前置核对
- [x] 0.1 读取 `AGENTS.md`、`openspec/AGENTS.md`、`openspec/project.md`。
- [x] 0.2 运行 `openspec list` 和 `openspec list --specs`。
- [x] 0.3 读取本 change 的 `proposal.md`、`design.md`、`tasks.md` 和 spec delta。
- [x] 0.4 读取相关 active change：`migrate-ref-timeline-editor-to-formal-action-config`、`refactor-character-behavior-authoring-source-boundary`。
- [x] 0.5 读取当前 `CommittedActionTimelineEditorWindow.cs`、`CommittedActionRefPortedTimelineView.cs`、`CommittedActionTimelineEditorAdapters.cs`。
- [x] 0.6 读取 Ref `TimelineEditorWindow.cs`、`TimelineFieldView.cs`、`TimelineTrackHandle.cs`、`TimelineTrackView.cs`、`TimelineClipView.cs`、`TimelineClipInspectorView.cs`。
- [x] 0.7 对将要修改的函数、类、方法运行 GitNexus `impact`。
- [x] 0.8 记录 HIGH / CRITICAL impact 的风险、直接调用方和受影响流程。

## 1. 当前实现安全清点
- [x] 1.1 列出当前 Timeline Editor 所有 C# 文件、UXML、USS 和 meta 文件。
- [x] 1.2 检查是否仍存在 Unity 2022 不安全的 Ref UXML 直接导入。
- [x] 1.3 检查是否存在手写或复制的 Ref `.meta`。
- [x] 1.4 检查当前 view 是否引用不存在、崩溃过或未验证的 UXML path。
- [x] 1.5 清理或替换不稳定资源入口。
- [x] 1.6 保留正式可打开的最小 editor window 作为后续迁移基线。

## 2. Ref UI 对照表
- [x] 2.1 建立 `TimelineEditorWindow` 到本项目 window shell 的功能映射。
- [x] 2.2 建立 `TimelineFieldView` 到本项目 field view 的功能映射。
- [x] 2.3 建立 `TimelineTrackHandle` 到本项目 track handle 的功能映射。
- [x] 2.4 建立 `TimelineTrackView` 到本项目 track view 的功能映射。
- [x] 2.5 建立 `TimelineClipView` 到本项目 clip view 的功能映射。
- [x] 2.6 建立 inspector view 到本项目 payload inspector 的字段映射。
- [x] 2.7 标记暂不迁移能力：Ref runtime runner、`TimelinePlayer`、PlayableGraph、scene object binding。

## 3. Unity 2022 资源迁移协议
- [x] 3.1 制定 UXML 转换规则：namespace、style 引用、GUID 引用、Resources 路径。
- [x] 3.2 制定 USS 转换规则：custom property、类名、资源路径。
- [x] 3.3 确认迁移资源只放在 Editor-only 路径。
- [x] 3.4 禁止复制 Ref `.meta`，由 Unity 2022 自动生成。
- [x] 3.5 每次只导入一个 UXML / USS 资源并验证 Unity Editor 可重新加载。
- [x] 3.6 增加静态检查，禁止项目内出现直接复制的 Ref `project://database/Assets/Addon/Taco` 样式引用。

## 4. Editor Timeline Model
- [x] 4.1 定义 editor-only timeline model 的 variant、duration、track、clip、payload、validation state。
- [x] 4.2 确认 track / clip 是否已有可持久化 stable id。
- [x] 4.3 缺 stable id 时补正式 authoring 字段和迁移校验。
- [x] 4.4 实现从 `CharacterActionDefinitionSO` 读取 Directional / Backstep timeline 的 snapshot。
- [x] 4.5 实现 model transaction 写回 serialized adapter。
- [x] 4.6 实现 Undo / dirty / save / reload 后 model 与 asset 一致。
- [x] 4.7 编写 model 读写测试。

## 5. Window Shell
- [x] 5.1 保持菜单名 `Tools/3C/Committed Action Timeline Editor`。
- [x] 5.2 默认加载正式 `CorinDodgeActionDefinition.asset`。
- [x] 5.3 ObjectField 限制为 `CharacterActionDefinitionSO`。
- [x] 5.4 实现 Directional / Backstep variant 切换。
- [x] 5.5 实现 Save / Validate / Preview toolbar 状态。
- [x] 5.6 窗口不显示或声明通用技能编辑器。

## 6. Timeline Field View
- [x] 6.1 迁移或重建 seconds ruler。
- [x] 6.2 迁移或重建 tick grid。
- [x] 6.3 实现 timeline position map，写回保持 seconds authoring / compiled tick 语义。
- [x] 6.4 实现 locator click 和 drag。
- [x] 6.5 实现 scroll。
- [x] 6.6 实现 zoom。
- [x] 6.7 实现中键 pan。
- [x] 6.8 实现 F 定位选中 clip 或当前 preview local time / local tick。
- [x] 6.9 实现 rectangle selector 基础框选。
- [x] 6.10 编写 field view 纯逻辑或 UI Toolkit 结构测试。

## 7. Track View
- [x] 7.1 实现 track handle 视觉和选中状态。
- [x] 7.2 实现 track view 视觉和空轨道状态。
- [x] 7.3 实现 add track dropdown，限制正式 `ActionTimelineTrackKind`。
- [x] 7.4 实现 track delete。
- [x] 7.5 实现 track reorder。
- [x] 7.6 实现 track kind 与可用 clip kind 的约束。
- [x] 7.7 编写 add / delete / reorder track 写回测试。

## 8. Clip View
- [x] 8.1 实现 clip 视觉结构和 label。
- [x] 8.2 实现 clip 选择和多选。
- [x] 8.3 实现 add clip dropdown，限制正式 `ActionTimelineClipKind`。
- [x] 8.4 实现 clip delete。
- [x] 8.5 实现 clip move。
- [x] 8.6 实现 clip left resize。
- [x] 8.7 实现 clip right resize。
- [x] 8.8 实现非法 clip 视觉状态。
- [x] 8.9 暂不支持 runtime 未承认的 ease-in / ease-out 语义。
- [x] 8.10 编写 add / delete / move / resize clip 写回测试。

## 9. Payload Inspector
- [x] 9.1 选中 Animation clip 时显示并编辑 AnimationKey payload。
- [x] 9.2 选中 Motion clip 时显示并编辑 Motion payload。
- [x] 9.3 选中 HitboxWindow / CancelWindow clip 时显示并编辑 Window payload。
- [x] 9.4 选中 Cue clip 时显示并编辑 Cue payload。
- [x] 9.5 多选时显示共同字段或明确不可编辑状态。
- [x] 9.6 编写 payload 写回和 reload 测试。

## 10. Preview Data
- [x] 10.1 Preview 当前 local tick 调用正式 `CommittedActionBranchEvaluator`。
- [x] 10.2 显示 selected node id。
- [x] 10.3 显示 animation key。
- [x] 10.4 显示 motion spec。
- [x] 10.5 显示 active window facts。
- [x] 10.6 显示 cue requests。
- [x] 10.7 缺 preview binding 时显示未绑定状态，不查找 scene object。
- [x] 10.8 编写 preview adapter 与 runtime evaluator 一致测试。

## 11. 边界与命名测试
- [x] 11.1 增加 runtime 静态边界测试：不引用 UnityEditor、GraphView、TimelinePlayer、PlayableGraph、Taco runner。
- [x] 11.2 增加 editor resource 静态检查：不保留 Ref `project://database/Assets/Addon/Taco` 引用。
- [x] 11.3 增加命名边界测试：菜单、窗口标题、文档不称为通用技能编辑器。
- [x] 11.4 增加 Graph Editor 边界测试：不复制 Dodge timeline 数据。

## 12. 验证
- [x] 12.1 运行 `openspec validate port-ref-timeline-ui-to-unity-2022-compatible-editor --strict --no-interactive`。
- [x] 12.2 通过 Unity MCP 尽量运行相关 EditMode 测试。
- [x] 12.3 Unity MCP 不可用时记录未执行测试名和原因。
- [x] 12.4 运行 `detect_changes({scope:"all"})` 并记录影响范围。
