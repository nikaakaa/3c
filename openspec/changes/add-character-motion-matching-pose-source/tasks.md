## 1. 现状清点与共享合同冻结

- [x] 1.1 枚举当前全部Animation Pose Source producer类型及其Projection lowering入口。
- [x] 1.2 标记现有Profile、Definition、Graph、Timeline与Runtime中允许新增source类型的正式扩展边界。
- [x] 1.3 枚举当前BaseLocomotion Channel、PoseSlot、Blend transition与Marker Group绑定。
- [x] 1.4 枚举当前Airborne与FullBodyAction继续使用的非MM Pose Source。
- [x] 1.5 枚举`ResolvedAnimationPoseRequest`当前全部生产者与消费者。
- [x] 1.6 枚举`AnimationBlendEntryId`、`AnimationPlaybackId`与request sequence当前比较点。
- [x] 1.7 枚举当前Animancer source sampling backend支持的Clip与ManualMixer输入。
- [x] 1.8 枚举当前Character Body Presentation committed/selected stream与ResetSequence入口。
- [x] 1.9 枚举Local、Prediction、Rollback与Observed Actor Presentation Factory装配入口。
- [x] 1.10 枚举Animation Foot Analysis Artifact的builder、store、codec、resolver与Projection消费入口。
- [x] 1.11 枚举Character Presentation Projection的identity、payload、compiler和stale校验入口。
- [x] 1.12 枚举RuntimeDebugSession动画interest、snapshot、capture与view入口。
- [x] 1.13 记录并行Blend Stack change最终source-neutral request合同。
- [x] 1.14 记录并行Pose Graph change最终PoseSlotFrame、FinalAnimationPoseFrame与Foot contribution合同。
- [x] 1.15 锁定Animation Presentation Profile、Projection、Animation Playback Runtime与Simulation Presentation Runtime的唯一集成owner。

## 2. Motion Matching Identity与公共合同

- [x] 2.1 定义`CharacterMotionMatchingProfileId`值对象。
- [x] 2.2 定义`CharacterMotionMatchingDatabaseId`值对象。
- [x] 2.3 定义`CharacterMotionMatchingFeatureSchemaId`值对象。
- [x] 2.4 定义`CharacterMotionMatchingSearchDomainId`值对象。
- [x] 2.5 定义`CharacterMotionMatchingSegmentId`值对象。
- [x] 2.6 定义`CharacterMotionMatchingSampleId`值对象。
- [x] 2.7 定义`CharacterMotionMatchingQueryId`值对象。
- [x] 2.8 定义`CharacterMotionMatchingPlanId`值对象。
- [x] 2.9 定义`MotionMatchingSelectionGeneration`值对象。
- [x] 2.10 定义`MotionMatchingTrajectorySourceIdentity`值对象。
- [x] 2.11 定义所有identity的有效性、相等性与stable hash合同。
- [x] 2.12 定义`MotionMatchingPoseSourceKind`并接入source-neutral Pose Source分类。
- [x] 2.13 定义`MotionMatchingInvalidReason`枚举。
- [x] 2.14 定义`MotionMatchingCandidateRejectReason`枚举。
- [x] 2.15 定义`MotionMatchingSearchTriggerReason`枚举。
- [x] 2.16 定义`MotionMatchingSelectionDecision`的Continue、Jump、Initialize与Invalid合同。
- [x] 2.17 将公共Runtime合同放入Character Animation/Presentation正式程序集。
- [x] 2.18 保持公共合同不引用Editor、AssetDatabase、Float32、Fixed或Network Model类型。

## 3. Motion Matching Profile与Authoring资产

- [x] 3.1 创建`CharacterMotionMatchingProfile`ScriptableObject。
- [x] 3.2 为Profile保存稳定ProfileId与Revision。
- [x] 3.3 为Profile添加唯一Feature Schema引用。
- [x] 3.4 为Profile添加唯一Trajectory Policy引用。
- [x] 3.5 为Profile添加唯一Cost Profile引用。
- [x] 3.6 为Profile添加唯一Search Policy引用。
- [x] 3.7 为Profile添加有序Database Definition引用。
- [x] 3.8 为Profile添加producer-to-SearchDomain binding。
- [x] 3.9 创建`CharacterMotionMatchingFeatureSchema`资产。
- [x] 3.10 创建`CharacterMotionMatchingTrajectoryPolicy`资产。
- [x] 3.11 创建`CharacterMotionMatchingCostProfile`资产。
- [x] 3.12 创建`CharacterMotionMatchingSearchPolicy`资产。
- [x] 3.13 创建`CharacterMotionMatchingDatabaseDefinition`资产。
- [x] 3.14 创建`CharacterMotionMatchingSegmentDefinition`序列化合同。
- [x] 3.15 为Segment保存稳定SourceClipId而不是路径、名称、列表index或运行时Clip对象。
- [x] 3.16 为Segment保存Start/End、Loop、CanInitialize与CanJumpInto。
- [x] 3.17 为Segment保存Entry/Exit exclusion。
- [x] 3.18 为Segment保存显式Continuation target或Terminal。
- [x] 3.19 禁止Profile、Schema和Database保存Graph State名、Action名或InputAction引用。
- [x] 3.20 将条件式唯一MM Profile引用加入`CharacterAnimationPresentationProfile`schema。
- [x] 3.21 创建`CharacterMotionMatchingSourceSet`ScriptableObject。
- [x] 3.22 为Source Set保存稳定SourceSetId与Revision。
- [x] 3.23 为Source Set保存Target Rig identity。
- [x] 3.24 定义`HumanoidRetargeted`与`ExactGenericRig`采样兼容模式。
- [x] 3.25 为Source Set保存唯一Sampling Compatibility Mode。
- [x] 3.26 为Source Set保存稳定Motion Root BoneId。
- [x] 3.27 定义`CharacterMotionMatchingSourceClipEntry`。
- [x] 3.28 为Source Clip保存稳定SourceClipId。
- [x] 3.29 为Source Clip保存AnimationClip Asset GUID与local file id。
- [x] 3.30 禁止Source Set保存按文件名推导的Idle、Start、Loop、Pivot或Stop角色。
- [x] 3.31 让Database Definition显式引用有序Source Set identity。
- [x] 3.32 定义Database Coverage Requirement序列化合同。
- [x] 3.33 为Coverage Requirement保存速度与面对变化区间。
- [x] 3.34 为Coverage Requirement保存Initialization与左右脚接触要求。
- [x] 3.35 为Coverage Requirement保存最短plan horizon。

