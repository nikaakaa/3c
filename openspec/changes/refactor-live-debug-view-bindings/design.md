# 设计：Live Debug 目标与视图绑定

## 问题边界

运行时负责发布事实，Editor 负责读取事实。运行时不应选择“正在被作者看的角色”，也不应保存任何窗口选择；Editor 也不得回读 runtime Graph、Node、Timeline clone 或重采样 Timeline。

本设计把以下两类状态分开：

| 状态 | 归属 | 作用 |
| --- | --- | --- |
| target、channel、live/pause、history、分析后的 Trace snapshot | RuntimeDebugSession | 让所有观察页面处于同一角色、同一历史位置 |
| source、Follow / Pin、选中的 runtime instance、窗口局部状态 | RuntimeDebugViewBinding | 让 Graph 与 Timeline 独立选择各自需要看的执行实例 |

这不是新增第二个 Session。每个 Character 仍只有一条正式 Trace Buffer；所有窗口仍消费同一个分析后的 snapshot。

## 目标选择

每个进入 Live Debug 的窗口都会提交一个 source 请求：

- Graph：GraphAuthoringId + GraphAuthoringFingerprint
- Timeline：TimelineAuthoringId + TimelineAuthoringFingerprint

Target 解析按以下顺序执行：

1. 若当前 Unity 场景选择包含 CharacterPipelineHost 或其子对象，则把该 Host 视为作者的显式目标意图。
2. 显式 Host 已注册且 source map / content hash 精确匹配时，附着该 target。
3. 显式 Host 未注册或不匹配时，显示对应状态；不得偷偷切换到其它角色。
4. 没有显式 Host 时，若当前已附着 target 与 source 精确匹配，则保持该 target。
5. 没有显式 Host 且没有可保持 target 时，只有一个 registered target 精确匹配 source 时才自动附着。
6. 零个匹配或多个匹配时不自动选择。窗口显示候选与原因，作者通过 Target 菜单或 Host Inspector 显式选择。

“唯一精确匹配自动附着”是正式选择规则，不是按名称、列表顺序或默认配置的 fallback。Source Map 缺少 source、content hash 不同或 revision 不一致都不能视为匹配。

Diagnostics Editor 不直接引用角色管线程序集。它只公开 editor-only 的场景选择 resolver 注册点；Character Pipeline Editor 注册“当前 Selection 向上取最近 CharacterPipelineHost”的唯一解析器。这个注册点只传递显式 Host instance id，不扫描其它对象，也不形成第二条 target 选择路径。

Host Inspector 的显式 attach 可以选择任意已注册 Host；Graph / Timeline overlay 仍在自己的 source 校验失败时停止显示。这样 Host Inspector 可以查看角色级 Trace，而作者页不会画出错误来源的高亮。

## 视图绑定

RuntimeDebugViewBinding 为 editor-only 对象，不序列化到 Graph、Timeline 或 Character asset。它至少保存：

- 绑定视图类型与 source 请求；
- Following 或 Pinned 模式；
- 当前选中的 RuntimeInstanceKey；
- 本窗口的无实例、revision mismatch、已结束等状态。

Graph 窗口进入 Live Debug 时创建或刷新自己的 binding，并默认 Follow 当前 GraphAuthoringId。Timeline 窗口进入 Live Debug 时创建或刷新自己的 binding，并默认 Follow 当前 TimelineAuthoringId 的 playback。

Follow 从共享 snapshot 中取当前 source 最近的正式 runtime instance；Pin 只固定本窗口实例。一个窗口切换 Follow / Pin、页面栈或 Target 菜单不得改写其它窗口的 binding。

共享 Session 的 Pause / History 仍是全局的。这样 Graph 与 Timeline 同时停在同一个 logic tick / presentation frame，便于把状态跳转、TreeClip、动画 sample 和播放生命周期对齐。独立 history 会让两个窗口失去因果比较价值，因此不引入每窗口时间游标。

domain reload 不序列化 RuntimeDebugViewBinding、target 或 runtime instance。BaseTreeWindow 只序列化当前 Graph 的 serialized owner、property path 和 GraphAuthoringId；TimelineEditorWindow 只序列化已有 Timeline locator 与窗口 mode。窗口重建后必须从该精确 locator 重建 authoring page 和新的本地 binding，再由共享 Session 重新解析 runtime target。locator 失效或 identity 不一致时停止恢复，不按名称、路径近似或打开顺序猜测其它 Graph/Timeline。

## Timeline 多次播放

