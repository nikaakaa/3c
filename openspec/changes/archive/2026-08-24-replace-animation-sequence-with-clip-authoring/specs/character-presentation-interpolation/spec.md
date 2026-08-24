## MODIFIED Requirements

### Requirement: 角色表现插值必须基于 logic sample 历史

Presentation MUST从 Pipeline Egress允许并由 Committer提交的 BodyState interval生成visual history。`CharacterPresentationBodyState` MUST保留ActorId、Position、Rotation、LinearVelocity与Grounded；这些值 MUST来自正式Float32、Fixed或observed World Body，不得从Transform或表现帧差分反推。`CharacterBodyPresentationRuntime` MUST是committed interval历史、selected interval、表现时钟、stream reset/replacement、target trajectory sampling、visual correction和visual root pose的唯一owner。Rollback Presentation MUST从Pipeline atomic Commit提交的predicted/confirmed BodyState interval维护同一份visual history；Replay产生替换或撤销时，Body Runtime MUST按ActorId/Tick和显式stream update整批更新历史，不得逐Replay step显示中间Body。Committed branch replacement MUST只删除replacement起点及之后的旧样本，并 MUST在同一presentation sample tick比较旧、新target trajectory。Presentation MUST不修改Float32/Fixed WorldState、已提交Snapshot、Prediction state或Solver输入，也 MUST不直接读取WorldSimulationState、WorldSolver、runtime clone、Network私有history、Transform或MotionDebug作为逻辑真值。

#### Scenario: Local Pipeline 提交 Body Interval

- **WHEN** Standard Local Pipeline发布一个成功SimulationTickResult的BodyState interval
- **THEN** Committer MUST向Body Runtime提交唯一canonical kinematic interval
- **AND** Body Runtime MUST按presentation delta生成并应用visible pose

#### Scenario: Replay 替换 Predicted Pose

- **WHEN** Tick T的predicted BodyState被replay result替换
- **THEN** Rollback Output Commit MUST暂存同一outer transaction的全部BodyResult并只提交Replay后的最终连续分支
- **AND** Body Runtime MUST在同一presentation sample tick比较旧、新target position、rotation与velocity
- **AND** visual correction MUST从上一帧visible pose与visible velocity接管
- **AND** canonical Body MUST立即保持replay后的结果

#### Scenario: Replay 替换已表现移动分支

- **WHEN** replay替换已经表现的Committed Body与Intent分支
- **THEN** Body branch sequence MUST表示新的history revision
- **AND** Presentation Fact的Pose discontinuity generation MUST保持不变
- **AND** PoseStateMachine、Clip Player、Root Orientation Warp与Presentation clock MUST继续当前Locomotion连续状态
- **AND** Foot Placement与Motion Matching trajectory MUST只重定向到新Body分支
- **AND** 只有Initialization或显式Selected Stream Reset MAY推进Pose discontinuity generation并执行硬重置

#### Scenario: 连续移动输入产生高频分支替换

- **WHEN** 相邻PresentationFrame持续收到canonical差异并替换Committed Body分支
- **THEN** 表现Tick游标 MUST保持单调推进
- **AND** 每次替换 MUST从当前visible状态重新计算相对误差
- **AND** MUST不累计旧offset或重置固定时长恢复计时器

#### Scenario: 远端角色保持当前预测时间线

- **WHEN** Peer使用last-known continuous input预测尚未到达的远端输入
- **THEN** 远端Body与动画 MUST继续消费predicted current timeline
- **AND** confirmed horizon MUST不被用作远端表现延迟缓冲
- **AND** canonical差异到达后 MUST通过同一原子Body/动画提交事务纠正

#### Scenario: Grounded target发生分支纠偏

- **WHEN** 新target Body为Grounded且水平姿态需要视觉纠偏
- **THEN** Follower MUST只对水平position error执行有界收敛
- **AND** visible Y MUST直接使用target Y

### Requirement: 动画 visual playback 必须来自表现帧重采样和生命周期注册表

