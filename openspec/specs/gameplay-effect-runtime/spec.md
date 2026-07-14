# gameplay-effect-runtime Specification

## Purpose
规定独立 Gameplay Effect 通用运行时的定义、事务、生命周期、数值安全和 ChangeSet 边界，使角色等业务模块能在固定逻辑 Tick 中复用同一套 Tag、Attribute 与 Effect 规则，而不把 Character、网络模型或表现职责带入 GE。
## Requirements
### Requirement: Gameplay Effect Runtime 必须形成独立编译模块

系统 MUST 在正式 `Runtime/Gameplay` 目录使用独立 `ThirdPersonGameplay` 程序集承载通用 Behavior identity contract、Tag、Attribute、Effect 和 `GameplayEffectRuntime`。该程序集 MUST NOT 引用 Character、BTSMTL、Character SyncFacts、Networking、Presentation 或 Diagnostics；Character 与其他业务模块 MAY 单向依赖该程序集。独立程序集只建立代码依赖边界，MUST NOT 创建独立 Tick、Manager 或网络 Runtime。

#### Scenario: EffectDefinition 提供 Effect Behavior 身份

- **WHEN** `GameplayEffectDefinition` 实现 `IGameplayBehaviorProfile`
- **THEN** `IGameplayBehaviorProfile` 与 `GameplayBehaviorKind` MUST 位于通用 Gameplay Contracts
- **AND** Gameplay 程序集 MUST NOT 为引用该合同依赖 Character 命名空间或程序集

#### Scenario: Character 接入 GE

- **WHEN** CharacterPipeline 需要角色 Gameplay Effect
- **THEN** Character 接入层 MUST 单向引用 `ThirdPersonGameplay`
- **AND** Gameplay 程序集 MUST NOT 引用 CharacterPipeline、CharacterGraphContext 或 CharacterPipelineFrame

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

#### Scenario: 增加新的 Character 输出消费者

- **WHEN** Character 需要把同一 ChangeSet 投影到新的只读调试视图
- **THEN** 系统 MUST 在 Character 接入层新增或扩展 Projector
- **AND** MUST NOT 修改 GameplayEffectRuntime 以引用该消费者或发布全局事件

### Requirement: Effect 必须分离 Definition Spec 与 Active Instance

系统 MUST 使用 GameplayEffectDefinition 表达无状态作者定义，使用 GameplayEffectSpec 表达一次应用锁定的 Context、参数和快照，使用 ActiveGameplayEffect 表达目标角色上的 Duration/Infinite 运行状态。Definition 和 Component Definition MUST NOT 保存运行时层数、时间、来源或 Modifier handle。

#### Scenario: 同一定义应用到两个角色

- **WHEN** 同一个 Stun Definition 被应用到两个不同 Character
- **THEN** 系统 MUST 创建两个独立 Spec 和 Active Instance
- **AND** Definition MUST 保持只读且不共享运行状态

#### Scenario: Instant Effect

- **WHEN** Damage Definition 的 DurationPolicy 为 Instant
- **THEN** Runtime MUST 执行 Spec 的 mutation 和 lifecycle
- **AND** MUST NOT 把它加入 Active Effect Container

### Requirement: Effect Context 必须保存稳定业务来源

GameplayEffectContext MUST 保存 source/target actor identity、source ActionInstanceId、PredictionKey、GameplayResultId 和 source logic tick。Context MUST NOT 保存 GameObject、Graph runtime clone、Timeline clip、Network Model packet 或 transport object。

#### Scenario: 攻击资源消耗

- **WHEN** Attack ActionInstance 预测应用 Stamina Cost Effect
- **THEN** Context MUST 保存该 ActionInstanceId 和 PredictionKey
- **AND** 后续 Confirm/Reject MUST 能定位同一 Effect mutation

#### Scenario: 环境伤害

- **WHEN** 环境伤害应用 Damage Effect
- **THEN** Context MAY 没有 ActionInstanceId
- **AND** 必须仍能通过 source actor、GameplayResultId 和 tick 表达来源

### Requirement: Effect 时间必须由固定 Logic Tick 推进

Duration、Period、首次 Period 和 Expire MUST 使用 Gameplay Logic Tick。Authoring 秒数必须通过正式 Tick 配置转换为整数 Tick；GE MUST NOT 读取 Unity Time、启动 Coroutine、使用 WaitForSeconds 或拥有 MonoBehaviour Update。

