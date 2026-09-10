## Context

用户要求使用 Slate 插件的真实 Timeline UI。Slate 的正式入口是 `Slate.CutsceneEditor.ShowWindow(Cutscene)`，实现为独立的 IMGUI `EditorWindow`，内部读取 `Cutscene` 的 Group/Track/ActionClip 层级并自行处理时间尺、Clip 命中、拖动、缩放、Undo 和播放状态。

BTSMTL Timeline 的正式数据仍由 `TimelineData`、Track、Clip、Section、TreeClip、Session、Mutation、Undo、Preview 和 Live Debug 拥有。两套模型不能直接互换：Slate UI 不能直接读取 `TimelineData`，BTSMTL 也不能把普通 Clip 直接当成 Slate `ActionClip`。

因此本 change 采用一个 Editor-only bridge。Slate UI 作为作者看到和操作的真实编辑表面；BTSMTL TimelineData 作为唯一持久化真相。临时 Slate 对象只负责满足 Slate 编辑器的读取和交互要求，不能保存、编译或进入运行时。

## Goals / Non-Goals

**Goals:**

- 直接使用 Slate `CutsceneEditor` 的窗口、时间尺、Group/Track、Clip、拖动、缩放、选择、Inspector 和播放控制 UI。
- 将当前 BTSMTL Timeline 投影为 Slate 能编辑的临时层级，并保留每个 Track、Clip、Section、TreeClip 的 authoring identity 映射。
- 将 Slate UI 的有效改动转换为 BTSMTL `TimelineEditorSessionContext` 和 `ITimelineEditorMutationPort` 操作，使用正式 owner 和 Undo。
- Slate 窗口重新打开、Undo/Redo、外部 Timeline 刷新或 owner 切换时，能够销毁并重建临时投影。
- 继续由 BTSMTL 拥有 Preview、Live Debug、Skill/Shared owner、TreeClip ownership、Source Map 和 Document identity。

**Non-Goals:**

- 不把 Slate Cutscene 保存成项目资产。
- 不把 Slate `ActionClip` 当成 BTSMTL runtime clip、Timeline compiler 输入或第二个正式数据源。
- 不复制 Slate 的 `CutsceneEditor` UI 到 UI Toolkit，不重新实现 Slate 的绘制和时间轴交互。
- 不修改 Slate runtime、Cutscene 数据语义或播放器；允许在 Slate Editor 目录增加最小 transaction sink hook。
- 不在本 change 内改 Character Build、PoseGraph、SkillGraph、Blackboard 或 Timeline runtime。

## Decisions

### 1. Slate `CutsceneEditor` 是实际 UI owner

Timeline 打开命令直接创建或刷新 Editor-only Slate Cutscene projection，然后调用 `CutsceneEditor.ShowWindow(projection)`. Slate 的 IMGUI 窗口负责真实的视觉和交互；当前 BTSMTL `TimelineEditorView` 不再作为正式 Timeline UI 入口。

不使用 Slate 图片、GUI skin 或自定义 USS 仿制，因为那仍然会产生第二套 UI。只有调用 Slate 的真实 EditorWindow，才能保证看到的是 Slate 自己的实际 UI。

由于 Slate 编辑器是独立 `EditorWindow`，本 change 不把它嵌进现有 UI Toolkit 窗口；嵌入需要 fork Slate 的 OnGUI 实现，之后就不再是直接使用 Slate UI。

### 2. BTSMTL TimelineData 是唯一持久化真相

投影层为每个正式 Timeline 建立临时 Slate Cutscene、Group、Track 和 ActionClip wrapper，并保存以下映射：

```text
BTSMTL Timeline / Track / Clip / Section identity
    <-> temporary Slate Cutscene / Group / Track / ActionClip
```

临时对象使用 `HideFlags.HideAndDontSave`，不进入 AssetDatabase、不写入 Document、不进入 runtime build。Slate 只读写临时对象，适配层负责将结果转换回 BTSMTL owner。

### 3. Slate 改动通过 snapshot/diff 转换为正式 Mutation

Slate 插件直接修改它自己的 wrapper。适配层在 Slate 编辑操作结束、Undo/Redo 或 Editor update 检测到变化时，对比 wrapper snapshot，将变化转换为 BTSMTL 命令：

```text
Slate pointer edit
    -> temporary wrapper change
    -> projection diff by authoring identity
    -> TimelineEditorSessionContext / Mutation
    -> one BTSMTL Undo transaction
    -> rebuild projection from BTSMTL owner
```

