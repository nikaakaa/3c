# Design

## 目标与非目标

目标是让作者从一套明确源码和生成物构建出多个可并存 Network Test Candidate，并在同一台机器上选择精确 Candidate 与 Session Slot 启动完整测试环境。任何日志、GM 响应和进程都必须能反查 Candidate、Tool Bundle、Run 与 Slot，任何不匹配都必须在启动前失败。

本 change 不把 GM 扩张成源码管理器或 Gameplay Authority。Build Workflow 拥有 Candidate，Test Orchestrator 拥有 Run，单场 GM 只拥有该 Run 的命令请求。Git worktree 由作者或 Codex 工作流在系统外创建；项目工具只验证当前 worktree，不移动分支、不stash、不合并代码。

## 术语

| 术语 | 含义 | 不负责 |
| --- | --- | --- |
| Network Test Product | UnityAuthority、DotRecastAuthority 或 DeterministicRollback 这类稳定产品 | 某次源码版本、某次运行 |
| Candidate | 某 Product 从一个干净 Git 提交构建出的不可变完整闭包 | 端口、token、PID、运行状态 |
| Tool Bundle | Candidate 携带的 Test Orchestrator、启动 adapter、GM 等精确工具闭包 | Gameplay 状态 |
| Session Slot | 作者配置的本机端口和窗口资源集合 | 动态找空闲端口、负载均衡 |
| Run | 一个 Candidate 在一个 Slot 上的一次运行实例 | 编译、发布、修复 Candidate |
| Test Control Center | Launcher 中选择 Candidate、Slot 和 Run 的作者界面 | 重活、进程 Pump、GM 命令业务 |
| GM Console | 连接一个已运行 Session 的命令面 | Candidate、Slot、其它 Session |

## 唯一链路

```text
clean Git worktree
  -> explicit CandidateLabel
  -> NetworkTestProductBuildWorkflow
     -> pre-Prepare Git identity
     -> Product Prepare/Program/Projection validation
     -> post-Prepare Git identity
     -> Player + runtime artifacts + tool bundles
     -> schema v3 exact manifest
     -> immutable Build/Network/<Product>/<CandidateId>

Tools/3C/Launcher Test Control Center
  -> select exact Candidate + explicit Session Slot
  -> launch candidate-owned ThirdPerson.NetworkTest.Orchestrator
     -> validate Candidate/tool/slot
     -> create Run directory and RunManifest
     -> create run-owned endpoint/token/config files
     -> invoke candidate-owned product launch adapter
     -> own process group and readiness
     -> publish RunStatus
     -> open candidate-matched GM for Rollback
```

Build 与 Run 保持分离。Run 可以生成本次运行所需的实例配置，但不能生成或修改 Candidate 文件、Program、Projection、Server Product 或工具。这里的 Run 配置不是 Build fallback，而是正式部署实例：Candidate 表达“运行什么”，Run 表达“在哪里、以哪个实例身份运行”。

## Candidate 源码身份

作者必须显式填写短的 kebab-case `CandidateLabel`。Build 从当前 Git worktree读取完整40位 `SourceCommit` 与 commit tree hash `SourceTreeHash`，并按以下格式建立目录身份：

```text
CandidateId = <CandidateLabel>-<SourceCommit前12位>
```

CandidateLabel用于作者识别，SourceCommit和SourceTreeHash用于证明内容。分支名、worktree目录、构建时间和EditorPrefs都不参与身份。`BuiltAtUtc`可以进入manifest展示，但不得参与目录选择、匹配或覆盖判断。

Build 在两个边界检查 worktree：

1. Prepare 前必须是Git worktree、HEAD明确且没有被跟踪修改、未暂存修改或未跟踪的非忽略项目输入。
2. Product adapter完成Program、Projection、Scene和Server输入准备后，HEAD与tree必须不变，worktree仍必须干净。

如果Prepare产生新的正式生成物，Build必须在Player构建前失败并列出变化。作者先审查并提交生成物，再以该commit重跑。这样Player永远不包含“commit之外刚生成但未提交”的资产。Build目录、Library和Git已忽略本机文件不属于源码脏状态。

## 版本化目录与保留

正式布局迁移为：

