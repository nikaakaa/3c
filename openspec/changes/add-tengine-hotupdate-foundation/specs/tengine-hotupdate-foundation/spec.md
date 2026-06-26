## ADDED Requirements

### Requirement: TEngine 必须作为客户端热更和资源底座接入

项目 MUST 将 TEngine 定位为客户端基础设施，用于启动流程、热更程序集加载、资源包初始化、异步资源加载和基础模块管理。

#### Scenario: 启动项目 runtime

- **WHEN** Unity 场景中的 TEngine bootstrap 被启动
- **THEN** 系统初始化 TEngine 基础模块
- **AND** 系统初始化 YooAsset 默认资源包
- **AND** 系统进入项目自己的 runtime 入口
- **AND** 系统不得进入 TEngine 示例 `GameApp` 入口

#### Scenario: 加载热更入口

- **WHEN** 热更程序集清单可用
- **THEN** 启动流程加载配置中的热更程序集
- **AND** 启动流程调用本项目 `HotUpdateEntry`
- **AND** 启动流程不得引用示例 `BattleMainUI`、`LoginUI` 或示例业务入口

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

### Requirement: 资源配置必须使用正式主备端点

项目 MUST 允许 TEngine/YooAsset 的正式主资源端点和正式备用资源端点。备用资源端点只用于资源下载容灾，不得用于兼容旧配置、旧资源目录或测试环境。

#### Scenario: 设置远端资源 URL

- **WHEN** ResourceModule 创建远端资源服务
- **THEN** 主 URL 来自正式 `PrimaryResourceEndpoint`
- **AND** 备用 URL 来自正式 `FallbackResourceEndpoint`
- **AND** 两个端点都不得指向旧资源目录、旧配置数据或示例资源链路

### Requirement: TEngine 启动流程不得替代 gameplay tick 权威

项目 MUST 保持 `CharacterPipelineRunner` 作为 gameplay tick 权威。TEngine Procedure 只负责启动、资源、热更和项目入口装配。

#### Scenario: 进入角色 runtime

- **WHEN** `ProcedureEnterProjectRuntime` 完成
- **THEN** 系统进入 `HotUpdateEntry`
- **AND** 角色运行由 `CharacterPipelineRunner` 调度
- **AND** TEngine `UpdateDriver` 不得直接 tick Taco gameplay graph

### Requirement: Taco authoring 和 runtime 主线必须保持统一

项目 MUST 保持 Taco 作为 graph、state machine、transition rule 和 timeline authoring/runtime 主线。TEngine FSM 和 GameEvent 不得绕过 Taco 图。

#### Scenario: 状态机跳转求值

- **WHEN** 状态机需要判断 Transition
- **THEN** runtime 使用 Taco `TransitionRuleGraph` 求值
- **AND** runtime 不得用 TEngine FSM 替代 `StateMachineGraphRuntime`
- **AND** runtime 不得用 TEngine GameEvent 直接驱动状态跳转

### Requirement: Fantasy 必须保持最小权威服务端边界

项目 MUST 保持 Fantasy 作为服务端和 Unity 客户端网络边界。TEngine 接入不得导入第二套网络服务端或协议路径。

#### Scenario: 初始化客户端网络

- **WHEN** 项目入口初始化网络边界
- **THEN** Unity 客户端使用现有 Fantasy.Unity 依赖
- **AND** 服务端仍位于 `3cDemo/Server`
- **AND** TEngine 示例网络路径不得进入运行时依赖

### Requirement: 示例目录不得成为运行时主线

项目 MUST 将 TEngine 示例代码视为参考。迁移后运行时路径必须使用本项目命名和模块归属。

#### Scenario: 迁移 TEngine 示例 Procedure

- **WHEN** 项目需要启动流程
- **THEN** 流程类创建在 `Assets/Scripts/Bootstrap`
- **AND** 类名和命名空间使用项目口径
- **AND** 示例 `Assets/GameScripts/Procedure` 不得作为运行时路径保留

#### Scenario: 迁移 TEngine 示例 UI

- **WHEN** 项目需要热更或启动 UI
- **THEN** UI 代码和 prefab 按本项目 UI 命名重建
- **AND** 示例 `Launcher/Resources/UIWindow` 不得作为运行时 UI 目录保留
