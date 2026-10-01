# Editor CPU 热点直接读取

本轮按用户纠正，编辑器允许必要的 GC，优先处理大的计算热点；Player 运行期 0 GC 目标继续保留。没有构建 Player、切换 Play 或保存场景，没有新增或修改测试代码。角色哈希修正由目标 Editor 的现有脚本导入和编译流程加载。

目标实例为 `e852139597e42532`，返回项目路径为 `D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets`，Profiler 连接标识为 `Editor`，Deep Profile 关闭。先读取并保存停止录制后仍保留的 18778–19077 共 300 帧。读取过程中目标 Editor 自行进入、退出 Play；每次返回的状态单独记录，未将读取时的 attachment 或 Graph 状态追认为全部历史帧的状态。

原始帧：[18778–19077](../../.performance-build/reports/20261001-editor-retained-18778-19077.raw)。全部主线程统计：[分析 JSON](../../.performance-build/reports/20261001-editor-retained-18778-19077-analysis.json)。300 帧中有 299 帧包含角色 Update/LateUpdate；第 18864 帧为 MCP 请求本身，耗时 1723.71 ms。再排除含恢复 JIT 的 18865 帧和关闭 Profiler 窗口的 19077 帧后，297 个常规运行帧的中位耗时为 16.89 ms、P95 为 34.98 ms，22 帧超过 33.33 ms。这是同一记录中不同输入、窗口状态的分布，不是修改前后 A/B。

第 18812 帧主线程为 41.66 ms，Update 阶段为 16.82 ms、LateUpdate 阶段为 14.58 ms、GameView.Paint 为 5.30 ms。`MainBehaviour.Update` 本体为 16.76 ms，但该记录没有内部 CPU 标记，暂不能将其归因到哈希、技能、Timeline 或某个具体函数。LateUpdate 中的 Pose 阶段可以继续拆分：

| 阶段 | 两次调用合计 |
| --- | ---: |
| CharacterPose.EvaluateNodes | 7.96 ms |
| CharacterPose.FootPlacement | 3.33 ms |
| CharacterPose.FullBodyIK | 2.06 ms |
| CharacterPose.PrepareSources | 2.84 ms |
| CharacterPose.SampleFootFeatures | 1.63 ms |
| CharacterPose.EvaluateAnimation | 1.06 ms |
| CharacterPose.ComponentToLocal | 0.68 ms |
| CharacterPose.LocalToComponent | 0.41 ms |
| CharacterPose.Commit | 0.81 ms |
| CharacterPose.ValidatePending | 0.096 ms |

FootPlacement、FullBodyIK 和空间转换是 EvaluateNodes 的子项；SampleFootFeatures 是 PrepareSources 的子项，不能和父阶段重复相加。两次 FootPlacement 一共执行 276 次 RaycastNonAlloc、8 次 SphereCastNonAlloc，原生检测合计约 0.386 ms，FootPlacement 的主要耗时仍在托管计算。[该帧的 Pose 调用树](../../.performance-build/reports/20261001-editor-frame-18812-pose-tree.json)保存了具体子调用和次数。

历史帧没有记录分配调用栈，GetSampleCallstack 返回空列表。后续短时开启调用栈后读取的 35709–36008 帧没有角色 Update/LateUpdate，读取时 `isPlaying=false`、RuntimeDebug 为 Detached、Canvas 没有具体 Graph；它不能用于运行卡顿或桥接开启后的性能结论。[原始帧](../../.performance-build/reports/20261001-editor-current-35709-36008.raw)及[统计](../../.performance-build/reports/20261001-editor-current-35709-36008-analysis.json)保留这一失败采样。采集结束已关闭本轮开启的两个 Profiler 录制开关、CPU area 和分配调用栈。

随后目标 Editor 重新进入 Play，直接采集 `GameplayLabFixed` 的 3349–3648 共 300 帧，Deep Profile 和分配调用栈关闭。读取时仍在 Play，RuntimeDebug 为 Detached，Canvas 没有具体 Graph。CPU 主线程耗时中位数为 17.27 ms、P95 为 26.22 ms；LateUpdate 平均 9.87 ms，FootPlacement 平均 2.70 ms、其中独占 2.56 ms，FullBodyIK 平均 1.34 ms，PrepareSources 平均 1.77 ms、其中 SampleFootFeatures 为 0.88 ms，GameView.Paint 平均 3.97 ms。MainBehaviour.Update 平均 1.28 ms、单次最大 8.37 ms。当前可细分的持续计算热点首先是 FootPlacement，其次是 FullBodyIK 和来源准备；输入不同，不能把旧、新采集的数值差异解释为优化收益。

