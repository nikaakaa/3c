# Timeline Slate 重构实施说明

## 当前边界

Timeline 的持久化真相仍是 `BTSMTL.Timeline.TimelineData`。BTSMTL 路径只创建 Slate 的 `CutsceneEditorSurface` UI ScriptableObject 和短生命周期曲线编辑数据，不创建 `GameObject`、`Cutscene`、Director、Group、Track 或 ActionClip 对象代理，也不把 Slate 编辑对象写入资产、Document 或运行时编译产物。

Scene Play、Skill 请求、Build、采用、历史恢复和输入回放仍由 Graph Shell 与正式预览 coordinator 拥有。Timeline 只显示作者帧、编辑曲线，并接收精确运行观察标记。

## 已完成代码链

```text
TimelineEditorWindow
  -> TimelineEditorOpenRequest
  -> TimelineEditorSessionContext
  -> BtsmtlSlateTimelineProjection（只持有正式 Binding，不创建 Slate 组件）
  -> Slate CutsceneEditorSurface.InitializeEmbedded(IEmbeddedTimelineBinding)
  -> Slate Track/Clip/Curve 手势
  -> TimelineData identity 快照
  -> Session.Apply
  -> TimelineData 正式 owner
```

这条链路没有 Slate `Cutscene` 输入。`IEmbeddedTimelineBinding` 只向原 `CutsceneEditorSurface` 提供显示字段、作者帧、选择和正式命令；编辑中的 Clip/曲线对象是非持久化适配数据，提交时通过 `TimelineEditorSessionContext` 回写正式 `TimelineData`。

新增链路：

- `+ Track` 使用 `TimelineContractCatalog`、`TimelineAuthoringTypeCatalog` 和正式 Track 字段 binding。
- 轨道右键按作者帧打开 Add Clip 表单；Animation、Tree、Motion、MotionWarp、Camera、Cue 和 Scene binding 均走正式类型工厂/`TimelineAuthoringClipBinding`。
- Add Track/Add Clip 失败或 owner revision 过期时保留表单输入，不留下半成品或额外 Undo。
- Scene Presentation 的 `valueCurve` 已注册到正式 `TimelineCurveChannelCatalog`，Slate 编辑后通过 `TimelineCurveAuthoring.Replace` 回写正式曲线。
- 运行观察从 `BtsmtlSkillObservationSession` 按 Timeline/Graph/Node 精确筛选 `RuntimeTimelinePlaybackDebugSummary`，调用 `TimelineEditorWindow.ApplyRuntimeObservation`；运行线和作者编辑游标分离。
- Graph Shell 历史折叠区的 `Segment` 驱动 `RuntimeDebugSession.HistoryOffset`，历史观察调用 `ApplyHistoryObservation` 绘制独立 History 线；历史线、实时线和作者帧互不写同一个时间状态。
- Timeline 顶部的 `Preview` 只返回已绑定的 Graph Shell；没有绑定时明确提示，不启动 Play、不重建 Session。
- `TimelineRuntimeObservationBridge` 消费统一 `RuntimeDebugSession` 的正式 Timeline playback summary；唯一调用显示 Runtime/History overlay，多调用保持不猜选，避免复制 Scene Play 播放器。

作者界面：

