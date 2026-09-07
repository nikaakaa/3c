## ADDED Requirements

### Requirement: 性能采集必须通过唯一显式工作流执行

系统 MUST通过唯一 Development 命令服务提交固定请求，由 Development Run Host 拥有顶层状态、资源和进程组，由既有实现迁移的 PerformanceCaptureWorker 执行采集。Launcher、CLI 与 development.* MCP MUST只是薄控制面，不能复制 Build、Gate、Capture 或分析实现。现有 Player Agent、Scenario Runner 与 WPR/xperf/WPA 实现 MUST保留其领域责任；旧独立 Performance Controller 顶层运行状态、performance.* 别名、10 秒菜单和单 JSON 路径 MUST删除。

#### Scenario: 作者开始一次性能采集

- **WHEN** 作者从 Center 或命令行选择精确候选、Scenario、采样工具和 Profile
- **THEN** Host MUST固定请求并按机器资源规则启动唯一采集 Worker
- **AND** 同次运行 MUST只有一个顶层状态和进程组所有者

#### Scenario: MCP长任务恢复

- **WHEN** Agent通过显式 unity_instance 发起开发操作后 Unity 域重载
- **THEN** MCP MUST通过持久化 OperationId/RunId 恢复状态
- **AND** MUST不自动重新构建、采样或改用另一个 Unity 实例

### Requirement: Capture必须由同身份Smoke和Replay Gate准入

统一命令服务 MUST把本地运行拆成固定顺序的Smoke、Replay与Capture。Smoke MUST只执行Player启动、`HELLO -> READY -> STOP`和正常退出，不得启动WPR、Profiler Recorder或Fixed Input Replay。Replay MUST要求当前Player、Scenario与Profile身份的Completed Smoke Gate，执行精确Warmup暂停和完整Fixed Input/Camera Trace，发布Start Body、Input Sequence与Body Trajectory hash；不得启动WPR或Profiler Recorder。Capture MUST同时要求当前Completed Smoke Gate以及由该Smoke产生的Completed Replay Gate，任一manifest缺失、Faulted、文件hash断裂或身份不匹配时 MUST在启动Player前拒绝。

#### Scenario: Player启动合同损坏

- **WHEN** Smoke未在Ready timeout内完成HELLO与READY
- **THEN** 采集 Worker MUST报告Smoke失败，由Run Host通过统一进程能力停止本次Player并封存Faulted Gate
- **AND** MUST不启动WPR、请求管理员权限或产生ETL

#### Scenario: Replay行为闭包通过

- **WHEN** Replay完成声明的Warmup与Capture LogicTick总量
- **THEN** Replay Gate MUST记录匹配Trace的Start Body、Input Sequence与Body Trajectory hash
- **AND** Capture MUST只接受由当前Smoke Gate产生且Scenario、Player、Profile、输入与相机身份全部一致的Replay Gate

#### Scenario: 作者绕过Gate直接Capture

- **WHEN** 当前Smoke或Replay Gate缺失、Faulted、属于旧Player或不属于当前前序Gate
- **THEN** Launcher MUST禁用Capture，MCP与采集 Worker MUST拒绝请求
- **AND** MUST不启动Player、WPR或Profiler

#### Scenario: 旧入口仍被调用

- **WHEN** 代码、菜单或快捷键仍引用旧`SimulationPerformanceCapture`
- **THEN** 迁移 MUST视为未完成
- **AND** 系统 MUST不通过兼容wrapper或隐藏转发继续生成旧JSON

### Requirement: Performance Toolchain必须显式配置且严格预检

本机Toolchain Definition MUST显式保存WPR、xperf、WPA Exporter、WPA、Unity Profiler打开方式和Symbol Cache的精确路径与版本。采集 Worker MUST在启动Player前校验文件、架构、权限、磁盘、WPR owned instance能力与输出目录。缺失、歧义或非法配置 MUST使Capture失败，MUST不搜索另一安装、不改用Editor Play Mode、不关闭未知WPR session或选择其它Collector。

