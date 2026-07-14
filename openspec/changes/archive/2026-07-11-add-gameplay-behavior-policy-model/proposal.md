# Proposal: 建立 Gameplay Behavior 统一标记和网络策略模型

## Why

当前角色管线已经把动作事务、连续运动、状态效果、玩法结果和表现事件分到不同 SyncDomain：

- `ActionProfile` 集中配置攻击、闪避、格挡等动作事务的网络策略。
- `MotionSyncDomain` 处理普通 locomotion、motion intent、snapshot 和 correction。
- `Pipeline Blackboard` 已经统一图变量，但明确不能成为黑板 key/value 网络同步通道。
- `CharacterGameplaySyncAdapter` 只消费 `SyncFacts` 和 effective policy，不直接读取 Graph 或 Timeline。

新的问题是：作者希望“所有行为都可以被标记”，不管是离散行为还是连续行为。如果继续只靠 `ActionProfile`，普通 locomotion 会被迫伪装成 ActionInstance；如果给 locomotion、state、cue 分别做一套 profile，作者又会面对多个分裂的行为目录。需要补一个统一的 Gameplay Behavior 作者语义：所有 gameplay 行为都有稳定 identity、tag 和 policy 入口，但运行时仍按行为类型落到不同 SyncDomain。

## What Changes

- 新增 `Gameplay Behavior` authoring model，统一表达 `BehaviorId`、`BehaviorKind`、tags、debug category 和网络策略摘要。
- 定义 `BehaviorKind` 到 runtime/sync 语义的映射：
  - `Transaction`：离散动作事务，落到 `ActionInstance` 和 `ActionSyncDomain`。
  - `Stream`：连续行为流，落到输入 command、`MotionSyncDomain`、snapshot 和 correction，不创建 ActionInstance。
  - `State`：状态或效果实例，落到 `StateEffectSyncDomain`。
  - `Event`：一次性玩法或表现事件，落到 `GameplayResultSyncDomain` 或 `PresentationSyncDomain`。
- 规定 `ActionProfile` 是 Transaction 行为的专门实现或等价 profile，不能和 Gameplay Behavior 形成两套身份表。
- 规定未来普通 locomotion、瞄准、蓄力移动等连续行为要通过 `Stream` 行为策略解析，而不是写死在 `ActionNetworkPolicyResolver.ResolveClientCommandFrame()`。
- 引入统一 `BehaviorNetworkPolicyResolver` 口径：resolver 根据 BehaviorProfile、BehaviorKind 和 SyncFact 类型返回只读 effective policy；adapter 只消费 resolver 结果和 SyncFacts。
- 扩展 authoring/debug 心智：作者在一个行为目录里查看行为 id、kind、tags、预计 SyncFacts、预计 SyncDomain packet 和被策略过滤原因。

## Out of Scope

- 不实现完整服务端权威裁决、Fantasy peer 或真实 transport。
- 不把 Graph、SubTree、Timeline clip 或 Blackboard variable 变成网络同步单位。
- 不把所有行为都塞进 `ActionInstance`。
- 不恢复旧 ActionModule、ActionSO、locomotion 特化 SO/config 或 BBB 状态类主线。
- 不新增通用 blackboard key/value 网络包。

## Impact

- `CharacterPipelineDefinition` 需要暴露一个统一 behavior registry，而不是让作者分别猜 Action、Motion、State、Cue 的身份入口。
- `ActionProfile` 需要与统一 behavior identity 对齐；它保留动作事务细节，但不再是“唯一能被标记的行为”。
- `CharacterGameplaySyncAdapter` 当前硬编码的 client command、state effect、correction ack policy 需要迁移到 behavior policy resolver。
- Graph/Timeline 节点继续只产出 typed output、BehaviorId 或 runtime context，不分散保存完整网络策略。
- Runtime Debug 需要能按 BehaviorId/BehaviorKind 展示事实如何进入 SyncDomain。

## Tradeoff

### 方案 A：保持 ActionProfile、MotionProfile、StateProfile、CueProfile 各自独立

业务取舍：实现短期最直观，但作者会面对多个行为目录。普通 locomotion、攻击、防守、支援、objective、表现 cue 的网络策略会分散在不同面板，难以回答“这个行为到底怎么同步”。这会重新制造分裂语义。

### 方案 B：把所有行为都做成 ActionInstance

业务取舍：统一得最表面，但业务语义错误。普通移动、瞄准、目标点占领和表现 cue 没有自然的 activation/confirm/reject/end 生命周期。把它们塞进 ActionInstance 会污染动作事务层，也违反当前 SyncDomain spec 中“普通 locomotion 不进入 ActionSyncDomain”的要求。

### 方案 C：建立 Gameplay Behavior 统一标记层，按 BehaviorKind 分发 runtime/sync 语义

业务取舍：作者有统一行为身份和策略入口，同时 runtime 仍保持 Motion、Action、GameplayResult、StateEffect、Presentation 的边界。复杂度比方案 A 高，但不会把所有行为伪装成动作，也不会让网络层读黑板或图结构。这是本 proposal 选择的方向。

## Existing Spec Alignment

- 与 `character-network-sync-domain-contract` 一致：SyncDomain 仍是 runtime/pipeline contract，Graph 路径、SubTree membership 和 Timeline 结构不是同步单位。
- 与 `character-action-instance-runtime` 一致：ActionInstance 只表达离散动作事务，不表达普通 locomotion。
- 与 `character-action-network-policy-authoring` 一致：动作事务的详细策略仍集中在 ActionProfile 或等价 Transaction behavior profile。
- 与 `character-pipeline-blackboard` 方向一致：Blackboard variable 不默认网络同步，只能通过正式 SyncFacts 边界产生网络可见事实。

## Gaps to Clarify During Implementation

- `ActionProfile` 是直接继承/实现 Gameplay Behavior profile contract，还是迁移为 `GameplayBehaviorProfile(kind = Transaction)` 的专门数据分区。
- `Event` 行为是否第一阶段同时覆盖 GameplayResult 和 Presentation Cue，还是拆成 `ResultEvent` 和 `PresentationEvent` 两个更窄 kind。
- 行为 registry 在 `CharacterPipelineDefinition` Inspector 中是单独列表，还是由 ActionProfiles 和非 Transaction profiles 组合成一个只读统一视图。
