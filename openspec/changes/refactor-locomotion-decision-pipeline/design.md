# 统一 Locomotion 决策管线设计

## Context

现有运行时链路已经具备统一状态机、Animancer presenter、root motion source、motion executor 和诊断日志。问题在于 Locomotion 决策阶段没有被显式建模：`PlayerLocomotionController.TryEvaluateWithStateMachine` 同时负责输入解析、相机处理、run latch、phase facts、状态机 context、motion facts 和 pipeline 调用。

TurnBack 暴露了这个结构问题。它真正需要在状态机 tick 前，根据人物朝向和当前世界移动输入派生一个纯数据事实；但当前实现把判断挂在 transition evaluator 里即时算角度，导致被普通旋转、空输入帧和当前 phase 影响。

## Goals

- 把 Locomotion 每帧决策拆成明确阶段，但保持同一个 `PlayerLocomotionController` 主入口。
- 用统一帧事实承载移动派生信息，避免 TurnBack、Run、Stop、未来 Jump/Roll/Combo 各自读取 Transform/Input/Animator。
- 让统一状态机只读取 context 中的纯数据 facts。
- 让运动和动画继续保持外围 adapter，不拥有状态决策权。
- 让实现可以小步落地，首个竖切是 TurnBack 早期捕获。

## Non-Goals

- 不引入 ECS、任务图或新框架。
- 不拆出新的 MonoBehaviour 控制器。
- 不把所有 Action 系统一次性迁移到新模型。
- 不改变现有 simulation tick phase 顺序。

## Proposed Pipeline

```text
ReadInput
-> ResolveMovementIntent
-> ResolveSpatialFacts
-> DeriveLocomotionDecisionFacts
-> BuildStateMachineContext
-> StateMachineDecision
-> BuildMotionFactsAndCommand
-> ExecuteMotion
-> PresentAnimation
-> FeedbackFacts
```

## Stage Responsibilities

- `ReadInput`：只产生 `BasicLocomotionInputSnapshot` 或 replay 等价输入。
- `ResolveMovementIntent`：只处理 deadzone、normalized input、strength、run held/latch 和 gait candidate。
- `ResolveSpatialFacts`：只产生 camera basis、world move direction、facing forward 等空间事实。
- `DeriveLocomotionDecisionFacts`：只从前面事实派生 has move、phase facts、TurnBack intent 等纯数据事实。
- `BuildStateMachineContext`：把 Locomotion facts、Action input request facts、runtime blackboard facts 打包给统一状态机。
- `StateMachineDecision`：唯一决定逻辑状态和 phase。
- `BuildMotionFactsAndCommand`：根据状态机输出和 animation/root motion facts 生成 `MovementCommand`。
- `ExecuteMotion`：只消费 command，不选状态。
- `PresentAnimation`：只消费状态机/animation context，不选状态。
- `FeedbackFacts`：把 animation progress、root motion delta、can exit 等事实写回 runtime blackboard 或下一帧 facts。

## Data Model Direction

实现可以新增 `LocomotionDecisionFacts`、`LocomotionSpatialFacts`、`LocomotionDerivedIntentFacts` 或等价命名。首版不要求过度抽象，但事实对象必须是纯数据：

- 不引用 `Transform`、`Animator`、`AnimancerState`、`InputAction`、`CharacterController`。
- 可以包含 `MovementInputIntent`、world move direction、facing forward、gait、phase facts、TurnBack intent。
- 可以被 EditMode 测试直接构造。

## TurnBack As First Vertical Slice

TurnBack 的改法必须落在 `DeriveLocomotionDecisionFacts`：

```text
MovementInputIntent + WorldMoveDirection + FacingForward + CurrentStep
-> LocomotionDecisionFacts.TurnBackIntent
-> CharacterStateMachineContext
-> MoveTurnBackRequested
-> FullBody/Locomotion/TurnBack
```

TurnBackIntent 可以有短 step 窗口，但它不是按钮预输入请求，也不直接播放动画。状态机消费后仍进入现有 TurnBack root motion 链路。

## Relation To Future Attack Combo

未来攻击连招不应复制 Locomotion 派生逻辑。按钮输入先进入 `InputRequestBuffer`，再由 Action/Combo fact builder 生成纯数据 request facts，最后与 Locomotion facts 一起进入同一个 `CharacterStateMachineContext`。统一点是 context facts 和状态机决策，不是所有输入共用同一个 buffer。

当前竖切会把 Dodge request 的方向决策接到 `LocomotionDecisionFacts`：Dodge 按钮仍来自 `InputRequestBuffer`，但 directional dodge 使用 Locomotion 已解析出的 world move direction，backstep 使用 Locomotion 已解析出的人物 facing，而不是在 Action gate 中重新读取 raw input、camera basis 或 facing provider。

## Risks

- 风险：一次拆太大导致行为回退。
  - Mitigation: 首版只显式化现有顺序、接 TurnBack，并让 Dodge request 消费统一 Locomotion facts；Attack/Combo 仍只保留未来接入边界。
- 风险：事实对象变成新的大泥球。
  - Mitigation: 按阶段分事实，保持纯数据和测试构造能力。
- 风险：状态机 context 同时带旧字段和新 facts，短期重复。
  - Mitigation: 允许兼容构造器，但新增逻辑只能读取统一 facts；任务中加入静态检查。
