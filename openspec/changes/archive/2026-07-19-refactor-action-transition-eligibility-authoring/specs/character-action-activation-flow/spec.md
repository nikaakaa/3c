## ADDED Requirements

### Requirement: 动作准入查询与激活提交必须共享唯一规则

系统 MUST提供 numeric-neutral Action admission evaluator，并让纯条件 `CanActivateAction` operation 与 `ActivateActionInstance` operation 复用同一 catalog/profile、统一 Gameplay Effect state、active ActionInstance、target block query 和 target cancel query 语义。Float32 与 Fixed Target MUST只提供窄状态读取端口，不得各自复制业务判断。纯查询 MUST不消费 request、不创建 ActionInstance、不提交 lifecycle，也 MUST不保留 reservation 或跨 Tick 缓存。

ActionInstance 创建成功时，ActionProfile granted tags MUST以稳定 `action:<ActionInstanceId>` source 写入唯一 Gameplay Effect Tag Container；ActionInstance 进入 Complete、Cancel、Interrupt、Abort 或 teardown 时 MUST精确撤销该 source。Target block query、BTSMTL Gameplay Tag query 与 Gameplay Effect requirement MUST读取该唯一 Container，不得由 Action runtime 保存第二份角色 owned-tag 状态。

#### Scenario: Transition 预览目标动作准入

- **WHEN** ConditionRuleGraph 查询目标 Dodge ActionProfile
- **AND** 当前 active Attack tags 满足 Dodge cancel query 且没有 block 条件
- **THEN** `CanActivateAction` MUST返回 true
- **AND** 当前 ActionInstance、Gameplay Effect state 与 input request MUST保持不变

#### Scenario: 准入规则拒绝目标动作

- **WHEN** target ActionProfile 被当前 Gameplay Effect tags 阻止或其 cancel query 不匹配 active source
- **THEN**纯查询与最终 activation MUST返回同一稳定 reject reason
- **AND** StateMachine MUST不通过 fallback profile、字符串 tag 副本或 target-specific exception 接受动作

#### Scenario: Numeric Target 对等

- **WHEN** Float32 与 Fixed Program 对相同 Semantic IR、Action catalog、Gameplay Effect state 和 active ActionInstance 执行准入
- **THEN**两个 Target MUST得到相同 allowed/rejected 业务结果与原因
- **AND**数值表示差异 MUST不改变 tag/query 关系

#### Scenario: Action granted tags 对 BTSMTL 可见

- **WHEN** Dodge ActionInstance 成功激活且 Dodge ActionProfile granted `Dodge`
- **THEN**同一 Tick 后续 BTSMTL `HasGameplayTag(Dodge)` MUST读取到 true
- **AND** Dodge 进入任一 terminal lifecycle 后该 ActionInstance source MUST被撤销

### Requirement: Target activation 不得隐式结束 Source Action

`ActivateActionInstance` MUST只在当前没有 active source Action 时创建 target ActionInstance。StateMachine replacement MUST先通过通用 source stop barrier 运行 source OnExit，由显式 lifecycle operation 提交唯一 Complete、Cancel、Interrupt 或 Abort，再启动 target。`ActivateActionInstance` MUST NOT自动 Cancel active source、吞掉重复 terminal 或替 Graph 猜测退出原因。

#### Scenario: Recovery cancel 后启动闪避

- **WHEN** Attack→Dodge replacement edge 被提交
- **THEN** Attack source OnExit MUST先提交 `Cancel(RecoveryCancel)` 并关闭旧 Action Context
- **AND** Dodge target activation MUST随后创建独立 ActionInstance
- **AND** lifecycle 中 MUST只有一条旧 Attack terminal transition

#### Scenario: Source 尚未关闭

- **WHEN** `ActivateActionInstance` 提交时仍存在 active source Action
- **THEN** operation MUST以 `SourceActionStillActive` 或等价 typed reason 明确拒绝
- **AND** MUST不自动取消 source、覆盖 Action Context 或进入兼容路径
