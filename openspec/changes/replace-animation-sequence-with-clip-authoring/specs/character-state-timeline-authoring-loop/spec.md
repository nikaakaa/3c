## MODIFIED Requirements

### Requirement: Corin Pose source必须具有稳定binding与node-local policy

Corin每个持续Locomotion Clip、Blend Space或Motion Matching source MUST拥有Graph-owned typed Source Slot与Profile-owned typed Binding子资产；Clip Binding MUST直接引用精确AnimationClip，Projection Compiler MUST把全部Binding降低为连续dense source index，不得保存作者source/provider字符串。每个有限Action Timeline producer MUST拥有稳定presentation identity、FullBodyAction channel binding与直接AnimationClip resource binding。PoseState transition与Slot transition MUST分别来自对应node-local Policy；Gameplay State edge和Timeline MUST不保存另一份表现transition策略。

#### Scenario: 配置Run source

- **WHEN** Profile Inspector显示Run Presentation Pose source
- **THEN** 必须显示ClipPlayer或BlendSpacePlayer消费者与精确资源Binding
- **AND** 不得要求Sequence或Timeline producer identity

#### Scenario: 配置Attack1至Attack5

- **WHEN** Profile Inspector显示五个Action producer
- **THEN** 必须显示各自stable identity、FullBodyAction AnimationSlot与直接Clip binding
- **AND** 不得把它们列为Locomotion Pose State

### Requirement: Corin Walk与Run MAY共享Locomotion.Gait

Corin Walk、Run、Start与Turn Presentation Pose source MAY在同一Locomotion PoseStateMachine可达分支中通过Profile共享`Locomotion.Gait` Sync Group。Group MUST只装配精确AnimationClip成员；每个Direct Clip endpoint和Blend Space Dynamic Sample MUST具有合法Locomotion Phase Curve。source-local Phase映射 MUST只影响Pose sample time，不得改变Pose transition rule、Gameplay movement、Motion request或WorldSolver结果。Corin MUST不为Phase同步恢复Timeline producer。

#### Scenario: Walk Pose切换Run Pose

- **WHEN** PoseStateMachine从Walk handoff到Run且两侧source endpoint属于Locomotion.Gait
- **THEN** source-local relation MUST按compiled unwrapped Phase解析target sample time
- **AND** Gameplay Program MUST不产生WalkLoop或RunLoop playback

### Requirement: Corin旧Locomotion Timeline数据必须原子迁移

旧Idle、WalkStart、WalkLoop、RunStart、RunLoop、RunEnd与MovingTurn Timeline中的数据 MUST按用途迁移：AnimationClip引用迁入Profile Clip Binding，归一化Foot Placement Weight按`SourceDurationSeconds`换算为秒域并与Locomotion Phase写入原生AnimationClip注册Curve，Rig与Foot Analysis进入Profile统一装配；真实影响Body的Motion数据迁入唯一Gameplay Motion owner；无正式消费方的数据删除。ClipPlayer MUST删除Loop副本并只消费AnimationClip正式Loop设置。迁移完成后 MUST删除旧TimelineNode、BaseLocomotion AnimationChannel producer、Sequence、Marker、source binding副本、lifecycle配置、ActionOverride与旧ownership Blackboard declaration，MUST不保留旧新双写。

#### Scenario: 迁移RunLoop

- **WHEN** RunLoop Timeline只负责循环Pose、Marker和Foot Placement Weight
- **THEN** Clip引用 MUST进入Run direct Binding，素材曲线 MUST进入同一原生AnimationClip
- **AND** RunLoop Timeline producer、Sequence和Marker MUST删除

#### Scenario: MovingTurn含Gameplay MotionCurve

- **WHEN** 曲线确实参与CharacterMotionRequest
- **THEN** 曲线 MUST保留在明确Gameplay Motion owner并保持唯一消费链
- **AND** PoseStateMachine MUST不读取该曲线驱动World movement

### Requirement: Corin资产迁移必须通过正式Agent Document事务

