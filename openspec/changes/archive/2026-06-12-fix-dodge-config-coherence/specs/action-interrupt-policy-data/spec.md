## MODIFIED Requirements
### Requirement: 默认 Dodge 打断策略
系统 MUST 为默认可琳 FullBody Dodge 提供可配置的进入策略，表达从空 Action 或当前可允许状态进入 `Action.Dodge` 的最小优先级、时间规则、force 和抗性语义。`Action.None → Action.Dodge` 和 `Action.Dodge → Action.Dodge` 两条策略 SHALL 使用 `AfterElapsedTime` timing rule，时间起点 SHALL 为 `DodgeActionConfig.DirectionalDuration`（如 0.35s）。

#### Scenario: 默认策略允许合法 Dodge
- **GIVEN** 当前动作状态为空 Action 或等价可允许状态
- **AND** Dodge 请求优先级满足策略最小优先级
- **AND** 当前 resistance 不阻挡请求
- **AND** 满足策略定义的时间规则
- **WHEN** FullBody Action 请求门面执行仲裁
- **THEN** `ActionInterruptArbiter` MUST 返回 accepted decision

#### Scenario: 默认策略拒绝低优先级 Dodge
- **GIVEN** 当前动作状态匹配默认 Dodge 策略
- **AND** Dodge 请求优先级低于策略最小优先级
- **WHEN** FullBody Action 请求门面执行仲裁
- **THEN** `ActionInterruptArbiter` MUST 返回 rejected decision
- **AND** 拒绝原因 MUST 表示优先级不足

#### Scenario: Dodge→Dodge 连闪策略有时间保护
- **GIVEN** 当前动作状态为 `Action.Dodge`
- **AND** elapsed time 小于 `DodgeActionConfig.DirectionalDuration`
- **WHEN** 收到新的 Dodge 请求
- **THEN** `Dodge → Dodge` 策略 MUST NOT 放行
- **AND** 拒绝原因 MUST 表示 timing 不满足

## REMOVED Requirements
### Requirement: DodgeActionPolicies 静态工厂方法
**Reason**: `DodgeActionPolicies.CreateDefaultFromNone` 和 `CreateDefaultFromDodge` 为无人调用的死代码。策略编译链路全走 `ActionInterruptPolicySetSO.CompilePolicies()`，代码辅助方法无消费点且不参与策略数据流。

**Migration**: 直接删除 `Assets/Scripts/Character/Action/Solver/DodgeActionPolicies.cs` 中的两个方法。如果该文件仅剩空类型，则删除整个文件和 `.meta`。运行时和生产配置不受影响，策略始终来自 SO 资产。
