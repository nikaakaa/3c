## MODIFIED Requirements

### Requirement: Presentation 闭环必须只消费表现事实

表现链路 MUST 消费 AnimationContribution、ordered handoff/owner lifecycle facts、`AnimationLayerPlan`、`AnimationLayerPlaybackOutput` 与 PresentationCue。PresentationStage MUST 使用 Registry、Arbitrator、LayerRuntime 与 presentation delta生成最终 outputs，但 MUST NOT推进 Timeline logic、执行 Action 决策、读取 transport 或产生 gameplay 裁决。原始 transition facts MUST 在 Arbitrator commit后才进入每层唯一播放计划，MUST NOT直接交给播放 adapter。

#### Scenario: Timeline contribution

- **WHEN** AnimationTrack 产生 contribution
- **THEN** Registry MUST 记录 producer lifecycle
- **AND** Arbitrator MUST 生成 DesiredCandidate 与 LayerPlan
- **AND** LayerRuntime MUST 生成最终 layer output
- **AND** Presenter MUST 只应用该 output

#### Scenario: State Driver

- **WHEN** StateMachine 发布 ordered Driver fact
- **THEN** Arbitrator MUST 在表现域决定其 causal disposition与 LayerPlan
- **AND** LayerRuntime MUST 只执行已提交 plan
- **AND** source/target State、ActionWindow、Motion 与 SyncFacts MUST 继续由逻辑域决定

#### Scenario: 连续 State facts

- **WHEN** 多条连续 State transition facts 在一次表现 commit前到达
- **THEN** Arbitrator MUST 先归并 causal chain
- **AND** 播放层 MUST 每层只收到一个 LayerPlan

#### Scenario: Presentation Cue

- **WHEN** Timeline/Graph 输出 VFX、SFX、camera cue 或 hit stop
- **THEN** cue MUST 进入正式 presentation output
- **AND** cue MUST NOT绕过 SyncFacts 伪装成网络事件

#### Scenario: AllowEmpty

- **WHEN** AllowEmpty layer 的正式 LayerPlan最终 weight 为 0
- **THEN** Presenter MAY 静音该 layer
- **AND** PresentationStage MUST NOT创建隐藏 producer
