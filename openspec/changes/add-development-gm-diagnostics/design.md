## Context

目标是让作者在本地与双端使用相同方式观察问题：先明确正在看哪个客户端、哪个角色，再选择采什么，最后能从结论跳到原始帧和动作事件。GM、采样与诊断有不同输入输出，不能把它们写成一个窗口里的大段业务逻辑。

当前 Action 异常与 Foot 预测装配缺失是不同问题。已有修复提交 `d3e51a5` 只接回未来位移源，`9465996` 只将领先量改为 8 Tick，`709f280` 只补充脚步失败上下文；本变更不扩大这些修复。

## Goals / Non-Goals

- GM 首先交付可用的运行信息、目标选择和诊断订阅入口，后续阶段通过命令模块接入采样与结果定位。
- 采样在 Editor 和 Development Player 使用同一个状态机、字段定义、封口和写盘模块。
- 诊断复用原始记录、现有 Foot Analyzer 和唯一 Publisher，输出有身份、有覆盖范围的结论。
- 不修复 Action 生命周期，不修改 IK/FBBIK、Foot 评分或网络预测政策，不建设远程运维、账号权限平台或任意脚本控制台。
- 第一批不提供玩法状态修改、联机暂停、联机输入回放或跨进程一键录制。

## Decisions

### 1. 分开入口、控制、采集和分析

| 模块 | 输入 | 输出 | 不负责 |
| --- | --- | --- | --- |
| GM 界面 | 作者选择、输入焦点、命令参数 | 带精确目标的命令请求、结果展示 | 查询物理、改变骨骼、写 CSV、计算评分 |
| 共用工具控制服务 | 命令合同、目标注册信息、订阅请求 | 执行结果、采样状态、只读目标视图 | 拥有第二份角色状态或解释玩法图 |
| 运行时采样 | 正式 Trace、已提交帧快照、失败上下文 | 有界记录、封存包、完整性说明 | 重新求值动画、重新查询地面、修复失败 |
| 离线诊断 | 精确封存包和分析配置 | 摘要、事件索引、原始帧引用 | 控制运行中的角色、重新模拟以补齐缺失证据 |

调用链固定为：`GM / Editor 适配器 -> 共用控制服务 -> 已注册诊断 provider -> 采样 Writer -> 封存包 -> 现有 Analyzer / 查询适配器 -> 只读视图`。未来加入新的工具入口时只增加适配器。

### 2. 每个进程先管理自己的目标

保留现有 Gameplay 与 Animation 注册表及 provider 所有权。共用目标目录通过同一个 Character runtime identity 关联它们，只维护元数据和可用能力，不缓存第二份可写状态。

目标描述必须包含：进程运行身份、开发产品/启动批次、业务 SessionId、Peer/进程角色、ActorId、Character runtime identity、LocalOwner/本进程模拟角色身份、Numeric Target、Program/Projection/Pipeline/Model 身份。现有 diagnostics context 的 Guid 不能冒充业务 SessionId；HostInstanceId 和显示名也不能作为跨进程身份。

GM 顶部始终显示当前进程和目标。打开时不按注册顺序附着第一个角色。目标结束后保留只读 Ended 状态；重新启动后的同名角色必须有新运行身份，不自动接管旧记录。

Network Test 启动脚本把本次启动批次显式传给两个客户端；单机 Editor 启动由开发工具装配根生成本次进程运行身份。该身份只用于工具和记录，不改变网络模型身份，不新增游戏协议字段。

### 3. 命令模块可扩展，执行器不识别具体业务

命令描述包含稳定 Id、中文名称、所属分类、参数 schema、目标范围、操作类别、可用能力与结果 schema。模块通过显式装配注册处理器，禁止扫描程序集发现任意方法、执行 C# 字符串或用一个大 switch 实现所有工具。

阶段一安装 `runtime.targets.list`、`runtime.target.describe` 和 `diagnostics.interest.set`。阶段二才安装 `diagnostics.capture.start/stop/status`。界面从同一目录生成菜单/参数表单，文本命令也通过同一参数解析与执行器；作者不需要记住命令字符串。

