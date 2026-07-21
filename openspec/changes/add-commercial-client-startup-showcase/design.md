# Design: 商业客户端启动展示链

## 目标

建立一条真实、唯一、可观察的普通产品启动链，使作品展示可以沿同一个运行实例说明：

1. Client、Resource 与 Protocol 三类版本如何校验。
2. 缓存文件如何检查完整性，损坏后如何只恢复缺失内容。
3. Core、Gameplay 与 OptionalHD 如何按业务时机下载。
4. 中断下载如何依赖 HTTP Range 从临时文件继续。
5. 登录通信为什么是 WSS，为什么它不等于现有 Gameplay KCP/UDP。
6. 单 AuthGateway 内如何保证同一 AccountId 只有一个有效 Session Generation。
7. 主页和玩法资源如何按 scope 预加载、复用、释放和观察内存变化。

## 非目标

- 不把项目改造成完整在线游戏或账号平台。
- 不给 Gameplay Program、SimulationSessionHost、Network Model 或 WorldSolver 增加启动职责。
- 不建立备用端点、离线入口、无服务器快速进入、旧 Procedure 兼容或 mock 登录。
- 不删除Editor直接运行Gameplay Lab的本地玩法开发能力；它不是产品入口。
- 不让 Fault Lab 参与普通 Release Player 的策略选择。

## 当前问题

### 启动权威分裂

当前 `ProcedureLoadAssembly` 先切换到 `ProcedureStartGame`，再反射调用 `GameApp.Entrance()`。AOT Procedure 随后加载 `StandaloneGameplay`，HotFix `GameApp` 同时初始化 Fantasy。两边都在推进“开始游戏”，但没有一个对象拥有从资源完成到主页 Ready 的完整状态。

### 资源能力存在但未形成产品策略

- `ProcedureInitPackage` 以 `needInitMainFest=true` 初始化，内部已经请求版本并更新 Manifest；`ProcedureInitResources` 再次请求相同事实。
- `DefaultPackage` 只有 Scenes 与 HotUpdateAssemblies 收集组，标签为空。
- Host/Web 资源地址为空，Bootstrap 使用 EditorSimulateMode。
- YooAsset 缓存初始化使用默认 Middle 校验。
- 断点续传最小尺寸仍为 `long.MaxValue`，等价于未启用。
- 只有 fatal 日志，没有可取消任务、结构化错误、重试状态或下载证据。

### 网络 Session 所有权不满足认证加玩法并存

`FantasyClientBootstrap` 当前暴露一个全局 `FantasySessionFacade`，每次 Connect 会先 Disconnect。若认证 WSS 与后续 Gameplay 控制连接复用该对象，建立第二个连接会销毁第一个连接，无法维持唯一登录推送，也会让 Network Model 失去自己的连接生命周期。

### 资源释放没有业务所有者

TEngine 能合并并发加载、维护 AssetObject pool 并释放零引用资源，但它不知道某个引用属于 Home、Gameplay 或临时窗口。业务如果继续直接拿 Unity Object 并分散调用 `UnloadAsset`，无法解释谁负责释放，也无法可靠展示加载前后内存。

## 总体架构

```text
Player 内置 Bootstrap Scene
  -> ProductBootstrapRunner (AOT, 唯一启动状态写入者)
     -> StartupPolicyClient
     -> ProjectResourceInitializationAdapter
        -> TEngine ResourceModule
           -> YooAsset DefaultPackage
     -> HotUpdateAssemblyLoader
     -> GameApp.Entrance

GameApp.Entrance
  -> ProductStartupCoordinator (HotFix, 唯一产品进入写入者)
     -> ProductShell Scene
     -> ProductAuthSessionOwner
        -> Fantasy WSS AuthGateway
     -> HomeResourceScope
     -> Home
     -> GameplayResourceScope
     -> StandaloneGameplay

只读观察
  -> ProductStartupSnapshot
  -> ResourceRuntimeSnapshot
  -> MemoryRuntimeSnapshot
  -> Bootstrap / ProductShell Diagnostics UI
```

TEngine Procedure 只编排热更代码可用之前的 AOT 阶段。`ProductStartupCoordinator` 只在 `GameApp.Entrance` 后存在。两个写入者通过一次性的 handoff result 交接，不同时决定场景。

## 决策一：用一个 DefaultPackage 和业务标签，不创建多个 Package

正式标签：

