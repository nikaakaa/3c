## Context

当前`ICharacterWorldSolver`已使用portable Float32 world request/result，但Unity外层的`IFloat32SimulationActorRegistration.WorldBodyBinding`直接返回`UnityCharacterControllerWorldBodyBinding`。这使WorldSolver在Core层可替换、在真实Unity装配层却不可替换。

本change只解决“同一导航世界和Solver如何同时服务Unity与普通.NET”。Network Model如何发送输入、服务端如何权威推进、客户端如何restore/replay属于后续change。

## Parallel Ownership

本change并行实施时唯一拥有：

- `Simulation/Unity`中的WorldBodyBinding抽象及所有registration签名迁移。
- 新增DotRecast package/source set、artifact、build tool、Solver和Unity Definition。
- `WorldFeature.NavigationSurface`与通用Composition feature校验。

它不得修改：

- ServerAuthoritative Pipeline/Source/Transport实现。
- Fantasy Outer协议、generated代码、Room Handler或Worker注册。
- DotRecast网络Scene与launch profile。

这些边界使本change可与`refactor-server-authoritative-host-portability`并行。

## Reference Boundary

参考工程客户端和服务端都会从当前position执行FindNearestPoly与MoveAlongSurface。它的客户端直接写Transform并定期上报position，服务端允许一定误差内接受client position。本change只采用共享导航查询，不采用pose同步和信任策略。

## Module Shape

```text
Packages/com.thirdperson.dotrecast/
  fixed upstream Core/Detour/Recast source

Assets/.../Simulation/Core/Float32/DotRecast/
  NavigationGeometryArtifact codec
  NavigationSurfaceArtifact codec
  DotRecastQueryProfile
  DotRecastNavigationWorldSolver

Assets/.../Simulation/Unity/
  Float32WorldBodyBinding
  UnityCharacterControllerWorldBodyBinding
  DotRecastStateWorldBodyBinding
  NavigationSurfaceAsset
  DotRecastWorldSolverDefinition

Tools/ThirdPersonSimulation.Portable/
  ThirdPersonSimulation.DotRecast.csproj
  NavigationBuildTool.csproj
```

Unity asmdef与net8.0 csproj都编译同一个adapter/solver源码目录。第三方DotRecast源码也只有一个仓库快照。Recast build代码只进入Editor/build tool；Player与runtime只编译Core/Detour。

## Binding Decision

```text
abstract Float32WorldBodyBinding
  BindingId
  ActorId
  InitialBody
  RequireValid

UnityCharacterControllerWorldBodyBinding
  CharacterController
  LogicRoot

DotRecastStateWorldBodyBinding
  explicit initial body
  no CharacterController
  no runtime Transform writer
```

Character Host和registration只依赖抽象binding。每个Solver Definition负责验证具体binding类型。类型不匹配直接阻止Composition，不搜索替代组件。

## Artifact Decision

```text
Unity explicit static geometry
  -> NavigationGeometryArtifact
  -> net8.0 NavigationBuildTool
  -> NavigationSurfaceArtifact canonical bytes
       -> Unity NavigationSurfaceAsset exact-byte wrapper
       -> ordinary .NET file loader
```

客户端与服务端不能分别烘焙。NavigationSurfaceArtifact锁定坐标profile、Map/World、build参数、area/filter、tile顺序和ContentHash。QueryProfile单独锁定nearest extents、projection tolerance、max displacement与filter选择。

## Solver Decision

每个Create、Reconstruct和ResolveBatch从committed Body position执行严格FindNearestPoly。投影距离超过profile容差、filter不允许或height失败都直接失败。成功后执行MoveAlongSurface与final height projection，并返回标准body/applied motion。

Solver不缓存跨Tickpoly，使用`WorldStatePersistenceMode.Reconstruct`和空SolverStatePayload。这样普通.NET与Unity只需同一artifact/profile/body即可重建，不需要扩张通用Snapshot或Network合同。

## Tradeoffs

- 同源源码保证两端执行实现一致，但增加仓库第三方源码和升级审计成本。
- 每TickFindNearestPoly增加查询成本，但避免opaque poly state跨roster和跨Host传输。
- NavigationSurface适合当前静态弱物理Demo，但不能替代未来KCC。

## Failure Policy

- artifact/profile/hash错误：Definition或loader失败。
- binding与Solver不匹配：Composition失败。
- spawn/body不在允许surface容差：Create/Reconstruct失败。
- 任一Actor query失败：整个batch失败，不发布部分WorldState。
- 不提供CC、Transform直写、扩大extents或无约束位移fallback。

## Implementation Order

1. 抽象WorldBodyBinding并迁移现有CC路径。
2. 安装唯一DotRecast源码依赖。
3. 建立geometry/navsurface artifact与build tool。
4. 建立Unity exact-byte wrapper。
5. 增加WorldFeature与Composition校验。
6. 实现共享Solver与Unity Definition。
7. 生成Corin正式artifact并删除临时路径。
