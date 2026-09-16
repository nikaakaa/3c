# character-state-timeline-authoring-loop Specification

## Purpose

定义Corin Gameplay StateMachine、有限Action Timeline与Presentation PoseState的唯一职责边界：BTSMTL只拥有Gameplay控制、Motion、Action、Window、Cue与有限Action时间，持续Locomotion姿态只由Presentation Fact、PoseStateMachine和state-local source选择。

## Requirements

### Requirement: Corin Skill Graph必须表达角色Gameplay流程层

AbilityGraph MUST作为对应GameplayAbility的Gameplay执行层，包含输入、Gameplay移动控制、Action StateMachine、Action Timeline和有限Gameplay生命周期内容。角色主线状态与Movement规则属于C# ControlModule和正式 domain state；AbilityGraph不得重新承载角色总RootTree。AbilityGraph MUST不平铺Idle、Walk、Run、Start、Stop、Turn等纯表现Pose State，持续Locomotion PoseStateMachine MUST只存在于Presentation Pose Graph。

#### Scenario: 打开Corin Skill Graph

- **WHEN** 作者从Definition的SkillId和EntryGraphAuthoringId打开Corin Skill Graph
- **THEN** 作者 SHOULD看到该Skill的Gameplay执行入口
- **AND** Attack1的Timeline细节 MUST位于Action State下钻图
- **AND** Locomotion Pose State MUST通过Open Presentation导航查看而不成为Skill Graph节点

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

Corin外层Action StateMachine MUST只表达`None`、`Attack`和`Dodge`动作大类。Attack1至Attack5 MUST位于Attack State body内的nested StateMachine，DodgeBack与DodgeForward MUST位于Dodge State body内的nested StateMachine。具体Ability MUST唯一拥有GameplayAbilityAdmissionProfile、当前执行上下文、inline Timeline、Window、Cue、Motion与lifecycle。连段、恢复取消与replacement MUST复用ConditionRuleGraph、State edge、Runnable stop、Ability lifecycle和Timeline cancel，不得创建Action专用旁路或作者OnExit终态链。

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

Corin有限Action与真正包含Gameplay时序的移动行为 Timeline MUST默认保存为对应TimelineNode私有的inline TimelineData。纯Idle、Start、Loop、Stop与Turn动画 MUST使用Presentation Pose source，MUST不为其创建TimelineNode或inline Timeline。Graph/domain preparation MUST保留inline/shared Timeline引用并连接到正式 Action Playback 合同，不得创建runtime clone或Timeline专用编译产物。

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
- **THEN** Gameplay/Ability owner MUST 不提交 BaseLocomotion selection
- **AND** PoseStateMachine MUST从movement fact生成基础Pose

#### Scenario: 同tick切换Locomotion与Action

- **WHEN** 同一logic tick内Gameplay movement mode和Attack ownership均变化
- **THEN** Gameplay/Ability owner MUST 只提交最终 Gameplay Body 事实与 FullBodyAction playback
- **AND** Locomotion Pose source MUST由同帧Presentation Fact独立选择

### Requirement: Corin必须使用PoseStateMachine加Action Slot的唯一表现拓扑

Corin Presentation Pose Graph MUST以typed Presentation Fact驱动Locomotion PoseStateMachine，以FullBodyAction exact playback驱动唯一AnimationSlot，并在同一Pose Plan完成transition、composition、Local/Component Pose显式转换、Component Pose控制、FootPlacement与PoseBone Goal Contribution、唯一Goal Assembler、唯一FullBodyIK与Output。Corin MUST不同时保留BaseLocomotion Selection Input、旧Timeline Player、共享Playback总管或第二动画链。

#### Scenario: 编译Corin Pose Graph

- **WHEN** Presentation binding owner 解析 Corin Profile
- **THEN** MUST发现唯一Locomotion PoseStateMachine和唯一FullBodyAction AnimationSlot
- **AND** MUST拒绝可达BaseLocomotion Gameplay Selection Input

