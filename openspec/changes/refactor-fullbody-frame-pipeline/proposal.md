# Change: 收口 FullBody 一帧管线

## Why

当前 FullBody 主线已经有输入缓冲、Locomotion decision facts、统一状态机、motion executor、Animancer presenter 和本地回滚地基，但真实运行时仍把输入缓冲更新、Action 仲裁、状态机推进、运动执行、动画提交和 runtime facts 写入混在 `PlayerFullBodyActionController.Tick` / `PlayerLocomotionController` / tick adapter 的隐式顺序里。`SimulationTickPhase` 的阶段名已经存在，但 FullBody gameplay 主体仍整包注册在 `ExecuteMotion`，导致 TurnBack、Dodge、后续 Attack combo 和 rollback 都要记住同一批时序细节。

需要先把 FullBody 一帧拆成正式 frame pipeline，使“谁读输入、谁构建 facts、谁推进状态机、谁执行运动、谁提交表现、谁写快照”成为可测试 contract，而不是继续靠 MonoBehaviour Tick 的调用顺序维持。

## What Changes

- 新增 FullBody frame pipeline contract，将一帧拆成：输入事实读取、输入请求缓冲更新、Locomotion facts 准备、FullBody Action 请求仲裁、统一状态机推进、运动命令构建、运动执行、动画表现提交、runtime facts / snapshot 写入和相机/表现桥接。
- 将 `FullBodyActionTickAdapter` 从“在 `ExecuteMotion` 整包调用 FullBody Tick”迁移为按 `SimulationTickPhase` 分阶段执行或委托同一个 pipeline context。
- 将 `PlayerFullBodyActionController` 降级为装配/调试入口，避免它继续硬编码完整一帧流程和单个 Dodge 请求门。
- 将 `PlayerLocomotionController` 中的 Locomotion facts、TurnBack motion facts、状态机 context、运动执行、动画提交和 snapshot restore 职责逐步收口到明确的 pipeline step 或 helper module。
- 保持统一状态机仍是 FullBody base layer 状态权威；pipeline 只编排步骤，不新增第二状态机。
- 保持 `MotionExecutor` 是唯一角色根运动出口；pipeline 不直接调用 `CharacterController.Move`。
- 保持 Animancer presenter 只消费动画命令并回传纯数据事实；pipeline 不让动画层决定状态切换。
- 将 FullBody replay / synctest 改为使用同一条 FullBody frame pipeline 推进输入帧，而不是单独拼装 input buffer、controller tick 和 playback restore。
- 不实现攻击连段、伤害判定、hitbox、真实网络、Fantasy proto、UpperBody 并行层或完整 Timeline 编辑器。

## Impact

- Affected specs:
  - `fullbody-action-framework`
  - `simulation-tick-system`
  - `fullbody-rollback-replay`
- Affected code:
  - `Assets/Scripts/Character/Action/FullBody/Runtime/PlayerFullBodyActionController.cs`
  - `Assets/Scripts/Character/Action/FullBody/Runtime/FullBodyActionTickAdapter.cs`
  - `Assets/Scripts/Character/Movement/Runtime/PlayerLocomotionController.cs`
  - `Assets/Scripts/Character/Movement/Solver/BasicLocomotionPipeline.cs`
  - `Assets/Scripts/Character/StateMachine/*`
  - `Assets/Scripts/Input/Runtime/*`
  - `Assets/Scripts/Simulation/Core/*`
  - `Assets/Scripts/Simulation/Rollback/*`
  - `Assets/Tests/Editor/*`
- Related active changes:
  - Builds on `refactor-locomotion-decision-pipeline` facts model instead of replacing it.
  - Keeps `formalize-turnback-locomotion-state` TurnBack motion policy semantics intact.
  - Provides the stable frame pipeline that `add-light-attack-combo-action` should consume before Attack is implemented.
  - Must coordinate with `refactor-rollback-layering-contract` before editing rollback snapshot ownership.
