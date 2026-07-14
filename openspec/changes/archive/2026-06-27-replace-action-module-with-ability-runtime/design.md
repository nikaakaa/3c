# Design: 独立 Ability Runtime 和图执行体边界

## 背景

当前角色管线已经有明确硬时序：

```text
NetworkReceive
Input
Tree
Timeline
Motion
Presentation
NetworkSend
```

Tree/Graph 可以做玩法编排，但它不应该拥有所有跨系统语义。Ability 激活事务涉及输入、网络确认、预测 key、取消、阻塞、target snapshot 和生命周期，这些不是普通 `SubTreeNode` 或 `StateNode` 的结构职责。

UE/GAS 的可借鉴点不是完整复刻所有子系统，而是边界：

```text
AbilitySystemComponent
  拥有 ability 授予、激活、tag、预测、复制相关状态

GameplayAbility
  表达 ability 的执行逻辑和生命周期入口

AbilityTask
  承接异步执行和等待
```

本项目的轻量映射是：

```text
AbilityRuntime
  独立拥有 ability 激活事务

AbilityAsset
  作者配置入口，引用 BodyGraph

BTSMTL / Graph
  作为 ability body 执行体

Timeline
  作为 ability body 内部的时间窗口编排资源
```

## 目标模型

本变更只实现独立模块：

```text
Character/Ability
  AbilityRuntime
  AbilityAsset
  AbilitySpec
  AbilityRequest
  AbilityActivation
  AbilityContext
  AbilityLifecycleState
  AbilityPredictionPolicy
  AbilityActivationResult
  AbilityTargetSnapshot
  IAbilityBody
```

后续接入角色管线时才增加：

```text
CharacterAbilityStage
  调用 AbilityRuntime
  把 AbilityContext 映射给 CharacterGraphContext
```

最终链路预期是：

```text
Input / AI / Network / Debug
-> AbilityRequest
-> AbilityRuntime
-> AbilityActivation / AbilityContext
-> BTSMTL / Graph body
-> Timeline
-> Motion
```

## 核心对象

### AbilityAsset

`AbilityAsset` 是作者编辑的 ability 外壳，持有：

- `AbilityId`
- 显示名
- ability tags
- activation tags
- block tags
- cancel tags
- target key
- prediction policy
- body graph 引用

它不是旧 `ActionDefinition` 的复活。区别是：`AbilityAsset` 是 ability 包本身，图是它的执行体引用；它不平行保存另一套执行逻辑，也不替代 Graph/Timeline。

### AbilitySpec

`AbilitySpec` 是运行时授予记录，表示某个角色拥有某个 ability。它持有 stable spec id、ability id、asset 引用和运行时 enable 状态。后续如果需要等级、输入绑定或运行时覆盖，应扩展 spec，而不是在 Graph 节点上加业务字段。

### AbilityRequest

`AbilityRequest` 是外部激活请求，来源可以是输入、AI、网络、调试器或脚本。它持有 request id、ability id、source、input sequence、simulation tick 和 target snapshot。

### AbilityActivation

`AbilityActivation` 是一次运行时激活事务，持有：

- `ActivationId`
- `AbilityId`
- `SpecId`
- `PredictionKey`
- `StartTick`
- `InputSequence`
- `TargetSnapshot`
- `LifecycleState`

后续预测、取消、服务端确认和回滚都应围绕 activation，而不是围绕 Graph 节点。

### AbilityContext

`AbilityContext` 是只读上下文，给未来 Graph/BTSMTL 读取：

- 当前是否有 active ability
- 当前 ability id
- activation id
- prediction key
- target snapshot
- ability tags
- authority/prediction policy

Graph 可以读它，但不能通过它创建 ability 身份。

### AbilityRuntime

`AbilityRuntime` 管：

- grant/remove ability
- 接收或直接尝试 `AbilityRequest`
- `CanActivate`
- `Activate`
- `Commit`
- `Cancel`
- `End`
- block/cancel tag 判定
- active activation 和 read-only context

首期只做单 active ability 模型。多 ability 并发、channel、stack 或 montage-like slot 后续再加。

## 生命周期

轻量生命周期如下：

```text
Granted
-> Requested
-> CanActivate
-> Activating
-> Active
-> Committed
-> Cancelling
-> Ended / Rejected
```

