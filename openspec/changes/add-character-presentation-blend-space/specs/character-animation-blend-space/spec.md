## ADDED Requirements

### Requirement: 系统必须提供正式Blend Space资产

系统 MUST使用`CharacterAnimationBlendSpaceAsset`作为连续参数动画样本空间的唯一authoring真相。资产 MUST保存稳定BlendSpaceId、content revision、Rig identity、正式mode、typed axis、稳定SampleId、精确AnimationClip引用、sample position、sample role、phase policy、LocomotionPhase模式的稳定Phase Reference Sample、Foot Analysis source binding与显式Pose Parameter policy。资产 MUST不保存Runtime weight、time、Playable、Animator或Transform状态。

#### Scenario: 作者创建一维移动Blend Space

- **WHEN** 作者通过正式authoring service创建Linear1D资产并配置Speed轴、Idle、Walk与Run样本
- **THEN** 每个样本 MUST拥有稳定且唯一的SampleId
- **AND** 资产 MUST只保存authoring输入而不保存当前播放状态

### Requirement: Blend Space必须使用有限正式模式

正式Blend Space mode MUST只包含`Linear1D`、`FreeformCartesian2D`与`FreeformDirectional2D`。Linear1D MUST执行排序后的相邻样本线性插值；Cartesian与Directional MUST分别遵循项目内Animancer对应Mixer的可见算法语义。系统 MUST不把Direct、SimpleDirectional、nested Blend Space或任意子图作为隐藏或占位模式。

#### Scenario: 编译未知模式

- **WHEN** 资产包含正式目录之外的mode值
- **THEN** Compiler MUST拒绝资产并定位BlendSpaceId
- **AND** MUST不改用Linear1D或最近样本

### Requirement: 权重求解必须是target-neutral纯计算

系统 MUST把三种mode编译为不持有Unity对象、Playable、时钟或authoring asset的`BlendSpaceWeightEvaluator`数据。Evaluator MUST只消费compiled sample position与有限参数值，在预分配page中输出按稳定SampleId排序的非负有限权重；正权重总和 MUST经过唯一normalization pass成为1。退化、NaN、Infinity或空结果 MUST产生typed failure，不得保留上一帧结果或选择最近样本作为fallback。

#### Scenario: 求解Linear1D区间值

- **WHEN** Speed位于两个相邻样本position之间
- **THEN** Evaluator MUST只激活这两个样本并按线性比例输出归一化权重
- **AND** MUST不创建或访问Playable

#### Scenario: 求解数据退化

- **WHEN** 编译数据出现重合坐标或Runtime参数不是有限值
- **THEN** Compiler或Runtime MUST输出机器可读失败
- **AND** MUST不选择任意默认样本

### Requirement: Blend Space时间策略必须显式且确定

每个资产 MUST显式选择`SharedNormalizedPhase`或`LocomotionPhase`。SharedNormalizedPhase MUST把同一canonical normalized phase映射到全部DynamicCycle sample。LocomotionPhase MUST使用稳定DynamicCycle Phase Reference Sample作为source raw clock carrier；Compiler MUST要求全部DynamicCycle Sample的AnimationClip属于同一Profile Locomotion Sync Group并具有合法per-clip forward/inverse Phase plan，把Reference raw time转换为unwrapped phase后再通过每个Sample自己的inverse plan得到effective time。资产 MUST不保存reference-to-sample pairwise warp、Marker topology或动态leader。StationaryPose MUST使用显式固定normalized time且不得成为Phase Reference。Phase Reference、Group成员或Curve无效时 MUST编译失败，不得回退SharedNormalizedPhase。

#### Scenario: 参数跨越多个动态样本

- **WHEN** Direction参数从一个区域连续移动到另一个区域
- **THEN** 全部正权重DynamicCycle sample MUST继续使用同一canonical phase
- **AND** 系统 MUST不按最大weight动态更换Phase Reference

#### Scenario: Locomotion Phase输入缺失

- **WHEN** LocomotionPhase中的一个DynamicCycle不属于共同Profile Group或缺少合法Phase Curve
- **THEN** Projection Build MUST失败并定位AssetId与SampleId
- **AND** MUST不静默使用SharedNormalizedPhase

### Requirement: BlendSpacePlayer必须是显式Pose Graph Player

Pose Graph MUST提供`BlendSpacePlayer`正式节点。节点 MUST只位于PoseState inline subgraph，精确引用一个Graph-owned typed Blend Space Source Slot，一维模式消费X typed Fact Parameter，二维模式消费X/Y typed Fact Parameter，并输出普通Pose Value与typed Pose Discontinuity。节点 MUST不保存BlendSpace资产引用；Compiler MUST按Slot对象引用从Profile-owned typed Binding子资产解析唯一资源真相并生成Projection-local dense source index。节点 MUST不保存跨State source entry、transition clock、Stored Pose或inertial residual。

#### Scenario: 一维资产进入BlendSpacePlayer

- **WHEN** PoseState中的Player绑定具有Speed轴合同的Linear1D Pose source
- **THEN** Compiler MUST把X Parameter端口和资产轴绑定到同一typed ParameterId
- **AND** 节点 MUST按State source clock和Speed Fact参数输出Pose

#### Scenario: 非BlendSpace Pose source进入BlendSpacePlayer

- **WHEN** Player绑定的source kind为Clip或MotionMatching
- **THEN** Compiler MUST拒绝该节点并定位Source Slot业务名与对象owner
- **AND** MUST不改用ClipPlayer或MM Player

### Requirement: BlendSpacePlayer与连续性节点必须分责

