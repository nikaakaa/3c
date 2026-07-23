# Design: 短时序接触约束 Motion Matching 姿势源

## 重新基线

`refactor-animation-selection-pose-graph-boundary`覆盖本文原先的`ResolvedAnimationPoseRequest -> fixed PoseSlot Blend Stack`接线。Trajectory、Database、Pose History、Query、Admission、Exact Search、Plan、Selection Lifecycle与Replay设计继续有效；公开输出改为`AnimationSelectionFrame`和表现参数。Selection进入`MotionMatchingSelectionInput`后，由Pose Graph显式节点决定直接Player硬切、局部Inertialization连续化或Blend Stack多source CrossFade。

本文后续出现的PoseSlotFrame、BaseLocomotionSlot、per-slot Blend Library与ResolvedAnimationPoseRequest只描述迁移前基线，不再是当前接口。Pose History已经读取与MM节点绑定的已完成Pose Value，不读取固定Slot身份。

## Context

迁移前的职责链是：

```text
Gameplay Program
  -> AnimationChannel producer ownership
  -> Presentation Playback Lifecycle
  -> AnimationSelectionFrame
  -> MotionMatchingSelectionInput
  -> explicit Player / local Inertialization or BlendStack
  -> Character Presentation Pose Graph Plan
  -> FinalAnimationPoseFrame
  -> Foot Placement
```

Motion Matching必须位于Animation Selection阶段。它回答“BaseLocomotion下一段从数据库采样哪个姿势”，不回答“角色能否移动”“实际移动到哪里”“Action是否覆盖Locomotion”“Pose Graph怎样合成”或“脚如何贴合当前世界表面”。

UE 5.8公开基线已经不只是线性最近邻：Pose Search提供Pose/Trajectory channel、Pose History、Continuing Pose、Brute Force、PCAKDTree、实验VPTree、Normalization Set、Chooser过滤和debug；Game Animation Sample使用capsule-driven movement、GenerateTrajectory、IsStarting/IsPivoting等状态与Chooser；实验Interaction API还支持多角色候选。参考：

