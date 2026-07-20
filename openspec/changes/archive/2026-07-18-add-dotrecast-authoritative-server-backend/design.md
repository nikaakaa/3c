## Context

两个基础change分别解决“共享世界求解实现”和“Authority Host运行合同可移植”。本change原本只把已有组件组成可部署的DotRecast环境；实际运行确认现有共享Solver只逐Actor裁决静态Surface，尚未闭合固定roster内的Actor硬接触。因此本change继续保持Source、Pipeline、Composer和checkpoint不变，但扩展唯一`DotRecastWorldSolver.ResolveBatch`，使静态Surface与Actor接触共同产生一次FinalBody。

原设计把DotRecast Authority放进独立普通.NET Worker，并让Worker通过Fantasy Console Client连接Gate。当前安装的`Fantasy-Net 2026.0.1020`只提供服务端平台，没有普通.NET Console Client的公开`Scene.Create/Connect`入口。更重要的是，纯C# DotRecast Authority没有必须跨进程的业务理由。

`Ref/94.移动同步前后端完整代码`展示了更合适的部署所有权：Gate负责连接和路由，Map Scene在Fantasy Server内部拥有权威移动。本文采用相同的Scene级职责分离，但不复制该参考工程由客户端提交position、服务端按100ms移动段推进的同步语义。

最终存在两个互不混合的ServerAuthoritative测试组合：

```text
Unity Authority
  Fantasy Gate/Room process
  External Unity Authority Worker process
  Client A process
  Client B process

DotRecast Authority
  Fantasy Server process
    Gate Scene
    DotRecast Authority Scene
  Client A process
  Client B process
```

Unity环境是四个OS进程；DotRecast环境是三个OS进程。Fantasy Scene不是额外OS进程。

## Composition Ownership

`ServerAuthoritativeHybridModelDefinition`只定义网络协议、Prediction/Authority Pipeline Pair、同步策略以及Numeric/ABI/Backend/Solver能力要求，不保存具体Program Runtime、Execution Backend或WorldSolver引用。

Unity Client和Unity Authority Worker由`SimulationSessionCompositionDefinition`显式选择Program Runtime、Backend、当前Pipeline和Solver。Fantasy DotRecast Authority Scene由唯一`DotRecastAuthoritySceneManifest`提供等价选择，并由portable Authority Source交付的`ServerAuthoritativeAuthoritySessionRuntimeLauncher`通过`ServerAuthoritativeAuthorityHostLaunchRequest`进入同一个portable Float32 Composer。

Unity Authority和DotRecast Authority共享同一Model identity、Program与Pipeline语义。当前每个测试环境要求Prediction与Authority使用相同Solver identity，不支持Unity Prediction与DotRecast Authority混搭。

## Scene Ownership

Fantasy Server内的职责固定为：

```text
Gate Scene
  Client control connection
  Room lifecycle
  fixed roster
  authority host identity lock
  data-plane ticket issuance
  reliable event / full checkpoint routing
  session failure propagation

DotRecast Authority Scene
  manifest and artifact loading
  portable Authority Source
  direct UDP gameplay endpoint
  Authority Pipeline runtime
  Corin Program catalog
  DotRecast Solver and World state
  fixed authority clock
  committed checkpoint / replication production
```

Gate Scene MUST不执行Program、WorldSolver或读取gameplay datagram。Authority Scene MUST不拥有Client control Session、Room规则或Client Presentation。两个MultiThread Scene之间 MUST通过Fantasy正式Inner/Address消息传递控制产品，不直接跨线程保存彼此Entity引用。

Fantasy Address是有符号`long` RuntimeId，`0`才表示无效；由对象池创建的Authority Host Entity可以拥有合法负地址。Gate注册、Room route与Inner消息校验 MUST按非零判断，不得把正数范围误当成有效地址范围。

Source Port descriptor MUST由Core根据Session Source identity与Pipeline source-port requirement统一生成。Unity authoring、preflight、Unity runtime与普通.NET Authority Scene runtime MUST不分别维护ConfigurationHash公式；manifest校验消费这一份canonical descriptor。

