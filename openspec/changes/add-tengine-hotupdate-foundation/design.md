# Design

## 目标模型

```text
Unity Scene
  TEngineBootstrapper
    -> TEngine ModuleSystem
    -> Project Procedure
       -> InitResourcePackage
       -> CheckHotUpdateAssemblies
       -> LoadHotUpdateAssemblies
       -> EnterProjectRuntime
          -> CharacterPipelineRunner
          -> Taco authoring/runtime
          -> Fantasy client session boundary
```

TEngine 是底座，不是玩法主线。启动流程负责把资源系统、热更程序集和项目入口拉起来；角色运行之后，每帧 gameplay 决策仍然走 `CharacterPipelineRunner -> CharacterPipeline -> Taco Graph/Timeline`。

## 目录布局

### 第三方和框架包

```text
Packages/com.alex.tengine
  Runtime
  Editor
  Extension
  Libraries
  package.json
  LICENSE
  README.md
  CHANGELOG.md

Packages/UniTask
Packages/YooAsset
```

`com.alex.tengine` 放框架源码和 analyzer DLL。项目不得在这个包内写业务代码。若必须修改 TEngine 源码来符合项目规则，修改也保留在这个包内，并在设计中记录为正式 fork，不新增外部补丁脚本。

### 项目配置和 prefab

```text
Assets/Settings/TEngine
  TEngineProjectSettings.asset
  TEngineProcedureSettings.asset
  TEngineUpdateSettings.asset
  YooAssetSettings.asset

Assets/Prefabs/TEngine
  TEngineBootstrap.prefab
  TEngineUIRoot.prefab
```

这些是项目实例配置，不放在 package 内。`TEngineUpdateSettings.asset` 可以暴露正式主资源地址和正式备用资源地址，但不得暴露旧目录、旧数据源或测试环境兼容入口。

### 项目启动和热更代码

```text
Assets/Scripts/Bootstrap
  Runtime
    TEngineBootstrapper.cs
    ProjectModule.cs
    Procedures
      ProcedureLaunch.cs
      ProcedureInitializeResourcePackage.cs
      ProcedureCheckHotUpdate.cs
      ProcedureLoadHotUpdateAssemblies.cs
      ProcedureEnterProjectRuntime.cs

Assets/Scripts/HotUpdate
  Runtime
    HotUpdateAssemblyManifest.cs
    HotUpdateEntry.cs
```

`Bootstrap` 是 AOT 启动层，`HotUpdate` 是热更入口层。第一版可以先让 `HotUpdateEntry` 进入本地 demo runtime，不实现完整业务热更内容，但程序集边界必须是真实的。

### Fantasy 边界

```text
Assets/Scripts/Network/Fantasy
  Runtime
    FantasyClientBootstrap.cs
    FantasySessionFacade.cs
```

这里只放 Unity 客户端连接和会话边界。Fantasy 服务端仍在 `3cDemo/Server`，不通过 TEngine Procedure 改服务端结构。

## 依赖迁移

`Packages/manifest.json` 增加正式依赖：

- `com.code-philosophy.hybridclr`
- `com.unity.nuget.newtonsoft-json`

Embedded packages：

- `Packages/UniTask`
- `Packages/YooAsset`
- `Packages/com.alex.tengine`

保持现有依赖：

- `com.fantasy.unity`
- `com.unity.inputsystem`
- `com.unity.cinemachine`
- `com.unity.timeline`
- `com.unity.render-pipelines.universal`
- `com.kybernetik.animancer`
- `com.janooba.kcc`

不得新增 Addressables 作为第二套资源系统。YooAsset 是唯一热更资源管线。

## 程序集边界

建议拆分为：

```text
Project.Bootstrap.asmdef
Project.HotUpdate.asmdef
Project.Character.asmdef
Project.Network.Fantasy.asmdef
```

第一版可以先只建立 `Project.Bootstrap` 和 `Project.HotUpdate`，角色 pipeline change 实施时再新增 `Project.Character`。AOT 层只引用稳定底座和启动必须类型；HotUpdate 层引用 gameplay、UI、表现和可变业务。

Taco 资产类型和节点类型需要保持稳定。凡是 Unity 序列化资产直接引用的类型，默认放 AOT 或稳定程序集，不轻易放入热更程序集，避免热更后资产反序列化类型断裂。

## 启动流程

第一版流程：

1. `ProcedureLaunch`：初始化 TEngine 基础模块、日志、UpdateDriver。
2. `ProcedureInitializeResourcePackage`：初始化 YooAsset 默认包，使用编辑器模拟或正式构建模式。
3. `ProcedureCheckHotUpdate`：读取热更程序集清单；编辑器下可以走 EditorSimulateMode，但不新增旧配置 fallback。
4. `ProcedureLoadHotUpdateAssemblies`：加载 HotUpdate 程序集；HybridCLR 未启用时保持 AOT 入口，但这不是第二套业务路径，只是同一入口的编辑器可运行形态。
5. `ProcedureEnterProjectRuntime`：调用 `HotUpdateEntry.Enter()`，再由项目入口创建或唤醒 `CharacterPipelineRunner` 和后续 UI/网络边界。

## 资源端点

TEngine 原始 `UpdateSetting` 有主地址和 fallback 地址。本项目保留这两个资源下载端点，但它们都必须是正式资源配置：

```text
ProjectName
PrimaryResourceEndpoint
FallbackResourceEndpoint
PlayMode
PackageName
```

`FallbackResourceEndpoint` 只表达 CDN/资源服务容灾，不表达旧资源目录、旧配置表、旧 ActionSO 或示例资源链路。

## TEngine 示例代码取舍

### 迁移

- TEngine Runtime/Editor/Extension/Libraries。
- GameEvent SourceGenerator 和 Analyzer DLL。
- YooAsset 和 UniTask embedded packages。
- 运行所需 settings/prefab 结构，但改名为本项目配置。

### 参考后重写

- `GameEntry`
- `Procedure*`
- `GameApp`
- `GameModule`
- `UIModule`
- Launcher UI

### 不迁移

- 示例 BattleMainUI、LoginUI。
- 示例 Luban 生成代码和示例配置表。
- 示例 `Launcher/Resources/UIWindow` prefab。
- 示例网络或服务端路径。

## 与 active changes 的关系

### add-character-pipeline-runtime-entry

`CharacterPipelineRunner` 是玩法 tick 权威。TEngine `UpdateDriver` 只能作为 Unity lifecycle 工具或底层模块驱动，不直接 tick gameplay graph。`ProcedureEnterProjectRuntime` 创建或定位 runner 后，runner 自己按 character pipeline change 的规则调度。

### add-taco-transition-rule-graph-authoring

Transition 条件继续走 Taco `TransitionRuleGraph`。TEngine `GameEvent` 只可用于 UI/启动/资源进度通知，不可作为状态机跳转条件的旁路。

## 风险

- TEngine 默认示例路径较多，直接搬运会污染项目命名，需要严格按迁移规则裁剪。
- HybridCLR 需要生成和配置步骤，接入后 Unity 编译不等于热更完整可用。
- TEngine ResourceModule 默认有资源 fallback URL 概念，可以保留为正式备用资源端点；风险在于不能把它误用成旧资源兼容路径。
- 如果 Taco 节点类型放入热更程序集，资产序列化可能出现类型稳定性问题，因此第一版不要这么做。
