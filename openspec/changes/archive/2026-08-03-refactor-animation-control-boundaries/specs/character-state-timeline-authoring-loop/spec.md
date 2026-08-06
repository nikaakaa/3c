# character-state-timeline-authoring-loop Specification

## MODIFIED Requirements

### Requirement: Corin RootTree 必须只表达角色主流程层

Corin RootTree MUST作为角色每tick主流程编排层，包含输入、Gameplay移动控制、Locomotion Gameplay StateMachine和Action StateMachine等高层节点。RootTree MUST不平铺具体Attack Timeline、window、cue或lifecycle，也 MUST不平铺Idle、Walk、Run、Start、Stop、Turn等纯表现Pose State。持续Locomotion PoseStateMachine MUST只存在于Presentation Pose Graph。

#### Scenario: 打开Corin RootTree

- **WHEN** 作者打开Corin RootTree
- **THEN** 作者 SHOULD看到Gameplay Locomotion与Action控制入口
- **AND** Attack1的Timeline细节 MUST位于Action State下钻图
- **AND** Locomotion Pose State MUST通过Open Presentation导航查看而不成为RootTree节点

### Requirement: Corin Locomotion 必须使用 StateMachine + Timeline 编排

Corin BTSMTL Locomotion StateMachine MUST只表达输入准入、Gameplay movement mode、Motion authority、转向与加速度约束、Action对移动的打断以及其它影响Simulation结果的控制。只有具有Gameplay时序、Motion或事实输出的移动行为 MAY使用Timeline；它 MUST不包含只为Idle、WalkStart、WalkLoop、RunStart、RunLoop、RunEnd、MovingTurn动画存在的Timeline playback，不提交BaseLocomotion AnimationSelection，并 MUST不使用ActionOverride停止基础Pose输出。持续Pose选择 MUST由Corin Pose Graph中的Locomotion PoseStateMachine完成。

#### Scenario: 角色开始跑动

- **WHEN** Gameplay接受移动输入并由Motor产生速度
- **THEN** BTSMTL MUST只更新移动控制和committed Body结果
- **AND** PoseStateMachine MUST根据Fact选择Start或Locomotion Pose

#### Scenario: FullBody Action活跃

- **WHEN** Action取得Motion authority
- **THEN** Gameplay Locomotion control MUST按正式Motion arbitration让渡或限制Motor
- **AND** MUST不进入ActionOverride动画状态

### Requirement: Corin 基础连招必须使用 Action StateMachine + Timeline 编排

Corin Attack1至Attack5、DodgeForward与DodgeBack MUST继续由Action StateMachine、ActionProfile、Action Context和inline Timeline表达准入、连段、Motion、Window、Cue、完成与打断。每个有限Action Timeline MAY拥有AnimationTrack并向FullBodyAction Slot提交exact playback。Action Timeline MUST不决定Locomotion PoseState或动作结束后的Idle/Run恢复动画。

#### Scenario: Attack1自然结束

- **WHEN** Attack1 Timeline完成并提交Action release
- **THEN** Gameplay MUST完成Action lifecycle并释放Motion authority
- **AND** FullBodyAction Slot MUST过渡回当前Locomotion Source Pose

#### Scenario: Attack被Dodge打断

- **WHEN** Gameplay规则允许Dodge替换Attack
- **THEN** source Action Timeline MUST按统一Runnable与Action lifecycle停止
- **AND** Slot MUST只处理Attack playback到Dodge playback的Pose transition

### Requirement: Corin TimelineNode 必须默认拥有 inline Timeline

Corin有限Action与真正包含Gameplay时序的移动行为 Timeline MUST默认保存为对应TimelineNode私有的inline TimelineData。纯Idle、Start、Loop、Stop与Turn动画 MUST使用Presentation Pose source，MUST不为其创建TimelineNode或inline Timeline。Compiler MUST把保留的inline/shared Timeline编译为同一不可变Program与Action Playback合同，不得创建runtime clone。

#### Scenario: 下钻Attack1 Timeline

- **WHEN** 作者从Attack1 State body打开TimelineNode
- **THEN** Timeline Editor MUST显示Animation、Motion与Decision Tree tracks
- **AND** playback MUST属于Attack1 Action Context

#### Scenario: 编辑Run Pose

- **WHEN** 作者需要替换持续Run动画或marker
- **THEN** 必须导航到Presentation Profile的Run Pose source binding
- **AND** MUST不创建RunLoop inline Timeline

### Requirement: Corin 必须由逻辑层按Animation Channel提交唯一playback selection

