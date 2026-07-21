## RENAMED Requirements

- FROM: `### Requirement: Animancer 原生 transition 数据必须是转场权威`
- TO: `### Requirement: Pose Slot Blend Stack transition数据必须是转场权威`

## ADDED Requirements

### Requirement: Animation Rig与Blend Profile必须由稳定BoneId连接

`CharacterAnimationRigDefinition` MUST作为Stack transition和Pose Graph dense skeleton的唯一authoring来源，保存RigId、revision、父节点优先BoneId、ParentIndex、root exclusion、scale policy与左右脚语义BoneId。Blend Profile MUST引用同一Rig并保存global duration multiplier与按BoneId override。Prefab Rig Binding MUST按dense顺序显式绑定Runtime Transform。Graph、Timeline、Humanoid、名称、path或独立Rig表 MUST不推导第二份identity。

#### Scenario: 作者配置腿部更快Blend

- **WHEN** Blend Profile为左右腿BoneId配置override
- **THEN** Inspector MUST显示Rig identity、BoneId与multiplier
- **AND** Timeline和StateMachine MUST不复制这些参数

#### Scenario: Rig拓扑修改

- **WHEN** BoneId、ParentIndex或顺序改变
- **THEN** Rig revision与ProjectionRevision MUST改变
- **AND** 旧dense payload与Rig Binding MUST被拒绝

### Requirement: Blend Library必须编译每Pose Slot完整Transition Matrix

`CharacterAnimationBlendLibrary` MUST为每个PoseSlotId显式保存Stack Policy、default transition rule与可选source-target override。每条rule MUST声明CrossFade或Inertial、duration、canonical curve与匹配Rig的Blend Profile。Projection Compiler MUST枚举该slot绑定AnimationChannel的全部可达source-target/Empty组合并物化完整matrix；Runtime只做exact lookup。Library MUST不保存Pose Graph topology、State priority或Action interruption。

#### Scenario: Attack1到Attack2使用Inertial

- **WHEN** 作者为FullBodyActionSlot配置exact override
- **THEN** Projection MUST生成完整matrix entry
- **AND** Runtime MUST不读取Animancer TransitionLibrary

#### Scenario: override跨Pose Slot

- **WHEN** source与target属于不同PoseSlotId
- **THEN** validation与Build MUST失败
- **AND** 系统 MUST不利用Pose Graph Mask修正

#### Scenario: AllowEmpty缺少Empty规则

- **WHEN** Optional slot存在Empty target但matrix无法物化source-Empty
- **THEN** Build MUST失败
- **AND** Runtime MUST不使用固定duration或Immediate fallback

## MODIFIED Requirements

### Requirement: Pose Slot Blend Stack transition数据必须是转场权威

系统 MUST使用项目Blend Library、完整Projection matrix、canonical curve、CrossFade/Inertial与dense Per-Bone Blend Profile作为每Pose Slot唯一transition权威。Profile MUST不引用Animancer TransitionLibrary、ITransition、FadeMode或FadeGroup easing；Pose Graph edge与BTSMTL State edge MUST不拥有slot Fade Clock或transition rule。Runtime MUST不实现matrix缺失默认值。

#### Scenario: target首样本到达

- **WHEN** selected producer收到首份合法sample
- **THEN** Stack MUST exact lookup同slot source-target entry
- **AND** clock、curve与per-bone weight MUST由Slot Evaluator推进

#### Scenario: 使用slot default rule

- **WHEN** 作者没有配置pair override
- **THEN** Compiler MUST在Build时物化显式slot default为exact entry
- **AND** Runtime MUST不知道来源是default或override

### Requirement: 播放生命周期调试必须只保留统一视图

RuntimeDebugSession与Host调试视图 MUST作为committed channel command、Timeline sample、Pending、PoseSlot Stack depth、EntryId、PlaybackId、technique、clock、raw/eased alpha、BoneId weight、Stored capture、Inertial residual、PoseSlotFrame、retirement与pose job状态的唯一调试入口。Profile、Blend Library、Rig与Pose Graph Inspector MUST不复制Live Trace。Editor MUST不重新运行Gameplay Graph、求值Stack或按Animancer state重建weight。

#### Scenario: 排查连续Attack

- **WHEN** Attack1尚未完成又进入Attack2并触发Stored capture
- **THEN** Debug MUST显示FullBodyActionSlot entry、capture原因、Stored与Attack2 clock
- **AND** 数据 MUST来自正式snapshot

#### Scenario: 排查Per-Bone差异

- **WHEN** 观察左脚和脊柱BoneId
- **THEN** Debug MUST显示slot内部actual weight与Profile multiplier
- **AND** MUST明确它尚未经过Pose Graph最终Mask
