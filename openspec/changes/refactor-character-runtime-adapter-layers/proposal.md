# Change: 收口 Character Runtime Adapter 分层

## Why
`refactor-locomotion-adapter-modules` 已经把 Locomotion adapter 的第一批内部职责拆出，但 Character 运行时仍有多个胖 Runtime Adapter：`PlayerFullBodyActionController`、`BasicLocomotionAnimancerPresenter`、`CharacterControllerBasicMotionExecutor` 和 `FullBodyFramePipeline` 仍混合 Unity 引用、状态机编排、动画播放细节、运动转换、snapshot/restore 和诊断日志。

如果只继续按文件长度机械拆分，会得到更多浅 Module，调用方仍要理解同一批细节。需要先把跨 Movement、Animation、FullBody 的分层规则写成 OpenSpec：Runtime Adapter 只做装配和外围调用，领域规则进入 Model/Solver，日志进入 Diagnostics，正式权威和既有行为不变。

## What Changes
- 定义 Character runtime adapter 的统一分层规则：`Runtime / Model / Solver / Diagnostics / Contracts`，并要求拆分后的 Module 有明确 Interface、Depth 和 Locality。
- 在 Locomotion 拆分完成后，继续收口 `BasicLocomotionAnimancerPresenter`、`CharacterControllerBasicMotionExecutor`、`PlayerFullBodyActionController` 和 `FullBodyFramePipeline` 的内部职责。
- 将动画 alias 解析、动画 presenter 诊断、motion executor 的 planar delta 解析、FullBody 引用解析、FullBody 诊断和 pipeline 请求门协作逻辑迁移到明确 Module。
- 保持 `PlayerFullBodyActionController` 作为唯一正式 `CharacterStateMachineRunner` owner；拆出的 Module 不得创建 runner、注册 tick driver 或引入第二状态权威。
- 保持 `MotionExecutor` 是唯一角色根运动出口；拆出的 Module 不得调用 `CharacterController.Move` 或写角色 Transform，除非它本身就是正式 runtime motion adapter。
- 保持 Animancer Presenter 只负责播放和只读进度，不新增动画决定状态、动画执行位移或动画写入黑板的旁路。
- 保留现有日志语义和关键 event id；除非用户明确要求，不删除 log。

## Non-Goals
- 不改变 Idle、MoveStart、MoveLoop、MoveStop、TurnBack、Dodge 或后续 Action 的玩法语义。
- 不改变 `FullBodyActionTickAdapter -> PlayerFullBodyActionController -> FullBodyFramePipeline` 的正式 tick 主线。
- 不重新定义 animation playback rollback authority；该范围归属 `formalize-animation-playback-rollback-authority`。
- 不重新定义 animation motion source；该范围归属 `add-animation-motion-source-pipeline`。
- 不新增第二角色控制器、第二 runner owner、第二 motion executor 或 fallback 配置。
- 不删除日志，不清理资源资产，不运行 Unity batchmode。

## Impact
- Affected specs:
  - `project-structure`
  - `unified-character-state-machine`
  - `basic-locomotion-animation`
  - `runtime-diagnostic-logging`
- Affected code:
  - `Assets/Scripts/Character/Movement/Runtime/PlayerLocomotionController.cs`
  - `Assets/Scripts/Character/Movement/Runtime/CharacterControllerBasicMotionExecutor.cs`
  - `Assets/Scripts/Character/Animation/Runtime/BasicLocomotionAnimancerPresenter.cs`
  - `Assets/Scripts/Character/Action/FullBody/Runtime/PlayerFullBodyActionController.cs`
  - `Assets/Scripts/Character/Action/FullBody/Runtime/FullBodyFramePipeline.cs`
  - `Assets/Scripts/Character/Movement/Model`
  - `Assets/Scripts/Character/Movement/Solver`
  - `Assets/Scripts/Character/Movement/Diagnostics`
  - `Assets/Scripts/Character/Animation/Model`
  - `Assets/Scripts/Character/Animation/Solver`
  - `Assets/Scripts/Character/Animation/Diagnostics`
  - `Assets/Scripts/Character/Action/FullBody/Model`
  - `Assets/Scripts/Character/Action/FullBody/Solver`
  - `Assets/Scripts/Character/Action/FullBody/Diagnostics`
  - `Assets/Tests/Editor`
- Related active changes:
  - `refactor-locomotion-adapter-modules` 是本变更的前置 Movement 局部拆分；本变更不得重复其任务。
  - `refactor-fullbody-frame-pipeline` 已经定义 FullBody frame pipeline；本变更只能继续深化 Module，不改变 phase 语义。
  - `formalize-animation-playback-rollback-authority` 定义 playback restore / sampling window 权威；本变更不得在 Presenter 拆分时重写该语义。
  - `add-animation-motion-source-pipeline` 定义 TickSampledMotion；本变更不得恢复 Animator runtime delta 作为正式 movement facts。

## Clarifications
- “继续拆”在本提案中解释为：在现有统一状态机和 FullBody pipeline 主线不变的前提下，继续把胖 Runtime Adapter 内部的纯逻辑、诊断和引用解析拆到明确 Module。
- 本提案不要求一次性把所有文件缩到某个固定行数；验收以职责归属、静态边界和行为测试为准。
