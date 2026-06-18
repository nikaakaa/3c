# Change: 迁移 Ref Timeline Editor 到正式 Action 配置

## 背景
当前 `add-character-behavior-editor-adapters` 已经要求 Committed Action Timeline Editor 默认编辑正式 `CharacterActionDefinitionSO`，磁盘现状也已经有 `CommittedActionTimelineEditorWindow` 指向 `Assets/Configs/3C/Action/Corin/Actions/Dodge/CorinDodgeActionDefinition.asset`。但当前实现仍然只是轻量仿制 Ref：能展示 Directional / Backstep 的 track 和 clip，能拖动/缩放已有 clip，能高亮当前 timeline 位置，但没有完整迁移 `Ref/wly970123` 的 timeline 编辑能力和预览逻辑。

Ref 的 `taco-editor` timeline 提供了更完整的 Unity 原生编辑器体验：track hierarchy、add track dropdown、track/clip selection、rectangle selector、locator drag、scroll/zoom、delete、clip inspector、clip resize/move/ease、drop 创建 clip、play/pause/speed 和 preview 绑定。需要迁移这些 Editor UI 能力，但数据源必须是本项目的 `CharacterActionDefinitionSO` / `DodgeCommittedActionBranchAuthoring` / `ActionTimelineTrackAuthoring` / `ActionTimelineClipAuthoring`，不能让 Ref runtime runner 进入正式 gameplay。

## 目标
- 将 Committed Action Timeline Editor 的正式目标固定为本项目 `CharacterActionDefinitionSO`，默认加载 Corin Dodge action definition，支持用户选择其它正式 action definition。
- 迁移 Ref/wly970123 timeline editor 的 UXML、USS、window、field、track handle、track view、clip view、inspector、manipulator、选择、拖拽、缩放、添加、删除和预览交互。
- 将迁移后的 UI 通过 adapter 读写本项目正式 timeline authoring 数据，而不是 Taco `Timeline`、`Track`、`Clip`、`TimelinePlayer` 或 scene object。
- 让 Dodge selector 的 Directional / Backstep timeline 可查看、可编辑、可保存、可校验、可编译为 runtime `CommittedActionBranchDefinition` / `ActionTimelineDefinition`。
- 提供 editor-only preview：先基于本项目 `CommittedActionBranchEvaluator` / `ActionTimelineEvaluator` 显示帧结果，再通过正式预览绑定展示动画 key、motion、window、cue 的编辑器预览状态。
- 增加测试证明正式 asset 可编辑/可编译、非法 timeline 报错、runtime 不引用 Ref runner / PlayableGraph / Editor 类型。

## 非目标
- 不实现通用 Skill Editor。
- 不改变 `CharacterFramePipeline`、motion executor、Animancer presenter、blackboard writer 或角色控制入口。
- 不让 `TimelinePlayer`、Taco `BaseTree`、`RunnableTree`、`RunnableNode`、Ref `TreeRunner` 或 Ref runtime `PlayableGraph` 进入正式 gameplay。
- 不把 Timeline track、GraphView lane、Animancer layer 当成 gameplay slot 或 claim 权威。
- 不新增 fallback 配置；缺正式 action definition、branch、timeline、track 或 preview binding 时报告正式错误。

## 现状依据
- 正式 Unity 项目位于 `3cDemo/Client/3C_Client`。
- 当前 editor 窗口：`3cDemo/Client/3C_Client/Assets/Editor/Character/Action/Timeline/CommittedActionTimelineEditorWindow.cs`。
- 当前轻量仿 Ref 视图：`3cDemo/Client/3C_Client/Assets/Editor/Character/Action/Timeline/CommittedActionRefPortedTimelineView.cs`。
- 正式 Dodge asset：`3cDemo/Client/3C_Client/Assets/Configs/3C/Action/Corin/Actions/Dodge/CorinDodgeActionDefinition.asset`。
- 正式 runtime timeline model：`ActionTimelineDefinition`、`ActionTimelineEvaluator`、`CommittedActionBranchEvaluator`。
- Ref 可迁移 UI：`Ref/wly970123/taco-editor/Assets/Addon/Taco/Timeline/Editor` 下的 `TimelineEditorWindow`、`TimelineFieldView`、`TimelineTrackHandle`、`TimelineTrackView`、`TimelineClipView`、UXML、USS 和 inspector view。
- Ref 禁入 gameplay：`Ref/wly970123/taco-editor/Assets/Addon/Taco/Timeline/Scripts/TimelinePlayer.cs` 使用 `PlayableGraph`、Animator、Audio、FixedUpdate 和 root motion，只能作为 editor preview 边界参考，不能成为正式 gameplay runner。

## 影响范围
- Affected specs:
  - `committed-action-timeline-editor`
  - `character-action-catalog`
  - `dodge-action`
- Editor-only timeline window、view、resources 和 tests。
- `CharacterActionDefinitionSO` 的 serialized adapter 或 validator 可能需要增加正式编辑入口，但不得改变 runtime 数据模型语义。
- Dodge 示例正式 asset 可能需要补齐 schema / stable id / preview binding 字段；如果字段缺失，应通过正式数据模型 proposal 或本 change spec 补齐，不做隐藏默认值。

## 验证
- `openspec validate migrate-ref-timeline-editor-to-formal-action-config --strict --no-interactive`
- 定向 EditMode 测试覆盖 timeline editor adapter、formal asset compile、invalid graph/timeline validation、runtime static boundary、preview evaluator。
- Unity MCP 可用时运行相关 EditMode 测试；不可用时记录未执行测试名和原因。
