## 1. 锁定依赖与修订后的集成边界

- [x] 1.1 确认`refactor-server-authoritative-hybrid-runtime`已归档并进入current specs。
- [x] 1.2 确认`add-shared-dotrecast-navigation-solver`全部任务完成且strict validation通过。
- [x] 1.3 确认`refactor-server-authoritative-host-portability`全部任务完成且strict validation通过。
- [x] 1.4 记录共享Solver、Artifact、Binding正式类型与程序集所有权。
- [x] 1.5 记录portable Authority Pipeline、Source、Transport与Launch Request正式类型和程序集所有权。
- [x] 1.6 盘点现有Fantasy Outer/Inner协议源、ProtocolExportTool和generated输出目录。
- [x] 1.7 盘点现有direct UDP command/snapshot endpoint与Network Checkpoint唯一实现。
- [x] 1.8 盘点Bootstrap、Unity Authority Scene、launch profile、build profile和启动脚本。
- [x] 1.9 建立本change不得实现的Solver、Artifact、Binding、Source、Pipeline和Composer删除清单。
- [x] 1.10 确认DotRecast环境客户端与服务端均不使用CharacterController。
- [x] 1.11 从ServerAuthoritative Network Model删除具体Program Runtime、Execution Backend与WorldSolver所有权。
- [x] 1.12 从Network Model资产和Inspector删除具体Runtime、Backend与Solver字段。
- [x] 1.13 让Session Composition把实际Runtime、Backend与Solver descriptor传入Source preparation。
- [x] 1.14 让Pipeline兼容性与握手身份从实际Composition组件编译，不再读取Model内具体实现。
- [x] 1.15 保持当前Prediction与Authority必须使用同一Solver identity的正式约束。
- [x] 1.16 对照`Ref/94.移动同步前后端完整代码`确认Gate与权威Map Scene的所有权分离。
- [x] 1.17 确认参考工程的Fantasy Scene不是独立OS进程。
- [x] 1.18 确认DotRecast Authority改为Fantasy Server内独立Authority Scene。
- [x] 1.19 确认Unity Authority继续保留外部Unity Worker与四进程环境。
- [x] 1.20 确认不迁入参考工程的客户端position权威、100ms移动段和Transform真值。

## 2. 将Worker Manifest迁为Authority Scene Manifest

- [x] 2.1 将`DotNetAuthorityWorkerManifest`正式重命名为`DotRecastAuthoritySceneManifest`。
- [x] 2.2 将manifest magic与schema名称迁为Authority Scene语义。
- [x] 2.3 保留canonical bytes与ManifestHash唯一实现。
- [x] 2.4 删除Fantasy Control endpoint字段。
- [x] 2.5 删除外部WorkerId与Worker process role字段。
- [x] 2.6 增加AuthorityHostProfileId与稳定HostId。
- [x] 2.7 增加Fantasy process identity与Authority Scene identity。
- [x] 2.8 保留RoomId与direct UDP Data endpoint。
- [x] 2.9 保留Program相对路径、ProgramId、ProgramHash和LayoutHash。
- [x] 2.10 保留NumericProfile、Target ABI、operation-set和TickRate。
- [x] 2.11 保留Authority Pipeline descriptor、PipelineHash和Backend identity。
- [x] 2.12 保留Authority Source policy、clock、catch-up和queue bounds。
- [x] 2.13 保留按ActorId稳定排序的locked roster。
- [x] 2.14 保留每Actor initial Character state、body和output route。
- [x] 2.15 保留World、Map、Solver、NavigationSurfaceArtifact与QueryProfile identity。
- [x] 2.16 保留Checkpoint、Committer、Transport和diagnostics identity。
- [x] 2.17 让loader在注册Authority Host route前校验全部artifact实际bytes与identity。
- [x] 2.18 在`ThirdPersonClient.Editor`中将Unity Editor exporter迁为Authority Scene manifest exporter，并只引用portable manifest合同。
- [x] 2.19 让exporter输出Fantasy Server正式Authority相对目录。
- [x] 2.20 删除旧Worker schema reader、旧类型名、旧文件名和兼容转换。

