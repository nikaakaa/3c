## ADDED Requirements

### Requirement: AI Program必须只消费Perception并产出Character Input

系统 MUST将AIControllerTree编译为独立versioned AI Semantic IR与`AIIntentProgram`。AI Program MUST复用现有唯一portable Runnable control topology/runtime对Sequence、Selector、Parallel、Decorator、Running、activation、abort与value edge的实现，只增加Observation、AIMemory与AIIntent operation，并直接产出匹配受控Character Program catalog的`CharacterSimulationInput`。AI Program MUST NOT复制第二套control evaluator，也 MUST NOT执行Character Action、StateMachine、Timeline、Motion、GameplayEffect、WorldSolver或Presentation operation。

#### Scenario: AI决定攻击

- **WHEN** AI Program根据Perception选择Attack intent
- **THEN** 输出 MUST是带稳定RequestId、sequence与source tick的CharacterSimulationInput request
- **AND** 后续Character Program MUST自行判断准入并创建ActionInstance

#### Scenario: AI Program包含Character operation

- **WHEN** AI Semantic IR包含ActivateActionInstance或Timeline operation
- **THEN** AI Program编译 MUST失败
- **AND** Runtime MUST不跳过未知operation

#### Scenario: AI实现复制Selector运行时

- **WHEN** AI runtime提供独立于portable control Core的Selector、Running或abort evaluator
- **THEN** 构建与架构校验 MUST失败
- **AND** AI operation adapter MUST改为消费唯一control topology/runtime

### Requirement: AI Perception必须来自冻结的Committed Session观察

每个Logic Tick的Execution Backend MUST在outer transaction开始时，从上一轮已提交roster与World Body构造唯一不可变`CommittedActorObservationSnapshot`，并通过正式read port交给Local Control Input Ingress。Snapshot第一版 MUST只包含ObservationTick、locked roster identity、ActorId与最近committed Logic Body；AIPerceptionProfile MUST通过显式ActorId绑定候选并为所有AI Actor构造typed AIPerceptionFrame。所有AI Actor MUST读取同一Observation Tick。Perception MUST NOT读取Team、Faction、任意公开Gameplay fact、当前Tick部分结果、Character私有State、VisualRoot、Animator、Camera、Scene扫描、Tag、GameObject名称或ActorId前缀推断。

#### Scenario: 两个AI按不同顺序注册

- **WHEN** Session roster中两个AI Actor的注册顺序变化
- **THEN** 它们在同一Logic Tick MUST看到相同committed world版本
- **AND** 决策输入 MUST不依赖Character Evaluate顺序

#### Scenario: 目标正在视觉插值

- **WHEN** 目标VisualRoot位于两个逻辑Body sample之间
- **THEN** Perception MUST读取最近committed逻辑Body
- **AND** 表现插值 MUST不改变AI输入

#### Scenario: AI Profile配置显式目标

- **WHEN** AIPerceptionProfile为AI Controller配置候选Actor
- **THEN** Profile MUST保存稳定ActorId并从CommittedActorObservationSnapshot解析
- **AND** MUST不通过Team、Tag、名称或Scene搜索推断目标

### Requirement: AI State必须拥有独立生命周期和身份

AIIntentProgram MUST拥有独立ProgramHash、LayoutHash、OperationSetVersion、AIControllerState schema与canonical codec。Controller Blackboard、Runnable control state、稳定request sequence和必要AI memory MUST进入AI State；Tick workspace、Perception candidate缓存和output builder MUST NOT进入committed AI State。Runtime MUST NOT创建authoring Graph clone或调用通用RunnableTree解释AI。

AI Evaluate MUST从正式AI State产生candidate AI State，不得立即覆盖已提交状态。拥有AI State的Local Control Input Ingress MUST作为正式Pipeline state participant提供canonical state、hash、checkpoint与restore；只有outer state publish成功后candidate才成为正式AI State。任一Control Source、Character Evaluate、WorldSolver、Finalize或Egress失败 MUST恢复Tick前AI State与request sequence。

#### Scenario: AI节点跨Tick运行

- **WHEN** 一个AI Flow节点在多个Tick保持Running
- **THEN** control state MUST保存在AIControllerState
- **AND** CharacterSimulationState MUST不保存该AI节点状态

#### Scenario: AI运行时加载过期Program

- **WHEN** AI Program source revision与Definition不匹配
- **THEN** Control Source activation MUST失败
- **AND** MUST不回退authoring Graph解释执行

#### Scenario: AI输入已准备但WorldSolver失败

- **WHEN** AI Evaluate已经产生candidate state和Attack request但同一outer Tick的WorldSolver失败
- **THEN** Pipeline MUST恢复Tick前AI State与request sequence
- **AND** candidate input、Character结果与World结果 MUST全部不发布

### Requirement: Local Session必须在Character Evaluate前冻结全部AI输入

Standard Local Pipeline MUST只有一个Local Control Input Ingress和一个CanonicalInputBatch writer。Ingress MUST读取同一CommittedActorObservationSnapshot，按稳定ActorId准备Player、Neutral与AI Control Source输入，验证完整roster后一次冻结全部`CharacterSimulationInput`，之后才开始Character Schedule/Evaluate/ResolveBatch。公共Ingress、Source与Composer MUST不按AI具体类型、Actor名称、Tag或fallback选择Control Source。AI Evaluate失败 MUST使当前Session Tick按正式failure policy停止，MUST NOT使用Neutral、上一Tick input或空request继续。

#### Scenario: AI与玩家同一Tick输入

- **WHEN** Local Session包含玩家与任意合法AI Control Source
- **THEN** 两者当前Tick的CharacterSimulationInput MUST在任何Character Evaluate前完成冻结
- **AND** 两者Character Program MUST继续进入同一个World ResolveBatch

#### Scenario: AI Evaluate失败

- **WHEN** AI Program因无效状态或Perception合同失败
- **THEN** 当前batch MUST不发布部分Character/World结果
- **AND** diagnostics MUST报告ControllerId、ActorId、AI node与Tick

#### Scenario: 玩家与AI共享Local Ingress

- **WHEN** Local roster同时包含Player、Neutral与AI Control Source
- **THEN** 三者 MUST通过同一个Control Source roster与Local Control Input Ingress生成唯一CanonicalInputBatch
- **AND** 系统 MUST不安装AI专用Ingress或第二个input writer

### Requirement: 不支持AI的Session Composition必须明确拒绝

每种Session Source、Pipeline与Numeric Target组合 MUST显式声明是否支持AI Control Source及匹配AI Program ABI。本change只要求Local Float32组合支持。ServerAuthoritative客户端、Authority与DeterministicRollback组合在未安装正式AI capability时 MUST在Session Active前拒绝AI Actor，MUST NOT回退Neutral、在客户端偷偷运行AI或按Actor名称切换策略。

#### Scenario: 网络客户端Scene配置AI Actor

- **WHEN** 当前Network Composition未声明AI Control Source capability
- **THEN** Session preparation MUST失败并定位Actor与缺失capability
- **AND** Client MUST不把该Actor改成Neutral或Local AI
