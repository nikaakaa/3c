## ADDED Requirements

### Requirement: Completed Step必须发布唯一Canonical State Candidate

Float32与Fixed Pipeline working state MUST直接持有当前immutable `SimulationWorldStateSet`的canonical引用。每个completed simulation step MUST在Actor result与World result确定后只构造一个next candidate；BeginSimulationStep MUST发布当前引用，ApplyCompletedStep MUST替换为该candidate引用，后续step MUST直接消费它，最终StateStore publish MUST接收同一实例。Pipeline MUST不通过`ToStateSet`、重复Actor roster排序、`FreezeActors`或等价包装重建同一状态。Snapshot与StateHash仍只在execution plan明确要求时创建独立持久数据。

#### Scenario: Local单Step完成

- **WHEN** Local Pipeline完成一个SimulationStep
- **THEN** CompleteStep MUST构造一个next state candidate
- **AND** working apply与StateStore publish MUST复用该candidate实例

#### Scenario: 一个Outer Transaction包含多个Step

- **WHEN** Prediction、Rollback replay或其它合法schedule在同一outer transaction产生多个step
- **THEN** 每个step MUST只构造自己的一个candidate
- **AND** 下一step MUST直接以前一candidate为输入，不得重新freeze相同Actor roster

#### Scenario: Restore应用后失败

- **WHEN** restore candidate已准备但后续participant validation失败
- **THEN** working state MUST原子恢复outer transaction开始前的canonical引用
- **AND** MUST不留下Character与World来自不同candidate的混合状态
