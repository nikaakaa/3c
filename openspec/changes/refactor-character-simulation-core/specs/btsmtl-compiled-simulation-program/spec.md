# btsmtl-compiled-simulation-program Specification

## ADDED Requirements

### Requirement: Character authoring 必须编译为唯一 Simulation Program

系统 MUST以 CharacterPipelineDefinition 为唯一编译根，递归解析其 RootTree、inline/shared Graph、StateMachine、ConditionRuleGraph、Timeline、TreeClip、Blackboard、Action、Behavior、GameplayEffect 和 motion curve，并生成唯一 CharacterSimulationProgram。Runtime MUST不直接从 authoring object 创建 gameplay runtime clone。

#### Scenario: 编译 Corin

- **WHEN** 作者编译 Corin CharacterPipelineDefinition
- **THEN** Compiler MUST从该 Definition 可达的全部 authoring source 生成一个 Program
- **AND** Runtime MUST不再递归 clone RootTree 或 Timeline graph

### Requirement: Program 必须是不可变 portable 数据

CharacterSimulationProgram MUST只包含稳定 identity、operation/data table、state layout、portable catalog、source map、capability manifest 和 ProgramHash。Program MUST不包含 UnityEngine.Object、GameObject、AnimationClip、Animancer state、Endpoint、Transport 或 Network Model 引用。

#### Scenario: 纯 CSharp 加载 Program

- **WHEN** 普通 .NET assembly 加载 Program bytes
- **THEN** MUST不需要 UnityEngine assembly 才能解析 gameplay Program

### Requirement: Authoring type 必须通过唯一 Emitter 生成 Operation

每个可执行 Node/Module/Track/Clip authoring type MUST在 Compiler registry 中对应唯一 emitter。Emitter MUST生成 model-neutral operation 与 state slot declaration，MUST不按 Local、ServerAuthoritative 或 Rollback 生成不同业务规则。

#### Scenario: 缺少 Emitter

- **WHEN** 可达 authoring source 包含没有 emitter 的可执行类型
- **THEN** Program build MUST失败并报告精确 source identity
- **AND** MUST不回退到 authoring node 虚方法执行

### Requirement: Program 必须声明完整 State Layout

Program MUST为 Runnable、StateMachine、Timeline、Blackboard、Action、GameplayEffect、Body、RNG、counter 和 sequence 分配明确 state layout。任何影响未来 SimulationTick 的可变数据 MUST不留在 authoring object、operation 或 emitter 内。

#### Scenario: 检查有状态 Operation

- **WHEN** Wait、StateMachine 或 Timeline operation 影响后续 Tick
- **THEN** 其可变数据 MUST存入已声明 State slot

### Requirement: Program bytes 与 ProgramHash 必须稳定

相同 authoring content、compiler version 和 TickRate MUST产生相同 canonical bytes 与 ProgramHash。Traversal、operation、constant、scope 和 catalog MUST使用稳定 identity/order，MUST不依赖 Unity instance id、display name 或无序集合迭代。

#### Scenario: 重复编译未修改资产

- **WHEN** 相同 source revision 被重复编译
- **THEN** ProgramHash MUST保持不变

### Requirement: Program Artifact 必须与 Source Revision 严格对齐

Program Asset MUST记录 compiler version、source revision、TickRate 和 ProgramHash。Host MUST在 artifact stale、Program 缺失或 capability 不满足时创建失败，MUST不在运行时重新编译或使用旧 interpreter。

#### Scenario: Authoring 已修改但 Program 未重建

- **WHEN** Host 检测到 source revision 与 Program artifact 不同
- **THEN** Host MUST拒绝创建角色并报告 stale source

### Requirement: Presentation Projection 必须与 Gameplay Program 分离

Compiler MUST从同一 authoring root 生成 CharacterPresentationProjection，用于映射 Program producer identity 到 AnimationClip、Animancer、Camera 和 Cue 资源。Projection MUST不保存 Graph flow、State transition、Timeline Window、MotionCurve 或 GameplayEffect 真值。

#### Scenario: 客户端定位攻击动画

- **WHEN** Program 输出 Attack producer command
- **THEN** Presentation MUST通过 Projection 定位 Unity 动画资源
- **AND** Projection MUST不决定 Attack 状态或 Window
