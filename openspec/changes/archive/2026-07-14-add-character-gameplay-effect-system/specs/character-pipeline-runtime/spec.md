## ADDED Requirements

### Requirement: CharacterPipeline 必须编排唯一 Gameplay Effect 阶段

`CharacterPipeline` MUST 只持有 `CharacterGameplayEffectAdapter`，由 Adapter 唯一持有通用 `GameplayEffectRuntime`。Pipeline MUST 在每个固定逻辑 tick 将 Adapter 编排进 NetworkReceive 之后、Input/BTSMTL 之前的 Begin 阶段，以及 Motion 之后、NetworkSend 之前的 CommitFacts 阶段。Pipeline MUST NOT 访问 GE Container 或实现 GE 规则；Presentation frame MUST 只消费已提交 cue，不得推进 effect runtime。

#### Scenario: Pipeline 执行逻辑 tick

- **WHEN** 当前 tick 已完成 incoming network/result 注入
- **THEN** pipeline MUST 调用 Adapter 将 semantic input 映射并推进 GameplayEffectRuntime 的 incoming effect、period、expiry 和 inhibition
- **AND** MUST 再让 Input 与 BTSMTL 使用协调后的统一状态

#### Scenario: Pipeline 执行表现帧

- **WHEN** PresentationStage 消费本 tick 的 gameplay cues
- **THEN** 它 MUST 不改变 tag count、attribute value、active effect 或 prediction journal
- **AND** 下一逻辑 tick 的 Gameplay Effect 结果 MUST 不依赖 render frame 数量
