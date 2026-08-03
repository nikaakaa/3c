# character-state-timeline-authoring-loop Specification

## Purpose

定义Corin Gameplay StateMachine、有限Action Timeline与Presentation PoseState的唯一职责边界：BTSMTL只拥有Gameplay控制、Motion、Action、Window、Cue与有限Action时间，持续Locomotion姿态只由Presentation Fact、PoseStateMachine和state-local source选择。

## Requirements

### Requirement: Corin RootTree 必须只表达角色主流程层

Corin RootTree MUST作为角色每tick主流程编排层，包含输入、Gameplay移动控制、Locomotion Gameplay StateMachine和Action StateMachine等高层节点。RootTree MUST不平铺具体Attack Timeline、window、cue或lifecycle，也 MUST不平铺Idle、Walk、Run、Start、Stop、Turn等纯表现Pose State。持续Locomotion PoseStateMachine MUST只存在于Presentation Pose Graph。

#### Scenario: 打开Corin RootTree

- **WHEN** 作者打开Corin RootTree
- **THEN** 作者 SHOULD看到Gameplay Locomotion与Action控制入口
- **AND** Attack1的Timeline细节 MUST位于Action State下钻图
- **AND** Locomotion Pose State MUST通过Open Presentation导航查看而不成为RootTree节点

### Requirement: Corin Locomotion StateMachine必须只控制Gameplay运动

Corin BTSMTL Locomotion StateMachine MUST只表达输入准入、Gameplay movement mode、Motion authority、转向与加速度约束、Action对移动的打断以及其它影响Simulation结果的控制。只有具有Gameplay时序、Motion或事实输出的移动行为 MAY使用Timeline；它 MUST不包含只为Idle、WalkStart、WalkLoop、RunStart、RunLoop、RunEnd或MovingTurn动画存在的Timeline playback，不提交BaseLocomotion AnimationSelection，并 MUST不使用ActionOverride停止基础Pose输出。持续Pose选择 MUST由Corin Pose Graph中的Locomotion PoseStateMachine完成。

#### Scenario: 角色开始跑动

- **WHEN** Gameplay接受移动输入并由Motor产生速度
- **THEN** BTSMTL MUST只更新移动控制和committed Body结果
- **AND** Presentation PoseStateMachine MUST根据Fact选择Start或Locomotion Pose

#### Scenario: FullBody Action活跃

- **WHEN** Action取得Motion authority
- **THEN** Gameplay Locomotion control MUST按正式Motion arbitration让渡或限制Motor
- **AND** MUST不进入ActionOverride动画状态

### Requirement: Corin基础连招必须使用Action StateMachine和Timeline编排

Corin外层Action StateMachine MUST只表达`None`、`Attack`和`Dodge`动作大类。Attack1至Attack5 MUST位于Attack State body内的nested StateMachine，DodgeBack与DodgeForward MUST位于Dodge State body内的nested StateMachine。具体leaf MUST唯一拥有ActionProfile、Action Context、inline Timeline、Window、Cue、Motion与lifecycle。连段、恢复取消与replacement MUST复用ConditionRuleGraph、State edge、Runnable stop、source OnExit、Action lifecycle和Timeline cancel，不得创建Action专用旁路。

#### Scenario: Attack1进入Attack2

- **WHEN** Attack1的ComboAccept active、存在Attack request且Attack2 admission成立
- **THEN** source MUST先按统一Action lifecycle关闭
- **AND** target MUST消费request并创建新的ActionInstance
- **AND** FullBodyAction channel MUST提交Attack2 exact playback

#### Scenario: Attack被Dodge打断

- **WHEN** Gameplay规则允许Dodge替换Attack
- **THEN** source Action Timeline MUST按统一Runnable与Action lifecycle停止
- **AND** AnimationSlot MUST只处理Attack playback到Dodge playback的Pose transition

#### Scenario: Action自然结束

- **WHEN** 当前leaf没有更高优先级replacement且Timeline完成
- **THEN** Gameplay MUST完成Action lifecycle并释放Motion authority
- **AND** FullBodyAction AnimationSlot MUST过渡回同帧当前Locomotion Source Pose

### Requirement: Corin一次性状态行为必须默认使用inline Graph

Corin Locomotion Gameplay状态行为和基础连招状态行为 MUST默认保存为StateNode内部inline graph data。只有多个状态明确复用同一行为图时，作者 MAY显式抽取shared `BaseTreeAsset`。外层Action category MUST不复制leaf数据或创建一次性SubTree asset。

#### Scenario: 下钻Attack1

- **WHEN** 作者下钻Attack1 StateNode
- **THEN** 编辑器 MUST打开该StateNode的inline StateBehaviorSubTree
- **AND** 项目 MUST不要求`Attack1SubTree.asset`

### Requirement: Corin有限Action Timeline必须默认使用inline Timeline

Corin有限Action与真正包含Gameplay时序的移动行为 Timeline MUST默认保存为对应TimelineNode私有的inline TimelineData。纯Idle、Start、Loop、Stop与Turn动画 MUST使用Presentation Pose source，MUST不为其创建TimelineNode或inline Timeline。Compiler MUST把保留的inline/shared Timeline编译为同一不可变Program与Action Playback合同，不得创建runtime clone。

