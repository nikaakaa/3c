# gameplay-simulation-session-composition Specification

## Purpose
定义 Unity gameplay Session 的唯一装配 owner、不可变 Actor roster、正式 Tick 生命周期、失败关闭与资源销毁边界。
## Requirements
### Requirement: SimulationSessionHost 必须是 Unity Session Composition 的唯一 owner

Unity gameplay场景 MUST以唯一 `SimulationSessionHost` 持有 Session preparation、Composition Definition、compiled Pipeline plan、runtime handle、Actor launch roster、GameplayTickSystem logic registration与销毁顺序。单个 `CharacterPipelineHost` MUST不创建 Session Source、WorldSolver、Program Runtime、Execution Backend、Pipeline Runtime或独立 Logic target。一个 Active Session无论包含多少 Actor，MUST只存在一个 runtime handle和一个正式 world owner。

#### Scenario: 两个 Actor 进入同一 Local Session

- **WHEN** SimulationSessionHost使用两个合法 Actor registration创建 Local Session
- **THEN** MUST只创建一个 Float32 Pass Pipeline runtime和一个 Unity WorldSolver
- **AND** 两个 CharacterPipelineHost MUST不各自创建 Session

#### Scenario: Active Actor 随场景退出

- **WHEN** 任一已锁定 Actor Host 被禁用、销毁或随场景卸载
- **THEN** SimulationSessionHost MUST停止整个不可变 Session并按 owner顺序释放全部 roster与 runtime资源
- **AND** 正常停止 MUST不报告 `active_actor_registration_released`，也 MUST不保留可重新启用的暂停 runtime

### Requirement: Session Composition 必须显式选择五个组成部分

`SimulationSessionCompositionDefinition` MUST显式引用一个 Program Runtime Definition、一个 Execution Backend Definition、一个 Pipeline Definition、一个 Session Source Definition与一个 WorldSolver Definition。Host MUST不通过 enum、类型名、已安装实现扫描、第一个可用对象或默认值选择任何组成部分。Local Source与 Gameplay Network Model Source MAY复用相同 composition contract，但 Local MUST不被声明为 Network Model。

#### Scenario: Local Float32 组合

- **WHEN** 作者配置 Float32 Program Runtime、Float32 Pass Backend、Standard Local Pipeline、Local Session Source与 Unity CharacterController Solver
- **THEN** Host MUST只按五个显式引用创建组合
- **AND** 缺少任一引用时 MUST在创建 Runtime前失败

### Requirement: Program Runtime 与 Execution Backend 必须是独立选择维度

Program Runtime Definition MUST只拥有 NumericProfile、Target ABI、Program/State/Kernel/Snapshot codec与 Target services；Execution Backend Definition MUST只拥有 Pipeline descriptor编译、Pass runtime、working transaction与 outer runtime handle创建。Target-specific Composer MUST强类型校验二者兼容。同一 Program Runtime MAY与多个兼容 Backend组合，但 Common Host MUST不做 Float/Fixed转换、反射调用或 runtime backend switch。

#### Scenario: 同一 Float32 Runtime 选择不同 Backend

- **WHEN** 后续安装另一个明确支持 Float32 Program ABI的 Execution Backend
- **THEN** Composition MAY显式选择该 Backend与合法 Pipeline
- **AND** MUST不修改 CharacterPipelineHost或把 Backend类型写进 Program

### Requirement: Session Source 必须通过 Preparation 产生完整 Launch Plan

Session Source MUST创建 `ISimulationSessionPreparation`，并只返回 Pending、Ready或 Failed。只有 Ready preparation MAY产生一次不可变 `SimulationSessionLaunchPlan`；Launch Plan MUST包含 Session identity、TickRate、Program Runtime/Target ABI、Backend、compiled Pipeline plan/hash、ProgramCatalog、完整 Actor roster、Source ports、Solver、Snapshot codec、Committer、initial Character/World/Pipeline state与 diagnostics identity。Preparation MUST不把半成品 Runtime暴露给 Host，也 MUST不在失败时切换其它 Source、Pipeline、Backend或 Solver。

#### Scenario: Network Model 等待 roster

- **WHEN** 后续 Network Model endpoint已连接但 canonical roster尚未到齐
- **THEN** preparation MUST保持 Pending且不得创建 Pipeline Runtime
- **AND** roster完整后 MUST以一个锁定 Launch Plan进入 Ready

### Requirement: Session Host 必须使用 Numeric-Neutral Runtime Handle

