## 0. 前置同步
- [x] 0.1 读取本变更 `proposal.md`、`design.md` 和全部 spec delta。
- [x] 0.2 确认 `refactor-state-timeline-facts-authority` 主体已完成；剩余未勾选项不阻塞本变更，关键链路由本次定向测试覆盖。
- [x] 0.3 确认 `refactor-state-action-motion-output` 主体已完成；剩余未勾选项不阻塞本变更，关键链路由本次定向测试覆盖。
- [x] 0.4 确认 `refactor-transition-condition-evaluators` 主体已完成；剩余未勾选项不阻塞本变更，关键链路由本次定向测试覆盖。
- [x] 0.5 确认本变更不新增轻攻击、跳跃或受击。
- [x] 0.6 对 `CharacterStateMachineRunner`、`CharacterActionRequestSubmissionArbiter`、`ActionMotionResolver` 执行影响面/引用扫描，并记录风险。

## 1. 现状锁定测试
- [x] 1.1 覆盖 TurnBack request 仍由 accepted request fact 进入状态机。
- [x] 1.2 覆盖 rejected TurnBack request 不进入 TurnBack。
- [x] 1.3 覆盖 Dodge Directional request 行为保持。
- [x] 1.4 覆盖 Dodge Backstep request 行为保持。
- [x] 1.5 覆盖同一帧 TurnBack 与 Dodge 候选按 priority 稳定选择。
- [x] 1.6 覆盖 ActionMotionResolver 当前 Dodge Directional 距离保持。
- [x] 1.7 覆盖 ActionMotionResolver 当前 Dodge Backstep 距离保持。
- [x] 1.8 覆盖 runner restore 保留当前 Action/TurnBack 方向行为。
- [x] 1.9 覆盖 snapshot 当前 Owner/ActionState/LocomotionPhase 解释迁移到 view 后行为保持。

## 2. Request candidate seam
- [x] 2.1 定义 request candidate builder 输入模型。
- [x] 2.2 定义 request candidate builder 输出模型。
- [x] 2.3 定义 request candidate collection 或等价组合入口。
- [x] 2.4 将 Dodge 候选构建迁入 Dodge request candidate builder。
- [x] 2.5 将 TurnBack 候选构建迁入 TurnBack request candidate builder。
- [x] 2.6 修改 gate 主流程，只遍历 candidate builders。
- [x] 2.7 修改 gate 选择逻辑，支持 0..N 个 accepted request。
- [x] 2.8 保持 `ActionInterruptArbiter` 为唯一准入裁决入口。
- [x] 2.9 降级旧 `BuildDodgeRequestFact`，gate 正式路径不再调用。
- [x] 2.10 降级旧 `BuildTurnBackRequestFact`，gate 正式路径不再调用。
- [x] 2.11 增加静态测试：gate 源码不包含 `BuildDodgeRequestFact`。
- [x] 2.12 增加静态测试：gate 源码不包含 `BuildTurnBackRequestFact`。
- [x] 2.13 增加静态测试：gate 主流程不包含 `InputRequestKind.Dodge` 或 `InputRequestKind.TurnBack` 分支。

## 3. Action motion spec / resolver seam
- [x] 3.1 定义通用 action motion profile/spec 字段，覆盖 duration、distance、rotate、run latch。
- [x] 3.2 将 Dodge 配置解析迁到 Dodge motion spec adapter。
- [x] 3.3 修改 `ActionMotionResolveInput`，移除 `DodgeActionConfig` 字段。
- [x] 3.4 修改 `ActionMotionResolver`，只读取通用 spec。
- [x] 3.5 保持 `ActionMovementCommand` 执行出口不变。
- [x] 3.6 增加静态测试：`ActionMotionResolver` 不引用 `DodgeActionConfig`。
- [x] 3.7 增加静态测试：`ActionMotionResolver` 不引用 `ActionStateIds.Dodge`。
- [x] 3.8 覆盖 Dodge Directional motion result 与迁移前一致。
- [x] 3.9 覆盖 Dodge Backstep motion result 与迁移前一致。
- [x] 3.10 增加测试：新增占位动作 motion spec 不需要修改 resolver 分支。

## 4. Runner state payload seam
- [x] 4.1 定义通用 state payload 模型或等价可恢复 payload carrier。
- [x] 4.2 将 Action locked direction 写入通用 payload。
- [x] 4.3 将 TurnBack locked direction 写入通用 payload。
- [x] 4.4 将 TurnBack entry basis forward 写入通用 payload。
- [x] 4.5 修改 lifecycle/output 输入，从 payload 读取所需方向。
- [x] 4.6 修改 restore state，保存通用 payload。
- [x] 4.7 删除 runner 字段 `actionWorldDirection` 的正式用途。
- [x] 4.8 删除 runner 字段 `turnBackWorldDirection` 的正式用途。
- [x] 4.9 删除 runner 字段 `turnBackEntryBasisForward` 的正式用途。
- [x] 4.10 增加静态测试：runner 不包含 `CharacterStateIds.TurnBack` 特判。
- [x] 4.11 增加静态测试：runner 不包含 `turnBackWorldDirection` 字段。
- [x] 4.12 增加 rollback 测试：restore 后 Dodge 和 TurnBack 行为一致。

## 5. Snapshot / FullBody view seam
- [x] 5.1 定义 `FullBodyStateView` 或等价外围解释模型。
- [x] 5.2 从 snapshot 构建 FullBody view。
- [x] 5.3 将 FullBody diagnostics 迁到 view。
- [x] 5.4 将 Action facts 写入迁到 view。
- [x] 5.5 将 Locomotion adapter 中的 phase 读取迁到 view。
- [x] 5.6 将 rollback/debug 字符串迁到 view。
- [x] 5.7 删除 snapshot 上 `IsAction` 的正式用途。
- [x] 5.8 删除 snapshot 上 `IsLocomotion` 的正式用途。
- [x] 5.9 删除 snapshot 上 `Owner` 的正式用途。
- [x] 5.10 删除 snapshot 上 `ActionState` 的正式用途。
- [x] 5.11 删除 snapshot 上 `LocomotionPhase` 的正式用途。
- [x] 5.12 增加静态测试：`CharacterStateMachineSnapshot` 不包含 FullBody/Locomotion/Action 派生解释。

## 6. 验证
- [x] 6.1 运行 `openspec validate refactor-fullbody-action-boundaries --strict --no-interactive`。
- [x] 6.2 运行 `dotnet build .\3cDemo\Client\3C_Client\Assembly-CSharp.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [x] 6.3 运行 `dotnet build .\3cDemo\Client\3C_Client\Assembly-CSharp-Editor.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [x] 6.4 运行 `Tests.Editor.UnifiedCharacterStateMachineTests` 相关定向测试。
- [x] 6.5 运行 `ThirdPersonSimulation.Tests.FullBodyRollbackReplayTests` 相关定向测试。
- [x] 6.6 运行 `Tests.Editor.FullBodyConfigAuthoringLayoutTests` 相关定向测试。
- [x] 6.7 运行 `ThirdPersonSimulation.Tests.LocalRollbackSynctestFoundationTests` 相关定向测试。
- [x] 6.8 运行 `node .\.gitnexus\run.cjs detect_changes` 并记录当前工作树影响范围。
