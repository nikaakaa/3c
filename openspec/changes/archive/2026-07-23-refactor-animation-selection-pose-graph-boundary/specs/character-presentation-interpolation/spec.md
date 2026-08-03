## MODIFIED Requirements

### Requirement: 动画 visual playback 必须来自表现帧重采样和生命周期注册表

`SimulationActorTickResult` MUST只提供AnimationChannel producer、EventId与playback intent。Presentation MUST把正式Timeline/MM采样降低为含raw visual time的`AnimationSelectionFrame`；显式MarkerSync、Player、Animancer source backend与编译Pose Plan MUST在PresentationFrame依次执行effective time解析、source sampling、时间连续性、空间合成和world-aware处理。Kernel MUST不记录Marker relation、Animancer state、Player entry、Pose Value或Pose Plan workspace。

#### Scenario: Attack Timeline选中producer

- **WHEN** Committer收到compiled FullBodyAction producer command
- **THEN** Presentation MUST通过Projection生成对应Animation Selection
- **AND** Pose Graph MUST只按typed edge消费其Player结果

### Requirement: 表现插值不得产生同步事实

Visual interpolation、EventId keep/replace/cancel、Animation Selection、显式Player、Animancer source sampling、Pose Plan与visual recovery MAY产生visual pose、player state和diagnostics snapshot，但 MUST不生成canonical input、state hash、rollback decision或Gameplay fact，也 MUST不写CharacterSimulationState、WorldSimulationState、SimulationIngress、TickResult facts或Model Output queue。

#### Scenario: 高帧率表现帧

- **WHEN** 多个PresentationFrame发生在两个SimulationTick之间
- **THEN** visual root、Player与Pose Plan MAY连续更新
- **AND** MUST不创建额外Gameplay fact或world snapshot

### Requirement: 表现插值必须提供调试可追踪性

Diagnostics SHOULD暴露Body SourceMode、logic tick、interpolation alpha、raw visual Timeline time、AnimationChannel selection、playback generation、PoseNodeId、MarkerSync raw/effective time与relation、Player source usage、Blend Stack entry/Stored、Inertialization residual、Pose availability、参数来源、world-aware completion、final per-foot contribution与错误。Debug MUST不成为Gameplay、Selection、Player或Graph输入。

#### Scenario: 排查Action与Locomotion快速切换

- **WHEN** Action结束、Locomotion继续且MovingTurn同tick生效
- **THEN** Logic Trace MUST分别显示两个channel的最终selection
- **AND** Animation Trace MUST显示两个Player与OutputPose来源

### Requirement: Timeline pose time与显式Player time必须独立连续推进

CharacterSimulationState MUST保存Timeline logic time，Presentation Source Cursor MUST提供visual Timeline time，每个显式Player MUST以presentation delta推进自身sample或transition clock，Animancer MUST只按resolved sample descriptor采样。Body Visual Trajectory Follower MUST不修改Animation sample、Player delta、Pose Plan completion或playback generation。

#### Scenario: 两个Logic Tick之间渲染

- **WHEN** PresentationFrame在下一个SimulationTick前推进
- **THEN** Body target sample、Player clock与visual animation sample MUST连续推进
- **AND** Timeline Gameplay state与Pose Graph topology MUST保持不变

### Requirement: 动画重入必须遵守显式Player连续性语义

同一AnimationChannel收到新selection identity或rollback替换时，`SelectedPosePlayer` MUST发布typed discontinuity；没有Inertialization时明确硬切。`BlendStack` MUST按其Blend Policy执行CrossFade或Stored Pose接管；局部`Inertialization` MUST按自身Policy决定HardCut或残差rebase。Animancer MUST只维护source sample；Rollback Pipeline MUST不维护第二套CrossFade、Inertial或动画时间轴。

#### Scenario: Replay改变Attack producer

- **WHEN** predicted Attack2在replay后被Attack1替换
- **THEN** FullBodyAction Player MUST按图定义接管新source
- **AND** BaseLocomotion Player与Pose Graph topology MUST保持不变

### Requirement: Rollback 动画同步必须来自同一 Gameplay 输入模拟

Rollback MUST从同一Gameplay input、Program执行与EventId replacement重新产生Animation Selection。网络协议 MUST不发送AnimationClip、Animancer state、Player entry、Pose Value、normalized time或最终visual pose；表现帧 MUST按本地编译Pose Plan重新求值。

#### Scenario: 对端修正动作分支

- **WHEN** rollback以相同Gameplay输入重新产生新的Action selection
- **THEN** 本地Player MUST按新的selection identity接管
- **AND** 网络 MUST不发送或恢复旧Blend Stack entry
