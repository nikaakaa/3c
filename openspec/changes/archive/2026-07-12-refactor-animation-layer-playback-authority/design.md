## Context

角色逻辑以固定 tick 推进，Timeline 动画在表现帧按 visual time 重采样。一个表现帧之前可能执行多个 catch-up logic tick，或者前一个动画交接仍在等待 target Ready 时，逻辑状态已经继续向后推进。因此下面这条逻辑历史是合法的：

```text
RunLoop#4 -> RunEnd#5 -> MovingTurn#6 -> RunEnd#7
```

每个 logic tick 的状态仲裁都只有一个结果，但表现层第一次看到这批变化时，当前画面可能仍是 `RunLoop#4`，最终 DesiredCandidate 已是 `RunEnd#7`。

当前 queue 已经保存完整顺序：

```text
AnimationLifecycleCommand
  LocalLogicTick
  LifecyclePhase
  Sequence
  HandoffIntent(None/Driver)
```

当前缺口发生在 queue 之后：Stage 将 HandoffIntent 抽成扁平列表，LayerRuntime 丢弃 None 并把所有 Driver 长期放入 PendingDrivers，再用 Previous/Desired 的任一 endpoint 命中做反查。这会让连续链首段与末段的 Driver 同时匹配同一次可见变化。

这不是两个目标动画同时胜出。DesiredCandidate 已经唯一；歧义来自“谁为这一次可见切换提供策略”没有在播放层之前提交完成。

目标结构：

```text
StateMachine / Timeline / Action
  -> ordered lifecycle commands
  -> CharacterPresentationStage
       -> Contribution Registry
       -> CharacterAnimationLayerArbitrator
            -> contribution allocation
            -> ordered handoff ledger
            -> causal-chain reduction
            -> one AnimationLayerPlan per layer
       -> CharacterAnimationLayerRuntime
            -> held/final output
            -> active blend/inertialization
       -> AnimationLayerPlaybackOutput
       -> Animancer Presenter
```

逻辑层可以发布多条事实；只有动画仲裁层完成 commit 后，播放执行器才收到每层唯一消息。

## Goals / Non-Goals

### Goals

- 每个 LayerId 每个表现帧只产生一个完整 `AnimationLayerPlan`。
- 保留跨 logic tick、跨表现帧的 transition 因果顺序。
- 同一连续因果链上的多个 Driver 归并为一次可见 handoff。
- 互不连通的同 authority 请求继续作为真实冲突报告。
- LayerRuntime 只执行已仲裁计划，不再推断状态机路径。
- 当前最终视觉输出继续只由 LayerRuntime 持有，不在仲裁器复制第二份视觉真相。
- source 逻辑在 stop barrier 内结束后，表现仍能从当前 FinalOutput 完成交接。
- diagnostics 能区分逻辑事实、仲裁 disposition、LayerPlan 和播放 lifecycle。

### Non-Goals

- 不让 StateMachine 或逻辑仲裁决定当前画面从哪一帧、哪一层开始混合。
- 不丢弃中间 lifecycle facts，也不只保留最后一条逻辑 transition。
- 不为被跳过的中间状态建立虚拟播放或补帧。
- 不新增第二套 TransitionRuntime、Graph coordinator 或 layer policy asset。
- 不修改动画资源、Timeline gameplay、Motion、Blackboard、网络或 Corin authoring。
- 不实现 command rollback、通用 event sourcing 或跨角色动画事务。

## Contracts

### Ordered Lifecycle Record

`AnimationLifecycleCommand` 继续作为唯一有序 envelope，不新增平行 wrapper。对 handoff planning 有意义的字段至少包括：

- `LocalLogicTick`；
- lifecycle phase；
- `Sequence`；
- source/target logical owner；
- source/target resolved presentation leaf；
- target empty；
- HandoffRole 与 strategy definition；
- cause；
- OwnerReady 与 owner release facts。

Stage 可以按 command kind 分发 Registry lifecycle，但交给 Arbitrator 的 transition records MUST 保留 envelope 顺序。Role=None MUST 保留为 topology edge，不能在 commit 前过滤。

