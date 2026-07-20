# Tasks

## 1. 正式启动合同与配置

- [x] 1.1 新建 AOT ProductStartup 公共目录与程序集归属。
- [x] 1.2 定义稳定 `ProductStartupStage` 值。
- [x] 1.3 定义稳定 `ProductStartupErrorCode` 值。
- [x] 1.4 定义 `ClientBuildVersion` 值对象。
- [x] 1.5 定义 `AuthProtocolVersion` 值对象。
- [x] 1.6 定义版本化 `StartupPolicy` schema。
- [x] 1.7 为 StartupPolicy 增加 schema version 校验。
- [x] 1.8 为 StartupPolicy 增加 MinimumClientBuildVersion 校验。
- [x] 1.9 为 StartupPolicy 增加未知字段与缺失字段错误结果。
- [x] 1.10 定义唯一 `ProductStartupProfile` 正式配置资产。
- [ ] 1.11 在 Profile 中保存唯一 HTTPS ResourceEndpoint。
- [ ] 1.12 在 Profile 中保存唯一 WSS AuthEndpoint。
- [x] 1.13 在 Profile 中保存正式 ClientBuildVersion。
- [x] 1.14 在 Profile 中保存正式 AuthProtocolVersion。
- [x] 1.15 在 Profile 中保存下载并发、重试和超时参数。
- [x] 1.16 在 Profile 中保存断点续传最小尺寸和响应码集合。
- [x] 1.17 在 Profile 中保存磁盘空间安全余量。
- [x] 1.18 在 Profile 中保存正式平台内存预算引用。
- [x] 1.19 增加 Profile 缺失字段的 fail-fast 校验。
- [x] 1.20 拒绝非 HTTPS ResourceEndpoint。
- [x] 1.21 拒绝非 WSS AuthEndpoint。
- [x] 1.22 拒绝备用资源 URL 和备用认证 URL 字段。
- [x] 1.23 从 TEngine 更新配置删除备用资源 URL 的运行时读取。
- [x] 1.24 将正式 ProductStartupProfile 绑定到 Bootstrap 唯一配置入口。

## 2. AOT 启动状态与只读快照

- [x] 2.1 定义 immutable `ProductStartupSnapshot`。
- [x] 2.2 在 snapshot 中加入 stage 和 generation。
- [x] 2.3 在 snapshot 中加入 Client/Resource/Protocol 版本。
- [x] 2.4 在 snapshot 中加入文件总数和完成数。
- [x] 2.5 在 snapshot 中加入总字节和完成字节。
- [x] 2.6 在 snapshot 中加入当前文件与下载速度。
- [x] 2.7 在 snapshot 中加入预计剩余时间。
- [x] 2.8 在 snapshot 中加入阶段开始时间和耗时。
- [x] 2.9 在 snapshot 中加入重试次数。
- [x] 2.10 在 snapshot 中加入结构化错误码和安全错误文本。
- [x] 2.11 定义只读 `IProductStartupSnapshotSource`。
- [x] 2.12 定义唯一 `ProductStartupSnapshotStore` 写入实现。
- [x] 2.13 让 SnapshotStore 保留有界阶段历史。
- [x] 2.14 禁止 UI 取得 SnapshotStore 写接口。
- [x] 2.15 定义 `ProductStartupStageResult`。
- [x] 2.16 定义 `ProductStartupHandoff`。
- [x] 2.17 在 handoff 中保存 package、resource version 与程序集结果。
- [x] 2.18 禁止 handoff 保存 UI 对象和可写 downloader。

## 3. ProductBootstrapRunner 单链路

