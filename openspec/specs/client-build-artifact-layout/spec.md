# client-build-artifact-layout Specification

## Purpose
定义商业客户端 Content、Player、Workspace 与 Network 构建产物的唯一版本化目录和发布边界。
## Requirements
### Requirement: 客户端正式产物必须收敛到唯一 Build 根

客户端 Unity 工程 MUST只使用 `Build` 作为正式产物根，并将正式产物分为 `Build/Content`、`Build/Players` 与 `Build/Network`。`Builds`、`Bundles`、`Library`、`HybridCLRData` 和 `Build/.Workspace` MUST不被解释为正式 Content、Player、Network Product 或 CDN 上传源。三个正式分区 MUST互不写入对方目录。

#### Scenario: 查看客户端工程根目录

- **WHEN** 开发者区分项目输入、本机缓存和正式产物
- **THEN** Content、普通 Player 与 Network Test Product MUST分别只出现在三个正式分区
- **AND** `Builds` 与 `Bundles` MUST不存在
- **AND** Library、HybridCLRData 与 Workspace MUST只被视为可重建的本机数据

#### Scenario: 输出路径逃逸

- **WHEN** 任一构建请求的规范化目标离开客户端 `Build` 根或进入另一正式分区
- **THEN** 构建 MUST在写文件前失败
- **AND** MUST不修正到默认路径或改写另一产品目录

### Requirement: 商业客户端必须使用项目拥有的唯一正式构建入口

普通商业客户端 Content 与 Player MUST由 `ThirdPersonClient.Editor` 拥有的 `CommercialClientBuildWorkflow` 构建。该工作流 MUST从正式 ProductStartupProfile 读取 ClientBuildVersion，MUST要求调用方显式提供 ResourcePackageVersion 与 MinimumClientBuildVersion，并 MUST通过无项目业务语义的 TEngine Editor 构建服务执行 HotFix、YooAsset 与 Player 底层步骤。通用 TEngine/YooAsset 菜单 MUST不直接写入 `Build/Content` 或 `Build/Players`。

#### Scenario: 构建普通商业客户端完整版本

- **WHEN** 开发者从唯一商业客户端构建入口执行 Content 后 Player 构建
- **THEN** 工作流 MUST先校验三类显式版本和固定目标路径
- **AND** MUST通过同一 TEngine 服务完成热更 DLL、YooAsset 与 Player 步骤
- **AND** 成功产物 MUST只发布到 Content 与 Players 正式分区

#### Scenario: 版本依赖默认时间

- **WHEN** ResourcePackageVersion 或 MinimumClientBuildVersion 没有显式提供
- **THEN** 构建 MUST在开始前失败
- **AND** MUST不从当前日期、分钟、目录名、文件时间或 EditorPrefs 生成版本

### Requirement: Content 必须由 Workspace 原子发布为精确版本闭包

商业 Content 原始构建、YooAsset `OutputCache`、BuildReport 与 transient candidate MUST只位于 `Build/.Workspace`。成功候选 MUST按 `Build/Content/<BuildTarget>/DefaultPackage/<ResourcePackageVersion>` 发布，并且 MUST精确包含 `StartupPolicy.json`、YooAsset runtime version/manifest/Bundle 文件和 `CommercialContentReleaseManifest` 声明的文件。正式版本 MUST不包含 OutputCache、BuildReport、EditorSimulate 文件或未声明文件。

#### Scenario: Content 构建全部成功

- **WHEN** HotFix DLL、YooAsset build、StartupPolicy 生成、hash 与 exact closure 校验全部成功
- **THEN** 工作流 MUST原子发布完整 ResourcePackageVersion 目录
- **AND** 本地资源服务 MUST可以直接把该目录作为唯一 ResourceEndpoint document root

#### Scenario: Content 候选校验失败

- **WHEN** 候选缺少 manifest 引用的 Bundle、包含未声明文件或 hash 不匹配
- **THEN** 工作流 MUST不创建正式 ResourcePackageVersion 目录
- **AND** MUST不修改任何既有正式 Content 版本

#### Scenario: 相同资源版本已经存在

- **WHEN** 正式 Content 目录已经存在相同 ResourcePackageVersion
- **THEN** 新构建 MUST明确失败
- **AND** MUST不覆盖、合并或按文件时间判断两次构建等价

### Requirement: Player 必须按 ClientBuildVersion 发布完整闭包

