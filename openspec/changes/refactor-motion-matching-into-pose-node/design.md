## Context

现有MM底层已经能回答“从哪些数据库中，按什么feature和cost，选择哪个clip的哪个时间”，但正式Pose Graph只接收一个`PresentationPoseSourceSample`。随后`SelectedPosePlayer`再次解释采样时间，显式`BlendStack`再解释跳转连续性。三个owner通过generation和source usage间接对账，作者还必须在每个MM状态手工拼出同一模板。

UE的Motion Matching节点把这三个动作放回同一节点状态，GASP再把Pose History、Trajectory、Chooser和entry处理图明确暴露出来。本项目采用同一职责分法，同时保留自己的显式构建、typed事实、Pose IR和事务边界。

当前Corin Presentation Profile没有MM Profile绑定，Projection的MM payload为空；GASP目录已有1271个Humanoid FBX。本change不把Corin改成MM角色，而是创建`MotionMatchingDemoCharacter`独立角色Prefab。Rig/IK active change会把Animation Rig schema从v3升级为v4，因此节点架构不需要等待，但新Prefab正式数据库发布必须使用它在最终schema上的Rig lineage。

## Goals

- 让一个作者节点完整表达MM搜索、播放、跳转混合和Local Pose输出。
- 让一个节点实例成为选择generation、播放时间、Blend Stack和source usage的唯一owner。
- 让Pose History记录实际MM基础Pose，同时排除下游动作与world-aware修正。
- 让数据库筛选由typed Chooser显式完成，不把Gameplay状态藏在动画名字或runtime if链中。
- 复用现有搜索、数据库和Blend Stack数学，不复制第二套实现。
- 使用唯一Presentation Rig关闭Profile、Schema、Database、SourceSet和Artifact身份链。
- 让新增`MotionMatchingDemoCharacter` Prefab拥有可解释、可运行的Grounded Motion Matching完整内容链。
- 保持Corin现有角色资产与非MM表现配置不变。
- 一次删除旧MM Slot、SelectedPosePlayer和外接MM BlendStack路径。

## Non-Goals

- 不复制UE Pose Search数据库格式、Blueprint、Property Access或AnimGraph VM。
- 不在本change实现Orientation Warping、Stride Warping、Steering、Distance Matching或Traversal选择算法。
- 不让MM决定Gameplay状态、Action ownership、Root Motion或KCC运动。
- 不把Crouch、Jump、Slide、Mantle、Vault、Hurdle、Climb按文件名自动加入正式数据库。
- 不在同一个角色Prefab内新增第二Animation Rig、MM Rig、shadow skeleton或运行时Retarget链。
- 不提供旧MM source slot的兼容读取、双写、fallback或迁移开关。
- 不自动构建数据库、Foot Analysis、Projection或Native Pose Program。

## Evidence Audit

### UE 5.7与GASP

- 本地`FAnimNode_MotionMatching`继承`FAnimNode_BlendStack_Standalone`，说明搜索结果和Blend Stack是同一节点运行状态。
- 本地实现只在搜索产生Jump时调用`BlendTo`，Continue保持当前stack entry并推进时间。
- GASP在主AnimGraph向Motion Matching节点提供Trajectory与Pose History，数据库集合由Chooser先筛选。
- GASP允许双击Motion Matching节点编辑内部Blend Stack Graph，每个live entry在混合前经过同一图。
- GASP把Action Slot、Root Offset、Orientation/Stride调整和IK放在基础MM Pose之后；这些职责不属于数据库搜索本身。

### 本项目

- `CharacterPoseNodeKind`当前没有Motion Matching或Pose History节点。
- `CharacterSelectedPosePlayerPayload`与`CharacterBlendStackPosePayload`都保存同一`CharacterMotionMatchingPoseSourceSlot`，形成两种MM消费入口。
- 项目资产中没有序列化的`CharacterSelectedPosePlayerPayload`或`CharacterBlendStackPosePayload`实例；当前引用只存在于合同、Mutation、Compiler和文档，适合直接清理而不保留兼容。新增角色Prefab可以从第一版就只保存新节点合同。
- `CharacterMotionMatchingPoseSourceBinding`尚未验证绑定数据库属于所选MM Profile，也尚未验证MM FeatureSchema Rig等于Presentation Profile Rig。
- Database、SourceSet和Artifact已经保存Rig lineage，因此正式内容只需把入口闭包补齐，不需要第二套Rig配置。

## Selected Architecture

