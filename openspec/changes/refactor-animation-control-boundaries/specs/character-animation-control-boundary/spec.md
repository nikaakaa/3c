# character-animation-control-boundary Specification

## ADDED Requirements

### Requirement: Gameplay与Presentation必须分别拥有行为选择和持续姿势选择

Gameplay Program MUST唯一决定Action准入、Action实例、Gameplay movement mode、Motion contribution、Window、Cue与打断结果。Pose Graph MUST唯一决定持续Locomotion的Idle、Start、Move、Stop、Turn、Jump、Fall与Land等Pose State及其Transition。Gameplay Program MUST不提交具体Locomotion AnimationClip、Pose State或Pose transition；Pose Graph MUST不激活Action、不修改Gameplay State、不提交Motion或World request。

#### Scenario: 角色从静止开始移动

- **WHEN** committed Body与Intent使Presentation Fact从静止变为有期望速度
- **THEN** PoseStateMachine MUST按typed fact从Idle切换到Start或Locomotion
- **AND** Gameplay Program MUST不提交WalkStart或RunLoop animation producer

#### Scenario: 攻击输入被Gameplay拒绝

- **WHEN** Gameplay Action admission因资源或状态拒绝攻击
- **THEN** Action Timeline MUST不创建有效playback
- **AND** Slot与PoseStateMachine MUST不根据输入自行播放攻击Pose

### Requirement: Presentation Fact必须是只读committed投影

系统 MUST从同一committed Simulation、Body与Intent结果构造版本化`CharacterPresentationFactFrame`。Fact MUST使用typed id保存Grounded、速度、加速度、方向、朝向误差、垂直速度、MovementMode、稳定Motion phase与discontinuity。Fact MUST不保存Gameplay Timeline或MotionCurve sample、AnimationClip、PoseStateId、TransitionId、Blend Logic或mutable CharacterSimulationState地址，也 MUST不进入Rollback snapshot或网络协议。Pose Runtime MUST只读取当前有效frame，MUST不写回Fact或Gameplay。

#### Scenario: 两个表现帧位于同一Simulation Tick之间

- **WHEN** Presentation interpolation产生两个不同render frame
- **THEN** 两帧 MAY产生不同插值速度与方向fact
- **AND** MUST不重复执行Gameplay Timeline、Motion、Window、Cue或Action mutation

#### Scenario: Simulation correction替换Body结果

- **WHEN** committed correction提升Body discontinuity generation
- **THEN** Presentation Fact MUST携带新的generation
- **AND** PoseStateMachine与Inertialization MUST按正式reset规则处理而不是读取旧mutable state

#### Scenario: MovingTurn进入表现插值

- **WHEN** committed Body与Intent进入MovingTurn MovementMode
- **THEN** Presentation Fact MUST携带MovementMode、Facing Error与Body discontinuity
- **AND** Turn Sequence与RootOrientationWarp MUST按Presentation自己的连续sample求值，不读取Gameplay MotionCurve相位

### Requirement: 有限Action必须通过exact playback进入Slot

Attack、Dodge、Hit、Death、Interaction与其它有限Action的动画表现 MUST由Gameplay已确认的Action Timeline或等价有限playback提交versioned exact playback。Playback MUST包含Action/producer identity、generation、权威raw visual time、continuous time、loop、play rate、source-local clip sample与表现参数；MUST不包含PoseState transition或最终Pose weight。Slot MUST只消费该playback，不得重新选择Action。

#### Scenario: Attack1进入Attack2

- **WHEN** Gameplay连段规则结束Attack1并启动Attack2 Timeline
- **THEN** FullBodyAction Slot MUST收到Attack2 exact playback generation
- **AND** Slot MUST只决定Attack1到Attack2如何混合

#### Scenario: Action结束

- **WHEN** Gameplay提交Action playback release
- **THEN** Slot MUST按compiled Blend Logic过渡回当前Source Pose
- **AND** Gameplay MUST不提交RunLoop或Idle动画作为恢复目标

