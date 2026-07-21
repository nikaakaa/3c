## ADDED Requirements

### Requirement: 动作目标候选必须作为 portable typed input 进入 Simulation

系统 MUST将可选 `ActionTargetSnapshot` 作为 `CharacterSimulationInput.Values` 的正式 typed value kind，通过稳定 InputId 表达 TargetId、position、yaw 和有效性。Float32、Fixed、canonical codec、GameplayHash、ServerAuthoritative input command、DeterministicRollback input history 与 replay MUST保存同一业务字段和顺序。系统 MUST NOT创建 Target 专用 packet、第二 input buffer 或 Scene 对象引用。

#### Scenario: 本地玩家采样训练敌人

- **WHEN** 显式目标 provider 读取到训练敌人最近提交的逻辑 Body
- **THEN** Unity Input Adapter MUST在当前输入帧写入 typed `ActionTargetSnapshot`
- **AND** Program MUST只通过 portable CharacterSimulationInput 消费该目标

#### Scenario: 当前输入帧没有目标

- **WHEN** provider 不可用或明确返回无目标
- **THEN** 当前输入 MUST保存显式 None
- **AND** 输入层 MUST NOT延续上一帧目标或按 Scene 搜索替代目标

#### Scenario: Rollback 预测缺少精确输入

- **WHEN** DeterministicRollback 为缺失输入构造 predicted frame
- **THEN** 目标候选 MUST按正式预测规则成为 None
- **AND** MUST NOT把上一个已知目标快照跨帧延续

#### Scenario: ServerAuthoritative 传输目标候选

- **WHEN** Network Model 从 CharacterSimulationInput 构造 canonical input command
- **THEN** target payload MUST进入同一 command identity、codec 与 hash
- **AND** Network Model MUST NOT建立目标同步旁路

### Requirement: Neutral Input 必须从 Program 输入目录生成

Neutral Input Source MUST依据已验证 Program input catalog 为每个 continuous input value 生成类型正确的 neutral 值，并始终生成空 request 集合。它 MUST覆盖 Bool、Scalar、Vector2、Vector3、Yaw 与 `ActionTargetSnapshot`，MUST NOT按 Corin 输入名称硬编码，也 MUST NOT读取 Unity InputAction、Camera、Scene 或 Character 名称。

#### Scenario: 训练敌人生成一帧输入

- **WHEN** 同 Session 的训练敌人进入一个 Logic Tick
- **THEN** Neutral Input Source MUST生成完整且类型匹配的 CharacterSimulationInput
- **AND** target candidate MUST为 None，request 集合 MUST为空
