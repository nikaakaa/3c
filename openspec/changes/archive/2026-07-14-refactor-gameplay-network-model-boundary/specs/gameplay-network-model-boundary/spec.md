## ADDED Requirements

### Requirement: Gameplay Network Model 必须是 Session 级唯一装配

系统 MUST 使用 `GameplayNetworkSessionHost`、`GameplayNetworkModelDefinition` 或等价边界，在一个 gameplay Session 启动前装配唯一完整 Network Model。Model MUST 决定同步输入、历史、确认、修正、Remote 推进和副作用提交规则。系统 MUST NOT 按 Character、Graph、State、Action 或 Timeline 分别选择模型，也 MUST NOT 在 Session 运行中切换模型。

#### Scenario: Session 启动当前模型

- **WHEN** Sandbox 启动 gameplay Session
- **THEN** SessionHost MUST 只创建一个 `ServerAuthoritativeHybrid` model session
- **AND** 所有 Character bindings MUST 归属该 model session

#### Scenario: 运行中修改模型

- **WHEN** model session 已连接、绑定 actor 或开始 tick
- **THEN** 系统 MUST 拒绝更换 model definition
- **AND** MUST 不迁移未确认事务或 history 到另一模型

### Requirement: Model、Endpoint 和 Transport 必须分层

系统 MUST 区分 Network Model、model Endpoint 和底层 Transport。Model MUST 表达 gameplay 同步规则；Endpoint MUST 表达该模型消息的远端实现；Transport MUST 只负责连接、序列化和收发。未配置 endpoint 的 disconnected 状态、`LocalLoopback` 和未来 `Fantasy` MUST NOT 被描述为三个同步模型。

#### Scenario: 选择 LocalLoopback

- **WHEN** `ServerAuthoritativeHybrid` 使用 LocalLoopback endpoint
- **THEN** gameplay prediction、correction、snapshot 和 action decision 语义 MUST 保持属于该模型
- **AND** Loopback MUST 只在进程内模拟模型远端

#### Scenario: 后续切换 Fantasy

- **WHEN** 后续 change 实现 Fantasy endpoint
- **THEN** 它 MUST 替换同一模型的 endpoint
- **AND** 该替换 MUST 不被宣传为 network model 切换

### Requirement: 只允许选择完整实现的 Network Model

Model authoring UI MUST 只显示已安装、可创建 runtime 且配置闭环的 model definition。系统 MUST NOT 暴露 `Rollback`、`Lockstep`、`Snapshot` 或其它只有 enum、空 factory、空 profile 或占位 runtime 的选项。

#### Scenario: 当前查看模型配置

- **WHEN** 作者查看 SessionHost model 配置
- **THEN** 可用正式模型 MUST 只有 `ServerAuthoritativeHybrid`
- **AND** UI MUST 不显示尚未实现的 Rollback

#### Scenario: 未来增加第二模型

- **WHEN** 后续 change 完整实现另一模型的 runtime、配置、actor binding 和 tick integration
- **THEN** 该模型 MAY 作为新的 definition 类型进入 Session 装配
- **AND** CharacterPipeline MUST 不增加 model id switch 才能使用它

### Requirement: Common Session Host 不得解释模型消息

Common SessionHost MUST 只管理 model definition、model session lifecycle 和唯一 ownership。它 MUST NOT 引用 MotionCommand、Snapshot、Correction、ActionDecision、Rollback input bundle、world snapshot 或 model policy 类型。

#### Scenario: Model Session 产生 packet

- **WHEN** 当前模型构造 MotionCommand 或 ActionActivation packet
- **THEN** packet MUST 只存在于 `ServerAuthoritativeHybrid` 模块
- **AND** common SessionHost MUST 不读取 packet kind 或 payload

### Requirement: Character Runtime 必须通过事实和语义输入连接模型

CharacterPipeline MUST 向模型暴露 input frame、resolved motion、Action lifecycle、window、result、state、cue 和 correction application result 等事实，并只接收 Character/gameplay 语义输入。CharacterPipeline MUST NOT 持有 model packet、model history、endpoint 或 transport。

#### Scenario: Owner 完成本 tick 运动

- **WHEN** CharacterMotionStage 完成 LocalSolver 结算
- **THEN** Pipeline MUST 输出 resolved motion fact
- **AND** ServerAuthoritative adapter MAY 将其转换为 MotionCommand
- **AND** Pipeline MUST 不直接创建 MotionCommand packet

#### Scenario: 收到动作确认

- **WHEN** ServerAuthoritative model 收到 ActionDecision packet
- **THEN** model adapter MUST 先转换为 Character 已有的 `ActionLifecycleTransition`
- **AND** prediction key、authority tick 和 defense-favor metadata MUST 留在模型内部
- **AND** CharacterNetworkReceiveStage MUST 不保存 model packet payload

### Requirement: Character 输入来源与运动权威必须正交

系统 MUST 使用独立的 `CharacterInputSource` 与 `CharacterMotionAuthority` 或等价合同表达角色输入和位姿结算。GameplayTickSystem 和 CharacterPipeline MUST NOT 依赖 `LocalPredicted`、`RemoteProxy` 或具体 network model 枚举决定全部行为。

#### Scenario: 服务端权威 Owner

- **WHEN** 当前 Session 创建本地 Owner
- **THEN** Character MUST 使用 LocalDevice input source
- **AND** MUST 使用 LocalSolver motion authority

#### Scenario: 未来 Rollback 远端 Actor

- **WHEN** 后续完整 Rollback 模型在本地模拟远端 actor
- **THEN** 它 MUST 能使用 ExternalFacts input source + LocalSolver motion authority
- **AND** CharacterPipeline MUST 不需要把该 actor 伪装成 ServerAuthoritative RemoteProxy

### Requirement: BTSMTL Authoring 不得拥有 Network Model 配置

BTSMTL Graph、StateMachine、Timeline、TreeClip、Blackboard、Animation authoring 和 Agent Patch MUST 只表达 gameplay 结构、身份和事实。它们 MUST NOT 保存 model id、endpoint、transport、prediction、snapshot、correction、replication 或 rollback 配置。

#### Scenario: 作者配置攻击窗口

- **WHEN** 作者在 Timeline TreeClip 配置 Attack HitWindow
- **THEN** TreeClip MUST 只表达时间、WindowType、WindowId 和 gameplay fact projection
- **AND** ServerAuthoritative model policy MUST 由模型专属 profile 解析