### AnimationLayerPlan

Arbitrator 对每个正式 layer 输出一份 plan。Plan 至少包含：

- Layer definition；
- 完整 DesiredCandidate；
- `InitialSeed`、`Update`、`Hold`、`Handoff`、`Empty` 或 `Invalid` 指令；
- 当前 runtime visible owner 摘要；
- Hold/Invalid 原因；
- 可选 `AnimationHandoffPlan`。

`AnimationHandoffPlan` 至少包含：

- 当前可见来源 owner 集合；
- 最终目标 owner 集合；
- 被选 Driver identity 与 strategy definition；
- 因果链覆盖的首末 command order；
- 链内 record identities；
- `Selected/Coalesced/Retired/Conflict` disposition；
- 是否 supersede 当前 ActiveHandoff。

Plan 不携带 gameplay 决策，也不修改 Registry membership。

### Playback Snapshot

LayerRuntime 向 Arbitrator 暴露只读当前播放 snapshot：FinalOutput、HeldOutput、ActiveHandoff target 与状态。Arbitrator 只读取它来规划当前帧，不保存另一份可写视觉历史。

## Decisions

### 1. PresentationStage 仍是聚合根

`CharacterPresentationStage` 统一拥有并按顺序调用 queue、Registry、Arbitrator、LayerRuntime、Presenter 与 inertialization adapter。

业务取舍：聚合根统一事务顺序，但算法仍分别属于 Registry、Arbitrator、LayerRuntime 和 Adapter，不把所有逻辑堆入 Stage。

### 2. Registry 只管理 Producer 真相

Registry 继续处理 playback/contribution instance、owner membership、Active、CompletedHeld、Retired、Sample、Complete 与 Release。

Registry 不拥有 transition ledger、可见 output、混合进度、OwnerReady、HandoffPlan 或 visual retirement。

业务取舍：Registry 不因动画切换变成第二个状态机；同一 contribution lifecycle 仍可供角色 runtime 与 Preview 复用。

### 3. Arbitrator 是唯一动画 commit 仲裁层

`CharacterAnimationLayerArbitrator` 不再只是无状态 DesiredCandidate helper。它统一完成两类业务决策：

1. 从 Registry snapshot 计算每层 priority allocation、override coverage、additive contributions 与 DesiredCandidate。
2. 从有序 lifecycle records、OwnerReady/release、当前 playback snapshot 与 DesiredCandidate 生成唯一 LayerPlan。

Candidate allocation 算法可以保持纯函数，但 transition ledger 由同一个 Arbitrator 私有持有。不得再新增另一个与 Arbitrator 并列的公开 handoff 权威。

业务取舍：Arbitrator 变为有状态 commit 服务，reset 成本增加；换来的是播放层接口稳定为每层一个计划，并且所有动画业务仲裁集中在一个边界。

### 4. LayerRuntime 只拥有播放历史和执行状态

每个 LayerId 的 Runtime 状态只包含：

- FinalOutput；
- HeldOutput；
- 唯一 ActiveHandoff；
- blend elapsed 与 duration；
- inertialization session；
- playback completion、supersede 与 teardown 状态。

LayerRuntime 接口从：

```text
Resolve(candidates, raw intents, ready, releases, delta)
```

收敛为：

```text
Apply(AnimationLayerPlan, presentationDelta)
```

它不再保存 PendingDrivers、ready leaf、released owner、matching authority 或 transition topology。

业务取舍：Runtime 无法再“自己补救”不完整计划；Arbitrator 给出 Invalid 时 Runtime 只能按计划保持最后合法输出并暴露错误。这是显式失败，不是能力倒退。

### 5. 连续 transition 必须先构造成因果链

Arbitrator 的私有 ledger 使用带 activation generation 的 owner identity 建图。两个有序 record 只有在满足以下条件时才能连接：

- 前一 record 严格早于后一 record；
- 前一 target logical owner 与后一 source logical owner 精确相等，或经正式 OwnerReady leaf 解析后的 target/source presentation owner 精确相等；
- 两者属于同一方向的 source-to-target 路径。