#### Scenario: 周期中毒

- **WHEN** Poison Effect 的 StartTick=100、PeriodTick=10、EndTick=131
- **THEN** Period MUST 只在 110、120、130 执行
- **AND** EndTick 后 MUST 进入 Expired

#### Scenario: 缺失 Tick 配置

- **WHEN** Duration Effect 无法解析正式 fixed tick 配置
- **THEN** Spec 创建 MUST 失败
- **AND** Runtime MUST NOT 回退到 Unity delta time

### Requirement: Effect 必须由类型化无状态 Component 组合

GameplayEffectDefinition MUST 使用类型化 Component Definition 表达 Modifier、Granted Tag、Application/Ongoing/Removal Requirement、Execution、Additional Effect 和 Cue Binding。Component 输出 MUST 使用类型化结果，MUST NOT 使用 `params object[]`、任意 Dictionary payload 或反射调用 fallback。

#### Scenario: Stun Effect 生效

- **WHEN** Stun Effect 通过 Application Requirement
- **THEN** Granted Tag Component MUST 授予 `State.Control.Stunned`
- **AND** Duration 结束时 MUST 通过同一 ActiveEffectHandle 撤销来源

#### Scenario: Additional Effect 引用成环

- **WHEN** Effect A 在 Applied 时施加 B，Effect B 又间接施加 A
- **THEN** 配置校验 MUST 失败
- **AND** Runtime MUST NOT 用最大递归深度掩盖配置环

### Requirement: Effect 应用和生命周期必须使用可观察的原子事务

系统 MUST 依次解析 Definition、校验 Context/参数、捕获 Tag/Attribute、执行 Application Requirement、解析 StackKey、创建或更新实例、应用 Modifier/Tag 并产生 lifecycle result。任一步失败 MUST 不留下部分 Attribute、Tag、Stack 或 Cue 修改。生命周期阶段触发 Additional Effect 失败时，系统 MUST 回滚该次生命周期事务，并 MUST 在当前 ChangeSet 产生包含 owner effect、instance、trigger、requested effect、failure code 和 reason 的结构化 execution failure。

#### Scenario: 应用条件失败

- **WHEN** Target 拥有 Effect 的免疫 Tag
- **THEN** Application MUST 返回 Rejected
- **AND** Attribute、Tag、Active Container 和 Cue MUST 保持未应用状态

#### Scenario: 同 Tick 后续节点读取

- **WHEN** Graph ApplyEffectNode 成功应用自施加 Cooldown Effect
- **THEN** 同一 Logic Tick 后续节点 MUST 立即查询到 Cooldown Tag
- **AND** lifecycle fact 和 Cue MAY 在 CommitFacts 时统一提交

#### Scenario: Period Additional Effect 条件失败

- **WHEN** ActiveEffect 在 Period 阶段触发 Additional Effect 且后者未通过 Application Requirement
- **THEN** 本次 Period mutation MUST 原子回滚
- **AND** ChangeSet MUST 包含可诊断的 execution failure
- **AND** Scheduler MUST NOT 静默吞掉失败原因

### Requirement: Effect 必须支持来源隔离的正式叠层和溢出策略

系统 MUST 支持 Independent、AggregateBySource 和 AggregateByTarget，并显式配置 MaxStacks、Duration Keep/Refresh/Extend、Period Keep/Reset 与 Overflow Reject/ReplaceOldest/AdditionalEffects。聚合 Stack MUST 使用稳定 StackKey；AggregateBySource 的 MaxStacks MUST 只约束对应 source 的当前 Stack。ReplaceOldest MUST 替换达到上限的当前聚合 Stack，不得删除其他 source 的同 Effect 实例。

#### Scenario: 同来源刷新 Buff

- **WHEN** AggregateBySource Effect 从同一 SourceActor 再次应用
- **THEN** Runtime MUST 按定义增加层数并执行 Duration/Period 更新策略
- **AND** MUST NOT 创建无法查询的重复实例

#### Scenario: 达到最大层数

- **WHEN** Effect 已达到 MaxStacks
- **THEN** Runtime MUST 执行定义的 OverflowPolicy
- **AND** MUST 产生结构化 Overflow/Stack lifecycle 结果

#### Scenario: 一个来源达到最大层数