| 标签 | 内容 | 下载时机 | 生命周期 |
|---|---|---|---|
| `Core` | HotFix DLL、ProductShell、登录/Home UI、公共字体与必要共享表现资源 | 加载热更程序集之前 | Global |
| `Gameplay` | StandaloneGameplay、Corin 玩法资源、必要动画与战斗表现依赖 | Home 点击开始之后 | Gameplay |
| `OptionalHD` | 不影响登录和玩法正确性的真实高清表现资源 | Home 显式选择之后 | Home 或 Gameplay 显式 scope |

一个 Package 的业务收益是版本和 Manifest 只有一个真相，标签已经足够表达首包、玩法包和可选包。多个 Package 可以获得更强物理隔离，但需要独立初始化、版本协调和跨包依赖治理；对当前单角色 Gameplay Demo 没有足够收益。

不得为了凑标签把 Gameplay 必需资源标为 OptionalHD。OptionalHD 没有真实可选资源时，实施必须先创建或选择真实业务资产，再配置标签，不能留下空标签占位。

## 决策二：AOT Bootstrap 内置，Core 下载后才允许加载 HotFix

Bootstrap UI、错误文本、最小字体和启动运行器必须进入 Player 内置闭包。它们不能由 DefaultPackage 的远端 Core 提供，否则首次安装或 Core 损坏时没有界面可以说明失败。

Core 下载和完整性满足前，`HotUpdateAssemblyLoader` 不得运行。AOT 层只理解程序集清单和 handoff，不引用登录、Home 或 Gameplay 类型。

业务收益是启动失败始终可解释，旧热更代码不会在新 Manifest 尚未完整激活时提前运行。代价是 Bootstrap UI 必须保持小且稳定，更新它需要发新 Player，而不是资源热更。

Editor只保留`Tools/3C/Launcher`一个入口，并按业务目的固定为四组：`单机 / Gameplay Lab`直接运行Local Float32或Local Fixed且不进入资源与认证链；`双端验证 / Network Test Products`分别Prepare、Build、Run网络测试产品；`正式启动 / Published Player`构建并运行正式Content与Player发布闭包；`编辑器启动 / Bootstrap Play`在Editor中从Bootstrap进入同一正式资源、认证与Gameplay链。Gameplay Lab使用`Assets/Scenes/GameplayLab/GameplayLab.unity`，复用正式角色、Presentation与测试环境作者来源，但与产品`StandaloneGameplay`分责；它不修改Build Settings、不加载ProductBootstrap、不填充假endpoint，也不进入Release Player。当前仓库没有Motion Matching、Pose Search或Pose Database，因此本地入口只能按真实能力标为IK、步态相位匹配、Motion Warp、KCC与角色管线验证。

## 决策三：StartupPolicy、YooAsset 版本和 Auth Protocol 各自保存一个事实

版本身份分为：

- `ClientBuildVersion`：编译进 Player，表示二进制版本。
- `MinimumClientBuildVersion`：由正式 ResourceEndpoint 下的 StartupPolicy 提供，决定当前 Player 是否还能进入资源更新。
- `ResourcePackageVersion`：只来自 YooAsset package version 请求，决定 Manifest 与 Bundle 集合。
- `AuthProtocolVersion`：编译进 Client/Server 协议合同，在 Auth 登录时再次校验。

StartupPolicy 不重复保存 ResourcePackageVersion，避免两个远端文件争夺资源版本真相。Auth Server 不重新决定应下载哪个 Manifest，只拒绝不兼容的 Client/Protocol。

若 ClientBuildVersion 低于最低版本，启动进入 `ClientUpdateRequired` 终态；本变更不实现商店跳转，也不继续资源更新。任何缺失、非法或 schema 不支持的 StartupPolicy 都是配置/服务错误，不使用内置默认值。

## 决策四：启动状态使用单一可取消运行器

正式阶段：

```text
Launch
-> RequestStartupPolicy
-> InitializePackageAndVerifyCache
-> RequestPackageVersion
-> UpdatePackageManifest
-> PlanCoreDownload
-> AwaitCoreDownloadConsent
-> DownloadCore
-> ClearObsoleteCache
-> LoadHotUpdateAssemblies
-> EnterProductRuntime
```

HotFix 产品阶段：

```text
LoadProductShell
-> ConnectAuthGateway
-> AwaitGuestLogin
-> PreloadHome
-> HomeReady
-> PlanGameplayDownload
-> DownloadGameplay
-> PreloadGameplay
-> EnterGameplay
```

每个阶段产生 immutable snapshot：阶段、generation、开始时间、已用时间、版本、文件计数、字节、速度、重试次数和结构化错误。UI 只订阅 snapshot，不直接调用 ResourceModule 或改变状态。

