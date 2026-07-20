# 实现清单

## 依赖状态

- `refactor-server-authoritative-hybrid-runtime`已归档到`openspec/changes/archive/2026-07-17-refactor-server-authoritative-hybrid-runtime`，其要求已进入current specs。
- `add-shared-dotrecast-navigation-solver`任务全部完成，strict validation通过。
- `refactor-server-authoritative-host-portability`任务全部完成，strict validation通过。

## 修订后的部署结论

`Ref/94.移动同步前后端完整代码`在一个Fantasy Server OS进程中配置Account、Gate、MapControl和Map等多个Scene，权威移动由Map Scene执行。该参考只用于确认Scene级部署和所有权，不作为运行时依赖，也不迁入其客户端position权威、100ms移动段或服务端MoveComponent。

本change最终部署固定为：

```text
Unity Authority: Fantasy Server + Unity Authority Worker + Client A + Client B
DotRecast Authority: Fantasy Server(Gate Scene + DotRecast Authority Scene) + Client A + Client B
```

DotRecast环境不再建立外部Worker executable或Fantasy Console Client adapter。

## 共享DotRecast所有权

| 正式合同 | 所有程序集 | 路径 |
|---|---|---|
| `Float32WorldBodyBinding` | `ThirdPersonSimulation.Unity` | `Assets/GameScripts/Main/Runtime/Simulation/Unity/Float32WorldBodyBinding.cs` |
| `DotRecastStateWorldBodyBinding` | `ThirdPersonSimulation.DotRecast.Unity` | `Assets/GameScripts/Main/Runtime/Simulation/Unity/DotRecast/DotRecastStateWorldBodyBinding.cs` |
| `NavigationSurfaceAsset` | `ThirdPersonSimulation.DotRecast.Unity` | `Assets/GameScripts/Main/Runtime/Simulation/Unity/DotRecast/NavigationSurfaceAsset.cs` |
| `DotRecastWorldSolverDefinition` | `ThirdPersonSimulation.DotRecast.Unity` | `Assets/GameScripts/Main/Runtime/Simulation/Unity/DotRecast/DotRecastWorldSolverDefinition.cs` |
| `NavigationSurfaceArtifact`与codec | `ThirdPersonSimulation.DotRecast` | `Assets/GameScripts/Main/Runtime/Simulation/DotRecast/NavigationArtifacts.cs` |
| `DotRecastQueryProfile` | `ThirdPersonSimulation.DotRecast` | `Assets/GameScripts/Main/Runtime/Simulation/DotRecast/NavigationArtifacts.cs` |
| `ActorContactShape`与`ActorContactSolverConfiguration` | `ThirdPersonSimulation.DotRecast` | `Assets/GameScripts/Main/Runtime/Simulation/DotRecast/ActorContactContracts.cs` |
| `ActorContactSolver` | `ThirdPersonSimulation.DotRecast` | `Assets/GameScripts/Main/Runtime/Simulation/DotRecast/ActorContactSolver.cs` |
| `DotRecastWorldSolver` | `ThirdPersonSimulation.DotRecast` | `Assets/GameScripts/Main/Runtime/Simulation/DotRecast/DotRecastWorldSolver.cs` |

本change此前只装配这些合同。新增12.x任务只扩展唯一`DotRecastWorldSolver.ResolveBatch`与对应binding/identity：Navigation artifact和Recast surface query继续保持原实现，Actor硬接触不得在Authority Host或Client另建执行路径。

Unity Client Composition和Navigation exporter必须分别通过`ThirdPersonSimulation.DotRecast.Unity`与`ThirdPersonClient.Editor`接入；不得把DotRecast Unity adapter移回公共`ThirdPersonSimulation.Unity`，也不得让普通.NET Authority Scene引用任一Unity程序集。

## Actor接触实现与所有权

12.x实施前的旧链路是：

```text
DotRecastWorldSolver.ResolveBatch
  -> 按Actor循环
  -> SolveActor
  -> MoveAlongSurface
  -> 立即生成该Actor FinalBody
```

该旧链路已经删除。当前正式链路是：

```text
DotRecastWorldSolver.ResolveBatch
  -> 全部Actor Surface candidate
  -> 唯一portable ActorContactSolver
      -> 垂直区间过滤
      -> 稳定ActorId pair
      -> 初始有界去穿透
      -> 连续圆盘TOI
      -> 闭合法向裁剪与切向滑动
  -> Surface重新约束
  -> 最终pair最小间距校验
  -> 全部FinalBody与NextWorldState原子提交
```

