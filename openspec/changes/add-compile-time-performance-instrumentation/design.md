## Context

见 [proposal.md](proposal.md)。当前 3C 已有一条性能采集链：`PerformanceMetricDefinition` 和各领域 Catalog 定义指标，Unity Capture Agent 使用 `ProfilerRecorder`，`PerformanceCaptureTelemetry` 记录 LogicTick／Simulation 阶段，独立 Controller 负责现有 Smoke、Replay、Capture、WPR、xperf 和产物发布。业务侧仍在 `GameplayTickSystem`、`SimulationSessionHost`、Presentation、Pose Graph 和 Simulation Sink 中手写 `ProfilerMarker`、`Begin/End` 与时间数组。

现有 `btsmtl-runtime-diagnostics` 已规定行为诊断必须只读、使用 Source Map／Trace，并由 `RuntimeDebugSession` 管理；`add-generated-diagnostic-sampling-framework` 与 `extract-generated-diagnostic-sampling-package` 已规定 Fact Root、`DiagnosticEvent`、typed packet 和 Disabled 零闭包；`add-schema-driven-diagnostic-analysis` 只在 Host 侧分析 sealed artifact。它们都不是方法耗时织入的归属。本 change 只给现有 Performance Capture 增加一个可复用的调用点层，不把三套诊断数据合成万能 DTO。

本机 Unity 版本是 2022.3.62f2c1。Unity 安装目录提供 `Unity.CompilationPipeline.Common` 和 Unity IL Post Processing 合同；项目 PackageCache 已有 `com.unity.nuget.mono-cecil@1.11.6`。现有 Unity 代码已经使用 `Unity.Profiling`，因此不需要另买运行时 Profiler。

## Goals / Non-Goals

**Goals:**

- 让业务只声明需要观察的方法和少量运行根上下文，编译期生成统一的 Marker、调用点 identity 和异常安全的范围。
- 让一个调用点同时能被 Unity Profiler 观察，并在显式 Span Capture 中关联到 Actor、RenderFrame 和 LogicTick。
- 让现有 Performance Catalog、Player、Capture Agent、manifest、Comparer 和报告继续是唯一外部链路。
- 支持 Unity 2022.3 Windows x64 IL2CPP Performance Player，并以构建产物证明 Disabled Player 没有织入闭包。
- 在清理当前手写 Marker、Simulation Sink 和 `PerformanceCaptureTelemetry` 重复职责后，只留下一个性能运行时采集入口。

**Non-Goals:**

- 不织入所有方法，不修改 Unity 引擎、第三方包、`Assets/GameScripts/HotFix` 或未进入显式程序集集合的程序集。
- 不把 ILPostProcessor 变成通用业务 AOP 框架，不支持任意方法替换、运行时代理、表达式或脚本。
- 不修改 Gameplay、Pose Graph、Simulation、Foot Placement、Timeline、World Solver 或网络协议的业务算法和事务语义。
- 不把方法耗时 Span 写入 Generated Diagnostic Sampling 的 Fact Root packet，也不把它接入 BTSMTL `RuntimeDebugSession` Trace Store。
- 不在本 change 内自动化 RenderDoc、PIX、Nsight、RGP、VTune、PMC、Heap 全量跟踪或线上遥测。

## Decisions

### Decision 1: 使用 Unity ILPostProcessor、Mono.Cecil 和 Unity.Profiling 三层组合

新包固定拆成 Runtime Contracts、Runtime Capture 和 Editor Weaver 三层：

```text
Runtime Contracts
    -> Runtime Capture / Context / Scope
Editor Weaver
    -> ILPostProcessor + Mono.Cecil
Existing Performance workflow
    -> Player / Agent / Controller / Report
```

