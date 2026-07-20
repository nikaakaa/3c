## ADDED Requirements

### Requirement: 普通产品认证必须使用独立 WSS Auth Session

ProductStartupCoordinator MUST通过固定 WebSocket + TLS 配置连接唯一 AuthEndpoint。AuthEndpoint MUST使用 `wss://`，认证 Session MUST由 ProductAuthSessionOwner 独立持有，MUST不使用 TCP、KCP、`ws://` 或禁用证书校验作为失败替代。

#### Scenario: 连接正式 AuthEndpoint

- **WHEN** ProductShell 进入 ConnectAuthGateway 阶段且 AuthEndpoint 合法
- **THEN** ProductAuthSessionOwner MUST创建自己的 Fantasy Scene 和 WebSocket Session
- **AND** 客户端 MUST启用 TLS
- **AND** diagnostics MUST显示 transport 为 WSS

#### Scenario: AuthEndpoint 非 WSS

- **WHEN** 正式配置包含空地址、`ws://`、TCP 或 KCP AuthEndpoint
- **THEN** 认证配置 MUST在连接前失败
- **AND** 系统 MUST不自动改用其它协议或现有 Gameplay endpoint

### Requirement: Startup Server 必须是独立 Fantasy 产品

项目 MUST提供 `ThirdPerson.Startup.Server` 产品。该产品 MUST只装配 AuthGateway 所需 Fantasy Scene、Entity/Hotfix 模块、Outer 协议与共享 Server Host，不得包含 ServerAuthoritative Room、Authority Host、UDP Gameplay 数据面、Character Program、WorldSolver 或 DeterministicRollback Relay。

#### Scenario: 发布 Startup Server

- **WHEN** Startup Server 产品被发布
- **THEN** 产物 MUST拥有唯一 ProductId、Fantasy.config、AuthGateway Scene type、正式入口和产品 manifest
- **AND** MUST不进入 Network Test Product catalog 或输出目录

#### Scenario: Startup Server 闭包混入 Gameplay Authority

- **WHEN** 产品 manifest 或模块闭包包含 Authority Scene、ServerAuthoritative Room runtime 或 Gameplay portable runtime
- **THEN** Startup Server 发布 MUST失败
- **AND** MUST不删除多余模块后继续运行

### Requirement: 游客登录协议必须通过正式 Outer 生成链路

游客登录 RPC、登录响应与顶号推送 MUST定义在正式 Outer proto，并通过 ProtocolExportTool 生成 Unity 与 Server 代码。协议 MUST包含 GuestAccountId、ClientInstanceId、ClientBuildVersion、AuthProtocolVersion、AccountId、SessionGeneration、SessionToken、TokenExpiresAt 与结构化结果；实现 MUST不手写 generated `.g.cs` 或复制 DTO。

#### Scenario: 导出游客登录协议

- **WHEN** Outer proto 中的 Auth 消息发生变化
- **THEN** ProtocolExportTool MUST重新生成 Client/Server 消息和 Opcode
- **AND** Handler MUST消费生成类型

#### Scenario: Auth Protocol 版本不匹配

- **WHEN** LoginRequest.AuthProtocolVersion 与 AuthGateway 当前版本不同
- **THEN** Handler MUST返回明确业务 ErrorCode
- **AND** Session MUST不进入 authenticated registry

### Requirement: 游客身份必须明确不是密码账号系统

本变更的认证 MUST使用受约束 GuestAccountId 与 ClientInstanceId 建立 Demo 身份，并由 Server 生成短期 SessionToken。UI 和 diagnostics MUST标记为 Guest Demo Identity，MUST不声称已经实现密码、注册、数据库账号、支付身份或防盗号认证。SessionToken MUST不写入普通日志或未脱敏 diagnostics。

#### Scenario: 游客登录成功

