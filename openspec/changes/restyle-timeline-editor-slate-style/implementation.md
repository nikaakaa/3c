# Timeline Slate 重构实施说明

## 当前边界

Timeline 的持久化真相仍是 `BTSMTL.Timeline.TimelineData`。Slate 只承担编辑表面；BTSMTL Timeline 打开链已经切换为纯内存 `SlateTimelineEditorSurface`，不创建临时 `GameObject`、`Cutscene`、Group、Track、ActionClip 或 AnimatedParameter 组件树。

Scene Play、Skill 请求、Build、采用、历史恢复和输入回放仍由 Graph Shell 与正式预览 coordinator 拥有。Timeline 只显示作者帧、编辑曲线，并接收精确运行观察标记。

## 已完成代码链

```text
TimelineEditorWindow
  -> TimelineEditorOpenRequest
  -> TimelineEditorSessionContext
  -> BtsmtlSlateTimelineEditorAdapter
  -> SlateTimelineEditorSurface
  -> Slate 风格 Track/Clip/Curve 手势
  -> 纯内存内容读视图与命令 port
  -> source identity 快照
  -> Session.Apply
  -> TimelineData 正式 owner
```

新增链路：

- `+ Track` 使用 `TimelineContractCatalog`、`TimelineAuthoringTypeCatalog` 和正式 Track 字段 binding。
- 轨道右键按作者帧打开 Add Clip 表单；Animation、Tree、Motion、MotionWarp、Camera、Cue 和 Scene binding 均走正式类型工厂/`TimelineAuthoringClipBinding`。
- Add Track/Add Clip 失败或 owner revision 过期时保留表单输入，不留下半成品或额外 Undo。
- Scene Presentation 的 `valueCurve` 已注册到正式 `TimelineCurveChannelCatalog`，Slate 编辑后通过 `TimelineCurveAuthoring.Replace` 回写正式曲线。
- 纯内存 Surface 已接入正式 TimelineData 读视图、稳定 Track/Clip/Curve identity、整数帧游标、Clip 拖动、曲线关键帧草稿、Add Track/Add Clip 命令和运行/历史只读标记；命令通过 `TimelineEditorSessionContext` 回写正式 owner。
- 运行观察从 `BtsmtlSkillObservationSession` 按 Timeline/Graph/Node 精确筛选 `RuntimeTimelinePlaybackDebugSummary`，调用 `TimelineEditorWindow.ApplyRuntimeObservation`；运行线和作者编辑游标分离。
- Graph Shell 历史折叠区的 `Segment` 驱动 `RuntimeDebugSession.HistoryOffset`，历史观察调用 `ApplyHistoryObservation` 绘制独立 History 线；历史线、实时线和作者帧互不写同一个时间状态。
- Timeline 顶部的 `Preview` 只返回已绑定的 Graph Shell；没有绑定时明确提示，不启动 Play、不重建 Session。
- `TimelineRuntimeObservationBridge` 消费统一 `RuntimeDebugSession` 的正式 Timeline playback summary；唯一调用显示 Runtime/History overlay，多调用保持不猜选，避免复制 Scene Play 播放器。

作者界面：

- 嵌入 Slate 顶栏提供 `+ Track`、上一帧、下一帧、Fit 和当前作者帧；没有 Timeline 本地 Play/Sample/ReSample/Stop。
- 作者帧现在在 Surface 内提供上一帧、下一帧、整数帧输入、总帧数和 FPS 标识；轨道左侧提供可见的 `+` Clip 入口，顶部时间范围滑块避开编辑按钮，不与控件重叠。
- 左右轨道共享行高和滚动，曲线展开同步；右侧 Inspector 可折叠、拖拽调宽并保持最小可读宽度，窗口宽度不足时自动收起 Inspector。
- `SurfaceLayout` 统一计算 Slate Surface 的工具栏、搜索、标尺、轨道区域、时间区域和命中几何。
- DopeSheet key strip 从纯内存完整曲线快照读取；支持关键帧多选和按作者帧整体拖动，正式 key、切线、权重和 wrap 不被删除或量化，CurveRenderer 负责切线与细节编辑。
- DopeSheet key strip 以 `ClipId + CurveId` 区分活动曲线，使用每个曲线自己的作者帧域显示关键帧；拖动草稿会同步刷新 Slate CurveRenderer，提交仍只经过正式曲线命令 port。
- Section 标记支持选中，右侧 Inspector 直接编辑名称、作者帧和下一个 Section 引用，提交使用 `TimelineData.ConfigureSection/ConfigureSectionNext`，不再留下只能拖动、不能查看属性的漂浮标记。
- Track Inspector 直接编辑正式 Track 名称和 `PersistentMuted`；曲线关键帧选择会保留所属 Clip 的 Inspector 上下文，不再因为纯 Surface 的 `Key` 选择被清空。
- Graph Shell 预览控制按场景控制、试验与采用、观察、历史与录制分组；历史刷新不会覆盖作者已经输入的 Tick。

共享预览宿主：