```text
CharacterPresentationFactFrame -----------+
Trajectory -------------------------------+----> MotionMatchingDatabaseChooser
Previous Pose History --------------------+               |
                                                          v
PoseHistoryCollector -> MotionMatchingPose --------------------------> AnimationSlot
       ^                 |  Search Kernel                                  |
       |                 |  Selected Entry Player                          v
       |                 |  Entry Processing Graph                    downstream pose
       |                 |  Internal Blend Stack                      / Root / IK
       |                 +---------------- Local Pose ---------------------+
       |                                                                  |
       +---------------- record completed MM base pose <------------------+
```

### Actor级输入与节点级状态

`CharacterMotionMatchingFrameContext`每个表现帧只解析一次Trajectory、typed表现事实、delta time、frame identity和Rig lineage。它不保存当前clip、当前time、上次search、active entries或reset状态。

每个`MotionMatchingPose`编译节点实例保存：

- 上次查询plan与query cadence状态。
- 当前selection identity、generation、database、segment、sample和播放时间。
- 当前节点自己的internal Blend Stack entries与Stored Pose。
- source usage、retention和release token。
- 对`PoseHistoryCollector`上一帧历史页的只读租约。

`CharacterMotionMatchingSearchKernel`是无状态算法入口。它接收完整query、Chooser结果和数据库只读页，返回Continue、Jump或Invalid计划；它不能读取actor组件、Animator、Transform或节点全局单例。

### 节点端口

`MotionMatchingPose`固定包含以下输入：

- `history.pose`：来自唯一兼容`PoseHistoryCollector`的上一帧历史。
- `trajectory.query`：来自Presentation frame context的轨迹样本。
- `presentation.facts`：当前typed表现事实页。
- `motion-matching.binding`：Profile、Chooser、SearchDomain和正式生成物binding。

它只输出`pose.local`。节点payload保存binding identity、Blend Policy、entry processing graph identity、relevance reset policy和明确的search cadence策略，不保存运行时选择或数据库缓存。

### Pose History两阶段时序

`PoseHistoryCollector`是Local Pose passthrough节点，但编译器把它分为两个有序阶段：

1. `HistoryRead`在本帧MM搜索前向其绑定节点暴露上一帧完成页。
2. `HistoryCommit`在MM基础Local Pose完成后，把本帧骨骼Pose、root kinematics、source lineage和frame identity追加到环形历史。

Collector记录点必须位于MM输出之后、Action Slot和所有world-aware节点之前。首帧、重相关、Rig revision变化或明确reset时历史为显式Unseeded；MM节点按Profile定义的initial selection规则进行首次搜索，不复制当前Animator Pose充当隐藏seed。

一个MM节点必须绑定且只绑定一个同Rig Collector。一个Collector可以服务同一基础Pose链上明确列出的MM节点，但编译器必须证明它们不会在同帧产生互相覆盖的commit；否则Build失败。

### Typed Database Chooser

`CharacterMotionMatchingDatabaseChooser`由有序规则组成。每条规则只允许读取Capability Catalog声明的typed presentation fact，并输出：

- Profile内数据库identity的有序集合。
- `ShouldSearch`。
- `InterruptMode`。
- 可选的cost/search policy override identity；override也必须属于当前Profile。

Chooser不执行Pose搜索，不读动画名称，不访问GameObject，不调用任意脚本。多条规则同时匹配时按明确priority和exclusive policy解析；结果为空、包含Profile外数据库、Rig不一致或规则存在歧义时返回Invalid并阻止节点输出，不选择第一库或默认Idle。

业务上Chooser只负责粗分区，例如Grounded Gait；Start、Loop、Stop、Pivot等纯视觉差异留给MM成本搜索。Gameplay Action、Dodge和Traversal仍由上层状态与Timeline明确拥有。

### Internal Blend Stack

每个MM节点是一个Blend Stack owner，复用统一`CharacterAnimationBlendStackKernel`：

- Continue只更新当前entry的采样时间与source lineage，不创建新entry。
- Jump创建新entry，以节点Blend Policy计算独立clock、curve和per-bone权重。
- 容量到达上限时，把仍有贡献的旧entries压成一个Stored Pose，再加入新entry。
- entry权重归零且不再被Stored Pose引用后，owner发布release并回收source。
- 相同generation、重复Jump、倒退frame或不同Rig lineage直接失败。

Standalone BlendStack作者节点与MM Slot消费入口被删除。Kernel没有自主tick、资产引用或隐藏默认值，只能由编译后的owner调用。以后若其它业务需要独立Blend Stack，必须另行定义正式输入producer和owner，不得复活MM Slot。

### Entry Processing Graph

