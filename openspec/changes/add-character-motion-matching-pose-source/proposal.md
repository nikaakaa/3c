# Change: 增加短时序接触约束 Motion Matching 姿势源

## 重新基线

`refactor-animation-selection-pose-graph-boundary`重新定义MM与最终表现图的边界：本change继续拥有Trajectory、Pose History、Query、Admission、Search、Plan与Selection算法，但输出改为统一`AnimationSelectionFrame`和表现参数，不再降低为同时携带transition的`ResolvedAnimationPoseRequest`。

MM是Selection provider。`MotionMatchingSelectionInput`把结果送入Pose Graph，图上的`SelectedPosePlayer`、可选局部`Inertialization`或显式`BlendStack`决定如何采样和连续化。MM不得固定要求每PoseSlot Stack，也不得建立私有播放器、惯性器、crossfade或最终Pose路径。

## Why

最终方向已经收敛为：Gameplay只提交每个Animation Channel的业务producer，Timeline或MM产生Animation Selection，完整Pose Graph显式决定直接播放、Blend Stack、空间合成、骨骼修改与Foot Placement。当前缺失的是一个可由任意正式角色配置显式安装、又不夺取Gameplay Body权威的Motion Matching Selection Source。Corin没有本能力所需的成套动画，因此本change不配置、不迁移也不修改Corin；能力通过用户另行提供的独立正式配置验证。

UE 5.8 的公开 Motion Matching 已经覆盖 Pose Search Schema、Pose History、Trajectory、Continuing Pose、Chooser 数据库过滤、Brute Force/PCAKDTree/VPTree 搜索、Game Animation Sample 的 capsule-driven locomotion，以及实验性的多角色交互搜索。只复制“轨迹加最近姿势”不会形成项目竞争力，也会把现有 Graph 状态名改写成另一套数据库标签，继续保留人工视觉状态机。

本 change 把超越目标限定在本项目 Grounded Locomotion 纵切，而不是宣称覆盖 UE 整个动画生态。正式差异是：

- 由已接受的运动意图与Committed/Selected Body形成带置信包络的未来轨迹，不直接读取InputAction或Scene Transform。
- 先以Search Domain、初始化资格、片段边界和双脚受保护接触做硬准入，再计算代价；脚接触不是一个可被低权重淹没的普通特征。
- 使用可证明保留精确Top-K结果的分层下界剪枝，而不是按帧时间提前停止或依赖近似结果；再对Top-K执行短时序continuation plan评估，避免只选“当前很像、下一瞬间必坏”的姿势。
- 每次查询都能解释候选被拒绝、入围和最终胜出的原因，并能显式捕获Search Replay Artifact离线重放同一查询。
- Motion Matching selection、pose history与plan明确响应Committed branch replacement、Selected stream reset和Rollback EventId replacement；网络不发送动画pose，但表现分支不会继续使用旧历史。
- 数据库Artifact、Foot Analysis、Rig、Feature Schema、Projection与Clip Binding形成exact identity闭包；重数据分析只由显式Build触发，stale时失败，不在Inspector或普通编译中自动重建。
- 后续导入的FBX或独立AnimationClip先进入显式Motion Source Set，导入、选择、拖入列表和普通资源重导入只更新authoring/status，不触发姿势采样、Foot Analysis、MM Database Build或Character Build。

## What Changes

