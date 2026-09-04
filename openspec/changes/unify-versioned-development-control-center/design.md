## Context

项目目标是 Gameplay 客户端迭代。Center 负责让作者选版本、运行操作、查看结果；网络产品是其中的压力场景，不是所有本地实验必须经过的入口。现有两套工具各自完成了部分职责，需要合并公共所有权，保留已有领域实现。

本设计以 2026-09-04 工作区审计为依据，不把 active delta 当成已安装 current spec。其他任务正在修改 Performance 与 Generated Diagnostic Sampling 接口；相关冲突只记录在本提案，不覆盖它们的工作区文件。

## Goals / Non-Goals

目标：版本可追溯；多 worktree 的产物集中管理；同次运行只有一个生命周期所有者；测量与分析版本可分别升级；比较有明确准入理由；历史不依赖工作区仍然存在。

非目标：任意脏目录构建、远程构建农场、常驻第二 Editor、机器间性能归一化、商业发布系统改造、网络自动化回放能力补造、第二诊断框架、通用插件反射发现、业务 GC 优化、自动提交作者代码、自动归档未验收 change。

## Decisions

| 决策 | 方案一及业务取舍 | 方案二及业务取舍 | 本提案采用 |
|---|---|---|---|
| 源码来源 | 干净提交：版本容易复现，作者需先提交准备完成的输入 | 封存未提交修改：试验更快，但必须同时保存补丁、新文件、依赖和构建期间隔离规则 | 干净提交，延续已有 Center 规则 |
| 产物位置 | 独立本机共享库：低运维成本，worktree 删除不影响证据；需明确磁盘和清理责任 | 远程制品服务：跨机器共享与备份更好，但增加服务、认证与上传成本 | 显式配置本机共享库 |
| 生命周期 | 将现有 Orchestrator 提升为公共 Host：取消、资源隔离和状态一致；需要迁移两个入口 | Center 仅汇总两个独立控制器：接入改动少，但每种任务继续实现自己的资源锁与恢复规则 | 公共 Host，采集保留为专业模块 |
| 性能并行 | 同机排队：环境较容易控制，正式测量吞吐较低 | 多机并行：吞吐较高，但必须各自建立基线并承担机器配置维护 | 同机排队；本期不实现远程调度 |
| Unity 构建驻留 | 按需后台构建后退出：内存可回收，首次导入较慢 | 常驻多个 Editor：切换和反复构建较快，持续占用大量内存 | 按用户在关联任务确认的约束，采用按需后台构建 |
| 不同测量工具的结果 | 固定同一组精确采集工具重测：成本较高，结论来源清楚 | 引入工具间等价认证：能复用更多历史，需要持续证明开销与语义相等 | 固定精确测量工具，不建立兼容白名单 |
| 分析升级 | 原始数据与报告分开：能够重算与追溯，目录和状态增加一层 | 每次报告变化重新采样：流程较短，但重复运行成本高，难区分算法与游戏变化 | 分开封存，Center 合并展示 |

### 1. 谁负责什么版本

| 对象 | 唯一拥有者 | 人维护的内容 | 自动生成的内容 |
|---|---|---|---|
| 源码 | Git 与源码发布器 | 正常提交；作者标签 | SourceCommit、SourceTree、源码包与解析依赖哈希 |
| 游戏候选 | Development Candidate Build Workflow | 显式产品和构建配置 | CandidateId、文件闭包、所用 Build 工具身份 |
| 工具 | 各工具模块自己的发布描述 | ToolId、工具版本、接口版本、输出格式版本 | 工具源码来源、二进制包哈希、依赖闭包 |
| 性能 Metric | 现有业务 Metric Owner | 单位、采样范围、聚合规则及语义修订 | 排序后目录身份 |
| 领域采样 Schema | KK Generator 与对应领域声明 | 框架版本由独立仓库维护；领域声明由领域维护者修改 | Schema、Program、Sampler Set 与 AOT 闭包身份 |
| 领域分析 | 现有 Analyzer/Operator/Plan Owner | 算法和规则修订 | Analyzer 包与 Plan 内容身份 |
| 场景与配置 | 场景发布器 | 输入、初始条件、窗口、质量设置、预算 | 不可变 ArtifactId 和内容哈希 |
| 一次运行、采样或分析 | Run Host 与对应结果发布器 | 作者选择准确输入与基线 | RunId、CaptureId、AnalysisId、ComparisonId |

