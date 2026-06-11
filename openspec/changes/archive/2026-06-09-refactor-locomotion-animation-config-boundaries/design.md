# Design: 基础移动动画配置边界与分层

## Context

当前基础移动链路已经拆成输入、移动意图、状态机、运动命令、动画上下文和 Animancer Presenter。问题不在状态机，而在动画配置边界：Animancer TransitionAsset 已经天然保存 clip、fade、speed、normalized start time 和事件，但项目侧 Run 配置又暴露了部分同名播放参数。继续这样做会让设计者不知道该改哪一处，也会让后续编辑器同时维护两套数据。

工业上更常见的拆法是：

```text
逻辑状态机
  决定当前 phase 和是否允许切换

项目侧动画语义配置
  决定 phase 使用哪个稳定 alias，以及逻辑退出时间

Animancer TransitionLibrary / TransitionAsset
  决定 alias 对应哪个 clip、fade、speed、起播点、动画事件

Presenter
  消费上下文并调用 Animancer，不做业务仲裁
```

## Goals

- 消除项目侧 Run 配置和 Animancer TransitionAsset 的播放参数重复。
- 保留 `Idle / MoveStart / MoveLoop / MoveStop` 逻辑状态。
- 当前仍只支持 Run-only 基础移动 alias：`Idle / RunStart / RunLoop / RunEnd`。
- `RunEnd` 退出时长以纯数据进入状态机，状态机不读取 Animancer。
- Presenter 不覆盖 Animancer TransitionAsset 的 fade、speed 和起播点。
- 文件夹分层能支撑后续打断规则、多层动画、IK 和编辑器，但不提前实现这些功能。

## Non-Goals

- 不建立完整 `CharacterActionStateDefinition`。
- 不建立通用 `InterruptPolicy`。
- 不建立动画 Timeline 窗口数据。
- 不建立全局角色动画 catalog。
- 不让编辑器成为运行时核心依赖。

## Decisions

### Decision: 项目侧 Run 配置只保留 alias 和逻辑退出时长

`RunLocomotionAnimationConfigSO` 只表达：

```text
idleAlias
runStartAlias
runLoopAlias
runEndAlias
runEndExitDuration
```

或者等价的轻量 entry 结构。entry 中不得继续保存 `fadeDuration / speed / normalizedStartTime`。

Reason: 这些播放参数已经在 Animancer TransitionAsset 中配置；项目侧重复配置会形成双权威。

### Decision: `RunEndExitDuration` 属于逻辑，不属于 Animancer

`RunEndExitDuration` 控制 `MoveStop -> Idle` 何时允许发生。它不等价于 clip length，也不等价于 Animancer fade。该数值进入状态机前必须变成纯 `float`，状态机只读数值，不关心 alias、clip 或 TransitionAsset。

### Decision: 轻量打断语义先体现在状态图优先级，不新增通用 InterruptPolicy

当前唯一明确打断是：

```text
MoveStop + HasMoveIntent -> MoveStart
```

它应继续通过 `LocomotionStateGraphTransitionConfig` 的优先级和条件表达，优先于：

```text
MoveStop + NoMoveIntent + MoveStopExitTimeReached -> Idle
```

后续攻击、闪避、受击等才需要通用 cancel window 和 interrupt policy。

### Decision: 文件夹分层先服务当前代码，不提前搬大目录

运行时代码继续放在：

```text
Assets/Scripts/Character/Animation/
  Model/
  Config/
  Runtime/
  Editor/
```

资产继续放在：

```text
Assets/Configs/3C/Locomotion/
Assets/Configs/3C/Animacer/Corin/
```

本变更只明确边界和必要迁移，不把当前所有动画系统重排成完整动作框架。

## Proposed Folder Shape

```text
Assets/Scripts/Character/Animation/
  Model/
    RunLocomotionAnimationEntry.cs
    RunLocomotionAnimationConfigValidationResult.cs
  Config/
    RunLocomotionAnimationConfigSO.cs
  Runtime/
    BasicLocomotionAnimancerPresenter.cs
  Editor/
    RunLocomotionAnimationConfigSOEditor.cs

Assets/Scripts/Character/Movement/
  Model/
    BasicMovementSettings.cs
    LocomotionStateGraphTransitionConfig.cs
  Config/
    BasicMovementConfigSO.cs
    LocomotionStateGraphConfigSO.cs
  Solver/
    BasicLocomotionStateMachine.cs
    LocomotionStateGraphConditionEvaluator.cs
  Runtime/
    PlayerLocomotionController.cs

Assets/Configs/3C/
  Locomotion/
    DefaultRunLocomotionAnimationConfig.asset
    DefaultLocomotionStateGraph.asset
  Movement/
    BasicMovementConfig.asset
  Animacer/Corin/
    Corin_TransitionLib.asset
    Pramater/
      Idle.asset
      RunStart.asset
      RunLoop.asset
      RunEnd.asset
    TransitionAsset/
      Corin_Idle.asset
      Corin_RunStart.asset
      Corin_RunLoop.asset
      Corin_RunEnd.asset

Assets/Tests/Editor/
  PlayerLocomotionControllerTests.cs
```

## Risks / Trade-offs

- Risk: 收缩字段会让旧序列化资产丢失 fade/speed/startTime 值。
  - Mitigation: 这些值应迁回 Animancer TransitionAsset；实现时测试和手动验证明确检查 Presenter 不覆盖它们。
- Risk: `RunEndExitDuration` 仍需手填，可能和 clip 长度不一致。
  - Mitigation: 当前先保留手填；后续如果需要自动读取 clip 长度，另起编辑器/导入器 proposal。
- Risk: 轻量 Editor 可能被误认为 Timeline 编辑器。
  - Mitigation: 本变更只做普通 Inspector/validator，不做轨道编辑或预览采样。

## Validation

- OpenSpec strict 校验。
- Unity EditMode 测试覆盖：
  - Run 配置只暴露 alias 和 RunEnd exit duration。
  - Presenter 调用 alias 播放，不覆盖 TransitionAsset fade/speed/startTime。
  - `MoveStop` 未到 exit duration 保持停止。
  - `MoveStop` 到 exit duration 回 `Idle`。
  - `MoveStop` 中有输入立即回 `MoveStart`。
  - 状态机不引用 Animancer。
  - Presenter 不引用状态图 builder 或运动执行端口。
- 手动验证：
  - 在当前演示场景改 Animancer TransitionAsset 的 fade/speed/startTime，播放表现跟随 Animancer 配置。
  - 改 RunEnd exit duration，逻辑回 Idle 时间变化。
