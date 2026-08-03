## MODIFIED Requirements

### Requirement: Presentation Profile必须唯一绑定Pose source

`CharacterPresentationPoseGraphAsset` MUST以可读命名、类型明确的`CharacterPresentationPoseSourceSlot`子资产声明持续Pose source语义插槽。`CharacterAnimationPresentationProfile` MUST以Profile-owned binding子资产把每个可达Source Slot对象唯一绑定到AnimationClip、Blend Space或Motion Matching资源，以及对应Rig、loop capability、marker、source-local Foot Placement Weight与Foot Analysis配置。Pose Graph Player MUST只引用类型匹配的Source Slot对象，不得保存可编辑Source Id、Provider Id、Clip或Profile binding副本。Gameplay Graph、BTSMTL StateMachine、Timeline、ActionProfile、Prefab与generated Program MUST不复制持续Locomotion source binding。

#### Scenario: 作者替换Run动画

- **WHEN** 作者在Corin Presentation Profile为Run Source Slot选择新的AnimationClip
- **THEN** PoseStateMachine topology与Gameplay Program MUST不需要修改
- **AND** Projection MUST只在作者明确执行Build后重建

#### Scenario: 两个Profile复用同一Pose Graph

- **WHEN** 两个Character Presentation Profile引用同一Pose Graph和同一个Run Source Slot
- **THEN** 两个Profile MUST分别用自己的binding子资产绑定各自Run资源
- **AND** Pose Graph MUST不复制或改写任一角色的AnimationClip

### Requirement: Action producer authoring必须只允许有限Timeline Action

`AnimationProducerPresentationBinding`、Profile Inspector与正式authoring mutation MUST只允许有限Action Timeline producer。Motion Matching、Blend Space与Sequence source MUST只通过Graph-owned Source Slot对象、Profile-owned typed binding子资产和PoseState source provider配置，MUST不作为Gameplay producer、AnimationChannel candidate或Action Playback Input。Projection Compiler MUST分别建立Action-only binding index与Pose source/provider dense binding index。

#### Scenario: 作者配置Locomotion Blend Space

- **WHEN** PoseState Pose Graph中的BlendSpacePlayer引用一个Blend Space Source Slot
- **THEN** Profile MUST为该Slot建立类型匹配的Blend Space binding子资产
- **AND** Inspector MUST不提供Gameplay producer或Action channel选项

### Requirement: Pipeline Definition 必须引用唯一 Animation Presentation Profile

`CharacterPipelineDefinition` MUST引用唯一`CharacterAnimationPresentationProfile`，不得内联保存动画表现数据。Profile MUST唯一引用Pose Graph、Profile-owned Pose source binding子资产、有限Action producer source binding、node-local Policy、Rig与Foot Analysis配置。Pose Graph MUST唯一保存Presentation Fact Input、PoseStateMachine、Graph-owned Source Slot子资产、SequencePlayer、AnimationSlot、Selection Player、composition、FootPlacement与Output topology。Gameplay Graph、BTSMTL StateMachine、Timeline、Presenter与Prefab MUST不复制这些配置。

#### Scenario: Corin配置动画表现

- **WHEN** Corin Definition引用正式Animation Presentation Profile
- **THEN** Profile MUST为PoseStateMachine Source Slot和Action Slot producer提供唯一资源绑定
- **AND** Definition MUST不内联Run Clip、State transition或Slot policy

#### Scenario: shared Graph被多个角色使用

- **WHEN** 两个CharacterPipelineDefinition引用同一个shared Graph/Timeline
- **THEN** 两个角色 MAY引用不同CharacterAnimationPresentationProfile和Analysis Source
- **AND** 每个Profile MUST为shared Source Slot对象提供自己的binding子资产
- **AND** shared Graph/Timeline MUST不保存角色级资源、分析Rig或校准

### Requirement: Animation producer 必须拥有稳定 presentation identity

每个有限Action Timeline animation producer MUST拥有稳定authoring producer identity。每个持续Pose source在authoring层 MUST由稳定Unity Source Slot对象和Profile binding对象精确表达，不得保存作者可编辑Source Id字符串。Projection Compiler MUST把Action identity写入Program source map与Projection binding，并把Pose source对象关系降低为Projection-local dense source index与只读source map。Runtime MUST只在匹配Projection revision内使用dense source index、Player identity和generation，不得使用显示名、数组index、asset path或当前State名称作为fallback。

#### Scenario: Timeline Track 重排

- **WHEN** 作者重排 AnimationTrack 或 Clip
- **THEN** 原 producer identity MUST 保持
- **AND** Program 与 Projection binding MUST 不因列表 index 变化而 orphan

#### Scenario: Source binding重排

- **WHEN** 作者重排Profile中的Pose source binding卡片
- **THEN** Source Slot与binding对象引用 MUST保持
- **AND** Compiler MUST重新确定性生成dense index而不改变任何作者引用

#### Scenario: binding 指向未知Source Slot