第 3640 帧主线程达到 132.44 ms，其中 Application.Message 为 100.87 ms；该帧包含 GameView 的多个 KeyDown/Character 事件，原因仍需进一步定位，不能直接归因为 GC 或角色算法。原始帧为 [当前 Play CPU](../../.performance-build/reports/20261001-editor-current-3349-3648.raw)，完整统计为 [当前 Play 分析](../../.performance-build/reports/20261001-editor-current-3349-3648-analysis.json)。再通过 FrameDataView.frameStartTimeMs 读取 297 个相邻帧间隔，中位数为 17.275 ms、P95 为 26.224 ms，记录见 [帧开始间隔](../../.performance-build/reports/20261001-editor-current-3349-3648-frame-intervals.json)。这段输入和窗口状态未复现持续低于 30 FPS，仍有超过 100 ms 的异常帧；它不能用于证明 Graph/Timeline 开启后的表现。

源码核对还发现角色哈希 writer 在整个 roster 开始时仅清空一次：第二个角色会连同第一个角色的完整编码一起求哈希，结果与独立状态快照的角色哈希不一致。对于编码长度相同的 N 个角色，哈希扫描量由 N 份编码变成 1+2+…+N 份。当前 Fixed / Float32 的 ComputeHash(state, writer) 在生成单个角色哈希的入口清空 writer，Finalize 删除批次级清空；复用同一缓冲区，不新建 writer。修改前确认目标 Editor 非 Play、非编译；修改后目标 Editor 已编译并加载两条 Reset → WriteString → WriteCanonical → ComputeHash 调用链，scriptCompilationFailed=false。没有新增测试代码，没有进行运行同输入 A/B，实际耗时收益未验证。

FootPlacement、FullBodyIK 和来源准备的算法尚未修改，不能声称 FPS 已改善。下一步处理这些大的计算热点，并细分 MainBehaviour.Update 的真实运行调用及 Application.Message 的异常帧；这些改动应由对应 Editor CPU 阶段实测确认。完整 Player 0 GC 也未证明。

后续在原调用边界加入 CPU 子标记。FootPlacement 拆为 CurrentSupport、LandingPrediction、GroundPath、BodyTrajectory、Lifecycle、Completion、SoleSupport，统一前缀为 `CharacterPose.FootPlacement.`。FullBodyIK 拆为 BindPose、ApplyGoals、Solve、Diagnostics，统一前缀为 `CharacterPose.FullBodyIK.`；其中 Diagnostics 只包围实际开启诊断后的数据生成。`MainBehaviour.Update → GameplayTickBootstrap.FrameUpdate` 拆为 `GameplayTick.FrameUpdate`、`GameplayTick.Hotkeys`、`GameplayTick.Input`、`GameplayTick.Logic`，Logic 按实际逻辑 Tick 调用计数，用于区分单 Tick 计算和一帧推进多个 Tick。标记直接读取 Editor Profiler，沿已有静态 ProfilerMarker 模式实现，没有新增逐帧日志、闭包、结果缓存或执行路径，没有改变支撑查询、状态推进、曲线采样与 IK 算法。

本次核对同一目标实例，项目路径正确、非 Play、非编译、scriptCompilationFailed=false，保留帧仍是 3351–3650，两个录制开关均关闭。该帧集早于新增子标记，没有重读旧帧制造新结论，没有擅自进入 Play、构建 Player 或新增测试代码。本次持有和释放一次 AssetDatabase 自动刷新禁用，计数已配对；向目标 Editor 请求一次脚本编译后，确认编译完成、scriptCompilationFailed=false、Console 错误查询为空，六个类型的十五个静态 ProfilerMarker 字段均已加载。新增标记的实际运行帧、各子阶段排序和性能改善仍未验证；需要下一轮实际 Play 采样，尤其是具体 Graph/Timeline 活跃及自动来源切换时的帧。

## RuntimeDebug 静态检查与计算清理

用户随后将当前范围收敛为 RuntimeDebug，并要求先静态检测；之后已有运行时直接读取 Profiler。本轮没有启动 Play、回放、新 Profiler 录制或 Player 构建，没有新增或修改测试代码。前文的 FootPlacement 和 FullBodyIK 数据保留为历史证据，不用于判断 RuntimeDebug 开启后的耗时。

