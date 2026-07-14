# Proposal: 增加 StateMachine 运行事实

## Why

当前 `StateMachineGraphRuntime` 能运行 `StateNode`，但 TransitionRuleGraph 看不到当前状态运行事实。这样 `RunStart -> RunLoop`、`WalkEnd -> Idle`、`Attack1 -> None` 只能靠输入条件或永真 transition，无法表达“状态行为 Timeline 播完后再切”。

这会直接影响角色手感：

- 起步状态会瞬间跳到 loop，起跑动画没有完整时序。
- 停步状态无法等 Timeline 完成后回 Idle。
- 连招状态无法基于 attack recovery、combo window 或状态 root 完成条件进入下一段。

## What Changes

- 在 `StateMachineGraphRuntime` 的运行工作副本中维护当前 active state 的运行事实。
- 运行事实至少包含 active state identity、elapsed ticks、elapsed seconds、状态 root 上次返回状态和状态 root completed。
- 提供 TransitionRuleGraph 可读的 value node 或等价只读接口。
- 保持 `StateMachineGraph` 本层只表达状态结构；事实读取节点只能出现在 TransitionRuleGraph 等条件图中。
- 保持 `StateBehaviorSubTree` root completed 不自动退出状态，离开仍由同层 Transition 决定。

## Non-Goals

- 不配置 Corin locomotion 或 action 资产。
- 不新增 Timeline 轨道。
- 不实现 motion channel 仲裁。
- 不新增业务状态节点类，例如 `RunStartNode`、`Attack1Node`。
- 不让状态名、SubTree membership 或 Timeline asset identity 成为网络同步身份。

## 当前代码事实

- `StateMachineGraphRuntime` 当前持有 `m_ActiveState`，并在每 tick 调用 `m_ActiveState.UpdateState(deltaTime)`。
- `StateNode.UpdateState()` 当前会 tick 状态行为图，但没有向 transition rule 暴露 root 返回状态。
- `StateBehaviorSubTree.UpdateStateRoot()` 会返回 root 运行状态。
- `TransitionRuleGraphRuntime.Evaluate(context)` 当前只接收 `BaseGraph` context，并未接收状态机 runtime facts。

## 决策和 Tradeoff

### 方案 A：用固定等待时间节点拼在状态行为里

- 优点：不改状态机 runtime。
- 缺点：transition rule 仍不知道状态是否完成；Timeline 播放完成和状态切换条件分裂。
- 业务取舍：不适合调 locomotion 起停和连招 recovery。

### 方案 B：State root Success 自动退出状态

- 优点：实现简单，Timeline 播完自然离开。
- 缺点：Idle/Loop 这类状态会被迫重新进入或退出；状态机失去“状态保持由 Transition 决定”的语义。
- 业务取舍：破坏现有 StateMachine 设计。

### 方案 C：记录 runtime facts，由 TransitionRuleGraph 显式读取

- 优点：状态机语义干净；transition 条件可组合输入、时间和 root 完成；适配 locomotion 和 action。
- 缺点：需要补 runtime facts、上下文传递和 value node。
- 业务取舍：最适合后续手感调试。

本 proposal 选择方案 C。