### Requirement: Corin Pose source必须具有稳定binding与node-local policy

Corin 每个持续 Locomotion Clip、Blend Space 或 Motion Matching source MUST 由 Player 直接保存类型匹配的原生资源或 typed 资源参数；Presentation binding owner MUST 把全部 Player 资源降低为连续 stable source binding，不得保存作者 source/provider 字符串。每个有限 Action Timeline producer MUST 拥有稳定 presentation identity、FullBodyAction channel binding、Slot/Group 与直接 AnimationClip resource binding。PoseState transition 与 Slot transition MUST 分别来自对应 node-local Policy；Gameplay State edge 和 Timeline MUST 不保存另一份表现 transition 策略。

#### Scenario: 配置Run source

- **WHEN** Profile Inspector显示Run Presentation Pose source
- **THEN** 必须显示ClipPlayer或BlendSpacePlayer消费者与精确资源Binding
- **AND** 不得要求Sequence或Timeline producer identity

#### Scenario: 配置Attack1至Attack5

- **WHEN** Profile Inspector显示五个Action producer
- **THEN** 必须显示各自stable identity、FullBodyAction AnimationSlot与直接Clip binding
- **AND** 不得把它们列为Locomotion Pose State

### Requirement: Corin Walk与Run MAY共享Locomotion.Gait

Corin Walk、Run、Start与Turn Presentation Pose source MAY在同一Locomotion PoseStateMachine可达分支中通过Profile共享`Locomotion.Gait` Sync Group。Group MUST只装配通过Foot Analysis关系质量门槛的精确AnimationClip成员；每个Direct Clip endpoint和Blend Space Dynamic Sample MUST具有合法Locomotion Phase Curve。不相容的有限Start或Turn MUST保持组外并删除无消费Phase曲线，其edge只执行显式Standard Blend。source-local Phase映射 MUST只影响Pose sample time，不得改变Pose transition rule、Gameplay movement、Motion request或WorldSolver结果。Corin MUST不为Phase同步恢复Timeline producer。

#### Scenario: Walk Pose切换Run Pose

- **WHEN** PoseStateMachine从Walk handoff到Run且两侧source endpoint属于Locomotion.Gait
- **THEN** source-local relation MUST按compiled unwrapped Phase解析target sample time
- **AND** Gameplay/Ability owner MUST 不产生 WalkLoop 或 RunLoop playback

#### Scenario: 有限Locomotion素材与Loop不相容

- **WHEN** Start或Turn的有限出口无法满足目标Loop的Plant、脚底位置、高度或速度门槛
- **THEN** 该Clip MUST不进入Locomotion.Gait且 MUST不保留无消费Locomotion Phase曲线
- **AND** 对应edge MUST继续使用显式Standard Blend且不生成Phase relation

### Requirement: Corin旧Locomotion Timeline数据必须原子迁移

旧Idle、WalkStart、WalkLoop、RunStart、RunLoop、RunEnd与MovingTurn Timeline中的数据 MUST按用途迁移：AnimationClip引用迁入Profile Clip Binding，全部Foot Placement Weight按`SourceDurationSeconds`换算为原生AnimationClip秒域注册Curve，只有通过关系质量门槛并正式加入Sync Group的素材写入Locomotion Phase；Rig与Foot Analysis进入Profile统一装配，真实影响Body的Motion数据迁入唯一Gameplay Motion owner，无正式消费方的数据删除。ClipPlayer MUST删除Loop副本并只消费AnimationClip正式Loop设置。迁移完成后 MUST删除旧TimelineNode、BaseLocomotion AnimationChannel producer、Sequence、Marker、source binding副本、lifecycle配置、ActionOverride与旧ownership Blackboard declaration，MUST不保留旧新双写。

#### Scenario: 迁移RunLoop

