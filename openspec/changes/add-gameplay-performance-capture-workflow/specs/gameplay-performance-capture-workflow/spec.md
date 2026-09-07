## ADDED Requirements

### Requirement: 性能采集必须通过唯一显式工作流执行

系统 MUST只通过共享`ThirdPersonPerformanceCaptureWorkflow`创建不可变Run Request；`Tools/3C/Launcher` Performance区与项目`performance.*` MCP工具可以作为两种显式控制面，但MCP MUST不复制Build、Gate、Capture、Analyzer或外部进程实现。独立Performance Controller拥有Player、Collector、staging与最终发布。Editor窗口和MCP MUST只发起命令、调度长任务和读取只读状态/报告，不得执行WPR、xperf/WPA导出、符号解析、统计或Capture大文件写入。旧10秒菜单、快捷键和单JSON采集路径 MUST删除，不得转发到新入口或并行保留。

#### Scenario: 作者开始一次性能采集

- **WHEN** 作者显式选择合法Toolchain、Scenario、Performance Player、Budget和Capture Profile并从Launcher或MCP发起Capture
- **THEN** 共享workflow MUST生成完整Request并启动一个独立Controller进程
- **AND** 同次Capture MUST只有一个Controller和一个产物staging Owner

#### Scenario: Agent通过MCP采集并读取报告

- **WHEN** Agent通过`performance.build_player`、`performance.smoke`、`performance.replay`或`performance.capture`启动长耗时操作
- **THEN** MCP MUST返回稳定`job_id`并允许`status`轮询，而不是复制或旁路共享workflow
- **AND** Unity域重载后MCP MUST只通过持久化job状态和精确workspace/manifest恢复结果，MUST不自动重跑操作
- **AND** `performance.report` MUST只读取manifest精确声明的产物并返回结构化Summary或Comparison

### Requirement: Capture必须由同身份Smoke和Replay Gate准入

共享workflow MUST把本地运行拆成固定顺序的Smoke、Replay与Capture。Smoke MUST只执行Player启动、`HELLO -> READY -> STOP`和正常退出，不得启动WPR、Profiler Recorder或Fixed Input Replay。Replay MUST要求当前Player、Scenario与Profile身份的Completed Smoke Gate，执行精确Warmup暂停和完整Fixed Input/Camera Trace，发布Start Body、Input Sequence与Body Trajectory hash；不得启动WPR或Profiler Recorder。Capture MUST同时要求当前Completed Smoke Gate以及由该Smoke产生的Completed Replay Gate，任一manifest缺失、Faulted、文件hash断裂或身份不匹配时 MUST在启动Player前拒绝。

#### Scenario: Player启动合同损坏

- **WHEN** Smoke未在Ready timeout内完成HELLO与READY
- **THEN** Controller MUST停止自己拥有的Player并原子发布Faulted Smoke Gate
- **AND** MUST不启动WPR、请求管理员权限或产生ETL

#### Scenario: Replay行为闭包通过

- **WHEN** Replay完成声明的Warmup与Capture LogicTick总量
- **THEN** Replay Gate MUST记录匹配Trace的Start Body、Input Sequence与Body Trajectory hash
- **AND** Capture MUST只接受由当前Smoke Gate产生且Scenario、Player、Profile、输入与相机身份全部一致的Replay Gate

#### Scenario: 作者绕过Gate直接Capture

- **WHEN** 当前Smoke或Replay Gate缺失、Faulted、属于旧Player或不属于当前前序Gate
- **THEN** Launcher MUST禁用Capture，MCP与Controller MUST拒绝请求
- **AND** MUST不启动Player、WPR或Profiler

#### Scenario: 旧入口仍被调用

- **WHEN** 代码、菜单或快捷键仍引用旧`SimulationPerformanceCapture`
- **THEN** 迁移 MUST视为未完成
- **AND** 系统 MUST不通过兼容wrapper或隐藏转发继续生成旧JSON

### Requirement: Performance Toolchain必须显式配置且严格预检

