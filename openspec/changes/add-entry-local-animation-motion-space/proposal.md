# Change: 增加 EntryLocal 动画运动坐标空间

## Why

当前 TurnBack 已经改为 `TickSampledMotion` profile 权威路线，但 profile 的平面位移是以动画/状态进入时的固定初始基准烘焙出来的累计本地曲线。运行时仍把该 delta 标记为 `Local` 并按角色当前 root 朝向解释，角色一边应用 yaw 一边解释 translation 时，会把位移基准不断旋转，导致 180 度转身动画出现位移方向反转或路径扭曲。

后续预测、回滚、预测矫正和攻击 root-motion profile 也需要同一个可复现语义：采样出来的动画位移必须声明坐标空间，并且需要能选择“相对动作进入瞬间的固定基准”，而不是依赖表现层或当前 root 旋转的隐式解释。

## What Changes

- 新增 `EntryLocal`/固定进入基准平面 delta 语义：profile local X/Z 按状态进入时捕获的平面 forward/right 转换到世界。
- 保留现有 `Local` 和 `World` 语义：普通 RunEnd 等已使用路径不被强制改语义。
- TurnBack 的 baked profile translation 默认使用 `EntryLocal`，yaw 仍由 sampled profile 单独累加应用。
- 状态机或运动事实必须保存并传递进入基准，使预测/回滚 restore 后重放得到同一条世界路径。
- 诊断日志必须能看到 delta space、entry basis、sampled local delta 和 resolved world delta。
- 不新增 TurnBack 专用 executor、第二套 movement path 或 Animator runtime root-motion fallback。

## Non-Goals

- 不重新设计 `TickSampledMotion` 和 `AnimatorRuntimeDirect` 的模式选择。
- 不让 `OnAnimatorMove` pending delta 重新参与 TurnBack 权威位移。
- 不改变 profile 烘焙数据结构的“累计 local 曲线差分采样”基本模型。
- 不在本变更里迁移所有 Dodge/Attack；只把能力做成后续可复用。
- 不重做 TurnBack timeline/transition window，但实现时必须验证 motion/exit window 没有掩盖位移结果。
- 不运行 Unity batchmode。

## Impact

- Affected specs:
  - `animation-motion-source-pipeline`
  - `basic-locomotion-animation`
  - `simulation-tick-locomotion`
  - `unified-character-state-machine`
- Affected code:
  - `Assets/Scripts/Character/Movement/Model/BasicMovementMotionFacts.cs`
  - `Assets/Scripts/Character/Movement/Model/MovementCommand.cs`
  - `Assets/Scripts/Character/Movement/Runtime/CharacterControllerBasicMotionExecutor.cs`
  - `Assets/Scripts/Character/Movement/Runtime/PlayerLocomotionController.cs`
  - `Assets/Scripts/Character/StateMachine/Model/CharacterStateMachineRuntimeTypes.cs`
  - `Assets/Scripts/Character/StateMachine/Solver/CharacterStateMachineRunner.cs`
  - `Assets/Scripts/Character/Animation/Solver/AnimationMotionProfileSampler.cs`
  - `Assets/Tests/Editor/UnifiedCharacterStateMachineTests.cs`
  - `Assets/Tests/Editor/Simulation`
- Related active changes:
  - Depends on `add-animation-motion-source-pipeline` using `TickSampledMotion` as TurnBack authority.
  - Must remain compatible with `refactor-fullbody-frame-pipeline` and `refactor-rollback-layering-contract`.
