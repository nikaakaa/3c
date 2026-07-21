## ADDED Requirements

### Requirement: Gameplay Lab必须是独立Editor/Development单机场景

项目MUST提供`Assets/Scenes/GameplayLab/GameplayLab.unity`作为Gameplay技术展示与手感调试场景。唯一`Tools/3C/Launcher` MUST明确显示`单机 / Gameplay Lab`、`双端验证 / Network Test Products`、`正式启动 / Published Player`与`编辑器启动 / Bootstrap Play`四个业务分组。Gameplay Lab MUST不替换Release产品的StandaloneGameplay，不进入商业启动、认证、Home或YooAsset分包链，也 MUST不替换DeterministicRollback、ServerAuthoritative或其它网络模型场景。Editor launcher MAY直接打开Gameplay Lab，但 MUST不修改普通Build Settings、不填充假Endpoint，也 MUST不作为产品启动失败后的fallback。

#### Scenario: 开发者直接运行Gameplay Lab

- **WHEN**开发者通过正式Editor launcher进入Gameplay Lab
- **THEN**MUST可在不启动CDN、Auth、Relay或远端客户端的条件下创建Local Session
- **AND**StandaloneGameplay与全部网络模型场景 MUST保持可独立运行

#### Scenario: 开发者运行正式发布Player

- **WHEN**开发者在同一Launcher中选择`正式启动 / Published Player`
- **THEN**系统 MUST先校验Player与Content发布清单再运行正式Player
- **AND**Player MUST只从Bootstrap进入版本策略、缓存完整性校验、Core资源与Gameplay预加载链
- **AND**Endpoint配置无效时 MUST禁用正式运行与Player构建而不切换到Gameplay Lab
- **AND**Content构建 MAY在Endpoint部署前独立产出待发布的YooAsset版本

#### Scenario: 开发者在Editor运行正式链

- **WHEN**开发者在同一Launcher中选择`编辑器启动 / Bootstrap Play`
- **THEN**Editor MUST从Bootstrap进入与正式Player相同的资源、认证与Gameplay链
- **AND**MUST不直接加载StandaloneGameplay或切换到Gameplay Lab

### Requirement: Gameplay Lab必须通过显式Variant锁定完整Session组合

Gameplay Lab MUST只引用一个显式`GameplayLabSessionVariantDefinition`。Variant MUST保存稳定VariantId，并完整引用一个runtime root与预期Numeric Profile、Source、Pipeline和Solver identity。Bootstrap MUST在任何Session进入Preparing前实例化且只实例化一个runtime root；Active后 MUST不切换Variant。系统 MUST不通过enum、名称、已安装类型扫描、默认值或失败回退选择Variant。

#### Scenario: 选择Local Fixed Variant

- **WHEN**场景显式引用Local Fixed Variant并进入Play
- **THEN**MUST只创建一个Fixed Numeric Target Session与一个Deterministic KCC
- **AND**MUST不同时创建Float Session或备用Fixed Session

#### Scenario: Variant配置缺失

- **WHEN**Gameplay Lab Bootstrap没有显式Variant或runtime root不完整
- **THEN**MUST在Actor registration与Session preparation前失败
- **AND**MUST不自动选择Float、Fixed或任一网络模型Variant

### Requirement: Gameplay Lab必须提供Local Float与Local Fixed两个正式Variant

项目MUST提供`Local Float32 + Unity CharacterController`与`Local Fixed + Deterministic KCC`两个完整Variant。Local Float Variant MUST复用现有Float32 Program Runtime、Float Backend、Standard Local Pipeline、Local Source与Unity CharacterController Solver。Local Fixed Variant MUST复用现有Fixed Program、Fixed Backend、Deterministic KCC与collision artifact，并使用正式Local Fixed Source和Standard Fixed Local Pipeline。

#### Scenario: 对比同一测试环境

- **WHEN**开发者分别以两个Variant运行Gameplay Lab
- **THEN**两个Session MUST消费同一个CharacterMovementTestEnvironment与OpenKCCMovementCourse作者几何
- **AND**角色ActorId、初始作者位置和Presentation语义 MUST保持可对比

### Requirement: Local Fixed不得依赖Gameplay Network Model

Local Fixed Source与Pipeline MUST不创建或要求GameplayNetworkModelDefinition、Endpoint、Transport、Relay、Peer、Player handshake、network roster packet、history、prediction、rollback restore或远端Actor。Local Fixed MUST每个outer logic tick从显式Fixed Control Source产生一个simulation step，并通过Immediate Commit发布当前结果。系统 MUST不以单Peer Rollback、Loopback Endpoint、禁用Rollback pass或假confirmed tick表达Local Fixed。

#### Scenario: 离线运行Local Fixed

- **WHEN**Local Fixed Variant在没有网络服务和远端进程的环境中启动
- **THEN**Session MUST使用Fixed Program、Standard Fixed Local Pipeline与Deterministic KCC进入Active
- **AND**diagnostics MUST明确显示Local Source而不是Rollback Model

### Requirement: Fixed Character装配必须与Rollback模型所有权分离

Model-neutral Fixed Character Host MUST显式消费SimulationSessionHost、Fixed Program、ActorId、WorldBodyBindingId、Fixed Control Source、Presentation Role、Projection、Body Presentation与Foot Placement配置。它 MUST不引用Endpoint、Peer、Rollback history或network diagnostics。Rollback adapter MAY在此基础上附加local input owner、restore与network diagnostics端口，但 MUST不重新创建InitialBody、Program、Presentation、Foot Placement或基础registration。

#### Scenario: Local Fixed玩家装配

- **WHEN**Fixed Character Host使用Player Control Source与LocalOwner Presentation Role
- **THEN**MUST创建本地设备输入、相机、Fixed registration与唯一Presentation runtime
- **AND**MUST不创建Rollback adapter

