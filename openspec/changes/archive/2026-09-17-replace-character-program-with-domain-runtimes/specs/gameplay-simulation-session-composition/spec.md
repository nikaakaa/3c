## MODIFIED Requirements

### Requirement: SimulationSessionHost 必须是 Unity Session Composition 的唯一 owner

Unity gameplay场景 MUST以唯一 `SimulationSessionHost` 持有 Session preparation、Composition Definition、compiled Pipeline plan、runtime handle、Actor launch roster、GameplayTickSystem logic registration与销毁顺序。单个 `CharacterPipelineHost` MUST不创建 Session Source、WorldSolver、Gameplay Runtime、Execution Backend、Pipeline Runtime或独立 Logic target。一个 Active Session无论包含多少 Actor，MUST只存在一个 runtime handle和一个正式 world owner。

#### Scenario: 两个 Actor 进入同一 Local Session

- **WHEN** SimulationSessionHost使用两个合法 Actor registration创建 Local Session
- **THEN** MUST只创建一个 Float32 Pass Pipeline runtime和一个 Unity WorldSolver
- **AND** 两个 CharacterPipelineHost MUST不各自创建 Session

#### Scenario: Active Actor 随场景退出

- **WHEN** 任一已锁定 Actor Host 被禁用、销毁或随场景卸载
- **THEN** SimulationSessionHost MUST停止整个不可变 Session并按 owner顺序释放全部 roster与 runtime资源
- **AND** 正常停止 MUST不报告 `active_actor_registration_released`，也 MUST不保留可重新启用的暂停 runtime



### Requirement: Session Composition 必须显式选择五个组成部分

`SimulationSessionCompositionDefinition` MUST显式引用一个 Gameplay Runtime Definition、一个 Execution Backend Definition、一个 Pipeline Definition、一个 Session Source Definition与一个 WorldSolver Definition。Host MUST不通过 enum、类型名、已安装实现扫描、第一个可用对象或默认值选择任何组成部分。Local Source与 Gameplay Network Model Source MAY复用相同 composition contract，但 Local MUST不被声明为 Network Model。

#### Scenario: Local Float32 组合

- **WHEN** 作者配置 Float32 Gameplay Runtime、Float32 Pass Backend、Standard Local Pipeline、Local Session Source与 Unity CharacterController Solver
- **THEN** Host MUST只按五个显式引用创建组合
- **AND** 缺少任一引用时 MUST在创建 Runtime前失败



### Requirement: Session Source 必须通过 Preparation 产生完整 Launch Plan

Session Source MUST创建 `ISimulationSessionPreparation`，并只返回 Pending、Ready或 Failed。只有 Ready preparation MAY产生一次不可变 `SimulationSessionLaunchPlan`；Launch Plan MUST包含 Session identity、TickRate、Gameplay Runtime/Target ABI、Backend、compiled Pipeline plan/hash、GameplayContentCatalog、完整 Actor roster、Source ports、Solver、Snapshot codec、Committer、initial Character/World/Pipeline state与 diagnostics identity。Preparation MUST不把半成品 Runtime暴露给 Host，也 MUST不在失败时切换其它 Source、Pipeline、Backend或 Solver。

#### Scenario: Network Model 等待 roster

- **WHEN** 后续 Network Model endpoint已连接但 canonical roster尚未到齐
- **THEN** preparation MUST保持 Pending且不得创建 Pipeline Runtime
- **AND** roster完整后 MUST以一个锁定 Launch Plan进入 Ready



### Requirement: Session Host 必须使用 Numeric-Neutral Runtime Handle

公共 Session Host MUST只持有 `ISimulationSessionRuntimeHandle` 与 `SimulationSessionCompositionDescriptor`。Runtime handle MUST提供外层 LogicTick、状态/身份查询与 Dispose，不得暴露 Float32/Fixed 领域运行、Character/World/Pipeline state、Source、Solver、Snapshot或可变 World数据。Host MUST不转换 Numeric Target数据，也 MUST不解释 Pipeline Pass。

#### Scenario: Fixed Rollback Session 接入 Host

- **WHEN** Fixed Gameplay Runtime与 Deterministic Backend返回合法 runtime handle
- **THEN** 同一个 SimulationSessionHost lifecycle MUST能推进该 handle
- **AND** Host MUST不引用 Fixed scalar、Fixed state、Rollback Pass或 KCC具体类型