GM 的命令目标属于本入口，不能覆盖 Editor Debug Session 的选择与页面 Follow/Pin。查询和绘制只读已发布状态；文件扫描、解析、Build、分析和封口不得放进 `OnGUI`、`OnInspectorGUI` 或页面重绘回调。

查询、诊断控制和未来的玩法控制明确分型。注册了某种 Session Debug Port 不代表所有命令都可以公开。联机目标不得把全局 `Time.timeScale`、Tick 暂停或直接写 Transform 暴露为普通诊断命令。

### 4. 输入焦点属于设备适配边界

GM 使用正式 Input System 配置与开发 UI 资产。交互焦点被 GM 占用时，本地设备适配器按 Program input catalog 生成 neutral gameplay 输入，不产生 Attack/Dodge 请求；相机也不消费被 UI 占用的鼠标移动。已有 committed request、历史输入与网络队列不被清空或修改。

焦点释放时不得将 UI 点击或按键补发为动作。该策略在现有设备到 portable input 的边界共用，GM 不分别操作 Fixed、Float32、Rollback 的内部状态。打开窗口不暂停 Session，不等同于停止采样；窗口关闭也不擅自终止后台采样。

### 5. 迁移唯一采样核心，保留成熟数据合同

从 `CharacterFootLandingPredictionSampler` 提取已经存在的帧关联、字段构造、CSV Writer 与封口流程，去掉对 UnityEditor、固定 Actor 名字和场景扫描的依赖。Editor 保留菜单、资产跳转和本地调试控制适配，不再保存第二份 Writer、字段常量和采样生命周期。

采样目标在 Start 时锁定，可选择本进程一个或多个已注册 Actor。采样按频道获得有独立 owner 的 interest，Stop/失败/销毁只释放自己的订阅，不关闭其它观察者。未开启采样时不构造完整骨骼页或额外文件数据。

统一记录身份区分：启动批次、采样运行、进程、业务 Session、Actor、runtime instance、Program/Projection revision、逻辑 tick/sequence、表现 RenderFrame、Body sample tick/alpha、Completion 和结果类型。Foot/Goal/FBBIK/最终写入必须来自同一已提交完成身份，不能在下一帧读取实时 Transform 拼回旧帧。

Action 频道记录已有正式命令进入、替换/撤销、Inbox 消费、生命周期快照与失败边界；Select、Sample、Complete、Release 携带同一 PlaybackId、Producer/Generation、ActionInstanceId、EventId 与 tick/sequence。只冻结边界事实，不增加第二个动作执行器。

采样状态固定为 Idle、Capturing、Finalizing、Sealed 或 Failed。主线程只完成有界冻结与入队；文件 IO 放在后台。后台队列溢出、目标结束或写盘失败必须留下明确的终止原因和覆盖区间，不阻塞 Gameplay，不假装记录完整。通用 Trace store 原有的完整 segment 容量淘汰政策保持不变；持久写盘的丢失范围必须单独记录。

失败信息可以记录 Attempted 阶段和失败命令，但不得给失败帧伪造 Seal、Goal 或最终骨骼。失败后的重复日志与首个失败分开计数，查询优先定位第一个完整失败上下文。

### 6. 数据封存与离线诊断

新采样统一写入开发工具配置声明的可写根目录，目录使用有界短标识，完整业务身份写在清单内。禁止写入已校验的 Network ProductRoot、Assets 或需要 AssetDatabase 的路径。具体根目录是正式开发配置，不做多路径探测或写盘 fallback。

每个采样包具有唯一清单，列出目标、频道、文件/hash/schema、开始结束边界、完整性和错误。Foot 原始 `samples.csv`、`ground-path-geometry.csv` 的字段数学保留；外层清单关联多目标文件。旧的单机新建路径与重复 Writer 在迁移完成时删除，已封存历史证据不改写。

Player 停采只完成记录封存，不在游戏帧内执行 Analyzer。Editor 导入或接收精确包后调用既有唯一 Finalizer，完整包继续自动分析；同一输入、Analyzer 版本与配置只发布一份结果。封存历史 CSV 如需新报告，按现有 storage spec 在显式新目录重新分析，不增加旧格式兼容读取。

