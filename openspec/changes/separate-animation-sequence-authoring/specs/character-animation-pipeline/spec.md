## MODIFIED Requirements

### Requirement: Gameplay Timeline只能提交有限Action播放事实

Compiler MUST把有限Action Timeline的Sequence Segment降低为稳定producer binding、AnimationChannel binding、committed sample contract与引用的Sequence plan。SimulationTick MUST只推进Gameplay Timeline logic time并提交Select、Sample、Complete或Release command；PresentationFrame sampler MUST按committed raw sample、Segment ClipIn/Weight/Ease和Sequence plan生成Action playback frame与typed parameter page。Marker、Time Mapping、素材Curve与Notify MUST从Sequence plan解析；Timeline MUST不复制这些素材数据、不解析Pose或创建transition、Bone Mask或IK plan。持续Idle、Walk、Run、Start、Stop与Turn MUST不依赖Gameplay Timeline或AnimationChannel。

#### Scenario: Attack Timeline采样Sequence Segment

- **WHEN** Attack Timeline在当前logic sample命中一个Attack Sequence Segment
- **THEN** committed Action command MUST保存Timeline producer与Segment identity
- **AND** Presentation MUST从Segment引用的Sequence plan解析Clip、Marker与素材Curve
- **AND** Sequence Notify MUST不作为Gameplay Timeline事实重复提交

#### Scenario: Attack Timeline同时产生Window与动画

- **WHEN** Attack Timeline在一个SimulationTick推进Window并选择Sequence Segment producer
- **THEN** Window MUST进入Gameplay事实链
- **AND** Action playback command MUST进入Presentation-owned inbox

#### Scenario: Locomotion持续播放

- **WHEN** 角色保持Run
- **THEN** PoseStateMachine的state-local provider MUST推进Run Sequence source
- **AND** Program MUST不创建Run Timeline producer

### Requirement: Marker effective time必须由source-local计划解析

Action Timeline与Pose source只提交raw sample及精确Sequence identity。PoseState Transition的Source Sync Plan、Blend Space内部phase plan或AnimationSlot的Action source usage MUST在采样前从两侧Sequence plan读取Marker Group、Time Mapping、topology、role与occurrence，生成effective time并在共同可见期持续求值。Timeline Track、Profile Binding、Blend Space sample与Transition MUST不保存Marker副本；Runtime MUST不按State、Action、clip或Sequence显示名建立relation。

#### Scenario: Walk到Run同步

- **WHEN** Pose Transition两侧Source Binding分别引用共享合法MarkerGroup的Walk与Run Sequence
- **THEN** Runtime MUST按编译Sequence计划映射target effective sample
- **AND** Gameplay movement MUST不等待marker边界

#### Scenario: Action Segment同步

- **WHEN** AnimationSlot relation两侧Action Segment引用兼容Sequence
- **THEN** Action source usage MUST从Sequence计划解析Marker relation
- **AND** Timeline AnimationTrack MUST不提供第二份Marker binding

#### Scenario: Action不同步

- **WHEN** Action Segment引用的Sequence SyncMode为None
- **THEN** Action source MUST使用raw visual time
- **AND** Runtime MUST不自动补relation

## ADDED Requirements

### Requirement: Sequence Notify不得进入Gameplay Timeline执行

Sequence Notify MUST只由PresentationFrame的正式Sequence sampler按typed consumer合同发布为表现事件或只读Preview/Diagnostics。SimulationTick、Gameplay Timeline、TreeClip、Action lifecycle、Window、Cue、Motion、Warp与StateMachine MUST不读取或执行Sequence Notify；rollback或presentation resample MUST按完整Sequence playback identity避免重复表现提交，但 MUST不把Notify写入Gameplay Snapshot或Network。

#### Scenario: 两个表现帧跨过同一Footstep Notify

- **WHEN** Presentation按同一playback generation跨过Sequence Notify frame
- **THEN** 注册的表现consumer MUST按正式生命周期至多接收一次该occurrence
- **AND** Gameplay Program与Action Timeline事实 MUST保持不变