有限Action Timeline、Gameplay Graph、Blackboard、Presentation Binding、Pose Graph、Locomotion Sync Group、AnimationClip注册Curve与旧Locomotion数据清理 MUST通过`btsmtl-agent-authoring-document.v4`的`checkout_document -> editable修改 -> dry_run_document -> apply_document(expected_document_hash) -> validate`唯一事务完成。Reconciler MUST把全部目标降低为同一immutable Mutation Plan和Undo事务。实现 MUST不直接修改Unity YAML、不恢复旧Patch链、不创建一次性migrator或第二mutation service。Document apply MUST只修改authoring并标记生成物Stale，不得自动Build。

#### Scenario: 应用Corin Document

- **WHEN** dry-run成功并返回exact document hash
- **THEN** apply MUST消费同一hash并在一个Undo事务保存Clip、Profile、Pose Graph、Timeline与Gameplay authoring
- **AND** 成功后反向导出Package MUST为Clean
- **AND** Document MUST不再包含Sequence、Marker、BaseLocomotion、ActionOverride或旧Selection字段

### Requirement: Corin生成产物必须显式重建

Corin迁移 MUST先用`AnimationClipAnalysisInputHash`与新Phase Validation Descriptor显式重建Foot Analysis Artifact，再通过Document v4写入注册Curve、Profile、Pose Graph与Timeline；Curve写回 MUST不使该Artifact stale。Document apply成功后，Presentation Projection、Float32 Program wrapper与Fixed Program wrapper MUST通过精确Definition的正式显式Build入口按依赖顺序重建。Program MUST不包含BaseLocomotion animation producer；Projection MUST包含PoseStateMachine、Clip/BlendSpace state-local source、Locomotion Phase endpoint、AnimationSlot、完整Rig v4与唯一ordered Pose Plan。产物 MUST共享匹配的source revision闭包，不得自动Build、部分发布或使用旧wrapper、Sequence plan或Marker relation。

#### Scenario: 迁移后显式Build

- **WHEN** Corin新schema Foot Analysis已Ready且Document apply成功
- **THEN** 作者 MUST显式触发Projection、Float32 Build与Fixed Build
- **AND** 任一阶段失败 MUST保留明确typed diagnostic且不得发布混合revision

## ADDED Requirements

### Requirement: Corin Locomotion Transition必须统一使用Standard Blend

Corin Idle、Start、Walk、Run、Stop与Turn之间的全部可达PoseState edge MUST使用显式Standard Blend。Locomotion Phase relation MUST只确定目标source sample time；edge-owned Standard Blend MUST只确定共同可见期的Pose权重。Corin Locomotion MUST不连接局部Inertialization，也不得通过默认Policy或Runtime自动安装惯性残差。Action、受击或其它非Locomotion业务 MAY继续由其明确owner选择Inertialization。

#### Scenario: TurnBack进入RunLoop

- **WHEN** TurnBack结束并切换到RunLoop
- **THEN** Phase relation MUST先给出RunLoop有效时间，Standard Blend MUST按edge计划混合两侧Pose
- **AND** MUST不使用Inertialization拉扯腿部历史

## REMOVED Requirements

### Requirement: Corin全部动画owner必须显式选择Marker策略

该Requirement被删除。有限Action没有同步策略字段，持续Locomotion只由Profile Group成员关系决定是否编译Phase relation。

#### Scenario: 旧策略字段存在

- **WHEN** Corin Timeline、Binding或Clip仍保存SyncMode、Group、Topology、Role或Marker
- **THEN** 新schema MUST拒绝旧字段

### Requirement: Corin有限动作只能在资源满足时加入Marker Group

该Requirement被删除。当前有限Action不参加Locomotion同步，也不保留通用Action Marker Group能力。

#### Scenario: Action包含Marker Group

- **WHEN** Attack或Dodge producer仍声明MarkerGroup
- **THEN** Timeline Validator MUST拒绝该字段
- **AND** Action MUST继续使用raw visual sample与Slot transition
