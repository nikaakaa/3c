## RENAMED Requirements

- FROM: `### Requirement: Timeline pose time 与 Animancer fade time 必须独立连续推进`
- TO: `### Requirement: Timeline pose time 与Pose Slot blend time必须独立连续推进`
- FROM: `### Requirement: 动画重入必须从 Animancer 当前视觉图接管`
- TO: `### Requirement: 动画重入必须从Pose Slot当前视觉结果接管`

## MODIFIED Requirements

### Requirement: 动画 visual playback 必须来自表现帧重采样和生命周期注册表

`SimulationActorTickResult` MUST只提供AnimationChannel producer/EventId/playback intent；CharacterPresentationProjection MUST定位source资源与PoseSlot binding；AnimationPlaybackLifecycle、PoseSlot Blend Stack、Animancer source backend与Pose Graph MUST在PresentationFrame执行visual sampling、time blend、source reuse和space composition。Kernel MUST不记录Animancer state、Stack entry、PoseSlotFrame或Pose Graph workspace。

#### Scenario: Attack Timeline选中producer

- **WHEN** Committer收到compiled FullBodyAction producer command
- **THEN** MUST通过Projection定位source与FullBodyActionSlot并提交给正式Lifecycle
- **AND** Pose Graph MUST只消费该slot完成结果

### Requirement: 表现插值不得产生同步事实

PresentationFrame MUST保持为committed/predicted presentation command消费阶段。Visual interpolation、EventId keep/replace/cancel、PoseSlot Stack、Animancer source sampling、Pose Graph与visual recovery MAY产生visual pose、playback state和diagnostics snapshot，但 MUST不生成canonical input、state hash、rollback decision或Gameplay fact，也 MUST不写CharacterSimulationState、WorldSimulationState、SimulationIngress、TickResult facts或Model Output queue。网络与SimulationState MUST不读取visual root、Stack或Pose Graph作为真值。

#### Scenario: 高帧率表现帧

- **WHEN** 多个PresentationFrame发生在两个SimulationTick之间
- **THEN** visual root、slot Stack、source sampling与Pose Graph MAY连续更新
- **AND** MUST不创建额外Gameplay fact、input command或world snapshot

#### Scenario: Visual Correction进行中

- **WHEN** visual root平滑过渡到replay body sample
- **THEN** world state hash MUST不因visual interpolation或动画composition改变

### Requirement: 表现插值必须提供调试可追踪性

系统 SHOULD暴露Body SourceMode、TrajectoryMode、logic tick、interpolation alpha、target/visible pose、correction、visual Timeline time、每AnimationChannel selection、playback generation、PendingFirstSample、PoseSlotId、Stack entry/Stored/Inertial、Pose Graph output、final per-foot contribution、retention与错误。Graph、StateMachine、Timeline、Body trajectory、Animation Channel、Pose Slot和Pose Graph MUST区分逻辑执行、target sample、visual correction、时间混合与空间合成；Debug MUST不成为Gameplay、selection、Stack或Graph输入。

#### Scenario: 排查Action与Locomotion快速切换

- **WHEN** Action结束、Locomotion继续且MovingTurn同tick生效
- **THEN** Logic Trace MUST分别显示FullBodyAction与BaseLocomotion最终selection
- **AND** Animation Trace MUST显示action slot淡出、base slot transition和OutputPose来源

#### Scenario: missing first sample

- **WHEN** selected target在release前始终没有合法sample
- **THEN** Debug MUST显示playback generation、AnimationChannelId、PoseSlotId与lifecycle error
- **AND** MUST不伪造fallback output

### Requirement: Timeline pose time 与Pose Slot blend time必须独立连续推进

CharacterSimulationState MUST保存Timeline logic time，Presentation Source Cursor MUST提供visual Timeline time，每个Pose Slot Blend Stack MUST以presentation delta推进独立Fade Clock，Animancer MUST只按resolved sample time推进source sampling graph。Body Visual Trajectory Follower MUST只修改visible body pose，不得修改Animation sample、Stack delta、Pose Graph evaluation identity或playback generation。这些时钟 MUST不共享mutable state，也 MUST不把表现时间或correction写回CharacterSimulationState。

#### Scenario: 两个Logic Tick之间渲染

- **WHEN** PresentationFrame在下一个SimulationTick前推进
- **THEN** Body target sample、slot Stack clock与visual animation sample MUST连续推进
- **AND** Timeline Gameplay state与Pose Graph topology MUST保持不变

#### Scenario: Body correction正在收敛

- **WHEN** BoundedCorrection收敛visible body
- **THEN** 动画 MUST继续按Source Cursor采样并按presentation delta推进Stack/PoseGraph
- **AND** MUST不按position error减速、重启playback或生成第二个动画clock

### Requirement: 动画重入必须从Pose Slot当前视觉结果接管

同一AnimationChannelId/PoseSlotId在旧entry尚未淡出时收到新selected target，或replay后command被替换/重入时，AnimationPlaybackLifecycle MUST把EventId变化提交给该slot唯一Blend Stack。Stack MUST从当前PoseSlotFrame按正式CrossFade、Stored Pose或Inertial规则接管；Animancer只维护source sample。项目 MUST不冻结最终OutputPose、回放中间逻辑状态、清空其它slot或建立第二套handoff stack，Rollback Pipeline MUST不维护第二套CrossFade或动画时间轴。

#### Scenario: Dodge淡出到Empty

- **WHEN** FullBodyActionSlot的Dodge仍有贡献且action channel提交None
- **THEN** 该slot Stack MUST从当前视觉结果淡出到NoPose
- **AND** Pose Graph MUST连续显露BaseLocomotion，不先跳Idle或bind pose

#### Scenario: Replay改变Attack producer

- **WHEN** predicted Attack2在replay后被Attack1替换
- **THEN** lifecycle MUST让FullBodyActionSlot从当前pose接管新source
- **AND** BaseLocomotionSlot与Pose Graph topology MUST保持不变

### Requirement: Rollback 动画同步必须来自同一 Gameplay 输入模拟

Rollback网络协议 MUST不发送AnimationClip、Animator state、Animancer state、Stack entry、PoseSlotFrame、normalized time或visual pose。每个Peer MUST从同一Fixed Program输入与Action/Timeline状态生成稳定AnimationChannel producer lifecycle；PresentationFrame再独立推进source sample、slot Stack与Pose Graph。进攻request的选择性延迟 MUST作用于Gameplay request eligible tick，使双方从同一SimulationTick开始对应动作，而不是由表现层等待或瞬切补齐。

#### Scenario: 双Peer进入Attack producer

- **WHEN** Offensive Attack request在Tick T变为eligible并进入双方同一Gameplay input history
- **THEN** 两端Fixed Program MUST从Tick T生成相同FullBodyAction producer lifecycle identity
- **AND** 各自PresentationFrame MUST在本地连续采样source并推进相同slot/graph合同

#### Scenario: 连续移动驱动循环动画

- **WHEN** Relayed MoveAxis持续到达且Locomotion状态保持Run
- **THEN** 远端BaseLocomotion producer MUST由本地模拟持续拥有
- **AND** 网络协议 MUST不逐帧同步Run动画时间或PoseSlotFrame
