# Workbench 当前实现状态

更新时间：2026-09-30

本文只记录当前工作区能从代码确认的实现状态。产品目标和验收合同以本 change 的 `design.md` 与 delta spec 为准；历史过程保留在 `implementation-audit.md`，不把历史提交或旧验收记录当作当前入口。

## 本次 Preview 设计与宿主接入（整体尚未完成）

2026-09-30 已确定采用 CMC 式隐藏编辑器 Scene，不进入 Unity Play。编辑器宿主接入已有 GameplayTickSystem，继续执行正式 Session 和完整节点图。打开预览区自动装配，正常使用不暴露准备步骤；只有耗时或失败才提示。生命周期方案和待对齐交互见 design.md 第 3.1、3.2 节。

Preview 已从 EditorPlayModeSceneLauncher 迁移到 BtsmtlScenePlayPreviewHost。Profile 改为 AssemblyPrefab、ContextId、DefaultActorId；GameplayLabFixed.unity 与隐藏场景共用 GameplayLabFixedAssembly.prefab，装配保留原嵌套 Prefab 与引用。隐藏 Scene 经公开 NewPreviewScene 创建，资源、Camera、Actor 和 Session 使用正式生命周期，既有 GameplayTickSystem 提供帧驱动。角色画面、技能图入口和角色输入请求已迁移到独立 Preview 窗口，原 Timeline 不再内嵌预览区；焦点键鼠输入、Ability 执行投影和完整历史恢复仍未完成，不能把宿主就绪当作整个 Preview 已可用。

## 当前边界

Workbench 使用原 Timeline 与 FlowCanvas 作者工作面，并通过独立可停靠 `BtsmtlScenePlayPreviewWindow` 显示共享角色视口和执行时间线。Slate Timeline 与 FlowCanvas 是编辑器表面，ScenePlay、正式 Timeline/Graph、Pose 和输入链才是运行事实的 owner。

当前代码没有把 RuntimeDebug 变成第二运行链：观察层只读取 `RuntimeDebugSession` 发布的目标、SourceMap、事件和 playback summary；Timeline 观察通过 Slate 的只读 Overlay 显示游标、活动 Track/Clip 和生命周期事实。

## 已接入的编辑器链路

### RuntimeDebug 开关和观察绑定

- `Editor/CharacterPipeline/ScenePlay/BtsmtlScenePlayTimelineController.cs` 负责共享 Profile、Preview 入口、RuntimeDebug 开关、Follow/Pin 菜单和来源导航请求。
- Preview 使用隐藏场景宿主的 Changed、Session、Actor 与失败状态；旧 PlayMode Launcher 不再是 Preview 调用路径。
- 普通进入 Play 不会因为打开 Timeline 就自动创建观察；打开技能 FlowCanvas 或 Timeline 时，才由观察入口接入 RuntimeDebug。
- `Editor/CharacterPipeline/Diagnostics/BtsmtlSkillRuntimeObservationAutoBinder.cs` 在技能 FlowCanvas 打开时请求正式运行观察，并使用 `RuntimeSourceElementKey` 解析当前图、节点、边和 Timeline 来源。
- 同一技能实例内来源节点变化会重新导航；并行候选仍要求作者 Pin，不能按候选列表顺序猜测目标。
- 自动 Follow 导航会保存并恢复导航前的编辑器焦点，观察跳转不会把 GameView 输入切走。
- Live RuntimeDebug 观察在 `RuntimeDebugSession` 以 30Hz 刷新；Capture 仍按既有 100ms 节奏读取，节流只作用于编辑器观察，不改变正式 Session、输入或 Timeline 时钟。
- Timeline playback summary 和 Scope 过滤复用窗口 scratch list，观察刷新不重复创建 summary、Scope 过滤和节点筛选容器。

这条链只负责把编辑器表面绑定到正式运行事实，不推进 Timeline、不创建 playback、不修改角色输入，也不把编辑器焦点转发到 GameView 输入链。

### Slate Timeline RuntimeDebug Overlay

- `Runtime/BTSMTL/Timeline/Editor/Scripts/Tree/TimelineEditorMainWindow.cs` 从当前 `RuntimeDebugViewModel` 读取对应 playback 的事件和 summary，并把运行时间、活动 Track、活动 Clip 与历史位置交给 Slate。
- `Runtime/BTSMTL/Timeline/Editor/Scripts/RuntimeTimelinePlaybackProjection.cs` 现在只保存 `RuntimeTimelineObservationBuffer` 事件缓冲；不再克隆运行 `TimelineData`，不调用 `TimelineData.Init`，也不拥有运行时播放器或时钟。
- `Runtime/BTSMTL/Timeline/Editor/Scripts/BtsmtlSlateTimelineProjection.cs` 和 `BtsmtlSlateTimelineBinding.cs` 保留原 Slate 表面和只读 Overlay，删除运行 Timeline 替换与 `RefreshRuntimeTimeline` 路径。
- 每次观察刷新都重新读取正式 playback 事实；作者 Timeline 资产仍是作者表面，不能被运行状态反写。
- Overlay 按逻辑/表现执行域的最新位置和 cycle 过滤 Clip 事件，保留已执行状态并排除未来 cycle。2026-09-30 补齐 TreeDecision 显示结束位置：活动实例取对应域游标，Exit 后固定正式退出时间，较晚 Destroy 不覆盖 Exit；History 使用其所选位置的 ViewModel 重新建立结束位置。
- TreeClip runtime event 携带正式 `ExitSource` 事实；仅 TreeDecision 向 Slate 提供运行结束位置。Slate 使用该位置计算只读片段的实际绘制与命中宽度，删除仅在作者区间之外绘制延长条的旧路径；FrameBoundary 保持作者区间，不反写资产。结束位置复用观察缓冲，投影层不再复制一份字典。

因此当前实现的真实入口是“正式 playback 事实 → 事件缓冲 → Slate Overlay”，不是“运行副本 Timeline → Slate 替换”。

## 尚未闭环的能力

以下项目仍不能标为完成，文档不得把它们描述为已可用：

1. **Preview 隐性正式 ScenePlay 入口（任务 4.1、4.2）**：有效 Profile 已能装配隐藏 Scene、激活唯一正式 Session，并在原 Timeline 窗口显示正式角色相机结果；Ability 输入、执行投影和完整编辑交互仍未闭环，不能据此认定 Workbench 已可用。
2. **RuntimeDebug 调用栈闭环（任务 5.2、5.3、5.4）**：代码已有 target、SourceMap、候选解析和来源导航入口，但尚未取得可复现运行记录证明 RootTree → 子图 → Timeline/TreeClip → 父调用返回能在 FlowCanvas 与 Slate 间正确切换，且未执行内容不会提前显示。
3. **特定 TreeClip 的动态长度观察与历史重建（任务 6.2、6.3）**：只有声明 `TreeDecision` 结束来源的动态 TreeClip 才需要实际退出位置和开放状态；固定区间 Clip 仍使用正式作者边界。Slate 已接入对应 playback/cycle 的动态结束位置与历史投影；记录时内容与实际起点的完整显示、退出前后回看及多 playback 隔离仍需继续核对，尚未完成 Unity 端到端验证，任务 6.2、6.3 不勾选。