本机Toolchain Definition MUST显式保存WPR、xperf、WPA Exporter、WPA、Unity Profiler打开方式和Symbol Cache的精确路径与版本。Controller MUST在启动Player前校验文件、架构、权限、磁盘、WPR owned instance能力与输出目录。缺失、歧义或非法配置 MUST使Capture失败，MUST不搜索另一安装、不改用Editor Play Mode、不关闭未知WPR session或选择其它Collector。

#### Scenario: WPR路径失效

- **WHEN** Request引用的WPR不存在或版本不满足正式Toolchain
- **THEN** Controller MUST在启动Player前发布Preflight Fault
- **AND** MUST不改用其它WPR副本、Superluminal或无CPU采样模式

#### Scenario: 系统已有其它WPR采集

- **WHEN** Controller无法创建自己的唯一WPR instance或发现同名instance已被占用
- **THEN** Capture MUST明确失败或只清理自己已确认拥有的instance
- **AND** MUST不停止、取消或覆盖其它采集会话

### Requirement: 正式性能场景必须冻结可比较输入和环境

每个`PerformanceScenarioArtifact` MUST保存稳定ScenarioId、Revision、Scene、Gameplay Lab Variant、角色输入Trace、local camera input Trace、Ready条件、Warmup LogicTick数量、Capture LogicTick数量、采集开始/结束边界和canonical hash。角色与相机Trace总帧数 MUST等于Warmup与Capture之和。角色输入 MUST复用既有canonical input/Drive端口；相机输入 MUST复用local camera look采样边界。Runner MUST不创建第二`CharacterSimulationInput` writer、不直接Tick Program、不写Camera Transform或Cinemachine状态。

#### Scenario: 回放楼梯战斗场景

- **WHEN** Performance Player加载合法楼梯战斗Scenario
- **THEN** Runner MUST在指定Variant和Ready条件满足后按Artifact输入推进既有Gameplay与Camera链
- **AND** Capture的开始和结束 MUST对应Artifact声明的精确边界

#### Scenario: Warmup到达精确LogicTick边界

- **WHEN** Fixed Input Replay完成Scenario声明的Warmup LogicTick前缀
- **THEN** Trace Module MUST暂停Replay并停止同一RenderFrame的后续Logic Tick
- **AND** Controller MUST在该边界写WPR start marker，Agent MUST在Recorder与binary log就绪后恢复同一Replay的Capture后缀
- **AND** MUST不使用冻结状态空等RenderFrame、固定秒数或第二份输入完成Warmup

#### Scenario: Scenario缺少相机输入

- **WHEN** Capture Profile要求比较Presentation帧但Scenario没有匹配revision的local camera Trace
- **THEN** Request preflight MUST拒绝该Scenario
- **AND** MUST不允许人工镜头输入或固定Camera Transform补齐

### Requirement: Capture Agent必须保持只读且不成为第二运行驱动

Player Capture Agent MUST只发布Runtime Ready、接收Warmup/Start/Stop/Fault命令、控制Profiler/Recorder和冻结采集身份。Controller MUST建立只绑定`127.0.0.1`的临时TCP listener并只把系统分配端口传给自己启动的Player，Agent MUST拒绝其它transport身份。Agent的transport reader与writer MUST只运行在独立后台线程，Unity主线程 MUST只交换有界命令字符串；连接、读写和释放 MUST不持有Unity主线程读取的状态锁，HELLO MUST先于READY进入唯一发送队列。cancel文件 MUST由Controller唯一消费并立即触发owned cleanup，不得等待Player通过transport确认。旧Named Pipe传输 MUST不存在且不得作为fallback。Agent MUST不注册`IGameplayLogicTickTarget`、`IGameplayPresentationFrameTarget`或私有Update runner，不创建SimulationTick，不修改Character/World/Pipeline state，不调用WorldSolver、Program、Committer或Camera Rig。性能数据 MUST不进入Gameplay Snapshot、Pipeline Product或BTSMTL RuntimeDebugSession Capture Store。

#### Scenario: Session尚未Active

- **WHEN** Controller已连接但目标Session、roster或canonical start body尚未满足Scenario Ready合同
- **THEN** Agent MUST保持WaitingForRuntime且不得开始预热或录制
- **AND** MUST不通过额外Tick、场景搜索或默认Actor推进就绪