每次 Retry 增加 generation、取消上一 generation、等待其资源清理完成，然后从失败阶段的正式入口重新执行。旧异步完成回调发现 generation 不匹配时不得推进状态。不存在“失败后直接进游戏”“退回 EditorSimulateMode”或“换备用 URL”。

## 决策五：完整性、可信来源和资源混淆分开表达

- High CRC 检查缓存和下载文件是否与 Manifest 记录一致。
- HTTPS 保护 StartupPolicy、Manifest 与 Bundle 的传输通道。
- 当前变更不实现 Manifest 数字签名，因此不宣称能抵抗控制了资源源站的恶意篡改。
- TEngine FileOffset/FileStream 只属于资源混淆，当前正式配置保持关闭，也不出现在通信安全状态中。

这种口径让展示结果可被追问：CRC 负责损坏，TLS 负责传输，签名才负责离线验证发布者。代价是本轮不会宣称完整防篡改能力，但避免用错误术语包装已有实现。

## 决策六：断点续传使用 YooAsset 原生临时文件和 HTTP Range

项目只设置 YooAsset 正式文件系统参数：

- High FileVerifyLevel。
- 明确的 ResumeDownloadMinimumSize。
- 明确支持的响应码集合。
- 正式下载并发、每帧请求和重试策略。

这些参数通过 TEngine `IResourceModule` 的通用 `ResourcePackageInitializationOptions` 进入唯一 PackageMap。该扩展只负责把文件系统参数、单一远端服务和缓存校验结果送入/送出 YooAsset；它不引用 ProductStartupProfile、页面、登录或 Gameplay。项目层不得绕过 TEngine 再初始化第二个 ResourcePackage。

ResourceEndpoint 必须为 Bundle 响应 `Accept-Ranges` 并正确处理 `Range`。取消或进程退出保留 YooAsset 管理的合法临时文件；重启后继续同一 Bundle。项目不创建第二份 `.part` 格式、不复制下载器、不自行拼接文件。

下载前根据 downloader 的总字节数执行磁盘空间预检。需要空间包含剩余下载字节、临时文件增长与明确安全余量；空间不足进入结构化错误，不开始部分下载。

## 决策七：WSS 只服务认证，Gameplay 网络模型保持原样

认证端点固定为 WSS，不提供 TCP/KCP/WebSocket 运行时下拉。TLS 可在正式反向代理或 Fantasy WebSocket 服务边界终止，但 Client 只接受 `wss://` AuthEndpoint，Release 配置不得关闭证书校验。

现有 ServerAuthoritative 仍保持 Fantasy KCP 控制面和直接 UDP Gameplay 数据面；DeterministicRollback 仍使用自己的 Relay。认证成功不会把 Auth Session 注入 Gameplay Network Model，也不会把 KCP 描述成加密链路。

业务收益是“登录通信已加密”的范围清楚，同时不为了启动展示重写已完成的实时网络纵切。代价是客户端可能同时拥有 Auth WSS 和 Gameplay Control Session，因此 Session ownership 必须从全局单例拆开。

## 决策八：认证和 Gameplay Session 分别拥有 Fantasy Scene

`FantasyClientBootstrap` 只负责 Fantasy runtime 初始化/关闭和创建 session owner 所需的公共入口，不再持有唯一可变 SessionFacade。

- `ProductAuthSessionOwner` 创建并销毁自己的 Fantasy Scene 与 WSS Session。
- `ServerAuthoritativeControlSessionModule` 创建并销毁自己的 Fantasy Scene 与 KCP Session。
- 任一 owner 的 Disconnect 不得 Dispose 另一 owner 的 Scene。
- GameApp 最终退出时按 ProductCoordinator、Gameplay Session、Auth Session、Fantasy runtime 的明确顺序清理。

这不是提供任意数量的通用连接池；只为两个已知业务所有者提供窄生命周期。

## 决策九：唯一登录限定为单 AuthGateway Scene

AuthGateway Scene 在 `OnCreateScene` 时创建唯一 `AuthSessionRegistryComponent`。Registry 保存：

```text
AccountId -> SessionAddress/RuntimeId + ClientInstanceId + Generation + SessionTokenIdentity
```

游客登录流程：

