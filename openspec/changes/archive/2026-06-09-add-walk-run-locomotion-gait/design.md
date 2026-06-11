# Design: Walk/Run 基础移动档位接入

## Context

现有 locomotion 逻辑阶段已经由 UnityHFSM 管理，并通过 `PhaseCanExit` 与动画播放进度解耦。当前运行时只有 Run 配置和 `RunStart / RunLoop / RunEnd` alias。用户明确需要普通移动为 Walk，按住 Shift 为 Run，同时不希望把 Sprint 简单塞进同一档位方案。

本设计将 Walk/Run 定义为基础移动内部的档位事实，而不是逻辑状态。Sprint、Dodge、Attack 等会改变输入规则、打断规则或资源消耗的行为不进入本 change。

## Goals

- 普通移动默认使用 Walk 档位。
- 按住 Run 输入时使用 Run 档位。
- UnityHFSM 仍只输出 `Idle / MoveStart / MoveLoop / MoveStop`。
- 状态机不引用 Walk/Run 动画资源、Animancer、Input System 或具体运动实现。
- 动画配置以 `phase + gait` 解析 alias、退出策略和 motion profile。
- `MoveStop` 使用最后移动档位选择 WalkEnd 或 RunEnd。
- 中途按下或松开 Run 输入时，在 `MoveLoop` 中允许 WalkLoop 与 RunLoop 表现切换。

## Non-Goals

- 不把 Sprint 建模为 gait。
- 不设计外层 FullBody 状态机。
- 不接 ActionInterruptArbiter 到 locomotion 主链。
- 不迁移到 BBB 的状态类互跳架构。
- 不新增 root motion 主权威路径。

## Decisions

### Decision: Walk/Run 是基础移动档位，不是 phase

`MoveStart / MoveLoop / MoveStop` 表达移动过程阶段，Walk/Run 表达该阶段的移动档位。这样状态机不会因 Walk/Run 组合膨胀成 `WalkStart / RunStart / WalkLoop / RunLoop` 等逻辑状态。

### Decision: Run 输入只影响档位选择

输入 adapter 读取 move、look、runHeld 或等价事实；controller 和 intent 使用该事实选择 `Walk` 或 `Run`。`BasicLocomotionStateMachine` 仍只读取 `hasMoveIntent`、delta、settings 和 phase facts。

### Decision: MoveStop 使用 last moving gait

松开移动输入后当前帧没有 move intent，不能再从输入推导停止动画。controller 或 intent 需要保存最后一个有效移动档位，用于 `MoveStop + Walk -> WalkEnd` 或 `MoveStop + Run -> RunEnd`。

### Decision: 动画配置升级为 Walk/Run 配置

现有 `RunLocomotionAnimationConfigSO` 的职责会升级或替换为基础移动动画配置。配置必须能按 `BasicMovementPhase + BasicMovementGait` 解析 phase config、alias key、motion profile 和退出策略。旧 Run-only 配置不应与新 Walk/Run 配置并行作为两套权威。

### Decision: MoveLoop 中切换档位不切 phase

第一版在 `MoveLoop` 中按下或松开 Run 输入时，只切换动画 alias 和速度，不强制回到 `MoveStart`。这样保持实现最小，并避免 Walk/Run 之间产生额外状态迁移规则。若后续需要 Walk->Run 起跑动画或 Run->Walk 过渡动画，应另起 proposal。

## Risks / Trade-offs

- 直接在 MoveLoop 中切 WalkLoop/RunLoop 可能不如专门过渡动画自然；当前优先保证架构边界和最小可玩闭环。
- 旧 `add-run-locomotion-animation-parameters` 与本 proposal 范围重叠；实施前必须停止旧 Run-only 路线，避免 `RunLocomotionAnimationConfigSO` 和新 Walk/Run 配置同时成为权威。
- InputActionAsset 中可能没有 `Run` 动作；实施任务需要先验证或补充资产绑定，不能在 controller 中硬编码键盘 Shift。

## Migration Plan

1. 在实现前确认 `add-run-locomotion-animation-parameters` 不再单独实施，并把其需求合并到本 change。
2. 引入 `BasicMovementGait` 或等价纯数据类型。
3. 扩展输入快照和输入 adapter，保持 `PlayerLocomotionController` 不直接引用 Input System。
4. 扩展 intent、settings、command 和 animation context 以携带当前档位。
5. 将 Run-only 动画配置升级为 Walk/Run 配置，并迁移现有 Run asset 引用。
6. 为 Walk 创建或绑定基础 alias、退出策略和必要 motion profile。
7. 更新 prefab 引用到新的单一 Walk/Run 配置。

## Validation

- OpenSpec strict 校验。
- Unity EditMode 定向测试覆盖普通移动 Walk、Shift Run、MoveLoop 档位切换、停止使用 last moving gait、状态机不新增 Walk/Run phase。
- 静态边界验证覆盖状态机不引用 Animancer/Input System/CharacterController/KCC/BBB。
- 手动 Play Mode 验证 W 为 Walk、Shift+W 为 Run、松开 W 播对应停止动画、RunEnd/WalkEnd 中重新输入立即起步。
