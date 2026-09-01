# Design

## 目标与非目标

唯一目标是让作者能以同一场景、同一Player身份和同一采集口径重复获得三类证据：Unity业务阶段耗时、Windows函数采样热点、可比较的帧/Tick/GC统计。工具必须先回答“是否超预算、慢在哪个正式业务阶段”，再把具体调用栈交给WPA或Unity Standalone Profiler。

本change不实现VTune式硬件计数器、内核驱动、调用栈展开、PDB解析、自动Flame Graph、自动RenderDoc/PIX/Nsight/RGP、GPU预算、Unity CI性能测试、Editor Play Mode正式Baseline或线上遥测。RenderDoc继续作为已有的显式单帧图形诊断工具，后续自动化必须另建change。

## 唯一工作流

```text
Tools/3C/Launcher | performance.* MCP tools
  -> immutable Run Request
  -> Performance Controller process
     -> Smoke: launch Player -> HELLO -> READY -> STOP -> atomic Smoke Gate
     -> Replay: require matching Smoke Gate -> START -> exact Warmup pause -> CONTINUE
        -> complete Fixed Input + Camera Trace -> atomic Replay Gate
     -> Capture: require matching Smoke + Replay Gates -> launch Player -> READY
        -> START -> exact Warmup pause -> validate Recorder availability
        -> start owned WPR session -> WPR start marker
        -> start Unity binary capture and resume Replay
        -> stop at exact Scenario boundary -> WPR stop marker
        -> stop WPR and Player capture -> export xperf stacks and WPA Context Switches between markers
        -> analyze and atomically publish Capture package
```

Launcher与MCP只调用同一个`ThirdPersonPerformanceCaptureWorkflow`创建Run Request、启动Controller和读取只读状态/报告；MCP不得复制Build、Gate、Capture、Analyzer或直接启动WPR、xperf、WPA。两种控制面不得在`OnInspectorGUI`、Editor update回调或Unity主线程执行外部进程、WPR、xperf/WPA导出、符号加载、统计或大文件写入。Controller必须是唯一外部进程owner，Player不得自行启动WPR、xperf或WPA。Smoke与Replay使用非提权Controller且不得读取或启动这些Windows采集分析工具；只有通过两道同身份Gate的Capture可以提权。

## 性能场景与输入所有权

`PerformanceScenarioArtifact`保存ScenarioId、Revision、Gameplay Lab Scene/Variant、角色输入Trace、相机输入Trace、预热准入、开始/结束边界和内容哈希。角色输入必须复用现有Fixed Input Trace的canonical input与Drive Command边界；Performance Runner不能创建第二`CharacterSimulationInput` writer或直接调用Program。相机Trace只在既有local camera look input采样边界替代人工设备值，继续经过`CharacterCameraPresentationRuntime -> CameraPosePlan -> ICameraRigAdapter`，不能回放最终Camera Transform、Cinemachine状态或写Gameplay CameraBasis事实。

Ready要求至少包含GameplayTickSystem已初始化、所选Session Active、锁定roster以及Scenario目标Actor的canonical Fixed start body已登记。Fixed Input Trace必须显式分为Warmup LogicTick前缀与Capture LogicTick后缀；Controller发出START后正式Replay推进Warmup前缀，Trace Module在精确Warmup边界暂停并停止同帧后续Logic Tick。Warmup使各领域正式Owner自然创建自己的Marker，Agent在暂停边界按Catalog严格验证全部Recorder；验证通过后Controller写WPR start marker并命令Agent创建Recorder和binary log，Agent完成后再恢复同一Replay进入Capture后缀。预热不得使用冻结状态下空等表现帧、启动后固定秒数、第二份输入或无法精确停在LogicTick边界的RenderFrame计数。Scenario结束后Runner停止提供录制输入，不能顺手回滚或修改正式Session状态。

## Performance Player与符号闭包

唯一正式采集目标是Windows x64 IL2CPP Development Performance Player，关闭Deep Profiling、Script Debugging与Autoconnect Profiler。Player只包含Gameplay Lab及Performance Agent所需内容，输出到`Library/Performance/Players/<BuildIdentity>`。BuildIdentity锁定Unity版本、BuildTarget、ScriptingBackend、场景、Player文件、GameAssembly、PDB、Burst/Native符号、Scenario catalog、构建输入哈希及全部显式诊断capability identity。

