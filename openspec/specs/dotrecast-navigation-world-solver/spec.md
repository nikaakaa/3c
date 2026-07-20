# dotrecast-navigation-world-solver Specification

## Purpose
定义 Unity 与普通 .NET 共享的 DotRecast 导航世界资产、查询、Actor 接触和 ResolveBatch 合同，使 Prediction 与 Authority 使用同一 portable 求解语义。
## Requirements
### Requirement: Unity与普通.NET必须编译同一DotRecast Solver源码

系统 MUST安装一份固定DotRecast第三方源码revision并记录version、commit和license。Unity asmdef与net8.0工程 MUST编译同一第三方源码和同一项目adapter/solver源码，MUST不引用`Ref`运行路径、浮动NuGet或第二份源码副本。

#### Scenario: 两个Target构建Solver

- **WHEN** Unity与net8.0分别编译DotRecast模块
- **THEN** 两者 MUST记录相同第三方source revision、adapter revision和SolverId/version
- **AND** 任一revision不同 MUST形成不同且不可兼容的Solver identity

### Requirement: NavigationSurfaceArtifact必须是唯一Runtime导航数据

Unity Editor MUST从显式canonical静态地图Prefab生成NavigationGeometryArtifact，固定build tool MUST生成canonical NavigationSurfaceArtifact。需要展示该地图的Unity测试Scene MUST实例化同一地图Prefab，MUST不分别保存地面、墙或其Transform。Unity MUST通过exact-byte wrapper加载该artifact，普通.NET MUST直接加载相同bytes。Runtime MUST不读取Scene、Prefab、Mesh路径、navgeom或重新烘焙。旧的独立导航源Scene MUST删除，MUST不作为fallback或第二几何源保留。

#### Scenario: Unity发布NavigationSurfaceAsset

- **WHEN** Editor将正式navsurface发布为Unity wrapper
- **THEN** wrapper bytes与ContentHash MUST和源artifact完全一致
- **AND** wrapper MUST不保存第二份可编辑build参数

#### Scenario: 作者调整测试地图墙体

- **WHEN** 作者修改canonical测试地图Prefab中的墙体几何或Transform并重新发布NavigationSurface
- **THEN** 客户端可见墙体与NavigationGeometryArtifact MUST来自同一Prefab revision
- **AND** 跨越墙体的局部Surface位移 MUST被NavigationSurface边界截断

### Requirement: DotRecast Solver必须使用Reconstruct世界状态

DotRecast Solver MUST从锁定artifact/profile与committed Body集合完整重建，并使用空SolverStatePayload。Create、Reconstruct和ResolveBatch MUST严格执行nearest-poly与surface容差校验；poly ref与query cache MUST不跨Tick持久化。

#### Scenario: Body无法定位Surface

- **WHEN** committed Body与nearest polygon距离超过profile容差
- **THEN** Solver MUST失败
- **AND** MUST不扩大查询、吸附远点或沿用旧poly

### Requirement: DotRecast查询私有信息只能进入Diagnostics

start/final poly、area、filter、query status、visited count、clamp reason和耗时 MUST只进入结构化Solver Trace。Program、CharacterState、WorldState payload和通用WorldSolveResult MUST不增加DotRecast专属字段。

#### Scenario: 观察当前Query

- **WHEN** Diagnostics读取某Actor某Tick的WorldSolve
- **THEN** MUST能关联query与applied motion
- **AND** Gameplay MUST不读取该Trace决定下一Tick状态

### Requirement: DotRecastWorldSolver必须在唯一ResolveBatch内闭合静态Surface与Actor硬接触

`DotRecastWorldSolver.ResolveBatch` MUST先对同一SimulationStep的全部Actor执行nearest-poly、`MoveAlongSurface`与height projection并生成静态Surface candidate，再由唯一portable ActorContactSolver对完整candidate集合执行Actor硬接触，最后复用同一Surface查询重新约束接触修正并一次性生成全部FinalBody、WorldSolveResult与NextWorldState。任一Actor或pair失败时整个batch MUST失败，MUST不发布部分Body。Recast查询阶段 MUST不调用findPath、Crowd、TileCache或off-mesh link；ActorContactSolver MUST不创建第二World state、第二提交入口或跨Tick接触cache。

#### Scenario: 两个Actor相向移动

- **WHEN** 两个垂直区间重叠的Actor在同一batch内相向移动且圆盘轨迹将在Tick结束前相交
- **THEN** ActorContactSolver MUST使用连续扫掠求得接触时刻
- **AND** MUST裁剪双方剩余位移的闭合法向分量、保留合法切向分量
- **AND** 两个最终Body MUST满足配置的最小接触间距

#### Scenario: 移动Actor撞向静止Actor

- **WHEN** Actor A主动移动到静止Actor B的接触形状内
- **THEN** 通用`SolidBodyBlock`响应 MUST裁剪Actor A的闭合位移
- **AND** MUST不把Actor B的零位移转换成通用推行位移

#### Scenario: 接触修正靠近静态墙体

- **WHEN** Actor pair修正会把任一Actor推出合法Navigation Surface或穿入静态边界
- **THEN** 修正结果 MUST通过同一DotRecast Surface查询重新约束
- **AND** 固定迭代结束后仍无法同时满足Surface与接触约束时整个batch MUST失败

### Requirement: Actor接触形状与求解配置必须是显式World Identity

