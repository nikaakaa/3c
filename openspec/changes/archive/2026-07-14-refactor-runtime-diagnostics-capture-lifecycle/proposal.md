# 提案：重构运行时诊断采集生命周期

## Why

当前 Live Debug 的卡顿不是单个窗口的刷新频率问题，而是运行时采集、Editor 分析和窗口绘制三个层面都在重复处理完整历史。

- `CharacterPipeline.RegisterDiagnosticsTarget` 创建的 `RuntimeTraceBuffer` 默认启用 `All` channel，因此即使没有任何调试窗口，Graph、StateMachine、Timeline、Animation、Motion 与 Blackboard producer 仍会构建并写入诊断事件。
- `RunnableNode` 每个 logic tick 写 `NodeStatus`，`CompositeNode` 每次条件判断写 `EdgeEvaluated`；Timeline 每个 logic tick 写 logic time、每个表现帧写 visual time；Animation 又为 sample、lifecycle snapshot 和 fade 写事件。
- `RuntimeDebugSession` 的每次 Editor update 都调用 `RuntimeTraceBuffer.Snapshot()` 复制所有可见 segment/event，并由 `RuntimeDebugAnalyzer` 从头重建 source map、element state、instance 索引、Timeline playback 摘要和 event 列表。
- Graph 与 Timeline 窗口各自在 `Update` 和 Session changed 回调中再次刷新：前者清空并重绘整个 overlay，后者用 LINQ 扫描完整 event 列表、重建菜单和细节。两个窗口同时打开时，重复成本继续叠加。

这会把“当前角色正在做什么”和“为了复盘而保存的完整过程”混成一条始终开启、始终全量重放的数据流。当前观察到 FPS 降至约 10，根因正是这条链路，而不是游戏 Tick 或 Animancer 本身。

## What Changes

- 将运行时诊断正式拆为两种数据：
  - **Live State**：只保留当前 Graph/StateMachine/Timeline/Animation/Blackboard/Motion 的最新事实，用于实时高亮、当前 Timeline playhead、当前 playback 和 Host Inspector。
  - **Capture/Rewind**：只在作者显式开始录制时保存有界历史，用于停止录制后的时间轴、scrub 与问题复盘。
- 每个 registered diagnostics target 默认关闭采集。Graph、Timeline、Host Inspector 进入观察时向共享 Session 声明需要的 channel；Session 对同一 target 汇总这些声明后，运行时只开启并集。最后一个声明释放后，target 回到 `None`，producer 不再构建 payload、解析 source handle 或写入诊断数据。
- 为 Capture 引入明确 detail：默认只保存生命周期、状态切换、edge selected、TreeClip 进入/退出、Timeline terminal、动画 lifecycle 等边界事实；逐 tick 条件求值、逐帧 Timeline time、animation sample/fade 等连续细节必须由作者显式请求，不能再默认无限地产生。
- 使用 target 级的共享增量 read provider：首次附着时缓存严格 Source Map，之后只按单调 cursor 读取 Live State 与 Capture 的新增变化。一个 target 在一个 Editor update 最多分析一次；Graph、Timeline 与 Host Inspector 都读取该 provider 的版本化 change set。
- Graph 与 Timeline 继续共享同一 target 和同一 Capture 位置，但保持各自 Follow / Pin binding。两者同时打开时共享一次采集、一次增量分析，绝不各自扫描 runtime buffer 或各自录制。
- 将当前 Live/Pause/History UI 改为清楚的产品语义：Live 观察当前事实；Capture 明确开始/停止；冻结 Live 只冻结当前 read model；Capture 停止后才显示可 scrub 的有界历史。不会再把持续滚动 buffer 伪装成“暂停后的稳定历史”。
- 删除旧的始终 `All`、全量 `Snapshot()`、每帧 full analyzer、`RuntimeDebugViewModel.Events` 作为 Live 数据源、窗口 `Update` 全量 overlay 刷新，以及直接向 Buffer 写全局 channel 的 API；不保留兼容包装或双写路径。

## 产品结果

作者打开 Graph 与 Timeline Live Debug 时，可以同时看到同一角色的当前 State、当前 Timeline logic/visual time、active Track/Clip 与动画播放生命周期；正常 Play 中没有任何诊断观察者时，不会因为 diagnostics 产生持续 Trace 成本。

当作者需要定位“刚才为什么没打断”或“哪一帧动画输出跳变”时，主动开始 Capture，并按需要开启条件求值或连续 sample 细节。停止 Capture 后，再在同一份冻结历史中让 Graph 和 Timeline 对齐查看。这是一次有明确成本边界的复盘，而不是把所有角色的每一帧都默默存下来。

## 不在范围内

- 不改变 Gameplay Tick、StateMachine、TimelinePlaybackScheduler、AnimationPlaybackLifecycle、Animancer、输入、动作、网络或角色表现的业务结果。
- 不改动现有严格 target 选择、source identity/content hash 校验、窗口本地 Follow / Pin、domain reload locator 恢复语义。
- 不增加第二套 runtime clone 读取、Timeline 重新采样、动画仲裁、Debug Server、场景搜索或名称/path fallback。
- 不以降低 Buffer 容量、限制为 30Hz、只减少 UI Repaint 或隐藏某些窗口作为正式修复；这些只能遮住症状，不能消除始终采集和全量重放。
- 不新增测试，不运行 Unity batchmode。

## Impact

### 现行规格对比

- `btsmtl-runtime-diagnostics` 当前把“实时观察、暂停、历史回看”都建立在每个 target 始终存在的 `RuntimeTraceBuffer` 上，并要求 Session 持有统一全量 Trace snapshot。该口径与无观察者时零采集成本、实时状态增量读取直接冲突。本 change 将其替换为 Live State + 显式 Capture 两条正式数据语义。
- `btsmtl-timeline-editor-preview` 与 `btsmtl-tree-inspector-information-architecture` 当前要求 Live Debug 读取共享 Trace snapshot。它们将改为读取共享的增量 read model；只有 Capture 模式才读取冻结历史，仍不重采样 authoring Timeline。
- `character-pipeline-runtime` 当前要求 active target 提供 Trace Buffer，并描述 target 持续产生 Trace。它将改为注册 metadata/source map 与按需 diagnostics store，未被订阅时不采集。
- `openspec/project.md` 当前“每个 active CharacterPipeline 注册独立 diagnostics target 和有界 Trace Buffer，视图共享 live/pause/history/frozen Trace snapshot”的表述需要同步替换为“默认关闭的按需 Live State 与显式 Capture，单 target 单 provider 增量共享”。

归档本 change 时必须更新上述 current specs 和 `openspec/project.md`，删除旧的 always-on buffer / full snapshot 表述及对应 API，不保留兼容路径。

### 依赖与影响范围

- 依赖既有 Debug Source Map、runtime target registry、严格 target resolver、窗口本地 binding 和 Timeline playback provenance。
- 主要实现范围为 `BTSMTL/Diagnostics`、TreeDesigner Graph Editor、Timeline Editor 与 `CharacterPipelineHostEditor`，以及当前 diagnostics producer 的调用边界。
- 现有动画表现架构仍是唯一业务权威；diagnostics 只观察其正式 lifecycle、selection 与 sample，不重新生成任何动画决定。
