# dotrecast-authoritative-server-backend Specification

## Purpose
定义 Fantasy 进程内 DotRecast Authority Scene 如何复用 portable ServerAuthoritative Host、共享 DotRecast WorldSolver 与独立产品装配，形成三进程权威同步纵切。
## Requirements
### Requirement: FantasyDotRecast必须只集成既有共享Solver与Portable Host

FantasyDotRecast MUST作为ServerAuthoritativeHybrid的独立Authority Host Profile，消费`add-shared-dotrecast-navigation-solver`交付的共享Solver、Artifact与state-only binding，以及`refactor-server-authoritative-host-portability`和`refactor-float32-session-runtime-launcher-boundary`交付的Authority Pipeline、Source、Transport、Runtime Launcher和Host launch request。集成模块 MUST不复制或内联这些实现；Actor硬接触只能按本change对`dotrecast-navigation-world-solver`的正式delta扩展唯一共享Solver，MUST不在Authority Host中实现。

#### Scenario: 基础合同缺失

- **WHEN** Authority Scene无法通过正式共享Solver、Authority Runtime Launcher与portable Host launch request创建runtime
- **THEN** 集成 MUST失败
- **AND** MUST不建立第二Composer、第二Source或Unity依赖桥接

### Requirement: DotRecastAuthoritySceneManifest必须是Authority Scene唯一运行组合配置

Manifest MUST以canonical bytes锁定Fantasy process/Authority Scene、Room、Data endpoint、Program、Backend、Authority Pipeline、Source policy、roster、HostProfile、Solver、World、NavigationSurfaceArtifact、QueryProfile、每Actor接触形状、接触求解配置、Transport、clock与diagnostics identity，并形成ManifestHash。全部文件 MUST使用受约束的相对路径；Authority Scene MUST不读取Unity YAML、Collider、默认目录、环境猜测或旧Worker schema。Manifest MUST不包含Fantasy Control endpoint、外部WorkerId或Worker process role。

#### Scenario: Manifest的artifact路径逃逸

- **WHEN** manifest相对路径规范化后离开manifest根目录
- **THEN** loader MUST在注册Authority Host route前拒绝启动
- **AND** MUST不搜索其它artifact

### Requirement: DotRecast Authority必须运行在Fantasy Server的独立Authority Scene

DotRecast Authority MUST由Fantasy Server内独立MultiThread Authority Scene拥有。该Scene MUST加载正式`.csim`、portable Authority Pipeline catalog、portable Source runtime、共享DotRecast Solver和同一Network Checkpoint codec，从manifest传入expected Authority PipelineIdentity并构造正式Authority Runtime Launcher，再由Launcher通过唯一Host launch request与Float32 Composer创建runtime。Gate Scene MUST继续只拥有Client control connection、Room与路由，MUST不执行Program、WorldSolver或读取gameplay datagram。

#### Scenario: Authority Scene推进两个Actor

- **WHEN** locked Actor A/B的canonical input进入Authority Source
- **THEN** Authority Pipeline MUST按stable ActorId执行一次multi-actor World ResolveBatch
- **AND** 同一batch MUST先解析全部静态Surface candidate，再统一裁决Actor硬接触
- **AND** checkpoint与replication MUST只来自Finalize后的committed state

#### Scenario: 两个权威Actor发生body-block

- **WHEN** Actor A与Actor B的candidate轨迹在当前authority tick发生接触
- **THEN** Authority Scene MUST通过共享DotRecastWorldSolver的唯一ActorContactSolver生成两个FinalBody
- **AND** MUST不由Gate、Transport、Client position或Presentation执行第二次去穿透

### Requirement: Gate与Authority Scene必须使用正式Fantasy Inner控制路由

Gate Scene与DotRecast Authority Scene MUST通过正式Inner/Address协议交换Authority Scene register、roster、ticket、heartbeat、reliable event、full checkpoint、leave和failure。Authority Scene adapter MUST实现既有`IServerAuthoritativeAuthorityControlTransport`并只写portable Source边界queue。两个MultiThread Scene MUST不直接持有彼此Entity引用，也 MUST不建立Worker到Gate的Fantasy Console Client连接或自定义IPC。

#### Scenario: Gate锁定DotRecast Host

- **WHEN** Authority Scene以完整Host、Program、Pipeline、Solver、World和Data endpoint identity注册
- **THEN** Room MUST锁定唯一InProcess DotRecast Authority Host route
- **AND** 后续roster与ticket MUST按该Scene Address精确路由

