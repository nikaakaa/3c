# Change: 修复 Flash 配置一致性问题

## Why
上次连续 Dodge 链路修复对齐了策略层、状态机层和动画层三层行为。在验证过程中发现多处配置不一致和防御性缺失，包括 Dodge duration 值在多个源之间不同步、Dodge→Dodge transition 缺少时间下限保护、以及死代码残余。这些问题独立于已完成的修复，需要单独处理。

## What Changes
- 把 `CharacterStateMachineDefinition.CreateDefault()` 中的 Dodge transition 的 `StateElapsedAtLeast` 时间源从本地 `const DefaultDodgeDuration` 改为从 `DodgeActionConfig` 读取，消除 `0.35f` 的重复定义
- 给 `Dodge → Dodge` transition 增加 `StateElapsedAtLeast` 时间下限，对应 Dodge 策略的时间窗口保护
- 移除 `DodgeActionPolicies` 中无人调用的 `CreateDefaultFromDodge` 和 `CreateDefaultFromNone` 死代码
- 同步更新 `DefaultCharacterStateMachine.asset` SO 资产和 `DefaultDodgeInterruptPolicySet.asset` 策略资产
- 针对 Dodge 退出条件不对称（Dodge→MoveLoop vs Dodge→Idle）保留现状并记录为已知设计决策，等后续单独讨论

## Impact
- Affected specs:
  - `locomotion-state-graph-config`
  - `action-interrupt-policy-data`
- Affected code:
  - `Assets/Scripts/Character/StateMachine/Model/CharacterStateMachineDefinition.cs`
  - `Assets/Scripts/Character/Action/Solver/DodgeActionPolicies.cs`
  - `Assets/Configs/3C/Statemachine/DefaultCharacterStateMachine.asset`
  - `Assets/Configs/3C/Action/DefaultDodgeInterruptPolicySet.asset`
  - `Assets/Tests/Editor/` 相关测试文件
- Not in scope:
  - ActionStateId vs CharacterStateId 两套 ID 统一（需要架构讨论，另开提案）
  - Dodge 退出条件不对称（可能是有意设计，先记录为 open question）
  - 新增第二套 Dodge 策略或打断系统
