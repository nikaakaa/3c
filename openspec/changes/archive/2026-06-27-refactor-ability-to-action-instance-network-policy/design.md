# Design: ActionInstance 网络事实链路

## 核心结论

本项目不采用 UE/GAS 式 `GameplayAbility owns execution graph`。

最终模型固定为：

```text
Graph/BTSMTL
  唯一玩法编排层

ActionProfile
  动作身份和网络策略中心

ActionInstance
  一次动作启动后的运行时实例身份

Timeline
  时间窗口事实来源

NetworkStage
  收集 command / action instance / window digest / motion sample / correction
```

## 问题关键

接网络的核心问题不是“Graph 怎么同步”，而是：

```text
本地预测出来的动作表现，必须能和服务端裁决结果一一对应。
```

需要对上的对象包括：

- 哪个输入序号启动了动作
- 哪次动作实例产生了窗口
- 哪段窗口用于 combat rewind
- 哪段 motion 是本地预测
- 服务端确认/拒绝的是哪次动作
- correction 应该修哪次 motion/action 表现

因此同步对象不是 Graph，也不是 Tree，而是 Graph/Timeline/Motion 运行时产出的事实。

## 不同步 Graph

系统 MUST NOT 同步 Graph 执行路径。Graph 是本地或服务端的行为编排器，网络同步的是它产出的关键事实：

```text
ClientCommand
ActionInstance
GameplayWindowFact
MotionSample / MotionResult
CombatEvent
PresentationCue
ConfirmedEvent
Correction
ActorSnapshot
```

业务取舍：

- 好处：保持 Graph 统一编排，不需要把 BTSMTL 图做成跨端确定性协议。
- 好处：支持混合架构，本地预测、远端插值、服务端裁决、combat rewind 各用合适事实。
- 代价：需要为事实建立归属字段和集中策略解析，不能只看 Tree 类型。

## ActionProfile

`ActionProfile` 是动作身份和策略包，不拥有执行图。

它表达：

- `ActionId`
- 显示名、调试分类
- tags、block tags、cancel tags
- prediction policy
- authority policy
- replication policy
- correction policy
- target policy
- window policies
- motion policies
- cue policies

它不表达：

- BodyGraph
- Timeline 播放顺序
- Motion 结算逻辑
- 命中成立
- 伤害计算

Graph 通过 `BeginTrackedAction` 引用 `ActionProfile` 或 `ActionId`，Timeline 通过窗口类型产出事实，NetworkStage 用 `ActionProfile + FactType` 查策略。

## ActionInstance

`ActionInstance` 是一次动作启动后的运行时工单号。

字段包括：

```text
ActionInstanceId
ActionId
PredictionKey
InputSequence
StartTick
TargetSnapshot
Phase
State
```

phase 示例：

```text
Startup
Active
Recovery
Cancel
Ended
```

state 示例：

```text
Requested
Predicted
Confirmed
Rejected
Cancelled
Ended
Corrected
```

## Graph 如何产出 ActionInstance

不是给 Tree/SubTree/SMNode 静态打标。

Graph 执行到正式节点或调用正式 context service：

```text
BeginTrackedAction(actionId/profile, target)
```

Runtime 接受后生成：

```text
ActionInstanceId
PredictionKey
```

随后 Graph、Timeline、Motion、Combat、Presentation 的产出事实携带该 instance id，直到：

```text
EndTrackedAction(instanceId)
```

这是一种运行时 action scope，而不是静态 node membership table。

## 为什么不标记 Tree

### 方案 A：Tree/SubTree/SMNode 标记网络类型

例如：

```text
SubTreeModule: NetworkedAction
SMNodeModule: ReplicatedState
```

业务收益：

- 作者一眼看到该结构和网络相关。
- 编辑器可做强约束。

业务代价：

- 粒度过粗。一棵动作流程里 hit、cancel、motion、cue 的网络策略不同。
- 结构语义和网络事实语义绑死。
- 不适合混合预测、窗口同步、server correction 和 combat rewind。