#### Scenario: Authority建立初始Checkpoint基线

- **WHEN** Client尚无可确认baseline且Authority以`RequestSequence = 0`发布bootstrap full checkpoint
- **THEN** Gate MUST在该Player不存在pending checkpoint request时把checkpoint精确路由给owner Client
- **AND** Client恢复请求的响应 MUST使用非零sequence并与Gate保存的pending sequence精确匹配

### Requirement: DotRecast Authority必须使用固定且有界的Authority时钟

Authority Scene MUST在自身Scene线程使用单调时钟和manifest TickRate生成固定delta，每个tick只推进一次runtime handle。短时落后 MAY执行有界catch-up，但 MUST不合并delta、跳Tick或并行推进；超过MaxClockLagTicks MUST使当前Room fail-stop。

#### Scenario: Authority Scene短时卡顿

- **WHEN** pump落后多个tick但未超过lag上限
- **THEN** 每次pump MUST最多执行MaxCatchUpTicksPerPump
- **AND** 剩余tick MUST留待后续固定步推进

### Requirement: ServerAuthoritative握手必须锁定实际Host、Solver与World Identity

External Unity Worker register、InProcess DotRecast Scene register和Client join MUST共同锁定AuthorityHostProfileId、HostId、SolverId/version/capabilities/features、WorldId、MapId、WorldRevision、NavigationSurfaceArtifactHash、DotRecastQueryProfileHash与WorldConfigurationHash；DotRecast WorldConfigurationHash MUST覆盖每Actor接触形状与接触求解配置。同时保持Program、Layout、operation-set、TickRate、Backend和Prediction/Authority Pipeline pair校验。Room MUST只校验和路由identity，MUST不替Client选择Solver。Client处理Join response并安装SessionId与Authority Host identity后 MUST发送正式`ClientJoinAccepted`；Gate MUST等locked roster内全部Client完成精确一次确认后才发布locked roster与ticket，不得依赖RPC response与push handler的到达或执行顺序。

#### Scenario: Client QueryProfile过期

- **WHEN** Client Program与Artifact匹配但QueryProfileHash不同
- **THEN** join MUST失败并返回明确world identity错误
- **AND** Client MUST不进入Prediction Session或切换CC

#### Scenario: Client Actor接触配置过期

- **WHEN** Client与Authority的Program、NavigationSurfaceArtifact和QueryProfile匹配但Actor接触形状或接触求解配置不同
- **THEN** WorldConfigurationHash MUST不同且join MUST失败
- **AND** Client MUST不进入Prediction Session或使用本地Collider配置继续运行

#### Scenario: 第二个Client触发固定Roster

- **WHEN** 第二个Client加入后Room已经可以形成locked roster
- **THEN** 两个Client MUST先分别处理Join response并安装返回的SessionId与Authority Host identity
- **AND** 每个Client MUST发送匹配Room、Session、Player和Host的`ClientJoinAccepted`
- **AND** Gate MUST在两个确认都完成后才向Client与Authority Host发布locked roster和ticket
- **AND** 身份不匹配或重复确认 MUST被拒绝，系统 MUST不使用延时、重试猜测、提前push缓存或旧协议fallback绕过确认屏障

### Requirement: Client Command不得携带Pose Authority

Client command MUST只携带canonical input、input sequence、target authority tick与route identity。Position、Transform、Body、applied displacement和DotRecast查询结果 MUST不进入Authority运动输入；Authority Scene MUST从自己的committed Character/World state执行Program与Solver。

#### Scenario: Client预测领先

- **WHEN** Client已模拟未确认的预测Tick
- **THEN** command MUST只提交对应输入与时序身份
- **AND** Authority Scene MUST独立计算权威位置

### Requirement: Control与Routine Gameplay数据必须继续使用既有双平面

Fantasy控制路由 MUST只承载register、roster、ticket、heartbeat、reliable event、full checkpoint、leave与failure。Routine command/snapshot MUST复用Authority Scene拥有的既有portable direct UDP endpoint和codec；系统 MUST不增加DotRecast专属packet、Inner/Outer gameplay relay、KCP gameplay fallback或双写数据面。

#### Scenario: Authority发布Routine Snapshot

- **WHEN** Authority Egress产生routine snapshot
- **THEN** snapshot MUST通过既有UDP endpoint发送
- **AND** Inner control adapter与Gate MUST不重复转发该snapshot

### Requirement: DotRecast Client必须使用隔离Scene且完全不使用CharacterController