- [x] 3.1 新建唯一 `ProductBootstrapRunner`。
- [x] 3.2 让 Runner 拥有当前 cancellation generation。
- [x] 3.3 让 Runner 拥有唯一阶段推进表。
- [x] 3.4 让 Runner 拒绝重复 Start。
- [x] 3.5 让每次 Retry 增加 generation。
- [x] 3.6 让 Retry 先取消旧 generation。
- [x] 3.7 让 Retry 等待旧阶段清理完成。
- [x] 3.8 让旧 generation 回调无法写 snapshot。
- [x] 3.9 让旧 generation 回调无法改变 PackageVersion。
- [x] 3.10 让 Runner 统一捕获异步异常并降低为错误结果。
- [x] 3.11 让 Runner 在退出 Bootstrap 时取消活动阶段。
- [x] 3.12 实现 `RequestStartupPolicy` 阶段。
- [x] 3.13 使用唯一 ResourceEndpoint 请求 StartupPolicy。
- [x] 3.14 解析并校验 StartupPolicy schema。
- [x] 3.15 比较 ClientBuildVersion 与 MinimumClientBuildVersion。
- [x] 3.16 实现 `ClientUpdateRequired` 终态。
- [x] 3.17 实现 `InitializePackageAndVerifyCache` 阶段。
- [x] 3.18 实现 `RequestPackageVersion` 阶段。
- [x] 3.19 实现 `UpdatePackageManifest` 阶段。
- [x] 3.20 实现 `PlanCoreDownload` 阶段。
- [x] 3.21 实现 `AwaitCoreDownloadConsent` 阶段。
- [x] 3.22 实现 `DownloadCore` 阶段。
- [x] 3.23 实现 `ClearObsoleteCache` 阶段。
- [x] 3.24 实现 `LoadHotUpdateAssemblies` 阶段。
- [x] 3.25 实现 `EnterProductRuntime` 阶段。
- [x] 3.26 让每个阶段只发布 immutable snapshot。
- [x] 3.27 让不可重试配置错误只提供 Exit。
- [x] 3.28 让网络和服务错误只从同一阶段 Retry。
- [x] 3.29 禁止任一错误转入 Gameplay 或本地模式。

## 4. TEngine Procedure 收敛

- [x] 4.1 将 `ProcedureLaunch` 改为只启动 ProductBootstrapRunner。
- [x] 4.2 从 `ProcedureInitPackage` 删除 `needInitMainFest=true`。
- [x] 4.3 删除 `ProcedureInitPackage` 内部版本请求副作用。
- [x] 4.4 将 package 初始化接入 Runner 对应阶段。
- [x] 4.5 将版本请求从 `ProcedureInitResources` 迁入独立阶段。
- [x] 4.6 将 Manifest 更新从 `ProcedureInitResources` 迁入独立阶段。
- [x] 4.7 将整包 downloader 创建替换为 Core 标签 downloader。
- [x] 4.8 删除 `ProcedureInitResources` 的重复 PackageVersion 写入。
- [x] 4.9 删除 `ProcedureInitResources` 的直接 Fatal 终止链。
- [x] 4.10 将 HotUpdateAssemblyLoader 接入 Runner handoff。
- [x] 4.11 让程序集加载失败返回结构化阶段错误。
- [x] 4.12 新建无业务分支 `ProcedureProductRuntime`。
- [x] 4.13 让 ProcedureLoadAssembly 在 handoff 成功后进入 ProductRuntime。
- [x] 4.14 让 ProcedureLoadAssembly 只调用一次 GameApp.Entrance。
- [x] 4.15 删除 ProcedureStartGame 类。
- [x] 4.16 删除 ProcedureStartGame 的场景常量。
- [x] 4.17 从 Procedure Settings 删除 ProcedureStartGame 注册。
- [x] 4.18 在 Procedure Settings 注册新启动阶段和 ProductRuntime。
- [x] 4.19 删除所有 AOT 启动后直接加载 StandaloneGameplay 的调用。
- [x] 4.20 清理迁移后不再使用的 Procedure 字段与命名。

## 5. YooAsset 正式文件系统参数

- [x] 5.1 新建项目层 `ProjectResourceInitializationAdapter`。
- [x] 5.2 让 adapter 只通过 TEngine/YooAsset 公共初始化参数接入。
- [x] 5.3 将缓存文件校验级别设置为 High。
- [x] 5.4 将缓存校验并发数写入正式配置。
- [x] 5.5 设置 ResumeDownloadMinimumSize。
- [x] 5.6 设置 ResumeDownloadResponseCodes。
- [x] 5.7 设置正式 DownloadMaxConcurrency。
- [x] 5.8 设置正式 DownloadMaxRequestPerFrame。
- [x] 5.9 设置正式 DownloadWatchDogTime。
- [x] 5.10 将唯一 ResourceEndpoint 注入 RemoteServices。
- [x] 5.11 删除 ResourceModule 中硬编码 Basic Auth 示例凭据。
- [x] 5.12 删除备用端点切换逻辑。
- [x] 5.13 删除空端点时的本地 URL 拼装。
- [x] 5.14 在在线 PlayMode 初始化前校验 HTTPS endpoint。
- [x] 5.15 将缓存校验进度映射到启动 snapshot。
- [x] 5.16 将缓存有效和失效文件计数映射到诊断。
- [x] 5.17 将 package version 请求结果写入唯一 snapshot 字段。
- [x] 5.18 将 Manifest 更新结果写入唯一 snapshot 字段。
- [x] 5.19 将 Core downloader callbacks 接入 Runner generation。
- [x] 5.20 将下载文件错误转换为稳定 ProductStartupErrorCode。
- [x] 5.21 在 BeginDownload 前读取 downloader 文件数与字节数。
- [x] 5.22 实现正式磁盘可用空间查询 adapter。
- [x] 5.23 计算下载、临时增长与安全余量总预算。
- [x] 5.24 在空间不足时阻止 BeginDownload。
- [x] 5.25 在 Core 完成后清理未使用 Bundle 缓存。
- [x] 5.26 禁止 Core 清理删除当前 Manifest 引用文件。

