# Proposal: 让 Timeline 承载动作时间事实

## Why

当前 runtime 已经支持 `ActionWindowTrack` 和 `ActionCueTrack` 采样，但 Corin 攻击闭环仍在 RootTree 中手动平铺 `Submit Attack Window`、`Submit Attack Cue` 和 `Submit Loopback Result`。这会让作者误以为动作事实应该在主图逐个提交，而不是在 Timeline 中按时间编排。

Timeline 攻击的正确心智应该是：

```text
Action State OnEnter: Activate Action Context
Action State Root: Play Timeline(Action Context)
Timeline: Animation / Window / Cue / MotionWarp
Action State OnExit: Lifecycle Transition
```

RootTree 不应该知道 HitWindow、GameplayCue 的具体时间。

## What Changes

- 明确 Timeline 攻击的 window/cue 必须由 Timeline 轨道表达。
- 要求 `ActionWindowTrack` 和 `ActionCueTrack` 的输出继续通过 Action Context 关联 ActionInstance。
- 要求 Timeline window/cue clip 只保存输出类型、id、时间和业务参数，不保存完整网络策略。
- 清理 Corin 攻击闭环中由 RootTree 平铺补动作 window/cue 的错误做法。
- GameplayResult 不强行塞进 Timeline；命中结果仍来自 gameplay solver、loopback debug 或服务端裁决。

## Non-Goals

- 不实现完整 combo 状态机。
- 不配置 locomotion 状态机。
- 不实现服务端命中、伤害或目标归属裁决。
- 不让 Timeline asset 或 clip membership 成为 ActionInstance 身份。
- 不新增 per-node 网络策略。

## 当前代码事实

- `TimelinePlaybackScheduler` 已能采样 `ActionWindowTrack` 和 `ActionCueTrack`。
- `TimelinePlaybackScheduler` 在 Action Context 有效时会提交 `ActionWindowSample` 和 `ActionCueEvent`。
- `ActionProfile` 已集中配置 window、motion、cue 和 gameplay result 策略。
- `CorinAttackTimeline.asset` 当前只有 `AnimationTrack`。
- `CorinPlayableRootTree.asset` 当前有平铺的 `Submit Attack Window`、`Submit Attack Cue` 和 `Submit Loopback Result` 测试节点。

## 决策和 Tradeoff

### 方案 A：继续在 Graph 中手动提交 window/cue

- 优点：调试直观，短期不用改 Timeline 资产。
- 缺点：时间事实和动画 Timeline 分裂；RootTree 变脏；复用 Timeline 时策略预览更难解释。
- 业务取舍：不适合动作手感调试。

### 方案 B：所有 action 输出都必须来自 Timeline

- 优点：动作时间结构高度统一。
- 缺点：持续格挡、非 Timeline 受击、环境交互等会被迫创建无意义 Timeline。
- 业务取舍：过度收紧，不符合 current spec。

### 方案 C：Timeline 动作用 Timeline 轨道，非 Timeline 动作保留 Graph/stage 输出

- 优点：时间型动作干净；非时间型动作仍灵活；共享同一 Action Context 和 policy resolver。
- 缺点：作者需要理解 Timeline 动作和非 Timeline 动作的边界。
- 业务取舍：最贴合当前项目。

本 proposal 选择方案 C。