## 4. Authoring校验与Inspector入口

- [x] 4.1 创建Profile完整identity与引用闭包校验器。
- [x] 4.2 校验Feature horizons有限、严格递增且包含零时刻。
- [x] 4.3 校验Feature BoneId唯一且存在于Rig Definition。
- [x] 4.4 校验Cost Profile覆盖全部启用Feature group。
- [x] 4.5 校验Trajectory Policy的tolerance、confidence、acceleration与turn参数有限。
- [x] 4.6 校验Search Policy的TopK、leaf capacity、plan horizon与固定容量合法。
- [x] 4.7 校验Database与Schema/Rig identity一致。
- [x] 4.8 校验Segment identity唯一且Clip range合法。
- [x] 4.9 校验Loop Segment只回到自身合法入口。
- [x] 4.10 校验Finite Segment拥有Continuation target或Terminal。
- [x] 4.11 校验Continuation target存在且Search Domain一致。
- [x] 4.12 校验每个Search Domain至少存在一个CanInitialize sample来源。
- [x] 4.13 在Animation Presentation Profile Inspector添加MM Profile入口。
- [x] 4.14 在MM Profile Inspector按模块显示Schema、Policy、Database与binding。
- [x] 4.15 在Database Inspector显示Segment identity、范围、结束语义与Artifact状态。
- [x] 4.16 增加显式`Build Motion Matching Database`重操作按钮。
- [x] 4.17 在执行重操作前显示目标Database、Source Set、Clip数量、sample数量、Foot Artifact状态与内存上界。
- [x] 4.18 禁止Inspector OnGUI、OnValidate、selection与domain reload触发分析。
- [x] 4.19 禁止普通Character Compile隐式触发MM分析。
- [x] 4.20 让Product Build只显示Artifact missing/stale错误而不提供自动修复。
- [x] 4.21 在Database Inspector提供Source Set真实owner入口。
- [x] 4.22 在Source Set Inspector显示SourceClipId、GUID、local file id与解析状态。
- [x] 4.23 提供显式Object Picker和拖放命令登记Clip。
- [x] 4.24 提供显式“加入当前选择Clip”authoring命令。
- [x] 4.25 保证登记Clip命令只修改Source Set且不执行任何分析或Build。
- [x] 4.26 禁止AssetPostprocessor在FBX导入或重导入后触发Foot Analysis或MM Build。
- [x] 4.27 禁止Project selection change触发Sampling Rig实例化或Clip采样。
- [x] 4.28 轻量校验只解析GUID、local file id、Importer声明与Avatar有效性。
- [x] 4.29 校验SourceClipId在Source Set与Database闭包内唯一。
- [x] 4.30 校验Database引用的SourceClipId存在且没有orphan。
- [x] 4.31 校验一个Source Set只声明一种Sampling Compatibility Mode。
- [x] 4.32 校验全部Source Set降低到Database Target Rig identity。
- [x] 4.33 校验Motion Root BoneId存在于Target Rig。
- [x] 4.34 校验Coverage Requirement区间有限、有序且不重叠冲突。
- [x] 4.35 在Source Set Inspector增加显式`Build Source Set Foot Analysis`按钮。
- [x] 4.36 在确认框显示Analysis Source、Sampling Rig、Clip状态统计与预计sample数量。

## 5. Database Artifact Identity、Codec与Store

- [x] 5.1 定义`CharacterMotionMatchingDatabaseArtifactIdentity`。
- [x] 5.2 在Identity中保存Artifact Schema Version。
- [x] 5.3 在Identity中保存Analysis Algorithm Version。
- [x] 5.4 在Identity中保存Database、Schema与Rig identity/revision。
- [x] 5.5 在Identity中保存Foot Analysis Artifact content hash。
- [x] 5.6 在Identity中保存有序Clip dependency hashes。
- [x] 5.7 在Identity中保存canonical ContentHash。
- [x] 5.8 定义Artifact header与section table。
- [x] 5.9 定义Segment与Sample canonical codec。
- [x] 5.10 定义dense feature与normalization canonical codec。
- [x] 5.11 定义continuation graph canonical codec。
- [x] 5.12 定义lower-bound search index canonical codec。
- [x] 5.13 定义runtime capacity canonical codec。
- [x] 5.14 定义coverage summary canonical codec。
- [x] 5.15 为codec拒绝未知schema version、尾随字节和非canonical排序。
- [x] 5.16 实现Artifact exact-byte round-trip读取路径。
- [x] 5.17 实现`Library/CharacterSimulation/Analysis/MotionMatching/<guid>.mmdb`路径解析。
- [x] 5.18 实现候选文件写入与原子替换。
- [x] 5.19 让写入失败保留旧文件但不伪造新identity成功。
- [x] 5.20 实现Artifact status的Missing、Ready、Stale与Invalid诊断。
- [x] 5.21 将Source Set identity与Revision加入Artifact identity。
- [x] 5.22 将SourceClipId、Asset GUID与local file id加入Artifact identity。
- [x] 5.23 将当前Clip import dependency加入Artifact identity。
- [x] 5.24 将Humanoid Avatar identity或Generic hierarchy signature加入Artifact identity。
- [x] 5.25 将Motion Root BoneId加入Artifact identity。
- [x] 5.26 让FBX重导入只把Artifact判为Stale而不启动重建。

## 6. Clip采样与Feature提取

