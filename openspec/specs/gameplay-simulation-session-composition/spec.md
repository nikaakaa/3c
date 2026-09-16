# gameplay-simulation-session-composition Specification

## Purpose
定义 Unity gameplay Session 的唯一装配 owner、不可变 Actor roster、正式 Tick 生命周期、失败关闭与资源销毁边界。
## Requirements
### Requirement: SimulationSessionHost 必须是 Unity Session Composition 的唯一 owner

Unity gameplay场景 MUST以唯一 `SimulationSessionHost` 持有 Session preparation、Composition Definition、compiled Pipeline plan、runtime handle、Actor launch roster、GameplayTickSystem logic registration与销毁顺序。单个 `CharacterPipelineHost` MUST不创建 Session Source、WorldSolver、Graph/domain Runtime、Execution Backend、Pipeline Runtime或独立 Logic target。一个 Active Session无论包含多少 Actor，MUST只存在一个 runtime handle和一个正式 world owner。

#### Scenario: 两个 Actor 进入同一 Local Session

- **WHEN** SimulationSessionHost使用两个合法 Actor registration创建 Local Session
- **THEN** MUST只创建一个 Float32 Pass Pipeline runtime和一个 Unity WorldSolver
- **AND** 两个 CharacterPipelineHost MUST不各自创建 Session

#### Scenario: Active Actor 随场景退出

- **WHEN** 任一已锁定 Actor Host 被禁用、销毁或随场景卸载
- **THEN** SimulationSessionHost MUST停止整个不可变 Session并按 owner顺序释放全部 roster与 runtime资源
- **AND** 正常停止 MUST不报告 `active_actor_registration_released`，也 MUST不保留可重新启用的暂停 runtime

### Requirement: Session Composition 必须显式选择五个组成部分

`SimulationSessionCompositionDefinition` MUST显式引用一个 Graph/domain Runtime Definition、一个 Execution Backend Definition、一个 Pipeline Definition、一个 Session Source Definition与一个 WorldSolver Definition。Host MUST不通过 enum、类型名、已安装实现扫描、第一个可用对象或默认值选择任何组成部分。Local Source与 Gameplay Network Model Source MAY复用相同 composition contract，但 Local MUST不被声明为 Network Model。

#### Scenario: Local Float32 组合

- **WHEN** 作者配置 Float32 Graph/domain Runtime、Float32 Pass Backend、Standard Local Pipeline、Local Session Source与 Unity CharacterController Solver
- **THEN** Host MUST只按五个显式引用创建组合
- **AND** 缺少任一引用时 MUST在创建 Runtime前失败

### Requirement: Domain Runtime 与 Execution Backend 必须是独立选择维度

Graph/domain Runtime Definition MUST只拥有 NumericProfile、Target ABI、Graph/domain state、Kernel、Snapshot codec与 Target services；Execution Backend Definition MUST只拥有 Pipeline descriptor编译、Pass runtime、working transaction与 outer runtime handle创建。Target-specific Composer MUST强类型校验二者兼容。同一 Graph/domain Runtime MAY与多个兼容 Backend组合，但 Common Host MUST不做 Float/Fixed转换、反射调用或 runtime backend switch。

#### Scenario: 同一 Float32 Runtime 选择不同 Backend

- **WHEN** 后续安装另一个明确支持 Float32 domain ABI的 Execution Backend
- **THEN** Composition MAY显式选择该 Backend与合法 Pipeline
- **AND** MUST不修改 CharacterPipelineHost或把 Backend类型写进 Graph/domain artifact

### Requirement: Session Source 必须通过 Preparation 产生完整 Launch Plan

Session Source MUST 创建 `ISimulationSessionPreparation`，并只返回 Pending、Ready 或 Failed。只有 Ready preparation MAY 产生一次不可变 `SimulationSessionLaunchPlan`；Launch Plan MUST 包含 Session identity、TickRate、Graph/domain runtime/numeric ABI、Backend、compiled Pipeline plan/hash、领域 binding、完整 Actor roster、Source ports、Solver、Snapshot codec、Committer、initial Character/World/Pipeline state 与 diagnostics identity。Preparation MUST 不把半成品 Runtime 暴露给 Host，也 MUST 不在失败时切换其它 Source、Pipeline、Backend 或 Solver。

#### Scenario: Network Model 等待 roster

- **WHEN** 后续 Network Model endpoint已连接但 canonical roster尚未到齐
- **THEN** preparation MUST保持 Pending且不得创建 Pipeline Runtime
- **AND** roster完整后 MUST以一个锁定 Launch Plan进入 Ready

### Requirement: Session Host 必须使用 Numeric-Neutral Runtime Handle

