# Change: 增加短时序接触约束 Motion Matching 姿势源

## 重新基线

当前MM与最终表现图的边界由前置`refactor-animation-control-boundaries`安装：本change继续拥有Trajectory、Pose History、Query、Admission、Search、Plan与Selection算法，公开输出统一为state-local `PresentationPoseSourceSample`和表现参数，transition只由显式Pose Graph节点处理。

MM是PoseState内部Selection provider。当前relevant State的显式Player消费结果，图上的`SelectedPosePlayer`、可选局部`Inertialization`或显式`BlendStack`决定如何采样和连续化。MM不得固定要求每PoseSlot Stack，也不得建立私有播放器、惯性器、crossfade或最终Pose路径。

## Why

最终方向已经收敛为：Gameplay只提交有限Action Timeline业务事实，PoseState relevance驱动State内部Sequence、Blend Space或Motion Matching source，完整Pose Graph显式决定Player、局部Inertialization、Blend Stack、空间合成、骨骼修改、FootGrounding Baseline Goals、可选PredictiveFootPlacementModifier Final Goals与FullBodyIK。当前缺失的是一套具有完整内容闭包、又不夺取Gameplay Body权威的正式Motion Matching配置。Corin没有本能力所需的成套动画，因此本change不配置、不迁移也不修改Corin；能力通过用户另行提供的独立正式配置验证。

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