Timeline binding 必须从正式 Trace 构建 playback 摘要，而不是检查 Timeline authoring 数据。摘要至少包含：

- TimelinePlaybackId；
- Timeline source identity；
- 发起 Graph / Node source；
- 发起时可用的 State activation 或 TreeClip runtime context；
- 最近 logic / visual time、cycle、当前 lifecycle 或 terminal cause。

Timeline source 只有一个活跃 playback 时，窗口可进入 Follow 并显示该实例。多个实例时菜单必须展示上述摘要并要求作者 Pin 其中一次；不得按数组顺序静默选择。没有实例时显示“当前角色未执行该 Timeline”，不调用 preview evaluator 或用 authoring time 伪造运行状态。

若现有 Trace payload 缺少来源 Graph、Node 或 activation 信息，实现必须在正式 Timeline scheduler Trace 上补齐该结构化 provenance；不得从 runtime clone、显示名称或临时静态表反推。

## Target 结束与冻结历史

runtime target 注销时，RuntimeDiagnosticsTarget 仍按正式生命周期注销并释放其 Trace Buffer。Session 在注销通知中复制最后一个已分析的只读 snapshot，并将 attachment 状态置为 Ended：

- Ended snapshot 不持有 runtime Graph、Node、Track、Clip 或可继续写入的 Buffer；
- Graph 与 Timeline 可以继续在该 snapshot 上查看最后的 overlay；
- Ended snapshot 不接收新事件，不可 Resume Live；
- 作者显式附着新 target 或清除 Debug Session 时才丢弃该 snapshot。

这保留调试复盘能力，同时不让已结束角色继续作为 live target。

## 状态呈现

窗口必须显示清楚的状态，而不是统一显示“没有数据”：

- 未进入 Play Mode或没有已注册 target；
- 场景显式 Host 未注册；
- 场景显式 Host 不包含当前 source；
- 没有 source/hash 精确匹配 target；
- 多个匹配 target 待作者选择；
- 当前 source 尚未执行；
- source revision mismatch；
- target 已结束，正在查看冻结历史。

这些状态只描述 Trace 与 source 关系，不读取或改写 gameplay 状态。

## API 与迁移

删除 RuntimeDebugSession 的全局 SelectedInstance、FollowGraph、FollowTimeline、Follow、Pin 和对应单一 follow 字段；不保留兼容包装。

Session 暴露 target 选择、候选解析、channel、共享时间游标、分析 snapshot 和终止状态。Graph、Timeline 与 Host Inspector 使用统一 target attach API。Graph / Timeline 分别创建自己的 binding；Host Inspector 只显示 Session 级 Trace 摘要，不持有 Graph 或 Timeline binding。

Tree Inspector 的 Live Debug 左栏切换只影响 authoring information architecture，不重置 TreeWindow 自己的 binding。USS 中不支持的 :first-child / :last-child 选择器改为 Unity 支持的正式样式表达，不引入第二套 Inspector 布局。

## 取舍

### 不保留全局 instance 选择

全局 instance 简单，但 Graph 和 Timeline 的实例类型不同，最后一次操作必然覆盖前一次。业务上作者无法同时关联“状态走到 Attack1”和“Attack1 Timeline 播放到哪一帧”，因此不可接受。

### 不创建每窗口独立 RuntimeDebugSession

独立 Session 会让窗口能选择不同角色和不同历史位置，但同时观察时无法保证状态、Timeline 与动画 trace 的时间一致，还会重复扫描同一 Buffer。作品演示中更需要因果对齐，因此只分离 binding，不分离 target/history。

### 不按场景搜索结果自动选第一个

多角色时按顺序选择会把错误角色的事实映射到当前作者页。唯一精确匹配才可自动附着；其余情况要求显式选择，代价是多角色场景多一次点击，收益是调试可信。

### 保留结束快照而不是持有 runtime Buffer

直接保留 target/buffer 会延长 runtime 生命周期，可能保留已销毁对象。复制只读 snapshot 允许复盘，又不反向持有 runtime；代价是结束后不能继续刷新，这符合 target 已结束的事实。

## 停止条件

- 若现有正式 Trace 无法提供多 playback 的发起 source / activation provenance，且必须通过 runtime clone、名称猜测或第二套 Timeline 采样才能补齐，停止实施并说明缺少的正式 Trace contract。
- 若 Unity UI Toolkit 无法在不共享 runtime instance 的前提下维持 Graph 与 Timeline 同一历史游标，停止实施并说明需要调整 editor UI 结构的取舍。