## 3. 建立Fantasy DotRecast Authority Scene Host

- [x] 3.1 在Fantasy server config增加唯一DotRecast Authority Scene类型与Scene配置。
- [x] 3.2 让DotRecast Authority Scene与Gate Scene在Demo中归属同一processConfigId。
- [x] 3.3 保持Authority Scene使用独立MultiThread Scene lifecycle。
- [x] 3.4 建立Authority Host Entity保存manifest、runtime handle与唯一资源owner。
- [x] 3.5 建立Authority Host Awake/Create流程。
- [x] 3.6 建立Authority Host Destroy流程。
- [x] 3.7 为Server工程增加portable Core、Float32、ServerAuthoritative、Transport、DotRecast和Authority manifest程序集引用。
- [x] 3.8 禁止Server Authority Host引用Unity工程、UnityEngine、CharacterController或Unity场景对象。
- [x] 3.9 让Authority Scene只从显式server发布目录读取manifest。
- [x] 3.10 按顺序加载manifest、`.csim`、Pipeline descriptor和NavigationSurfaceArtifact。
- [x] 3.11 使用正式loader校验Program、Layout、ABI和operation-set。
- [x] 3.12 使用portable catalog取得neutral Runtime Package，并锁定该package只能由准备完成的Authority Runtime Launcher消费；禁止拆分或重新拼装descriptor与三类factory catalog。
- [x] 3.13 按manifest roster创建ProgramCatalog与initial Character state。
- [x] 3.14 使用共享DotRecast Solver创建initial World state。
- [x] 3.15 创建portable Authority Source runtime与typed ports。
- [x] 3.16 创建既有portable gameplay datagram endpoint。
- [x] 3.17 构造绑定Source policy与locked roster的Authority Runtime Launcher，由Launcher通过Host launch request调用唯一Float32 Composer。
- [x] 3.18 建立唯一runtime handle owner。
- [x] 3.19 在Authority Scene线程建立单调authority clock。
- [x] 3.20 使用manifest TickRate生成固定delta。
- [x] 3.21 实现有界MaxCatchUpTicksPerPump。
- [x] 3.22 超过MaxClockLagTicks时传播Room failure并停止。
- [x] 3.23 保证每个authority tick只推进一次runtime handle。
- [x] 3.24 保证checkpoint与replication只来自Finalize后的committed state。
- [x] 3.25 删除第二Session Host、第二Composer、第二Evaluator和重复clock/queue。

## 4. 建立Gate与Authority Scene的正式控制路由

- [x] 4.1 将`ServerAuthoritativeRoom`从外部`AuthoritySession`所有权迁为唯一`AuthorityHostRoute`。
- [x] 4.2 定义稳定HostProfile、HostId、route kind和lifecycle state。
- [x] 4.3 定义外部Unity Worker Session route。
- [x] 4.4 定义InProcess DotRecast Authority Scene Address route。
- [x] 4.5 禁止Room同时保存两种活动Authority route。
- [x] 4.6 禁止Active Room热切换route kind或HostProfile。
- [x] 4.7 让现有Unity Worker register降低为统一Authority Host identity。
- [x] 4.8 让Authority Scene通过Fantasy Inner/Address request向Gate注册。
- [x] 4.9 让Gate注册响应返回唯一SessionId与RoomRevision。
- [x] 4.10 让Gate向Authority Scene发送locked roster。
- [x] 4.11 让Gate向Authority Scene发送data-plane ticket。
- [x] 4.12 让Authority Scene回报ticket consumed。
- [x] 4.13 建立Authority Scene heartbeat与latest authority tick路由。
- [x] 4.14 将可靠Event batch从Authority Scene路由到精确Client Session。
- [x] 4.15 将Client Full Checkpoint request路由到Authority Scene。
- [x] 4.16 将Authority Full Checkpoint response路由到精确Client Session。
- [x] 4.17 建立Authority Scene leave、destroy和Session failure传播。
- [x] 4.18 让Room在Authority Scene失活时按固定roster策略fail-stop。
- [x] 4.19 建立Server侧adapter实现`IServerAuthoritativeAuthorityControlTransport`。
- [x] 4.20 保证Fantasy Handler只验证外壳并写入adapter queue。
- [x] 4.21 禁止Gate Scene执行Program、Solver、checkpoint policy或读取gameplay datagram。
- [x] 4.22 禁止Authority Scene保存Client control Session或执行Presentation。

