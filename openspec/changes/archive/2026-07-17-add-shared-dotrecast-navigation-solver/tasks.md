## 1. 锁定边界与依赖

- [x] 1.1 盘点WorldBodyBinding、Actor registration、Character Host、Authority Host和Preview的具体CC引用。
- [x] 1.2 盘点WorldFeature、Solver Definition descriptor和portable Composer校验链。
- [x] 1.3 盘点参考工程客户端与服务端NavMesh加载、FindNearestPoly和MoveAlongSurface入口。
- [x] 1.4 记录不得迁入的client pose权威、Transform真值和100ms移动段路径。
- [x] 1.5 锁定本change不修改ServerAuthoritative Source/Pipeline、Fantasy协议和网络Scene。
- [x] 1.6 锁定与并行Host portability change不重叠的文件所有权。

## 2. 抽象WorldBodyBinding

- [x] 2.1 定义抽象`Float32WorldBodyBinding`。
- [x] 2.2 迁移BindingId、ActorId、InitialBody和通用校验。
- [x] 2.3 让现有CC binding继承抽象binding。
- [x] 2.4 保持CharacterController和LogicRoot只存在于CC具体binding。
- [x] 2.5 新增state-only DotRecast binding。
- [x] 2.6 禁止state-only binding引用CharacterController和Rigidbody。
- [x] 2.7 将`IFloat32SimulationActorRegistration`改为抽象binding。
- [x] 2.8 迁移CharacterSimulationActorRegistration。
- [x] 2.9 迁移CharacterPipelineHost序列化引用。
- [x] 2.10 迁移Authority Actor registration与Host签名。
- [x] 2.11 迁移Preview registration签名。
- [x] 2.12 让CC Solver Definition严格要求CC binding。
- [x] 2.13 保持现有CC资产引用与BindingId不变。
- [x] 2.14 删除通用registration中的具体CC类型泄漏。

## 3. 安装唯一DotRecast源码

- [x] 3.1 核对参考包上游版本、commit和license。
- [x] 3.2 锁定正式DotRecast源码revision。
- [x] 3.3 安装仓库内固定第三方源码与license metadata。
- [x] 3.4 建立Core Unity runtime asmdef。
- [x] 3.5 建立Detour Unity runtime asmdef。
- [x] 3.6 建立Recast Editor/build-only asmdef。
- [x] 3.7 新建portable DotRecast adapter source目录与asmdef。
- [x] 3.8 新建net8.0 DotRecast csproj并链接同一adapter源码。
- [x] 3.9 让net8.0工程编译同一第三方Core/Detour源码。
- [x] 3.10 新建NavigationBuildTool csproj并额外编译Recast源码。
- [x] 3.11 禁止Player引用Recast build、Crowd和TileCache。
- [x] 3.12 删除`Ref`路径、浮动NuGet、临时DLL和第二源码副本。

## 4. 建立NavigationGeometryArtifact

- [x] 4.1 定义magic、schema和canonical codec。
- [x] 4.2 定义MapId、WorldRevision、Scene revision和GeometryHash。
- [x] 4.3 定义统一坐标profile。
- [x] 4.4 定义稳定mesh source排序。
- [x] 4.5 定义vertex、index、transform和area编码。
- [x] 4.6 拒绝非法数值、退化triangle和越界index。
- [x] 4.7 建立显式Scene/layer/static/area authoring配置。
- [x] 4.8 建立Unity Editor geometry exporter。
- [x] 4.9 禁止Runtime Scene扫描和默认layer猜测。
- [x] 4.10 写入Library正式navgeom路径。
- [x] 4.11 写后重读并核对GeometryHash。

## 5. 建立NavigationSurfaceArtifact

- [x] 5.1 定义magic、schema、source revision和ContentHash。
- [x] 5.2 保存Map/World/Geometry与坐标identity。
- [x] 5.3 定义Recast build profile。
- [x] 5.4 定义稳定area catalog。
- [x] 5.5 定义稳定filter catalog。
- [x] 5.6 定义canonical DotRecastQueryProfile。
- [x] 5.7 保存nearest extents。
- [x] 5.8 保存projection与height tolerance。
- [x] 5.9 保存max displacement和boundary阈值。
- [x] 5.10 让BuildTool严格读取navgeom。
- [x] 5.11 使用固定Recast源码构建navmesh。
- [x] 5.12 稳定排序tile。
- [x] 5.13 保存navmesh params、bounds和tile bytes。
- [x] 5.14 计算ContentHash。
- [x] 5.15 写后重读并核对全部identity。
- [x] 5.16 建立portable runtime loader。
- [x] 5.17 拒绝旧schema、错误hash和非法tile。

