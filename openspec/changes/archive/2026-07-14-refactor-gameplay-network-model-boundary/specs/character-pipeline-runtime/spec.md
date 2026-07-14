## MODIFIED Requirements

### Requirement: CharacterPipeline 是纯 C# 运行时主体

系统 MUST 将 `CharacterPipeline` 实现为纯 C# 对象，并由 GameplayTickSystem 提供 logic/presentation 时间。Tick context MUST 表达 fixed/presentation delta、render frame、local logic tick、input sequence 和 interpolation alpha；MUST 不保存具体 Network Model authority mode。CharacterPipeline 自身 MUST 显式持有 CharacterInputSource 与 CharacterMotionAuthority，且 MUST 不直接读取 Unity Time、transport 或 model packet。

#### Scenario: GameplayTickSystem 驱动角色

- **WHEN** tick system 推进 CharacterPipeline
- **THEN** context MUST 提供统一时间和 sequence
- **AND** CharacterPipeline MUST 从自身正式配置读取 input source 与 motion authority

### Requirement: Graph 执行上下文来自 CharacterGraphContext

系统 MUST 使用 CharacterGraphContext 作为 RootTree context，提供 Timeline 请求、typed input、gameplay blackboard、tick pose、Character 语义外部输入、动画选择和 diagnostics。Context MAY 暴露 input source 与 motion authority，但 MUST 不暴露 Network Model id、model packet、endpoint、history 或 transport。

#### Scenario: Graph 读取外部动作事实

- **WHEN** ExternalFacts input source 注入动作 request
- **THEN** Graph MUST 通过 CharacterInputFrame/request buffer 读取
- **AND** MUST 不读取 ServerAuthoritative ActionReplication packet

### Requirement: Pipeline 分阶段处理输入、图、motion、表现和网络边界

CharacterPipeline MUST 保持 external semantic input、input、BTSMTL、motion、presentation、fact collection 和 cleanup 的明确阶段。External semantic input MUST 在 input/Graph 前进入；fact collection MUST 在本 tick事实产生后运行。Model-owned binding/adapter MUST 位于 Pipeline 外围，不得把 endpoint pump 或 packet mapping塞入 Pipeline stage。

#### Scenario: 当前模型注入 correction

- **WHEN** ServerAuthoritative adapter 产生 ExternalPoseCorrection
- **THEN** external input stage MUST 在 Graph/Motion 前收集该语义输入
- **AND** endpoint MUST 不由 CharacterPipeline Pump

### Requirement: CharacterPipeline 支持混合架构 authority mode

系统 MUST 删除把 LocalPredicted、RemoteProxy 和 PresentationOnly 混为单一 authority mode 的合同，并使用独立 CharacterInputSource 与 CharacterMotionAuthority 表达行为。所有合法组合 MUST 继续使用同一 CharacterPipeline 主线；Network Model MUST 只在 actor binding 时选择组合，不得在 Pipeline 内按 model id 分支。

#### Scenario: 当前本地 Owner

- **WHEN** input source 是 LocalDevice 且 motion authority 是 LocalSolver
- **THEN** Pipeline MUST 采样本地输入并结算本地运动
- **AND** 是否网络预测 MUST 不由 Pipeline enum 决定

#### Scenario: 后续外部位姿角色

- **WHEN** input source 是 ExternalFacts 且 motion authority 是 ExternalPose
- **THEN** Pipeline MUST 使用外部输入驱动 gameplay/animation
- **AND** MUST 不调用 LocalSolver 修改逻辑位姿

#### Scenario: 纯展示角色

- **WHEN** input source 和 motion authority 都是 None
- **THEN** Pipeline MUST 不采样控制输入或结算 gameplay motion
- **AND** Presentation MAY 继续消费显式表现数据

### Requirement: NetworkStage 是正式边界但不实现真实 transport

CharacterPipeline 中的 network/fact stages MUST 只暴露 Character gameplay facts和接收 Character 语义外部输入。它们 MUST 不认识 ServerAuthoritative packet、Rollback bundle、LocalLoopback、Fantasy Session、endpoint、transport 或 model policy resolver。Model-owned adapter MUST 在 Pipeline 外完成 policy 和 packet 映射。

#### Scenario: NetworkSendStage 收集输出

- **WHEN** Pipeline 本 tick 产生 resolved motion 和 Action facts
- **THEN** stage MUST 保留稳定 fact identity
- **AND** MUST 不构造 MotionCommand 或 ActionActivation packet

#### Scenario: NetworkReceiveStage 接收输入

- **WHEN** model adapter 注入 `ActionLifecycleTransition`
- **THEN** stage MUST 缓存并交给正式 action stage
- **AND** MUST 不保存原始 model packet

### Requirement: Pipeline 输出分为 strict、presentation 和 sync facts

CharacterPipelineOutput MUST 继续区分 StrictGameplayOutput、PresentationOutput 和 SyncFacts。SyncFacts MUST 表达已发生、可被 recording、debug 或 Network Model 消费的事实，MUST 不等同于 packet、model command、history 或 transport API。Resolved motion 与 correction application result MUST 作为事实提供，具体模型 adapter MAY 据此构造自己的协议输出。

#### Scenario: 单机 Pipeline

- **WHEN** 没有 Network Model endpoint
- **THEN** Pipeline MUST 继续产出必要 gameplay facts
- **AND** 不得因为无人发送而构造空 packet 或 fallback model
