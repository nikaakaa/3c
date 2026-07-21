## MODIFIED Requirements

### Requirement: CharacterPipelineHost 只负责装配和注册

CharacterPipelineHost MUST只加载并校验 CharacterPipelineDefinition 对应的 CharacterSimulationProgramAsset 与 Projection，建立显式 ActorId、World body binding、显式 Control Source、显式 Presentation Role、模型无关的 Gameplay output change port、Presentation output port 和 diagnostics metadata，并向显式 SimulationSessionHost 提供不可变 Actor registration。Gameplay output port MUST只记录当前 Tick 已提交的 Publish、Replace 与 Retire 变更，不得解释 history、correction、rollback 或 Network Model 策略。

Control Source MUST通过统一 Unity-facing input adapter 生命周期合同提供 portable `ISimulationInputAdapter`。Presentation Role MUST显式区分 `LocalOwner` 与 `SimulatedActor`：LocalOwner MAY拥有玩家设备、look input 与 Camera；SimulatedActor MUST不要求玩家设备或 Camera。CharacterPipelineHost MUST NOT根据 GameObject 名称、Camera 是否存在或 Network Model 推断两种角色。

CharacterPipelineHost MUST NOT创建 ProgramCatalog、Session Source、WorldSolver、Program Runtime、Execution Backend、Pipeline Runtime、Snapshot codec、Committer aggregate 或 Logic target，也 MUST NOT选择 Network Model 或 Pipeline。

#### Scenario: 注册单机 Corin 玩家

- **WHEN** Standalone 中的玩家 Corin CharacterPipelineHost 启用
- **THEN** MUST以 Player Control Source 与 LocalOwner Presentation Role 提交 Actor registration
- **AND** Local Source、标准 Pipeline、Float32 Backend 与 Unity Solver MUST只由 Session composition 创建

#### Scenario: 注册同 Session 训练敌人

- **WHEN** 训练敌人 CharacterPipelineHost 启用
- **THEN** MUST以 Neutral Control Source 与 SimulatedActor Presentation Role 提交独立 Actor registration
- **AND** MUST复用同一正式 Program、Projection、WorldSolver 与 Presenter 链
- **AND** MUST不创建第二个 Enemy gameplay runtime

#### Scenario: Egress 提交 Gameplay 纠偏变更

- **WHEN** 当前 Session 的 Egress OutputPlan 包含 Gameplay Fact Replace 或 Retire
- **THEN** Character Gameplay output port MUST记录对应 source EventId、target EventId、ActorId 与可选 Fact
- **AND** Character Host MUST不因当前组合是 Local 或 Network 而拒绝或改写该生命周期变更

## ADDED Requirements

### Requirement: 显式目标 provider 必须读取已提交逻辑 Body

Unity-facing Action Target Provider MUST通过显式配置绑定同一 SimulationSessionHost 中另一个稳定 ActorId，并只读取该 Actor 最近一次已提交的逻辑 Body pose。Registration MUST从 InitialBody 初始化该值，并在 published Body result 后更新。Provider MUST NOT读取 VisualRoot、Animator root、表现插值 Transform、Tag、名称、Scene 扫描或全局 registry。

#### Scenario: 玩家采样训练敌人目标

- **WHEN** 训练敌人已经注册并拥有最近提交 Body
- **THEN** provider MUST输出该 ActorId、逻辑 position 与 yaw
- **AND** 表现层插值或动画 root 变化 MUST不改变该输入事实

#### Scenario: 绑定另一个 Session 的 Actor

- **WHEN** provider 目标不属于 owner 的 SimulationSessionHost
- **THEN** Host activation MUST失败并报告精确配置错误
- **AND** provider MUST NOT按 ActorId 在其它 Session 或 Scene 中回退查找

#### Scenario: 绑定自己

- **WHEN** provider 的 target ActorId 与 owner ActorId 相同
- **THEN** Host activation MUST失败
- **AND** MUST NOT产生自身目标快照
