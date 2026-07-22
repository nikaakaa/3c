## ADDED Requirements

### Requirement: Motion Matching必须是Animation Presentation Profile装配的正式Pose Source

当且仅当`CharacterAnimationPresentationProfile`声明至少一个Motion Matching Pose Source producer时，它 MUST唯一引用一个`CharacterMotionMatchingProfile`，后者 MUST装配Feature Schema、Trajectory Policy、Cost Profile、Search Policy、Database Definition与producer-to-SearchDomain binding。未声明MM producer的Profile MUST不引用MM Profile，Projection MUST不生成MM payload，Runtime MUST不构造MM模块。Graph、Timeline、Prefab、Presenter与Runtime MUST不保存另一份MM配置。MM MUST只作为已编译AnimationChannel的上游Pose Source，并进入该Channel绑定PoseSlot的唯一Blend Stack。

#### Scenario: 独立验证配置装配Grounded数据库

- **WHEN** 独立验证Profile绑定自己的稳定Motion Matching producer
- **THEN** binding MUST精确解析GroundedLocomotion SearchDomain、Database、BaseLocomotion Channel与Slot
- **AND** Runtime MUST不按状态名、clip名或目录猜测绑定

#### Scenario: 未配置MM Profile

- **WHEN** producer声明Motion Matching Pose Source但Animation Presentation Profile没有合法MM Profile
- **THEN** Projection Build MUST失败
- **AND** MUST不改用Timeline producer或默认数据库

#### Scenario: 配置没有声明MM producer

- **WHEN** Character Animation Presentation Profile没有MM producer且没有MM Profile引用
- **THEN** Projection Build MUST不生成MM payload
- **AND** Presentation Runtime MUST不构造MM query、search、plan、history或pose source模块

### Requirement: 导入动画必须通过Motion Source Set显式登记

`CharacterMotionMatchingSourceSet` MUST作为导入FBX或独立AnimationClip进入MM的唯一authoring边界。Source Set MUST保存稳定SourceSetId、Target Rig identity、Sampling Compatibility Mode、Motion Root BoneId，以及每个Clip的稳定SourceClipId、AnimationClip Asset GUID与local file id。Database Segment MUST只引用SourceClipId。系统 MUST不扫描目录、按文件名推断动画角色、按列表index绑定Clip或自动选择其它Avatar、Rig与Prefab。

#### Scenario: 作者登记FBX内嵌Clip

- **WHEN** 作者通过Object Picker、拖放或显式加入命令把FBX内嵌Clip登记到Source Set
- **THEN** Source Set MUST保存该Clip的Asset GUID、local file id与新生成的SourceClipId
- **AND** 该操作 MUST不执行Foot Analysis、MM采样、数据库构建或Character Build

#### Scenario: Clip移动或重命名

- **WHEN** 已登记Clip在GUID与local file id不变的情况下移动目录或修改显示名
- **THEN** SourceClipId与Segment引用 MUST保持不变
- **AND** 系统 MUST不按旧路径或旧名称重建绑定

#### Scenario: FBX重新导入

- **WHEN** GUID/local file id仍可解析但import dependency或Avatar/Rig signature发生变化
- **THEN** 既有MM Artifact MUST变为Stale
- **AND** 系统 MUST等待作者显式Build而不是自动采样或修复

### Requirement: Motion Source采样兼容性必须显式且在目标Rig上证明

Source Set MUST显式选择`HumanoidRetargeted`或`ExactGenericRig`。`HumanoidRetargeted` MUST要求源Clip与目标Sampling Rig拥有有效Humanoid Avatar，并在目标Sampling Rig上采样最终retarget结果。`ExactGenericRig` MUST要求源Clip的root node和Schema所需完整bone hierarchy signature与目标Rig精确匹配。一个Source Set MUST不混合两种模式；不同Source Set只有在降低到相同Target Rig identity时才 MAY进入同一Database。

#### Scenario: Humanoid动画包进入目标Rig

- **WHEN** Source Set选择HumanoidRetargeted且所有Clip与目标Sampling Rig都拥有有效Humanoid Avatar
- **THEN** Builder MUST在目标Sampling Rig上采样root、pose与foot输入
- **AND** MUST不把源骨架局部曲线直接当作目标Rig feature

#### Scenario: Generic骨架不一致

