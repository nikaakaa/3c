# btsmtl-runtime-diagnostics Specification

## MODIFIED Requirements

### Requirement: Runtime producer 必须在正式生命周期边界发布 Trace

Graph、RunnableNode、Composite、StateMachine、ConditionRuleGraph、Timeline scheduler、TreeClip、Pipeline Blackboard、Animation Playback Lifecycle 与 Animancer adapter MUST在各自正式边界发布对应 channel 事件。Graph Trace MUST观察逻辑 child 选择、Runnable result 和 stop；StateMachine Trace MUST观察 transition decision、State scope 与 barrier；Animation Trace MUST观察逻辑 selection、Timeline sample、PendingFirstSample、Current、Outgoing、Retired 和 Animancer fade。Producer MUST不为调试新增第二套 selection、Timeline 时间、播放生命周期或混合权威。

#### Scenario: 普通 Selector replacement

- **WHEN** Selector 停止旧 child 并启动 replacement child
- **THEN** Graph channel MUST显示 stop cause、source、replacement 和逻辑顺序
- **AND** Graph channel MUST不伪造动画 owner change 或 Driver

#### Scenario: State transition

- **WHEN** StateMachine 提交 edge 并激活 target StateNode
- **THEN** StateMachine channel MUST显示 condition、source scope、target scope 与 barrier
- **AND** Animation channel MUST只在逻辑层另行提交 AnimationLayerSelection 后显示选择变化

#### Scenario: 逻辑选择动画 producer

- **WHEN** 逻辑层为 Base 提交唯一 AnimationPlaybackId
- **THEN** Animation channel MUST显示 LayerId、playback generation、logic tick 与 selection source
- **AND** diagnostics MUST不比较 Priority 或推断第二个赢家

#### Scenario: Timeline clip membership 变化

- **WHEN** 正式 Timeline scheduler 进入、保持或离开 Track/Clip
- **THEN** Timeline channel MUST从 scheduler 的正式 sample/release 发布事件
- **AND** diagnostics MUST不独立重采样 Timeline

#### Scenario: Animancer 淡出完成

- **WHEN** Animancer 报告 outgoing state fade 完成
- **THEN** Animation channel MUST显示对应 producer 从 Outgoing 进入 Retired
- **AND** 该事件 MUST不反向改变 Tree 或 State 结果