## 6. Bootstrap UI

- [x] 6.1 在 Bootstrap Scene 建立内置 UI 根。
- [x] 6.2 创建最小内置启动 UI 资源目录。
- [x] 6.3 让内置 UI 不引用 DefaultPackage 远端资源。
- [x] 6.4 创建阶段时间线视图。
- [x] 6.5 创建当前阶段和耗时视图。
- [x] 6.6 创建 Client/Resource/Protocol 版本视图。
- [x] 6.7 创建缓存校验结果视图。
- [x] 6.8 创建文件和字节进度视图。
- [x] 6.9 创建下载速度和预计剩余时间视图。
- [x] 6.10 创建当前文件和重试次数视图。
- [x] 6.11 创建结构化错误视图。
- [x] 6.12 创建 Core 下载确认视图。
- [x] 6.13 将 Retry 按钮只绑定 Runner Retry command。
- [x] 6.14 将 Exit 按钮绑定正式退出 command。
- [x] 6.15 禁止 View 直接调用 ResourceModule。
- [x] 6.16 禁止 View 直接持有 downloader callbacks。
- [x] 6.17 让 View 只订阅 IProductStartupSnapshotSource。
- [x] 6.18 在 ClientUpdateRequired 终态隐藏下载入口。
- [x] 6.19 增加 CRC、HTTPS 与 Manifest Signature 范围说明。
- [x] 6.20 增加 Bootstrap 内置依赖闭包检查器。
- [x] 6.21 让闭包检查器拒绝 HotFix 和 DefaultPackage 远端引用。

## 7. DefaultPackage 标签与资源闭包

- [x] 7.1 在 AssetBundleCollector 设置中建立唯一 Core 标签。
- [x] 7.2 将全部 HotFix DLL 文本资产标记为 Core。
- [x] 7.3 将 AOT Metadata 文本资产标记为 Core。
- [x] 7.4 将 ProductShell Scene 标记为 Core。
- [x] 7.5 将登录与 Home UI 资源标记为 Core。
- [ ] 7.6 将公共字体和必要共享 UI 资源标记为 Core。
- [x] 7.7 建立唯一 Gameplay 标签。
- [x] 7.8 将 StandaloneGameplay Scene 标记为 Gameplay。
- [x] 7.9 将 Corin 玩法必需资源闭包标记为 Gameplay。
- [x] 7.10 将玩法必需动画和战斗表现依赖标记为 Gameplay。
- [x] 7.11 建立唯一 OptionalHD 标签。
- [x] 7.12 选择或创建不影响正确性的真实 OptionalHD 表现资源。
- [x] 7.13 将 OptionalHD 资源从 Core 和 Gameplay 必需闭包移出。
- [x] 7.14 删除旧无标签 Scenes 收集规则。
- [x] 7.15 删除旧无标签 HotUpdateAssemblies 收集规则。
- [x] 7.16 为 Scene 保持独立打包规则。
- [ ] 7.17 为共享字体、材质和图集配置正式共享打包规则。
- [x] 7.18 为 Corin 资源配置业务聚合打包规则。
- [x] 7.19 删除无业务理由的逐文件 PackSeparately 配置。
- [x] 7.20 增加标签闭包编辑器校验。
- [x] 7.21 让 Core 校验拒绝 Gameplay 和 OptionalHD 非必要依赖。
- [x] 7.22 让 Gameplay 校验拒绝缺失 StandaloneGameplay 依赖。
- [x] 7.23 让 OptionalHD 校验拒绝空标签和正确性依赖。

## 8. HotFix 产品入口与 ProductShell