- **WHEN** Source Set选择ExactGenericRig但任一所需bone path、root node或hierarchy signature不匹配
- **THEN** Build MUST报告精确不匹配identity并停止
- **AND** MUST不按骨骼名、Humanoid映射或场景Animator近似retarget

### Requirement: Motion Matching Database必须由显式Editor Analysis Build生成

每个`CharacterMotionMatchingDatabaseDefinition` MUST通过显式Editor重操作生成规范`CharacterMotionMatchingDatabaseArtifact`。Artifact MUST固定写入`Library/CharacterSimulation/Analysis/MotionMatching/<database-guid>.mmdb`并使用候选文件原子发布。Asset import、Inspector repaint、selection change、domain reload、Play Mode切换与普通Character Compile MUST不触发分析。Product Build MUST只消费既有合法Artifact。

#### Scenario: 作者显式构建数据库

- **WHEN** 作者从MM Profile Inspector执行Build Database
- **THEN** Builder MUST精确读取Database、Source Set、Schema、Rig、Clip dependency与Foot Analysis Artifact
- **AND** 成功后 MUST原子发布新的`.mmdb`

#### Scenario: Character Build发现Artifact过期

- **WHEN** Database authoring或任一Clip dependency已经变化但Artifact identity仍属于旧内容
- **THEN** Character Build MUST报告stale并停止
- **AND** MUST不自动重分析或打包旧Artifact

### Requirement: Motion Matching重分析必须显式、可取消且不长时间阻塞Editor

作者执行`Build Motion Matching Database`后，Editor MUST先显示Database、Source Set、Clip数量、sample数量、缺失或Stale Foot Artifact数量与内存上界，并在作者确认后创建不可变Build Request。存在无效Foot Artifact时Preflight MUST失败并指向现有显式Foot Analysis入口，不得隐式生成。Build Job MUST按固定sample或工作单元在Editor update之间切片，显示stage与精确进度并允许Cancel；切片 MUST不改变sample顺序、计算顺序或Artifact bytes。

#### Scenario: 作者只选择或登记大量Clip

- **WHEN** 作者选择Project资源、打开Source Set或登记一批Clip但没有点击Build
- **THEN** Editor MUST不创建Build Job、不实例化Sampling Rig且不采样AnimationClip
- **AND** Inspector MUST只更新authoring与轻量status

#### Scenario: 作者取消数据库构建

- **WHEN** Build Job在采样、Normalization、Index或Coverage阶段收到Cancel
- **THEN** Job MUST释放隐藏Sampling Rig并删除候选文件
- **AND** 旧完整Artifact MUST保持不变且不得伪装成当前Ready

#### Scenario: 构建期间输入变化

- **WHEN** 任一Source Set、Clip dependency、Rig、Schema、Policy或Foot Artifact identity在Job期间变化
- **THEN** 最终发布校验 MUST拒绝候选Artifact
- **AND** Job MUST不使用新旧输入混合完成发布

### Requirement: Database Artifact必须形成完整Identity与Feature闭包

Artifact identity MUST包含schema/algorithm version、Database/Source Set/Schema/Rig identity与revision、SourceClipId、Asset GUID/local file id、import dependency、Avatar或Generic hierarchy signature、Motion Root、Foot Analysis Artifact hash、有序Clip dependency hash及ContentHash。Artifact MUST保存dense feature layout、segment/sample表、normalization、continuation graph、exact lower-bound index、runtime capacity与coverage summary。Runtime MUST只消费Projection中的不可变payload，不读取Library Artifact或Editor authoring。

#### Scenario: Foot Analysis已变化

- **WHEN** 匹配Clip的Foot Analysis Artifact content hash改变
- **THEN** 旧MM Artifact MUST判定stale
- **AND** Projection Compiler MUST拒绝混用旧contact feature

#### Scenario: Runtime加载数据库

- **WHEN** Character Host加载匹配的Presentation Projection
- **THEN** MM Runtime MUST从Projection取得全部query/search/source payload
- **AND** MUST不访问AssetDatabase、Library路径或Database Definition对象

### Requirement: Feature Schema必须显式描述Trajectory、Pose与Initialization输入

