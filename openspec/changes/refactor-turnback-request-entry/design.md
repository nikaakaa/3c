## Context
`add-configurable-state-interrupt-windows` 已经把 TurnBack 纳入状态请求仲裁、timeline window 和 motion 输出链路。当前需要收口的是入口权威：`LocomotionTurnBackIntent` 仍然有价值，因为它表达 `MoveStart` 或 `MoveLoop` 中的反向输入、朝向角度、候选方向和短时间保持窗口；但 intent 不应绕过仲裁直接驱动状态机 transition。

## Goals / Non-Goals
- Goals:
  - 保留移动侧 `LocomotionTurnBackIntent` 作为候选事实。
  - 让 `ActionInterruptArbiter` 接受后的 TurnBack request fact 成为从 `MoveStart` 或 `MoveLoop` 进入 TurnBack 的唯一权威事实。
  - 删除或降级默认 TurnBack transition 对 `MoveTurnBackRequested` 的依赖。
  - 用测试证明 rejected TurnBack request 不会进入 TurnBack。
- Non-Goals:
  - 不重做 timeline policy 数据模型。
  - 不新增 TurnBack 专用仲裁器。
  - 不改 Humanoid 资源和 Humanoid 验证链路。
  - 不删除诊断日志。

## Decisions
- Decision: `LocomotionTurnBackIntent` 保留在 locomotion facts 中，但只作为 `FullBodyActionInterruptGate.BuildTurnBackRequestFact` 的输入。
- Decision: 默认 `MoveStart -> TurnBack` 与 `MoveLoop -> TurnBack` transition 使用 `HasInputRequest(InputRequestKind.TurnBack)` 或等价 accepted request fact 条件。
- Decision: `MoveTurnBackRequested` 可在迁移期保留为模型/测试辅助或诊断条件，但不得被默认 TurnBack 进入路径使用。
- Decision: TurnBack 方向优先来自 accepted request fact；如缺失 accepted fact，默认 transition 不应进入 TurnBack。

## Risks / Trade-offs
- 风险: 现有测试可能依赖 `turnBackIntent` 直接触发 TurnBack。
  - Mitigation: 测试改为显式提供 accepted TurnBack request，并新增 rejected request 不切状态测试。
- 风险: 旧条件枚举直接删除会影响序列化资产。
  - Mitigation: 第一版优先从默认配置和 evaluator 使用点降级，不强制删除枚举值，避免资产迁移风险。

## Migration Plan
1. 查找默认状态机配置和测试里所有 `MoveTurnBackRequested` 用法。
2. 将默认 `MoveStart -> TurnBack` 与 `MoveLoop -> TurnBack` 入口改为 accepted TurnBack request fact。
3. 保留 intent 捕获和日志，但确保 intent 不直接决定状态切换。
4. 更新测试覆盖 accepted、rejected、intent-only 三种路径。

## Open Questions
- 无。当前需求已明确为统一路径：intent 保留为候选，accepted request fact 作为唯一进入权威。
