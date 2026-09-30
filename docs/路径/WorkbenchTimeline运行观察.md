# Workbench Timeline 运行观察

正式 Timeline 逻辑提交与表现提交产生各自的 TreeClip 事件。`CharacterTimelinePlaybackDiagnostics` 使用每条请求的实际 Time 和 Cycle 发布事实，`RuntimeDebugViewModel` 将当前播放的事实复制到复用缓冲，`TimelineRuntimeObservationBridge` 按 Sequence 选择当前记录，最后由 Slate projection 绘制只读覆盖层。

TreeDecision 片段的显示终点来自同一播放、同一 Cycle 的进入、更新、退出和销毁记录。片段仍在执行时显示当前播放时间；退出后保留本次执行的真实结束时间。重新进入后，较早退出记录不能覆盖新的执行。覆盖层不写回作者片段的 EndTime。

当前事件复制不再排序整个列表，消费者直接选择 Sequence 最新的事实。Timeline 窗口在 OnEnable 注册、OnDisable 释放，观察刷新使用该窗口列表；不再每次扫描全部 EditorWindow。窗口观察缓冲随关闭和关闭 RuntimeDebug 释放，新建 projection 同步当前只读状态。删除了只请求 Repaint 的冗余 ClearRuntimeTimeline 入口，覆盖层清理统一使用 ClearRuntimeOverlay。

## 2026-10-01 检查与范围

本批涉及六个 Workbench 源文件。Roslyn 静态检查覆盖 16 个程序集、785 个源码，语义错误为 0；没有新增或修改测试代码，没有执行 Workbench 端到端运行验收，也没有编辑器 FPS 数据。

纯 Timeline 接入正式 Session／角色播放、结构资源刷新、History 角色状态恢复仍是后续工作。当前观察修正不代表这些能力已完成。
