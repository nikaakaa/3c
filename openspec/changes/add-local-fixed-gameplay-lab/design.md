# Design: Local Fixed Gameplay Lab 与手感验收闭环

## Context

项目已经具备两个可工作的数值与求解组合：

```text
Local Float32
  Float32 Program
  -> Float32 Pass Backend
  -> Standard Local Pipeline
  -> Local Source
  -> Unity CharacterController Solver

Deterministic Rollback Fixed
  Fixed Program
  -> Fixed Pass Backend
  -> Rollback Pipeline
  -> Rollback Source / Endpoint / History
  -> Deterministic KCC
```

真正缺失的是：

```text
Local Fixed
  Fixed Program
  -> 同一个 Fixed Pass Backend
  -> Standard Fixed Local Pipeline
  -> Local Fixed Source
  -> 同一个 Deterministic KCC
```

Core层已有`FixedSimulationSessionComposer`，但Unity层的`FixedProgramRuntimeDefinition`、`FixedPassExecutionBackendDefinition`、`IFixedSimulationActorRegistration`、output/diagnostics aggregate、`UnityFixedSimulationSessionComposer`和`DeterministicKccWorldSolverDefinition`仍在`Simulation/Unity/DeterministicRollback`目录、命名空间或程序集里。`UnityFixedSimulationSessionComposer`直接要求`IDeterministicRollbackPreparedSource`、Rollback pipeline、RollbackState、restore/history committer与network diagnostics，因此Local Fixed无法只替换Source和Pipeline。

现有Gameplay环境已经包含`CharacterMovementTestEnvironment`，内部包含`OpenKCCMovementCourse`；Standalone与三个网络场景复用该环境。GameplayLab继续复用这一个作者来源，不复制坡面、台阶、墙体和薄墙几何。Float通过同一可见Collider层求解，Fixed通过该作者几何Bake出的唯一collision artifact求解。

## Goals

- 让Fixed Unity composition从网络模型中独立出来。
- 让Local Fixed和Rollback复用同一Fixed Program、Backend、Composer、KCC与collision artifact。
- 用一个独立GameplayLab场景观察Float和Fixed两种完整Session组合。
- 把KCC工作重心放在手感参数、诊断和真实场景证据，而不是复制基础碰撞算法。
- 让MotionWarp目标来自已提交Actor状态，并通过KCC产生最终位移。
- 让Timeline、步态相位匹配、Foot IK与Warp在同一Presentation链可观察。

## Non-Goals

- 不把GameplayLab接入商业版本同步、登录、主页、YooAsset分包或CDN。
- 不用Loopback Relay、单Peer Rollback或虚假Endpoint模拟Local Fixed。
- 不把Float与Fixed数值放进同一个runtime handle做热切换。
- 不建立第三份KCC配置、collision artifact或Fixed Program。
- 不在Presentation层修正KCC、Warp或Actor Body。
- 不增加Moving Platform、Rigidbody或完整Motion Matching。

## Terms

### Gameplay Lab

独立Editor/Development技术展示场景。它不是Release产品入口，不承担商业启动状态，也不替代任何网络模型场景。

### Session Variant

在Session创建前锁定的一份显式资产，完整引用一个runtime root prefab与对应Composition identity。Variant决定Numeric Target、Source、Pipeline、WorldSolver和actor host种类；它不是运行时模式枚举。

### Local Fixed

Fixed Numeric Target上的Local Session Source与Standard Local Pipeline。它只消费本机Control Source，不创建Network Model、Endpoint、Transport、Peer、history、restore或prediction policy。

### Committed Actor Observation

上一份已完成World Solve并提交的Actor逻辑Body快照。Input provider只从这个model-neutral port读取目标事实，不读取Visual Root、Animator或插值Transform。

### KCC Feel Profile

决定接触、坡面、台阶、贴地、滑动和查询边界的正式Fixed配置，加上Program中决定速度、加速、减速、转向、空中控制与落地衔接的正式运动参数。两类参数属于不同owner，但都必须有明确identity。