显示工具版本采用 `主版本.次版本.修订号`：接口或既有输出含义不兼容时升主版本，增加显式可选能力升次版本，修复实现升修订号。任何实现更新仍由自动文件身份识别，不能因维护者漏改显示版本而被视为同一工具。数据格式版本只说明如何读取，不证明指标意义和测量开销相同。

版本描述放在模块自己的正式源码/包描述中；删除发布器替其他模块写死版本的常量。旧报告保留旧版本记录，不要求新工具保留旧格式 reader。第三方安装工具由本机配置固定精确文件及必要依赖，不擅自复制安装目录或下载替代品。

### 2. 身份关系与防止互相包含

`SourceId -> CandidateId -> RunId -> CaptureId -> AnalysisId -> ComparisonId` 是引用关系，不是一串必须同值的版本号。

- SourceId 由提交、树及正式构建依赖描述计算。标签、分支名、worktree 路径和时间不进入内容身份。源码 Git bundle 保留所选提交；未被 Git 保存但参与构建的依赖必须作为明确版本的依赖产物封存或声明精确安装身份，不能只记录一个仍会移动的 file 路径。
- CandidateId 在构建前由 ProductId、SourceId、构建配方、Build 工具、引擎/目标、业务配置、嵌入工具与 DiagnosticCapabilitySet 计算，可供子产物绑定；最终文件闭包单独由 Candidate manifest/hash 封存。实际运行同时锁定 CandidateId 和 manifest/hash，避免 CandidateId 与内含该 ID 的二进制互相求哈希。相同输入声明却产生不同文件时拒绝覆盖，不冒充相同产物。相同源码不同配置或织入版本可以发布不同候选，标签不再承担 `label + commit` 的唯一键职责。
- 工具包身份按规范化相对路径与内容计算，固定排序；排除自身 identity 字段、显示标签、时间和物理安装位置。实际二进制中的差异仍保留在精确包身份中，不能为让比较通过而忽略文件变化。
- RunId 是每次调度独立的 UUID，时间用于展示；相同候选重复运行仍是不同 Run。RunRequest 在入队时冻结，之后只追加运行状态与结果引用。
- 报告引用输入 manifest/hash、Analyzer、算法、Plan 和配置。manifest 自身哈希由上一级引用者保存，不把自己的哈希再放回参与计算的正文。
- 不同 worktree 使用同一测量版本时，显式选择共享库中同一份已发布工具包；不要求各 worktree 重新编译出完全相同的工具二进制。

### 3. 正式产物位置与 worktree

项目拥有稳定 ProjectId，并通过正式本机注册配置显式选择 ArtifactRoot。ArtifactRoot 必须位于该项目所有已注册 worktree、客户端工程和 Library 之外；缺失配置报错，不推导默认根。示例位置为 `D:/Unity_Project_1/3C-Artifacts`，本提案不创建该目录或写入本机配置。

```text
<ArtifactRoot>/<ProjectId>/
  Sources/<SourceId>/
  Tools/<ToolId>/<ToolArtifactId>/
  Scenarios/<ScenarioId>/
  Candidates/<ProductId>/<CandidateId>/
  Runs/<RunId>/
  Captures/<CaptureId>/
  Analyses/<AnalysisId>/
  Comparisons/<ComparisonId>/
  History/<EvidenceId>/
  .staging/<OperationId>/
```

Catalog 只索引已原子发布的精确 manifest，不递归猜测候选、不建立 latest 链接。多个发布者写独立 staging，通过同卷原子发布和短临界区更新索引；重复内容返回已存在身份，文件不一致报错。跨盘 Unity 构建先在被显式登记的同盘短路径临时工作区完成，再复制到产物库 staging 全量校验后原子发布；临时目录不接受 Run 消费。

每个 worktree 独立拥有 Assets/Packages/ProjectSettings、Library 和 Temp，不要求常驻 Unity Editor。登记记录工作区路径、Git 公共目录和 HEAD；已有交互 Editor 的操作必须显式提供并握手核对 unity_instance，后台构建则以精确 projectPath、Unity 安装身份和 OperationId 绑定工作区，不要求 MCP 连接。

