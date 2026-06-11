# Design: 动作打断仲裁模块

## Context

当前基础移动链路已经把状态机、运动执行和 Animancer Presenter 分开。`MoveStop` 中重新输入立即进入 `MoveStart` 已经由 Locomotion 状态图的 transition priority 表达，这属于局部状态流转，不需要额外仲裁器。

真正需要独立仲裁的是后续动作系统：攻击、闪避、翻滚、受击、死亡、全身接管、上半身动作、输入缓冲和网络预测都需要在同一帧内根据请求优先级、当前状态抗性和时间窗口决定“能不能切”。这个决定属于逻辑层，动画层只能负责播放、混合、事件和调试显示。

BBB 的参考价值在于：

- `ActionArbiter` 使用 request priority 和 current resistance 做动作接管判断。
- `GlobalInterruptProcessor` 把高优先级拦截放在状态自身逻辑之前。
- `OverrideState` 把全身动作接管和动画播放封装起来，并在结束后返回原状态。

本项目不直接复用 BBB 代码，因为 BBB 运行时强绑定 `BBBCharacterController`、`PlayerRuntimeData`、`StateRegistry`、具体状态类型、`AnimationClip` 和 `AnimFacade`。本变更只吸收 priority/resistance/interceptor 思路，重写成可测试、可同步、可回滚的纯数据仲裁模块。

## Goals

- 提供不依赖 Unity 场景对象的动作打断仲裁核心。
- 使用稳定 ID 和纯数据表达请求、当前状态、策略和裁决结果。
- 支持 priority/resistance 规则。
- 支持 `Always`、`AfterElapsedTime`、`DuringElapsedTimeWindow` 三类基础时间规则。
- 同一帧多请求时输出确定性结果。
- 拒绝原因可诊断，便于后续编辑器和日志显示。
- 保持当前 Locomotion 四阶段和 Presenter 边界不变。

## Non-Goals

- 不实现动作状态机。
- 不接 Animancer、Animator、AnimationClip 或 TransitionAsset。
- 不接输入缓冲消费。
- 不接网络协议、预测或回滚。
- 不实现多动画层冲突。
- 不实现编辑器窗口。

## Proposed Model

```text
ActionStateId
  稳定状态 ID，例如 Locomotion.MoveStop、Action.Attack01、Action.Dodge、Action.Death

ActionRequestType
  输入或事实类型，例如 Attack、Dodge、HitReact、Death、Locomotion

ActionInterruptRequest
  RequestId
  RequestType
  TargetState
  Priority
  SourceTick/Sequence
  ExpiresAtTick 或 ExpiresAfterSeconds

ActionInterruptContext
  CurrentState
  CurrentStateTags
  CurrentStateElapsedSeconds
  CurrentStateResistance
  CurrentTick

ActionInterruptPolicy
  FromState 或 FromTag
  ToState 或 ToTag
  MinPriority
  TimingRule
  WindowStart
  WindowEnd
  Force

ActionInterruptDecision
  Accepted
  SelectedRequest
  TargetState
  RejectReason
```

## Decisions

### Decision: 仲裁器只输出 decision，不直接切状态

`ActionInterruptArbiter` 的输出是 `ActionInterruptDecision`。它不持有状态机，不调用 `ChangeState`，不播放动画，不写运行时组件。

Reason: 这样可以被 EditMode 测试、服务器模拟、预测回滚和未来工具复用，也避免出现绕过当前 `PlayerLocomotionController` 或状态图的新路径。

### Decision: 第一版使用 priority + resistance

第一版规则：

```text
request.Priority >= policy.MinPriority
request.Priority > context.CurrentStateResistance
```

如果策略标记为 `Force`，可绕过 resistance，但仍必须有显式 policy。

Reason: 这借鉴 BBB 的 ActionArbiter 思路，但避免把抗性写死在具体状态类型判断里。

### Decision: 时间窗口基于逻辑状态 elapsed time

第一版只读取 `CurrentStateElapsedSeconds`。动画 normalized time、clip length、Animancer event 不进入仲裁器。

Reason: 当前系统还没有统一动作 timeline 和预测回滚快照。先用逻辑 elapsed time 建立纯逻辑闭环，后续 timeline evaluator 可以把动画窗口采样成纯事实再喂给仲裁器。

### Decision: 当前 Locomotion 状态图不迁入仲裁器

`MoveStop + HasMoveIntent -> MoveStart` 和 `MoveStop + NoMoveIntent + PhaseExitTimeReached -> Idle` 继续由 `LocomotionStateGraphTransitionConfig` 表达。

Reason: 这是基础移动内部的局部流转，当前已经有明确状态图优先级。如果同时放进仲裁器，会形成两套切换路径。

### Decision: 第一版不做 ScriptableObject 配置资产

第一版可先用纯结构和构造器建立规则，测试通过后再做 SO 配置和 Inspector。

Reason: 先验证裁决语义和边界，避免编辑器提前固化错误模型。

## Proposed Folder Shape

```text
Assets/Scripts/Character/Action/
  Model/
    ActionStateId.cs
    ActionRequestType.cs
    ActionInterruptRequest.cs
    ActionInterruptTimingRule.cs
    ActionInterruptPolicy.cs
    ActionInterruptContext.cs
    ActionInterruptDecision.cs
    ActionInterruptRejectReason.cs
  Solver/
    ActionInterruptArbiter.cs
    ActionInterruptPolicyValidator.cs

Assets/Tests/Editor/
  ActionInterruptArbiterTests.cs
```

## Risks / Trade-offs

- Risk: 过早抽象成完整动作状态机。
  - Mitigation: 第一版只做仲裁决策，不新增状态机、不接动画、不接输入。
- Risk: `float` elapsed time 后续和 tick 回滚不一致。
  - Mitigation: 当前只做本地逻辑验证；预测回滚接入时新增 tick/fixed-point proposal。
- Risk: 规则表达不够覆盖复杂动作。
  - Mitigation: 第一版只承诺 `Always / AfterElapsedTime / DuringElapsedTimeWindow`，攻击 timeline 和 combo window 后续扩展。
- Risk: 与 Locomotion 状态图职责重叠。
  - Mitigation: spec 明确当前基础移动四阶段不迁入仲裁器。

## Validation

- OpenSpec strict 校验。
- Unity EditMode 定向测试覆盖：
  - 无请求返回 rejected。
  - 无匹配 policy 返回 rejected。
  - `Always` policy 接受请求。
  - `AfterElapsedTime` 未到时间拒绝，到时间接受。
  - `DuringElapsedTimeWindow` 窗口外拒绝，窗口内接受。
  - 多请求选择最高优先级可接受请求。
  - 同优先级按稳定顺序选择。
  - 请求优先级低于 current resistance 被拒绝。
  - `Force` policy 可绕过 resistance。
  - 仲裁核心静态验证不引用 Animancer、AnimationClip、Animator、CharacterController、Cinemachine、Input System、`BBBNexus`。

## Future Extensions

- `ActionStateDefinitionSO` 和 `ActionInterruptPolicySO`。
- Timeline window evaluator 将 cancel/combo/hitbox window 采样为纯事实。
- FullBody / UpperBody / LowerBody 多层仲裁。
- 输入缓冲消费窗口。
- 预测回滚快照和事件去重。
- 轻量 Inspector 和后续 Timeline 编辑器。
