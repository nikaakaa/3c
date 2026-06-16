# Change: 拆分状态输出与 Action Motion 计算

## Why
`CharacterStateOutputResolver` 当前既解析状态输出，又计算 Action 每帧位移、完成状态、Run latch 和 TurnBack policy。它虽然不执行运动，但已经开始承载 gameplay motion 求解，后续轻攻击、跳跃、受击加入后会变成第二个 gameplay solver。

## What Changes
- 状态机输出层只产出纯数据 action motion spec、variant、state time、locked direction 和 timeline facts。
- `ActionMotionResolver` 或等价模块负责把 action motion spec 转换为 `ActionMovementCommand`。
- action completed、run latch on complete 等 action motion 派生结果归 Action motion resolver 或 action lifecycle adapter。
- `CharacterStateOutputResolver` 不再计算帧距离。
- Character frame pipeline 继续是唯一 frame phase owner；FullBodySubmissionBuilder 或等价提交构建器负责把 Action motion resolver result 提交给角色级输出层，motion executor 不变。
- runtime blackboard 的 action facts MUST 来自状态机 frame 与 Action motion resolver result，而不是从 output resolver 重算。
- rollback/replay MUST 记录并比较 resolver 结果所需的稳定 facts，保证预测路径和正式路径一致。

## Non-Goals
- 不新增第二条运动执行路径。
- 不改变 `ActionMovementCommand -> IActionMovementExecutor` 执行出口。
- 不实现轻攻击、跳跃或受击动作。
- 不修改现有 Dodge 位移数值。
- 不新增 fallback 配置。

## Impact
- Affected specs:
  - `unified-character-state-machine`
  - `fullbody-action-framework`
  - `character-runtime-blackboard`
  - `fullbody-rollback-replay`
- Affected code:
  - `Assets/Scripts/Character/StateMachine/Solver/Output/CharacterStateOutputResolver.cs`
  - `Assets/Scripts/Character/StateMachine/Model/CharacterStateMachineRuntimeTypes.cs`
  - `Assets/Scripts/Character/Pipeline/Runtime/CharacterFramePipeline.cs`
  - `Assets/Scripts/Character/Action/FullBody/Runtime/FullBodySubmissionBuilder.cs`
  - `Assets/Scripts/Character/Action/FullBody/Model/*`
  - `Assets/Scripts/Character/Action/FullBody/Solver/*`
  - `Assets/Tests/Editor/UnifiedCharacterStateMachineTests.cs`

## Related Changes
- Builds on `refactor-character-state-node-modules`.
- Coordinates with `refactor-character-frame-submission-pipeline`.
- Should land before expanding `add-light-attack-combo-action`, so light attack motion does not add more math to `CharacterStateOutputResolver`.

## Parallel Implementation Plan
- 本变更可以和 `refactor-state-timeline-facts-authority`、`refactor-transition-condition-evaluators` 并行推进，但它只拥有 action motion spec、resolver、resolve result 和 action motion facts 写入来源。
- 可以先并行实现 `ActionMotionSpec`、`ActionMotionResolveInput`、`ActionMotionResolveResult`、`ActionMotionResolver` 和纯 resolver 测试，不需要等待其他 proposal。
- `CharacterStateMachineFrame`、`CharacterFrameSubmission`、`FullBodySubmissionBuilder` 的公共字段和集成点必须等待 `refactor-state-timeline-facts-authority` 稳定 facts 字段后再同步修改。
- 本变更不得重新采样 timeline；action motion resolver 只能消费状态机 frame 提供的 state time、variant、motion spec 和 timeline facts。
- `refactor-transition-condition-evaluators` 如需 ActionCanExit，应通过本变更暴露的稳定 resolve result 或 action facts 进入 condition context，不得直接读取 resolver 内部策略。
- Character frame pipeline 仍是唯一 frame phase owner，`IActionMovementExecutor` 仍是唯一动作运动执行出口。
- 轻攻击、跳跃、受击只能在 Dodge motion spec/result 拆分、blackboard 写入和 rollback comparison 都完成后，再新增自己的 motion spec。

## Stop Conditions
- 如果需要让 `CharacterStateOutputResolver` 继续计算帧距离才能保持行为，必须停止并重新评审 spec/result 切分。
- 如果引入了第二条 motion executor 或绕过 `IActionMovementExecutor`，必须停止。
- 如果 rollback 只能忽略 action motion result 才能通过，必须停止并补确定性字段。
- 如果并行实现要求新增第二套 action motion facts 或 frame result，必须停止并先和 timeline facts proposal 合并字段模型。
- 如果 condition evaluator 需要直接依赖 `ActionMotionResolver` 内部计算细节，必须停止并改为稳定 result/facts 合约。
