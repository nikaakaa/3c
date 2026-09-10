## Why

当前 Timeline 编辑器的 UI Toolkit 视图不是用户要的编辑表面。项目已经安装 ParadoxNotion Slate Cinematic Sequencer，用户要求直接使用 Slate 的真实 `CutsceneEditor` UI，而不是重新实现一套相似的颜色、Clip 外观和时间尺。

Slate 的 `CutsceneEditor` 是独立的 IMGUI `EditorWindow`，它直接要求 `Slate.Cutscene`、`CutsceneGroup`、`CutsceneTrack` 和 `ActionClip` 层级；它不能直接接收 BTSMTL `TimelineData`。因此需要一个只存在于 Unity Editor 的适配层，把当前 BTSMTL Timeline 投影成 Slate 编辑器可读写的临时层级，再把 Slate UI 的改动转换回 BTSMTL 正式 Mutation。

本 change 将 Slate `CutsceneEditor` 作为真实 Timeline 编辑 UI，BTSMTL `TimelineData` 继续作为唯一持久化和运行时作者数据源。不复制 Slate 运行时，不保存 Slate Cutscene 资产，不把 Slate 数据变成第二个正式 Timeline。

## What Changes

- 正式 Timeline 打开入口改为调用 Slate `CutsceneEditor.ShowWindow(Cutscene)`，使用 Slate 的真实 IMGUI Timeline UI。
- 增加 Editor-only BTSMTL-to-Slate projection，将 Timeline owner、Track、Clip、Section、Curve 摘要和稳定 authoring identity映射到临时 Slate Cutscene 层级。
- 增加 Slate-to-BTSMTL mutation bridge：Slate UI 的移动、裁剪、删除、添加、Section 和时间编辑先转换成 BTSMTL 编辑命令，再经 `TimelineEditorSessionContext`、正式 owner、Mutation 和 Undo 写回。
- 明确 proxy 的双写边界：BTSMTL 是唯一持久化真相，但 Slate 原生 UI 会先修改临时 proxy；若要一个动作只有一个正式 Undo，必须提供 Slate transaction/Undo sink 扩展，不能假设原生窗口自动完成同步。
- Slate 临时层级关闭、Timeline owner 变化、Undo/Redo 或外部刷新时重新建立投影，禁止把临时 Slate 对象保存成资产或进入运行时编译链。
- 保留 BTSMTL 的 Skill Timeline、TreeClip、ActionContext、AnimationSlot、Document v7、Preview、Live Debug、Source Map 和 authoring identity。
- 删除上一轮错误方向中新增的 UI Toolkit Slate 仿制层、项目自定义 Timeline 皮肤和重复时间视口实现；不再维护第二个正式 Timeline UI。
- 不修改 Slate runtime、Cutscene 数据语义或播放链；允许对 Slate Editor 的 `CutsceneEditor` 增加最小 transaction sink 扩展，以隔离 proxy Undo 并把提交边界交给 BTSMTL adapter。

## Capabilities

### New Capabilities

- Editor-only BTSMTL Timeline 到 Slate `CutsceneEditor` 的投影与回写适配能力。

### Modified Capabilities

- `btsmtl-timeline-editor-preview`: Timeline 的正式编辑表面改由 Slate `CutsceneEditor` 拥有，BTSMTL 继续拥有正式数据、Mutation、Undo、Preview 和 Live Debug。

## Impact

- 主要入口：`Assets/GameScripts/Main/Runtime/BTSMTL/Timeline/Editor/Scripts/Tree/TimelineEditorMainWindow.cs` 和 Timeline editor open request composition。
- 新增范围：Editor-only Slate projection、identity map、snapshot/diff、BTSMTL mutation bridge 和临时 Cutscene 生命周期管理。
- 删除范围：上一轮 UI Toolkit 仿制 Slate 的 UXML/USS、Timeline 自定义 viewport/interaction/rendering 路径中仅为仿制 UI 新增的代码。
- 保留范围：`TimelineData`、Track/Clip/Section/TreeClip identity、`TimelineEditorSessionContext`、`ITimelineEditorMutationPort`、Undo、PreviewSession、Live Debug 和 Skill Timeline owner。
- 不修改 `Assets/ParadoxNotion/SLATE Cinematic Sequencer` 的 runtime、Cutscene 数据语义或播放器；Editor 目录允许增加最小 transaction hook，不保存临时 Cutscene，不增加 Slate runtime player、第二个时钟或第二个正式 Timeline compiler。
- 该 change 不包含 Character Build、PoseGraph、SkillGraph、TreeClip runtime 语义或 Timeline data schema 迁移。