`SimulationActorTickResult` MUST提供构造Presentation Fact所需的committed Body/Intent、有限Action channel producer、EventId与playback intent。有限Action Sample MUST作为committed raw time锚点进入Action sample history，MUST不携带最终骨骼Pose。Presentation MUST重采样Body/Intent生成typed Fact，把正式Action Timeline采样降低为exact Action playback，并在相邻committed sample之间按presentation delta投影visual time。PoseStateMachine、ClipPlayer/BlendSpacePlayer、source-local Phase endpoint、Slot、Animancer source backend与编译Pose Plan MUST在PresentationFrame依次执行持续Pose选择、source phase/time解析、source sampling、Action插入、时间连续性、空间合成和world-aware处理。Kernel MUST不记录PoseState workspace、Phase relation、Animancer state、Player entry、Pose Value或Slot weight。

#### Scenario: 普通Locomotion表现帧

- **WHEN** Committer提供移动Body但没有Action producer
- **THEN** Presentation MUST重采样movement fact并求值Locomotion PoseStateMachine
- **AND** source-local Phase relation MUST只在匹配Profile Group时执行
- **AND** Slot MUST透传基础Pose

#### Scenario: Attack Timeline选中producer

- **WHEN** Committer收到compiled FullBodyAction producer command
- **THEN** Presentation MUST通过Projection生成对应Action playback
- **AND** FullBodyAction Slot MUST按typed edge消费

### Requirement: 表现插值必须提供调试可追踪性

Diagnostics SHOULD暴露Body SourceMode、logic tick、interpolation alpha、Presentation Fact、PoseState active/target identity、TimeInState、Action channel selection、playback generation、Slot identity、source endpoint、Phase raw/effective time、actual coverage、source usage、BlendStack entry/Stored、Routing lifecycle、Inertialization residual、Pose availability、world-aware completion与错误。Debug MUST不成为Gameplay、State Rule、Selection、Slot、Player或Graph输入。

#### Scenario: 排查Action与Locomotion快速切换

- **WHEN** Action结束且Body从移动减速到静止
- **THEN** Logic Trace MUST显示Action release与committed Body
- **AND** Animation Trace MUST显示Slot退出、PoseStateMachine当前基础State与匹配的Phase relation

### Requirement: Timeline pose time与显式Player time必须独立连续推进

CharacterSimulationState MUST保存Gameplay Timeline logic time。`ActionCommittedSampleHistory` MUST保存已提交的Action raw sample锚点；`ActionPresentationSampleProjector` MUST按presentation delta在锚点之间生成独立`ProjectedPresentationSampleTime`；每个ClipPlayer、BlendSpacePlayer、Action Player与transition clock MUST只在PresentationFrame推进。新committed sample、rollback replacement或stream reset MUST按完整playback identity重基线表现投影。Animancer MUST只按resolved sample descriptor采样。Projected time MUST不覆盖committed raw time，不得写回Timeline或产生Window、Motion、Warp、Cue与Action lifecycle。持续Pose source MUST直接使用presentation-owned raw clock，并 MAY通过Projection Phase endpoint得到effective sample；它 MUST不进入Action committed sample history。Body Visual Trajectory Follower MUST不修改Animation sample、Player delta、Phase continuation、Pose Plan completion或playback generation。

#### Scenario: 两个Logic Tick之间渲染

- **WHEN** PresentationFrame在下一个SimulationTick前推进
- **THEN** Action projected time、ClipPlayer或BlendSpacePlayer、Slot transition与最终visual animation sample MUST连续推进
- **AND** Timeline Gameplay state、committed raw time与Action lifecycle MUST保持不变

### Requirement: 动画重入必须遵守显式Player连续性语义

同一Action AnimationChannel收到新selection identity或rollback替换时，AnimationSlot MUST按compiled Blend Logic执行Action source handoff；PoseStateMachine transition MUST独立按Fact和state edge执行。ClipPlayer、SelectedPosePlayer、BlendSpacePlayer、BlendStack与Inertialization MUST各自只拥有编译分配的连续性状态；Phase continuation MUST只属于source-local endpoint，不成为第二生命周期。Rollback Pipeline MUST不维护第二套CrossFade、Inertial、PoseState、Phase relation或动画时间轴。

#### Scenario: Replay改变Attack producer

- **WHEN** predicted Attack2在replay后被Attack1替换
- **THEN** FullBodyAction Slot MUST按图定义接管新source
- **AND** Locomotion PoseStateMachine与source-local Phase endpoint MUST继续由selected Body事实驱动
