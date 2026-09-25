# 2026-09-20 实施记录

## 产品边界

Authoring、Preview、RuntimeDebug 始终从原 TimelineEditorWindow 切换。Timeline 始终使用原 Slate 面板；调用图导航复用已有 FlowCanvas 面板。不新增窗口播放器、场景、Session 或运行时求值链。

## 本轮补齐

### 2026-09-22 动态长度 TreeClip

表现域 TreeClip 已接入正式表现帧候选链。TreeDecision 片段由表现 TimelineBody 的 OnEnable/Root 图请求结束；请求在当前候选帧执行 OnDisable、移除 ActiveTreeClips 并撤回持续 Camera 输出，只有 Commit 保存退出状态，Discard 保留上一次已接受状态。FrameBoundary 仍表示固定区间。

运行观察按执行域分别使用 LogicCycle/LogicTime 和 VisualCycle/VisualTime。未退出片段跟随对应已提交游标，已退出片段使用实际退出事实；History 不读取未来退出事件，作者资产不被运行长度改写。动态长度按 playback、generation、cycle 和 clip identity 隔离。

作者入口中，Logic TreeDecision 的 End 跟随 Timeline 终点；Presentation TreeDecision 的 End 可编辑但只代表布局上界。表现域实际退出仍来自 TreeDecision 图请求，FrameBoundary 才使用作者 End 作为固定边界。

相关入口：`TimelineRuntimePresentationDriver.RequestTreeClipExit`、`CharacterTimelineHost.ExecutePresentationGraphs`、`RuntimeTimelinePlaybackProjection.UpdateOpenClipEnds`。表现图只读取只读角色事实并提交已有 Camera owner，不新增播放器或时钟。

| 任务 | 输入与处理 | 作者可见结果 |
|---|---|---|
| 2.3 | 最后一个 Timeline 窗口关闭时释放控制器与图观察的 interest；不调用 Session Stop | 关闭工具面不停止角色运行，不遗留该窗口的观察订阅 |
| 4.1 | Profile 按 Scene → ContextId/Composition.SessionId → ActorId 精确解析；监听目标注册和已绑定 Session 生命周期变化 | 显示未连接、准备中、正式失败或实际就绪；同名 Context 不跨场景混用 |
| 4.4 | 只有正式 Actor/Timeline Host 已初始化且返回有效作者与采用版本时才比较；作者修改、Undo 和项目变化重查冻结导出 | 不把空版本显示为已采用，过期 Export/Plan/Publication 作废；当前 playback 保持原内容 |
| 5.5 | 执行实例保存记录对应的 SourceMap；观察位置变化重新建立运行投影；同一 Clip 取最新事件 | 较晚 Clip 不污染较早历史，旧历史不使用当前调用表；开放 TreeClip 使用实际退出位置，退出不再显示 open |
| 8.2 | 逐项对照现行 spec 与 project.md，删除已经退役或被现行规范替代的 delta | 不恢复 Document v5、Pose Image、整角色 Projection 或独立 Fixture；保留正式领域 owner |

## 2026-09-25 状态更正：RuntimeDebug 导航未闭环

目标仍以本 change 的 spec 为准：Profile 选定正式 Scene、Session 和 Actor 后，RuntimeDebug 应沿该 Actor 的实际技能调用栈在 FlowCanvas 与 Slate 间切换；子调用返回时恢复父路径。唯一明确的活动调用自动 Follow，并行调用由作者显式 Pin。

当前代码已有 Profile 到 Actor 的精确解析、Diagnostics target 附着、Live interest、调用候选解析和来源导航入口。`BtsmtlScenePlayTimelineController.FollowRuntime` 只有在候选数恰好为一时才导航；并行调用由作者 Pin。编辑器在没有活跃候选时保留最近完成的短调用，并排除同一调用链的父子重复候选；这些候选现在可由正式 Fixed 技能 Trace 提供，实际运行显示尚待确认。`TimelineEditorWindow.GetRuntimeObservationSummaries` 按 Timeline 与来源 Graph identity 精确过滤；独立打开 Timeline 资产时没有来源 Graph identity，当前结果可能为空，零结果的未绑定状态已另行补充。

Fixed 角色已在 `FixedCharacterRegistration` 装配 `FixedCharacterRuntimeDiagnosticsAdapter`，不再接空诊断接收器。编译 SourceMap 在装配时建立操作、节点、边、图调用和 TreeClip 入口映射；候选 Trace 留在正式 Step 结果中，`FixedSimulationOutputAggregate` 等所有角色提交完成后才向现有 `RuntimeDiagnosticsStore` 发布，提交中止则丢弃。编辑器 `BtsmtlRuntimeFocusResolver` 按 `SkillExecution` 节点事件识别技能图，Timeline 仍由 Timeline Host 的正式播放事实提供；两者按动作实例、技能代数和调用路径对应，不从作者资产反推运行结果。

作者已报告运行时 RuntimeDebug 一直看不到有效内容。正式技能节点事实与编辑器候选之间的代码缺口已接通；`51fef1cd8` 已修正 Fixed/Float32 嵌套 Timeline 来源，Timeline 启动时使用当前 TreeClip 入口对应的 GraphInvocationPath 和图调用代数。尚未取得本版运行记录来确认角色绑定后的技能、子图、Timeline、返回父路径和并行 Pin 在 Unity 中实际显示正确。任务 5.2、5.3 保持未完成。当前并行闭环任务明确确认未修改本目标的诊断文件；双方不同时触发 Unity 刷新、编译或回放。

