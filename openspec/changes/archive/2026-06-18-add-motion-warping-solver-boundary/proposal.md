# Change: 规划 Motion Warping 求解层边界

## Why
当前项目已经有 `ActionMotionResolver`、`AnimationMotionProfileSampler`、`TurnBackMotionResolver` 和统一 motion executor，但它们还没有被收敛成正式 Motion Warping / Motion Source Solver 层。后续翻越、近战吸附、处决、跳跃落点校正和动作对齐如果直接塞进 Action、Locomotion 或 Animancer Presenter，会重新产生分裂运动路径。

本变更先定义 Motion Warping 在角色帧管线中的位置、数据形状和边界，不直接实现具体翻越或攻击动作。

## What Changes
- 明确 Motion Warping 属于 Motion Channel 的纯数据求解层，位于 motion intent / animation motion source 之后、`MovementCommand` / `ActionMovementCommand` 之前。
- 为 ActionTimeline 的 Motion clip 预留 warp payload / target binding，不让 Timeline runtime 直接读取场景对象或执行运动。
- 将 Locomotion 动画运动源和 Action 动作位移统一到同一类 motion source / warp solver 边界中，现有 `ActionMotionResolver`、`TurnBackMotionResolver` 可作为迁移输入。
- 明确第一版 warp target 只消费当前 tick 的 target pose snapshot；移动目标由 provider 每 tick 刷新 snapshot，solver 不追踪目标、不预测轨迹、不缓存 Unity object。
- 明确 Action 与 Locomotion 第一版共享 `MotionWarpInput` / `MotionWarpResult`，输出继续分别适配到 `MovementCommand` / `ActionMovementCommand`，不在本变更中强行合并 command contract。
- 明确第一版可展示 slice 聚焦攻击吸附和转向修正，先解决近战动作窗口内的位置与朝向对齐。
- 明确 warp target 必须先解析为纯数据 target snapshot，solver 不持有 `Transform`、`GameObject`、`Animator`、Animancer runtime 或 `CharacterController`。
- 明确最终运动仍由 `CharacterFramePlan` 选择，并经统一 output applier 调用现有 motion executor。
- 增加自动测试和静态边界验证，证明不会新增第二 motion executor、第二 presenter、直接 Transform 写入或 fallback 目标。

## Non-Goals
- 不实现完整翻越、攀爬、处决、连招、命中判定、目标选择 AI 或跳跃落点系统。
- 不实现跨 tick 目标预测、目标轨迹缓存、网络补偿或持续追踪型 moving target solver。
- 不合并 `MovementCommand` 与 `ActionMovementCommand` 的正式 contract；第一版只统一 solver 输入输出。
- 不实现 IK、脚锁、碰撞探测、导航查询或目标选择 AI。
- 不改变 `CharacterFramePipeline` phase 顺序。
- 不替换当前 `ActionMotionResolver`、`TurnBackMotionResolver` 的现有行为输出。
- 不绕过 `formalize-animation-playback-rollback-authority`；影响 simulation 输出的播放进度仍由该变更定义。
- 不把 `OnAnimatorMove` runtime delta、Animancer callback 或 Presenter 状态作为 Motion Warping 权威。
- 不新增 fallback 配置；缺 warp target、motion profile 或绑定时必须报正式配置错误或输出明确无效结果。

## Impact
- Affected specs:
  - `animation-motion-source-pipeline`
  - `action-timeline-framework`
  - `character-frame-pipeline`
- Affected code:
  - `Assets/Scripts/Character/Action/Model/ActionMotionTypes.cs`
  - `Assets/Scripts/Character/Action/Timeline/Model/...`
  - `Assets/Scripts/Character/Action/Solver/ActionMotionResolver.cs`
  - `Assets/Scripts/Character/Movement/Runtime/LocomotionMotionFactsProvider.cs`
  - `Assets/Scripts/Character/Movement/Solver/TurnBack/TurnBackMotionResolver.cs`
  - `Assets/Scripts/Character/Movement/Model/MovementCommand.cs`
  - `Assets/Scripts/Character/Pipeline/Model|Runtime/...`
  - `Assets/Tests/Editor/Character/...`
- Related active changes:
  - `formalize-animation-playback-rollback-authority`
  - `migrate-ref-timeline-editor-to-formal-action-config`

## 验证
- `openspec validate add-motion-warping-solver-boundary --strict --no-interactive`
- 后续实现阶段必须补 EditMode 测试覆盖：
  - Motion warp solver 纯数据输入输出。
  - ActionTimeline Motion clip 的 warp payload 评估不执行副作用。
  - 缺失 target / profile 不 fallback。
  - warp result 进入 `CharacterFrameSubmission` 并由 `CharacterFramePlan` 选择。
  - Animancer Presenter、Timeline evaluator 和 solver 不调用 motion executor 或写 Transform。
