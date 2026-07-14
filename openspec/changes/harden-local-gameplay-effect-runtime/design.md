# Design

## Context

当前 GE 外部方向已经正确：通用 Runtime 不引用 Character，Character 通过 Adapter 和 Projector 接入，Graph 只通过端口查询或提交命令。但实现仍有六个本地缺口：

- `ReplaceOldest` 在聚合 Stack 溢出时按 EffectId 全局搜索，破坏 AggregateBySource 的来源隔离。
- Scheduler 的 transaction commit 只得到 bool，Additional Effect 失败原因被丢弃。
- ChangeSet 的 Tick 归属依赖调用者自觉 drain，没有 Runtime 状态机保证。
- float 输入和计算结果没有统一 finite 校验。
- Graph Apply 节点把 Context metadata 误装成目标路由能力。
- MotionStage 持有完整 GraphContext，违反只读最小能力边界。

项目目前没有正式命中 solver、hurtbox、目标 registry 或本地 Result router。现有 `GameplayResultEvent` 是输出事实，`IncomingGameplayResult` 只由 network receive 注入。直接增加一个场景单例找到目标 Adapter 并 Apply，会绕过现行 `GameplayResult -> target receive -> target GE` 约束，因此本变更不建立该路径。

## Goals

- 让单机 Self Effect 的应用、叠层、周期、移除和 ChangeSet 具备明确事务边界。
- 让失败可被 Character diagnostics 看见，不依赖网络。
- 让所有进入 Attribute 运算的数值保持有限。
- 让 Graph 作者不会把 Context actor 字段误认为目标路由。
- 让 ActorId 在 Character 层唯一拥有，并可被模型 binding 复用。
- 让 Motion 只能得到运动所需能力。

## Non-Goals

- 不补写完整 GAS AbilitySystemComponent。
- 不实现跨角色命中、伤害裁决或本地敌人注册表。
- 不实现网络 prediction/reconciliation。
- 不为 GE 建立专用 Cue 表现链。
- 不增加兼容旧 Graph 字段的 fallback。

## Decision 1: ReplaceOldest 只替换当前聚合 Stack

当 `AggregateBySource` 或 `AggregateByTarget` 的当前 Stack 达到 MaxStacks，`ReplaceOldest` 删除当前 `active`，随后用 incoming Spec 创建新 ActiveEffect。当前 StackKey 在容器中始终只有一个聚合实例，因此“oldest”指该聚合实例承载的旧 Stack，不再扫描同 EffectId 的其他来源。

### Tradeoff

全局限制目标身上同类 Buff 来源数量也是有效业务，但它需要独立的全局实例上限和 eviction group。复用 MaxStacks 会把“单个来源可叠几层”与“目标最多容纳几个来源”混成一个字段。本变更保留来源隔离；未来如需要全局上限，应新增独立 authoring 语义。

## Decision 2: Scheduler 返回结构化 execution failure

`GameplayEffectLifecycleScheduler.Advance` 返回本 Tick 生命周期提交结果。Additional Effect 失败时仍回滚本次 scheduler transaction，然后由 Runtime 在 transaction 外把失败写入当前 ChangeSet。Failure 至少包含 owner EffectId、owner InstanceId、触发阶段、失败 code 和 reason。

Character TraceProjector 将 failure 投影到 GameplayEffect diagnostics。Failure 不进入 GameplayEffect lifecycle fact，也不被网络模型消费。

### Tradeoff

直接抛异常最容易暴露问题，但 Application Requirement 失败可能是合法运行结果，不能把正常业务条件变成进程级异常。结构化 failure 既保留原子回滚，也能让作者看到原因；代价是 ChangeSet 增加一种只读结果。

## Decision 3: Runtime 显式管理 Tick transaction

Runtime 使用 `Idle/Open` 两态：

```text
Idle
  BeginLogicTick -> Open
Open
  Apply / Remove / scheduler / tag source mutation
  DrainChangeSet -> Idle
```

- Open 状态再次 Begin 明确失败。
- Idle 状态 Apply/Remove 明确失败。
- Drain 只能执行一次。
- Dispose 可以从任意状态清理，不要求额外 Drain。
- Reader 可以在 Idle 状态读取最后提交的 Runtime state。

### Tradeoff