- **WHEN** RunLoop Timeline只负责循环Pose、Marker和Foot Placement Weight
- **THEN** Clip引用 MUST进入Run direct Binding，素材曲线 MUST进入同一原生AnimationClip
- **AND** RunLoop Timeline producer、Sequence和Marker MUST删除

#### Scenario: MovingTurn含Gameplay MotionCurve

- **WHEN** 曲线确实参与CharacterMotionRequest
- **THEN** 曲线 MUST保留在明确Gameplay Motion owner并保持唯一消费链
- **AND** PoseStateMachine MUST不读取该曲线驱动World movement

### Requirement: Corin资产迁移必须通过正式业务作者入口

有限Action Timeline、Gameplay Graph、Blackboard、Presentation Binding、Pose Graph、Locomotion Sync Group、AnimationClip注册Curve与旧Locomotion数据清理 MUST通过各自正式类型化作者API完成，人工入口与C#入口 MUST共享同一业务约束。跨owner的同一次业务修改 MUST使用已有编辑事务的组合边界，明确引用、Undo与保存结果；普通单对象修改 MUST不依赖全角色协议事务。实现 MUST不直接修改Unity YAML、不恢复旧Patch链、不创建一次性migrator或第二mutation service。作者修改 MUST只影响authoring及其生成物stale状态，不得自动Build。

#### Scenario: C#修改Corin相关资产

- **WHEN** 调用方明确指定Corin Definition及同一次需要修改的Clip、Profile、Pose Graph、Timeline与Gameplay owner
- **THEN** 正式作者入口 MUST按业务引用关系执行修改并提供一致的Undo与保存结果
- **AND** MUST不生成目录包、整包hash或要求反向导出Clean
- **AND** MUST不恢复Sequence、Marker、BaseLocomotion、ActionOverride或旧Selection字段

### Requirement: Corin生成产物必须显式重建

Corin 迁移 MUST 先用 `AnimationClipAnalysisInputHash` 与新 Phase Validation Descriptor 显式重建 Foot Analysis Artifact，再通过正式作者 API 写入注册 Curve、Profile、Pose Graph 与 Timeline；Curve 写回 MUST 不使该 Artifact stale。正式 authoring 保存成功后，各领域 binding、原生 Pose Graph 实例、Float32/Fixed domain data 与 Timeline 内容 MUST 通过精确 Definition 的正式显式 Prepare/Adopt 入口按依赖顺序重建。Gameplay/Ability owner MUST 不包含 BaseLocomotion animation producer；Presentation binding MUST 包含 PoseStateMachine、Clip/BlendSpace state-local source、Locomotion Phase endpoint、AnimationSlot、完整 Rig v4 与唯一 ordered Pose 规则。产物 MUST 共享匹配的 source revision 闭包，不得自动 Build、部分发布或使用旧 wrapper、Clip plan 或 Phase relation。

#### Scenario: 迁移后显式Build

- **WHEN** Corin新schema Foot Analysis已Ready且正式authoring保存成功
- **THEN** 作者 MUST显式触发 Graph preparation、Float32 domain preparation 与 Fixed domain preparation
- **AND** 任一阶段失败 MUST保留明确typed diagnostic且不得发布混合revision

### Requirement: Corin Locomotion Transition必须统一使用Standard Blend

Corin Idle、Start、Walk、Run、Stop与Turn之间的全部可达PoseState edge MUST使用显式Standard Blend。Locomotion Phase relation MUST只确定目标source sample time；edge-owned Standard Blend MUST只确定共同可见期的Pose权重。Corin Locomotion MUST不连接局部Inertialization，也不得通过默认Policy或Runtime自动安装惯性残差。Action、受击或其它非Locomotion业务 MAY继续由其明确owner选择Inertialization。

#### Scenario: TurnBack进入RunLoop

- **WHEN** TurnBack结束并切换到RunLoop
- **THEN** Phase relation MUST先给出RunLoop有效时间，Standard Blend MUST按edge计划混合两侧Pose
- **AND** MUST不使用Inertialization拉扯腿部历史
