## ADDED Requirements

### Requirement: Gameplay Tag 必须来自唯一正式 Catalog

系统 MUST 使用 Gameplay Tag Catalog 声明稳定 TagId、显示信息和父子层级。运行时、ActionProfile、GameplayBehaviorProfile 和 GameplayEffectDefinition MUST 只引用 Catalog 中存在的 TagId。系统 MUST NOT 使用任意字符串、显示名或未注册路径作为运行时 Tag fallback。

#### Scenario: 作者配置眩晕标签

- **WHEN** 作者配置 `State.Control.Stunned`
- **THEN** Catalog MUST 保存其稳定 TagId 和父 Tag `State.Control`
- **AND** Action、Effect 和 Graph MUST 通过同一 TagId 引用它

#### Scenario: 引用未注册标签

- **WHEN** 某个 ActionProfile 或 Effect Definition 引用 Catalog 中不存在的 TagId
- **THEN** 配置校验 MUST 失败
- **AND** Runtime MUST NOT 临时创建字符串标签继续运行

### Requirement: Gameplay Tag 查询必须支持层级与 All Any None

系统 MUST 让子 Tag 匹配自身和全部祖先 Tag，并使用显式 All、Any、None 组合表达查询。查询 MUST 使用稳定 TagId，不得通过字符串前缀、Contains 或显示名推断层级。

#### Scenario: 查询控制状态

- **WHEN** 角色拥有 `State.Control.Stunned`
- **THEN** 查询 `State.Control` MUST 成功
- **AND** 查询 None=`State.Control` MUST 失败

#### Scenario: 组合动作阻止条件

- **WHEN** ActionProfile 要求 Any=`State.Control.Stunned, State.Dead` 且 None=`State.Defense.Invulnerable`
- **THEN** Runtime MUST 按 Tag Query 的正式组合求值
- **AND** MUST NOT 把列表顺序解释为优先级或隐式 OR/AND

### Requirement: Runtime Tag 必须按来源计数

Gameplay Tag Container MUST 使用稳定 source handle 记录 Character 初始来源、ActionInstance 来源和 ActiveGameplayEffect 来源。移除来源时 MUST 只撤销该来源授予的 Tag；同一 Tag 的其他来源仍存在时 Tag MUST 保持有效。

#### Scenario: 两个 Effect 同时授予眩晕

- **WHEN** 两个不同 Active Effect 都授予 `State.Control.Stunned`
- **AND** 其中一个 Effect 被移除
- **THEN** Tag source count MUST 从二变为一
- **AND** 角色 MUST 继续拥有 `State.Control.Stunned`

#### Scenario: ActionInstance 结束

- **WHEN** ActionInstance 进入终态
- **THEN** Runtime MUST 精确移除该 ActionInstance source handle 的 Tags
- **AND** MUST NOT 移除 Character 或 Effect 来源的同名 Tag

### Requirement: Action 与 Effect 必须共用唯一 Tag 状态

系统 MUST 删除 `ActionRuntime` 私有 Tag 集合和字符串 `SetTag` 路径。ActionRuntime MAY 通过只读 Tag 查询验证 activation，并 MAY 通过 source sink 管理当前 ActionInstance Tags；它 MUST NOT 创建第二份角色 Tag 真相。

#### Scenario: Stun 阻止攻击激活

- **WHEN** Active Effect 授予 `State.Control.Stunned`
- **AND** Attack ActionProfile 的 Block Query 命中该 Tag
- **THEN** ActionRuntime MUST 拒绝 activation
- **AND** 判断 MUST 通过 `IGameplayTagReader` 来自 GameplayEffectRuntime 的统一 Tag Container

#### Scenario: 迁移旧 Action 标签

- **WHEN** ActionProfile 已迁移到正式 TagId
- **THEN** 旧 `List<string>` Tag 字段、`m_Tags` 和 `SetTag()` MUST 被删除
- **AND** 系统 MUST NOT 保留兼容读取或双写
