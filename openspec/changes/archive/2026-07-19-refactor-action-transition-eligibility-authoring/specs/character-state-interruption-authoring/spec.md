## MODIFIED Requirements

### Requirement: 状态退出业务必须通过纯条件读取与显式 lifecycle 节点表达

OnExit 与 Transition 条件 MUST 使用 `StateExitCauseInfoNode`、Action Context reader、Pipeline Blackboard ValueNode、`ActionWindowActiveInfoNode`、`CanActivateActionInfoNode` 和通用 Equal/And/Or/Not 等纯条件节点组合。所有 Timeline 时间门 MUST 继续由 Decision TreeClip 写入 owner-local scope variable；具有 ActionWindow projection 的 declaration MUST成为窗口时间、ActionInstance provenance、WindowId 和 Digest 的唯一来源。ConditionRuleGraph MAY按 WindowType 只读当前帧同一 projection candidate，但 MUST NOT创建专用 timeline decision cache、active-window registry、历史窗口副本或目标专用 cancel node。Action terminal lifecycle MUST由 source leaf 的显式 lifecycle 节点提交，StateMachine runtime 与 target activation MUST NOT隐式推导或自动取消 source Action。

State transition ConditionRuleGraph 的词法可见范围 MUST包含普通祖先 graph 与该 transition 的 source StateNode 直接 body graph，MUST NOT包含 target State body、兄弟 State body 或任意后代 leaf 的局部 declaration。Topology projection、Semantic IR compiler、Agent Snapshot、Inspector 与 Validator MUST使用同一规则。嵌套 StateMachine 的 leaf transition MUST读取 leaf-local Timeline window 并先退出内层状态机；外层 category transition MUST只在 `state_root_completed` 后选择目标，MUST NOT跨层重复读取 leaf window。

#### Scenario: Source State transition 读取自己的 Timeline 窗口

- **WHEN** source State body 的 inline Timeline 在当前 Tick 投影 owner-local `RecoveryEarly`
- **AND**从该 source State 离开的 ConditionRuleGraph 查询 `RecoveryEarly`
- **THEN** topology projection 与 compiler MUST把该 source body 纳入合法词法范围
- **AND** condition MUST读取当前 Tick、当前 ActionInstance 的同一 projection candidate

#### Scenario: Transition 不得读取其它 State 的局部窗口

- **WHEN** ConditionRuleGraph 引用了 target State body、兄弟 State body 或任意后代 leaf 的 local declaration
- **THEN** authoring validation 或 compilation MUST失败并定位越界 owner
- **AND** Runtime MUST NOT通过全局查找、历史值或 fallback 让该条件继续执行

#### Scenario: 嵌套 Action 通过完成结果向外路由

- **WHEN** Attack leaf 的 replacement 条件命中并退出 Attack 内层 StateMachine
- **THEN**外层 Attack transition MUST在 `state_root_completed` 后结合未消费的 request、target admission 或 Move 输入选择 Dodge 或 None
- **AND**外层 transition MUST NOT再次读取该 Attack leaf 的 Timeline window
- **AND**只有最终 target activation MUST消费 request

#### Scenario: ComboAccept 离开攻击

- **WHEN** Attack1 的 `ComboAccept` Decision TreeClip 在当前 Tick 产生匹配当前 ActionInstance 的 projection candidate
- **AND** Attack request 成立且 `CanActivateAction(Attack2)` 为 true
- **THEN** Attack1 Transition MUST通过纯条件节点离开 source State
- **AND** Attack1 OnExit MUST显式提交一次 `Cancel(RecoveryCancel)`
- **AND** target activation MUST在 source stop barrier 完成后消费 request 并创建 Attack2 ActionInstance

#### Scenario: RecoveryEarly 允许闪避替换攻击

- **WHEN** 当前攻击处于 `RecoveryEarly`
- **AND** Dodge request 成立且 `CanActivateAction(Dodge)` 为 true
- **THEN** StateMachine MUST选择指向 Dodge 的显式 replacement edge
- **AND** source OnExit MUST先关闭攻击 ActionInstance
- **AND** target activation MUST NOT隐式取消 source

#### Scenario: Dodge RecoveryOpen 离开动作

- **WHEN** Dodge Decision TreeClip 在当前 Tick 产生 `RecoveryOpen`
- **AND** Attack、Dodge 或 Move 的对应纯条件成立
- **THEN** Dodge StateMachine MUST按显式 edge priority 选择唯一 target
- **AND** 未命中的请求 MUST不被 condition query 消费
- **AND** 系统 MUST不读取 `DodgeRecoveryCancel` 或 `CanDodgeMoveCancel` 兼容 key

#### Scenario: Locomotion 状态抢占

- **WHEN** RunEnd 通过普通输入 Transition 离开
- **THEN** runtime MUST处理状态退出并发布通用 Runnable/EdgeCommit facts
- **AND** MUST NOT生成 Action Cancel、Interrupt 或 Abort