- **WHEN** GuestAccountId、ClientInstanceId 与版本字段均合法
- **THEN** Server MUST返回 canonical AccountId、SessionGeneration、随机 SessionToken 和过期时间
- **AND** Client MUST只在当前 Auth Session 生命周期内保存 token

#### Scenario: 登录字段非法

- **WHEN** GuestAccountId 或 ClientInstanceId 为空、超长或包含非法格式
- **THEN** Handler MUST返回参数 ErrorCode
- **AND** MUST不创建 registry 记录或 SessionToken

### Requirement: AuthGateway Scene 必须唯一拥有认证会话 Registry

AuthGateway Scene MUST在 OnCreateScene 生命周期中创建唯一 AuthSessionRegistryComponent。Registry MUST保存每个 AccountId 当前 Session identity、ClientInstanceId、单调 SessionGeneration 与 SessionToken identity。Handler、Session component 和 diagnostics MUST通过该 Scene owner 访问 registry，MUST不使用静态全局字典或挂到 Gameplay Room。

#### Scenario: AuthGateway Scene 创建

- **WHEN** Fantasy 创建 AuthGateway Root Scene
- **THEN** OnCreateScene handler MUST附加唯一 AuthSessionRegistryComponent
- **AND** Registry MUST随 Scene 销毁级联释放

#### Scenario: 非 AuthGateway Scene 收到认证请求

- **WHEN** Login Handler 所在 Session Scene 不拥有 AuthSessionRegistryComponent
- **THEN** 请求 MUST返回服务器配置错误
- **AND** MUST不临时创建全局 registry

### Requirement: 单 AuthGateway 内同一 AccountId 必须只有一个当前 Generation

每次成功登录 MUST在 AuthGateway Scene 顺序执行边界内生成更大的 SessionGeneration，并原子替换该 AccountId 当前记录。替换完成后旧 Session MUST收到 `AccountSessionReplaced` 推送并被关闭。任一时刻 registry 对一个 AccountId MUST最多保存一个当前记录。

#### Scenario: 第二个客户端登录同一 GuestAccountId

- **WHEN** Client B 对已有 Client A 当前记录的 AccountId 登录成功
- **THEN** Registry MUST将当前记录替换为 Client B 和新的 Generation
- **AND** Client A MUST收到包含替换原因和新 Generation 的顶号推送
- **AND** Client A Session MUST关闭

#### Scenario: 两个登录请求连续到达

- **WHEN** 同一 AccountId 的两个合法请求在同一 AuthGateway Scene 连续处理
- **THEN** 后处理请求 MUST成为唯一当前 Generation
- **AND** 前一请求的 Session MUST按旧会话规则退出

### Requirement: 旧 Session 销毁必须使用条件化 Registry 清理

认证 Session MUST附加 AccountId、SessionGeneration 与 Session identity。Session 销毁时，Registry MUST仅在当前记录仍精确匹配这三项时删除；旧 Session 的迟到 Destroy MUST不删除新登录记录。

#### Scenario: 被替换的旧 Session 随后销毁

- **WHEN** Client A 已被 Client B 替换且 Client A DestroySystem 执行
- **THEN** Registry 清理 MUST发现 Generation 或 Session identity 不匹配
- **AND** Client B 当前记录 MUST保持不变

#### Scenario: 当前 Session 正常断开

- **WHEN** 当前记录对应 Session 主动退出或连接断开
- **THEN** 条件化清理 MUST删除匹配记录
- **AND** diagnostics MUST显示该 AccountId 当前离线

### Requirement: 顶号推送必须使旧客户端回到登录状态

Unity 侧 MUST使用正式 Fantasy push Handler 接收 `AccountSessionReplaced`，只把 typed event 写入 ProductAuthSessionOwner 队列。ProductStartupCoordinator MUST在后续产品 update 边界撤销认证状态、释放 Home scope、关闭旧 Auth Session 并返回 ProductShell 登录视图。Handler MUST不直接操作 Scene、UI 或 Gameplay Session。

