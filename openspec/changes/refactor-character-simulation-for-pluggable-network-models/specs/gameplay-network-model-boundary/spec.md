# gameplay-network-model-boundary Specification

## MODIFIED Requirements

### Requirement: Gameplay Network Model 必须是 Session 级唯一装配

系统 MUST在 Session 启动前通过 `GameplayNetworkSessionHost -> GameplayNetworkModelDefinition -> model session` 装配唯一完整 Network Model。Model session MUST拥有自己的 Simulation Driver、actor binding、history、commit 和 model protocol，并声明 Program/World Solver/Host capability要求。Session 运行中 MUST不切换 model、Program 或 solver；Character、Graph、State、Action 和 Timeline MUST不分别选择模型。

#### Scenario: 创建 ServerAuthoritative Session

- **WHEN** SessionHost 使用 ServerAuthoritativeHybrid definition
- **THEN** session MUST创建该模型的 Driver 与 endpoint
- **AND** Unity/DotRecast server backend MUST由 server launch composition决定

#### Scenario: 创建 DeterministicRollback Session

- **WHEN**完整 Rollback definition 被选择
- **THEN** session MUST要求 deterministic Program 与 KCC capabilities
- **AND** MUST不复用 ServerAuthoritative correction runtime

### Requirement: Model、Endpoint 和 Transport 必须分层

系统 MUST区分 Network Model、Simulation Driver、World Solver、Host、model Endpoint 和底层 Transport。Model/Driver 表达 gameplay 同步、history、authority、replay 和 commit；World Solver 表达世界约束；Host 表达 Unity 或 .NET 进程；Endpoint 表达模型远端；Transport 只负责连接和字节收发。LocalLoopback、Fantasy、Unity server、DotRecast 和 KCC MUST不被错误计为五个 Network Model。

#### Scenario: 两个 ServerAuthoritative Demo

- **WHEN** Unity authoritative 与 DotRecast authoritative Demo 启动
- **THEN** 两者 MUST使用同一 ServerAuthoritativeHybrid model id
- **AND** MUST只在 Host/World Solver manifest 上不同

### Requirement: 只允许选择完整实现的 Network Model

Model authoring UI MUST只显示已安装、可创建 runtime、具备完整 actor binding/protocol/history/commit 且配置闭环的 definition。完成本 change 后，正式模型 MUST为 `ServerAuthoritativeHybrid` 与 `DeterministicRollback`；不完整插件、solver 或 host MUST不进入可运行列表。

#### Scenario: 查看模型配置

- **WHEN** 两个模型均已完整实现
- **THEN** UI MUST显示两个明确 model definition 类型
- **AND** MUST分别显示 required Program/solver capabilities

#### Scenario: Rollback KCC 缺失

- **WHEN** DeterministicRollback definition 缺少正式 KCC
- **THEN** 配置 MUST失败
- **AND** MUST不隐藏错误并改用 ServerAuthoritativeHybrid

### Requirement: Character Runtime 必须通过事实和语义输入连接模型

Character runtime MUST向 model Driver 暴露 compiled Program handle、portable simulation input、SimulationState/body sample、gameplay facts 和 presentation commit sink，并只接收 Driver 选择的 canonical input/state/sample。Character runtime MUST不持有 model packet、history、endpoint、transport 或 server backend。Model adapter MUST不把客户端 resolved motion 作为服务端 canonical motion。

#### Scenario: Owner 完成本地预测

- **WHEN** ServerAuthoritative Owner 完成当前 Tick
- **THEN** Driver MAY记录 body result 作为 prediction comparison
- **AND** 服务端 MUST从 portable input 和自己的 state独立执行

#### Scenario: Rollback 本地模拟远端 Actor

- **WHEN** canonical bundle 包含远端 Actor input
- **THEN** Rollback Driver MUST在同一 world state 中推进该 Actor
- **AND** Character runtime MUST不切换 RemoteProxy/ExternalPose mode

### Requirement: BTSMTL Authoring 不得拥有 Network Model 配置

BTSMTL Graph、StateMachine、Timeline、TreeClip、Blackboard、Animation authoring、compiled Program 和 Agent Patch MUST只表达 gameplay 结构、输入 schema、身份、状态和事实。它们 MUST不保存 model id、Driver、endpoint、transport、prediction、snapshot、correction、replication、rollback history 或 solver implementation。Program capability 是 compiler 产出的运行性质，不是节点网络策略。

#### Scenario: 作者配置攻击

- **WHEN** 作者编辑 Attack1 Timeline、Window 和 motion curve
- **THEN**同一 source MUST可由 Local、ServerAuthoritative 和 Rollback Driver执行
- **AND** Graph Inspector MUST不出现 model switch

## REMOVED Requirements

### Requirement: Character 输入来源与运动权威必须正交

**Reason**：`CharacterInputSource` 与 `CharacterMotionAuthority` 仍然把 actor 模拟角色作为 CharacterPipeline 内模式。新设计把输入适配和 Actor simulation role 分别交给 input adapter 与 Session Driver binding，并删除 motion authority enum。

**Migration**：Local Device、external canonical input 和 snapshot sample 通过正式 adapter/Driver 组合；不恢复 `LocalPredicted`、`RemoteProxy` 或 `ExternalPose` 总控枚举。

#### Scenario: 迁移远端 Actor

- **WHEN** ServerAuthoritative model 收到 remote snapshot
- **THEN** model Driver MUST更新 remote sample buffer和Presentation
- **AND** MUST不创建另一种 CharacterPipeline authority mode

