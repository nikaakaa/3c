# Change: 增加商业客户端启动展示链

## Why

项目已经接入 TEngine、YooAsset、HybridCLR 与 Fantasy，但普通产品入口仍从 Bootstrap 直接进入 `StandaloneGameplay`。当前实现把资源包初始化、版本请求、Manifest 更新和整包下载压在少量 Procedure 中，在线配置为空、资源没有标签、启动缓存仍使用默认中等级校验、断点续传阈值没有开启；登录、唯一会话、主页和资源生命周期证据均不存在。底层库“支持”这些能力，不等于当前产品已经形成可观察、可失败、可恢复的商业启动链。

本变更把普通求职 Demo 的唯一产品入口收敛为一条真实纵切：内置 Bootstrap 完成版本兼容、资源校验与 Core 更新，HybridCLR 入口接管安全登录和主页，主页再按业务需要下载 Gameplay 分包并进入现有 `StandaloneGameplay`。展示只证明本项目真实完成的边界，不把 CRC 描述成防篡改，不把资源混淆描述成通信加密，也不把单 AuthGateway 的唯一会话描述成分布式账号系统。

## What Changes

- 将普通产品启动链改为 `Bootstrap -> 显式资源更新状态 -> HybridCLR GameApp -> ProductShell -> WSS 游客登录 -> Home -> Gameplay 下载/预加载 -> StandaloneGameplay`，删除 `ProcedureStartGame` 直接加载玩法场景的旧路径。
- 在 AOT Main 层建立唯一可取消启动运行器和只读状态快照，拆分启动策略请求、资源包初始化与缓存校验、资源版本请求、Manifest 更新、Core 下载规划、下载确认、下载、旧缓存清理、热更程序集加载和产品入口交接。
- 让 Bootstrap UI 作为 Player 内置资源常驻于启动场景；它显示阶段、版本、校验、文件数、字节数、速度、剩余时间、重试次数、错误码和耗时，不依赖尚未下载的热更资源。
- 在唯一 `DefaultPackage` 内使用 `Core`、`Gameplay` 与 `OptionalHD` 标签表达业务分包；Core 在热更入口前完成，Gameplay 在主页点击开始后完成，OptionalHD 只承载真实可选表现资源。
- 将 YooAsset 缓存校验正式配置为 High CRC，启用明确最小尺寸的断点续传并要求正式 HTTPS ResourceEndpoint 支持 Range；下载前执行磁盘空间预检，失败后停留在当前阶段并通过同一状态重试，不切换备用 URL、离线资源路径或第二套下载器。
- 增加版本化 StartupPolicy。Player 内置 ClientBuildVersion 与 AuthProtocolVersion；ResourceEndpoint 提供 MinimumClientBuildVersion；YooAsset 仍是 ResourcePackageVersion 的唯一来源。三类版本分别展示和校验，不互相替代。
- 在热更 GameLogic 层增加唯一 `ProductStartupCoordinator`，加载 Core 中的 ProductShell，持有认证状态并在登录成功后进入 Home；TEngine Procedure 在 `ProcedureProductRuntime` 停留，不再决定登录、主页或玩法业务。
- 增加独立 `ThirdPerson.Startup.Server` Fantasy 产品和单一 AuthGateway Scene。客户端固定通过 WSS 登录，服务端提供最小游客身份、SessionToken、单 AuthGateway 内的 AccountId 唯一 Session Generation、旧会话顶号推送和条件化断开清理。
- 将 Fantasy 初始化与具体 Session 所有权分离：认证 Session 由产品认证模块持有，ServerAuthoritative Gameplay 控制 Session 继续由对应 Network Model 模块持有；两者不得通过一个全局 SessionFacade 相互断开。
- 在项目业务层增加 ResourceScope/Lease 生命周期，并对 `Packages/com.alex.tengine` 的 ResourceModule 做窄公共初始化扩展。正式 scope 为 Global、Home、Gameplay 与 Transient；业务预加载显式声明顺序，底层 Bundle 依赖继续由 YooAsset 解析，业务状态和流程不得迁入 TEngine 包。
- 保留 Editor-only 本地玩法开发入口，可直接运行当前正式 `Assets/Scenes/Sandbox/SandBox.unity` 验证 IK、Motion Warp 和角色管线；该入口不进入 Player、不修改普通 Build Settings，也不作为资源或认证失败后的产品 fallback。
- 增加只读启动、资源和内存诊断，展示 logical load、physical load、in-flight join、cache hit、active lease、scope、TEngine pool、YooAsset package、Unity memory 与阶段耗时；仅在 Editor/Development Build 提供取消下载、破坏缓存文件、同资源并发加载和释放 scope 的正式 Fault Lab。
- 更新 `openspec/project.md` 中普通产品场景链和服务端代码组织；保持全部 Network Test Product、ServerAuthoritative KCP 控制面、UDP Gameplay 数据面、DeterministicRollback 和 Gameplay Session 语义不变。

## Capability Boundaries

### 本变更包含