- [x] 6.1 创建Editor-only MM Database Build Request。
- [x] 6.2 创建Editor-only MM Database Build Result与diagnostic。
- [x] 6.3 精确解析Database引用的AnimationClip资源。
- [x] 6.4 读取每个Clip dependency hash并纳入ordered identity。
- [x] 6.5 按Database固定sample rate展开每个Segment sample time。
- [x] 6.6 在Segment范围外拒绝sample。
- [x] 6.7 提取每个sample的root local translation。
- [x] 6.8 提取每个sample的root facing。
- [x] 6.9 从相邻sample计算root linear velocity。
- [x] 6.10 从相邻sample计算root yaw velocity。
- [x] 6.11 按Schema horizons提取candidate future root trajectory。
- [x] 6.12 按稳定BoneId提取root-relative bone position。
- [x] 6.13 按稳定BoneId计算bone velocity。
- [x] 6.14 对rotation使用最短弧和规范化Quaternion。
- [x] 6.15 精确解析每个Clip的Animation Foot Analysis Artifact。
- [x] 6.16 按sample time重采样Left Foot feature。
- [x] 6.17 按sample time重采样Right Foot feature。
- [x] 6.18 将Foot Artifact hash加入Database Artifact identity。
- [x] 6.19 检测非有限root、bone与foot sample。
- [x] 6.20 检测超过硬阈值的root discontinuity。
- [x] 6.21 检测Segment范围内缺失的Rig pose sample。
- [x] 6.22 生成每个sample的CanInitialize、CanJumpInto与exclusion metadata。
- [x] 6.23 生成每个sample的left/right contact mask与contact velocity metadata。
- [x] 6.24 保持Feature Analyzer不读取Graph、Timeline runtime、Program或Projection。
- [x] 6.25 创建Source Set到精确AnimationClip的Editor resolver。
- [x] 6.26 使用Asset GUID与local file id解析FBX内嵌Clip。
- [x] 6.27 为HumanoidRetargeted创建目标Sampling Rig采样adapter。
- [x] 6.28 校验源Clip与目标Sampling Rig均拥有有效Humanoid Avatar。
- [x] 6.29 从目标Sampling Rig结果提取稳定BoneId pose而非源骨架曲线。
- [x] 6.30 为ExactGenericRig计算所需bone hierarchy signature。
- [x] 6.31 校验Generic root node、bone path与hierarchy signature精确匹配。
- [x] 6.32 禁止Generic按骨骼名或近似层级执行retarget。
- [x] 6.33 从显式Motion Root BoneId采样root trajectory。
- [x] 6.34 将runtime pose root降低为root-locked Clip binding。
- [x] 6.35 禁止从脚速度、Gameplay速度或Clip名称合成root trajectory。
- [x] 6.36 拒绝同一Database混入不同Target Rig的采样结果。
- [x] 6.37 定义不可变`MotionMatchingDatabaseBuildRequest`依赖快照。
- [x] 6.38 创建显式`MotionMatchingDatabaseBuildJob`状态机。
- [x] 6.39 为Build Job定义Preflight、Sampling、Normalization、Index、Coverage、Publish阶段。
- [x] 6.40 Preflight发现Foot Artifact Missing或Stale时停止并指向`Build Source Set Foot Analysis`入口。
- [x] 6.41 禁止MM Build Job隐式调用Foot Artifact Builder。
- [x] 6.42 让Sampling阶段每次Editor update只处理固定sample数量。
- [x] 6.43 让Normalization阶段每次Editor update只处理固定feature block数量。
- [x] 6.44 让Index阶段每次Editor update只处理固定node工作单元。
- [x] 6.45 让Coverage阶段每次Editor update只处理固定requirement工作单元。
- [x] 6.46 保持不同切片次数下sample与计算顺序完全一致。
- [x] 6.47 发布Build stage、完成数、总数与当前输入identity进度。
- [x] 6.48 提供显式Cancel并停止后续Editor update callback。
- [x] 6.49 Cancel时销毁隐藏Sampling Rig与Playable资源。
- [x] 6.50 Cancel时删除候选文件并保留旧完整Artifact。
- [x] 6.51 domain reload前终止活动Build Job且不自动恢复。
- [x] 6.52 发布前重新解析全部依赖identity并拒绝中途变化。
- [x] 6.53 Build异常时统一释放Sampling Rig、Playable与候选文件。
- [x] 6.54 定义不可变Source Set Foot Analysis Build Request。
- [x] 6.55 创建Source Set Foot Analysis Build Job。
- [x] 6.56 按稳定SourceClipId顺序收集Missing与Stale Clip。
- [x] 6.57 逐Clip调用现有`AnimationFootAnalysisArtifactBuilder`。
- [x] 6.58 禁止Source Set Build Job实现第二个Foot Analyzer。
- [x] 6.59 在相邻Clip之间通过Editor update让出控制权。
- [x] 6.60 发布当前Clip、完成Clip数与总Clip数进度。
- [x] 6.61 在相邻Clip之间响应Cancel并停止后续构建。
- [x] 6.62 保持已完整发布的单Clip Artifact并清理未完成候选。
- [x] 6.63 Source Set dependency变化时停止批量Job且不继续旧请求。
- [x] 6.64 保持Source Set Foot Analysis不依赖Timeline producer。

## 7. Normalization、Continuation与Coverage编译

- [x] 7.1 为每个dense feature channel计算中位数。
- [x] 7.2 为每个dense feature channel计算稳健尺度。
- [x] 7.3 将零尺度constant channel显式标记为不参与distance。
- [x] 7.4 拒绝非有限normalization值。
- [x] 7.5 将Cost Profile编译为dense feature weight buffer。
- [x] 7.6 校验dense weight长度与Feature layout精确一致。
- [x] 7.7 为普通Segment sample建立next sample link。
- [x] 7.8 为Loop Segment末端建立显式回绕link。
- [x] 7.9 为Finite Segment末端解析显式Continuation target入口。
- [x] 7.10 为Terminal Segment标记无后继语义。
- [x] 7.11 检测continuation graph悬空SampleId。
- [x] 7.12 检测unreachable Segment。
- [x] 7.13 统计每Search Domain的sample数量。
- [x] 7.14 统计velocity、facing与contact coverage区间。
- [x] 7.15 检测没有Initialization入口的coverage区间。
- [x] 7.16 检测无法覆盖Plan horizon的candidate区域。
- [x] 7.17 检测protected contact可能导致的空候选区域。
- [x] 7.18 统计duplicate与near-duplicate sample密度。
- [x] 7.18a 将有限且大于0的`CoverageNearDuplicateCostThreshold`编译进唯一Search Policy Runtime payload。
- [x] 7.19 统计每Domain最大admitted candidate上界。
- [x] 7.20 将coverage summary写入Artifact只读section。
- [x] 7.21 将每项Coverage Requirement降低为固定coverage region。
- [x] 7.22 从真实root sample统计速度coverage。
- [x] 7.23 从真实root sample统计面对变化coverage。
- [x] 7.24 从Segment资格统计Initialization coverage。
- [x] 7.25 从Foot Artifact统计左右脚接触组合coverage。
- [x] 7.26 从Continuation Graph统计最短plan horizon coverage。
- [x] 7.27 为每项Requirement生成Satisfied或Missing证明。
- [x] 7.28 任一正式Requirement为Missing时拒绝发布新Artifact。
- [x] 7.29 在Build失败时保持旧完整Artifact但不把它标为当前Ready。
- [x] 7.30 禁止自动镜像、复制Segment或借用其它Database补Coverage。