- 新增`CharacterMotionMatchingProfile`。当且仅当`CharacterAnimationPresentationProfile`存在Motion Matching producer binding时，它 MUST唯一引用该Profile；未声明MM producer的Profile MUST不引用MM Profile、不生成MM payload且不实例化MM Runtime。MM Profile唯一装配Feature Schema、Trajectory Policy、Cost Profile、Search Policy、Database Definition与producer binding；Graph、Timeline、Prefab和Runtime不保存副本。
- 新增`CharacterMotionMatchingSourceSet`作为导入动画进入MM的唯一登记边界。它保存稳定SourceClipId、AnimationClip GUID/local file id、目标Rig、显式`HumanoidRetargeted`或`ExactGenericRig`采样模式和Motion Root Bone；不得扫描目录、按名称分类或自动选择Avatar/Rig。
- 新增`CharacterMotionMatchingDatabaseDefinition`与稳定Database/Segment/SearchDomain identity。Database显式引用Source Set；每个Segment显式保存SourceClipId、采样区间、loop语义、初始化资格、跳入资格、结束行为和有限continuation link，不按clip名或目录猜测。
- Database保存正式Coverage Requirement，声明该业务Domain必须覆盖的速度、面对变化、初始化、脚接触与plan horizon区域；导入动画不足时显式Build失败并报告缺口，不自动镜像、合成root轨迹或借其它数据库补洞。
- Source Set Inspector提供显式`Build Source Set Foot Analysis`，按稳定SourceClipId逐Clip调用既有唯一Foot Analyzer；asset import、selection和MM Build都不得隐式触发它。
- 新增Editor-only `CharacterMotionMatchingDatabaseArtifact`分析链。唯一MM重操作入口是作者明确执行`Build Motion Matching Database`；它只消费Ready Foot Analysis artifact，按目标Sampling Rig采样导入Clip的root轨迹、稳定BoneId姿势/速度、左右脚contact feature、segment boundary和continuation数据，并生成规范化特征、精确搜索树与coverage report。
- Artifact固定写入`Library/CharacterSimulation/Analysis/MotionMatching/<database-guid>.mmdb`；Character Build只解析匹配identity与content hash的Artifact并把不可变payload写入target-neutral Presentation Projection。
- 新增`CharacterPresentationTrajectoryIntent`与model-neutral trajectory source合同。本地/Prediction消费已被Program/World request接受的意图，Remote消费Selected Body observation；二者均降低为统一`MotionMatchingTrajectoryEnvelope`，Runtime不按Network Model切换搜索算法。
- `MotionMatchingTrajectoryEnvelope`在每个未来horizon保存局部位置、面对方向、容许半径和置信度。远期或Remote不确定性通过正式包络表达，不通过隐藏权重或回退数据库处理。
- 新增只记录上一帧及更早、与MM Selection Input绑定的Player完成Pose的固定容量Pose History。FullBody Action覆盖后的最终pose、Foot Placement IK和VisualRoot correction不进入查询姿势真相。
- 新增constraint-first candidate admission：Search Domain、Rig/Clip可用性、初始化资格、jump interval、segment horizon、continuation graph和双脚protected contact任一不满足时直接拒绝候选。
- 新增确定性分层精确搜索：对规范化特征树计算代价下界并稳定剪枝，精确计算剩余候选，使用stable SampleId打破同分；不按毫秒预算提前退出，也不在无结果时回退全库或旧Locomotion。
- 新增短时序Plan Rerank：对精确Top-K候选沿显式continuation graph评估固定horizon内的轨迹、面对、姿势趋势、contact release与segment终点，输出一个有界`MotionMatchingSelectionPlan`。
- 新增Initialization Query。Pose History不可用时只允许`CanInitialize`样本，并使用Schema显式声明的初始化Feature Mask；不播放隐藏Idle等待历史。
- 新增`MotionMatchingSelectionGeneration`，独立于Program拥有的`AnimationPlaybackId`。同plan continuation保持Selection identity并更新sample；同一MM playback内部pose jump提升generation，由`SelectedPosePlayer`发布typed discontinuity。后续是硬切、局部Inertialization还是Blend Stack CrossFade只由编译图决定。
- 将Timeline与Motion Matching都降低为source-neutral `AnimationSelectionFrame`。Selection共同表达Channel、producer/source identity、generation、sample time、clip sample descriptor、Pose Parameter与Foot Feature；显式Player和Animancer source backend不读取搜索器类型。
- MM只作为`BaseLocomotion`上游Pose Source。它不建立私有crossfade、Pose Graph、IK、root motion应用、Gameplay事件、Notify或网络同步路径。
- 动画root只进入离线trajectory feature。Runtime不得读取`Animator.deltaPosition`/`deltaRotation`修改Body；VisualRoot继续只服从`CharacterBodyPresentationRuntime`。
- 每帧正式顺序扩展为`Body -> MM trajectory/query/search -> Selection -> compiled Pose Graph Plan -> append matched Base Pose History -> FootPlacement phase -> Camera`。
- 新增统一Motion Matching diagnostics、Database coverage inspection与显式Search Replay capture，显示query envelope、admission reject、Top-K cost、plan cost、continue/jump、contact protection、reset、search visit count和exact identity。
- 独立验证配置使用现有`CharacterPipelineDefinition`、`CharacterAnimationPresentationProfile`、Rig、Foot Analysis、MM Profile与Database类型完整装配，并由验证环境显式选择；它不是第二套Runtime、fallback或临时配置，也不得引用Corin资产或缺失动画的占位资源。