`add-generated-diagnostic-sampling-framework`提供canonical `DiagnosticCapabilityDescriptor`与稳定排序`DiagnosticCapabilitySet`。Performance Build Request只保存该Set，删除并禁止任何领域专属Disabled／Capture字段。每项descriptor显式保存CapabilityId、Mode、Sampler Set identity、Schema identity、Program identity、packet capacity与transport identity；Program identity闭合AOT Generated Program hash、Generator revision与packet layout revision。Disabled是纯性能基线并通过Player编译约束排除对应领域Definitions、Bridge、Generated Program、capture页、packet队列与interest；Capture只包含匹配Program闭包。`refactor-foot-ik-diagnostic-sampling`只注册首个`character-foot-ik` descriptor。Build workflow使用Player专属编译输入生成全部选中Capability闭包，不修改全局配置后等待隐式重编译，也不在Player启动后切换Program。

Build与Run分离。Performance Build通过现有`ProductBuildValidationContext`的显式`PerformancePlayer` kind进入全局Build回调，不伪装成Commercial Client或Network Test Player。由于Unity禁止把`BuildPipeline`输出直接指向项目`Library`，Build固定使用仓库根下项目外同盘的`.performance-build/<job>`工作区，并以短路径避免IL2CPP与StreamingAssets输出越过Windows文件API边界。Build成功后工作流必须把`GameAssembly.pdb`移到`GameAssembly.dll`同目录、保留Data中的Burst PDB，并删除Unity明确标注不得随游戏发布的`BackUpThisFolder_ButDontShipItWithYourGame`构建中间目录；完整Player、符号、Scenario、诊断capability和manifest校验成功后先移动进`Library/Performance/Players/.staging`，再原子发布本机Player目录。该外部工作区是正式Build边界且被Git忽略，不是fallback、第二产物根或可消费Player。Capture只消费作者显式选择且manifest完全合法的既有BuildIdentity，不自动重建、不改用Editor Play Mode，也不扫描其它Player目录补齐缺失文件。商业`Build`根不接收任何Performance文件。

## Controller与本机工具链

Controller是`Tools/ThirdPersonPerformanceCapture`下被跟踪的`.NET 8 Windows`控制台工程。每台机器必须显式保存本机Toolchain Definition，至少包含WPR、xperf、WPA Exporter、WPA、Unity Standalone Profiler启动信息和Symbol Cache路径。Launcher或`performance.prepare`只能显式Apply一份精确配置进入Run Request；未知版本、路径歧义、缺文件或不匹配架构必须在Capture启动Player前失败。Smoke与Replay只预检Player、Scenario、Profile、Run边界和前序Gate，不因未使用的Windows采集工具权限或版本失败。

Controller为每次Run建立只绑定`127.0.0.1`的临时TCP listener并把系统分配端口只传给自己启动的Player；只有Capture生成WPR instance name。Player侧transport reader与writer使用独立后台线程，Unity主线程只交换有界命令字符串，任何连接、读写或释放都不得发生在Unity主线程共享的状态锁内。HELLO必须先于READY进入唯一发送队列，Controller继续用RunId与目标PID拒绝错误客户端。Controller必须检查RunId、持久化status、cancel与staging/result边界、Player manifest和输出目标；Capture额外检查权限、磁盘、PDB、工具链与既有同名WPR实例。cancel文件是唯一取消权威，Controller检测后立即进入owned cleanup，不等待Player通过transport确认。Controller不得取消或停止不属于本Capture的WPR session。Controller异常、Player崩溃、transport断开或超时后，Controller只停止自己拥有的Player/WPR，Smoke与Replay发布Gate Fault，Capture封存已有日志和ETL并发布Faulted结果。旧Named Pipe传输直接删除，不保留fallback。

## WPR、xperf与WPA函数热点

第一版CollectorId固定为`windows-wpr-cpu/1`，使用项目拥有的WPR profile采集Sampled Profile、Process/Thread、Context Switch和必要File IO；不启用PMC、VTune或Heap全量跟踪。Controller在正式Scenario开始和结束时向自己的WPR实例写入带CaptureId的命名Marker。停止WPR后，xperf先读取两个精确Marker的微秒区间，再以目标PID、`PROFILE`事件、匹配PDB与项目Symbol Cache生成官方Butterfly栈报告；WPA Exporter通过结构化`exporterconfig`只导出目标Player的Context Switch与线程栈表。

Analyzer只消费xperf官方栈报告和WPA正式导出表，不解析ETL、不展开PDB、不实现采样算法。Controller只从xperf固定的Uni-Inclusive函数表归一化Process、Module、Function、Inclusive Samples与Exclusive Samples，并把跨线程总热点明确标为`All Threads`；线程调度与等待证据来自WPA Context Switch表。无法解析符号、丢失ETW event、Marker区间缺失或目标PID不唯一时，CPU热点阶段必须失败，不能输出地址名或半符号化排行榜充当成功。

