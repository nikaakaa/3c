# Timeline Slate 重构实施说明

## 当前边界

Timeline 的持久化真相仍是 `BTSMTL.Timeline.TimelineData`。Slate 只承担编辑表面，临时 `Cutscene`、Group、Track、ActionClip 和 AnimatedParameter 使用 `HideAndDontSave`，不会写入资产、Document 或运行时编译产物。

Scene Play、Skill 请求、Build、采用、历史恢复和输入回放仍由 Graph Shell 与正式预览 coordinator 拥有。Timeline 只显示作者帧、编辑曲线，并接收精确运行观察标记。

## 已完成代码链

```text
TimelineEditorWindow
  -> TimelineEditorOpenRequest
  -> TimelineEditorSessionContext
  -> BtsmtlSlateTimelineProjection
  -> Slate CutsceneEditorSurface
  -> Slate Track/Clip/Curve 手势
  -> source identity 快照
  -> Session.Apply
  -> TimelineData 正式 owner
```

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
- 左右轨道共享行高和滚动，曲线展开同步；属性区可折叠、调高，窄窗口自动收起属性区。
- `SurfaceLayout` 统一计算 Slate Surface 的工具栏、搜索、标尺、轨道区域、时间区域和命中几何。
- DopeSheet 只按像素密度减少显示 key，正式 key、切线、权重和 wrap 不被删除或量化。
- Graph Shell 预览控制按场景控制、试验与采用、观察、历史与录制分组；历史刷新不会覆盖作者已经输入的 Tick。

## 正式能力对账

Skill Document exporter、Timeline authoring applier 和 validator 继续消费 `TimelineAuthoringTrackBinding`、`TimelineAuthoringClipBinding`、`TimelineContractCatalog` 与 Scene external binding contract。UI 没有复制 Document schema；Scene `valueCurve` 也使用同一正式曲线 descriptor。

## 编译证据

已通过：

```text
dotnet build 3cDemo/Client/3C_Client/BTSMTL.Timeline.Tree.Editor.csproj \
  --no-restore --disable-build-servers /nr:false /m:1 /p:UseSharedCompilation=false
```

结果为 0 errors；仅有项目及第三方既有 warnings。每次构建后执行 `dotnet build-server shutdown`。

主 Editor 工程的联合编译仍受工作区已有状态影响：当前可复现的错误来自 `AgentAuthoringPresentationPackageExporter/Validator` 找不到 `BTSMTL.EventGraphs`，以及部分构建轮次的 `ThirdPersonSimulation.Core.dll` 文件锁；这些不属于 Timeline 改动，未在本 change 中引入 fallback 或旁路。

## 尚未完成

- 预览宿主边界审计：Scene Play presenter 当前挂在 `GraphAuthoringEditorShell/BaseTreeWindow`；Skill FlowCanvas 仍通过 `BtsmtlSkillFlowGraph.OnGraphEditorToolbar` 绘制自己的 IMGUI 入口，项目没有现成的共同 presenter 挂载点。因此 P1 不能只凭 BaseTreeWindow 上出现分组控件宣称完成，后续需要由预览 change 提供正式共享宿主合同。
- 场景预览 coordinator 的精确 SceneAsset/context/非 Skill 目标接线。
- Timeline runtime binding 的 Follow/Pin、多调用选择和历史位置独立显示。
- 编辑 revision 到真实 Build/adoption 状态的 Graph Shell 展示。
- 历史 Capture、checkpoint restore、输入 replay 的完整能力门禁和完成结果。
- 最终联合窗口的关闭、重载、切页和绑定释放验收，以及基于真实 Unity Editor 操作的截图证据。
