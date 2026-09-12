## Why

当前 Timeline 编辑器的 UI Toolkit 视图不是用户要的编辑表面。项目已经安装 ParadoxNotion Slate Cinematic Sequencer，用户要求直接使用 Slate 的真实 `CutsceneEditor` UI，而不是重新实现一套相似的颜色、Clip 外观和时间尺。

Slate 的 `CutsceneEditor` 原始实现是独立的 IMGUI `EditorWindow`，它直接要求 `Slate.Cutscene`、`CutsceneGroup`、`CutsceneTrack` 和 `ActionClip` 层级；它不能直接接收 BTSMTL `TimelineData`。因此需要同时修改 Slate Editor 源码，把其 OnGUI 时间轴抽成可嵌入的 Surface，并用只存在于 Unity Editor 的适配层把当前 BTSMTL Timeline 投影成 Slate 可读写的临时层级，再把 Slate UI 的改动转换回 BTSMTL 正式 Mutation。

本 change 将 Slate `CutsceneEditor` 作为真实 Timeline 编辑 UI，BTSMTL `TimelineData` 继续作为唯一持久化和运行时作者数据源。不复制 Slate 运行时，不保存 Slate Cutscene 资产，不把 Slate 数据变成第二个正式 Timeline。

BTSMTL Skill、Timeline、Preview 和 Runtime 不使用 Slate 的 GameObject Actor、DirectorGroup、PlayableGraph 或 Slate 播放内核。Slate 的 Group/Track/ActionClip 只允许作为 Editor-only UI 兼容对象存在；它们不拥有角色、不拥有技能、不拥有预览时钟，也不进入 authoring/runtime 数据链。

## What Changes

- 用户后续要求预览窗口随 Timeline 联合实施：增加 preview-integration-plan.md 作为唯一跨窗口体验与联合排期入口，覆盖 SkillGraph 预览分组布局、技能/纯 Timeline 两类正式入口、实例选择、双向导航、运行标记、编辑后采用以及历史/恢复/回放交互。
- 复用现有场景协调器和正式运行 owner；Timeline 扩展本地观察/导航，不复制运行命令。实际 FlowCanvas SkillGraph 宿主必须接入，不能仅在旧 BaseTreeWindow 上显示工具条便宣称技能预览可用。

- 2026-09-12 收口范围：新增/属性编辑、统一整数帧、整体 GUI 布局、曲线与视图状态恢复必须共同完成；已有投影和编译通过不代表这些能力已交付。
- 将重复的文档/来源/ownership 行合并，明确工具栏、搜索/标尺、左右同步轨道和可收起底部属性区；背景、裁剪与命中使用同一布局结果，窄窗口次要操作折叠。
- 标尺、游标、逐帧、Clip/Section 和曲线时间编辑使用正式 Timeline FrameRate；不读取 Slate 全局 FPS 作为 BTSMTL 时间权威，不将作者帧擅自等同于 Runtime Logic Tick。
- 编辑游标与真实运行标记分开；本次不把尚未确认的 Timeline 内 Play 快捷按钮或本地自动播放游标列为交付能力。删除当前误接的嵌入 Slate Play/Sample 链，已有场景预览仍归 Graph Shell。
- 正式创建允许作者主动建立无 Clip 的合法空 Track；取消/失败不留下半成品。修改后按稳定 identity 恢复选择、展开、游标、缩放、滚动和属性区状态。