`ActorContactShape`与接触求解配置归`ThirdPersonSimulation.DotRecast`的portable binding/solver identity所有；Unity asset只负责显式authoring，Fantasy manifest只负责canonical装配。Corin当前固定roster显式使用同一`Radius=0.3`、`Height=2`、`SkinWidth=0.08`，Solver使用固定4次迭代、`0.001`接触容差与`0.2`最大去穿透距离。形状、求解配置、静态Surface配置共同生成WorldConfigurationHash。不得新增独立Contact World、客户端专属去穿透、RVO硬碰撞或网络碰撞结果字段。

## Portable Authority所有权

| 正式合同 | 所有程序集 | 路径 |
|---|---|---|
| `ServerAuthoritativeAuthorityPipelineCatalog` | `ThirdPersonSimulation.ServerAuthoritative` | `Simulation/Core/Float32/Network/ServerAuthoritative/ServerAuthoritativeAuthorityPipelineCatalog.cs` |
| `ServerAuthoritativeAuthoritySourcePolicy` | `ThirdPersonSimulation.ServerAuthoritative` | `Simulation/Core/Float32/Network/ServerAuthoritative/ServerAuthoritativeAuthoritySourcePolicy.cs` |
| `IServerAuthoritativeAuthorityControlTransport` | `ThirdPersonSimulation.ServerAuthoritative` | `Simulation/Core/Float32/Network/ServerAuthoritative/ServerAuthoritativeAuthorityControlTransport.cs` |
| `ServerAuthoritativeAuthorityHostLaunchRequest` | `ThirdPersonSimulation.ServerAuthoritative` | `Simulation/Core/Float32/Network/ServerAuthoritative/ServerAuthoritativeAuthorityHostLaunchRequest.cs` |
| `ServerAuthoritativeAuthoritySourceRuntime` | `ThirdPersonSimulation.ServerAuthoritative.Transport` | `Simulation/Core/Float32/Network/ServerAuthoritative/Transport/ServerAuthoritativeAuthoritySourceRuntime.cs` |
| gameplay datagram endpoint与codec | `ThirdPersonSimulation.ServerAuthoritative.Transport` | `Simulation/Core/Float32/Network/ServerAuthoritative/Transport/ServerAuthoritativeDatagramEndpoint.cs`、`ServerAuthoritativeDatagramCodec.cs` |
| Network Checkpoint与canonical codec | `ThirdPersonSimulation.ServerAuthoritative` | `Simulation/Core/Float32/Network/ServerAuthoritative/ServerAuthoritativeNetworkCheckpoint.cs`、`ServerAuthoritativeCanonicalCodec.cs` |

Fantasy DotRecast Authority Scene必须从canonical manifest取得expected Authority PipelineIdentity，构造正式Authority Runtime Launcher，再由Launcher通过Host launch request进入唯一Float32 Composer。不得直接绕过Launcher，也不得建立第二Pipeline factory、Source queue/clock、checkpoint baseline、datagram codec或Session evaluator。

## Prediction aggregate依赖

Client Prediction固定复用`ThirdPersonSimulation.ServerAuthoritative`中的唯一`ServerAuthoritativePredictionState` aggregate root。该root内部唯一拥有`ServerAuthoritativePredictionConfirmationState`、`ServerAuthoritativePredictionHistory`、`ServerAuthoritativePredictionDispositionJournal`和无状态`ServerAuthoritativePredictionReconciler`，canonical状态统一由`ServerAuthoritativePredictionStateCodec`读写。

DotRecast change不得新增专属History、Correction、Journal、checkpoint DTO或codec，也不得让DotRecast Scene、Solver或Host直接取得上述内部模块。Correction Schedule、History Egress与Output Disposition继续只通过唯一Prediction State port访问aggregate root；DotRecast只替换正式Composition中的WorldSolver与Authority host装配。

## 当前Manifest实施状态

`DotRecastAuthoritySceneManifest`、canonical codec、loader和Unity exporter已经成为唯一正式链路。Manifest锁定HostProfile/HostId、Fantasy process与Authority Scene、Room、Program/Pipeline/Source、Solver/World/Navigation、固定双Actor roster、UDP端点、clock、Transport和diagnostics身份。

Manifest schema 3已直接保存每Actor接触形状、固定接触求解配置、静态Surface配置哈希和组合后的WorldConfigurationHash。Loader在注册Host route前重算接触与Solver identity；旧schema没有兼容reader，正式Build会直接重建并替换旧manifest。