- 普通产品从启动到主页、再进入现有 StandaloneGameplay 的唯一正式入口。
- 可真实观察的版本、校验、分包、断点续传、登录、唯一会话、加载合并、卸载和内存证据。
- 单 AuthGateway Scene 内的游客唯一在线与 WSS 认证连接。
- 一个正式资源端点、一个正式认证端点和明确失败状态。

### 本变更不包含

- 注册、密码、验证码、账号找回、数据库账号档案或支付身份。
- Redis/数据库支持的跨 Gate、跨机房全局唯一在线。
- 匹配、动态 Room、完整断线续局、反作弊或把登录 Session 当作 Gameplay Session。
- 修改现有 ServerAuthoritative KCP/UDP 或 DeterministicRollback 网络模型。
- 自研网络密码协议、KCP 加密、资源文件加密或 Manifest 数字签名。
- 多 YooAsset Package、Addressables、YooAsset 3.x 迁移、备用 CDN、离线 fallback 或旧资源链兼容。
- 新增 Motion Matching、Pose Search 或 Pose Database；仓库当前只有 Motion Warp、Root Motion、动画状态与 Foot Placement/FinalIK 管线。
- 完整主页产品功能；Home 只承载启动结果、资源诊断、可选资源和进入现有 Gameplay 的入口。

## Impact

- 修改 current capability：`tengine-hotupdate-foundation`、`fantasy-unity-authoritative-session`。
- 新增 capability：`client-startup-resource-delivery`、`client-authenticated-entry-session`、`client-resource-lifecycle-observability`。
- 客户端 AOT：`Assets/GameScripts/Main/Procedure`、TEngine/YooAsset 正式配置、Bootstrap Scene/UI、启动状态与资源初始化 adapter。
- 客户端 HotFix：`Assets/GameScripts/HotFix/GameLogic` 中的 ProductStartup、认证、Home、Gameplay 进入和 ResourceScope。
- 资源资产：`AssetBundleCollectorSetting.asset`、ProductShell/Home UI、真实 OptionalHD 资源、DefaultPackage 标签。
- 协议：`3cDemo/Tools/NetworkProtocol/Outer/OuterMessage.proto` 与正式导出生成物。
- 服务端：新的 Startup Auth Entity/Hotfix 模块、AuthGateway Fantasy.config、`ThirdPerson.Startup.Server` 产品及其发布入口。
- 清理：删除 `ProcedureStartGame` 直接进玩法、全局唯一 `FantasyClientBootstrap.SessionFacade` 所有权、空备用资源地址和无标签收集配置。

## Comparison With Current Specs

- current `tengine-hotupdate-foundation` 已要求 TEngine 只做启动/资源/热更底座、业务代码不得写入包、资源使用唯一正式端点。本变更保持业务代码在项目目录，只扩展 TEngine ResourceModule 的通用初始化合同，使唯一端点、High 校验、续传和下载参数可以进入 YooAsset；同时删除旧 `ResDownLoadPath`、`FallbackResDownLoadPath`、示例 Basic Auth、重复版本/Manifest 请求和无标签资源配置，不在包内增加 ProductStartup 业务类型。
- current `tengine-hotupdate-foundation` 规定 `ProcedureLoadAssembly` 调用 `GameApp.Entrance()`，本变更继续保留；但当前 `ProcedureStartGame` 同时直接加载 `StandaloneGameplay`，使热更入口和 AOT Procedure 并行决定产品启动。本变更将其替换为无业务分支的 `ProcedureProductRuntime`，由唯一 HotFix `ProductStartupCoordinator` 决定 ProductShell、登录、Home 和 Gameplay 进入。
- `openspec/project.md` 当前把普通产品链写为 `Bootstrap -> ProcedureStartGame -> StandaloneGameplay`。该描述在本变更实施时必须修改为新的唯一产品链，不能保留旧入口作为快速启动选项。
- current `fantasy-unity-authoritative-session` 规定 Fantasy KCP 只承载 ServerAuthoritative Gameplay 控制面，UDP 承载高频数据。本变更不改变该要求；新增 WSS 仅属于进入 Gameplay 前的 Auth Session，并通过独立 Session owner 与 Network Model 控制连接隔离。
- current `network-test-runtime-product-boundary` 只管理三个 Network Test Product。本变更的 Startup Server 不是 Network Model Product，不进入该 catalog、不修改 schema v2，也不复用其 Build/Run 分支。
- active `add-corin-targeted-motion-warp-demo` 要求 Bootstrap 与 Standalone Scene 不产生第二启动路径。本变更与其目标一致：StandaloneGameplay 仍是唯一普通 Gameplay Scene，只把它从“启动后立即进入”改为“Home 下载并预加载 Gameplay 后进入”。若两项实施同时修改 Bootstrap、Build Settings 或 Standalone 资产，必须以最新正式资产重基线，不能保留两套入口。
- 其它 active authoring、Timeline、Presentation 和 AI changes 不拥有启动、认证或资源交付语义，本变更不修改其 Program、Session、WorldSolver 或 Presentation 主链。
