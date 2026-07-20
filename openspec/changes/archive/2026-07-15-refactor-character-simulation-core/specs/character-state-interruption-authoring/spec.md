# character-state-interruption-authoring Specification

## MODIFIED Requirements

### Requirement: 状态抢占必须复用分层停止协议

状态抢占 authoring MUST 继续表达通用 Runnable stop、StateMachine transition、State.OnExit 与 Timeline producer release。Compiler MUST 将其生成为统一 control-flow、stop barrier、ownership 与 release operation；Program MUST 在关闭 source State、Action 与 Timeline Gameplay output 后，为受影响 LayerId 输出唯一 producer command。系统 MUST 不让 source 逻辑为 fade 继续 active，也 MUST 不使用 StateMachine external animation、Tree Driver、Animation priority 或 CharacterGraphContext selection。

#### Scenario: RunEnd 被输入抢占

- **WHEN** RunEnd compiled edge 命中更高优先级条件
- **THEN** StateMachine operation MUST 完成 source exit 与 target activation
- **AND** Locomotion operation MUST 输出 target producer command
- **AND** AnimationPlaybackLifecycle MUST 只消费已提交 command 与 sample

#### Scenario: 上层 Selector 抢占 StateMachineNode

- **WHEN** LowerPriority replacement 停止整个 StateMachine operation
- **THEN** stop cause MUST 沿 active descendant operation 传播
- **AND** Action/Locomotion operation MUST 在 barrier 完成后输出最终 producer command
- **AND** MUST 不读取 StateMachineNode external animation definition

#### Scenario: ForceStop

- **WHEN** Session、Actor 或 Host ForceStop/deactivate/dispose
- **THEN** SessionRuntime MUST 立即关闭 logic activation 并输出 retire lifecycle
- **AND** Committer/Presentation MUST 清理 playback lifecycle、Animancer states 与 retention
- **AND** MUST 不读取 transition duration 或等待 fade