- 新增`CharacterMotionMatchingProfile`。当且仅当`CharacterAnimationPresentationProfile`的Pose Graph存在可达Motion Matching provider时，它 MUST唯一引用该Profile；未声明MM provider的Profile MUST不引用MM Profile、不生成MM payload且不实例化MM Runtime。MM Profile唯一装配Feature Schema、Trajectory Policy、Cost Profile、Search Policy、Database Definition与provider-to-SearchDomain binding；Graph、Timeline、Prefab和Runtime不保存副本。
- 新增`CharacterMotionMatchingSourceSet`作为导入动画进入MM的唯一登记边界。它保存稳定SourceClipId、AnimationClip GUID/local file id、目标Rig、显式`HumanoidRetargeted`或`ExactGenericRig`采样模式和Motion Root Bone；不得扫描目录、按名称分类或自动选择Avatar/Rig。
- 新增`CharacterMotionMatchingDatabaseDefinition`与稳定Database/Segment/SearchDomain identity。Database显式引用Source Set；每个Segment显式保存SourceClipId、采样区间、loop语义、初始化资格、跳入资格、结束行为和有限continuation link，不按clip名或目录猜测。
- Database保存正式Coverage Requirement，声明该业务Domain必须覆盖的速度、面对变化、初始化、脚接触与plan horizon区域；导入动画不足时显式Build失败并报告缺口，不自动镜像、合成root轨迹或借其它数据库补洞。
- Source Set Inspector提供显式`Build Source Set Foot Analysis`，按稳定SourceClipId逐Clip调用既有唯一Foot Analyzer；asset import、selection和MM Build都不得隐式触发它。
- 新增Editor-only `CharacterMotionMatchingDatabaseArtifact`分析链。唯一MM重操作入口是作者明确执行`Build Motion Matching Database`；它只消费Ready Foot Analysis artifact，按目标Sampling Rig采样导入Clip的root轨迹、稳定BoneId姿势/速度、左右脚contact feature、segment boundary和continuation数据，并生成规范化特征、精确搜索树与coverage report。
- Artifact固定写入`Library/CharacterSimulation/Analysis/MotionMatching/<database-guid>.mmdb`；Character Build只解析匹配identity与content hash的Artifact并把不可变payload写入target-neutral Presentation Projection。
- 新增`CharacterPresentationTrajectoryIntent`与model-neutral trajectory source合同。本地/Prediction消费已被Program/World request接受的意图，Remote消费Selected Body observation；二者均降低为统一`MotionMatchingTrajectoryEnvelope`，Runtime不按Network Model切换搜索算法。
- `MotionMatchingTrajectoryEnvelope`在每个未来horizon保存局部位置、面对方向、容许半径和置信度。远期或Remote不确定性通过正式包络表达，不通过隐藏权重或回退数据库处理。
- 新增只记录上一帧及更早、与PoseState MM Player绑定的完成Pose的固定容量Pose History。FullBody Action覆盖后的最终pose、FootGrounding的Lyra current/contact/anchor/pelvis Baseline Goals、PredictiveFootPlacementModifier Final Goals、FullBodyIK结果和VisualRoot correction不进入查询姿势真相。
- 新增constraint-first candidate admission：Search Domain、Rig/Clip可用性、初始化资格、jump interval、segment horizon、continuation graph和双脚protected contact任一不满足时直接拒绝候选。
- 新增确定性分层精确搜索：对规范化特征树计算代价下界并稳定剪枝，精确计算剩余候选，使用stable SampleId打破同分；不按毫秒预算提前退出，也不在无结果时回退全库或旧Locomotion。
- 新增短时序Plan Rerank：对精确Top-K候选沿显式continuation graph评估固定horizon内的轨迹、面对、姿势趋势、contact release与segment终点，输出一个有界`MotionMatchingSelectionPlan`。
- 新增Initialization Query。Pose History不可用时只允许`CanInitialize`样本，并使用Schema显式声明的初始化Feature Mask；不播放隐藏Idle等待历史。
- 新增`MotionMatchingSelectionGeneration`，独立于Gameplay playback。同plan continuation保持Projection-local dense source index、Player NodeId与lease并更新sample；同一MM source内部pose jump提升generation，由绑定Player发布typed discontinuity。后续是硬切、局部Inertialization还是Blend Stack CrossFade只由编译图决定。
- 将Motion Matching降低为state-local `PresentationPoseSourceSample`。Sample表达Projection-local dense source index、Player NodeId、generation、lease、sample time、availability、clip sample descriptor、Pose Parameter与Foot Feature；不携带作者Source Slot/Binding对象、source字符串、Gameplay channel、producer或PlaybackId，显式Player和Animancer source backend不读取搜索器类型。
- MM只作为Locomotion PoseState内部Pose Source。它不建立私有crossfade、Pose Graph、IK、root motion应用、Gameplay事件、Notify或网络同步路径。
- 动画root只进入离线trajectory feature。Runtime不得读取`Animator.deltaPosition`/`deltaRotation`修改Body；VisualRoot继续只服从`CharacterBodyPresentationRuntime`。
- 每帧正式顺序扩展为`Body -> MM trajectory/query/search -> Selection -> compiled Pose Graph Plan内绑定Player节点完成时append matched Base Pose History -> FootGrounding Baseline Goals -> optional PredictiveFootPlacementModifier Final Goals -> FullBodyIK -> FinalPublication -> Camera`。
- 新增统一Motion Matching diagnostics、Database coverage inspection与显式Search Replay capture，显示query envelope、admission reject、Top-K cost、plan cost、continue/jump、contact protection、reset、search visit count和exact identity。
- 独立验证配置使用现有`CharacterPipelineDefinition`、`CharacterAnimationPresentationProfile`、Rig、Foot Analysis、MM Profile与Database类型完整装配，并由验证环境显式选择；它不是第二套Runtime、fallback或临时配置，也不得引用Corin资产或缺失动画的占位资源。

## Current Integration Status

截至2026-07-23，Motion Matching在业务和内容层面仍是未接入状态。工作区存在大量Runtime、Editor、编译器和数据类型，但没有任何正式角色能够进入MM链路：

- Corin的`CharacterAnimationPresentationProfile.m_MotionMatchingProfile`为空。
- Corin生成Projection中的MM开关为0，`m_MotionMatchingClips`为空。
- 当前没有独立正式MM验证Definition同时装配Rig、Foot Analysis Artifact、Source Set、动画Segment、MM Profile、Database Artifact、state-local provider binding与PoseState Player route。
- 因为Projection不含MM payload，Factory不会构造`CharacterMotionMatchingPresentationModule`，Runtime不会执行trajectory、query、search、selection或history。
- 已导入的MxM插件也没有被GameScripts、Character Config或Projection引用，不属于当前接入结果。

已经完成的是“可供接入使用的基础设施代码”，不是“MM已经接入角色”：