Loader在创建Gate route与runtime前重读Program和Navigation artifact，核对exact bytes、路径边界与全部identity。Exporter只向Fantasy Server的`Authority/`正式目录写入manifest和artifact。旧`DotNetAuthorityWorkerManifest`类型、magic、schema reader、Control endpoint、WorkerId、外部process role和兼容转换均已删除。

## 协议与生成链

- 唯一Outer协议源：`3cDemo/Tools/NetworkProtocol/Outer/OuterMessage.proto`。
- 唯一Inner协议源：`3cDemo/Tools/NetworkProtocol/Inner/InnerMessage.proto`。
- 唯一生成工具：`3cDemo/Tools/ProtocolExportTool/Fantasy.ProtocolExportTool`，配置为同目录`ExporterSettings.json`。
- Server generated输出：`3cDemo/Server/Entity/Generate/NetworkProtocol`。
- Unity generated输出：`3cDemo/Client/3C_Client/Assets/Generated/NetworkProtocol`。
- generated文件不得手写，协议修改只发生在正式`.proto`源并重新导出。

Outer继续服务Client与External Unity Worker。DotRecast Authority Scene通过Inner/Address控制协议注册Gate Room；routine command/snapshot仍只走portable direct UDP。

## 当前Fantasy Server装配

`Fantasy.config`在同一process内显式配置Gate Scene与DotRecast Authority Scene。`ServerAuthoritativeRoom`只保存唯一`ServerAuthoritativeAuthorityHostRoute`，支持：

```text
ExternalUnityWorker -> Outer Session route
InProcessDotRecastScene -> Authority Scene Address route
```

Gate Scene只保存Room和route，不执行Program、Solver或读取gameplay datagram。DotRecast Authority Scene通过正式Inner/Address控制协议注册Gate，拥有manifest、Source、Pipeline、Solver、World、clock、data endpoint和runtime handle。Authority Scene发送的后续控制消息都携带自身Address，Gate按Host identity、Address与Session三者共同校验。

## 当前测试环境

- Bootstrap Scene：`Assets/Scenes/ServerAuthoritative/ServerAuthoritativeNetworkTestBootstrap.unity`。
- Unity Authority Scene：`Assets/Scenes/ServerAuthoritative/ServerAuthoritativeAuthorityWorker.unity`。
- Unity Client Scene：`Assets/Scenes/ServerAuthoritative/ServerAuthoritativeClient.unity`。
- DotRecast Client Scene：`Assets/Scenes/ServerAuthoritative/DotRecastAuthorityClient.unity`，不含`CharacterController`或CC binding。
- Bootstrap运行入口：`ServerAuthoritativeNetworkTestBootstrap`。
- Unity与DotRecast Client launch assets位于`Assets/Configs/Simulation/ServerAuthoritative/Launches`，Launch显式锁定HostProfile和route kind。
- Unity Authority/Prediction与DotRecast Prediction Composition位于`Assets/Configs/Simulation/ServerAuthoritative/Compositions`。
- Unity Authority测试入口拆为`Tools/3C/Network Tests/Unity Authority/Build`与`Run`，实现为`UnityAuthorityNetworkTestBuildAndRun`；Build不启动进程，Run不触发编译。
- DotRecast测试入口拆为`Tools/3C/Network Tests/DotRecast Authority/Build`与`Run`，实现为`DotRecastAuthorityNetworkTestBuildAndRun`；Build不启动进程，Run不触发编译。
- Player固定使用`StandaloneWindows64 + IL2CPP + Development + StrictMode`，构建结束恢复Editor原Scripting Backend。Unity Authority输出到`Build/Network/UnityAuthority/Player/`，DotRecast Authority输出到`Build/Network/DotRecastAuthority/Player/`。
- Fantasy Server固定使用Debug publish并带`--disable-build-servers /nr:false /p:UseSharedCompilation=false`，分别输出到`3cDemo/Server/Build/Network/UnityAuthority/Server/`和`3cDemo/Server/Build/Network/DotRecastAuthority/Server/`，随后立即执行`dotnet build-server shutdown`。
- Unity Authority发布目录的`Fantasy.config`只包含Gate Scene；DotRecast Authority发布目录只包含Gate Scene与DotRecast Authority Scene，避免同一Room同时出现外部Unity Worker与InProcess DotRecast Host。
- DotRecast Player构建完成后会按固定资产路径重新加载Build Profile，再导出Authority manifest与artifacts，避免跨`BuildPipeline.BuildPlayer`保留失效的Editor资产实例。
- 每个模型根目录只保存当前正式build manifest；manifest中的BuildId记录当前产物时间戳和实际编译选项，但不参与路径寻址。再次Build只替换同模型Player、Server、manifest与Authority artifacts，保留该模型Logs并禁止覆盖另一模型。
- Unity启动脚本为`3cDemo/Tools/ServerAuthoritative/Start-ServerAuthoritativeDemo.ps1 -StopExisting`，只消费Unity Authority当前manifest、Player与Server并启动四进程。
- DotRecast Server目录包含独立`Fantasy.config`、`Authority/DotRecastAuthorityScene.manifest`、Program和Navigation artifact；启动脚本为`3cDemo/Tools/ServerAuthoritative/Start-DotRecastAuthorityDemo.ps1 -StopExisting`，只启动Fantasy Server、Client A与Client B三个OS进程，并在启动前校验当前build manifest和Authority artifacts。
- DotRecast启动脚本通过进程环境变量`THIRDPERSON_DOTRECAST_AUTHORITY_SERVER_ROOT`把固定Server发布根交给Authority Scene；Fantasy命令行只保留框架正式支持的`--m Develop`。`-StopExisting`只停止带`dotrecast-authority-client`场景参数的Player和DotRecast发布目录内的Server，不触碰Unity Authority或Deterministic Rollback进程。
- 每次Run将日志写入对应模型`Build/Network/<Model>/Logs/<RunId>/`，Build不会删除既有日志。
- Unity Authority与Prediction Composition显式保存同一`WorldId + MapId + WorldRevision`；实际World Solver生成World Configuration、Navigation Surface和Query Profile身份，并通过Authority register与Client join锁定。
- DotRecast Prediction Composition与Authority Scene manifest同样锁定Solver、World、Map、NavigationSurface和QueryProfile；Client join会核对Authority返回的World与Pipeline Solver identity。Deterministic Rollback由其独立change提供第三个模型专属入口，不进入本change。