- [x] 8.1 新建 HotFix `ProductStartupCoordinator`。
- [x] 8.2 让 GameApp.Entrance 创建唯一 ProductStartupCoordinator。
- [x] 8.3 从 GameApp 删除等待 AOT Procedure 进 Gameplay 的隐含职责。
- [x] 8.4 让 ProductStartupCoordinator 接收只读 Startup handoff。
- [x] 8.5 让 ProductStartupCoordinator 拒绝重复创建。
- [x] 8.6 新建 `ProductShell` Scene。
- [x] 8.7 在 ProductShell 建立登录视图根。
- [x] 8.8 在 ProductShell 建立 Home 视图根。
- [x] 8.9 在 ProductShell 建立只读 diagnostics 根。
- [x] 8.10 让 ProductShell 初始只显示登录视图。
- [x] 8.11 实现 `LoadProductShell` 阶段。
- [x] 8.12 实现 HotFix 产品阶段 snapshot。
- [x] 8.13 将 AOT 启动阶段历史投影到 ProductShell diagnostics。
- [x] 8.14 让 ProductStartupCoordinator 拥有 Auth、Home 和 Gameplay 子状态。
- [x] 8.15 让 ProductStartupCoordinator 在销毁时按顺序释放子状态。
- [x] 8.16 禁止 ProductShell 创建 SimulationSessionHost。
- [x] 8.17 禁止 Home 复制 StandaloneGameplay runtime。

## 9. Fantasy Client Session 所有权重构

- [x] 9.1 将 FantasyClientBootstrap 收敛为 runtime 初始化 owner。
- [x] 9.2 删除 FantasyClientBootstrap 的全局可变 SessionFacade。
- [x] 9.3 定义窄 `FantasyClientSessionOwner` 生命周期基座。
- [x] 9.4 让每个 Session owner 创建自己的 Fantasy Scene。
- [x] 9.5 让每个 Session owner 只销毁自己的 Scene 和 Session。
- [x] 9.6 新建 ProductAuthSessionOwner。
- [x] 9.7 将 AuthEndpoint 映射为 WebSocket + HTTPS Fantasy settings。
- [x] 9.8 让 ProductAuthSessionOwner 拒绝 TCP 与 KCP。
- [x] 9.9 让 ProductAuthSessionOwner 拒绝禁用 TLS。
- [x] 9.10 将 ServerAuthoritativeControlSessionModule 迁入独立 session owner。
- [x] 9.11 删除 ServerAuthoritative 对全局 SessionFacade 的调用。
- [x] 9.12 保持现有 KCP control 连接参数不变。
- [x] 9.13 保持现有 UDP Gameplay data plane 不变。
- [x] 9.14 让 Gameplay Disconnect 不影响 Auth Session。
- [x] 9.15 让 Auth Disconnect 不直接 Dispose Gameplay Session。
- [x] 9.16 在 GameApp shutdown 中按 owner 顺序清理 Fantasy runtime。

## 10. Auth Outer 协议

- [x] 10.1 在正式 Outer proto 定义 Auth 结果码。
- [x] 10.2 定义 `C2G_GuestLoginRequest`。
- [x] 10.3 在请求中加入 GuestAccountId。
- [x] 10.4 在请求中加入 ClientInstanceId。
- [x] 10.5 在请求中加入 ClientBuildVersion。
- [x] 10.6 在请求中加入 AuthProtocolVersion。
- [x] 10.7 定义 `G2C_GuestLoginResponse`。
- [x] 10.8 在响应中加入 canonical AccountId。
- [x] 10.9 在响应中加入 SessionGeneration。
- [x] 10.10 在响应中加入 SessionToken。
- [x] 10.11 在响应中加入 TokenExpiresAt。
- [x] 10.12 定义 `G2C_AccountSessionReplaced` push。
- [x] 10.13 在顶号推送中加入替换原因和新 Generation。
- [x] 10.14 运行正式 ProtocolExportTool 生成 Unity 消息。
- [x] 10.15 运行正式 ProtocolExportTool 生成 Server 消息。
- [x] 10.16 更新生成 Opcode。
- [x] 10.17 删除任何手写 Auth DTO 或 Opcode。

## 11. Startup Auth Server 模块

