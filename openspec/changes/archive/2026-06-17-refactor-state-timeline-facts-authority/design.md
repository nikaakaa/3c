## Context
已有 `StateTimelinePolicy` 和 `StateTimelineWindowFacts`，也已有独立 sampler。问题在运行时归属：Action request resolver 会按 current snapshot 采样一次，runner 会按 projected state time 采样一次，transition 后又按新状态采样一次。它们都叫 timeline facts，但语义不完全一样。

## Goals
- current timeline facts 在一帧中只有一个权威来源。
- request submission / interrupt arbitration、transition evaluator、output resolver 的 facts 输入一致。
- projected facts 和 target facts 必须显式命名。
- sampler 仍是纯逻辑模块，不依赖 runner、MonoBehaviour、Animancer runtime 或 motion executor。
- 诊断由外围 adapter 提交，不由 runner 直接写日志。

## Non-Goals
- 不删除 `StateTimelinePolicy`。
- 不把 timeline facts 合并进 ActionInterruptPolicy。
- 不把 animation normalized time、clip、fade 或 TransitionAsset 变成逻辑窗口权威。

## Decisions
### Decision: Character frame context 拥有 current facts
`CharacterFramePipeline` 或等价角色帧上下文 builder MUST 在 Action request submission / interrupt arbitration 前生成 current timeline facts，并把它放入本帧 context。后续模块只能消费该 facts，不得重新自行采样 current facts。

### Decision: projected facts 是 transition 专用输入
如果 transition 判断需要 `StateTime + DeltaTime` 视角，必须以 `ProjectedTimelineFacts` 或等价名字显式表达，并只用于 transition evaluation。它不得被 Action request submission / interrupt arbitration 消费。

### Decision: target facts 是进入状态后的局部事实
发生 transition 后，新状态 Enter/Tick 如需要 timeline facts，必须用 target state、target variant、target state time 重新生成 target facts，并在 frame trace 中可区分。

### Decision: sampler 不理解请求准入
`CharacterStateTimelineFactSampler` 只根据状态、播放进度和 policy 产出 facts。它不调用 ActionInterruptArbiter，不消费 input buffer，不选择 transition。

### Decision: runner 产出 trace，不提交日志
runner 可以返回 transition evaluation trace、facts trace 或等价纯数据诊断结果。日志提交由 Character diagnostics adapter 或等价外围 adapter 统一处理。

## Interface Shape
### CurrentTimelineFacts
- Owner: Character frame context。
- Source: 当前状态 snapshot、当前播放进度 facts、timeline policy。
- Consumers: Action request submission / interrupt arbitration、transition evaluator、state output resolver。
- Invariant: 一帧内同一个 source step 只能有一个 current facts。

### ProjectedTimelineFacts
- Owner: state machine runner transition evaluation。
- Source: 当前状态 snapshot、`StateTime + DeltaTime`、当前播放进度 facts、timeline policy。
- Consumers: transition evaluator only。
- Invariant: 不得传给 request submission / interrupt arbitration，不得写回 current facts。

### TargetTimelineFacts
- Owner: state machine runner after transition。
- Source: target state、target variant、target state time、当前播放进度 facts、timeline policy。
- Consumers: Enter/Tick lifecycle、output resolver。
- Invariant: trace 必须能标识 target state。

## Rejected Alternatives
### Alternative: 让 Action request resolver 继续自行采样
拒绝原因：resolver 需要知道 state machine definition 和 snapshot，Interface 变深但 Leverage 不增加；新增请求类型会继续把状态机结构泄漏到准入模块。

### Alternative: runner 内统一采样后把 facts 回传给 request submission
拒绝原因：request submission / interrupt arbitration 发生在状态推进之前，反向回传会破坏 Character frame order，并且容易形成循环依赖。

### Alternative: 只保留 projected facts
拒绝原因：请求准入需要当前状态窗口视角；自然退出可以需要 projected 视角。两者语义不同，合并会导致同一帧请求准入被未来时间提前放行。

## Test Surface
- `CharacterFramePipeline` / `CharacterFrameSubmission` 是 current facts ownership 的测试入口。
- FullBody action request submission resolver 是禁止反向采样的静态边界测试入口。
- `CharacterStateMachineRunner` 是 projected/target facts trace 的测试入口。
- rollback replay 测试必须比较 current facts 关键字段，避免预测路径和正式路径分叉。

## Risks / Trade-offs
- Risk: current/projected/target 三套名称增加表面复杂度。
  - Mitigation: 三者各自只在一个阶段使用，并通过测试锁定不能混用。
- Risk: 迁移时 Action request submission 和 runner 双采样短期并存。
  - Mitigation: 任务要求先加静态测试禁止 resolver 直接采样，再迁调用点。
- Risk: 诊断日志迁出 runner 后丢字段。
  - Mitigation: trace 数据先覆盖现有 `state-timeline-window-facts` 与 TurnBack condition 诊断字段。

## Migration Plan
1. 增加 current/projected/target timeline facts characterization 测试。
2. 在 Character frame context 中增加 current facts 字段。
3. 让 Action request submission resolver 只消费 current facts。
4. 让 runner context 明确接收 current facts，并独立产出 projected/target facts。
5. 把 runner 内直接日志提交改为 trace 返回。
6. 更新诊断 adapter 统一提交 timeline facts 日志。
