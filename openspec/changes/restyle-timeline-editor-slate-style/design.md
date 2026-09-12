## Context

用户要求使用 Slate 插件的真实 Timeline UI，并且最终只有一个 Timeline 窗口。Slate 原始入口是 `Slate.CutsceneEditor.ShowWindow(Cutscene)`，实现为独立的 IMGUI `EditorWindow`，内部读取 `Cutscene` 的 Group/Track/ActionClip 层级并自行处理时间尺、Clip 命中、拖动、缩放、Undo 和播放状态。BTSMTL 路径修改 Slate Editor 源码，把这套 OnGUI 时间轴抽成可嵌入的 Surface，由唯一 `TimelineEditorWindow` 承载。

BTSMTL Timeline 的正式数据仍由 `TimelineData`、Track、Clip、Section、TreeClip、Session、Mutation、Undo、Preview 和 Live Debug 拥有。两套模型不能直接互换：Slate UI 不能直接读取 `TimelineData`，BTSMTL 也不能把普通 Clip 直接当成 Slate `ActionClip`。

BTSMTL 的 Skill、Timeline、Preview 和 Runtime 不依赖 Slate 的 GameObject Actor、DirectorGroup、PlayableGraph 或 Slate 播放内核。Projection 中的 Slate 对象只是满足 Slate Surface 的 Editor-only 兼容对象，不拥有 BTSMTL 领域数据、不拥有角色、不拥有预览时钟。

因此本 change 采用一个 Editor-only bridge。Slate UI 作为作者看到和操作的真实编辑表面；BTSMTL TimelineData 作为唯一持久化真相。临时 Slate 对象只负责满足 Slate 编辑器的读取和交互要求，不能保存、编译或进入运行时。

## Goals / Non-Goals

**Goals:**

- 在唯一 `TimelineEditorWindow` 内直接使用 Slate `CutsceneEditor` 的时间尺、Group/Track、Clip、拖动、缩放、选择、Curve/DopeSheet 和 Inspector 编辑 UI；Timeline 不拥有运行时播放控制。
- 修改 Slate Editor 源码提供可嵌入 Surface；BTSMTL Timeline 入口不创建第二个 Slate `EditorWindow`。
- Embedded Surface 不创建 Slate 默认 DirectorGroup、Camera/Audio/Director Track，不显示 Actor 创建入口，也不调用 Slate Preview/PlayableGraph。
- 将当前 BTSMTL Timeline 投影为 Slate 能编辑的临时层级，并保留每个 Track、Clip、Section、TreeClip 的 authoring identity 映射。
- 将 Slate UI 的有效改动转换为 BTSMTL `TimelineEditorSessionContext` 和 `ITimelineEditorMutationPort` 操作，使用正式 owner 和 Undo。
- Slate Surface 重绘、Undo/Redo、外部 Timeline 刷新或 owner 切换时，能够销毁并重建临时投影。
- Timeline 只保留作者编辑与被动 Runtime Trace overlay；Scene Play、Build、Skill、Live Debug、Capture、History、Restore 和 Replay 由 SkillGraph/Graph Shell 与 Scene Play coordinator 拥有。
- Scene Play 期间允许继续编辑 Timeline；正式 Mutation 后由 Graph Shell 发起 Build，在同一 Session 的 adoption barrier 采用兼容的新 ProgramEpoch。

**Non-Goals:**

- 不把 Slate Cutscene 保存成项目资产。
- 不把 Slate `ActionClip` 当成 BTSMTL runtime clip、Timeline compiler 输入或第二个正式数据源。
- 不复制 Slate 的 `CutsceneEditor` UI 到 UI Toolkit，不重新实现 Slate 的绘制和时间轴交互。
- 不修改 Slate runtime、Cutscene 数据语义或播放器；允许在 Slate Editor 目录增加最小 transaction sink hook。
- 不把 Slate GameObject、Actor、Director 或 Preview 状态写入 BTSMTL Skill、Timeline、Character 或 Runtime。
- 不在本 change 内改 Character Build、PoseGraph、SkillGraph、Blackboard 或 Timeline runtime。

## Decisions

### 1. Slate `CutsceneEditor` Surface 是实际 UI owner

Timeline 打开命令只创建或刷新 Editor-only Slate Cutscene projection，然后在唯一 `TimelineEditorWindow` 的 `IMGUIContainer` 中调用 Slate `CutsceneEditor.DrawEmbeddedGUI`. Slate Surface 负责真实的视觉和交互；当前 BTSMTL `TimelineEditorView` 不再作为正式 Timeline UI 入口。

不使用 Slate 图片、GUI skin 或自定义 USS 仿制，因为那仍然会产生第二套 UI。BTSMTL 只承载 Slate Surface，不复制 Slate 绘制和时间轴交互。

Slate 原生 `EditorWindow` 仍可供 Slate 插件自身入口使用，但 BTSMTL Timeline 路径不得调用它；BTSMTL 只使用修改后的可嵌入 Surface。

### 2. BTSMTL TimelineData 是唯一持久化真相

投影层为每个正式 Timeline 建立临时 Slate Cutscene、Group、Track 和 ActionClip wrapper，并保存以下映射：

```text
BTSMTL Timeline / Track / Clip / Section identity
    <-> temporary Slate Cutscene / Group / Track / ActionClip
```

临时对象使用 `HideFlags.HideAndDontSave`，不进入 AssetDatabase、不写入 Document、不进入 runtime build。Slate 只读写临时对象，适配层负责将结果转换回 BTSMTL owner。

这些对象即使在 Editor 中采用 Slate 所需的组件形态，也只能是 UI 兼容层；它们不是 BTSMTL 的 GameObject authoring、Actor binding 或 Runtime object。