## Implementation Baseline

截至2026-07-22，工作区已经实现并接入以下基础，不再把它们描述为尚未开始的设计：

- Authoring与离线产物链已经存在：Motion Matching Profile、Source Set、Database Definition、显式Foot Analysis批处理、显式Database Build、`.mmdb` identity/codec/store、Coverage、Projection payload与Runtime Database。
- Runtime选择链已经存在：Accepted Intent/Selected Body source合同、Trajectory Envelope、Base Pose History、Query、Candidate Admission、Exact Top-K、Short-Horizon Plan、Selection Lifecycle与Motion Matching Pose Source。
- 当前公共表现接线已经把MM结果降低为`ResolvedAnimationPoseRequest`并进入固定Pose Slot Stack；该实现是本次迁移输入。最终必须改为`AnimationSelectionFrame -> MotionMatchingSelectionInput -> explicit Player -> Pose Plan`，MM仍不得拥有第二个PlayableGraph、私有fade或root-motion Body写回。
- Local Float32、Local Fixed、ServerAuthoritative Prediction owner与ServerAuthoritative Observed Actor已经分别沿Accepted Intent或Selected Body装配；Projection无MM payload时不创建MM Runtime。

本轮实施已经继续收口以下闭环：

- Selected Body source把selected cursor的真实sample age送入Trajectory Envelope；Rollback remote复用Rollback输出提交器最终选中的Body transaction，不接入Intent，也不在Network Model复制MM search、cost或database。
- 受关注candidate reject保存实际值、阈值与第二组比较值；全部MM payload进入统一`RuntimeDebugSession`，interest关闭时不构造candidate detail集合。
- Runtime Debug提供显式Search Replay Capture，Database Inspector提供exact identity校验后的Editor Replay；Editor-only Query Fixture直接复用正式Runtime Database、Admission、Search与Plan实现。
- Profile条件式装配提供typed orphan/missing/duplicate-owner diagnostic；诊断宿主区分项目具备能力与当前Definition是否启用MM，并只在当前Projection含MM payload时读取正式snapshot。

尚未收口的是真实内容闭环，而不是上述类型是否存在：

- Query Fixture尚未连接正式Pose Source、编译Pose Plan、显式Player与Pose Graph的隔离姿势预览。
- 用户另行提供的独立正式验证配置、显式选择入口和完整identity闭包尚未落地；按本提案停止条件，缺少完整Rig、Foot Analysis、动画Clip、Database segment或正式producer binding时不得用Corin或占位资源补齐。
- current specs仍把基础动画正式来源限定为Timeline producer；必须等本change完成并在依赖change之后归档，才能把“项目具备可选MM能力”安装为current truth。Corin当前仍未配置MM。

因此本change继续保持active。任务状态以重新审计后的`tasks.md`为准，不再使用旧的497/596完成度判断实现范围。

## Capabilities

### New Capabilities

- `character-motion-matching-presentation`：定义Motion Matching条件式装配、导入动画Source Set、Rig兼容性、显式切片Build、coverage合同、analysis artifact、Projection payload、trajectory envelope、pose history、constraint-first search、short-horizon plan、pose source、reset、network隔离、diagnostics与独立验证配置合同。

### Modified Capabilities

- `character-animation-presentation-authoring`：让唯一Animation Presentation Profile装配Motion Matching Profile，并保持显式Analysis Build与target-neutral Projection边界。
- `character-animation-pipeline`：把Motion Matching置于Animation Selection阶段，并固定进入同一编译Pose Plan、显式Player、Pose Graph与FootPlacement链。
- `character-animation-layer-runtime`：区分Program playback generation与MM内部pose selection generation，使同playback pose jump能够产生typed discontinuity，而不是强制创建Blend Entry。
- `character-presentation-interpolation`：增加轨迹意图、MM表现历史和分支重置语义，同时保持Simulation与Presentation单向隔离。
- `character-animation-foot-analysis-artifact`：让MM Artifact精确复用同一Foot Analysis，不生成第二份foot phase/contact数据源。
- `character-foot-placement-presentation`：让MM选中sample的Foot Feature沿最终Pose贡献进入既有Foot Placement，并禁止Foot Placement反向选择动画。
- `btsmtl-runtime-diagnostics`：增加统一Motion Matching query、candidate、plan和replay只读诊断。

## Dependencies And Sequencing

