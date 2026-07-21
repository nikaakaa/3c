# tengine-hotupdate-foundation Specification

## Purpose
定义 TEngine、YooAsset、HybridCLR 和资源端点作为客户端启动、热更和资源底座的接入边界；TEngine 负责基础设施和流程装配，不替代 gameplay tick、角色管线或网络同步语义。
## Requirements
### Requirement: TEngine 必须作为客户端热更和资源底座接入

项目 MUST 将 TEngine 定位为客户端基础设施，用于启动流程、热更程序集加载、资源包初始化、异步资源加载和基础模块管理。

#### Scenario: 启动项目 runtime

- **WHEN** Unity 场景中的 TEngine bootstrap 被启动
- **THEN** 系统初始化 TEngine 基础模块
- **AND** 系统初始化 YooAsset 默认资源包
- **AND** 系统通过 `GameApp.Entrance()` 进入项目自己的 runtime 入口
- **AND** 系统不得进入 TEngine 示例 UI 或示例业务入口

#### Scenario: 加载热更入口

- **WHEN** 热更程序集清单可用
- **THEN** 启动流程加载配置中的热更程序集
- **AND** 启动流程调用 `GameApp.Entrance()`
- **AND** 启动流程不得引用示例 `BattleMainUI`、`LoginUI` 或示例 `GameModule/UIModule`

### Requirement: TEngine 依赖必须迁移到正式包和正式配置

项目 MUST 将 TEngine 核心、UniTask、YooAsset 和 HybridCLR 作为正式依赖接入，不得通过 `Ref`、临时脚本复制或示例工程路径运行。

#### Scenario: 框架源码落位

- **WHEN** TEngine 核心代码被迁移
- **THEN** 代码位于 `Packages/com.alex.tengine`
- **AND** 项目业务代码不得写入该包
- **AND** 示例业务目录不得作为运行时代码进入项目

#### Scenario: 三方依赖落位

- **WHEN** 项目解析 Unity packages
- **THEN** `UniTask` 和 `YooAsset` 作为 embedded packages 存在于项目 `Packages`
- **AND** `HybridCLR` 通过 `manifest.json` 正式声明
- **AND** 项目不得新增 Addressables 作为第二套资源管线

### Requirement: 资源配置必须使用正式单一端点

项目 MUST 使用一个显式配置的 TEngine/YooAsset 正式资源端点。系统 MUST NOT 配置备用端点、fallback URL、旧资源目录或测试环境地址作为运行时自动切换路径。

#### Scenario: 设置远端资源 URL

- **WHEN** ResourceModule 创建远端资源服务
- **THEN** URL 来自正式 `ResourceEndpoint`
- **AND** 该端点不得指向旧资源目录、旧配置数据或示例资源链路
- **AND** 系统 MUST NOT 在失败时自动切换到第二个 URL

#### Scenario: 未配置真实远端资源 URL

- **WHEN** HostPlayMode 或 WebPlayMode 读取远端资源端点
- **AND** `ResourceEndpoint` 为空
- **THEN** 系统直接报资源端点未配置错误
- **AND** 系统不得退回本地测试 URL

### Requirement: TEngine 启动流程不得替代 gameplay tick 权威

项目 MUST 保持 TEngine 作为启动、资源、热更和 frame source 底座。Gameplay tick 权威 MUST 位于 `GameplayTickSystem`。TEngine Procedure、TEngine FSM、TEngine TimerModule 和 TEngine UpdateDriver MUST NOT 直接 tick BTSMTL authoring object、单个 Character actor、SimulationKernel operation、Network Model session 或 Timeline preview runtime。

#### Scenario: 进入角色 runtime

- **WHEN** `ProcedureLoadAssembly` 完成
- **THEN** 系统进入 `GameApp.Entrance()`
- **AND** 项目 runtime MUST 初始化或取得正式 `GameplayTickSystem`
- **AND** TEngine frame source MUST 只驱动 `GameplayTickSystem`
- **AND** 角色本地逻辑 tick 和表现帧 MUST 由 `GameplayTickSystem` 调度

#### Scenario: TimerModule 不作为角色 tick

- **WHEN** 项目需要推进角色 gameplay
- **THEN** 系统 MUST NOT 使用 TEngine `TimerModule` callback 作为角色 logic tick
- **AND** 系统 MUST NOT 通过多个 timer 为不同角色分别 tick pipeline

### Requirement: BTSMTL authoring 和 runtime 主线必须保持统一

项目 MUST保持 BTSMTL 作为 Graph、StateMachine、ConditionRuleGraph 与 Timeline 的唯一 authoring source，并以该 source编译出的 CharacterSimulationProgram作为正式 Character runtime主线。TEngine FSM/GameEvent MUST不绕过 Program operation直接驱动角色状态、Timeline、WorldSolver或Action lifecycle。

#### Scenario: 状态机 Transition 求值

- **WHEN** Corin runtime判断 Transition
- **THEN** MUST执行由 BTSMTL ConditionRuleGraph编译的 operation
- **AND** MUST不使用 TEngine FSM或旧 StateMachineGraphRuntime替代

### Requirement: Fantasy 必须保持最小权威服务端边界