第二个Client完成固定roster时，Gate可以在Room内准备locked roster与ticket，但 MUST不依赖“先发送Join RPC response，客户端就一定先执行response continuation”的网络回调顺序。每个Client处理Join response、安装SessionId与Authority Host identity并接收初始roster后，MUST发送包含RoomId、SessionId、PlayerId与HostId的正式`ClientJoinAccepted`控制消息。Gate必须对该确认执行精确身份校验和exactly-once记录，只有locked roster内全部Client均已确认后，才可以向两个Client和Authority Host发布locked roster与ticket。延时、重试猜测、提前缓存依赖身份的push或兼容旧协议均不得替代该确认屏障。

Authority Source在首个Authority tick前 MUST泵data-plane以收集启动输入。Command接收统计与liveness基线 MUST使用command内最新canonical input的source tick，不得读取尚未启动的Authority outer clock。非法零source tick必须由command schema拒绝。

Authority Host主动上报leave或failure后，Gate只向Client传播Room fail-stop，不得把同一failure回发给已经失败或正在销毁的原Authority Host。

## Authority Host Route

Room不再以`AuthoritySession != null`等同于Authority存在，而是锁定一个正式Authority Host route：

```text
AuthorityHostRoute
  HostProfileId
  HostId
  route kind
  Program / Pipeline / Backend identity
  Solver / World / Artifact / QueryProfile identity
  Data endpoint
  lifecycle state
```

只允许两种显式route kind：

```text
ExternalUnityWorker
  control route = existing Outer worker Session

InProcessDotRecastScene
  control route = Fantasy Authority Scene Address
```

它们只是Host adapter不同。两者都必须创建同一个portable Authority Source、Authority Pipeline和Host launch request；Room不得同时锁定两种route，也不得在Active后切换。

## Dependency Boundary

本change继续消费：

- `Float32WorldBodyBinding`、`DotRecastStateWorldBodyBinding`、`NavigationSurfaceArtifact`、`NavigationSurfaceAsset`、`DotRecastWorldSolver`和`DotRecastWorldSolverDefinition`。
- portable Authority Pipeline catalog、neutral Runtime Package、Authority Source runtime、`ServerAuthoritativeAuthoritySessionRuntimeLauncher`、`IServerAuthoritativeAuthorityControlTransport`和`ServerAuthoritativeAuthorityHostLaunchRequest`。
- 既有ServerAuthoritative direct UDP endpoint、canonical command/snapshot codec和Network Checkpoint codec。

Authority Scene只提供manifest lowering、Fantasy Inner control adapter、显式DotRecast Solver和Entity lifecycle装配。Actor硬接触属于共享DotRecast WorldSolver的批量世界求解阶段，不属于Authority Scene、Network Model或Host adapter。若它需要第二Composer、第二Source、第二Evaluator、第二WorldSolver、Unity Definition或另一份packet codec才能启动，实施 MUST停止并修正基础边界。

## Runtime Chain

```text
Client Input
  -> Corin Float32 Program
  -> ServerAuthoritative Prediction Pipeline
  -> shared DotRecast Solver
  -> committed predicted state
  -> existing UDP command endpoint
  -> Fantasy Server DotRecast Authority Scene
  -> portable Authority Source
  -> same Authority Pipeline neutral Runtime Package
  -> same Corin Float32 Program
  -> same shared DotRecast Solver
  -> Authority Runtime Launcher
  -> Host launch validation
  -> unique portable Float32 Composer
  -> finalized committed authority state
  -> Network Checkpoint / remote replication
  -> existing baseline merge, restore and replay
  -> existing Presentation runtime
```

Command只发送输入和时序身份。Prediction结果、Transform、Body、applied displacement和DotRecast polygon MUST不发送给Authority作为运动起点。

## Actor Contact Ownership

Recast/Detour继续只负责静态Navigation Surface定位、局部可达位移和高度投影。它不负责角色之间的硬接触。Actor硬接触由portable `ActorContactSolver`负责，但该Solver不是第二个World owner，也不独立提交World state；它只能由`DotRecastWorldSolver.ResolveBatch`在取得同一Tick完整roster候选解后调用。

唯一处理链固定为：

```text
同一SimulationStep的完整WorldSolveBatchRequest
  -> 对全部Actor执行DotRecast静态Surface候选求解
  -> ActorContactSolver同时裁决候选轨迹
  -> 对接触修正结果重新执行Surface约束
  -> 一次性生成全部FinalBody与CharacterWorldSolveResult
  -> 原子发布NextWorldState
```

