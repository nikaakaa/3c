# Change: 收口客户端构建产物目录与正式构建入口

## Why

客户端 Unity 工程根目录目前同时存在 `Build`、`Builds`、`Bundles` 与 `HybridCLRData`。这些目录分别来自 Unity Player、TEngine、YooAsset 和 HybridCLR，但名字没有表达“正式交付产物”还是“本机可删除缓存”。更严重的是，TEngine 通用构建默认写 `Builds`，Windows 一键构建写 `Builds/Windows`，Android/iOS 一键构建写 `Bundles`，Player 又写 `Build`；YooAsset EditorSimulate 也把模拟清单写入 `Bundles`。项目没有唯一构建产物合同。

当前 `.gitignore` 和 Repository Policy 只完整覆盖 `Build/Builds`，没有把 `Bundles` 与 `HybridCLRData` 作为客户端生成根处理，导致 YooAsset 模拟清单已经进入 Git。商业启动变更已经建立 ResourceEndpoint、资源版本和启动策略语义，但还没有一个正式构建入口产出可直接交给本地 HTTPS 资源服务或远端 CDN 的精确资源闭包。

本变更把 `Build` 收敛为客户端唯一正式产物根，把 YooAsset 模拟与低层工具输出迁入 `Library`，并建立项目拥有的商业客户端构建入口。TEngine 继续负责热更 DLL、YooAsset 和 Player 的底层构建执行，但不再决定项目的产品目录、版本身份或发布闭包。

## What Changes

- 固定客户端正式产物根为 `Build`，只使用 `Build/Content`、`Build/Players` 与现有 `Build/Network` 三个一级业务分区。
- 新增项目层 `CommercialClientBuildWorkflow` 和唯一用户入口；它从正式配置读取 ClientBuildVersion，要求显式 ResourcePackageVersion 与 MinimumClientBuildVersion，并调用 TEngine Editor 构建服务。
- 将 YooAsset 原始构建、增量缓存和临时 staging 放在 `Build/.Workspace`，成功后验证精确闭包并原子发布到 `Build/Content/<BuildTarget>/DefaultPackage/<ResourcePackageVersion>`。
- 让正式 Content 版本目录包含可直接服务的 `StartupPolicy.json`、YooAsset version/manifest/Bundle 文件和产品发布 manifest；不得包含 `OutputCache`、BuildReport 或模拟清单。
- 将普通 Player 固定发布到 `Build/Players/<BuildTarget>/<ClientBuildVersion>`，并记录所配套的内置资源与客户端版本身份；不再使用无版本 `Build/Windows/Release_Windows.exe`。
- 保留 `Build/Network/UnityAuthority`、`Build/Network/DotRecastAuthority`、`Build/Network/DeterministicRollback` 与 `Build/Network/RunLogs` 的现行合同，只让它们通过统一路径定义取得 Network 根。
- 将 YooAsset 默认 Builder 与 EditorSimulate 输出迁到 `Library/YooAsset/BuildOutput`；删除根目录 `Bundles`，不保留兼容读取或镜像。
- 删除 TEngine 中按平台分裂的 `Builds/Bundles` 默认值、时间推断版本、可写正式目录的旧一键菜单和未使用的 `BuildAddress` 配置。
- 将 `Build`、旧 `Builds`、旧 `Bundles` 与 `HybridCLRData` 全部纳入 Git ignore 和 Repository Policy；移除已跟踪的模拟清单。
- 删除旧 `Builds`、`Bundles`、无版本 `Build/Client` 与 `Build/Client_ServerAu` 生成目录；正式产物不迁移旧文件，必须由唯一新工作流重新构建。
- 更新商业启动部署文档，明确本地 HTTPS 服务直接选择一个完整 Content 版本目录作为根，不在 Unity 工程内复制第二份 `CDN` 或 `Remote` 目录。

## Capability Boundaries

### 本变更包含

