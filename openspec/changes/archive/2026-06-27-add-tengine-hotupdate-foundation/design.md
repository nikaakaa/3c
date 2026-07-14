# Design

## 目标模型

```text
Unity Scene
  GameEntry
    -> TEngine ModuleSystem
    -> Procedure
       -> ProcedureInitPackage
       -> ProcedureInitResources
       -> ProcedureLoadAssembly
       -> ProcedureStartGame
          -> GameApp.Entrance()
          -> CharacterPipelineRunner
          -> BTSMTL authoring/runtime
          -> Fantasy client session boundary
```

TEngine 是底座，不是玩法主线。启动流程负责把资源系统、热更程序集和项目入口拉起来；角色运行之后，每帧 gameplay 决策仍然走 `CharacterPipelineRunner -> CharacterPipeline -> BTSMTL Graph/Timeline`。

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
Assets/GameScripts
  Main
    GameEntry.cs
    Procedure
      ProcedureBase.cs
      ProcedureLaunch.cs
      ProcedureInitPackage.cs
      ProcedureInitResources.cs
      ProcedureLoadAssembly.cs
      ProcedureStartGame.cs
      HotUpdateAssemblyLoader.cs
      HybridClrRuntimeBridge.cs
    Runtime
      BTSMTL
      Character/Pipeline
      Camera
      Rendering
  HotFix
    GameBase
      GameBase.asmdef
    GameProto
      GameProto.asmdef
    BattleCore
      BattleCore.asmdef
    GameLogic
      GameLogic.asmdef
      GameApp.cs
      HotUpdateAssemblyManifest.cs
      Network/Fantasy
        FantasyClientBootstrap.cs
        FantasySessionFacade.cs
```

`Main` 是 AOT 启动层，`HotFix` 是热更程序集层。`GameBase` 承载热更基础业务类型，`GameProto` 承载后续协议和配置类型，`BattleCore` 承载不直接依赖 Unity 序列化资产的战斗核心，`GameLogic` 承载项目入口、局内流程和 Fantasy 客户端边界。第一版 `GameApp.Entrance()` 只进入最小 runtime 边界，不实现完整角色控制业务，但程序集边界必须是真实的。

### 美术和热更资源目录

```text
Assets/AssetArt
  Animation
  Animator
  ArtRes
  Mat
  Model
  Tex
  Stylized Grass Shader

Assets/AssetRaw
  HotUpdate/DLL
```

`AssetArt` 是美术资产工作区，保留导入素材、动画、材质、模型、贴图和第三方美术包。`AssetRaw` 是热更资源采集目录，第一版只放热更程序集 `.dll.bytes`，后续 YooAsset Collector 也从这里收敛运行时原始资源入口。真正的构建输出是 YooAsset 构建产物、StreamingAssets 或 CDN 目录。旧 `Assets/Art` 不保留。

### Fantasy 边界

```text
Assets/GameScripts/HotFix/GameLogic/Network/Fantasy
  FantasyClientBootstrap.cs
  FantasySessionFacade.cs