禁止按 state display name、Graph 布局、共同祖先、clip 名称或 GUID 前缀连接。重复进入同一 State 使用不同 activation generation，因此不会形成伪循环。

Role=None record 参与连接，但不能提供 strategy。Role=Driver record 同时参与连接并可提供策略。

如果 owner lineage 无法通过正式事实连接，Arbitrator 必须把它视为独立组件或缺失链路，不得猜测。

### 6. 同一链先归并，再选择最终策略

对于从当前可见 owner 到最终 Desired owner 的唯一有向路径：

- outgoing 始终取 LayerRuntime 当前 FinalOutput；
- incoming 始终取完整 DesiredCandidate；
- 路径中最后一个 Role=Driver 的 record 提供 strategy；
- 更早 Driver 标记 Coalesced；
- None 仅保留 provenance；
- 中间 owner 不创建 AnimationLayerPlan，也不消耗 presentation 时间。

选择最后 Driver 的业务含义是：最终进入哪个业务状态，就使用最接近该最终目标的进入策略；实际混合来源仍是玩家当前看到的姿态。选择第一个 Driver 会让早已被逻辑跳过的中间状态控制最终进入手感，因此拒绝。

路径没有 Driver 时是配置错误。路径出现无法确定唯一末端的分支时也是歧义，不能按 Sequence 随便选一支。

### 7. 独立因果组件才进行 Driver 冲突仲裁

Arbitrator 先得到互不连通的因果组件，再计算每个组件对当前 layer 的 authority：

- source 参与当前 FinalOutput 时使用其 contribution priority；
- target 参与 DesiredCandidate 时使用其 contribution priority；
- 两端都参与时取较高值；
- 尚未 Ready 的 source-side 组件继续使用当前 source authority 保持 Hold。

较低 authority 组件是 underlay transition，标记 Retired。最高 authority 只有一个组件时，该组件归并为 HandoffPlan。多个互不连通组件具有相同最高 authority 时才报告真实冲突。

`Sequence` 只决定同一已连通组件内的路径顺序，绝不决定独立组件胜负。这既允许 catch-up 链归并，也保留 Parallel 配置错误诊断。

### 8. Ready、Release 与 ledger 生命周期属于 Arbitrator

`AnimationOwnerReady` 表示某 activation 获得过正式执行机会，是单调事实，不等于当前 Registry membership。

Arbitrator 规则：

- Driver 已到达但最终 target 尚未 Ready时，输出 Hold plan并保留整条待定链；
- RequireOutput 的最终 incoming 尚未形成时，输出 Hold plan，不把 underlay 或 Empty 偷换为目标；
- Ready 与 release 同批到达时，先用于当前 commit，再记录 release；
- record、ready 与 release 只有在所有 layer 都已 Selected、Coalesced、Retired 或确认不再引用时才清理；
- reset、deactivate、Preview seek、target switch 与 dispose 必须清空完整 ledger。

LayerRuntime 不再通过 PendingDriver 引用控制 ready fact 寿命。

### 9. ActiveHandoff 的重入仍由播放执行器完成

Arbitrator 发现最终 Desired 或选定策略变化时输出新的 Handoff plan，并标记是否 supersede。LayerRuntime 从当前 FinalOutput 重新 capture：

- CrossFade 从当前加权 state plans 接管；
- Inertialization 从当前最终 local pose/velocity 接管；
- 旧 ActiveHandoff 在新 capture 后 Superseded；
- 同一 layer 不建立 handoff stack。

表现 delta 只推进 ActiveHandoff，不参与逻辑 record 排序或目标选择。

### 10. OutputPolicy 由 Plan 明确执行

RequireOutput：

- 无 InitialSeed 时 Invalid；
- pending causal chain 或 incoming 未形成时 Hold；
- Invalid plan 保持最后合法输出并暴露原因；
- 不超时到 Idle、bind pose、旧 SO 或 Empty。

AllowEmpty：