#### Scenario: Rollback角色装配

- **WHEN**Rollback场景为同一Fixed Character Host附加Rollback adapter
- **THEN**adapter MUST只增加模型所需port与identity校验
- **AND**MUST不创建第二个角色runtime或第二个Presentation

### Requirement: Local Fixed目标输入必须来自唯一Committed Actor Observation

Gameplay Lab玩家的Fixed ActionTarget input MUST消费model-neutral Committed Actor Observation port。Observation MUST来自目标Actor最近一次已提交的逻辑Body，并按稳定ActorId显式绑定。Provider MUST不读取Visual Root、Animator、插值Transform、Scene搜索、Tag或名称，也 MUST不建立Fixed专用Body registry。Observation缺失时 MUST写入正式None target value，MUST不沿用上一帧目标。

#### Scenario: 目标移动后再次攻击

- **WHEN**训练目标的逻辑Body已经在新tick提交且玩家开始下一段Attack
- **THEN**Fixed input MUST从该Committed Observation生成新的target candidate
- **AND**ActionInstance MUST冻结本次激活时的target snapshot

### Requirement: MotionWarp必须继续由当前WorldSolver裁决

Gameplay Lab中的Float与Fixed MotionWarp MUST只修改Program产出的CharacterMotionRequest，最终Body MUST由当前Variant唯一WorldSolver裁决。Diagnostics MUST同时显示source、target snapshot、requested displacement、applied displacement、yaw correction与solver disposition。Presentation、Foot Placement或调试面板 MUST不在World Solve后补写Transform或Body。

#### Scenario: 目标位于墙后

- **WHEN**MotionWarp请求指向静态墙体后的训练目标
- **THEN**Deterministic KCC MUST按正式连续碰撞合同裁剪或阻挡该请求
- **AND**角色 MUST不穿墙、瞬移到目标、由Presentation补偿穿过墙体或自动绕路

#### Scenario: 目标贴近墙角

- **WHEN**Warp请求在同一步同时受到墙面与Actor contact约束
- **THEN**唯一WorldSolver MUST在同一batch内求解并提交最终Body
- **AND**无法满足约束时 MUST按Solver合同失败或显示Blocked，而不是绕过KCC

### Requirement: Gameplay Lab必须复用唯一测试环境与碰撞产物

Gameplay Lab MUST只实例化一份CharacterMovementTestEnvironment与其OpenKCCMovementCourse。Float Solver MAY消费同一作者几何的Unity Collider；Fixed Solver MUST消费由该唯一作者来源Bake出的DeterministicCollisionWorldArtifact。Scene MUST不为Fixed复制隐藏Collider、运行时Physics读取或第二collision artifact。

#### Scenario: 修改测试坡面

- **WHEN**作者修改唯一surface authoring下的坡面并重新Bake
- **THEN**Fixed collision ContentHash MUST变化
- **AND**Gameplay Lab与Rollback MUST引用同一新artifact

### Requirement: Gameplay Lab必须区分角色手感参数与KCC碰撞参数

起步、速度、加速、减速、急停、转向、空中控制与落地衔接参数 MUST由正式Character Program/Graph/Body Motion authoring拥有并进入SemanticHash与目标Program identity。Capsule、skin、坡度、step height、ground snap、query tolerance、capacity与iteration MUST由DeterministicKccWorldSolverDefinition拥有并进入KCC ConfigurationHash、Solver Identity与World Identity。Gameplay Lab、EditorPrefs、static field或Development UI MUST不保存隐藏倍率。

#### Scenario: 调整Ground Snap

- **WHEN**作者修改Fixed KCC的Ground Snap正式配置
- **THEN**KCC ConfigurationHash与Solver/World identity MUST反映变化
- **AND**Rollback与Local Fixed资产未同步时 MUST在Composition或handshake阶段拒绝不一致

#### Scenario: 调整加速感

- **WHEN**作者修改角色起步或减速正式参数
- **THEN**Float32与Fixed Program product MUST从同一authoring重新生成对应identity
- **AND**KCC ConfigurationHash MUST不因纯Program运动参数变化而伪变化

### Requirement: Gameplay Lab必须提供同一Presentation链的只读诊断

Gameplay Lab diagnostics MUST显示Variant、Numeric Profile、Source、Pipeline、Solver、collision hash、KCC configuration hash、Actor committed Body、MotionWarp trace、Foot Placement状态与Animation Marker Sync group/phase。诊断 MUST只读取正式runtime snapshot与trace，不进行Physics查询、Graph求值、目标选择或Transform推断。Animation Marker Sync MUST描述为Walk/Run步态相位匹配，MUST不宣称Motion Matching、Pose Search或Stride Warping。

#### Scenario: 观察Walk/Run切换

- **WHEN**玩家在Gameplay Lab中从Walk进入Run
- **THEN**诊断 MAY显示marker group、源phase与目标phase
- **AND**MUST不显示不存在的Pose Database或Motion Matching结果

### Requirement: Gameplay Lab自动操作输入必须经过正式Input port

为computer-use提供的离散键盘Attack或镜头操作 MUST进入现有Character Input Profile、Control Source、CharacterSimulationInput与request timing链。它 MUST不直接激活Action、不写Blackboard、不写Actor Body或Transform，也 MUST不提供传送到目标、穿墙或跳过KCC的调试命令。原正式玩家绑定 MUST保持可用。

#### Scenario: computer-use触发Attack

- **WHEN**自动操作通过Gameplay Lab键盘binding触发Attack
- **THEN**request MUST在正式输入采样阶段进入当前Session
- **AND**Timeline、ActionInstance、MotionWarp与WorldSolver MUST沿玩家实际输入相同的链执行