这些未完成项不能通过作者游标、当前 Timeline 内容、默认 Actor 或第二个播放器补齐；缺少正式事实时应显示未绑定、无匹配或尚无可导航事实。

## 当前代码入口

以下路径是当前实现的主要调用链，均相对于 `3cDemo/Client/3C_Client/Assets/GameScripts/Main`：

```text
BtsmtlScenePlayTimelineController
  → BtsmtlSkillRuntimeObservationAutoBinder
  → RuntimeDebugSession / RuntimeDebugViewModel
  → TimelineEditorMainWindow
  → RuntimeTimelineObservationBuffer
  → BtsmtlSlateTimelineProjection Overlay
```

FlowCanvas 的实际页面导航仍由 `RuntimeDebugSourceNavigator` 和 `BtsmtlSkillObservationSession` 复用已有编辑器面板完成；它们不能据此推断运行链已经通过端到端验收。

## 本轮恢复 Goal 后的接入证据

- Goal 已恢复 active；本轮没有将任何尚未闭环的大任务标为完成。
- 目标 Unity `e852139597e42532` 核对为本项目，修改前 playing=False、compiling=False、profiler=False。
- TreeDecision 显示修改已完成 Editor 编译与重载：scriptCompilationFailed=False，Console error=0；反射确认 CutsceneEditorSurface 的运行状态接入口已加载新的 3 参数签名。
- 本轮 diff 检查通过；未新增测试代码、未进入 Play、未执行端到端回放，绘制结果仍需实际使用验证。
- YooAsset 的同一 OperationSystem 已接入 Edit Mode 更新；UniTask 复用现有 Editor PlayerLoop。ProjectSceneResourceHost.PrepareAsync、Session.Activate、Actor.Initialize/Release 与 Camera.Initialize 暴露正式宿主生命周期，未增加备用加载器。
- 隐藏宿主已实际进入 Active：playing=False，IsPreviewScene=True，普通 sceneCount 维持 1，预览 PhysicsScene=(2723)，与 defaultPhysics=(0) 隔离。
- 暂停无命令时不提交逻辑或表现帧。实际单步由 tick/frame=1/1 到 2/2，等待后仍为 2/2；Close 后 TickSystem 未初始化，previewSceneCount 回到开启前的 1。
- 修复零增量表现帧触发 animationDeltaSeconds 异常，以及 Pose 运行图在 Edit Mode 使用 Destroy 的释放错误。后续短时验证未复现这两项错误。
- 响应性观测：Workbench 关闭后后台 3 秒约 16ms CPU；含一次单步的 3 秒窗口约 62ms CPU。失焦 Editor update 平均约 107ms，不能据此推断前台交互帧率。另一个采集操作同期触发 Profiler raw 反序列化与整机提交内存 93% 告警，未操作其采集数据。
- 所有本轮验证结束后均关闭隐藏预览；未将连续播放留在后台。

## 共享视口与关闭生命周期接入记录

- 原 Timeline 窗口的「预览画面」接入隐藏宿主的正式输出相机，以 URP RenderRequest 渲染到窗口纹理；只有正式表现帧或视口尺寸改变时重新渲染，暂停保留上一帧。
- 目标 Editor 脚本编译通过。实际输出包含角色与场景，未进入 Play；单步前 tick/render frame=1/1，单步后为 2/2，等待后仍为 2/2。
- 收起预览画面、关闭最后一个 Timeline 窗口都会关闭隐藏宿主。实际关闭窗口后 IsOpen=False、GameplayTickSystem.IsInitialized=False。
- 修复关闭当帧仍在 Unity update 调用列表中的宿主回调访问已释放 Session 的异常；关闭状态直接退出该回调，后续短时验证未复现。
- CPU 采样不能作为前台 FPS 验收：窗口单独打开约 47ms/3s，隐藏宿主且不显示画面约 31ms/3s，稳定静止视口约 94ms/3s；一次单步所在窗口约 3063ms/3s，随后暂停窗口约 500ms/3s。首次渲染与单步附近的额外开销仍需定位，不能宣称编辑器性能问题已经解决。
- 验证结束已关闭预览，未留下连续播放；未操作其它窗口 Profiler 数据，未清理 Console 或日志，未新增测试代码。

## Edit Mode 观察与正式输入接入记录

- FlowCanvas 观察绑定、子图观察和来源导航已移除 Unity Play 前提，改由正式诊断目标、明确执行 identity 与当前页面决定绑定；RuntimeDebugSession 按注册目标解析，删除只要求进入 Play 的旧状态。
- 预览工具栏从当前 Actor Definition 构建技能图菜单，从正式 InputProfile 构建动作请求菜单。「提交输入」调用 FixedCharacterHost.EnqueueAbilityInputRequest，消费仍由正式下一逻辑 Tick 完成，不绕过请求仲裁。倍速和表现时钟按钮提交既有 GameplayTickDriveCommand。
- 2026-09-30 目标 Editor 完成这批程序集编译，scriptCompilationFailed=False，验证时 Console error=0。隐藏预览在 playing=False 下提交 Attack 并单步 3 Tick，获得有效 RuntimeDebug 记录。
- 实际复现 Follow 已入队但 delayCall 未执行；同一导航方法直接执行能正确定位。连接刷新与 Follow 改为由既有 Editor update 消费待处理标记，暂停且没有状态变化时不反复导航。修改后 Attack 自动定位 Attack1 State Body，editorObservation 非空，待导航标记归零。
- Attack 后提交 Dodge 时导航报告两个并行 playback。进一步直接读取正式 CharacterTimelineHost，两个 playback 均为 Running、LogicOwnerReleased=False、TerminalPublished=False；不能将其判成诊断漏报后伪造终止，也不能直接选最新输入。正常结束后再次触发、抢占路径的正式生命周期与多调用导航仍需继续验证，5.2–5.4 保持未完成。
- 用户已明确确认：关闭最后一个承载预览的窗口结束并释放 Preview；还有承载窗口时普通页面关闭保留 Session。design、相关 delta 与现行 Timeline 预览 spec 已同步；实际游戏 Session 不受观察窗口关闭影响。
- 本轮验证均为有限单步，没有开启持续播放。期间其它模块触发编译与域重载，连接曾暂时失效；未修改其代码或操作其 Profiler 数据。