#### Scenario: WPR路径失效

- **WHEN** Request引用的WPR不存在或版本不满足正式Toolchain
- **THEN** 采集 Worker MUST在启动Player前发布Preflight Fault
- **AND** MUST不改用其它WPR副本、Superluminal或无CPU采样模式

#### Scenario: 系统已有其它WPR采集

- **WHEN** 采集 Worker无法创建自己的唯一WPR instance或发现同名instance已被占用
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
- **AND** 采集 Worker MUST在该边界写WPR start marker，Agent MUST在Recorder与binary log就绪后恢复同一Replay的Capture后缀
- **AND** MUST不使用冻结状态空等RenderFrame、固定秒数或第二份输入完成Warmup

#### Scenario: Scenario缺少相机输入

- **WHEN** Capture Profile要求比较Presentation帧但Scenario没有匹配revision的local camera Trace
- **THEN** Request preflight MUST拒绝该Scenario
- **AND** MUST不允许人工镜头输入或固定Camera Transform补齐


Scenario 的工作负载身份 MUST只包含输入、初始场景/roster、Ready规则、时钟与窗口等实验条件，候选的 Program/Projection 实现身份 MUST独立保存；不能因业务代码版本不同就认定录制输入不同。场景内容本身变化仍 MUST作为不同工作负载。

### Requirement: Capture Agent必须保持只读且不成为第二运行驱动

Player Capture Agent MUST只发布Runtime Ready、接收Warmup/Start/Stop/Fault命令、控制Profiler/Recorder和冻结采集身份。采集 Worker MUST建立只绑定`127.0.0.1`的临时TCP listener，并通过Run Host的统一进程能力把系统分配端口传给本次Player，Agent MUST拒绝其它transport身份。该专用控制连接与固定网络业务Slot分离，不构成游戏endpoint的动态fallback。Agent的transport reader与writer MUST只运行在独立后台线程，Unity主线程 MUST只交换有界命令字符串；连接、读写和释放 MUST不持有Unity主线程读取的状态锁，HELLO MUST先于READY进入唯一发送队列。取消请求 MUST由 Run Host 唯一消费并通过公共进程能力触发本次清理，采集 Worker 只完成已登记 Recorder/WPR 资源停止，不得等待 Player 确认才取消。旧Named Pipe传输 MUST不存在且不得作为fallback。Agent MUST不注册`IGameplayLogicTickTarget`、`IGameplayPresentationFrameTarget`或私有Update runner，不创建SimulationTick，不修改Character/World/Pipeline state，不调用WorldSolver、Program、Committer或Camera Rig。性能数据 MUST不进入Gameplay Snapshot、Pipeline Product或BTSMTL RuntimeDebugSession Capture Store。

#### Scenario: Session尚未Active

- **WHEN** 采集 Worker已连接但目标Session、roster或canonical start body尚未满足Scenario Ready合同
- **THEN** Agent MUST保持WaitingForRuntime且不得开始预热或录制
- **AND** MUST不通过额外Tick、场景搜索或默认Actor推进就绪

#### Scenario: Capture中途断开

- **WHEN** 采集 Worker loopback transport断开或Player检测到协议错误
- **THEN** Agent MUST停止自己拥有的Recorder和binary log并发布Fault
- **AND** Gameplay Session MUST继续按原正式驱动运行或由其自身生命周期结束

#### Scenario: Transport写入阻塞或作者取消Run

- **WHEN** Player transport写入未完成或作者取消当前 Run
- **THEN** Unity主线程 MUST保持可运行且不得等待transport状态锁
- **AND** Run Host MUST立即清理本次 Player 与已登记 WPR 资源并发布 Cancelled

### Requirement: Performance Player必须是可符号化的本机诊断闭包