### Requirement: Target-specific Composer 必须唯一创建完整 Runtime

每个已安装 Gameplay Runtime/Execution Backend组合 MUST通过唯一强类型 Composer集中校验并创建GameplayContentCatalog、compiled Pipeline plan、roster、initial state、Source ports、Kernel services、WorldSolver、Snapshot codec、Committer与diagnostics。当前Float32 Pass Backend MUST只有一个位于portable source set的正式Composer入口。Unity target adapter MUST只把五项显式Composition与Actor registration降低为一个完整portable request，并通过Prepared Source显式提供的Runtime Launcher调用该Composer。Runtime Launcher MAY增加模型专属启动约束，但 MUST不复制Runtime构造、Pipeline compile、LaunchPlan、identity或capability校验。Common Host、Unity Composer、Character Host、Preview和Demo MUST不识别具体Network Model、Prepared Source或Pipeline Definition类型。

#### Scenario: Local 与 ServerAuthoritative Prediction 共用 Float32 基座

- **WHEN** Local Pipeline与ServerAuthoritative Prediction Pipeline都选择Float32 Gameplay Runtime和Float32 Pass Backend
- **THEN** 两者 MUST通过Standard Runtime Launcher进入同一个target-specific Composer
- **AND** 差异 MUST存在于Source和Pipeline Pass，不得存在两份Float32 Session构造器

#### Scenario: ServerAuthoritative Authority增加Host约束

- **WHEN** Authority Prepared Source提供带Source policy与locked roster的Authority Runtime Launcher
- **THEN** Launcher MUST完成模型专属启动校验后调用同一个portable Float32 Composer
- **AND** 公共Unity Composer MUST不引用Authority Prepared Source、Authority Pipeline Definition或Host launch具体类型

#### Scenario: Fantasy DotRecast Authority Scene装配Float32 Session

- **WHEN** Fantasy Server内DotRecast Authority Scene提供合法Float32 Gameplay Runtime、Runtime Package、Source ports、Launcher、Solver与输出端口
- **THEN** MUST调用与Unity相同的模型Launcher和portable Float32 Composer
- **AND** MUST不复制Unity Composer、Pipeline compiler或LaunchPlan构造逻辑



### Requirement: Actor Registration 必须在 Active 前形成不可变 roster

Character Actor Host MUST提供带显式ActorId、技能与领域配置、Projection、抽象Float32 World body binding、可选local input、Presentation/output port与diagnostics metadata的不可变registration。通用registration与Character Host MUST不暴露或要求`UnityCharacterControllerWorldBodyBinding`具体类型。每个具体WorldSolver Definition MUST在Active前校验binding实现与自己匹配；Unity CharacterController Solver MUST只接受CC binding，DotRecast Solver MUST只接受state-only DotRecast binding。Session preparation MUST在Active前校验ActorId唯一性、玩法模块／动画绑定 identity、GameplayContentCatalog binding、当前Pipeline/Source/Solver所需端口与initial state；Active后 MUST不增删Actor、不换角色玩法、修改binding或切换Solver。

#### Scenario: DotRecast Composition注册Actor

- **WHEN** Composition收到显式state-only DotRecast binding
- **THEN** 同一Character Host MUST建立正式Actor registration
- **AND** registration MUST不要求CharacterController或第二Character Host

#### Scenario: Binding类型错误

- **WHEN** DotRecast Solver Definition收到CC binding
- **THEN** Composition MUST在创建World前失败
- **AND** MUST不搜索替代binding或切换Solver



### Requirement: Session Host 必须按正式 Tick 生命周期推进 Preparation 与 Runtime

SimulationSessionHost MUST通过 GameplayTickSystem正式 Input/Logic target推进 Preparation与 Active Runtime，不得创建私有 Update、协程、Task loop或 Network Model专用 runner。Preparing状态 MUST不执行 角色玩法 Tick；Active状态每个 LocalLogicTick MUST只调用一次 runtime handle。该 handle MAY按 compiled ExecutionPlan执行零到多个内部 SimulationTick。Presentation target MAY按 Actor独立存在，但其注册和释放 MUST归当前 Active composition。

