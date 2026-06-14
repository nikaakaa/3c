## Context
当前 `CharacterStateMachineDefinition.CreateDefault()` 内部用 `const float DefaultDodgeDuration = 0.35f` 定义 Dodge transition 的时间下限。但 `DodgeActionConfig`（运行时配置）和 `DodgeActionConfigSO`（ScriptableObject 配置）各自维护了独立的 duration 值。如果策划在 SO 中修改 duration（如把 DirectionalDuration 从 0.35 改为 0.5），状态机 transition 的 `StateElapsedAtLeast` 仍然使用 `const 0.35f`，导致状态机比动作位移更早认为可以退出，造成动画和位移不同步。

此外，`Dodge → Dodge` transition 尚无任何时间下限保护，状态机层面对连续 Dodge 没有任何防御。虽然输入边沿触发当前挡住了同帧双请求，但策略层也没有配置窗口下限（`timingRule: Always`），属于纵深不足。

## Goals / Non-Goals
- Goals:
  - 消除 `DefaultDodgeDuration` const 与 `DodgeActionConfig` duration 的重复定义
  - `CreateDefault()` 中的 Dodge transition 的 `StateElapsedAtLeast` 从 `DodgeActionConfig.DirectionalDuration` 获取
  - 给 `Dodge → Dodge` transition 增加时间下限保护
  - 给 `Dodge → Dodge` 策略增加最小时间窗口，与状态机层一致
  - 移除 `DodgeActionPolicies.CreateDefaultFromDodge` 和 `CreateDefaultFromNone` 死代码
  - 同步 `DefaultCharacterStateMachine.asset` 和 `DefaultDodgeInterruptPolicySet.asset`
- Non-Goals:
  - 不消除 `DodgeActionConfigSO.serializedField` 默认值与 `DodgeActionConfig.Default` 之间的重复（合理冗余，SO 默认值是 Inspector 默认值）
  - 不统一 ActionStateId/CharacterStateId 两套 ID（需架构讨论，另开提案）
  - 不在本变更中决定 Dodge→MoveLoop vs Dodge→Idle 退出条件不对称的最终方案

## Decisions
- Decision: `CreateDefault()` 直接取 `DodgeActionConfig.Default.DirectionalDuration` 作为 transition 时间下限。
  - Reason: 不引入新的依赖注入或接口，用现有 `DodgeActionConfig.Default` 作为单一真值源。`DirectionalDuration` 是 Dodge 标准时长（0.35s），已有的 Dodge→MoveLoop 和 Dodge→Idle 都使用它。
  - Alternative: 用一个新接口注入 duration → 过度工程，本次窄修复不需要。
- Decision: `Dodge → Dodge` transition 的 `StateElapsedAtLeast` 使用与 MoveLoop/Idle 退出相同的 duration（`DirectionalDuration`）。
  - Reason: 连闪的最小间隔应该和基础 Dodge 时长一致，保证前一次 Dodge 的核心位移窗口结束后才允许下一次。
- Decision: `Dodge → Dodge` 策略层的时间规则从 `Always` 改为 `AfterElapsedTime`，用 `DirectionalDuration` 作为窗口起点。
  - Reason: 策略层和状态机层双层保护，确保即使输入系统产生了极短间隔的两次请求，也会被策略层的 timing 规则拦截。
- Decision: 直接删除 `DodgeActionPolicies.CreateDefaultFromDodge` 和 `CreateDefaultFromNone`。
  - Reason: 策略编译链路全走 SO 资产，代码辅助方法无人调用且无用途。
- Decision: 退出条件不对称（Dodge→MoveLoop 不等 ActionCanExit vs Dodge→Idle 等 ActionCanExit）保留现有行为。
  - Reason: 移动输入打断 Dodge 可能是期望设计——玩家推摇杆意味着想恢复控制，不等动画播完。留作 open question 后续讨论。

## Risks / Trade-offs
- Risk: 策划修改 SO 中的 `directionalDuration` 后，`CreateDefault()` 中的 `DodgeActionConfig.Default` 不会自动跟随。
  - Mitigation: 这是代码层的 fallback；生产环境使用 SO 资产传入定义，`Default` 仅在不传配置时生效。`CreateDefault()` 是编程默认，SO 是设计默认，两层的值同步保持一致即可。如果后续发现不一致风险大，可在 `CharacterStateMachineDefinitionSO` 层面强制校验。
- Risk: 删除 `DodgeActionPolicies` 中的方法后，有其他未追踪的反射调用或编辑器脚本依赖它们。
  - Mitigation: 已全量搜索确认无调用点；删除后如果 CI 报错可快速恢复。

## Migration Plan
1. 修改 `CharacterStateMachineDefinition.CreateDefault()` 中的 Dodge transition 条件
2. 删除 `DodgeActionPolicies` 中的死代码
3. 同步更新 `DefaultCharacterStateMachine.asset`
4. 同步更新 `DefaultDodgeInterruptPolicySet.asset`
5. 更新 unit test 断言（如果有对 const 值的硬编码断言）
6. 运行 `openspec validate --strict --no-interactive`

## Open Questions
- Dodge→MoveLoop（有输入）不需要 `ActionCanExit`，Dodge→Idle（无输入）需要 `ActionCanExit`——这个不对称是否是有意设计？当前记录为 open question，待后续确认后决定是否需要单独调整。
