# Change: 新增动作打断仲裁模块

## Why

当前基础移动的 `MoveStop -> MoveStart` 打断已经由 Locomotion 状态图优先级表达，但后续攻击、闪避、受击、死亡、全身接管、上半身动作和预测回滚需要一套独立的纯逻辑仲裁层。继续把“能不能打断”写在动画 Presenter、Animancer TransitionAsset 或多个状态类互跳里，会让业务优先级、取消窗口和网络同步边界变得难以验证。

本变更规划一个最小动作打断仲裁模块，先只做纯数据请求、策略和裁决结果，不接入 Animancer，不新增攻击状态，不改变当前 Run-only Locomotion 四阶段流转。

## What Changes

- 新增 `action-interrupt-arbiter` 能力，用纯 C# 数据表达动作打断请求、当前状态事实、打断策略和裁决结果。
- 引入稳定状态 ID、请求类型、优先级、抗性、时序规则和窗口规则，避免直接引用 Unity Object、AnimationClip、Animancer state 或场景实例。
- 仲裁器从同一帧候选请求中选择一个可接受请求，并给出明确拒绝原因。
- 支持第一版时序规则：`Always`、`AfterElapsedTime`、`DuringElapsedTimeWindow`。
- 支持第一版 priority/resistance：请求优先级必须满足当前状态或策略要求，低优先级请求不得打断高抗性状态。
- 明确当前 `MoveStop/RunEnd` 仍由 `LocomotionStateGraphTransitionConfig` 处理，不迁入本仲裁模块。
- 为后续 Action 状态机、输入缓冲、上半身/下半身分层、预测回滚和编辑器预留纯数据边界。

## Non-Goals

- 不播放动画，不调用 Animancer，不读取 Animancer 当前播放进度。
- 不新增攻击、闪避、受击、死亡或 OverrideState 运行时状态。
- 不修改 `BasicLocomotionStateMachine` 的 `Idle / MoveStart / MoveLoop / MoveStop` 流转。
- 不把 `MoveStop -> MoveStart` 或 `MoveStop -> Idle` 从 Locomotion 状态图迁到仲裁器。
- 不接 Input System，不消费输入缓冲，不发网络消息。
- 不实现 FullBody / UpperBody / LowerBody 动画层。
- 不实现 cancel window Timeline 编辑器。
- 不复制 BBB 的 `ActionArbiter`、`OverrideState`、`BBBCharacterController` 或 `PlayerRuntimeData`。

## Impact

- Affected specs:
  - `action-interrupt-arbiter`
- Affected code planned:
  - `Assets/Scripts/Character/Action/Model/ActionStateId.cs`
  - `Assets/Scripts/Character/Action/Model/ActionRequestType.cs`
  - `Assets/Scripts/Character/Action/Model/ActionInterruptRequest.cs`
  - `Assets/Scripts/Character/Action/Model/ActionInterruptTimingRule.cs`
  - `Assets/Scripts/Character/Action/Model/ActionInterruptPolicy.cs`
  - `Assets/Scripts/Character/Action/Model/ActionInterruptContext.cs`
  - `Assets/Scripts/Character/Action/Model/ActionInterruptDecision.cs`
  - `Assets/Scripts/Character/Action/Solver/ActionInterruptArbiter.cs`
  - `Assets/Scripts/Character/Action/Solver/ActionInterruptPolicyValidator.cs`
  - `Assets/Tests/Editor/ActionInterruptArbiterTests.cs`
- Reference only:
  - `Ref/BBB-Nexus/Character/Arbitration/Arbiters/ActionArbiter.cs`
  - `Ref/BBB-Nexus/Character/States/Core/GlobalInterruptProcessor.cs`
  - `Ref/BBB-Nexus/Character/States/Override/OverrideState.cs`

## Relationship To Active Changes

- `add-locomotion-animation-phase-exit-policy` 继续负责基础移动 phase 的 `alias / exitPolicy / exitDuration`，本变更不修改该范围。
- `refactor-locomotion-animation-config-boundaries` 已明确 Presenter 不做业务仲裁，本变更把业务打断裁决放入独立纯逻辑模块。
- `simulation-tick-system` 已预留 GameplayDecision phase；本变更的仲裁器未来可以在 GameplayDecision phase 中被调用，但第一版不强制接入 tick runner。
- `local-preinput-buffer` 仍只保存输入事实和请求；本变更不让输入缓冲直接决定动作结果。

## Assumptions

- 第一版使用字符串或轻量值对象作为稳定状态 ID；如果后续需要生成器或 catalog，可另起 proposal。
- 第一版 elapsed time 使用纯 `float` 秒表达；接入预测回滚时再补 tick/fixed-point 映射。
- 第一版只做单层动作仲裁；多层 layer conflict 另起 proposal。
