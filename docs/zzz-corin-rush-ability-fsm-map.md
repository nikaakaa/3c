# ZZZ Corin RushAttack Ability / FSM 对照

本文登记 RushAttack 独立 Ability 的当前正式落盘结果。Timeline 数据见 [zzz-corin-rush-timeline-map.md](zzz-corin-rush-timeline-map.md)。

## 正式入口

| 对象 | 资产 / 代码 |
|---|---|
| Admission Profile | `Assets/Configs/Character/Corin/Pipeline/Abilities/AdmissionProfiles/CorinRushAdmissionProfile.asset` |
| Ability Definition | `Assets/Configs/Character/Corin/Pipeline/Abilities/CorinRushAttackGameplayAbilityDefinition.asset` |
| Profile authoring | `Assets/GameScripts/Main/Editor/CharacterPipeline/Authoring/CodeGeneration/Generated/CorinRushAdmissionProfileAuthoringCode` |
| Ability authoring | `Assets/GameScripts/Main/Editor/CharacterPipeline/Authoring/CodeGeneration/Generated/CorinRushAttackGameplayAbilityAuthoringCode` |

Admission Profile 使用 `Rush` tag、单实例；Cancel tags 是 `Attack`、`Dodge`、`Rush`。Profile 尚未接入 `CorinCharacterPipelineDefinition.m_AbilityGrants`，因此当前不会形成 fake Dodge 自动激活入口。

## 状态与 Timeline

8 个业务状态是 `Attack_Rush`、`Attack_Rush_Explode`、`Attack_Rush_End`、`Attack_Rush_Enhance`、`Attack_Rush_Enhance_Loop`、`Attack_Rush_Enhance_End`、`Attack_Rush_Enhance_Explode`、`Attack_Rush_Enhance_Explode_End`。

每个状态有独立 State Body；State Body 只做一件事：播放各自的 shared Timeline。7 个 Motion 复用关系由 Timeline 层保留，Ability 层不合并状态身份。

## 当前 FSM 转移

- 入口只接受 `RushSelected` 或 `RushEnhanceSelected`，两者默认 false；Dodge/Control owner 提供真实选择前，Rush Ability 不会被伪造启动。
- `Attack_Rush -> Attack_Rush_Explode` 已有 terminal 转移，另有 `RushHoldReleased`、`RushSawExplode` 显式条件位；条件数据源未接入前保持 false，不猜测默认值。
- `Attack_Rush_Explode -> Attack_Rush_End`、`Attack_Rush_Enhance -> Enhance_Loop`、`Enhance_Loop` 重入/收口、`Enhance_End -> Rush_End`、`Enhance_Explode -> Explode_End` 使用 terminal 转移。
- `Attack_Rush_End`、`Attack_Rush_Enhance_Explode_End` 到 Exit 使用 terminal 收口。
- 回归 `Attack_Normal_04` 当前表达为 `RushNormalHandoff` 条件到 Exit；`Attack`、`Evade`、`Aid`、`Switch` 等 `special_*` owner 未接前不伪造目标。

## Effect 依赖

Rush Ability 已登记 8 个唯一 Rush AttackProperty GameplayEffect：普通侧 `_01_01`、`_01_02`、`_02`，强化侧 `_01_01`、`_01_02`、`_01_03`、`_01_04`、`_02`。Timeline cue 只携带身份事件；伤害、碰撞、相机 payload 仍归对应 owner。

## 复验

同 Trace `369327502f7a4add8a21a19a7713d24d` 在新增 Rush Admission Profile 与 Ability 后复跑：`matched:1121`，基线 proof 为 `20260919-085836-293-d0cfb77806a54ee5a8263e90763d42e3.json`，本次 proof 为 `20260919-100603-789-32d845b6af3d44edbd227f361fc7395c.json`。该结果证明新资产不破坏现有 NormalAttack Replay；Rush 主动激活链仍在等待 Dodge/Control owner。