Feature Schema MUST绑定稳定Rig identity，保存严格递增且包含零时刻的trajectory horizons、参与比较的稳定BoneId、启用的position/velocity/facing/angular channels、Foot Feature来源和Initialization Feature Mask。Feature layout MUST在Build时降低为dense index；Runtime MUST不按字符串或Humanoid骨骼查找。

#### Scenario: Schema使用未知BoneId

- **WHEN** Schema引用Rig中不存在的BoneId
- **THEN** Artifact Build MUST失败并指出Bone identity
- **AND** MUST不按名称、层级或Humanoid mapping补全

#### Scenario: History尚未建立

- **WHEN** Reset后首次执行MM query
- **THEN** Runtime MUST使用Schema编译的Initialization Feature Mask
- **AND** MUST只允许`CanInitialize`候选

### Requirement: Segment与Continuation必须显式且有限

每个Database Segment MUST拥有稳定SegmentId、SourceClipId、合法时间范围、Loop/Finite语义、CanInitialize、CanJumpInto、Entry/Exit exclusion及显式Continuation target或Terminal。Compiler MUST从该authoring降低sample-level有向continuation graph；Runtime MUST不按pose相似度、clip末尾或文件名猜测后继。

#### Scenario: Finite Stop进入Idle

- **WHEN** Stop segment作者声明Idle segment为Continuation target
- **THEN** Compiler MUST建立末端到Idle合法入口的sample link
- **AND** Plan Rerank MAY沿该link覆盖horizon

#### Scenario: Finite Segment没有结束声明

- **WHEN** 非Loop segment既没有Continuation target也没有Terminal
- **THEN** Artifact Build MUST失败
- **AND** MUST不隐式Hold最后一帧或回绕

### Requirement: Database必须用Coverage Requirement证明导入内容满足业务范围

每个正式Search Domain MUST声明有界Coverage Requirement，至少表达所需速度区间、面对变化区间、Initialization资格、左右脚接触组合与最短plan horizon。Builder MUST只从目标Sampling Rig上的真实root、pose、foot与continuation sample证明覆盖。文件名、目录、作者标签和Gameplay期望速度 MUST不作为证明。任一要求缺失时Build MUST失败并输出缺失区域；系统 MUST不自动镜像、合成root trajectory或借用其它Database补足。

#### Scenario: 导入动画覆盖完整移动范围

- **WHEN** 真实sample覆盖Definition声明的全部速度、转向、初始化、接触与plan requirement
- **THEN** Coverage Report MUST为每项Requirement保存Satisfied证明与sample区间
- **AND** `.mmdb` MAY原子发布为Ready

#### Scenario: 移动动画实际为in-place

- **WHEN** Coverage Requirement包含非零移动速度但目标Sampling Rig上的Motion Root trajectory接近零
- **THEN** Build MUST报告对应速度区域Missing并停止发布
- **AND** MUST不从脚速度、输入速度或Clip名称合成root displacement

### Requirement: Coverage诊断必须使用正式Search Policy代价语义

Search Policy MUST显式保存有限且大于0的`CoverageNearDuplicateCostThreshold`。该阈值 MUST与Runtime最终使用的weighted normalized squared feature cost单位一致；Builder MUST不硬编码第二阈值或在字段无效时使用默认值。Coverage diagnostics MUST在Artifact唯一coverage section中保存从全部`CanInitialize` sample沿正式continuation边计算的sample/segment reachability、canonical active normalized feature exact duplicate、排除exact pair后的near duplicate、按`CoverageRequirementId + ProtectedContactMask`实际评估的protected-contact空区、完整hard admission候选上界与root edge-depth为0的search index最大深度。全部count、ratio、pair denominator、capacity与depth MUST自洽，且这些离线诊断 MUST不改变Runtime admission、search、plan或selection。

#### Scenario: 统计near duplicate

- **WHEN** Builder比较两个不属于exact duplicate的sample
- **THEN** 它 MUST使用最终dense weight和active normalized feature计算与Runtime相同的squared cost
- **AND** 只有cost小于或等于正式`CoverageNearDuplicateCostThreshold`时才 MUST计入near-duplicate pair

#### Scenario: Search Policy阈值无效

- **WHEN** `CoverageNearDuplicateCostThreshold`非有限或不大于0
- **THEN** Authoring validation与Runtime payload construction MUST拒绝该Search Policy
- **AND** Builder MUST不使用硬编码或fallback阈值继续构建

