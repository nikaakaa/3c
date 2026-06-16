# Change: 新增 Locomotion 脚相位匹配

## Why
当前 `TurnBack -> RunLoop` 的衔接只知道动画播放进度，不知道退出帧是哪只脚处于支撑相位。`RunLoop` 进入时也没有消费脚相位事实来选择起播 normalized time，导致 TurnBack 后左右脚混合偶发不顺。

## What Changes
- 新增 `locomotion-foot-phase-matching` 能力，用纯数据表达 locomotion clip 的脚相位 marker、采样结果和入场匹配结果。
- 为基础移动动画配置增加脚相位 profile 绑定，使 `Locomotion.Turn.Back` 和 `RunLoop` 可以通过正式配置声明脚相位 marker。
- 扩展动画 timeline facts，使播放进度可以采样出当前脚相位和退出脚相位。
- 扩展角色运行时黑板，使脚相位事实可 snapshot/restore，并由动画 facts adapter 写入。
- 扩展移动动画上下文和 Animancer Presenter，使 `MoveLoop + Run` 新进入时可按匹配结果设置一次起播 normalized time。
- 第一版只覆盖 `FullBody/Locomotion/TurnBack -> FullBody/Locomotion/MoveLoop + RunLoop`，不引入 IK、Motion Matching、左右脚动画变体或新控制器。

## Impact
- Affected specs: `locomotion-foot-phase-matching`, `basic-locomotion-animation`, `animation-phase-timeline-facts`, `character-runtime-blackboard`
- Affected code: 
  - `Assets/Scripts/Character/Animation/Model/MovementAnimationContext.cs`
  - `Assets/Scripts/Character/Animation/Runtime/BasicLocomotionAnimancerPresenter.cs`
  - `Assets/Scripts/Character/Animation/Config/RunLocomotionAnimationConfigSO.cs`
  - `Assets/Scripts/Character/StateMachine/Model/CharacterRuntimeBlackboard.cs`
  - `Assets/Scripts/Character/Movement/Runtime/PlayerLocomotionController.cs`
  - `Assets/Configs/3C/Animation/Locomotion/Corin/DefaultRunLocomotionAnimationConfig.asset`
- Non-goals:
  - 不改变 TurnBack 进入规则。
  - 不改变 TurnBack motion source、EntryLocal 或 motion executor 权威。
  - 不新增 Animator Controller、IK 修脚路径、独立 TurnBack 控制器或未审批 fallback 配置。
  - 不删除现有诊断 log。