运行事件由 `RuntimeDiagnosticsStore.CopyLiveStateSince` 提供，`RuntimeDebugTargetProvider.Refresh` 将增量提交给现有 `RuntimeDebugViewModel`；Workbench 焦点、Graph 观察和执行 Timeline 消费同一模型。本轮清理两处已确认的重复计算：

- `BtsmtlRuntimeFocusResolver.Refresh` 原先筛选节点事件时调用一次 `RuntimeNodeExecutionObservation.TryCreate`，遍历最新节点时再转换一次。现在节点索引保存第一次得到的 observation，后续直接消费其 Event 和终止状态；最新事件的 Sequence 比较、销毁排除、父子调用关系和完成位置选择保持原语义。
- `RuntimeDebugViewBinding.Refresh` 原先在 Following、Pinned 和 None 三种模式下都复制并排序全部实例，Pinned 随后只线性查询所选实例。现在只在 Following 中执行原来的实例列表投影；Pinned Graph 查询 ViewModel 的现有 Graph 实例索引，Pinned Timeline 使用现有 `TryGetTimelinePlaybackSummary` 及相同的 Timeline/来源 Graph 匹配。没有增加另一份缓存或索引，实例消失时仍显示 PinnedInstanceMissing，多实例 Following 的选择规则不变。

在原调用边界加入七个静态 CPU 标记，用于后续读取实际运行帧：

| 标记 | 包围的处理 |
| --- | --- |
| RuntimeDebug.LiveSync | Provider 的一次实时同步 |
| RuntimeDebug.LiveRead | Store 的增量读取 |
| RuntimeDebug.LiveApply | 发生变化后的 ViewModel 更新与提交 |
| RuntimeDebug.ResolveFocus | 模型版本变化后的焦点候选解析 |
| RuntimeDebug.GraphObservation | Graph 脏数据的节点、边与值投影 |
| RuntimeDebug.ExecutionProjection | 版本变化后的执行 Timeline 投影 |
| RuntimeDebug.ExecutionDraw | Slate 执行 Timeline 绘制 |

LiveRead 和 LiveApply 是 LiveSync 的子项，不能与父项重复相加。标记没有改变同步频率、订阅、采集模式、历史存储或重绘间隔，没有加入逐帧日志、闭包或额外结果集合。

四个标记文件与节点转换修改经 `ThirdPersonClient.Editor.csproj` 编译，90 个警告、0 个错误，见 [Editor 编译日志](../../.performance-build/reports/20261001-runtime-debug-profiler-editor-build.log)；固定实例查询修改经 `BTSMTL.Diagnostics.Editor.csproj` 编译，2 个 Unity Test Framework 警告、0 个错误，见 [绑定编译日志](../../.performance-build/reports/20261001-runtime-debug-pinned-binding-build.log)。两次构建均使用 `--disable-build-servers /nr:false /p:UseSharedCompilation=false`，结束后执行 build-server shutdown。六个源文件的 `git diff --check` 通过。

同一目标 Editor 曾在非 Play、非编译且 scriptCompilationFailed=false 时确认七个 CPU 标记已加载。随后自动导入经历一次编译和域重载；最终再次核对项目路径正确、playing=false、compiling=false、updating=false、scriptCompilationFailed=false，并通过只读反射确认 `ContainsGraphInstance` 已加载。没有主动刷新或请求新的编译。

最终 Console 查询保留两条 `timeline_dependency_unresolved` 与一条 `actor_roster_missing` 启动错误，没有清空 Console。本轮未启动运行，不能将这些保留消息当成本轮重现，也没有扩大范围修改资源装配或角色注册。

这些结果证明源码改动可编译，尚未证明实际 FPS、毫秒耗时或 RuntimeDebug 全链路 0 GC。焦点解析仍按 ViewModel Revision 扫描当前 Graph/Timeline 事件，Graph 投影和 Slate 绘制的实际成本也仍需新标记的运行帧判断。后续已有运行时直接读取对应帧，当前不追加启动和采集。

## TreeClip Graph 编辑性能静态清理

用户澄清卡顿发生在 TreeClip 打开的 Graph 内调整节点。正式打开链为 `TimelineEditorWindow.TreeClipOpenRequested → BtsmtlSkillTimelineTreeClipEditorEntryPoint.Open → GraphEditor.OpenWindow`，编辑内容是原有 `BtsmtlSkillFlowGraph`。本轮检查 Graph 画布、节点 Inspector、技能变更事务及图序列化；没有修改 TreeClip 播放逻辑或 Timeline 运行观察。