```text
Build/Network/
  UnityAuthority/<CandidateId>/
  DotRecastAuthority/<CandidateId>/
  DeterministicRollback/<CandidateId>/
  RunLogs/<Product>/<RunId>/
```

Product目录只作为Candidate容器，不再直接包含Player、Server或Product manifest。Candidate先在同一Network分区的短临时目录完整构建并校验，再原子移动到最终Candidate目录。最终目录已存在时Build明确失败，不比较时间、不覆盖、不合并。

Test Control Center只枚举三个显式Product根的一层Candidate目录，并要求目录名、manifest CandidateId、ProductId与完整hash全部匹配。它不递归搜索其它位置、不按时间选择最新、不读取旧固定根。旧schema v2正式产物在迁移时直接删除；不提供复制、升级或兼容reader。

候选清理由作者显式执行。删除前必须确认目录位于精确Product根、manifest合法、没有Active/Starting Run引用且没有该Candidate拥有的进程。系统不按数量、时间或磁盘阈值自动删除，也不把删除失败的Candidate隐藏。

## schema v3 Candidate Manifest

`NetworkTestProductManifest`升级到schema v3，至少保存：

- CandidateId、CandidateLabel、SourceCommit、SourceTreeHash、BuiltAtUtc。
- ProductId、NetworkModelIdentity、RuntimeTopologyIdentity。
- Program、Projection、Pipeline、Solver、World和Server Product正式身份。
- Runtime artifacts[]及其RoleId、Kind、ProductId、entry point、configuration identity、manifest path/hash和exact closure。
- ToolBundles[]，每项包含ToolId、ToolVersion、ContractVersion、ArtifactRoleId、EntryPoint、ConfigurationIdentity和BundleHash。
- Candidate-owned Session Plan及其schema/hash。
- Player scene、target、scripting backend、Build options。
- 全部正式文件的相对路径、长度与SHA-256。

`BuildId`时间戳字段从Network Product、Server Product和Rollback工具目标身份中删除。需要显示构建时刻时只读BuiltAtUtc。schema v2 reader、旧BuildId比较和固定ProductRoot validation同时删除，不保留两种manifest语义。

## Tool Bundle 与工具版本

工具版本不是自动更新系统。Candidate必须携带运行它所需的精确工具，并通过Tool Bundle身份绑定：

```text
ToolId
ToolVersion
ContractVersion
ConfigurationIdentity
BundleHash
```

第一项公共工具是`thirdperson.network-test-orchestrator/1`。它作为普通.NET 8 Windows executable发布到每个Candidate，由Launcher只传递Candidate manifest、SlotId和Run根。产品adapter继续拥有具体拓扑知识，并把产品启动adapter和Session Plan发布进同一Tool Bundle；公共Orchestrator不按ProductId、Network Model或文件名分支猜测进程。

Deterministic Rollback另外发布`thirdperson.rollback-gm/1`。GM Tool Identity包含ProtocolVersion和按稳定顺序计算的CommandCatalogHash；Catalog Hash覆盖命令Id、命令Version、权限、参数合同和结果合同。GM executable、依赖、静态容量策略、Tool Manifest与产品启动adapter共同进入BundleHash。

Launcher或Orchestrator发现ToolVersion、ContractVersion、BundleHash、entry point或Candidate引用不匹配时直接拒绝。系统不从全局Tools目录找最新版、不下载旧版本、不加载兼容adapter，也不拿仓库当前脚本替换Candidate工具。

GM与Network Test的纯合同Module唯一源码位于仓库级`3cDemo/Shared/UnityPackages/com.thirdperson.tooling-contracts`。Unity Editor通过正式local UPM dependency消费Editor-only asmdef，外部.NET工程直接编译同一源码；不得在Unity客户端`Assets`与Server/Tools之间复制合同。Unity Player不引用该Package。

## Candidate-owned Session Plan

每个Product adapter在Build阶段输出类型化Session Plan，显式声明：

- 有序Process Role和其Runtime Artifact。
- 参数合同和允许从RunManifest读取的值。
- 进程可见性、日志归属、启动依赖和ready条件。
- 所需逻辑Endpoint key和窗口Role。
- 失败时的当前Session清理顺序。

