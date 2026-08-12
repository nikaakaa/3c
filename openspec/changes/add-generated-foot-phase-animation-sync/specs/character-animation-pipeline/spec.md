## MODIFIED Requirements

### Requirement: Marker effective time必须由source-local计划解析

Action Timeline与Pose source只提交raw sample、MarkerGroup binding和明确Time Mapping。PoseState Transition的Source Sync Plan或AnimationSlot的Action source usage MUST在采样前定位有向marker pair与occurrence，并按Projection中的`MarkerSegmentFraction`或`GeneratedFootPhase`计划解析effective time；共同可见期 MUST持续求值。GeneratedFootPhase只消费固定warp knots，不得读取Library artifact、AnimationClip、当前Pose、FootGrounding或IK。Pose Graph MUST不序列化MarkerSync或FootPhase节点；Runtime MUST不按State、Action或clip显示名建立relation。

持续Pose的raw Movement time MUST来自获胜Motion Contribution原子携带的`CommittedMovementPlaybackClock`，不得在Motion resolve后反查Locomotion operation状态。Locomotion Input Motion与Timeline Motion Curve MUST分别提交自己的owner、generation、authority tick和continuous ticks；Action Timeline MUST继续使用独立Action playback clock。Marker映射只产生effective time，不得拥有或改写raw clock。

#### Scenario: Walk到Run同步

- **WHEN** Pose Transition启用MarkerGroup、两侧binding合法且共同选择GeneratedFootPhase
- **THEN** Runtime MUST按有向marker occurrence与双脚warp plan映射target sample
- **AND** Gameplay movement与Transition start MUST不等待marker边界

#### Scenario: Action明确使用线性Marker时间

- **WHEN** Action binding的Time Mapping为MarkerSegmentFraction
- **THEN** Action source MUST按编译线性计划生成effective time
- **AND** Runtime MUST不查询Foot Analysis或尝试GeneratedFootPhase

#### Scenario: Action不同步

- **WHEN** Action binding的SyncMode为None
- **THEN** Action source MUST使用raw visual time并清空Time Mapping
- **AND** Runtime MUST不自动补relation

#### Scenario: MovingTurn由Timeline产生移动

- **WHEN** MovingTurn Timeline Motion Curve赢得Movement motion channel
- **THEN** committed Movement clock MUST来自该Timeline owner和activation generation
- **AND** Runtime MUST不读取Locomotion Input operation的elapsed或把MovingTurn归类为Action clock