- 客户端正式 Content、Player 与 Network 产物的目录合同。
- 商业客户端 Content/Player 的项目级正式构建入口。
- Content 版本的精确发布闭包、显式版本身份、staging 和原子发布。
- YooAsset Editor 模拟/低层输出与正式发布输出的物理隔离。
- 生成目录的 Git ignore 与 Repository Policy。

### 本变更不包含

- 自动上传 CDN、云端 CD、对象存储账号、域名或证书签发。
- 修改 Runtime ResourceEndpoint、YooAsset 下载器、断点续传或缓存目录。
- 修改三个 Network Test Product 的产品组成、manifest schema、Build/Run 语义或目录名称。
- 修改 HybridCLR 的程序集列表、热更 ABI 或运行时加载链。
- 把 Unity `Library`、`Temp`、`Logs` 等引擎固定目录重新包装成自定义工作区。
- 迁移服务端自身的 publish 根；本变更只管理 Unity 客户端工程内的产物编排。

## Impact

- 新增 capability：`client-build-artifact-layout`。
- 扩展 capability：`tengine-hotupdate-foundation`，把 TEngine Editor 构建能力收敛为显式请求/结果的底层服务。
- 修改 capability：`repository-ci-foundation`，补齐客户端生成根的 Git 跟踪禁令。
- 客户端 Editor：新增 ProductBuild 目录、商业客户端构建工作流、正式发布 manifest 与路径合同。
- TEngine Editor：重构 `BuildConfig`、`ReleaseTools` 和旧构建菜单，不再保存项目正式路径和时间版本默认值。
- YooAsset Editor：默认 Builder/EditorSimulate 输出根从项目 `Bundles` 迁入 `Library/YooAsset/BuildOutput`。
- 配置与清理：`.gitignore`、Repository Policy、`TEngineUpdateSettings.asset`、旧构建产物目录和已跟踪模拟清单。
- 文档：商业启动资源端点部署说明和 `openspec/project.md` 客户端产物口径。

## Comparison With Current Specs

- current `network-test-runtime-product-boundary` 要求三个 Network Test Product 使用互不覆盖的固定目录并由正式 manifest 驱动 Build/Run。本变更保留 `Build/Network/<Product>` 和现有原子替换语义，不把网络产品拆进普通 `Players`，也不修改 schema v2。
- current `repository-ci-foundation` 已要求仓库策略拒绝客户端 `Build/Builds` 和通用生成目录，但没有覆盖 `Bundles` 与 `HybridCLRData`；当前已有四个 YooAsset 模拟清单被跟踪。该 requirement 必须同步扩展，旧模拟文件必须从索引删除。
- current `tengine-hotupdate-foundation` 要求项目业务不得进入 `Packages/com.alex.tengine`。本变更保持该边界：TEngine 只提供无 ProductStartup 类型的显式构建服务，产品版本、目录和发布闭包由 `ThirdPersonClient.Editor` 拥有。
- active `add-commercial-client-startup-showcase` 正在修改 TEngine Runtime 初始化合同，并新增 ResourceEndpoint、StartupPolicy 和 ResourcePackageVersion 语义。本变更不修改其运行时状态机，只补充这些文件的正式生成与本地服务来源；两项对 `tengine-hotupdate-foundation` 的 delta 分别修改 Runtime 初始化与新增 Editor 构建服务要求，不建立第二资源包。
- active `add-commercial-client-startup-showcase` 明确 ClientBuildVersion、MinimumClientBuildVersion 和 ResourcePackageVersion 不能互相替代。本变更分别使用它们命名 Player、生成 StartupPolicy 和命名 Content 版本，删除当前按日期分钟自动生成资源版本的旧行为。
- `openspec/project.md` 已把 `Build/Network` 定义为三个网络产品的正式根；本变更只新增 `Build/Content` 与 `Build/Players` 并把该三分区写入当前项目口径。
- current `repository-ci-foundation` 明确当前没有 Unity 云端构建或 CD。本变更只是本地 Unity Editor 正式构建工作流，不修改 GitHub Actions，不声称 CI 产出 Player 或发布 CDN。