- 硬依赖`refactor-animation-selection-pose-graph-boundary`、`add-character-presentation-pose-graph`与`refactor-inertial-blending-to-local-pose-node`安装同一最终运行链。当前工作区的共享SourceId、request、固定Stack与Pose Graph接线只是迁移基线；归档前必须改为Selection、显式Player、可选局部Inertialization或Blend Stack和统一Pose Plan，不能把旧接线提前宣称为current truth。
- 依赖已完成的`refactor-presentation-projection-target-boundary`。MM payload只进入target-neutral Projection，不读取Float32/Fixed ProgramHash、NumericProfile或Target ABI。
- 复用已完成的`add-predictive-foot-placement-presentation-pass`与Animation Foot Analysis artifact。MM不新增FootPhase Track、Blackboard变量或第二套脚接触分析。
- `add-character-presentation-pose-graph`与本change共享Animation Selection、Pose Source和Pose Graph边界，但本change只增加条件式MM source能力，不编辑现有角色Graph、Timeline、Definition、Profile、Projection或Prefab资产。
- `refactor-animation-playback-to-blend-stack`保留完整`AnimationPoseSourceId`与多source usage语义；`refactor-inertial-blending-to-local-pose-node`拥有局部残差。Continue保持Selection identity，Jump提升Selection Generation并发布discontinuity，不能通过伪造Playback、Blend Entry或私有fade绕过图拓扑。
- 用户另行提供的验证配置必须通过现有正式Definition/Profile入口被验证环境显式选择。本change不预设Gameplay Lab或现有角色为验证宿主，也不为缺少动画的角色增加能力标签。
- 唯一集成owner保持为`CharacterAnimationPresentationProfile`、`CharacterPresentationProjection`、`CharacterAnimationPlaybackRuntime`与`CharacterSimulationPresentationRuntime`；后续诊断、Replay与验证配置只能接入这些边界，不再创建并行协调器。

## Current Spec Comparison

- 现行`character-animation-pipeline`把正式姿势来源写成Timeline visual sampler。本change不建立第二条播放器，而是把Timeline与MM都降低为同一个Animation Selection合同，再执行同一编译Pose Plan；该spec需要增加非Timeline Selection provider准入。
- 现行`character-animation-layer-runtime`按Blend Entry解释同Playback连续sample。MM内部会在同一Program producer activation中切换Clip/Sample，因此必须把Playback identity与Pose Selection identity分离；否则Player无法发布准确discontinuity，局部Inertialization或显式Blend Stack也无法按图处理切换。
- 现行`character-animation-presentation-authoring`只有Foot Analysis Source和producer binding，没有条件式Motion Matching Profile、Database、Schema或Search Policy。本change继续由同一个Profile Inspector进入配置，不新增独立Workbench或运行时SO读取；没有MM producer的Profile保持无MM引用。
- 现行`character-presentation-interpolation`只定义Body、Timeline visual time、动画fade和网络selected stream。本change新增Presentation-owned trajectory envelope、MM history与plan，但不把它们写入Character/World state、Snapshot、Hash或协议。
- 现行`character-animation-foot-analysis-artifact`已经拥有Editor-only规范artifact、显式build、stale校验和Projection发布。本change复用其Artifact identity与sample语义；MM database不重新计算另一套plant/landing真相。
- 现行`character-foot-placement-presentation`要求Foot Placement消费最终pose与visible contribution。本change只让MM source按选中Clip/SampleTime提供正式Foot Feature；Foot Placement仍然在Pose Graph之后执行且不能反向影响搜索。
- 现行`character-state-timeline-authoring-loop`描述Corin的既有Locomotion链。本change不修改该capability，也不修改其Graph、Timeline、Marker、producer或transition资产。
- `openspec/project.md`现在明确记录MM工作区基础和剩余闭环，但current specs尚未安装`character-motion-matching-presentation`。实施完成后必须把current口径拆成“项目具备可选MM能力”与“Corin未配置MM”；active阶段不提前宣称已安装。

## UE 5.8 Baseline And Overtake Boundary

