# Design: Action 运行时状态跟踪器

## Context

`ActionInterruptArbiter` 是无状态纯函数：

```text
ActionInterruptContext + requests + policies -> ActionInterruptDecision
```

它不应该自己记住当前角色处于哪个 Action，也不应该自己推进 elapsed time。BBB 的 `OverrideState` 同时承担了当前动作事实、动画播放、结束回调和返回状态。本项目当前先只拆出其中最小的一块：当前 Action 事实。

这不是完整状态机，只是事实持有器。它的职责类似 BBB `RuntimeData.Override` 里“当前是否有动作、当前请求是什么、当前 priority 是多少”的纯数据部分，但不保存 `AnimationClip`，不切状态，不播放动画。

## Goals

- 保存当前 Action 状态 ID。
- 保存当前 Action elapsed seconds。
- 保存当前 Action resistance。
- 保存 current tick。
- 能生成 `ActionInterruptContext`。
- 能应用 accepted/rejected `ActionInterruptDecision`。
- 保持 Action 事实层纯数据、可测试、可回滚。

## Non-Goals

- 不实现状态图。
- 不实现自动退出。
- 不定义状态 catalog。
- 不消费输入。
- 不驱动动画。
- 不接角色 prefab。
- 不修改 Locomotion。
- 不实现黑板。

## Proposed Model

```text
ActionRuntimeStateSnapshot
  CurrentState: ActionStateId
  ElapsedSeconds: float
  CurrentResistance: int
  CurrentTick: int

ActionRuntimeStateTracker
  CurrentState
  ElapsedSeconds
  CurrentResistance
  CurrentTick
  Snapshot
  Reset()
  EnterState(ActionStateId state, int resistance = 0)
  Tick(float deltaSeconds, int currentTick)
  CreateInterruptContext()
  ApplyDecision(ActionInterruptDecision decision, int targetResistance = 0)
```

`ApplyDecision` 只读取 decision 是否 accepted。accepted 时进入 target state，重置 elapsed seconds，并使用调用方传入的 target resistance；rejected 时不改变任何状态事实。

## Decisions

### Decision: 不叫状态机

本变更使用 `ActionRuntimeStateTracker` 作为概念名。

Reason: 第一版没有状态图、transition、condition、entry/exit 节点，也不用状态机库。叫 tracker 更贴近真实职责。

### Decision: tracker 不解析状态定义

第一版不做 `ActionStateDefinition`，target resistance 由调用方传入或使用 0。

Reason: 当前只需要给 arbiter 一个当前事实源。状态定义表和编辑器会引入额外配置层，等 action runtime 需求稳定后再单独规划。

### Decision: tracker 不调用仲裁器

tracker 只提供 `CreateInterruptContext` 和 `ApplyDecision`。外部组合：

```text
context = tracker.CreateInterruptContext()
decision = ActionInterruptArbiter.Arbitrate(context, requests, policies)
tracker.ApplyDecision(decision, targetResistance)
```

Reason: 保持仲裁和状态事实分离，避免 tracker 变成隐藏的 action decision system。

### Decision: 不做自动退出

第一版不会基于 duration 自动回到 `Action.None`。

Reason: 用户当前确认“就是一个状态信息而已”。自动退出需要 action state definition 或动画事实，不属于本变更。

## BBB 对比

BBB 的等价运行时事实在：

```text
RuntimeData.Override.IsActive
RuntimeData.Override.Request
OverrideState.CurrentPriority
```

但 BBB 的 request 直接保存 `AnimationClip`，`ActionArbiter` 直接切 `OverrideState`，`OverrideState` 直接播动画。本变更只保留“当前动作事实”这部分：

```text
CurrentStateId
ElapsedSeconds
CurrentResistance
CurrentTick
```

这样后续可以再接输入、动画、黑板、预测回滚，而不会提前耦合到 clip 或角色主控。

## Risks / Trade-offs

- Risk: 没有状态定义时 resistance 来源需要调用方提供。
  - Mitigation: 第一版测试显式传入 resistance；后续再由状态定义或 catalog 提供。
- Risk: 没有自动退出时 action state 可能一直保持。
  - Mitigation: 这符合本变更“只做状态事实”的范围；退出策略另起 proposal。
- Risk: 和之前 `add-minimal-action-state-machine` 重叠。
  - Mitigation: 新 proposal 取代旧 proposal，implementation 只做 tracker，不做 state machine。

## Validation

- OpenSpec strict 校验。
- Unity EditMode 测试覆盖：
  - 默认状态为 `Action.None`。
  - `EnterState` 设置 current state 和 resistance。
  - `EnterState` 重置 elapsed seconds。
  - `Tick` 推进 elapsed seconds 和 current tick。
  - 负 delta 不减少 elapsed seconds。
  - 负 resistance 被安全处理。
  - `CreateInterruptContext` 输出当前 state、elapsed、resistance、tick。
  - accepted decision 更新 current state。
  - accepted decision 重置 elapsed seconds。
  - accepted decision 使用传入 target resistance。
  - rejected decision 不改变状态事实。
  - 组合 `ActionInterruptArbiter` 后 accepted decision 可更新 tracker。
  - 静态搜索确认 Action tracker 不引用 Animancer、AnimationClip、Animator、CharacterController、Cinemachine、Input System 或 BBB。

## Future Extensions

- Action state definition / catalog。
- Action decision system / tick phase handler。
- 输入缓冲到 action request mapper。
- Action animation presenter。
- 自动退出和 return state。
- FullBody / UpperBody / LowerBody 分层。
- Character runtime context / blackboard。
- 预测回滚 snapshot。