- Authoring与离线工具类型已经存在：Motion Matching Profile、Source Set、Database Definition、Foot Analysis批处理、Database Build、`.mmdb` codec/store、Coverage与Projection payload compiler。
- Runtime算法类型已经存在：Accepted Intent/Selected Body source合同、Trajectory Envelope、Pose History、Query、Candidate Admission、Exact Top-K、Short-Horizon Plan、Selection Lifecycle与Pose Source。
- 公共动画边界最终使用`PoseState relevance -> MM PresentationPoseSourceSample -> explicit Player -> Pose Plan`接口。
- Diagnostics、Search Replay、Database Inspector与Query Fixture Preview代码已经存在；没有正式Definition、Database与Projection时，这些入口不能证明Runtime内容接入完成。

本change下一阶段的目标是完成一套独立正式MM配置及其完整内容identity，使现有基础设施第一次被真实Projection和角色调用。它不是继续深化尚未运行的内部Module。缺少完整Rig、Foot Analysis、动画Clip、Database Segment或正式provider binding时，必须停止，不得用Corin、MxM Demo资产、placeholder或fallback补齐。

current specs已经安装PoseState内部Sequence与Blend Space的正式来源，但尚未安装可选Motion Matching capability；只有本change的正式配置、Projection payload和运行链闭合后，才能把“项目具备可选MM能力”安装为current truth。因此本change继续保持active。

## 后续动画职责重构关系

本change继续完成Motion Matching的通用Trajectory、Database、Query、Search、Plan、Selection与Pose source闭环。剩余正式内容任务 MUST在`refactor-animation-control-boundaries`和`refactor-motion-matching-presentation-module`依次完成后实施，直接使用PoseState relevance、State内部MM provider与Player，不得创建Gameplay BaseLocomotion channel、旧MotionMatchingSelectionInput外部入口或并行接线。

## Capabilities

### New Capabilities

- `character-motion-matching-presentation`：定义Motion Matching条件式装配、导入动画Source Set、Rig兼容性、显式切片Build、coverage合同、analysis artifact、Projection payload、trajectory envelope、pose history、constraint-first search、short-horizon plan、pose source、reset、network隔离、diagnostics与独立验证配置合同。

### Modified Capabilities

- `character-animation-presentation-authoring`：让唯一Animation Presentation Profile装配Motion Matching Profile，并保持显式Analysis Build与target-neutral Projection边界。
- `character-animation-pipeline`：把Motion Matching置于Animation Selection阶段，并固定进入同一编译Pose Plan、显式Player、`Lyra Current Grounding -> Stance Stabilization -> Pelvis Resolve` FootGrounding、可选Swing脚PredictiveFootPlacementModifier与FullBodyIK链。
- `character-animation-layer-runtime`：区分Program playback generation与MM内部pose selection generation，使同playback pose jump能够产生typed discontinuity，而不是强制创建Blend Entry。
- `character-presentation-interpolation`：增加轨迹意图、MM表现历史和分支重置语义，同时保持Simulation与Presentation单向隔离。
- `character-animation-foot-analysis-artifact`：让MM Artifact精确复用同一Foot Analysis，不生成第二份foot phase/contact数据源。
- `character-foot-placement-presentation`：让MM选中sample的Foot Feature沿最终Pose贡献进入普通FootGrounding，下一落地Feature再供可选Swing脚Predictive Modifier消费，并禁止FootGrounding current/contact/anchor/pelvis、Predictive Extension与FullBodyIK反向选择动画。
- `btsmtl-runtime-diagnostics`：增加统一Motion Matching query、candidate、plan和replay只读诊断。

## Dependencies And Sequencing

