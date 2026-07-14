## 1. 会话与快照模型

- [x] 1.1 盘点 RuntimeDebugSession 的 target、channel、history、selected instance、follow、pin 和终止处理字段及全部调用方。
- [x] 1.2 定义 editor-only 的 target 解析请求、解析结果和 attachment 状态模型。
- [x] 1.3 定义 editor-only 的窗口本地 RuntimeDebugViewBinding 模型及 Follow / Pin 语义。
- [x] 1.4 为分析后的 Trace 定义可在 target 结束后保留的只读 snapshot 表达。
- [x] 1.5 将 RuntimeDebugSession 收敛为 target、channel、live/pause、history 和 snapshot 服务。
- [x] 1.6 删除 Session 中全局 selected instance、follow、pin 字段和公开 API，不保留兼容入口。
- [x] 1.7 让 Session 的 Changed 通知同时覆盖 target、snapshot、history 和 attachment 状态变化。

## 2. 正式 Target 解析

- [x] 2.1 实现场景选择到最近 CharacterPipelineHost 的显式目标解析，不扫描或猜测其它 Host。
- [x] 2.2 实现 source identity 与 content hash 的 registered target 精确匹配查询。
- [x] 2.3 实现显式 Host 已注册且匹配时的 attach。
- [x] 2.4 实现显式 Host 未注册或不匹配时的明确状态，不自动改选其它 target。
- [x] 2.5 实现无显式 Host 时保留当前精确匹配 target 的规则。
- [x] 2.6 实现无显式 Host 且唯一精确匹配 target 时的自动 attach。
- [x] 2.7 实现零个或多个精确匹配 target 时的候选状态与显式 Target 菜单数据。
- [x] 2.8 让 Host Inspector、Graph 和 Timeline 复用同一条 target attach / detach 路径。

## 3. 结束快照与严格 source 校验

- [x] 3.1 在 target 注销前捕获最后一个可分析 Trace snapshot。
- [x] 3.2 将 target 注销后的 Session 状态改为 Ended，而不是直接丢弃 editor view model。
- [x] 3.3 保证 Ended snapshot 不持有 runtime Buffer、Graph、Node、Track 或 Clip 引用。
- [x] 3.4 禁止 Ended snapshot 接收新事件或 Resume Live。
- [x] 3.5 让显式新 target attach 或清除 Session 正式释放旧 Ended snapshot。
- [x] 3.6 保留 Source Map / content hash revision mismatch 的严格拒绝，不增加名称、路径或顺序 fallback。

## 4. Graph 窗口绑定

- [x] 4.1 在 BaseTreeWindow 持有当前 Graph 的本地 RuntimeDebugViewBinding。
- [x] 4.2 进入 Live Debug 时按当前 Graph source 请求解析 target，并默认 Follow 当前 GraphAuthoringId。
- [x] 4.3 让 Graph instance 菜单只操作 TreeWindow 的本地 Follow / Pin。
- [x] 4.4 让 Graph overlay 从共享 snapshot 与本地 binding 读取 Node、Edge 和 StateMachine 状态。
- [x] 4.5 让 Data / Inspector 页签和 Graph breadcrumb 切换不重置当前 TreeWindow binding。
- [x] 4.6 为未运行、多 target、mismatch、已结束和无实例显示明确 Graph 状态。
- [x] 4.7 让 BaseTreeWindow 保存当前 Graph 的 serialized owner、property path 和 authoring identity，并在 domain reload 后只按该精确 locator 恢复当前 Graph 与本地 binding。

## 5. Timeline 窗口绑定

- [x] 5.1 在 TimelineEditorWindow 持有当前 Timeline 的本地 RuntimeDebugViewBinding。
- [x] 5.2 进入 Live Debug 时按当前 Timeline source 请求解析 target，并默认 Follow 当前 TimelineAuthoringId。
- [x] 5.3 从正式 Timeline Trace 构建 playback 摘要，包含 playback、来源 Graph / Node、activation context、时间、cycle 和 terminal / lifecycle 状态。
- [x] 5.4 若正式 Trace 缺少 playback 来源 provenance，在 Timeline scheduler 的正式 Trace contract 补齐结构化来源字段。
- [x] 5.5 只有一个匹配 playback 时让 Timeline binding 跟随它。
- [x] 5.6 多个匹配 playback 时在 Timeline 菜单显示来源摘要，并只固定当前 Timeline 窗口选择。
- [x] 5.7 没有匹配 playback 时显示“当前角色未执行该 Timeline”，不调用 preview 或重新采样 authoring 数据。
- [x] 5.8 让 Timeline overlay 从共享 snapshot 与本地 binding 显示 logic/visual time、Track、Clip、TreeClip 和动画生命周期。
- [x] 5.9 修正 Timeline Live Debug 中仍使用全局 Graph Follow 文案或行为的残留。
- [x] 5.10 让 TimelineEditorWindow 在 domain reload 后保留 Live Debug mode，并从已序列化 Timeline locator 重建本地 binding。

## 6. Host Inspector 与编辑器清理

- [x] 6.1 将 CharacterPipelineHostEditor 的 attach 控件收敛到统一 target 选择 API。
- [x] 6.2 让 Host Inspector 只读取共享 Session snapshot，不创建或修改 Graph / Timeline binding。
- [x] 6.3 让 Host Inspector 显示 live、ended、未附着和 source 不匹配等 Session 状态。
- [x] 6.4 移除 BaseTreeInspectorInside.uss 中不受 Unity 支持的 :first-child / :last-child 选择器，并保持现有布局边界。
- [x] 6.5 搜索确认不存在全局 runtime instance/follow/pin、第二个 RuntimeDebugSession 或 runtime clone editor binding 残留。

## 7. 文档与静态校验

- [x] 7.1 更新 openspec/project.md 的 RuntimeDebugSession 架构口径为“共享 target/history，窗口独立 binding”。
- [x] 7.2 更新受影响的 current spec，不保留共享全局 instance/follow/pin 的旧要求。
- [x] 7.3 使用项目既有的受影响程序集静态构建入口编译；若调用 dotnet build/msbuild，使用 --disable-build-servers /nr:false /p:UseSharedCompilation=false。
- [x] 7.4 若执行 dotnet build/msbuild，构建结束后立即执行 dotnet build-server shutdown。
- [x] 7.5 运行 openspec validate refactor-live-debug-view-bindings --strict --no-interactive。
- [x] 7.6 通过已连接 Unity Editor 触发脚本编译并确认无本 change 引入的 Console error。