公开基线以Epic官方[Motion Matching文档](https://dev.epicgames.com/documentation/en-us/unreal-engine/motion-matching-in-unreal-engine?lang=en-US)、[Game Animation Sample](https://dev.epicgames.com/documentation/en-us/unreal-engine/game-animation-sample-project-in-unreal-engine)和实验性[Motion Match Multi](https://dev.epicgames.com/documentation/en-us/unreal-engine/BlueprintAPI/Animation/PoseSearch/MotionMatchMulti?lang=en-US)为准。

本change计划超过的是Grounded Locomotion业务闭环的以下部分：

- UE常见配置使用Chooser先过滤数据库，再由Pose Search选择当前pose；本change用编译后的producer-to-domain binding、硬约束准入和短时序plan替代运行时Blueprint/Chooser胶水。
- UE提供Brute Force、PCAKDTree和实验VPTree；本change只安装一种结果可证明精确、stable tie-break且可逐项重放的分层下界搜索，不向作者暴露多个运行模式。
- UE轨迹主要表达一条预测路径；本change正式表达每个horizon的容许半径与置信度，让本地意图和Remote observation使用同一query结构但不伪装成同等确定。
- UE提供Continuing Pose Bias和丰富debug；本change把双脚protected contact、segment horizon、continuation graph作为硬准入，并保存每个reject与plan分项，能够显式捕获exact replay artifact。
- UE Game Animation Sample仍通过AnimBlueprint functions和Chooser维护IsMoving/IsStarting/IsPivoting等视觉状态；本change的正式MM配置以稳定producer、编译Search Domain和trajectory query完成sample选择，不强迫业务Graph复制这些视觉阶段。

本change不声称超过UE的完整资源生态、Orientation/Stride Warping、Traversal、Montage工具或实验性多角色交互。那些能力不属于本次Grounded Locomotion Pose Source，不能用名称包装成已实现。

## Breaking Changes

- `CharacterAnimationPresentationProfile`增加条件式Motion Matching Profile引用；只有声明MM producer的配置必须提供完整引用并生成MM Projection payload，未声明MM producer的配置不生成该payload。
- `AnimationSelectionFrame`增加独立Pose Selection identity；仅按PlaybackId判断continuation的旧实现删除。
- MM Selection连接局部Inertialization时必须提供完整endpoint policy；连接BlendStack时必须提供完整可达CrossFade pair。不得由MM创建私有fade、默认transition或私有惯性器。
- 已声明MM producer时，数据库Artifact与Projection payload缺失、stale、Rig/Schema/Foot Analysis identity不匹配会使Build直接失败；不得改用Timeline、旧Locomotion或默认数据库。
- 旧diagnostics中把Marker Sync、Motion Warp或状态切换误称为MM的占位口径删除，统一读取正式MM snapshot。

## Out Of Scope

- 不让动画root motion、selected clip速度或pose correction修改Gameplay Body、CharacterController、Fixed KCC、DotRecast或WorldSolver结果。
- 不实现Stride Warping、Orientation Warping、Slope Warping、Distance Matching、Root Offset Bone或Motion Warping替代品。
- 不把Attack、Dodge、Hit Reaction、Traversal、Vault、Climb或Airborne第一版迁入MM；它们继续使用正式Timeline/Action Pose Source。
- 不实现UE实验性的多角色Interaction Motion Matching；当前项目命中、目标registry与跨角色GameplayResult尚未闭环，不能由Presentation先行旁路。
- 不把Foot Placement world anchor、IK结果或Scene Physics查询写回MM candidate selection。
- 不建立运行时数据库构建、动态Clip扫描、目录约定、clip名Tag、Humanoid bone fallback或stale Artifact fallback。
- 不自动镜像动画，不从in-place移动Clip猜测或合成root trajectory，不在MM内实现通用DCC清洗/重定向烘焙；首版只接受通过显式Humanoid Avatar retarget或Exact Generic Rig合同后能在目标Sampling Rig上得到合法姿势与root trajectory的Clip。
- 不新增BTSMTL Gameplay Graph MM状态、Timeline Motion Matching Track或独立Animation Blueprint解释层；唯一`MotionMatchingSelectionInput`只是Pose Graph的typed输入节点。
- 不修改Corin的Graph、StateMachine、Timeline、Profile、Definition、Projection、Prefab、动画、Marker Group或transition；不从Corin借用Rig、Foot Analysis或Clip作为验证占位。
- 不增加测试或人工验证task，不运行Unity batchmode。

## Stop Conditions

- 如果Animation Selection与SelectedPosePlayer仍无法在同一`AnimationPlaybackId`下表达独立Pose Selection Generation和typed discontinuity，停止并先修正共享identity，不伪造新Playback、Blend Entry或在MM内部连续化。
- 如果Motion Matching必须读取InputAction、MonoBehaviour Update、Scene Transform或Network packet才能获得轨迹，停止并补正式Presentation trajectory source，不直接旁路。
- 如果数据库Build需要在Inspector repaint、asset import、domain reload或普通Character Compile中隐式运行，停止并保留显式重操作入口。
- 如果导入Clip只能通过骨骼名猜测、场景Animator、隐式Avatar替换或运行时retarget fallback才能在目标Rig播放，停止并要求作者修正Source Set或Unity import配置。
- 如果业务Coverage Requirement声明非零移动范围但导入Clip在目标Rig上的root trajectory仍是in-place，停止并要求正式root-motion内容或后续独立的trajectory authoring能力，不在Builder推测位移。
- 如果Foot Analysis Artifact与MM所需脚接触语义不一致，停止并升级唯一Foot Analysis合同，不在MM目录复制Analyzer。
- 如果独立验证配置缺少完整Rig、Foot Analysis、动画Clip、Database segment或正式producer binding，停止内容接线并等待完整配置；不得复用Corin资产、生成占位动画或建立临时路径。
- 如果无合法candidate只能通过旧Idle、旧state machine、bind pose或全库fallback维持输出，停止并修正Database coverage/Initialization eligibility；正式结果必须是Selection或typed Invalid。
- 如果短时序plan需要按帧毫秒预算提前退出而造成相同query不同结果，停止并收紧Artifact规模或分区，不引入时间驱动非稳定搜索。

## Success Criteria

- Corin全部现有Graph、StateMachine、Timeline、Profile、Definition、Projection、Prefab与动画引用在本change前后保持不变，且不新增MM Profile或MM producer。
- 未声明MM producer的正式配置不引用MM Profile、不生成MM Projection payload且不实例化MM Runtime；声明MM producer的独立正式验证配置必须完整装配并明确失败，不能回退。
- Program、WorldSolver与Committed/Selected Body继续唯一决定真实位移；MM只选择Presentation pose，Runtime没有`Animator.deltaPosition`或deltaRotation回写。
- Motion Matching Profile、Database、Schema、Policy、Rig、Foot Analysis、Artifact、Projection与Clip binding拥有完整可追踪identity，任何stale或错配都明确失败。
- 重数据分析只由显式Build触发，Character Build只消费现有合法Artifact。
- 导入或重导入FBX、选择资源、打开Inspector、修改Source Set和普通Character Compile都不执行采样；只有显式`Build Source Set Foot Analysis`执行脚分析，只有显式`Build Motion Matching Database`执行MM重分析。
- Source Set精确追踪Clip GUID/local file id、import dependency、Avatar/Generic rig兼容性、Motion Root与目标Rig identity；重导入后旧`.mmdb`只变为Stale并等待作者主动重建。
- 本地已接受意图与Remote selected body都降低为带source identity、confidence和tolerance的同一Trajectory Envelope，不直接读取InputAction或Transform。
- Initialization、Search Domain、segment boundary、continuation和protected foot contact全部在代价计算前准入；无合法candidate产生typed Invalid，不回退旧动画。
- 分层搜索对完整合法候选保持精确Top-K语义、稳定tie-break和固定数据访问上界；不按运行帧时长提前退出。
- Top-K候选经过短时序plan评估，Selection能解释当前成本、未来horizon成本和continuation/jump原因。
- 同MM playback continuation保持Selection identity并只更新sample；pose jump提升Selection Generation并由SelectedPosePlayer发布discontinuity。推荐BaseLocomotion图使用局部Inertialization；若作者显式选择Blend Stack，则只复用该节点的CrossFade、Stored Pose和retirement。
- FullBody Action覆盖期间BaseLocomotion MM继续更新绑定Player的Pose和history，Action退出后不需要重新启用旧Locomotion状态。
- Rollback branch replacement、Selected stream reset、Presentation reset和Projection replacement会原子清空query history、selection plan与contact protection，并在新分支执行Initialization Query。
- MM Foot Feature沿普通Pose Value的实际脚骨骼贡献进入显式FootPlacement节点；Foot Placement、IK和world anchor不反向影响candidate选择。
- Diagnostics能够显示完整query envelope、stage candidate count、reject reason、Top-K cost、plan cost、selected sample、contact protection、selection generation、Player discontinuity、可选Stack entry或Inertialization residual与reset reason。
- 显式Search Replay Artifact能在相同Database identity上复现query、候选顺序、reject、cost与最终Selection；identity不匹配时拒绝重放。