- 已归档的Selection、Pose Graph、Blend Stack、Inertialization与Motion Matching模块已经安装同一最终运行链；本change不得恢复Shared PlaybackId、每槽固定Stack、私有播放器、私有fade或第二条Pose路径。
- 依赖已完成的`refactor-presentation-projection-target-boundary`。MM payload只进入target-neutral Projection，不读取Float32/Fixed ProgramHash、NumericProfile或Target ABI。
- 复用已完成的`add-predictive-foot-placement-presentation-pass`与Animation Foot Analysis artifact。MM不新增FootPhase Track、Blackboard变量或第二套脚接触分析。
- `refactor-animation-control-boundaries`拥有state-local Selection ABI、PoseState readiness和业务拓扑；MM不得新增Gameplay channel、producer binding或Action playback identity。
- `refactor-pose-graph-to-btsmtl-authoring-domain`拥有唯一Capability Catalog、Document v3只读资源引用、共享UI、typed mutation与Pose IR；MM不得新增独立GraphView、节点switch或绕过Document的写入口。
- 当前Blend Stack与Inertialization合同保留完整`AnimationPoseSourceId`、多source usage和局部残差所有权。Continue保持Selection identity，Jump提升Selection Generation并发布discontinuity，不能通过伪造Playback、Blend Entry或私有fade绕过图拓扑。
- 用户另行提供的验证配置必须通过现有正式Definition/Profile入口被验证环境显式选择。本change不预设Gameplay Lab或现有角色为验证宿主，也不为缺少动画的角色增加能力标签。
- 唯一集成owner保持为`CharacterAnimationPresentationProfile`、`CharacterPresentationProjection`、`CharacterAnimationPresentationRuntime`与`CharacterSimulationPresentationRuntime`；后续诊断、Replay与验证配置只能接入这些边界，不再创建并行协调器。
- 本change剩余独立MM内容不修改Corin Rollback关键资产，也不阻塞Corin迁移与Rollback闭合；唯一串行关系见`openspec/character-pipeline-serial-execution.md`。

## Current Spec Comparison

- 现行`character-animation-pipeline`把正式姿势来源写成Timeline visual sampler。本change不建立第二条播放器，而是把Timeline与MM都降低为同一个Animation Selection合同，再执行同一编译Pose Plan；该spec需要增加非Timeline Selection provider准入。
- 现行`character-animation-layer-runtime`已经把PoseState relevance与Pose source selection generation分开。MM必须沿用该state-local identity；否则Player无法发布准确discontinuity，局部Inertialization或显式Blend Stack也无法按图处理切换。
- 现行`character-animation-presentation-authoring`已有Pose Graph、node-local policy、Rig、Foot Analysis Source和Timeline producer binding，但没有条件式Motion Matching Profile、Database、Schema或Search Policy。本change继续由同一个Profile Inspector进入配置，不新增独立Workbench或运行时SO读取；没有MM provider的Profile保持无MM引用。
- 现行`character-presentation-interpolation`只定义Body、Timeline visual time、动画fade和网络selected stream。本change新增Presentation-owned trajectory envelope、MM history与plan，但不把它们写入Character/World state、Snapshot、Hash或协议。
- 现行`character-animation-foot-analysis-artifact`已经拥有Editor-only规范artifact、显式build、stale校验和Projection发布。本change复用其Artifact identity与sample语义；MM database不重新计算另一套plant/landing真相。
- active `replace-pose-ik-with-finalik-full-body-solver`要求FootGrounding消费最终pose contribution并发布Baseline Goals，可选PredictiveFootPlacementModifier只消费Swing资格与未来落点并发布Final Goals。本change只让MM source按选中Clip/SampleTime提供正式Foot Feature；Grounding、Predictive Extension与FullBodyIK仍位于MM history source节点之后且不能反向影响搜索。
- 现行`character-state-timeline-authoring-loop`描述Corin的既有Locomotion链。本change不修改该capability，也不修改其Graph、Timeline、Marker、producer或transition资产。
- `openspec/project.md`现在明确记录MM工作区基础和剩余闭环，但current specs尚未安装`character-motion-matching-presentation`。实施完成后必须把current口径拆成“项目具备可选MM能力”与“Corin未配置MM”；active阶段不提前宣称已安装。

## UE 5.8 Baseline And Overtake Boundary