公共 Session Host MUST只持有 `ISimulationSessionRuntimeHandle` 与 `SimulationSessionCompositionDescriptor`。Runtime handle MUST提供外层 LogicTick、状态/身份查询与 Dispose，不得暴露 Float32/Fixed Graph/domain runtime、Character/World/Pipeline state、Source、Solver、Snapshot或可变 World数据。Host MUST不转换 Numeric Target数据，也 MUST不解释 Pipeline Pass。

#### Scenario: Fixed Rollback Session 接入 Host

- **WHEN** Fixed Graph/domain Runtime与 Deterministic Backend返回合法 runtime handle
- **THEN** 同一个 SimulationSessionHost lifecycle MUST能推进该 handle
- **AND** Host MUST不引用 Fixed scalar、Fixed state、Rollback Pass或 KCC具体类型

### Requirement: Target-specific Composer 必须唯一创建完整 Runtime

每个已安装 Graph/domain Runtime/Execution Backend 组合 MUST 通过唯一强类型 Composer 集中校验并创建领域 binding、compiled Pipeline plan、roster、initial state、Source ports、Control/Ability/Timeline services、WorldSolver、Snapshot codec、Committer 与 diagnostics。当前 Float32 Pass Backend MUST 只有一个位于 portable source set 的正式 Composer 入口。Unity target adapter MUST 只把五项显式 Composition 与 Actor registration 降低为一个完整 portable request，并通过 Prepared Source 显式提供的 Runtime Launcher 调用该 Composer。Runtime Launcher MAY 增加模型专属启动约束，但 MUST 不复制 Runtime 构造、Pipeline compile、LaunchPlan、identity 或 capability 校验。Common Host、Unity Composer、Character Host、ScenePlay 和 Demo MUST 不识别具体 Network Model、Prepared Source 或 Pipeline Definition 类型。

#### Scenario: Local 与 ServerAuthoritative Prediction 共用 Float32 基座

- **WHEN** Local Pipeline与ServerAuthoritative Prediction Pipeline都选择Float32 Graph/domain Runtime和Float32 Pass Backend
- **THEN** 两者 MUST通过Standard Runtime Launcher进入同一个target-specific Composer
- **AND** 差异 MUST存在于Source和Pipeline Pass，不得存在两份Float32 Session构造器

#### Scenario: ServerAuthoritative Authority增加Host约束

- **WHEN** Authority Prepared Source提供带Source policy与locked roster的Authority Runtime Launcher
- **THEN** Launcher MUST完成模型专属启动校验后调用同一个portable Float32 Composer
- **AND** 公共Unity Composer MUST不引用Authority Prepared Source、Authority Pipeline Definition或Host launch具体类型

#### Scenario: Fantasy DotRecast Authority Scene装配Float32 Session

- **WHEN** Fantasy Server内DotRecast Authority Scene提供合法Float32 Graph/domain Runtime、Runtime Package、Source ports、Launcher、Solver与输出端口
- **THEN** MUST调用与Unity相同的模型Launcher和portable Float32 Composer
- **AND** MUST不复制Unity Composer、Pipeline compiler或LaunchPlan构造逻辑

### Requirement: Actor Registration 必须在 Active 前形成不可变 roster

Character Actor Host MUST 提供带显式 ActorId、Control/Ability/Timeline/Presentation binding、抽象 World body binding、可选 local input、Presentation/output port 与 diagnostics metadata 的不可变 registration。通用 registration 与 Character Host MUST 不暴露或要求 `UnityCharacterControllerWorldBodyBinding` 具体类型。每个具体 WorldSolver Definition MUST 在 Active 前校验 binding 实现与自己匹配；Unity CharacterController Solver MUST 只接受 CC binding，DotRecast Solver MUST 只接受 state-only DotRecast binding。Session preparation MUST 在 Active 前校验 ActorId 唯一性、各领域 identity、当前 Pipeline/Source/Solver 所需端口与 initial state；Active 后 MUST 不增删 Actor、替换领域 owner、修改 binding 或切换 Solver。

#### Scenario: DotRecast Composition注册Actor

- **WHEN** Composition收到显式state-only DotRecast binding
- **THEN** 同一Character Host MUST建立正式Actor registration
- **AND** registration MUST不要求CharacterController或第二Character Host

#### Scenario: Binding类型错误

- **WHEN** DotRecast Solver Definition收到CC binding
- **THEN** Composition MUST在创建World前失败
- **AND** MUST不搜索替代binding或切换Solver

### Requirement: Session Host 必须按正式 Tick 生命周期推进 Preparation 与 Runtime