`ActorContactSolver` MUST不读取Graph、StateMachine、Timeline、Action、Blackboard、Network Model、Transform、Collider或Presentation。它只读取当前batch的BeforeBody、surface candidate、显式接触形状与固定求解配置，并返回修正后的候选位姿。任一Actor求解失败时整个batch失败，不能发布部分Body。

## Actor Contact Data

每个DotRecast Actor binding必须显式提供canonical接触形状：

```text
ActorContactShape
  Radius
  Height
  SkinWidth
```

当前角色只支持直立地面角色，因此接触几何定义为XZ平面的圆盘与Y轴垂直区间。两个Actor只有在垂直区间重叠时才参与平面硬接触。形状必须进入binding identity、Authority Scene manifest canonical bytes和WorldConfigurationHash；Unity Prediction与Fantasy Authority必须加载相同值。Navigation build的AgentRadius/AgentHeight只描述可行走surface烘焙，不得隐式充当每个Actor的运行时接触形状。

求解配置必须显式保存固定迭代次数、接触容差和最大去穿透距离，并进入Solver/World identity。不得使用未序列化默认值、按帧自适应迭代或Host私有参数。World state只保存最终Body，不保存pair cache、TOI cache或上一Tick接触集合。

本change只安装`SolidBodyBlock`一种正式响应语义。若未来攻击、霸体、击退、ghost或队伍规则需要改变响应，必须先把model-neutral接触策略编译进正式Motion/World request，再由同一ActorContactSolver消费；不得读取状态名、动画producer、Tag字符串或新增Action专属碰撞执行器。

## Actor Contact Algorithm

固定roster按ActorId排序，pair按`(minActorId, maxActorId)`排序。当前2v2vE目标数量很小，采用确定性的`O(n^2)` pair枚举；不提前引入空间索引或Crowd状态。

每个Tick按以下步骤求解：

1. 为全部Actor独立计算受Navigation Surface约束的candidate displacement，但不生成最终result。
2. 对垂直区间重叠的每个pair，以BeforeBody为起点、candidate displacement为轨迹执行相对运动扫掠，求最早圆盘TOI，避免20至60Hz固定Tick下冲刺穿透。
3. 到达接触点后，只移除双方剩余位移中使距离继续缩小的法向分量，保留切向分量形成滑动。静止Actor的零位移不因另一个Actor主动移入而被通用接触层转换成推行位移；双方同时移动时分别裁剪自己的闭合法向分量。
4. 对初始轻微重叠或浮点误差执行固定次数的有界去穿透。修正按稳定pair顺序累积到独立缓冲后统一应用，避免遍历到谁就先改谁。超过最大去穿透距离视为无效World/Spawn配置并使batch失败。
5. 每轮接触修正后，通过同一DotRecast surface查询重新约束修正目标。接触层不得把Actor推出合法surface或穿入墙体；若一侧修正被静态边界截断，剩余约束继续由后续固定迭代裁决，不直接写入未约束位置。
6. 固定迭代结束后再次校验全部垂直重叠pair的最小间距。仍存在超容差穿透时batch失败，不静默发布重叠World state。
7. 只从最终位置计算AppliedDisplacement、Velocity、Sides与FinalBody，并一次性创建NextWorldState和全部WorldSolveResult。

该算法是窄范围的运动学Actor接触求解，不是完整KCC：静态台阶、斜坡、边界与高度仍由DotRecast Surface负责；它不实现刚体质量、冲量、摩擦、弹跳、旋转碰撞、任意动态障碍或物理堆叠。

## Network And Prediction Semantics

Authority必须在同一World ResolveBatch内同时求解完整locked roster，最终接触结果进入committed World state、checkpoint和replication。Client Prediction继续只模拟本地owner，remote actor只消费权威replication；Client与Authority必须使用相同Solver revision和shape/config identity，但Client不得为remote actor建立Collider、客户端专属去穿透或第二套接触求解。权威checkpoint是Actor间接触的最终裁决，既有restore/replay负责把owner收敛到权威接触结果。

网络协议不新增pair contact、TOI、客户端FinalBody或applied displacement字段。握手继续通过Solver identity与WorldConfigurationHash拒绝形状或求解配置不一致。Presentation只插值committed/predicted body sample，不执行客户端专属去穿透或视觉位置反推。