## 8. 精确Lower-Bound Search Index编译

- [x] 8.1 定义stable Search Index Node identity。
- [x] 8.2 定义叶节点SampleId有序范围。
- [x] 8.3 定义节点Search Domain metadata summary。
- [x] 8.4 定义节点left/right contact metadata summary。
- [x] 8.5 定义节点可剪枝feature min/max bounds。
- [x] 8.6 以stable SampleId顺序构建初始样本集。
- [x] 8.7 使用固定规则递归构建平衡层级树。
- [x] 8.8 以Search Policy leaf capacity结束分裂。
- [x] 8.9 为每个内部节点计算保守feature bounds。
- [x] 8.10 验证任一child bounds不超出parent bounds。
- [x] 8.11 验证所有admitted SampleId在树中精确出现一次。
- [x] 8.12 计算并保存最大tree depth与workspace capacity。
- [x] 8.13 拒绝超过Search Policy maximum admitted sample count的Domain。
- [x] 8.14 将index section纳入Artifact ContentHash。

## 9. Projection Compiler与Payload

- [x] 9.1 定义`MotionMatchingProjectionPayload`Runtime schema。
- [x] 9.2 在Payload保存Profile identity与revision。
- [x] 9.3 在Payload保存编译Feature Schema。
- [x] 9.4 在Payload保存编译Trajectory Policy。
- [x] 9.5 在Payload保存编译Cost Profile。
- [x] 9.6 在Payload保存编译Search Policy。
- [x] 9.7 在Payload保存producer-to-SearchDomain binding。
- [x] 9.8 在Payload保存全部Database Artifact identity。
- [x] 9.9 在Payload保存Segment、Sample与continuation数据。
- [x] 9.10 在Payload保存normalization与lower-bound index。
- [x] 9.11 在Payload保存Clip resource binding。
- [x] 9.12 在Payload保存history、candidate、Top-K与plan容量。
- [x] 9.13 扩展Projection Compiler仅在producer声明MM Pose Source时解析唯一MM Profile。
- [x] 9.14 扩展Projection Compiler精确解析每个`.mmdb`。
- [x] 9.15 让Projection Compiler拒绝Missing、Stale与Invalid Artifact。
- [x] 9.16 将MM Artifact identity/content加入ProjectionRevision。
- [x] 9.17 保持Projection payload不包含Numeric ProgramHash、ABI或State地址。
- [x] 9.18 扩展Projection Runtime校验全部MM identity与dense长度。
- [x] 9.19 扩展Projection Asset序列化新的MM payload。
- [x] 9.20 让无MM producer的Projection保持无MM payload的正式语义，不引入新旧schema双读或兼容分支。

## 10. Runtime Database与Workspace

- [x] 10.1 创建只读`CharacterMotionMatchingRuntimeDatabase`。
- [x] 10.2 在构造时校验Profile、Schema、Database与Rig identity。
- [x] 10.3 在构造时解析dense feature layout。
- [x] 10.4 在构造时解析Search Domain到sample range。
- [x] 10.5 在构造时解析Segment与continuation graph。
- [x] 10.5a 让Database payload只沿Sample的`ClipBindingIndex`解析唯一SourceClipId，并校验它与所属Segment完全一致。
- [x] 10.6 在构造时解析lower-bound tree。
- [x] 10.7 在构造时解析Clip binding index。
- [x] 10.8 预分配tree traversal workspace。
- [x] 10.9 预分配admission bitset与reject workspace。
- [x] 10.10 预分配exact Top-K workspace。
- [x] 10.11 预分配short-horizon plan workspace。
- [x] 10.12 预分配query feature与cost component buffer。
- [x] 10.13 预分配可选diagnostic detail buffer。
- [x] 10.14 禁止Runtime Database访问AssetDatabase、Library文件或authoring对象。
- [x] 10.15 实现明确Dispose顺序并释放全部Native资源。

## 11. Accepted Intent与Trajectory Source端口

- [ ] 11.1 定义`CharacterPresentationTrajectoryIntent`不可变合同。
- [ ] 11.2 在Intent保存ActorId、Previous/Current Tick与SourceSequence。
- [ ] 11.3 在Intent保存DesiredPlanarVelocity与DesiredFacing。
- [ ] 11.4 在Intent保存AcceptedAcceleration与AcceptedTurnRate。
- [ ] 11.5 在Intent保存Grounded、MovementMode与ResetSequence。
- [ ] 11.6 定义Intent interval的连续性与branch replacement规则。
- [ ] 11.7 定义`ICharacterMotionMatchingTrajectorySource`接口。
- [x] 11.8 定义统一Trajectory Source Frame。
- [ ] 11.9 实现Accepted Intent trajectory source。
- [x] 11.10 从World Solve后已接受motion result构造Intent候选。
- [x] 11.11 在atomic Body commit成功后发布匹配Intent interval。
- [x] 11.12 保证失败事务不发布部分Intent。
- [ ] 11.13 实现Selected Body trajectory source。
- [ ] 11.14 从selected interval读取position、rotation、velocity、yaw velocity与Grounded。
- [ ] 11.15 在Selected Body source保存sample age与source tick。
- [ ] 11.16 为Local Presentation Factory显式装配Accepted Intent source。
- [x] 11.17 为Prediction Presentation Factory显式装配Accepted Intent source。
- [ ] 11.18 为Observed/Remote Presentation Factory显式装配Selected Body source。
- [x] 11.19 为Rollback owner显式装配当前分支Accepted Intent source。
- [ ] 11.20 禁止任一source缺失时改读InputAction、Scene Transform或packet。
- [ ] 11.21 扩展Host/Factory校验trajectory source与Body source identity一致。
- [ ] 11.22 将trajectory source生命周期纳入Presentation Reset与Dispose。

## 12. Trajectory Envelope运行时