## 5. 扩展正式Inner/Outer协议与Room Identity

- [x] 5.1 在正式portable identity中增加AuthorityHostProfileId与HostId。
- [x] 5.2 增加SolverId、SolverVersion、capabilities和features。
- [x] 5.3 增加WorldId、MapId、WorldRevision和WorldConfigurationHash。
- [x] 5.4 增加NavigationSurfaceArtifactHash与DotRecastQueryProfileHash。
- [x] 5.5 将新身份字段加入现有Unity Worker Outer register。
- [x] 5.6 在正式Inner协议增加Authority Scene register request/response。
- [x] 5.7 在正式Inner协议增加roster、ticket、heartbeat、reliable、checkpoint、leave和failure消息。
- [x] 5.8 将Client实际Prediction Solver/World identity加入join request。
- [x] 5.9 将Room锁定的Authority identity加入join response。
- [x] 5.10 保持现有Program、Pipeline pair、Backend和TickRate身份字段。
- [x] 5.11 让Room校验HostProfile允许的route kind与Solver/World identity形状。
- [x] 5.12 让Room继续只锁定一个Authority Host。
- [x] 5.13 让Client用本地实际加载的Solver/Artifact/Profile identity校验join结果。
- [x] 5.14 保持Client command只携带canonical input、sequence、target tick与route identity。
- [x] 5.15 禁止Client command携带position、Transform、Body或applied displacement。
- [x] 5.16 保持routine command/snapshot走既有direct UDP数据面。
- [x] 5.17 使用正式ProtocolExportTool重新生成server/client代码。
- [x] 5.18 核对generated Inner/Outer opcode与消息身份唯一。
- [x] 5.19 禁止手写或修补generated `.g.cs`。
- [x] 5.20 删除DotRecast外部Worker DTO、DotRecast专属packet和KCP gameplay fallback。
- [x] 5.21 在正式Outer协议增加携带Room、Session、Player和Host身份的`ClientJoinAccepted`消息。
- [x] 5.22 让Client只在处理Join response并安装SessionId、Authority Host与初始roster后发送加入确认。
- [x] 5.23 让Gate精确校验并exactly-once记录每个locked roster Client的加入确认。
- [x] 5.24 让Gate只在全部Client确认后向Client与Authority Host发布locked roster与ticket。
- [x] 5.25 将ModelProtocolVersion递增并使用正式ProtocolExportTool重新生成server/client代码。
- [x] 5.26 删除对Join RPC response与push handler执行顺序的假设，禁止延时、缓存或旧协议fallback。

## 6. 建立DotRecast Client Composition与资产

- [x] 6.1 使用`ThirdPersonSimulation.DotRecast.Unity`中的正式Definition建立DotRecast Prediction Session Composition资产。
- [x] 6.2 显式引用现有Prediction Source Definition。
- [x] 6.3 显式引用现有Prediction Pipeline Definition。
- [x] 6.4 显式引用现有Fantasy Endpoint Definition。
- [x] 6.5 显式引用Corin Program与Projection。
- [x] 6.6 显式引用DotRecastWorldSolverDefinition。
- [x] 6.7 显式引用Corin NavigationSurfaceAsset与QueryProfile。
- [x] 6.8 为owner配置DotRecastStateWorldBodyBinding。
- [x] 6.9 保持remote actor只注册Presentation output。
- [x] 6.10 删除Composition中的CC Solver与CC binding引用。
- [x] 6.11 校验Composition identity锁定Solver/World/Artifact/Profile。
- [x] 6.12 保持既有baseline merge、restore、replay与hard recovery配置。
- [x] 6.13 保持既有remote body、producer和fact presentation配置。
- [x] 6.14 禁止DotRecast专属History、Correction、Checkpoint或Presentation配置。