1. Client 通过 WSS 提交 GuestAccountId、ClientInstanceId、ClientBuildVersion 与 AuthProtocolVersion。
2. Handler 在任何异步挂起前完成格式和版本校验。
3. Registry 为 AccountId 生成单调 Generation，并原子替换当前记录。
4. 新 Session 附加包含 AccountId 与 Generation 的认证组件。
5. Server 返回不记录到日志的短期 SessionToken 和 Generation。
6. 若存在旧 Session，Server 向其发送 `AccountSessionReplaced` 后关闭旧 Session。
7. Session 销毁时仅在 Registry 当前记录仍匹配自己的 AccountId、Generation 和 Session identity 时删除。

单 Scene 的顺序执行边界足以展示竞态安全的顶号和旧连接条件清理。跨 Gate 的全局唯一需要外部一致性存储和租约，本变更明确不实现，UI 与文档不得声称支持。

## 决策十：新建独立 Startup Server 产品

新增 `ThirdPerson.Startup.Server`，只包含 AuthGateway 所需 Entity/Hotfix 模块、配置、协议和共享 Server Host。它不是 ServerAuthoritative Authority 产品，不包含 Room、Authority Scene、UDP 数据面、Character Program 或 Gameplay runtime，也不进入 Network Test Product catalog。

该产品使用明确 ProductId、Fantasy.config、WSS 外部端点与正式 publish root。Build/publish 命令可以复用共享 Server Host manifest 工具，但不得在 Run 时临时编译、改写证书路径或搜索其它产品目录。

## 决策十一：ProductShell 是登录和主页的唯一产品场景

Core 包含一个 `ProductShell` Scene。`ProductStartupCoordinator` 加载它后创建：

- 登录视图。
- Home 视图。
- Startup/Resource/Network/Memory 只读诊断页。
- Development Build Fault Lab。

登录成功前 Home 不可进入。Home 点击开始后创建 Gameplay scope、下载 Gameplay tag、按 PreloadPlan 加载必要资源，再以 Single 模式进入现有 StandaloneGameplay。场景进入失败时释放未提交的 Gameplay scope并返回明确错误，不直接使用 SceneManager 加载未校验场景。

`ProcedureStartGame` 和任何启动后自动加载 StandaloneGameplay 的调用必须删除，避免两条场景权威。

## 决策十二：业务资源通过 Scope/Lease 持有

项目层 `ProductResourceRuntime` 是业务唯一入口，内部调用 TEngine `IResourceModule`：

```text
GlobalScope
  Core/ProductShell/公共资源

HomeScope
  Home UI/展示角色/主页背景

GameplayScope
  StandaloneGameplay/Corin/战斗资源

TransientScope
  明确短生命周期操作
```

每个逻辑加载返回 lease。相同 package/location/type 的并发请求共享一个 in-flight physical load；成功后每个调用方得到自己的逻辑 lease。Prefab asset 只加载一次，但实例化 GameObject 仍具有独立 instance identity和销毁责任。

Scope Dispose 原子禁止新 lease，取消未提交请求，释放已提交 lease。只有引用归零的资源才会被送回 TEngine pool。ProductResourceRuntime 不直接 Dispose YooAsset 内部 handle，不复制 TEngine AssetObject pool。

## 决策十三：预加载顺序是业务计划，Bundle 依赖由 YooAsset 负责

每个目标页面使用 immutable PreloadPlan 声明业务 barrier，例如：

```text
Home: Shared UI -> Home UI -> Home presentation
Gameplay: Shared shader/font -> Scene -> Corin presentation -> Ready
```

同一 barrier 内允许并发，不同 barrier 必须按序完成。YooAsset 继续解析 Bundle dependency，PreloadPlan 不保存 AssetBundle 名称、不复制 Manifest dependency graph。

业务收益是能解释“什么时候允许进入页面”，同时不把资源底层依赖手写一遍。代价是每个产品页面需要维护小型、稳定的 location 清单。

## 决策十四：卸载只在所有权释放后和安全点执行

关闭页面或离开 Gameplay 时先 Dispose 对应 scope。`UnloadUnusedAssets` 只在以下安全点执行：

- ProductShell 与 Gameplay 的 Single Scene 切换后。
- 返回 Home 的加载遮罩阶段。
- 显式资源维护阶段。
- Unity low-memory 回调。

普通窗口关闭只释放 lease，不立即调用 `Resources.UnloadUnusedAssets` 或 `GC.Collect`。这样避免频繁全局扫描造成帧卡顿。Low-memory 会释放所有零引用资源，但不得销毁仍被 Global/Home/Gameplay scope 持有的资源。

## 决策十五：诊断只读，Fault Lab 只操作正式边界

诊断采集：