Editor Weaver 使用 `Unity.CompilationPipeline.Common` 的 IL Post Processing 合同；IL 修改使用项目已解析的 `com.unity.nuget.mono-cecil@1.11.6`。不直接引用 Unity 编辑器内部的 `Unity.Cecil.dll`，不复制 DLL，不引入 Fody、PostSharp、MethodBoundaryAspect 或 Castle DynamicProxy。运行时只使用 `Unity.Profiling` 的 `ProfilerMarker` 及项目已有的固定布局采集能力。

选择 ILPostProcessor 而不是 Fody，是因为目标是 Unity 2022.3 的程序集编译链和 IL2CPP AOT：织入发生在 Player 编译前，生成代码在普通 C#／IL2CPP 可见调用图内，不需要运行时加载、代理对象或反射。Fody 虽然也能改 IL，但它会再引入一套构建入口、版本和织入顺序；PostSharp 还不适合作为当前 Unity 运行链的基础依赖。运行时代理则无法覆盖 AOT 静态调用，也会改变对象和调用成本。

### Decision 2: 业务用一个探针声明，Metric Catalog 仍是唯一名称来源

Runtime Contracts 只提供一个最小探针声明，至少携带 MetricId；不在业务声明中复制 profiler name、parent、unit、budget 或报告字段。调用点 identity 由程序集 identity、声明类型、完整方法签名和声明序号规范化后生成稳定 hash；方法改名或签名变化视为新的调用点，不提供 alias。

现有 `PerformanceMetricDefinition`／领域 Catalog 继续是 Metric 的唯一来源。现有静态 Catalog 在 Performance Build 的准备阶段被规范化为一个显式 Weaver 输入，输入包含 MetricId、ProfilerName、ParentId、SampleScope、Unit、Aggregation 和 Catalog revision。ILPostProcessor 只消费这个已锁定输入，不扫描场景、不搜索最近 Catalog、不根据方法名猜 Metric。缺少或过期输入时，带探针的 Capture 编译失败。

这样既保留了现在报告使用的 `ProfilerName` 和父子关系，也不让每个方法 attribute 再写一份 Marker 字符串。一个 Metric 可以有多个调用点：Profiler 侧按 Metric 汇总，Span 侧按 PointId 分解，xperf 侧仍按原生函数和 PDB 给出机器级热点。

### Decision 3: 只织入显式声明的同步方法，并在 IL 层保持原方法签名

Weaver 只处理 Build Request 明确允许的业务程序集和带探针的方法。对每个目标方法，保持原有可见性、虚方法身份、参数和返回类型，在原方法体周围注入：

```text
scope = ProbeRuntime.Enter(pointId, marker)
try
    original body
finally
    ProbeRuntime.Exit(scope)
```

真实 IL 不依赖 C# 源码重写；所有原有 return 路径必须经过统一 Exit，异常路径必须经过 finally。Weaver 需要校验异常处理块、分支、局部变量和返回值，不能只在方法首尾插一对调用。生成的 Scope 是值类型，PointId、上下文快照和计时起点均为固定字段。

首版接受静态或实例的同步普通方法，允许探针方法位于泛型类型中，但方法本身不能是泛型方法；允许普通参数、`ref`／`out` 参数和普通返回值；拒绝 async、iterator、abstract、extern、构造函数、by-ref return、函数指针和无法重写异常／返回路径的方法。拒绝发生在编译期，并列出程序集、类型、方法和原因。第三方和 Unity 方法不通过“尽量织入”方式处理，避免构建成功但报告缺段。

选择直接修改原方法体而不是生成一个 Wrapper 方法，是为了保留虚派发、接口调用和现有调用图；选择明确拒绝复杂方法，是为了让报告中“已织入”表示真的覆盖了完整调用范围。对 async／iterator 可以以后建立专门的状态机规范，但不能在本 change 中用不完整的近似包装。

### Decision 4: Marker 与 Span 分成两个采集等级

同一个织入 Scope 提供两个构建等级：

