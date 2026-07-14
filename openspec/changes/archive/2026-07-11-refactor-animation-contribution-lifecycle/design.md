# Design: 统一动画贡献生命周期

## Context

当前动画提交链路已经统一，但生命周期仍是隐式的：

```text
TimelinePlaybackScheduler.SamplePresentation
  -> frame.Output.Presentation.AnimationContributions
  -> CharacterAnimationLayerRuntime.Build(currentFrameContributions)
  -> CharacterPresentationStage
  -> AnimancerAnimationPresenter.ApplyVisual
```

当某个 producer 在当前表现帧没有写入 contribution 时，LayerRuntime 不会生成对应 plan，Presenter 会停止该 state。这个行为无法区分以下业务含义：

- Timeline 仍存在，但当前时间已经离开某个 `ExtraPolationMode=None` clip，应该释放该 clip contribution。
- Once Timeline 已完成，但所属 State 还没有完成 transition，应该保持最后一个仍有效的动画 contribution。
- 状态已经发生 transition，旧 contribution 应作为 outgoing 参与淡出。
- producer 已经取消或 owner 已销毁，应该立即正式释放。
- 当前 render frame 执行了多个 logic tick，中间的 Complete/Transition 被后一个 transient frame 覆盖。

因此问题不是 Animancer 插值公式，而是统一动画层缺少来源无关、可保序的生命周期合同。

## Goals

- 保持 Timeline、State、Tree、Action 都只通过统一提交合同影响动画。
- 让 contribution 的创建、更新、完成、退场和释放具有显式语义。
- 让 State transition 的 outgoing/incoming 来自统一注册表，不依赖旧状态继续 tick，也不只依赖上一表现帧缓存。
- 在 60Hz logic / 120fps render 和单帧 catch-up 多个 logic tick 下保持相同动画生命周期结果。
- 保持 AnimationTrack 只在表现帧采样动画，logic tick 不生成动画 pose。
- 保持 Timeline clip 的 None/Hold、weight、priority 和 layer authoring 语义，不用 registry 隐藏资产空段。

## Non-Goals

- 不让统一注册表选择隐藏 Idle、默认 clip 或 fallback controller state。
- 不让 TimelinePlaybackScheduler 直接管理 layer 混合或 Animancer state。
- 不让 Animancer 根据自身播放完成事件反向决定 StateMachine 或 Timeline 状态。
- 不重新 tick 已退出状态来获得 outgoing pose。
- 不修改 Corin 动画 clip 和 Timeline window 数据。

## Proposed Architecture

```text
Timeline / State / Tree / Action producers
                |
                v
AnimationContributionSubmissionQueue
                |
                v
CharacterAnimationContributionRegistry
                |
                v
CharacterAnimationLayerRuntime
                |
                v
CharacterPresentationStage
                |
                v
AnimancerAnimationPresenter
```

### 1. 职责边界

`AnimationContributionSubmissionQueue` 保存尚未被表现帧消费的生命周期命令。它属于 presentation runtime，不属于 `CharacterPipelineFrame.Output` 的 transient 真相，因此不能在每个 logic tick 的 `Begin()` 中被清空。

`CharacterAnimationContributionRegistry` 是动画播放实例和 contribution 存续的唯一状态化权威。它不选择最终 layer 结果，不调用 Animancer，也不生成 gameplay facts。

`CharacterAnimationLayerRuntime` 保持为无状态或帧内纯仲裁器，只消费 registry 当前快照，处理 layer、priority、override/additive、weight normalization、mask 和 debug snapshot。

`CharacterPresentationStage` 负责 visual pose、状态 transition session、outgoing/incoming 权重和最终 visual playback plans。它不能修改 Timeline status 或 gameplay facts。

`AnimancerAnimationPresenter` 继续只应用最终 plan。它可以停止最终计划中已经不存在的 state，但“是否不存在”必须是 registry 和 presentation transition 已经完成正式释放后的结果。

### 2. 身份合同

统一提交至少需要三类身份：