## Unity采集与Metric Catalog

每个领域Owner发布不可变`PerformanceMetricDefinition`集合，定义MetricId、Profiler名称、Domain、ParentId、Unit、SampleScope和Aggregation。Gameplay、Session、Simulation、Presentation各自拥有自己的Catalog；Unity适配器显式组合Catalog并在Player Ready前拒绝重复Id、重复Profiler名称、断裂Parent或未知Aggregation。Unity Marker继续只由正式Owner按Catalog名称创建；Capture Agent不得预注册第二组同名Marker，而是在Warmup边界从同一Catalog建立并验证Recorder。Simulation Core继续通过`ISimulationPerformanceSink`保持Unity无关；Unity sink从Simulation Catalog建立Marker。Presentation重构后的正式Owner从Presentation Catalog建立Marker。

Capture Agent在Recording前一次创建全部`ProfilerRecorder`和固定容量buffer，容量由Profile的最大表现帧率、Logic Tick率、采集范围和安全余量计算。Recording期间不写磁盘、不格式化报告、不反射发现Marker、不扩容。Unity binary profiler必须写入当次staging目录；Recorder只保存报告所需的Counter与Marker序列。

统计必须明确区分：

- RenderFrame Aggregate：一个表现帧内同一Marker全部调用之和。
- LogicTick Aggregate：一个Logic Tick内同一阶段之和。
- Invocation Mean：总耗时除以调用数，只表示均值。
- Parent/Child Inclusive：父子Marker不得相加，报告按父级占比解释。

P50/P95/P99/Max必须基于对应SampleScope的原始序列计算。`Idle`跨线程总和不得进入热点排序；Editor全帧GC不得作为Player GC。缺少必须Counter时Capture在Ready阶段失败，不输出0值。

## Capture产物与原子发布

本机根固定为`Library/Performance/Captures`。每次Capture先写`.staging/<CaptureId>`，完成后原子发布为：

```text
<CaptureId>/
  manifest.json
  summary.json
  metric-samples.csv
  unity-profiler.raw
  windows-cpu.etl
  xperf-marks.csv
  xperf-stack.xhtml
  wpa-exporter.json
  cpu-hotspots.csv
  thread-stacks.csv
  context-switches.csv
  player.log
  controller.log
  process.json
  comparison.json
```

manifest锁定Capture schema、Request、Scenario、Build、Program、Pipeline、Projection、Solver、Actor roster、设备、OS、CPU/GPU、分辨率、画质、VSync、targetFrameRate、Toolchain、WPR profile、全部文件hash和最终Status。Completed Capture要求全部声明文件存在且hash匹配；Faulted Capture保存失败阶段、进程退出、WPR状态和已有证据，但Comparer必须拒绝它。旧`Library/Performance/Simulation-*.json`不读取、不迁移、不覆盖。

当任一Diagnostic Capability为Capture时，该Capability独立拥有Session、cadence、opaque typed lineage、sample key、packet流、Writer和runtime manifest；对应领域Bridge只通过通用框架Writer向自己的staging子目录封存版本化typed packet。Controller在Player停止且各流分别封存后按稳定CapabilityId调用框架Host Finalizer，再由领域Host Adapter生成Sampler格式、Analyzer与Publisher产物，并把每个Capability manifest作为当前Capture manifest的显式子闭包。Performance只编排开始、停止、Finalizer顺序和顶层完成状态，不解释领域lineage、不跨Capability对齐sample、不合并packet或重写子manifest。该扩展不得改变WPR、Unity Profiler或Metric采集顺序，也不得让Player主线程执行格式化、Analyzer或Publisher。Disabled Capability不得创建空目录、Null Adapter或占位manifest。

## 分析、预算与比较

`PerformanceBudgetProfile`显式声明目标FPS、Main Thread、Logic Tick、Dropped Tick、GC和选定业务Metric预算。Summary保存P50/P95/P99/Max、样本数、调用数、超预算数量和父级占比。预算失败是诊断结果，不使Capture流程本身Faulted；采集、身份、符号或文件错误才是流程失败。

Baseline由作者显式选择CaptureId。Comparer必须核对Scenario hash、Variant、Actor roster、Build mode、分辨率、画质、VSync、targetFrameRate、hardware identity、Metric catalog revision、WPR profile、统计schema与完整`DiagnosticCapabilitySet` identity。任一Capability的Mode、Program、Sampler Set、Schema、packet capacity或transport identity不同都必须拒绝生成性能差值，不自动选择最新Capture、近似场景或其它机器结果。合法比较输出绝对值、百分比、预算变化和热点函数样本差异，但不得把耗时下降解释为Gameplay行为改善。