公共 Session Host MUST只持有 `ISimulationSessionRuntimeHandle` 与 `SimulationSessionCompositionDescriptor`。Runtime handle MUST提供外层 LogicTick、状态/身份查询与 Dispose，不得暴露 Float32/Fixed Program、Character/World/Pipeline state、Source、Solver、Snapshot或可变 World数据。Host MUST不转换 Numeric Target数据，也 MUST不解释 Pipeline Pass。

#### Scenario: Fixed Rollback Session 接入 Host

- **WHEN** Fixed Program Runtime与 Deterministic Backend返回合法 runtime handle
- **THEN** 同一个 SimulationSessionHost lifecycle MUST能推进该 handle
- **AND** Host MUST不引用 Fixed scalar、Fixed state、Rollback Pass或 KCC具体类型

### Requirement: Target-specific Composer 必须唯一创建完整 Runtime

每个已安装 Program Runtime/Execution Backend组合 MUST通过唯一强类型 Composer集中校验并创建ProgramCatalog、compiled Pipeline plan、roster、initial state、Source ports、Kernel services、WorldSolver、Snapshot codec、Committer与diagnostics。当前Float32 Pass Backend MUST只有一个位于portable source set的正式Composer入口。Unity target adapter MUST只把五项显式Composition与Actor registration降低为一个完整portable request，并通过Prepared Source显式提供的Runtime Launcher调用该Composer。Runtime Launcher MAY增加模型专属启动约束，但 MUST不复制Runtime构造、Pipeline compile、LaunchPlan、identity或capability校验。Common Host、Unity Composer、Character Host、Preview和Demo MUST不识别具体Network Model、Prepared Source或Pipeline Definition类型。

#### Scenario: Local 与 ServerAuthoritative Prediction 共用 Float32 基座

- **WHEN** Local Pipeline与ServerAuthoritative Prediction Pipeline都选择Float32 Program Runtime和Float32 Pass Backend
- **THEN** 两者 MUST通过Standard Runtime Launcher进入同一个target-specific Composer
- **AND** 差异 MUST存在于Source和Pipeline Pass，不得存在两份Float32 Session构造器

#### Scenario: ServerAuthoritative Authority增加Host约束

- **WHEN** Authority Prepared Source提供带Source policy与locked roster的Authority Runtime Launcher
- **THEN** Launcher MUST完成模型专属启动校验后调用同一个portable Float32 Composer
- **AND** 公共Unity Composer MUST不引用Authority Prepared Source、Authority Pipeline Definition或Host launch具体类型

#### Scenario: Fantasy DotRecast Authority Scene装配Float32 Session

- **WHEN** Fantasy Server内DotRecast Authority Scene提供合法Float32 Program Runtime、Runtime Package、Source ports、Launcher、Solver与输出端口
- **THEN** MUST调用与Unity相同的模型Launcher和portable Float32 Composer
- **AND** MUST不复制Unity Composer、Pipeline compiler或LaunchPlan构造逻辑

### Requirement: Actor Registration 必须在 Active 前形成不可变 roster

Character Actor Host MUST提供带显式ActorId、Program artifact、Projection、抽象Float32 World body binding、可选local input、Presentation/output port与diagnostics metadata的不可变registration。通用registration与Character Host MUST不暴露或要求`UnityCharacterControllerWorldBodyBinding`具体类型。每个具体WorldSolver Definition MUST在Active前校验binding实现与自己匹配；Unity CharacterController Solver MUST只接受CC binding，DotRecast Solver MUST只接受state-only DotRecast binding。Session preparation MUST在Active前校验ActorId唯一性、Program/Projection identity、ProgramCatalog binding、当前Pipeline/Source/Solver所需端口与initial state；Active后 MUST不增删Actor、不换Program、修改binding或切换Solver。

#### Scenario: DotRecast Composition注册Actor

- **WHEN** Composition收到显式state-only DotRecast binding
- **THEN** 同一Character Host MUST建立正式Actor registration
- **AND** registration MUST不要求CharacterController或第二Character Host

#### Scenario: Binding类型错误

- **WHEN** DotRecast Solver Definition收到CC binding
- **THEN** Composition MUST在创建World前失败
- **AND** MUST不搜索替代binding或切换Solver

### Requirement: Session Host 必须按正式 Tick 生命周期推进 Preparation 与 Runtime

SimulationSessionHost MUST通过 GameplayTickSystem正式 Input/Logic target推进 Preparation与 Active Runtime，不得创建私有 Update、协程、Task loop或 Network Model专用 runner。Preparing状态 MUST不执行 Program Tick；Active状态每个 LocalLogicTick MUST只调用一次 runtime handle。该 handle MAY按 compiled ExecutionPlan执行零到多个内部 SimulationTick。Presentation target MAY按 Actor独立存在，但其注册和释放 MUST归当前 Active composition。

