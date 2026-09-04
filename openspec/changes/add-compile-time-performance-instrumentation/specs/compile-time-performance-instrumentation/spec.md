## Purpose

定义一套可复用的编译期性能织入能力，让业务方法以少量声明获得统一的阶段耗时、调用点身份和运行上下文，并在不改变 Gameplay 结果的前提下接入现有性能采集工作流。

## ADDED Requirements

### Requirement: 性能探针必须由显式声明形成唯一调用点

性能探针 MUST 只能由明确声明的业务方法产生。每个声明 MUST 指向一个现有 Performance Metric，并在所属程序集、类型、方法签名和声明身份的闭包内生成稳定且唯一的调用点身份。声明未知 Metric、重复调用点或身份无法稳定生成时，构建 MUST 在 Player 生成前失败。

#### Scenario: 为正式阶段声明探针

- **WHEN** 业务方法声明一个合法 Metric 的性能探针
- **THEN** Capture Player MUST 为该方法生成一个可在报告中定位的调用点身份
- **AND** 该调用点 MUST 复用该 Metric 的名称、父级、采样范围和聚合语义

#### Scenario: 探针引用未知 Metric

- **WHEN** 探针声明引用不存在或已删除的 Metric
- **THEN** 构建 MUST 报告程序集、类型、方法和 Metric 身份
- **AND** MUST 不生成无父级或临时名称的 Marker

#### Scenario: 两个声明产生相同身份

- **WHEN** 两个探针声明在同一构建闭包内产生重复调用点身份
- **THEN** 构建 MUST 失败并列出全部冲突声明
- **AND** MUST 不按声明顺序覆盖其中一个调用点

### Requirement: 织入必须保持业务方法语义

织入后的方法 MUST 保持原有参数、返回值、异常传播、调用次数和状态写入语义。正常返回和异常退出都 MUST 完成对应性能范围的结束记录。无法保持这些语义的目标方法 MUST 在构建期失败，MUST 不静默跳过、部分织入或生成 fallback。

#### Scenario: 方法正常返回

- **WHEN** 被织入方法正常返回任意受支持的返回值
- **THEN** 调用方 MUST 收到与未织入版本相同的返回值
- **AND** 性能范围 MUST 在返回前后完整闭合

#### Scenario: 方法抛出异常

- **WHEN** 被织入方法抛出原有异常
- **THEN** 异常类型、实例和传播边界 MUST 与未织入版本一致
- **AND** 性能范围 MUST 被标记为异常结束且不得吞掉异常

#### Scenario: 目标方法不受支持

- **WHEN** 探针指向 async、iterator、abstract、extern、构造函数、by-ref return 或其它无法安全保持语义的方法
- **THEN** 构建 MUST 明确指出不受支持的方法及原因
- **AND** MUST 不生成一个看似成功但没有完整范围的 Player

### Requirement: Metric、Marker 和调用跨度必须共享一个身份来源

运行时 Marker、调用跨度和采集器 MUST 使用同一 Performance Metric Catalog。业务 Owner、织入器和 Capture Agent MUST 不分别维护同名 Metric 的字符串清单。一个调用跨度 MUST 至少记录调用点身份、Metric 身份、开始所在 RenderFrame／LogicTick、可用的 Actor 身份、耗时和结束状态。

#### Scenario: 同一方法被多个角色调用

- **WHEN** 两个 Actor 在不同表现帧调用同一个织入方法
- **THEN** Capture 产物 MUST 保留两个调用跨度的独立时间和上下文
- **AND** 报告 MUST 能按 Metric 汇总并按调用点、Actor、RenderFrame 与 LogicTick 细分

#### Scenario: 嵌套父子阶段

- **WHEN** 一个已织入父方法调用另一个已织入子方法
- **THEN** Marker 和调用跨度 MUST 保留父子关系
- **AND** Analyzer MUST 不把父级 Inclusive Duration 与子级 Duration 相加为额外耗时

### Requirement: 上下文必须通过少量正式接缝提供且不得猜测

性能上下文 MUST 能表达 RenderFrame、LogicTick、Actor identity、Program identity 和 Pipeline identity。上下文 MUST 由 Gameplay Tick、Presentation Actor 或其它明确的运行根在正式边界设置；普通织入方法 MUST 只读取当前上下文，MUST 不扫描场景、读取 Transform、推断最近 Actor 或创建第二个 Tick。缺少某项上下文时，产物 MUST 明确标记不可用，而不是借用上一次值。

#### Scenario: 表现帧执行一个 Actor

- **WHEN** Presentation Actor 根建立当前 Actor 和 RenderFrame 上下文后调用多个织入方法
- **THEN** 这些方法的跨度 MUST 带有同一个 Actor 与 RenderFrame
- **AND** 业务代码 MUST 不需要在每个下游模块传递诊断对象

#### Scenario: 一个表现帧补多个 LogicTick

- **WHEN** 同一 RenderFrame 内执行多个 LogicTick
- **THEN** 每个跨度 MUST 记录实际所属 LogicTick
- **AND** MUST 不把 RenderFrame 编号当作 LogicTick 编号

#### Scenario: 没有 Actor 上下文的全局阶段

- **WHEN** 全局 Gameplay 或 Session 阶段没有 Actor identity
- **THEN** 跨度 MUST 使用明确的全局上下文状态
- **AND** MUST 不把前一个 Actor 的 identity 复制到该阶段

### Requirement: Capture 期间的调用跨度必须固定、有界且不阻塞业务