- [ ] 12.1 定义`MotionMatchingTrajectoryEnvelopePoint`。
- [ ] 12.2 在Envelope Point保存TimeOffset与LocalPositionCenter。
- [ ] 12.3 在Envelope Point保存LocalFacingCenter。
- [ ] 12.4 在Envelope Point保存PositionToleranceRadius。
- [ ] 12.5 在Envelope Point保存FacingToleranceDegrees。
- [ ] 12.6 在Envelope Point保存Confidence。
- [ ] 12.7 定义固定容量`MotionMatchingTrajectoryEnvelope`。
- [ ] 12.8 创建`CharacterMotionMatchingTrajectoryRuntime`。
- [ ] 12.9 将Accepted Intent按加速度限制积分到各horizon。
- [ ] 12.10 将Accepted Intent按turn rate限制积分facing。
- [ ] 12.11 将Selected Body velocity/yaw velocity外推到各horizon。
- [ ] 12.12 按Trajectory Policy计算各horizon position tolerance。
- [ ] 12.13 按Trajectory Policy计算各horizon facing tolerance。
- [ ] 12.14 按source age与horizon计算confidence。
- [ ] 12.15 将world trajectory转换到当前Body局部空间。
- [x] 12.16 拒绝非有限source、horizon与envelope结果。
- [ ] 12.17 保持Runtime不按Network Model type分支。
- [ ] 12.18 为ResetSequence变化清空trajectory历史。
- [ ] 12.19 输出source identity、tick、age与envelope continuity。

## 13. Base Pose History与Query构建

- [ ] 13.1 定义固定容量`CharacterMotionMatchingPoseHistory`。
- [ ] 13.2 按Schema BoneId分配dense pose history buffer。
- [ ] 13.3 保存每个history sample的presentation time。
- [ ] 13.4 保存每个history sample的Base slot continuity identity。
- [ ] 13.5 保存每个history sample的left/right Foot Feature。
- [ ] 13.6 从相邻Base slot sample计算bone velocity。
- [ ] 13.7 在全部PoseSlot求值后定位BaseLocomotionSlot frame。
- [ ] 13.8 在Foot Placement前追加Base slot sample。
- [ ] 13.9 禁止追加FullBody overlay后的Final Pose。
- [ ] 13.10 禁止追加Foot Placement solver修改后的pose。
- [ ] 13.11 禁止追加VisualRoot world correction。
- [ ] 13.12 Body ResetSequence变化时清空history。
- [ ] 13.13 Base slot Invalid时记录gap但不伪造sample。
- [ ] 13.14 定义`MotionMatchingQuery`不可变合同。
- [ ] 13.15 在Query保存QueryId、Domain、Profile与Database identity。
- [ ] 13.16 在Query保存Trajectory Source与Envelope。
- [ ] 13.17 按Schema history horizons重采样pose position。
- [ ] 13.18 按Schema history horizons重采样pose velocity。
- [ ] 13.19 在Query保存当前left/right contact protection。
- [ ] 13.20 在Query保存current selection与plan identity。
- [ ] 13.21 history不足时设置Initialization Mode与Feature Mask。
- [ ] 13.22 将Query feature按Artifact normalization降低为dense buffer。
- [ ] 13.23 拒绝任何非有限query feature。

## 14. Candidate Admission与Exact Cost

- [ ] 14.1 创建固定顺序`MotionMatchingCandidateAdmission`。
- [ ] 14.2 校验candidate Database、Rig、Schema与Clip binding identity。
- [ ] 14.3 校验candidate Search Domain。
- [ ] 14.4 在Initialization Mode校验CanInitialize。
- [x] 14.5 在普通Jump校验CanJumpInto。
- [x] 14.6 校验candidate不位于Entry exclusion。
- [x] 14.7 校验candidate不位于Exit exclusion。
- [x] 14.8 校验minimum jump interval。
- [x] 14.9 为Reset与Domain activation实现显式强制search语义。
- [x] 14.10 校验continuation graph能够覆盖Plan horizon。
- [x] 14.11 校验合法Terminal plan语义。
- [x] 14.12 校验left protected contact mask兼容。
- [x] 14.13 校验right protected contact mask兼容。
- [x] 14.14 校验protected foot root-relative position jump阈值。
- [x] 14.15 校验protected foot velocity jump阈值。
- [x] 14.16 为每个reject写入稳定RejectReason。
- [ ] 14.17 按interest保存受关注candidate reject数值。
- [x] 14.18 无admitted candidate时发布typed Invalid。
- [x] 14.19 创建`MotionMatchingExactCostEvaluator`。
- [x] 14.20 实现Trajectory Position dead-zone cost。
- [x] 14.21 实现Trajectory Facing dead-zone cost。
- [x] 14.22 实现Trajectory Velocity cost。
- [x] 14.23 实现Pose Position cost。
- [x] 14.24 实现Pose Velocity cost。
- [x] 14.25 实现合法contact之间的soft cost。
- [x] 14.26 实现Continuation Bias。
- [x] 14.27 实现Jump Cost。
- [x] 14.28 为每项cost保留独立分量。
- [x] 14.29 使用dense compiled weight且禁止字符串feature查找。
- [x] 14.30 对同分candidate使用stable SampleId顺序。

## 15. 精确Top-K Search与Short-Horizon Plan

- [x] 15.1 创建固定容量lower-bound tree traversal实现。
- [x] 15.2 为query计算root node lower bound。
- [x] 15.3 按stable child order推进tree traversal。
- [x] 15.4 用Domain/contact summary提前排除不可能节点。
- [x] 15.5 计算节点可剪枝feature lower bound。
- [x] 15.6 当lower bound高于Top-K阈值时安全剪枝节点。
- [x] 15.7 在叶节点逐sample执行完整admission。
- [x] 15.8 对admitted leaf sample执行完整exact cost。
- [x] 15.9 实现固定容量Top-K有序结构。
- [x] 15.9a 在Build Request preflight拒绝estimated sample count小于Search Policy TopK。
- [x] 15.9b 让Database Compiler把runtime Top-K capacity精确编译为Search Policy TopK并禁止使用`Math.Min`降容量。
- [x] 15.10 保持Top-K按cost与SampleId稳定排序。
- [x] 15.11 记录node visited、node pruned与exact sample count。
- [x] 15.12 禁止按wall-clock deadline提前退出。
- [x] 15.13 创建`MotionMatchingPlanEvaluator`。
- [x] 15.14 从Top-K每个candidate读取continuation graph cursor。
- [x] 15.15 按固定plan sample count推进horizon。
- [x] 15.16 累计trajectory position horizon cost。
- [x] 15.17 累计trajectory facing horizon cost。
- [x] 15.18 累计contact hold/release horizon cost。
- [x] 15.19 累计segment end与continuation cost。
- [x] 15.20 累计速度与yaw变化cost。
- [x] 15.21 计算下一次mandatory search time。
- [x] 15.22 拒绝无法合法完成horizon的plan。
- [x] 15.23 生成`MotionMatchingSelectionPlan`不可变结果。
- [x] 15.24 按TotalCost与stable SampleId选择winner。
- [x] 15.25 在无合法plan时发布typed Invalid。