## Diagnostics状态

- `DotRecastAuthoritySceneDiagnostics`使用固定512条环形记录，暴露HostProfile、Host route、Program、Backend、Pipeline、Source、Solver、World、Map、NavigationSurface、QueryProfile和Scene identity。
- Authority Source通过正式`SimulationModelTraceRecord`提交authority tick、input ack、snapshot sequence、队列与transport状态；Boundary、Pipeline、Operation和World仍复用公共diagnostics sink。
- Prediction继续复用既有correction diagnostics，显示position/yaw error、restore tick和replayed ticks。
- Gate Room fail-stop要求每个调用点显式提交channel与Actor，日志同时记录tick、HostProfile、HostId、error code和reason。
- Actor pair、TOI、法向裁剪、去穿透、最终间距、surface重新约束和失败原因通过既有World Solver Trace只读发布；诊断不保存跨Tick接触cache，也不反向修改World。

## 网络模型与具体Solver边界

`ServerAuthoritativeHybridModelDefinition`只拥有协议、Prediction/Authority Pipeline Pair要求、同步策略、Numeric/ABI/Backend要求和Solver能力要求。具体Program Runtime、Backend与Solver由Client Composition或Authority Scene manifest选择，并把实际descriptor交给Source完成Pipeline与握手身份编译。

- Unity Authority Composition选择`UnityCharacterControllerWorldSolverDefinition`。
- DotRecast Prediction Composition选择`DotRecastWorldSolverDefinition`。
- Fantasy Authority Scene manifest选择同一共享`DotRecastWorldSolver`实现。
- Network Model资产不得恢复`m_ProgramRuntime`、`m_PassBackend`、`m_UnitySolver`或任何具体执行组件引用。

## 禁止与删除边界

本change不得新增或保留：

- 外部DotRecast Worker executable、Fantasy Console Client adapter或Worker到Gate的自定义IPC。
- DotRecast专属Program、Pipeline、Source、Composer、History、Correction、Checkpoint、baseline或packet codec。
- Unity YAML manifest reader、ScriptableObject manifest镜像、旧Worker schema reader或临时manifest。
- 第二Session Host、第二Authority clock、第二input queue或第二replication lowering。
- Client position、Transform、Body、applied displacement或DotRecast query result作为authority输入。
- KCP/Inner gameplay fallback、DotRecast专属Outer协议或手写generated消息。
- DotRecast Client Scene中的`CharacterController`、CC binding、CC Solver或`CharacterController.Move`路径。
- Gate Scene中的Program runtime、Kernel、WorldSolver、gameplay datagram读取或Presentation。
- Authority Scene中的Unity、UnityEngine、CharacterController、Client control Session或Presentation。
- 独立Actor Contact World、第二World state、DetourCrowd/RVO硬碰撞、BEPU/Jitter/Box2D运行时物理世界或Client专属接触修正。
