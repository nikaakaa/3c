## ADDED Requirements

### Requirement: 普通产品必须只有一条从 Bootstrap 到 Gameplay 的启动链

普通产品 MUST 使用 `Bootstrap -> ProductBootstrapRunner -> GameApp.Entrance -> ProductStartupCoordinator -> ProductShell -> Authenticated Home -> Gameplay Preload -> StandaloneGameplay` 作为唯一入口。AOT Procedure MUST不在 HotFix handoff 后直接加载 Gameplay Scene，Home 和 Gameplay MUST不提供绕过认证或资源准备的第二入口。

#### Scenario: 普通 Player 冷启动

- **WHEN** 普通 Player 从 Bootstrap Scene 启动
- **THEN** ProductBootstrapRunner MUST先完成启动策略和 Core 资源阶段
- **AND** GameApp.Entrance MUST只在 Core 与热更程序集有效后运行
- **AND** StandaloneGameplay MUST只在登录成功且 Gameplay 准备完成后进入

#### Scenario: 旧直接进玩法入口仍存在

- **WHEN** Build Settings、Procedure 或 HotFix 中仍存在启动后直接加载 StandaloneGameplay 的调用
- **THEN** 产品启动配置 MUST视为无效
- **AND** 系统 MUST不把该路径保留为快速启动或失败 fallback

### Requirement: Bootstrap 展示面必须由 Player 内置闭包提供

Bootstrap Scene、启动 UI、最小字体、错误资源和 AOT 启动运行器 MUST进入 Player 内置闭包。它们 MUST不依赖远端 DefaultPackage、HotFix DLL、ProductShell 或尚未完成校验的缓存 Bundle。

#### Scenario: 首次安装且本地没有缓存

- **WHEN** Player 第一次启动且 DefaultPackage 缓存为空
- **THEN** Bootstrap UI MUST仍可显示启动阶段、下载计划和错误

### Requirement: Editor 本地玩法入口必须与产品启动入口隔离

项目 MUST保留Editor-only的正式Gameplay Lab直接运行入口，用于验证IK、步态相位匹配、Motion Warp、KCC与角色管线。该入口 MUST使用独立`Assets/Scenes/GameplayLab/GameplayLab.unity`并复用正式角色、Presentation与测试环境作者来源，MUST不替换产品`StandaloneGameplay`，MUST不修改普通Player Build Settings、ProductStartupProfile、ResourceEndpoint或AuthEndpoint，MUST不进入Release Player，也 MUST不在商业启动失败后成为离线fallback。项目没有Motion Matching、Pose Search或Pose Database时，工具与文档 MUST不把现有Motion Warp或动画状态管线标为Motion Matching。

#### Scenario: 开发者直接验证本地 IK

- **WHEN** 开发者从`Tools/3C/Launcher`的`单机 / Gameplay Lab`分组运行Gameplay Lab
- **THEN** 工具 MUST先校验场景包含正式可琳 Prefab、Foot Placement composition 与 FinalIK solver
- **AND** MUST直接运行目标场景而不经过 ProductBootstrap、资源版本同步或认证
- **AND** 退出 Play 后 MUST恢复开发者原先打开的场景

#### Scenario: 普通 Player 构建

- **WHEN** 构建普通 Release Player
- **THEN** 本地玩法菜单与直接运行 handler MUST不进入运行闭包
- **AND** 普通产品首场景 MUST仍为 Bootstrap
- **AND** UI MUST不先下载自身资源才能工作

#### Scenario: Core 缓存损坏

- **WHEN** Core 中的 ProductShell 或 HotFix Bundle 校验失败
- **THEN** Bootstrap UI MUST保持可用并显示恢复过程
- **AND** 系统 MUST不运行损坏 Bundle 中的 UI 或代码

### Requirement: 启动版本身份必须分离 Client、Resource 与 Protocol

