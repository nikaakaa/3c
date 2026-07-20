## MODIFIED Requirements

### Requirement: Runtime Tag 必须按来源计数

Gameplay Tag Container MUST 使用稳定 source handle 记录 Character 初始来源、ActionInstance 来源和 ActiveGameplayEffect 来源。ActionInstance 创建成功时 MUST以 `action:<ActionInstanceId>` source 授予其 ActionProfile tags；ActionInstance 进入 Complete、Cancel、Interrupt、Abort 或 teardown 时 MUST精确撤销该 source。移除任一来源时 MUST只撤销该来源授予的 Tag；同一 Tag 的其他来源仍存在时 Tag MUST保持有效。Float32 与 Fixed MUST使用相同 source identity 与 lifecycle 语义。

#### Scenario: 两个 Effect 同时授予眩晕

- **WHEN** 两个不同 Active Effect 都授予 `State.Control.Stunned`
- **AND** 其中一个 Effect 被移除
- **THEN** Tag source count MUST 从二变为一
- **AND** 角色 MUST 继续拥有 `State.Control.Stunned`

#### Scenario: ActionInstance 激活与结束

- **WHEN** Dodge ActionInstance 成功激活并授予 `Dodge`
- **THEN**唯一 Gameplay Tag Container MUST出现 `action:<ActionInstanceId>` 来源
- **AND** BTSMTL 与 Action admission MUST在同一 Tick 读取到该 Tag
- **WHEN**该 ActionInstance 随后 Complete、Cancel、Interrupt、Abort 或 teardown
- **THEN** Runtime MUST精确移除该 ActionInstance source
- **AND** MUST NOT移除 Character 或 Effect 来源的同名 Tag

### Requirement: Action 与 Effect 必须共用唯一 Tag 状态

系统 MUST不存在 Action operation 私有持久 Tag 集合和字符串 `SetTag` 路径。Action operation MUST通过 Program 的只读 Tag query 验证 activation，并通过稳定 ActionInstance source 管理当前 ActionProfile Tags。Target block query、Graph `HasGameplayTagNode`/`MatchGameplayTagQueryNode` 与 Gameplay Effect requirement MUST读取 `CharacterSimulationState` 中由 `SimulationGameplayEffectState` 维护的唯一 Tag 状态。Active source cancel query MAY读取 active ActionProfile 的不可变 tag 定义来描述来源动作类别，但 MUST不把它保存为第二份角色 Tag 状态。

#### Scenario: Stun 阻止攻击激活

- **WHEN** Active Effect 授予 `State.Control.Stunned`
- **AND** Attack ActionProfile 的 Block Query 命中该 Tag
- **THEN** `ActivateActionInstance` operation MUST 拒绝 activation
- **AND**判断 MUST读取唯一 Gameplay Effect Tag Container

#### Scenario: BTSMTL 查询 active Action Tag

- **WHEN** active ActionInstance source 已授予 `Attack`
- **THEN** `HasGameplayTagNode(Attack)` MUST返回 true
- **AND** Action admission MUST不额外把 active Action tags 合并进私有 owned-tag 集合