### Requirement: Motion authority与Pose coverage必须分离

Action对Character Motor的控制 MUST通过Action/Motion arbitration表达；Action对全身或局部Pose的覆盖 MUST通过Slot与Pose composition表达。两者 MUST不共享`HasActionLocomotionOwnership`、ActionOverride Pose State或按动画名称恢复的状态路由。全身Slot活跃期间Locomotion PoseStateMachine MUST继续消费最新Presentation Fact并生成Source Pose。

#### Scenario: Root Motion Dodge覆盖全身

- **WHEN** Dodge Action同时获得Motion authority并在FullBody Slot播放
- **THEN** Motor MUST只消费Dodge Motion request
- **AND** Locomotion PoseStateMachine MUST继续更新但最终Pose由FullBody Slot覆盖

#### Scenario: 上半身动作不取得移动权

- **WHEN** Action只在UpperBody Slot播放且未取得Motion authority
- **THEN** Character Motor MUST继续执行Locomotion movement
- **AND** Layered Blend Per Bone MUST只覆盖配置骨骼

### Requirement: 有限Action与state-local Pose source必须使用不同身份合同

有限Action command与playback frame MUST使用`AnimationPlaybackId`、`ActionInstanceId`、`AnimationChannelId`、Gameplay producer identity与committed raw sample表达Gameplay已经确认的有限实例。SequencePlayer、BlendSpacePlayer与Motion Matching state-local source MUST在authoring中使用类型化Source Slot对象与Profile Binding子资产，在Projection/Runtime中使用dense source index、PlayerNodeId、source generation、frame lease、availability与表现sample表达PoseState内部来源。state-local source MUST不携带作者对象，也 MUST不携带或伪造`AnimationPlaybackId`、`AnimationChannelId`、`ProgramProducerIndex`或Action lifecycle phase。

#### Scenario: Locomotion State使用Motion Matching

- **WHEN** PoseStateMachine使一个Motion Matching Player relevant
- **THEN** MM MUST按state-local provider identity发布Pose source sample
- **AND** MUST不创建Gameplay playback或占用Action channel

#### Scenario: Attack Timeline提交Action

- **WHEN** Gameplay已经确认Attack ActionInstance并提交Timeline sample
- **THEN** Action command MUST保存同一个ActionInstance、producer、channel与playback generation
- **AND** Slot MUST通过Action Playback Input消费该有限实例

### Requirement: Action Slot无Action占用必须表示Source Pose

AnimationSlot MUST把没有有限Action的正常占用表示为`SourcePoseEndpoint`，并持续透传同帧Source Pose。`SourcePoseEndpoint` MUST与`NoPose`分离；Routing plan、exact matrix、snapshot与作者UI MUST不使用`Empty`同时表达“没有Action”和“没有有效Pose”。

#### Scenario: FullBodyAction没有活动Action

- **WHEN** Slot没有活动或保留的Action playback且Source Pose有效
- **THEN** Slot route MUST位于`SourcePoseEndpoint`
- **AND** Slot输出 MUST是当前持续更新的Source Pose而不是Empty Pose

#### Scenario: Source Pose本身不可用

- **WHEN** Required PoseState source返回Invalid
- **THEN** Slot MUST报告`NoPose`或上游typed failure
- **AND** MUST不把该状态解释为正常无Action占用

#### Scenario: Source Pose端点快速切入下一Action

- **WHEN** Slot当前占用为`SourcePoseEndpoint`
- **AND** Blend Stack自身上一完成值为`NoPose`且新Action命中快速替换阈值
- **THEN** Blend Stack MUST原子替换无输出的Source Pose历史并启动新Action source
- **AND** MUST不对不存在的Stack Pose执行Stored Pose捕获
- **AND** Action source或Invalid输出仍 MUST要求合法completed Pose才能执行历史压缩