## Launcher、MCP与工具打开

Launcher Performance区只显示Toolchain、Scenario、Player BuildIdentity、`DiagnosticCapabilitySet`、每项Mode／Program／Sampler Set、Smoke Gate、Replay Gate、Budget、Controller状态和最近显式Capture。操作固定为Build Performance Player、`1. Smoke`、`2. Replay`、`3. Capture`、Cancel Owned Run、Open Summary、Open Unity Standalone Profiler、Open WPA、Select Baseline和Compare。Capability模式与Program只是同一Build Request的显式输入，不新增领域专属构建或采集按钮。Replay只在当前Smoke Gate合法时启用；Capture只在当前Smoke与由该Smoke产生的Replay Gate都合法时启用。Open动作必须使用manifest中的精确文件，不扫描目录寻找“最新”。

MCP固定暴露`performance.prepare`、`performance.build_player`、`performance.smoke`、`performance.replay`、`performance.capture`和`performance.report`。长耗时Player Build、Smoke、Replay与Capture必须返回稳定`job_id`并把状态写入`Library/Performance/McpJobs`，`status`在Unity域重载后只根据同job Build workspace、Player manifest、Gate manifest或Capture manifest恢复结果，不自动重跑任务。报告读取必须消费显式manifest或本工作流记录的最近显式Gate/Capture，不得按目录时间自动选择。MCP只做参数归一化、调度和结构化返回，所有业务写入继续落到同一个Editor workflow与独立Controller。

旧`Tools/3C/Diagnostics/Capture Simulation Performance (10s)`、快捷键、静态Marker数组、单JSON writer和旧SessionState pending key在新入口闭合后直接删除，不提供旧入口转发、兼容reader或格式迁移器。

## 与现行Spec和Active Change对账

- `gameplay-tick-system`：Performance Runner复用正式Drive/Input边界，Agent不注册第二Logic target，不产生SimulationTick。
- `gameplay-simulation-pipeline`：Metric只包围当前Pass/事务，不成为Pass、Product、SnapshotParticipant或Commit副作用。
- `btsmtl-runtime-diagnostics`：行为Trace与性能样本保持两套明确用途；只在manifest关联身份，不共享Capture Store。
- `client-build-artifact-layout`：Library Player永远不是正式产品，不修改`Build`三分区。
- `repository-ci-foundation`：精确允许Controller project，但现有CI不build、不run、不采集性能。
- `character-camera-pipeline`与`character-input-pipeline`：Scenario只替代正式输入来源，不写Camera Transform、不建立第二request buffer。
- `refactor-character-pose-graph-architecture`：Marker迁移只落在新Program Runtime、Source Module、Constraint Module与Final Publication Owner。若旧类仍在迁移中，相关任务等待Owner闭合并报告冲突，不对旧类加桥接。
- `add-generated-diagnostic-sampling-framework`：提供通用Capability Set、编译闭包descriptor、packet与Host Finalizer合同，本change继续唯一拥有Build、Player、Controller、Run Request、manifest与Comparer；框架不得建立第二性能入口。
- `refactor-foot-ik-diagnostic-sampling`：只注册首个`character-foot-ik` descriptor与领域Host Adapter；Foot IK packet不进入Metric、Unity Profiler、WPR或Gameplay状态，也不建立Foot专属传输或第二Capture发布根。

## 取舍

- 选择Windows Performance Toolkit的WPR、xperf与WPA Exporter而不是自研采样器，得到现成函数Inclusive/Exclusive、线程调度和Context Switch，代价是第一版仅支持Windows并要求管理员权限和严格PDB管理。
- 选择独立Controller而不是Editor内执行，避免Unity主线程和Inspector承担外部工具、导出与大文件IO，代价是增加一个被跟踪的Windows控制台工程和进程协议。
- 选择IL2CPP Development Player同时获得Unity raw和可符号化Native栈，代价是Development Profiler有测量开销；本change用于稳定诊断和回归比较，不宣称等同商业Release绝对性能。
- 选择在同一Performance构建入口中消费通用Diagnostic Capability Set，保留唯一Controller并允许未来领域扩展；代价是任一Capability身份变化都产生新BuildIdentity，开启采样或Capability集合不同的Player不能与纯性能基线比较。
- 选择完整逐帧/Tick样本而非近似直方图，保证分位数和尖峰可追溯，代价是Capture包更大；固定时长、预分配和原子发布控制该成本。
- 选择角色输入加相机输入的正式Scenario而不是人工操作，得到CPU与表现帧可比性，代价是需要维护Scenario revision；业务行为变化后必须显式发布新Scenario，不能把新旧revision硬比较。