## Target Architecture

```text
GameplayLab.unity
  Shared Environment Authoring
    -> visible Unity colliders
    -> baked DeterministicCollisionWorldArtifact

  GameplayLabBootstrap
    -> explicit GameplayLabSessionVariantDefinition
    -> instantiate exactly one Session Runtime Root

Float Variant Runtime Root
  SimulationSessionHost
  + Float32 Composition
  + Float CharacterPipelineHost roster
  + Unity CharacterController Solver

Fixed Variant Runtime Root
  SimulationSessionHost
  + Fixed Composition
  + Fixed CharacterPipelineHost roster
  + Deterministic KCC

Both
  -> same Corin authoring source
  -> target-specific compiled Program
  -> same Presentation Semantic ContractHash
  -> one target-neutral Timeline/Animation/Foot IK Projection
  -> same committed observation semantics
```

Fixed程序集依赖方向：

```text
Portable Fixed Core
        ^
        |
Unity Fixed Composition
  Program Runtime
  Pass Backend
  Actor Registration
  Output / Diagnostics
  Composer
  Fixed Control Sources
        ^
        |-------------------|
        |                   |
Local Fixed Adapter   DeterministicRollback Adapter
Local Source          Rollback Source / Endpoint
Local Pipeline        Rollback Pipeline / History
Immediate Commit      Restore / Confirmed Commit
```

## Decisions

### Decision 1: 抽出唯一Unity Fixed Composition，而不是复制Local Composer

选择：把Fixed Program Runtime、Backend、actor registration合同、output lifecycle、diagnostics aggregate、KCC Definition与基础Composer迁入model-neutral Unity Fixed程序集。Prepared Source提供强类型Fixed Runtime Launcher与source/runtime package；Local和Rollback launcher都调用同一个Fixed Composer。

业务收益：同一角色在本地手感调试和Rollback联机中运行相同Fixed Program、KCC与输出表现，不会出现“本地调好了，联机又是另一套”的展示风险。

代价：现有Rollback Unity代码需要一次原子拆分和命名迁移，程序集引用面较大；迁移中允许明确编译失败，但不保留旧命名wrapper。

未选择复制`UnityFixedSimulationSessionComposer`为Local版本：两份构造器会逐渐产生不同identity校验、output commit和diagnostics行为。

未选择让Local直接创建RollbackState：它会把本地玩法变成网络模型的特殊配置，继续需要无业务意义的Peer和history。

### Decision 2: 一个GameplayLab场景通过显式Variant资产选择完整runtime root

选择：场景中只有共享环境、摄像机外壳和`GameplayLabBootstrap`。Bootstrap必须引用一个`GameplayLabSessionVariantDefinition`；Variant引用一个完整runtime root prefab和稳定identity。Bootstrap在任何Actor注册前只实例化该root一次，随后锁定；缺配置或重复root直接失败。

业务收益：用户在同一个场景里切换Float或Fixed，不需要维护两份测试地形；每次Play仍只有一个完整Session、一个Solver和一套Actor roster。

代价：Float与Fixed需要各自runtime root prefab，因为它们的Program asset、world binding和actor registration类型不同。

未选择一个enum控制两组序列化字段：隐藏字段会产生组合爆炸和默认值，无法从资产引用看出完整Session身份。

未选择运行中切换：Current spec要求Active Session roster、Program、Source、Pipeline和Solver不可变；热切换会让表现、输入和诊断生命周期混在一起。

### Decision 3: GameplayLab与StandaloneGameplay职责分离

选择：StandaloneGameplay继续是商业产品在资源、认证与Gameplay preload完成后的玩法场景；GameplayLab是Editor/Development直接运行的技术展示场景。唯一`3C Launcher`面板按业务目的呈现四个分组：`单机 / Gameplay Lab`只创建所选Local Session Variant；`双端验证 / Network Test Products`保留各产品独立Build/Run；`正式启动 / Published Player`构建、校验并运行正式Player与Content发布闭包；`编辑器启动 / Bootstrap Play`在Editor宿主中从Bootstrap进入相同正式资源链。Gameplay Lab不修改Build Settings、不经过ProductBootstrap，也不作为产品失败后的fallback。