系统 MUST分别保存 `ClientBuildVersion`、`MinimumClientBuildVersion`、`ResourcePackageVersion` 与 `AuthProtocolVersion`。ClientBuildVersion 和 AuthProtocolVersion MUST编译进 Player；MinimumClientBuildVersion MUST来自正式 StartupPolicy；ResourcePackageVersion MUST只来自 YooAsset package version 请求。系统 MUST不从文件时间、资源目录名或缺省值推断任一版本。

#### Scenario: Client 版本仍受支持

- **WHEN** ClientBuildVersion 大于或等于 StartupPolicy 的 MinimumClientBuildVersion
- **THEN** 启动 MUST继续请求 YooAsset ResourcePackageVersion
- **AND** Bootstrap diagnostics MUST分别显示 Client、Resource 和 Protocol identity

#### Scenario: Client 版本过低

- **WHEN** ClientBuildVersion 小于 MinimumClientBuildVersion
- **THEN** 启动 MUST进入 `ClientUpdateRequired` 终态
- **AND** 系统 MUST不请求 ResourcePackageVersion、不加载 HotFix 且不进入登录

#### Scenario: StartupPolicy schema 不受支持

- **WHEN** ResourceEndpoint 返回缺字段、非法或未知 schema 的 StartupPolicy
- **THEN** 启动 MUST报告结构化策略错误
- **AND** 系统 MUST不使用内置默认版本或旧 schema reader

### Requirement: 启动资源过程必须由显式可取消阶段组成

ProductBootstrapRunner MUST依次拥有 `RequestStartupPolicy`、`InitializePackageAndVerifyCache`、`RequestPackageVersion`、`UpdatePackageManifest`、`PlanCoreDownload`、`AwaitCoreDownloadConsent`、`DownloadCore`、`ClearObsoleteCache`、`LoadHotUpdateAssemblies` 与 `EnterProductRuntime`。每个阶段 MUST只有一个写入者、一个 cancellation generation 和一个结构化结果。

#### Scenario: 阶段成功推进

- **WHEN** 当前阶段以匹配 generation 成功完成
- **THEN** Runner MUST只推进到定义的下一阶段
- **AND** snapshot MUST记录阶段耗时与结果

#### Scenario: 已退出阶段的异步回调完成

- **WHEN** 旧 generation 的下载、版本或 Manifest 回调在 Retry 后完成
- **THEN** Runner MUST忽略该完成结果
- **AND** 旧回调 MUST不改变 UI、PackageVersion 或当前阶段

#### Scenario: 用户重试失败阶段

- **WHEN** 用户在可重试错误上选择 Retry
- **THEN** Runner MUST先取消并清理当前 generation
- **AND** MUST从同一正式阶段重新执行
- **AND** MUST不跳过失败步骤或切换另一实现

### Requirement: DefaultPackage 必须使用正式业务标签分包

项目 MUST只使用一个 `DefaultPackage`，并以 `Core`、`Gameplay` 与 `OptionalHD` 三个正式标签表达资源交付。Core MUST包含进入 ProductShell 所需的完整闭包；Gameplay MUST包含进入 StandaloneGameplay 所需的完整闭包；OptionalHD MUST只包含不影响登录、Home 和 Gameplay 正确性的真实可选表现资源。

#### Scenario: 启动需要 Core

- **WHEN** Core 标签存在未缓存 Bundle
- **THEN** ProductBootstrapRunner MUST只为 Core 标签创建启动 downloader
- **AND** MUST不因 Core 更新隐式下载 Gameplay 或 OptionalHD

#### Scenario: Home 点击开始游戏

- **WHEN** 用户在已认证 Home 选择开始 Gameplay
- **THEN** ProductStartupCoordinator MUST为 Gameplay 标签规划下载与预加载
- **AND** StandaloneGameplay MUST等待 Gameplay 完整准备

#### Scenario: OptionalHD 没有真实可选资产