- 只有正式 LayerPlan 可以把 layer 交接到 weight 0；
- raw producer 缺席不能被 Runtime/Presenter解释为空。

### 11. PresentationFrame 使用一次 commit

正式顺序：

1. `SamplePresentation` 按 visual Timeline time提交 Sample commands。
2. Stage 复制完整、已排序 command batch。
3. Registry 应用 producer lifecycle 并生成单一 snapshot。
4. Arbitrator 接收完整 ordered records、当前 playback snapshots 和 Registry snapshot。
5. Arbitrator更新 ledger、计算 DesiredCandidates并输出每层一个 LayerPlan。
6. LayerRuntime 分别 Apply 对应 plan，并用 presentation delta 推进 active handoff。
7. Runtime 生成最终 `AnimationLayerPlaybackOutput` 集合。
8. Presenter 应用一次最终 outputs。
9. diagnostics 记录 record、plan 与 playback lifecycle。
10. Stage acknowledge 已成功提交的 command batch。

Presenter 永远看不到 source release、target Ready、candidate 变化或链归并的中间状态。

### 12. StateMachine 只发布事实，不提交播放命令

Transition edge 继续内联保存 HandoffRole：

- None：逻辑 transition 事实和 topology bridge，不提供动画策略；
- Driver：逻辑 transition 事实，并提供 Immediate、ContributionCrossFade 或 Inertialization strategy。

StateMachine 在逻辑 barrier 内完成 source exit 与 target activation，不等待仲裁或播放。它不读取当前 layer output，也不决定这条 Driver 最后会 Selected、Coalesced 或 Retired。

这保留作者语义，同时避免 Graph 理解 Base layer 当时真正显示的是 Locomotion、Dodge 还是 Attack leaf。

### 13. Preview 复用同一 Plan 合同

Timeline Preview 没有 StateMachine handoff 时，Arbitrator仍输出 InitialSeed、Update 或 Empty plan。连续播放更新同一 owner；非连续 seek、target switch、stop 与 dispose 同时 reset Registry、Arbitrator ledger、LayerRuntime 和 Presenter state。

Preview 不新增专用 matcher，也不直接把 DesiredCandidate 交给 Runtime。

### 14. Debug 必须展示四个层次

Animation diagnostics 分别展示：

1. ordered lifecycle records：tick、phase、sequence、source/target、Role；
2. causal components：连接关系、Selected/Coalesced/Retired/Conflict；
3. LayerPlan：kind、Desired、selected policy、Hold/Invalid 原因；
4. playback lifecycle：FinalOutput、ActiveHandoff、elapsed、state/layer weights。

删除裸 `DriverIds` 或 `PendingDrivers` 被误读为同时播放请求的观察口径。Debug 只读正式仲裁结果，不维护第二套 ledger。

## Key Scenarios

### Rapid Locomotion Chain

```text
Ordered records:
  RunLoop#4 -> RunEnd#5      Driver A
  RunEnd#5 -> MovingTurn#6   Driver B/None
  MovingTurn#6 -> RunEnd#7   Driver C

Playback FinalOutput: RunLoop#4
DesiredCandidate:     RunEnd#7
```

三条 record 构成一个连续组件。Arbitrator选择通往最终目标的最后 Driver C，A 与中间 Driver标记 Coalesced，输出一个 `RunLoop#4 -> RunEnd#7` HandoffPlan。不存在 multiple Drivers。

### Action Exit And Locomotion Underlay

```text
Dodge -> None                 Driver, priority 100 component
ActionOverride -> MovingTurn  None/Locomotion component, priority 0
```

两个组件不连通时先按可见 authority 仲裁。Action 组件主导 Dodge 到完整 Desired Locomotion 的交接；较低 authority Locomotion 组件 Retired。Sequence 不参与胜负。

### True Parallel Conflict

两个独立 Action/Replacement 组件同时连接当前 Base 与不同目标，且都具有相同最高 authority。Arbitrator输出 Invalid plan，Runtime保持最后合法 output，并报告两个完整 component provenance。不得按最后 command 选择。

### Active Handoff Supersede

