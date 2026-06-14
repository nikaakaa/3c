# Change: 修正 Backstep Dodge 过早回 Idle

## Why
当前 `Action.Dodge.Backstep` 的逻辑位移时长和状态退出时机都使用 0.35 秒，导致可琳后闪动画尚未完成蹲下到起身恢复过程时，统一状态机已经退出到 `Idle` 并清空动作动画。该行为让表现被过早打断，也暴露出动作位移时长和动作恢复退出事实混用的问题。

## What Changes
- 为 Action 动画播放增加只读结束事实，使状态机可以通过纯数据判断动作恢复是否可退出。
- 将 Backstep Dodge 无输入回 Idle 的退出条件从单纯 `StateElapsedAtLeast(0.35)` 改为等待动作恢复退出事实。
- 保持 Backstep 恢复段可被重新移动输入提前打断回移动阶段，不要求等待完整后闪动画结束。
- 保持 Backstep 位移参数仍为短时动作位移，不把动画总长写回位移 duration。
- 保持 Directional Dodge 现有 0.35 秒完成后接移动的行为。
- 不实现完整 Timeline 编辑器、cancel window、hitbox window、Root Motion 权威或第二套状态机。

## Impact
- Affected specs: `animation-phase-timeline-facts`, `locomotion-state-graph-config`
- Affected code: `CharacterRuntimeBlackboard`, `ActionAnimationAnimancerPresenter`, `PlayerFullBodyActionController`, `CharacterStateTransitionEvaluator`, `CharacterStateMachineDefinition`, `DefaultCharacterStateMachine.asset`, `UnifiedCharacterStateMachineTests`