- Startup：阶段、generation、版本、错误、耗时、下载统计。
- Resource：logical/physical load、in-flight join、cache hit、active lease、scope、pool、package。
- Network：Auth transport、TLS、endpoint、AccountId 脱敏值、Generation、连接状态、RTT。
- Memory：Total Used/Reserved、GC、Texture、Mesh、活跃 scope 和配置预算。

采集不得使用反射读取 Fantasy 或 YooAsset 私有字段，不改变下载、Session、资源引用或 Gameplay state。

Fault Lab 仅存在于 Editor/Development Build，并调用真实公开边界：取消当前 downloader、损坏一个明确选中的缓存 Bundle、并发申请同一 location、Dispose 指定 scope、触发正式 low-memory 处理。它不注入假成功、不替换 ResourceEndpoint、不提供 mock Auth Server。

## 失败处理

| 失败 | 正式结果 |
|---|---|
| StartupPolicy 缺失或非法 | Bootstrap 显示配置/服务错误，允许同阶段重试或退出 |
| Client 版本过低 | `ClientUpdateRequired` 终态，不请求资源、不进登录 |
| ResourceEndpoint 为空或非 HTTPS | 配置错误，禁止初始化在线链路 |
| 缓存 CRC 失败 | 失效对应缓存记录，由 downloader 重新取得缺失 Bundle |
| 磁盘不足 | 下载前失败，不开始部分 Core/Gameplay 更新 |
| 下载中断 | 保留 YooAsset 合法临时文件，重启后通过 Range 继续 |
| HotFix 程序集缺失或不匹配 | 停留 Bootstrap，不调用 GameApp |
| AuthEndpoint 非 WSS | 配置错误，不连接 TCP/KCP 代替 |
| 登录版本不匹配 | 返回业务 ErrorCode，停留登录页 |
| 旧 Session 被替换 | 收到推送、清理认证状态、回到登录页 |
| ProductShell/Home 预加载失败 | 释放未提交 scope，显示明确资源错误 |
| Gameplay 进入失败 | 释放未提交 Gameplay scope，Home 保持可用 |

## 迁移与删除

- 将 `ProcedureInitPackage(..., true)` 改为只初始化 package，版本和 Manifest 由独立正式阶段各执行一次。
- 删除 `ProcedureInitResources` 的整包隐式职责，拆成明确状态和共享 runner。
- 删除 `ProcedureStartGame` 及其直接 Scene load。
- 新增无业务选择的 `ProcedureProductRuntime` 作为 HotFix handoff 后的 TEngine 终态。
- 将 `GameApp.Entrance` 从“初始化 Fantasy 后等待 AOT 进场景”改为启动唯一 ProductStartupCoordinator。
- 删除全局 `FantasyClientBootstrap.SessionFacade`，把现有 ServerAuthoritative 调用迁入其 own session owner。
- 清空并删除备用资源 URL 序列化字段的运行时使用；正式配置只保留单一 ResourceEndpoint。
- 将无标签收集规则迁移为 Core/Gameplay/OptionalHD 唯一标签集合，不保留旧无标签整包下载路径。
- 更新普通 Build Settings 与 `openspec/project.md`；Network Test Build Settings/manifest 不变。

## 风险与控制

### Bootstrap 资源体积增长

内置 UI 如果引用大字体、贴图或 Addressable 资源，会扩大 Player 并形成依赖环。控制方式是对 Bootstrap scene dependency closure 建立编辑器检查，只允许最小内置资源。

### WSS 部署证书增加运行条件

真实 TLS 需要正式证书和域名。配置缺失必须阻止 Release 启动，不能自动改用 `ws://`。Editor 也使用显式配置，不把忽略证书错误写进正式客户端。

### High CRC 增加启动时间

High 校验能为展示提供真实完整性证据，但缓存量增长后会增加启动耗时。诊断必须记录校验耗时；后续若要按平台调整校验策略，需要新 change 明确可信缓存模型，当前不预留动态降级开关。

### 单 Gateway 唯一会话不是分布式保证

该范围通过命名、UI 和 spec 明确限制。若未来增加多个 AuthGateway，必须先引入正式共享租约/一致性存储，不能复制每个进程的字典后继续声称全局唯一。

### ResourceScope 可能与 TEngine pool 双重计数

Scope 只拥有逻辑 lease，TEngine 仍拥有物理 AssetObject 和 YooAsset handle。实现不得直接操作两层引用计数或为每个 lease 创建新物理 handle；诊断必须同时显示 logical 与 physical 指标，避免把二者相加当资源数。
