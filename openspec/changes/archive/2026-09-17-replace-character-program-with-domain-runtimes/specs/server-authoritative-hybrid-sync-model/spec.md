## MODIFIED Requirements

### Requirement: ServerAuthoritativeHybrid 必须是完整独立的 Network Model

系统 MUST将 owner prediction、authority correction、remote replication、baseline、ack 与模型 history 明确归属 ServerAuthoritativeHybrid。该模型 MUST通过正式 Prediction Source、Authority Source、Prediction Pipeline、Authority Pipeline、Fantasy Endpoint、Gameplay Runtime、Execution Backend 与 WorldSolver 形成完整组合，不得把公共 Gameplay 合同重新命名为模型专属实现。

#### Scenario: 查看模型身份

- **WHEN** ServerAuthoritative Session 进入 Active
- **THEN** diagnostics MUST显示明确 ModelId、protocol、Prediction/Authority Pipeline identity
- **AND** correction、baseline、ack 与 replication MUST显示为该模型的事实



### Requirement: 模型策略必须集中在模型 Definition 与 Pass 配置

ServerAuthoritativeHybrid的prediction、authority、history、snapshot、cadence、missing-input与output disposition policy MUST只在模型Definition、Source或Pass config。可靠输出 MUST由GameplayFactKind与ProducerId coverage配置；ActionProfile、Effect、Graph、Timeline与Blackboard MUST不复制模型策略，也 MUST不要求逐Action或逐Effect策略表。缺失角色玩法 coverage时配置 MUST失败。

#### Scenario: Producer 缺少复制覆盖

- **WHEN** Corin已授予技能声明animation producer
- **AND** 模型配置没有ProducerId coverage
- **THEN** ModelDefinition 或 Pipeline compile MUST明确失败
- **AND** MUST不按名称或ActionProfile推断



### Requirement: ServerAuthoritative 权威运动必须拥有独立模拟后端

ServerAuthoritativeHybrid权威端 MUST通过唯一Authority Source与显式Authority Pipeline向同一GameplayContentCatalog提供Host校验后的canonical input，并由Float32 Gameplay Runtime、唯一Float32 Pass Backend、标准Evaluate/WorldSolve/Finalize Pass与当前Authority Host Profile的唯一WorldSolver独立产生canonical Character/World state。External Unity CharacterController Host与InProcess DotRecast Authority Scene MUST使用相同GameplayContentHash、BackendId、Authority Pipeline descriptor/hash和model products，只允许HostProfile、Solver、World与control route identity不同。Fantasy Gate Scene MUST不执行角色玩法或Solver；Fantasy Server内的独立Authority Scene MAY作为普通.NET Host执行它们。客户端predicted displacement、Transform、Body sample或position字段 MUST不成为权威位移输入。

#### Scenario: InProcess DotRecast Authority Scene推进两个Actor

- **WHEN** Authority Pipeline收到Actor A/B canonical input batch且当前HostProfile为InProcessDotRecast
- **THEN** Authority Scene MUST按稳定ActorId执行同一Float32 领域运行并进行一次DotRecast World ResolveBatch
- **AND** MUST从finalized canonical state生成checkpoint与replication



### Requirement: ServerAuthoritative 模型缺少正式 Source 或 Pipeline 时必须不可用

ServerAuthoritativeHybrid Network Model MUST只保存协议、Prediction/Authority Pipeline Pair、同步策略以及Numeric/ABI/Backend/Solver能力要求，MUST不保存具体Gameplay Runtime、Execution Backend或WorldSolver引用。客户端Session Composition MUST显式选择Prediction Source、Prediction Pipeline、客户端Pass factory、Fantasy Endpoint、Float32 Gameplay Runtime/Backend、Prediction Solver与Actor/presentation registration。每个Authority Host Profile MUST另外提供完整portable Authority Source runtime、Authority Pipeline catalog、Float32 Gameplay Runtime/Backend、匹配Authority Solver、locked roster、World、Host Profile要求的control adapter和gameplay datagram endpoint。InProcess DotRecast环境的Prediction与Authority MUST锁定相同SolverId/version、NavigationSurfaceArtifactHash和QueryProfileHash。仅保留protocol、profile、Room、Authority Scene或外部Worker壳 MUST不构成可启动Host。

#### Scenario: DotRecast Client仍配置CC Solver

- **WHEN** Client launch profile要求InProcessDotRecast World identity但Prediction Composition配置CC Solver
- **THEN** Client preparation MUST失败
- **AND** MUST不连接Room后依赖correction掩盖不匹配



### Requirement: ServerAuthoritative 模型必须显式区分Gate Room与Authority Host

Fantasy Gate Scene MUST只拥有Client control connection、Room、roster、ticket、可靠事务路由与失败传播。Authority Host MUST拥有角色玩法、canonical roster、portable Authority Source/Pipeline、gameplay datagram endpoint、WorldSolver与权威state。Authority Host MAY是外部Unity Worker或同一Fantasy Server进程内的独立DotRecast Authority Scene；同一OS进程 MUST不消除Scene级所有权。Room MUST锁定唯一Host route，普通Client MUST不能提升为Host。Host断开或Authority Scene失活时当前Room MUST fail-stop，不得由Client接管、在Active中切换Host或启动fallback。

#### Scenario: DotRecast Authority Scene注册Room

- **WHEN** DotRecast Authority Scene通过Inner route提交完整角色玩法/Pipeline/Host/Solver/World identity
- **THEN** Room MUST锁定唯一InProcess Authority Scene Address与Data endpoint
- **AND** clients MUST只在本地Prediction组合与该identity兼容后进入Active

#### Scenario: Unity Authority Worker注册Room

- **WHEN** Unity Authority Worker通过Outer route提交完整角色玩法/Pipeline/Host/Solver/World identity
- **THEN** Room MUST锁定唯一External Worker Session与Data endpoint
- **AND** 同一Room MUST不再接受InProcess Authority Scene注册



## ADDED Requirements

### Requirement: Authority兼容身份必须覆盖领域玩法内容和状态格式

握手 MUST锁定双方控制模块语义版本、必要配置、技能与授予内容、状态 schema、数值目标、世界／Solver 以及原 Pipeline 兼容 Pair。玩法内容 Hash MUST不包含纯 Pose 图或相机资源。缺失或不匹配身份 MUST拒绝会话，不得省略状态格式检查。

#### Scenario: 相同技能搭配不同控制逻辑
- **WHEN** Prediction 与 Authority 技能一致但控制模块语义版本不同
- **THEN** 握手 MUST明确拒绝，不能只比较技能内容
