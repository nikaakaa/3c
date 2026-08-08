## MODIFIED Requirements

### Requirement: Character Authoring 必须先编译为 Numeric-Neutral Semantic IR

Compiler Frontend MUST以 CharacterPipelineDefinition 为唯一根，将全部可达 Graph、StateMachine、ConditionRuleGraph、Timeline、TreeClip、Blackboard、Action、Behavior、GameplayEffect 和 MotionCurve 编译为不可变 Gameplay Semantic IR。Frontend MUST先完成稳定 Authoring Discovery，再从唯一 discovered model 执行 Semantic Emission，并产出经过 canonical encode、decode、header 与 SemanticHash 校验的正式 artifact。IR MUST表达稳定 source identity、operation语义、控制流、状态声明、数值字面量、producer identity、Blackboard基础声明、可选InputValueId、可选Fact Projection和能力要求；IR MUST不保存 Unity object、Float32 runtime value、Fixed runtime value、Network Model、Blackboard Authority/SyncPolicy 或 mutable runtime state。Numeric Target MUST消费该 validated artifact，不得重新遍历 authoring 或直接接收 CharacterPipelineDefinition。

#### Scenario: 编译 Corin Semantic IR Artifact

- **WHEN** Compiler Frontend 读取 Corin CharacterPipelineDefinition
- **THEN** MUST只生成一份与 Numeric Target 无关且可 canonical 读取的 Semantic IR artifact
- **AND** Float32 与 FixedQ32.32 Target MUST消费该 artifact，不得重新遍历节点生成另一套业务规则
- **AND** Blackboard catalog MUST只包含正式基础字段、InputValueId和Fact Projection

#### Scenario: Frontend Discovery 失败

- **WHEN** 可达 authoring 存在重复 identity、循环引用、缺失 owner 或缺失 Emitter
- **THEN** Frontend MUST在 Semantic Emission 或 artifact publish 前失败并报告精确 source identity
- **AND** MUST不跳过无效元素、读取旧 cache 或调用 Target Compiler

#### Scenario: 检查旧网络字段

- **WHEN** Semantic IR Reader检查新 artifact 的 Blackboard catalog
- **THEN** MUST不存在`Authority`或`SyncPolicy` field
- **AND** Reader MUST不从旧 artifact补默认值或转换旧枚举

## ADDED Requirements

### Requirement: Blackboard Input Binding 与 Fact Projection 必须独立进入 SemanticHash

Semantic emission MUST分别规范编码 Blackboard Input Binding 与 Fact Projection。InputValueId变化 MUST改变SemanticHash和对应Target Program identity；ActionWindow payload变化 MUST改变SemanticHash和fact projection语义；无消费者的旧网络标签 MUST不参与任何hash。没有binding或projection时 MUST使用正式缺失形状，不得编码`None`、空identity或旧枚举占位。

#### Scenario: 只修改 ActionTarget InputValueId

- **WHEN** 作者把ActionTarget declaration绑定到另一个合法InputValueId
- **THEN** SemanticHash MUST变化
- **AND** Fact Projection catalog MUST保持不变

#### Scenario: 普通 Config declaration

- **WHEN** Blackboard declaration只有基础配置且没有binding或projection
- **THEN** Semantic IR MUST只编码实际基础配置
- **AND** MUST不生成ConfigVersion、LocalOnly或None策略常量

