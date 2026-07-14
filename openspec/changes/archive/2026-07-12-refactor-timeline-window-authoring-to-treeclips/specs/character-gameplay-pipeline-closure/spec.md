## MODIFIED Requirements

### Requirement: Graph 和 Timeline 必须只输出 gameplay facts

系统 MUST 让 Graph、StateMachine 和 Timeline 输出 gameplay facts，而不是直接执行最终 Transform、命中裁决、扣血或网络发送。Timeline 时间范围的 Window 作者输入 MUST 统一为 Decision TreeClip 写入 scope variable；显式 Blackboard fact projection MUST 在统一 phase 将合法写入转换为 `ActionWindowSample`。第一阶段 facts 至少包括 `ActionActivationRequest`、`ActionLifecycleTransition`、`ActionWindowSample`、`ActionMotionSample`、`ActionCueEvent`、`GameplayResultEvent`、`MotionContribution`、`MotionWarpWindow` 和 `AnimationContribution`。系统 MUST NOT保留 ActionWindowTrack/Clip 或 SubmitActionWindowSampleNode 作为并行事实生产路径。

#### Scenario: Timeline 输出攻击窗口

- **WHEN** 动作 Timeline 的 Hit、IFrame、Parry 或 Cancel Decision TreeClip 在当前 Tick写入 projected variable=true
- **THEN** 统一 projection MUST 输出 `ActionWindowSample`
- **AND** sample MUST 关联写入 provenance 中 Action Context 的 ActionInstanceId
- **AND** Timeline MUST NOT直接判定命中、扣血或发送网络包

#### Scenario: Timeline 输出动作位移

- **WHEN** 动作 Timeline 采样到 root motion 或 motion warp window
- **THEN** Timeline MUST 输出 `MotionContribution` 或 `MotionWarpWindow`
- **AND** 最终位移 MUST 由 `CharacterMotionStage` 结算
- **AND** Window 作者路径重构 MUST NOT改变 Motion 轨道权威

#### Scenario: 本地状态时间门

- **WHEN** Decision TreeClip 写入 Projection=None 的 Bool Frame variable
- **THEN** Graph 与 StateMachine MAY 将其作为本地条件读取
- **AND** Pipeline MUST NOT生成 ActionWindowSample 或 outgoing packet

