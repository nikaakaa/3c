# gameplay-sync-runtime Specification

## MODIFIED Requirements

### Requirement: Common Network Session 必须只管理模型生命周期

Common SessionHost MUST只持有唯一 ModelDefinition、创建/锁定 model session、持有 model-owned Simulation Driver composition、注册 actor roster 并管理 dispose。Host MUST不定义 packet、history、prediction、correction、rollback、snapshot recovery、WorldSolver algorithm 或 commit policy。

#### Scenario: Common Host 创建完整 Model Session

- **WHEN** Host 引用 capability 完整的 ModelDefinition
- **THEN** MUST由 Definition 创建具体 session/Driver composition
- **AND** Common Host MUST不解释 Driver tick plan 内容

### Requirement: 同步 Runtime、Packet、History 和 Debug 必须声明模型归属

所有 model packet、protocol、history、policy、snapshot recovery、queue 和 diagnostics MUST使用明确 model ownership/namespace。系统 MUST不新增无模型归属的通用 correction/history/rollback 类型，也 MUST不把 SimulationWorldSnapshot 本身描述为某个模型的 history policy。

#### Scenario: 搜索 Model Runtime

- **WHEN** 实施完成后定位 packet、history、policy 和 recovery
- **THEN** 每个类型 MUST唯一归属具体 model