## 共享渲染归属修正

- BtsmtlScenePlayPreviewHost 现在持有唯一 BtsmtlScenePlayPreviewRenderer。页面只登记视口尺寸、显示共享纹理，不再分别创建 RenderTexture 或提交相机 RenderRequest。
- 渲染器使用当前可见视口中最大面积对应的尺寸生成同一画面，其它页面按比例适配。正式表现帧和尺寸均未变化时不重新渲染；普通页面重建期间保留宿主纹理，结束预览统一清理纹理与视口登记。
- 本批修改通过目标 Editor 编译与程序集重载，scriptCompilationFailed=False；Workbench 文件 diff 检查通过。
- 实测前隐藏 Session 在 Pose 初始化失败：Foot Placement calibration schema '5' is unsupported。因此双视口计数得到 0 次渲染、两张纹理均为空，属于无效性能样本，不能作为优化前后比较或帧率改善证据。未修改或绕过 FootPlacement；完整共享画面与关闭释放仍待正式 Session 可运行后验证，任务 7.7 暂不勾选。

## 执行区间归并修正

- 复用 RuntimeExecutionTimelineBuilder 时发现开放区间更新只修改 PendingSpan 结构体副本，未回写字典；已补齐写回，使 NodeRunning 等后续记录更新同一开放区间的末端。
- 归并输入与结果排序改为正式 Sequence，避免把逻辑 Tick 和表现 Frame 的数值大小当作同一时钟。SpanKey 分离表现域；Lifecycle 与 Logic 共用 RuntimeDiagnosticsContext 正式记录的逻辑 Tick 空间。时间尺仍须使用各自正式时钟，Sequence 不替代时间坐标。
- 目标 Editor 编译通过且新 SpanKey.Domain 已进入程序集，Console error=0；diff 检查通过。尚无本轮可用 Capture 的运行回看证据，不能据此勾选 Ability 执行投影或 History 完成；当时 Slate 执行片段表面尚未接通；当前代码状态见后续接入记录，历史角色恢复仍未完成。

## Capture 增量读取与执行区间接入

- LiveState 与 Capture 读取改为向观察者复用的 List 复制变更，删除每次读取新建数组的旧入口；固定输入证据采集调用方同步迁移。Capture 读取携带 CaptureId、版本、通道、精度与淘汰数量，新 Capture 即使版本数值相同也重置读取游标。
- RuntimeDebugTargetProvider 在页面请求执行时间线时按 Capture 变更追加 RuntimeExecutionTimeline 区间；普通 Live 刷新不建立区间或申请投影缓冲。开放区间保留原索引并更新末端。完整实时 Session 的 BuildExecutionTimeline 通过 ReadExecutionTimeline 消费上次投影之后的记录，不再冻结 Capture 或排序全部历史。冻结历史使用同一个 SpanAccumulator，删除旧 PendingSpan 归并实现。
- Capture 切换或窗口淘汰导致的 RequiresFullSync 会重建保留部分；选定实例的关联筛选与 BuildExecutionProjections 的历史部分仍未完成增量接入；Slate 执行表面的后续接入见下文，不能据此宣称任务 4.14 或 7.6 已完成。
- History 的 GetEvents(historyOffset) 修正为返回截至所选 Segment 的前缀，原代码丢弃较早记录并返回后缀，会把未来片段带入过去。
- 本批源码 diff 检查与 OpenSpec strict 校验通过；目标 Editor 编译结束，scriptCompilationFailed=False、playing=False，反射确认 SpanAccumulator 与三参数 CopyCaptureSince 已加载，Console error=0。未新增测试代码，未执行用户端到端验收，尚无本批 FPS 或 GC 实测结论。
- 随后的隐藏预览准备检查被外部 Play 切换打断：实际读取 playing=True、previewOpen=False。没有将启动请求作为成功运行证据，也未停止外部场景或留下隐藏预览。

## Slate 绘制与只读入口静态修正

- 用户要求先实施代码、静态检查；此批修改没有通过 Unity MCP 刷新、启动 Preview 或运行验收。
- CutsceneEditorSurface 的正式与原生 Track 绘制不再每次 Select/ToArray 新建全部 Clip 包装对象和数组。绑定按结构变化创建，绘制复用；作者绑定 Rebuild 后显式释放旧绑定引用，重新装配和关闭表面也清理缓存。
- Clip 布局使用 group/track/clip 数值元组标识，GUI.Window 使用分配给包装对象的整数窗口身份。删除每次绘制时三次 ToString、字符串拼接与 int.Parse 的 UID 路径，不再依赖字符串位数来区分布局。原生 Clip 的 AuthoringId 在绑定创建时生成。
- 嵌入式只读页面的 Track 新增、粘贴、删除和拖动入口按既有 IsReadOnly 禁用。原 Btsmtl binding 已拒绝只读 Mutation；本次修正的是 Slate 仍展示和触发编辑动作的交互入口，不宣称旧页面已经实际改写过资产。
- RuntimeDebugTargetProvider 的执行区间已改为页面调用 ReadExecutionTimeline 时按需归并；普通 Live 刷新只更新状态和 Capture 计数，不生成未显示的执行投影。
- 相关 C# 文件使用本机 Roslyn ParseText 静态检查，启用 UNITY_EDITOR 后未发现语法错误；git diff --check 通过，布局身份及构造调用方已搜索核对。该检查不等同于程序集编译、交互验收或性能实测。
- 本批当时尚未接入执行 Timeline 业务绑定，后续代码接入见下文；共享 Slate 绘制中仍有其它格式化、委托及交互分配未处理，本批不能作为全编辑器 0 GC 或 FPS 恢复证据，4.14、7.5、7.6 保持未完成。

## 执行 Timeline 表面与历史游标代码接入