BlendSpacePlayer MUST只拥有同一BlendSpace source内部的参数权重、source canonical phase到child effective time的映射、multi-clip sample与source-local feature聚合。跨PoseState source的Phase handoff MUST由edge编译的`AnimationPhaseRelationPlan`拥有；跨来源CrossFade、Stored Pose和exact release MUST仍由Transition或BlendStack拥有；单Pose discontinuity的residual与rebase MUST仍由显式Inertialization拥有。Runtime与Preview MUST不自动插入Relation、Stack或Inertialization。

#### Scenario: BlendSpace source identity变化

- **WHEN** BlendSpacePlayer收到不同Projection-local source index或generation
- **THEN** 节点 MUST发布typed Pose Discontinuity
- **AND** 只有图中显式连接的Inertialization MAY平滑该跳变

### Requirement: Blend Space必须编译为固定Projection计划

Projection Compiler MUST把资产编译为不可变BlendSpace plan，其中包含Projection-local dense source index、identity/revision/Rig、dense axis、stable sample table、weight solver data、phase policy、Phase Reference、per-clip Phase plan index、clip resource binding、Foot Analysis binding、Pose Parameter policy、workspace offset与可读diagnostic source map。LocomotionPhase资产 MUST同时降低为`AnimationSourcePhasePlan`供PoseState relation使用。Runtime MUST只读取匹配Projection revision的plan，不得读取Source Slot或Binding ScriptableObject、AnimationClip Curve、AssetDatabase、Timeline authoring、Profile或Marker。

#### Scenario: Runtime创建BlendSpacePlayer

- **WHEN** Character Presentation Runtime加载Ready Projection
- **THEN** Runtime MUST从固定plan和预分配workspace创建节点状态
- **AND** MUST不动态搜索AnimationClip或构建sample关系

### Requirement: Blend Space必须复用现有Animancer采样后端

BlendSpacePlayer MUST把正权重样本降低为现有`ClipSamplePlan`并交给Animancer source backend。Animancer MUST只创建或复用child ClipState、应用compiled effective time、loop、play rate与weight并提供source pose capture；MUST不读取Phase Curve、重新求解Blend Space权重、选择phase leader、执行Phase relation或拥有最终Pose。

#### Scenario: 三个样本同时贡献

- **WHEN** Weight Evaluator输出三个正权重SampleId
- **THEN** 现有ManualMixerState MUST按同一ClipSamplePlan采样三个clip
- **AND** 样本权重 MUST来自Projection BlendSpace evaluator而非Animancer Parameter

### Requirement: Pose Parameter必须按样本权重显式聚合

Blend Space资产 MUST为每个可发布source-local ParameterId声明`RequireAllSamplesWeighted`、`WeightedAvailableSamples`或`Unavailable`。Runtime MUST按最终sample weight与该policy生成Pose Value parameter page；MUST不使用未声明全局默认值、字符串查找或上一帧参数。下游跨Pose解析 MUST仍由PoseParameterResolve拥有。

#### Scenario: 部分样本缺少可选参数

- **WHEN** ParameterId声明WeightedAvailableSamples且一个正权重样本没有该值
- **THEN** Runtime MUST只对有值样本重新归一化并聚合
- **AND** MUST在diagnostics中保留缺失样本事实

### Requirement: Foot Analysis feature必须使用姿势相同的样本贡献

每个BlendSpace sample MUST通过稳定AssetId/SampleId/Clip/Rig/Calibration identity绑定正式Foot Analysis artifact。Runtime MUST按每个样本effective time读取feature，并用与骨骼姿势相同的最终sample weight聚合左右脚feature和source contribution。BlendSpacePlayer MUST不执行Physics query、Foot State、contact/anchor、Pelvis、Goal Assembly或IK；唯一CharacterFootPlacementModule MUST消费composition后的最终贡献并生成Resolved Foot Pair、Pelvis Result与typed Goal Contribution，唯一Goal Assembler MUST形成一个Goal Set，下游唯一FullBodyIK MUST只求解一次。

#### Scenario: Walk与Run共同贡献

- **WHEN** Walk和Run样本分别以0.4与0.6权重生成Pose
- **THEN** 左右脚feature MUST按同一0.4与0.6贡献聚合
- **AND** CharacterFootPlacementModule MUST只执行一次，Goal Assembler MUST只形成一个Goal Set，FullBodyIK MUST只求解一次

### Requirement: Blend Space必须拥有正式资产编辑体验

Character Animation Authoring Workspace MUST提供Blend Space资产模式，并复用Navigator、参数空间Canvas、Details Authoring/Live/References、Bottom Dock、Preview、Pose Watch与编译诊断外壳。作者修改mode、轴、sample、phase或policy MUST只调用正式Authoring Service并标记Stale；只有显式Compile/Build MAY发布Projection。系统 MUST不创建旧Workbench、自动Build或临时Preview Mixer路径。

#### Scenario: 作者拖动二维样本

- **WHEN** 作者在参数空间Canvas拖动一个Sample
- **THEN** 正式Authoring Service MUST更新同一SampleId并进入Undo
- **AND** Projection MUST标记Stale而不自动重建

### Requirement: Preview与Runtime必须共享同一Blend Space计划

Blend Space资产Preview、PoseState Preview、Pose Graph Preview和正式Runtime MUST使用同一Projection revision、weight evaluator、phase mapper、Animancer sampling backend、Pose Parameter policy和Foot feature聚合。Live Debug MUST只读取Runtime Snapshot。Diagnostics MUST按NodeId与SampleId显示参数、权重、canonical phase、effective time、feature来源与revision，不得重新求值。

#### Scenario: Preview拖动参数落点

- **WHEN** 作者在Ready Projection上拖动X/Y preview参数
- **THEN** Preview MUST执行正式BlendSpace plan并显示active SampleId与weight
- **AND** Live视图 MUST不从Animancer child state反推权重
