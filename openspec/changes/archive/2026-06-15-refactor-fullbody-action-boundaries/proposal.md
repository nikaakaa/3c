# Change: 收口 FullBody Action 请求与运行时边界

## Why
当前状态机主线已经统一到 FullBody frame pipeline，但仍有几处会在接轻攻击、跳跃、受击前放大分裂：request submission arbiter 只手写 TurnBack/Dodge，Action motion resolver 又重新读取 Dodge 配置，runner 保存 TurnBack/Action 专用 payload，snapshot 直接派生 FullBody/Locomotion/Action 解释。

这些残留会让后续新增动作时继续修改 gate、runner、motion resolver 和 snapshot 四个核心位置，形成新的业务分发中心。

## What Changes
- 将 FullBody 请求准入拆为可组合的请求候选构建模块，gate 只负责收集候选、调用仲裁、输出 accepted request fact。
- 将 Action motion resolver 改为只消费通用 `ActionMotionSpec` 或等价纯数据 motion profile，不再读取 Dodge 专用配置。
- 将 runner 内的 Action/TurnBack 专用 payload 下沉为状态 payload 或状态输出数据，runner 只保存可恢复的通用状态推进事实。
- 将 `CharacterStateMachineSnapshot` 中的 FullBody/Locomotion/Action 派生解释迁到外围 view/adapter，snapshot 保持纯状态机身份。
- 保持现有 Dodge、TurnBack、WASD 行为数值不变。

## Non-Goals
- 不实现轻攻击、跳跃或受击。
- 不新增第二套状态机 runtime。
- 不新增第二条 motion executor。
- 不重建 timeline policy 数据模型。
- 不引入 fallback 配置。
- 不在本变更中完成 lifecycle 模块化。

## Impact
- Affected specs:
  - `fullbody-action-framework`
  - `action-interrupt-arbiter`
  - `unified-character-state-machine`
- Affected code:
  - `Assets/Scripts/Character/Action/FullBody/Solver/CharacterActionRequestSubmissionArbiter.cs`
  - `Assets/Scripts/Character/Action/FullBody/Solver/CommittedActionInterruptRequestFactory.cs`
  - `Assets/Scripts/Character/Action/FullBody/Solver/ActionMotionResolver.cs`
  - `Assets/Scripts/Character/Action/FullBody/Model/ActionMotionTypes.cs`
  - `Assets/Scripts/Character/StateMachine/Solver/Runtime/CharacterStateMachineRunner.cs`
  - `Assets/Scripts/Character/StateMachine/Model/CharacterStateMachineRuntimeTypes.cs`
  - `Assets/Scripts/Character/StateMachine/Solver/Output/CharacterStateOutputResolver.cs`
  - `Assets/Scripts/Character/Movement/*`
  - `Assets/Tests/Editor/UnifiedCharacterStateMachineTests.cs`
  - `Assets/Tests/Editor/Simulation/FullBodyRollbackReplayTests.cs`

## Related Changes
- Depends on finishing verification for `refactor-state-timeline-facts-authority`, `refactor-state-action-motion-output`, and `refactor-transition-condition-evaluators`.
- Coordinates with `add-configurable-state-interrupt-windows`; this change owns request candidate composition, while that change owns timeline/policy data.
- Blocks implementation expansion of `add-light-attack-combo-action` until request and motion seams are stable.