- **WHEN** Asset Collector 中没有不影响正确性的真实 OptionalHD 资源
- **THEN** 资源配置 MUST视为未完成
- **AND** MUST不创建空标签、重复 Gameplay 资产或占位 Bundle 伪装分包

### Requirement: 缓存初始化必须执行 High 完整性校验

DefaultPackage 缓存文件系统 MUST使用 High 校验级别检查记录的文件大小与 CRC。损坏记录 MUST从有效缓存集合移除并进入缺失下载计划。Bootstrap MUST将校验阶段、耗时、有效文件数与失效文件数作为只读诊断公开。

#### Scenario: 缓存 Bundle CRC 不匹配

- **WHEN** 缓存文件大小合法但 CRC 与 Manifest 记录不匹配
- **THEN** 该文件 MUST不作为有效缓存使用
- **AND** Core 或 Gameplay downloader MUST只重新取得受影响的缺失文件及必要依赖

#### Scenario: CRC 校验成功

- **WHEN** 缓存文件大小和 CRC 均与记录匹配
- **THEN** downloader MUST将该文件视为已缓存
- **AND** diagnostics MUST不把缓存命中计为网络下载

### Requirement: 断点续传必须使用 YooAsset 原生临时文件与 HTTP Range

项目 MUST配置明确且有限的 ResumeDownloadMinimumSize 和正式响应码集合。ResourceEndpoint MUST通过 HTTPS 正确支持 Range。项目 MUST不创建第二种临时文件、下载器或文件合并格式。

#### Scenario: 大文件下载中断后重新启动

- **WHEN** 大于断点阈值的 Bundle 已下载部分字节且进程退出
- **THEN** 下次相同版本下载 MUST从 YooAsset 合法临时文件的已完成偏移继续
- **AND** 完成后 MUST执行 High 校验再激活缓存记录

#### Scenario: 服务器不接受 Range

- **WHEN** ResourceEndpoint 对续传请求返回不受支持或不一致的 Range 结果
- **THEN** downloader MUST按正式 YooAsset 错误语义失败
- **AND** UI MUST报告服务器续传能力错误
- **AND** 系统 MUST不自行拼接响应或静默声明续传成功

### Requirement: 下载开始前必须完成容量规划和用户确认

Core、Gameplay 与 OptionalHD downloader 在 BeginDownload 前 MUST公开文件数、总字节数与剩余字节数，并检查可用磁盘空间是否覆盖剩余下载、临时文件增长和正式安全余量。存在网络下载时 MUST进入明确确认阶段；空间不足 MUST不开始下载。

#### Scenario: Core 有远端文件且空间足够

- **WHEN** PlanCoreDownload 得到非零下载量且可用空间满足预算
- **THEN** Bootstrap MUST显示文件数和字节数并等待确认
- **AND** 用户确认后 MUST启动同一个已规划 downloader generation

#### Scenario: 磁盘空间不足

- **WHEN** 可用空间低于正式下载容量预算
- **THEN** 阶段 MUST返回 `InsufficientDiskSpace`
- **AND** downloader MUST不开始任何新文件

### Requirement: 下载状态必须提供真实进度与结构化失败

下载阶段 MUST显示总文件数、已完成文件数、总字节、已完成字节、当前速度、估算剩余时间、当前文件、重试次数和结构化错误。UI MUST消费 runner snapshot，MUST不直接修改 downloader callback 或 PackageVersion。

#### Scenario: 下载文件失败

- **WHEN** downloader 报告文件名与错误信息
- **THEN** snapshot MUST记录失败文件、错误类别和本 generation 重试次数
- **AND** UI MUST提供同阶段 Retry 或 Exit
- **AND** MUST不直接进入 LoadHotUpdateAssemblies

#### Scenario: Core 已全部缓存

- **WHEN** Core downloader 的 TotalDownloadCount 为零
- **THEN** 阶段 MUST记录零网络下载和缓存命中
- **AND** MUST继续执行旧缓存清理与热更加载