- 正式 Timeline 打开入口只创建一个 `TimelineEditorWindow`，由该窗口的 IMGUI Surface 调用 Slate `CutsceneEditor` 的真实绘制和交互；BTSMTL 路径不得再调用 `CutsceneEditor.ShowWindow(Cutscene)` 创建第二个窗口。
- 修改 Slate Editor 源码，提供可嵌入的 `InitializeEmbedded` / `DrawEmbeddedGUI` Surface API，同时保留 Slate 原生 EditorWindow 供插件自身其它入口使用。
- 增加 BTSMTL Embedded Surface policy：禁止 Slate 默认 DirectorGroup、Camera/Audio/Director Track、Actor 创建和 Slate 播放控件进入 BTSMTL Timeline Surface。
- 增加 Editor-only BTSMTL-to-Slate projection，将 Timeline owner、Track、Clip、Section、Timeline-local Curve Channel、完整关键帧和稳定 authoring identity映射到临时 Slate Cutscene 层级。
- Clip wrapper 必须以 Slate `CutsceneTrack` 能发现的正式 ActionClip 形态挂载；不得因子 GameObject 层级错误而让 Clip 或 Curve 静默丢失。
- 增加 Slate-to-BTSMTL mutation bridge：Slate UI 的移动、裁剪、删除、添加、Section 和时间编辑先转换成 BTSMTL 编辑命令，再经 `TimelineEditorSessionContext`、正式 owner、Mutation 和 Undo 写回。
- 补齐 Timeline 编辑器的新增内容入口：Add Track 与 Add Clip 菜单必须由 `TimelineContractCatalog` 提供合法类型，并调用正式 `TimelineData.AddTrack` / `AddClip` 和唯一 Mutation/Undo；不得直接让 Slate 创建无 BTSMTL identity 的任意 `CutsceneTrack` / `ActionClip`。
- 新增 Clip 必须按 Track contract 提供准确创建方式：Animation 使用已有原生 AnimationClip 引用，TreeClip 使用正式 Graph/Tree 来源，Camera、Motion 和其它 typed Clip 使用各自 authoring binding；不得创建替代资源或默认 Clip。
- 明确 proxy 的双写边界：BTSMTL 是唯一持久化真相，但 Slate 原生 UI 会先修改临时 proxy；若要一个动作只有一个正式 Undo，必须提供 Slate transaction/Undo sink 扩展，不能假设原生窗口自动完成同步。
- Slate 临时层级关闭、Timeline owner 变化、Undo/Redo 或外部刷新时重新建立投影，禁止把临时 Slate 对象保存成资产或进入运行时编译链。
- 保留 BTSMTL 的 Skill Timeline、TreeClip、ActionContext、AnimationSlot、Document v7、Source Map 和 authoring identity；Timeline 页面只显示作者内容与被动运行标记。
- 将 Scene Play 的 Start/Pause/Resume/Reset/Stop、Build、Skill request、Runtime Trace/Live Debug、Capture、History、Restore 和 Replay 控制统一放到 SkillGraph/Graph Shell 与 Scene Play coordinator；Timeline 不再拥有这些运行命令。
- Scene Play 运行期间 Timeline 仍保持可编辑。编辑通过正式 Mutation 改变作者 Timeline，Build 在同一 Session 中发布并由 ProgramEpoch adoption 采用，不退出 Play、不切换到 Timeline 私有 Session。
- 删除上一轮错误方向中新增的 UI Toolkit Slate 仿制层、项目自定义 Timeline 皮肤和重复时间视口实现；不再维护第二个正式 Timeline UI。
- 不修改 Slate runtime、Cutscene 数据语义或播放链；允许对 Slate Editor 的 `CutsceneEditor` 增加最小 transaction sink 扩展，以隔离 proxy Undo 并把提交边界交给 BTSMTL adapter。

## Capabilities

### New Capabilities

- Editor-only BTSMTL Timeline 到 Slate `CutsceneEditor` 的投影与回写适配能力。

### Modified Capabilities

- `btsmtl-timeline-editor-preview`: Timeline 的正式编辑表面改由 Slate `CutsceneEditor` 拥有，BTSMTL 继续拥有正式数据、Mutation、Undo 和只读运行 overlay；Scene Play/Build/Live Debug 控制归 Graph Shell。
- `btsmtl-timeline-editor-preview`: 增加 contract-driven Add Track/Add Clip 的作者入口，新增对象仍由 Timeline owner 和 typed Mutation 拥有。

## Impact

- 本次只更新现有 change，并同步修正场景预览 delta 的“结构只读”冲突；不安装未完成的 active delta 到 current specs，不把文档更新当成实施或验收完成。
- 联合实施消费 rebuild-btsmtl-preview-with-scene-play 原任务的公开合同；共享预览 UI 与接线纳入本次交付，底层启动、adoption、checkpoint 和领域执行仍由原 owner 实现，具体映射见预览联动计划。
- 实施在主线单线推进，已有预览 worktree 仅作历史/未集成内容参考，不自动双写。具体行为、业务取舍和完成标准见 design.md。

- 主要入口：`Assets/GameScripts/Main/Runtime/BTSMTL/Timeline/Editor/Scripts/Tree/TimelineEditorMainWindow.cs` 和 Timeline editor open request composition。
- 新增范围：Editor-only Slate projection、identity map、snapshot/diff、BTSMTL mutation bridge 和临时 Cutscene 生命周期管理。
- 新增范围：Slate Add Track/Add Clip 的 contract menu、资源选择和正式 authoring mutation bridge。
- 删除范围：上一轮 UI Toolkit 仿制 Slate 的 UXML/USS、Timeline 自定义 viewport/interaction/rendering 路径中仅为仿制 UI 新增的代码。
- 保留范围：`TimelineData`、Track/Clip/Section/TreeClip identity、`TimelineEditorSessionContext`、`ITimelineEditorMutationPort`、Undo、被动 Runtime Trace overlay 和 Skill Timeline owner；Scene Play Session、Build/adoption、Live Debug 命令由 Graph Shell/Scene Play coordinator 提供。
- 不修改 `Assets/ParadoxNotion/SLATE Cinematic Sequencer` 的 runtime、Cutscene 数据语义或播放器；允许修改其 Editor 目录抽出可嵌入 Surface 和 transaction hook，不保存临时 Cutscene，不增加 Slate runtime player、第二个时钟或第二个正式 Timeline compiler。
- 不把 Slate Actor、DirectorGroup、Camera/Audio Track 或 Slate Preview 解释成 BTSMTL Skill/Timeline/Character 数据；Scene Play coordinator 是真实运行的唯一控制入口，Slate Surface 不执行预览。
- 该 change 不包含 Character Build、PoseGraph、SkillGraph、TreeClip runtime 语义或 Timeline data schema 迁移。