调用跨度记录 MUST 只在显式 Capture 窗口启用。记录期间 MUST 使用预先确定的容量和固定布局，MUST 不为每次调用创建托管对象、动态集合、字符串或等待 Writer／Controller。容量耗尽、序列断裂或写出失败 MUST 使当前 Capture 进入结构化 Faulted，并保留已写入的可验证证据。

#### Scenario: Capture 尚未开始

- **WHEN** Player 处于 Smoke、Replay 或等待 Capture 命令阶段
- **THEN** 织入探针 MUST 不写调用跨度文件
- **AND** Gameplay 执行结果 MUST 与没有性能插件时一致

#### Scenario: Capture 缓冲区耗尽

- **WHEN** 调用跨度数量超过 Profile 声明的固定容量
- **THEN** Capture MUST 进入 Faulted 并记录容量、调用点和最后有效序号
- **AND** MUST 不扩容、不截断后伪装为 Completed

#### Scenario: Writer 暂时变慢

- **WHEN** 后台 Writer 不能及时消费调用跨度
- **THEN** Unity 主线程 MUST 不等待 Writer 或 Controller
- **AND** 采集结果 MUST 通过有界队列故障语义报告丢失或失败

### Requirement: 业务接入必须保持声明式且不建立第二性能链路

业务接入 MUST 只包含探针声明以及必要的少量运行根上下文接缝。Gameplay、Pose Graph、Simulation 和 Presentation 代码 MUST 不直接创建 Session、Packet、Recorder、Writer、Controller 或外部采集进程。现有 Performance Capture workflow MUST 继续是唯一的 Player、Agent、Controller、Capture staging、manifest 和报告入口。

#### Scenario: 新增一个表现阶段

- **WHEN** 新阶段需要进入性能报告
- **THEN** 业务 MUST 只增加该阶段的正式 Metric／探针声明及必要的 Owner 接缝
- **AND** MUST 不新增领域专属 Capture Agent、Controller、CSV writer 或 MCP 入口

#### Scenario: 删除旧 Marker Owner

- **WHEN** 旧的手写 Marker Owner 被删除或替换为织入探针
- **THEN** 旧 Marker、Recorder 注册项和无消费的 Metric MUST 同步消失
- **AND** MUST 不保留旧名称的兼容占位项

### Requirement: Disabled 与 Capture Player 必须形成可证明的不同闭包

普通发布或纯性能基线 Player 未启用织入时，业务程序集 MUST 不包含性能探针 Attribute、织入 Runtime 引用、调用点身份或生成探针代码。Capture Player 只能包含当前 Build Request 选择的探针集合、Metric 和采集模式。关闭能力 MUST 通过编译输入和产物 Gate 证明，MUST 不使用运行时 bool、空实现、Linker 推测或构建后删除代替。

#### Scenario: 构建纯性能基线

- **WHEN** Build Request 未启用性能织入
- **THEN** ILPostProcessor MUST 不修改业务程序集
- **AND** Managed 与 IL2CPP 产物 MUST 通过零 Attribute、零 Runtime AssemblyRef、零探针身份和零生成调用的检查

#### Scenario: 构建 Span Capture Player

- **WHEN** Build Request 选择 Span 模式和一组合法探针
- **THEN** Player manifest MUST 保存精确探针集合、Metric Catalog revision、织入版本、采集模式和调用跨度布局
- **AND** Player MUST 不包含未选择的领域探针或分析器程序集

### Requirement: 织入身份必须进入现有 Capture 与比较闭包

Performance Player、Run Request、Capture manifest、调用跨度产物和报告 MUST 使用同一个 Instrumentation identity。该 identity MUST 包含探针 manifest、Metric Catalog、织入版本、采集模式、跨度布局和相关 Build identity。任一身份不同的 Capture MUST 被 Comparer 拒绝直接做性能差值。

#### Scenario: 完成一次带调用跨度的 Capture

- **WHEN** Smoke、Replay、Capture 按既有 workflow 成功完成
- **THEN** 同一 Capture 目录 MUST 包含调用点 manifest、跨度数据、文件 hash 和与顶层 manifest 的闭包引用
- **AND** 报告 MUST 能从阶段 Metric 定位到具体方法调用点及其 Actor／Frame／Tick 证据

#### Scenario: Marker-only 与 Span Capture 比较

- **WHEN** Baseline 使用 Marker-only 而 Candidate 使用 Span 模式
- **THEN** Comparer MUST 报告 Instrumentation identity 不一致并拒绝性能差值
- **AND** MUST 不把采集模式造成的成本解释为 Gameplay 回归

### Requirement: 插件必须在 Unity 2022.3 Editor 与 IL2CPP Capture Player 可用

首版能力 MUST 支持项目当前的 Unity 2022.3 LTS Editor、Windows x64 IL2CPP Performance Player 和既有 AOT 构建闭包。插件 MUST 不要求把项目升级到 Unity 6，也 MUST 不依赖只在其它 Unity 大版本存在的运行时接口。未列入首版目标的 Unity 引擎、第三方或 HotFix 程序集 MUST 不被静默修改。

#### Scenario: Unity 2022.3 构建 Capture Player

- **WHEN** 通过当前 Performance Player workflow 构建 Unity 2022.3 Windows x64 IL2CPP Capture Player
- **THEN** 选定业务程序集 MUST 完成织入并被 AOT 编译
- **AND** 生成的 manifest、Metric 与调用点 identity MUST 可被现有 Host 读取

#### Scenario: 目标程序集不在首版范围

- **WHEN** 第三方、Unity 引擎或 HotFix 程序集没有进入明确的织入程序集集合
- **THEN** 构建 MUST 在 manifest 中明确列出未织入范围
- **AND** MUST 不宣称这些程序集已经具备自动诊断能力
