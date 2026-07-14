# character-gameplay-pipeline-closure Specification

## MODIFIED Requirements

### Requirement: Graph 和 Timeline 必须只输出 gameplay facts

系统 MUST让 Graph、StateMachine 和 Timeline 输出 gameplay facts，而不是直接执行最终 Transform、命中裁决、扣血或网络发送。Timeline 时间范围的 Window 作者输入 MUST统一为 Decision TreeClip 写入 scope variable；显式 Blackboard fact projection MUST在统一 phase 将合法写入转换为 `ActionWindowSample`。第一阶段 gameplay facts 至少包括 `ActionActivationRequest`、`ActionLifecycleTransition`、`ActionWindowSample`、`ActionMotionSample`、`GameplayCueFact`、`GameplayResultEvent`、`MotionContribution` 与 `MotionWarpWindow`。动画选择与表现采样 MUST通过独立 AnimationLayerSelection/AnimationProducerSample 合同提交，不得伪装成 gameplay fact。系统 MUST不保留 `ActionCueEvent`、ActionWindowTrack/Clip 或 SubmitActionWindowSampleNode 作为并行事实生产路径。

#### Scenario: Timeline 输出攻击窗口

- **WHEN** 动作 Timeline 的 Hit、IFrame、Parry 或 Cancel Decision TreeClip 在当前 Tick 写入 projected variable=true
- **THEN** 统一 projection MUST输出 `ActionWindowSample`
- **AND** sample MUST关联写入 provenance 中 Action Context 的 ActionInstanceId
- **AND** Timeline MUST不直接判定命中、扣血或发送网络包

#### Scenario: Timeline 输出动作位移

- **WHEN** 动作 Timeline 采样到 root motion 或 motion warp window
- **THEN** Timeline MUST输出 `MotionContribution` 或 `MotionWarpWindow`
- **AND** 最终位移 MUST由 `CharacterMotionStage` 结算
- **AND** Window 作者路径重构 MUST不改变 Motion 轨道权威

#### Scenario: 本地状态时间门

- **WHEN** Decision TreeClip 写入 Projection=None 的 Bool Frame variable
- **THEN** Graph 与 StateMachine MAY将其作为本地条件读取
- **AND** Pipeline MUST不生成 ActionWindowSample 或 outgoing packet

### Requirement: Presentation 闭环必须只消费表现事实

动画表现链路 MUST消费每层唯一 AnimationLayerSelection、匹配 generation 的 AnimationProducerSample、Complete 与 Release。PresentationStage MUST原子提交 AnimationPlaybackLifecycle，并由 AnimancerPlaybackAdapter 使用 presentation delta 应用正式 TransitionLibrary、state、mixer 与 fade；它 MUST不消费 PresentationSync cue，不推进 Timeline logic、执行 Action 决策、读取 transport 或产生 gameplay 裁决。Presentation MUST不读取 Tree priority、Tree lifecycle 或 StateMachine edge 来二次选择动画赢家。

#### Scenario: Timeline producer sample

- **WHEN** selected Timeline AnimationTrack 在表现帧产生合法 sample
- **THEN** command queue MUST保存对应 playback generation 的 AnimationProducerSample
- **AND** lifecycle MUST在首个 sample 后将 PendingFirstSample 原子切换为 Current
- **AND** AnimancerPlaybackAdapter MUST应用该 producer 的正式 transition

#### Scenario: 逻辑所有权变化

- **WHEN** State/Action 逻辑为 Base 提交新的唯一 AnimationLayerSelection
- **THEN** Presentation MUST等待目标首个合法 sample
- **AND** MUST不从 Tree edge、Priority 或历史 sample 推断另一个目标

#### Scenario: Presentation Cue

- **WHEN** Timeline/Graph 输出 VFX、SFX、camera cue 或 hit stop
- **THEN** cue MUST进入正式 presentation output
- **AND** cue MUST不绕过 SyncFacts 伪装成网络事件

#### Scenario: AllowEmpty

- **WHEN** AllowEmpty layer 收到正式 Empty selection
- **THEN** Animancer MAY淡出该 layer 到空
- **AND** PresentationStage MUST不创建隐藏 producer
