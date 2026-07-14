# 设计：按需 Live State 与显式 Capture/Rewind

## 设计目标

本次不是把现有 Trace Buffer 调小，也不是让窗口少刷新几次，而是让诊断数据的生命周期与作者真正需要的信息匹配。

作者平时问的是“角色现在运行到哪里了”；这只需要当前状态。作者复盘问题时问的是“刚才每一步为什么这样走”；这才需要历史。两者数据量、保留时长和产品操作都不同，不能继续使用同一个总是开启的 event list。

| 作者动作 | 需要的数据 | 运行时保留方式 | Editor 消费方式 |
| --- | --- | --- | --- |
| 打开 Graph Live Debug | 当前 node/edge/state scope | 每个状态键的最新值 | 增量 change set 更新对应 overlay |
| 打开 Timeline Live Debug | 当前 playback、logic/visual time、Track/Clip、animation lifecycle | 每个 playback/track/clip 的最新值 | 增量更新 playhead、轨道与详情 |
| 选择 Host Inspector | 当前 action、blackboard、motion、animation 摘要 | 每个诊断域的最新值 | 读取当前摘要，不扫描历史 event |
| 开始 Capture | 过程中的边界事实，或作者明确要求的连续细节 | target 独立有界 capture segment | Provider 增量接收，停止后形成冻结历史 |
| Stop Capture 后 scrub | 指定历史位置 | 冻结 capture snapshot | 仅在位置改变时重建该位置的只读 view |

## 运行时采集模型

### Interest 与采集开关

每个 `RuntimeDiagnosticsTarget` 注册时只提供 target metadata、program revision、Source Map 和空的 diagnostics store，不默认启用任何 channel。

Editor 中的 Graph、Timeline、Host Inspector 不直接改写 target 的“全局当前 channel”。它们向 `RuntimeDebugSession` 获取和释放一个目标级 interest：

- interest 声明观察种类：Live State 或 Capture；
- interest 声明 channel：Graph、StateMachine、Timeline、Blackboard、Animation、Motion；
- Capture interest 额外声明 detail；
- Session 对同一 target 的所有有效 interest 做并集，并把结果一次性应用到 runtime diagnostics context。

当 target 没有任何 interest 时，effective channel 必须是 `None`。Producer 在构造 `RuntimeTracePayload`、解析 Source Map handle 或拼接字符串前，必须先查询该事实是否被 Live State 或 Capture 需要。这样关闭诊断的成本只剩一次轻量的 interest/channel 判断，不是写入后再丢弃。

Graph Live Debug 默认请求 Graph + StateMachine 的 Live State；Timeline Live Debug 默认请求 Timeline + Animation 的 Live State；Host Inspector 按它实际显示的域请求 Live State。多窗口同时打开时，只有一个 target 的并集，不存在第二个 Buffer 或第二个采集器。

### Live State

Live State 是一张按稳定键覆盖更新的当前事实表，不是滚动 event history。键至少包含 source identity、runtime instance identity、诊断域和事实种类；值保留最新 position、sequence、payload 和结束/激活状态。

例子：

- `RunnableNode` 的 Running/Success/Failure 是当前 node state；同一状态不重复写入。
- `EdgeSelected`、State transition、state enter/exit、TreeClip enter/exit 是边界事实，同时更新与该 element 相关的当前状态。
- Timeline 的 logic time、visual time、active Track/Clip 与 playback terminal 是当前 playback state。visual time 可以每个表现帧改变，但始终覆盖同一个 state record，不积累为 history。
- Animation 的 selection、PendingFirstSample、Current、Outgoing、Retired、sample time 与 fade progress 是当前 layer/playback state；每次变化覆盖当前记录。

Live State store 维护单调 revision 和可读取的 delta cursor。新 provider 首次附着可以复制一次当前状态；之后只读取 cursor 之后变更的 key。若 consumer 落后到增量日志不可用，store 只允许回传一次完整“当前状态表”，其大小受活跃 source/instance 数量限制，不能退化为历史全量复制。

### Capture/Rewind

Capture 只由作者在共享 Session 中显式开始。它拥有独立有界 segment buffer、单调 cursor 和 capture identity；停止 Capture 时复制成不可变 snapshot，target runtime 不再持有冻结数据。