- [x] 11.1 新建 Startup Auth Entity 模块工程。
- [x] 11.2 新建 Startup Auth Hotfix 模块工程。
- [x] 11.3 固定 Entity 与 Hotfix Fantasy 版本一致。
- [x] 11.4 定义 AuthGateway Scene type。
- [x] 11.5 定义 sealed AuthSessionRegistryComponent。
- [x] 11.6 定义 Registry entry 值对象。
- [x] 11.7 在 entry 中保存 AccountId。
- [x] 11.8 在 entry 中保存 Session identity。
- [x] 11.9 在 entry 中保存 ClientInstanceId。
- [x] 11.10 在 entry 中保存 SessionGeneration。
- [x] 11.11 在 entry 中保存 SessionToken identity。
- [x] 11.12 实现 OnCreateScene AuthGateway 初始化事件。
- [x] 11.13 让初始化事件只对 AuthGateway Root Scene 添加 Registry。
- [x] 11.14 实现 Registry 的原子 ReplaceCurrent。
- [x] 11.15 实现 Registry 的单调 Generation 分配。
- [x] 11.16 实现 Registry 的精确 TryRemoveCurrent。
- [x] 11.17 禁止 Registry 使用静态全局字典。
- [x] 11.18 定义 Session-owned AuthenticatedGuestComponent。
- [x] 11.19 在组件中保存 AccountId、Generation 与 Session identity。
- [x] 11.20 实现 AuthenticatedGuestComponent DestroySystem。
- [x] 11.21 让 DestroySystem 调用精确 TryRemoveCurrent。
- [x] 11.22 在 DestroySystem 重置全部对象池字段。
- [x] 11.23 实现游客请求字段校验器。
- [x] 11.24 实现 ClientBuildVersion 兼容校验。
- [x] 11.25 实现 AuthProtocolVersion 精确校验。
- [x] 11.26 实现服务端随机短期 SessionToken 生成。
- [x] 11.27 禁止 SessionToken 写入普通日志。
- [x] 11.28 实现 C2G_GuestLoginRequest Handler。
- [x] 11.29 让 Handler 使用 FTask 和业务 ErrorCode。
- [x] 11.30 让 Handler 在异步挂起前完成 replace 决策。
- [x] 11.31 将新认证组件附加到当前 Session。
- [x] 11.32 向新 Session 返回 AccountId、Generation 与 token。
- [x] 11.33 向旧 Session 发送 AccountSessionReplaced。
- [x] 11.34 在推送后关闭旧 Session。
- [x] 11.35 保证旧 Session Destroy 不删除新 entry。

## 12. ThirdPerson.Startup.Server 产品

- [x] 12.1 新建 `ThirdPerson.Startup.Server` executable 工程。
- [x] 12.2 定义唯一 Startup Server ProductId。
- [x] 12.3 新建产品专属 Fantasy.config。
- [x] 12.4 在配置中只声明 AuthGateway Scene。
- [x] 12.5 配置 WebSocket 外部协议。
- [x] 12.6 配置正式 WSS 部署边界。
- [x] 12.7 将 Startup Auth Entity 模块加入产品闭包。
- [x] 12.8 将 Startup Auth Hotfix 模块加入产品闭包。
- [x] 12.9 将正式生成协议加入产品闭包。
- [x] 12.10 只引用共享 ThirdPerson.Server.Host。
- [x] 12.11 禁止引用 Gate Room Hotfix 模块。
- [x] 12.12 禁止引用 Authority 和 Gameplay portable 模块。
- [x] 12.13 定义 Startup Server Product manifest。
- [x] 12.14 记录 executable、config 和模块 hash。
- [x] 12.15 增加 exact file closure 校验。
- [x] 12.16 增加 Scene type 精确集合校验。
- [x] 12.17 增加模块集合精确校验。
- [x] 12.18 增加产品专属 publish 命令。
- [x] 12.19 让 publish 原子替换自己的产品目录。
- [x] 12.20 让 Run 只消费已发布产品和 manifest。
- [x] 12.21 禁止 Run 临时 build、publish 或改写配置。
- [x] 12.22 禁止 Startup Server 进入 Network Test Product catalog。

## 13. Unity 游客认证流程

- [x] 13.1 定义 immutable GuestLoginCommand。
- [x] 13.2 定义 authenticated session state。
- [x] 13.3 定义 Auth 结构化错误模型。
- [x] 13.4 在登录视图采集 GuestAccountId。
- [x] 13.5 从正式启动参数或生成器取得 ClientInstanceId。
- [x] 13.6 禁止按进程启动顺序推断 ClientInstanceId。
- [x] 13.7 实现 ConnectAuthGateway 产品阶段。
- [x] 13.8 将 WSS 连接结果写入产品 snapshot。
- [x] 13.9 实现 GuestLogin RPC 调用。
- [x] 13.10 在请求中发送正式三类版本身份。
- [x] 13.11 将 Auth ErrorCode 映射为登录错误。
- [x] 13.12 在成功响应提交 AccountId 与 Generation。
- [x] 13.13 只在 Auth Session owner 内保存 SessionToken。
- [x] 13.14 禁止 diagnostics 和日志读取 SessionToken。
- [x] 13.15 实现 AccountSessionReplaced Unity push Handler。
- [x] 13.16 让 push Handler 只写 typed Auth event queue。
- [x] 13.17 让 ProductStartupCoordinator 在产品 update 消费 Auth event。
- [x] 13.18 撤销被替换 Session 的 authenticated state。
- [x] 13.19 释放被替换 Session 的 Home scope。
- [x] 13.20 关闭被替换 Auth Session。
- [x] 13.21 返回 ProductShell 登录视图并显示顶号原因。
- [x] 13.22 禁止缓存 token 自动跳过登录。
- [x] 13.23 在 UI 标明 Guest Demo Identity 范围。
- [x] 13.24 在 UI 标明单 AuthGateway 唯一会话范围。

