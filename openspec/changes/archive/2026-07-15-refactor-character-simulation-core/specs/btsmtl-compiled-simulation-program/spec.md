# btsmtl-compiled-simulation-program Specification

## ADDED Requirements

### Requirement: Character authoring 必须按显式 Numeric Target 生成 Simulation Program

系统 MUST先以 CharacterPipelineDefinition 为唯一编译根生成 Gameplay Semantic IR，再由显式 Numeric Target 生成 CharacterSimulationProgram。每个 target artifact MUST只包含一个 NumericProfile；同一 source MAY为不同 target 生成不同 Program，但 MUST不重新实现或改变 Semantic IR operation。Runtime MUST不直接从 authoring object 创建 gameplay runtime clone。

#### Scenario: 编译 Corin

- **WHEN** 作者编译 Corin CharacterPipelineDefinition
- **THEN** Compiler MUST从该 Definition 可达的全部 authoring source 生成一份 Semantic IR 和一个 Float32 Program
- **AND** Runtime MUST不递归 clone RootTree、StateMachine 或 Timeline graph

### Requirement: Program 必须是不可变 portable 数据

CharacterSimulationProgram MUST只包含稳定 identity、SemanticHash、typed operation/data table、Character state layout、portable catalog、source map、required world capability manifest、NumericProfile、operation-set version 和 ProgramHash。Program MUST不包含 UnityEngine.Object、GameObject、AnimationClip、Animancer state、Endpoint、Transport、Network Model 或 mutable World state。

#### Scenario: 纯 CSharp 加载 Program

- **WHEN** 普通 .NET assembly 加载 Program bytes
- **THEN** MUST不需要 UnityEngine assembly 才能解析 Gameplay Program

### Requirement: Authoring type 必须通过唯一 Emitter 生成 Operation

每个可执行 Node、Module、Track 和 Clip authoring type MUST在 Compiler Frontend registry 中对应唯一 emitter。一个 Emitter MAY生成多个 Semantic IR operation 或引用共享 catalog entry，但每个 operation MUST声明 source map、state declaration、input、world request 和 output。Emitter MUST不按 Local、ServerAuthoritative、Rollback 或 Numeric Target 生成不同业务规则；Target Compiler 只负责 lowering 与 capability validation。

#### Scenario: 缺少 Emitter

- **WHEN** 可达 authoring source 包含没有 Emitter 的可执行类型
- **THEN** Program build MUST失败并报告精确 source identity
- **AND** MUST不回退到 authoring node 虚方法执行

### Requirement: Program 必须声明完整 Character State Layout

Program MUST为 Runnable、StateMachine、Timeline、Blackboard、Action、GameplayEffect、motion pending、RNG、counter 和 sequence 分配明确 Character state layout。任何影响未来 SimulationTick 的 Actor Gameplay 数据 MUST不留在 authoring object、operation 或 emitter 内。Body/world/solver state MUST由独立 WorldSimulationState layout 拥有。

#### Scenario: 检查有状态 Operation

- **WHEN** Wait、StateMachine 或 Timeline operation 影响后续 Tick
- **THEN** 其可变数据 MUST存入已声明 Character state slot
- **AND** operation object MUST保持不可变

### Requirement: Program 必须声明唯一 Numeric Target ABI

Program manifest MUST声明 NumericProfile、scalar/vector ABI、operation-set version、rounding/overflow policy 和 serialization version。所有 Gameplay constant MUST由 Target Compiler 从 Semantic IR source literal 转为该 target 格式。Program MUST不保存 float/fixed 双值，也 MUST不允许 Driver 或 Network Model 在运行时切换 target。本 change 的正式 target MUST只有 Float32；未来 Fixed target 必须生成独立 artifact。

#### Scenario: Authoring 数值无法表达

- **WHEN** GameplayEffect magnitude 或 MotionCurve key 无法由当前 Numeric Target 合法表达
- **THEN** target lowering MUST失败并报告 source identity、原值、NumericProfile 和原因

### Requirement: Program bytes 与 ProgramHash 必须稳定