不选择。

### 方案 B：特殊 ActionTree / AbilityTree

业务收益：

- 作者心智强，打开就是动作树。
- 可以强制入口、出口和调试 UI。

业务代价：

- 新增图类型，破坏当前 Graph/BTSMTL 统一编排层。
- 网络仍然需要细分窗口、motion、cue 的策略，特殊 Tree 不能解决事实归属。

不选择第一阶段。

### 方案 C：ActionProfile + ActionInstance + 事实策略

业务收益：

- 最贴合项目 goal 的混合网络架构。
- Graph 继续统一编排。
- ActionProfile 集中策略，避免散在每个 clip/node 上。
- ActionInstance 让预测、窗口、motion、server confirm 能一一对应。

业务代价：

- 作者需要理解“Graph 编排”和“ActionInstance 归属”是两个维度。
- Editor 需要提供清晰 inspector 和 debug overlay，否则会感觉抽象。

选择该方案。

## 网络策略分层

策略不散写在每个事实上，而是集中配置、事实引用。

### Action 级

```text
PredictionPolicy
AuthorityPolicy
ReplicationPolicy
CorrectionPolicy
BlockTags
CancelTags
TargetPolicy
```

### Window 级

```text
WindowType = Hit
AuthorityPolicy = ServerAuthoritative
HistoryPolicy = IncludeInCombatRewind
ReplicationPolicy = DigestOnly
```

### Motion 级

```text
RootMotion
PredictionPolicy = ClientPredicted
CorrectionPolicy = SmoothCorrection

MotionWarp
PredictionPolicy = ClientPredicted
CorrectionPolicy = ServerCorrectable
```

### Cue 级

```text
SwingTrail = LocalPredicted
HitSpark = ServerConfirmed
CameraShake = LocalOnly
```

## UI 心智

作者 UI 分为四个面板：

```text
ActionProfile Inspector
Graph Node Inspector
Timeline Track/Clip Inspector
Runtime Debug Inspector
```

### ActionProfile Inspector

集中配置身份和策略：

```text
Identity
Network
Windows
Motion
Cues
Tags
Debug
```

### Graph Node Inspector

`BeginTrackedActionNode` 只引用：

```text
ActionProfile / ActionId
TargetKey
```

它不配置完整网络策略。

### Timeline Inspector

Window clip 只配置：

```text
WindowType
WindowId
参数
```

它不配置完整权威策略。

### Runtime Debug Inspector

显示：

```text
ActionInstanceId
ActionId
PredictionKey
InputSequence
State/Phase
Windows
Network confirm/reject/correction
```

## 和 UE/GAS 的对应

借鉴：

- prediction policy
- activation/prediction key
- tags/block/cancel
- confirmed/rejected/cancelled 生命周期
- ability/action 运行时实例身份

不借鉴：

- `GameplayAbility` 拥有执行图
- `AbilityTask` 框架
- `GameplayEffect` / `AttributeSet` / `GameplayCue` 完整生态
- 用 Ability 蓝图替代项目 Graph 编排

对应表：

| UE/GAS | 本项目 |
| --- | --- |
| GameplayAbility class defaults | ActionProfile |
| Ability activation / prediction key | ActionInstance |
| Ability task / blueprint graph | Graph/BTSMTL + Timeline |
| GameplayCue policy | CuePolicy |
| Tag block/cancel | ActionProfile tags |
| GameplayEffect | 后续 Combat/Effect proposal，不在本变更 |

## 后续管线接入方向

目标链路：

```text
InputStage
-> Graph/BTSMTL
-> BeginTrackedAction
-> ActionRuntime
-> TimelineStage
-> MotionStage
-> PresentationStage
-> NetworkSendStage
```

服务端确认回来：

```text
NetworkReceiveStage
-> ActionRuntime mark confirm/reject/correct
-> Graph 读取事实继续编排
-> MotionStage 做 correction smoothing
-> PresentationStage 调整表现
```

本变更只规划数据结构、策略和清理方向，不实现真实网络。