## 16. Selection Lifecycle与Search Cadence

- [x] 16.1 创建`CharacterMotionMatchingSelectionRuntime`。
- [x] 16.2 保存当前Plan、cursor与Selection Generation。
- [x] 16.3 实现Initialization selection路径。
- [x] 16.4 实现同plan continuation判定。
- [x] 16.5 实现新entry Jump判定。
- [x] 16.6 实现Search Domain activation强制Jump。
- [x] 16.7 实现Reset后新Generation创建。
- [x] 16.8 保证Selection Generation严格递增且不回绕。
- [x] 16.9 保持Program AnimationPlaybackId不因MM jump变化。
- [x] 16.10 定义Presentation delta累计Search Cadence。
- [x] 16.11 在cadence到期时执行正式search。
- [x] 16.12 在mandatory search boundary强制执行search。
- [x] 16.13 在current plan失效时强制执行search。
- [x] 16.14 在ResetSequence变化时强制Initialization search。
- [x] 16.15 在Domain producer release时释放current plan。
- [x] 16.16 在同plan continuation时只推进sample cursor。
- [x] 16.17 在Jump时创建新Pose Selection identity。
- [x] 16.18 Invalid时保留结构化failure而不保留旧合法plan。
- [x] 16.19 将Continue/Jump/Initialize/Invalid原因发布给diagnostics。
- [x] 16.20 在Segment runtime payload保存作者声明的StartTime、EndTime与由二者确定的Duration。
- [x] 16.21 在`MotionMatchingSelectionPlan`保存`EntryVisualAdvanceRate`，从当前sample正式next link与Database Sample Rate计算，并在segment尾使用authored EndTime剩余推进。
- [x] 16.22 定义唯一`MotionMatchingPoseTimePlan`的SampleTime、ContinuousVisualTime、Cycle、VisualTimeScale、Looping与`AnimatorStateSpeed = 0`字段，并以specified guard保证default值无效。
- [x] 16.23 让Selection Runtime按表现delta累计SampleAccumulator，并以`SampleAccumulator * EntryVisualAdvanceRate`推进有效SampleTime。
- [x] 16.24 在同segment sample time回绕时递增Cycle，并实现非loop continuous等于sample time、loop continuous等于sample time加`Cycle * Segment.Duration`。
- [x] 16.25 让VisualTimeScale精确取EntryVisualAdvanceRate，并保持AnimatorStateSpeed固定0只表达后端手动采样。
- [x] 16.26 在Invalid plan、无Selection continuation、Reset与Release Domain路径同步清理SampleAccumulator、Loop Cycle与旧PoseTime decision状态。
- [x] 16.27 在MM Pose Source diagnostics记录Clip sample time、ContinuousVisualTime、Cycle与VisualTimeScale。
- [x] 16.28 在exact replay digest记录EntryVisualAdvanceRate，使有效视觉时间推进参与确定性比较。

## 17. Source-Neutral Request与Blend Stack接入

- [x] 17.1 定义公共`AnimationPoseSourceId`完整身份，由AnimationPlaybackId、AnimationPoseSourceKind与AnimationPoseSelectionGeneration共同组成。
- [x] 17.2 让公共request严格要求非零`SourcePoseContinuityIdentity`，并让MM Pose Source output将其精确映射为有效`MotionMatchingSelectionGeneration.Value`。
- [x] 17.3 定义source-neutral `ClipSamplePlan`。
- [x] 17.4 让Timeline adapter降低为同一ClipSamplePlan。
- [x] 17.5 创建`MotionMatchingPoseSourceRuntime`。
- [x] 17.6 从Selection Plan解析Database Clip Binding。
- [x] 17.7 从Plan cursor解析精确SampleTime。
- [x] 17.8 从selected sample解析Pose Parameter。
- [x] 17.9 从selected sample解析Left/Right Foot Feature。
- [x] 17.10 为MM continuation生成同Selection request。
- [x] 17.11 为MM Jump生成新Selection request。
- [x] 17.12 扩展`AnimationBlendEntryId`包含Pose Selection identity。
- [x] 17.13 修改Blend Stack current target比较同时检查Playback与Selection。
- [x] 17.14 保持Timeline同Playback同Selection连续sample不创建新entry。
- [x] 17.15 让MM同Playback新Selection创建新entry。
- [x] 17.16 在Blend Library Compiler生成MM producer self-pair transition。
- [x] 17.17 让MM self-pair exact lookup使用现有CrossFade/Inertial规则。
- [x] 17.18 禁止MM request携带私有blend duration、curve或weight。
- [x] 17.19 扩展Animancer source backend按ClipSamplePlan创建source。
- [x] 17.20 让MM source state使用精确sample time且Speed保持零。
- [x] 17.21 禁止MM source加入Timeline Marker Sync relation。
- [x] 17.22 保持source capture进入现有AnimationSlotBlendPoseEvaluator。
- [x] 17.23 保持Stored Pose、Inertial与retirement由Slot Stack拥有。
- [x] 17.24 确保MM source不创建独立PlayableGraph或crossfade。
- [x] 17.25 让MM Pose Source只从Selection PoseTime生成ClipTime、NormalizedTime、ContinuousVisualTime、Cycle、IsLooping与VisualTimeScale。
- [x] 17.26 让MM Pose Source output的`SourcePoseContinuityIdentity`唯一等于当前Selection Generation Value，不使用sample、时间、request sequence或独立allocator。
- [x] 17.27 将`MotionMatchingDiagnosticsContracts`从`ClipSamplePlan.SampleTime`迁移到正式`ClipTime`。
- [x] 17.28 删除`MotionMatchingClipSamplePlan.SampleTime`旧alias且不保留兼容入口。

## 18. Animation Root与Runtime帧序接线