- `PlaybackInstanceId`：一次 producer 播放实例的身份。Timeline 使用 `TimelinePlaybackHandle` 映射得到；同一个 TimelineNode 再次播放必须获得新实例身份。
- `ContributionInstanceId`：一次 playback 内单个动画 contribution 的身份。Timeline 至少需要区分 track 和 clip slot，不能只用 AnimationClip 引用，因为同一 clip 可以在同一 Timeline 中出现多次。
- `OwnerScopeId`：拥有一组 contribution 的 runtime 业务 scope。StateMachine 内的 owner 必须是某次 state activation，而不是静态 state GUID；独立 Timeline 可以使用 playback-scoped owner。

`SourceId`、track name 和 clip reference 继续用于 authoring 追踪与 debug，但不能代替 runtime instance identity。

### 3. 生命周期命令

统一提交使用显式命令，而不是通过当帧列表缺席推断：

- `Sample`：创建或更新一个 contribution 的 clip、time、weight、priority、layer 和 loop context。
- `Complete`：producer 不再推进该 contribution，但该 contribution 当前仍是 owner 的有效终止表现，可以进入 `CompletedHeld`。
- `Release`：该 contribution 已离开有效 clip 范围、owner 已明确释放，或 playback 失败/销毁，不再参与后续仲裁。
- `OwnerTransition`：source owner 退出并向 target owner 交接，携带 transition id、duration 和 curve。duration 为 0 时仍必须存在，表达原子退场与进入。
- `OwnerReady`：target activation 的 state body 已经实际 tick 过一次，OnEnter 或 Root 中的 producer 已获得提交请求的机会；Registry 可以正式执行此前 pending 的 owner handoff。

Registry 内部状态可以表达为：

`Active -> CompletedHeld -> Outgoing -> Retired`

`Complete` 只作用于当时仍存在的 contribution。已经因为 clip 离开范围而 `Release` 的 contribution 不得在 Timeline 最终完成时被恢复。

### 4. Clip membership 与禁止隐式 Hold

Registry 不能把“本表现帧没有 Sample”自动当作 Release，因为 logic catch-up、terminal handoff 和 producer 调度可能跨帧；同时也不能无限保留所有历史 contribution，否则会把 Timeline 空段隐式变成 Hold。

因此每个 producer 必须维护自己已提交的 contribution membership：

- 当前时间进入 clip 时提交 `Sample`。
- clip 持续有效时更新同一个 `ContributionInstanceId`。
- 当前时间离开 `ExtraPolationMode=None` clip 时提交 `Release`。
- `ExtraPolationMode=Hold` 时 track 继续提交正式 Hold sample，而不是由 registry 猜测。
- Timeline 内多个重叠 clip 各自拥有 contribution identity，继续由统一 LayerRuntime 处理 weight 和 priority。

这保证 `Attack1` 动画在第 49 帧结束、后续 Timeline 仍运行但 extrapolation 为 None 时仍会真实释放攻击动画；本 change 不会把该资产缺口自动延长到第 80 帧。

### 5. State activation owner scope

StateMachine runtime 每次进入 StateNode 时生成新的 activation scope。scope 至少能区分：

- 哪一个 StateMachine runtime；
- 哪一个 StateNode；
- 该 StateNode 的第几次 runtime activation。

StateMachine 在 tick OnEnter、Root 和 OnExit 图时，通过通用执行 scope 合同向 `BaseGraph.User` 暴露当前 activation。`CharacterGraphContext` 在 TimelineNode 或其它动画 producer 提交时，把该 scope 写入 playback/contribution 请求。

这里必须使用 BTSMTL 通用 runtime scope 接口，不能让通用 StateMachine runtime 依赖 Character、Animancer 或 Timeline 类型。若实现只能通过 state 名称、Owner.name、场景搜索或 Character 专属静态变量猜测 owner，必须停止。

State transition 完成时，无论 blend duration 是否为 0，都发布包含 source activation scope 和 target activation scope 的 presentation transition event。现有仅在 duration 大于 0 时发布的 blend event 将被正式 transition event 取代，不保留兼容双事件。

