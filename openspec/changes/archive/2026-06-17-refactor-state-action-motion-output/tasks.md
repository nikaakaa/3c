## 0. 并行协同
- [x] 0.1 读取 `refactor-state-timeline-facts-authority` 的 proposal/design/tasks，确认 current/projected/target facts 字段命名。
- [x] 0.2 facts 字段稳定前，只实现 action motion spec、resolve input/result、resolver 和纯测试。
- [x] 0.3 修改 `CharacterStateMachineFrame`、`CharacterFrameSubmission` 或 `FullBodySubmissionBuilder` 前，与 timeline facts proposal 对齐字段放置。
- [x] 0.4 向 `refactor-transition-condition-evaluators` 暴露 ActionCanExit 所需的稳定 result/facts，不暴露 resolver 内部策略。
- [x] 0.5 如果实现需要第二套 action motion facts、第二条 motion executor 或绕过 `IActionMovementExecutor`，停止集成并先同步设计。

## 1. 准备
- [x] 1.1 读取本变更 `proposal.md`、`design.md` 和全部 spec delta。
- [x] 1.2 读取 `CharacterStateOutputResolver`。
- [x] 1.3 读取 `CharacterStateMachineFrame`。
- [x] 1.4 读取 `ActionMovementCommand` 和 `IActionMovementExecutor`。
- [x] 1.5 读取 FullBody submission / action motion build 阶段。
- [x] 1.6 列出 action movement distance/duration/complete/run latch 的所有使用点。

## 2. 自动测试先行
- [x] 2.1 增加 characterization 测试：Dodge Directional 本帧距离保持。
- [x] 2.2 增加 characterization 测试：Dodge Backstep 本帧距离保持。
- [x] 2.3 增加 characterization 测试：Directional 完成后 set run latch。
- [x] 2.4 增加 characterization 测试：Backstep 完成后不 set run latch。
- [x] 2.5 增加测试：`CharacterStateOutputResolver` 不构造 `ActionMovementCommand`。
- [x] 2.6 增加测试：`CharacterStateOutputResolver` 不计算 frame distance。
- [x] 2.7 增加测试：`ActionMotionResolver` 输出 `ActionMovementCommand`。
- [x] 2.8 增加测试：`ActionMotionResolver` 不引用 CharacterController、Animancer、Animator、InputAction、Transform。
- [x] 2.9 增加 Character frame pipeline / FullBody submission 测试：motion spec 经 resolver 后再执行。
- [x] 2.10 增加 rollback characterization：Dodge replay action facts 保持。
- [x] 2.11 增加测试：blackboard action facts 从 resolver result 写入。
- [x] 2.12 增加测试：resolver result source step 进入 action facts。
- [x] 2.13 增加测试：ActionMotionSpec 不持有 UnityEngine.Object。
- [x] 2.14 增加测试：ActionMotionResolveResult 可复制且不持有场景对象。
- [x] 2.15 增加测试：轻攻击占位 spec 不要求修改 output resolver 数学。
- [x] 2.16 增加测试：rollback comparison 不忽略 strict action motion result。

## 3. 实现
- [x] 3.1 定义 `ActionMotionSpec` 或等价纯数据模型。
- [x] 3.2 定义 `ActionMotionResolveInput`。
- [x] 3.3 定义 `ActionMotionResolveResult`。
- [x] 3.4 修改 `CharacterStateMachineFrame`，加入 action motion spec。
- [x] 3.5 修改 `CharacterStateOutputResolver`，只解析 spec。
- [x] 3.6 新增 `ActionMotionResolver`。
- [x] 3.7 将帧距离计算迁入 `ActionMotionResolver`。
- [x] 3.8 将 action completed 判断迁入 `ActionMotionResolver`。
- [x] 3.9 将 run latch on complete 派生迁入 resolver result 或 action lifecycle adapter。
- [x] 3.10 修改 FullBody submission 构建阶段，在 Action motion 构建中调用 resolver。
- [x] 3.11 修改 Character output applier 或等价正式出口，执行 resolver 产出的 command。
- [x] 3.12 修改 runtime blackboard action facts 写入来源。
- [x] 3.13 删除 output resolver 中遗留 action motion 计算。
- [x] 3.14 将 resolver result 写入 `CharacterFrameSubmission` 或等价结果。
- [x] 3.15 修改 `CharacterRuntimeActionFacts.FromStateFrame` 或等价入口，接收 resolver result。
- [x] 3.16 修改 rollback snapshot capture，保留 action motion result 必要字段。
- [x] 3.17 修改 rollback comparer，使用 resolver result 判断 strict gameplay action facts。
- [x] 3.18 添加 resolver 诊断摘要，但不直接提交日志。
- [x] 3.19 确认 motion executor 接口不变。

## 4. 配置与文档
- [x] 4.1 确认默认 Dodge 配置数值不变。
- [x] 4.2 更新 Character frame order / FullBody submission 文档。
- [x] 4.3 更新状态输出文档：输出 spec，不输出执行 command。
- [x] 4.4 更新新增 Action 指南：动作位移数学归 ActionMotionResolver。
- [x] 4.5 更新 blackboard 文档：action facts 来源为 resolver result。
- [x] 4.6 更新 rollback 文档：action motion result 属于 strict gameplay facts。

## 5. 验证
- [x] 5.1 运行 `openspec validate refactor-state-action-motion-output --strict --no-interactive`。
- [x] 5.2 运行 `dotnet build .\Assembly-CSharp.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [x] 5.3 运行 `dotnet build .\Assembly-CSharp-Editor.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [x] 5.4 运行 `Tests.Editor.UnifiedCharacterStateMachineTests` 相关定向测试。
- [x] 5.5 运行 `ThirdPersonSimulation.Tests.FullBodyRollbackReplayTests` 相关定向测试。
- [x] 5.6 全部任务真实完成后再将 checklist 标为 `- [x]`。