- `MarkerOnly`：只进入 Unity Profiler Marker，使用 Catalog 的 profiler name 和父级结构；适合低扰动的阶段定位。
- `Span`：在 Marker 之外，将每次调用写入当前 Capture 的固定布局跨度缓冲；记录 PointId、MetricId、RenderFrame、LogicTick、ActorId（或明确的 Global）、开始时间、耗时、线程身份和结束状态。

Span 使用 numeric PointId 和结构化字段，不在每次调用中创建字符串、Dictionary、boxing、闭包或业务对象引用。缓冲和后台队列在 Capture 开始前按 Profile 容量建立，主线程只执行固定字段写入和非阻塞提交。容量、序列或 Writer 出错都进入当前 Capture 的 Faulted，不扩容、不丢掉错误后继续发布 Completed。

只用 Unity Profiler 的方案可以看到方法范围，但难以在一份稳定的运行产物中把每次调用直接关联到动态 Actor／LogicTick；每次都写 Span 又会明显扰动基线。因此 MarkerOnly 与 Span 分开建 identity，Span 只用于显式诊断 Player，不能和纯基线或 MarkerOnly 结果直接做差值。

### Decision 5: 上下文只在三个正式边界接入

通用 Runtime 不知道 `GameplayTickSystem`、Pose Graph 或 Actor 类型。它只提供一个无托管分配的上下文值类型和嵌套 Scope：

```text
Frame root       -> RenderFrame
Logic root       -> LogicTick
Actor root       -> ActorId + Program/Pipeline identity
Woven method     -> read current immutable snapshot
```

3C 只在现有正式根接入：Gameplay Tick 的 Frame／Logic 边界、Presentation Actor 的 Present 边界，以及需要独立 Logic root 的 Simulation Session 边界。接缝负责设置、恢复和清除上下文，不负责采样、写文件、启动外部进程或拼诊断 DTO。下游方法只通过织入 Scope 读取上下文，不新增参数传递和领域 Bridge。

上下文使用线程局部的固定状态和显式嵌套恢复，不使用 `AsyncLocal`、场景搜索、Transform、全局“最后一个 Actor”或线程间隐式传播。没有 Actor 的全局阶段使用 Global 标记；如果上下文缺少必要的 Frame／Tick，Span 明确记录不可用状态。这样不会把前一个 Actor 的 identity 错挂到下一个阶段。

### Decision 6: Weaver manifest 是编译产物，不是第二份配置

每个被处理程序集产生一个规范化的 point descriptor，最终由 Performance Build 聚合成 `instrumentation-manifest`。每个 descriptor 至少包含：

- PointId、MetricId、Catalog revision。
- 程序集 identity、类型和方法签名。
- 织入版本、采集等级和 Span layout revision。
- 原始方法的 source sequence point（若存在）及是否成功生成完整异常范围。

Manifest 是 BuildIdentity 和 Capture manifest 的子闭包。它用于让报告从 Metric 找到 Point，再找到程序集／方法／源码位置；运行时不读取方法名、字符串路径或反射类型。生成前对所有 point、Metric、父级和 catalog revision 做确定性排序并校验。

不选择只把方法名写进 `ProfilerMarker`，因为同一 Metric 的多个调用点会互相覆盖，且动态 Actor／Tick仍然无法落到结构化产物；也不选择运行时反射建立方法表，因为它会扩大 IL2CPP 闭包并让 Disabled 零残留难以证明。

### Decision 7: Span 作为现有 Capture Agent 的子产物

现有 `ThirdPersonPerformanceCaptureAgent` 继续是 Player 侧唯一 Agent。它在既有 Warmup 暂停边界完成以下动作：

```text
READY
  -> 校验 Catalog / Instrumentation identity
  -> Capture Start 时建立 Span buffer 与 Recorder
  -> Scenario capture 后封存 span 数据
  -> 将 span 文件和 manifest 交给现有 Controller / Host
```

