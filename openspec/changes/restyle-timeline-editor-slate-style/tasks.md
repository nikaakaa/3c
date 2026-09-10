## 1. 确认Slate真实编辑器边界

- [x] 1.1 登记 Slate `CutsceneEditor.ShowWindow(Cutscene)`、`Cutscene`、`CutsceneGroup`、`CutsceneTrack`、`ActionClip` 和 Slate Undo/dirty 生命周期；以 API 入口和 `OnGUI` 写入点代码清单确认不是直接接收 BTSMTL TimelineData
- [x] 1.2 明确正式数据链：BTSMTL TimelineData 是唯一持久化真相，Slate proxy 只存在 Editor；以 SerializedOwner、AssetDatabase、Document manifest、runtime compile 搜索确认 proxy 没有正式写入路径

## 2. 删除上一轮错误实现

- [x] 2.1 删除上一轮为仿 Slate 添加的 UI Toolkit UXML/USS、工具栏、颜色 token、TimelineViewportState 和自定义时间轴视觉入口；以 Timeline 正式打开调用链不再进入这些文件确认旧 UI 没有第二入口
- [x] 2.2 删除或恢复上一轮只为仿 Slate UI 增加的 zoom/pan/rendering/interaction 分支；以代码搜索确认不再保留第二套时间几何、Clip hit-test 或 Slate 风格替代路径

## 3. 建立Editor-only Slate Projection

- [x] 3.1 创建临时 Cutscene host、Groups、Tracks、ActionClip wrappers 和 Section projection；以 `HideFlags.HideAndDontSave`、无 SerializedObject 绑定、无 AssetDatabase 保存的代码检查确认生命周期隔离
- [x] 3.2 建立 BTSMTL Timeline/Track/Clip/Section/TreeClip authoring identity 到 Slate proxy object 的双向 map；以 owner 切换、窗口关闭和重建都能释放旧 map 的生命周期检查确认不残留对象
- [x] 3.3 将 Timeline length、view range、Track 顺序、Clip start/end/blend、Section 和可显示的 Curve 摘要投影到 Slate；以不支持字段显示 unavailable、没有默认值覆盖的映射清单确认数据没有静默丢失
- [x] 3.4 将 Skill owner、TreeClip ownership、ActionContext 和 AnimationClip 导航信息保存在 adapter context；以 Slate UI 关闭后这些信息仍从 BTSMTL owner恢复确认 proxy 没有夺取领域所有权

## 4. 建立Slate到BTSMTL的正式写回

- [x] 4.1 为 projection 建立初始 snapshot、临时 snapshot 和 identity diff；以一次 Slate Clip 拖动只生成一条 BTSMTL mutation 命令的代码路径确认不会逐帧写入
- [x] 4.2 将 diff 转换为 `TimelineEditorSessionContext` / `ITimelineEditorMutationPort` 操作，并在提交后从 BTSMTL owner 重建 projection；以正式 owner、Source Map 和单次 BTSMTL Undo 调用链确认写回唯一
- [x] 4.3 处理 Pointer Cancel、窗口关闭、owner 切换、Undo/Redo、外部 Timeline 刷新和 stale identity；以未提交 proxy 改动被丢弃且不写半成品 TimelineData 的状态路径确认取消安全
- [x] 4.4 处理 Slate 原生 `Undo.RecordObject`、`Undo.RegisterFullObjectHierarchyUndo` 和 `EditorUtility.SetDirty`；以 transaction/Undo sink 扩展或明确隔离策略证明 proxy Undo 不会冒充 BTSMTL 正式 Undo

## 5. 接入正式Timeline入口与运行状态

- [x] 5.1 将正式 Skill/Shared Timeline 打开入口切换到 Slate `CutsceneEditor.ShowWindow`；以打开调用链和窗口 owner 检查确认不再创建旧 UI Toolkit Timeline窗口
- [x] 5.2 保留 BTSMTL Authoring Preview、Live Debug、Follow/Pin overlay、TreeClip 下钻和 Character Preview Target；以 Slate 播放控件不启动第二个正式时钟、Live Debug只读的代码检查确认运行时链唯一
- [x] 5.3 Slate 插件缺失或版本不兼容时显示 typed Unavailable；以不回退到旧 UI、不创建默认数据、不写 Slate 资产的路径检查确认没有 fallback 分裂实现

## 6. 文档与变更收口

- [x] 6.1 对照当前 `btsmtl-timeline-editor-preview` spec、proposal 和 design，确认“真实Slate UI、Editor-only projection、BTSMTL唯一持久化真相、proxy双写边界”术语一致
- [x] 6.2 运行 `openspec validate "restyle-timeline-editor-slate-style" --type change --strict`，并以 change 状态显示所有规划任务完成作为文档交付证据
- [ ] 6.3 交付 handoff，列出 Slate 实际入口、projection host、identity map、snapshot/diff、Undo boundary、旧 UI 删除范围和未包含的 Character Build/runtime 范围