## 14. Home 与 Gameplay 进入

- [x] 14.1 定义 Home 产品状态。
- [x] 14.2 定义 immutable Home PreloadPlan。
- [x] 14.3 在 Home Plan 定义 Shared UI barrier。
- [x] 14.4 在 Home Plan 定义 Home UI barrier。
- [x] 14.5 在 Home Plan 定义 Home presentation barrier。
- [x] 14.6 登录成功后创建唯一 Home scope。
- [x] 14.7 执行 Home PreloadPlan。
- [x] 14.8 在全部 barrier 成功后提交 HomeReady。
- [x] 14.9 HomeReady 前保持 Gameplay 入口禁用。
- [x] 14.10 在 Home 显示启动、认证、资源和内存摘要。
- [x] 14.11 创建 Gameplay 下载计划命令。
- [x] 14.12 只为 Gameplay 标签创建 downloader。
- [x] 14.13 在 Home 显示 Gameplay 下载文件数与字节数。
- [x] 14.14 在下载前执行磁盘空间预检。
- [x] 14.15 将 Gameplay downloader 接入产品 snapshot。
- [x] 14.16 定义 immutable Gameplay PreloadPlan。
- [x] 14.17 在 Gameplay Plan 定义 shared resource barrier。
- [x] 14.18 在 Gameplay Plan 定义 Scene barrier。
- [x] 14.19 在 Gameplay Plan 定义 Corin presentation barrier。
- [x] 14.20 创建唯一 Gameplay scope。
- [x] 14.21 在 Gameplay 下载完成后执行 PreloadPlan。
- [x] 14.22 只在 PreloadPlan 提交后加载 StandaloneGameplay。
- [x] 14.23 使用 TEngine SceneModule 加载正式 Scene location。
- [x] 14.24 禁止直接 SceneManager 加载未准备 Gameplay Scene。
- [x] 14.25 保持 StandaloneGameplay 内现有 SimulationSessionHost 主链不变。
- [x] 14.26 在 Scene 进入失败时释放未提交 Gameplay scope。
- [x] 14.27 在 Scene 进入失败时保持 Home 可用并显示错误。

## 15. ProductResourceRuntime 与 Lease

- [x] 15.1 新建项目层 `ProductResourceRuntime`。
- [x] 15.2 定义稳定 `ResourceScopeId`。
- [x] 15.3 定义 `ResourceScopeKind` 的 Global/Home/Gameplay/Transient 值。
- [x] 15.4 定义 package/location/type 物理资源 identity。
- [x] 15.5 定义 logical `ResourceLease`。
- [x] 15.6 定义 prefab `ResourceInstanceLease`。
- [x] 15.7 让每个 lease 精确保存一个 scope owner。
- [x] 15.8 让无 owner 请求明确失败。
- [x] 15.9 创建唯一 Global scope。
- [x] 15.10 禁止自动把无 owner 请求放入 Global scope。
- [x] 15.11 实现 Home scope 创建。
- [x] 15.12 实现 Gameplay scope 创建。
- [x] 15.13 实现显式 Transient scope 创建。
- [x] 15.14 实现 scope 状态 Active/Closing/Disposed。
- [x] 15.15 让 Closing scope 拒绝新请求。
- [x] 15.16 建立 in-flight physical load 表。
- [x] 15.17 让相同 identity 复用同一 physical load。
- [x] 15.18 记录每次 logical load。
- [x] 15.19 记录每次 physical load。
- [x] 15.20 记录每次 in-flight join。
- [x] 15.21 记录每次 pool/cache hit。
- [x] 15.22 让失败 physical load 唤醒全部等待者。
- [x] 15.23 禁止失败请求登记 lease。
- [x] 15.24 让每个成功等待者取得独立 logical lease。
- [x] 15.25 使用 TEngine LoadAssetAsync 作为物理 asset load。
- [x] 15.26 使用 TEngine LoadGameObjectAsync 作为 prefab instance load。
- [x] 15.27 分开记录 prefab asset 与 live instance。
- [x] 15.28 让 instance lease 负责销毁自己的 GameObject。
- [x] 15.29 让 asset lease release 调用一次 ResourceModule.UnloadAsset。
- [x] 15.30 禁止项目层直接 Dispose YooAsset 内部 handle。
- [x] 15.31 禁止项目层创建第二个 AssetObject pool。
- [x] 15.32 实现 lease 幂等 Dispose。
- [x] 15.33 记录重复 Dispose 错误。
- [x] 15.34 实现 scope 取消未提交逻辑请求。
- [x] 15.35 保持其它 scope 对共享 physical load 的等待有效。
- [x] 15.36 实现 scope 批量释放已提交 lease。
- [x] 15.37 实现 scope 批量销毁 live instance。
- [x] 15.38 让 scope Dispose 完成后清除 owner 索引。

