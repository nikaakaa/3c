# ZZZ Corin RushAttack Ability / FSM 对照

本文登记 RushAttack 独立 Ability 的当前正式落盘结果。Timeline 数据见 [zzz-corin-rush-timeline-map.md](zzz-corin-rush-timeline-map.md)。

## 正式入口

| 对象 | 资产 / 代码 |
|---|---|
| Admission Profile | `Assets/Configs/Character/Corin/Pipeline/Abilities/AdmissionProfiles/CorinRushAdmissionProfile.asset` |
| Ability Definition | `Assets/Configs/Character/Corin/Pipeline/Abilities/CorinRushAttackGameplayAbilityDefinition.asset` |
| Input Profile | `Assets/Configs/Character/Corin/Pipeline/Input/CorinCharacterInputProfile.asset` |
| Ability authoring | `Assets/GameScripts/Main/Editor/CharacterPipeline/Authoring/CodeGeneration/Generated/CorinRushAttackGameplayAbilityAuthoringCode` |
| Timeline authoring | `Assets/GameScripts/Main/Editor/CharacterPipeline/Authoring/CodeGeneration/Generated/CorinAttackRush*TimelineAuthoringCode` |

`CorinCharacterPipelineDefinition` 已正式登记 Rush Ability Grant、Rush Fixed/Float32 执行数据和三条 Rush Timeline。Definition 的 Control Motion Timeline catalog 当前共 13 条，没有强化 Rush 的残留引用。

## 输入合同

- 移动中或闪避接招窗口内的 `Attack` 请求由 Control 提交为 `RushAttack`，使用现有普攻输入，不设独立 `Rush` 请求。
- `MoveAxis` 有方向输入时维持 `Attack_Rush`；`RushRelease` 打开后松开方向输入进入爆发段。普攻按住值 `AttackHeld` 不决定 Rush 的长度。
- 爆发段的 `RushAttackHandoff` 窗口内再次收到 `Attack` 请求时，Control 以当前 Rush 实例为替换源激活普通攻击的 `Attack4` 入口。

## 状态与转移

正式状态只有三段：

`Attack_Rush -> Attack_Rush_Explode -> Attack_Rush_End -> Exit`

| 来源 | 目标 | 条件 |
|---|---|---|
| Entry | `Attack_Rush` | Control 已从移动攻击或闪避接招提交 RushAttack |
| `Attack_Rush` | `Attack_Rush_Explode` | `RushRelease` 窗口打开且 `MoveAxis` 无方向输入 |
| `Attack_Rush` | `Attack_Rush_Explode` | 当前 Timeline 完成 |
| `Attack_Rush_Explode` | `Attack_Rush_End` | 当前 Timeline 完成 |
| `Attack_Rush_End` | Exit | 当前 Timeline 完成 |

`RushAttackHandoff` 内的普通攻击由 Control 走动作替换，不是 Rush FSM 内部的 Exit 边。第 44 帧后的 `RushMoveExit` 打开时，若仍有移动输入，Control 可结束 Rush 并继续跑动。每个状态的 State Body 只播放对应 shared Timeline，播放模式为 Once。旧的 `RushSelected`、`RushEnhanceSelected`、`RushHoldReleased`、`RushSawExplode` 和五段强化 Rush 状态已经删除。

## 结束规则

Ability 的宿主结束规则为：

- `AbortRequested -> Abort`
- `InterruptRequested -> Interrupt`
- `ExecutionCompleted -> Complete`
- `ActionWindowClosed(RushMoveExit) -> Cancel`

释放由状态机条件负责，普通攻击交接由 Control 的替换请求负责；移动退出通过 `RushMoveExit` 结束规则收口。

## Effect 依赖

Rush Ability 只登记三个普通 Rush AttackProperty GameplayEffect：

- `Corin_Attack_Rush_AttackProperty_01_01`
- `Corin_Attack_Rush_AttackProperty_01_02`
- `Corin_Attack_Rush_AttackProperty_02`

五个强化 Rush GameplayEffect 已从 `CorinCharacterGameplayEffectProfile` 移除并删除。当前 Rush Timeline 没有 AttackProperty 应用节点或 ActionCue；这三个 Effect 在本链路中是发布依赖，不应把它们解释成已经发生的命中事件。

## 发布合同

正式 Republish 同时生成 `RushAttack.FixedData.asset` 和 `RushAttack.Float32Data.asset`。两份产物的 `SourceRevision`、`SemanticHash`、`AbilityGuid` 与 `ContentIdentity` 必须一致；Attack、DodgeBack、DodgeForward 也遵循同一合同。
