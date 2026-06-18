# Ref 源码移植盘点

## Timeline editor 职责映射
- Ref `TimelineEditorWindow` → 项目 `CommittedActionTimelineEditorWindow`，只负责窗口、正式 action definition / TimelineNode 绑定和 preview 入口。
- Ref `TimelineFieldView` → 项目 timeline field view / `CommittedActionRefPortedTimelineView` 中的 field 职责，必须持有 frame position map、scale、offset、locator、pan、zoom、rectangle selection、selection set、move leader、apply move、resize clamp 和 invalid preview。
- Ref `TimelineTrackView` → 项目 `CommittedActionTimelineTrackView`，负责 track field、drop area、clip view 创建和 context menu。
- Ref `TimelineTrackHandle` → 项目 `CommittedActionTimelineTrackHandle`，负责 track selection、delete、reorder drag。
- Ref `TimelineClipView` → 项目 `CommittedActionTimelineClipView`，只负责显示、选择、context menu，并把 move / resize 委托给 field view。
- Ref `DragManipulator` → 项目 Editor-only timeline drag manipulator，用于 move drag、locator drag 和 track handle drag。
- Ref `DragLineManipulator` → 项目 Editor-only timeline drag line manipulator，用于 left / right resize，不再通过 root pointer mode 猜测。
- Ref UXML / USS / icons → 项目 `RefPortedResources`，必须清理 Ref project path 和 `.meta` 依赖。

## TreeDesigner / GraphView 职责映射
- Ref TreeDesigner GraphView shell → 项目 `CharacterBehaviorRefPortedGraphView`。
- Ref node view / port / edge / SearchWindow → 项目 `CharacterBehaviorRefPortedNodeView`、GraphView ports、SearchWindow provider。
- Ref root / protected node capability → 项目 root node stable id、protected root 标记和 delete rejection。
- Ref node panel / inspector → 项目 `CharacterBehaviorEditorWindow` 中的 node property panel，通过 stable node id 和 serialized adapter 写回。
- Ref runtime tree access → 项目 `CharacterBehaviorAuthoringGraphAdapter` 或 `CommittedActionBranchRefPortedGraphAdapter`，只读写项目 authoring 数据。

## 禁止进入正式 runtime 的 Ref 内容
- Taco `TimelinePlayer`、`Timeline`、`Track`、`Clip` runtime object。
- Taco `BaseTree`、`RunnableTree`、`RunnableNode`、`TreeRunner`、`RootNode`、`EnterNode`。
- Ref `PlayableGraph` runner、Animator 直接驱动、sample asset 和 Ref `.meta`。
- Editor-only GraphView、node view、port、edge、selection、layout、window、preview view。

## 当前需要替换或删除的半移植路径
- `CommittedActionTimelineClipView` 的 root pointer mode / `ResolvePointerModeFromHandleBounds` / `ShouldApplyPointerDelta` / 局部 delta drag path。
- `CommittedActionRefPortedTimelineView` 内 selection move 的临时 tick 字典和旧 drag end 刷新流程需要收敛到 field view move leader / apply move。
- Timeline resize 必须由独立 drag line manipulator 触发，旧 content pointer 推断 resize 不再作为正式路径。
- Branch graph 任何 card/list 伪图、重复 branch editor 菜单或 Dodge-only branch authoring 正式入口必须不可达或删除。
