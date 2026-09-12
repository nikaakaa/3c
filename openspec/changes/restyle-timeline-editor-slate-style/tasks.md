## 1. 确认Slate真实编辑器边界

- [x] 1.1 登记 Slate `CutsceneEditor` 的 `InitializeEmbedded` / `DrawEmbeddedGUI`、`Cutscene`、`CutsceneGroup`、`CutsceneTrack`、`ActionClip` 和 Slate Undo/dirty 生命周期；以 Surface API 和 `OnGUI` 写入点代码清单确认不是直接接收 BTSMTL TimelineData
- [x] 1.2 明确正式数据链：BTSMTL TimelineData 是唯一持久化真相，Slate proxy 只存在 Editor；以 SerializedOwner、AssetDatabase、Document manifest、runtime compile 搜索确认 proxy 没有正式写入路径

## 2. 删除上一轮错误实现

- [x] 2.1 删除上一轮为仿 Slate 添加的 UI Toolkit UXML/USS、工具栏、颜色 token、TimelineViewportState 和自定义时间轴视觉入口；以 Timeline 正式打开调用链不再进入这些文件确认旧 UI 没有第二入口
- [x] 2.2 删除或恢复上一轮只为仿 Slate UI 增加的 zoom/pan/rendering/interaction 分支；以代码搜索确认不再保留第二套时间几何、Clip hit-test 或 Slate 风格替代路径

## 3. 建立Editor-only Slate Projection

- [x] 3.1 创建临时 Cutscene host、Groups、Tracks、ActionClip wrappers 和 Section projection；以 `HideFlags.HideAndDontSave`、无 SerializedObject 绑定、无 AssetDatabase 保存的代码检查确认这些对象只属于 Editor UI 兼容层，不是 BTSMTL GameObject/Actor/Runtime
- [x] 3.2 建立 BTSMTL Timeline/Track/Clip/Section/TreeClip authoring identity 到 Slate proxy object 的双向 map；以 owner 切换、窗口关闭和重建都能释放旧 map 的生命周期检查确认不残留对象
- [x] 3.3 将 Timeline length、view range、Track 顺序、Clip start/end/blend、Section 和每个 Clip 的 Timeline-local Curve Channel 投影到 Slate；proxy ActionClip 被 Slate `Validate` 发现并通过原生 Curve/DopeSheet 显示当前 Clip 的曲线和关键帧，normalized authoring time 转换为 Slate local seconds，未支持字段显示 unavailable且不覆盖默认值
- [x] 3.4 将 Skill owner、TreeClip ownership、ActionContext 和 AnimationClip 导航信息保存在 adapter context；以 Slate UI 关闭后这些信息仍从 BTSMTL owner恢复确认 proxy 没有夺取领域所有权
- [ ] 3.5 从 `TimelineContractCatalog` 投影合法 Add Track 候选；新增 Track 必须经正式 Timeline owner/API 生成 identity，不允许 Slate proxy 组件直接成为 authoring Track
- [ ] 3.6 按选中 Track 的 allowed clip kinds 提供 Add Clip、AnimationClip 选择、TreeClip Graph/Tree 选择和其它 typed binding；新增 Clip 必须经正式 owner/API 创建并重建 projection

## 4. 建立Slate到BTSMTL的正式写回

- [x] 4.1 为 projection 建立初始 snapshot、临时 snapshot 和 identity diff；Clip 时间与 Curve Channel 都按 authoring identity比较，一次 Slate Clip/Curve 手势只生成一条 BTSMTL mutation 命令，不逐帧写入
- [x] 4.2 将 diff 转换为 `TimelineEditorSessionContext` / `ITimelineEditorMutationPort` 操作，并在提交后从 BTSMTL owner 重建 projection；以正式 owner、Source Map 和单次 BTSMTL Undo 调用链确认写回唯一
- [x] 4.3 处理 Pointer Cancel、窗口关闭、owner 切换、Undo/Redo、外部 Timeline 刷新和 stale identity；以未提交 proxy 改动被丢弃且不写半成品 TimelineData 的状态路径确认取消安全
- [x] 4.4 处理 Slate 原生 `Undo.RecordObject`、`Undo.RegisterFullObjectHierarchyUndo` 和 `EditorUtility.SetDirty`；以 transaction/Undo sink 扩展或明确隔离策略证明 proxy Undo 不会冒充 BTSMTL 正式 Undo
- [ ] 4.5 为 Add Track/Add Clip 建立 typed creation mutation；资源取消、contract validation失败或owner stale时不得留下空对象、半成品Undo或Slate-only identity