- 执行过程最初内嵌在 TimelineEditorWindow，现已迁移到独立 Preview 窗口；原 Timeline 始终显示作者表面，没有生成作者 Track/Clip 资产。BtsmtlExecutionTimelineView 直接绑定同一个 RuntimeExecutionTimeline，复用 CutsceneEditorSurface 的时间尺、缩放、Track、Clip、选择和来源打开。
- 正式隐藏 Session 就绪并成功 Attach 到目标 Actor 后自动开始 Capture；不要求用户额外执行准备或先点击记录。显式结束记录后保留 History，不自动重启录制。实际游戏 Session 不由隐藏宿主自动启动记录。
- 节点、等待、状态、决策、Graph/Timeline/TreeClip 调用按正式实例、来源、分支、runtime epoch 与内容版本分轨。新增区间使用按 Capture 容量预建的显示对象，现有区间直接读取同一模型更新后的末端；TimelineLogicTime、TimelineVisualTime、TreeClipUpdated、ClipActive 等采样不另画成重复片段，同来源连续缺少进入记录只保留一个缺口标记。普通作者绑定重建保留执行表面，隐藏或关闭表面释放对象与订阅。
- 逻辑 Tick 与表现帧使用独立时间尺。模型随新事件维护各域游标和分支时钟，不在刷新中扫描全部片段求最大时间。只有有正式进入记录且尚未退出的区间随该分支已记录时钟增长；退出后固定，缺少进入记录的事件显示缺口而不是伪造开放区间。该变化不改作者 Timeline 的 FrameBoundary Clip 长度规则。
- 双击执行片段调用原 RuntimeDebugSourceNavigator，以记录的 SourceMap、revision 和实例定位原 FlowCanvas 或作者 Timeline。选定片段后拖动执行游标，按该片段的 branch/epoch 和时钟域选择已有 Capture Segment；不按数值位置擅自选择另一个分支。历史观察仍使用记录前缀，隐藏预览同时接收正式 Pause 命令；返回实时恢复观察，不把历史位置伪装成已经恢复运行。
- 当前历史游标只更新节点、变量及执行区间。视口仍是当前角色画面，界面明确显示「历史节点与变量；角色画面尚未恢复」。历史角色表现、过去修改后分支续跑、多技能正常返回导航及完整低分配绘制仍未完成；Loop 事实的后续代码接入见下文。
- 为只读执行表面补齐 Slate 的只读交互入口：不允许拖动、缩放、多选改时或通过右键、Track 启用/锁定按钮修改运行事实；单击选择、双击来源定位和视图操作仍可用。
- 静态检查采用 Unity 生成的 csproj 来源、引用、C# 9 与预处理符号，在内存中建立 6 个程序集的 Roslyn 语义模型并调用 GetDiagnostics，不 Emit、不执行 MSBuild、不运行 Unity。最终检查覆盖 318 个源码文件，6 个程序集语义错误均为 0；来源路径按 Windows 绝对路径规范化后去重。此项证明源码接口和引用关系，不证明程序集已经在目标 Editor 加载或交互/FPS 已通过。

## RuntimeDebug 焦点与重新订阅的静态修正

- BtsmtlRuntimeFocusResolver 不再要求非终止节点的最后记录位置等于最新逻辑 Tick。OperationExecutionLifecycleRuntime 在 Stopping 等阶段可以保持运行而没有逐 Tick 记录；现在按最后正式节点状态与 GraphDestroyed 事实判断，等待期间不会仅因时间推进丢失候选。
- Timeline 的完成候选单独读取 TimelineCompleted、TimelineCancelled、TimelineStopped 记录；后续采样不会改变完成导航的事件位置。父调用在子调用之后正式完成时，完成导航返回父路径；历史上较早的已结束子调用不再遮掉较晚的父调用。无活动或并行歧义阶段清除上次导航身份，重新出现唯一焦点时允许重新定位；真实并行调用继续要求 Pin。
- Live State 的重新订阅边界清理停订期间未更新的旧事实；保留其它持续观察的通道。释放 interest 时仍保留最后状态，以支持 FreezeTarget/Ended 冻结；Capture 记录独立保留。该变化避免重新开启观察后把旧运行节点当成当前活动调用。
- Graph 状态选择统一使用正式 Sequence；FlowCanvas 的连接脉冲按事件自己的逻辑或表现域比较位置，不把表现帧与逻辑 Tick 作数值比较。
- Node/Wait 执行区间的归并键增加正式 ActivationGeneration，使重复节点发生与各自终止事实对应。这只补齐节点发生身份，不将该代次冒充 Loop 迭代编号；独立的 Loop 领域事实在后续代码批次接入，见下文。
- 6 个相关程序集、318 个源码文件再次通过 Roslyn 静态语义检查，错误数为 0。没有操作目标 Editor，也没有运行多技能输入、返回导航或 FPS 验收；此前两个 playback 均实际 Running 的证据仍有效，不能将其重述为已确认的诊断漏报。

## Loop 迭代事实与事件级历史游标静态接入

- 共享 OperationCompositeRuntime 的 Loop 使用既有正式 RunnableChildCursor 状态槽保存迭代序号；节点重新激活时按原生命周期重置。开始子节点求值前发布迭代进入，子节点返回非 Running 后发布迭代完成；计数由执行状态维护，不由观察窗口推算，不复用 ActivationGeneration 或 Timeline cycle。
- Fixed 与 Float32 Ability 的 SimulationTraceRecord 携带 LoopIteration，FixedCharacterRuntimeDiagnosticsAdapter 将其写入正式 Graph Trace。Loop 执行区间按实例、节点发生代次与迭代序号区分；父 Loop 正式结束、停止或强制停止时收束仍开放的迭代。选中片段显示迭代序号，决策片段显示记录中的判断结果。Float32 表现 Graph 的诊断开关仍未接通；嵌套迭代与内部调用的完整展开尚未完成，4.13、4.15 不勾选。
- RuntimeDebugSession 改为只保存一个 HistorySequence；原 Segment 菜单和 Inspector 滑条将位置换算为该事件截止点，显示偏移由快照反算。执行区间、节点、变量和执行历史共用同一记录前缀，不把同 Segment 后续事实带到选中事件之前。
- 执行时间线新增「查看进入时」「查看退出时」，消费所选片段的正式 CaptureId 与事件 Sequence；进入事实缺失时不可查看进入，尚未结束时不可查看退出。普通拖动仍按所选 branch、epoch 和逻辑 Tick/表现帧定位，Sequence 不替代时间尺。切到历史后保留选定片段的身份，便于在其进入与退出记录间切换。
- 关闭预览目标后保留当前历史快照和事件位置，不跳回最新记录。节点、变量与开放区间的观察入口已接入，4.16 按代码完成勾选；历史角色画面恢复仍未接通，4.17 保持未完成，UI 继续明确显示缺失状态。
- RuntimeCaptureSnapshot 在构造时建立 Segment 结束位置索引；读取已有快照按 Sequence 二分截取 ReadOnlySpan，不新建事件数组。此项不代表全部 History 重建无分配：区间/状态模型重建和运行 Trace 列表首次扩容仍存在，7.5、7.6 不勾选。新增 Loop 记录使用原 Trace 工作区，未随意添加无法证明的容量倍率。
- 仅做代码与静态检查：按当前 Unity csproj 的引用与符号，在内存中检查 10 个程序集、607 个源码文件，Roslyn 语义错误为 0；没有 Emit、运行 Unity、启动 Preview 或新增测试代码。这不是目标 Editor 编译、历史交互或 FPS/GC 的运行验收。