本机后台构建使用 Unity 的 batchmode 与 executeMethod 调用同一正式 Build Workflow，成功/失败后都退出。nographics 只用于该产品已验证支持的构建配方；不支持时明确拒绝，不静默切换图形模式。Unity 仍会导入资源并占用内存，这不是普通 dotnet 编译。全机一次只允许一个受管理的 Unity 构建；目标工程已由交互 Editor 打开时，明确选择交互构建或等待其释放，不能并开同工程。交互与后台只是同一 Build 服务的显式执行方式，不是失败后的 fallback。

复用“并行开发能力”任务的 ProjectEditorPreferences、SessionState 隔离以及 ThirdPersonPerformanceBuildCommandLine 接缝，迁移为统一 Development 命令参数与持久化请求。Center 不自动创建 worktree、不复制或共享 Library、不关闭作者的主 Editor。

Build 在准备正式输入之后检查干净状态，再固定提交和依赖。从开始构建到发布期间，同一工作区只接受一个受管理 Build；输入变化、HEAD 变化、依赖变化或监测完整性丢失使构建失败，不能发布混合版本。准备生成了 Program/Projection 时，Center 列出需要作者提交的变化，不自动提交或继续构建。

删除 worktree 只移除工作地点登记。Sources、工具、候选与报告仍在产物库；删除产物必须检查活跃任务和所有保留的引用，不通过自动清理破坏已有报告可追溯性。

### 4. 模块边界与迁移落点

| 模块 | 输入 | 输出与责任 | 现有实现的去向 |
|---|---|---|---|
| DevelopmentArtifactStore | 正式项目配置、manifest、文件集合 | 发布/读取不可变产物、引用关系、目录索引 | 提取两条链的身份、staging、文件校验公共能力 |
| DevelopmentCandidateBuildWorkflow | 精确工作区、产品、源码、工具、构建配方 | 一个完整候选或结构化失败 | 从 Network Workflow 提升；Performance Build 成为显式产品 adapter |
| DevelopmentRunHost | 固定 RunRequest、角色计划、资源声明 | Run 状态、本次进程组、取消与结果引用 | 重命名并扩展既有 Network Orchestrator，删除旧顶层入口 |
| PerformanceCaptureWorker | Host 分配的运行上下文、Player、场景与测量工具 | 原始性能证据和基础 Capture manifest | 迁入原 Controller 的 Agent 协议、Gate、WPR/xperf/WPA 采集实现 |
| PerformanceAnalyzer | Completed Capture、统计器、算法/Plan/预算 | 独立 Analysis、Comparison | 从原 Publisher 提取下游计算；不启动 Player 或改 Capture |
| KK 采样与领域分析 | 原有 Event/Fact Root/Schema/Plan | 自己的基础子产物与离线结论 | 保持原所有者，Center 只引用 manifest |
| Launcher 与 CLI/MCP | 作者操作 | 提交统一命令、读取轻量状态 | 一个 Center；删除独立 Network/Performance 顶层调度与旧别名 |

公共纯合同放在既有 `com.thirdperson.tooling-contracts` 内按模块划分；只有 Player 握手确实需要的 DTO 进入独立 Runtime 合同程序集。进程、磁盘目录、工具发布、报告等 Host 合同继续 Editor/Host-only，不让 GM/编排/分析程序集进入 Player。

最终精确工具项目为 `Tools/ThirdPersonDevelopment/ThirdPerson.Development.RunHost.csproj`、`Tools/ThirdPersonPerformanceCapture/ThirdPersonPerformanceCapture.Worker.csproj` 和 `Tools/ThirdPersonPerformanceCapture/ThirdPersonPerformanceCapture.Analyzer.csproj`。前两项来自现有工程职责迁移，分析器为独立发布单元；删除旧 Orchestrator/Controller 工程和策略允许项，不保留转发 exe。

### 5. 运行、采集与进程所有权

Run Host 是唯一顶层运行状态和 Windows Job Object 所有者。产品 adapter 提供角色计划、配置生成与业务 Ready 条件；通过 Host 的统一进程能力启动/结束声明角色，不独立扫描和 Kill 进程。Collector Worker 通过同一能力申请 Player 与外部工具，拥有本次 Recorder/WPR 资源的领域清理，不再拥有第二套 Run 状态或取消文件。

Smoke、Replay、Capture 是同一 Host 下的不同明确操作。已有同候选/场景/模式的 Smoke -> Replay -> Capture Gate 关系保持，不能拿另一个版本的 Gate 放行。RunRequest 同时固定 Host、产品 adapter、Player、采集模块、安装工具、场景、嵌入诊断能力和测量配置。Analyzer 在另一个明确操作中读取数据，其失败不回写 Capture 成败。

