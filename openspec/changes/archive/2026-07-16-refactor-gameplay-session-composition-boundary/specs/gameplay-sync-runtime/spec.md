## MODIFIED Requirements

### Requirement: Common Network Session 必须只管理模型生命周期

Common SimulationSessionHost MUST只持有显式 Composition Definition、创建/锁定 Model Source preparation、compiled Pipeline identity、Actor roster与 runtime handle，并管理 Dispose。Host MUST不定义 packet、history、prediction、correction、rollback、snapshot recovery、WorldSolver algorithm、Pass业务规则或 commit policy。

#### Scenario: Common Host 创建完整 Model Session

- **WHEN** Host引用完整 Model Source、Pipeline、Program Runtime、Execution Backend与 Solver组合
- **THEN** MUST由 Source factory与 Pipeline compiler产生完整 LaunchPlan
- **AND** Common Host MUST不解释 Source message、Pass product或 ExecutionPlan内容

