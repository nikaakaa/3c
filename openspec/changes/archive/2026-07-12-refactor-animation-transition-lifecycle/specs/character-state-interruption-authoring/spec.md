## MODIFIED Requirements

### Requirement: 状态抢占必须复用分层停止协议

状态抢占 MUST 继续复用 TreeNode `OnStopRequested -> Stopping -> terminal`、StateMachine transition、State OnExit 和 producer release 的分层协议。逻辑层 MUST 在 stop barrier 内关闭 source State、Action、Timeline 和 gameplay output；动画表现层 MUST 通过显式 `AnimationTransitionRequest` 独立完成 Immediate、ContributionCrossFade 或 Inertialization。系统 MUST NOT 以 source 逻辑继续 Running 代替动画收尾。

#### Scenario: RunEnd 被输入 Transition 抢占

- **WHEN** RunEnd 尚未 terminal 且输入条件命中更高优先级 edge
- **THEN** StateMachine MUST 立即执行 source State 退出和 target State 激活
- **AND** runtime MUST 发布该 edge 的 animation transition request
- **AND** RunEnd State body MUST NOT 为动画混合继续 tick

#### Scenario: 上层 Selector 抢占 SMNode

- **WHEN** 上层 Selector 以 graceful replacement 抢占 StateMachineNode
- **THEN** stop context MUST 向 StateMachine 层传递明确 Empty release definition
- **AND** source State、Timeline 和 Action MUST 在逻辑 barrier 内关闭
- **AND** 动画 Transition MAY 在 PresentationStage 中继续 Running

#### Scenario: ForceStop

- **WHEN** Tree、pipeline 或 host 发出 ForceStop/deactivate/dispose
- **THEN** StateMachine MUST 使用 Immediate source -> Empty request
- **AND** 任何 transition-owned snapshot/native data MUST 确定性释放

### Requirement: 状态退出逻辑屏障与表现收尾必须分离

source State root、Action lifecycle、Timeline gameplay output 和 animation owner membership MUST 在逻辑 stop barrier 内完成关闭或归属切换。Animation Presentation MAY 在逻辑退出后使用 TransitionRuntime 捕获的 contribution snapshot 或 final pose/velocity 继续收尾，但 MUST NOT 为此继续 tick source State、Timeline 或 Action。Visual retirement MUST NOT 被 Registry owner membership release 隐式代表。

#### Scenario: Tree abort 后 ContributionCrossFade

- **WHEN** StateMachineNode graceful stop 已完成
- **AND** Empty release strategy 为 ContributionCrossFade
- **THEN** 父 Tree MAY 启动 replacement child
- **AND** TransitionRuntime MAY 使用冻结 source contribution 继续淡出
- **AND** 旧 State MUST NOT 再产生 gameplay facts

#### Scenario: Tree abort 后 Inertialization

- **WHEN** StateMachineNode graceful stop 已完成
- **AND** Empty release strategy 为 Inertialization
- **THEN** TransitionRuntime MAY 使用捕获的最终 pose/velocity 继续衰减
- **AND** source playback 与 source logic MUST 已停止

#### Scenario: target 无动画

- **WHEN** replacement target 已 Ready 但没有 animation contribution
- **THEN** 表现层 MUST 将 Empty 作为真实 target
- **AND** 系统 MUST NOT 恢复旧 owner、旧 Timeline 或默认 Idle

## ADDED Requirements

### Requirement: 动画 Transition 的完成不得反向阻塞 Tree terminal

Tree/StateMachine 的逻辑 terminal MUST 由逻辑停止协议决定，MUST NOT 等待 animation transition duration。Animation transition 的 Completed、Superseded 和 Retired MUST 由表现 runtime 独立推进，并在 host 销毁时确定性清理。

#### Scenario: 长动画淡出与新 child 并行

- **WHEN** source SMNode 已逻辑 terminal
- **AND** 其 animation transition 尚在 Running
- **THEN** 父 Tree MUST 可推进 replacement child
- **AND** 新 child 的 contributions MUST 可与 transition-owned source snapshot 进入同一 LayerRuntime

#### Scenario: host 在 Transition 中销毁

- **WHEN** host 在 animation transition Running 时 dispose
- **THEN** transition MUST 以明确 dispose cause Retire
- **AND** source snapshot、pose history 引用和 native data MUST 释放
