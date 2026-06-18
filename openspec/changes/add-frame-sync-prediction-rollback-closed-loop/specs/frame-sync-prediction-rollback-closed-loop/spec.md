## ADDED Requirements

### Requirement: 客户端 Prediction Network Buffer
系统 MUST 定义客户端 prediction network buffer，用于保存 pending outbound input、predicted input history、confirmed input history 和 resolved input stream。Buffer MUST 保存输入事实和网络确认状态，不得保存动作接受结果、角色状态快照、Fantasy Session 或 Unity runtime 对象。

#### Scenario: 本地输入进入 pending 与 predicted
- **WHEN** 客户端采集 tick N 的本地输入
- **THEN** 该输入 MUST 进入 pending outbound
- **AND** MUST 进入 predicted history
- **AND** MUST NOT 保存动作接受结果

#### Scenario: confirmed input 替换 predicted
- **GIVEN** tick N 已有 predicted input
- **WHEN** tick N 的 confirmed input 到达
- **THEN** resolved input stream MUST 使用 confirmed input
- **AND** 系统 MUST 能比较 predicted 与 confirmed 的字段差异

### Requirement: Confirmed Input Reconciliation
系统 MUST 将 confirmed input set 接入现有 rollback/replay 主线。Reconciliation MUST 通过 `ILocalRollbackSynctestSimulation` 执行 restore、advance 和 capture，并使用 scoped snapshot comparison 分类结果。

#### Scenario: confirmed input 一致不回滚
- **GIVEN** confirmed input 与 predicted input 字段一致
- **WHEN** reconciliation 检查 tick range
- **THEN** 结果 MUST 为 no correction required
- **AND** 系统 MUST NOT 执行 rollback adjust

#### Scenario: confirmed input 不一致触发回滚
- **GIVEN** tick M 的 confirmed input 与 predicted input 不同
- **WHEN** reconciliation 运行
- **THEN** first divergence tick MUST 为 M
- **AND** 系统 MUST 从 M-1 的 snapshot restore
- **AND** MUST 使用 resolved input replay 到 current tick

#### Scenario: 相同输入重放仍分叉
- **GIVEN** resolved input stream 已确定
- **WHEN** restore 后使用相同 resolved input replay 仍出现 strict mismatch
- **THEN** 结果 MUST 为 replay nondeterminism
- **AND** MUST NOT 把它归类为普通 prediction correction

### Requirement: Correction Queue
系统 MUST 将 correction 表达为排队请求，并由 simulation tick 消费。Transport callback MUST NOT 直接修改角色 Transform、snapshot、motion executor 或 Character frame runtime。

#### Scenario: Correction 入队
- **WHEN** transport 收到 correction DTO
- **THEN** 系统 MUST 将 correction request 入队
- **AND** MUST NOT 立即执行 restore/replay

#### Scenario: Simulation tick 消费 correction
- **WHEN** simulation tick 到达 correction consume phase
- **THEN** 系统 MUST 读取 correction request
- **AND** 按 restore、resolved input replay、compare 的顺序处理

### Requirement: Strict Checksum
系统 MUST 定义 strict checksum，用于检测多端 strict gameplay 是否一致。Checksum MUST 从 strict gameplay projection 生成，并排除 presentation drift、真实相机状态、Animancer runtime、Cinemachine 和 debug tooling state。

#### Scenario: 相同 strict snapshot checksum 一致
- **GIVEN** 两端 strict gameplay projection 字段一致
- **WHEN** 生成 checksum
- **THEN** checksum MUST 一致

#### Scenario: Presentation drift 不影响 checksum
- **GIVEN** 两端只有 visual animation normalized time drift
- **WHEN** 该字段被标记为 presentation drift
- **THEN** strict checksum MUST 不因该 drift 变化

#### Scenario: Checksum mismatch 保留字段诊断
- **WHEN** checksum mismatch 发生
- **THEN** 系统 MUST 能输出字段级 comparison 入口
- **AND** MUST NOT 只输出 hash 不同