诊断工具统一提供 `summary/events/detail/frame`：Foot 调用已有 Analyzer/Store/Reader；Action 按实际边界事件和已有生命周期快照展示时间线。没有记录到 Select 只能报告“当前覆盖内缺少 Select 证据”，除非完整边界记录支持更强结论。报告不能根据异常字符串猜测修复方法。

两端 Gameplay 对比按相同 Actor、canonical tick、模型/程序身份关联；Presentation 对比同时展示 Body sample、Clip/phase、branch revision 与采样间隔，不要求 RenderFrame、Completion 或最终骨骼逐位相同。先比较同一帧内 Source Pose、Goal、IK 和 Final Pose 的变化，再解释客户端之间的差异。

### 7. 开发版装配与退出

GM UI 与操作入口只显式装配进 Gameplay Lab 开发场景，Local Float32、Local Fixed、Rollback 共用同一 UI/控制模块。网络 transport 和纯.NET Relay 不依赖 GM UI。非 Development Player 不允许带入 GM 入口或配置，正式构建验证必须拒绝误装。

target 结束、退出 Play、进程正常退出时，控制服务完成订阅释放与已接收记录的封口。异常进程退出留下未 Sealed 的包，离线工具明确识别不完整包，不自动修补为完整。

## 有业务价值的取舍

| 决策 | 方案一 | 方案二 | 本草案范围 |
| --- | --- | --- | --- |
| GM 首版能力 | 观测与采样控制：直接服务问题定位，不触碰玩法权限；不能传送或修改属性 | 同时包含玩法命令：更快布置测试场景；每条命令都要接入合法 Source/Session 业务路径并明确联机权威 | 方案一；玩法命令尚待用户明确 |
| 双端操作 | 每客户端本地面板：职责和身份直接，不能一键同步开始两端录制 | 独立工具进程协调两端：操作集中、可以共同触发；需要正式工具协议、连接生命周期和操作范围合同 | 方案一；记录保留关联身份，但不预埋伪远程能力 |
| 分析位置 | 离线复用现有 Analyzer：不争用游戏帧预算，现有规则容易保持；停采后需交给分析端 | Player 内异步分析：现场就能看结论；需要可部署分析包、计算预算和生命周期管理 | 方案一；GM 能看封存状态，完整结论由诊断工具给出 |

这些取舍说明能力与成本，不替用户决定后续实施优先级。任一范围调整都应更新正式合同，而不是保留两套并行实现。

## Migration Plan

1. 交付 GM、身份与命令目录以及输入焦点边界，提供实际可用的只读运行信息。
2. 迁移并接通同一个运行时采样核心，GM 与 Editor 接入同一 Start/Stop/Status。删除旧 Host 扫描和重复采样实现，保留原始字段与现有录制/回放边界。
3. 接通封存包、现有 Foot Finalizer/Reader 与 Action 事件查询，让 Editor/MCP/诊断界面消费同一结果。
4. 各阶段按实际完成状态更新任务，独立提交；不将尚未安装的能力记入 current specs。

## Risks / Open Questions

- 用户尚未确定 GM 首版具体操作清单；本草案不授权玩法状态修改。
- 当前 Foot 采样器包含多种 Editor 关联和大量字段。迁移必须以原字段、帧关联和现有封存数据为边界，不顺便重写 Foot 或评分。
- active 存储与评分 change 尚未归档；实施前核对它们的实际最终 schema，若冲突则明确提出，不覆盖已改对的实现。
- 本提案的 Foot storage MODIFIED delta 依赖 active storage 合同先安装或显式协调合并；Player 封存与离线提交的边界不能在两份 change 中保留相互矛盾的自动执行要求。
- 当前缺少适用于 Player 的共用 UI 焦点合同，需要作为正式输入增量实现；不能只关闭某个角色组件解决点击穿透。
- 单端暂停联机模拟和跨进程协调没有安装的正式合同，本提案不公开这些命令。
- 现有 Action 错误保持已知未修复；工具应完整保留首个失败及之前的证据，不能吞错让它“看起来通过”。