每个MM节点引用一个root-owned flat子图。它固定包含一个`EntryPoseInput`和一个`GraphOutput`，输入输出都是`pose.local`。新建MM节点时，正式Mutation在同一事务创建身份图；删除节点时只在引用计数归零后删除该图。

内部图对每个live entry独立执行，随后才进入Blend Stack。第一期允许Sequence局部Pose上的纯、无状态或entry-local节点；禁止StateMachine、MotionMatchingPose、PoseHistoryCollector、AnimationSlot、ActionPlaybackInput、world-aware节点、Component Pose IK和外部source player。任何允许的有状态节点都必须按`MM Node Identity + Entry Generation + Inner Node Identity`分配状态，不能让两个entry共享状态。

本change只交付身份图和扩展合同。没有正式Orientation/Stride/Steering节点时，`MotionMatchingDemoCharacter`内部图保持显式identity，不用隐藏数学冒充GASP处理。

### Rig Identity Closure

正式Build前必须满足：

```text
PresentationProfile.RigDefinition
  == MotionMatchingProfile.FeatureSchema.Rig
  == Database.TargetRig
  == SourceSet.TargetRig
  == DatabaseArtifact.RigBinding
  == FootAnalysisArtifact.RigBinding
  == PresentationProjection.RigBinding
```

相等表示RigId和Revision都一致。Humanoid Avatar只负责Unity导入和采样映射，不能替代该身份闭包。Rig v4 active change完成前可实现节点、Chooser、编译器和runtime kernel；一旦最终Rig revision改变，所有旧MM数据库与派生产物必须显式重建，不能运行时接受旧revision。

每个角色Prefab各自拥有一份正式Presentation Rig identity。`MotionMatchingDemoCharacter` MAY因为使用GASP目标骨架而拥有与Corin不同的RigId；这不构成分裂路径。禁止的是同一个新Prefab同时保存Presentation Rig和另一份MM专用Rig，或者在运行时通过Retarget/fallback弥合不一致。

### MotionMatchingDemoCharacter内容边界

第一期只建立Grounded数据库：

- Idle数据库：静止与小幅调整。
- Locomotion数据库：Walk与Run。
- Sprint数据库：明确Sprint事实成立时开放。

Chooser根据Grounded与Gait typed事实选择数据库集合。方向、速度、朝向误差、姿势与历史连续性由feature/cost搜索；不得创建`IsStarting`、`IsStopping`、`IsPivoting`等仅由动画文件名推断的Gameplay事实。

新角色的Action、Dodge、Attack和Traversal如果存在，必须保持Timeline/Slot所有权；任何承担明确Gameplay/root-motion语义的MovingTurn也必须保留独立状态。GASP的Crouch、Jump、Slide和Traversal素材先不进入正式库，直到对应业务状态、输入事实、root motion和打断边界存在正式spec。Corin的Definition、Presentation Profile、Pose Graph、Rig和生成物不由本change改写。

### 独立角色Prefab装配

`MotionMatchingDemoCharacter.prefab`是新增内容装配根，不是独立runtime。它 MUST使用标准`CharacterPipelineHost`和现有Session注册边界，并显式引用自己的`CharacterPipelineDefinition`。该Definition引用专属Presentation Profile；Profile引用专属Pose Graph、唯一Presentation Rig、MM Profile、Chooser和生成物。

Prefab MAY使用与Corin不同的模型、Animator Avatar和Rig Definition，但 MUST不挂第二套MM组件、自主Update player、MxMAnimator、Animator Controller或shadow skeleton。运行时仍只存在`GameplayTickSystem -> SimulationSessionHost -> CharacterPipeline -> Presentation -> Pose Program`一条链。

## Runtime Order

同一表现帧的顺序为：

1. 解析`CharacterMotionMatchingFrameContext`。
2. Collector发布previous history read view。
3. Chooser按typed事实解析数据库集合。
4. MM节点构建query并调用Search Kernel。
5. 节点把Continue或Jump应用到自己的entry player与internal Blend Stack。
6. 每个live entry经过内部entry processing graph。
7. Blend Stack Kernel产出MM基础Local Pose与source usage。
8. Collector提交该基础Pose到history。
9. 下游Action Slot、Root处理、空间转换、Foot Placement与FullBodyIK继续执行。
10. FinalPublication完成后释放本帧只读页和零贡献source。

任何阶段出现Rig、generation、completion、artifact或workspace不一致都阻止本帧后续Pose publication；系统不得回放上一帧Pose、改用默认clip或切回旧MM provider。

## Authoring and Build

