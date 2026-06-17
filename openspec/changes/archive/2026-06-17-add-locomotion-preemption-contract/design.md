# Design: FullBody 抢占 Locomotion transient motion source

## Context
当前主线已经把 `Locomotion.TurnBack` 留在 Locomotion graph 中，把 `Action.Dodge` 迁到 Action lifecycle。`DefaultBodyArbiter` 可以通过 full-body claim 压制 Locomotion motion / animation output，但这个压制只是 frame output 选择，不会改变 Locomotion graph 的 active state，也不会取消 TurnBack 的 timeline window、pending intent 或 motion playback window。

问题序列：

1. Locomotion graph 进入 `Locomotion.TurnBack`。
2. TurnBack root motion 由 `TurnBackMotionResolver` 根据 TurnBack state、timeline facts 和 baked motion profile 采样。
3. 玩家点按 Shift，Action lifecycle 接受 `Action.Dodge`。
4. BodyArbiter 接受 Dodge full-body claim，当帧压制 Locomotion output。
5. Dodge 结束后，如果 Locomotion 仍是 TurnBack，TurnBack motion source 会继续采样，角色被旧曲线拉回。

该问题不是单个配置错误，而是“输出压制”和“生命周期取消”之间缺少正式契约。

## Goals
- FullBody Action 抢占 Locomotion transient 时，有明确、可测试、可回放的 lifecycle 结果。
- TurnBack 被 Dodge 抢占时，旧 TurnBack motion source 不得在 Dodge 结束后继续执行。
- 规则保持通用：未来 HitReact、Knockback 或其它 full-body action 可复用同一 preemption contract。
- 保持现有抽象边界：Action 不直接拥有 Locomotion，Locomotion 不读取具体 Action 运行时对象，pipeline 不硬编码 Dodge/TurnBack 业务分支。

## Non-Goals
- 不把 Dodge 放回 Locomotion graph。
- 不让 `TurnBackMotionResolver` 判断 Action 状态。
- 不用缩短 TurnBack motion profile 或 timeline window 掩盖问题。
- 不新增并行 pipeline、第二 motion executor 或第二 animation presenter。

## Decisions

### Decision: 抢占在 Action 开始时生效
当 FullBody Action 开始并成功提交 full-body claim 时，若当前 Locomotion state 是可抢占 transient motion source，系统在该帧产生 preemption fact。不要等 Dodge 结束后再取消 TurnBack。

Rationale: Dodge active 期间 base layer 已由 Action 占用，Locomotion 提前退出 TurnBack 不会暴露视觉断层；提前取消能避免 Dodge 结束后旧 motion source 恢复。

### Decision: preemption 是纯数据事实，不是直接状态写入
Action submitter 或 plan builder 只产生 `LocomotionPreemptionFact` 或等价纯数据事实，包含 source locomotion state、source action id、source step 和原因。它不直接调用 Locomotion runtime 的私有方法，也不直接重置 state machine。

Rationale: Action 与 Locomotion 保持 sibling module；状态切换仍由 Locomotion graph / transition evaluator 读取 context facts 后完成。

### Decision: Locomotion graph 消费一次性抢占事实
Locomotion graph 对 `Locomotion.TurnBack` 增加高优先级退出规则：

- 有移动输入：`TurnBack -> MoveLoop`，gait 由 Locomotion intent / Run latch 决定。
- 无移动输入：`TurnBack -> Idle`。

该规则优先于 TurnBack 的自然 `NaturalExitReady` 出口，并且抢占事实必须一次性消费。

Rationale: `Run` 是 gait，不是 graph state。抢占后目标应是 desired locomotion state，而不是写死 `Run` 或 `Dodge -> Run`。

### Decision: TurnBack runtime residue 必须同步清理
消费抢占事实时，Locomotion runtime 必须清除会导致 TurnBack 重新进入或继续采样的残留，包括 pending TurnBack intent 和 TurnBack motion playback window。清理属于 lifecycle 结束，不是 fallback。

Rationale: 只切 state 不清 motion playback window，仍可能在后续 frame 或 rollback/replay 中暴露旧 motion delta。

### Decision: BodyArbiter 的 suppress 语义不扩大
`BodyArbiter` 继续只决定本帧 output 选择和压制关系。它可以参与生成 plan 诊断或携带 preemption 输出，但不承担状态机 transition 逻辑。

Rationale: BodyArbiter 是输出仲裁，不是 Locomotion lifecycle owner。

## Pipeline Touchpoints
- `CharacterFrameSubmitterGraph`：仍保持 Locomotion 与 FullBody Action sibling submitter 顺序。
- `FullBodyActionFrameSubmitter`：在 Action lifecycle started + full-body claim 条件下提交 preemption candidate。
- `CharacterFramePlan` / `CharacterFrameOutput`：携带或暴露最终 preemption fact，同时继续应用 output suppress。
- `ICharacterFrameRuntimePort` / runtime adapter：提供写入或传递 preemption fact 的正式端口。
- `CharacterRuntimeBlackboard` 或等价 runtime facts：保存一次性 preemption fact，供下一次 Locomotion graph context 读取并消费。
- `LocomotionFrameRuntime` / `LocomotionFrameBuilder`：读取 preemption fact，传入 state machine context，并在消费后清理 TurnBack residue。
- `CharacterStateTransitionEvaluator`：新增或扩展条件，使 Locomotion graph 能判断 preemption fact。
- `CorinLocomotionStateGraph.asset`：新增 TurnBack 被抢占后的 MoveLoop / Idle 退出 transition。

## Risks / Trade-offs
- Risk: 如果把 preemption fact 写成 Dodge 专用条件，后续 HitReact/Knockback 会复制规则。
  - Mitigation: 条件命名和数据模型使用 FullBody/Locomotion preemption 语义，Action id 只作为来源诊断。
- Risk: 如果只在 output 层 suppress，不切 Locomotion state，会复现本 bug。
  - Mitigation: 自动测试必须断言 Dodge 结束后不再处于 TurnBack 且无 TurnBack motion delta。
- Risk: 如果直接从 Action submitter 写 Locomotion runtime 私有状态，会破坏 sibling module 边界。
  - Mitigation: 只通过 frame fact / runtime fact / context 传递，Locomotion 自己消费。
- Risk: rollback/replay 若没有保存 preemption fact，会产生分歧。
  - Mitigation: preemption fact 作为纯数据纳入 runtime facts 或等价可恢复输入。

## Migration Plan
1. 先加 characterization tests，复现 TurnBack 中 Dodge 抢占后不应恢复旧 motion source 的目标行为。
2. 增加纯数据 preemption fact 和最小端口。
3. 让 FullBody Action submitter 在 started full-body action 时提交 fact。
4. 让 Locomotion graph context 读取并消费 fact。
5. 更新 Corin TurnBack transition 和 residue cleanup。
6. 补 rollback/replay 或 snapshot 层测试，确认 preemption fact 不造成不可恢复分歧。
7. 运行 OpenSpec validate 与定向 Unity EditMode 测试。

## Open Questions
- 暂按当前讨论收敛：本次只把 `Locomotion.TurnBack` 纳入可抢占 transient。后续如 MoveStart、MoveStop 或其它 locomotion transient 也需要相同行为，应通过同一 preemption contract 扩展列表，而不是新增动作特判。
