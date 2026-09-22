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

- `Rush` 是一次性动作请求，由输入动作的 `WasPressedThisFrame()` 产生；Ability 入口消费该请求。
- `RushHeld` 是同一输入动作的连续布尔值，由 `IsPressed()` 每帧锁存；它只决定蓄力段何时释放，不承担 Ability 激活。
- `Attack` 保留为普通攻击请求；Rush 爆发段只读取它，不提前消费，退出 Rush 后由普通攻击链继续处理。

## 状态与转移

正式状态只有三段：

`Attack_Rush -> Attack_Rush_Explode -> Attack_Rush_End -> Exit`

| 来源 | 目标 | 条件 |
|---|---|---|
| Entry | `Attack_Rush` | 存在 `Rush` 请求 |
| `Attack_Rush` | `Attack_Rush_Explode` | `RushRelease` 窗口打开且 `RushHeld=false` |
| `Attack_Rush` | `Attack_Rush_Explode` | 当前 Timeline 完成 |
| `Attack_Rush_Explode` | Exit | `RushAttackHandoff` 窗口打开且存在 `Attack` 请求 |
| `Attack_Rush_Explode` | `Attack_Rush_End` | 当前 Timeline 完成 |
| `Attack_Rush_End` | Exit | 当前 Timeline 完成 |

每个状态的 State Body 只播放对应 shared Timeline，播放模式为 Once。旧的 `RushSelected`、`RushEnhanceSelected`、`RushHoldReleased`、`RushSawExplode` 和五段强化 Rush 状态已经删除。

## 结束规则

Ability 只保留三条宿主结束规则：

- `AbortRequested -> Abort`
- `InterruptRequested -> Interrupt`
- `ExecutionCompleted -> Complete`

释放和普通攻击交接由状态机条件负责，不再复制成 Ability EndRule。

## Effect 依赖

Rush Ability 只登记三个普通 Rush AttackProperty GameplayEffect：

- `Corin_Attack_Rush_AttackProperty_01_01`
- `Corin_Attack_Rush_AttackProperty_01_02`
- `Corin_Attack_Rush_AttackProperty_02`

五个强化 Rush GameplayEffect 已从 `CorinCharacterGameplayEffectProfile` 移除并删除。当前 Rush Timeline 没有 AttackProperty 应用节点或 ActionCue；这三个 Effect 在本链路中是发布依赖，不应把它们解释成已经发生的命中事件。

## 发布合同

正式 Republish 同时生成 `RushAttack.FixedData.asset` 和 `RushAttack.Float32Data.asset`。两份产物的 `SourceRevision`、`SemanticHash`、`AbilityGuid` 与 `ContentIdentity` 必须一致；Attack、DodgeBack、DodgeForward 也遵循同一合同。
