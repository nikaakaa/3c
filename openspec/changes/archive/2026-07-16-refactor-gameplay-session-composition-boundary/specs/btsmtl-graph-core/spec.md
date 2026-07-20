## MODIFIED Requirements

### Requirement: BaseGraph 承载运行上下文但不承担执行生命周期

系统 MUST允许 `BaseGraph` 保存非序列化运行上下文，包括 `User`、`DeltaTime`和类型化上下文读取能力。`BaseGraph` MUST NOT拥有 `Running`、`State`、`UpdateTree`或 `ResetTree`。通用 BTSMTL解释器 MAY从 resolved authoring graph data创建隔离运行工作副本，但正式 Character runtime MUST将同一 authoring编译为 `CharacterSimulationProgram`，并由 Session Pipeline的标准 Program Step Pass执行，不得通过 `RunnableTree`、`StateMachineGraphRuntime`或运行时 Graph clone执行角色 Gameplay。两种用途 MUST不共享或回写运行状态。

#### Scenario: Character 正式运行

- **WHEN** CharacterPipelineDefinition已生成有效 Program artifact且 Session Pipeline进入 Active
- **THEN** Program Evaluate/Finalize Pass MUST只执行 Program operation
- **AND** MUST不创建 BaseGraph运行工作副本或调用通用解释器

#### Scenario: 非角色通用 RunnableTree tick

- **WHEN** 非 Character组合显式调用 `RunnableTree.UpdateTree(deltaTime)`
- **THEN** 它 MUST将 `deltaTime`写入自己的隔离 `BaseGraph`运行上下文
- **AND** MUST不读取 CharacterSimulationState或注册 Character Session Pipeline Pass