编辑器提交 `fc019a287` 已让独立 Timeline 缺少来源图、Timeline 无匹配调用、角色已附着但无可导航事实分别显示明确状态；它没有提供缺失的技能执行事实，也不改变 5.2、5.3 的未完成状态。

编辑器提交 `facb9e41d` 修正 Capture History / Ended 状态的来源导航：角色从 live registry 注销后，导航改用冻结 ViewModel 的 Host 信息和历史 SourceMap，不再在入口处直接失败。

## 2026-09-25 运行验收阻塞

使用 Unity 实例 `e852139597e42532` 尝试进入正式 ScenePlay 场景的 Play；本轮实例长时间停在 PlayMode transition，MCP 读状态多次超时，没有观察到已注册的 `RuntimeDiagnosticsTarget`，之后通过 CLI 退出 Play。`Editor-prev.log` 中另有较早运行的 `TEngine.PlayModeErrorAutoExit` 和分页内存约 `60.8/65.1 GB`（93%）的记录，但不能据此认定本轮切换未完成的原因。本轮没有取得角色、技能、子图或 Timeline 诊断事实，不能用它证明 RuntimeDebug 自动导航失败或成功。

当前结论仍是：代码链已完成到运行时接线，但当前没有角色、技能、子图或 Timeline 的运行事实，5.2、5.3 不标为完成。

编辑器提交 `3ab0b56d2` 将 Ability Timeline 的来源图与技能执行实例按动作实例、技能代数和调用路径对应，打开 Slate 时同时绑定已有 FlowCanvas 观察会话；`93313a61a` 保留最近完成的短调用作为 Follow 候选；`db9f553fc` 保留仅有正式 Timeline 事实时的现有导航。`33f47fbb0`、`297dd8e66`、`75bf029ab`、`f3fbffda6`、`c1ea75756` 接通 Fixed 诊断来源、TreeClip 入口和提交后发布；`a33e48729` 修正子图父调用匹配与返回；`c06bfc115` 将本次诊断热路径的链表节点和枚举文本分配移至装配阶段；`51fef1cd8` 统一 Fixed/Float32 嵌套 Timeline 的实际调用入口。以上是代码与编译进度，不是运行时验收结论。

## 代码入口

- `Editor/CharacterPipeline/ScenePlay/BtsmtlScenePlayTimelineController.cs`：模式、Session 菜单、精确目标、准备/采用状态与导航请求。
- `Editor/CharacterPipeline/Diagnostics/BtsmtlRuntimeFocusResolver.cs`：只读解析实际调用关系，不创建执行实例。
- `Editor/CharacterPipeline/Diagnostics/RuntimeDebugSourceNavigator.cs` 与 `BtsmtlSkillObservationSession.cs`：复用原面板和来源绑定；关闭旧观察再导航，避免遗留调用覆盖新选择。
- `Runtime/BTSMTL/Diagnostics/Editor/Scripts/RuntimeDebugViewModel.cs`：观察实例对应的记录 SourceMap，以及复用列表的事件读取接口。
- `Runtime/Simulation/Unity/Fixed/FixedCharacterRuntimeDiagnosticsAdapter.cs` 与 `FixedSimulationCompositionContracts.cs`：将已提交的 Fixed 技能 Trace 映射为现有 RuntimeDebug 节点、边及调用实例事实。
- `Runtime/BTSMTL/Timeline/Editor/Scripts/RuntimeTimelinePlaybackProjection.cs` 与 `Tree/TimelineEditorMainWindow.cs`：只读内容、历史位置、最新生命周期和实际退出位置。

以上代码路径均相对 `3cDemo/Client/3C_Client/Assets/GameScripts/Main`。诊断接线位于正式 Fixed 输出提交边界和 Editor 观察层，不修改 Timeline 推进、Pose 帧事务或正式输入规则。

## 提交与并行事实

- 首批预览代码曾进入暂存区，原提交因共享 index 锁失败；另一任务随后在 `efa7eaeaf` 中一并提交了这些文件。该提交同时包含另一任务的 Pose/Timeline 改动，不能把整个提交归为本轮预览工作，也不重写共享历史。
- `d515212a2` 独立提交记录 SourceMap、历史投影、最新状态与开放 TreeClip 退出显示修正。
- `implementation-audit.md` 的 9 月 18 日记录只作为历史状态；跨页面自动导航以本文件 2026-09-25 状态更正为准。

## 验证边界

- 当轮未新增测试、未启动 Play 或进行端到端验收，保留主 Editor。Fixed Core、Fixed Unity、Diagnostics 与 Editor 的相关程序集独立编译均为 0 个错误；这只能证明当前代码可编译，不能证明作者运行时已经看到有效技能事实。
- `facb9e41d` 后尝试按项目规定编译 `ThirdPersonClient.Editor.csproj`；Unity 生成的 `Temp/obj/ThirdPersonClient.Editor/project.assets.json` 当前缺失，命令以 `NETSDK1004` 结束，随后已执行 `dotnet build-server shutdown`。这次没有把依赖文件缺失误判为源码错误。
- Center 改动：`e7c0a8207336464ea127d6569dff9343`，名称“完成原Timeline的作者预览与运行观察”。正式 compile 请求返回 `WorkspaceEditorInUse`，没有产生可引用的成功 Run。
- 使用现有 Unity 实例 `e852139597e42532` 的正式脚本刷新；首次等待就绪超时后，仅查询原实例，确认重载完成、新类型已加载且 Console 无错误。后续修改继续使用同一实例编译。
- 多任务同时修改主目录，因此编辑器编译结果只说明当时的脚本加载状态，不是 Center 固定版本对比或运行功能验收。
- OpenSpec strict 校验通过；未执行 archive，不把完成勾选解释为用户已验收。
