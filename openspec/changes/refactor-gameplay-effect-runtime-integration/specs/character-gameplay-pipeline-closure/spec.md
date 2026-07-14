## MODIFIED Requirements

### Requirement: Graph 和 Timeline 必须只输出 gameplay facts

系统 MUST 让 Graph、StateMachine 和 Timeline 输出 gameplay facts，而不是直接执行最终 Transform、命中裁决、扣血或网络发送。Timeline 时间范围的 Window 作者输入 MUST 统一为 Decision TreeClip 写入 scope variable；显式 Blackboard fact projection MUST 在统一 phase 将合法写入转换为 `ActionWindowSample`。正式 gameplay facts 至少包括 `ActionActivationRequest`、`ActionLifecycleTransition`、`ActionWindowSample`、`ActionMotionSample`、`GameplayCueFact`、`GameplayResultEvent`、`MotionContribution` 与 `MotionWarpWindow`。动画选择与表现采样 MUST 通过独立 AnimationLayerSelection/AnimationProducerSample 合同提交，不得伪装成 gameplay fact。系统 MUST 不保留 `ActionCueEvent`、ActionWindowTrack/Clip 或 SubmitActionWindowSampleNode 作为并行事实生产路径。

#### Scenario: Timeline 输出攻击窗口

- **WHEN** 动作 Timeline 的 Hit、IFrame、Parry 或 Cancel Decision TreeClip 在当前 Tick 写入 projected variable=true
- **THEN** 统一 projection MUST 输出 `ActionWindowSample`
- **AND** sample MUST 关联写入 provenance 中 Action Context 的 ActionInstanceId
- **AND** Timeline MUST 不直接判定命中、扣血或发送网络包

#### Scenario: Timeline 输出动作位移

- **WHEN** 动作 Timeline 采样到 root motion 或 motion warp window
- **THEN** Timeline MUST 输出 `MotionContribution` 或 `MotionWarpWindow`
- **AND** 最终位移 MUST 由 `CharacterMotionStage` 结算
- **AND** Window 作者路径重构 MUST 不改变 Motion 轨道权威

#### Scenario: Timeline 输出表现 Cue

- **WHEN** Timeline 在当前 Tick 触发动作表现 cue
- **THEN** Timeline MUST 产生统一 `GameplayCueFact`
- **AND** fact MAY 携带来源 ActionInstanceId
- **AND** Timeline MUST NOT 产生已经删除的 `ActionCueEvent`
