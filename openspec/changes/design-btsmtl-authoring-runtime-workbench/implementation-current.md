# 2026-09-20 实施记录

## 产品边界

Authoring、Preview、RuntimeDebug 始终从原 TimelineEditorWindow 切换。Timeline 始终使用原 Slate 面板；调用图导航复用已有 FlowCanvas 面板。不新增窗口播放器、场景、Session 或运行时求值链。

## 本轮补齐

| 任务 | 输入与处理 | 作者可见结果 |
|---|---|---|
| 2.3 | 最后一个 Timeline 窗口关闭时释放控制器与图观察的 interest；不调用 Session Stop | 关闭工具面不停止角色运行，不遗留该窗口的观察订阅 |
| 4.1 | Profile 按 Scene → ContextId/Composition.SessionId → ActorId 精确解析；监听目标注册和已绑定 Session 生命周期变化 | 显示未连接、准备中、正式失败或实际就绪；同名 Context 不跨场景混用 |
| 4.4 | 只有正式 Actor/Timeline Host 已初始化且返回有效作者与采用版本时才比较；作者修改、Undo 和项目变化重查冻结导出 | 不把空版本显示为已采用，过期 Export/Plan/Publication 作废；当前 playback 保持原内容 |
| 5.3 | 从正式节点状态、Timeline summary、父调用 generation 与 SourceMap 解析活动叶调用；Follow 唯一目标，Pin 指定实例 | 在原 Graph/Timeline 面板间导航，返回仍活动的父调用；并行调用要求选择，不按列表顺序猜测 |
| 5.5 | 执行实例保存记录对应的 SourceMap；观察位置变化重新建立运行投影；同一 Clip 取最新事件 | 较晚 Clip 不污染较早历史，旧历史不使用当前调用表；开放 TreeClip 使用实际退出位置，退出不再显示 open |
| 8.2 | 逐项对照现行 spec 与 project.md，删除已经退役或被现行规范替代的 delta | 不恢复 Document v5、Pose Image、整角色 Projection 或独立 Fixture；保留正式领域 owner |

## 代码入口

- `Editor/CharacterPipeline/ScenePlay/BtsmtlScenePlayTimelineController.cs`：模式、Session 菜单、精确目标、准备/采用状态与导航请求。
- `Editor/CharacterPipeline/Diagnostics/BtsmtlRuntimeFocusResolver.cs`：只读解析实际调用关系，不创建执行实例。
- `Editor/CharacterPipeline/Diagnostics/RuntimeDebugSourceNavigator.cs` 与 `BtsmtlSkillObservationSession.cs`：复用原面板和来源绑定；关闭旧观察再导航，避免遗留调用覆盖新选择。
- `Runtime/BTSMTL/Diagnostics/Editor/Scripts/RuntimeDebugViewModel.cs`：观察实例对应的记录 SourceMap，以及复用列表的事件读取接口。
- `Runtime/BTSMTL/Timeline/Editor/Scripts/RuntimeTimelinePlaybackProjection.cs` 与 `Tree/TimelineEditorMainWindow.cs`：只读内容、历史位置、最新生命周期和实际退出位置。

以上代码路径均相对 `3cDemo/Client/3C_Client/Assets/GameScripts/Main`。新增代码位于 Editor 观察层，不修改 Timeline 推进、Pose 帧事务或正式输入规则。

## 提交与并行事实

- 首批预览代码曾进入暂存区，原提交因共享 index 锁失败；另一任务随后在 `efa7eaeaf` 中一并提交了这些文件。该提交同时包含另一任务的 Pose/Timeline 改动，不能把整个提交归为本轮预览工作，也不重写共享历史。
- `d515212a2` 独立提交记录 SourceMap、历史投影、最新状态与开放 TreeClip 退出显示修正。
- `implementation-audit.md` 原有未提交改动来自其它工作，本轮没有覆盖或一并提交；其中 9 月 18 日“跨页面切换未完成”是历史状态，以本记录描述本轮增量。

## 验证边界

- 未新增测试，未启动 Play 或进行端到端验收，保留主 Editor。
- Center 改动：`e7c0a8207336464ea127d6569dff9343`，名称“完成原Timeline的作者预览与运行观察”。正式 compile 请求返回 `WorkspaceEditorInUse`，没有产生可引用的成功 Run。
- 使用现有 Unity 实例 `e852139597e42532` 的正式脚本刷新；首次等待就绪超时后，仅查询原实例，确认重载完成、新类型已加载且 Console 无错误。后续修改继续使用同一实例编译。
- 多任务同时修改主目录，因此编辑器编译结果只说明当时的脚本加载状态，不是 Center 固定版本对比或运行功能验收。
- OpenSpec strict 校验通过；未执行 archive，不把完成勾选解释为用户已验收。
