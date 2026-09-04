## Why

当前性能工作流已经能启动可复现的 IL2CPP Player、采集 Unity Profiler 与 WPR/xperf 数据，但 Gameplay、Session、Simulation 和 Pose Graph 仍需要各自声明 `ProfilerMarker`、维护 `Begin/End` 或性能 Sink。这样既难以覆盖真实方法调用，又让性能代码进入业务实现，当前 30–40 FPS 的问题仍只能看到一层粗粒度阶段。现在需要补上一个可复用的编译期织入层，让选定方法自动获得统一 Marker、调用点身份和 Actor／Frame／Tick 上下文。

## What Changes

- 新增面向 Unity 2022.3 LTS 的通用性能织入插件，使用 Unity `ILPostProcessor` 与 Unity 随附的 `Mono.Cecil`，不引入 Fody、PostSharp、DynamicProxy 或运行时反射代理。
- 新增稳定的性能探针声明、Metric 绑定、织入结果 manifest 和诊断构建 profile。业务方法只增加极少量声明，插件自动生成异常安全的 `ProfilerMarker` 与可选固定容量调用跨度记录。
- 让织入点复用现有 `PerformanceMetricCatalog`，Marker 名称、父子关系、采样范围和预算仍只有一个来源；删除业务 Owner 与采集器之间重复的 Marker 字符串和手写探针注册。
- 在 Gameplay Tick、Presentation Actor 根和必要的 Simulation 根建立少量正式上下文接缝，统一提供 RenderFrame、LogicTick、ActorId、Program／Pipeline identity；织入方法直接读取当前上下文，不在 Gameplay 各层铺设桥接代码。
- 将当前手写的 Gameplay、Session、Simulation、Presentation 和 Pose Graph 性能包围迁移到织入探针，删除并行的手写 Marker／性能 Sink 路径；不改变业务计算、事务顺序、Pose 结果或 Gameplay 状态。
- 将调用点身份、方法身份、Metric、上下文键和采样模式纳入 Performance Player BuildIdentity，并由现有 Capture Agent 在同一 Warmup／Capture 边界启动固定容量跨度流；不新增 Player、Controller、WPR、xperf 或顶层 Capture 路径。
- 现有性能报告增加“Metric 阶段 → 具体方法调用点 → Actor／RenderFrame／LogicTick”的关联证据，并继续把 Unity Profiler、WPR/xperf 与 AOP 记录分开解释，避免把采样次数当成耗时。
- 普通发布构建不定义织入符号，ILPostProcessor 不修改业务程序集，且发布 Gate 必须证明零性能 Attribute、零织入 Runtime 引用和零生成探针残留；不以运行时开关冒充关闭。
- 插件首版只织入明确声明且受支持的同步方法；async、iterator、abstract、extern、构造函数和无法保持异常／返回语义的方法必须在构建期报错，不静默跳过或生成 fallback。

## Capabilities

### New Capabilities

- `compile-time-performance-instrumentation`: 定义 Unity 编译期性能探针织入、Metric 与调用点身份、零分配上下文、固定容量跨度、BuildIdentity 闭包及与现有 Performance Capture workflow 的接入合同。

### Modified Capabilities

无。当前 `openspec/specs/` 没有独立的性能织入 capability；现有 `btsmtl-runtime-diagnostics`、generated diagnostic sampling 和 Performance active changes 继续各自拥有行为 Trace、事实采样和外部采集职责，本 change 只定义它们之间不能混淆的性能织入边界。

## Impact

- Unity 包：新增可复用的 Runtime contracts 与 Editor-only ILPostProcessor；使用 Unity 2022.3 提供的 `Unity.CompilationPipeline.Common`，以及项目已有的 `com.unity.nuget.mono-cecil@1.11.6`，运行时使用 `Unity.Profiling`。
- 3C Runtime：迁移现有性能 Marker、`ISimulationPerformanceSink` 和 `PerformanceCaptureTelemetry` 的职责，使 Gameplay 只保留少量上下文接缝与探针声明，不持有性能实现细节。
- 3C Editor／Build：现有 Performance Player、Catalog、Capture Agent、manifest、Comparer 和 Disabled Cecil／IL2CPP Gate 增加织入版本、探针 manifest 和跨度产物闭包；不创建第二 Controller 或第二产物根。
- active `add-gameplay-performance-capture-workflow`：需要把织入 manifest、跨度流和新的 BuildIdentity 字段接入既有 Smoke／Replay／Capture workflow；其 WPR/xperf 与报告职责不变。
- active `add-generated-diagnostic-sampling-framework`、`extract-generated-diagnostic-sampling-package`、`add-schema-driven-diagnostic-analysis`：不修改其 Fact Root、DiagnosticEvent、Schema、Packet 或 Host-only Operator；性能跨度只作为 Performance Capture 子产物，不进入 generated sampling packet 或 BTSMTL RuntimeDebugSession。
- 业务风险：开启调用跨度会增加 Capture Player 的采集成本，因此 Marker-only、Span 和 Disabled 必须形成可识别的 BuildIdentity，不能拿不同织入模式直接做性能差值。