公开基线以Epic官方[Motion Matching文档](https://dev.epicgames.com/documentation/en-us/unreal-engine/motion-matching-in-unreal-engine?lang=en-US)、[Game Animation Sample](https://dev.epicgames.com/documentation/en-us/unreal-engine/game-animation-sample-project-in-unreal-engine)和实验性[Motion Match Multi](https://dev.epicgames.com/documentation/en-us/unreal-engine/BlueprintAPI/Animation/PoseSearch/MotionMatchMulti?lang=en-US)为准。

本change计划超过的是Grounded Locomotion业务闭环的以下部分：

- UE常见配置使用Chooser先过滤数据库，再由Pose Search选择当前pose；本change用编译后的provider-to-domain binding、硬约束准入和短时序plan替代运行时Blueprint/Chooser胶水。
- UE提供Brute Force、PCAKDTree和实验VPTree；本change只安装一种结果可证明精确、stable tie-break且可逐项重放的分层下界搜索，不向作者暴露多个运行模式。
- UE轨迹主要表达一条预测路径；本change正式表达每个horizon的容许半径与置信度，让本地意图和Remote observation使用同一query结构但不伪装成同等确定。
- UE提供Continuing Pose Bias和丰富debug；本change把双脚protected contact、segment horizon、continuation graph作为硬准入，并保存每个reject与plan分项，能够显式捕获exact replay artifact。
- UE Game Animation Sample仍通过AnimBlueprint functions和Chooser维护IsMoving/IsStarting/IsPivoting等视觉状态；本change的正式MM配置以稳定provider、编译Search Domain和trajectory query完成sample选择，不强迫业务Graph复制这些视觉阶段。

本change不声称超过UE的完整资源生态、Orientation/Stride Warping、Traversal、Montage工具或实验性多角色交互。那些能力不属于本次Grounded Locomotion Pose Source，不能用名称包装成已实现。

## Breaking Changes

- `CharacterAnimationPresentationProfile`增加条件式Motion Matching Profile引用；只有Pose Graph声明可达MM provider的配置必须提供完整引用并生成MM Projection payload，未声明MM provider的配置不生成该payload。
- `PresentationPoseSourceSample`增加独立provider/player与Pose Selection identity；任何按PlaybackId判断MM continuation的旧实现删除。
- MM Selection连接局部Inertialization时必须提供完整endpoint policy；连接BlendStack时必须提供完整可达CrossFade pair。不得由MM创建私有fade、默认transition或私有惯性器。
- 已声明MM provider时，数据库Artifact与Projection payload缺失、stale、Rig/Schema/Foot Analysis identity不匹配会使Build直接失败；不得改用Timeline、旧Locomotion或默认数据库。
- 旧diagnostics中把Marker Sync、Motion Warp或状态切换误称为MM的占位口径删除，统一读取正式MM snapshot。

## Out Of Scope

- 不让动画root motion、selected clip速度或pose correction修改Gameplay Body、CharacterController、Fixed KCC、DotRecast或WorldSolver结果。
- 不实现Stride Warping、Orientation Warping、Slope Warping、Distance Matching、Root Offset Bone或Motion Warping替代品。
- 不把Attack、Dodge、Hit Reaction、Traversal、Vault、Climb或Airborne第一版迁入MM；它们继续使用正式Timeline/Action Pose Source。
- 不实现UE实验性的多角色Interaction Motion Matching；当前项目命中、目标registry与跨角色GameplayResult尚未闭环，不能由Presentation先行旁路。
- 不把Lyra Foot Plant current trace/smoothing、PredictiveFootPlacementModifier future world-query result、FullBodyIK结果或Scene Physics查询写回MM candidate selection。
- 不建立运行时数据库构建、动态Clip扫描、目录约定、clip名Tag、Humanoid bone fallback或stale Artifact fallback。
- 不自动镜像动画，不从in-place移动Clip猜测或合成root trajectory，不在MM内实现通用DCC清洗/重定向烘焙；首版只接受通过显式Humanoid Avatar retarget或Exact Generic Rig合同后能在目标Sampling Rig上得到合法姿势与root trajectory的Clip。
- 不新增BTSMTL Gameplay Graph MM状态、Timeline Motion Matching Track或独立Animation Blueprint解释层；MM Selection只存在于PoseState内部typed Player输入。
- 不修改Corin的Graph、StateMachine、Timeline、Profile、Definition、Projection、Prefab、动画、Marker Group或transition；不从Corin借用Rig、Foot Analysis或Clip作为验证占位。
- 不增加测试或人工验证task，不运行Unity batchmode。

## Stop Conditions

- 如果state-local sample与绑定Player仍无法通过dense source index、Player NodeId、lease和Selection Generation表达typed discontinuity，停止并先修正公共Pose source ABI，不伪造作者字符串、Gameplay Playback、Blend Entry或在MM内部连续化。
- 如果Motion Matching必须读取InputAction、MonoBehaviour Update、Scene Transform或Network packet才能获得轨迹，停止并补正式Presentation trajectory source，不直接旁路。
- 如果数据库Build需要在Inspector repaint、asset import、domain reload或普通Character Compile中隐式运行，停止并保留显式重操作入口。
- 如果导入Clip只能通过骨骼名猜测、场景Animator、隐式Avatar替换或运行时retarget fallback才能在目标Rig播放，停止并要求作者修正Source Set或Unity import配置。
- 如果业务Coverage Requirement声明非零移动范围但导入Clip在目标Rig上的root trajectory仍是in-place，停止并要求正式root-motion内容或后续独立的trajectory authoring能力，不在Builder推测位移。
- 如果Foot Analysis Artifact与MM所需脚接触语义不一致，停止并升级唯一Foot Analysis合同，不在MM目录复制Analyzer。
- 如果独立验证配置缺少完整Rig、Foot Analysis、动画Clip、Database segment或正式provider binding，停止内容接线并等待完整配置；不得复用Corin资产、生成占位动画或建立临时路径。
- 如果无合法candidate只能通过旧Idle、旧state machine、bind pose或全库fallback维持输出，停止并修正Database coverage/Initialization eligibility；正式结果必须是Selection或typed Invalid。
- 如果短时序plan需要按帧毫秒预算提前退出而造成相同query不同结果，停止并收紧Artifact规模或分区，不引入时间驱动非稳定搜索。

## Success Criteria

- Corin全部现有Graph、StateMachine、Timeline、Profile、Definition、Projection、Prefab与动画引用在本change前后保持不变，且不新增MM Profile或MM provider。
- 未声明MM provider的正式配置不引用MM Profile、不生成MM Projection payload且不实例化MM Runtime；声明MM provider的独立正式验证配置必须完整装配并明确失败，不能回退。
- Program、WorldSolver与Committed/Selected Body继续唯一决定真实位移；MM只选择Presentation pose，Runtime没有`Animator.deltaPosition`或deltaRotation回写。
- Motion Matching Profile、Database、Schema、Policy、Rig、Foot Analysis、Artifact、Projection与Clip binding拥有完整可追踪identity，任何stale或错配都明确失败。
- 重数据分析只由显式Build触发，Character Build只消费现有合法Artifact。
- 导入或重导入FBX、选择资源、打开Inspector、修改Source Set和普通Character Compile都不执行采样；只有显式`Build Source Set Foot Analysis`执行脚分析，只有显式`Build Motion Matching Database`执行MM重分析。
- Source Set精确追踪Clip GUID/local file id、import dependency、Avatar/Generic rig兼容性、Motion Root与目标Rig identity；重导入后旧`.mmdb`只变为Stale并等待作者主动重建。
- 本地已接受意图与Remote selected body都降低为带source identity、confidence和tolerance的同一Trajectory Envelope，不直接读取InputAction或Transform。
- Initialization、Search Domain、segment boundary、continuation和protected foot contact全部在代价计算前准入；无合法candidate产生typed Invalid，不回退旧动画。
- 分层搜索对完整合法候选保持精确Top-K语义、稳定tie-break和固定数据访问上界；不按运行帧时长提前退出。
- Top-K候选经过短时序plan评估，Selection能解释当前成本、未来horizon成本和continuation/jump原因。
- 同一PoseState relevance continuation保持Selection identity并只更新sample；pose jump提升Selection Generation并由SelectedPosePlayer发布discontinuity。推荐State subgraph使用局部Inertialization；若作者显式选择BlendStack，则只复用该节点的CrossFade、Stored Pose和retirement。
- FullBody Action覆盖期间Locomotion PoseState MM继续更新绑定Player的Pose和history，Action退出后Slot直接回到当前基础Pose。
- Rollback branch replacement、Selected stream reset、Presentation reset和Projection replacement会原子清空query history、selection plan与contact protection，并在新分支执行Initialization Query。
- MM Foot Feature沿普通Pose Value的实际脚骨骼贡献进入显式FootGrounding与可选PredictiveFootPlacementModifier；Lyra current grounding、contact/anchor、pelvis resolve、Predictive Extension、FullBodyIK和world-query result不反向影响candidate选择。
- Diagnostics能够显示完整query envelope、stage candidate count、reject reason、Top-K cost、plan cost、selected sample、contact protection、selection generation、Player discontinuity、可选Stack entry或Inertialization residual与reset reason。
- 显式Search Replay Artifact能在相同Database identity上复现query、候选顺序、reject、cost与最终Selection；identity不匹配时拒绝重放。
