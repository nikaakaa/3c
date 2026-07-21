# 商业客户端启动展示链

## 唯一产品链

普通产品只使用以下入口：

```text
Bootstrap
-> ProductBootstrapRunner
-> GameApp.Entrance
-> ProductStartupCoordinator
-> ProductShell
-> WSS Guest Login
-> Home
-> Gameplay Download / Preload
-> StandaloneGameplay
```

Bootstrap 属于 Player 内置闭包。`ProductShell` 与热更程序集属于 `Core`；`StandaloneGameplay` 及其依赖属于 `Gameplay`。Procedure 在热更入口交接后停留于 `ProcedureProductRuntime`，不再选择登录、主页或玩法场景。

## 资源端点

资源端点是唯一 HTTPS 基地址，负责提供：

- `StartupPolicy.json`；
- YooAsset package version；
- YooAsset manifest；
- Bundle 文件。

端点为空、不是 HTTPS 或属于旧示例地址时，客户端在网络请求前失败。没有备用 CDN、协议降级、离线入口或本地目录 fallback。

Bundle 服务必须保留 YooAsset 请求路径并支持 HTTP Range。对合法范围请求返回 `206 Partial Content`、正确的 `Content-Range` 和与请求偏移一致的内容；完整请求返回 `200 OK`。代理层不得压缩或改写 Bundle 字节。取消和进程退出后保留的临时文件由 YooAsset 管理，项目不定义第二种 `.part` 文件。

缓存正式使用 High 校验：文件大小和 CRC 都与 manifest 一致后才可用。`ProductStartupProfile` 的校验并发、断点续传阈值、续传响应码、下载并发、每帧请求数和 watchdog 通过 TEngine 的单一 package 初始化合同进入 YooAsset。CRC 用于发现下载或磁盘损坏，HTTPS 用于保护传输；当前没有 Manifest 发布者数字签名，资源混淆也不属于通信加密。

## 正式构建与本地资源服务

Unity Editor 只使用 `Tools/3C/Build/Commercial Client` 作为普通商业客户端正式入口。窗口要求显式填写 `ResourcePackageVersion` 与 `MinimumClientBuildVersion`，`ClientBuildVersion` 只读自唯一 `ProductStartupProfile`，不使用时间、目录名或 EditorPrefs 推断版本。

正式输出固定为：

```text
Build/
├─ .Workspace/                                      构建过程数据，不发布
├─ Content/<BuildTarget>/DefaultPackage/<ResourcePackageVersion>/
├─ Players/<BuildTarget>/<ClientBuildVersion>/
└─ Network/                                         现有网络测试产品
```

Content 版本目录只包含 `StartupPolicy.json`、YooAsset runtime version/manifest/Bundle 文件和 `CommercialContentRelease.manifest.json`。Player 版本目录包含平台可执行闭包和 `CommercialPlayerRelease.manifest.json`。两者都先进入 `.Workspace`，写入相对路径、长度与 SHA-256，完成 exact closure 校验后才原子发布；同名版本已存在时直接失败，不覆盖。

`.Workspace`、YooAsset `OutputCache`、BuildReport、`Library/YooAsset/BuildOutput` 与 `HybridCLRData` 都是可重建的本机工作区，不是 CDN 或 Player 发布源。YooAsset embedded Editor 的默认输出 helper 已改到 `Library/YooAsset/BuildOutput`，升级 YooAsset 时必须审查并保留这一 editor-only patch。`HybridCLRData` 保持插件原生工作区，只通过正式热更 DLL 复制与 YooAsset 收集链进入 Content。

本地 HTTPS 服务直接把一个已经发布的完整 Content 版本目录设为 document root，不复制 `LocalCDN`、`Remote` 或 `ServerData` 镜像。切换资源版本时修改服务器的 document root 映射。远端 CDN 上传、域名、证书申请和部署自动化不属于当前变更。

## 证书边界

Release Client 只接受受系统信任链验证的 HTTPS/WSS 证书。证书私钥只部署在资源源站、反向代理或 Startup Server 的 TLS 终止边界，不进入 Unity 项目、资源包、产品 manifest、日志或诊断快照。

`AuthEndpoint` 必须使用 `wss://`。认证连接不回退 `ws://`、TCP 或 KCP。现有 ServerAuthoritative KCP 控制面与 UDP Gameplay 数据面保持原样，它们不因认证使用 WSS 而变成加密 Gameplay 通道。

## Startup Server 产品

`ThirdPerson.Startup.Server` 是独立 Fantasy 产品，只包含共享 Server Host、Auth Entity、Auth Hotfix、生成 Outer 协议、产品配置和产品 manifest。它不包含 Gate Room、Authority Scene、Character Program、WorldSolver、UDP Gameplay 数据面或 Network Test Product 分支。

发布命令必须先构建产品，再原子替换 Startup 产品目录并写入 exact file closure manifest。Run 只读取已经发布且 hash、ProductId、Scene type 和模块集合都精确匹配的目录；Run 不临时 build、publish、查找其它产品或改写证书路径。

WSS 可以在受信任反向代理终止 TLS，再把 WebSocket 转发到产品配置声明的 AuthGateway 外部端口。Unity 侧仍只配置公开的 `wss://` 地址，不感知代理后的明文内部跳数。

## 游客身份与唯一会话范围

登录使用 `GuestAccountId + ClientInstanceId + ClientBuildVersion + AuthProtocolVersion`。它是求职 Demo 身份，不是密码账号、注册、数据库档案、支付身份或防盗号系统。

AuthGateway Registry 的唯一事实为：

```text
AccountId -> Session identity + ClientInstanceId + SessionGeneration + SessionToken identity
```

