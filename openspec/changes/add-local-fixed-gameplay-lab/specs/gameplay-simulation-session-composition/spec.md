## ADDED Requirements

### Requirement: Unity Fixed Composition必须由模型无关程序集拥有

Unity Fixed Program Runtime、Execution Backend、Actor registration合同、output aggregate、diagnostics aggregate与target-specific Composer MUST位于不引用DeterministicRollback或其它Gameplay Network Model程序集的model-neutral Unity Fixed程序集。DeterministicRollback与Local Fixed adapter MUST单向引用该程序集，并通过各自Prepared Source和Runtime Launcher进入同一个Fixed Composer。Fixed Composer MUST不按Source具体类型构造Rollback state、history、Endpoint或network diagnostics。

#### Scenario: Local Fixed组合

- **WHEN**Composition显式选择Fixed Program Runtime、Fixed Backend、Standard Fixed Local Pipeline、Local Fixed Source与Deterministic KCC
- **THEN**同一个SimulationSessionHost MUST创建Fixed runtime handle
- **AND**Fixed Composer MUST不要求Rollback Prepared Source或Rollback actor registration

#### Scenario: Rollback组合

- **WHEN**Composition显式选择相同Fixed Program Runtime、Fixed Backend、Rollback Pipeline、Rollback Source与同一Deterministic KCC
- **THEN**Rollback Runtime Launcher MUST把模型专属ports传给同一个Fixed Composer
- **AND**MUST不复制ProgramCatalog、Pipeline compile、KCC创建或output aggregate构造

### Requirement: Local Fixed Composition必须使用完整五项显式选择

Local Fixed MUST通过现有`SimulationSessionCompositionDefinition`显式引用Fixed Program Runtime、Fixed Execution Backend、Standard Fixed Local Pipeline、Local Fixed Session Source与Deterministic KCC WorldSolver。Host MUST不通过Gameplay Lab Variant名称、Actor类型、Network Model配置或Fallback推断任一组成部分。Active后 MUST不切换Numeric Target、Source、Pipeline或Solver。

#### Scenario: Local Fixed缺少Source

- **WHEN**Composition包含Fixed Program、Backend、Pipeline与KCC但缺少Local Fixed Source
- **THEN**preparation MUST在创建runtime前失败
- **AND**MUST不改用Rollback Source或Float Local Source