## 独立 Preview 窗口与状态刷新静态迁移

- 按用户确认的新布局，原 Timeline/FlowCanvas 保留作者区域；BtsmtlScenePlayPreviewWindow 通过 Window/BTSMTL/Preview 或 Timeline 顶部「打开 Preview」打开，重复打开聚焦既有窗口。上下 TwoPaneSplitView 分别承载共享角色视口和现有 Slate 执行时间线，通过 viewDataKey 保存分隔位置，删除固定 260 像素内嵌视口。
- Preview 窗口沿用唯一 BtsmtlScenePlayPreviewHost、GameplayTickSystem、RuntimeDebugSession 和 Renderer。关闭作者 Timeline 只移除本地控件；关闭 Preview 释放视图订阅与隐藏宿主。进入实际 Unity Play 时仍由宿主关闭隐藏场景，窗口关闭不停止实际游戏 Session。窗口开关直接决定 Preview 形态，删除独立保存的模式状态。
- 来源定位直接消费所选执行区间，复用原 RuntimeDebugSourceNavigator；删除 TimelineEditorWindow 的执行面切换、预览视图桥接和执行来源事件，避免新旧两处承载。ThirdPersonClient.Editor 新增对既有 BTSMTL.Timeline.Editor 的显式程序集引用。
- Session 菜单只在控件创建或用户打开菜单时构建，状态刷新不再重复创建菜单项和闭包。RuntimeDebugTargetProvider 的普通刷新只读取 CaptureId/Version，不再为计数复制一遍 Capture 事件；删除无消费者的 CaptureEventCount 与重复版本游标。执行页面继续使用原增量事件读取。
- 黑板写入记录侧栏见后续接入记录；Fixed 已提交黑板快照见后续接入记录，Fixed 正式调值核心与 Preview 编辑控件见下文，2.7、4.18 的代码接入已完成；运行验证未完成；Timeline 自动内容采用的后续实现见下文，普通 Track/Clip 结构刷新和纯 Timeline 定位仍待实现（3.5–3.7）。未用空控件或成功状态占位，未宣称新窗口意味着整个 Workbench 已完成。
- 按用户要求仅做静态检查：10 个程序集、608 个源码文件 Roslyn 语义错误为 0。新窗口来源与 asmdef 引用显式纳入检查，未修改 Unity 生成 csproj，未 Emit、刷新或运行 Unity。布局拖动、实际显示、释放和 FPS/GC 没有本批运行证据。

## Timeline 编辑提交后的自动内容采用

- TimelineEditorWindow 的正式 OnValueChanged、Undo/Redo 和项目内容变化只登记待处理请求；BtsmtlScenePlayTimelineController 在后续 Editor update、隐藏宿主实际 Active 且没有编译或导入时消费。Slate 拖动继续使用原 BeginEdit/CommitEdit 边界，不在 Inspector 绘制或每个拖动采样内直接导出。
- 自动路径仅针对 BtsmtlScenePlayPreviewHost 拥有的 Actor，不自动对实际游戏目标提交内容修改。关闭 Preview、切换 Profile 或 Play 状态时撤销待处理请求。一次处理复用正式 TryExportContent、TryPrepareContentAdoption、TryPublishContentAdoption 和 TryAdoptContent；作者版本与已采用版本相同时不重复安装。
- 成功后明确提示新调用采用新版本、活动 playback 保持原内容；失败使用正式错误与拒绝状态，不假装已经更新画面。高级菜单仍能观察各阶段，但常规兼容内容修改无需手动点四次。删除 OnContentChanged 中同步计算 revision 的旧路径和无调用方 ResolveCurrentAuthoringRevision。
- 3.7 仍未整体完成：正式内容层目前拒绝 Track/Clip 拓扑变化；按 playback 内容版本生成的来源映射已在后续代码批次接入，运行资源和绑定更新仍未接通。未删除拒绝条件来掩盖这些缺口，也没有把图 Build 或整个 Session 重建作为自动兜底。纯 Timeline 当前游标效果定位仍待 3.5、3.6 接入。
- 本批仅执行源码静态检查，10 个程序集、608 个源码文件语义错误为 0；未刷新或运行 Unity，未新增测试代码，没有内容采用、布局或 FPS 的新运行证据。

## playback 内容版本与来源映射静态接入

- CharacterTimelineHost 为每个 playback 保存对应的来源映射；正常创建和正式恢复均从准备结果取得内容身份、RootFingerprint 与 ContentHash。同一内容复用映射，不再重复遍历 Timeline 计算指纹；首次映射填充复用已计算指纹。图调用仍使用角色原有映射，Timeline 事件显式携带本次 playback 的映射版本，不改变默认图来源表。
- RuntimeDiagnosticsContext 保存正式映射目录；RuntimeDebugSession 在消费新事件或冻结 Capture 前采集新增映射，RuntimeDebugTargetProvider 按事件 ContentRevision 解析来源。直接增量读取执行时间线也先同步映射，避免页面刷新早于普通观察刷新时漏掉新来源。
- RuntimeDebugSourceNavigator 删除把所有事件版本等同角色总版本的限制，改为在同一目标与 Session 中查找记录对应的映射，再核对来源。RuntimeDebugViewModel 显式区分 Live 与 History，实时来源缺失仍显示错误，不因传入指定映射就被视作历史缺口；删除不再使用的默认映射 Apply 入口与冻结对象重复 revision 字段。
- 固定输入 Trace 的 source-map.json 改为 runtime-debug-source-map-catalog/1 映射目录；整份证据 schema 更新为 character-fixed-input-runtime-trace-evidence/3。仓库内未发现依赖旧单映射结构的读取方，未保留双格式兼容出口。导出只在文件生成阶段物化目录，不进入逐帧链路。
- 该批没有解除内容层的结构更新限制：新增动画 Track 依赖 producer/source-owner 绑定；新增动画资源依赖 Pose 资源目录；TreeClip 编译调用依赖正式调用绑定。活动 playback 所用资源仍需保留，不能直接清空旧绑定。3.7 保持未完成。
- 本批最终静态检查覆盖 11 个程序集、699 个源码文件，Roslyn 语义错误为 0；diff 与 OpenSpec strict 校验通过。未 Emit、刷新或运行 Unity，未新增测试；不据此宣称导航交互、内容采用、FPS/GC 或端到端验收通过。

## 黑板正式写入事实与采集开关