#### Scenario: Capture中途断开

- **WHEN** Controller loopback transport断开或Player检测到协议错误
- **THEN** Agent MUST停止自己拥有的Recorder和binary log并发布Fault
- **AND** Gameplay Session MUST继续按原正式驱动运行或由其自身生命周期结束

#### Scenario: Transport写入阻塞或作者取消Run

- **WHEN** Player transport写入未完成或作者写入当前Run的cancel文件
- **THEN** Unity主线程 MUST保持可运行且不得等待transport状态锁
- **AND** Controller MUST立即清理自己拥有的Player与WPR并发布Cancelled结果

### Requirement: Performance Player必须是可符号化的本机诊断闭包

正式Capture MUST只消费Windows x64 IL2CPP Development Performance Player。Build MUST关闭Deep Profiling、Script Debugging和Autoconnect Profiler。Unity BuildPipeline MUST使用项目外同盘的正式临时工作区，完整校验后移动进`Library/Performance/Players/.staging`并在`Library/Performance/Players/<BuildIdentity>`原子发布可执行文件、Data/GameAssembly、PDB/Burst/Native符号、Scenario catalog与manifest；外部工作区 MUST不成为第二可消费Player根。BuildIdentity MUST锁定Unity、目标、场景、脚本后端、Program、Pipeline、Projection、Solver、全部文件hash及canonical、稳定排序`DiagnosticCapabilitySet` identity。Performance Build Request MUST只用该Set承载诊断构建能力，不得增加Foot或其它领域专属Disabled／Capture字段。全部Capability Disabled时 MUST不定义`KK_DIAGNOSTIC_SAMPLING`或领域专属define；Capture时 MUST定义通用及匹配Capability符号。每项descriptor MUST保存CapabilityId、Mode、Event Set、Sampler Set、Schema、Program、维度、packet capacity与transport identity。Disabled闭包 MUST通过Cecil与IL2CPP硬门禁排除Diagnostic Attribute metadata／Sampling AssemblyRef、领域Definitions、typed Event Handler、Generated Program、Capture Runtime、capture页、packet队列、interest及Capability／Field identity字符串；Capture闭包 MUST只包含匹配Event／Program。该目录 MUST不被解释为商业Player、Network Product或第二诊断Player。

#### Scenario: 构建Performance Player

- **WHEN** 作者从Launcher或MCP显式执行Build Performance Player
- **THEN** 工作流 MUST在Library staging完成Build、符号与exact closure校验后发布唯一BuildIdentity
- **AND** MUST不写入`Build/Players`、`Build/Network`或商业Content

#### Scenario: 构建领域实机采样变体

- **WHEN** 同一Build Performance Player入口将一个或多个Diagnostic Capability设为Capture并引用合法Program Definition
- **THEN** 工作流 MUST通过Player编译输入定义`KK_DIAGNOSTIC_SAMPLING`及匹配Capability符号，把对应Generated Event Handler／Program、Runtime、capture页和packet队列纳入IL2CPP AOT闭包
- **AND** Player manifest MUST保存完整Capability Set identity且不得包含运行时表达式编译或反射提取路径

#### Scenario: 构建全部诊断关闭的纯性能基线

- **WHEN** 同一Build Performance Player入口把全部Diagnostic Capability设为Disabled
- **THEN** 构建 MUST不定义采样符号，Cecil检查 MUST确认业务程序集零Diagnostic Attribute metadata／Sampling AssemblyRef，Player不包含领域Diagnostics／Runtime、Handler、Program、capture页、packet队列或interest
- **AND** IL2CPP输出 MUST不包含Generated Program／Session／packet／queue／interest类型或Capability／Field identity字符串，且不得使用运行时bool、Null Adapter、Linker推测或空manifest冒充关闭

#### Scenario: Capture引用旧Player

- **WHEN** 所选Player manifest、PDB或Scenario catalog与Request身份不匹配
- **THEN** Capture MUST在启动前失败
- **AND** MUST不自动重建、改选其它Player或继续使用不匹配符号

### Requirement: Metric Catalog必须是Marker和采集器的唯一名称来源