正式性能采集 MUST只消费 Windows x64 IL2CPP Development Local Fixed 候选。DevelopmentCandidateBuildWorkflow 的显式产品 adapter MUST复用既有 Player、符号与 Scenario 构建；关闭 Deep Profiling、Script Debugging 和 Autoconnect Profiler。同盘短路径 Unity 临时输出 MUST经过产物库 staging 全量校验后原子发布到 Candidates/<ProductId>/<CandidateId>，不能成为第二可运行根。源码、Build 工具、Unity、Program/Pipeline/Projection/Solver、文件闭包和 canonical DiagnosticCapabilitySet MUST完整封存。

DiagnosticCapabilitySet MUST按稳定 CapabilityId 排序；每项包含 Mode、Event Set、Sampler Set、Schema、Program、维度、packet capacity 和 transport 身份，Program 身份闭合 AOT Handler、Generator 与 layout。Disabled MUST编译期排除对应领域 Definition、Handler、Generated Program、页、队列与 interest；Capture MUST只包含所选闭包。公共 Build Request MUST不增加 Foot 等领域专属开关，不通过运行时 bool 或空实现冒充关闭。外部 KK 框架接口 MUST由其原接入 change 确认，不在本模块重建。

#### Scenario: 同一工作区后台构建性能候选

- **WHEN** 作者提交明确 Local Fixed 配方并满足机器构建/内存条件
- **THEN** 唯一 Build Workflow MUST通过临时 Unity 进程生成可符号化候选并在结束后退出
- **AND** MUST不发布到 Library/Performance 或 Build/Network

#### Scenario: 构建领域采样变体

- **WHEN** 配方启用合法 DiagnosticCapabilitySet
- **THEN** Managed 与 IL2CPP MUST包含精确声明的 AOT 采样闭包并在 manifest 记录身份
- **AND** MUST不包含领域离线 Analyzer 或运行时表达式编译

#### Scenario: 构建诊断全部关闭的基线

- **WHEN** 配方声明所有领域采样能力 Disabled
- **THEN** 对应诊断依赖与生成代码 MUST在编译闭包中不存在
- **AND** MUST不使用空目录、Null Adapter 或布尔开关代替证明

#### Scenario: 候选符号不匹配

- **WHEN** Player、符号或 Scenario 引用与候选 manifest 不匹配
- **THEN** Host MUST在启动前拒绝
- **AND** MUST不重建、搜索替代符号或改选候选

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

Recording开始前Agent MUST按Profile计算容量并一次创建Recorder，Unity MUST把binary Profiler数据写入当次staging。采集 Worker MUST只在Smoke和Replay Gate已通过、当前Capture Player完成READY且Replay到达精确Warmup暂停边界后，才启动`windows-wpr-cpu/1`命名实例采集Sampled Profile、Process/Thread、Context Switch和声明的File IO，并在Scenario精确开始/结束写入唯一WPR Marker。停止后xperf MUST按两个Marker的精确微秒区间、目标PID、`PROFILE`事件和匹配PDB生成函数Uni-Inclusive/Exclusive栈报告；WPA Exporter MUST通过结构化`exporterconfig`按同Marker区间导出目标Player Context Switch与线程栈表。项目 MUST不解析ETL、PDB或实现采样器。

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

Capture MUST在项目共享产物库 .staging 中写入，保留 Unity raw、Metric/Span、WPR ETL、精确标记、基础 xperf/WPA 转换、日志、进程与完整能力子产物。全部必需基础产物和引用哈希完成后 MUST原子发布 Captures/<CaptureId>；统计 summary、预算和领域规则 MUST在独立 Analyses/<AnalysisId> 下生成，不能回写 Capture。

