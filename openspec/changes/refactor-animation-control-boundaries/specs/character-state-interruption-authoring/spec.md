# character-state-interruption-authoring Specification

## MODIFIED Requirements

### Requirement: 状态抢占必须复用分层停止协议

Gameplay状态抢占authoring MUST继续表达通用Runnable stop、BTSMTL StateMachine transition、State.OnExit、Action lifecycle与有限Action Timeline producer release。Compiler MUST把它们生成为统一control-flow、stop barrier、Motion ownership与release operation；Program MUST在关闭source Action与Timeline Gameplay output后，为每个受影响的有限Action AnimationChannel输出至多一个producer command。持续Locomotion Pose transition MUST由PoseStateMachine处理，Program MUST不为视觉transition保持source Gameplay State active。

#### Scenario: Attack被Dodge抢占

- **WHEN** 更高优先级Dodge replacement停止Attack State
- **THEN** Gameplay MUST完成Attack source exit并激活Dodge
- **AND** FullBodyAction MUST提交Dodge playback
- **AND** Slot MUST独立处理Attack到Dodge的Pose transition

#### Scenario: 上层Selector抢占StateMachineNode

- **WHEN** LowerPriority replacement停止整个Gameplay StateMachine operation
- **THEN** stop cause MUST沿active descendant传播
- **AND** Program MUST释放受影响Action playback与Motion ownership
- **AND** MUST不读取PoseStateMachine active state

### Requirement: 状态退出逻辑屏障与表现收尾必须分离

source Gameplay State root、Action lifecycle、Timeline Gameplay output与Motion ownership MUST在stop barrier内关闭。`CharacterActionPlaybackRuntime` MAY让已释放的有限Action source以Retained视觉状态存在；AnimationSlot、BlendStack与Inertialization MUST按compiled route完成表现收尾。PoseStateMachine source relevance MUST只属于Presentation workspace。逻辑release MUST不等待Slot或PoseState transition，表现收尾 MUST不重新tick source Gameplay。

#### Scenario: Action Slot收尾

- **WHEN** Attack已经逻辑退出但FullBodyAction Slot仍在淡出
- **THEN** Attack source MAY保持Retained animation-only sample
- **AND** MUST不再产生Gameplay、Timeline logic、Motion或Window

#### Scenario: PoseState transition继续

- **WHEN** Gameplay movement mode已更新且PoseStateMachine仍在Start到Locomotion过渡
- **THEN** Gameplay MUST继续下一Tick
- **AND** transition progress MUST只按Presentation delta推进

### Requirement: 动画 Transition 的完成不得反向阻塞 Tree terminal

Tree与Gameplay StateMachine terminal MUST只由逻辑停止协议决定，MUST不等待Animation Slot、PoseStateMachine transition、PendingFirstSample、BlendStack或Inertialization。Action playback lifecycle与PoseState workspace MUST由PresentationFrame推进；teardown MUST按编译Plan确定性清理。

#### Scenario: 长Action淡出与新child

- **WHEN** source Action State已经terminal但Action Pose仍为Retained
- **THEN** parent Tree MUST能推进replacement child
- **AND** 新Action MUST能提交新的playback generation