transition event 到达 Registry 时先进入 pending。旧 state 已经停止 tick，但 source owner 的最后合法 contribution 仍保存在 Registry；target state body 在后续 logic tick 首次执行后发布 `OwnerReady`。由于 Timeline 请求会在同 tick 的 scheduler 阶段接管，而动画会在同一 render frame 的 PresentationFrame 采样，队列可在一个表现批次内先接收 target Sample，再执行 ready handoff。若 target 没有任何动画 contribution，ready 仍会正式释放或淡出 source，从而暴露真实空配置，不生成隐藏 Hold。

### 6. Timeline 生命周期

Timeline 仍由 scheduler 统一推进：

- logic tick 推进时间并采样 motion、window、cue、camera 等非动画事实；
- presentation frame 使用插值后的 Timeline 时间采样 AnimationTrack；
- loop Timeline 继续保持同一 playback instance 和 cycle 信息；
- Once Timeline 到达 duration 时立刻向节点状态表发布 `Succeeded`，但不会在 terminal animation handoff 前丢弃所需的 presentation record；
- terminal sample 仍通过表现帧的同一 AnimationTrack 采样和统一提交合同进入 registry，不在 logic tick 烘焙动画 pose；
- state-owned playback 完成或因 state root 停止而取消时，当前仍有效的 contribution 可以 `Complete` 并由 owner scope 保持；
- standalone playback 完成、明确取消、失败或 pipeline deactivation 时必须按正式 owner 规则释放，不留下无法回收的 registry entry。

`TimelineNode` 的 `RunnableNode` 生命周期只映射逻辑播放请求。节点返回 Success、被 stop 或 reset 时可以查询或取消尚未完成的 Timeline request，但不能直接删除已经提交给统一 Registry、仍归当前 state activation owner 所有的表现 contribution。State owner handoff、standalone owner release 或 pipeline dispose 才决定这些 entries 的正式退场。

Scheduler 只保留完成 terminal handoff 所需的最小 pending record。Registry 接收 terminal sample/complete 后，scheduler 可以释放 Timeline clone；后续 outgoing 混合由 registry snapshot 和 PresentationStage 负责，不由 scheduler 继续采样旧 gameplay Timeline。

state-owned terminal record 在表现采样被 Registry 接受后提交内部 acknowledgement，只清理“后续 terminal Sample 仍应标记 CompletedHeld”的短期 playback 元数据，不释放 owner-held entries。这样同一 state activation 内重复完成多个 Once Timeline 也不会无限累积历史 playback metadata。

### 7. Logic catch-up 与命令顺序

一个 render frame 可以执行多个 logic tick。每个 lifecycle command 必须带 `LocalLogicTick` 和同 tick 内的稳定顺序，命令队列必须在每个 logic tick 结束前接收当 tick 事件。

PresentationFrame 按以下业务顺序消费：

1. 收集 active Timeline 的当前表现采样，以及已经 logic-complete 的 terminal pending sample。
2. 按 local logic tick、命令 phase 和稳定 sequence 应用 `Sample / contribution Release / Complete`。
3. 应用 owner transition 和 `OwnerReady`；transition 先 pending，ready 后使 source owner entries 成为 outgoing 或原子 retired。
4. 从 registry 生成 active、incoming 和 outgoing 快照。
5. 交给 LayerRuntime 仲裁，再由 PresentationStage 生成 transition visual plans。
6. Presenter 应用最终计划。
7. PresentationStage/Registry 确认 handoff 后清理可 retired entries，Scheduler 清理已交付 terminal 的 pending records。

如果 Timeline 在较早 logic tick 完成、StateMachine 在后续 catch-up tick transition，两类事件都必须到达同一个 PresentationFrame，且 terminal/最近有效 sample 可以成为 outgoing。`CharacterPipelineFrame.Begin()`、`Output.Clear()` 和 `ClearTransient()` 不得清掉尚未消费的生命周期命令。

### 8. Transition 混合

Transition edge 继续保存 duration 和 curve。状态机只发布 transition 事实，不参与动画权重计算。

非零 duration：

- source owner 当前有效或 `CompletedHeld` entries 转成 outgoing；
- target owner entries 作为 incoming；
- PresentationStage 按 edge curve 淡出/淡入；
- 未发生变化的其它 owner contributions 继续保持，例如 Action transition 不应重启 Locomotion，Locomotion transition 也不应重启仍有效的 Attack contribution。

零 duration：