- Fixed/Float32 的普通 BlackboardSet 继续调用原 BlackboardRuntime.Write，沿用 Config 只读、scope owner、Materialize、写入戳与业务投射规则；本批在成功写入后记录值类型的 SimulationBlackboardTrace，携带值槽、实际值与正式 BlackboardOwnerToken。预测求值不记录；事实跟随原候选 Trace，只在 Fixed 已提交结果中发布 BlackboardWritten，不直接从编辑器读取或修改运行状态。
- FixedCharacterRuntimeDiagnosticsAdapter 在装配时绑定值槽的声明来源，发布时沿用正式角色、技能、Action、Graph invocation 与代次。运行事实明确包含 BlackboardStateSlot、BlackboardScope、BlackboardOwnerIndex 和 BlackboardOwnerGeneration；Capture payload 比较与证据哈希同步覆盖这些字段，不把同值但不同作用域代次的写入合并。
- Blackboard interest 通过既有 diagnostics aggregate、EvaluatePass、CharacterEvaluationRuntime、Invocation、OperationControl 和 TraceSink 传递；只看 Graph 或端口值而未订阅 Blackboard 时不生成黑板写入记录。事件使用值类型，不为每次写入新建对象或格式化字符串；既有 Trace List 首次扩容及整体 Trace 大小仍不能据静态检查宣称全链路零分配或性能已达标。
- 执行时间线把该声明事件显示为瞬时记录；选中后显示记录值、采用 Tick 和作用域代次，双击可以进入声明所在图。正式 History 前缀自然包含此前已经提交的写入，不读取未来值或当前作者默认值。
- 本批接通普通 BlackboardSet 的事实；当前快照、侧栏和 Fixed 调值核心分别见下文。Float32 表现图提交诊断代码已在后续批次接入，运行验证仍未完成；Fixed Preview 调值交互见后续接入，2.7、4.18 的代码接入已完成。
- 扩展静态检查包含 ThirdPersonSimulation.Unity，覆盖 12 个程序集、728 个源码文件，Roslyn 语义错误为 0；未 Emit、刷新或运行 Unity，未新增测试，没有本批黑板交互或 FPS/GC 实测。

## Preview 黑板记录侧栏

- 独立 Preview 上半区增加横向 TwoPaneSplitView：左侧仍是共享角色视口，右侧为 BtsmtlScenePlayBlackboardView，分隔位置通过既有 UIElements viewDataKey 保存。没有新增 Session、时钟、状态副本或变量覆盖层。
- 侧栏读取 RuntimeDebugSession 的 BlackboardWritten 事实，显示截至当前实时/历史位置的最后写入值及作用域代次；可按实际 RuntimeInstanceKey 筛选技能释放与图调用，点击声明打开原图。当前侧栏已由后续快照接入改为“黑板”，支持已提交值和生命周期有效性；Fixed 调值控件见后续接入。
- 列表使用 ListView 虚拟化；RuntimeDebugChangeSet 按来源种类判断是否需要读取，普通节点变化不会刷新变量列表。已有实例选项顺序保持稳定，只有调用集合增减才更新下拉选项；切到历史、目标结束或模型替换时同步状态，关闭视图释放 Blackboard interest 和 Changed 订阅。
- 2.7、4.18 的 Fixed 当前值快照、正式调值核心及编辑控件代码已接通，运行验证尚未完成。UI 不能直接写状态槽。
- 新视图显式纳入静态检查，Unity 生成 csproj 已包含它时按规范化绝对路径去重。本批最终静态检查覆盖 12 个程序集、729 个源码文件，Roslyn 语义错误为 0；没有 Emit、运行 Unity 或界面验收，不以控件代码存在证明可用性或 FPS。

## Fixed 当前黑板快照与历史值接入

- FixedGameplayAbilityExecutionInstallation 新增正式只读快照入口；FixedBlackboardSnapshotReader 从已提交 Character/Ability 状态及真实技能执行帧读取值，复用正式编译默认值。Character、Graph、State、ActionInstance、Frame 按其已有 owner 与生命周期判断有效性；Config 显示为只读。状态机已选中但尚未开始执行的新 State 不会提前成为有效变量实例。读取不会 Materialize、创建执行帧或改写状态。
- FixedCharacterRuntimeDiagnosticsAdapter 在提交结果的原入口读取快照；无 Blackboard interest 时不读取。它只发布值、owner、技能代次或有效性变化，已释放执行帧发布一次未激活状态。诊断存储显式提供 LiveStateGeneration，清空或重新订阅后重发完整快照；新 Capture 也补初始快照，避免仅录到后续变化。
- 新 BlackboardSnapshot 记录携带值槽、作用域、owner、lifetime 和有效性。RuntimeLiveStateKey 同时包含黑板技能标识与值槽，防止复用同一声明来源的不同编译槽相互覆盖；Live 与 History 使用同一个事件键构造入口，payload 比较与证据哈希同步覆盖新增字段。
- Preview 侧栏合并快照与正式 BlackboardWritten，按技能、释放实例和值槽采用已记录的最后事件。历史游标位于一次写入之后、Tick 末快照之前时，立即显示该次写入；Character 作用域合并到角色实例，不按写入它的技能调用复制变量。侧栏显示未激活及 Config 只读状态，仍通过来源映射定位声明。
- BlackboardSnapshot 属于观察采样，不生成执行 Timeline Clip；原写入事件继续显示为瞬时记录。列表保持虚拟化与按来源变化刷新，未新增逐帧作者扫描或编译。
- 边界：快照从首个已提交 Tick 开始；尚未创建的技能实例不伪造运行值。Float32 表现图的帧内参数通过下述只读端口采样观察，不属于 Gameplay 黑板快照，也不拥有 Gameplay 黑板可写生命周期。Fixed 写入核心及编辑控件见下文，没有宣称通过运行验收。
- 12 个程序集、730 个源码文件通过 Roslyn 静态语义检查，错误数为 0。未 Emit、刷新或运行 Unity，未新增测试；静态检查不能证明实际 UI、生命周期回看、FPS 或 GC 已通过。

## 黑板声明绑定与统一写入元数据

- 编译器为每个黑板值槽输出无 SourceOperation 的正式 StateSlot 引用，ExternalIdentity 指向 BlackboardDeclaration；同一声明在不同图调用路由的值槽分别绑定。不再依赖图中是否存在 BlackboardSet，也不解析作者路径或读取诊断 SourceMap 来推算业务元数据。
- ProgramCatalogRuntimeIndex 在运行服务装配时绑定声明，缺失、重复或无法解析的绑定在该边界明确失败。Fixed/Float32 的原 BlackboardRuntime 均从所属槽组读取声明，用于既有 ActionWindow 投射与 Action Context 解析；普通节点写入继续沿用原 owner、生命周期、写入戳和提交规则。
- 已删除 BlackboardGet/Set 的旧节点到声明 CatalogEntry 引用及运行时查询入口。节点仍通过 StateSlot 引用定位变量，投射直接复用已经定位的槽组；本批没有增加备用映射或每次写入的分配。
- 迁移要求：已有编译图必须重新 Build，旧产物缺少新绑定时显示明确错误，不回退到 Setter 引用。原 ProgramReference 编解码格式不变，生成的引用参与原有内容版本计算。本批未操作 Unity 或重新生成资产。
- 本批完成外部调值所需的正式声明绑定；Fixed 命令队列、求值边界、实例过期结果与编辑控件见下文，2.7、4.18 的代码接入已完成。
- 本批最终 Roslyn 静态语义检查覆盖 12 个程序集、730 个源码文件，错误数为 0；限定文件 diff 检查与 OpenSpec strict 校验通过。未 Emit、刷新或运行 Unity，未新增测试，不将绑定代码存在表述为运行调值可用。