业务收益：求职展示可以直接演示KCC、IK、Timeline和Warp，不需要先搭CDN/Auth；商业启动链仍能单独展示资源与登录能力。

代价：统一面板需要依赖倒置注册Gameplay Lab Editor模块，并同时展示四类运行状态；正式Player与Editor Bootstrap会共享配置校验但拥有不同宿主按钮。用户只需要记住一个入口，且不会再把“Editor运行”误认为“绕过资源的单机运行”。

未选择把Standalone改造成带Float/Fixed开关的Lab：会把开发调试配置带进商业玩法场景，并增加分包依赖和Release验证负担。

### Decision 4: Local Fixed拥有自己的Source与Pipeline，但不拥有第二套Program/KCC

选择：Local Fixed Source只负责从显式Fixed Control Source形成当前tick input；Standard Fixed Local Pipeline固定为Input Ingress、Single Step Schedule、Program Evaluate、World Solve、Body Finalize与Immediate Commit。Program Runtime、Backend、Composer、Solver和output aggregate全部复用Fixed公共层。

业务收益：本地手感反馈即时，逻辑简单；同一Fixed Program与KCC稍后接入Rollback时结果边界不变。

代价：需要实现一套Local语义的Fixed pass package和committer，但这些是Source/Pipeline业务差异，不是重复Session runtime。

未选择复用Rollback Pipeline并关闭网络pass：跳过pass或伪造confirmed tick属于fallback配置，也会让identity无法说明实际执行链。

### Decision 5: Fixed角色装配与Rollback Peer选择分离

选择：model-neutral Fixed Character Host显式消费Fixed Program、ActorId、WorldBodyBindingId、Fixed Control Source、Presentation Role和diagnostics配置。Rollback adapter只负责把Endpoint选出的local Actor绑定为rollback input owner，并为registration附加restore/network diagnostics端口；它不再创建Program、Presentation或KCC。

业务收益：Local Fixed玩家、Local Fixed训练目标和Rollback角色复用同一角色创建与表现路径；Endpoint不再决定相机和角色资产。

代价：现有`DeterministicRollbackCharacterHost`需要拆分，Prefab字段与序列化引用必须一次迁移。

未选择保留Rollback Host再增加Local Fixed Host副本：两者会重复构建InitialBody、Presentation、Foot IK和diagnostics context。

### Decision 6: MotionWarp目标只消费唯一Committed Observation

选择：等待`add-btsmtl-ai-controller-authoring`安装model-neutral Committed Actor Observation port后，Float玩家目标provider与Fixed玩家目标provider都消费该port。Fixed target value按Fixed input ABI编码，目标pose从同一逻辑快照降低，不读取Transform。

业务收益：玩家、AI和Fixed/Float输入看到同一份上一提交Actor事实，MotionWarp、锁定和后续AI不会各自维护目标位置。

代价：Local Fixed MotionWarp实现顺序受AI Controller observation基础设施约束。

未选择在本change复制`latest committed body`字典：这会与正在进行的AI change形成两个真相源。

### Decision 7: Warp请求永远由WorldSolver裁决

选择：MotionWarp只改变Program产出的`CharacterMotionRequest`。Fixed World Solve用Deterministic KCC连续胶囊查询裁剪实际位移；diagnostics同时记录requested displacement、applied displacement、target snapshot和solver disposition。Presentation只播放已提交结果。

业务收益：隔墙、贴墙、墙角和不可达目标不会穿墙或瞬移；展示可以解释“想去哪里”和“碰撞允许去了哪里”的差值。

代价：MotionWarp不会绕开障碍自动寻路，目标在墙后时可能只移动到墙前并显示Blocked/Clamped。

未选择Warp后直接改Transform：它会绕过KCC、Rollback state、hash与Foot IK输入。

### Decision 8: 手感参数按Owner分层，不把所有感觉塞进KCC

