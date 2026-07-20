# Design: Timeline Authoring Preview 纯表现边界

## Context

现有 Preview Controller 有两条执行路径：

```text
纯 Animation Timeline -> PreviewPlaybackEngine
含 TreeClip/Motion Timeline -> PreviewSimulationExecution -> Preview Simulation Session
```

第二条路径为了保持 Gameplay 语义完整，必须准备正式 Program、Source、Pipeline、Solver、Actor、Output 与 Presentation。游标 seek 又固定重置 lifecycle，导致每个 PointerMove 都重复装配完整 Session。优化重放或增加 checkpoint 只能降低成本，仍会把 Gameplay simulator 留在动画作者窗口中。

## Goals

- Authoring Preview 拖动只做当前时间的动画表现采样和已创作 MotionCurve 的只读轨迹投影。
- 保持正式动画 producer、queue、lifecycle 与 Animancer adapter，不直接 `AnimationClip.SampleAnimation` 或 `Animancer.Play`。
- TreeClip、Cue、Motion 与 Window authoring 继续可见、可编辑、可下钻。
- 真实 Gameplay 执行只有 Program/Session 一条路径，由 Live Debug 投影。
- 删除 Preview Simulation 的全部专用合同、代码和配置，不保留兼容入口。

## Non-Goals

- Authoring Preview 不验证 TreeClip Decision/Commit、Action admission、跨来源 Motion arbitration、MotionWarp、碰撞或 WorldSolver。
- 本 change 不增加磁盘录制、Simulation replay 或新的调试窗口。
- 本 change 不改变 Timeline compiler、正式 Program operation、Projection producer 或 runtime Live Debug trace。

## Decisions

### Authoring Preview 只拥有表现时间

`TimelinePreviewSession` 继续拥有 session identity、time、play speed 与 playback generation。它把 Timeline 和当前时间提交给 `TimelinePreviewTarget`，但不再保存 Action target snapshot，也不创建输入或逻辑 Tick。

Timeline 游标、Clip 与轨道拖动统一以 PointerDown 的面板坐标为固定起点。拖动元素可以在 PointerMove 期间更新布局，但位移计算不得读取该移动元素的新局部坐标；否则布局变化会抵消鼠标位移，使游标无法连续推进量化帧。

拖动器成功捕获 Pointer 后拥有完整手势，并停止 Pointer 冒泡与兼容 Mouse 事件。Timeline 标尺吞掉左键 MouseDown，框选器只处理轨道空白区域发起的手势，不得与游标拖动同时激活。

### Preview Controller 只有一个 Engine

`CharacterPipelinePreviewController` 始终创建 `PreviewPlaybackEngine`。连续播放与手动 seek 都复用同一 playback generation：连续播放推进表现时间，手动 seek 直接更新当前 producer 的 sample time，并由 Animancer 在零时间步应用精确姿势。只有 Timeline、Target 或 authoring 内容切换时才 retire 旧 generation 并重置动画 lifecycle。`PreviewSession` 删除 Simulation 分支。

手动 seek 不得将同一个动画 producer 重新解释为一次逻辑切换。若每次 PointerMove 都 retire 并重新 Play，而表现增量又为零，新的淡入权重无法推进，游标虽变化但角色姿势会停在旧采样。Preview 只提交新的 sample time，`AnimationPlaybackLifecycle` 继续持有当前 playback，`AnimancerPlaybackAdapter.UpdateSample` 直接设置动画时间。

`AnimancerPlaybackAdapter` 使用显式 transition evaluation mode。正式 Character Presentation 使用 `Timed` 并按 Presentation Profile 推进 producer transition；Timeline Authoring Preview 使用 `Immediate`，仍经过同一 lifecycle 与 adapter，但首次选中和切换 producer 时以零 duration 立即应用目标姿势。Timeline Clip 自身的重叠权重仍由 Projection sample 保留，Authoring Preview 只是不模拟 Graph producer 之间的实时时间淡入。

共享 Timeline 可以由多个 TimelineNode 编译为多个 Timeline operation。Authoring Preview 不执行或选择这些逻辑入口，动画 producer 身份仍由同一个 Timeline/Track authoring identity 决定；预览事件从匹配 operation 中确定性选择最小 handle 作为身份盐，不把合法共享引用判定为冲突。

### MotionCurve 只投影已创作轨迹

AnimationTrack 通过 Projection producer 采样。MotionCurve 使用自身正式的区间采样 API，从 Timeline 0 时刻按 `TimelineUtility.FrameRate` 重建到当前时间的累计位移和朝向。每次 seek 都从预览开始姿态绝对求值，不依赖上一次游标位置，因此向前、向后和反复拖动不会累积漂移。

投影只修改 `CharacterPipelineHost.VisualRoot`。Local 位移按当时累计朝向旋转，World 位移直接累加；退出预览、切换 Target 或 Timeline 时恢复进入预览前的位置和旋转。预览不得修改 logic root、CharacterController、Simulation body 或场景碰撞状态。

Authoring Preview 只接受每个采样区间至多一个可解析 MotionCurve contribution。单来源下 Additive、WeightedBlend 与 Override 都按其权重投影；若多个来源在同一区间重叠，预览显式报错并要求通过正式 Session/Live Debug 验证，不能在作者窗口复制一套会漂移的 Motion arbitration。

TreeClip、Action Cue 与 MotionWarp 只由 Timeline Editor 的 authoring renderer 显示。MotionWarp 需要正式 Action target snapshot、body state 与 modifier lifecycle，Authoring Preview 不伪造这些输入，也不执行目标修正。

### Live Debug 是 Gameplay 事实入口

正式 Session 运行后，Graph 与 Timeline Live Debug 继续消费同一 diagnostics store。TreeClip phase、Action、Window、Motion request、MotionWarp 和 Solver result 只从 runtime trace 显示，不从 authoring time 推测。

### 删除 Preview Simulation 模块

Preview Source、Pipeline 与 passes 只有 Timeline Preview 一个消费者。删除消费者后同步删除其 portable contracts、Unity definitions、配置资产与 Host 字段，避免留下可被重新接回的废弃能力。

Preview Source 曾通过 `SimulationTickSourceKind.Preview` 和 `EntryOperation` 绕过 Root operation，直接从 Timeline operation 启动 Float32 Program。删除 Source 后，Float32 与 Fixed 的 StepInput、Kernel request、evaluation frame、timeline target 和 trace 均不再保留该无生产者入口；正式执行始终从 Program Root operation 开始。

## Tradeoffs

- 优点：动画拖动不再创建 Session、隐藏 CharacterController 或重放逻辑 Tick；作者可以快速调整动画和衔接。
- 优点：Authoring Preview 与 Live Debug 职责清楚，Gameplay 只有正式运行链。
- 优点：MotionCurve 位移可与动画姿势同步拖动，且不会创建 Session、Solver 或写入 Gameplay 状态。
- 代价：编辑模式不能直接预览 MotionWarp、多个并发 Motion 来源的仲裁或 TreeClip 副作用；作者必须运行场景并使用 Live Debug 验证。
