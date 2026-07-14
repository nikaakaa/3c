## ADDED Requirements

### Requirement: Decision TreeClip 必须通过声明式 Frame Blackboard 输出决策

Decision TreeClip 写入的变量 MUST 来自 ExposedProperty 对应的 Pipeline Blackboard declaration，并且 MUST 使用 `Frame` scope 和 `Frame` lifetime。运行时 MUST 在 Frame 开始清理旧值，在当前 clip active 时重新求值并写入，在 State.OnExit 完成后的 Frame 结束统一清理。Decision Blackboard 写入 MUST NOT 自动产生 SyncFact。

#### Scenario: Dodge 恢复段开放移动取消

- **WHEN** Dodge Timeline 的 Decision TreeClip 在当前 Tick active
- **THEN** Tree MUST 写入声明为 Bool 的 `CanDodgeMoveCancel=true`
- **AND** Dodge Transition ConditionRuleGraph MUST 能在同一 Tick通过纯 ValueNode 读取该值
- **AND** 该写入 MUST NOT 产生 ActionWindowSample

#### Scenario: Decision clip 不再 active

- **WHEN** 新 logic frame 中 Decision TreeClip 不在 active 时间范围
- **THEN** Frame Blackboard MUST 不保留上一 Tick的 true 值
- **AND** runtime MUST NOT 依赖 OnDisable 写 false 才能清理 gate

#### Scenario: 声明策略冲突

- **WHEN** Timeline inline Tree 与 RootTree 对同一 Blackboard key 声明不同类型、scope、lifetime、authority 或 sync policy
- **THEN** validator 或 runtime MUST 报告配置错误
- **AND** 系统 MUST NOT 选择任一声明作为 fallback

### Requirement: Decision TreeClip 必须保持纯决策边界

Decision TreeClip graph MUST 只包含允许的纯读取、值转换、条件组合和 Blackboard 写入能力。它 MUST NOT 包含跨 Tick Running、Wait、TimelineNode、Action lifecycle、Motion、Cue、Camera、GameplayResult、网络发送或场景副作用节点。

#### Scenario: Decision Tree 包含副作用节点

- **WHEN** 作者在 Decision TreeClip 下钻 Graph 中加入 Motion 或 Cue 提交节点
- **THEN** graph validator MUST 报告非法节点能力
- **AND** runtime MUST NOT 执行该 Decision Graph
