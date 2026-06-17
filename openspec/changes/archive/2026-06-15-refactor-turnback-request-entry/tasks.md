# 收口 TurnBack 请求入口任务

## 1. 现状确认
- [x] 1.1 读取本 change 的 `proposal.md`、`design.md` 和 spec delta。
- [x] 1.2 读取 `add-configurable-state-interrupt-windows` 中 TurnBack request、timeline 和状态机相关 spec。
- [x] 1.3 搜索 `MoveTurnBackRequested`、`LocomotionTurnBackIntent`、`InputRequestKind.TurnBack` 的运行时和测试引用。
- [x] 1.4 确认不新增 TurnBack 专用仲裁器、不新增 fallback 配置、不修改 Humanoid。

## 2. TurnBack 入口收口
- [x] 2.1 保留 `LocomotionTurnBackIntent` 作为移动侧候选事实。
- [x] 2.2 确认 TurnBack request fact 只由状态请求仲裁 accepted 后生成。
- [x] 2.3 将默认 `MoveStart -> TurnBack` 和 `MoveLoop -> TurnBack` transition 改为 `HasInputRequest(InputRequestKind.TurnBack)`（kind:3/requestKind:4）。
- [x] 2.4 确认 `MoveTurnBackRequested` 不再作为默认 TurnBack 进入 transition 的条件。
- [x] 2.5 确认 TurnBack 方向来源优先使用 accepted request fact 的 world direction。
- [x] 2.6 确认 intent-only 输入不会进入 TurnBack。
- [x] 2.7 为 `MoveStart` 增加正式 `turnback-enter` timeline window 和 interrupt policy。

## 3. 边界和诊断
- [x] 3.1 保留 TurnBack intent 捕获、拒绝和消费诊断日志。
- [x] 3.2 增加或调整诊断，让日志能区分 intent captured、request accepted/rejected、state entered。
- [x] 3.3 检查 transition evaluator 不读取 priority、resistance 或策略 SO。
- [x] 3.4 检查 Locomotion 不读取 `ActionInterruptPolicySetSO` 或等价策略资产。

## 4. 自动测试
- [x] 4.1 增加 accepted TurnBack request 进入 TurnBack 测试（`MoveLoopAcceptedTurnBackRequestEntersTurnBack`、`MoveStartAcceptedTurnBackRequestEntersTurnBack`）。
- [x] 4.2 增加 intent-only 不进入 TurnBack 测试（`MoveLoopIntentOnlyDoesNotEnterTurnBack`）。
- [x] 4.3 增加 rejected TurnBack request 不进入 TurnBack 测试（`RejectedTurnBackRequestDoesNotEnterTurnBack`）。
- [x] 4.4 更新既有 TurnBack MoveLoop、MoveStart、MoveStop、Idle 测试，确保 `MoveStart`/`MoveLoop` 验证 accepted request fact，`MoveStop`/`Idle` 不通过 intent-only 直进。
- [x] 4.5 增加静态边界测试，确认默认 TurnBack 入口不使用 `MoveTurnBackRequested`（`ConfiguredTurnBackEntryOnlyConsumesAcceptedTurnBackInputFact`）。
- [x] 4.6 增加默认 interrupt policy 静态测试，确认 `MoveStart` 和 `MoveLoop` 都通过 `turnback-enter`。

## 5. 验证
- [x] 5.1 运行 `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal` — 0 error。
- [x] 5.2 运行 `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` — 3 个既有 warning，0 error。
- [x] 5.3 使用 Unity Test Runner 定向运行本次影响的 `UnifiedCharacterStateMachineTests` — 10/10 通过。
- [x] 5.4 使用 Unity Test Runner 定向运行 ActionInterrupt 相关 EditMode 测试 — 44/44 通过。
- [x] 5.5 使用 Unity Test Runner 定向运行 TurnBack motion/profile 相关 EditMode 测试 — 4/4 通过。
- [x] 5.6 读取 Unity Console，确认 TurnBack、ActionInterrupt、StateTimeline 相关 error 为 0。
- [x] 5.7 运行 `openspec validate refactor-turnback-request-entry --strict --no-interactive` — valid。
- [x] 5.8 不运行 Unity batchmode。
- [x] 5.9 运行新增/调整后的 `UnifiedCharacterStateMachineTests` 和 `ActionInterruptPolicyDataTests` — 11/11 通过。

## 6. Sandbox 手动验证（需用户在 Unity Editor 中操作）
- [x] 6.1 打开 Sandbox 场景并使用 Generic 可琳。
- [x] 6.2 启用 Locomotion、FullBody、ActionInterrupt 或等价诊断日志。
- [x] 6.3 按 W 后在 MoveStart 或 MoveLoop 切 S，确认 TurnBack 请求先 accepted 再进入 TurnBack。
- [x] 6.4 在 Walk、MoveStop、Idle 反向输入，确认不会进入 TurnBack。
- [x] 6.5 制造 TurnBack request rejected 条件，确认 intent captured 但状态不进入 TurnBack。
- [x] 6.6 TurnBack 进入后确认 motion、input lock 和 exit window 行为不回退。

## 7. 收尾
- [x] 7.1 检查没有新增 TurnBack 专用仲裁器。
- [x] 7.2 检查没有新增运行时 fallback 配置加载。
- [x] 7.3 检查没有让 intent、Animator 或 Animancer 直接决定 TurnBack 进入。
- [x] 7.4 全部任务真实完成后再将 checklist 标为 `- [x]`。
