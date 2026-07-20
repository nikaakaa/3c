# dotrecast-navigation-world-solver Specification

## ADDED Requirements

### Requirement: Unity与普通.NET必须编译同一DotRecast Solver源码

系统 MUST安装一份固定DotRecast第三方源码revision并记录version、commit和license。Unity asmdef与net8.0工程 MUST编译同一第三方源码和同一项目adapter/solver源码，MUST不引用`Ref`运行路径、浮动NuGet或第二份源码副本。

#### Scenario: 两个Target构建Solver

- **WHEN** Unity与net8.0分别编译DotRecast模块
- **THEN** 两者 MUST记录相同第三方source revision、adapter revision和SolverId/version
- **AND** 任一revision不同 MUST形成不同且不可兼容的Solver identity

### Requirement: NavigationSurfaceArtifact必须是唯一Runtime导航数据

Unity Editor MUST从显式静态几何生成NavigationGeometryArtifact，固定build tool MUST生成canonical NavigationSurfaceArtifact。Unity MUST通过exact-byte wrapper加载该artifact，普通.NET MUST直接加载相同bytes。Runtime MUST不读取Scene/Mesh路径、navgeom或重新烘焙。

#### Scenario: Unity发布NavigationSurfaceAsset

- **WHEN** Editor将正式navsurface发布为Unity wrapper
- **THEN** wrapper bytes与ContentHash MUST和源artifact完全一致
- **AND** wrapper MUST不保存第二份可编辑build参数

### Requirement: DotRecast Solver必须使用Reconstruct世界状态

DotRecast Solver MUST从锁定artifact/profile与committed Body集合完整重建，并使用空SolverStatePayload。Create、Reconstruct和ResolveBatch MUST严格执行nearest-poly与surface容差校验；poly ref与query cache MUST不跨Tick持久化。

#### Scenario: Body无法定位Surface

- **WHEN** committed Body与nearest polygon距离超过profile容差
- **THEN** Solver MUST失败
- **AND** MUST不扩大查询、吸附远点或沿用旧poly

### Requirement: DotRecast Solver必须只执行局部Surface约束

Solver MUST将CharacterMotionRequest按既有space规则转换为target，执行MoveAlongSurface与height projection，并返回标准FinalBody、AppliedDisplacement和AppliedYaw。合法surface MUST产生Grounded/Below，边界截断 MUST产生Sides。Solver MUST不调用findPath、Crowd、TileCache、off-mesh link或actor collision。

#### Scenario: 位移越过边界

- **WHEN** target超出局部可达surface
- **THEN** Solver MUST返回截断后的body和Sides
- **AND** MUST不绕行或应用原始位移

### Requirement: DotRecast查询私有信息只能进入Diagnostics

start/final poly、area、filter、query status、visited count、clamp reason和耗时 MUST只进入结构化Solver Trace。Program、CharacterState、WorldState payload和通用WorldSolveResult MUST不增加DotRecast专属字段。

#### Scenario: 观察当前Query

- **WHEN** Diagnostics读取某Actor某Tick的WorldSolve
- **THEN** MUST能关联query与applied motion
- **AND** Gameplay MUST不读取该Trace决定下一Tick状态