新登录在同一个 AuthGateway Scene 的顺序执行边界内替换旧记录、增加 generation、推送顶号消息并关闭旧 Session。旧 Session 销毁时只有在 AccountId、generation 和 Session identity 都仍匹配当前记录时才删除记录，因此不会清掉新登录。

该能力只叫“单 AuthGateway Scene 唯一 Session”。它不保证跨 Gate、跨进程或跨机房全局唯一，也没有 Redis 或数据库 fallback。SessionToken 只保存在当前 Auth Session owner 内，不写日志或诊断。

## 资源分包

唯一 `DefaultPackage` 使用三个业务标签：

| 标签 | 资源 | 时机 | Owner |
|---|---|---|---|
| `Core` | HotFix DLL、AOT Metadata、ProductShell | 进入热更前 | Global scope |
| `Gameplay` | StandaloneGameplay 与完整玩法依赖 | Home 点击开始后 | Gameplay scope |
| `OptionalHD` | 不影响正确性的主页高清主题 | Home 显式选择后 | Home/Transient scope |

Scene 保持独立打包；热更程序集因按名称单独加载而保持独立地址；共享依赖交给 YooAsset manifest 解析。业务 `PreloadPlan` 只声明页面进入 barrier，不保存 Bundle 文件名，也不复制依赖图。

## ResourceScope 与回收

`ProductResourceRuntime` 是 HotFix 业务加载入口。每次申请必须显式提供 `Global`、`Home`、`Gameplay` 或 `Transient` scope，并取得逻辑 lease。相同 package、规范化 location 和 asset type 的并发请求共享一个物理加载；每个调用方仍取得独立 lease。Prefab asset 的物理加载与每个 GameObject instance 的销毁责任分开统计。

Scope 关闭顺序为：拒绝新请求、取消该 scope 未提交的逻辑等待、销毁 live instances、释放已经提交的 lease、进入 Disposed。其它 scope 对共享物理加载的等待和引用保持有效。

普通窗口关闭只释放自己的 lease，不调用全局扫描或 `GC.Collect`。全局 unused asset 回收只发生在 Single Scene 切换完成、返回 Home 加载遮罩、显式维护或 low-memory 安全点。Gameplay 内的 `Return Home` 入口先让 `SimulationSessionHost` 停止 tick 并关闭 Actor 端口，再释放 Session runtime、Actor registration 与 Endpoint，然后销毁 Scene-owned runtime；三步成功后才释放 Gameplay scope。ProductShell 加载遮罩接管画面后执行 unused asset 回收并重新预加载 Home。任一 Session teardown 阶段失败都会保留 Gameplay scope，并允许再次尝试，禁止把仍活动 Session 依赖的资源强制清空。

## 当前实施边界

`ProductStartupProfile` 已作为唯一正式配置挂到 Bootstrap，但 `ResourceEndpoint` 和 `AuthEndpoint` 保持为空。项目不会编造部署地址；在填入真实 HTTPS/WSS 地址前，启动链会在首次网络请求前明确失败，Player 构建校验也会拒绝通过。

项目层已经建立启动策略、版本门禁、磁盘预检、启动快照、ProductShell、游客 WSS 认证、单 AuthGateway Scene 顶号、业务资源 scope、预加载计划、按标签下载和诊断链。TEngine 继续拥有唯一 YooAsset PackageMap；新增的通用初始化合同只接收文件系统参数、单一远端服务和缓存校验结果，不引用 ProductStartup、登录、页面或 Gameplay 类型。旧 Middle 默认路径、示例 Basic Auth、备用端点合同和空端点本地 URL 拼装已经从正式链删除，没有第二套 YooAsset 初始化。

Fault Lab 通过 YooAsset 的 Development-only 公开边界枚举已登记缓存，只向业务层暴露 Bundle ID 与大小，不暴露文件路径。用户必须先从当前可选集合选中一个 Bundle ID，破坏命令才会改写该记录的数据文件；下一次 High 校验会把它识别为失效缓存。Bootstrap、标签闭包和 Player Build Settings 的 Editor 校验代码已经存在，但还需要由 Unity Editor 实际执行。

## 本地玩法开发入口

Unity Editor 的唯一入口是 `Tools/3C/Launcher`。其中`单机 / Gameplay Lab`可选择 Local Float32 或 Local Fixed Variant，直接运行 `Assets/Scenes/GameplayLab/GameplayLab.unity`，不启动 CDN、Auth、Relay 或远端客户端。

工具会先校验两个显式 Variant、完整 Session 组合、正式可琳玩家 Prefab、Foot Placement composition 与 FinalIK solver；随后保存当前场景、运行所选 Variant，并在退出 Play 后恢复原场景。它不修改 Build Settings、ProductStartupProfile 或 endpoint，也不会成为正式启动失败后的离线 fallback。

当前仓库没有 Motion Matching、Pose Search 或 Pose Database。现有能力是 Motion Warp、Root Motion、动画状态管线和 Foot Placement/FinalIK，展示与文档不得把这些能力改名为 Motion Matching。

## 展示口径

启动页当前可以展示阶段、三类版本、High CRC 校验进度、有效/失效缓存文件数、磁盘预检、下载文件和字节进度、速度、ETA、重试次数、结构化错误与耗时。ProductShell 诊断展示 logical load、physical load、in-flight join、known physical reuse、active lease、live instance、scope、公开 pool/package 指标和 Unity 内存。

诊断只读。Fault Lab 只进入 Editor 与 Development Build；当前接通正式下载 generation 取消、显式缓存 Bundle 选择与破坏、同资源并发申请、非 Global scope 释放和 low-memory 边界。Fault Lab 不接收任意文件路径、不伪造成功、不替换端点、不禁用 TLS、不注入 mock Auth Server。
