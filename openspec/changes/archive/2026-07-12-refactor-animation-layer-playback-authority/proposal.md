# Change: 将动画播放权威收敛到角色管线

## Why

此前实现已经删除按 StateMachine animation domain 分裂的播放路径，并把同一 `LayerId` 的最终视觉历史收敛到角色管线。但运行时继续暴露出更深一层的职责缺口：动画候选已经完成仲裁，动画交接却仍由 `CharacterAnimationLayerRuntime` 从一组扁平 Driver 中反向猜测。

实际故障链路为：

```text
RunLoop#4 -> RunEnd#5 -> MovingTurn#6 -> RunEnd#7
```

这些状态切换在逻辑时间上依次成立，并不存在两个同时激活的非法目标。`CharacterAnimationLifecycleCommandQueue` 也保存了 `LocalLogicTick`、phase 与 `Sequence`。问题发生在表现提交边界：`CharacterPresentationStage` 把有序 command 拆成无顺序 `AnimationHandoffIntent` 列表，Role=None 的结构事实又在 LayerRuntime 前被丢弃。LayerRuntime 最后只能用 `PreviousOutput` 和 `DesiredCandidate` 的首尾 owner 反查 Driver，于是把同一条连续因果链上的前段 Driver 与末段 Driver 误判成两个独立竞争者。

因此当前管线只完成了“多个 contribution 选出一个 DesiredCandidate”，没有完成“多个有序逻辑事实提交为一个可执行动画交接计划”。错误表现在动画层，但根因是逻辑生命周期事实与动画播放之间缺少正式 commit 仲裁边界。

本 change 在归档前重新打开：不通过 Corin 配置、全局 last-wins、Driver ID 特判或 `Resolve` 分支隐藏问题，而是把提交、仲裁与播放职责彻底分开。

## What Changes

- 保持 `CharacterPresentationStage` 为角色动画表现聚合根，但将内部正式链路收敛为：

  `ordered lifecycle commands -> Registry snapshot -> CharacterAnimationLayerArbitrator -> one AnimationLayerPlan per layer -> CharacterAnimationLayerRuntime -> AnimationLayerPlaybackOutput -> Presenter`

- `CharacterAnimationLifecycleCommandQueue` 继续保留全部 Sample、Complete、Release、None/Driver handoff、OwnerReady 与 owner release，并以 `LocalLogicTick + phase + Sequence` 提供唯一正式顺序。
- `CharacterPresentationStage` MUST NOT再把 handoff command 压平成裸 intent 列表；完整有序 record 必须进入动画仲裁阶段。
- 将 `CharacterAnimationLayerArbitrator` 从“只计算 DesiredCandidate 的无状态 helper”提升为动画层 commit 仲裁器：
  - 先按 layer priority、weight 与 blend mode 计算完整 DesiredCandidate；
  - 再维护尚未被视觉消费的有序 owner-transition ledger；
  - 先归并连续因果链，再对互不连通的竞争链做 authority 仲裁；
  - 每个正式 LayerId 每个表现帧只输出一个完整 `AnimationLayerPlan`。
- `AnimationLayerPlan` 必须显式表达 layer、DesiredCandidate、`InitialSeed/Update/Hold/Handoff/Empty/Invalid` 指令；Handoff 指令额外表达当前可见来源、最终目标、唯一 strategy、所选 Driver 与完整因果 provenance。
- 连续因果链使用带 activation generation 的 logical/resolved owner 连接，并且连接方向必须符合 command 顺序。Role=None 不能提供混合策略，但必须作为链路桥接事实保留。
- 同一连续链上的多个 Driver 不再互相冲突：由该链通往最终可见目标的最后一个 Driver 提供 strategy，前面的 Driver 标记为 Coalesced。实际 outgoing 始终是 LayerRuntime 当前 FinalOutput，实际 incoming 始终是完整 DesiredCandidate，中间未显示状态不建立虚拟播放。
- 命令顺序只允许在已经证明连通的同一因果链内确定先后，MUST NOT作为互不连通 Parallel 请求的隐藏胜负规则。
- 对互不连通的因果组件继续使用可见 endpoint contribution priority 计算 authority。较低 authority 组件作为 underlay Retired；多个相同最高 authority 的独立组件仍是正式配置/运行歧义。
- `AnimationOwnerReady` 与 owner release 的持久引用迁入仲裁 ledger。target 尚未 Ready 或 RequireOutput incoming 尚未形成时，Arbitrator 输出 Hold plan；ready/release 事实直到所有相关 plan 不再引用后才清理。
- `CharacterAnimationLayerRuntime` 降为持久播放执行器：只保存 Final/Held output、唯一 ActiveHandoff、blend elapsed 与 inertialization session，并消费单个 `AnimationLayerPlan`。它不再接收原始 intents、维护多个 PendingDriver、解析 owner leaf、计算 authority 或按 endpoint 猜 Driver。
- ActiveHandoff 被新 plan 抢占时，Runtime 继续从当前 FinalOutput 或最终 pose/velocity capture；不建立 handoff stack。
- diagnostics snapshot/trace 改为展示 ordered record range、causal components、Coalesced/Selected/Retired/Conflict disposition、最终 LayerPlan 与播放 handoff lifecycle，删除含糊的裸 PendingDriver/DriverIds 观察口径。
- Timeline Preview 继续复用同一 Registry、Arbitrator、LayerPlan、LayerRuntime 与 Presenter；非连续 seek、target switch 和 dispose 同时清理 ledger 与播放状态。
- Corin StateMachine、Timeline、动画 clip、HandoffRole 与 layer 配置保持不变。本修正不通过改边、降频状态机或减少合法状态切换规避故障。
- 删除旧 endpoint-only matcher、每层 PendingDriver 列表、source/target 任一端命中即算 Driver、LayerRuntime 内 ready/release 管理和相关 debug 字段，不保留兼容或并行路径。

