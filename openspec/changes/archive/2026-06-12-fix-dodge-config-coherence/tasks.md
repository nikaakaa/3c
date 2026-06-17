## 1. 代码层修改
- [x] 1.1 `CharacterStateMachineDefinition.CreateDefault()`: 移除本地 `const float DefaultDodgeDuration`，Dodge transition 的 `StateElapsedAtLeast` 改为引用 `DodgeActionConfig.Default.DirectionalDuration`
- [x] 1.2 `CharacterStateMachineDefinition.CreateDefault()`: 给 `Dodge → Dodge` transition 增加 `StateElapsedAtLeast(DodgeActionConfig.Default.DirectionalDuration)` 条件
- [x] 1.3 删除 `DodgeActionPolicies.cs` 中的 `CreateDefaultFromNone` 和 `CreateDefaultFromDodge` 方法；如果文件为空静态类则删除整个文件和 `.meta`

## 2. SO 资产同步
- [x] 2.1 更新 `DefaultCharacterStateMachine.asset`: 给 `Dodge → Dodge` transition 增加 `StateElapsedAtLeast` condition，minSeconds 设为 0.35
- [x] 2.2 更新 `DefaultDodgeInterruptPolicySet.asset`: `Action.None → Action.Dodge` 和 `Action.Dodge → Action.Dodge` 策略的 `timingRule` 从 `Always`(0) 改为 `AfterElapsedTime`(1)，`windowStart` 设为 0.35

## 3. 测试更新
- [x] 3.1 更新 `UnifiedCharacterStateMachineTests`: 增加测试验证 `Dodge → Dodge` transition 在 stateTime < 0.35s 时不成立
- [x] 3.2 更新 `UnifiedCharacterStateMachineTests`: 增加测试验证 `Dodge → Dodge` transition 在 stateTime >= 0.35s 时成立
- [x] 3.3 更新 `ActionInterruptArbiterTests` 或 `ActionInterruptPolicyDataTests`: 增加测试验证 Dodge→Dodge 策略有 AfterElapsedTime 保护
- [x] 3.4 静态确认 `DodgeActionPolicies` 的删除无编译引用断裂（全量编译通过）

## 4. 校验和回归
- [x] 4.1 运行 Unity EditMode 测试全量（`Unity.TestProtocol`），确认无回归
- [x] 4.2 运行 `openspec validate fix-dodge-config-coherence --strict --no-interactive` 通过
