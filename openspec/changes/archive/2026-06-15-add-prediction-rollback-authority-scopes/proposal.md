# Change: 预测回滚权威域与比较域骨架

## Why

当前本地 F6/F8 已经能暴露 replay 后的字段级差异，但角色控制器底层还没有一套正式的“哪些事实必须严格回滚、哪些只是表现漂移、哪些可预测后校正”的权威声明。结果是 animation normalized time、Action animation facts、TurnBack profile window 等字段容易被同一个 comparer 临时分类，形成硬编码 TurnBack 或临时忽略表现层字段的风险。

这个变更要先规划底层骨架：让状态、动画、运动来源和快照比较都通过权威域与比较域声明协作。后续业务可以标记某些状态 strict rollback、某些状态 predictive、某些状态只做表现插值，而不是为 MOBA/MMO、格斗或单机手感各写一套角色控制器路径。

## What Changes

- 新增预测回滚权威域概念，区分 `VisualOnly`、`LogicTimed`、`ProfileDriven`、`AnimatorRuntimeDirect` 等动画/运动权威类型。
- 新增回滚比较域概念，区分 `StrictGameplay`、`PredictiveGameplay`、`PresentationDrift`、`Ignored`。
- 建立状态/动画/运动的权威矩阵，使 TurnBack、MoveLoop、Dodge、Attack 等可以通过正式配置或 policy 声明回滚语义。
- 将本地 synctest 的 `differences` 与 `presentationDifferences` 提升为正式结果模型，而不是单个 comparer 内的临时分类。
- 要求 F6/F8 只因 strict gameplay mismatch 失败；表现漂移必须可诊断但不阻塞 strict replay 验收。
- 要求 runtime blackboard 和 snapshot 明确哪些 facts 是 gameplay 权威，哪些是 presentation facts 或诊断 facts。
- 保持单一 FullBody/Locomotion/motion executor 主线，不新增第二套角色控制器、第二套 replay 或 Animator root motion fallback。

## Non-Goals

- 不在本变更中实现真实网络协议、Fantasy 同步或服务端校正。
- 不引入 fixed-point 定点数库。
- 不把所有动画播放时间强行纳入 simulation tick。
- 不改变 TurnBack 的 EntryLocal 坐标空间定义。
- 不替换 `TickSampledMotion`、`AnimatorRuntimeDirect` 等既有规划，只定义它们在预测回滚里的权威语义。
- 不删除现有诊断日志。
- 不运行 Unity batchmode。

## Impact

- Affected specs:
  - `prediction-rollback-authority-scopes`
  - `local-rollback-synctest-foundation`
  - `character-runtime-blackboard`
  - `fullbody-rollback-replay`
- Affected code:
  - `3cDemo/Client/3C_Client/Assets/Scripts/Simulation/Rollback/CharacterSimulationSnapshotComparer.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Simulation/Rollback/LocalRollbackSynctestRunner.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Simulation/Rollback/LocalRollbackSynctestLogFormatter.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Simulation/Rollback/LocalRollbackSoakDebugRunner.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/StateMachine/Model/CharacterRuntimeBlackboard.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Movement/Runtime/PlayerLocomotionController.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Action/FullBody/Runtime/PlayerFullBodyActionController.cs`
  - `3cDemo/Client/3C_Client/Assets/Tests/Editor/Simulation`
- Related active changes:
  - `formalize-animation-playback-rollback-authority` 处理 profile-driven 动画播放时钟恢复。
  - `add-entry-local-animation-motion-space` 处理 TurnBack profile translation 的 EntryLocal 空间。
  - `harden-local-prediction-rollback-tooling` 提供 F6/F8 strict 工具基础。
  - `add-animation-motion-source-pipeline` 定义动画运动来源模式。