公共Orchestrator只解释正式合同，不执行任意shell文本、不扫描目录生成进程、不从连接顺序推断role。现有三个PowerShell启动脚本中的产品规则迁入candidate-owned启动adapter；仓库当前脚本不再是Run依赖。若保留PowerShell作为正式adapter，它必须随Candidate发布并参与Tool Bundle hash，只接受RunManifest，不提供默认ProductRoot、默认端口或StopExisting。

## Session Slot

Editor-only正式Slot Profile定义稳定SlotId和一组全局不重叠资源。Build把允许的portable Slot Catalog发布进Tool Bundle。每个Slot至少包含各Product可能消费的逻辑endpoint端口与窗口布局；Product Session Plan只读取自己声明的key。

本change为DeterministicRollback提供至少两个非重叠Slot，使两份不同Candidate可以同时运行。UnityAuthority与DotRecastAuthority继续通过同一Slot合同启动，但本轮不承诺两个Authority Session并行；其Product adapter可以只接受明确的默认Slot。

Orchestrator在创建Run前验证全部端口、Slot owner和Candidate要求。Slot被占用、owner身份不明或端口被其它进程监听时直接失败，不杀未知进程、不选择另一个Slot、不改变端口。Slot lease绑定Orchestrator PID、进程启动身份和RunId；释放前必须证明仍属于同一owner。

## Run Manifest 与生命周期

每次Start生成唯一RunId，并在以下目录保存正式实例事实：

```text
Build/Network/RunLogs/<Product>/<RunId>/
  RunManifest.json
  RunStatus.json
  Config/
  Logs/
```

RunManifest至少绑定Candidate manifest path/hash、CandidateId、ProductId、RuntimeTopologyIdentity、Tool Bundle identities、SlotId、RunId、SessionId、resolved endpoints、role launch identities与配置文件hash。访问token只写本次Config，不进入Candidate、命令行或普通日志。

RunStatus只表达Preparing、Starting、Running、Stopping、Completed或Faulted及各role进程身份，不作为Gameplay、Candidate或Slot真相。Orchestrator拥有一个有界OS进程组；Stop只终止RunManifest声明且启动身份匹配的本次进程。某个角色启动失败时只回收本次进程并保留Faulted Run证据；不得调用StopExisting杀死同路径的其它Candidate或Session。

Orchestrator不因Editor域重载退出。Launcher只启动Orchestrator、读取小型状态和发出显式Stop请求，不在OnGUI/OnInspectorUpdate执行进程枚举、网络等待、大文件hash或日志解析。

## Rollback Candidate 与 Run 配置拆分

Rollback Candidate继续锁定Model、Protocol、Program、Layout、Semantic、Collision、KCC、roster、TickRate、prediction/confirmation policy和容量，但不锁定本次listen端口、Peer端口、GM端口、Relay查询端口、token、RunId或运行SessionId。

Orchestrator根据Candidate与Slot创建：

- Relay Run Manifest：Candidate静态身份引用、SessionId、listen endpoint、Peer endpoints和RunId。
- Relay Query Run Manifest：GM查询endpoint、token、容量与Candidate/Run/Session身份。
- GM Server Run Manifest：Tool identity、GM endpoint、Relay query目标、token、超时与容量。
- GM Console Run Manifest：Tool identity、GM endpoint、访问token和UI容量。
- Peer Run Config：Candidate、Run、Session、role和Gameplay endpoint；不包含GM配置或凭据。

Relay、GM和Player分别校验CandidateId、RunId、SessionId、Tool/Model/Protocol身份。GM请求继续携带requestId、service/relay instance、commandId/version，并新增CandidateId、RunId和GmToolIdentity。任何旧实例或跨Candidate请求明确拒绝。

现有`RollbackGmBuildProfile`迁移为只保存消息、队列、超时、历史和输出容量的静态Tool Policy；端口从该资产删除。`GmServerManifest.json`、`GmConsoleManifest.json`和`RelayQueryManifest.json`不再写入Candidate固定位置，统一由Run创建到自己的Config目录。

## Authority Product 边界

UnityAuthority与DotRecastAuthority同步迁移CandidateId、SourceCommit、Tool Bundle、candidate-owned启动adapter和版本目录，删除时间BuildId、固定当前Product替换和仓库脚本依赖。Server Product manifest使用CandidateId绑定Network Product。