## 7. 建立隔离DotRecast双客户端Scene

- [x] 7.1 在Network Test Bootstrap增加唯一DotRecast TestScenarioId。
- [x] 7.2 新建独立DotRecast Client Scene。
- [x] 7.3 Scene显式引用DotRecast Prediction Composition。
- [x] 7.4 Scene显式引用owner/remote Actor、spawn、camera和diagnostics。
- [x] 7.5 Scene不得配置CharacterController组件、CC binding或CC Solver。
- [x] 7.6 建立Client A launch profile并锁定PlayerId/ActorId。
- [x] 7.7 建立Client B launch profile并锁定不同PlayerId/ActorId。
- [x] 7.8 让A/B复用同一Scene和相同Solver/Artifact/Profile identity。
- [x] 7.9 让owner只由Prediction Session与DotRecast Solver推进。
- [x] 7.10 让remote actor只消费Authority replication进入Presentation。
- [x] 7.11 保持移动、转身、闪避、Run和motion curve走同一Corin Program。
- [x] 7.12 保持Attack1/Attack2、连段、打断、Timeline Window、GE、Attribute和Cue走同一Corin Program。
- [x] 7.13 保留现有Unity CharacterController Authority Scene与Composition不变。
- [x] 7.14 Scene unload前停止Source输入并释放SessionHost。
- [x] 7.15 释放Actor registration、Endpoint、History、Solver/World与Presentation。
- [x] 7.16 禁止相关owner通过`DontDestroyOnLoad`跨Scene存活。
- [x] 7.17 禁止Active Session热切换Network Model、HostProfile或Solver。

## 8. 建立隔离Server/Player Build与启动脚本

- [x] 8.1 定义`BuildId=yyyyMMdd-HHmmss`为当前产物manifest身份，不用于目录寻址。
- [x] 8.2 建立Unity Authority独立`Build`与`Run` Editor入口，并锁定`StandaloneWindows64 + IL2CPP + Development + StrictMode`编译选项。
- [x] 8.3 将Unity Authority Player输出到`Build/Network/UnityAuthority/Player/`。
- [x] 8.4 建立Unity Authority Fantasy Server Debug publish入口，带规定build-server与shared compilation参数并立即shutdown。
- [x] 8.5 将Unity Authority Server输出到`3cDemo/Server/Build/Network/UnityAuthority/Server/`。
- [x] 8.6 建立DotRecast Client Player build profile。
- [x] 8.7 将DotRecast Client输出到`Build/Network/DotRecastAuthority/Player/`。
- [x] 8.8 建立DotRecast Fantasy Server publish profile。
- [x] 8.9 将DotRecast Server输出到`3cDemo/Server/Build/Network/DotRecastAuthority/Server/`。
- [x] 8.10 为DotRecast Server发布只包含Gate与Authority Scene的正式`Fantasy.config`。
- [x] 8.11 将Authority Scene manifest、Program和Navigation artifact发布到Server正式相对路径。
- [x] 8.12 禁止Unity Authority与DotRecast Authority build互相覆盖可执行文件或配置。
- [x] 8.13 按模型与RunId建立隔离日志目录，并在同模型Build时保留既有日志。
- [x] 8.14 建立只消费Unity Authority当前正式build manifest与固定模型目录的四进程启动脚本。
- [x] 8.15 让Unity `Run`入口启动Fantasy Server、Unity Authority Worker、Client A与Client B，且不触发Build。
- [x] 8.16 建立DotRecast Authority三进程启动脚本。
- [x] 8.17 让DotRecast脚本只启动Fantasy Server、Client A与Client B。
- [x] 8.18 让脚本传递明确TestScenarioId、role、PlayerId、ActorId和server build路径。
- [x] 8.19 先构造完整PowerShell参数数组再传入`Start-Process -ArgumentList`。
- [x] 8.20 让同模型Build替换自己的当前产物，并禁止Unity Authority与DotRecast Authority互相覆盖。

