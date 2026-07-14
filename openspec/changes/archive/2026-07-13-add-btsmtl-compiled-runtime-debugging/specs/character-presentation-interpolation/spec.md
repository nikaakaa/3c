# character-presentation-interpolation Specification

## ADDED Requirements

### Requirement: Presentation diagnostics 必须暴露完整动画仲裁链

PresentationFrame 的 Animation channel Trace MUST 暴露 previous/current logic tick、interpolation alpha、Timeline playback、logic time、visual time、contribution/owner identity、Registry lifecycle、ordered handoff records、causal components、Layer priority allocation、LayerPlan 和最终 playback output。该 Trace MUST 只观察正式表现链，不得成为任何 gameplay 或 presentation 决策输入。

#### Scenario: Action contribution 覆盖 locomotion

- **WHEN** 同一 layer 中 action 高优先级 contribution 与 locomotion contribution 同时参与仲裁
- **THEN** Trace MUST 显示每个输入 priority/weight
- **AND** MUST 显示每个 priority group 分配的容量与 final weight
- **AND** Timeline 和 Host Inspector MUST 能引用同一 contribution identity

#### Scenario: Handoff commit 接管 owner

- **WHEN** Arbitrator 将 ordered transition facts提交为 LayerPlan
- **THEN** Trace MUST 显示 record 的 tick、phase、sequence 与 None/Driver role
- **AND** MUST 显示 causal component 的 Selected、Coalesced、Retired 或 Conflict disposition
- **AND** MUST 显示最终 strategy、target readiness、playback progress、输出 contribution 和 retirement reason

#### Scenario: visual time 重采样

- **WHEN** 渲染帧在两个 logic ticks 之间重采样动画
- **THEN** Trace MUST 记录 interpolation alpha、visual Timeline time 和最终 clip time
- **AND** MUST 将事件标记为 Presentation domain