本change不重写Fantasy Room、Authority Pipeline、Worker/InProcess Authority Scene、control/data transport或Gameplay endpoint语义。Authority adapter可以继续只接受一个正式默认Slot；若以后要多场Authority并行，必须单独把其源码部署配置中的endpoint拆为Run-owned配置，不能由公共Orchestrator修改Fantasy.config或猜测端口。

## Test Control Center

唯一`Tools/3C/Launcher`的Network Test区改为：

- CandidateLabel输入和三个Product的显式Build。
- 按Product列出全部已严格校验Candidate，显示SourceCommit、Program/Projection、Tool版本、BuiltAtUtc和文件状态。
- 显式选择Candidate与Slot并Start Session。
- 列出RunId、Candidate、Slot、状态和role进程，提供Open GM、Open Logs和Stop Owned Session。
- 显式Remove Candidate，并展示被Active Run阻止的原因。

界面不按目录时间自动选最新，不在选择、窗口恢复或刷新时Build/Run，不自动停止旧Session。旧固定三行Build/Run和StopExisting入口删除，不转发到新接口。

## 与现有 GM 的关系

`add-rollback-gm-console`是本change的前置，不被复制。以下内容原样保留：

- 独立GM进程的文本控制台。
- GM HTTP API、显式命令目录、权限和参数校验。
- Relay线程上的有界只读查询桥。
- help、session.info、actor.list、runtime.status。
- Player不安装GM组件、不持有工具凭据。
- GM退出不改变Gameplay Session，Relay退出保持Session失败语义。

本change只改变其上层归属与身份：GM从“固定Product唯一实例”变为“Candidate携带的版本化工具，在某个Run中服务唯一Session”。图形化GM业务面板、玩法修改和采样仍不进入本change。

## 与其它 active change 的关系

- `add-rollback-gm-console`已实现但未归档。实施本change前必须先由用户验收并归档它，或者重新读取其最终current spec后rebase本change；不得同时修改其核心命令、HTTP和Relay bridge文件。
- `add-gameplay-performance-capture-workflow`已经完成Launcher、MCP与独立Controller。它的产物固定在Library/Performance，不能注册为Network Candidate、Tool Bundle或Run。Launcher修改只允许在Network Test区域发生文件级协调。
- Foot、IK和Pose Graph active changes可以在各自Git worktree推进，但Candidate Build只接受完整干净commit。Test Orchestrator不创建worktree、不选择change优先级，也不通过Scripting Define排除其它任务文件。

## 迁移与删除

迁移完成后只保留schema v3和版本化候选链：

- 删除schema v2 Product reader/writer与按BuildId比较。
- 删除固定ProductRoot中的正式manifest消费和同产品backup替换。
- 删除仓库外部Run脚本作为运行依赖；正式adapter必须进入Candidate Tool Bundle。
- 删除`StopExisting`和按executable path粗杀进程。
- 删除Rollback构建期端口、token和运行Session配置。
- 删除Launcher旧固定Build/Run行和默认当前Product语义。
- 删除旧固定根产物，不迁移、不创建latest链接或兼容索引。

## 取舍

- 选择完整Candidate目录而不是DLL差分、hardlink或全局共享工具，保证每份Player和工具可独立证明，代价是磁盘占用增加；显式Remove Candidate承担清理。
- 选择干净commit而不是工作区hash，保证候选可追溯和可重建，代价是测试前必须提交checkpoint，不能直接Build未提交实验。
- 选择Candidate自带Tool Bundle而不是全局最新版GM，避免旧Candidate被新工具误解释，代价是重复少量.NET工具文件。
- 选择Run-owned配置而不是每个Candidate绑定一套端口，使同一Candidate可在不同Slot运行且不重建Player，代价是新增正式RunManifest与Session Orchestrator。
- 选择显式Slot而不是动态找空闲端口，保证窗口和连接可预测，代价是作者需要管理有限Slot且占用时必须先停止对应Session。
- 选择普通.NET Orchestrator而不是Editor直接管理进程，避免域重载和Inspector重活中断Session，代价是新增一个受版本控制的工具工程和进程合同。
- 选择公共Candidate/Tool合同覆盖三个Network Product，避免Rollback建立第二套Build目录；多场并行只先在Rollback闭合，Authority endpoint运行化留给其独立业务change。