- **WHEN** Profile binding引用不属于当前Pose Graph闭包的Source Slot对象
- **THEN** Compiler/Validator MUST报告orphan binding并定位对象owner
- **AND** Runtime MUST拒绝Program/Projection组合，不能按名称或Clip猜测目标

### Requirement: CharacterAnimationPresentationProfile Inspector必须是唯一Presentation配置入口

Profile Inspector MUST唯一编辑Pose Graph、Profile-owned Pose source binding子资产、Blend Policy、Inertialization Policy、Rig Definition、有限Action producer source binding、Foot Analysis Mode与Analysis Source。Timeline Editor继续唯一编辑Action producer-local Clip、Window、Motion、Cue和Timeline marker。持续Locomotion Sequence source的Clip、marker和analysis归属Profile binding子资产；系统 MUST不要求为该source创建Timeline。Inspector MUST通过类型受限Unity对象选择器和可读业务名编辑资源，不得要求作者输入Source Id、Provider Id、GUID、local file id、revision或hash。

#### Scenario: 从Profile打开Timeline Analysis

- **WHEN** 作者从精确Profile上下文打开Timeline并选择AnimationClip
- **THEN** Analysis provider MAY把该Profile的Source作为显式初始选择
- **AND** Timeline资产 MUST不因打开或分析而变脏

#### Scenario: shared Timeline用于不同角色

- **WHEN** 两个Profile使用同一shared Timeline但不同Analysis Source
- **THEN** 各自 MUST生成不同artifact identity与Projection
- **AND** shared Timeline MUST不保存任一角色的Analysis Source

### Requirement: Profile Inspector 必须按正式 identity 显示 producer binding

Profile Inspector MUST在显式Definition context下分别显示持续Pose source与有限Action producer。Pose source MUST按Source Slot业务名与实际Unity资源显示消费它的PoseState/Sequence/BlendSpace/MM节点、resource、marker、Foot Placement Weight与analysis binding；Action producer MUST按Timeline、Track与资源业务名显示AnimationChannel、ActionPlaybackInput/AnimationSlot consumer与resource binding。稳定identity、revision、GUID、local file id、hash与compiled index MUST默认隐藏，只能在显式Diagnostics区域只读显示。服务 MUST不从显示名、目录、旧BaseLocomotion channel或generated产物反推authoring。

#### Scenario: 查看Run Pose source

- **WHEN** 作者从Corin Definition展开Locomotion PoseStateMachine
- **THEN** Inspector MUST显示Run Source Slot、实际动画资源、consumer和Profile binding owner
- **AND** MUST不要求RunLoop Timeline producer或显示可编辑Source Id

#### Scenario: 查看 Action 覆盖 Locomotion

- **WHEN** Definition context 同时包含 Action 与 Locomotion producer
- **THEN** Inspector MUST用各自业务名与资源对象分组显示binding
- **AND** MUST不推断覆盖关系或创建 Driver、Priority

### Requirement: Pose Graph Producer Navigator必须从显式Definition上下文投影

Pose Graph Navigator MUST要求精确Definition context，并从Profile、Pose Graph和Gameplay composition roots分别投影Pose source与有限Action producer。Locomotion分组 MUST显示PoseState、Sequence/BlendSpace/MM consumer、Source Slot业务名、Profile binding与实际资源名；Action分组 MUST显示Timeline、Track、AnimationChannel与AnimationSlot业务名。Navigator MUST不读取generated Program/Projection完成bootstrap，不按显示名猜测，不显示机器identity作为项目项名称，也不得保存第二份binding。

#### Scenario: 查看Locomotion sources

- **WHEN** 作者从Corin Definition展开Locomotion
- **THEN** Navigator MUST列出Idle、Start、Move、Stop、Turn的Source Slot及其实际资源
- **AND** MUST不列出BaseLocomotion Timeline producer或Source Id字符串

#### Scenario: 缺少Definition上下文

- **WHEN** 作者直接打开shared Pose Graph且没有精确Definition call-site context
- **THEN** Producer Navigator MUST显示Unavailable及缺失上下文原因
- **AND** MUST不搜索使用该图的任意角色或使用上一次窗口context

### Requirement: 跨资产表现配置必须保持唯一写入口

Pose Graph Workspace、Navigator与Details MAY只读显示Action Timeline Track和Profile Pose source binding子资产的resource、marker、curve、Policy、Rig与analysis状态。修改Action Clip、marker、window或curve MUST导航到Timeline Editor；修改Pose source resource、marker或Foot Placement Weight MUST导航到Profile source editor；修改State transition与Slot Policy MUST导航到Pose Graph/Policy owner。人工Workspace与Document v3 Reconciler MUST把目标状态降低到同一typed Presentation Mutation和资产事务；系统 MUST不复制字段、提供第二mutation命令、按窗口类型分叉写链或保留字符串binding镜像。

#### Scenario: 从Pose Graph调整Run marker

- **WHEN** 作者在State source引用面板选择Open Source
- **THEN** 必须打开Profile中的Run binding子资产编辑器
- **AND** Pose Graph节点 MUST保持只读资源摘要和Source Slot对象引用
