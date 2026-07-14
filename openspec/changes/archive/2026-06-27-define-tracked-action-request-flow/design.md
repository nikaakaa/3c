# Design: Graph Request 驱动的 ActionInstance 闭环

## 统一口径

| 名称 | 含义 | 不是什么 |
| --- | --- | --- |
| ActionProfile | 动作身份和网络策略配置 | 不拥有 Graph，不播放 Timeline |
| TrackedActionStartRequest | Graph 提交“开始一次可追踪动作事务”的请求 | 不是输入 request 本身 |
| ActionInstance | ActionRuntime 接受请求后生成的本次动作事务身份 | 不是 Tree、State、Timeline 或 Ability |
| ActionFact | Timeline、Graph、Motion、Combat、Cue 产出的事实 | 不重复保存完整网络策略 |
| ActionRuntime | 管理 begin/confirm/reject/cancel/end 生命周期 | 不 tick Graph，不裁决命中 |

## 推荐链路

```text
InputStage
  -> CharacterInputRequest

Graph/BTSMTL
  -> 判断输入、状态、资源、目标和业务事实
  -> Submit TrackedActionStartRequest

ActionRuntime
  -> 查 ActionProfile
  -> 检查 block/cancel/prediction/authority 策略
  -> 生成 ActionInstance

Graph / Timeline / Motion / Combat / Presentation
  -> 产出带 ActionInstanceId 的事实

NetworkStage
  -> 后续收集 action instance、window digest、motion sample、combat event
  -> 接收 confirm/reject/correction 后更新 ActionRuntime
```

## 格挡反击示例

作者配置：

```text
ActionProfile = Combat.ParryCounter
PredictionPolicy = LocalPredicted
AuthorityPolicy = ServerAuthoritative
CorrectionPolicy = CancelOnReject
WindowPolicy:
  Hit -> ServerAuthoritative / IncludeInCombatHistory / DigestOnly
  Invulnerable -> ServerAuthoritative / IncludeDigestOnly / OwnerOnly
MotionPolicy:
  RootMotion -> LocalPredicted / SmoothCorrection
CuePolicy:
  ParryFlash -> LocalPredicted
  HitSpark -> ServerConfirmed
```

Graph 编排：

```text
GuardState / ParryBranch
  if HasInputRequest("Guard")
  if HasFact("ReceivedAttackInParryWindow")
  if !HasTag("State.Stunned")
  TryConsumeInputRequest("Guard")
  SubmitTrackedActionStart(
      ActionProfile = Combat.ParryCounter,
      SourceInputRequest = Guard,
      TargetKey = LastAttacker)
  if Started:
      RequestTimelinePlayback(ParryCounterTimeline)
```

ActionRuntime 生成：

```text
ActionInstance {
  InstanceId = 77
  ActionId = Combat.ParryCounter
  PredictionKey = 3009
  InputSequence = 1024
  StartTick = 38020
  TargetSnapshot = LastAttacker snapshot
  Phase = Startup
  State = Predicted
}
```

Timeline 只产事实：

```text
ActionWindowFact {
  ActionInstanceId = 77
  WindowId = Hit_01
  WindowType = Hit
  StartTick = 38027
  EndTick = 38031
  Digest = ...
}
```

非 Timeline 动作同样成立：

```text
HoldGuard
  SubmitTrackedActionStart(Combat.Guard)
  while GuardHeld:
      EmitActionWindowFact(WindowType = Guard)
  SubmitTrackedActionEnd(CurrentActionInstance)
```

## UI 分层

### ActionProfile Inspector

主配置入口：

```text
Identity
Network
Tags
Windows
Motion
Cues
Debug
```

这里配置完整策略。

### CharacterPipelineDefinition Inspector

角色管线资产持有正式 ActionProfile 列表：

```text
Action Profiles
  Combat.ParryCounter
  Attack.Light.01
  Dodge.Forward
```

pipeline 初始化时注册到 `ActionRuntime`。缺失、重复、空 id 直接报配置错误，不做 fallback。

### Graph Authoring

Graph 里不是给节点或 subtree 打 action 标记，而是提交 request。

UI 可以表现为普通 command/request 节点：

```text
Submit Tracked Action Request
  ActionProfile / ActionId
  SourceInputRequest
  TargetKey
  StoreInstanceFact
  ConsumeInputRequest
```

它的职责只是调用 context service。它不改变 Tree 类型，不拥有执行体，不保存 window/motion/cue 策略。

### Timeline Window Inspector

Timeline window clip 只编辑：

```text
WindowType
WindowId
Start / End
Parameters
```

策略从 `ActionProfile + WindowType` 解析。没有 action context 时，Timeline 仍可普通播放，不强制创建 ActionInstance。

### Runtime Debug

显示链路：

```text
InputRequest
TrackedActionStartRequest
ActionInstance
WindowFact / MotionFact / CombatEvent / CueFact
Confirm / Reject / Correction
```

## 设计取舍

### 方案 A：Tree/SubTree/StateNode 绑定 ActionProfile

不选择。

业务代价：

- 静态结构和运行时动作事务混在一起。
- 很容易恢复旧 `ActionModule`。
- 同一状态里可能包含多个动作事务、普通逻辑和 Timeline，静态标记粒度错误。

### 方案 B：Timeline 自动创建 ActionInstance

不选择。

业务代价：

- Timeline 被迫成为动作根。
- 格挡、蓄力、交互、持续状态等非 Timeline 动作表达别扭。
- Graph 对目标选择、资源消耗、输入消费、打断判断的编排地位被削弱。

### 方案 C：Graph 提交 TrackedActionRequest

选择。

业务收益：

- 贴合当前 pipeline 的 Input Request 和 Timeline service 模式。
- Graph 保持唯一逻辑编排层。
- ActionInstance 只承担网络追踪和事实归属。
- Timeline 和非 Timeline 产物共用同一套 ActionInstanceId。

业务代价：

- 需要清晰 UI，否则作者会误以为 ActionProfile 应该挂到 tree 上。
- 需要 Runtime Debug 让 request 到 instance 到事实链路可见。

## 与当前实现的差距

- `ActionRuntime` 已存在，但未由 `CharacterPipeline` 持有和注册 profile。
- `CharacterPipelineDefinition` 未配置 ActionProfiles。
- `CharacterGraphContext` 未暴露 tracked action request service。
- 当前 `ActionStartRequest` 缺少 source request、target key、source graph identity。
- `ActionAuthoringContracts.TrackedActionNodeContract` 命名应移除或改为 request authoring contract。
- Timeline facts 还没有携带 ActionInstanceId。
- Runtime Debug 尚未实现。

