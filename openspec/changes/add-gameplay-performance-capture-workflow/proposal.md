# Change: 建立 Gameplay 性能采集工作流

## Why

项目现有 `SimulationPerformanceCapture` 只能在 Editor Play Mode 中通过隐藏菜单固定预热3秒、采集10秒，并把手写 Marker 名称、Main Thread、Idle 和全帧 GC 压成单个 JSON。它没有独立 Player、符号闭包、原始 Unity Profiler 数据、函数调用栈、稳定性能场景、预算或基线比较；硬编码1024样本会在10秒120 FPS时覆盖早期帧，逐帧总量、每Tick和每调用口径也没有严格分开。Marker 名称在运行Owner与采集器中双写，已经留下三个失效 Foot Placement Marker。

本项目需要一条面向求职 Gameplay Demo 的正式本地性能工作流：作者或自动化Agent从Launcher或项目MCP控制面选择明确场景和预算，两者调用同一个Editor workflow，由独立进程启动可符号化 Performance Player、控制现成 Windows Performance Toolkit 外部CPU采样与分析、收集 Unity Marker 与二进制 Profiler 记录，最后生成可追溯报告和同口径差异。项目只实现业务采集编排、可复现场景、指标目录和报告，不自研内核采样器、栈展开器或符号解析器。

## What Changes

- 新增版本化 `Performance Scenario`、`Capture Profile`、`Budget Profile` 与不可变 Capture Request；场景显式锁定 Gameplay Lab Variant、角色输入、相机输入、分辨率、画质、时钟、预热和采集范围。
- 新增位于 `Library/Performance/Players` 的 Windows x64 IL2CPP Development Performance Player 构建闭包，包含 Gameplay Lab、精确可执行文件、PDB、Program/Pipeline/Projection/Solver身份和文件哈希；它不是商业或Network正式产品。
- 扩展同一Performance Player Build Request与BuildIdentity以消费`add-generated-diagnostic-sampling-framework`的canonical、稳定排序`DiagnosticCapabilitySet`，删除任何领域专属Disabled／Capture构建字段。每项`DiagnosticCapabilityDescriptor`只保存CapabilityId、Mode、Event Set、Sampler Set、Schema、Program、维度、packet capacity与transport identity；Program identity闭合Generated Program、Generator与packet layout revision。`character-foot-ik`是首个Capability，未来领域只增加descriptor，不修改Build核心；每个Capture Capability由Generated Started／CommittedSample／Stopped Handler独立拥有Session、cadence、lineage、packet流和子manifest，Performance只组合顶层Build、Player、Controller、Gate、Capture与Comparer，不对齐或合并领域样本。
- 新增轻量 Player Capture Agent。Agent只处理Ready、Warmup、Start、Stop与Fault协议，控制 Unity binary Profiler log和预分配 Recorder，不注册第二Tick target、不驱动Session、不修改Gameplay或Presentation状态。
- 新增独立 `.NET 8 Windows` Performance Controller。Controller通过显式本机工具链配置启动Player、建立仅绑定`127.0.0.1`的临时TCP传输、控制唯一WPR命名实例、写入WPR起止Marker、停止并回收ETL；随后用xperf按Marker区间和匹配PDB生成函数Inclusive/Exclusive栈报告，用WPA Exporter生成目标Player的Context Switch表。
- 把Gameplay、Session、Simulation、Presentation Marker迁移到各领域Owner发布的稳定Metric Catalog；采集器只组合Catalog，不再复制每个Marker字符串。当前失效Foot Marker和旧单JSON schema直接删除。
- 每次采集原子发布manifest、逐帧/逐Tick汇总、Unity Profiler raw、WPR ETL、xperf函数栈报告、WPA Context Switch CSV、Player日志、进程结果和比较报告；失败采集保留结构化失败证据但不能成为Baseline。
- 在 `Tools/3C/Launcher` 增加Performance区，并提供`performance.prepare`、`performance.build_player`、`performance.smoke`、`performance.replay`、`performance.capture`与`performance.report`六个MCP薄入口；Smoke只验证`HELLO -> READY -> STOP`，Replay只验证精确Warmup与完整Trace，二者均不启动WPR。Capture必须消费同Scenario、Player和Profile身份的Completed Smoke与Replay Gate。两种控制面共用唯一workflow、状态和产物身份，不复制Build、Capture或分析实现。删除旧10秒隐藏菜单，不保留旧格式入口。
- 仓库策略精确允许新的Controller工程文件，但现有GitHub基础CI仍不构建Player、不运行Unity或性能采集，也不新增性能测试job。

## Impact

- 新增能力 `gameplay-performance-capture-workflow`；现有current specs没有性能采集能力，因此不是对当前业务行为spec的替代。
- `gameplay-tick-system`继续由原Frame Source唯一驱动Session；Capture Agent只是观察端口和外部控制端点，不成为Input/Logic/Presentation target。
- `gameplay-simulation-pipeline`的Pass、Schedule、Kernel、WorldSolver与Commit边界不改变；性能Metric只包围既有Owner边界。
- `btsmtl-runtime-diagnostics`继续负责source-mapped行为Trace。性能样本不进入RuntimeDebugSession、Trace Capture Store或Gameplay Snapshot，两套数据只在最终Capture manifest中以身份关联。
- `client-build-artifact-layout`保持不变。Performance Player、PDB与Capture都属于`Library/Performance`可重建本机诊断数据，不进入`Build/Players`、`Build/Network`或商业发布manifest。
- `repository-ci-foundation`只增加Controller `.csproj`的精确允许路径；基础CI仍明确禁止Unity batchmode、Player构建和性能测试。
- active `refactor-character-pose-graph-architecture`将删除`PosePlanExecutionRuntime`和`CharacterPoseGraphStagedExecutor`旧Owner。实施本change时，Presentation Metric必须绑定重构后的正式Owner；不得修改即将删除的旧类型、增加兼容wrapper或保留双Marker链。
- active `add-generated-diagnostic-sampling-framework`提供通用`DiagnosticCapabilityDescriptor`、`DiagnosticCapabilitySet` identity、typed生命周期Handler和Schema-driven Host Finalizer合同；本change唯一负责把Set接入Build Request、BuildIdentity、Player/Run/Capture manifest、握手与Comparer，并在控制窗口发布Started／Stopped Event。`refactor-foot-ik-diagnostic-sampling`只注册首个`character-foot-ik`领域descriptor和CommittedSample Event。Performance不得解释Capability的cadence、lineage、维度或packet布局，也不得把多个子manifest合并成统一采样表；任一Capability模式、Event Set、Program、Sampler Set、Schema、容量或transport不一致时不得比较性能差值。
- archive中的旧性能报告只作历史证据，不作为新schema、预算或Baseline输入，也不被迁移或覆盖。