## Research And Alternatives

公开资料只能证明商业游戏使用的引擎与可观察行为，不能证明其私有角色接触算法。可用结论是：For Honor公开credits列出Havok，公开平衡说明把动作位移、轨迹、墙碰撞和推行作为逐动作业务参数；Delta Force公开技术访谈确认使用Unreal Engine 4，而Unreal CharacterMovement公开合同把胶囊硬碰撞、depenetration、服务器预测/校正与可选RVO分开。它们支持“硬接触、动作位移和网络校正分层”的方向，不支持把RVO当成无穿透保证。

参考资料：

- [For Honor credits](https://www.ubisoft.com/en-gb/game/for-honor/credits-2017)
- [For Honor公开动作与碰撞调整示例](https://www.ubisoft.com/en-us/game/for-honor/news-updates/5K7gJrsS7BGjegCjnnZydv)
- [Delta Force Unreal Engine 4访谈](https://store.epicgames.com/news/delta-force-behind-the-scenes-interview-cover-story)
- [Unreal CharacterMovementComponent](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Runtime/Engine/UCharacterMovementComponent)
- [Unreal Networked Movement](https://dev.epicgames.com/documentation/unreal-engine/understanding-networked-movement-in-the-character-movement-component-for-unreal-engine)

方案取舍：

- `DetourCrowd/RVO`适合让AI提前绕开彼此，但只调整期望速度，不能保证冲刺、出生重叠或网络误差下绝不穿透，因此不能作为硬接触层。
- `Box2D.NET Character Mover`提供纯C#几何移动器和可参考的碰撞平面裁剪，但当前接口仍标记experimental，且引入完整2D碰撞世界会与既有DotRecast静态Surface形成第二份几何真相；本change只借鉴其运动学裁剪思路，不引入运行时依赖。
- `BEPUphysics2/Jitter2`可提供纯C# 3D碰撞，但会增加刚体World、broadphase、质量/冲量状态和额外snapshot所有权。当前Demo没有物理玩法，成本与业务不匹配。
- 完整自研KCC能覆盖台阶、斜坡、动态平台与复杂形状，但会重复DotRecast已经拥有的静态Surface职责。本change只实现Actor对Actor的窄接触层。
- 选择内置portable ActorContactSolver会承担少量几何算法维护成本，但它直接消费现有batch、没有第二World state、同时服务Unity Prediction与Fantasy Authority，是当前固定小roster最小且完整的闭环。

## Authority Scene Manifest

Manifest是Fantasy DotRecast Authority Scene唯一运行组合配置，保存canonical bytes并形成ManifestHash。它包含：

- HostProfile、HostId、Fantasy process/Authority Scene、Room和direct UDP Data endpoint。
- `.csim`相对路径及Program/Layout/Numeric/ABI/operation-set身份。
- Authority Pipeline descriptor、Backend和Source policy身份。
- stable Actor roster、initial Character state/body与output route。
- WorldId、MapId、WorldRevision、NavigationSurfaceArtifact、QueryProfile、每Actor接触形状与接触求解配置身份。
- checkpoint、Committer、Transport、clock和diagnostics配置。

它 MUST不包含Fantasy Control endpoint、外部WorkerId、Worker process role或Console Client连接配置。路径只能相对manifest目录，不能逃逸。Loader在注册Authority Host route和创建Transport前重读全部artifact并核对实际bytes；不读取Unity YAML、不保存ScriptableObject镜像、不提供旧Worker schema reader。

## Fantasy Inner Control Adapter

DotRecast Authority Scene使用正式Inner/Address消息与Gate Scene交换：

```text
authority scene register / response
roster lock
data-plane ticket issue / consume
heartbeat / latest authority tick
reliable event batch
full checkpoint request / response
leave
session failure
```

Inner Handler只验证消息外壳并写入host-neutral control adapter queue。Adapter实现`IServerAuthoritativeAuthorityControlTransport`，不执行Program、Solver、checkpoint policy或Presentation。Routine command/snapshot只走Authority Scene拥有的portable direct UDP endpoint，绝不经Inner或Outer控制消息转发。

全量checkpoint有两种明确的控制语义：Authority在Client尚无可确认baseline时以`RequestSequence = 0`主动发布bootstrap checkpoint；Client因delta base缺失发起恢复请求时，Gate生成非零request sequence，Authority响应必须精确回显该sequence。Gate只在不存在pending request时接受bootstrap checkpoint，只在sequence精确匹配时接受requested checkpoint；两种路径都继续校验Room/Session/Player/Actor、Authority Tick、Snapshot Sequence、Layout、Hash和payload边界。

## Protocol Identity

External Unity Worker register、InProcess DotRecast Scene register和Client join共同锁定：

```text
Program + Layout + OperationSet + TickRate
Prediction/Authority Pipeline pair + Backend
AuthorityHostProfile
SolverId + Version + Capabilities + Features
WorldId + MapId + WorldRevision
NavigationSurfaceArtifactHash + QueryProfileHash
WorldConfigurationHash
```

Room把两种Host register降低为同一个portable Authority identity。Client join request携带本地Prediction组合的实际identity，join response返回Room锁定的Authority identity；两端不匹配时在Prediction Session Active前失败。

`ClientJoinAccepted`改变了Outer控制协议的生命周期合同，因此ModelProtocolVersion MUST递增。旧Player、旧Fantasy Server与新协议不得混跑；版本不匹配必须在join阶段明确失败，不得降级为旧的response/push顺序。

## Clock And Lifecycle

Authority Scene的Entity lifecycle拥有唯一runtime handle。启动顺序固定为：

1. 读取并校验manifest及全部artifact。
2. 创建direct UDP endpoint和Fantasy Inner control adapter。
3. 向Gate Scene注册Host route并锁定Room identity。
4. 接收固定Actor roster和data-plane ticket。
5. 创建共享DotRecast Solver及initial World state。
6. 构造绑定Authority Source policy与locked roster的Runtime Launcher，由Launcher通过portable Host launch request调用唯一Float32 Composer。
7. 在Authority Scene线程用单调时钟按固定delta推进runtime handle。

短时落后只允许执行有界catch-up tick；超过lag上限时Room fail-stop。每个authority tick只推进一次runtime handle。销毁顺序固定为停止输入、runtime handle、Source、Solver/World、control/data transport和artifact owner。

## Static Map Authoring

`ServerAuthoritativeTestMap.prefab`是测试环境可见静态地图、Unity Authority Worker碰撞地图和NavigationSurface的唯一几何源。DotRecast客户端、Unity Authority客户端与Unity Authority Worker Scene只实例化该Prefab，不再分别保存地面、墙及其Transform；Navigation authoring直接加载同一Prefab的canonical asset contents，按显式Layer筛选生成NavigationGeometryArtifact。

Prefab dependency hash形成source revision，几何、Transform或依赖Mesh变化后必须重新发布NavigationSurfaceAsset和Authority Scene artifact。Runtime仍只读取canonical NavigationSurfaceArtifact，不读取Prefab或Unity Scene。旧`CorinNavigationSurfaceSource.unity`纯平面源必须删除，不能作为fallback或第二烘焙入口保留。

## Client Scene And Lifecycle

DotRecast Client Scene显式引用Prediction Composition、Endpoint、Corin Program/Projection、DotRecast Solver Definition、NavigationSurfaceAsset、state-only owner binding、remote presentation registration、spawn、camera和diagnostics。Scene不包含CharacterController binding或CC Solver。

Client A/B只通过launch profile提供不同PlayerId/ActorId；不复制Scene。两端Actor binding必须显式保存相同的接触形状，且与Authority Scene manifest的roster binding形成相同WorldConfigurationHash。Bootstrap只按TestScenarioId跳转。离开Scene时必须释放Source输入、SessionHost、Actor registration、Endpoint、History、Solver/World和Presentation；这些owner不能通过`DontDestroyOnLoad`跨测试环境存活。

## Build Layout

每种网络模型拥有固定且互不重叠的当前构建目录。同一模型再次Build时替换自己的Player、Server、manifest与Authority artifacts；不同模型不得互相覆盖。`BuildId=yyyyMMdd-HHmmss`只记录在build manifest中，用于识别当前产物，不参与路径寻址。Build与Run是两个独立入口，Build不启动进程，Run不触发编译：

```text
Build/Network/UnityAuthority/
  UnityAuthorityBuild.json
  Player/
  Logs/<RunId>/
3cDemo/Server/Build/Network/UnityAuthority/Server/

Build/Network/DotRecastAuthority/
  DotRecastAuthorityBuild.json
  Player/
  Logs/<RunId>/
3cDemo/Server/Build/Network/DotRecastAuthority/Server/
  Fantasy.config
  Authority/DotRecastAuthorityScene.manifest
  Authority/Artifacts/CharacterProgram.csim
  Authority/Artifacts/NavigationSurface.navsurface
```

Unity Authority Run脚本校验当前build manifest后启动Fantasy Server、Unity Authority Worker、Client A和Client B。DotRecast Run脚本校验当前build manifest与Authority artifacts后，只启动包含Gate与Authority Scene的Fantasy Server、Client A和Client B。脚本必须先构造完整参数数组，再调用`Start-Process -ArgumentList`。

DotRecast Run脚本必须通过进程环境变量`THIRDPERSON_DOTRECAST_AUTHORITY_SERVER_ROOT`把已校验的Server发布根传给Fantasy Server，并在创建进程后恢复脚本进程原值。该路径不得作为Fantasy自定义命令行参数，因为Fantasy入口会拒绝不属于其正式Parser的选项。

DotRecast Run脚本 MUST把Server stdout/stderr和两个Client日志写入同一RunId目录；进程提前退出或endpoint deadline失败时，脚本 MUST回传三端最近的正式失败事实，不得只返回无业务上下文的进程退出码。

Unity Editor的Run入口 MUST通过一次性UTF-8 result文件取得启动结果，不得持续读取启动脚本stdout/stderr；Server与Client长驻进程的输出必须重定向到RunId日志目录，不能继承并占用Editor等待的匿名管道。

Unity Authority与DotRecast Authority测试环境共享固定control/data ports，不能并发运行。两个Run入口的`-StopExisting` MUST清理两种模型目录下已识别的网络测试进程并等待端口释放；若端口由仓库外进程占用，MUST保留该进程并报告端口与PID。

Server publish完成后必须按模型收紧正式`Fantasy.config`：Unity Authority目录只允许Gate Scene，DotRecast Authority目录只允许Gate Scene与DotRecast Authority Scene。两种Build不得直接复用同一份未裁剪的Server配置，否则会在Unity四进程环境中同时启动两个Authority Host。

## Failure Policy

- Manifest、artifact、identity、spawn或组合错误：Authority Scene注册Room前失败。
- Client与Authority Scene的Solver/World/Artifact/Profile不一致：join失败。
- Authority Scene失活、Inner control route断裂或任一Actor的solve/checkpoint/transport/reliable queue失败：固定双Actor Room整体fail-stop。
- Scene/Server关闭：完整销毁，不保留可恢复的旧Session。
- 不回退CC、Local、Transform直写、旧协议、外部DotRecast Worker或另一Host。

## Tradeoffs

- 将DotRecast放进Fantasy Server减少一个进程和Console Client依赖，但使该测试Host的部署生命周期归Fantasy Server所有。
- 使用独立Authority Scene而不是直接放进Gate增加一条正式Inner控制路由，但保持Room与Gameplay执行的线程、Entity和销毁边界清楚。
- Unity Authority仍保留外部Worker，导致两种Host的控制adapter不同；它们共享相同portable Source/Pipeline/Composer和数据面，因此不是两套Gameplay实现。
- 前后端共享DotRecast与ActorContactSolver可统一静态surface和角色body-block语义，但仍不提供完整KCC、动态障碍、刚体推挤或动作专属接触策略。

## Implementation Order

1. 迁移Worker manifest为Authority Scene manifest并删除外部Worker字段。
2. 建立Fantasy DotRecast Authority Scene与portable Host装配。
3. 将Room重构为唯一Authority Host route并接Fantasy Inner control adapter。
4. 扩展正式Inner/Outer identity协议并重新生成代码。
5. 建立DotRecast Prediction Composition、Client Scene和launch profiles。
6. 建立隔离Server/Player Build发布和三进程启动脚本。
7. 删除外部DotRecast Worker概念与旧路径，完成编译和严格校验。
8. 将DotRecast逐Actor最终求解重构为全roster surface candidate阶段。
9. 增加显式Actor接触形状、portable ActorContactSolver与唯一batch硬接触阶段。
10. 将接触配置纳入Prediction/Authority World identity，更新资产、manifest、诊断和严格校验。