## 5. 接入正式Timeline入口与运行状态

- [x] 5.1 将正式 Skill/Shared Timeline 打开入口切换到唯一 `TimelineEditorWindow` 内的 Slate `DrawEmbeddedGUI` Surface；以打开调用链和窗口 owner 检查确认不再创建旧 UI Toolkit Timeline窗口或第二个 Slate 窗口
- [ ] 5.2 将 Timeline 收敛为作者编辑 Surface 与被动 Runtime Trace overlay；移除 Timeline 内的 Authoring Preview、Live Debug、Preview Target、TimelinePreviewSession 和本地播放控制，改由 SkillGraph/Graph Shell 调用唯一 Scene Play coordinator
- [x] 5.3 Slate 插件缺失或版本不兼容时显示 typed Unavailable；以不回退到旧 UI、不创建默认数据、不写 Slate 资产的路径检查确认没有 fallback 分裂实现
- [x] 5.4 修改 Slate Editor 源码抽取可嵌入 Surface；以 `IMGUIContainer` 承载时间轴且 Timeline 打开调用链不再出现 `CutsceneEditor.ShowWindow` 确认最终只有一个 BTSMTL Timeline 窗口
- [x] 5.5 在 Embedded Surface 模式下清理 Slate 默认 DirectorGroup、Camera/Audio/Director Track、Actor入口和 Slate Preview；确认 Scene Play coordinator 是唯一运行控制入口
- [ ] 5.8 接入 Slate 风格的 Add Track/Add Clip UI，但命令必须路由到 BTSMTL contract/catalog/mutation，不得恢复 Slate 原生任意 Track/ActionClip 创建菜单

## 5.1 修正Slate兼容层生命周期

- [x] 5.6 不再用 `ScriptableObject.CreateInstance<CutsceneEditor>` 伪造 `EditorWindow`；抽出不依赖 Unity EditorWindow 注册的 Slate Surface 状态模块，消除 `Invalid editor window` 错误

- [ ] 5.7 在 Graph Shell/SkillGraph 装配 Scene Play Start/Pause/Resume/Reset/Stop、Build、Skill、Live Debug、Capture、History、Restore 和 Replay 控件；Timeline 只消费 binding/overlay，并允许同一 Session 内继续编辑后由 Graph Shell Build/adopt

## 6. 文档与变更收口

- [x] 6.1 对照当前 `btsmtl-timeline-editor-preview` spec、proposal 和 design，确认“真实Slate编辑Surface、单窗口承载、Embedded Surface无Slate内核、Editor-only projection、BTSMTL唯一持久化真相、proxy双写边界、运行控制归Graph Shell、Play期间可编辑、Add Track/Add Clip走typed contract”术语一致
- [x] 6.2 运行 `openspec validate "restyle-timeline-editor-slate-style" --type change --strict`；校验通过只证明文档结构合法，不把未完成的实现任务标记为完成
- [x] 6.3 交付 handoff，列出 Slate 实际入口、projection host、identity map、snapshot/diff、Undo boundary、旧 UI 删除范围和未包含的 Character Build/runtime 范围
- [ ] 6.4 在 Add Track/Add Clip 完成后对照 `TimelineContractCatalog`、`TimelineData.AddTrack/AddClip`、authoring binding、Document/Reconciler 和本 change spec，确认人工 UI 与 Document 创建使用同一正式 owner/Mutation 语义