### Requirement: Trajectory Source必须来自正式Accepted Intent或Selected Body

每个Actor MUST由Presentation Factory显式装配唯一`ICharacterMotionMatchingTrajectorySource`。Local与Prediction source MUST消费已被Program/World request链接受并随atomic Body发布的`CharacterPresentationTrajectoryIntent`；Observed/Remote source MUST消费正式Selected Body interval。Runtime MUST不直接读取InputAction、Scene Transform、CharacterController、packet或Network Model类型。

#### Scenario: Local预测角色生成轨迹

- **WHEN** Tick T的motion request已经World Solve并atomic Commit
- **THEN** Accepted Intent source MUST发布与Body interval同identity的desired velocity/facing事实
- **AND** MM MUST不读取原始设备输入

#### Scenario: Remote没有未来意图

- **WHEN** Observed Actor只有Selected Body interval
- **THEN** Factory MUST装配Selected Body trajectory source
- **AND** Runtime MUST不改读本地Input或Scene Transform

### Requirement: Future Trajectory必须表达置信包络而不是单一假事实

`MotionMatchingTrajectoryEnvelope` MUST为每个编译horizon保存局部position/facing中心、position/facing tolerance与confidence。Accepted Intent与Selected Body MAY生成不同包络，但 MUST使用同一payload与cost语义。所有tolerance/confidence曲线 MUST来自编译Trajectory Policy；Runtime MUST不按Network Model应用隐藏倍率。

#### Scenario: Remote远期预测不确定

- **WHEN** Selected Body sample age和未来horizon增加
- **THEN** envelope MUST按正式Policy扩大tolerance并降低confidence
- **AND** Debug MUST显示该变化来自source uncertainty

#### Scenario: 本地立即转向

- **WHEN** Accepted Intent明确改变desired facing且短期confidence较高
- **THEN** trajectory cost MUST对不匹配转向的candidate产生明确分项
- **AND** MUST不等待当前Body完全转向后才响应

### Requirement: Pose History必须只记录BaseLocomotionSlot正式结果

MM Pose History MUST在全部PoseSlot完成后追加当前`BaseLocomotionSlot`的dense pose、bone velocity、per-foot feature、continuity与presentation time。Query MUST只消费上一帧及更早history。FinalAnimationPoseFrame中的FullBody覆盖、Foot Placement解算结果与VisualRoot world correction MUST不进入MM Pose History。

#### Scenario: Attack全身覆盖Locomotion

- **WHEN** FullBodyActionSlot完全遮蔽BaseLocomotionSlot
- **THEN** MM History MUST继续记录BaseLocomotionSlot结果
- **AND** MUST不记录Attack最终骨骼姿势

#### Scenario: Body分支重置

- **WHEN** Body ResetSequence变化
- **THEN** Pose History MUST在下一次query前清空
- **AND** Runtime MUST进入Initialization Query

### Requirement: Candidate Search必须先执行硬准入

Search MUST在计算soft cost前依序校验identity、SearchDomain、Initialization/Jump资格、segment exclusion、minimum jump interval、plan horizon、continuation graph与左右脚protected contact。任一失败 MUST产生稳定RejectReason且不得通过调整cost weight重新准入。无合法candidate MUST产生typed Invalid。

#### Scenario: 左脚仍处于受保护plant

- **WHEN** 当前Base pose声明左脚contact受保护且candidate入口左脚状态或速度不兼容
- **THEN** candidate MUST在admission阶段被拒绝
- **AND** 高轨迹匹配分 MUST不能覆盖该拒绝

#### Scenario: 所有候选均不合法

- **WHEN** 当前Domain没有通过admission的candidate
- **THEN** MM Runtime MUST发布typed Invalid及reject统计
- **AND** MUST不搜索其它Domain、全库或旧Locomotion

### Requirement: Search必须以稳定下界剪枝保持精确Top-K

Artifact MUST保存能够计算feature cost下界的稳定层级索引。Runtime MUST按stable node order遍历，以当前Top-K最差exact cost安全剪枝节点，并对未剪枝叶candidate计算完整exact cost。最终Top-K MUST与对全部admitted candidate执行同一exact cost所得结果一致；同分 MUST由stable SampleId打破。Search MUST不按wall-clock budget提前退出。

#### Scenario: 索引剪枝大批候选