#### Scenario: 下钻Attack1 Timeline

- **WHEN** 作者从Attack1 State body打开TimelineNode
- **THEN** Timeline Editor MUST显示Animation、Motion与Decision Tree tracks
- **AND** playback MUST属于Attack1 Action Context

#### Scenario: 编辑Run Pose

- **WHEN** 作者需要替换持续Run动画或marker
- **THEN** 必须导航到Presentation Profile的Run Pose source binding
- **AND** MUST不创建RunLoop inline Timeline

### Requirement: Corin Action Timeline Window必须由owner-local事实表达

Attack1至Attack5、DodgeForward和DodgeBack的inline Timeline MUST以Decision TreeClip和owner-local Bool Frame declaration表达Hit、IFrame、ComboAccept、RecoveryEarly、RecoveryLate与RecoveryOpen。ActionWindow projection MUST保留Action Context、WindowId、Digest、phase和frame range；ConditionRuleGraph与EndFrame fact MUST消费同一candidate。系统 MUST不建立Root-owned per-state window key、WindowTrack、专用submit node、cache或registry。

#### Scenario: Attack窗口

- **WHEN** 作者打开任一Attack inline Timeline
- **THEN** Hit、ComboAccept、RecoveryEarly与RecoveryLate MUST位于该owner
- **AND** projection MUST指向当前ActionInstance

### Requirement: Corin Gameplay只能提交有限Action playback

Corin Gameplay MUST只为FullBodyAction及其它有限Gameplay-owned channel提交唯一playback selection。Locomotion、Action、Dodge与nested combo MUST在逻辑层完成Gameplay状态、打断和Action channel所有权；持续BaseLocomotion MUST不再是AnimationChannel，也 MUST不提交AnimationPlaybackId。Pose Graph MUST从Presentation Fact选择Locomotion Pose，并通过FullBodyAction AnimationSlot组合有限Action。

#### Scenario: Locomotion正常运行

- **WHEN** 当前没有FullBodyAction且Body正在移动
- **THEN** Program MUST不提交BaseLocomotion selection
- **AND** PoseStateMachine MUST从movement fact生成基础Pose

#### Scenario: 同tick切换Locomotion与Action

- **WHEN** 同一logic tick内Gameplay movement mode和Attack ownership均变化
- **THEN** Program MUST只提交最终Gameplay Body事实与FullBodyAction playback
- **AND** Locomotion Pose source MUST由同帧Presentation Fact独立选择

### Requirement: Corin必须使用PoseStateMachine加Action Slot的唯一表现拓扑

Corin Presentation Pose Graph MUST以typed Presentation Fact驱动Locomotion PoseStateMachine，以FullBodyAction exact playback驱动唯一AnimationSlot，并在同一Pose Plan完成transition、composition、TwoBoneIK、FootPlacement与Output。Corin MUST不同时保留BaseLocomotion Selection Input、旧Timeline Player、共享Playback总管或第二动画链。

#### Scenario: 编译Corin Pose Graph

- **WHEN** Projection Compiler解析Corin Profile
- **THEN** MUST发现唯一Locomotion PoseStateMachine和唯一FullBodyAction AnimationSlot
- **AND** MUST拒绝可达BaseLocomotion Gameplay Selection Input

### Requirement: Corin Pose source必须具有稳定binding与node-local policy

Corin每个持续Locomotion Sequence、BlendSpace或Motion Matching source MUST拥有Graph-owned typed Source Slot与Profile-owned typed Binding子资产；Projection Compiler MUST把它们降低为连续dense source index，不得保存作者source/provider字符串。每个有限Action Timeline producer MUST拥有稳定presentation identity、FullBodyAction channel binding与resource binding。PoseState transition与Slot transition MUST分别来自对应node-local Policy；Graph Gameplay State edge和Timeline MUST不保存另一份表现transition策略。

#### Scenario: 配置Run source

- **WHEN** Profile Inspector显示Run Presentation Pose source
- **THEN** 必须显示provider/source id、Sequence或BlendSpace消费者与resource binding
- **AND** 不得要求Timeline producer identity

#### Scenario: 配置Attack1至Attack5

- **WHEN** Profile Inspector显示五个Action producer
- **THEN** 必须显示各自stable identity、FullBodyAction AnimationSlot与resource binding
- **AND** 不得把它们列为Locomotion Pose State

### Requirement: Corin Walk与Run MAY共享Locomotion.Gait

Corin Walk与Run Presentation Pose source MAY在同一Locomotion PoseStateMachine可达分支中共享`Locomotion.Gait` SyncGroup。启用时，两项source binding MUST按真实AnimationClip配置完整Cyclic marker sequence；source-local marker映射 MUST只影响Pose sample time，不得改变Pose transition rule、Gameplay movement、Motion request或WorldSolver结果。Walk与Run MUST不为marker同步恢复Timeline producer。

#### Scenario: Walk Pose切换Run Pose

