## MODIFIED Requirements

### Requirement: TEngine 依赖必须迁移到正式包和正式配置

项目 MUST 将 TEngine 核心、UniTask、YooAsset 和 HybridCLR 作为正式依赖接入，不得通过 `Ref`、临时脚本复制或示例工程路径运行。项目 MAY在 `Packages/com.alex.tengine` 内扩展通用基础设施合同，但 MUST不把 ProductStartup、登录、Home、Gameplay 或 ResourceScope 业务实现迁入包。

#### Scenario: ResourceModule 缺少正式初始化参数

- **WHEN** 项目需要向 YooAsset 传入唯一资源端点、High 校验、续传和下载限制
- **THEN** TEngine ResourceModule MUST通过通用公共初始化合同接收这些参数
- **AND** 合同 MUST不引用项目 Profile、业务状态或页面类型
- **AND** 项目 MUST不绕过 TEngine PackageMap 创建第二个 ResourcePackage

#### Scenario: 框架源码落位

- **WHEN** TEngine 核心代码被扩展
- **THEN** 通用基础设施代码 MUST位于 `Packages/com.alex.tengine`
- **AND** 项目业务代码 MUST继续位于项目程序集
- **AND** 示例业务目录 MUST不作为运行时代码进入项目

### Requirement: TEngine 必须作为客户端热更和资源底座接入

项目 MUST 将 TEngine 定位为客户端基础设施，用于内置 Bootstrap、启动 Procedure、热更程序集加载、资源包初始化、异步资源加载和基础模块管理。AOT TEngine Procedure MUST只拥有 HotFix 可用之前的资源交付状态，并在调用 `GameApp.Entrance()` 后进入无业务分支的 `ProcedureProductRuntime`；登录、Home、Gameplay 下载和 Gameplay Scene 选择 MUST由 HotFix `ProductStartupCoordinator` 唯一拥有。

#### Scenario: 启动项目 runtime

- **WHEN** Unity 场景中的 TEngine bootstrap 被启动
- **THEN** 系统 MUST初始化 TEngine 基础模块
- **AND** 系统 MUST通过显式阶段初始化 YooAsset 默认资源包、执行缓存完整性检查并准备 Core
- **AND** Core 与热更程序集有效后系统 MUST通过 `GameApp.Entrance()` 进入项目自己的 runtime 入口
- **AND** 系统 MUST不得进入 TEngine 示例 UI、示例业务入口或直接 Gameplay Scene

#### Scenario: 加载热更入口

- **WHEN** Core 标签和热更程序集清单可用
- **THEN** 启动流程 MUST加载配置中的热更程序集
- **AND** 启动流程 MUST切换到 `ProcedureProductRuntime` 并只调用一次 `GameApp.Entrance()`
- **AND** `ProductStartupCoordinator` MUST成为 ProductShell、登录、Home 与 Gameplay 进入的唯一写入者
- **AND** 启动流程 MUST不得引用示例 `BattleMainUI`、`LoginUI`、示例 `GameModule/UIModule` 或旧 `ProcedureStartGame`

### Requirement: 资源配置必须使用正式单一端点

项目 MUST 使用一个显式配置的 TEngine/YooAsset 正式 `ResourceEndpoint`。该端点 MUST通过 HTTPS 提供 StartupPolicy、YooAsset package version、Manifest 与 Bundle，并支持断点续传所需的 Range。系统 MUST NOT 配置备用端点、fallback URL、旧资源目录、本地测试 URL 或运行时协议降级路径。

#### Scenario: 设置远端资源 URL

- **WHEN** ResourceModule 创建远端资源服务
- **THEN** URL MUST来自正式 `ResourceEndpoint`
- **AND** StartupPolicy 与 DefaultPackage 文件 MUST共享该正式基地址
- **AND** 该端点 MUST不指向旧资源目录、旧配置数据或示例资源链路
- **AND** 系统 MUST NOT 在失败时自动切换到第二个 URL

#### Scenario: 未配置真实远端资源 URL

- **WHEN** HostPlayMode 或 WebPlayMode 读取远端资源端点
- **AND** `ResourceEndpoint` 为空、非 HTTPS 或包含旧示例地址
- **THEN** 系统 MUST直接报资源端点未配置或不安全错误
- **AND** 系统 MUST不得退回本地测试 URL、EditorSimulateMode 或 StreamingAssets 整包路径

#### Scenario: 断点续传请求远端文件

- **WHEN** YooAsset 对达到正式阈值的临时文件发起 Range 请求
- **THEN** ResourceEndpoint MUST返回与请求偏移一致的部分内容
- **AND** 完整文件 MUST在 High 校验成功后才成为有效缓存
