# Change: 收敛基础移动动画配置边界和目录分层

## Why

当前 `add-run-locomotion-animation-parameters` 把 alias、fade、speed、normalized start time 和 RunEnd 退出时长都放进项目侧 Run 配置，导致 Animancer TransitionAsset 和项目配置同时能改播放参数，形成双权威。基础移动动画第一阶段需要先把职责边界收窄：Animancer 管播放参数，项目侧只管逻辑 phase 到 alias 的约定和 `MoveStop -> Idle` 的纯数据退出时间。

同时，后续会继续扩展打断规则、多层动画、IK、预测回滚和编辑器，因此需要先明确文件夹分层，避免动画数据、状态机逻辑、Presenter 和编辑器工具混在一起。

## What Changes

- 将基础移动动画配置从“播放参数配置”收敛为“Run-only phase alias 配置 + RunEnd 逻辑退出时长”。
- `fade duration / speed / normalized start time / clip / transition event` 继续由 Animancer TransitionAsset 或 TransitionLibrary 管理。
- `BasicLocomotionAnimancerPresenter` 只根据 `MovementAnimationContext.Phase` 解析 alias 并请求 Animancer 播放，不再覆盖 Animancer TransitionAsset 的播放参数。
- `RunEnd` 的 `MoveStop -> Idle` 退出时长保留为项目侧纯数据，因为它属于逻辑状态机判定，不属于 Animancer 播放参数。
- `MoveStop` 期间重新出现移动输入仍必须立即进入 `MoveStart`，优先级高于等待 `RunEnd` 退出时长。
- 明确基础移动动画相关文件夹编排：运行时代码、纯模型、配置资产、编辑器工具和测试分层。
- 当前只规划轻量 Inspector/validator，不做 Timeline 编辑器或完整动作编辑器。

## Non-Goals

- 不实现 Walk/Run gait 选择；Shift 进入 Run 或 Walk 的规则另起 proposal。
- 不实现攻击、闪避、受击、死亡等通用动作状态。
- 不实现通用 interrupt/cancel window 数据。
- 不实现 FullBody / UpperBody / LowerBody 多层动画。
- 不实现 IK、预测回滚、网络动画快照。
- 不做 Timeline 编辑器。
- 不新增第二套角色控制器、第二套移动入口或绕过 `PlayerLocomotionController` 的播放路径。

## Impact

- Affected specs:
  - `basic-locomotion-animation`
  - `unityhfsm-locomotion`
  - `project-structure`
- Affected code/assets:
  - `Assets/Scripts/Character/Animation/Model/RunLocomotionAnimationEntry.cs`
  - `Assets/Scripts/Character/Animation/Config/RunLocomotionAnimationConfigSO.cs`
  - `Assets/Scripts/Character/Animation/Runtime/BasicLocomotionAnimancerPresenter.cs`
  - `Assets/Scripts/Character/Movement/Runtime/PlayerLocomotionController.cs`
  - `Assets/Configs/3C/Locomotion/DefaultRunLocomotionAnimationConfig.asset`
  - `Assets/Configs/3C/Animacer/Corin/*`
  - `Assets/Tests/Editor/PlayerLocomotionControllerTests.cs`

## Relationship To Active Changes

- 本变更修正 `add-run-locomotion-animation-parameters` 中“项目侧重复配置 Animancer 播放参数”的方向。
- 如果 `add-run-locomotion-animation-parameters` 已经 apply 但尚未 archive，实现本变更时应在同一条现有基础移动链路上收缩字段，不新增并行配置资产。
- 旧 `update-locomotion-animation-parameters` 已废弃，不作为实现来源。

## Open Questions

- 第一版轻量 Inspector 是否只做校验提示，还是也需要 alias 下拉？本 proposal 默认先做 validator 和普通 Inspector 可读性，不做强制下拉。
