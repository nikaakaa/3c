# 可琳录制与诊断采样

## 入口与输出

现有 `character.fixed_input_trace` 的 `record_start` 同时启动正式输入录制、足部 full 采样、表现 full 采样和 Runtime Diagnostics。`record_stop` 保存输入并结束采样。`diagnostic_replay_start` 使用同一组采样能力回放已有输入。

普通 Play 不自动启动这些采样。完整诊断含额外采集和文件保存开销，不把开启完整诊断时的帧率当作未采样的性能基线。

`Diagnostics/CharacterRuntimeTraces/<trace_id>/<capture_id>/summary.json` 关联原始输入文件、启动记录、足部采样身份与目录、表现采样身份与目录。其同目录保留 `events.jsonl` 和 `source-map.json`。生成的 CSV、schema 和 manifest 继续由原有 generated sampling 生成，不另写一套逐帧 CSV。

## 数据范围

| 问题 | 数据来源 |
| --- | --- |
| E、普攻、方向与闪避输入 | 原始 canonical input 文件，保留每个逻辑 Tick 的输入载荷 |
| Rush 分支与接招 | 已提交的状态转换、端口值、动作阶段、动作结果与退出原因；源映射定位到实际作者节点 |
| 接招或移动窗口 | 已提交的 ActionWindow fact：窗口 ID、类型、动作实例、开始与结束 Tick、digest；窗口查询还记录当前是否成立 |
| 动画停在末帧或退出时机 | 动画选择、生产者采样、完成与释放事件；生成表中的片段时间、持续时间、循环、权重、状态机与过渡 |
| TurnBack、Root Motion、PIK | 原有 full 足部表及 ground 表；表现主表的身体位置、位移、速度、采样系数；动画来源表的左右脚权重 |
| 镜头 FOV、震动、拉近 | 最终相机输出、效果来源、动作实例、代数、循环、资源、请求权重、存活状态、剩余时间及退出原因 |
| 镜头时间 | presentation delta、scaled/unscaled delta、暂停状态、时钟模式、Owner/LocalAvatar 倍率、是否可用及倍率来源 |
| 启动慢 | 请求、等待 Play、进入 Play、角色会话就绪、Pose 资源就绪、采样就绪的单调时钟累计耗时 |
| 报错中断 | 保留已读 RuntimeTrace、错误消息/堆栈/渲染帧、失败原因、流完整性；表现采样收到异常后以 Faulted 结束 |

值采样直接消费现有 Fixed 求值输出。Fixed 标量同时保存原始整数和便于阅读的数值；向量是诊断显示精度。未表达的值类型显式标成 TypeOnly 并计数。达到端口采样上限或事件读取容量时标记数据不完整，不伪装成完整通过。

相机 RemainingSeconds 不有限时有明确的不可用标记，CSV 留空。当前角色共用 Tick 时钟，额外 Owner/LocalAvatar 倍率为 1，来源为 SharedTickClock；独立角色时间倍率没有实现。效果 Weight 是请求权重，不是把包络求值后的幅度冒充权重。

## 实现路径

- `CharacterFixedInputTraceWorkflow`：录制/回放生命周期和输入文件关联。
- `CharacterFixedInputRuntimeTraceCapture`：订阅已有 Runtime Diagnostics，停止或中断时导出事件、源映射与汇总。
- `FixedCharacterRuntimeDiagnosticsAdapter`：把已经提交的技能端口、动作、窗口及效果事实送入同一个 Runtime Diagnostics。
- `CharacterNativeCameraCaptureFrame` / `CameraEffectContribution`：声明生成采样字段及 camera-effects 子表。
- `CharacterInputStartupCapture`：跨 Editor domain reload 保存启动阶段时间。

新增相机字段通过只读事实视图同步采集，不在每帧复制效果列表。JSON 写盘在 Editor 停止采集阶段执行。

## 已获得的证据

首次完整运行：录制 `757f243033414fc7b123c97e2fcb0d70`，2716/2716 帧；无运行错误，无动画/Timeline 时间倒退。相机 2716 帧有效输出、984 次请求。生成表现主表 2716 行、效果表 2746 行、来源表 4929 行；足部 full 2716 个相对帧。

这次启动从请求到进入 Play 为 34.38 秒，角色会话就绪为 133.28 秒，Pose 资源就绪为 139.59 秒，采样就绪为 139.61 秒。这是阶段耗时证据，还没有定位角色会话准备内部最慢的具体函数。

该录制不能证明 E 长按行走的视觉效果，也不能代替作者对脚部同步和镜头手感的验收。

补齐技能采样后的再次运行同样完成 2716 帧：57,763 个事件、29,114 个值样本、2,417 个窗口样本、19 个动作结果；值采样上限触发 0、未表达值类型 0、运行错误 0。两次运行的输入与逐帧身体轨迹无差异。新增事件和散列字段使 trace hash 改变，旧比较器据此报了一项汇总差异；因此证据格式升为 `/2`，后续只选同版本证据作自动基线，保留原来的旧版结果。

实际产物：`Diagnostics/CharacterRuntimeTraces/757f243033414fc7b123c97e2fcb0d70/b61765a793c6496faed027db40a3eec2/summary.json`。已确认其中足部/表现目录均存在，回放 Proof 的主表路径指向 `full.csv`，不再误指效果子表。

本次没有另造新的用户输入录制；普通录制入口的自动联动已编译，端到端落盘证据来自现有录制回放。