- [x] 18.1 在MM Database明确区分root trajectory feature与runtime pose root。
- [x] 18.2 让MM source采样使用编译后的root-lock policy。
- [x] 18.3 禁止读取`Animator.deltaPosition`修改Body。
- [x] 18.4 禁止读取`Animator.deltaRotation`修改Body。
- [x] 18.5 保持CharacterBodyPresentationRuntime唯一写VisualRoot world pose。
- [x] 18.6 在Simulation Presentation Runtime构造MM各模块。
- [x] 18.7 在Body Present后优先处理MM ResetSequence。
- [x] 18.8 在Slot frame plan前构造Trajectory Envelope。
- [x] 18.9 在Slot frame plan前构造MM Query。
- [x] 18.10 在需要时执行Search与Plan Rerank。
- [x] 18.11 在其它Pose Source解析前完成MM Selection lowering。
- [x] 18.12 同帧构建全部PoseSlot Stack plan。
- [x] 18.13 同帧采样全部Animancer source。
- [x] 18.14 同帧求值全部PoseSlotFrame。
- [x] 18.15 同帧只求值一次Pose Graph。
- [x] 18.16 Pose Graph完成后追加Base MM Pose History。
- [x] 18.17 History追加后执行唯一Foot Placement。
- [x] 18.18 Foot Placement后推进Camera。
- [x] 18.19 更新Reset顺序清理MM、Blend、Pose Graph与Foot Placement历史。
- [x] 18.20 更新Dispose顺序完成jobs并释放MM workspace。
- [x] 18.21 拒绝同一PresentationFrame重复search、advance或history append。

## 19. Network Model与分支替换适配

- [x] 19.1 保持MM状态不进入Float32 CharacterSimulationState。
- [x] 19.2 保持MM状态不进入Fixed CharacterSimulationState。
- [x] 19.3 保持MM状态不进入WorldSimulationState。
- [x] 19.4 保持MM状态不进入Snapshot与StateHash。
- [x] 19.5 保持MM状态不进入canonical input与network protocol。
- [x] 19.6 让Local Float32使用Accepted Intent source。
- [x] 19.7 让Local Fixed使用Accepted Intent source。
- [x] 19.8 让ServerAuthoritative Prediction owner使用Accepted Intent source。
- [x] 19.9 让ServerAuthoritative Observed Actor使用Selected Body source。
- [x] 19.10 让Deterministic Rollback owner使用当前分支Intent source。
- [ ] 19.11 让Rollback remote使用当前selected/relayed Body source。
- [x] 19.12 Committed branch replacement时清理MM history与plan。
- [x] 19.13 Selected stream reset时清理MM history与plan。
- [x] 19.14 EventId Replace/Retire影响MM producer时清理selection lifecycle。
- [x] 19.15 Projection replacement时释放旧Database Runtime并重新Initialization。
- [x] 19.16 保证Remote MM只读取当前selected interval而非最新packet。
- [x] 19.17 禁止Network Model程序集拥有MM search、cost或database副本。
- [x] 19.18 保证不同render cadence不影响Gameplay output。

## 20. Foot Feature、Action覆盖与Preview

- [x] 20.1 将MM selected sample Foot Feature写入Resolved Pose Request。
- [x] 20.2 让BaseLocomotionSlot live contribution传播MM Left Foot feature。
- [x] 20.3 让BaseLocomotionSlot live contribution传播MM Right Foot feature。
- [x] 20.4 复用Stored Pose Foot Feature aggregate。
- [x] 20.5 复用Inertial Foot Feature transition。
- [x] 20.6 让Pose Graph按最终per-foot contribution输出MM feature。
- [x] 20.7 让Foot Placement只消费FinalAnimationPoseFrame输入。
- [x] 20.8 禁止Foot Placement读取MM Database、Query、Cost与Selection。
- [x] 20.9 禁止MM读取Foot Placement Locked/Sliding/anchor与IK结果。
- [x] 20.10 FullBodyAction覆盖期间保持Base MM Search Cadence。
- [x] 20.11 FullBodyAction覆盖期间保持Base MM source sampling。
- [x] 20.12 FullBodyAction覆盖期间持续更新Base Pose History。
- [x] 20.13 Action退出时直接显露当前Base slot结果。
- [x] 20.14 创建Motion Matching Database Inspector。
- [ ] 20.15 创建显式Query Fixture Editor输入。
- [ ] 20.16 让Database Inspector加载Search Replay Artifact。
- [ ] 20.17 Preview复用正式Runtime Database、Search与Plan实现。
- [ ] 20.18 Preview复用正式Pose Source、Blend Stack与Pose Graph。
- [x] 20.19 禁止Timeline Preview为MM伪造Body、Intent或query。
- [x] 20.20 禁止MM Preview执行Program、WorldSolver、Foot Physics或Camera。

## 21. Diagnostics、Coverage与Search Replay

- [x] 21.1 定义Motion Matching diagnostics interest位。
- [x] 21.2 定义Query summary trace payload。
- [x] 21.3 定义Trajectory Envelope trace payload。
- [x] 21.4 定义Pose History availability trace payload。
- [x] 21.5 定义Admission stage count trace payload。
- [x] 21.6 定义RejectReason aggregate trace payload。
- [x] 21.7 定义受关注candidate reject detail payload。
- [x] 21.8 定义Search node visit/prune trace payload。
- [x] 21.9 定义Top-K exact cost component payload。
- [x] 21.10 定义Plan horizon cost component payload。
- [x] 21.11 定义Selection continue/jump/generation payload。
- [x] 21.12 定义MM source到Blend Entry关联payload。
- [x] 21.13 定义Reset与Initialization原因payload。
- [ ] 21.14 将全部payload接入RuntimeDebugSession。
- [x] 21.15 interest关闭时禁止构造candidate detail集合。
- [x] 21.16 在Database Inspector显示Coverage Summary。
- [x] 21.17 显示每Domain、velocity、facing与contact coverage。
- [x] 21.18 显示unreachable、horizon不足与空Initialization区域。
- [x] 21.19 显示最大candidate上界、tree depth与runtime容量。
- [x] 21.20 定义`MotionMatchingSearchReplayArtifact`schema。
- [x] 21.21 在Replay保存Database Artifact与Projection exact identity。
- [x] 21.22 在Replay保存Query、current plan与Search Policy payload。
- [x] 21.23 在Replay保存candidate/reject/cost/selection digest。
- [ ] 21.24 增加RuntimeDebugSession显式Capture Replay入口。
- [ ] 21.25 增加Editor Replay执行入口。
- [x] 21.26 Replay调用正式Admission、Search与Plan实现。
- [x] 21.27 Replay比较expected与actual digest并输出结构化差异。
- [x] 21.28 identity不匹配时拒绝Replay且不迁移旧capture。