```

这里只放 Unity 客户端连接和会话边界，并随 `GameLogic` 热更程序集进入项目入口。Fantasy 服务端仍在 `3cDemo/Server`，不通过 TEngine Procedure 改服务端结构。

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

第一版拆分为：

```text
Assembly-CSharp（GameScripts/Main）
GameBase.asmdef
GameProto.asmdef
BattleCore.asmdef
GameLogic.asmdef
Project.Character.asmdef
```

第一版不建立 `Project.Bootstrap` 和 `Project.Network.Fantasy` 独立程序集，避免把 TEngine 原始 `GameScripts` 结构拆成多条项目路径。角色 pipeline change 实施时再新增或迁移 `Project.Character` 稳定程序集。AOT 层只引用稳定底座和启动必须类型；`GameLogic` 热更层引用 `GameBase`、`GameProto`、`BattleCore` 和可变业务。

BTSMTL 资产类型和节点类型需要保持稳定。凡是 Unity 序列化资产直接引用的类型，默认放 AOT 或稳定程序集，不轻易放入热更程序集，避免热更后资产反序列化类型断裂。

## 启动流程

第一版流程：

1. `ProcedureLaunch`：进入启动流程。
2. `ProcedureInitPackage`：初始化 YooAsset 默认包，使用编辑器模拟或正式构建模式。
3. `ProcedureInitResources`：读取资源版本和 manifest，必要时下载资源；编辑器模拟模式直接进入程序集加载。
4. `ProcedureLoadAssembly`：加载 `GameBase.dll`、`GameProto.dll`、`BattleCore.dll`、`GameLogic.dll`；编辑器下从当前 AppDomain 查找同一热更程序集。
5. `ProcedureStartGame`：保持 TEngine 流程状态，并由 `ProcedureLoadAssembly` 调用 `GameApp.Entrance()` 进入项目 runtime。

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

当前默认配置不填本地测试 URL。未配置真实 CDN 时，Host/Web 模式在读取资源端点时直接报错；EditorSimulateMode 不依赖远端资源端点。

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
- Launcher UI

### 不迁移

- 示例 BattleMainUI、LoginUI。
- 示例 Luban 生成代码和示例配置表。
- 示例 `Launcher/Resources/UIWindow` prefab。
- 示例网络或服务端路径。

## 与 active changes 的关系

### add-character-pipeline-runtime-entry

`CharacterPipelineRunner` 是玩法 tick 权威。TEngine `UpdateDriver` 只能作为 Unity lifecycle 工具或底层模块驱动，不直接 tick gameplay graph。`GameApp.Entrance()` 创建或定位 runner 后，runner 自己按 character pipeline change 的规则调度。

## 角色控制代码归属规划

角色控制不直接塞进 `GameEntry` 或 Procedure。启动层只做底座装配，热更入口只做 runtime 进入。

稳定层放 AOT：

```text
Assets/GameScripts/Main/Runtime/Character/Pipeline
Assets/GameScripts/Main/Runtime/BTSMTL
Assets/GameScripts/Main/Runtime/Camera
Assets/GameScripts/Main/Runtime/Rendering
```

这些目录承载 Unity 序列化资产类型、BTSMTL 节点类型、相机适配、表现组件和 `CharacterPipelineRunner` 这类稳定入口。业务原因是这些类型会被场景、Prefab、Timeline 或 BTSMTL 图资产直接引用，频繁热更会增加反序列化断裂风险。

可热更层放 `GameLogic`：

```text
Assets/GameScripts/HotFix/GameLogic/Character
Assets/GameScripts/HotFix/GameLogic/Combat
Assets/GameScripts/HotFix/GameLogic/Network/Fantasy
```

这些目录承载角色策略、动作请求编排、局内 demo 流程、Fantasy 客户端会话边界和不被 Unity 资产直接序列化的业务服务。业务原因是这些代码最可能随 demo 展示节奏变化，放入 `GameLogic` 后可以跟热更程序集一起迭代。

当前第一版只建立 `GameApp.Entrance()` 到 Fantasy 客户端边界，不创建第二套 gameplay tick。后续角色代码接入时，链路保持：

```text
GameApp.Entrance()
  -> Character runtime bootstrap
  -> CharacterPipelineRunner
  -> CharacterPipeline
  -> BTSMTL Graph / Timeline
```

### add-btsmtl-transition-rule-graph-authoring

Transition 条件继续走 BTSMTL `TransitionRuleGraph`。TEngine `GameEvent` 只可用于 UI/启动/资源进度通知，不可作为状态机跳转条件的旁路。

## 风险

- TEngine 默认示例路径较多，直接搬运会污染项目命名，需要严格按迁移规则裁剪。
- HybridCLR 需要生成和配置步骤，接入后 Unity 编译不等于热更完整可用。
- TEngine ResourceModule 默认有资源 fallback URL 概念，可以保留为正式备用资源端点；风险在于不能把它误用成旧资源兼容路径。
- 如果 BTSMTL 节点类型放入热更程序集，资产序列化可能出现类型稳定性问题，因此第一版不要这么做。