相同 SemanticHash、compiler version、operation-set version、NumericProfile、required world capability 和 TickRate MUST产生相同 canonical bytes 与 ProgramHash。Traversal、operation、constant、scope 和 catalog MUST使用稳定 identity/order，MUST不依赖 Unity instance id、display name 或无序集合迭代。不同 NumericProfile MUST产生不同 ProgramHash。

#### Scenario: 重复编译未修改资产

- **WHEN** 相同 source revision 被重复编译
- **THEN** ProgramHash MUST保持不变

### Requirement: Program Artifact 必须与 Source Revision 严格对齐

Program Asset MUST记录 compiler version、operation-set version、source revision、SemanticHash、TickRate、NumericProfile、ProgramHash 和 Character LayoutHash。Host MUST在 artifact stale、Program 缺失、target ABI 不匹配或 required capability 不满足时创建失败，MUST不在运行时重新编译或使用旧 interpreter。

#### Scenario: Authoring 已修改但 Program 未重建

- **WHEN** Host 检测到 source revision 与 Program artifact 不同
- **THEN** Host MUST拒绝创建角色并报告 stale source

### Requirement: Presentation Projection 必须与 Gameplay Program 分离

Compiler MUST从同一 authoring root 生成 CharacterPresentationProjection，用于映射 Program producer identity 到 AnimationClip、Animancer、Camera 和 Cue 资源。Projection MUST不保存 Graph flow、State transition、Timeline Window、MotionCurve 或 GameplayEffect 真值。

#### Scenario: 客户端定位攻击动画

- **WHEN** Program 输出 Attack producer command
- **THEN** Presentation MUST通过 Projection 定位 Unity 动画资源
- **AND** Projection MUST不决定 Attack 状态或 Window

### Requirement: Session ProgramCatalog 必须不可变且支持每 Actor 显式绑定

系统 MUST以 `SimulationProgramCatalog` 保存一个 Session 可执行的有序 Program 集，并以 ProgramId、ProgramHash、LayoutHash、SemanticHash、NumericProfile、operation-set version 与 capability manifest 计算稳定 CatalogHash。Catalog 内全部 Program MUST使用相同 TickRate、NumericProfile 与 ABI version，并将 required world capabilities 合并为 Session requirement union。每个 Actor roster entry MUST显式绑定 Catalog 中唯一 ProgramId；SessionRuntime MUST不假定全部 Actor 使用同一角色 Program，也 MUST不在运行中热替换 Program、切换 NumericProfile 或迁移 layout。

#### Scenario: Corin 与另一角色共享 Session

- **WHEN** Session roster 的 ActorA 与 ActorB 绑定不同 ProgramId
- **THEN** SessionRuntime MUST 按各自 binding 选择 Program 执行
- **AND** 两者 MUST 仍进入同一个 WorldSolver batch

#### Scenario: Actor 引用未知 Program

- **WHEN** roster entry 的 ProgramId 不存在于启动时 Catalog
- **THEN** Session 创建 MUST 失败
- **AND** MUST 不回退默认 Corin Program 或按 Program 名称查找

#### Scenario: Catalog 混入不同 Numeric Target

- **WHEN** 两个 Program 的 TickRate、NumericProfile 或 target ABI version 不一致
- **THEN** Catalog 创建 MUST失败并报告两个 Program identity
- **AND** WorldSolver MUST不接收混合格式 batch

#### Scenario: 相同 Authoring 生成 Float 与 Fixed Program

- **WHEN** 同一 Corin Semantic IR 生成 Float32 Program 与未来 FixedQ32.32 Program
- **THEN** 两者 MUST共享 SemanticHash 与 source identity
- **AND** 两者 MUST具有不同 ProgramHash，且 Snapshot MUST不可互换

#### Scenario: Solver 不满足某个 Program

- **WHEN** Catalog capability union 包含当前 WorldSolver 未声明的能力
- **THEN** Session composition MUST失败
- **AND** MUST不只按第一个 Actor 的 Program 检查能力

#### Scenario: 运行中 authoring 重新编译

- **WHEN** 已运行 Session 对应 authoring 生成了新 ProgramHash
- **THEN** 当前 Session MUST 继续保持原 Catalog 或明确停止并重建
- **AND** MUST 不热替换 Program bytes 或迁移现有 Character state
