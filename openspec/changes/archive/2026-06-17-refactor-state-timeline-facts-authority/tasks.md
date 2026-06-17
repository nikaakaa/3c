## 0. 并行协同
- [x] 0.1 确定 current/projected/target facts 的最终类型名和字段名。
- [x] 0.2 标记 `CharacterFramePipeline`、`CharacterFrameSubmission`、`FullBodySubmissionBuilder` 中 timeline facts 采样与传递由本变更优先修改。
- [x] 0.3 修改 `CharacterStateMachineRuntimeTypes.cs` 或 `CharacterStateMachineFrame` 前，与 `refactor-state-action-motion-output` 对齐字段放置。
- [x] 0.4 修改 `CharacterStateMachineRunner.cs` transition 主循环前，与 `refactor-transition-condition-evaluators` 对齐 condition context 输入。
- [x] 0.5 如果并行分支引入第二套 frame facts/context，停止集成并先统一模型。

## 1. 准备
- [x] 1.1 读取本变更 `proposal.md`、`design.md` 和全部 spec delta。
- [x] 1.2 读取 `add-configurable-state-interrupt-windows` 的 proposal/design/spec，确认不重复定义窗口模型。
- [x] 1.3 读取 `CharacterStateTimelineFactSampler`、FullBody action request submission resolver、`CharacterStateMachineRunner`、`CharacterFramePipeline`。
- [x] 1.4 列出所有 `SampleCurrent` 调用点。
- [x] 1.5 标记每个调用点当前语义：current、projected 或 target。
- [x] 1.6 确认本变更不改 TurnBack/Dodge 数值。

## 2. 自动测试先行
- [x] 2.1 增加测试：Action request resolver 不调用 `CharacterStateTimelineFactSampler.SampleCurrent`。
- [x] 2.2 增加测试：Action request resolver 输入包含 current `StateTimelineWindowFacts`。
- [x] 2.3 增加测试：同一帧 request submission / interrupt arbitration 和 transition evaluator 可观察到相同 current facts id。
- [x] 2.4 增加测试：projected facts 不会覆盖 current facts。
- [x] 2.5 增加测试：transition 后 target facts 使用新状态 state time。
- [x] 2.6 增加测试：runner 不直接调用 `RuntimeDiagnosticLog.Submit`。
- [x] 2.7 增加测试：diagnostics adapter 仍输出 `state-timeline-window-facts`。
- [x] 2.8 增加 TurnBack 回归测试：RunLoop 反向输入进入 TurnBack。
- [x] 2.9 增加 Dodge 回归测试：Dodge request window 行为保持。
- [x] 2.10 增加 rollback characterization：同输入 replay 的 timeline facts 序列稳定。
- [x] 2.11 增加测试：current facts 的 source step 与 FullBody frame step 一致。
- [x] 2.12 增加测试：projected facts 的 elapsed seconds 等于 current state time + delta。
- [x] 2.13 增加测试：target facts 的 state id 等于 transition target。
- [x] 2.14 增加测试：request submission / interrupt arbitration 不消费 projected facts。
- [x] 2.15 增加测试：output resolver 不自行调用 sampler。
- [x] 2.16 增加测试：缺失 current facts 时需要 timeline window 的请求被拒绝或配置报错。

## 3. 实现
- [x] 3.1 定义帧内 timeline facts 输入模型。
- [x] 3.2 将 current facts 写入 Character frame context。
- [x] 3.3 修改 FullBody action request submission resolver input，用 current facts 替换 definition/snapshot 采样入口。
- [x] 3.4 修改 resolver 内部逻辑，只消费输入 facts。
- [x] 3.5 修改 `CharacterStateMachineContext` 或等价模型，显式携带 current facts。
- [x] 3.6 修改 runner transition 逻辑，显式命名 projected facts。
- [x] 3.7 修改 transition 后 Enter/Tick 逻辑，显式命名 target facts。
- [x] 3.8 修改 output resolver，只消费传入 facts。
- [x] 3.9 将 runner 内 timeline 诊断提交改为纯数据 trace。
- [x] 3.10 在 Character diagnostics adapter 中提交 trace。
- [x] 3.11 删除或降级旧的 resolver 反向采样代码。
- [x] 3.12 删除不再使用的 helper 或保留为内部 sampler。
- [x] 3.13 将 current facts 纳入 `CharacterFrameSubmission` 或等价结果，便于测试读取。
- [x] 3.14 将 projected facts 纳入 transition trace。
- [x] 3.15 将 target facts 纳入 transition trace。
- [x] 3.16 更新 rollback snapshot comparison，确认不把表现层漂移误判为 logic facts 分歧。

## 4. 配置与文档
- [x] 4.1 确认默认状态机资产不新增 parallel timeline 配置入口。
- [x] 4.2 更新 agent 文档中的 frame order。
- [x] 4.3 更新 timeline facts 术语：current、projected、target。
- [x] 4.4 明确 `add-configurable-state-interrupt-windows` 仍是 window 数据权威。
- [x] 4.5 更新新增 Action 编写指南，禁止 request submission / interrupt arbitration 自行采样 timeline。
- [x] 4.6 更新诊断日志字段说明，标识 current/projected/target。

## 5. 验证
- [x] 5.1 运行 `openspec validate refactor-state-timeline-facts-authority --strict --no-interactive`。
- [x] 5.2 运行 `dotnet build .\Assembly-CSharp.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [x] 5.3 运行 `dotnet build .\Assembly-CSharp-Editor.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [x] 5.4 运行 `Tests.Editor.UnifiedCharacterStateMachineTests` 相关定向测试。
- [x] 5.5 运行 `ThirdPersonSimulation.Tests.FullBodyRollbackReplayTests` 相关定向测试。
- [x] 5.6 全部任务真实完成后再将 checklist 标为 `- [x]`。