- [Motion Matching in Unreal Engine](https://dev.epicgames.com/documentation/en-us/unreal-engine/motion-matching-in-unreal-engine?lang=en-US)
- [Game Animation Sample](https://dev.epicgames.com/documentation/en-us/unreal-engine/game-animation-sample-project-in-unreal-engine)
- [Motion Match Multi](https://dev.epicgames.com/documentation/en-us/unreal-engine/BlueprintAPI/Animation/PoseSearch/MotionMatchMulti?lang=en-US)

因此本设计不以“也有KDTree”作为领先点，而以当前3C业务真正需要的完整闭环为目标：编译身份、轨迹不确定性、硬接触约束、短时序计划、网络分支重置和可复现解释。

这不是从零发明Motion Matching，也不把UE类名逐个翻译。Schema、Database、Pose History、Trajectory Query、Continuing Pose、feature normalization、不可跳入区间和调试视图属于成熟Motion Matching共同基础，本设计明确参考UE公开工作流。项目不照搬的是UE的Animation Blueprint Motion Matching Node、Chooser、内置Blend Tree、Derived Data自动索引与root-motion gameplay组织；这些职责必须进入本项目已有Program producer、target-neutral Projection、Animancer source backend、显式Player/局部Inertialization/Blend Stack、Pose Graph、Body Presentation和显式Artifact链。

## Goals

- 提供可由独立正式角色配置显式启用的Grounded Locomotion Pose Source，不改动未启用MM的角色。
- 保持Gameplay Body、WorldSolver和Program为真实移动权威。
- 让Local accepted intent与Remote selected body使用同一query模型，并显式表达置信度差异。
- 使用上一帧与MM Selection Input绑定的Player完成Pose建立pose query，不被FullBody Action或Foot IK污染。
- 先执行可解释硬准入，再执行结果可证明精确的Top-K搜索。
- 在Top-K上评估固定短时序continuation plan，而不是只比较单个当前sample。
- 把左右脚受保护接触作为候选资格，不允许普通代价权重掩盖脚步断裂。
- 将MM selection降低为统一Animation Selection并复用唯一编译Pose Plan与显式Player节点集。
- 让查询、候选、拒绝、代价、计划、选择、混合和重置完整可诊断、可捕获、可重放。
- 全部Runtime内存按Projection容量预分配，不在表现帧扩容。

## Non-Goals

- 不用MM控制Gameplay移动、碰撞、动作准入、窗口、伤害或网络裁决。
- 不实现Airborne、Traversal、Attack、Dodge、Hit Reaction或多人Interaction数据库。
- 不实现Stride/Orientation/Slope Warping或Root Offset Bone。
- 不用Foot Placement world anchor或IK结果做搜索输入。
- 不提供Brute Force/PCAKDTree/VPTree运行模式下拉；项目只安装一种正式搜索语义。
- 不让Runtime读Authoring asset、AssetDatabase或Library artifact。
- 不建立自动分析、运行时数据库构建或stale fallback。

## Overtake Axes

| 轴 | UE 5.8公开常见路径 | 本设计 |
|---|---|---|
| 高层过滤 | AnimBlueprint状态与Chooser选择数据库 | Program producer到Search Domain的编译绑定；Query不读取状态名 |
| 轨迹 | 单条当前/未来预测轨迹 | 每horizon位置、面对、容许半径与置信度组成Trajectory Envelope |
| 候选合法性 | filter、notify、cost bias组合 | Domain、初始化、片段边界、continuation、jump interval和protected contact先做硬准入 |
| 搜索 | Brute Force或近似索引后rerank | 下界剪枝保持精确Top-K，stable SampleId打破同分，无时间预算提前退出 |
| 决策视野 | 当前pose与其future feature | Top-K之后沿显式continuation graph评估固定短时序plan |
| 网络纠偏 | 通用AnimInstance/Pose History更新 | Body ResetSequence、branch replacement和EventId replacement原子重置history/plan |
| 可解释性 | debugger和trace | 每个reject/cost/plan分项、exact identity和显式Search Replay Artifact |
| 产物闭包 | Engine资产与Derived Data | Schema/Rig/Foot Artifact/Database/Projection exact identity，stale直接失败 |
| 导入与索引 | Content Browser中向Pose Search Database加入动画，由Engine资产与Derived Data链管理索引 | Source Set显式登记GUID/local file id；Foot与MM重分析各有明确按钮、进度与取消，导入和selection只产生status |

这张表只比较Grounded Locomotion选择闭环。UE的动画资源生态、Warping、Traversal和实验Interaction不属于本设计覆盖范围。

## Target Architecture

```text
CharacterAnimationPresentationProfile
  -> [when an MM producer is declared] CharacterMotionMatchingProfile
       -> FeatureSchema
       -> TrajectoryPolicy
       -> CostProfile
       -> SearchPolicy
       -> DatabaseDefinitions[]
       -> ProducerBindings[]

Explicit Motion Matching Analysis Build
  -> validated Clip/Range/Rig/Foot Analysis inputs
  -> root/pose/contact features
  -> normalization
  -> continuation graph
  -> exact lower-bound search index
  -> coverage report
  -> Library/.../<database-guid>.mmdb

Explicit Character Build
  -> Semantic Contract
  -> Presentation authoring
  -> exact MM artifacts
  -> target-neutral CharacterPresentationProjection

PresentationFrame
  -> Body Presentation Frame
  -> Trajectory Source
  -> Trajectory Envelope
  -> previous Base Pose History
  -> Constraint Admission
  -> Exact Top-K Search
  -> Short-Horizon Plan Rerank
  -> MotionMatchingSelection
  -> MotionMatchingPoseSourceRuntime
  -> ResolvedAnimationPoseRequest
  -> BaseLocomotionSlot Blend Stack
  -> Pose Graph
  -> append current Base Pose History
  -> Foot Placement
  -> Camera
```

## Current Implementation Baseline

截至2026-07-22，设计已经从“待实现目标”进入“主链已接通、跨模块闭环待收口”阶段。工作区里的正式调用链是：

```text
CharacterAnimationPresentationProfile
  -> MotionMatchingProjectionPayloadCompiler
  -> CharacterPresentationProjection.MotionMatching
  -> CharacterPresentationRuntimeFactory
       -> CharacterAnimationPlaybackRuntime
            -> CharacterMotionMatchingPresentationModule
                 -> internal Accepted Intent or Selected Body Adapter
  -> CharacterSimulationPresentationRuntime.Present
       -> Body Presentation Frame / ResetSequence
       -> CharacterAnimationPlaybackRuntime.Present
            -> CharacterMotionMatchingPresentationModule.ResolveFrame
              -> CharacterMotionMatchingProducerRuntime.Resolve
                 -> CharacterMotionMatchingTrajectoryRuntime
                 -> MotionMatchingQueryBuilder
                 -> MotionMatchingCandidateAdmission
                 -> MotionMatchingExactSearch
                 -> MotionMatchingPlanEvaluator
                 -> CharacterMotionMatchingSelectionRuntime
                 -> MotionMatchingPoseSourceRuntime
              -> MotionMatchingResolvedPoseRequestFactory
            -> AnimationBlendStackRuntime
            -> native Pose Slot evaluation
            -> native Pose Graph evaluation
            -> CharacterMotionMatchingPresentationModule.CompleteFrame
                 -> append Base Pose History
       -> Foot Placement
       -> Camera
```

当前已落盘的职责：

| 边界 | 当前实现 | 仍缺的闭环 |
|---|---|---|
| Authoring与离线分析 | Profile、Source Set、Database、显式Foot批处理、显式MM Build、Artifact、Coverage与Projection payload均已存在 | 用户尚未提供独立正式验证配置及其完整内容identity |
| Trajectory | Accepted Intent与Selected Body已收敛为MM Module内部Adapter；Factory、Simulation Presentation与Playback不再识别具体类型；Envelope、真实selected sample age和Reset已存在 | Rollback remote只消费最终selected Body transaction，仍等待独立验证配置 |
| Query与选择 | Pose History、Query、硬准入、Exact Top-K、Plan Rerank、Selection Generation与Pose Time均已存在；受关注reject保存实际值与阈值 | 无代码闭环缺口 |
| 公共动画链 | MM已降低为正式Animation Selection，复用编译Pose Plan、显式SelectedPosePlayer、可选局部Inertialization或BlendStack与FootPlacement | 仍需完成独立正式验证配置，current specs才能安装可选MM能力 |
| Diagnostics与Replay | Snapshot payload、显式Capture、exact codec、Database Inspector Replay均已进入正式链；Query Fixture可选择显式producer与场景Preview Target，并执行同一MM Module、Pose Source与编译Pose Plan | 仍需完成独立正式验证配置 |
| 条件式能力 | 无MM payload时不构造MM Runtime，也不分配查询workspace；orphan/missing/duplicate-owner使用typed diagnostic；Inspector区分项目能力与当前Definition启用状态 | 独立正式验证Definition的内容装配与显式环境选择仍等待用户输入 |

这份基线只说明工作区实现事实，不把active change提前升级为current architecture。Corin没有MM producer、MM Profile、Database或验证资产，仍沿现有Timeline producer运行。

## Responsibility Boundary

| 模块 | 输入 | 输出 | 明确不拥有 |
|---|---|---|---|
| Program | input、state、world result | channel producer lifecycle、accepted motion | clip、sample、query、pose history |
| MM表现Module内部Trajectory Adapter | committed/selected body、accepted intent | source frame | channel winner、Stack transition、最终pose |
| MM表现Module | 正式Body/Intent、MM playback demand、Stack retention | resolved pose request、frame completion、pose history | channel winner、Stack entry/clock、Pose Graph |
| Trajectory Runtime | source frame、policy | trajectory envelope | Program input、World request修改 |
| Pose History | BaseLocomotionSlot frame | bounded pose samples | FullBody/IK/VisualRoot真相 |
| Admission | query、domain、candidate metadata | admitted candidate set与reject | soft cost |
| Exact Search | query、admitted set、index | exact Top-K | Blend、Gameplay状态 |
| Plan Rerank | Top-K、continuation graph、horizon | selection plan | 私有crossfade |
| MM Pose Source | selection、Projection clip binding | resolved pose request | 搜索代价、Body位移 |
| Blend Stack | resolved pose request | PoseSlotFrame | 搜索、跨slot组合 |
| Pose Graph | 全部slot frame | final pose frame | source选择、Foot IK |
| Foot Placement | final pose、body、world query | IK plan | 动画选择、Gameplay contact |

## Authoring Model

### CharacterMotionMatchingProfile

当且仅当`CharacterAnimationPresentationProfile`声明至少一个Motion Matching producer binding时，它必须只引用一个`CharacterMotionMatchingProfile`。未声明MM producer时不得引用该Profile，Projection不得包含MM payload，Presentation Runtime也不得构造MM模块。该Profile是MM表现配置装配根：

```text
MotionMatchingProfileId
Revision
FeatureSchema
TrajectoryPolicy
CostProfile
SearchPolicy
DatabaseDefinitions[]
ProducerBindings[]
```

Profile不内联Clip列表，不保存Graph State、Action、InputAction、Layer、Pose Graph节点或Blend曲线。Runtime不读取Profile对象。

### CharacterMotionMatchingSourceSet

Source Set是后续导入动画进入MM的唯一登记边界，负责“这段动画是什么资源、能否在目标Rig上被正式采样和播放”，不负责Search Domain、Segment资格、Cost或Runtime选择：

```text
MotionSourceSetId
Revision
TargetRigId
SamplingCompatibilityMode
MotionRootBoneId
SourceClips[]

SourceClip
  SourceClipId
  AnimationClipAssetGuid
  AnimationClipLocalFileId
```

Source Set保存作者选择的稳定资源identity，不保存构建时计算的dependency hash副本；Builder按GUID/local file id精确解析Clip并把当前import dependency、Avatar或Generic hierarchy signature写入Artifact identity。FBX内嵌多个Clip依靠local file id区分，重命名、列表重排与目录移动不得改变SourceClipId。

`SamplingCompatibilityMode`只有两种显式合同：

- `HumanoidRetargeted`：源Clip必须由有效Humanoid Avatar导入，目标Sampling Rig也必须是有效Humanoid；Builder必须把Clip放到目标Sampling Rig上采样，不能读取源骨架局部曲线假装成目标骨架结果。
- `ExactGenericRig`：源Clip的root node与完整所需骨骼hierarchy signature必须和目标Rig精确匹配；Generic不提供按骨骼名或近似层级retarget。

一个Source Set只能选择一种模式。Database可以引用多个Source Set，但它们必须降低到同一Target Rig identity；混合不兼容模式或Rig时Build失败。Motion Root只用于Editor提取trajectory feature，Runtime播放继续root lock，不能把clip root delta应用到Gameplay Body或VisualRoot。

### Feature Schema

Schema显式保存：

- 匹配Rig identity。
- root trajectory过去/当前/未来sample horizons。
- 每个horizon启用的位置、面对、速度与角速度feature。
- 参与pose的稳定BoneId及位置/速度通道。
- 左右脚contact feature来源。
- Initialization Query启用的feature mask。
- 每个feature group的规范化与cost binding。

horizon必须严格递增、有限且包含零时刻。BoneId不得重复或依赖名称扫描。

### Database Definition

每个Definition保存：

```text
DatabaseId
Revision
RigId
FeatureSchemaId
SearchDomainId
SourceSetIds[]
Segments[]
CoverageRequirements[]
```

每个Segment保存：

```text
SegmentId
SourceClipId
StartTime
EndTime
LoopMode
CanInitialize
CanJumpInto
EntryExclusion
ExitExclusion
ContinuationTargetSegmentId or Terminal
```

Loop只能显式回到本Segment起点。有限Segment需要显式Continuation target或Terminal；Runtime不按相似度自动生成业务continuation。Compiler可以生成sample级邻接表，但只能从作者声明的segment拓扑降低。

Coverage Requirement不是Clip角色标签，而是该Search Domain必须服务的业务输入范围。每项显式声明速度区间、面对变化区间、是否要求Initialization、允许的左右脚接触组合和最短plan horizon。Builder从真实采样feature证明覆盖；Clip文件名中的Idle、Start、Run、Pivot或Stop不能作为证明。

### Search Domain Binding

Producer Binding把稳定Program producer identity绑定到一个Search Domain和一个Database集合。独立验证配置可以安装：

```text
Validation.BaseLocomotion.MotionMatching
  -> Validation.GroundedLocomotion
  -> ValidationGroundedLocomotionDatabase
```

这里的identity只是展示正式绑定形状，不要求仓库内置同名资产。用户提供的验证配置必须使用自己的Definition、Profile、Rig、Foot Analysis、Clip、Database和producer identity。Graph仍决定producer是否有效。Search Runtime不读取Idle/Start/Pivot状态，也不运行Chooser或按速度切换数据库。多个未来姿态类别必须由不同正式producer/domain表达，不能靠数据库缺失时fallback。

## Imported Asset Intake

```text
导入FBX/.anim
  -> Unity完成普通资源导入
  -> 作者显式创建或编辑Motion Source Set
  -> 作者通过Object Picker或拖放登记Clip
  -> 轻量结构校验显示Rig/Avatar/Root/identity状态
  -> 作者在Database Definition显式建立Segment与Coverage Requirement
  -> 作者点击Build Source Set Foot Analysis显式构建缺失或Stale的单Clip artifact
  -> 作者点击Build Motion Matching Database
  -> 重采样、Foot Artifact解析、索引、Coverage证明、.mmdb原子发布
  -> 作者另行执行Character Build发布Projection
```

导入、重导入、选择Project资源、把Clip拖入Source Set、Inspector repaint、`OnValidate`、domain reload和进入Play Mode都不得触发Foot Analysis、MM采样、索引构建或Character Build。轻量结构校验只允许解析GUID/local file id、Importer声明、Avatar有效性和已保存identity；凡是需要实例化Sampling Rig、遍历animation sample或生成Artifact的工作都属于显式Build。

Source Set登记不会按目录批量发现Clip，也不会按资源名推断Idle/Start/Loop/Pivot/Stop。可以提供“显式加入当前选择Clip”的编辑命令，但点击该命令只修改Source Set authoring，不执行分析。

首版不把in-place动画转换成root-motion动画。静止Segment可以拥有接近零的root trajectory；一旦Coverage Requirement声明非零速度或转向范围，Builder必须从目标Sampling Rig上的真实Motion Root采样证明覆盖。证明失败时报告缺失范围，不能积分脚速度、读取Gameplay期望速度或生成隐藏trajectory曲线补足。

## Explicit Analysis Build

### Trigger

重分析只允许来自：

- Motion Matching Profile Inspector的显式Build Database。
- Product Build只消费既有合法Artifact；不得隐式分析。

Asset import、domain reload、Inspector repaint、selection change、Play Mode进入/退出和普通Character Compile均不得触发MM分析。

Source Set Inspector提供独立的`Build Source Set Foot Analysis`重操作。它先显示Analysis Source、Sampling Rig、Clip总数、Ready/Missing/Stale数量和预计sample数量；作者确认后按稳定SourceClipId顺序逐Clip调用现有唯一`AnimationFootAnalysisArtifactBuilder`，不得实现第二个Foot Analyzer。Clip之间通过Editor update让出控制权，显示进度并允许Cancel；每个完成的Foot Artifact继续使用现有单Clip原子发布语义。

点击`Build Motion Matching Database`后先显示不可变Build Request摘要：Database identity、Source Set、Clip数量、总sample数量、缺失或Stale Foot Artifact数量和预计内存上界。存在无效Foot Artifact时Preflight直接失败并指向`Build Source Set Foot Analysis`，不替作者隐式生成。

确认后的`MotionMatchingDatabaseBuildJob`按固定sample数量切片，由Editor update逐片推进目标Rig采样；纯数组Normalization、Index和Coverage阶段也按固定工作单元推进。切片方式只能影响Editor响应与进度显示，不能改变sample顺序、浮点运算顺序、Artifact bytes或搜索结果。Build显示当前stage、完成数、总数和Cancel；取消、domain reload、输入dependency变化或异常只删除候选文件并释放隐藏Sampling Rig，旧完整`.mmdb`保持不变且不会被标记为当前Ready。

### Inputs

Builder精确接收：

- Database Definition identity/content。
- Source Set identity/content、SourceClipId、AnimationClip GUID/local file id与当前import dependency。
- Feature Schema identity/content。
- Rig Definition identity/content。
- Sampling compatibility mode、Avatar或Generic hierarchy signature、Motion Root与segment范围。
- 匹配的Animation Foot Analysis Artifact identity/content hash。
- Analysis algorithm version。

缺失任何输入时失败，不搜索引用者或使用默认Rig/Foot source。

### Feature Extraction

每个sample使用固定Database sample rate，生成：

- root局部平移、面对、线速度和yaw角速度。
- 各trajectory horizon相对当前root的位置和面对。
- 稳定BoneId在root-relative空间的位置与速度。
- 左右脚plant、landing、height、sole speed等正式Foot Analysis feature。
- segment边界、entry/exit资格和continuation sample。

Root Motion只在Editor提取。Runtime pose source必须禁用root位移对GameObject/Body的应用。

### Robust Normalization

每个feature channel编译中位数和稳健尺度。零尺度constant channel从distance中显式标记为不参与，不通过任意epsilon制造区分。非有限sample、异常root discontinuity、Bone缺失或Foot artifact错配直接失败。

Cost Profile编译为与dense feature layout严格同长的权重表。Runtime不按feature名称查字典。

### Continuation Graph

Compiler从Segment loop/continuation声明建立sample级有向图：

- 普通sample指向下一sample。
- Loop末端指向同Segment首个合法sample。
- Finite末端指向显式Continuation target的合法入口。
- Terminal没有下一节点。

图必须无悬空identity；计划horizon内无法继续且又不是合法Terminal计划的候选会被admission拒绝。

### Exact Lower-Bound Index

Artifact以规范化feature构建稳定平衡层级树。每个节点保存候选SampleId范围、Domain/contact metadata summary和每个可剪枝feature维度的min/max bounds。

Runtime对query计算节点理论最小cost：若lower bound已经高于当前Top-K最差精确cost，则整个节点可以安全剪枝。叶节点逐sample计算完整exact cost。该算法的剪枝不改变Top-K结果；树构建与遍历同分顺序由stable node/sample identity决定。

Search Policy保存固定TopK、leaf capacity、plan horizon、maximum admitted sample count和`CoverageNearDuplicateCostThreshold`。`CoverageNearDuplicateCostThreshold`必须有限且大于0，单位精确等于Runtime最终使用的weighted normalized squared feature cost；Builder与Runtime不得硬编码另一阈值或在字段无效时回退默认值。Builder必须验证每个Domain分区满足容量约束；Runtime不按毫秒预算少算候选。

### Coverage Report

Artifact附带只读coverage summary：

- 每Domain/velocity/facing/contact区间sample数量。
- 没有Initialization入口的区域。
- 过短或无法覆盖plan horizon的segment。
- unreachable segment。
- duplicate/near-duplicate sample密度。
- contact protection可能造成的空候选区域。
- 最大admitted set与索引深度。
- 每项Coverage Requirement的Satisfied或Missing证明及对应sample区间。

数据库级coverage diagnostics与每项Requirement summary共同属于Artifact唯一只读coverage section，不建立第二份summary路径。其规范统计口径如下：

- Continuation reachability以全部`CanInitialize` sample为唯一根集合，以每个sample的正式`NextSampleIndex`和segment显式`ContinuationEntrySampleIndex`为有向边。遍历得到的sample记为reachable；segment只要包含任一reachable sample就记为reachable。Payload同时保存`TotalSampleCount`、reachable/unreachable sample count与`TotalSegmentCount`、reachable/unreachable segment count，并强制两组reachable加unreachable分别精确闭合到对应total。
- Exact duplicate只比较最终compiled、normalized且active的feature vector；只有按dense active feature顺序取得的canonical float bits逐项完全相同才属于同一exact组。`ExactDuplicateSampleCount`统计所有成员数至少为2的exact组所覆盖的sample并集，每个sample只计一次；`ExactDuplicateSampleRatio = ExactDuplicateSampleCount / TotalSampleCount`。
- Near duplicate只统计不属于exact duplicate pair的无序sample pair。每个pair使用Runtime同一份最终weighted normalized squared feature distance，distance小于或等于正式`CoverageNearDuplicateCostThreshold`时计为near duplicate；不得改用raw feature、未加权距离或近似索引距离。`TotalUnorderedNonExactPairCount`精确等于全部sample无序pair数减去exact pair数，`NearDuplicatePairRatio = NearDuplicatePairCount / TotalUnorderedNonExactPairCount`；分母为0时ratio规范为0。
- Protected-contact region identity固定为`CoverageRequirementId + ProtectedContactMask`，其中mask只允许`Left`、`Right`或`Both`。Builder先得到满足该Requirement、但尚未应用protected-foot admission的raw sample set；raw set非空时该region才进入实际评估。随后只应用正式protected-foot admission，过滤结果为0才把该region计入`ProtectedContactEmptyRegionCount`。`ProtectedContactEmptyRegionRatio = ProtectedContactEmptyRegionCount / EvaluatedNonEmptyRawProtectedContactRegionCount`；没有实际评估region时ratio规范为0。
- `MaximumAdmittedCandidateSetUpperBound`的单位是sample count，取上述全部实际评估region经过完整hard admission后的candidate count最大值；没有实际评估region时为0。它必须不大于`TotalSampleCount`和Search Policy的maximum admitted sample count。
- `SearchIndexMaximumDepth`使用root edge-depth等于0的定义；单节点树最大深度为0，每沿一条parent-child边增加1，并且不得超过Search Policy的maximum tree depth。

全部count必须为非负整数，全部ratio必须有限且位于`[0, 1]`，阈值必须有限且大于0；exact/near-duplicate、reachability、protected-contact、capacity与depth字段必须通过上述total和分母关系自洽。它们只是在显式Database Build期间离线计算并写入Artifact的作者诊断，不改变Runtime admission、exact search、plan rerank或最终选择。

Coverage report用于作者决策，不在Runtime补洞或自动生成片段。任一正式Coverage Requirement缺失时本次Build失败，旧Artifact保持原样但不得被标记为当前Ready。

## Artifact And Projection Identity

### Artifact Identity

```text
MotionMatchingDatabaseArtifactIdentity
  SchemaVersion
  AlgorithmVersion
  DatabaseId / Revision
  FeatureSchemaId / Revision
  RigId / Revision
  FootAnalysisArtifactHash
  OrderedClipDependencyHashes
  ContentHash
```

Artifact固定发布到：

```text
Library/CharacterSimulation/Analysis/MotionMatching/<database-guid>.mmdb
```

写入使用临时候选加原子替换。失败不得覆盖上一份合法Artifact，但下一次Character Build仍会因为authoring identity已变化而判定旧Artifact stale。

### Projection Payload

Projection Compiler只读取validated Semantic Contract、Animation Presentation authoring和合法MM Artifact，生成：

```text
MotionMatchingProjectionPayload
  Profile identity
  Feature/Trajectory/Cost/Search policy payload
  Producer -> SearchDomain bindings
  Database payloads
  Clip resource bindings
  Runtime capacities
  Artifact identities
```

Payload属于target-neutral Presentation Projection，不包含Numeric ProgramHash、Float32/Fixed ABI、CharacterState地址或Editor对象。

## Trajectory Source Contract

### Accepted Intent Source

Local与Prediction使用`CharacterPresentationTrajectoryIntent`：

```text
ActorId
PreviousTick / CurrentTick
SourceSequence
DesiredPlanarVelocity
DesiredFacing
AcceptedAcceleration
AcceptedTurnRate
Grounded / MovementMode
ResetSequence
```

它来自已被Program motion与World request链接受的结果，不是原始InputAction。Egress/Commit只在atomic Body结果发布后发布对应intent interval。

### Selected Body Observation Source

Observed/Remote Actor使用Selected Body interval降低的source frame：当前位置、旋转、速度、yaw速度、Grounded、sample age与ResetSequence。它是正式较低置信来源，不冒充本地accepted intent。

### Factory Selection

Presentation Factory为每Actor显式装配唯一`ICharacterMotionMatchingTrajectorySource`。Runtime不能在accepted intent缺失时自动改读Transform，也不能在Remote无数据时改用Local输入。

## Trajectory Envelope

每个horizon生成：

```text
TimeOffset
LocalPositionCenter
PositionToleranceRadius
LocalFacingCenter
FacingToleranceDegrees
Confidence
```

Accepted Intent使用加速度和转向限制积分得到中心；Selected Body使用正式body velocity/yaw velocity外推。容许半径随horizon和source不确定性增大，confidence随不确定性降低。全部曲线和上限来自编译后的Trajectory Policy，没有Network Model if/switch。

轨迹cost在容许半径内不惩罚或使用平滑dead-zone，超出后按confidence加权。这样Remote远期不确定不会强迫选一个极端转弯姿势，本地短期明确意图仍保持高响应。

## Pose History

Pose History只追加与MM Selection Input绑定的Player节点已经完成的Pose Value：

- stable BoneId local pose。
- 基于表现delta的bone velocity。
- 左右脚feature aggregate。
- continuity identity。
- sample presentation time。

它不读取FinalAnimationPoseFrame中的FullBody覆盖，不读取FootPlacement骨骼结果，不读取VisualRoot world correction。查询发生在本帧Player求值前，因此只消费上一帧及更早历史；绑定PoseNode完成后再append，避免循环依赖。

History容量和sample horizons由Schema编译。时间不足时进入Initialization Query，不使用bind pose或隐藏Idle补历史。

## Query Construction

`MotionMatchingQuery`包含：

```text
QueryId
Database/Profile identities
SearchDomainId
TrajectorySourceIdentity
TrajectoryEnvelope
PoseFeatureVector
CurrentFootContactProtection
CurrentSelection/Plan
InitializationMode
ResetSequence
```

Pose feature由history按Schema horizon重采样。当前脚保护来自上一帧Base slot feature aggregate和MM selection contact metadata，不读取Foot Placement world lock。

## Candidate Admission

硬准入顺序固定：

1. Database、Rig、Schema与Clip binding identity合法。
2. SearchDomain匹配当前producer binding。
3. Initialization Mode只接受`CanInitialize`。
4. 普通模式只接受`CanJumpInto`或当前plan continuation。
5. candidate不位于Entry/Exit exclusion。
6. 距离上次jump满足minimum jump interval，除非Reset或Domain强制变化。
7. continuation graph可覆盖计划horizon或candidate为合法Terminal计划。
8. 左右脚protected contact与candidate entry contact兼容。
9. candidate的接触脚root-relative速度和位置跳变量在编译阈值内。

每个reject使用唯一枚举和必要数值记录。任何硬约束不能通过降低cost权重绕开。

## Exact Cost

通过admission的candidate计算：

```text
ExactCost =
  TrajectoryPositionCost
  + TrajectoryFacingCost
  + TrajectoryVelocityCost
  + PosePositionCost
  + PoseVelocityCost
  + ContactSoftCost
  + ContinuationCost
  + JumpCost
```

所有分项保留独立值。Hard contact compatibility已经在admission完成；ContactSoftCost只用于合法候选之间比较接触释放质量。

current continuation candidate必须与其它candidate使用相同exact cost，再加编译后的Continuation Bias。不能无条件保留当前pose，也不能在Domain变化后继续旧plan。

## Short-Horizon Plan Rerank

Exact Search输出稳定Top-K frame candidates。Plan Rerank从每个candidate沿continuation graph推进固定sample数，累计：

- 各horizon candidate root trajectory与query envelope的integrated cost。
- 面对变化与query facing的integrated cost。
- protected contact保持、合法释放与下一次plant的时序成本。
- segment末端、loop或显式continuation质量。
- plan内速度/角速度突变。
- 预计下一次必须search的时间裕量。

输出：

```text
MotionMatchingSelectionPlan
  PlanId
  EntrySampleId
  SegmentId
  EntryTime
  EntryVisualAdvanceRate
  HorizonEndSampleId
  ExactEntryCost
  HorizonCost
  TotalCost
  SelectionGeneration
  ContinueCurrent
  NextMandatorySearchTime
```

计划不是Gameplay Timeline，也不产生Window/Notify/Cue。它只让Pose Source在下一次搜索前连续采样同一segment/continuation。

`EntryVisualAdvanceRate`不是播放器state speed。Plan Evaluator先读取当前sample的正式`NextSampleIndex`，以相邻sample的clip time差乘`Database SampleRate`得到有效视觉时间推进倍率；segment尾没有next时，使用该segment作者声明的`EndTime`与当前sample time的剩余量乘`Database SampleRate`。它不从表现帧时长、sample index差、文件名或默认常量猜测。

### Pose Time Plan

Selection Runtime只产生一种正式`MotionMatchingPoseTimePlan`：

```text
MotionMatchingPoseTimePlan
  SampleTime
  ContinuousVisualTime
  Cycle
  VisualTimeScale
  Looping
  AnimatorStateSpeed = 0
```

`SampleAccumulator`按表现delta累计，并以当前plan的`EntryVisualAdvanceRate`推进有效`SampleTime`。沿同一segment前进时，如果下一sample time小于当前sample time，表示loop回绕，`Cycle`递增；切换segment或Initialize/Jump时重新从cycle 0开始。非loop时`ContinuousVisualTime = SampleTime`且`Cycle = 0`；loop时`ContinuousVisualTime = SampleTime + Cycle * Segment.Duration`，其中`Segment.Duration`精确来自作者声明的`StartTime/EndTime`。

`VisualTimeScale`精确等于`EntryVisualAdvanceRate`，只表达“有效视觉时间相对表现时间的推进倍率”。`AnimatorStateSpeed`固定为0，只表达Animancer后端手动设置采样时间，不会把`VisualTimeScale`改成0。Initialize、Jump、reset或其它source断裂由新的source continuity identity让下游清理首样本速度，禁止用`VisualTimeScale = 0`伪装断裂或暂停。

## Selection Lifecycle

### Initialization

Startup、Body reset、branch replacement、Projection replacement或history不足后进入Initialization。搜索只接受`CanInitialize`，使用Schema的Initialization Feature Mask。选中后创建新的Selection Generation，首个Base slot pose完成后开始积累普通history。

### Continue

当前plan仍合法且本次search选择同一plan continuation时：

- 保持`AnimationPlaybackId`。
- 保持`MotionMatchingSelectionGeneration`。
- 保持`SourcePoseContinuityIdentity`，其值精确等于当前有效`MotionMatchingSelectionGeneration.Value`。
- 更新SampleTime与plan cursor。
- 更新同一Blend Entry，不重启clock。

### Jump

选择不同entry sample或Reset强制重选时：

- 保持Program拥有的MM`AnimationPlaybackId`，只要producer activation未变。
- 提升`MotionMatchingSelectionGeneration`。
- 把`SourcePoseContinuityIdentity`同步为新的有效`MotionMatchingSelectionGeneration.Value`。
- 由`SelectedPosePlayer`发布新的typed PoseDiscontinuity。
- 按显式图连接解析局部Inertialization Policy或BlendStack CrossFade Policy。
- 若显式图选择BlendStack，复用该节点唯一CrossFade、Stored Pose和retirement；若选择局部Inertialization，只复用其history、residual和rebase。

这修复了“同Playback一律视为continuation”无法表达MM jump的合同缺口。

### Invalid

无合法candidate、Clip丢失、plan graph断裂、query非有限或identity错配时发布typed Invalid。RequireOutput slot由统一动画管线报告失败；不得播放旧state、bind pose或全库fallback。

## Animation Selection输出

Timeline和MM都降低为：

```text
AnimationSelectionFrame
  AnimationChannelId
  AnimationPoseSourceId
  SelectionGeneration
  ProgramProducerIndex
  VisualSampleTime
  ContinuousVisualTime
  Cycle
  VisualTimeScale
  ClipSamplePlan[]
  PoseParameters[]
  LeftFootFeatures
  RightFootFeatures
```

统一Selection不携带PoseSlot、transition technique、duration、curve、Bone Mask或最终weight。MM侧已有的playback identity、source kind与selection generation只能在最终公共降低边界组装一次稳定source identity，不能再建立MM私有source key。`SelectionGeneration`精确取当前有效`MotionMatchingSelectionGeneration.Value`，不得取sample index、sample time或独立allocator；因此Continue保持，Initialize/Jump随generation变化。

MM内部`MotionMatchingClipSamplePlan`只持有selection的`MotionMatchingPoseTimePlan`，并按以下唯一映射降低到Selection的source-local clip descriptor：

```text
ClipTime           <- MotionMatchingPoseTimePlan.SampleTime
ContinuousClipTime <- MotionMatchingPoseTimePlan.ContinuousVisualTime
NormalizedTime     <- ClipTime / Clip.length
IsLooping          <- MotionMatchingPoseTimePlan.Looping
```

Clip必须存在、长度有限且大于0，clip time必须在合法范围，loop cycle与continuous time必须一致。Playable state的`Speed`仍固定为0，只用于后端手动采样；Selection的`VisualTimeScale`继续取`EntryVisualAdvanceRate`。

Timeline adapter可以输出多Clip ManualMixer descriptor；MM第一版输出单个selected Clip sample。MM选中Clip的正式Projection曲线在`VisualSampleTime`处写入dense `PoseParameters[]`，其中Foot Placement Weight唯一使用canonical `PoseParameterId` `animation.foot-placement-weight`；左右脚正式feature分别写入`LeftFootFeatures`与`RightFootFeatures`，Foot不得二次查询MM Database。`AnimancerPoseSamplingBackend`只按Selection descriptor创建/更新时间冻结的source state并捕获pose，不读取Query、Cost、Domain、Plan或transition Policy。

当前工作区已经由`CharacterAnimationPlaybackRuntime`内部唯一`CharacterMotionMatchingPresentationModule`与`MotionMatchingResolvedPoseRequestFactory`接入旧公共request链，这只是迁移基线。最终Module统一拥有producer、query、selection、frozen output与history completion；Playback只提交正式demand并接收Animation Selection。必须在Selection、显式Player、局部Inertialization/BlendStack与Pose Plan依赖完成后才能归档为current truth；后续缺口不得通过wrapper、fallback或并行播放器解决。

MM不参与Timeline Marker Sync。Foot contact continuity来自MM Artifact feature与admission；跨Timeline/MM producer handoff按显式Pose Graph节点处理连续性，不伪造Marker relation，也不由MM选择Blend Stack或Inertialization。

## Runtime Frame Order

```text
1. consume committed/selected Body and channel commands
2. apply Body ResetSequence before any MM query
3. build trajectory source frame and envelope
4. build query from previous Base pose history
5. execute admission, exact Top-K and plan rerank when cadence requires
6. advance/replace MM selection and publish AnimationSelectionFrame
7. resolve Timeline selections for other channels
8. bind all selections to compiled Pose Plan inputs
9. sample Animancer sources
10. evaluate explicit Player, local Inertialization or BlendStack nodes
11. evaluate pose composition once
12. append bound MM PoseNode history
13. execute FootPlacement world-aware phase once
14. publish FinalAnimationPoseFrame
15. advance Camera, publish diagnostics and acknowledge batch
```

Search Cadence使用Presentation delta累计，但Reset、Domain activation、plan invalidation和mandatory search boundary会强制本帧search。Cadence不改变Gameplay tick，也不通过跳过搜索维持非法plan。

## Network And Branch Semantics

- MM selection、plan、pose history、query和contact protection不进入CharacterState、WorldState、Snapshot、Hash或packet。
- Local Float32、Local Fixed、ServerAuthoritative Prediction owner与Rollback owner已经使用Accepted Intent source；ServerAuthoritative Observed Actor已经使用Selected Body source。它们共用同一个MM Runtime与Database payload。
- Deterministic Rollback remote的Selected/relayed Body Presentation尚未接入，完成前不得把Rollback remote描述为已具备MM。
- 各模型只决定正式Body SourceMode并提交Body/Intent；MM Module按锁定SourceMode选择内部Adapter，不允许Factory或模型拥有搜索器、cost或database副本。
- Committed branch replacement、Selected stream reset、Rollback EventId Replace/Retire与Presentation reset在Body reset边界原子清理history、plan和selection。
- 两个客户端可能因表现帧时序选择不同pose，但不能影响Gameplay结果。相同captured query与database identity必须在Search Replay中产生相同结果。
- Remote没有accepted intent时使用显式Selected Body source和较低confidence，不读取本地Input或猜测authority意图。

## Foot Contact And Foot Placement

MM使用离线Foot Analysis feature解决“能否从当前接触跳到candidate”的动画连续性。Foot Placement使用最终pose与world query解决“脚在当前表面如何锁定”的世界约束。二者单向串联：

```text
Foot Analysis Artifact
  -> MM candidate contact metadata
  -> selected source FootFeatureSamples
  -> Blend Stack per-foot contribution
  -> Pose Graph final per-foot contribution
  -> Foot Placement world constraint
```

Foot Placement不得把Locked/Sliding/anchor写回MM。Body reset会分别清理MM history和Foot Placement world history，但二者不共享mutable state。

## FullBody Action Coexistence

FullBodyAction覆盖期间BaseLocomotion MM继续：

- 消费Body与trajectory source。
- 按正常cadence更新plan。
- 求值与MM Selection Input绑定的Player分支。
- 更新自己的pose history。

Pose Graph可以把Base脚部贡献完全遮蔽，但MM history仍记录绑定Player的完成Pose而非Action final pose。业务收益是Action结束立即显露与当前移动匹配的pose；代价是Action期间仍支付Base MM搜索和采样成本。本项目选择连续运行，不引入“Action覆盖时冻结MM”的第二策略。

## Preview And Tooling

Timeline Preview没有Body/intent，不得伪造MM query。Motion Matching Database Inspector已经提供Source Set owner、显式Build、Artifact状态、Coverage与Search Replay载入。Query Fixture隔离Preview已经完成：

- 只从显式Search Replay Artifact或作者创建的Query Fixture启动。
- 复用正式Runtime database、admission、search、plan、pose source、编译Pose Plan和显式Player节点。
- 显式选择Definition、Program producer和场景`CharacterPipelineHost` Preview Target，显示正式FinalAnimationPoseFrame，但不执行Program、WorldSolver、Foot Placement Physics或Camera。
- 保持Query Fixture只作为Editor预览输入，不进入Runtime Profile或Character资产。

普通Timeline producer preview保持原路径，不尝试把MM producer显示为Timeline clip。

## Diagnostics And Search Replay

`MotionMatchingRuntimeSnapshot`、`MotionMatchingSearchReplayArtifact`与`MotionMatchingSearchReplayRunner`已经进入`RuntimeDebugSession`统一provider、显式Capture和Editor Replay入口。帧事务额外发布Resolve/Complete identity、request count、history append/gap、retained frozen output与reset reason；Inspector不得重新计算另一份结果。

### Runtime Snapshot

固定容量snapshot包含：

- Profile/Database/Artifact/Projection identity。
- Trajectory source kind、age、reset和envelope points。
- Pose history availability与Initialization状态。
- 各admission阶段input/output count。
- 每种reject reason计数和受关注sample细节。
- lower-bound node visit/prune、exact sample count。
- Top-K exact cost分项。
- plan horizon cost分项。
- selected plan、continue/jump、generation和mandatory search time。
- 匹配Player source usage、BlendStack Entry/Stored与局部Inertialization结果。

Interest关闭时只保留正式搜索所需状态，不构造candidate debug集合。

### Search Replay Artifact

显式Capture保存：

```text
DatabaseArtifactIdentity
ProjectionIdentity
Query payload
Current selection/plan
Search policy payload
Expected candidate/reject/cost/selection digest
```

Editor Replay必须加载exact matching Artifact/Projection，运行同一search实现并比较digest。身份不匹配直接拒绝，不迁移旧capture或从当前Profile猜参数。

## Runtime Memory And Performance

- Projection编译所有database、feature、tree、candidate、Top-K、plan、history和diagnostic容量。
- Runtime构造时一次分配固定Native/managed buffers；Present期间不扩容。
- Search tree traversal、admission bitset、Top-K heap和plan workspace均复用。
- 不使用LINQ、反射、字符串feature lookup或Dictionary候选热路径。
- 不按wall-clock deadline提前退出。若Profile规模超过编译容量，Build失败并要求作者拆分正式Search Domain或减少数据。
- stable SampleId、NodeId和PlanId决定相同cost的顺序。

## Validation And Failure

以下情况必须失败：

- Profile声明MM producer却没有唯一MM Profile，或者引用MM Profile却没有MM producer。
- 已启用MM的Profile、Schema、Database、Rig、Foot Artifact或Projection identity错配。
- horizon不递增、缺少零时刻、weight/threshold非有限。
- Segment范围越界、重叠identity、无效loop或continuation悬空。
- Clip root/bone sample非有限或root discontinuity超过硬阈值。
- Feature normalization长度、dense layout或cost weight不一致。
- Search Domain没有Initialization候选。
- 任一正式Domain worst-case admitted sample超过Search Policy容量。
- protected contact配置使某已声明业务区域没有candidate且coverage未被作者修正。
- Pose Selection Generation回退、重复或与Blend Entry identity不一致。
- 同一PresentationFrame重复search/advance/append history。
- Body reset后仍引用旧plan、history或source pose。
- MM source试图应用root motion到Body/VisualRoot。

已启用MM时不得改用旧Locomotion、隐藏Idle、bind pose、全库scan fallback、默认Schema、默认Rig、Transform推断或自动重建。未启用MM不是失败恢复：它是一份没有MM producer、没有MM Profile、没有MM payload和没有MM Runtime的完整合法配置。

## Independent Validation Configuration Boundary

当前仓库尚未提供这份独立正式验证配置。以下十项是内容接线和change完成的门槛，不是已经存在的资产清单：

1. 用户另行提供一份完整正式验证配置及其动画内容。
2. 该配置复用现有`CharacterPipelineDefinition`、`CharacterAnimationPresentationProfile`、Pose Graph、Blend Library、Rig Definition、Foot Analysis Source、MM Profile和Database Definition类型，不创建验证专用Runtime或第二套配置模型。
3. 验证配置拥有自己的Graph producer、Rig、动画Clip、Foot Analysis、Database、Artifact、Projection和Prefab引用，不引用Corin资产，也不从Corin复制缺失内容的占位版本。
4. 验证环境必须显式选择这份Definition；能力代码不得按场景、角色名或资源缺失自动切换Definition。
5. Profile把验证配置自己的稳定MM producer绑定到GroundedLocomotion Search Domain与Database。
6. 显式Build只分析验证配置选中的Database，生成exact identity闭包的`.mmdb`。
7. Character Build只为显式请求的验证Definition编译MM Projection payload；无MM producer的其它Definition不生成该payload。
8. 验证配置缺少Rig、Foot Analysis、Clip、Segment、Artifact、self jump transition或producer binding时Build失败，不借用Corin、默认资源或placeholder。
9. Runtime只在所选Projection包含合法MM payload时构造MM模块；切回无MM配置后不存在挂起的MM实例或共享状态。
10. Diagnostics以验证Definition、Profile、Database、Artifact和Projection identity区分查询，不把能力安装状态误报成Corin已启用MM。

这条验证路径是正式配置实例，不是fallback、feature toggle、临时桥接或兼容读取。Corin的Graph、StateMachine、Timeline、Profile、Definition、Projection、Prefab与动画引用不在本change的写入范围。

## Tradeoffs

### 选择：每个MM Search Domain由稳定producer拥有

业务收益：启用MM的角色加入新Locomotion动画时不必扩张Gameplay状态图，start/stop/pivot由数据覆盖和轨迹决定；未启用角色不承担MM配置成本。代价：数据库coverage和query diagnostics必须足够成熟，否则错误不再能靠查看状态边直接定位。

### 选择：Source Set显式登记而不是扫描目录

业务收益：第三方资产移动目录、重命名Clip或同时导入多套包时不会改变绑定，作者能精确知道哪些内容进入商业构建。代价：首次导入后需要显式登记Clip和Segment，不能把一整个文件夹丢进去让系统猜。

### 选择：Humanoid显式retarget与Generic exact rig并列

业务收益：常见Humanoid动画包可以复用Unity正式Avatar retarget，项目自己的Generic Rig仍保持精确骨架语义。代价：两种模式都必须在Build前证明目标Rig兼容，Generic来源不同的动画不能靠近似骨骼名混用。

### 选择：首版要求真实root trajectory而不修复in-place动画

业务收益：搜索速度与转向feature来自真实动画运动，不会把程序猜出的位移包装成动画事实。代价：只有in-place内容的资产包无法覆盖移动Domain，需要作者换用带root motion的内容，或以后单独设计正式trajectory authoring能力。

### 选择：Trajectory Envelope而不是单条确定轨迹

业务收益：本地快速输入保持高响应，Remote远期预测不会被当作确定事实；同一数据库可服务不同表现source。代价：作者需要理解confidence/tolerance如何影响候选，调试必须展示包络而不是只画一条线。

### 选择：contact硬准入而不是提高脚权重

业务收益：受保护plant不会因为其它feature更优而突然换脚，Foot Placement不用为选片错误兜底。代价：数据库覆盖不足会明确Invalid，因此必须提供coverage report并准备足够入口。

### 选择：精确Top-K下界搜索而不是多搜索模式

业务收益：相同query结果稳定、可重放，作者不需要在Brute Force与近似树之间判断质量差异。代价：Artifact构建和节点bounds更复杂，数据库规模必须在正式容量内。

### 选择：短时序plan而不是单帧winner

业务收益：减少刚跳入就必须再次跳出的候选，stop/pivot/contact release更连贯。代价：每次search要对Top-K追加固定horizon求值，并需要显式continuation graph。

### 选择：FullBody覆盖期间MM继续运行

业务收益：Action退出立即回到当前移动姿势，不需要重新激活Locomotion状态。代价：被完全遮蔽时仍有搜索和Base pose求值成本。本项目优先动作退出连续性。

### 选择：MM selection不进网络与Simulation Snapshot

业务收益：网络模型、Fixed ABI和Gameplay Hash不被表现算法绑住，数据库和搜索可以独立迭代。代价：不同客户端render cadence可能出现不同但合法的姿势选择；Search Replay只保证相同captured query的结果一致。