Capture detail 分三档：

| detail | 默认内容 | 不包含的高频内容 |
| --- | --- | --- |
| Boundary | node/state 生命周期、edge selected、state transition、TreeClip 生命周期、Timeline request/start/terminal、animation selection/lifecycle | 每 tick NodeStatus、每次条件判断、每帧 time/sample/fade |
| Evaluation | Boundary 加条件图/edge/transition 的通过或失败事实 | 每帧连续采样 |
| Continuous | Evaluation 加 Timeline logic/visual time、animation sample/fade、presentation interpolation | 无 |

Capture 的默认 detail 是 Boundary。`EdgeEvaluated`、`ConditionGraphEvaluated` 和逐帧动画/Timeline sample 不再因为某个窗口刚打开就默认产生；它们只有在 Capture 明确请求对应 detail 时才保留历史。

Live State 与 Capture 可以同时工作：作者可以一边看当前 Graph/Timeline，一边录制有界历史。Stop Capture 不暂停 gameplay；它只固定 capture snapshot。Live 的“冻结”只固定当前 read model，并释放对应 Live interest；恢复 Live 后重新获取 interest，不把 frozen current state 误称为历史回放。

### Producer 归类

每个正式 producer 仍只在自己的业务生命周期边界发布 diagnostics，不得新增第二套 gameplay 状态：

- TreeDesigner：Graph、Runnable、Composite、ConditionRuleGraph、StateMachine。
- Character Pipeline：Action、Pipeline Blackboard、Motion、Timeline scheduler、TreeClip。
- Presentation：Animation selection、Timeline animation sample、AnimationPlaybackLifecycle、Animancer fade、presentation interpolation。

实现时会把已有 `RuntimeTraceEventKind` 分为 Live State、Boundary Capture、Evaluation Capture、Continuous Capture 四种发布策略。不存在“先发布 All 再在 Editor 过滤”的兼容分支。

## Editor 共享 Provider

`RuntimeDebugSession` 仍是唯一的 target resolver、attachment owner 和共享 capture 时间位置 owner，但不再每个 Editor update 把 runtime buffer 复制为完整 snapshot。

Session 为已附着 target 建立一个共享 read provider：

1. 附着时按严格 program revision/source map 捕获 Source Map snapshot 一次；只有 target revision 改变才替换。
2. 每个 Editor update 最多检查一次 target state/capture revision。
3. revision 未变时不分配 event list、不调用 analyzer、不通知窗口。
4. revision 变化时只读取 delta，更新 provider 内的 source-mapped current state、instance 索引、Timeline playback 摘要和 Host 摘要，并发布一个版本化 change set。
5. Graph、Timeline 和 Host Inspector 读取同一 provider revision；各自只消费影响自己 source/instance 的 change set。

Capture 录制中，provider 也按 cursor 增量吸收新的 capture event。作者停止 Capture 或拖动 history position 时，provider 才从冻结 capture 计算该位置的只读历史 view。拖动位置不回滚 runtime，不修改 live state，也不重新运行 Graph/Timeline。

严格 source identity/content hash target 解析、显式 Host 优先、唯一精确匹配自动附着、Ended snapshot 和每窗口 Follow/Pin 都保持原有语义。Source Map 只被 provider 缓存和查询；Graph/Timeline 不自行建立 map，也不读取 runtime clone。

## 窗口更新模型

### Graph

Graph window 在页面选择、authoring data 改动、Undo/Redo、domain reload 恢复时构造并缓存 `RuntimeDebugTargetRequest`。它不在每个 `Update` 重新计算 Graph fingerprint，也不在 Session changed 后清空全部 Node/Edge overlay。

收到 provider change set 后，Graph 只更新当前 binding 的变化 Node/Edge/State；Target 和 instance 菜单只在 target list 或 instance list revision 变化时重建。没有相关 change 时不 Repaint。

### Timeline

Timeline window 同样只在 Timeline locator 或 authoring 内容真正变化时重算 request。它从 provider 的 current playback summary 取得 logic/visual time、Track/Clip、TreeClip 和 animation lifecycle；不对 `Events` 做 LINQ 全量扫描，也不通过 authoring Timeline 重新采样。