## Capabilities

### Modified Capabilities

- `character-animation-layer-runtime`：动画仲裁器生成每层唯一 LayerPlan，LayerRuntime 只执行计划并保存视觉播放历史。
- `character-animation-pipeline`：逻辑事实到动画播放之间增加正式 commit 仲裁，不再把原始 Driver 交给播放层。
- `character-pipeline-runtime`：PresentationFrame 保留并消费完整有序 lifecycle records，完成计划后才 acknowledge。
- `btsmtl-sm-node-authoring`：None/Driver 都是有序 transition facts；Role 只提供作者语义，不直接成为播放命令。
- `character-state-interruption-authoring`：连续抢占链先归并，独立 Parallel 竞争再报冲突。
- `character-presentation-interpolation`：表现 delta 只推进已提交 HandoffPlan，不参与逻辑事实排序。
- `btsmtl-timeline-editor-preview`：Preview 复用完整 plan commit 链路。
- `character-gameplay-pipeline-closure`：表现闭环只把每层唯一计划交给播放执行器。

## Current Spec Comparison

- current `character-pipeline-runtime` 已要求 lifecycle command 按 tick、phase、sequence 保序，并明确不能只保留最后一个 catch-up tick；当前实现只在 queue 中满足该要求，抽取 handoff intents 后丢失顺序，属于合同未闭环。
- 本 active change 原先新增的“可见 Owner 变化必须由唯一 Driver 解释”把“同一因果链上的多个 Driver”和“独立组件的多个 Driver”混为一类，并要求 LayerRuntime 直接按 Previous/Desired endpoint 匹配。该 requirement 必须改为“先归并因果链，再仲裁独立组件”。
- 本 active change 原先要求 LayerRuntime 保存 `PendingDriver`、ready leaf 与 released owner；这与项目文档中 `CharacterAnimationLayerArbitrator` 作为仲裁阶段、LayerRuntime 作为持久播放权威的目标职责冲突。本提案把 pending transition ledger 与 ready/release 引用迁入 Arbitrator，只让 Runtime 保存已提交计划的播放状态。
- `character-animation-pipeline` 当前 active delta 已要求 PresentationStage 原子消费完整批次，但没有规定完整 command 顺序必须跨过 contribution candidate 阶段，也没有定义每层唯一 commit plan；本提案补齐这两个合同。
- `btsmtl-sm-node-authoring` 与 `character-state-interruption-authoring` 继续要求 Role=None 不主导策略、Role=Driver 显式提供 strategy；本提案不改变作者配置，只改变 runtime 如何解释连续事实。
- `add-btsmtl-compiled-runtime-debugging` active delta 仍残留 `TransitionRuntime` 旧术语。它在归档前必须重基线到 ordered records、causal components、LayerPlan 与 playback handoff，不得恢复已删除 TransitionRuntime。
- 现行 specs 没有要求修改 AnimationClip、MotionCurve、root motion、Motion Warping、Blackboard、TreeClip、输入、网络或 Corin 状态结构，因此这些全部保持不变。

## Dependency And Apply Order

`refactor-animation-layer-playback-authority` 尚未归档且正是本缺口的所有权 change，因此直接修订并重新打开该 change，不创建第二个重叠 change。

正式顺序：

1. proposal 阶段更新本 change 的 proposal、design、tasks 与 spec deltas。
2. apply 本 change 新增的 ordered commit arbitration 工作，并删除 endpoint-only 旧实现。
3. 使用 required flags 完成静态编译并 strict validate 本 change。
4. 将 `add-btsmtl-compiled-runtime-debugging` 在归档前重基线到新 trace 合同。

本 change 在上述实现完成前 MUST NOT保持 `Complete`，也 MUST NOT归档原有 endpoint matcher 规格。

## Out of Scope

- 不修改 StateMachine 逻辑调度、条件、transition 命中结果、abort 优先级或 tick 频率。
- 不让逻辑仲裁只保留最后一个 transition；完整逻辑生命周期仍必须保留。
- 不重放或显示被表现帧跳过的中间动画状态。
- 不新增 command rollback、通用 event-sourcing、事务重放或网络回滚框架。
- 不修改 Corin StateMachine/Timeline 资产、HandoffRole、blend 参数或动画资源。
- 不新增隐藏 layer、默认 Idle、fallback clip、兼容 Driver matcher或临时桥接路径。
- 不修改 Motion、root motion、Motion Warping、IK、Motion Matching、Blackboard、TreeClip、输入与网络事实。
- 不新增自动化测试，不运行 Unity batchmode。

## Impact

- 主要 runtime：`CharacterAnimationContributionLifecycle`、`CharacterAnimationLayerArbitrator`、`CharacterAnimationLayerRuntime` 与 `CharacterPresentationStage`。
- 观察合同：`AnimationLayerFrameSnapshot`、animation trace 与 Host Inspector 的 layer/handoff 信息。
- Preview：`CharacterPipelineHost.PreviewAnimationRuntime` 的私有动画链路。
- StateMachine/Graph 只需保证所有 None/Driver facts 继续进入同一有序 queue，不修改资产语义。
- 这是中等风险的内部破坏性重构：输入事实和最终播放输出保持不变，中间合同从“raw Driver list”替换为“one LayerPlan per layer”，旧 matcher 与 PendingDriver 状态全部删除。