首期可以实现为更小的状态枚举，但 API 需要表达：

```text
TryActivate(request)
Commit(activationId)
Cancel(activationId, reason)
End(activationId, reason)
```

## 为什么不把 Ability 放进 Pipeline

Pipeline 管硬时序：

```text
输入什么时候采样
图什么时候 tick
Timeline 什么时候采样
Motion 什么时候 Move
```

Ability 管业务事务：

```text
能不能激活
谁拥有 ability
当前 activation 是谁
prediction key 是什么
取消/阻塞谁
target snapshot 是什么
```

所以最终应该是：

```text
AbilityRuntime 独立
AbilityStage 只是 pipeline adapter
```

本变更先做前者，不做后者。

## 方案取舍

### 方案 A：保留 ActionModule

业务收益：

- 最快能在图上看到动作身份。
- 编辑路径短。

业务代价：

- SubTree 和 Ability 激活事务混在一起。
- 后续预测、取消、阻塞、target snapshot 都会继续塞进节点模块。
- 普通图结构会被 action/ability 身份污染。

不选择。

### 方案 B：独立 AbilityRuntime + AbilityAsset

业务收益：

- 和 UE/GAS 的核心边界一致：系统拥有激活事务，图作为执行体。
- 能给预测、取消、回滚、网络确认留下稳定归属。
- 不恢复旧 `ActionSO`，因为执行体仍是 Graph/BTSMTL。

业务代价：

- 比直接节点模块多一层 runtime 和 authoring asset。
- 首期不能只靠图节点直接表达完整 ability。

选择该方案。

### 方案 C：完整复刻 GAS

业务收益：

- 学习价值高，系统概念完整。
- 长期多人 RPG/动作项目可扩展性强。

业务代价：

- `GameplayEffect`、`AttributeSet`、`GameplayCue`、复杂 tag query 和完整复制会吃掉大量时间。
- 对当前求职向 3C demo 来说过重，容易牺牲动作手感和调试展示。

不在首期选择。

## 和 Graph/BTSMTL 的关系

Graph/BTSMTL 以后是 ability body：

```text
AbilityAsset
  -> BodyGraph
    -> PlayTimeline
    -> WaitTimeline
    -> Write gameplay fact
    -> EndAbility
```

Graph/BTSMTL 可以：

- 读取 `AbilityContext`
- 执行 body
- 请求 Timeline 播放
- 写 facts/tags/resources
- 输出 motion contribution 或 gameplay window

Graph/BTSMTL 不可以：

- 创建 activation id
- 创建 prediction key
- 自己决定 ability 身份
- 绕过 AbilityRuntime 处理 cancel/block/commit

## 和 Timeline/Motion 的关系

Timeline 仍然只表达时间窗口：

- Animation
- RootMotion
- MotionWarpWindow
- HitWindow
- CancelWindow
- Cue

MotionStage 仍然只负责最终运动：

```text
MotionContribution
-> MotionIntent
-> MotionModifier
-> CharacterController.Move
-> MotionResult
```

Ability 不直接改 Transform，不直接 Move。

## 清理策略

实现阶段采取破坏性清理：

- 删除 `ActionModule.cs` 和 `.meta`。
- 删除 `ActionSubTreeNode.cs`、`ActionStateNode.cs` 和 `.meta`。
- 移除 `IActionIdentitySink`、`ActionIdentity`。
- 移除 `CharacterGraphContext` 中 active action 写入逻辑。
- 移除 `StrictGameplayOutput` 中 `ActionId`、`ActionTags` 等字段。
- 用 `AbilityContext` 作为未来 graph 读取入口，不保留旧 action 字段桥接。

## 后续接入位置

后续 `CharacterAbilityStage` 推荐接在 ability request 生产之后、ability body 执行之前。

如果输入或外部系统直接产出 ability request：

```text
NetworkReceive
Input
AbilityStage
BTSMTLPhase
MotionStage
```

如果 BTSMTL 先做高层决策并产出 ability request：

```text
NetworkReceive
Input
BTSMTLDecision
AbilityStage
AbilityBody
BTSMTLPhase 内部 TimelinePlaybackScheduler
MotionStage
```

项目第一条正式主线推荐前者：输入映射和外部事件直接进入 AbilityRuntime，BTSMTL 作为 body 执行层。