- **WHEN** 某节点理论最小cost已经高于当前Top-K阈值
- **THEN** Runtime MAY跳过该节点全部sample
- **AND** Search snapshot MUST记录node prune与lower bound

#### Scenario: 两个candidate完全同分

- **WHEN** exact cost与plan cost均相等
- **THEN** winner MUST由stable SampleId顺序决定
- **AND** 资源枚举顺序 MUST不改变结果

### Requirement: Top-K必须经过固定短时序Plan评估

Exact Search的Top-K candidate MUST沿显式continuation graph推进编译后的固定horizon，累计trajectory、facing、contact release、segment end、速度变化与下一mandatory search余量，生成`MotionMatchingSelectionPlan`。Plan MUST有固定最大sample数和workspace；它 MUST不执行Gameplay Timeline、Notify、Window、Cue或Motion。

#### Scenario: 当前pose很像但片段即将非法结束

- **WHEN** candidate entry exact cost很低但无法沿continuation覆盖plan horizon
- **THEN** candidate MUST在admission或plan阶段被拒绝
- **AND** MUST不先选中再在下一帧紧急跳转

#### Scenario: Stop计划平滑进入Idle

- **WHEN** Stop candidate沿显式continuation进入Idle且整体horizon cost最低
- **THEN** Selection MUST保存entry与horizon end identity
- **AND** Pose Source MUST按plan continuation连续采样

### Requirement: Motion Matching时间计划必须表达连续视觉时间与手动采样

`MotionMatchingSelectionPlan` MUST输出`EntryVisualAdvanceRate`。该值 MUST由当前sample的正式`NextSampleIndex`、next sample clip time与Database Sample Rate计算；segment尾没有next时 MUST使用作者声明的Segment EndTime与当前sample time的剩余量计算，MUST不使用默认倍率、sample index差或表现帧率猜测。Selection Runtime MUST只使用包含SampleTime、ContinuousVisualTime、Cycle、VisualTimeScale、Looping与固定`AnimatorStateSpeed = 0`的`MotionMatchingPoseTimePlan`。SampleAccumulator MUST按表现delta累计，有效SampleTime MUST按`SampleAccumulator * EntryVisualAdvanceRate`推进。非loop MUST满足`ContinuousVisualTime = SampleTime`且`Cycle = 0`；loop MUST满足`ContinuousVisualTime = SampleTime + Cycle * Segment.Duration`，同segment sample time回绕时 MUST令Cycle递增。`VisualTimeScale` MUST精确等于`EntryVisualAdvanceRate`并只表示有效视觉时间推进；`AnimatorStateSpeed = 0` MUST只表示后端手动采样。Initialize、Jump、reset或source断裂 MUST不通过把`VisualTimeScale`设为0来表达。

#### Scenario: Continue跨越Loop边界

- **WHEN** 当前Selection沿同一loop segment的正式next link从较大sample time回绕到较小sample time
- **THEN** Selection Runtime MUST保持同一generation并令Cycle精确增加1
- **AND** ContinuousVisualTime MUST按Segment Duration保持连续推进而不得退回首圈时间

#### Scenario: Animancer手动采样MM Clip

- **WHEN** MM Pose Source把有效PoseTime降低为ClipSamplePlan
- **THEN** Animancer backend MUST按ClipTime精确设置source state采样时间
- **AND** source state Speed MUST固定为0，同时request VisualTimeScale MUST继续等于EntryVisualAdvanceRate

### Requirement: AnimationPoseSourceId必须完整表达Playback与Motion Matching Selection Generation

Program producer一次activation MUST继续唯一拥有`AnimationPlaybackId`。MM Initialize与每次Jump MUST提升`MotionMatchingSelectionGeneration`，并在公共降低边界将其精确降低为`AnimationPoseSelectionGeneration`；同plan Continue MUST保持generation。`AnimationPlaybackId`、Motion Matching source kind与降低后的selection generation MUST共同形成唯一`AnimationPoseSourceId`，Blend Stack MUST比较完整source identity，不能只因Playback相同就把Jump解释为Continue。`SourcePoseContinuityIdentity` MUST精确等于当前有效`MotionMatchingSelectionGeneration.Value`，Continue MUST保持该值，Initialize与Jump MUST随新generation改变；它 MUST不从sample index、sample time、`PresentationRequestSequence`或独立allocator派生。

