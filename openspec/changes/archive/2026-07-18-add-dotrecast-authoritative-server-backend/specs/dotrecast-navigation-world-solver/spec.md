## MODIFIED Requirements

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

## REMOVED Requirements

### Requirement: DotRecast Solver必须只执行局部Surface约束

**Reason**: 该要求把“Recast静态Surface查询不负责角色碰撞”错误扩大成“完整DotRecast WorldSolver不得裁决Actor硬接触”，导致同一World ResolveBatch内的Actor逐个独立提交FinalBody并互相穿透。Recast查询职责继续保持不变，但WorldSolver必须在其后组合唯一portable ActorContactSolver。

#### Scenario: 移除逐Actor独立最终求解约束

- **WHEN** 同一batch内两个Actor的Surface candidate发生接触
- **THEN** 系统 MUST不再允许两个candidate绕过同批Actor接触而直接成为FinalBody

## ADDED Requirements

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