## 16. 资源安全点与 Gameplay teardown

- [x] 16.1 定义正式 `ResourceMaintenanceReason`。
- [x] 16.2 定义 SceneTransitionCompleted 安全点。
- [x] 16.3 定义 ReturnHomeLoading 安全点。
- [x] 16.4 定义 ExplicitMaintenance 安全点。
- [x] 16.5 定义 LowMemory 安全点。
- [x] 16.6 让普通窗口关闭只释放 lease。
- [x] 16.7 禁止普通窗口关闭调用全局 UnloadUnusedAssets。
- [x] 16.8 禁止普通窗口关闭调用 GC.Collect。
- [x] 16.9 将安全点接入 TEngine UnloadUnusedAssets。
- [x] 16.10 记录安全点回收前资源 snapshot。
- [x] 16.11 记录安全点回收后资源 snapshot。
- [x] 16.12 将 Unity low-memory 回调接入 ProductResourceRuntime。
- [x] 16.13 让 low-memory 只回收零引用资源。
- [x] 16.14 保留所有活动 scope lease。
- [x] 16.15 定义普通 Gameplay 返回 Home 的 teardown 顺序。
- [x] 16.16 先停止 SimulationSessionHost。
- [x] 16.17 再销毁 Actor registration 与 Endpoint。
- [x] 16.18 再完成 Gameplay Scene runtime 清理。
- [x] 16.19 再 Dispose Gameplay scope。
- [x] 16.20 最后在加载遮罩执行 unused asset 回收。
- [x] 16.21 在 Session teardown 失败时阻止强制资源清空。

## 17. 资源与内存诊断

- [x] 17.1 定义 immutable `ResourceRuntimeSnapshot`。
- [x] 17.2 在 snapshot 中加入 logical/physical/join/hit 计数。
- [x] 17.3 在 snapshot 中加入 active lease 和 live instance。
- [x] 17.4 在 snapshot 中加入 scope 与每 scope lease 数。
- [x] 17.5 在 snapshot 中加入 TEngine 公共 pool 指标。
- [x] 17.6 在 snapshot 中加入 YooAsset package/version/tag 指标。
- [x] 17.7 禁止通过反射读取 TEngine 私有池。
- [x] 17.8 禁止通过反射读取 YooAsset 私有 handle。
- [x] 17.9 定义 immutable `MemoryRuntimeSnapshot`。
- [x] 17.10 使用 ProfilerRecorder 采集 Total Used。
- [x] 17.11 使用 ProfilerRecorder 采集 Total Reserved。
- [x] 17.12 使用 ProfilerRecorder 采集 GC 指标。
- [x] 17.13 使用公开指标采集 Texture 内存。
- [x] 17.14 使用公开指标采集 Mesh 内存。
- [x] 17.15 新建正式平台内存预算资产。
- [x] 17.16 在预算中写入 Home 限额。
- [x] 17.17 在预算中写入 Gameplay 限额。
- [x] 17.18 缺失预算时报告配置错误。
- [x] 17.19 禁止按当前峰值自动生成预算。
- [x] 17.20 在 HomeReady 冻结资源和内存 snapshot。
- [x] 17.21 在 GameplayReady 冻结资源和内存 snapshot。
- [x] 17.22 在 Gameplay 回收后冻结资源和内存 snapshot。
- [x] 17.23 建立有界 snapshot 对比历史。
- [x] 17.24 在 ProductShell 创建 Startup 诊断页。
- [x] 17.25 在 ProductShell 创建 Resource 诊断页。
- [x] 17.26 在 ProductShell 创建 Network 诊断页。
- [x] 17.27 在 ProductShell 创建 Memory 诊断页。
- [x] 17.28 让全部诊断页只消费 snapshot。
- [x] 17.29 禁止诊断页直接触发下载或回收。
- [x] 17.30 对 Auth endpoint 与 AccountId 做脱敏显示。
- [x] 17.31 完全隐藏 SessionToken 与证书敏感信息。

## 18. Development Fault Lab

