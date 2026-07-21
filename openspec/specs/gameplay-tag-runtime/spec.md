# gameplay-tag-runtime Specification

## Purpose
定义 Gameplay Tag 的稳定身份、Tag Container、requirement query、Gameplay Effect 授予与移除以及 Action operation 只读查询边界。
## Requirements
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

Tag Container MUST 以稳定 source handle 记录 Character、ActionInstance 和 Active Effect 来源。ActionInstance 成功时 MUST 以 `action:<ActionInstanceId>` 授予 profile tags；Complete、Cancel、Interrupt、Abort 或 teardown 时 MUST 精确撤销。移除一个来源 MUST NOT 撤销其它来源的同名 Tag。Float32 与 Fixed MUST 使用相同 source identity。

#### Scenario: 多来源同名 Tag

- **WHEN** 两个 Effect 都授予 Stunned 且移除一个
- **THEN** Tag source count MUST 从二变一
- **AND** Stunned MUST 保持有效

#### Scenario: ActionInstance 激活与结束

- **WHEN** Dodge ActionInstance 激活后结束
- **THEN**唯一 Container MUST 先加入再移除对应 `action:<ActionInstanceId>` source
- **AND** MUST NOT 移除 Character 或 Effect 来源

### Requirement: Action 与 Effect 必须共用唯一 Tag 状态

系统 MUST NOT 存在 Action 私有持久 Tag 集合或字符串 `SetTag`。Action admission、BTSMTL Tag query 与 Gameplay Effect requirement MUST 读取 CharacterSimulationState 的唯一 Tag Container。Active source cancel query MAY 读取 ActionProfile 不可变 tag 定义，但 MUST NOT 保存第二份角色状态。

#### Scenario: Stun 阻止攻击

- **WHEN** Effect source 授予 Stunned 且 Attack Block Query 命中
- **THEN** preview 与 activation MUST 都拒绝
- **AND** 判断 MUST 读取唯一 Tag Container

#### Scenario: BTSMTL 查询 Action Tag

- **WHEN** active Action source 授予 Attack
- **THEN** `HasGameplayTagNode(Attack)` MUST 为 true
- **AND** admission MUST NOT 合并私有 owned-tag 副本

### Requirement: Equipment Tag必须按Slot revision拥有稳定source

Equipment commit授予的Tag MUST使用由ActorId、SlotId与EquipmentRevision构成的稳定source identity，并由唯一Gameplay Tag aggregate保存。替换或卸下 MUST只撤销该source；Character State copy、codec与restore MUST重建相同source。系统 MUST不把装备Tag作为无来源bool或写入第二Blackboard。

#### Scenario: 更换MainWeapon

- **WHEN** MainWeapon从revision 4 Sawblade切换到revision 5 Gun
- **THEN** runtime MUST撤销revision 4 source全部Tag并安装revision 5 source Tag
- **AND** 其它Slot和GE source MUST保持

#### Scenario: Character State恢复到旧revision

- **WHEN** Character State codec恢复到MainWeapon revision 4
- **THEN** Tag aggregate MUST恢复revision 4 equipment source
- **AND** Action Required Query MUST得到相同结果

### Requirement: Equipment Action准入必须复用通用Tag Query

Gameplay Tag query evaluator MUST同时服务Action Required、Block、Cancel与Graph Condition查询，并从同一当前transaction view读取Equipment source。Equipment runtime MUST不增加武器专用Tag evaluator、字符串前缀判断或按FeatureId隐式授予未声明Tag。

#### Scenario: Required与Block同时存在

- **WHEN** Attack Required Query命中Sawblade而Block Query命中Stunned
- **THEN** 通用Action admission MUST拒绝该动作
- **AND** Equipment Route选择 MUST不覆盖Block结果