Timeline 的 Playback 菜单只在对应 Timeline 的 playback summary revision 变化时刷新。Graph 与 Timeline 同时打开时，二者读取同一 target provider，却分别维护自己的 Follow/Pin。

### Host Inspector

Host Inspector 使用 provider 的当前 channel summary；它不再以 `Latest(channel)` 从全量 event 列表筛选。作者进入 Capture scrub 时，Host、Graph、Timeline 都读取同一个 frozen history position。

## Target 结束与 domain reload

target 注销时，provider 必须先冻结已得到的 Live State，并在存在 active/frozen Capture 时取得不可变 capture snapshot，然后释放对 runtime target/store 的引用。Ended view 只读，不允许继续 Live 或继续录制；显式附着新 target 或 Clear Session 才释放它。

Graph/Timeline 不序列化 runtime interest、target 或 runtime instance。domain reload 后，已存在的精确 authoring locator 恢复页面和本地 binding，再由 Session 重新解析 target 并重新获取 interest。locator、source map 或 revision 不匹配时必须停止，不得按名称、路径近似、窗口顺序或旧 cursor 猜测。

## 删除与迁移

本 change 完成后必须删除以下旧链路，而不是保留开关：

- 默认 `RuntimeTraceChannel.All` 与 `RuntimeDebugSession.SetChannels` 直接写 target Buffer 的模型；
- `RuntimeTraceBuffer.Snapshot()` 的全量 event copy 和以它为核心的 paused history；
- `RuntimeDebugAnalyzer.Analyze` 对完整 history 重建 state/source map/instance/event list；
- `RuntimeDebugTraceSnapshot.Capture` 与 `RuntimeDebugViewModel.Events/Latest` 作为 Live 数据入口；
- Graph/Timeline `Update` 驱动的全量 Refresh、全 overlay clear、全菜单重建和每帧 request fingerprint；
- 同一 target 的平行 provider、Buffer、capture 或 fallback polling path。

保留的是一条正式路径：`producer -> target interest gate -> Live State / explicit Capture -> shared incremental provider -> local view binding -> Graph/Timeline/Host view`。

## 取舍

### 方案 A：Live State + 显式 Capture（推荐）

日常调试只看当前事实，只有作者决定复盘时才记录过程。对 demo 的业务价值是正常 Play、Graph/Timeline 双开和动画表现都不会被调试系统常驻拖慢；代价是没有在 Capture 开始前发生的历史。这是可理解的产品边界：没有录制，就只能看现在。

### 方案 B：打开 Rewind 面板时自动开始 Capture

作者一打开专门的 Capture/Rewind 页面就自动保留最近过程，能少一次点击，也符合“我正在排查”时的直觉；代价是打开该页面本身就会启动高频录制，性能成本仍存在，只是范围缩小到这个页面。本 change 不把它作为默认行为，但新的 interest/detail 模型允许后续以独立产品决策加入。

### 方案 C：继续全程记录所有细节

作者可以随时回看之前的所有信息，但每个角色每 tick/表现帧都产生成本，Editor 又必须持续重建历史。对单角色 demo 都已造成明显掉帧，多个角色只会更差，不能继续作为正式路径。

### 方案 D：只降低刷新频率或缩小 Buffer

实现最少，但 runtime 仍持续构造事件，Editor 仍反复复制和分析一整段历史，最多推迟掉帧出现。它不能保证 Graph、Timeline 双开时的稳定性，也不能解释 Capture 的数据语义，因此不采用。

## 停止条件

- 若现有 diagnostics producer 无法在不新增第二套 gameplay 状态、重新采样 Timeline 或读取 runtime clone 的前提下提供当前状态与边界事实，停止实施并说明缺失的正式 producer contract。
- 若 target registry 无法在不保留 always-on Buffer 或不建立平行 target 的前提下支持 interest 的 acquire/release，停止实施并说明需要先调整的 registry 生命周期。
- 若当前 UI Toolkit 窗口无法在 shared provider + local binding 下只重绘变更 element，且必须保留每帧全量 overlay 才能正确显示，停止实施并说明该 UI 边界的取舍。
