# Proposal: 定义 Action 生命周期 Transition 合同

## Why

当前动作网络底座已经能表达 `ActionActivationRequest -> ActionInstance -> Window/Motion/Cue/GameplayResult -> EndRequest` 的本地闭环，但仍有一个核心语义不干净：

- 动作是“一段时间内持续存在的事务”，不是某个 tick、某个 Timeline 或某个 subtree。
- 动作离开不是只有 `End`，还包括正常完成、主动取消、外部打断、权威拒绝、权威修正和系统中止。
- 网络、预测和回滚不能靠“某一帧没有 tick 到节点”来猜动作结束原因。
- 作者界面现在仍暴露 `Action Handle Slot` 这种内部口径，不贴合业务心智。

需要把动作生命周期提升为正式合同：**动作开始产生 Action Context，期间输出归属该 Context，离开必须提交明确的 ActionLifecycleTransition。**

## What Changes

本变更定义动作生命周期 transition 口径：

- `ActionLifecycleTransition` 是动作事务的生命周期事实，至少覆盖 `Confirm`、`Complete`、`Cancel`、`Interrupt`、`Reject`、`Correct` 和 `Abort`。
- `Complete/Cancel/Interrupt/Reject/Abort` 会让动作事务离开 active 状态；`Confirm/Correct` 可以更新状态但不一定结束事务。
- `ActionEndRequest` 不再作为作者和网络主语义；旧 end 语义迁移为 `ActionLifecycleTransition(Complete)`。
- Action Context 的有效性由 `ActionRuntime` 的 active instance 判定，后续节点不能因为持有旧 context 就继续产出动作归属输出。
- Graph、StateMachine、Timeline 或外部事件引起动作离开时，必须给出 transition type、reason、source 和 tick。
- 作者 UI 必须使用 `Action Context` 和 `Exit Semantics` 口径，不向作者暴露 handle/slot 作为主要概念。

## Non-Goals

- 不实现完整 rollback/replay。
- 不实现真实服务端裁决或 Fantasy handler。
- 不把 Graph、StateNode、SubTree、Timeline asset 或节点模块变成动作同步单位。
- 不恢复旧 `ActionModule`、`AbilityBody`、ActionTree 或 node membership table。
- 不强制第一阶段实现完整 `ActionScope` 可视化框；本变更先定义合同和最小作者口径。

## 决策和 Tradeoff

### 方案 A：节点没 tick 就自动销毁 Action Context

- 优点：实现很省，像普通控制流一样自然。
- 缺点：分支切走、Timeline 等待、状态机重入、远端校正都会被误判成结束；网络看不到结束原因。
- 业务取舍：手感和联机会出现难以解释的幽灵取消，不适合 `2v2vE` 中的打断、支援、防守优先裁决。

### 方案 B：保留单一 ActionEndRequest

- 优点：当前代码改动小，正常完成能跑。
- 缺点：`End` 不能区分正常完成、闪避取消、受击打断、服务端拒绝和系统中止；调试和同步语义不够用。
- 业务取舍：本地 demo 勉强可用，但一接预测校正和 PvP 打断就会返工。

### 方案 C：统一 ActionLifecycleTransition

- 优点：动作离开、权威决策和修正都变成明确事实；Graph/SM/Timeline 只负责提交 transition，不决定网络真相。
- 缺点：需要新增 transition 类型、输出、runtime 状态流转和作者 UI 口径。
- 业务取舍：最贴合动作游戏网络 demo，可以清楚解释“这次动作为什么结束/被打断/被服务端否了”。

本 proposal 选择方案 C。

## 与现有 Spec 的关系

- `character-action-activation-flow` 已定义 Graph 通过 activation 建立 ActionInstance，本变更补上动作离开和状态变化的统一 transition。
- `character-action-instance-runtime` 已定义 ActionRuntime 是事务层，本变更要求它应用 lifecycle transition，而不是只处理零散 end/reject/correct 方法。
- `character-action-authoring-closure` 已要求 UI 分层，本变更把作者可见口径从 handle/slot 收敛为 Action Context 和退出语义。
- `define-character-network-sync-domains` 已定义 ActionSyncDomain 稳定键是 `ActionInstanceId`；本变更后 ActionSyncDomain 应同步 lifecycle transition，而不是只同步 end request。
