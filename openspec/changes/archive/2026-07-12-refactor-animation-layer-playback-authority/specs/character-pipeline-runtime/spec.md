## MODIFIED Requirements

### Requirement: 角色管线必须保留跨 logic tick 的动画生命周期命令

系统 MUST 使用 presentation-owned 持久队列保存尚未消费的 Sample、terminal metadata、Complete、Release、owner membership、None/Driver handoff facts 与 AnimationOwnerReady。命令 MUST 独立于单个 `CharacterPipelineFrame.Output`，并按 local logic tick、phase 与 sequence 保序。该顺序 MUST 一直保留到 `CharacterAnimationLayerArbitrator` 完成 LayerPlan commit；系统 MUST NOT只保留最后一个 catch-up tick，也 MUST NOT在 Stage 中把 handoff commands 压平成无顺序 intent 列表。

#### Scenario: 单 render frame 多个 logic tick

- **WHEN** 较早 tick 完成 Timeline，后续多个 tick 连续发生 State transition
- **THEN** Complete、owner release、全部 None/Driver facts、ready 与 target Sample MUST 全部保留到 PresentationFrame
- **AND** 每条 handoff fact 的 tick、phase 与 sequence MUST 进入 Arbitrator
- **AND** 后续 `Frame.Begin()` MUST NOT覆盖较早命令

#### Scenario: transient output 清理

- **WHEN** Pipeline 清理 transient gameplay/presentation output
- **THEN** 未被 PresentationFrame acknowledge 的 animation commands MUST 保留

#### Scenario: plan commit 前不得确认

- **WHEN** Stage 已复制完整 command batch
- **AND** Registry、Arbitrator、LayerRuntime 或 Presenter 尚未完成本批正式提交
- **THEN** queue MUST NOT提前 acknowledge 该批 commands

#### Scenario: Pipeline 释放

- **WHEN** pipeline deactivate 或 dispose
- **THEN** pending commands、Registry entries、Arbitrator ledger 与全部 layer states MUST 清理
- **AND** 系统 MUST NOT等待隐藏 timeout

### Requirement: PresentationFrame 必须完成统一动画 lifecycle handoff

PresentationFrame MUST 按顺序处理 Timeline visual sampling、完整 ordered command batch、Registry lifecycle、Registry snapshot、Arbitrator LayerPlan commit、LayerRuntime playback、最终 layer outputs、Presenter application 与 acknowledgement。它 MUST 保证 source release 与 target sample 不产生中间空输出，并保持 gameplay facts 只在 logic tick 产生。

#### Scenario: Timeline 完成后切换

- **WHEN** Once Timeline 完成后 StateMachine 进入 target
- **THEN** PresentationFrame MUST 同时消费 terminal state、owner release、transition facts 与新 Sample
- **AND** Arbitrator MUST 生成每层唯一 LayerPlan
- **AND** outgoing MUST 来自当前 FinalOutput
- **AND** incoming MUST 来自完整 DesiredCandidate

#### Scenario: target sample 延迟

- **WHEN** Driver 已到达、target 尚未 Ready或目标 contribution 尚未进入 RequireOutput DesiredCandidate
- **THEN** Arbitrator MUST 保留待定链并生成 Hold plan
- **AND** 后续 PresentationFrame MUST 在正式 target facts 到达后继续同一 causal chain

#### Scenario: catch-up 批次内 target Ready 后立即 release

- **WHEN** 多个 logic tick 在一个 PresentationFrame 前依次提交 Driver、target AnimationOwnerReady 与 target owner release
- **THEN** Arbitrator MUST 保留该 activation 已 Ready 的事实供当前 plan commit
- **AND** Registry MUST 仍按 release 结束 target producer membership
- **AND** ready fact MUST 在全部 Layer 不再引用对应 records 后确定性清理

#### Scenario: 连续 transition 批次

- **WHEN** 当前 FinalOutput 到最终 DesiredCandidate 之间存在多条有序且连通的 transition records
- **THEN** Arbitrator MUST 先归并连续因果链
- **AND** LayerRuntime MUST 只执行一个 LayerPlan
- **AND** Presenter MUST NOT看到被逻辑跳过的中间 owner

#### Scenario: 没有 owner 变化

- **WHEN** DesiredCandidate 与当前 FinalOutput 的可见 owner 集合相同
- **THEN** Arbitrator MUST 生成 Update plan
- **AND** LayerRuntime MUST 更新本帧 visual samples且不要求 Driver

#### Scenario: 表现帧不产生 gameplay facts

- **WHEN** PresentationFrame 完成动画重采样、plan commit和播放 handoff
- **THEN** 它 MUST NOT重新 tick RootTree、ActionWindow、Motion 或 SyncFacts

## ADDED Requirements

### Requirement: PresentationFrame 必须输出逐层最终动画结果

`CharacterPipelineFrame` 的正式动画表现输出 MUST 保存 `AnimationLayerPlaybackOutput` 集合。每层 output MUST 表达 layer weight、mask、blend mode、state plans 与 playback handoff lifecycle。Ordered records、causal components、DesiredCandidate 与 LayerPlan MAY 进入 debug snapshot，但 MUST NOT成为 gameplay 输入。

#### Scenario: Base Stable

- **WHEN** Base layer 处于 Stable
- **THEN** frame MUST 输出一份 Base layer result
- **AND** Presenter MUST 只消费该 result

#### Scenario: Base Hold

- **WHEN** RequireOutput Base 的 LayerPlan 为 Hold
- **THEN** frame MUST 输出 HeldOutput 与 Hold debug
- **AND** frame MUST NOT用空 plan list 隐藏该状态

#### Scenario: Base Invalid

- **WHEN** Arbitrator 发现独立同 authority component 冲突
- **THEN** frame MUST 输出最后合法 Base result 与完整 Invalid provenance
- **AND** Presenter MUST NOT自行选择某个 Driver
