# Change: Walk/Run 基础移动档位接入

## Why

当前基础移动逻辑已经稳定为 `Idle / MoveStart / MoveLoop / MoveStop` 四阶段，但实际操作语义需要区分普通移动和按住 Shift 加速移动：普通移动应表现为 Walk，按住 Shift 时表现为 Run。现有 Run-only 方案会把后续 Walk 接入继续推迟，容易形成一条只服务 Run 的临时路径。

## What Changes

- 新增基础移动档位概念：普通移动为 `Walk`，按住 Run 输入（默认 Shift 对应的输入动作）为 `Run`。
- 保持 UnityHFSM 逻辑阶段仍只有 `Idle / MoveStart / MoveLoop / MoveStop`，不新增 `WalkStart / RunStart` 等逻辑状态。
- 输入快照或等价输入事实增加 Run 保持意图，具体 Unity Input System 读取仍限制在输入 adapter 内。
- 移动 intent、命令和动画上下文携带当前档位；停止阶段使用最后一个有效移动档位选择停止动画和停止退出事实。
- 将 Run-only 动画配置升级为 Walk/Run 基础移动动画配置，按 `phase + gait` 解析 alias、退出策略和 motion profile。
- 支持 `WalkStart / WalkLoop / WalkEnd` 与 `RunStart / RunLoop / RunEnd` 的配置和验证。
- 保持 `MoveStop` 中重新输入立即进入 `MoveStart`，不等待当前 WalkEnd 或 RunEnd。
- 明确本 change 替代未实施的 `add-run-locomotion-animation-parameters` Run-only 路线；实施前应停止并废弃或合并该旧 change，避免双配置路径并存。

## Non-Goals

- 不实现 Sprint；Sprint 是否属于能力状态或外层 FullBody 状态另起 OpenSpec。
- 不实现 Dodge、Attack、HitReact、Death、Vault、Jump/Fall/Land。
- 不实现 FullBody / UpperBody / LowerBody 分层状态机。
- 不新增第二套角色控制器、第二条移动入口或 BBB 运行时依赖。
- 不启用完整 Animator root motion 作为基础移动权威。
- 不修改网络协议、预测回滚或输入历史格式。

## Impact

- Affected specs:
  - `unityhfsm-locomotion`
  - `basic-locomotion-animation`
- Related active changes:
  - 替代/合并：`add-run-locomotion-animation-parameters`
  - 依赖当前状态事实：`add-animation-phase-timeline-facts`
  - 依赖当前 motion facts：`add-locomotion-motion-profile-facts`
- Affected code:
  - `Assets/Scripts/Character/Movement/Model/BasicLocomotionInputSnapshot.cs`
  - `Assets/Scripts/Character/Movement/Model/MovementInputIntent.cs`
  - `Assets/Scripts/Character/Movement/Model/BasicMovementSettings.cs`
  - `Assets/Scripts/Character/Movement/Model/MovementCommand.cs`
  - `Assets/Scripts/Character/Movement/Solver/MovementCommandBuilder.cs`
  - `Assets/Scripts/Character/Movement/Runtime/UnityInputSystemLocomotionInputSource.cs`
  - `Assets/Scripts/Character/Movement/Runtime/PlayerLocomotionController.cs`
  - `Assets/Scripts/Character/Animation/Model/MovementAnimationContext.cs`
  - `Assets/Scripts/Character/Animation/Config/*`
  - `Assets/Scripts/Character/Animation/Runtime/BasicLocomotionAnimancerPresenter.cs`
  - `Assets/Tests/Editor/PlayerLocomotionControllerTests.cs`

## Open Questions

- 默认 Run 输入动作名称是否使用 `Run`，并在项目 InputActionAsset 中绑定 Left Shift？当前提案按 `Run` 作为默认 action name 规划，实施时必须验证资产实际动作名。