- 嵌入 Slate 顶栏提供 `+ Track`、上一帧、下一帧、Fit 和当前作者帧；没有 Timeline 本地 Play/Sample/ReSample/Stop。
- 左右轨道共享行高和滚动，曲线展开同步；外层 Timeline 窗口不再添加 Toolbar、SplitView、Runtime 菜单或右侧自制 Inspector，只承载原 `CutsceneEditorSurface`。
- formal Clip 的时间、Blend、DopeSheet、关键帧和曲线交互全部由原 Slate `ActionClipWindow` / `ActionClipWrapper` 通过 `IEmbeddedTimelineBinding` 接回正式 Session；不再创建 `TimelineFormalClipDetailsView`、MotionWarp/Tree 自制属性面板。
- Timeline 打开时清空外部 Unity Selection，作者选择只进入 Timeline binding 与 Slate Surface，避免旧 Slate ActionClip Inspector 残留的 Actor/AnimatedParameter 报错。
- `TimelineEditorWindow` 所在的 `BTSMTL.Timeline.Tree.Editor` asmdef 显式引用 Slate 与 `ThirdPersonCamera.Contracts`；Timeline 窗口只作为 Unity 容器，实际可见编辑 UI 来自原 Slate Surface，均不改变 TimelineData owner，也不引入 Slate 组件树。
- 嵌入绘制现在从原 `ShowGroupsAndTracksList` / `ShowTimeLines` 入口进入；正式 binding 只在原函数入口处分派数据，`OnEmbeddedTimelineGUI` 不再直接调另一套顶层列表/时间线入口。
- 嵌入游标、逐帧快捷键和引导线也从原 `DoScrubControls`、`DoKeyboardShortcuts`、`DrawGuides` 入口进入；这些入口在正式 binding 下只切换作者时间、调用正式 Clip 编辑或绘制引导，不启用 Slate 播放或采样。
- formal Clip 不再使用独立的嵌入 Clip 手势循环；Slate 原 `ActionClipWindow` / `ActionClipWrapper` 通过 binding 分支读取 formal 时间、Blend、曲线和选择，拖动/裁剪/混合/DopeSheet/菜单仍走同一窗口交互，提交仍由 `IEmbeddedTimelineBinding` 接回 Session。
- 正式 Timeline 的快捷键在 `CutsceneEditor.DoKeyboardShortcuts` 入口先分流：逗号/句号只逐帧移动，K/S/F/C 分别进入正式 Clip 的加 key、拆分、适配和清理曲线；Space 被明确消费，不进入原生 Slate 播放。删除 Track/Clip/Section 等已经由 binding 自己调用正式 Session 的命令，不再被外层 Slate 草稿事务重复包裹。
- `BtsmtlTimelineDirectorBinding` 已删除；Projection 自身只实现 Slate `IDirector` 所要求的 root 合同，所有播放、采样和受影响 Actor 接口保持空实现，不再分配第二个 Director 对象或第二套时钟。
- `SurfaceLayout` 统一计算 Slate Surface 的工具栏、搜索、标尺、轨道区域、时间区域和命中几何。
- DopeSheet 只按像素密度减少显示 key，正式 key、切线、权重和 wrap 不被删除或量化。
- formal 参数展开区复用 Slate 的前后关键帧、加/删 key 和当前值显示入口；关键帧命令通过正式参数 binding 和 Session 提交，前后关键帧只移动作者游标。
- 原生 CurveEditor 的 formal cache key 使用当前 Surface、Clip AuthoringId 和参数 Id；同一组曲线不会因每帧重绘重置选择，正式 binding 刷新后也不会遗留旧 renderer 的 Undo 订阅，关闭 Surface 时统一释放。
- 相邻 Clip 的重叠显示仍保留 Slate 图形，但自动把重叠量写入 `BlendIn/BlendOut` 的逻辑只对真实 native Cutscene 生效；formal Ease 只在明确的边缘手势中通过正式 Session 提交，多选删除也直接调用正式 binding。
- formal 轨道参数绘制恢复原 Slate 行内坐标组；formal Clip 标题只由 `ActionClipWrapper` 统一绘制，底部 DopeSheet 条保留样式但不重复写 Info。`TimelineData.ApplyModify` 使用同一 Undo group，正式 mutation 抛错时回滚 owner 并重新建立序列化绑定。
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
- Timeline 绑定关闭时只释放 `CutsceneEditorSurface` 和编辑适配数据；没有临时 Slate GameObject、组件树或延迟重建代理需要销毁。
- Timeline 窗口新增完整构建后的 `WindowOpened` 生命周期通知，Runtime Observation Bridge 在 Slate Surface 和恢复状态都建立后再刷新；不再依赖过早的 AssetOpened 时机。
- Timeline 顶部和对应 Graph Shell/SkillGraph Preview 区显示当前 `TimelineAuthoringFingerprint` 的短作者 revision，并在正式 `TimelineData.OnValueChanged` 后广播变化；它只表示作者内容，不冒充运行时 adoption。
- C# authoring typed 合同已交付后，Projection 的新增 Clip 配置改为 `Read -> typed configuration 覆盖 popup 输入 -> Configure`；已删除 `BuildClipProperties`、`JObject` using 及旧 `Export/Apply` 消费。当前 Client 源码树中已不存在 `AgentSkillFlowDocumentExporter`、`BtsmtlSkillTimelineAuthoringApplier` 或 `TimelineAuthoringClipBinding.Export/Apply` 消费者。`BtsmtlSlateTimelineProjection` 仍是正式打开协调器，但只持有 `TimelineEditorSessionContext`、`BtsmtlSlateTimelineBinding` 和 `CutsceneEditorSurface`，不再创建 `GameObject`、`Cutscene`、Group、Track、ActionClip 代理。C# authoring 直接读取 `TimelineAsset.Data`、`TimelineData.Tracks/Sections/ExternalBindings` 与 `SerializedOwner/SerializedPropertyPath`；未新增 Slate 遍历、输出器或编辑器布局模型，10.3 已按既有正式 API 收口。

