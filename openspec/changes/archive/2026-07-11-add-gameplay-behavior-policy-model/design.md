# Design: Gameplay Behavior Policy Model

## Goal

建立一层类似 UE Gameplay Tags + GAS/CMC 分工的作者心智：所有 gameplay 行为都有统一身份和标签，但执行与同步不大一统。Behavior 只回答“这个业务行为是谁、是什么类型、按什么网络策略解释”，不直接替代 Graph、Timeline、ActionRuntime、MotionStage 或 SyncDomain。

## Terminology

- `GameplayBehaviorProfile`：行为作者配置或等价 profile contract，提供稳定身份和策略入口。
- `BehaviorId`：稳定业务 id，例如 `Movement.Locomotion.Move`、`Attack.Light.01`、`State.Stun`、`Cue.HitSpark`。
- `BehaviorTag`：层级标签，用于分类、查询、阻塞、调试和 UI 过滤。
- `BehaviorKind`：决定运行时同步单位的枚举，不由节点路径隐式推断。
- `BehaviorNetworkPolicy`：行为级网络策略，包含 authority、prediction、replication、correction、history 和 debug summary。
- `EffectiveBehaviorPolicy`：resolver 产出的只读结果，供 adapter、Inspector preview 和 Runtime Debug 使用。

## BehaviorKind

### Transaction

表达有明确生命周期的离散动作事务，例如攻击、闪避、格挡、支援动作、交互。运行时使用 ActionInstance、Action Context、activation、lifecycle transition、window、action-scoped motion、cue 和 result。

### Stream

表达持续每 tick 运行的行为流，例如普通 locomotion、瞄准、持续移动蓄力、引导型输入移动。运行时使用 input command、MotionContribution、MotionSyncDomain、snapshot、correction 和 remote interpolation。Stream 不创建 ActionInstance。

### State

表达状态或效果实例，例如 stun、armor、invincible、downed、objective contested、cooldown、resource lock。运行时使用 StateEffectSyncDomain 和 state/effect identity。

### Event

表达一次性事件，例如 GameplayResult、cue、camera shake、hit spark、objective tick event。Event 根据 policy 落到 GameplayResultSyncDomain 或 PresentationSyncDomain。

## Authoring Model

作者只需要先回答三个问题：

1. 这个行为叫什么：`BehaviorId` 和 tags。
2. 这个行为是什么类型：`BehaviorKind`。
3. 这个行为如何参与网络：authority、prediction、replication、correction、history。

Graph、StateMachine、Timeline 和 Blackboard 只引用或产出 BehaviorId/context：

- Graph 节点可引用 Transaction behavior 来激活动作。
- Locomotion 节点可引用 Stream behavior 或由管线默认 Stream behavior 解释输入移动。
- Timeline window 继续只声明 WindowType、WindowId 和参数；它通过当前 Action Context 找到 Transaction behavior。
- State/effect 节点输出 State behavior 的 effect id。
- Cue 节点输出 Event behavior 的 cue id/type。

## Runtime Boundary

Behavior profile 不直接 tick，也不直接移动角色：

- Transaction behavior 交给 ActionRuntime 管 lifecycle。
- Stream behavior 交给 InputStage、MotionResolver、MotionStage 和 MotionSyncDomain。
- State behavior 交给 StateEffectSyncDomain。
- Event behavior 交给 GameplayResultSyncDomain 或 PresentationSyncDomain。

`CharacterPipelineOutput.SyncFacts` 仍是唯一网络事实出口。BehaviorId 可以附着在 SyncFact 上用于策略解析、debug 和稳定身份追踪，但 BehaviorId 本身不是网络包。

## Resolver

新增或改造 resolver 为 `BehaviorNetworkPolicyResolver`：

- 输入：BehaviorProfile、BehaviorKind、SyncFact kind、可选 source type/window type/cue type/result type。
- 输出：EffectiveBehaviorPolicy，包含是否发送、目标 SyncDomain、packet kind、policy id、过滤原因和 debug summary。
- Adapter 只消费 resolver 结果和 SyncFacts，不反查 Graph、Timeline 或 Blackboard。

第一阶段 resolver 可以复用现有 `ActionNetworkPolicyResolver` 的 action 解析逻辑，但对外要收敛为 behavior policy 口径。`ResolveClientCommandFrame()`、`ResolveStateEffect()` 和 correction ack 这类硬编码策略应迁移到 Stream/State behavior policy。

## Data Relationship

```text
CharacterPipelineDefinition
  -> Behavior Registry
      -> Transaction behavior profile
      -> Stream behavior profile
      -> State behavior profile
      -> Event behavior profile

Graph / Timeline / Runtime output
  -> SyncFacts + optional BehaviorId

BehaviorNetworkPolicyResolver
  -> EffectiveBehaviorPolicy

CharacterGameplaySyncAdapter
  -> GameplaySyncPacket
```

`ActionProfile` 的处理原则：

- 不恢复旧 ActionSO 或 ActionModule。
- 不让 ActionProfile 与 GameplayBehaviorProfile 形成重复身份。
- 第一阶段可以把 ActionProfile 视为 Transaction behavior profile 的实现，但 registry 必须提供统一 BehaviorId 查询和重复 id 校验。

## UI

- `CharacterPipelineDefinition` Inspector 显示统一 Behavior Registry 摘要。
- Transaction 行为显示 action lifecycle、window、motion、cue、result 的 policy preview。
- Stream 行为显示 command send、prediction、snapshot、remote presentation、correction 的 policy preview。
- State 行为显示 effect identity、authority、replication、history。
- Event 行为显示目标 SyncDomain、replication 和 local-only/confirmed 语义。
- Runtime Debug 显示本 tick 行为产出的 SyncFacts、被 resolver 发送或过滤的原因。

## Why This Matches UE Without Copying GAS

UE 使用 Gameplay Tags 提供统一标签语言，但普通移动仍由 CharacterMovementComponent 的网络移动系统处理，Ability 处理可激活 gameplay 逻辑，GameplayEffect 处理状态和属性，GameplayCue 处理表现事件。这里采用同样的分工思想：统一标记，分开 runtime。

不照搬 GAS 的原因是当前项目已有 BTSMTL Graph、Timeline、ActionRuntime、MotionStage 和 SyncDomain。完整 GAS 会和现有 graph 编排职责重叠。Behavior model 只补作者身份和策略目录，不替代现有执行链路。

## Migration Notes

第一阶段应避免资产双主线：

- 如果保留 `ActionProfile` 类名，它必须被注册进统一 behavior registry。
- 如果新增 `GameplayBehaviorProfile` asset，Transaction 行为和 ActionProfile 的关系必须一次性说清楚，不能让作者同时在两处配置同一个 `Attack.Light.01`。
- 旧 hardcoded motion/state/correction policy 应迁移到 behavior policy resolver，不保留隐藏 fallback。

## Risks

- 如果 BehaviorProfile 过度抽象，会变成空泛大一统，作者仍不知道该填哪个字段。
- 如果节点也保存完整 network policy，会重新分裂。
- 如果 Stream 行为没有明确 command/snapshot/correction 口径，普通移动会继续被硬编码在 adapter 里。

## Decision

选择“统一 Behavior identity + BehaviorKind 分发”的模型。它比单独 MotionProfile 更统一，比所有行为 ActionInstance 更干净，也和当前 SyncDomain 架构一致。