每个领域 Capability MUST通过外部 KK 框架的正式生命周期独立拥有 Session、cadence、typed lineage、sample key、packet 与 Writer，并由框架 Host Finalizer 生成基础表及 manifest。Worker MUST只按 CapabilityId 编排并引用子 manifest，不解释领域维度、不合并 packet 流、不复制 Bridge/Adapter/字段映射。Disabled MUST不创建空占位产物。基础采样失败 MUST保留故障与原始证据，但不能成为可比较 Capture；旧无来源数据只能作为 History，不兼容读取或升级。

#### Scenario: 基础转换失败

- **WHEN** Unity raw与ETL存在，但所选必需xperf/WPA基础转换失败
- **THEN** 基础 Capture MUST标记故障并保留原始文件与工具错误
- **AND** MUST不能成为对应指标的可比较基线

#### Scenario: 离线统计失败

- **WHEN** 基础 Capture 已完整封存而 Analyzer 失败
- **THEN** Analysis MUST标记故障并引用该 Capture
- **AND** 已封存 Capture 状态及内容 MUST保持不变

#### Scenario: 重算报告

- **WHEN** 作者选择满足输入合同的新统计器分析同一 Capture
- **THEN** 系统 MUST生成新的 AnalysisId
- **AND** MUST不重新启动 Player 或覆盖原报告

### Requirement: Budget和Baseline比较必须使用严格相同测量条件

Budget MUST显式声明目标 FPS、线程、Tick、GC 与业务指标预算，超限是评价结论，不是采样故障。Baseline MUST由作者选择精确 Analysis；Comparer MUST按 versioned-performance-analysis 核对相同工作负载、测量工具、Metric/Probe 语义、完整 DiagnosticCapabilitySet、分析合同及机器环境。能力 Mode、Program、Sampler Set、Schema、capacity 或 transport 不同 MUST拒绝。WPR 配置只比较内容身份；源码路径、行号和被比较 CandidateId MUST不能作为相等条件。

不同预算结果 MUST不能直接比较，但相同测量数据 MAY按同一显式新预算重新评价。BehaviorComparison MUST独立呈现，不能把相同输入或采样成功等同于玩法一致。任何缺失、不支持或污染 MUST给出结构化原因，不选 latest、不映射旧格式、不按比例修正环境差异。

#### Scenario: 候选超过同一预算

- **WHEN** 两份合法 Analysis 在统一预算下得到候选 Animation P95 超限
- **THEN** 报告 MUST保留采样完整性并列出数值、差值和预算结论
- **AND** MUST不把超预算自动解释为回放行为错误

#### Scenario: 两个worktree仅配置路径不同

- **WHEN** 工具、配置内容和其余测量条件相同
- **THEN** 比较 MUST允许来自不同物理目录
- **AND** 必须分别保留原 SourceMap 供跳转

#### Scenario: 分辨率不同

- **WHEN** 相同录制输入的两份采样分辨率不同
- **THEN** Comparer MUST拒绝并列出冲突
- **AND** MUST不进行比例归一化

### Requirement: 性能采集不得扩张现有CI或外部分析范围

本能力 MUST只作为本地显式诊断工作流。现有GitHub基础CI MUST不构建Performance Player、不启动Unity、不运行Scenario、不执行WPR/xperf/WPA、不上传Capture，也不增加性能测试job。第一版 MUST不自动化RenderDoc、PIX、Nsight、RGP、VTune、PMC或Heap全量采集；这些工具若后续接入，必须以新的显式Collector能力替换或扩展采集 Worker合同，不得运行时探测并fallback。

#### Scenario: GitHub基础CI运行

- **WHEN** repository policy、OpenSpec和portable unit tests job执行
- **THEN** 它们 MUST不创建或消费Performance Capture产物
- **AND** CI成功 MUST不被描述为Player性能通过

#### Scenario: Toolchain没有GPU Collector

- **WHEN** 作者完成第一版CPU Capture
- **THEN** Summary MUST只声明已采集的Unity与Windows Performance Toolkit CPU能力
- **AND** MUST不生成GPU通过结论或自动启动RenderDoc