域重载后界面通过持久化 RunId、Host 心跳和进程启动身份恢复显示，不重新启动任务。机器重启或所有者失联时，精确核对 Job/进程/WPR 所有权；不能证明已停止时标记资源状态未知并拒绝复用。停止请求只作用于目标 Run。跨权限无法读取进程详情不等同于进程已退出。

### 6. 同机并行与性能独占

机器资源协调数据位于显式配置的 MachineControlRoot，独立于项目与 ArtifactRoot。稳定机器身份、命名互斥和持久化租约由 Host 公共模块拥有；旧/新 Host 工具版本使用同一资源命名空间，不因工具版本或 worktree 不同产生两套锁。租约格式不受支持时拒绝调度，不另建锁文件继续。

网络 Slot 的租约覆盖 SlotId 及实际 endpoint 资源。已有 rollback-a/b 配置保留，不动态换端口。同一 Slot 跨两个 worktree 也只能有一个运行者；不同槽位满足全局端口检查后可并行。

受管理的构建、功能 Player、Replay、分析导出持有机器负载共享租约，资源足够时多个任务可以并行；Unity 构建另受全机单实例构建租约约束。正式性能操作申请独占租约，从 Player 启动、Warmup 到采样结束和 Recorder/Collector 停止保持独占；已有任务完成后再开始，不抢占。等待独占时阻止新的重负载任务不断插队。没有资源时表现为排队与明确等待原因，不隐式降级为无保护采样。

正式机器配置显式声明启动所需可用内存、运行中最低余量、连续低内存判定窗口与终止超时，不把关联任务临时使用的约 2 GB 硬编码为所有机器规则。内存不足时不启动；运行中持续低于下限时取消自己拥有的构建并记录 MemoryBudgetExceeded，保留该 worktree 已完成的导入缓存与失败日志，未校验的输出不能发布候选。不自动关闭其他 Editor，不自动循环重试。下一次显式操作可复用 Unity 自身合法缓存，但仍重新执行完整输入与产物验证。

外部编辑器、游戏或其他程序并不受这些锁控制。Capture 保存电源、CPU/GPU/驱动、系统版本、画质与可观测负载信息，标明独占只覆盖受管理任务；检测到明确污染时记录受污染结果并拒绝作为自动性能基线，不声称机器绝对空闲，不终止未知进程。人工或外部进程干扰仍需要结合证据判断。

### 7. 基础采样、分析与比较

Capture 保存原始 Unity raw、Metric/Span、ETL、标记边界、进程/日志以及声明能力的基础子产物。xperf/WPA 是版本化基础转换阶段，保留其工具身份和原始输入；必需基础转换失败时该 Capture 不可用于对应性能比较，但原始证据仍保留。统计、预算、热点排序与领域规则属于下游 Analysis，失败不破坏已经封存的基础采样。

三类状态分开：采集是否完整；分析是否成功；业务/预算结论是什么。没有执行、证据缺失、条件不适用和通过不能混成绿色。Replay 完整性与两版本轨迹是否一致分别显示；相同输入不自动等于行为相同，性能差异报告必须带行为差异状态。

比较准入分两部分：

1. 测量条件：输入与初始场景内容、Variant/roster、Warmup/窗口、时钟与相机输入、构建模式/平台、Unity、硬件/驱动/电源与画质、Collector/嵌入采样工具的精确身份、完整 DiagnosticCapabilitySet、Metric/Probe 度量定义与容量布局一致。
2. 分析条件：相同 Analyzer 精确身份、算法修订、同一单位/范围/聚合及显式输入 Schema 合同。预算是本次评价的输入；原始数值可以在同一新预算下重新评价两边，不让不同预算结果直接相减。

CandidateId、源码提交、业务方法体和编译出的游戏二进制是被比较对象，允许不同。SourceMap 的文件路径和行号只用于定位，不进入测量身份。Scenario 的工作负载身份与候选的 Program/Projection 实现身份分开，避免把业务版本变化误判为录制输入变化；初始场景、roster 或输入本身变化仍必须拒绝当作同负载比较。

InstrumentedArtifactIdentity 保留完整 manifest、SourceMap 和文件证据；MeasurementIdentity 只组合固定探针签名、Metric 语义、织入工具精确版本/包、模式、布局与容量。声明变化或织入版本变化会改变 MeasurementIdentity；只改方法体或移动行号不改变它。稳定 PointId 使用程序集/类型/方法签名/声明身份，不使用位置或本次 BuildId。

