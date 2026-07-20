# Change: 增加Unity与普通.NET共享的DotRecast导航世界求解

## Why

当前Float32 WorldSolver合同本身是portable的，但Unity Actor registration和Character Host仍直接依赖`UnityCharacterControllerWorldBodyBinding`，导致任何客户端非CC Solver都必须绕过正式Host或复制registration链。同时项目尚无一份可由Unity与普通.NET共同加载的canonical NavigationSurfaceArtifact，也没有共享DotRecast Solver实现。

`Ref/94.移动同步前后端完整代码`证明客户端与服务端可以加载同源NavMesh并共同使用nearest-poly、move-along-surface和height projection约束角色移动。本change只提取这项世界求解能力，不接Fantasy、Network Model、Authority Worker或双客户端Demo。

本change交付一套独立可装配的共享DotRecast Solver：Unity和net8.0编译同一第三方源码与同一项目adapter；Unity Editor离线生成一次canonical navsurface；Unity通过exact-byte wrapper加载，普通.NET直接读取同一artifact；Solver从committed Body严格重建surface查询，不持久化current poly。

## Dependencies

- `refactor-character-simulation-core`、`refactor-character-semantic-frontend-artifact`与`refactor-gameplay-session-composition-boundary` MUST已归档。
- 本change只依赖current Float32 Program、WorldSolver和Composition合同，不依赖`refactor-server-authoritative-hybrid-runtime`归档。
- `Ref`只作为行为参考，不得成为编译、运行或部署依赖。

## What Changes

- 将固定DotRecast Core/Detour/Recast源码安装为仓库唯一依赖源，记录version、commit和license；Unity与net8.0编译同一份源码。
- 新增portable `ThirdPersonSimulation.DotRecast` source set、Unity asmdef和net8.0 csproj，唯一拥有artifact codec、query profile、类型转换与`DotRecastNavigationWorldSolver`。
- 新增`NavigationGeometryArtifact`、`NavigationSurfaceArtifact`和普通.NET NavigationBuildTool。
- 新增Unity exact-byte `NavigationSurfaceAsset`及正式publish service，确保Unity wrapper和普通.NET artifact字节与ContentHash一致。
- 将`IFloat32SimulationActorRegistration`、`CharacterPipelineHost`、Authority/Preview registration中的具体CC binding提升为抽象`Float32WorldBodyBinding`。
- 保留`UnityCharacterControllerWorldBodyBinding`为CC具体实现，并新增不写Transform的`DotRecastStateWorldBodyBinding`。
- 新增`WorldFeature.NavigationSurface`及Definition/runtime/Composition identity校验。
- 新增Unity `DotRecastWorldSolverDefinition`，使任意显式Composition可以选择共享DotRecast Solver。
- Solver每次从committed Body执行严格nearest-poly、move-along-surface和height projection；World使用Reconstruct模式与空SolverStatePayload。
- 生成Corin正式NavigationSurfaceArtifact和Unity wrapper，但不在本change创建网络Scene、Worker或协议字段。

## Non-Goals

- 不创建Network Model、Authority Worker、Fantasy消息、Room Handler、network checkpoint或客户端纠偏逻辑。
- 不复制参考工程的client position权威、100ms移动段、Transform gameplay真值或同步协议。
- 不实现KCC、capsule sweep、台阶、动态障碍、moving platform、actor collision、findPath、Crowd或TileCache。
- 不修改现有Unity Authority环境使用CC的业务选择。
- 不创建DotRecast专用Character Host、Input链或Presentation链。

## Current Spec Comparison

- `gameplay-simulation-session-composition`要求WorldSolver可显式选择，但Actor registration仍具体暴露CC binding。本change修改该Requirement，使binding成为抽象合同并由Solver Definition严格选择具体实现。
- `character-motion-simulation-boundary`已要求DotRecast不能冒充完整KCC。本change增加NavigationSurface feature、共享Solver和Reconstruct语义，不改变Program motion来源。
- `server-authoritative-hybrid-sync-model`不在本change修改；后续网络change只消费这里交付的Solver、artifact与identity。

## Impact

- 新能力：`dotrecast-navigation-world-solver`。
- 修改能力：`gameplay-simulation-session-composition`、`character-motion-simulation-boundary`。
- Unity：Actor binding抽象、DotRecast Solver Definition、NavigationSurfaceAsset与Editor构建入口。
- Portable：DotRecast source set、artifact codec、query profile、solver与build tool。
- 删除：Unity/NET双版本依赖、具体CC registration泄漏、current-poly持久状态、`Ref`运行依赖和Runtime重烘焙入口。
