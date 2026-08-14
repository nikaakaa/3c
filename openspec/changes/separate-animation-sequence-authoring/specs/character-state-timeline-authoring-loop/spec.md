## MODIFIED Requirements

### Requirement: Corin Pose source必须具有稳定binding与node-local policy

Corin每个持续Locomotion Sequence、Blend Space或Motion Matching source MUST拥有Graph-owned typed Source Slot与Profile-owned typed Binding。Sequence Binding MUST引用正式Animation Sequence，Blend Space sample也 MUST引用Sequence；Projection Compiler MUST把引用闭包降低为连续dense source与Sequence plan。每个有限Action Timeline producer MUST拥有稳定identity、FullBodyAction channel binding、Sequence Segment与resource binding。PoseState/Slot transition继续来自node-local Policy；Graph Gameplay State edge和Timeline MUST不保存另一份表现transition策略。

#### Scenario: 配置Run source

- **WHEN** Profile Inspector显示Run Source Slot
- **THEN** 必须显示Sequence binding、实际Sequence与consumer
- **AND** 不得在Binding显示可写Clip、Marker或Curve副本

#### Scenario: 配置Attack1至Attack5

- **WHEN** Profile Inspector显示五个Action producer
- **THEN** 必须显示各自stable identity、FullBodyAction Slot、Timeline Segment与Sequence引用
- **AND** 不得把它们列为Locomotion Pose State

### Requirement: Corin Walk与Run MAY共享Locomotion.Gait

Corin Walk与Run Animation Sequence MAY共享`Locomotion.Gait` SyncGroup。启用时，两份Sequence MUST按真实Clip配置完整Cyclic Marker、角色与Time Mapping；Profile Binding只引用Sequence。source-local映射 MUST只影响Pose sample time，不得改变Transition Rule、Gameplay movement或WorldSolver结果。

#### Scenario: Walk Pose切换Run Pose

- **WHEN** PoseStateMachine两侧Binding引用兼容Walk与Run Sequence
- **THEN** compiled plan MUST按Sequence的Locomotion.Gait有向pair解析target time
- **AND** Gameplay Program MUST不产生WalkLoop或RunLoop Action playback

### Requirement: Corin全部动画owner必须显式选择Marker策略

Corin每个可达Animation Sequence MUST显式配置`None`或`MarkerGroup`，不得保留Unspecified。Action Timeline Segment、Profile Binding与Blend Space sample MUST只引用Sequence且不得覆盖策略。选择 MUST根据真实素材语义、Loop/Finite能力、Analysis与全部consumer coverage作出，不得按显示名称硬编码。

#### Scenario: 检查Corin Sequence清单

- **WHEN** Compiler遍历Pose Source、Blend Space与Action Timeline的可达Sequence引用
- **THEN** 每个唯一Sequence MUST拥有明确sync mode
- **AND** 任一Unspecified MUST阻止发布并定位Sequence及全部consumer

#### Scenario: 检查Corin作者清单

- **WHEN** Compiler遍历有限Action、Presentation Pose Source与Blend Space的Sequence引用
- **THEN** 每个真实Sequence owner MUST拥有明确sync mode
- **AND** 任一Unspecified MUST阻止发布并定位全部consumer

### Requirement: Corin有限动作只能在资源满足时加入Marker Group

Attack1至Attack5、Dodge及其它有限Action Sequence MAY配置`MarkerGroup/Finite`，但仅当真实Clip从frame 0到DurationFrame具有完整coverage，并满足同AnimationSlot可达Sequence集合的directed pair契约。RunStart、RunEnd、MovingTurn等Pose Sequence MAY配置Finite MarkerGroup。资源不满足时Sequence MUST显式配置None并使用raw sample与自己的Transition或Slot Blend Logic；Timeline Track、Segment与Binding不得伪造支撑marker。

#### Scenario: Action没有共同姿态契约

- **WHEN** Attack1与Attack2 Sequence没有同组完整Marker语义
- **THEN** 两份Sequence MUST显式为None
- **AND** 连段准入 MUST继续由ComboAccept Window和Gameplay transition决定

#### Scenario: Action退出到Locomotion

- **WHEN** Action Sequence为None并结束
- **THEN** AnimationSlot MUST按compiled Action-to-Source Pose规则回到同帧当前Locomotion Pose
- **AND** MUST不从Action名称、Timeline时间或旧BaseLocomotion selection伪造步态phase

### Requirement: Corin旧Locomotion Timeline数据必须原子迁移

旧Idle、WalkStart、WalkLoop、RunStart、RunLoop、RunEnd与MovingTurn Timeline中的数据 MUST按用途迁移：AnimationClip、素材Marker、Foot Placement Weight、Notify与Foot Analysis identity迁入正式Animation Sequence；Profile Binding改为Sequence引用；真实影响Body的Motion数据迁入唯一Gameplay Motion owner；无正式consumer的数据删除。迁移完成后 MUST删除旧TimelineNode、旧素材Track字段、BaseLocomotion channel、旧binding正文、lifecycle配置与旧ownership Blackboard，不保留双写。

#### Scenario: 迁移RunLoop

- **WHEN** RunLoop Timeline的AnimationTrack只负责循环Pose和Locomotion Marker
- **THEN** Clip、Marker与素材Curve MUST迁入Run Sequence，Profile Binding MUST引用它
- **AND** RunLoop Timeline producer MUST删除

#### Scenario: MovingTurn含Gameplay MotionCurve

- **WHEN** 曲线确实参与CharacterMotionRequest
- **THEN** MotionCurve MUST保留在明确Gameplay Motion owner，动画素材进入MovingTurn Sequence
- **AND** Sequence与PoseStateMachine MUST不读取MotionCurve驱动World movement