### Requirement: 热更程序集必须在 Core 原子完成后加载

HotUpdateAssemblyLoader MUST只在 StartupPolicy、缓存初始化、ResourcePackageVersion、Manifest、Core 下载和清理全部成功后运行。程序集清单、AOT Metadata、HotFix DLL 与 LogicMain 类型任一缺失或不匹配时 MUST停留在 Bootstrap 错误状态，MUST不调用 GameApp.Entrance。

#### Scenario: GameLogic DLL 缺失

- **WHEN** Core Manifest 激活但 LogicMainDllName 对应资源无法加载
- **THEN** 启动 MUST报告 `HotUpdateAssemblyMissing`
- **AND** ProductStartupCoordinator MUST不创建

#### Scenario: 热更程序集完整

- **WHEN** 所有配置程序集与 AOT Metadata 成功加载且 GameApp.Entrance 存在
- **THEN** Procedure MUST进入无业务分支的 ProductRuntime 终态
- **AND** MUST只调用一次 GameApp.Entrance

### Requirement: ResourceEndpoint 必须是唯一正式 HTTPS 来源

StartupPolicy、YooAsset version、Manifest 与 Bundle MUST来自同一个显式 ResourceEndpoint 基地址。Release 配置 MUST要求 HTTPS，备用 URL 与失败自动切换 MUST不存在。端点为空、非 HTTPS 或包含旧示例路径时 MUST在发起请求前失败。

#### Scenario: 正式资源端点缺失

- **WHEN** HostPlayMode 或 WebPlayMode 启动且 ResourceEndpoint 为空
- **THEN** Bootstrap MUST报告 `ResourceEndpointNotConfigured`
- **AND** 系统 MUST不退回 EditorSimulateMode、StreamingAssets 全量资源或本地测试 URL

#### Scenario: 主端点请求失败

- **WHEN** 唯一 ResourceEndpoint 超时或返回失败
- **THEN** 当前阶段 MUST进入结构化错误
- **AND** Retry MUST继续请求同一正式端点

### Requirement: ProductShell 必须承载登录、Home 与 Gameplay 进入

Core MUST包含唯一 ProductShell Scene。GameApp.Entrance 创建的 ProductStartupCoordinator MUST加载 ProductShell，先显示登录，再在认证成功和 Home preload 完成后显示 Home。Home MUST提供 Gameplay 下载/预加载入口和只读诊断，但 MUST不复制 StandaloneGameplay 的 Gameplay runtime。

#### Scenario: ProductShell 加载成功但尚未登录

- **WHEN** ProductShell Scene 已加载且 Auth Session 尚未认证
- **THEN** 系统 MUST只显示登录与启动诊断
- **AND** Home 和 Gameplay 入口 MUST不可用

#### Scenario: Gameplay 准备完成

- **WHEN** 认证有效、Gameplay 标签完整且 Gameplay PreloadPlan 成功提交
- **THEN** ProductStartupCoordinator MUST加载现有 StandaloneGameplay Scene
- **AND** Scene 内现有 SimulationSessionHost 主链 MUST保持不变

### Requirement: 启动诊断必须只读且区分安全含义

启动诊断 MUST显示阶段、三类版本、ResourceEndpoint 脱敏主机、校验级别、下载续传状态、资源标签、HotFix 程序集状态和阶段耗时。UI MUST明确区分 CRC 完整性、HTTPS 传输安全与未实现的 Manifest 发布者签名，MUST不把 TEngine 文件混淆显示为通信加密。

#### Scenario: 查看资源安全状态

- **WHEN** 用户打开启动诊断
- **THEN** UI MUST分别显示 `CRC High` 与 `HTTPS`
- **AND** Manifest signature MUST显示为不在本变更范围
- **AND** TEngine encryptionType MUST不作为网络 TLS 状态