选择：

- 起步、速度、加速、减速、急停、转向、空中控制和落地衔接属于Character Program/Graph/Body Motion正式参数。
- capsule、skin、坡度阈值、step height、ground snap、movement/query tolerance、contact capacity和iteration属于Deterministic KCC Definition。
- Foot Placement只做视觉脚底贴合，不反向改变Grounded或Body。

业务收益：调整“角色响应”不会改变碰撞协议身份；调整“碰撞边界”会明确改变KCC ConfigurationHash并让网络握手拒绝不一致版本。

代价：手感调校需要同时观察Program trace与World trace，不能只改一个Inspector面板。

未选择新增隐藏Debug乘数：无法进入identity/hash，实机效果不可复现。

### Decision 9: 共享一份环境作者几何，但允许两个Solver各自消费正式表示

选择：CharacterMovementTestEnvironment和OpenKCCMovementCourse是唯一可见测试几何。Float Solver消费其Unity Collider；Fixed Solver消费由同一authoring根生成的DeterministicCollisionWorldArtifact。GameplayLab诊断显示artifact MapId、ContentHash和KCC ConfigurationHash。

业务收益：Float与Fixed在同一坡面、台阶、墙角和薄墙上对比，差异来自Solver而不是不同关卡。

代价：几何修改后必须重新Bake Fixed artifact；Hash变化是正式版本变化。

未选择运行时读取Unity Physics为Fixed KCC提供碰撞：会破坏确定性和两端一致性。

### Decision 10: 实机证据属于验收记录，不写进实施任务

选择：`tasks.md`只记录可实施、可静态确认的工作；computer-use操作矩阵、截图、Console与Live Debug结果写入`runtime-validation.md`。每次实机运行记录Variant、位置/角度、障碍条件、请求/应用位移、最终Body和异常现象。

业务收益：任务状态不再用“手动测试已勾选”冒充真实证据，失败样例可以持续追加。

代价：change可能在代码任务全部完成后仍明确处于“未通过实机验收”，不能仅凭`openspec list`的Complete状态判断完成。

### Decision 11: Program SourceMap必须携带每个作者容器的内容hash

选择：`ProgramSourceMapEntry`保存Graph或Timeline的作者内容hash。Frontend在Graph Node与Timeline Track/Clip source产生时计算正式fingerprint，Semantic IR和Float/Fixed Program codec原样保存；Runtime Debug先按Timeline/Track/Clip身份建立Timeline容器，再按Graph身份建立Graph容器，并用同一容器hash完成精确target匹配。

业务收益：Timeline Live Debug可以从实际运行Program定位到当前Timeline，同时在作者内容已改但Program尚未重建时明确显示revision mismatch，不会把旧运行结果画到新Timeline上。

代价：SourceMap canonical payload变化会改变SemanticHash和ProgramHash，Semantic IR、Float Program与Fixed Program artifact必须提升版本并重新生成。

未选择只按AuthoringId匹配：这会把过期Program错误附着到已修改Timeline，违反当前diagnostics规范。

未选择继续用整个ProgramHash作为source hash：编辑器持有的是Graph或Timeline自身内容，二者无法精确比较；Program中其它资产变化也不应伪装成当前Timeline内容变化。

### Decision 12: Gameplay Lab一次请求两个Target并只生成一份Projection

选择：Gameplay Lab Build向公共`CharacterSimulationBuildOrchestrator`提交一个有序Target集合，同时请求Float32与Fixed。Frontend、Presentation contract、Animation Analysis resolve和Projection Compiler各执行一次，两个Target Adapter只消费同一validated Semantic IR。Local Fixed或Rollback单独构建时只请求Fixed Adapter，不生成Float32 Program作为Projection前置条件。

业务收益：Float与Fixed对比使用同一Gameplay语义和同一表现资源，不会因为先后执行两个Build而让Projection、Analysis artifact或Definition引用落在不同revision。

代价：多Target发布必须作为一个事务提交；任一Target失败时两个Target和Projection都不发布。

