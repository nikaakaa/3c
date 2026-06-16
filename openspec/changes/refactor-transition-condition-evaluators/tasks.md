## 0. 并行协同
- [ ] 0.1 读取 `refactor-state-timeline-facts-authority` 的 proposal/design/tasks，确认 current/projected/target facts 语义。
- [ ] 0.2 facts 字段稳定前，只实现 evaluator collection、adapter、validator 和纯测试。
- [ ] 0.3 runner transition 主循环集成前，确认 condition context 只消费 timeline proposal 的权威 facts 包。
- [ ] 0.4 如需 action completed/exit facts，等待 `refactor-state-action-motion-output` 暴露稳定 result/facts 合约。
- [ ] 0.5 如果实现需要复制 timeline facts、action motion facts 或保留 runner 双路径，停止集成并先同步设计。

## 1. 准备
- [x] 1.1 读取本变更 `proposal.md`、`design.md` 和全部 spec delta。
- [x] 1.2 读取 `CharacterStateTransitionEvaluator`。
- [x] 1.3 读取 `CharacterStateMachineRunner` transition 选择代码。
- [x] 1.4 列出现有 `CharacterStateTransitionConditionKind` 全部成员。
- [x] 1.5 将现有条件分组为 core、Locomotion、Action、Animation。
- [x] 1.6 确认本变更不新增轻攻击、跳跃或受击状态。

## 2. 自动测试先行
- [x] 2.1 增加 characterization 测试：`HasMoveIntent` 行为保持。
- [x] 2.2 增加 characterization 测试：`NoMoveIntent` 行为保持。
- [x] 2.3 增加 characterization 测试：`StateCanExit` 行为保持。
- [x] 2.4 增加 characterization 测试：`HasInputRequest` 行为保持。
- [x] 2.5 增加 characterization 测试：`StateElapsedAtLeast` 行为保持。
- [x] 2.6 增加 characterization 测试：`MoveTurnBackRequested` 行为保持。
- [x] 2.7 增加 characterization 测试：`LocomotionAnimationCanExit` 行为保持。
- [x] 2.8 增加 characterization 测试：`ActionCanExit` 行为保持。
- [x] 2.9 增加测试：缺失 evaluator 时配置校验报错。
- [x] 2.10 增加测试：重复 evaluator key 时配置校验报错。
- [x] 2.11 增加静态测试：runner 不包含 `MoveTurnBackRequested` 分支。
- [x] 2.12 增加静态测试：runner 不包含 `ActionCanExit` 分支。
- [x] 2.13 增加静态测试：evaluator 不引用 Animancer、Animator、CharacterController、InputAction、Transform。
- [x] 2.14 增加测试：condition trace 可由 diagnostics adapter 输出。
- [x] 2.15 增加测试：condition definition 不保存 MonoBehaviour 或 ScriptableObject evaluator 引用。
- [x] 2.16 增加测试：evaluator collection 以稳定顺序处理 evaluator。
- [x] 2.17 增加测试：同一 condition key 被两个 evaluator 支持时报错。
- [x] 2.18 增加测试：Attack/Jump/HitReact 字符串不出现在 runner transition 选择源码。
- [x] 2.19 增加测试：TurnBack condition probe 日志字段从 trace 生成。
- [x] 2.20 增加测试：rejected action request 不进入 transition condition context。

## 3. 实现
- [x] 3.1 定义 condition evaluator 输入模型。
- [x] 3.2 定义 condition evaluator result。
- [x] 3.3 定义 condition evaluation trace。
- [x] 3.4 定义 evaluator collection。
- [x] 3.5 实现 core condition evaluator。
- [x] 3.6 实现 Locomotion condition evaluator。
- [x] 3.7 实现 Animation playback condition evaluator。
- [x] 3.8 实现 Action condition evaluator。
- [x] 3.9 将默认 evaluator collection 装配进状态机 runner。
- [x] 3.10 修改 runner transition 选择，只通过 collection 求值。
- [x] 3.11 将现有 `LogTurnBackConditionProbe` 迁到 trace consumer。
- [x] 3.12 保留旧 enum 到 key 的兼容映射。
- [x] 3.13 删除或降级中心 evaluator 中的业务分支。
- [x] 3.14 将 condition key 参数模型与旧 enum factory 建立兼容层。
- [x] 3.15 在 `CharacterStateMachineValidator` 中校验 condition evaluator 覆盖率。
- [x] 3.16 在 validator 中校验 evaluator key 不重复。
- [x] 3.17 将 condition trace 纳入 runner frame result 或等价结果。
- [x] 3.18 将 diagnostics adapter 接入 condition trace。
- [x] 3.19 标记旧中心 evaluator 为迁移兼容或删除。

## 4. 配置与文档
- [x] 4.1 更新统一状态机文档，说明 condition key 与 evaluator adapter 关系。
- [x] 4.2 更新新增状态指南，要求新业务条件新增 evaluator adapter。
- [x] 4.3 标记 path、tag、module type 和 condition key 的权威职责。
- [x] 4.4 确认默认状态机资产无需新增 fallback 字段。
- [x] 4.5 更新 `locomotion-state-graph-config` 相关文档，保持受控条件集合原则。
- [x] 4.6 更新 runtime diagnostic 文档，说明 condition trace 来源。
- [x] 4.7 更新轻攻击前置说明：新增攻击条件必须走 adapter。

## 5. 验证
- [x] 5.1 运行 `openspec validate refactor-transition-condition-evaluators --strict --no-interactive`。
- [x] 5.2 运行 `dotnet build .\Assembly-CSharp.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [x] 5.3 运行 `dotnet build .\Assembly-CSharp-Editor.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [ ] 5.4 运行 `Tests.Editor.UnifiedCharacterStateMachineTests` 相关定向测试。
- [ ] 5.5 全部任务真实完成后再将 checklist 标为 `- [x]`。