#### Scenario: 一个外层 Tick 触发多个 Replay Step

- **WHEN** Active runtime handle收到一个 LocalLogicTick并由 Schedule Pass生成三个内部 step
- **THEN** GameplayTickSystem MUST仍只调用一次 Session target
- **AND** 三个 step MUST由同一个 Pipeline transaction推进



### Requirement: Session Composition 必须锁定完整身份与真实 capability

Active descriptor MUST记录 SessionId、source clock、TickRate、Gameplay Runtime/NumericProfile/Target ABI、GameplayContentCatalogHash、roster、BackendId/semantic version、PipelineId/Revision/Hash、SourceId、Solver identity/version/capabilities、Snapshot codec、Committer与可选 Model/Endpoint identity。Composer MUST在首 Tick前校验所有 identity与 角色玩法/Pass capability union；显示名、Inspector状态或 capability位 MUST不能代替实际对象和 factory校验。

#### Scenario: Pipeline 要求 Solver 未支持能力

- **WHEN** 角色玩法与 Pass capability union包含当前 Solver未声明的能力
- **THEN** Composer MUST拒绝创建 runtime handle
- **AND** Host MUST不改用另一个 Solver、删除 Pass或忽略 capability



### Requirement: Composition必须校验Body Motion与Solver垂直能力

Session Composition MUST从compiled GameplayContentCatalog读取Body Motion descriptor与required world capability union，并在Runtime Launcher创建Session前验证选定WorldSolver真实支持`AirborneVerticalMotion`。Capability校验 MUST不按Network Model、Scene、Actor或Host放宽；失败 MUST按现有owner释放已经准备的资源，MUST不切换Solver、关闭重力或使用Grounded-only fallback。错误 MUST包含角色玩法 Catalog identity、Solver identity、Solver capabilities与精确缺失能力。

#### Scenario: Solver缺少AirborneVerticalMotion

- **WHEN** Program要求AirborneVerticalMotion但Solver descriptor不支持
- **THEN** Preparation MUST fail-closed
- **AND** Runtime Launcher MUST不创建Session runtime



### Requirement: Local Launch Plan必须锁定Control Source与Observation能力

Local Session Preparation MUST为完整Actor roster显式生成不可变Control Source roster。每个entry MUST包含ActorId、Control Source identity、Numeric ABI、Character 角色玩法 binding与所需runtime capability；Behavior Designer AI entry MUST同时绑定插件行为内容 identity、任务/插件版本、输入所有权与Committed Actor Pose observation schema。Launch Plan和Composition identity MUST包含这些binding。公共Host、Composer、Ingress与Source MUST不按具体插件类型、Actor名称、Tag、第一个可用实现或fallback选择Control Source，Active后 MUST不替换Control Source或Observation provider。

#### Scenario: Local插件AI Actor准备完成

- **WHEN** AI Actor的Behavior Designer内容、Character 角色玩法、输入所有权与Committed Observation capability全部匹配
- **THEN** Preparation MUST把其插件AI Control Source作为锁定Actor entry写入Launch Plan
- **AND** Standard Runtime Launcher MUST沿现有target-specific Composer创建唯一Session runtime

#### Scenario: Composition缺少Observation capability

- **WHEN** Actor绑定插件AI Control Source但当前Source、Pipeline或Execution Backend没有声明匹配Committed Observation schema
- **THEN** Preparation MUST在Session Active前失败并报告ActorId、Behavior identity与缺失capability
- **AND** MUST不替换为Neutral Source或创建Session查询旁路



## ADDED Requirements

### Requirement: Session必须显式装配领域运行模块而非角色Program

Session MUST继续显式选择 Gameplay Runtime、Execution Backend、Pipeline、Session Source 和 WorldSolver。原 Program Runtime 安装项 MUST迁为数值目标与领域模块服务，不增加重复选择；角色 registration MUST引用领域配置、技能数据和完整状态合同，MUST不依赖整体角色 Program 或 Pose 产物。

#### Scenario: Local与网络复用角色执行
- **WHEN** Local 和网络会话使用相同角色玩法模块及合法的不同 Pipeline
- **THEN** 两者 MUST通过同一个角色执行接口推进玩法，且 Pipeline 改变不要求重新编译技能