Span 文件放进当前 `Library/Performance/Captures/<CaptureId>` staging，由顶层 manifest 通过文件 hash 引用。Controller、WPR、xperf、WPA 和既有 `metric-samples.csv` 不另起流程；Analyzer 以 Metric sample 负责整体分布，以 Span 负责调用点／Actor／Frame／Tick 下钻。父级 Inclusive 和子级 Duration 继续按现有统计合同解释，不能相加。

现有 `PerformanceCaptureTelemetry` 的 LogicTick／Phase 数组与手写 Simulation Sink 将迁移为同一 Runtime Capture 的根 Span／聚合视图。迁移完成后不保留“手写 phase telemetry + AOP span”双写；如果某个阶段没有合法同步方法边界，必须在该正式 Owner 增加一个明确探针点，而不是恢复另一套计时器。

### Decision 8: Disabled 通过编译输入和产物 Gate 退出

织入符号只由 Performance Build Request 显式提供。未启用时：

- 探针声明不进入业务程序集 metadata。
- Weaver 对业务程序集返回未修改结果。
- Runtime、Generated point type、Span layout 和 PointId 不进入 Player。
- Build／IL2CPP Gate 检查 Attribute、AssemblyRef、生成类型和 identity 字符串均为零。

启用时只织入当前 Request 的程序集和声明，且把 Catalog、Probe manifest、Mode、Weaver version 和 layout revision 写入 BuildIdentity。不能在一个已构建 Player 中通过运行时 bool 把“已织入的 Span”伪装成 Disabled；运行时开关只负责 Capture Agent 在 Smoke／Replay 与 Capture 窗口之间控制记录，不能替代构建闭包。

### Decision 9: 只接入现有正式 Owner，不碰未收敛的旧类型

迁移顺序按当前架构真相执行：先接 Gameplay Tick、Session 和已完成收敛的 Presentation／Pose Graph Owner，再接 Simulation phase。active `refactor-character-pose-graph-architecture` 仍在删除旧 `PosePlanExecutionRuntime` 和旧 Executor，因此织入点必须落在它最终声明的 Program、Source、Constraint、Final Publication 和根协调边界；如果实现时 Owner 尚未收敛，暂停该部分并报告冲突，不给旧类加 wrapper。

`CharacterPoseDiagnosticsRuntime`、RuntimeDebugSession、Foot generated sampling Event 和 Host-only Operator 不作为性能探针目标。Generated Diagnostic Sampling 继续在成功 Commit 边界读取真实 Fact Root；AOP 只测方法范围，二者通过顶层 Capture manifest 的不同子闭包关联，不共享 packet、Session 或业务事实。

### 与 current spec 和 active change 对账

- `btsmtl-runtime-diagnostics`：本 change 的 Span 是性能证据，不是 Graph／Timeline／Blackboard Trace；不进入 RuntimeDebugSession，不改变 Source Map 或诊断 interest。
- `add-generated-diagnostic-sampling-framework`：继续拥有 Fact Root、Field、DiagnosticEvent、typed packet、Generated Program 和 Disabled sampling 闭包；本 change 不给它增加方法织入或 Span 字段。
- `extract-generated-diagnostic-sampling-package`：外部 `com.kk.generated-diagnostic-sampling` 依赖和 0.5.2 消费合同不改；性能插件不复制其 Generator、Host 或 Analyzer。
- `add-schema-driven-diagnostic-analysis`：Operator、Plan 和报告只读取已封存事实采样；它不解析 Span，也不承担 Capture 生命周期。
- `add-gameplay-performance-capture-workflow`：继续唯一拥有 Player、Agent、Controller、WPR、xperf、顶层 manifest 和 Comparer；本 change 只新增其内部 instrumentation 子闭包和一个 Span 子产物。
- `repository-ci-foundation`：基础 CI 仍不启动 Unity、Player、WPR 或性能 Capture；只需在正式实现后确认新包和精确工程路径符合既有仓库策略。

## Risks / Trade-offs