- **WHEN** Source A 的 AggregateBySource Effect 达到 MaxStacks，而 Source B 也拥有同 Effect Stack
- **THEN** ReplaceOldest MUST 只撤销 Source A 的旧 Stack并用 incoming Spec 创建 Source A 的新实例
- **AND** Source B 的实例、Modifier 和 Tag MUST 保持不变

### Requirement: Gameplay Effect 数值必须保持有限

系统 MUST 在 runtime definition build、ApplyRequest、Magnitude resolve 和 Attribute write 边界拒绝 NaN 与正负 Infinity。Initial value、constant bound、Magnitude constant/coefficient/post-add、SetByCaller、source snapshot、base mutation、authoritative base/current 和 current recompute MUST 只接受有限结果。系统 MUST NOT 把非法值静默替换为零、边界值或旧值作为 fallback。

#### Scenario: SetByCaller 传入 NaN

- **WHEN** Damage Effect 的 SetByCaller value 为 NaN
- **THEN** Spec 创建 MUST 返回明确拒绝原因
- **AND** Health 与 ChangeSet MUST 保持未应用状态

#### Scenario: Attribute 运算溢出

- **WHEN** 合法输入经过乘法聚合后产生 Infinity
- **THEN** 当前 mutation transaction MUST 失败并回滚
- **AND** Runtime MUST NOT 保存非有限 base/current value

### Requirement: Active Effect 必须支持 Inhibition 和正式移除

Ongoing Requirement 失败时 Active Effect MUST 进入 Inhibited，暂停其 Modifier、Granted Tag、Period 和 WhileActive cue，但保持实例时间推进；条件恢复时 MUST 恢复同一实例。系统 MUST 支持按 Handle、EffectId、SourceActorId 和 Effect Tag Query 移除并返回实际移除的 handles。

#### Scenario: Buff 暂时被抑制

- **WHEN** Ongoing Requirement 从通过变为失败
- **THEN** Active Effect MUST 进入 Inhibited
- **AND** 其 Modifier 与 Granted Tag MUST 被撤销但实例 MUST 保留

#### Scenario: 按 Handle 移除

- **WHEN** Graph 使用有效 EffectHandle 请求移除
- **THEN** Runtime MUST 精确移除该实例及其 Modifier/Tag source
- **AND** MUST 产生 Removed lifecycle result

### Requirement: Predicted Effect 必须使用 Effect-scoped journal 收口

Predicted Spec MUST 携带 ActionInstanceId 和 PredictionKey，并记录自己创建或修改的 Active Effect、Stack、Base mutation、Modifier、Tag 和 Cue identity。Confirm MUST 对齐 authoritative identity/revision；Reject MUST 精确撤销 journal；Correct MUST 先撤销预测修改再应用 typed authoritative facts。系统 MUST NOT 将该 journal 描述为完整世界 Rollback。

#### Scenario: 攻击被服务器拒绝

- **WHEN** 本地预测已应用 Stamina Cost 和 Cooldown
- **AND** 对应 ActionInstance 收到 Reject
- **THEN** Runtime MUST 撤销该 PredictionKey 对应的 Base mutation、Active Effect、Modifier 和 Tag
- **AND** MUST NOT 恢复其他 Action 或 Effect 的状态

#### Scenario: 预测修改后状态已被其他来源更新

- **WHEN** journal 记录的 Attribute revision 已不再由该预测修改持有
- **THEN** Runtime MUST 拒绝覆盖更新后的 confirmed state并报告 reconciliation 冲突
- **AND** MUST 等待 typed authoritative correction 收口

### Requirement: Effect 生命周期必须产生结构化结果

系统 MUST 至少产生 Applied、Confirmed、Rejected、StackChanged、Inhibited、Resumed、PeriodExecuted、Removed、Expired 和 Corrected 生命周期结果。结果 MUST 包含 EffectId、EffectInstanceId、tick、stack、revision 和来源 Context，并能投影到 SyncFacts、Cue 和 Diagnostics。无法完成的生命周期 Additional Effect MUST 另产生结构化 execution failure，MUST NOT 伪装成已成功 lifecycle fact。

#### Scenario: Duration Effect 到期

- **WHEN** Active Effect 到达 EndTick
- **THEN** Runtime MUST 产生 Expired 结果
- **AND** Attribute Modifier、Granted Tag、WhileActive cue 和 Active Container 必须在同一事务中收口