## 6. 建立Unity exact-byte wrapper

- [x] 6.1 定义NavigationSurfaceAsset exact-byte wrapper。
- [x] 6.2 禁止wrapper保存第二份可编辑参数。
- [x] 6.3 建立Editor publish service。
- [x] 6.4 Publish前通过portable loader校验artifact。
- [x] 6.5 Publish后重读wrapper核对exact bytes与hash。
- [x] 6.6 生成Corin正式NavigationSurfaceAsset。
- [x] 6.7 禁止Unity Player读取navgeom或Runtime烘焙。

## 7. 扩展Feature与Composition校验

- [x] 7.1 增加WorldFeature.NavigationSurface。
- [x] 7.2 更新known-value和canonical codec。
- [x] 7.3 将features加入Solver Definition descriptor。
- [x] 7.4 将features加入runtime descriptor匹配。
- [x] 7.5 将required features加入Composition校验。
- [x] 7.6 保持Program只声明通用WorldCapability。
- [x] 7.7 建立artifact/profile/Solver组成的WorldConfigurationHash。
- [x] 7.8 保持World identity不进入ProgramHash和PipelineHash。

## 8. 实现共享DotRecast Solver

- [x] 8.1 建立稳定SolverId、version和source identity。
- [x] 8.2 声明通用capability与NavigationSurface feature。
- [x] 8.3 从canonical bytes创建DtNavMesh与query。
- [x] 8.4 按stable ActorId锁定roster。
- [x] 8.5 Create时严格定位initial body。
- [x] 8.6 Reconstruct时严格定位committed body。
- [x] 8.7 使用Reconstruct persistence和空payload。
- [x] 8.8 Resolve前校验Tick、World、Actor和有限数值。
- [x] 8.9 校验每Tick最大查询位移。
- [x] 8.10 转换ActorLocal/World displacement。
- [x] 8.11 从BeforeBody执行FindNearestPoly。
- [x] 8.12 拒绝超过projection tolerance的结果。
- [x] 8.13 执行MoveAlongSurface。
- [x] 8.14 校验visited结果和final polygon。
- [x] 8.15 执行final height projection。
- [x] 8.16 计算FinalBody、AppliedDisplacement、Yaw和velocity。
- [x] 8.17 设置Grounded、Below与边界Sides。
- [x] 8.18 发布query与clamp diagnostics。
- [x] 8.19 禁止跨Tickpoly cache和非空payload。
- [x] 8.20 禁止findPath、Crowd、TileCache和fallback。
- [x] 8.21 任一Actor失败时拒绝整个batch。

## 9. 建立Unity Solver Definition并清理

- [x] 9.1 新建DotRecastWorldSolverDefinition。
- [x] 9.2 显式引用NavigationSurfaceAsset与QueryProfile。
- [x] 9.3 从actual bytes构建descriptor identity。
- [x] 9.4 严格要求state-only binding。
- [x] 9.5 拒绝CC binding和identity不匹配。
- [x] 9.6 使用共享portable factory创建Solver。
- [x] 9.7 保持Definition不复制query算法和parser。
- [x] 9.8 删除临时parser、旧artifact和一次性migrator。
- [x] 9.9 更新project.md记录共享DotRecast Solver边界。

## 10. 编译与严格校验

- [x] 10.1 编译portable Core与Float32工程并带规定build server参数。
- [x] 10.2 编译DotRecast类库与NavigationBuildTool并带相同参数。
- [x] 10.3 编译Unity Runtime/Editor相关工程并带相同参数。
- [x] 10.4 编译后立即执行`dotnet build-server shutdown`。
- [x] 10.5 运行`openspec validate add-shared-dotrecast-navigation-solver --strict --no-interactive`。
- [x] 10.6 运行`openspec validate --all --strict --no-interactive`并解决本change冲突。