Network Test Bootstrap MUST通过独立TestScenarioId进入DotRecast Client Scene。Client A/B MUST以不同PlayerId/ActorId launch profile复用该Scene；owner MUST使用DotRecast Prediction Composition、共享DotRecast Solver和state-only binding。Scene MUST不包含CharacterController组件、CC binding、CC Solver或`CharacterController.Move`路径；remote actor MUST只消费authority replication。

#### Scenario: 启动Client A与Client B

- **WHEN** 两个进程以不同launch profile进入同一DotRecast Scene
- **THEN** 两端owner MUST使用相同Solver/Artifact/Profile identity预测
- **AND** 每端 MUST显示另一个Actor的权威表现

### Requirement: 网络模型测试环境必须拥有独立生命周期

Unity Authority、DotRecast Authority与未来Rollback MUST使用不同Server/Player配置、Scene和Composition。DotRecast Authority Scene停止或Client Scene离开时 MUST释放runtime handle、Source、Actor registration、Endpoint、History、Solver/World与Presentation；这些owner MUST不跨环境存活，Active Session MUST不热切换Network Model、HostProfile、Host route或Solver。

#### Scenario: 从DotRecast返回Bootstrap

- **WHEN** DotRecast Client Scene卸载或对应Authority Scene停止
- **THEN** 旧Session全部owner MUST在新环境创建前释放
- **AND** 后续Unity Authority环境 MUST创建全新外部Worker与CC组合

### Requirement: Build与Run必须分离且产物必须按模型隔离

Unity Authority与DotRecast Authority MUST分别拥有模型专属`Build`与`Run` Editor入口、Player、Fantasy Server、build manifest和日志目录。Build MUST锁定该模型的Player target/options与Server configuration，只替换同模型的当前Player、Server、manifest与Authority artifacts，并保留日志；Build MUST不启动进程。Run MUST只校验和消费该模型当前正式manifest与产物，MUST不触发编译。manifest MUST记录`BuildId=yyyyMMdd-HHmmss`作为当前产物身份，但BuildId MUST不参与目录寻址。Unity Authority与DotRecast Authority的固定目录 MUST不重叠且不得互相覆盖。Unity Authority发布的`Fantasy.config` MUST只包含Gate Scene；DotRecast Authority发布的`Fantasy.config` MUST只包含Gate Scene与DotRecast Authority Scene。DotRecast Authority Scene manifest、Program和Navigation artifact MUST随DotRecast Server以正式相对路径发布。Unity脚本 MUST启动Fantasy Server、Unity Authority Worker、Client A和Client B；DotRecast脚本 MUST只启动Fantasy Server、Client A和Client B，并先构造完整PowerShell参数数组再调用`Start-Process`。每次Run MUST按模型与RunId建立日志目录。未形成完整可运行闭环的模型 MUST不注册占位测试入口。

#### Scenario: 分别Build和Run Unity Authority

- **WHEN** 作者点击`Tools/3C/Network Tests/Unity Authority/Build`
- **THEN** 系统 MUST按`StandaloneWindows64 + IL2CPP + Development + StrictMode`替换Unity Authority当前Player，并以`Debug`替换该模型Fantasy Server且写入实际编译选项
- **AND** MUST不启动任何测试进程
- **WHEN** 作者随后点击`Tools/3C/Network Tests/Unity Authority/Run`
- **THEN** 系统 MUST只从Unity Authority固定目录启动Fantasy Server、Unity Authority Worker、Client A与Client B
- **AND** MUST不重新构建Player或Server

#### Scenario: 连续发布两次DotRecast环境

- **WHEN** 仓库中已存在前一次DotRecast Authority当前产物
- **THEN** 新Build MUST替换DotRecast Authority自己的Player、Server、manifest与Authority artifacts，并保留既有日志
- **AND** MUST不修改Unity Authority的Player、Server、manifest或日志

### Requirement: DotRecast环境失败必须使固定Roster Room整体停止

Manifest、artifact、identity、spawn、Authority Scene lifecycle、Solver query、checkpoint、reliable queue、Inner control route或UDP Transport发生不可恢复错误时，当前固定双Actor Room MUST fail-stop并释放runtime handle、Source、Solver与Transport。系统 MUST不保留单Actor继续模拟、不选举Client、不启动外部DotRecast Worker、不切换Unity Worker或回退CC。

#### Scenario: Actor A的Authority Query失败

- **WHEN** Actor A在同一WorldSolveBatch返回非法query结果
- **THEN** 整个batch MUST不发布NextWorldState
- **AND** 当前Room MUST整体fail-stop