Corin Gameplay MUST只为FullBodyAction及其它有限Gameplay-owned channel提交唯一playback selection。Locomotion、Action、Dodge与nested combo MUST在逻辑层完成Gameplay状态、打断和Action channel所有权；持续BaseLocomotion MUST不再是AnimationChannel，也 MUST不提交AnimationPlaybackId。Pose Graph MUST从Presentation Fact选择Locomotion Pose，并通过FullBodyAction Slot组合有限Action。

#### Scenario: Locomotion正常运行

- **WHEN** 当前没有FullBodyAction且Body正在移动
- **THEN** Program MUST不提交BaseLocomotion selection
- **AND** PoseStateMachine MUST从movement fact生成基础Pose

#### Scenario: Attack1进入Attack2

- **WHEN** 连段规则完成Attack1并激活Attack2
- **THEN** Program MUST在FullBodyAction提交Attack2唯一playback
- **AND** Slot MUST处理Attack1到Attack2的Pose transition

### Requirement: Corin WalkLoop 与 RunLoop 必须共享 Locomotion.Gait

Corin Walk与Run Presentation Pose source MAY在同一Locomotion PoseStateMachine可达分支中共享`Locomotion.Gait` SyncGroup。启用时，两项source binding MUST按真实AnimationClip配置完整Cyclic marker sequence，MarkerSync MUST只影响Pose sample time，MUST不改变Pose transition rule、Gameplay movement、Motion request或WorldSolver结果。Walk/Run MUST不为marker同步恢复Timeline producer。

#### Scenario: Walk Pose切换Run Pose

- **WHEN** PoseStateMachine或BlendSpace source从Walk handoff到Run且两侧marker完整
- **THEN** MarkerSync MUST按Locomotion.Gait有向pair映射target sample time
- **AND** Gameplay Program MUST不产生WalkLoop或RunLoop playback

### Requirement: Corin 全部 AnimationTrack 必须显式选择 Marker Sync 策略

迁移后Corin每个可达有限Action AnimationTrack MUST显式配置`None`或`MarkerGroup`，不得保留Unspecified。持续Locomotion Pose source MUST在Profile binding独立显式配置自己的sync mode、group、topology、role与marker；它们不计入Timeline AnimationTrack inventory。选择 MUST根据真实资源语义、Action Timeline call site或Pose source loop capability及完整coverage作出，不得按显示名称硬编码。没有Action AnimationTrack或Pose source的Gameplay状态 MUST不创建伪Timeline、clip或marker。

#### Scenario: 检查Corin作者清单

- **WHEN** Compiler遍历Action Timeline与Presentation Pose source
- **THEN** 每个owner MUST拥有明确sync mode
- **AND** 任一Unspecified配置 MUST阻止发布并定位真实owner

#### Scenario: Gameplay状态没有动画资源

- **WHEN** 某Gameplay movement state只管理Motor约束
- **THEN** 迁移 MUST不为它创建AnimationTrack或fallback clip
- **AND** Pose选择 MUST由Presentation Fact与PoseStateMachine独立决定

### Requirement: Corin 有限动作只能在资源满足时加入 Marker Group

Attack1至Attack5、Dodge及其它有限Action producer MAY配置`MarkerGroup/Finite`，但仅当真实clip从frame 0到DurationFrame具有完整coverage，并满足同AnimationSlot可达集合、同组directed pair契约。RunStart、RunEnd、MovingTurn等迁移后的Pose source MAY在Profile binding配置Finite MarkerGroup，但 MUST不再作为Action Timeline producer。资源不满足时owner MUST显式配置None并使用raw sample与自己的Transition/Slot Blend Logic；不得伪造支撑marker。Combo、recovery、cancel、IFrame、damage与Gameplay Motion MUST继续由Action Context、Timeline window、ConditionRule和Gameplay State决定。

#### Scenario: Stop Pose source具有完整步态marker

- **WHEN** Stop真实动画能够表达Locomotion.Gait全部有向segment并覆盖完整duration
- **THEN** 作者 MAY在Profile binding把它配置为MarkerGroup/Finite
- **AND** PoseState Source Sync MUST使用通用Cyclic到Finite映射

#### Scenario: Attack动画没有共同姿态契约

- **WHEN** Attack1与Attack2没有同组完整marker语义
- **THEN** 两者AnimationTrack MUST显式为None
- **AND** 连段准入 MUST继续由ComboAccept window和Gameplay transition决定

#### Scenario: Action退出到Locomotion

- **WHEN** Action producer为None并结束
- **THEN** AnimationSlot MUST按compiled Action-to-Source Pose规则回到同帧当前Locomotion Pose
- **AND** MUST不从Action名称、Timeline时间或旧BaseLocomotion selection伪造步态phase

