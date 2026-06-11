# Change: 为基础移动动画阶段增加退出策略配置

## Why

当前基础移动动画配置已经收敛到 Run-only alias，但 `runEndExitDuration` 仍是顶层特例字段，`MoveStart` 的等待时间仍来自移动数值配置。继续这样扩展会让“动画阶段应该等多久”和“移动速度参数”混在两处，也会让后续 RunEnd、Start、转身、落地等阶段时间越来越难配置。

本变更把基础移动四阶段统一成 phase config：每个阶段配置自己的 alias、退出策略和退出时长。这样 `RunEnd` 不再是特殊字段，而是 `MoveStop` 阶段的一条普通配置。

## What Changes

- 引入基础移动动画阶段配置结构，表达 `aliasKey / exitPolicy / exitDuration`。
- `RunLocomotionAnimationConfigSO` 继续只服务当前 Run-only 基础移动，但内部改为 `Idle / MoveStart / MoveLoop / MoveStop` 四个 phase config。
- 移除 `runEndExitDuration` 顶层特例字段，用 `MoveStop.exitPolicy = AfterDuration` 和 `MoveStop.exitDuration` 表达停止动画等待时间。
- 将 `MoveStart` 的进入循环等待时间也纳入 phase timing，使起步和停止都走同一种数据结构。
- 状态机仍只读取纯数值和阶段，不读取 Animancer、AnimationClip、TransitionAsset 或 alias。
- Presenter 仍只读取当前 phase 对应 alias 并请求 Animancer 播放，不参与退出策略和状态切换。
- 默认配置仍只包含 `Idle / RunStart / RunLoop / RunEnd`，不引入 Walk。
- 保留 `MoveStop + 有输入 -> MoveStart` 的立即打断优先级，高于 `MoveStop.exitDuration`。

## Non-Goals

- 不实现 Walk/Run gait 选择；Shift 进入 Run 另起 proposal。
- 不新增攻击、闪避、受击、死亡等动作状态。
- 不新增通用 `InterruptPolicy`、cancel window 或 combo window。
- 不实现 FullBody / UpperBody / LowerBody 分层。
- 不实现 IK、预测回滚、网络动画快照。
- 不读取 Animancer clip length 自动计算退出时长。
- 不做 Timeline 编辑器或动作编辑器。
- 不新增并行角色控制器、并行移动入口或绕过 `PlayerLocomotionController` 的播放路径。

## Impact

- Affected specs:
  - `basic-locomotion-animation`
  - `unityhfsm-locomotion`
- Affected code/assets:
  - `Assets/Scripts/Character/Animation/Model/RunLocomotionAnimationEntry.cs`
  - `Assets/Scripts/Character/Animation/Model/LocomotionAnimationPhaseConfig.cs`
  - `Assets/Scripts/Character/Animation/Model/LocomotionAnimationExitPolicy.cs`
  - `Assets/Scripts/Character/Animation/Config/RunLocomotionAnimationConfigSO.cs`
  - `Assets/Scripts/Character/Movement/Model/BasicMovementSettings.cs`
  - `Assets/Scripts/Character/Movement/Model/BasicMovementPhaseTiming.cs`
  - `Assets/Scripts/Character/Movement/Model/LocomotionStateGraphCondition.cs`
  - `Assets/Scripts/Character/Movement/Solver/LocomotionStateGraphConditionEvaluator.cs`
  - `Assets/Scripts/Character/Movement/Runtime/PlayerLocomotionController.cs`
  - `Assets/Scripts/Character/Animation/Runtime/BasicLocomotionAnimancerPresenter.cs`
  - `Assets/Configs/3C/Locomotion/DefaultRunLocomotionAnimationConfig.asset`
  - `Assets/Tests/Editor/PlayerLocomotionControllerTests.cs`

## Relationship To Active Changes

- 本变更建立在 `refactor-locomotion-animation-config-boundaries` 的方向上：Animancer 继续管理 clip、fade、speed、normalized start time 和事件；项目侧只管理 phase 语义和逻辑退出时间。
- 如果 `refactor-locomotion-animation-config-boundaries` 尚未 archive，实施时 MUST 在同一条现有基础移动链路上继续演进，不新增第二套 Run 配置资产或 Presenter。
- `add-run-locomotion-animation-parameters` 中旧的 fade/speed/startTime 项目侧配置方向不得恢复。

## Assumptions

- 当前只做 Run-only 基础移动，逻辑状态仍只有 `Idle / MoveStart / MoveLoop / MoveStop`。
- 第一版退出时长由设计者手填；如果要“播完整个 RunEnd”，就把 `MoveStop.exitDuration` 填成 RunEnd clip 的有效时长。
- 后续可以通过轻量 editor 从 Animancer TransitionAsset 读取 clip length 并同步到 `exitDuration`，但该能力不在本变更内。