普通 Player MUST先在 `Build/.Workspace` 构建，并在 executable、Data 目录、版本身份和 exact closure 校验成功后原子发布到 `Build/Players/<BuildTarget>/<ClientBuildVersion>`。Player release manifest MUST记录 ClientBuildVersion、BuildTarget、配套内置资源身份和全部正式文件 hash。

#### Scenario: Player 构建成功

- **WHEN** Unity Player build 与 release manifest 校验全部成功
- **THEN** 正式 Player 版本目录 MUST包含可执行闭包和匹配 manifest
- **AND** MUST不使用无版本 `Build/Windows/Release_Windows.exe` 或 `Build/Client`

#### Scenario: 相同客户端版本已经存在

- **WHEN** 正式 Players 目录已经存在相同 BuildTarget 与 ClientBuildVersion
- **THEN** 新构建 MUST明确失败
- **AND** MUST不覆盖已发布 Player

### Requirement: YooAsset 模拟和默认 Builder 输出必须进入 Library

YooAsset Editor 默认 BuildOutputRoot MUST为 `Library/YooAsset/BuildOutput`。EditorSimulate 的 `Simulate-*` 和直接使用 YooAsset 原始 Builder 产生的低层结果 MUST只进入该本机目录。只有商业客户端正式工作流可以通过显式 request 写入自己的 Workspace，并在发布校验后写入 `Build/Content`。

#### Scenario: EditorSimulate 生成资源清单

- **WHEN** Unity Editor 为 DefaultPackage 执行模拟构建
- **THEN** 模拟 manifest MUST写入 `Library/YooAsset/BuildOutput`
- **AND** 项目根 MUST不创建 `Bundles`
- **AND** 模拟结果 MUST不进入 Git 或正式 Content

#### Scenario: 直接打开 YooAsset 原始 Builder

- **WHEN** 开发者使用低层 YooAsset Builder 而不是商业客户端正式入口
- **THEN** 默认输出 MUST留在 Library
- **AND** MUST不产生可被误认为正式发布版本的 `Build/Content` 目录

### Requirement: Network Test Product 必须保留现行 Build/Network 合同

三个 Network Test Product MUST继续使用 `Build/Network/UnityAuthority`、`Build/Network/DotRecastAuthority` 与 `Build/Network/DeterministicRollback`，Server 日志 MUST继续使用 `Build/Network/RunLogs/<Model>/<RunId>`。Network workflow MUST从公共 ClientBuildArtifactLayout 取得 NetworkRoot，但 MUST不改变 schema v2、产品闭包、staging、原子替换或 Run 语义。

#### Scenario: 构建任一 Network Test Product

- **WHEN** 现有 Network Product adapter 执行 Build
- **THEN** 产物 MUST继续写入原有 ProductRoot
- **AND** MUST不写入普通 Content 或 Players 分区
- **AND** 普通商业构建 MUST不修改该 Network Product

### Requirement: 本地资源服务必须消费一个正式 Content 版本目录

本地 HTTPS 资源服务 MUST明确选择一个已通过 release manifest 校验的 Content 版本目录作为 document root。项目 MUST不在 Unity 工程内创建 `CDN`、`Remote`、`ServerData` 或其它 Content 镜像目录，也 MUST不服务 Workspace、OutputCache、BuildReport、Library 或 HybridCLRData。

#### Scenario: 本地运行商业启动链

- **WHEN** 开发者配置本地 HTTPS 服务验证 StartupPolicy、版本请求、Manifest、Bundle 与 Range
- **THEN** 服务根 MUST指向一个完整正式 Content 版本目录
- **AND** 客户端 MUST仍只使用 ProductStartupProfile 中的唯一 ResourceEndpoint
- **AND** 切换版本 MUST通过显式服务器映射完成而不是复制第二份资源树

### Requirement: 旧构建根和无版本产物必须被彻底删除

迁移完成后项目 MUST删除旧 `Builds`、旧 `Bundles`、无版本 `Build/Client`、无版本 `Build/Client_ServerAu`、TEngine `BuildAddress` 和全部按平台分裂的旧默认输出。系统 MUST不提供旧目录扫描、自动迁移、软链接、镜像、兼容读取或 fallback。

#### Scenario: 旧目录仍然存在

- **WHEN** 构建代码、配置、文档或被跟踪文件仍引用 `Builds`、根 `Bundles` 或无版本普通 Player 路径
- **THEN** 迁移 MUST视为未完成
- **AND** 正式构建工具 MUST不把旧文件搬入新目录继续使用