## Fixed 黑板调值的正式求值与提交

- FixedCharacterRuntime 提供 QueueBlackboardWrite / TryTakeBlackboardWriteResult，命令携带完整技能编译身份、释放实例、技能代次、变量槽、作用域 owner 和强类型值。接收边界确认版本、类型、Config 只读及作用域地址，不从诊断 snapshot 直接修改状态。
- FixedAbilityInvocationRuntime 保存请求，在正式 Forward 求值中、原输入绑定之后和技能 Tick 之前交给 FixedAbilityDomainRuntime。领域模块只进入已存在且仍有效的对应技能实例，FixedBlackboardRuntime 检查当前 Graph/State/Action owner；已经结束或更换代次的目标返回 InstanceEnded、ScopeEnded 或 ScopeChanged，不改写后继实例。Frame 值在下一次实际求值 Tick 采用，之后仍按原 Frame 生命周期失效，没有永久覆盖层。
- 普通节点写入与命令写入共用 StoreValue、写入戳和 ActionWindow 投射。命令使用正式 BlackboardCommand 来源与请求序号，不伪造 Setter 节点或 Graph invocation。Fixed 诊断发布按真实角色/ActionInstance 与变量声明显示采用事件；节点写入继续保留原 Graph invocation 来源。
- 请求候选结果在外层事务开始时重置，只在 Character/World 原子状态发布后移出队列并成为可读取结果。角色求值结束、世界求解失败或丢弃候选都不能提前确认成功；同一外层事务的后续 Forward Step 不重复采用已处理请求。Replay Step 不重新消费调值请求，未新增调参重放或历史分叉协议。
- BlackboardWriteStamp 增加 CommandSequence，Fixed/Float32 状态格式版本由 12 升为 13，槽 codec 标识改为 state.blackboard-write-stamp/v2；EventId 及来源编解码同步表达命令身份。旧编译图和旧格式 Snapshot 需要按正式 Build/新 Session 边界重新生成，不提供兼容读取。
- Preview 编辑控件接入见下文。Float32 表现图只支持帧内宏参数，不作为 Gameplay 黑板外部调值目标；其只读执行诊断已在下述表现候选提交链接入，未取得运行证据。最终 Roslyn 静态检查覆盖 12 个程序集、731 个源码文件，错误数为 0；限定文件 diff 检查与 OpenSpec strict 校验通过。未操作 Unity，未新增测试，未验证实际调值、FPS 或 GC。

## Preview 黑板调值编辑控件

- 黑板变量行增加“改值”入口，下方 BtsmtlScenePlayBlackboardValueEditor 持有本次选择的草稿。变量列表按技能、释放实例与编译槽稳定排序，不再按最新事件排序，持续写入不会使同一变量跳行；虚拟行复用与实时快照刷新不替换已选草稿或目标。
- FixedCharacterRegistration 从现有诊断快照缓存公开精确只读值，编辑器据此初始化布尔、Int32、UInt64、Fixed Scalar、Vector2/3、Yaw、Identity 与 ActionTargetSnapshot 控件。Fixed 数值以原 Raw / OneRaw 的 decimal 文本往返，不使用侧栏缩短显示或 Unity float 向量生成修改值。解析在用户提交边界完成，非法输入直接说明原因。
- 只有当前隐藏 Preview、当前内容版本、实时观察及可写活动变量能提交；Config、历史、其他运行目标和失效作用域保持只读。编辑器把明确实例与原 owner 的强类型命令交给 FixedCharacterRuntime，不直接写诊断记录或状态槽，也不创建 Session/执行器。
- 提交先显示“尚未采用”，随后读取正式事务发布结果，显示实际采用 Tick 或实例结束、作用域结束、作用域换代原因。暂停时命令等待播放或单步，不自行推进 Tick。返回的已采用值更新编辑字段；之后节点继续按原规则读写，侧栏显示其正式最新快照。
- 仅有待处理请求时订阅 EditorApplication.update 读取结果；收到结果、目标结束及视图卸载时撤销轮询。预览结束释放注册目标引用；历史与目标切换禁用旧草稿提交。控件不在刷新中扫描资产、Build 图或求值。
- 仍未完成：表现图观察的实际运行、Workbench 其余未勾选能力及真实运行验收。2.7、4.18 按 Fixed 正式黑板的代码接入完成标记，不把这两个实施项完成当作完整 Workbench 可用。本批最终 Roslyn 静态检查覆盖 12 个程序集、732 个源码文件，错误数为 0；限定文件 diff 检查与 OpenSpec strict 校验通过。未启动 Unity、未新增测试，没有真实交互与 FPS/GC 验证。

## Float32 表现图边界与求值开销

- 实际入口为 CharacterTimelinePresentationGraphRuntime → Float32PresentationGraphRuntime。每次 Marker / TreeClip hook 求值从默认状态开始，仅允许已编译宏参数读写；ParameterSlot 明确拒绝访问 Gameplay 黑板，且一次调用必须完成。它不是 Float32CharacterRuntime 的角色黑板实例，不能套用 Fixed 调值命令或新增持续覆盖生命周期。
- 因此修正此前“Float32 外部调值尚未接入”的笼统描述：当前 Workbench 应记录该表现调用的节点、分支和帧内参数供只读查看。该诊断代码接入见下文；表现参数不因此成为 Gameplay 可调变量。
- 相机输出节点的唯一来源在准备阶段解析并绑定，CreateOutputIdentity 不再每次扫描整个 SourceMap。来源缺失或重复仍在正式准备边界报错。每次求值只重置已准备宏参数及可达表现操作的状态槽，不再复制整份技能状态数组；不保留上一次表现调用的业务状态。
- 删除 PrepareOperation / PrepareEdges 中始终为零、且结果没有消费者的 capacity 数组与累计值，保留原递归检测、可执行能力校验和初始化遍历。
- 静态检查曾因引用旧 BTSMTL.Timeline.Runtime DLL，报告 InstallContent 与 Prepare 签名不匹配；当前源码已经具有所需接口。检查器现同时解析 Timeline、Timeline.Tree、Timeline.Runtime 的当前源码，15 个程序集、777 个源码文件通过 Roslyn 语义检查，错误数为 0。这不是 Unity 编译或运行证据，也不能证明 FPS/GC 已达标。