#### Scenario: 同一MM producer从Run切到Pivot sample

- **WHEN** Program的BaseLocomotion MM playback没有变化但Search选择新的Pivot entry
- **THEN** MM MUST提升MotionMatchingSelectionGeneration，并使AnimationPoseSourceId与SourcePoseContinuityIdentity同时变化
- **AND** AnimationPlaybackId MAY保持不变，BaseLocomotionSlot MUST使用唯一self-pair Inertial/CrossFade规则

#### Scenario: 当前plan继续推进

- **WHEN** 下一次sample仍属于同一Selection Plan continuation
- **THEN** MM MUST保持MotionMatchingSelectionGeneration、AnimationPoseSourceId与SourcePoseContinuityIdentity
- **AND** ContinuousVisualTime MUST连续推进，Blend Stack MUST不重启entry clock

### Requirement: Motion Matching必须降低为source-neutral Pose Request

MM Pose Source MUST把Selection降低为与Timeline共用的`ResolvedAnimationPoseRequest`，并且该request MUST只通过以下正式字段表达source与采样结果：`AnimationChannelId`、`PoseSlotId`、`AnimationPoseSourceId`、`SourcePoseContinuityIdentity`、`PresentationRequestSequence`、`ProgramProducerIndex`、`VisualSampleTime`、`ContinuousVisualTime`、`Cycle`、`VisualTimeScale`、`ClipSamplePlan[]`、dense `PoseParameters[]`、`LeftFootFeatures`、`RightFootFeatures`与`ExactTransitionIdentity`。request MUST不再把`AnimationPlaybackId`、`PoseSourceKind`与`PoseSelectionGeneration`作为三项拆分的顶层source identity。MM内部Clip plan MUST从唯一PoseTime精确写入ClipTime、ContinuousClipTime、NormalizedTime与IsLooping；Animancer source backend MUST只按ClipTime采样该plan并保持state Speed为0。MM MUST不建立私有fade、Stored Pose、Pose Graph或IK。

#### Scenario: MM选择新Clip sample

- **WHEN** Selection Generation创建新的entry
- **THEN** Pose Source MUST解析Projection clip binding，把PoseTime降低为VisualSampleTime、ContinuousVisualTime、Cycle、VisualTimeScale及对应ClipSamplePlan并提交resolved request
- **AND** request MUST进入BaseLocomotionSlot唯一Blend Stack

#### Scenario: MM输出Pose Parameter与左右脚Feature

- **WHEN** Pose Source降低selected Clip在VisualSampleTime的正式Projection结果
- **THEN** request MUST写入固定容量dense PoseParameters，并以canonical PoseParameterId `animation.foot-placement-weight`表达Foot Placement Weight
- **AND** request MUST分别携带LeftFootFeatures与RightFootFeatures，Foot MUST不二次查询MM Database

#### Scenario: MM试图私自混合旧pose

- **WHEN** 新candidate需要与当前pose过渡
- **THEN** 过渡 MUST由Blend Library和Slot Blend Stack完成
- **AND** MM Runtime MUST不持有第二个crossfade weight

### Requirement: Motion Matching不得应用Animation Root Motion到Gameplay Body

Database Builder MAY提取Animation root trajectory用于feature。Runtime MUST将selected source作为root-locked pose采样并保持`Animator.deltaPosition`/`deltaRotation`不进入Program、Body、VisualRoot、WorldSolver或Network。CharacterBodyPresentationRuntime MUST继续唯一写VisualRoot world pose。

#### Scenario: 选中带Root Motion的Start动画

- **WHEN** MM选择包含root displacement的Clip sample
- **THEN** displacement MUST只影响数据库trajectory匹配
- **AND** 实际角色位置 MUST仍来自Committed/Selected Body

### Requirement: MM运行时必须响应Presentation分支重置

Initialization、Committed branch replacement、Selected stream reset、Rollback presentation Replace/Retire、Projection replacement、Presentation reset与Dispose MUST在下次query前原子清理trajectory history、pose history、current plan、selection generation引用和protected contact。重置后 MUST执行Initialization Query，不能继续旧plan。

#### Scenario: Rollback替换移动分支

