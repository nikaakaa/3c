# Design: Run 基础移动动画参数最小配置

## Context

现有状态机已经能表达基础移动四阶段：`Idle / MoveStart / MoveLoop / MoveStop`。用户明确要求当前不要把 Walk 混进来，未来再通过 Shift 引入 Walk/Run gait。当前阶段需要解决的是 Run 动画播放参数可配置，以及 `RunEnd` 对 `MoveStop -> Idle` 等待时间的影响。

## Goals

- 逻辑状态机继续保持四阶段，不出现 `Run` 或 `Walk` 作为逻辑状态。
- 当前动画配置只表达 Run 基础移动：`Idle / RunStart / RunLoop / RunEnd`。
- 状态机不引用 Animancer、AnimationClip、TransitionLibrary 或 Unity 场景动画对象。
- Presenter 不调用状态机切换 API，不执行位移，不写 Transform。
- `RunEnd` 的退出时长以纯数据形式传入状态机。

## Non-Goals

- 不设计通用动作系统。
- 不设计多层动画图。
- 不设计 IK 窗口或打断窗口。
- 不处理 Walk/Run 输入模式切换。

## Decisions

### Decision: 状态机继续使用 Phase，而不是 Run 状态

`RunStart / RunLoop / RunEnd` 是动画表现阶段，不应该成为逻辑状态。逻辑层保留 `MoveStart / MoveLoop / MoveStop`，这样之后加入 Walk 时只需要改变动画/速度选择参数，不需要复制状态机结构。

### Decision: 当前只做 Run-only 配置

配置资产只提供 `Idle / RunStart / RunLoop / RunEnd`。`WalkStart / WalkLoop / WalkEnd` 不进入本变更，避免提前设计 Shift gait 和 Walk 速度。

### Decision: `RunEnd` 退出时长进入状态机前先解析成纯数据

`PlayerLocomotionController` 或同等主链负责从动画配置解析出 `MoveStop` 所需的退出时长，并写入 `BasicMovementSettings` 或等价纯数据。状态机只读数值，不知道该数值来自 `RunEnd`。

### Decision: 动画配置不是第二套状态机

Run 动画配置只回答“这个 phase 播哪个 alias、淡入多久、速度多少、从哪里开始、停止等待多久”。状态切换仍由 `LocomotionStateGraphConfigSO` 和条件 evaluator 决定。

## Risks / Trade-offs

- 手填 `RunEnd` 退出时长可能和实际 clip 长度不一致；当前用测试覆盖逻辑行为，手动验证覆盖视觉一致性。
- 当前只做 Run，之后加 Walk 时需要扩展配置结构；但这比现在提前混入 Walk 更清晰。
- 如果项目决定完全依赖 Animancer TransitionLibrary 的 transition 参数，需要明确哪些参数由 TransitionLibrary 管、哪些由 Run 动画配置管，避免双权威。

## Validation

- OpenSpec 严格校验。
- Unity EditMode 定向测试覆盖 Run-only 动画配置解析、`MoveStop` 等待、`MoveStop` 重新输入打断、状态机和 Presenter 边界。
- 手动在当前演示场景验证移动、松开、RunEnd 等待、RunEnd 中途重新输入。