仅升级 Center 界面不改变已选择测量工具时，可继续比较。升级采样器或织入器后，新旧 Capture 保留历史，但不跨工具自动判优；给 A/B 都选同一套工具重新采集。升级 Analyzer 后，只有新分析器明确支持且证据充分的原始数据可以重算；不增加旧 Schema reader、自动字段映射或兼容修补。

### 8. GC 分配归因

已有 `GC Allocated In Frame` 是整个 Player 每帧托管分配量，不能单独说明回收耗时、泄漏或分配函数。新增显式托管分配诊断 Profile，使用当前 Unity 2022.3 支持的原生 Profiler 分配调用栈能力；构建/采集前验证能力，证据不完整时输出不可用或 MissingEvidence。

采集仍使用唯一 Agent/Worker，不创建领域 Collector 或自研 Heap 解析器。分析使用 Unity 支持的数据读取边界；若所需读取只能在 Editor 内进行，发布独立 Editor 分析操作并显式绑定 unity_instance，在 GUI 回调外调度。报告列出完整/未解析字节数、次数、函数/调用路径与范围，区分可归因到业务或采样设施的部分，不能拿 CPU 热点代替分配归因。

调用栈捕获会改变开销，因此此模式独立标识，不能作为普通性能基线。诊断目标是找到下一项代码修改的证据，本 change 不预先指定 GC 根因或顺手修改玩法模块。

## Migration Plan

1. 审批本提案的目录、公共 Host 和结果状态边界后，按 `spec-audit.md` 交接重叠 active delta 所有权；不自动 archive。
2. 提取现有公共身份/产物能力，发布模块自有版本描述，建立正式项目与机器配置及工作区登记。
3. 把 Network 和 Performance 构建迁入共同候选发布；把 Host 生命周期与 Collector/Analyzer 职责切开。每次迁移同时切换全部消费者并删除对应旧入口，不建立双写期间。
4. 接入 Center、CLI/MCP、引用关系与比较；保留现有产品/框架适配实现，未完成的产品操作显式不可用。
5. 核对旧产物所有权、活动进程和引用；不可追溯产物不能导入候选。需要保留的已有采样证据登记为只读 History，并明确不可运行/不可比较；确认不再使用的旧目录删除。所有需要的证据已经封存后删除旧正式根、工具工程和配置。
6. 最终同步 current specs 与 project.md，只在实现确实成立后更新已安装能力描述。人工端到端验收不写入 tasks；报告分别陈述完成代码、编译结果与实际运行证据。

## Risks / Trade-offs

- 源码和二进制都封存会增加磁盘使用；显式删除受引用检查约束，不能为省空间静默破坏复现。
- 当前主工作区有大量并行修改，干净提交规则会阻止直接构建；Center 应给出具体变动列表，不能替作者提交其他任务代码。
- “并行开发能力”任务已确认后台 Unity 首次导入会从约 1 GB 增长到接近 6 GB，不能把启动时内存检查当作全程安全保证。其独立程序集编译已通过，完整 Player 构建/运行尚无成功证据。
- 全部功能同时运行不等于性能可信；机器独占仅对本系统受管理任务有效。
- 与 KK 接入的冲突包括现行 active 文本混用 0.5.2/0.4、旧事件 DTO 描述；需由对应框架接入 change 确认实际包合同，本 change 不复制或重写该框架。
- 原工具入口和产物格式发生破坏性变化；旧证据可保留，但旧软件不继续作为活动运行路径。

## 技术依据

- Unity 2022.3 的命令行参数说明支持 batchmode、executeMethod、projectPath 与 quit，并明确不能在同一工程已由 Editor 打开时再次 batch 启动；nographics 不初始化图形设备且不支持需要 GPU 的 GI 烘焙。构建配方据此显式声明能力，不自动切换模式。[Unity 官方说明](https://docs.unity3d.com/2022.3/Documentation/Manual/EditorCommandLineArguments.html)
- Unity 2022.3 的 Profiler.enableAllocationCallstacks 记录托管 GC.Alloc 调用栈，并要求启用 Profiler。该 API 的存在不等于本项目 IL2CPP 归因导出已经验证成功，因此提案保留能力检查与证据覆盖状态。[Unity 官方 API](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Profiling.Profiler-enableAllocationCallstacks.html)