## 9. Diagnostics、清理与项目文档

- [x] 9.1 暴露HostProfile、Host route kind、Program、Backend、Pipeline和Source identity。
- [x] 9.2 暴露Solver、World、Map、NavigationSurfaceArtifact和QueryProfile identity。
- [x] 9.3 暴露Authority Scene address、authority tick、input ack、snapshot sequence和transport状态。
- [x] 9.4 复用Prediction diagnostics显示position/yaw error、restore tick和replayed ticks。
- [x] 9.5 让Room failure记录精确Actor、Tick、channel、HostProfile和reason。
- [x] 9.6 删除旧CC DotRecast Prediction资产与引用。
- [x] 9.7 删除重复Authority Source、Pipeline、Composer与packet mapper。
- [x] 9.8 删除`DotNetAuthorityWorkerManifest`旧类型、文件名和schema字段。
- [x] 9.9 删除外部DotRecast Worker executable、project和publish profile。
- [x] 9.10 删除Fantasy Console Client adapter与Worker到Gate连接概念。
- [x] 9.11 删除DotRecast专属baseline、correction、checkpoint和packet分支。
- [x] 9.12 删除旧协议reader、fallback Host和Transform gameplay authority。
- [x] 9.13 更新`openspec/project.md`为Gate Scene与Authority Scene分离口径。
- [x] 9.14 更新`openspec/project.md`记录Unity四进程与DotRecast三进程隔离环境。
- [x] 9.15 更新implementation inventory并确认无`DotRecastAuthorityWorker`残留。

## 10. 构建与严格校验

- [x] 10.1 编译portable Core、Float32、ServerAuthoritative与DotRecast工程并带规定参数。
- [x] 10.2 编译NavigationBuildTool与Authority Scene manifest工程并带规定参数。
- [x] 10.3 编译Fantasy Entity、Hotfix与Main相关工程并带规定参数。
- [x] 10.4 编译`ThirdPersonSimulation.Unity`、`ThirdPersonSimulation.DotRecast.Unity`、`ThirdPersonSimulation.ServerAuthoritative.Unity`、`ThirdPersonClient.Runtime`与`ThirdPersonClient.Editor`并带规定参数。
- [x] 10.5 每轮编译后立即执行`dotnet build-server shutdown`。
- [x] 10.6 执行正式Unity Authority Build入口并写入该模型固定当前产物目录。
- [x] 10.7 执行正式DotRecast Authority Build入口并写入该模型固定当前产物目录。
- [x] 10.8 发布两个隔离Fantasy Server目录及DotRecast Authority artifacts。
- [x] 10.9 运行`openspec validate add-dotrecast-authoritative-server-backend --strict --no-interactive`。
- [x] 10.10 运行已归档依赖对应current specs与本change的strict validation。
- [x] 10.11 运行`openspec validate --all --strict --no-interactive`并解决全部冲突。
- [x] 10.12 核对最终任务勾选与真实实现一致。

## 11. 静态地图与Navigation Surface同源闭环

- [x] 11.1 建立包含正式地面、墙与Transform的canonical测试地图Prefab。
- [x] 11.2 让DotRecast客户端、Unity Authority客户端与Unity Authority Worker Scene只实例化canonical测试地图Prefab。
- [x] 11.3 将Navigation authoring输入迁移为canonical测试地图Prefab并删除纯平面导航源Scene。
- [x] 11.4 从canonical Prefab重新发布NavigationSurfaceAsset并确认跨墙MoveAlongSurface被边界截断。
- [x] 11.5 重建DotRecast Authority隔离Player、Server manifest与Navigation artifact。
- [x] 11.6 运行本change strict validation并核对任务状态。

## 12. Actor硬接触批量求解闭环