项目 MUST 保持 Fantasy 作为服务端和 Unity 客户端网络边界。TEngine 接入不得导入第二套网络服务端或协议路径。

#### Scenario: 初始化客户端网络

- **WHEN** 项目入口初始化网络边界
- **THEN** Unity 客户端使用现有 Fantasy.Unity 依赖
- **AND** 服务端仍位于 `3cDemo/Server`
- **AND** TEngine 示例网络路径不得进入运行时依赖

### Requirement: 示例目录不得成为运行时主线

项目 MUST 采用 TEngine 的 `Assets/GameScripts` 目录结构作为正式启动和热更结构。迁移后运行时代码不得导入 TEngine 示例 UI、示例战斗业务或示例配置表。

#### Scenario: 迁移 TEngine 示例 Procedure

- **WHEN** 项目需要启动流程
- **THEN** 流程类创建在 `Assets/GameScripts/Main/Procedure`
- **AND** 命名空间使用 `Procedure`
- **AND** 流程代码按项目需求重写，不迁入 Launcher UI 流程

#### Scenario: 迁移 TEngine 示例 UI

- **WHEN** 项目需要热更或启动 UI
- **THEN** UI 代码和 prefab 按本项目 UI 命名重建
- **AND** 示例 `Launcher/Resources/UIWindow` 不得作为运行时 UI 目录保留

#### Scenario: 迁移 TEngine 热更入口

- **WHEN** 项目需要热更入口
- **THEN** 入口程序集为 `Assets/GameScripts/HotFix/GameLogic/GameLogic.asmdef`
- **AND** 热更分层目录包含 `GameBase`、`GameProto`、`BattleCore` 和 `GameLogic`
- **AND** 主入口类型为全局 `GameApp`
- **AND** 示例 `BattleMainUI`、`LoginUI`、`GameModule`、`UIModule` 不得进入运行时主线

### Requirement: 稳定 runtime 必须归属 TEngine Main 目录

项目 MUST 将 AOT 稳定 runtime 放入 `Assets/GameScripts/Main/Runtime`，不得继续以 `Assets/Scripts` 作为正式代码根目录。

#### Scenario: 迁移稳定 runtime

- **WHEN** 稳定 runtime 目录迁移完成
- **THEN** BTSMTL 位于 `Assets/GameScripts/Main/Runtime/BTSMTL`
- **AND** 角色 pipeline 位于 `Assets/GameScripts/Main/Runtime/Character/Pipeline`
- **AND** Camera 位于 `Assets/GameScripts/Main/Runtime/Camera`
- **AND** Rendering 位于 `Assets/GameScripts/Main/Runtime/Rendering`
- **AND** 旧 `Assets/Scripts` 根目录不存在

### Requirement: 美术资源和热更采集必须按 TEngine/YooAsset 口径分离

项目 MUST 将美术资产工作区和热更资源采集目录分离，不得保留旧 `Assets/Art` 作为正式入口。

#### Scenario: 迁移美术资产根目录

- **WHEN** 美术资产目录迁移完成
- **THEN** 美术资产位于 `Assets/AssetArt`
- **AND** 热更资源采集目录位于 `Assets/AssetRaw`
- **AND** 旧 `Assets/Art` 根目录不存在

#### Scenario: 写入热更程序集文本资产

- **WHEN** HybridCLR 产出热更 DLL 文本资产
- **THEN** 文件放入 `Assets/AssetRaw/HotUpdate/DLL` 参与资源采集
- **AND** 文件名和热更程序集清单一致

### Requirement: TEngine Editor 构建能力必须作为无项目业务的底层服务

TEngine Editor MUST以显式 request 和结构化 result 提供 HotFix DLL、YooAsset Content 与 Unity Player 构建能力。request MUST要求调用方提供 BuildTarget、PackageVersion、BuildOutputRoot 以及构建 Player 时的 PlayerOutputPath；TEngine MUST不引用 ProductStartupProfile、ClientBuildVersion、MinimumClientBuildVersion、商业产品目录或项目 release manifest。项目层 MUST拥有产品版本、固定路径、staging、闭包校验和正式发布。

#### Scenario: 项目工作流调用 TEngine Content 服务

- **WHEN** CommercialClientBuildWorkflow 提交完整显式 Content request
- **THEN** TEngine MUST先执行配置要求的 HotFix DLL 编译与复制并运行一次 YooAsset 构建
- **AND** result MUST返回真实输出目录、BuildReport 和结构化失败
- **AND** TEngine MUST不自行改写 PackageVersion 或 OutputRoot

#### Scenario: TEngine 构建请求缺少正式参数

- **WHEN** request 缺少 PackageVersion、BuildOutputRoot 或 PlayerOutputPath
- **THEN** TEngine MUST在调用对应构建器前失败
- **AND** MUST不使用 `Builds`、`Bundles`、当前时间或平台默认目录补齐

#### Scenario: 通用 TEngine 菜单尝试产生产品产物

- **WHEN** 开发者查看可用构建菜单
- **THEN** 项目 MUST只公开项目拥有的商业客户端正式构建入口
- **AND** TEngine 通用菜单 MUST不直接写入正式 Content 或 Players 分区