- Profile Inspector是MM Profile、Chooser、数据库和Rig闭包的唯一跨资产入口。
- Pose Graph Canvas只编辑节点和typed edge；双击MM节点进入entry graph。
- Analysis/Build入口显式生成Database Artifact、Foot Analysis、Projection和Native Pose Program。
- Mutation、Document exporter/reconciler、Canvas、Inspector、Validator和Compiler共享同一Capability描述，不分别维护节点默认值。
- 打开、选择、刷新、Domain Reload和Play Mode只显示stale或invalid诊断，不执行写资产。

## Diagnostics

Preview、Pose Watch、Live Debug和Trace必须能从一个MM节点identity追踪：Chooser命中规则、数据库集合、query cadence、admission结果、各cost channel、Continue/Jump原因、selection generation、active entries、每entry处理图、最终权重、Stored Pose、source usage、history read/commit frame和Rig lineage。

诊断只观察正式计划与已完成页，不重新查询、不补采样、不维护shadow player。Invalid必须指出具体合同，例如`ChooserDatabaseOutsideProfile`、`RigRevisionMismatch`、`MissingHistoryCollector`或`EntryGraphContainsWorldAwareNode`。

## Migration

1. 将旧change已完成的Database/Search/Artifact代码保留并改为Search Kernel输入输出。
2. 新增节点、Collector、Chooser和entry graph合同。
3. 重写Pose IR/Compiler/Runtime，使MM节点直接产出Local Pose。
4. 删除`CharacterMotionMatchingPoseSourceSlot`、MM `PresentationPoseSourceSample`、`SelectedPosePlayer`和显式MM BlendStack分支。
5. 删除旧Module的选择/历史/reset状态，只保留Frame Context与Search Kernel。
6. 更新Document、Mutation、Validator、Projection、Preview和Diagnostics。
7. 在Rig v4 schema上建立`MotionMatchingDemoCharacter`唯一Rig和Grounded内容并显式构建全部产物。
8. 创建并装配新增角色Prefab，使其从第一版只使用新MM节点链；Corin资产保持不变。

迁移期间不得提交同时可运行的新旧MM路径。若某一步必须依赖旧payload才能继续，实施应停在不可发布状态并完成下一步清理后再恢复正式链。

## Risks and Tradeoffs

### 节点内部拥有播放器

- 收益：搜索结果、采样时间、跳转、混合和source usage天然同一generation；作者只放一个节点。
- 代价：MM节点比普通Sequence Player重，Compiler与Preview必须理解内部entry生命周期。
- 业务取舍：MM本来就是“选择并播放最合适姿势”的完整表现能力，把Player拆出去没有用户可见价值，只增加配置和排错成本。

### Pose History使用外部Collector

- 收益：历史采样点在图中可见，可明确排除Action和IK，也允许未来其它查询能力复用。
- 代价：编译器需要两阶段read/commit和唯一绑定验证。
- 业务取舍：历史如果藏在MM节点里，作者无法看出记录的是基础Pose还是最终Pose，调动作与落脚时会产生错误搜索反馈。

### Chooser只允许typed规则

- 收益：数据库为什么被选中可以构建、预览、回放和诊断；不依赖运行时脚本顺序。
- 代价：新增业务分区必须先把事实加入正式Capability。
- 业务取舍：这是有意限制。MM不能从动画资源命名替Gameplay系统创造蹲伏、跳跃或Traversal状态。

### 正式内容等待Rig revision稳定

- 收益：数据库、Foot Feature与Projection只构建一次最终lineage，不产生马上失效的大量资产。
- 代价：节点和工具完成后，新角色Prefab的正式内容发布仍受Rig v4 schema及其最终Rig revision落地顺序影响。
- 业务取舍：等待的是该Prefab唯一Rig合同，不是等待Corin迁移、第二套MM Rig或IK算法；先造临时v3数据库只会增加删除成本。

## Open Questions Resolved

- MM现在是不是节点：本change完成后是正式Pose节点，不再是节点背后的source slot。
- 为什么不能继续外接Player：因为MM Jump和Player/Blend generation必须原子一致，拆开没有独立业务owner。
- 是否照抄UE：对齐职责和作者体验，不复制引擎资产格式与反射运行时。
- IK是不是MM前置：IK算法不是；唯一Rig identity是数据库生成物前置。MM基础Pose在IK之前执行。
- GASP全部素材是否一次接入：不是。首期只接入已有Gameplay事实能解释的Grounded Idle/Walk/Run/Sprint。
- 是否直接改Corin：不是。新增`MotionMatchingDemoCharacter` Prefab承载正式MM内容，Corin保持现有表现资产。
