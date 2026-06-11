# Change: 增加动画阶段 Timeline Fact 采样

## Why
当前基础移动的 `MoveStart / MoveStop` 退出已经从硬编码时长收敛到 phase config，但 `RunEnd` 想表达“动画真正播完再回 Idle”时，仍只能手填 `exitDuration`。这会让动画长度、Animancer 播放资产和逻辑退出数据产生重复维护。

下一步需要先建立最小动画事实层：动画播放层只提供当前播放进度，纯数据 sampler 根据 phase config 产出 `CanExit`，Locomotion 状态图只读取 `CanExit` 决定是否切换。这样以后 Timeline 编辑器、marker、cancel window、IK window 和预测回滚都能沿同一条数据路径扩展。

## What Changes
- 新增最小动画阶段 Timeline Fact 能力，先只覆盖基础移动 phase 的 `CanExit`。
- 扩展基础移动动画退出策略，支持 `OnAnimationEnd`，让 `MoveStop / RunEnd` 可以按动画结束事实退出。
- 增加纯数据播放进度快照和 sampler，输入 phase、phaseTime、normalizedTime、isEnded，输出 `CanExit`。
- 将 Locomotion 状态图退出条件从“只问 phase time”升级为“读取 phase can exit 事实”，同时保留 `AfterDuration` 兼容路径。
- 保持 Animancer Presenter 只负责播放 alias 和暴露只读进度，不注册 `OnEnd` 驱动状态切换。
- 规划未来 Timeline 编辑器写入的数据边界，但本变更不实现可视化编辑器、不实现攻击窗口、不实现 IK 曲线。

## Impact
- Affected specs: `animation-phase-timeline-facts`、`basic-locomotion-animation`、`unityhfsm-locomotion`
- Affected code:
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Animation/Model/`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Animation/Config/`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Animation/Runtime/`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Movement/Model/`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Movement/Solver/`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Movement/Runtime/`
  - `3cDemo/Client/3C_Client/Assets/Tests/Editor/PlayerLocomotionControllerTests.cs`
- Dependencies:
  - 建议先完成并验收 `add-locomotion-animation-phase-exit-policy`，因为本变更建立在 `LocomotionAnimationPhaseConfig` 和 per-phase exit policy 之上。
- Non-goals:
  - 不新增完整 Timeline 编辑器。
  - 不新增 `Walk` 或 `Run` 逻辑状态。
  - 不新增第二套角色控制器、状态机或动画映射表。
  - 不让 Animancer `OnEnd` 直接切 Locomotion 逻辑状态。
  - 不实现攻击 cancel window、hitbox window、IK window、VFX/SFX timeline 或预测回滚。
