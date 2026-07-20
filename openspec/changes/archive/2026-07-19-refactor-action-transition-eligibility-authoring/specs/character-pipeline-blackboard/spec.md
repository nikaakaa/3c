## MODIFIED Requirements

### Requirement: Decision TreeClip 必须通过声明式 Frame Blackboard 输出决策

Decision TreeClip 写入的变量 MUST来自 ExposedProperty 对应的 Pipeline Blackboard declaration，并且 MUST使用 `Frame` scope 和 `Frame` lifetime。运行时 MUST在 Frame 开始清理旧值，在当前 clip active 时重新求值并写入，在 State.OnExit 完成后的 Frame 结束统一清理。Decision TreeClip write MUST不隐式改变 declaration 的 projection 或 sync policy：Projection=None 的写入 MUST保持本地值；显式 ActionWindow projection 的写入 MUST通过唯一 projection stage 暂存 candidate，并在 EndFrame 生成正式 fact。

#### Scenario: Dodge 恢复段开放动作切换

- **WHEN** Dodge Timeline 的 `RecoveryOpen` Decision TreeClip 在当前 Tick active
- **THEN** Tree MUST写入 owner-local Bool Frame declaration
- **AND**唯一 projection stage MUST暂存匹配当前 ActionInstance 的 ActionWindow candidate
- **AND** Dodge Transition ConditionRuleGraph MUST能在同一 Tick通过 `ActionWindowActiveInfoNode` 读取该 WindowType

#### Scenario: Decision clip 不再 active

- **WHEN**新 logic frame 中 Decision TreeClip 不在 active 时间范围
- **THEN** Frame Blackboard 与 staged projection MUST不保留上一 Tick 的 true 值或 candidate
- **AND** runtime MUST NOT依赖 OnDisable 写 false 才能清理 gate

#### Scenario: Projection=None 的普通决策

- **WHEN** Decision TreeClip 写入 Projection=None 的 Bool Frame declaration
- **THEN**普通 Blackboard ValueNode MAY在同 Tick读取该值
- **AND**系统 MUST不生成 `ActionWindowFact` 或 typed WindowType 命中

#### Scenario: 声明策略冲突

- **WHEN** Timeline inline Tree 与可见上层 owner 对同一 Blackboard key 声明不同类型、scope、lifetime、authority 或 sync policy
- **THEN** validator 或 runtime MUST报告配置错误
- **AND**系统 MUST NOT选择任一声明作为 fallback

### Requirement: Pipeline Blackboard declaration 必须作为 Graph Data Catalog 的正式来源

Pipeline Blackboard authoring MUST将当前 authoring context 可见的 `BaseExposedProperty` declaration 投影到统一 `Graph Data Catalog`。每个条目 MUST保留 declaration identity、实际 owner、local/inherited 可见性、值类型、scope、lifetime、authority、sync policy、category、projection 和默认值语义。该投影 MUST NOT复制 declaration，也 MUST NOT建立 ExposedProperty 与 Pipeline Blackboard 之外的第二套变量或窗口配置。

#### Scenario: 显示当前 inline Timeline 本地 declaration

- **WHEN**作者从 Dodge state body 打开拥有 local `RecoveryOpen` declaration 的 inline Timeline
- **THEN**目录 MUST将其显示为 Timeline owner 的 local editable Blackboard 条目
- **AND**必须显示 ActionWindow projection、WindowType 与稳定 identity

#### Scenario: 显示 RootTree declaration

- **WHEN** inline state body 可见 RootTree 声明的 `RunThreshold`
- **THEN**目录 MUST将其显示为 inherited read-only 条目并标明真实 owner

#### Scenario: 同 key 不同 owner

- **WHEN**两个合法 owner 各自存在显示名相同但 identity 不同的 declaration
- **THEN**目录 MUST通过 declaration identity 和 owner 区分条目，MUST NOT按显示名合并
