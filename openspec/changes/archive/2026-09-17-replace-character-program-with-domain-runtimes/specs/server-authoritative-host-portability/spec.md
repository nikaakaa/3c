## MODIFIED Requirements

### Requirement: Authority Source Runtime必须Host-Neutral

每Actor command queue、authority clock、missing-input policy、每Client checkpoint baseline、snapshot sequence、reliable/full-checkpoint queue与typed Source ports MUST由portable Authority Source runtime唯一拥有。Unity与未来普通.NET Host MUST只提供transport adapter和显式launch输入。

#### Scenario: Source消费Command

- **WHEN** transport将已校验command写入Actor queue
- **THEN** portable Source MUST在outer tick边界消费并生成typed ingress
- **AND** transport MUST不执行角色玩法或missing-input决策



### Requirement: Authority Host必须通过唯一Launch Request调用Portable Composer

Authority Host launch request MUST显式提供Gameplay Runtime、Backend、Authority Pipeline、Source policy/ports、roster、WorldSolver、initial state、Committer、diagnostics和output routes，并调用唯一portable Float32 Composer。缺失或不兼容输入 MUST失败，不得选择默认组件或复制Composer。

#### Scenario: 普通.NET Host准备接入

- **WHEN** 后续Host提供完整portable launch输入
- **THEN** MUST可以在不引用Unity Definition的情况下调用同一launch request
- **AND** 当前change MUST不以空Worker或fallback证明该能力



### Requirement: 具体Authority Host Profile必须由Host Product拥有

neutral Simulation Core 与 ServerAuthoritative Model MUST只定义通用角色玩法、ABI、Pipeline、Solver capability、protocol和Host product identity合同，MUST不枚举、构造或降低`UnityAuthorityWorker`、`DotRecastAuthorityScene`或未来具体Host Profile。Unity Authority Product与DotRecast Authority Product MUST分别拥有自己的Host Profile、launch lowering、solver capability声明与manifest fields。新增Authority backend MUST通过新增Product adapter接入，不得修改neutral Core或既有Product实现。

#### Scenario: 生成Unity Authority Worker产品

- **WHEN** Unity Authority Product准备worker launch和build manifest
- **THEN** Unity Product adapter MUST提供worker Host identity、Unity solver capability和launch lowering
- **AND** neutral Core MUST只校验通用identity/ABI/capability合同

#### Scenario: 生成DotRecast Authority产品

- **WHEN** DotRecast Product准备普通.NET scene host
- **THEN** DotRecast Product adapter MUST提供scene Host identity、DotRecast solver capability和launch lowering
- **AND** MUST不修改Core枚举、Core factory或Unity Product代码

#### Scenario: Client prediction与Authority使用不同Solver

- **WHEN** Client使用Unity prediction solver而Authority使用DotRecast solver
- **THEN** compatibility MUST分别校验prediction solver与authority backend所需能力
- **AND** MUST不要求两端SolverId相同或让客户端选择authority Host Profile



## ADDED Requirements

### Requirement: PortableAuthority必须消费独立技能和领域配置

普通 .NET Authority MUST消费 portable C# 领域模块、技能执行数据及明确配置，并复用唯一 Composer、Source 和 Pipeline Pass。Host MUST不加载 Pose、FlowCanvas、Unity authoring object 或旧整角色 Program；Unity Authority 与 DotRecast 产品边界及 Solver 能力 MUST保持。

#### Scenario: 普通DotNet装配角色
- **WHEN** Authority Host 取得合法技能数据、模块配置、roster 和 Solver
- **THEN** 它 MUST在不加载 Unity 或 FlowCanvas 的条件下构造同一玩法运行，并继续按能力合同拒绝非法组合
