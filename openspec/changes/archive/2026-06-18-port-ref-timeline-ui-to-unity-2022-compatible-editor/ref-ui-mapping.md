# Ref UI 映射记录

## Window Shell

- Ref `TimelineEditorWindow` 的窗口生命周期、工具栏、保存、校验和预览入口映射到 `CommittedActionTimelineEditorWindow`。
- 本项目窗口只接收正式 `CharacterActionDefinitionSO`，菜单名保持 `Tools/3C/Committed Action Timeline Editor`。
- Ref 的 scene binding、runtime runner、`TimelinePlayer` 和 PlayableGraph 入口不迁移。

## Timeline Field

- Ref `TimelineFieldView` 的 ruler、grid、locator、scroll、zoom、中键 pan、F 定位和 rectangle selector 映射到 `CommittedActionRefPortedTimelineView`。
- 正式编辑语义使用 seconds ruler 和 tick grid；Ref 的 frame map 只能作为视图实现参考，写回必须通过 seconds authoring / compiled tick adapter。
- 数据源来自 `CommittedActionTimelineEditorModel` snapshot，不读 Ref timeline 数据。

## Track Handle / Track View

- Ref `TimelineTrackHandle` 的名称、类型和选中视觉映射为程序化 UI，避免导入 Unity 2023 / Taco UXML。
- Ref `TimelineTrackView` 的空轨道、clip 容器、track 拖拽排序映射为程序化 UI。
- 可新增 track 只允许正式 `ActionTimelineTrackKind`，clip kind 由 track kind 约束。

## Clip View

- Ref `TimelineClipView` 的 label、选择、多选、拖拽、左右 resize 映射到正式 adapter 操作。
- clip identity 使用本项目 authoring stable id，避免 reorder / delete 后依赖 `SerializedProperty.propertyPath`。
- 暂不迁移 Ref 的 ease-in / ease-out 语义，因为正式 runtime 未承认该输出。

## Inspector

- Ref inspector view 的 payload 编辑映射到正式 `ActionTimelineClipPayloadAuthoring`。
- Animation 对应 `AnimationKey`，Motion 对应 `Motion`，Hitbox / Cancel 对应 `Window`，Cue 对应 `Cue`。
- 多选时只显示多选状态，不创建 Ref inspector 的独立数据模型。