## 正式能力对账

Skill Document exporter、Timeline authoring applier 和 validator 继续消费 `TimelineAuthoringTrackBinding`、`TimelineAuthoringClipBinding`、`TimelineContractCatalog` 与 Scene external binding contract。UI 没有复制 Document schema；Scene `valueCurve` 也使用同一正式曲线 descriptor。

## 编译证据

2026-09-14 通过显式 Unity 实例刷新脚本后，Console 未出现 Timeline/Slate 类型错误；当前剩余错误来自其它窗口正在迁移的 Simulation Control 合同（`CharacterControlRuntimeState`、`CharacterControlRuntimeStateTransaction`、`CharacterControlStateSchema` 的构造/参数不匹配），因此 Unity 尚未加载本轮 Timeline 程序集，不能把当前结果当作曲线端到端通过。

本轮没有把主 Editor 的联合编译错误归因于 Timeline，也没有修改 Simulation 文件。真实打开、选 Clip、展开曲线、拖动 key、保存和重开仍待主工作区编译恢复后验证。

同日按项目要求执行窄编译：

```text
dotnet build 3cDemo/Client/3C_Client/BTSMTL.Timeline.Editor.csproj --no-restore --disable-build-servers /nr:false /m:1 /p:UseSharedCompilation=false /p:LangVersion=11.0
```

结果为 0 errors；输出包含 Unity TestRunner、InputSystem、TreeDesigner 和 Slate 既有 warnings；构建后已执行 `dotnet build-server shutdown`。该结果只证明 Timeline 编辑程序集源码闭合，不替代 Unity 主工程和真实 UI 验收。

后续重新执行同一完整 `BTSMTL.Timeline.Editor.csproj` 窄编译时，结果仍为 0 errors、19 个既有 warnings，并已再次关闭 MSBuild/C# 编译服务器；当前 Unity MCP HTTP 服务可连接但实例列表为空，因此新程序集尚未在主 Editor 内现场加载。

## 尚未完成

- 场景预览的非 Skill 正式目标、角色目标选择和纯 Timeline 调用入口仍未完成；SceneAsset/ContextId 的精确启动定位已完成。
- 作者 Timeline revision 与真实 Build/adoption 的精确匹配仍未完成；当前两者并列显示，分别来自 TimelineData 和 coordinator，避免伪造采用关系。
- 历史 Capture、checkpoint restore、输入 replay 的完整能力门禁和完成结果。
- C# authoring r2 的 typed Clip 合同和 Projection 接线已完成；公共 binding 旧 JSON 方法删除、公共输出根挂接和剩余 Agent 消费清理仍由 C# authoring owner 负责。
- 旧 JSON/Agent 文件协议消费者已从当前 Client 源码树清除；Timeline 公共 content/owner 读取沿用现有正式 API，编辑器局部选择和滚动不属于生成输出。
- 纯 Timeline 预览目前缺少正式的非 Skill Runtime Owner 内容选项/播放 identity 合同；现有 `IBtsmtlScenePlayRuntimeOwner` 只提供 Ready/Failure/Release，不提供可请求的 Timeline 内容列表，因此不按资源扫描或显示名猜测目标。
- 现有正式 Slate binding 已替代 BTSMTL 隐藏组件树，但右侧真实 owner Inspector 和 Section 的无 Director 编辑接线仍未完整收口；曲线展开/选择状态的稳定恢复已接通，仍需主 Unity Editor 现场验收。
- authoring revision 与 Character Program `SourceRevision` 属于不同正式哈希域，当前没有 owner 提供二者的 Timeline 调用级对应关系；Preview 只并列显示，不伪造“已采用”。
- 最终联合窗口的关闭、重载、切页和绑定释放验收，以及基于真实 Unity Editor 操作的截图证据。
- 当前无 Slate 对象入口需要在连接到正确的 `D:/Unity_Project_1/3C` Editor 后做一次真实打开、刷新、创建和曲线编辑验收；已连接的 Editor 是 `D:/Unity_Project_1/3C-parallel-test`，且本轮检查时尚未 ready，因此不能把该次连接当作主工作区证据。
