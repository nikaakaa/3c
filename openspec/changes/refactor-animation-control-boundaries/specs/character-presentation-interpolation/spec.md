# character-presentation-interpolation Specification

## MODIFIED Requirements

### Requirement: 动画 visual playback 必须来自表现帧重采样和生命周期注册表

`SimulationActorTickResult` MUST提供构造Presentation Fact所需的committed Body/Intent、稳定Movement Mode作者身份、有限Action channel producer、EventId与playback intent。Movement Mode MUST只从已提交Motion的Locomotion owner或Gameplay Result owner投影，MUST不消费Action owner，并在表现重采样期间保持离散。有限Action Sample MUST作为committed raw time锚点进入Action sample history，MUST不携带最终骨骼Pose。Presentation MUST重采样Body/Intent生成typed Fact，把正式Action Timeline采样降低为exact Action playback，并在相邻committed sample之间按presentation delta投影visual time。PoseStateMachine、SequencePlayer、MarkerSync、Slot、Animancer source backend与编译Pose Plan MUST在PresentationFrame依次执行持续Pose选择、source sampling、Action插入、时间连续性、空间合成和world-aware处理。Kernel MUST不记录PoseState workspace、Marker relation、Animancer state、Player entry、Pose Value或Slot weight。

#### Scenario: 普通Locomotion表现帧

- **WHEN** Committer提供移动Body但没有Action producer
- **THEN** Presentation MUST重采样movement fact并求值Locomotion PoseStateMachine
- **AND** 离散Movement Mode身份 MUST来自同一份committed Motion结果
- **AND** Slot MUST透传基础Pose

#### Scenario: Attack Timeline选中producer

- **WHEN** Committer收到FullBodyAction producer command
- **THEN** Presentation MUST通过Projection生成对应Action Selection
- **AND** FullBodyAction Slot MUST按typed edge消费

### Requirement: Timeline pose time与显式Player time必须独立连续推进

CharacterSimulationState MUST保存Gameplay Timeline logic time。`ActionCommittedSampleHistory` MUST保存已提交的Action raw sample锚点；`ActionPresentationSampleProjector` MUST按presentation delta在锚点之间生成独立`ProjectedPresentationSampleTime`；每个SequencePlayer、Action Player与transition clock MUST只在PresentationFrame推进。新committed sample、rollback replacement或stream reset MUST按完整playback identity重基线表现投影。Animancer MUST只按resolved sample descriptor采样。Projected time MUST不覆盖committed raw time，不得写回Timeline或产生Window、Motion、Warp、Cue与Action lifecycle。持续Pose source MUST直接使用presentation-owned clock，不进入Action committed sample history。

#### Scenario: 两个Logic Tick之间渲染

- **WHEN** 多个PresentationFrame发生在两个SimulationTick之间
- **THEN** Action projected time、SequencePlayer、Slot transition与最终visual animation sample MUST连续推进
- **AND** Timeline Gameplay state、committed raw time与Action lifecycle MUST保持不变

#### Scenario: committed sample校准表现时间

- **WHEN** 下一份同identity committed Action sample到达
- **THEN** Projector MUST以该sample重基线后继续表现推进
- **AND** MUST不把此前外推结果当作Gameplay事实

#### Scenario: Sample command进入表现层

- **WHEN** Simulation Tick提交Action Sample command
- **THEN** 该command MUST只更新Action committed sample history
- **AND** Clip采样、Pose混合、Slot与IK MUST仍由PresentationFrame执行

### Requirement: 表现插值必须提供调试可追踪性

Diagnostics SHOULD暴露Body SourceMode、logic tick、interpolation alpha、Presentation Fact、PoseState active/target identity、TimeInState、Action channel selection、playback generation、Slot identity、MarkerSync raw/effective time、source usage、BlendStack entry/Stored、Routing lifecycle、Inertialization residual、Pose availability、world-aware completion与错误。Debug MUST不成为Gameplay、State Rule、Selection、Slot、Player或Graph输入。

#### Scenario: 排查Action与Locomotion快速切换

- **WHEN** Action结束且Body从移动减速到静止
- **THEN** Logic Trace MUST显示Action release与committed Body
- **AND** Animation Trace MUST显示Slot退出和PoseStateMachine当前基础State

### Requirement: 动画重入必须遵守显式Player连续性语义

同一Action AnimationChannel收到新selection identity或rollback替换时，AnimationSlot MUST按compiled Blend Logic执行Action source handoff；PoseStateMachine transition MUST独立按Fact和state edge执行。SequencePlayer、SelectedPosePlayer、BlendSpacePlayer、BlendStack与Inertialization MUST各自只拥有编译分配的连续性状态。Rollback Pipeline MUST不维护第二套CrossFade、Inertial、PoseState或动画时间轴。

#### Scenario: Replay改变Attack producer

- **WHEN** predicted Attack2在replay后被Attack1替换
- **THEN** FullBodyAction Slot MUST按图定义接管新source
- **AND** Locomotion PoseStateMachine MUST继续由selected Body事实驱动

### Requirement: Rollback 动画同步必须来自同一 Gameplay 输入模拟

Rollback MUST从同一Gameplay input与Program执行重新产生committed Body/Intent、Action EventId replacement和有限Action Selection。Presentation MUST从修正后的Body/Intent重新构造Fact并本地求值PoseStateMachine、AnimationSlot与完整Pose Plan。网络协议 MUST不发送PoseState、AnimationClip、Animancer state、Player entry、Pose Value、normalized time或最终visual Pose。

#### Scenario: 对端修正动作与移动

- **WHEN** rollback重新产生新的Action selection和Body速度
- **THEN** 本地Slot MUST按新Action identity接管，PoseStateMachine MUST按新Fact重新求值
- **AND** 网络 MUST不发送或恢复旧Pose transition、Slot或BlendStack entry