- `BtsmtlScenePlayPreviewPresenter` 统一持有 `IBtsmtlScenePlayPreviewOperations`、状态变化、Build/Skill 请求、输入录制和历史 Tick 输入；它不创建新的 Scene Play coordinator，也不推进运行时钟。
- `BaseTreeWindow` 的 UI Toolkit 工具条和实际 FlowCanvas SkillGraph 的 IMGUI 工具条都只绘制同一个 Presenter；FlowCanvas 通过 `BtsmtlSkillGraphPreviewToolbarRegistry` 挂载，不再把 SkillGraph 误导到旧树窗口。
- 四组入口在两个宿主保持一致：场景控制、试验与采用、观察、历史与录制。SkillGraph 原有的端口采集、执行实例选择和父调用导航仍是 Graph 观察专属项，不与 Scene Play 命令重复。
- Timeline 运行观察现在按 Timeline、Graph 和绑定的 SourceNode 精确筛选；0 条或多条调用都会清空旧 overlay，多条时提示从 SkillGraph 选择实例，不再使用第一条 summary 猜测。
- Timeline 顶部 `Runtime` 菜单提供“自动（仅唯一调用）”“跟随最新调用”和按 playback identity 固定实例；固定项消失时明确提示，不自动换到新调用。运行源导航、SkillGraph 父调用导航和 Timeline 打开请求都携带 Graph/Node locator。
- Graph Shell 与 SkillGraph 的共享 Presenter 现在显示 coordinator 的真实 Build/adoption 状态：构建中、等待采用、已采用 Epoch 或失败；这只是运行版本事实，不把作者 revision 伪装成已采用。
- Scene Play 输入已改为正式 `SceneAsset + ContextId`，两个宿主不再要求作者拖场景里的 Context GameObject；启动时由 coordinator 按精确路径和 ContextId 打开场景、检查唯一 Context、角色列表和正式 Runtime Owner。
- 观察区已把正式 `RuntimeDebugSession` 的诊断 Capture 与输入录制拆成两个独立按钮；Capture 使用正式 All/Continuous 合同，恢复/回放仍显示 coordinator 返回的接受与失败结果。
- Restore 现在只允许选择当前历史中真实存在且 `CanRestore` 的 checkpoint；Input Replay 只允许落在当前采集 Tick 范围内，直接命令调用也返回对应拒绝原因。
- Timeline 窗口关闭时会通过 `WindowClosed` 通知清理 Skill Observation 的 active Timeline 和 overlay，不再只依赖下次刷新发现窗口不存在。
- 纯内存 Surface Dispose 时释放命令、拖动草稿、选择和视图引用；Timeline 关闭不再有待销毁的 Slate 组件树或静态 Projection。
- Timeline 窗口新增完整构建后的 `WindowOpened` 生命周期通知，Runtime Observation Bridge 在 Slate Surface、属性区和恢复状态都建立后再刷新；不再依赖过早的 AssetOpened 时机。
- Timeline 顶部和对应 Graph Shell/SkillGraph Preview 区显示当前 `TimelineAuthoringFingerprint` 的短作者 revision，并在正式 `TimelineData.OnValueChanged` 后广播变化；它只表示作者内容，不冒充运行时 adoption。
- C# authoring typed 合同已交付后，Timeline adapter 的新增 Clip 配置使用 `Read -> typed configuration 覆盖 popup 输入 -> Configure`；UI 不建立第二份业务配置模型。

## 正式能力对账

Skill Document exporter、Timeline authoring applier 和 validator 继续消费 `TimelineAuthoringTrackBinding`、`TimelineAuthoringClipBinding`、`TimelineContractCatalog` 与 Scene external binding contract。UI 没有复制 Document schema；Scene `valueCurve` 也使用同一正式曲线 descriptor。

## 编译证据

已通过：

```text
dotnet build 3cDemo/Client/3C_Client/BTSMTL.Timeline.Tree.Editor.csproj \
  --no-restore --disable-build-servers /nr:false /m:1 /p:UseSharedCompilation=false
```

结果为 0 errors；仅有项目及第三方既有 warnings。每次构建后执行 `dotnet build-server shutdown`。

本轮 Slate、BTSMTL.Timeline.Editor 和 BTSMTL.Timeline.Tree.Editor 通过局部编译；结果为 0 errors，警告为项目及第三方既有 warnings。该验证不替代 Unity Editor 端到端验收。

## 尚未完成

- 场景预览的非 Skill 正式目标、角色目标选择和纯 Timeline 调用入口仍未完成；SceneAsset/ContextId 的精确启动定位已完成。
- 作者 Timeline revision 与真实 Build/adoption 的精确匹配仍未完成；当前两者并列显示，分别来自 TimelineData 和 coordinator，避免伪造采用关系。
- 历史 Capture、checkpoint restore、输入 replay 的完整能力门禁和完成结果。
- C# authoring r2 的 typed Clip 合同和 Projection 接线已完成；公共 binding 旧 JSON 方法删除、公共输出根挂接和剩余 Agent 消费清理仍由 C# authoring owner 负责。
- 当前仍可定位到旧 JSON 消费者：`AgentSkillFlowDocumentExporter.cs:547` 调用 `TimelineAuthoringClipBinding.Export`，`BtsmtlSkillTimelineAuthoringApplier.cs:379` 调用 `TimelineAuthoringClipBinding.Apply`；在 C# authoring 的 Skill/FSM/Pose/EventGraph 迁移完成前，本 change 不删除这些共享路径。
- 纯 Timeline 预览目前缺少正式的非 Skill Runtime Owner 内容选项/播放 identity 合同；现有 `IBtsmtlScenePlayRuntimeOwner` 只提供 Ready/Failure/Release，不提供可请求的 Timeline 内容列表，因此不按资源扫描或显示名猜测目标。
- authoring revision 与 Character Program `SourceRevision` 属于不同正式哈希域，当前没有 owner 提供二者的 Timeline 调用级对应关系；Preview 只并列显示，不伪造“已采用”。
- 纯内存 Surface 的原生 DopeSheet 参数列表/缓存迁移、TreeClip 专用下钻和最终 Unity Editor 视觉证据仍未闭合；这些属于 tasks 11.3/11.7 的剩余实施，不恢复旧组件树。
- 最终联合窗口的关闭、重载、切页和绑定释放验收，以及基于真实 Unity Editor 操作的截图证据。