- [x] 12.1 记录当前`DotRecastWorldSolver.ResolveBatch`逐Actor独立最终求解造成角色互穿的准确调用链。
- [x] 12.2 确认Actor硬接触继续归属Session唯一WorldSolver，不归属Network Model、Authority Scene、Presentation或场景Collider。
- [x] 12.3 定义portable canonical `ActorContactShape`，显式保存Radius、Height与SkinWidth。
- [x] 12.4 定义固定迭代次数、接触容差与最大去穿透距离的canonical接触求解配置。
- [x] 12.5 将接触形状加入DotRecast body binding descriptor，并禁止从Navigation build agent尺寸隐式推导。
- [x] 12.6 将接触形状与求解配置加入Solver/World configuration identity。
- [x] 12.7 扩展Authority Scene manifest roster binding与canonical codec，保存每Actor接触形状。
- [x] 12.8 让manifest loader在注册Host route前校验接触形状、求解配置与WorldConfigurationHash。
- [x] 12.9 扩展Unity `DotRecastStateWorldBodyBinding`与Inspector，要求作者显式配置接触形状。
- [x] 12.10 更新DotRecast Prediction Composition中的Actor binding资产，不保留默认值或Collider读取路径。
- [x] 12.11 更新DotRecast Authority Scene manifest与发布artifact，使Prediction和Authority锁定相同接触配置。
- [x] 12.12 将现有`SolveActor`拆为只产生静态Surface candidate的内部阶段，不在该阶段创建最终WorldSolveResult。
- [x] 12.13 让`ResolveBatch`先完成全部Actor candidate，再进入唯一Actor接触阶段。
- [x] 12.14 建立不引用Unity、Fantasy、Graph、Action或Presentation的portable `ActorContactSolver`。
- [x] 12.15 按ActorId稳定排序roster，并按`(minActorId,maxActorId)`稳定枚举pair。
- [x] 12.16 使用垂直区间重叠过滤不在同一高度层的Actor pair。
- [x] 12.17 对BeforeBody到surface candidate的相对轨迹实现圆盘连续扫掠与最早TOI计算。
- [x] 12.18 在接触后只裁剪双方剩余位移的闭合法向分量，并保留合法切向滑动。
- [x] 12.19 保证静止Actor不会仅因另一个Actor主动移入而被通用接触层转换成推行位移。
- [x] 12.20 使用独立修正缓冲和固定迭代次数处理多Actor约束，避免pair遍历即时写回造成顺序偏置。
- [x] 12.21 对初始轻微重叠执行有界去穿透，并在超过最大距离时拒绝整个batch。
- [x] 12.22 在每轮Actor修正后复用同一DotRecast查询重新约束目标位置，禁止接触修正穿墙或离开surface。
- [x] 12.23 在固定迭代结束后校验全部有效pair的最小间距，超容差时使整个batch失败。
- [x] 12.24 只从最终接触解生成AppliedDisplacement、Velocity、Sides、FinalBody与CharacterWorldSolveResult。
- [x] 12.25 保持World state只保存最终Body，不加入pair cache、TOI cache或跨Tick接触集合。
- [x] 12.26 保持Client command、routine snapshot与checkpoint codec不新增客户端接触结果或Pose Authority字段。
- [x] 12.27 增加pair、TOI、法向裁剪、去穿透、surface重新约束和失败原因的结构化Solver Trace。
- [x] 12.28 删除逐Actor直接提交最终Body的旧循环与任何客户端专属去穿透路径。
- [x] 12.29 核对Unity Prediction与Fantasy Authority继续编译同一`DotRecastWorldSolver`和`ActorContactSolver`源码。
- [x] 12.30 更新implementation inventory与`openspec/project.md`，记录静态Surface加Actor硬接触的唯一链路和明确Non-Goals。
- [x] 12.31 编译portable DotRecast、DotRecast Authority manifest、Fantasy Server与Unity相关程序集，并带规定build-server参数。
- [x] 12.32 编译结束后立即执行`dotnet build-server shutdown`。
- [x] 12.33 运行`openspec validate add-dotrecast-authoritative-server-backend --strict --no-interactive`并解决全部冲突。
- [x] 12.34 运行`openspec validate --all --strict --no-interactive`并解决与current specs及其它active change的冲突。
- [x] 12.35 核对12.x任务勾选与真实实现一致，不把既有11.x完成状态当作Actor接触已经交付。