每个DotRecast Actor binding MUST显式保存Radius、Height与SkinWidth；WorldSolver配置 MUST显式保存固定迭代次数、接触容差与最大去穿透距离。上述数据 MUST进入binding identity、Authority Scene manifest canonical bytes、Solver/World configuration identity和握手兼容性。系统 MUST不从Navigation build AgentRadius/AgentHeight、Unity Collider、Scene Transform、未序列化默认值或网络包猜测接触配置。当前只允许`SolidBodyBlock`响应；动作专属推行、击退、ghost或队伍穿透 MUST不在Solver内按状态名、Tag或动画producer硬编码。

#### Scenario: Prediction与Authority接触形状不一致

- **WHEN** Client Prediction与Authority为同一Actor加载不同Radius、Height、SkinWidth或求解配置
- **THEN** WorldConfigurationHash或Solver identity MUST不同
- **AND** Session MUST在Prediction Active前拒绝组合或握手

#### Scenario: 初始Body重叠过深

- **WHEN** locked roster的BeforeBody重叠超过配置的最大去穿透距离
- **THEN** WorldSolver MUST拒绝整个batch并报告明确接触失败
- **AND** MUST不扩大容差、随机分离或保留重叠World state

### Requirement: DotRecast Actor接触必须区分Active与Observed参与者

`ActorContactSolver` MUST在同一candidate集合中显式区分`ActiveSimulated`与`ObservedKinematic` mobility。Active/Active MUST保持对称连续扫掠、闭合法向裁剪、切向保留和有界去穿透；Active/Observed MUST使用双方前后位置计算相对轨迹与TOI，只允许修改Active一侧；Observed/Observed MUST不产生可提交修正。Mobility MUST只表达本次World batch的可改写性，MUST不表达Gameplay priority、阵营、霸体、攻击或网络权威枚举。Observed candidate MUST引用Solver锁定canonical contact shape的configuration hash；Solver MUST验证一致后使用自己的Radius、Height与SkinWidth，MUST不从网络Body或默认值构造第二份形状。

#### Scenario: 本地owner撞向静止远端观察体

- **WHEN** Active owner的candidate轨迹与Observed remote Body相交
- **THEN** Solver MUST裁剪owner的闭合法向位移并保留合法切向位移
- **AND** MUST不移动Observed Body

#### Scenario: 远端观察体沿轨迹靠近owner

- **WHEN** Observed remote的前后Body轨迹主动闭合到Active owner
- **THEN** Solver MUST使用相对轨迹计算接触
- **AND** 需要分离时 MUST只修正Active owner

#### Scenario: Observed接触形状身份不一致

- **WHEN** 观察frame声明的contact shape hash与Prediction Solver锁定shape不一致
- **THEN** World batch MUST在接触求解前失败
- **AND** MUST不使用owner shape、网络字段或默认半径继续求解

### Requirement: DotRecastWorldSolver必须只提交Active参与者

`DotRecastWorldSolver.ResolveBatch` MUST对active requests生成Navigation Surface candidate，对observed constraints读取已选择Body轨迹，将两者按ActorId合并后调用唯一`ActorContactSolver`。接触后 MUST只对active位置执行Surface reconstraint，只为active request生成FinalBody、WorldSolveResult与NextWorldState；Observed参与者 MUST不进入committed World state。任一active/observed pair在固定迭代后不能同时满足Surface与最小间距时整个batch MUST失败。

#### Scenario: Observed约束参与Prediction batch

- **WHEN** World batch包含一个active owner request与一个observed remote constraint
- **THEN** 接触计算 MUST同时看到双方轨迹
- **AND** 结果roster与NextWorldState MUST仍只包含active owner

#### Scenario: Authority完整roster batch

- **WHEN** Authority以两个active Program actor执行同一World batch
- **THEN** 两个Actor MUST继续都产生FinalBody与WorldSolveResult
- **AND** Observed合同 MUST不改变Authority对称求解

### Requirement: Observed Actor接触必须通过World Feature锁定

支持ObservedKinematic约束的Solver MUST声明`ActorCollision`与`ObservedKinematicActorContact` World Feature，并将feature、Solver version和观察约束codec identity纳入组合兼容性。需要该能力的Prediction Composition MUST在Session Active前显式要求并验证；不支持该合同的Solver MUST不得忽略观察frame或伪装成功。

#### Scenario: Prediction选择不支持观察接触的Solver

- **WHEN** Composition要求ObservedKinematicActorContact但Solver未声明该feature
- **THEN** Session preparation MUST失败并报告缺失feature
- **AND** MUST不退化为只有静态Surface的预测

### Requirement: DotRecast Navigation Surface Solver不得声明空中垂直能力

当前DotRecastWorldSolver只通过nearest-poly、MoveAlongSurface、height projection与Surface reconstraint处理Navigation Surface运动，因此 MUST不声明`AirborneVerticalMotion`。需要该capability的Program与DotRecast Solver组合 MUST在Session Active前失败。DotRecast MUST不丢弃request Y、保持假Grounded、把Actor吸附到NavMesh、按Network Model关闭Body Motion、调用Unity Physics或隐藏Fixed KCC作为fallback来伪造支持。若非零Y request在组合校验之后仍到达Solver，Solver MUST明确失败，MUST不投影后返回成功。

#### Scenario: DotRecast组合需要重力的Corin Program

- **WHEN** Composition发现Corin Program要求AirborneVerticalMotion
- **AND** DotRecast descriptor未声明该capability
- **THEN** Composition MUST明确拒绝并报告缺失能力
- **AND** MUST不创建DotRecast runtime或发布部分Session资源
