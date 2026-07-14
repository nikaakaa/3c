## ADDED Requirements

### Requirement: Gameplay Effect 不得存入 Pipeline Blackboard

GameplayTag、Attribute Base/Current、ActiveGameplayEffect、stack、duration、period、inhibition 和 prediction journal MUST 由通用 `GameplayEffectRuntime` 正式持有。CharacterGameplayEffectAdapter 只委托端口和投影 ChangeSet；Blackboard MAY 保存 Graph 局部计算值或显式 fact projection，但 MUST NOT 作为上述 Gameplay Effect 的真相源、缓存副本或双写目标。

#### Scenario: Graph 读取 Health

- **WHEN** ValueNode 需要当前 Health
- **THEN** 它 MUST 通过 Gameplay Attribute 查询接口读取
- **AND** MUST NOT 从同名 Blackboard variable 读取或回写同步

#### Scenario: Transition 使用临时比较结果

- **WHEN** Graph 把 `Health < Threshold` 的本地计算结果写入 Frame Blackboard
- **THEN** Blackboard MAY 保存该临时 Bool
- **AND** Health 的 Base、Current 与 Revision MUST 仍只归属 Gameplay Effect
