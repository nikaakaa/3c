# Change: 抽出 Locomotion Frame 主干 Pipeline

## Why
`PlayerLocomotionController` 经过 facts、TurnBack、diagnostics 和 reference resolver 的外围拆分后仍有约 1550 行，并且核心一帧流程仍集中在同一个 Runtime Adapter：prepare decision facts、推进统一状态机、应用状态输出、生成 motion facts、更新 frame 状态、写 runtime blackboard、维护 phase/gait memory 和触发诊断。

继续只迁移日志或引用解析会改善局部文件长度，但不会解决真正的架构摩擦。需要把 Locomotion 一帧主干抽成明确的 `LocomotionFramePipeline`，让 `PlayerLocomotionController` 回到 Unity adapter 和正式外围调用角色。

## What Changes
- 新增或等价创建 `LocomotionFramePipeline`，负责 Locomotion 一帧主干编排：prepare facts、state machine tick、state output apply、motion facts resolution、frame build、memory update、blackboard write 和诊断触发。
- `PlayerLocomotionController` 保留 MonoBehaviour 生命周期、Unity 引用持有、输入读取、motion executor 调用、animation presenter 调用、camera resolve 和 snapshot/restore 公开入口。
- 继续复用现有 `LocomotionFactsBuilder`、`TurnBackIntentResolver`、`TurnBackMotionResolver`、`LocomotionStateMotionBuilder`、`LocomotionDiagnostics` 和 `LocomotionSnapshotAdapter`，不新增第二状态机、第二 tick driver 或第二运动出口。
- 本变更第一阶段不得迁移 animation playback restore / previous motion playback window 的语义；与 `formalize-animation-playback-rollback-authority` 重叠处只做接口隔离和 characterization 测试。
- 保留现有日志 event id 和 channel key；除非用户另行批准，不删除日志。

## Non-Goals
- 不改变 Idle、MoveStart、MoveLoop、MoveStop、TurnBack、RunEnd、Dodge 的玩法语义。
- 不改变 `FullBodyActionTickAdapter -> PlayerFullBodyActionController -> FullBodyFramePipeline -> PlayerLocomotionController` 的正式推进主线。
- 不把 `CharacterStateMachineRunner` 创建权移出 `PlayerFullBodyActionController`。
- 不让 Locomotion pipeline 注册 tick driver、直接执行 `CharacterController.Move`、直接播放 Animancer 或写角色 Transform。
- 不重新定义 snapshot 字段、playback rollback authority、animation motion source 或 F6/F8 replay 路线。
- 不新增 fallback 配置、`Resources.Load`、全局配置单例或旧字段回退。
- 不运行 Unity batchmode。

## Impact
- Affected specs:
  - `wasd-locomotion-pipeline`
  - `unified-character-state-machine`
  - `basic-locomotion-animation`
- Affected code:
  - `Assets/Scripts/Character/Movement/Runtime/PlayerLocomotionController.cs`
  - `Assets/Scripts/Character/Movement/Solver/LocomotionFramePipeline.cs`
  - `Assets/Scripts/Character/Movement/Model`
  - `Assets/Scripts/Character/Movement/Solver/Facts`
  - `Assets/Scripts/Character/Movement/Solver/TurnBack`
  - `Assets/Scripts/Character/Movement/Solver/Motion`
  - `Assets/Scripts/Character/Movement/Diagnostics`
  - `Assets/Tests/Editor/UnifiedCharacterStateMachineTests.cs`
  - `Assets/Tests/Editor/Simulation/FullBodyRollbackReplayTests.cs`
- Related active changes:
  - `refactor-character-runtime-adapter-layers`：已做外围分层，本变更专注主干 frame pipeline。
  - `refactor-locomotion-adapter-modules`：提供 facts / TurnBack / motion / snapshot / diagnostics 的前置模块。
  - `formalize-animation-playback-rollback-authority`：仍负责 playback restore 和 sampling window 语义，本变更不得抢权。
  - `add-animation-motion-source-pipeline`：定义 TickSampledMotion 和 profile motion source，本变更只编排已批准的 motion facts。
  - `refactor-fullbody-frame-pipeline`：定义 FullBody 一帧 phase order，本变更必须继续作为其 Locomotion 子职责。

## Clarifications
- “主干拆分”不是继续搬日志或引用解析，而是抽出 `TryPrepareDecisionFrame -> TryEvaluatePreparedGameplayDecision -> TryBuildMotionFromStateDecision` 这一条一帧编排链。
- `LocomotionFramePipeline` 可以持有纯 C# frame state 或小型 context，但不得持有 Unity scene object、MonoBehaviour、Transform、CharacterController、Animancer runtime object 或 InputAction。
- 如果实施时发现必须修改 playback restore、snapshot 字段或 animation sampling window 才能抽出 pipeline，必须停止并转入对应 active change。