#### Scenario: 一个外层 Tick 触发多个 Replay Step

- **WHEN** Active runtime handle收到一个 LocalLogicTick并由 Schedule Pass生成三个内部 step
- **THEN** GameplayTickSystem MUST仍只调用一次 Session target
- **AND** 三个 step MUST由同一个 Pipeline transaction推进

### Requirement: Session Composition 必须锁定完整身份与真实 capability

Active descriptor MUST记录 SessionId、source clock、TickRate、Program Runtime/NumericProfile/Target ABI、ProgramCatalogHash、roster、BackendId/semantic version、PipelineId/Revision/Hash、SourceId、Solver identity/version/capabilities、Snapshot codec、Committer与可选 Model/Endpoint identity。Composer MUST在首 Tick前校验所有 identity与 Program/Pass capability union；显示名、Inspector状态或 capability位 MUST不能代替实际对象和 factory校验。

#### Scenario: Pipeline 要求 Solver 未支持能力

- **WHEN** Program与 Pass capability union包含当前 Solver未声明的能力
- **THEN** Composer MUST拒绝创建 runtime handle
- **AND** Host MUST不改用另一个 Solver、删除 Pass或忽略 capability

### Requirement: Composition 失败必须 fail-closed 并按 owner 释放资源

Preparation、Pipeline compile、Composer或 Active Runtime任一阶段失败时，Session Host MUST进入 Failed，按明确 owner释放 Runtime/Pass、Source/Endpoint、Solver与 registration lifecycle，并停止后续 LogicTick。Actor activation与Host cleanup MUST是异常安全的：一个资源释放失败不得阻止其余已取得资源释放，且不得覆盖最初的Session Failure。系统 MUST不回退 Local、其它 Network Model、默认 Pipeline、默认 Solver、Transform直写或旧 Character Session。

#### Scenario: Pipeline factory 缺失

- **WHEN** Pipeline引用的一个 Pass factory未安装或 version不匹配
- **THEN** Host MUST保持 Failed并释放 preparation已创建资源
- **AND** MUST不跳过该 Pass或创建 Standard Local Pipeline继续运行

### Requirement: Unity WorldBodyBinding必须只有抽象合同与显式实现

Unity Float32 composition层 MUST提供唯一抽象WorldBodyBinding合同，包含BindingId、ActorId、InitialBody和严格校验。CC binding与DotRecast state-only binding MUST作为独立实现。抽象合同 MUST不包含CharacterController、Rigidbody、Transform写入或DotRecast类型；其它Composer、Source、Pipeline、Character Host和Presentation MUST只依赖抽象合同。

#### Scenario: 保留CC环境

- **WHEN** Composition选择Unity CharacterController Solver
- **THEN** CC binding MUST仍由该Solver adapter使用
- **AND** DotRecast binding MUST不获得CC组件

### Requirement: 公共Unity Composition必须由程序集依赖强制模型无关

公共Unity Session Composition、Float32 request lowering与标准Local/Preview authoring MUST位于不引用具体Network Model程序集的独立Unity程序集。Character Host和模型Unity adapter只能单向引用该公共程序集；它们 MUST不通过预定义程序集、friend assembly、反射、字符串类型查找或fallback registry绕过依赖方向。

#### Scenario: ServerAuthoritative Unity adapter被移除

- **WHEN** 构建中不包含ServerAuthoritative Unity程序集
- **THEN** Local与Preview Composition程序集 MUST仍可编译并创建正式Session
- **AND** 公共Composer源码 MUST不包含ServerAuthoritative类型或分支

### Requirement: Composition必须校验Body Motion与Solver垂直能力

Session Composition MUST从compiled ProgramCatalog读取Body Motion descriptor与required world capability union，并在Runtime Launcher创建Session前验证选定WorldSolver真实支持`AirborneVerticalMotion`。Capability校验 MUST不按Network Model、Scene、Actor或Host放宽；失败 MUST按现有owner释放已经准备的资源，MUST不切换Solver、关闭重力或使用Grounded-only fallback。错误 MUST包含Program Catalog identity、Solver identity、Solver capabilities与精确缺失能力。

#### Scenario: Solver缺少AirborneVerticalMotion

- **WHEN** Program要求AirborneVerticalMotion但Solver descriptor不支持
- **THEN** Preparation MUST fail-closed
- **AND** Runtime Launcher MUST不创建Session runtime
