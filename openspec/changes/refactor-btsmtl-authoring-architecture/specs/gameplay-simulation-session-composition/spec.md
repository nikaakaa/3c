## MODIFIED Requirements

### Requirement: Program Runtime 与 Execution Backend 必须是独立选择维度

Program Runtime Definition MUST只拥有 NumericProfile、Target ABI、Program/State/Kernel/Snapshot codec与 Target services；Execution Backend Definition MUST只拥有 Pipeline descriptor编译、Pass runtime、working transaction与 outer runtime handle创建。Target-specific Composer MUST强类型校验二者兼容。同一 Program Runtime MAY与多个兼容 Backend组合，但 Common Host MUST不做 Float/Fixed转换、反射调用或 runtime backend switch。

#### Scenario: 同一 Float32 Runtime 选择不同 Backend

- **WHEN** 后续安装另一个明确支持 Float32 Program ABI的 Execution Backend
- **THEN** Composition MAY显式选择该 Backend与合法 Pipeline
- **AND** MUST不修改 CharacterPipelineHost或把 Backend类型写进 Program
#### Scenario: 装配代码控制与技能目录

- **WHEN** 角色ProgramRuntime包含控制binding和多个SkillProgram
- **THEN** 同一选定Backend MUST执行完整组合
- **AND** MUST不为C#控制增加另一套Composer或隐藏Backend


### Requirement: Session Composition 必须锁定完整身份与真实 capability

Active descriptor MUST记录 SessionId、source clock、TickRate、Program Runtime/NumericProfile/Target ABI、ProgramCatalogHash、roster、BackendId/semantic version、PipelineId/Revision/Hash、SourceId、Solver identity/version/capabilities、Snapshot codec、Committer与可选 Model/Endpoint identity。Composer MUST在首 Tick前校验所有 identity与 Program/Pass capability union；显示名、Inspector状态或 capability位 MUST不能代替实际对象和 factory校验。

#### Scenario: Pipeline 要求 Solver 未支持能力

- **WHEN** Program与 Pass capability union包含当前 Solver未声明的能力
- **THEN** Composer MUST拒绝创建 runtime handle
- **AND** Host MUST不改用另一个 Solver、删除 Pass或忽略 capability
#### Scenario: 角色规则版本改变

- **WHEN** 控制模块语义／状态schema或技能依赖身份变化
- **THEN** Composition MUST同时锁定新完整ProgramCatalog与代码实现
- **AND** 缺失模块或混版 MUST拒绝Active