- source owner entries 与 target owner entries 在同一个表现处理批次中原子替换；
- source owner 在 target state body 首次 tick 前保持最后合法输出，`OwnerReady` 后才执行替换；
- 不经过空 registry snapshot；
- 不创建隐藏 blend 或默认 clip。

旧状态行为图在 transition 后不继续 tick。outgoing 只保存动画计划所需的 clip/time/weight 数据，不再产生 window、cue、motion 或 action facts。

### 9. Timeline 编辑器预览

每个 `TimelinePreviewSession` 拥有隔离的 contribution registry：

- 连续播放时按正式 Sample/Release 规则更新；
- 非连续拖拽游标时先重置 session registry，再从目标时间重建当前有效 contributions；
- 切换 target、停止预览或关闭窗口时释放 session owner；
- preview registry 不读取角色 runtime registry，也不把 preview state 写回 Timeline asset；
- preview 继续使用正式 LayerRuntime 和 Animancer adapter，不实现第二套仲裁。

## Tradeoffs

### 方案 A：独立统一 Contribution Registry，再由 LayerRuntime 纯仲裁

业务收益是所有动画来源共享同一生命周期，Timeline 不获得特殊权力，LayerRuntime 的 priority/layer 规则保持清晰，后续受击、技能或网络表现来源可以直接接入。代价是新增 identity、owner scope、命令队列和 handoff 协议，跨 StateMachine、Timeline 和 Presentation 多个模块。本 change 采用该方案。

### 方案 B：直接让 CharacterAnimationLayerRuntime 变成有状态播放器

业务收益是类数量较少，Registry 和 layer resolution 可以在一次调用中完成。代价是生命周期存储、owner transition、layer 仲裁和 debug snapshot 全部耦合在一个类型里，后续扩展 additive、并行状态机和 preview reset 时修改面更大。它仍可满足单一权威，但不符合当前模块化边界。

### 方案 C：只在 TimelinePlaybackScheduler 保留完成 Timeline

业务收益是修复 Once Timeline 闪帧所需代码最少，也容易拿到 terminal time。代价是 State、Tree、Action 等其它 producer 仍没有生命周期；Scheduler 还会开始承担 outgoing 保活和 transition 语义，形成 Timeline 特殊路径。该方案不能表达用户要求的统一处理链路。

### 方案 D：让 Animancer Presenter 保留未再次提交的 state

业务收益是视觉上最快消除绑定姿势闪帧。代价是 Presenter 无法区分 Timeline 完成、clip 空段、state exit 和配置缺失，会把 `ExtraPolationMode=None` 隐式变成 Hold，并让 adapter 成为隐藏生命周期权威。该方案违反无 fallback 和单一动画真相要求。

## Risks And Stop Conditions

- State activation scope 如果不能通过通用 BTSMTL runtime contract 传播到内联状态 body，必须停止，不能按 state name、tree name 或静态全局变量猜测。
- Contribution identity 如果无法在同一 playback 内稳定区分重复 clip slot，必须先补正式 runtime identity，不能退回只用 AnimationClip 引用。
- 若 transition event 无法在 duration 为 0 时可靠发布 owner handoff，必须停止，不能让零时长切换继续依赖计划缺席。
- 若 preview 只能绕过 registry 才能支持 seek，必须停止并说明 preview reset 与 runtime lifecycle 的 tradeoff，不能保留第二套正式采样链路。
- Registry entry 必须在 owner release、pipeline deactivation 和 preview session dispose 时确定清理；实现不能通过超时回收掩盖 owner 泄漏。

## Resolved Decisions

- Registry 是统一、来源无关的 presentation runtime，不属于 Timeline Scheduler。
- LayerRuntime 保持仲裁职责，不承担 producer 生命周期推断。
- State owner 使用 activation identity，不使用静态 state GUID 作为唯一 runtime owner。
- Timeline clip 离开范围必须显式 Release；Registry 不自动 Hold。
- Transition duration 为 0 仍发布正式 owner handoff。
- Owner handoff 必须等待 target state body 首次正式 tick 的 `OwnerReady`，但不等待目标必须产出动画；缺动画时暴露真实空输出。
- Corin 动画资源覆盖范围不在本 change 中自动修改。
