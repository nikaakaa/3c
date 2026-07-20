# character-state-interruption-authoring Specification

## MODIFIED Requirements

### Requirement: 状态抢占必须复用分层停止协议

状态抢占 MUST继续复用通用 Runnable stop、StateMachine transition、State.OnExit 与 Timeline producer release。逻辑层 MUST在 stop barrier 内关闭 source State、Action、Timeline gameplay output，并在完成 priority/ownership 决策后为受影响 LayerId 提交唯一 AnimationLayerSelection。系统 MUST不让 source 逻辑为 fade 继续 Running，也 MUST不使用 StateMachine external animation、Tree Driver 或 Animation priority。

#### Scenario: RunEnd 被输入抢占

- **WHEN** RunEnd 命中更高优先级 State edge
- **THEN** StateMachine MUST完成 source exit 与 target activation
- **AND** Locomotion 逻辑 MUST选择 target playback
- **AND** AnimationPlaybackLifecycle MUST只消费该 selection 与 sample

#### Scenario: 上层 Selector 抢占 StateMachineNode

- **WHEN** LowerPriority replacement 停止整个 StateMachineNode
- **THEN** stop cause MUST沿 StateMachineNode 与 active State descendants 传播
- **AND** Action/Locomotion 逻辑 MUST在 barrier 完成后提交最终 selection
- **AND** MUST不读取 StateMachineNode external animation definition

#### Scenario: ForceStop

- **WHEN** pipeline/host ForceStop、deactivate 或 dispose
- **THEN** Pipeline MUST立即清理 logic owner、playback lifecycle、Animancer states 与 retention
- **AND** MUST不读取 transition duration 或等待 fade

### Requirement: 状态退出逻辑屏障与表现收尾必须分离

source State root、Action lifecycle、Timeline gameplay output 与逻辑所有权 MUST在 stop barrier 内关闭。AnimationPlaybackLifecycle MAY让已释放 source 以 Outgoing 视觉状态存在，并通过 PresentationRetention 接收 animation-only sample；Animancer MUST负责 fade。逻辑 release MUST不等于 outgoing visual retirement，但表现收尾 MUST不重新 tick source gameplay。

#### Scenario: CrossFade 收尾

- **WHEN** source 已逻辑退出且 Animancer 正在淡出其 state
- **THEN** source MAY保持 Outgoing 与只读 animation retention
- **AND** source MUST不再产生 gameplay、Tree、Timeline logic、Motion、root motion 或 SyncFacts

#### Scenario: target 首样本延迟

- **WHEN** source 已退出但 selected target 尚无第一份 sample
- **THEN** lifecycle MUST保持上一 Current 并记录 PendingFirstSample
- **AND** MUST不恢复 source 逻辑所有权或选择 fallback

#### Scenario: 结构 target

- **WHEN** logical target 本身不产 animation producer
- **THEN** RequireOutput layer 的逻辑提交 MUST省略该层更新并保持已提交的正式 producer，或直接选择目标状态的正式 producer
- **AND** AllowEmpty layer MAY显式选择 None
- **AND** Animation 模块 MUST不从 Runnable executed 或 Tree route 推断 target

### Requirement: 动画 Transition 的完成不得反向阻塞 Tree terminal

Tree/StateMachine terminal MUST只由逻辑停止协议决定，MUST不等待 Animancer fade。PendingFirstSample、Current、Outgoing 与 Retired MUST由 AnimationPlaybackLifecycle 在表现帧推进；fade progress MUST由 Animancer 使用 presentation delta 推进。teardown MUST确定性清理播放生命周期。

#### Scenario: 长淡出与新 child

- **WHEN** source SMNode 已 terminal 但 source Animancer state 仍为 Outgoing
- **THEN** parent Tree MUST能推进 replacement child
- **AND** replacement logic MUST能提交新 selection

#### Scenario: Host 销毁

- **WHEN** host 在 fade 运行时 dispose
- **THEN** lifecycle、retention 与 Animancer states MUST立即释放

## REMOVED Requirements

### Requirement: 并行与嵌套停止必须先区分连续链与独立竞争

**Reason**: 连续因果链、独立 Driver 竞争与 authority 仲裁属于已删除动画架构。并行和嵌套停止只需完成逻辑所有权决策并输出每层唯一 selection。

#### Scenario: Parallel 与 nested State 同时变化

- **WHEN** 一个 logic tick 内多个分支改变状态
- **THEN** 逻辑层 MUST提交每层最终 selection
- **AND** Animation 模块 MUST不构建 ExecutionLineage 或 causal component