允许跨 Tick 累积 ChangeSet 会让调用更宽松，但事实的 LocalLogicTick 将失真。显式状态要求 Adapter 严格完成 Commit；这正好与 CharacterPipeline 固定 Begin/Commit 顺序一致。

## Decision 4: finite 校验覆盖输入、authoring 和运算结果

统一使用 `!float.IsNaN(value) && !float.IsInfinity(value)`：

- RuntimeDefinitionBuilder 拒绝非有限初值、constant bound、Magnitude constant/coefficient/post-add。
- SpecFactory 拒绝非有限 SetByCaller 和 source snapshot。
- Magnitude 解析后的最终值必须有限。
- AttributeStore 在写入 base/current 前确认结果有限，运算溢出明确失败。

### Tradeoff

把非法值 Clamp 成零可以保持运行，但会掩盖资产或上游业务错误并产生不可追踪的数值差异。本项目选择明确失败，不提供数值 fallback。

## Decision 5: Graph 命令只有 Self 语义

Character Adapter 构造时获得当前 ActorId，并创建：

- `CharacterGameplayEffectQueryPorts`：TagReader、AttributeReader。
- `CharacterGameplayEffectCommandPorts`：只提供 `ApplySelf` 和 `RemoveSelf`。

`ApplySelf` 由 Adapter 构造 source=target=current ActorId 的 Context。Graph 节点只提供 Effect、ActionContext、Predicted 和 SetByCaller，不再保存 actor 字符串。

跨角色 Damage 继续只能通过正式 GameplayResult 路由到目标 receive 边界，不增加本地直调 Adapter 的入口。

### Tradeoff

让节点手填目标字符串看起来灵活，但没有 resolver 时只是虚假能力。Self 命令覆盖资源消耗、Cooldown、自 Buff 等当前可正确执行的业务；跨角色能力等待正式结果路由，避免提前形成错误 API。

## Decision 6: ActorId 由 CharacterPipelineHost 唯一提供

`CharacterPipelineHost` 保存非空 ActorId，传给 CharacterPipeline、GraphContext 和 GameplayEffectAdapter。`CharacterServerAuthoritativeBinding` 读取同一 Host.ActorId 作为 SubjectActorId，并删除自己的序列化字段。

### Tradeoff

继续让网络 binding 单独保存 SubjectActorId 不影响单角色演示，但本地 Context、跨角色 Result 与网络 packet 会出现三套可能不一致的字符串。身份归 Character 实例所有后，网络模型只复制它；代价是现有 Sandbox 资产需要一次性迁移字段位置。

## Decision 7: Motion 使用专用只读上下文

新增 `ICharacterMotionContext`，只包含 MotionWarp 所需的 ActionInstance 查询和 diagnostics context。`CharacterGraphContext` 实现该接口，但 `CharacterMotionStage` 和 `MotionModifierContext` 只保存接口，不再保存完整 GraphContext。

### Tradeoff

完整 GraphContext 减少构造参数，但让 Motion 可以访问输入、Blackboard、Action mutation 和 GE command。专用接口增加一个类型，却把允许依赖写成编译期边界。

## Spec Comparison

- `gameplay-effect-runtime` 已要求每 Tick 唯一 ChangeSet，但没有规定 Begin/Drain 失败语义；本变更补齐。
- `character-gameplay-effect-integration` 已要求 Motion 不获得 command、跨角色走 GameplayResult，但现有 combined Graph ports 和手填 TargetActorId 与要求矛盾；本变更修改实现并收紧 Requirement。
- `character-gameplay-effect-authoring` 已要求严格校验引用闭包，但没有覆盖 finite 数值；本变更补齐。
- `character-gameplay-pipeline-closure` 明确完整命中伤害裁决仍不在当前阶段；因此本变更不伪造 Corin Damage 结果来源。

## Risks

- ActorId 字段迁移涉及场景 YAML；必须保留原 `LocalActor` 值并删除 binding 旧字段。
- Tick 状态收紧可能暴露已有 Tick 外 mutation；必须沿 CharacterPipeline Activate/LogicTick/Deactivate 顺序检查调用点。
- Additional Effect failure 新增到 ChangeSet 后，transaction snapshot/clone/reset 必须完整包含该列表。
- Graph managed-reference 删除字段会丢弃旧序列化值；这些值从未提供真实路由，迁移不保留兼容字段。
