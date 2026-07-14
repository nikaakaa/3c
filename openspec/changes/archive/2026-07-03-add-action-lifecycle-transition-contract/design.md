# Design: Action 生命周期 Transition 和作者心智

## 问题本质

角色主 Graph 是每 tick 执行的编排层，但动作是跨 tick 持续存在的 gameplay transaction。网络和预测要同步的不是“这段 graph 还在不在跑”，而是：

- 哪一帧开始了哪次动作事务。
- 这次事务期间产出了哪些 motion/window/cue/result。
- 这次事务为什么进入确认、修正、正常完成、取消、打断、拒绝或中止。

所以正式语义必须是：

```text
Activation -> ActionInstance / Action Context -> LifecycleTransition
```

Graph、StateMachine 和 Timeline 只是产生这些事实的作者编排方式。

## 术语

- `ActionInstance`：运行时动作事务身份，拥有 instance id、prediction key、start tick、phase、state。
- `Action Context`：作者和节点之间传递“当前输出归属哪次 ActionInstance”的业务上下文。
- `ActionLifecycleTransition`：动作事务生命周期变化事实。
- `ActionScope/ActionBody`：可选作者组织层；如果后续引入，它只负责默认 context 和退出语义，不作为网络同步真相。

## Transition 类型

第一阶段至少需要：

- `Confirm`：权威确认该预测动作成立，不一定结束动作。
- `Complete`：动作正常完成，例如 Timeline 播完、Recovery 结束。
- `Cancel`：主动取消，例如闪避取消、连段切换、新动作覆盖旧动作。
- `Interrupt`：外部打断，例如受击、硬直、击飞、控制。
- `Reject`：权威拒绝，例如服务端认为输入、资源或时机不成立。
- `Correct`：权威修正，例如动作成立但时间、位置、phase 或结果需要修正。
- `Abort`：系统中止，例如 actor despawn、组件禁用、场景切换。

`Complete/Cancel/Interrupt/Reject/Abort` 是 terminal transition。`Confirm/Correct` 默认不是 terminal，除非 payload 明确要求结束。

## 数据合同

`ActionLifecycleTransition` 至少包含：

- `ActionInstanceId`
- `TransitionType`
- `Reason`
- `LocalLogicTick`
- `InputSequence`
- `SourceGraphId`
- `SourceNodeId`
- `SourceName`
- 可选 `ServerTick`
- 可选 correction payload id 或 digest

`ActionRuntime` 应用 transition 后更新 `ActionInstance.State/Phase/LastReason`，并让 terminal transition 关闭 active context。

动作激活也可以附带产出生命周期事实。例如新动作覆盖旧动作时，`ActionRuntime` 必须在同一次 activation outcome 中返回旧动作的 `Cancel(CancelledByNewAction)`。Graph 或 Pipeline 只负责把该事实转发到 frame output 和网络同步域，不重新构造另一条等价 transition。

## 作者心智

作者不应该理解 `handle` 或 `slot`。作者应该看到：

```text
Activate Action Instance
  输出 Action Context

Timeline / Window / Cue / Result / Motion
  输入 Action Context
  产出属于这次动作的事实

Submit Action Lifecycle Transition
  提交 Complete / Cancel / Interrupt / Abort
```

`Activate Action Instance` 和 `Submit Action Lifecycle Transition` 是第一阶段低层节点，服务于合同打通和调试。正式作者主路径后续应由 `ActionScope`、状态机 exit 或 Timeline action binding 封装它们，避免作者手工拼 runtime transaction。

状态机边也必须能表达退出语义：

```text
Attack.Recovery -> Locomotion = Complete
Attack.Any -> Dodge = Cancel(DodgeCancel)
Attack.Any -> HitReact = Interrupt(HitReact)
NetworkReject -> Locomotion = Reject(AuthorityRejected)
```

## 为什么不靠没有 tick 判断结束

“没有 tick 到”只是控制流现象，不是业务事实。它无法区分：

- Timeline 正常播完。
- 状态机切到闪避。
- 受击打断。
- 服务端拒绝。
- actor 被销毁。

网络需要的是 transition type、reason 和 tick。没有这些，服务端和远端只能看到动作突然消失，无法做预测回滚、表现回退或 debug。

## 与 ActionScope 的关系

后续可以做 `ActionScope/ActionBody` 来改善编辑体验：

- Scope 内部默认共享一个 Action Context。
- Scope 的 exit edge 必须配置 transition type。
- Scope 可以在子流程完成、失败或被外部打断时提交 transition。

但运行时真相仍然是 `ActionInstance + ActionLifecycleTransition + SyncDomain output`，不是 subtree membership。