确认并清理了以下重复工作：

- 状态机、状态内容和 Timeline 节点的 Inspector 原先调用通用 ReadGraphReferences／ReadReferences，构造全部引用的投影数组，再查找一个字段。现在直接读取这些节点既有的 StateMachine、Body、TimelineAsset 属性。通用导出接口继续服务原有导出与编译消费者；Inspector 的两个私有投影包装及全部调用已删除。
- 顺序、选择、并行步骤 Inspector 原先每次绘制把节点的 Steps 投影成 BtsmtlSkillStepAuthoringValue 数组。现在直接消费节点现有的只读 Steps，步骤修改仍在原事务中创建替换列表并交给 SetSteps；并行完成方式直接读取原有 Mode。
- `BtsmtlSkillFlowEditorMutation.Apply → Graph.SelfSerialize → Graph.UpdateNodeIDs → AssignNodeID` 原先为每次节点或边遍历调用 parsed.Contains，线性扫描整份节点数组。现在用本次已编号节点的 ID 与 parsed 对应位置的引用判断是否已访问，复用原有数组，不增加集合或缓存。节点优先级排序、深度优先访问顺序、最终列表和 UID 规则保持原算法；这里只改变访问判断的成本。该共享方法也用于其它 NodeCanvas 图。

节点编号的访问判断由每次 O(V) 降为 O(1)，此部分整体由 O(V×(V+E)) 降为 O(V+E)；原有排序仍为 O(V log V)。这是源码复杂度结论，不是实际帧耗时或拖动流畅度的 A/B 结果。变更事务仍执行原有 Undo、闭包校验、私有资源处理、序列化和脏标记，没有跳过输入检查、延后提交或新增执行路径。

新增五个静态 CPU 标记：GraphEditor.Canvas 包围画布 OnGUI；GraphAuthoring.Serialize 包围实际图序列化；GraphAuthoring.UpdateNodeIDs 包围编号生成；SkillAuthoring.Mutation 包围 FlowGraph 变更事务；SkillAuthoring.ValidateClosure 包围原闭包校验。嵌套项不能与父项重复相加。Graph 的两个标记只在 UNITY_EDITOR 下编译。

`ParadoxNotion.csproj` 与 `ThirdPersonClient.Runtime.csproj` 均编译通过，各为 0 警告、0 错误。日志分别为 [Graph 框架编译](../../.performance-build/reports/20261001-treeclip-graph-paradox-build.log) 和 [节点 Inspector 编译](../../.performance-build/reports/20261001-treeclip-graph-client-build.log)。两次构建使用 `--disable-build-servers /nr:false /p:UseSharedCompilation=false`，结束后执行 build-server shutdown；五个源文件的 diff 检查通过。

写入与构建前从同一目标实例确认项目路径正确、非 Play、非编译、非导入。初始 Editor Console 有范围外 BepuQueries 的 Fix64.Max 编译错误；没有修改该模块。此后 MCP 的 execute_code 与正式 CLI get_editor_state 均未返回有效状态，尚未确认本轮五个标记在 Unity 内加载。保留的 3351–3650 帧仍是旧运行记录，不能用于 TreeClip Graph 编辑结论。本轮未启动 Play、Profiler 录制或回放，未新增或修改测试代码，拖动与参数调整的实际耗时仍未验证。

用户随后提供 Hold on 弹窗，显示 UnitySynchronization.ExecuteTasks 已忙 08:55。只读进程检查确认目标主 Editor 为 PID 5200，弹窗持续增长；线程 16032 为 Running，CPU 累计从 17592.125 秒增长到 17732.25 秒。Editor.log 尾部停在主段动画导入及 PhysX 初始化；这些信息说明主线程持续工作，不能单靠弹窗文字认定死锁或确定具体方法。

读取同项目“手感”聊天确认，该任务向同一实例提交了 `character.foot_motion_bake` 的 replace_source，目标为 Corin_Pipeline_Attack3_Inplace.anim，随后 BuildPlanFromReadyArtifact 读取和状态请求均未得到结果。当前证据指向这轮动画替换／脚部分析请求的占用；未取得线程调用栈，具体耗时函数仍未证实，没有修改该任务的代码、取消它的操作或向其它聊天发送消息。