### Requirement: Corin animation producer 必须绑定正式source与Player policy

Corin每个有限Action Timeline animation producer MUST拥有稳定presentation identity、FullBodyAction channel binding与Profile resource binding。每个持续Locomotion Sequence/BlendSpace source MUST拥有Graph-owned typed Source Slot与Profile-owned typed Binding子资产，并在Projection中降低为dense source index。PoseState transition与Slot transition MUST分别来自对应node-local Policy；Graph、Gameplay State edge和Timeline MUST不保存作者Source Id字符串或另一份表现transition策略。

#### Scenario: 配置Attack1至Attack5

- **WHEN** Profile Inspector显示五个Action producer
- **THEN** 必须显示各自stable identity、FullBodyAction Slot与resource binding
- **AND** 不得把它们列为Locomotion Pose State

#### Scenario: 配置Run source

- **WHEN** Profile Inspector显示Run Presentation Pose source
- **THEN** 必须显示source id、Sequence/BlendSpace消费者与resource binding
- **AND** 不得要求Timeline producer identity

### Requirement: Corin Marker Sync 配置必须通过正式 Agent Document authoring迁移

有限Action AnimationTrack的sync配置 MUST继续通过正式Agent Document/authoring事务写入Timeline。持续Locomotion Pose source的marker配置 MUST通过共享Presentation Capability与唯一Presentation Mutation写入Profile source owner，并进入Document v3 Presentation editable目标状态；Agent MUST不把Pose source marker伪装为Timeline Track marker或提供第二写入口。

#### Scenario: Agent导出Corin动画摘要

- **WHEN** 导出迁移后的Corin Document
- **THEN** Action Timeline marker MUST按Timeline owner输出
- **AND** Locomotion Pose source marker MUST按Profile binding只读输出

## REMOVED Requirements

### Requirement: Corin 循环 locomotion 状态必须使用 TimelineNode Loop 播放模式

**Reason**: 持续Locomotion动画改由PoseStateMachine内的SequencePlayer、BlendSpacePlayer或Motion Matching source播放，Gameplay Timeline不再承担循环Pose。

**Migration**: 把Idle、WalkLoop和RunLoop Clip、marker与Foot Analysis binding迁入Presentation Pose source，并删除对应TimelineNode。

#### Scenario: 迁移完成

- **WHEN** Corin正式Projection已经使用Locomotion PoseStateMachine
- **THEN** Gameplay Graph MUST不存在只为循环Pose运行的TimelineNode
- **AND** SequencePlayer或BlendSpacePlayer MUST拥有表现sample clock

## ADDED Requirements

### Requirement: Corin旧Locomotion Timeline数据必须原子迁移

现有Idle、WalkStart、WalkLoop、RunStart、RunLoop、RunEnd与MovingTurn Timeline中的数据 MUST按用途迁移：AnimationClip、表现marker、Foot Placement Weight曲线与Foot Analysis identity迁入Presentation Pose source binding；真实影响Body的Motion数据迁入唯一Gameplay Motion authoring；无正式消费方的数据删除。迁移完成后 MUST删除旧TimelineNode、AnimationChannel producer binding、source binding、lifecycle配置和generated producer contract，MUST不保留旧新双写。

#### Scenario: 迁移RunLoop

- **WHEN** RunLoop Timeline的AnimationTrack只负责循环Pose和Locomotion marker
- **THEN** Clip与marker MUST迁入Run Presentation Pose source
- **AND** RunLoop Timeline producer MUST删除

#### Scenario: Locomotion Timeline包含Gameplay MotionCurve

- **WHEN** 曲线确实参与CharacterMotionRequest
- **THEN** 曲线 MUST迁入明确Gameplay Motion owner并保持唯一消费链
- **AND** PoseStateMachine MUST不读取该曲线驱动World movement

### Requirement: Corin必须使用PoseStateMachine加Action Slot的唯一表现拓扑

Corin Presentation Pose Graph MUST以typed Presentation Fact驱动Locomotion PoseStateMachine，以FullBodyAction exact playback驱动Animation Slot，并在同一Pose Plan完成transition、composition、FootPlacement与Output。Corin MUST不同时保留BaseLocomotion Selection Input或旧Timeline Player。

#### Scenario: 检查Corin正式Pose Graph

- **WHEN** Projection Compiler解析Corin Profile
- **THEN** MUST发现唯一Locomotion PoseStateMachine和唯一FullBodyAction Slot
- **AND** MUST拒绝可达BaseLocomotion Gameplay Selection Input