- [x] 18.1 建立 Editor/Development Build Fault Lab 编译边界。
- [x] 18.2 在 Release Player 排除 Fault Lab UI。
- [x] 18.3 在 Release Player 排除 Fault Lab command handler。
- [x] 18.4 实现取消当前 downloader 命令。
- [x] 18.5 让取消命令走 Runner 正式 cancellation generation。
- [x] 18.6 实现选择一个明确缓存 Bundle 的命令。
- [x] 18.7 实现破坏已选缓存 Bundle 内容的命令。
- [x] 18.8 禁止缓存破坏命令接受任意文件系统路径。
- [x] 18.9 实现同 location 并发申请二十次命令。
- [x] 18.10 将二十次请求放入明确 Transient scope。
- [x] 18.11 实现 Dispose 指定非 Global scope 命令。
- [x] 18.12 禁止 Fault Lab Dispose Global scope。
- [x] 18.13 实现调用正式 low-memory 入口命令。
- [x] 18.14 为每个 Fault command 发布结构化 diagnostics event。
- [x] 18.15 禁止 Fault Lab 替换 ResourceEndpoint。
- [x] 18.16 禁止 Fault Lab 禁用 TLS。
- [x] 18.17 禁止 Fault Lab 注入 mock Auth Server。
- [x] 18.18 禁止 Fault Lab 直接修改资源引用计数。

## 19. 清理、文档和规格同步

- [x] 19.1 删除旧 ProcedureStartGame 代码与 meta。
- [x] 19.2 删除旧启动直接 Gameplay Scene 引用。
- [x] 19.3 删除旧无标签整包 downloader 路径。
- [x] 19.4 删除旧空 FallbackResDownLoadPath 字段的运行时合同。
- [x] 19.5 删除旧 Fantasy 全局 SessionFacade。
- [x] 19.6 删除迁移产生的未使用 using、字段和类型。
- [x] 19.7 更新 `openspec/project.md` 普通产品启动链。
- [x] 19.8 更新 `openspec/project.md` ProductShell 与 Home 边界。
- [x] 19.9 更新 `openspec/project.md` Startup Server 产品归属。
- [x] 19.10 更新 `openspec/project.md` ResourceScope 代码组织。
- [x] 19.11 新增正式 ResourceEndpoint、Range 与证书部署文档。
- [x] 19.12 新增 Startup Server publish 与 Run 文档。
- [x] 19.13 新增 Core、Gameplay 与 OptionalHD 资产归属文档。
- [x] 19.14 新增 Guest Identity 与单 Gateway 唯一会话范围文档。
- [x] 19.15 新增 ResourceScope、Lease 与安全回收时机文档。
- [x] 19.16 删除旧 Bootstrap 直接 Gameplay 的说明。
- [x] 19.17 删除旧全局 Fantasy SessionFacade 的说明。
- [x] 19.18 全局搜索并删除旧启动链命名和引用。
- [x] 19.19 全局搜索并删除备用 URL 和协议降级路径。
- [x] 19.20 全局搜索并删除业务直接 YooAssets package 调用。

## 20. 编译与严格规格校验

- [x] 20.1 编译受影响的 Unity AOT 与 HotFix 程序集。
- [x] 20.2 编译生成协议程序集。
- [x] 20.3 使用 `dotnet build --disable-build-servers /nr:false /p:UseSharedCompilation=false` 编译 Startup Server 产品。
- [x] 20.4 使用相同参数编译受影响的 Unity Authority Server 产品。
- [x] 20.5 使用相同参数编译受影响的 DotRecast Authority Server 产品。
- [x] 20.6 构建结束后执行 `dotnet build-server shutdown`。
- [x] 20.7 运行 Startup Server product manifest exact closure 校验。
- [x] 20.8 运行 Bootstrap 内置依赖闭包校验。
- [x] 20.9 运行 DefaultPackage 标签闭包校验。
- [x] 20.10 运行普通 Player Build Settings 单入口校验。
- [x] 20.11 运行 `openspec validate add-commercial-client-startup-showcase --strict --no-interactive`。
- [x] 20.12 运行 `openspec validate --all --strict --no-interactive`。

## 21. Editor 本地玩法开发入口

- [x] 21.1 定义 Editor 本地玩法入口与普通产品入口的隔离边界。
- [x] 21.2 增加唯一正式 Sandbox Gameplay Scene 直接运行菜单。
- [x] 21.3 增加本地 Gameplay Scene 依赖校验菜单。
- [x] 21.4 在运行前校验可琳 Prefab、Foot Placement composition 与 FinalIK solver。
- [x] 21.5 退出本地 Play 后恢复开发者原场景。
- [x] 21.6 禁止本地入口修改 Build Settings、ProductStartupProfile 或正式 endpoint。
- [x] 21.7 在规格和文档中明确当前没有 Motion Matching、Pose Search 或 Pose Database。