文件核对显示 Library/ScriptAssemblies/ParadoxNotion.dll 的修改时间仍为 2026-09-29 12:43:41，用户字符串中没有 GraphAuthoring.Serialize 与 GraphEditor.Canvas；不能把 Temp/bin 的静态编译成功当成 Editor 已加载本轮修改。已停止发送新的 Unity 请求并保留现场。步骤绘制中原有的连线 LINQ 与临时 GUIContent 清理尚未写入，等待确认 Editor 非编译后继续；没有据未知状态追加源码修改或构建。

用户确认长任务已结束后，同一 PID 5200 与同一实例恢复响应，项目路径正确，playing=false、compiling=false、updating=false、scriptCompilationFailed=false。只读反射确认上述五个 CPU 标记已加载。当前打开的“Timeline达到终止边界”只有 3 个节点；用户明确说明其它 TreeClip Graph 更卡，因此不能把这张图的空闲状态当作卡顿复现。

按正式 MonoScript GUID 检查 Corin/Pipeline 已保存资产，共有 229 张 Skill Graph，其中 TimelineBody／TimelineTrigger 共 76 张。最大 Timeline 图是 CorinAttackRushExplodeTimeline.asset 内的“Open RushAttackHandoff @14”，有 9 个节点、7 条连线；完整清单见 [已保存图规模](../../.performance-build/reports/20261001-treeclip-graph-static-size.json)。该统计只覆盖已保存的 Corin 资产，不能代表未保存图或其它角色。现有规模不能证明编号复杂度就是严重卡顿的主要来源。

继续清理 `GraphEditor.ShowNodesGUI → Node.DrawNodeWindow`：原先每个可见节点、每次 GUI 事件创建捕获节点的闭包与窗口委托，并重新创建两个固定 GUILayoutOption 及其参数数组。现在节点首次显示时绑定自身窗口回调，后续复用；固定最小尺寸选项由静态数组持有。窗口仍使用当前 node.ID 与原有 NodeWindowGUI，回调不捕获编号或 Editor。FullSerializer 的正式字段规则排除委托和 NonSerialized 字段，显示缓存不进入图数据。

`BtsmtlSkillStepInspector.Draw` 的连线检查改为索引遍历原有 outConnections，删除按钮复用两份固定 GUIContent。模式、步骤编辑、增加与删除的捕获闭包移入实际命令方法，只有输入确认后才创建；绘制入口不再因包含条件分支中的 lambda 而提前创建捕获对象。步骤修改仍通过 `Change → BtsmtlSkillFlowEditorMutation.Apply → SetSteps` 提交，并行方式仍提交给 SetMode；步骤身份、优先级、连线删除限制与原 Undo／校验规则保持原行为。

本轮 `ParadoxNotion.csproj` 与 `ThirdPersonClient.Runtime.csproj` 均编译通过，各 0 警告、0 错误；分别见 [节点画布编译](../../.performance-build/reports/20261001-treeclip-node-draw-build.log)、[步骤绘制编译](../../.performance-build/reports/20261001-treeclip-step-draw-build.log)。两次构建使用 `--no-restore --disable-build-servers /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false`，结束立即 shutdown；两个源文件 diff 检查通过。编译后 [IL 核对](../../.performance-build/reports/20261001-treeclip-draw-il.json)确认 DrawNodeWindow 没有捕获闭包构造，步骤 Draw 没有捕获闭包或 Action 构造；窗口委托仍在首次绑定分支创建。这不表示整个 IMGUI 或 Inspector 为 0 GC，布局作用域、枚举控件及编号文字仍有绘制开销。

检查期间 HTTP 服务重启，原 MCP 会话返回无效 Session ID；正式 CLI 在同一服务器和显式实例下恢复读取。读取发现 Unity 尚未加载本轮绘制修改，于非 Play、非编译、非导入状态仅请求一次脚本编译。未启动 Play、回放或新的 Profiler 录制；严重卡顿的主要 CPU 来源与实际拖动收益仍未取得问题图的帧证据。

域重载结束后，通过同一实例核对项目路径正确、playing=false、compiling=false、updating=false、scriptCompilationFailed=false。只读反射确认节点窗口回调、静态布局参数、步骤按钮内容和 ChangeStep 命令均已加载；FullSerializer 实际返回窗口回调不可序列化。Console 查询返回 0 条错误，两个 Profiler 录制开关均为 false。这些结果证明修改已加载并可编译，未执行节点拖动或参数编辑的端到端验收。