未选择先调用默认Float32 Build再单独写Fixed文件：这会重复Frontend与Projection，并把Fixed产品重新绑回Float32生成顺序。

## Runtime Validation Matrix

矩阵由computer-use在真实Unity Play Mode执行：

| 维度 | 样例 |
|---|---|
| Variant | Local Float32 / Local Fixed |
| 目标距离 | 近、中、超出Warp clamp |
| 目标角度 | 正面、侧面、背面 |
| 目标状态 | 静止、移动后已提交 |
| 障碍 | 无障碍、隔墙、贴墙、墙角、薄墙、狭窄通道、不可达 |
| 观察 | target snapshot、source、progress、requested/applied displacement、yaw、solver disposition、final body |
| 表现 | Timeline、步态相位、Foot IK、镜头、动画连续性 |
| 失败 | 穿墙、过冲、瞬移、旋转突变、脚滑、错误落点、Console exception |

## Risks

### Fixed抽离影响Rollback稳定性

风险：迁移registration、committer与diagnostics时破坏Rollback output/restore生命周期。

约束：Rollback Source/Pipeline/History语义不改；先建立model-neutral contracts，再原子迁移Rollback adapter，禁止旧新Composer并存。

### AI观察change并行修改目标链

风险：本change和AI change都试图保存Committed Body。

约束：本change声明依赖，只消费最终Observation port；不修改AI change占用的Semantic compiler、CharacterPipelineDefinition或Corin资产。

### GameplayLab Variant选择时序错误

风险：Actor OnEnable早于Bootstrap绑定Composition，产生半注册Session。

约束：Variant实例化完整runtime root；root Prefab内引用已经完整，Bootstrap不在激活对象上逐字段改绑。缺Variant直接fail-closed。

### computer-use鼠标影响自由镜头

风险：Game View锁定鼠标后，自动点击Attack会同时产生Look delta，污染不同角度验收。

约束：GameplayLab提供仅Development可用、但走正式Input request port的可操作验收面板或离散按键绑定；它不直接激活动作、不写Blackboard、不写Transform。输入来源和request仍进入同一Control Source。

## Migration Strategy

1. 锁定当前Fixed、Rollback、KCC、Scene和diagnostics inventory。
2. 建立model-neutral Unity Fixed程序集与合同，先迁移无模型语义的类型。
3. 让Rollback adapter改用新合同并删除旧命名空间类型。
4. 增加Local Fixed Source、Pipeline、committer与runtime launcher。
5. 增加Fixed Character Host/control source，并迁移Rollback Prefab装配。
6. 在AI observation change完成后接入Fixed ActionTarget provider。
7. 创建GameplayLab shared scene、两个Variant和diagnostics。
8. 修正Standalone、GameplayLab与不存在SandBox的文档/launcher命名。
9. 完成编译、OpenSpec严格校验和静态身份核对。
10. 用computer-use持续追加runtime-validation记录，直到矩阵有足够证据。
11. 修复Program SourceMap作者内容hash与Timeline source优先级，重新生成Program后复验Live Debug精确匹配。

## Open Questions Resolved

- 是否让Standalone成为唯一入口：不。Standalone是产品玩法；GameplayLab是独立开发展示场景。
- 是否需要CDN、登录或Relay：GameplayLab不需要；商业启动change继续单独负责这些能力。
- 是否重做KCC：不。先接通Local Fixed并调正式参数，只有复现明确碰撞缺陷才改kernel。
- 是否用单Peer Rollback代替Local Fixed：不。Local必须是独立Source与Pipeline。
- 是否保留两个Fixed Composer：不。只有一个model-neutral Fixed Composer。
- 是否运行中切Float/Fixed：不。Play前选择Variant，Active后不可变。
- 是否复制训练目标Body registry：不。消费AI Controller change的唯一Committed Observation port。
- 是否让Warp绕墙：不。Warp提出运动请求，KCC决定实际位移。
- 是否把Marker Sync叫Motion Matching：不，只称Walk/Run步态相位匹配。
