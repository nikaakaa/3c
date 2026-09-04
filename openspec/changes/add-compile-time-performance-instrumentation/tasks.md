## 1. 插件边界与依赖

- [x] 1.1 建立 `com.thirdperson.performance-instrumentation` 的 Runtime Contracts、Runtime Capture 和 Editor Weaver 目录及 asmdef，验证 Runtime 与 Weaver 的引用方向符合设计且 Weaver 不进入 Player
- [x] 1.2 锁定 Unity 2022.3 的 `Unity.CompilationPipeline.Common` 与项目已解析的 `com.unity.nuget.mono-cecil@1.11.6`，验证 Weaver 不引用 Unity 内部 `Unity.Cecil.dll`、Fody、PostSharp 或 DynamicProxy
- [x] 1.3 定义 Disabled、MarkerOnly、Span 三种正式模式、版本号、Span layout revision 与 instrumentation identity，验证缺失模式或版本不会生成默认配置

## 2. Metric 与探针声明

- [x] 2.1 实现最小性能探针声明和 Conditional metadata，验证声明只保存 MetricId，不复制 profiler name、parent、unit、budget 或报告字段
- [x] 2.2 为现有 `PerformanceMetricDefinition` 与领域 Catalog 增加规范化 Weaver 输入，验证 MetricId、ProfilerName、ParentId、SampleScope、Unit、Aggregation 和 Catalog revision 只有一个来源
- [x] 2.3 实现探针与 Catalog 的编译期绑定、重复 PointId 检查和 deterministic point manifest，验证未知 Metric、重复身份和 Catalog revision 不匹配会在 Player 生成前失败
- [x] 2.4 将探针程序集集合、模式、Catalog revision、Weaver version 和 Span layout 纳入现有 Performance Build Request，验证它们进入唯一 BuildIdentity

## 3. ILPostProcessor 织入

- [x] 3.1 实现只处理显式程序集集合和显式探针声明的 ILPostProcessor，验证未进入集合的 Unity、第三方和 HotFix 程序集保持未修改并在 manifest 中明确列出
- [x] 3.2 实现同步方法、异常处理块、分支、局部变量和返回路径校验，验证 async、iterator、abstract、extern、构造函数、by-ref return、函数指针及无法安全重写的方法给出确定编译错误
- [x] 3.3 在受支持方法体外生成异常安全的 Enter／Exit 范围，验证正常返回值、异常类型和原有异常传播边界不被改变
- [x] 3.4 生成每个调用点的 PointId、Metric descriptor、Marker descriptor 和程序集 manifest，验证同一调用点在相同输入下 identity、排序和 manifest hash 稳定
- [x] 3.5 生成 MarkerOnly 与 Span 两种不同的静态调用闭包，验证未选择的模式不会写入目标程序集的调用路径

## 4. 上下文与固定容量采集

- [x] 4.1 实现无托管分配的 Frame、Logic、Actor 上下文 Scope 和嵌套恢复，验证上下文不会使用 AsyncLocal、场景搜索、Transform 或上一次 Actor 值
- [x] 4.2 实现 MarkerOnly 的 Unity Profiler 范围和 Span 模式的固定字段记录，验证每次 Span 包含 PointId、MetricId、RenderFrame、LogicTick、Actor／Global、时钟、耗时、线程和结束状态
- [x] 4.3 实现 Capture 开始前分配的固定容量 Span buffer、非阻塞有界提交与结构化 Fault，验证容量、序列或 Writer 错误不会扩容、截断后伪装 Completed 或阻塞 Unity 主线程
- [x] 4.4 将 instrumentation Runtime 的启动、停止、取消和封存接入现有 Capture Agent，验证 Smoke／Replay 阶段不写 Span，Capture 只在既有精确 Warmup 边界后开始记录

## 5. 3C 正式 Owner 迁移

- [x] 5.1 在 Gameplay Tick 的 Frame／Logic 根、Presentation Actor 根和必要的 Simulation 根接入上下文接缝，验证下游探针无需新增诊断参数或领域 Bridge 即可取得正确上下文
- [ ] 5.2 按当前最终 Owner 为 Gameplay、Session、Presentation、Pose Graph 和 Simulation 阶段添加探针声明，验证每个声明都能在 Catalog 和 point manifest 中定位
- [ ] 5.3 删除已迁移 Owner 的手写 `ProfilerMarker`、重复 Marker 字符串和 Recorder 注册项，验证同一 Metric 不存在手写与织入双生产路径
- [x] 5.4 删除 `ISimulationPerformanceSink`、`PerformanceCaptureTelemetry` 与 AOP Span 重复承担的计时路径，验证 LogicTick／阶段统计只由统一 Performance Runtime 产出
- [ ] 5.5 遇到 active Pose Graph 重构尚未收敛的旧 Owner 时暂停该段迁移并报告冲突；在最终 Owner 可用后验证旧 `PosePlanExecutionRuntime`、旧 Executor 和兼容 wrapper 均未被重新引用

## 6. Capture 产物与报告

- [x] 6.1 将 instrumentation manifest、Span layout、Span 文件及其 hash 加入现有 Capture staging 和顶层 manifest，验证不新增 Player、Controller、顶层 Capture 或产物根
- [x] 6.2 扩展现有 summary／report／comparison 读取 Metric → Point → Actor／RenderFrame／LogicTick 的下钻证据，验证父级 Inclusive、子级 Duration、Profiler sample 和 Span sample 不被重复相加
- [x] 6.3 扩展现有 Comparer 的 identity 校验，验证 Disabled、MarkerOnly、Span、Probe manifest、Catalog、Weaver 和 layout 任一不同都会拒绝性能差值
- [x] 6.4 明确 `btsmtl-runtime-diagnostics`、Generated Diagnostic Sampling 与 Performance Span 的产物边界，验证 Span 不进入 RuntimeDebugSession、Fact Root packet、DiagnosticEvent 或 Host-only Operator

## 7. 构建闭包与文档收口

- [ ] 7.1 实现 Disabled Managed／IL2CPP 闭包 Gate，验证业务程序集和 Player 中不存在性能 Attribute、织入 Runtime AssemblyRef、生成 PointId、Marker descriptor 或 Span 类型
- [ ] 7.2 实现 Capture Player 闭包 Gate，验证 Player 只包含 Request 选择的程序集、探针、模式、Catalog 和 Runtime，且 manifest 与实际文件 hash 一致
- [ ] 7.3 用当前 Performance workflow 的正式 build、manifest 和 OpenSpec 关系更新 `openspec/project.md` 与受影响 active change，验证文档不再同时描述手写 Marker 和 Weaver 为正式路径
- [ ] 7.4 执行仓库现有 OpenSpec strict validation 和相关静态编译收口；若执行 .NET 构建，使用 `--disable-build-servers /nr:false /p:UseSharedCompilation=false` 并随后执行 `dotnet build-server shutdown`