### 2.1 BTSMTL Embedded Surface 不使用 Slate 内核

Projection 只向 Slate 提供 Track/Clip/Curve 的显示和编辑形态。BTSMTL 不创建或读取 Slate DirectorGroup、CameraTrack、DirectorAudioTrack、DirectorActionTrack、ActorGroup、PlayableGraph 或 Slate Preview output。

Scene Play Session 与领域 owner 是唯一时间推进和表现执行 owner；Slate Surface 的 current time 只用于编辑游标和被动 overlay。Slate 的 Play、Sample、Scene binding 和 Actor 语义在 Embedded Surface 中必须关闭或隐藏。Timeline 不创建 `TimelinePreviewSession`、独立 evaluator 或私有 clock。

Projection 必须清理 `Cutscene.Reset/TryReset` 自动创建的默认 Director 内容，并把每个 `BtsmtlSlateActionClip` 直接挂到 Slate Track 能发现的 ActionClip 集合中；不能用子 GameObject 层级导致 Slate `Validate` 丢失 Clip。

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
- Timeline Curve Channel 只投影当前 Clip 实际拥有的 typed channel。BTSMTL 的 normalized curve 在 proxy 中转换为 Slate ActionClip 的 local seconds，Slate 修改提交时再转换回 normalized curve，并保留 wrap mode、key、tangent、weight 与 WeightedMode。
- BTSMTL Clip 的正式类型、TreeClip、ActionContext、Curve channel 和 owner identity保存在 projection map，不伪装成 Slate runtime 语义。
- Slate 不支持的 BTSMTL 字段显示为只读或 unavailable；不得静默写入默认值。
- 双击 Clip、TreeClip 下钻、AnimationClip 导航和 BTSMTL Details 由 adapter 处理，不依赖 Slate 的 runtime player。
- Timeline 不提供 Preview/Live Debug 的控制按钮。运行状态通过 Graph Shell 建立的正式 binding 以只读 overlay 投影到 Slate Surface；Slate 播放按钮不能启动任何时钟。

### 4.1 运行控制归属 Graph Shell

Graph Shell/SkillGraph 页面是 Scene Play 控制面，统一持有 Start、Pause、Resume、Reset、Stop、Build、Skill request、Runtime Trace/Live Debug、Capture、History、Restore 和 Replay。它们调用同一个 Scene Play coordinator，不在 Timeline 窗口复制一套命令或状态机。

Timeline 窗口只持有 authoring owner、Slate projection、selection、geometry、Mutation/Undo 和被动 overlay。作者在 Play 期间拖动 Clip、修改 Curve 或 Section 时，提交仍进入 BTSMTL 正式 Mutation；Graph Shell 负责显示 dirty/build/adoption 状态并在同一 Session 中发布新 Program。当前 Action 不兼容时继续使用旧 Epoch，下一次 Action 才采用新版本。

### 4.2 新增Track与Clip必须由BTSMTL contract提供

Slate 原生 Add Track/Add Action 菜单不能直接作为 BTSMTL 新增入口。Slate 只能创建临时代理对象，而临时对象没有正式 `AuthoringId`、`ContractKind`、Typed Binding 和 owner，直接保存会形成第二套 Timeline 数据源。

新增入口由 Timeline adapter 提供 Slate 风格的菜单，但数据操作走 BTSMTL 正式链：

```text
Slate Add menu
    -> TimelineContractCatalog 合法候选
    -> 正式资源选择/typed creation input
    -> TimelineData.AddTrack / AddClip
    -> TimelineEditorSessionContext / Mutation / Undo
    -> owner revision + rebuild projection
```

Add Track 只显示当前 Timeline owner 支持的 Track contract。Add Clip 只显示当前 Track contract 的 allowed clip kinds。Animation Clip 只能选择已经存在的原生 AnimationClip；TreeClip 必须选择正式 Graph/Tree 来源；Camera、Motion、Cue 和其它 Clip 必须使用各自 typed authoring binding。新增对象的正式 identity 由 Timeline owner/API 生成，不能把 Slate proxy 的 GameObject、组件 instance id 或显示名称写入作者数据。

新增提交成功后，adapter 必须销毁旧 proxy 并从最新 Timeline owner 重建；selection 应按新 authoring identity恢复。取消资源选择或 contract 校验失败时，只丢弃菜单草稿，不创建空 Track、空 Clip 或默认资源。

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
- [Slate Editor 源码维护] → 需要维护 `CutsceneEditor` 的 Surface 抽取点，Slate 插件升级时必须重新对照 OnGUI、Undo 和输入事件；换来的收益是 Timeline 作者最终只面对一个窗口。

## Migration Plan

1. 删除上一轮错误的 UI Toolkit 仿 Slate实现及其 change 任务，不把它们继续当作正式 Timeline UI。
2. 登记 Slate `CutsceneEditor` 的公开入口、生命周期、可编辑字段和 Undo 行为。
3. 抽取 Slate Editor 可嵌入 Surface，并由唯一 TimelineEditorWindow 承载。
4. 建立 Editor-only projection host、identity map 和临时 Cutscene 生命周期。
5. 实现 Timeline/Track/Clip/Section 到 Slate Group/Track/ActionClip 的读取投影。
6. 实现 Slate wrapper snapshot/diff 到 BTSMTL Session/Mutation/Undo 的回写桥。
7. 接入 Skill Timeline owner、TreeClip、Graph Shell 的 Scene Play/Build/Live Debug binding 和 Surface 重建生命周期；Timeline 只接收只读运行 overlay。
8. 删除旧 UI Toolkit Timeline 正式入口，运行 OpenSpec 严格校验；不在本 change 内执行 Character Build 或用户端到端验收。
