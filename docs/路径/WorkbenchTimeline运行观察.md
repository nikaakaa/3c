# Workbench Timeline 运行观察

正式 Timeline 逻辑提交与表现提交产生各自的 TreeClip 事件。`CharacterTimelinePlaybackDiagnostics` 使用每条请求的实际 Time 和 Cycle 发布事实，`RuntimeDebugViewModel` 将当前播放的事实复制到复用缓冲，`TimelineRuntimeObservationBridge` 按 Sequence 选择当前记录，最后由 Slate projection 绘制只读覆盖层。

TreeDecision 片段的显示终点来自同一播放、同一 Cycle 的进入、更新、退出和销毁记录。片段仍在执行时显示当前播放时间；退出后保留本次执行的真实结束时间。重新进入后，较早退出记录不能覆盖新的执行。覆盖层不写回作者片段的 EndTime。

当前事件复制不再排序整个列表，消费者直接选择 Sequence 最新的事实。Timeline 窗口在 OnEnable 注册、OnDisable 释放，观察刷新使用该窗口列表；不再每次扫描全部 EditorWindow。窗口观察缓冲随关闭和关闭 RuntimeDebug 释放，新建 projection 同步当前只读状态。删除了只请求 Repaint 的冗余 ClearRuntimeTimeline 入口，覆盖层清理统一使用 ClearRuntimeOverlay。

## 2026-10-01 检查与范围

本批涉及六个 Workbench 源文件。Roslyn 静态检查覆盖 16 个程序集、785 个源码，语义错误为 0；没有新增或修改测试代码，没有执行 Workbench 端到端运行验收，也没有编辑器 FPS 数据。

当时提出的纯 Timeline 接入正式 Session／角色播放尚未实施。用户随后澄清，本次需要的是把主图实际执行的节点等显示为轻量只读 Clip；不要求扩展独立 Timeline 的角色播放入口。结构资源刷新、History 角色状态恢复仍未在此批验证，不能据当前观察修正称整个 Workbench 已完成。

## 主图执行 Clip

正式角色提交后的 `FixedCharacterRuntimeDiagnosticsAdapter.PublishCommitted` 发布主图与子调用中的节点进入、持续、等待、完成、停止、Loop 迭代和分支决策事实。`RuntimeDebugTargetProvider.ReadExecutionTimeline` 把新增事实送入 `SpanAccumulator`，`BtsmtlExecutionTimelineView` 直接消费这些区间，通过现有 Slate 嵌入面板显示只读 Track／Clip；它不生成作者 Timeline 资产，也不参与执行节点或角色状态。

节点与等待区间按 RuntimeInstance、分支、Epoch、内容版本、来源及 ActivationGeneration 配对。RuntimeInstance 已携带技能释放与图调用代数，Loop 区间额外使用 LoopIteration，重复发生不会覆盖另一调用的片段。持续片段的显示终点读取同分支、同 Epoch 的已提交时钟，完成或停止后使用真实退出位置。瞬时决策保留同 Tick 的起点与终点，Slate 使用最小 6 像素绘制和命中范围，不伪造执行时长。

入口是 `Window/BTSMTL/Preview` 下方的执行时间线。轨道名直接读取对应内容版本的 SourceMap 显示名称；节点、等待、Loop 与分支使用现有区间类型区分颜色。选中片段的执行、等待或退出状态直接读取最新区间，详细事实显示在状态提示中；投影重建或取消片段选择时清空状态显示，保留已选事件的历史定位身份。双击 Clip 沿 `BtsmtlScenePlayTimelineController.OpenExecutionSource` 定位原图来源，进入／退出按钮沿正式诊断 Session 定位历史事件。

实时投影复用既有增量累积器与预分配 Track／Clip，本次没有增加运行时状态、采样字段或新的角色播放路径。新增状态显示复用事实中的字符串或固定状态文本，不在每次刷新拼接文本。暂停且没有新的诊断事实时，Session 版本检查保留当前投影。

