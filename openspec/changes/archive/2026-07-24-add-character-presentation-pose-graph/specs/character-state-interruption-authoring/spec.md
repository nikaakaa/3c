## MODIFIED Requirements

### Requirement: 状态抢占必须复用分层停止协议

状态抢占authoring MUST继续表达通用Runnable stop、StateMachine transition、State.OnExit与Timeline producer release。Compiler MUST将其生成为统一control-flow、stop barrier、ownership与release operation；Program MUST在关闭source State、Action与Timeline Gameplay output后，为每个受影响`AnimationChannelId`输出唯一producer command。系统 MUST不让source逻辑为fade继续active，也 MUST不使用StateMachine external animation、Tree Driver、Animation priority、PoseSlotId、Pose Graph或CharacterGraphContext selection决定逻辑winner。

#### Scenario: FullBody Action被Locomotion条件抢占

- **WHEN** Dodge Action结束且BaseLocomotion仍有合法Run selection
- **THEN** Program MUST为FullBodyAction输出None/Release并保持BaseLocomotion command
- **AND** Pose Graph MUST只观察FullBodyAction Selection变化和显式Player收尾，不执行状态切换

#### Scenario: 上层Selector抢占StateMachineNode

- **WHEN** LowerPriority replacement停止整个StateMachine operation
- **THEN** stop cause MUST沿active descendant operation传播
- **AND** 各受影响AnimationChannel MUST在barrier后输出最终command

#### Scenario: ForceStop

- **WHEN** Session、Actor或Host ForceStop/deactivate/dispose
- **THEN** Pipeline Runtime MUST立即关闭logic activation并输出各channel retire lifecycle
- **AND** Presentation MUST清理Lifecycle、全部Player节点、Pose Graph workspace、Animancer sources与retention
- **AND** MUST不等待fade完成或读取Pose Graph transition

### Requirement: 状态退出逻辑屏障与表现收尾必须分离

source State root、Action lifecycle、Timeline Gameplay output与逻辑ownership MUST在stop barrier内关闭。Animation Selection lifecycle MAY让已释放source继续被显式Player节点引用，并通过PresentationRetention接收animation-only sample；该Player MUST负责时间收尾。逻辑release MUST不等于source visual retirement，但表现收尾 MUST不重新tick source Gameplay，Pose Graph MUST不恢复逻辑ownership。

#### Scenario: CrossFade收尾

- **WHEN** source已逻辑退出且显式Blend Stack节点仍混合其entry
- **THEN** source MAY保持只读animation retention
- **AND** MUST不再产生Gameplay、Tree、Timeline logic、Motion、root motion或GameplayFacts

#### Scenario: target首样本延迟

- **WHEN** source已退出但selected target尚无首样本
- **THEN** Lifecycle MUST保持上一份正式Player输出并记录PendingFirstSample
- **AND** MUST不恢复source逻辑ownership或选择fallback

#### Scenario: 结构target

- **WHEN** logical target本身不产animation producer
- **THEN** RequireOutput channel MUST保持已提交正式producer或由逻辑直接选择目标producer
- **AND** AllowEmpty channel MAY显式选择None
- **AND** Animation module MUST不从Runnable、Tree route或Pose Graph推断target

### Requirement: 动画 Transition 的完成不得反向阻塞 Tree terminal

Tree/StateMachine terminal MUST只由逻辑停止协议决定，MUST不等待任何Player transition。PendingFirstSample与Retired MUST由AnimationPlaybackLifecycle推进；BlendStack entry/Stored、Inertialization residual与Player source usage MUST由各自显式节点在PresentationFrame推进。Animancer只采样source，Pose Graph Plan负责连续性、组合、world-aware阶段与最终输出。teardown MUST确定性清理全部表现生命周期。

#### Scenario: 长淡出与新child

- **WHEN** source SMNode已terminal但source仍被显式Player节点引用
- **THEN** parent Tree MUST能推进replacement child
- **AND** replacement logic MUST能提交新AnimationChannel selection

#### Scenario: Host销毁

- **WHEN** Host在Player连续化运行时dispose
- **THEN** Runtime MUST清理Lifecycle、全部Player节点、source、Pose Graph workspace与retention
- **AND** Tree terminal MUST不等待transition duration