Gameplay、Session、Simulation与Presentation各领域Owner MUST发布不可变Metric Catalog，定义MetricId、Profiler名称、Domain、ParentId、Unit、SampleScope和Aggregation。Unity Marker与Capture Recorder MUST读取同一Definition；Catalog composer MUST在Ready前拒绝重复Id、重复名称、断裂Parent和未知语义。Unity Marker MUST只由正式Owner按Catalog名称创建；Agent MUST在Warmup边界从同一Catalog建立并验证Recorder，MUST不在Owner执行前预注册另一组同名Marker。采集器 MUST不维护第二份Marker字符串列表或保留无运行Owner的Metric。

#### Scenario: Presentation Owner增加正式阶段

- **WHEN** 重构后的Presentation模块新增一个需要采集的稳定阶段
- **THEN** 该Owner MUST在自己的Catalog声明Metric并使用同一名称建立Marker
- **AND** Capture composer MUST通过Catalog组合取得它而不是修改中央字符串数组

#### Scenario: Marker Owner被删除

- **WHEN** Foot或Pose旧Owner及其Metric不再存在
- **THEN** 对应Definition和Recorder MUST随Owner删除
- **AND** 新报告 MUST不输出`valid=false`占位或旧名称兼容项

### Requirement: 一次Capture必须同时保留Unity业务证据和Windows函数热点

Recording开始前Agent MUST按Profile计算容量并一次创建Recorder，Unity MUST把binary Profiler数据写入当次staging。Controller MUST只在Smoke和Replay Gate已通过、当前Capture Player完成READY且Replay到达精确Warmup暂停边界后，才启动`windows-wpr-cpu/1`命名实例采集Sampled Profile、Process/Thread、Context Switch和声明的File IO，并在Scenario精确开始/结束写入唯一WPR Marker。停止后xperf MUST按两个Marker的精确微秒区间、目标PID、`PROFILE`事件和匹配PDB生成函数Uni-Inclusive/Exclusive栈报告；WPA Exporter MUST通过结构化`exporterconfig`按同Marker区间导出目标Player Context Switch与线程栈表。项目 MUST不解析ETL、PDB或实现采样器。

#### Scenario: 完成一次CPU采集

- **WHEN** Scenario到达合法结束边界且Agent与WPR都正常停止
- **THEN** Capture MUST包含Unity raw、Metric samples、ETL、Marker证据、xperf函数栈报告和Marker区间内的WPA Context Switch表
- **AND** 函数热点 MUST至少包含线程、模块、函数、Inclusive与Exclusive样本

#### Scenario: PDB无法解析采样栈

- **WHEN** xperf不能用BuildIdentity中的匹配PDB符号化目标`PROFILE`栈
- **THEN** CPU热点阶段 MUST Fault并记录符号诊断
- **AND** MUST不把地址、unknown函数或部分符号化表当作成功排行榜

### Requirement: 统计必须区分帧、Tick、调用和父子包含关系

Analyzer MUST分别计算RenderFrame Aggregate、LogicTick Aggregate和Invocation Mean，并只对前两类对应原始序列计算P50、P95、P99与Max。父Metric与子Metric MUST按Inclusive关系显示占比，不得相加。跨线程`Idle`总和 MUST不进入热点排行。容量 MUST由采集范围、最大表现帧率、Logic Tick率和安全余量计算，MUST不固定为1024或在Recording中扩容。

#### Scenario: 一个表现帧补三个Logic Tick

- **WHEN** 同一RenderFrame中`Session.LogicTick`调用三次
- **THEN** 报告 MUST分别保存该帧Logic总量、三个Tick样本和每调用均值
- **AND** MUST不把逐帧P95命名为单Tick P95

#### Scenario: 查看Animation和Prepare

- **WHEN** `Animation.Prepare`是`Animation`的子Metric
- **THEN** Summary MUST显示Prepare占Animation的Inclusive比例
- **AND** MUST不把两者耗时相加成Presentation总成本

### Requirement: Capture产物必须原子发布且完整可追溯