- **WHEN** Body Runtime提升ResetSequence并替换当前分支
- **THEN** MM MUST在同一PresentationFrame清理旧history与plan
- **AND** 新分支首个selection MUST使用新的Generation

#### Scenario: Remote selected stream重置

- **WHEN** Observed Actor Selected Body source重置
- **THEN** MM MUST丢弃旧source age与trajectory envelope
- **AND** MUST不沿用旧Remote plan

### Requirement: Motion Matching必须保持Simulation与Network单向隔离

MM query、database、selection、plan、history、cost、Blend Entry与diagnostics MUST不进入CharacterSimulationState、WorldSimulationState、Snapshot、StateHash、canonical input或network packet。Local、Prediction、Rollback与Observed Actor MUST共用同一MM runtime语义，只由Factory显式提供trajectory source。

#### Scenario: 两个客户端表现帧率不同

- **WHEN** 两端从相同Gameplay状态以不同render cadence运行MM
- **THEN** 它们 MAY选择不同但合法的Presentation pose
- **AND** Gameplay Hash、Body和网络结果 MUST保持不受影响

### Requirement: Motion Matching必须提供完整只读诊断与Search Replay

Runtime diagnostics MUST按interest暴露Profile/Database identity、trajectory envelope、history availability、admission count、reject reason、index visit/prune、Top-K exact cost、plan cost、selection、continue/jump、contact protection、generation、Blend Entry与reset。显式Capture MAY生成Search Replay Artifact；Replay MUST要求exact Database/Projection identity并复用正式search实现。

#### Scenario: 排查错误Pivot

- **WHEN** 作者关注一次Run到Pivot jump
- **THEN** snapshot MUST显示query facing、所有关键reject、Top-K分项和最终plan
- **AND** Debug MUST不重新运行另一套候选选择

#### Scenario: Replay身份已过期

- **WHEN** Capture引用的Database Artifact已被重建
- **THEN** Editor Replay MUST拒绝执行并显示identity mismatch
- **AND** MUST不把旧query迁移到当前Schema

### Requirement: Motion Matching热路径必须固定容量且无隐藏降级

Projection MUST编译database sample、tree、admission、Top-K、plan、history和diagnostic容量。Runtime MUST在构造时一次分配并在PresentationFrame复用，不得使用动态候选集合、字符串查找、反射或按帧扩容。若任一正式Domain最坏admitted sample超过Policy容量，Artifact Build MUST失败；Runtime MUST不减少Top-K、缩短plan horizon或切换搜索模式。

#### Scenario: Database规模超过正式容量

- **WHEN** 新增Clip使Grounded Domain最坏candidate数量超过Search Policy
- **THEN** Artifact Build MUST报告超限及分区统计
- **AND** Runtime MUST不按设备或帧耗时自动降低搜索质量

### Requirement: Motion Matching验证必须使用独立正式配置且不得修改Corin

Motion Matching能力 MUST通过用户另行提供并由验证环境显式选择的正式角色配置装配。该配置 MUST复用现有`CharacterPipelineDefinition`、`CharacterAnimationPresentationProfile`、Pose Graph、Blend Library、Rig Definition、Foot Analysis、MM Profile与Database Definition合同，不得创建验证专用Runtime、fallback配置或临时桥接。Corin的Graph、StateMachine、Timeline、Profile、Definition、Projection、Prefab、动画、Marker Group与transition MUST不被本change修改或引用为验证输入。

#### Scenario: 选择独立验证配置

- **WHEN** 验证环境显式选择一份拥有完整Rig、Clip、Foot Analysis、Database、Artifact与MM producer binding的Definition
- **THEN** Character Build MUST只为该Definition生成exact identity匹配的MM Projection payload
- **AND** Runtime MUST只从该Projection构造MM模块

#### Scenario: 独立验证配置缺少动画内容

- **WHEN** 验证Definition缺少Rig、Clip、Foot Analysis、Segment或Artifact任一正式输入
- **THEN** Build MUST以typed diagnostic失败
- **AND** MUST不借用Corin资产、默认资源、placeholder或非MM Pose Source

#### Scenario: Corin未声明Motion Matching

- **WHEN** Corin继续使用现有非MM Profile与producer链
- **THEN** Corin Projection MUST不包含MM payload且Runtime MUST不构造MM模块
- **AND** 本change MUST不要求重写Corin资产