## 表现图的正式 SourceMap 注册

- AbilityDebugSourceMapFiller 从 Fixed Unity 实现层迁入共享客户端 Diagnostics，保留脚本 GUID。Fixed Character、Rollback Character 与 Fixed 诊断适配器同步使用共享实现；删除旧位置，不保留转发类、别名或第二套填充逻辑。
- CharacterTimelinePresentationGraphRuntime 在安装每份实际包含 Presentation Marker / TreeClip 的编译程序时，用同一填充器生成只读 SourceMap。映射按 float32-presentation/AbilityId 与编译 ContentHash 标识，保留正式节点、端口、边、声明与图调用来源，不用当前作者资产补推。
- CharacterTimelineHost 绑定 RuntimeDiagnosticsContext 时注册这些表现映射；它们进入既有 SourceMap catalog，可随记录版本交给 Live/Capture/History。执行程序与对应映射保存在同一安装项中，不合并到 Fixed 角色版本或 Timeline 作者版本。
- 此批完成映射归属及注册；Float32 表现图节点/分支/值事实的采集与提交见下述接入记录，单凭映射注册不能证明表现图高亮或历史导航可用。
- 静态检查同步迁移源文件位置并纳入 Rollback Unity 调用方，16 个程序集、784 个源码文件通过 Roslyn 语义检查，错误数为 0。未运行 Unity，未新增测试。

## 表现图节点、连线和值的候选提交诊断

- Float32PresentationGraphRuntime 的正式输出接口提供节点生命周期、作者连线和类型值回调。观察 interest 在每次求值入口确定；没有节点和值 interest 时不执行采集回调。输入端口名与默认单输出端口在准备阶段绑定，不在高频链路生成端口合同或格式化字符串。帧内参数继续按原单次求值合同重置，没有 Gameplay 黑板调值扩展。
- CharacterTimelinePresentationGraphDiagnostics 在安装时按本次编译 SourceMap 建立各 hook 的来源与父子调用表。实际 owner 的进入回调为表现调用分配发生代次，节点激活代次沿用执行游标；重复帧、不同 playback、cycle、hook 和嵌套调用保持独立。根调用的父代次取正式 provenance.SourceActivationGeneration：它由 Fixed/Float32 CreateTimelineInvocationSource 从 ReadInvocationGeneration 提供，不是从作者资产猜测。
- 表现执行先暂存值类型记录，缓冲在诊断装配时按现有 LiveStateCapacity 一次分配。回调没有列表扩容、业务对象副本或逐事件字符串生成；达到容量保留明确 TraceSamplingLimited、丢失数量和已有记录，不让观察改变 Gameplay 执行。FlowCanvas 对当前调用显示记录不完整，执行区间和历史完整性也包含该缺口。
- 发布入口在 CharacterPresentationDomainRuntime 的 Pose、Timeline、Action bridge、表现时钟、相机及表现桥正式提交完成之后。仅此边界把表现图记录交给 RuntimeDiagnosticsContext 的 Presentation 域；早退或异常沿原 finally 调用 CharacterTimelineHost.DiscardPresentationFrame 清空候选。关闭宿主清空候选并释放诊断 Context 与预分配缓冲引用。不能在图 Evaluate 或 Timeline 局部提交时提前写入 Capture。
- RuntimeDebug 继续按事件 ContentRevision 解析既有映射目录。端口值、节点无作者输出端口的结果和容量缺口明确作为瞬时采样，不再因来源是 Node/Graph 就生成错误的持续区间；真实编译宏入口的生命周期按 Graph 事件展示。
- TimelinePlaybackActionContext 从正式 TimelineActionContextIdentity 保留真实 SkillId，正常创建与恢复同步迁移；ActionId 继续表达动作，不能从它反构技能身份。Motion/MotionWarp 输出与表现程序选择使用原 SkillId，不新增另一份来源、旧构造重载或从动作名称推断的路径。这项修改不改变 Snapshot 格式，原 Snapshot 已保存完整技能身份。程序选择同时使用正式技能标识，避免不同技能复用表现来源时误匹配。运行调用关联与 History 选择对已携带 playback 的表现图按 playback/cycle 限定，不因共用 RuntimeId 或表现帧就带入另一个 playback；FlowCanvas 的节点、连线和端口位置明确显示表现帧或 Tick。
- FlowCanvas 值观察改用复用事件缓冲，并按真实 Sequence 取当前端口最新采样；删除旧 GetCurrentEvents 分配入口。角色 Inspector 的所有调用方同步迁移，缓存归该 Inspector 实例所有，按 ViewModel/Revision/Channel 刷新；筛选集合与临时列表复用，关闭或关闭观察后释放引用。没有保留新旧读取路径，仍未据此宣称 IMGUI 绘制或整个编辑器已零分配。
- 本批最终静态检查覆盖 16 个程序集、785 个源码文件，Roslyn 语义错误为 0。第一次检查碰到并行 Float32 事务类型修改的两个中间错误，未改动该任务文件；当前源码修正后重新检查通过。未 Emit、刷新或运行 Unity，未新增测试；没有节点高亮、History 导航或 FPS/GC 的运行证明。4.13、4.14、5.2 及编辑器整体性能项继续保持未完成。

## 先前验证记录（不代表当前编译状态）

- 已执行 `git diff --check`，通过。
- 已搜索旧的运行 Timeline 替换入口，没有发现调用方残留。
- 已使用目标 Unity 实例 `e852139597e42532` 打开原 Timeline 窗口并通过 Workbench Preview 入口提交一次正式 ScenePlay 启动；Editor 日志记录 `All compiler errors have to be fixed before you can enter playmode`，因此本轮没有真正进入 Play，也没有取得角色、技能、FlowCanvas/Slate 导航、Timeline 游标或动态 TreeClip History 的运行事实。
- 未执行 batchmode 或端到端回放；不能把本轮 Launcher 请求和 Editor 菜单成功误认为 Preview、FlowCanvas/Slate 自动导航、Timeline 游标或具备 TreeDecision 的动态 TreeClip 历史已经在运行时可用。
- `dotnet build --no-restore --disable-build-servers /nr:false /p:UseSharedCompilation=false` 受 Unity 生成的 `Temp/obj/.../project.assets.json` 缺失影响失败；随后已执行 `dotnet build-server shutdown`。该结果不能作为源码编译通过证据。

用户端到端验证前，任务清单中的未完成项保持未勾选；不归档本 change，也不修改 `openspec/changes/archive/` 历史文档。