#### Scenario: Home 中收到顶号推送

- **WHEN** 已认证客户端在 Home 收到 AccountSessionReplaced
- **THEN** ProductStartupCoordinator MUST使当前 authenticated state 失效
- **AND** Home scope MUST按正式顺序释放
- **AND** ProductShell MUST返回登录视图并显示原因

#### Scenario: Gameplay 已经开始时收到顶号推送

- **WHEN** 普通产品 Gameplay 运行期间 Auth Session 被替换
- **THEN** 产品协调器 MUST结束当前普通产品 Gameplay 进入流程并清理产品资源 owner
- **AND** MUST不继续以旧 token 运行或切换离线模式

### Requirement: Auth Session 与 Gameplay Session 必须隔离所有权

ProductAuthSessionOwner 与任一 Gameplay Network Model session owner MUST分别创建、保存和销毁自己的 Fantasy Scene/Session。Fantasy runtime 初始化 MAY共享，但全局可变 SessionFacade MUST不存在。一个 owner 的 Connect、Disconnect、Retry 或 Dispose MUST不改变另一 owner 的 Session。

#### Scenario: Auth Session 已连接后创建 Gameplay 控制连接

- **WHEN** 认证 WSS Session 仍有效且 ServerAuthoritative module 创建其 KCP control Session
- **THEN** 两个 owner MUST拥有不同 Fantasy Scene 和 Session identity
- **AND** Gameplay Connect MUST不先 Disconnect Auth Session

#### Scenario: Gameplay Session 失败

- **WHEN** Network Model control/data Session 失败并清理
- **THEN** Auth Session MAY保持有效供 ProductShell 显示失败和返回 Home
- **AND** Gameplay module MUST不 Dispose ProductAuthSessionOwner

### Requirement: 认证成功必须先于 Home Ready

ProductStartupCoordinator MUST只在 Auth Session 已连接、GuestLoginResponse 成功、SessionGeneration 已提交且 Home PreloadPlan 完成后进入 HomeReady。连接成功本身 MUST不等于认证成功；缓存中的旧 SessionToken MUST不自动跳过登录。

#### Scenario: WSS 已连接但登录失败

- **WHEN** ConnectAuthGateway 成功但 LoginResponse 返回业务错误
- **THEN** ProductShell MUST停留登录视图
- **AND** Home scope MUST不创建

#### Scenario: 登录成功但 Home 资源失败

- **WHEN** authenticated state 已提交但 Home PreloadPlan 失败
- **THEN** ProductShell MUST显示资源错误
- **AND** 未提交的 Home scope MUST释放
- **AND** MUST不把失败伪装为登录失败

### Requirement: 认证诊断必须公开边界而不泄漏凭据

认证诊断 MUST显示 Startup Server ProductId、WSS/TLS 状态、AuthEndpoint 脱敏主机、连接状态、脱敏 AccountId、ClientInstanceId、SessionGeneration、token 过期时间和最后错误码。它 MUST不显示 SessionToken、证书私钥、完整网络凭据或 Fantasy 私有发送窗口。

#### Scenario: 查看已认证连接

- **WHEN** Auth Session 已认证且 diagnostics 打开
- **THEN** UI MUST显示 WSS、TLS、Generation 和 token 过期时间
- **AND** SessionToken MUST完全不可见

### Requirement: 唯一在线能力必须声明单 Gateway 范围

产品说明、UI 和 diagnostics MUST将本能力描述为“单 AuthGateway Scene 唯一 Session”。系统 MUST不声称支持跨 Gate、跨进程或跨机房全局唯一登录，也 MUST不预留未使用的 Redis/数据库 fallback 配置。

#### Scenario: 查看能力范围

- **WHEN** 用户打开 Auth diagnostics 或作品能力说明
- **THEN** 系统 MUST显示唯一会话范围为当前 AuthGateway Scene
- **AND** 分布式唯一在线 MUST标记为未实现范围