不把 Slate 的 Undo 当成 BTSMTL 正式 Undo。Slate 的临时 Undo 只用于窗口交互回滚；正式提交必须进入 BTSMTL owner。发生 Pointer Cancel、窗口关闭或 stale identity 时丢弃临时修改并从 BTSMTL 重新投影。

### 4. 能力映射必须显式处理

- Timeline length、time range、Track 顺序、Clip start/end、Section frame 映射到 Slate 对应字段。
- BTSMTL Clip 的正式类型、TreeClip、ActionContext、Curve channel 和 owner identity保存在 projection map，不伪装成 Slate runtime 语义。
- Slate 不支持的 BTSMTL 字段显示为只读或 unavailable；不得静默写入默认值。
- 双击 Clip、TreeClip 下钻、AnimationClip 导航和 BTSMTL Details 由 adapter 处理，不依赖 Slate 的 runtime player。
- Preview/Live Debug 仍由 BTSMTL `TimelinePreviewSession` 和 runtime binding 拥有；Slate 播放按钮不能启动第二个正式时钟。

### 5. 删除错误的 UI Toolkit 仿制链

当前 change 之前为仿 Slate 添加的 Timeline UXML/USS、viewport、zoom、interaction 和 rendering 新路径不再作为正式实现。Apply 阶段必须先删除或恢复这些错误改动，再接入 Slate projection 和 bridge，避免同时保留 Slate 真 UI、旧 UI Toolkit 和仿 Slate UI 三条路径。

### 6. 明确投影的双写与 Undo 边界

投影不会把临时 Slate Cutscene 保存成正式资产，因此持久化数据可以保持单一：正式 Timeline 只写 BTSMTL owner。临时 Slate proxy 使用 `HideFlags.HideAndDontSave`，不得绑定 BTSMTL 的 `SerializedObject`，不得进入 AssetDatabase、Document 或 runtime build。

但是 Slate 原生 `CutsceneEditor` 在 `OnGUI` 中会直接对 proxy 调用 `Undo.RecordObject`、`Undo.RegisterFullObjectHierarchyUndo`、修改 Group/Track/ActionClip 并调用 `EditorUtility.SetDirty`。因此未经扩展的 Slate 原生窗口必然存在一次临时 proxy 写入和一组 Slate Undo 记录。它们不是 BTSMTL 持久化双写，但仍是第二个编辑状态来源。

要满足“一个用户动作只产生一个 BTSMTL 正式 Undo”，bridge MUST拥有明确的提交边界：要么扩展 Slate Editor 提供 proxy transaction begin/commit/cancel 和自定义 Undo sink，要么在 apply 阶段显式承认 Slate proxy Undo 只是临时层并禁止它影响 BTSMTL。不能在没有提交边界的情况下宣称完全无双写。

## Risks / Trade-offs

- [适配层复杂] → 换来的收益是真正使用 Slate UI；identity map、snapshot/diff 和生命周期必须集中在一个 Editor-only 模块，不把桥接扩散到 runtime。
- [Slate 临时 Undo 与 BTSMTL Undo 不同] → Slate wrapper 只作为草稿，正式提交统一由 BTSMTL Mutation/Undo 完成。
- [Slate 不覆盖全部 BTSMTL 字段] → 未映射字段由 BTSMTL Details/adapter 保留，显示 typed unavailable，不静默丢数据。
- [Slate 插件升级] → transaction sink 是局部 Editor patch；需要固定补丁点并在插件升级后重新对照 `CutsceneEditor.OnGUI` 的 Undo/dirty 入口，版本不兼容时 Timeline editor 明确 unavailable。
- [独立窗口而非嵌入] → 保留 Slate 原生 UI 的完整行为；如果未来必须嵌入，需要另开 change 处理 Slate IMGUI fork，不能在本 change 偷换成 UI Toolkit 重做。

## Migration Plan

1. 删除上一轮错误的 UI Toolkit 仿 Slate实现及其 change 任务，不把它们继续当作正式 Timeline UI。
2. 登记 Slate `CutsceneEditor` 的公开入口、生命周期、可编辑字段和 Undo 行为。
3. 建立 Editor-only projection host、identity map 和临时 Cutscene 生命周期。
4. 实现 Timeline/Track/Clip/Section 到 Slate Group/Track/ActionClip 的读取投影。
5. 实现 Slate wrapper snapshot/diff 到 BTSMTL Session/Mutation/Undo 的回写桥。
6. 接入 Skill Timeline owner、TreeClip、Preview、Live Debug 和窗口关闭/重建生命周期。
7. 删除旧 UI Toolkit Timeline 正式入口，运行 OpenSpec 严格校验；不在本 change 内执行 Character Build 或用户端到端验收。
