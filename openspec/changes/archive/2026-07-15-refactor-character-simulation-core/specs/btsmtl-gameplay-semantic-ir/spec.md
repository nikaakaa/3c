# btsmtl-gameplay-semantic-ir Specification

## ADDED Requirements

### Requirement: Character Authoring 必须先编译为 Numeric-Neutral Semantic IR

Compiler Frontend MUST以 CharacterPipelineDefinition 为唯一根，将全部可达 Graph、StateMachine、ConditionRuleGraph、Timeline、TreeClip、Blackboard、Action、Behavior、GameplayEffect 和 MotionCurve 编译为不可变 Gameplay Semantic IR。IR MUST表达稳定 source identity、operation 语义、控制流、状态声明、数值字面量、producer identity 和能力要求；IR MUST不保存 Unity object、Float32 runtime value、Fixed runtime value、Network Model 或 mutable runtime state。

#### Scenario: 编译 Corin Semantic IR

- **WHEN** Compiler Frontend 读取 Corin CharacterPipelineDefinition
- **THEN** MUST只生成一份与 Numeric Target 无关的 Semantic IR
- **AND** Float32 与未来 Fixed Target MUST消费该 IR，不得重新遍历节点生成另一套业务规则

### Requirement: Semantic IR Operation 必须由唯一 Emitter 定义

每个可执行 authoring type MUST由唯一 Frontend Emitter 生成 Semantic IR operation。Emitter MUST不读取 Local、ServerAuthoritative、Rollback、WorldSolver concrete type 或 Numeric Target 来改变业务控制流。Target 不支持某个 operation 时 MUST在 lowering 阶段明确失败，MUST不跳过 operation、改写规则或调用旧 runtime。

#### Scenario: Fixed Target 不支持某个 Operation

- **WHEN** 未来 Fixed Target 无法降低 Semantic IR 中的某个 operation
- **THEN** target build MUST报告 operation source identity 与缺失 capability
- **AND** MUST不生成 Rollback 专用节点或使用 Float32 evaluator fallback

### Requirement: Semantic 数值字面量必须保持来源与精确降低边界

Frontend MUST以 canonical source literal 保存 authoring 数值及其 source identity，不得在 IR 阶段提前量化为公共定点格式。Numeric Target MUST负责将 literal 转为自己的 scalar/vector 表示，并报告非法值、超范围、舍入或不支持的精度要求。

#### Scenario: 同一 MotionCurve 降低到不同 Target

- **WHEN** 同一 Semantic IR 分别交给 Float32 Target 与未来 FixedQ32.32 Target
- **THEN** 两个 Target MUST从同一 source literal 独立生成各自 constant
- **AND** Fixed lowering 的量化误差 MUST不改变 Float32 Program constant

### Requirement: Semantic IR 不得成为第二个 Runtime Interpreter

Gameplay Semantic IR MUST只存在于编译和诊断边界。Runtime Host MUST加载已完成 target lowering 的 CharacterSimulationProgram，MUST不在运行时解释 IR、从 IR 临时生成 operation 或在 stale Program 时回退 IR 执行。

#### Scenario: Program Artifact 过期

- **WHEN** Host 发现 Program source revision 或 target manifest 与当前 Definition 不一致
- **THEN** Host MUST拒绝创建 Session
- **AND** MUST不直接解释 Semantic IR 或 authoring object

### Requirement: Semantic Identity 与 Target Artifact 必须可追溯

Semantic IR MUST产生稳定 SemanticHash。每个 target Program manifest MUST记录 SemanticHash、source revision、compiler version、operation-set version、NumericProfile 和 required world capabilities；Program source map MUST能从 target operation、constant、state slot 和 producer 追溯回同一 Semantic IR/source identity。

#### Scenario: 比较 Float 与 Fixed Artifact

- **WHEN** 两个 Program 来自相同 source revision 与 Semantic IR 但使用不同 NumericProfile
- **THEN** 两者 MUST具有相同 SemanticHash
- **AND** MUST具有不同 ProgramHash 与可能不同的 LayoutHash
