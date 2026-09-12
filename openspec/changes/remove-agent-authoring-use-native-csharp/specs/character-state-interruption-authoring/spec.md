## MODIFIED Requirements

### Requirement: 状态退出业务必须通过纯条件读取与显式 lifecycle 节点表达

Transition MUST 用 Action Context、Blackboard ValueNode、`ActionWindowActiveInfoNode`、`CanActivateActionInfoNode` 与通用逻辑节点组合。Timeline 时间门 MUST 只由 Decision TreeClip 写 owner-local declaration；ActionWindow projection MUST 是 WindowType、ActionInstance、WindowId 和 Digest 的唯一来源。条件只读当前帧 candidate，MUST NOT 建 cache、registry、历史副本或目标专用节点。source leaf MUST 显式提交 terminal；StateMachine 与 target activation MUST NOT 自动取消 source。

条件可见范围 MUST 只包含祖先 graph、所在 StateMachine 和 source StateNode 直接 body，不包含 target、兄弟 state 或后代 leaf。Compiler、C#作者API、Inspector、Validator 与 runtime MUST 同规则。内层 leaf 读本地 window；外层 category 只在 `state_root_completed` 后选目标，不得再读 leaf window。

#### Scenario: Source transition 读取本地窗口

- **WHEN** source Timeline 投影 `RecoveryEarly`
- **THEN** source Transition MUST 读取当前 ActionInstance 的同一 candidate
- **AND** 其它 state 引用该 local declaration MUST 失败

#### Scenario: Action replacement

- **WHEN** `ComboAccept` 或 `RecoveryEarly` 与 request、target admission 成立
- **THEN** source MUST 显式 `Cancel(RecoveryCancel)` 后离开
- **AND** target MUST 在 stop barrier 后消费 request，MUST NOT 自动取消 source

#### Scenario: Dodge RecoveryOpen

- **WHEN** `RecoveryOpen` 与 Attack、Dodge 或 Move 条件成立
- **THEN** StateMachine MUST 按 edge priority 选择唯一 target
- **AND** MUST NOT 读取旧 cancel key