每次Capture MUST先写入`Library/Performance/Captures/.staging/<CaptureId>`，并只在manifest声明的Unity raw、Metric samples、WPR ETL、xperf Marker/栈报告、WPA Context Switch导出、日志、process结果、summary以及当前`DiagnosticCapabilitySet`要求的全部子产物完成且hash匹配后发布Completed Capture。每个Capture Capability MUST由Generated Started／CommittedSample／Stopped Handler独立拥有Session、cadence、opaque typed lineage、sample key、packet流、Writer和runtime manifest，并只在Player侧通过通用Writer封存自己的版本化typed packet流；Controller MUST在Player停止后按稳定CapabilityId调用框架唯一Schema-driven Host Finalizer，自动生成Sampler主表／子表CSV与manifest，不调用领域Bridge或Host Adapter。Performance MUST只编排顶层生命周期与子manifest闭包，不解释领域lineage／维度、不对齐跨Capability sample、不合并packet流，也不得重写子manifest。Analyzer／Publisher只读Completed基础产物并拥有独立下游结果。Disabled Capability MUST不创建空目录或占位产物。故障Capture MUST保存失败阶段、进程结果、WPR状态和已有证据，但 MUST不生成Completed身份或成为Baseline。旧`Simulation-*.json` MUST不读取、迁移或覆盖。

#### Scenario: Windows分析导出失败

- **WHEN** Unity raw和ETL已经完成但xperf或WPA Exporter返回失败
- **THEN** Controller MUST发布Faulted Capture并保留原始证据和错误
- **AND** MUST不生成缺少CPU热点表的Completed manifest

#### Scenario: 全部阶段成功

- **WHEN** Request、Scenario、Player、Unity、WPR、xperf、WPA和Analyzer全部完成
- **THEN** Controller MUST原子发布一个hash闭合的Completed Capture目录
- **AND** Launcher打开动作和MCP报告读取 MUST只通过manifest中的精确路径消费产物

### Requirement: Budget和Baseline比较必须使用严格相同身份

Budget Profile MUST显式声明目标FPS、Main Thread、Logic Tick、Dropped Tick、GC及选定业务Metric预算。预算超限 MUST作为Completed Capture的诊断结论，不得伪装成采集Fault。Baseline MUST由作者显式选择；Comparer MUST核对Scenario hash、Variant、roster、Build mode、分辨率、画质、VSync、targetFrameRate、hardware、Metric revision、WPR profile、统计schema与完整`DiagnosticCapabilitySet` identity。任一Capability的Mode、Program、Sampler Set、Schema、packet capacity或transport identity不同 MUST拒绝性能差值。任一身份不一致或Capture Faulted时 MUST拒绝比较，不得选择最新、近似或其它机器结果。

#### Scenario: 同场景候选超过预算

- **WHEN** 合法Candidate的Animation P95高于Budget
- **THEN** Capture MUST保持Completed并报告超预算Metric、绝对值与差值
- **AND** MUST不把该结果解释为Gameplay行为错误或采集失败

#### Scenario: 两份Capture分辨率不同

- **WHEN** 作者尝试比较相同Scenario但不同分辨率的Capture
- **THEN** Comparer MUST列出身份冲突并拒绝差值
- **AND** MUST不按比例归一化后继续

### Requirement: 性能采集不得扩张现有CI或外部分析范围

本能力 MUST只作为本地显式诊断工作流。现有GitHub基础CI MUST不构建Performance Player、不启动Unity、不运行Scenario、不执行WPR/xperf/WPA、不上传Capture，也不增加性能测试job。第一版 MUST不自动化RenderDoc、PIX、Nsight、RGP、VTune、PMC或Heap全量采集；这些工具若后续接入，必须以新的显式Collector能力替换或扩展Controller合同，不得运行时探测并fallback。

#### Scenario: GitHub基础CI运行

- **WHEN** repository policy、OpenSpec和portable unit tests job执行
- **THEN** 它们 MUST不创建或消费Performance Capture产物
- **AND** CI成功 MUST不被描述为Player性能通过

#### Scenario: Toolchain没有GPU Collector

- **WHEN** 作者完成第一版CPU Capture
- **THEN** Summary MUST只声明已采集的Unity与Windows Performance Toolkit CPU能力
- **AND** MUST不生成GPU通过结论或自动启动RenderDoc