RunStart handoff 正在 CrossFade，新的连续链最终指向 MovingTurn。Arbitrator针对当前 FinalOutput生成一个新的 HandoffPlan；Runtime capture当前加权输出后 supersede旧 handoff，不回到 RunStart 原始首帧。

## Rejected Alternatives

### 在 LayerRuntime 内增加 Causal Reducer

改动最小，但 Runtime 仍同时理解 transition topology、authority、ready、blend 和 pose history。播放接口继续接收多条 raw facts，职责问题没有解决。

### 逻辑仲裁只发送最后一个 Transition

逻辑层不知道表现帧当前显示到哪个 owner，也不知道 layer priority、ActiveHandoff 与 target sample readiness。丢弃中间 facts 还会破坏 stop、release、debug 和未来 rollback/network pressure 下的完整生命周期。

### 全局 Last Driver Wins

可以隐藏当前报错，但会把真正独立的 Parallel 同 authority 请求静默吞掉。Sequence 只能排序一条已证明连通的链，不能证明业务 authority。

### 逐个重放中间状态

为 RunEnd#5、MovingTurn#6 建立虚拟表现子步会播放逻辑已经离开的状态，增加延迟并让攻击/闪避手感落后于当前事实。

### 按 State 名称或 Graph 父子关系猜链

名称不具备 runtime identity，宽泛祖先关系会把 Parallel sibling 错连。只接受 activation owner 与正式 ready/resolved facts 的精确连接。

### 修改 Corin Edge 避免快速链

会把通用 presentation commit 缺口藏进角色配置，后续攻击连段、受击、网络校正或其它角色仍会复现。

## Risks / Trade-offs

- Arbitrator 从无状态 helper 变为持有 ledger 的 commit 服务，必须覆盖 reset、deactivate、dispose 与 Preview seek，否则历史 record 会跨会话泄漏。
- 每层一个 plan 会增加中间合同，但显著降低 LayerRuntime 心智负担，并让 diagnostics 能准确指出问题发生在事实、仲裁还是播放阶段。
- “最后 Driver 提供策略”会让被跳过的早期 transition 不再控制最终进入手感；这是面向最终业务目标的选择。需要 outgoing-centric 策略时，应配置在最终可见 transition，而不是依赖 catch-up 偶然时序。
- 严格精确连接可能暴露缺失 owner lineage/ready fact；系统会 Hold/Invalid，而不是按名称或最后事件猜测。
- 多 layer record 清理必须以所有 layer disposition 为准，不能在 Base 结算后过早删除仍可能影响其它 layer 的事实。
- `add-btsmtl-compiled-runtime-debugging` 的旧 TransitionRuntime/Driver trace 需要重基线；不允许为保持旧 UI 保留并行 debug 数据源。

## Migration Plan

1. 修订 spec，把“LayerRuntime 匹配唯一 Driver”改为“Arbitrator 先归并因果链并输出唯一 LayerPlan”。
2. 定义 LayerPlan、HandoffPlan、plan kind、causal disposition 与 read-only playback snapshot 合同。
3. 让 Stage 将完整 ordered lifecycle command records 交给 Arbitrator，不再抽取裸 intent 列表。
4. 将 DesiredCandidate allocation 保留在 Arbitrator，并新增私有 ordered handoff ledger。
5. 将 OwnerReady/release retention、因果连接、chain reduction、component authority 与 plan commit 迁入 Arbitrator。
6. 将 LayerRuntime API 收敛为逐层 Apply plan，并删除 PendingDriver、endpoint matcher、ready/release map 与 authority 代码。
7. 保留 ActiveHandoff、CrossFade、Inertialization、FinalOutput 与 supersede 的播放执行语义。
8. 更新 Stage、frame snapshot、trace、Host Inspector 与 Preview 使用唯一 plan 合同。
9. 删除旧 matcher、旧 debug 字段和所有 raw Driver -> LayerRuntime 调用。
10. 更新 `openspec/project.md`，静态编译并 strict validate；不运行 Unity batchmode，不新增测试。