SimulationSessionHost MUST通过 GameplayTickSystem正式 Input/Logic target推进 Preparation与 Active Runtime，不得创建私有 Update、协程、Task loop或 Network Model专用 runner。Preparing状态 MUST不执行 Gameplay Tick；Active状态每个 LocalLogicTick MUST只调用一次 runtime handle。该 handle MAY按 compiled ExecutionPlan执行零到多个内部 SimulationTick。Presentation target MAY按 Actor独立存在，但其注册和释放 MUST归当前 Active composition。

#### Scenario: 一个外层 Tick 触发多个 Replay Step

- **WHEN** Active runtime handle收到一个 LocalLogicTick并由 Schedule Pass生成三个内部 step
- **THEN** GameplayTickSystem MUST仍只调用一次 Session target
- **AND** 三个 step MUST由同一个 Pipeline transaction推进

### Requirement: Session Composition 必须锁定完整身份与真实 capability

Active descriptor MUST 记录 SessionId、source clock、TickRate、domain runtime/NumericProfile/Target ABI、roster、BackendId/semantic version、PipelineId/Revision/Hash、SourceId、Solver identity/version/capabilities、Snapshot codec、Committer 与可选 Model/Endpoint identity。Composer MUST 在首 Tick 前校验所有 identity 与 domain/Pass capability union；显示名、Inspector 状态或 capability 位 MUST 不能代替实际对象和 factory 校验。

#### Scenario: Pipeline 要求 Solver 未支持能力

- **WHEN** Graph/domain binding与 Pass capability union包含当前 Solver未声明的能力
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

公共Unity Session Composition、Float32 request lowering与标准Local authoring MUST位于不引用具体Network Model程序集的独立Unity程序集。Character Host和模型Unity adapter只能单向引用该公共程序集；它们 MUST不通过预定义程序集、friend assembly、反射、字符串类型查找或fallback registry绕过依赖方向。Timeline Authoring Preview MUST属于Character Presentation边界，只复用表现sampling与playback lifecycle，并且 MUST不创建Simulation Session、Source、Pipeline、Backend、Solver或Actor registration。

#### Scenario: ServerAuthoritative Unity adapter被移除

- **WHEN** 构建中不包含ServerAuthoritative Unity程序集
- **THEN** Local Composition程序集 MUST仍可编译并创建正式Session
- **AND** 公共Composer源码 MUST不包含ServerAuthoritative类型或分支
- **AND** Timeline Authoring Preview MUST不依赖该Composer创建Session

### Requirement: Composition必须校验Body Motion与Solver垂直能力

Session Composition MUST 从正式 Body Motion binding 和 domain capability union 读取 Body Motion descriptor，并在 Runtime Launcher 创建 Session 前验证选定 WorldSolver 真实支持 `AirborneVerticalMotion`。Capability 校验 MUST 不按 Network Model、Scene、Actor 或 Host 放宽；失败 MUST 按现有 owner 释放已经准备的资源，MUST 不切换 Solver、关闭重力或使用 Grounded-only fallback。错误 MUST 包含 domain binding identity、Solver identity、Solver capabilities 与精确缺失能力。

#### Scenario: Solver缺少AirborneVerticalMotion

- **WHEN** Graph/domain binding要求AirborneVerticalMotion但Solver descriptor不支持
- **THEN** Preparation MUST fail-closed
- **AND** Runtime Launcher MUST不创建Session runtime

### Requirement: Local Launch Plan必须锁定Control Source与Observation能力

Local Session Preparation MUST 为完整 Actor roster 显式生成不可变 Control Source roster。每个 entry MUST 包含 ActorId、Control Source identity、Numeric ABI、各领域 binding 与所需 runtime capability；Behavior Designer AI entry MUST 同时绑定插件行为内容 identity、任务/插件版本、输入所有权与 Committed Actor Pose observation schema。Launch Plan 和 Composition identity MUST 包含这些 binding。公共 Host、Composer、Ingress 与 Source MUST 不按具体插件类型、Actor 名称、Tag、第一个可用实现或 fallback 选择 Control Source，Active 后 MUST 不替换 Control Source 或 Observation provider。

#### Scenario: Local插件AI Actor准备完成

- **WHEN** AI Actor 的 Behavior Designer 内容、各领域 binding、输入所有权与 Committed Observation capability 全部匹配
- **THEN** Preparation MUST把其插件AI Control Source作为锁定Actor entry写入Launch Plan
- **AND** Standard Runtime Launcher MUST沿现有target-specific Composer创建唯一Session runtime

#### Scenario: Composition缺少Observation capability

- **WHEN** Actor绑定插件AI Control Source但当前Source、Pipeline或Execution Backend没有声明匹配Committed Observation schema
- **THEN** Preparation MUST在Session Active前失败并报告ActorId、Behavior identity与缺失capability
- **AND** MUST不替换为Neutral Source或创建Session查询旁路