本次显示改动的 Roslyn 静态检查覆盖 16 个程序集、790 个源码，语义错误为 0。没有新增或修改测试代码，没有启动 Play 或 Unity batchmode；未执行主图 Clip 的界面、端到端与 GC／FPS 运行验收。

## 历史角色画面

隐藏 Preview 在正式表现提交后的相机渲染中，将画面复制到共享 `BtsmtlScenePlayPreviewRenderer` 的有界环形存储。只在该角色的 Capture 正在录制时记录，保留最近 64 张画面，最长边不超过 512 像素；纹理存储准备后复用，不逐帧创建托管画面对象。

`RuntimeDebugSession.TryGetHistoryEvent` 提供当前历史位置的实际事件。Preview 视口使用 CaptureId、CharacterRuntimeId、ExecutionBranchId、RuntimeEpoch 和事件所属域的 Tick／表现帧查找对应画面，显示记录中的 Tick 与表现帧。没有匹配画面或画面已被淘汰时留空并显示原因，返回 Live 后显示当前相机输出。历史查看期间禁用播放、单步、速度、时钟和输入提交，保留暂停操作。

这条链查看已经输出的相机画面，不重放角色，也不恢复可执行的角色状态。关闭或重建 Preview 时释放画面存储；视口缩放只重建实时输出纹理，保留已有历史画面。开始新的 Capture 时复用纹理并重置记录身份。

## Timeline 内容采用与重新预览

作者资产修改后，`BtsmtlScenePlayTimelineController.ApplyChangedTimelineContent` 通过 `CharacterTimelineContentStore` 导出冻结内容。编辑器导出使用作者当前的 TreeClip／Marker 图引用和完整内容闭包；Player 装配使用正式编译图绑定。

准备阶段检查当前 Session 已准备的图调用、图内容版本、动画 producer 的 Channel／Slot、动画资源目录和逻辑 MotionWarp 状态身份。Track／Clip 调序以及不需要新绑定的片段增删可以采用，新增或改变正式编译依赖时说明具体缺失项。Publish 只封存已经准备的计划；最终 Adopt 确认计划属于当前 Session、内容代数未变且作者仍与冻结版本一致，不在 Export、Prepare、Publish、Adopt 四个阶段反复冻结验证同一份内容。已活动的播放持有原内容，后续正式播放使用采用后的版本。

Preview 工具栏提供“重建并重新预览”。输入仍来自所选 Profile 的装配 Prefab 和 Context；控制器收集该 Context 中不同角色 Definition，关闭旧 Preview 后调用 `GameplayAbilityExecutionDataAssetPublisher.RepublishDefinition`，在各 Definition 已引用的原路径重建 Fixed／Float32 技能产物，再调用 `CharacterPoseNativeDomainResourceSetCompiler.Compile` 编译正式动画资源。最后沿原 `BtsmtlScenePlayPreviewHost.OpenAsync` 重新装配角色、Session 和相机，当前播放与画面历史重新开始。

重建复用现有技能发布与动画资源编译路径，不猜测新资源绑定。新增动画仍需作者在正式 Profile 中声明资源及所需 Pose 绑定；尚未发布的技能缺少 Fixed／Float32 产物时明确报错，先由正式发布入口生成。发布器只保存本次技能产物与 Definition，避免保存其它正在编辑的资产。重建期间禁用 Profile 切换和重复重建；Play、编译、导入或脚本编译失败时不提供重建操作。

本批 Roslyn 静态检查覆盖 19 个程序集、1284 个源码，语义错误为 0。目标 Editor 的项目路径为 `D:/Unity_Project_1/3C/3cDemo/Client/3C_Client`，刷新与域重载后在同一实例 `e852139597e42532` 确认新版 API 已载入；最终 `isCompiling=false`、`isUpdating=false`、`isPlaying=false`、`scriptCompilationFailed=false`。没有新增或修改测试代码，没有执行 Play、Unity batchmode、内容重建按钮的端到端操作或 GC／FPS 运行验收。
