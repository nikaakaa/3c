# Design: StateMachine 运行事实

## 运行事实模型

运行事实属于 `StateMachineGraphRuntime`，不属于 authoring graph data。第一阶段建议提供：

- `ActiveStateGuid`
- `ActiveStateName`
- `StateElapsedTicks`
- `StateElapsedSeconds`
- `StateRootLastStatus`
- `StateRootCompleted`
- `ExitingStateGuid`
- `PendingTargetStateGuid`

`StateRootCompleted` 来自 active `StateNode` 的状态行为 root 返回 `Success`。`Failure` 应作为 root failed 记录，不等价于 completed。

## TransitionRuleGraph 读取方式

`TransitionRuleGraphRuntime` 可以接收一个只读 evaluation context，里面带当前状态机 runtime facts。Value node 通过 `Owner.User` 或 graph runtime 注入的正式上下文读取这些 facts。

第一阶段最小节点：

- `State Elapsed Seconds`
- `State Elapsed Ticks`
- `State Root Completed`

这些节点必须是 ValueNode，不是 RunnableNode。

## 不自动退出状态

状态 root completed 只是一条事实。状态仍保持 active，直到 transition rule 成立。

这样可以表达：

```text
RunStart -> RunLoop:
  StateRootCompleted && MoveMagnitude >= RunThreshold

RunEnd -> Idle:
  StateRootCompleted

Attack1 -> Attack2:
  StateElapsedSeconds >= ComboOpenTime && HasInputRequest("Attack")
```

## Debug

第一阶段可把 runtime facts 暴露给 editor/runtime debug，但不要求做完整 UI。至少代码链路应能定位当前 active state、elapsed 和 root status。

## 边界

该能力不决定 motion 仲裁、不提交 SyncFacts、不读取 network backend。它只是给 StateMachine transition rule 提供状态机自身的运行上下文。