- **WHEN** PoseStateMachine从Walk handoff到Run且两侧marker完整
- **THEN** source-local marker映射 MUST按Locomotion.Gait有向pair解析target sample time
- **AND** Gameplay Program MUST不产生WalkLoop或RunLoop playback

### Requirement: Corin全部动画owner必须显式选择Marker策略

Corin每个可达有限Action AnimationTrack MUST显式配置`None`或`MarkerGroup`，不得保留Unspecified。持续Locomotion Pose source MUST在Profile binding独立配置自己的sync mode、group、topology、role与marker；它们不计入Timeline AnimationTrack inventory。选择 MUST根据真实资源语义、Action Timeline call site或Pose source loop capability及完整coverage作出，不得按显示名称硬编码。

#### Scenario: 检查Corin作者清单

- **WHEN** Compiler遍历有限Action Timeline与Presentation Pose source
- **THEN** 每个真实owner MUST拥有明确sync mode
- **AND** 任一Unspecified配置 MUST阻止发布并定位真实owner

### Requirement: Corin有限动作只能在资源满足时加入Marker Group

Attack1至Attack5、Dodge及其它有限Action producer MAY配置`MarkerGroup/Finite`，但仅当真实clip从frame 0到DurationFrame具有完整coverage，并满足同AnimationSlot可达集合、同组directed pair契约。RunStart、RunEnd、MovingTurn等Pose source MAY在Profile binding配置Finite MarkerGroup，但 MUST不再作为Action Timeline producer。资源不满足时owner MUST显式配置None并使用raw sample与自己的Transition或Slot Blend Logic；不得伪造支撑marker。

#### Scenario: Action没有共同姿态契约

- **WHEN** Attack1与Attack2没有同组完整marker语义
- **THEN** 两者AnimationTrack MUST显式为None
- **AND** 连段准入 MUST继续由ComboAccept window和Gameplay transition决定

#### Scenario: Action退出到Locomotion

- **WHEN** Action producer为None并结束
- **THEN** AnimationSlot MUST按compiled Action-to-Source Pose规则回到同帧当前Locomotion Pose
- **AND** MUST不从Action名称、Timeline时间或旧BaseLocomotion selection伪造步态phase

### Requirement: Corin旧Locomotion Timeline数据必须原子迁移

旧Idle、WalkStart、WalkLoop、RunStart、RunLoop、RunEnd与MovingTurn Timeline中的数据 MUST按用途迁移：AnimationClip、表现marker、Foot Placement Weight曲线与Foot Analysis identity迁入Presentation Pose source binding；真实影响Body的Motion数据迁入唯一Gameplay Motion owner；无正式消费方的数据删除。迁移完成后 MUST删除旧TimelineNode、BaseLocomotion AnimationChannel producer、source binding、lifecycle配置、ActionOverride与旧ownership Blackboard declaration，MUST不保留旧新双写。

#### Scenario: 迁移RunLoop

- **WHEN** RunLoop Timeline的AnimationTrack只负责循环Pose和Locomotion marker
- **THEN** Clip与marker MUST迁入Run Presentation Pose source
- **AND** RunLoop Timeline producer MUST删除

#### Scenario: MovingTurn含Gameplay MotionCurve

- **WHEN** 曲线确实参与CharacterMotionRequest
- **THEN** 曲线 MUST保留在明确Gameplay Motion owner并保持唯一消费链
- **AND** PoseStateMachine MUST不读取该曲线驱动World movement

### Requirement: Corin资产迁移必须通过正式Agent Document事务

有限Action Timeline、Gameplay Graph、Blackboard与旧Locomotion Timeline清理 MUST通过`btsmtl-agent-authoring-document.v2`的`checkout_document -> editable修改 -> dry_run_document -> apply_document(expected_document_hash) -> validate`唯一事务完成。Presentation Pose source、Pose Graph、Rig与Foot Analysis只在Document context只读投影，并通过各自正式authoring service维护。实现 MUST不直接修改Unity YAML、不恢复v17 Patch链、不创建一次性migrator或第二mutation service。

#### Scenario: 应用Corin Document

- **WHEN** dry-run成功并返回exact document hash
- **THEN** apply MUST消费同一hash并在一个Undo事务保存正式authoring
- **AND** 成功后反向导出Package MUST为Clean
- **AND** Document MUST不再包含旧BaseLocomotion、ActionOverride或旧Selection字段

### Requirement: Corin生成产物必须显式重建

Corin作者数据迁移后，Presentation Projection、Float32 Program wrapper与Fixed Program wrapper MUST通过精确Definition的正式显式Build入口重建。Program MUST不包含BaseLocomotion animation producer；Projection MUST包含PoseStateMachine、state-local source、AnimationSlot、完整Rig v3与唯一ordered Pose Plan。三种产物 MUST共享相同Semantic IR/source revision闭包，不得自动Build、部分发布或使用旧wrapper。

#### Scenario: 迁移后显式Build

- **WHEN** Corin Document apply与Presentation authoring migration均成功
- **THEN** 作者 MUST显式触发Projection/Float32 Build与Fixed Build
- **AND** 任一阶段失败 MUST保留明确typed diagnostic且不得发布混合revision