- [Span 每次调用会增加 Capture Player 成本] → 默认只对明确探针启用；MarkerOnly 与 Span 使用不同 BuildIdentity，报告不得跨模式比较；纯性能基线完全不织入。
- [属性声明仍会让少量性能合同进入业务源码] → 每个方法最多一个探针声明，业务不持有 Session、Packet、Recorder 或 Writer；外部方法清单作为备选会失去编译期符号检查，因此不采用。
- [上下文接缝不足会造成无法按 Actor 定位] → 只在正式 Frame／Logic／Actor 根设置；缺失时标记 Global／Unavailable，不猜测，不复制旧上下文。
- [IL 重写可能破坏复杂异常处理或返回路径] → 对异常块、分支、返回和局部变量做编译期校验；不支持的方法直接失败，不生成部分结果。
- [现有手写 Marker 与新 Weaver 同时存在会重复计算] → 迁移以 Owner 为边界，最终 Catalog 中每个 Metric 只有一套生产方式；旧 Marker、Sink、Telemetry 和无消费定义直接删除。
- [Unity 2022.3 与 Unity 6 的 ILPP API 细节有差异] → 首版固定 2022.3 LTS 和当前项目版本，使用 Unity 提供的 Compilation Pipeline 合同；Unity 6 作为后续适配目标，不把升级 Unity 当作当前前置条件。
- [HotFix 程序集可能绕过 Unity ILPP] → 首版明确不把 HotFix 计入已织入闭包；报告 manifest 明确列出范围，不能把未处理程序集宣称为自动诊断对象。
- [AOP Span 与 Unity Profiler 统计的时钟或边界不同] → Span 只负责调用点关联，整体预算仍以已有 Metric／Recorder／LogicTick 口径为准；报告同时保留单位、时钟来源和采样范围。

## Migration Plan

1. 在独立 UPM 包中建立 Runtime Contracts、Runtime Capture、Editor Weaver 和精确 asmdef，锁定 Unity 2022.3 的 `Unity.CompilationPipeline.Common` 与 `com.unity.nuget.mono-cecil@1.11.6`，定义正式 Weaver 输入和 instrumentation manifest。
2. 为现有 Performance Build Request 增加 `Disabled`、`MarkerOnly`、`Span` 三种明确模式，生成规范化 Catalog 输入；让缺少 Catalog／程序集范围／模式 identity 的请求在编译前失败。
3. 实现同步方法探针的 IL 校验与织入，生成异常安全范围、PointId、Marker descriptor 和 per-assembly manifest；先只接入一个已稳定的 Gameplay／Presentation 根以确认调用图。
4. 实现 Frame／Logic／Actor 三个上下文接缝和固定布局 Span buffer，把 Span 文件挂到现有 Capture Agent、staging、顶层 manifest、summary 和 Comparer；不创建第二 Controller 或第二 Capture workflow。
5. 按正式 Owner 迁移 Gameplay Tick、Session、Presentation、Pose Graph 和 Simulation 的现有手写 Marker；删除重复 Marker 字符串、`ISimulationPerformanceSink` 和 `PerformanceCaptureTelemetry` 的重复计时路径。若 active Pose Graph change 尚未提供最终 Owner，等待并报告冲突。
6. 把现有报告增加 Metric → Point → Actor／Frame／Tick 的下钻关系，并保持 Unity Profiler、Span、WPR/xperf 三类证据的统计口径分离。
7. 完成 Disabled Managed／IL2CPP 闭包 Gate、Capture manifest identity 和当前 Performance workflow 的静态闭合；用户再按现有 Smoke、Replay、Capture 流程进行端到端验收。
8. 实施收口后同步 `openspec/project.md` 和受影响的 active Performance change；确认 current spec 与实际 Owner、包版本、模式和产物名称一致，再由用户验收后归档。

回退只恢复迁移前完整的 Performance 提交，不在运行时保留手写 Marker 与 Weaver 双路径。
