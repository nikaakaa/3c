# Corin RushAttack 正式链路

## Why

`RushAttack` 已完成 8 个状态的 Motion 对账：7 个独立 Clip 全部有导入证据，`Attack_Rush_Enhance` 复用 `Attack_Rush_Enhance_Start`，`Attack_Rush_Enhance_End` 复用 `Attack_Rush_Explode`。当前缺口不是素材，而是没有正式 Rush Ability、状态机、Timeline、命中 cue 和 Pose 播放链。

Rush 与 NormalAttack 是不同业务链：Rush 有独立入口、强化分支、Loop、Explode 与宿主中断路由。把 8 个状态塞进 NormalAttack 五段 Timeline 会破坏状态本地时间、分支语义和后续 BranchAttack 评估，因此必须建立独立正式链路。

## What Changes

- 新增独立 `CorinRushAttackGameplayAbilityDefinition`，承载 ZZZ `RushAttack` 的 8 个业务状态；不复用、不追加 NormalAttack 的五段 Timeline。
- 为 8 个状态各建一个正式 Timeline producer；7 个独立 Motion 按控制器实际引用复用，`StateId` 和 `BranchId` 保留状态与普通/强化分支身份。
- Timeline 起止采用 ZZZ 源状态总帧：Rush=70、Rush_Explode=70、Rush_End=80、Enhance_Start=44、Enhance_Loop=86、Enhance_End=70、Enhance_Explode=70、Enhance_Explode_End=118。
- 93 个 AttackProperty cue 挂在对应状态 Timeline 的 Logic ActionCue 轨：Rush 32、Rush_Explode 1、Enhance_Start 16、Enhance_Loop 43、Enhance_Explode 1。原始 `CueId` 保留，属性/碰撞 payload 仍归 GameplayEffect / Ability。
- 状态边界使用 dump 中的 `m_ExitTime`、`FrameCount`、Bool/Trigger 条件；`special_*` 不抄成图内状态，转给 Hit、Evade、Aid、Switch 等正式 owner。
- Pose 侧继续走既有 Timeline Action playback / FullBodyAction Slot；不把 Rush 状态扩进 Locomotion PoseStateMachine，也不新增第二条 Pose 链。

## Capabilities

### Modified Capabilities

- `character-state-timeline-authoring-loop`：新增 Corin RushAttack 正式 FSM/Timeline/ActionCue/Pose 接线合同，明确 8 状态、7 Motion、状态本地 cue 与 NormalAttack 的边界。

## Impact

- 新增 Rush Ability/Timeline authoring 与生成产物；不改 NormalAttack 已收口资产。
- Rush 的 93 个 cue 使用现有 `Corin_Attack_Rush*` GameplayEffect Profile；不改 Timeline runtime。
- Pose 继续消费 committed Action playback；不新增 Pose 状态机状态和第二条动画链。
- Dodge 入口、Normal04 回归、Hit/Evade/Aid/Switch owner 必须通过现有 Action admission / Control owner 对接；缺失 owner 时显式失败，不伪造路由。