## 22. 条件式能力与独立验证配置边界

- [ ] 22.1 定义Profile没有MM producer时不得引用MM Profile的validation code。
- [ ] 22.2 定义Profile引用MM Profile但没有MM producer时的typed orphan-profile diagnostic。
- [ ] 22.3 定义producer声明MM Pose Source但缺少MM Profile时的typed missing-profile diagnostic。
- [ ] 22.4 定义多个MM Profile owner被解析时的typed duplicate-owner diagnostic。
- [x] 22.5 让Projection Compiler在没有MM producer时完全省略MM payload。
- [x] 22.6 让Presentation Runtime Factory在Projection没有MM payload时不构造MM模块。
- [x] 22.7 让无MM配置不分配query、candidate、Top-K、plan、history与replay buffer。
- [x] 22.8 让无MM配置不发布伪MM runtime snapshot或capability状态。
- [ ] 22.9 定义独立验证配置必须提供的Definition、Presentation Profile与Graph producer identity。
- [ ] 22.10 定义独立验证配置必须提供的Pose Graph、Blend Library、Channel与Slot binding。
- [ ] 22.11 定义独立验证配置必须提供的Rig Definition与稳定BoneId闭包。
- [ ] 22.12 定义独立验证配置必须提供的Foot Analysis Source与Artifact identity。
- [ ] 22.13 定义独立验证配置必须提供的动画Clip、Segment与continuation闭包。
- [ ] 22.13a 定义独立验证配置必须提供的Motion Source Set与SourceClip identity闭包。
- [ ] 22.13b 定义独立验证配置必须提供的Sampling Compatibility Mode与Motion Root。
- [ ] 22.13c 定义独立验证配置必须提供的Coverage Requirement。
- [ ] 22.14 定义独立验证配置必须提供的MM Profile、Schema、Trajectory/Cost/Search Policy与Database。
- [ ] 22.15 定义独立验证配置必须提供的MM producer self jump transition。
- [ ] 22.16 让验证环境通过现有正式入口显式选择目标`CharacterPipelineDefinition`。
- [ ] 22.17 禁止按场景名、角色名、asset目录或资源缺失自动选择验证Definition。
- [ ] 22.18 让MM Analysis Build只枚举显式请求Definition引用的Database。
- [x] 22.19 让Character Build只消费显式请求Definition引用的合法`.mmdb`。
- [ ] 22.20 让Projection payload保存验证Definition、Profile、Rig、Foot Artifact、Database与Clip exact identity。
- [ ] 22.21 让验证配置缺少任一正式输入时Build直接返回typed diagnostic。
- [x] 22.22 禁止为验证配置注入默认Rig、默认Schema、默认Database、placeholder Clip或bind pose。
- [x] 22.23 禁止创建验证专用Runtime、第二套Profile类型、旧/MM开关或兼容converter。
- [x] 22.24 让不同Definition切换时按Projection replacement规则释放旧MM状态并重新构造正式模块。
- [ ] 22.25 让Diagnostics明确显示当前Definition、Profile、Database、Artifact与Projection identity。
- [ ] 22.26 记录用户另行提供正式验证配置时所需的最小输入清单与显式选择入口。
- [x] 22.27 保持Corin Graph、StateMachine、Timeline、Profile、Definition、Projection、Prefab、动画、Marker与transition资产不进入本change写入范围。
- [x] 22.28 禁止独立验证配置引用Corin Rig、Foot Analysis、Clip、Database或其它动画资产作为占位。

## 23. 清理、工具口径与项目文档

- [x] 23.1 删除任何为MM新增的Timeline producer复制路径。
- [x] 23.2 删除任何为MM新增的Marker Sync、Motion Warp或状态名推断路径。
- [x] 23.3 删除仅按PlaybackId判断MM continuation的旧比较逻辑。
- [x] 23.4 删除任何MM私有fade、临时PlayableGraph或direct Animancer Play入口。
- [x] 23.5 删除Runtime读取MM authoring asset的入口。
- [x] 23.6 删除自动MM Analysis build回调与隐式stale修复。
- [x] 23.7 删除Animator root delta到Gameplay Body或VisualRoot的潜在接线。
- [x] 23.8 删除MM查询InputAction、Scene Transform与Network packet的潜在接线。
- [ ] 23.9 让诊断宿主只在显式选择的Projection包含MM payload时读取正式MM runtime snapshot。
- [ ] 23.10 让诊断宿主区分“项目具备MM能力”与“当前Definition未启用MM”。
- [x] 23.11 保持Motion Warp、Marker Sync、Foot Placement与MM诊断名称互不混淆。
- [ ] 23.12 更新Animation Presentation Profile Inspector帮助文本。
- [x] 23.13 更新Database Inspector的显式重操作提示。
- [x] 23.13a 更新Source Set Inspector的“登记不构建”提示。
- [x] 23.13b 记录FBX重导入只产生Stale状态且必须由作者主动Build。
- [ ] 23.14 更新`openspec/project.md`的动画表现正式链路。
- [ ] 23.15 更新`openspec/project.md`的MM Artifact、Projection与Runtime职责。
- [ ] 23.16 更新`openspec/project.md`的Local/Prediction/Observed trajectory source边界。
- [ ] 23.17 更新`openspec/project.md`，明确Corin未配置MM且资产链保持现状。
- [ ] 23.18 将current specs中“项目没有Motion Matching”收敛为“能力可选、按producer显式启用”。
- [x] 23.19 保持`character-state-timeline-authoring-loop`及Corin视觉Timeline与Marker合同不被本change修改。
- [x] 23.20 记录最终`Accepted/Selected Trajectory -> Query -> Exact Top-K -> Plan -> Pose Source -> Blend Stack -> Pose Graph -> Foot Placement`链路。
- [x] 23.21 记录UE 5.8比较边界并避免宣称未实现的Warping、Traversal或Interaction能力。
- [x] 23.22 将已落盘的EntryVisualAdvanceRate、PoseTime、continuous cycle与SourcePoseContinuityIdentity合同同步到`design.md`。
- [x] 23.23 将完整AnimationPoseSourceId、source-neutral request字段与时间采样Scenario同步到Presentation spec delta。
- [x] 23.24 对上述design/spec/tasks同步运行change strict validate并保持通过。
