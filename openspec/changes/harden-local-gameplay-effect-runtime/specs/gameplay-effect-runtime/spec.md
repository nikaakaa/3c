## MODIFIED Requirements

### Requirement: Gameplay Effect Runtime 必须通过窄端口和单 Tick ChangeSet 交换数据

`GameplayEffectRuntime` MUST 只接收不可变 runtime definition、Gameplay Effect tick context、类型化 Apply/Remove request 和 authority input，并 MUST 使用窄合同暴露能力。Runtime MUST 以 `BeginLogicTick -> mutation -> DrainChangeSet` 表达唯一单 Tick 事务；上一 Tick 未 Drain 时 MUST 拒绝下一次 Begin，Tick 外 MUST 拒绝 Apply/Remove，Drain 后 MUST 关闭当前 Tick。Runtime MUST 把 Effect、Attribute、Tag、Cue 和本地 execution failure 写入当前 Tick 唯一 `GameplayEffectChangeSet`，MUST NOT 直接写 Character frame、SyncFacts、网络对象或消费者回调。

#### Scenario: 上一 Tick 未提交

- **WHEN** 调用者在当前 Tick 尚未 DrainChangeSet 时再次 BeginLogicTick
- **THEN** Runtime MUST 明确失败
- **AND** 两个 Tick 的 ChangeSet MUST NOT 被合并或改写 Tick 身份

#### Scenario: Tick 外应用 Effect

- **WHEN** 调用者在 BeginLogicTick 之前或 DrainChangeSet 之后调用 Apply/Remove
- **THEN** Runtime MUST 明确失败
- **AND** Attribute、Tag、ActiveEffect 和 ChangeSet MUST 保持不变

### Requirement: Effect 应用和生命周期必须使用可观察的原子事务

系统 MUST 依次解析 Definition、校验 Context/参数、捕获 Tag/Attribute、执行 Application Requirement、解析 StackKey、创建或更新实例、应用 Modifier/Tag 并产生 lifecycle result。任一步失败 MUST 不留下部分 Attribute、Tag、Stack 或 Cue 修改。生命周期阶段触发 Additional Effect 失败时，系统 MUST 回滚该次生命周期事务，并 MUST 在当前 ChangeSet 产生包含 owner effect、instance、trigger、failure code 和 reason 的结构化 execution failure。

#### Scenario: Period Additional Effect 条件失败

- **WHEN** ActiveEffect 在 Period 阶段触发 Additional Effect 且后者未通过 Application Requirement
- **THEN** 本次 Period mutation MUST 原子回滚
- **AND** ChangeSet MUST 包含可诊断的 execution failure
- **AND** Scheduler MUST NOT 静默吞掉失败原因

### Requirement: Effect 必须支持来源隔离的正式叠层和溢出策略

系统 MUST 支持 Independent、AggregateBySource 和 AggregateByTarget，并显式配置 MaxStacks、Duration Keep/Refresh/Extend、Period Keep/Reset 与 Overflow Reject/ReplaceOldest/AdditionalEffects。聚合 Stack MUST 使用稳定 StackKey；AggregateBySource 的 MaxStacks MUST 只约束对应 source 的当前 Stack。ReplaceOldest MUST 替换达到上限的当前聚合 Stack，不得删除其他 source 的同 Effect 实例。

#### Scenario: 一个来源达到最大层数

- **WHEN** Source A 的 AggregateBySource Effect 达到 MaxStacks，而 Source B 也拥有同 Effect Stack
- **THEN** ReplaceOldest MUST 只撤销 Source A 的旧 Stack 并用 incoming Spec 创建 Source A 的新实例
- **AND** Source B 的实例、Modifier 和 Tag MUST 保持不变

### Requirement: Gameplay Effect 数值必须保持有限

系统 MUST 在 runtime definition build、ApplyRequest、Magnitude resolve 和 Attribute write 边界拒绝 NaN 与正负 Infinity。Initial value、constant bound、Magnitude constant/coefficient/post-add、SetByCaller、source snapshot、base mutation 和 current recompute MUST 只接受有限结果。系统 MUST NOT 把非法值静默替换为零、边界值或旧值作为 fallback。

#### Scenario: SetByCaller 传入 NaN

- **WHEN** Damage Effect 的 SetByCaller value 为 NaN
- **THEN** Spec 创建 MUST 返回明确拒绝原因
- **AND** Health 与 ChangeSet MUST 保持未应用状态

#### Scenario: Attribute 运算溢出

- **WHEN** 合法输入经过乘法聚合后产生 Infinity
- **THEN** 当前 mutation transaction MUST 失败并回滚
- **AND** Runtime MUST NOT 保存非有限 base/current value
