# Design: 角色表现 Blend Space

## Context

当前最终目标链路是：

```text
BTSMTL / Motion Matching
  -> AnimationSelectionFrame
  -> AnimationSelectionInput
  -> 可选 MarkerSync
  -> SelectedPosePlayer 或 BlendStack
  -> 可选 Inertialization
  -> Pose composition / ModifyBone
  -> FootPlacement
  -> OutputPose
```

现有Animancer采样后端已经使用`ManualMixerState`和多个`ClipState`按`ClipSamplePlan.Weight`采样，因此“多个clip按权重生成真实骨骼姿势”已经存在。缺失的是一层正式模型：如何从Speed、Direction等连续参数计算样本权重，如何把同一phase映射到每个样本，如何把样本feature按同样权重传播，以及如何让Editor、Preview、Runtime和Agent Snapshot看到同一事实。

## Goals

- 用UE容易理解的Blend Space / Blend Space Player概念表达连续参数驱动的动画样本混合。
- 保持本项目的Selection、Projection、Pose Graph和显式连续性节点为唯一正式链路。
- 复用Animancer可见权重算法行为和现有Playable采样，不复制骨骼混合后端。
- 让所有authoring输入编译为target-neutral、固定大小、可校验、可预览的数据。
- 让姿势、Pose Parameter、Foot Analysis feature和source contribution使用同一组最终样本权重。
- 提供足够完整的资产编辑、Details、Preview、Live和References，不只增加一个字段列表Inspector。

## Non-Goals

- Unity AnimatorController、BlendTree、Animator StateMachine或AnimatorControllerPlayable集成。
- UE Animation Blueprint Event Graph、Montage、Sync Group自动leader、Notify或Root Motion业务决策。
- Direct blend、Simple Directional、嵌套Blend Space或运行时动态样本集合。
- Motion Matching查询、轨迹预测或候选打分。
- 跨来源CrossFade、Stored Pose、Inertial residual、FootPlacement和最终Animator写回。
- Agent对Presentation资产的写入。

## Concept Mapping

| 本项目概念 | UE接近概念 | Unity/Animancer基础 | 本项目边界 |
|---|---|---|---|
| `CharacterAnimationBlendSpaceAsset` | Blend Space资产 | Animancer Mixer阈值/位置数据 | 唯一保存轴、样本、phase和feature authoring |
| `BlendSpacePlayer` | Blend Space Player节点 | ManualMixerState / AnimationMixerPlayable | Selection驱动的显式Pose source节点 |
| `Linear1D` | Blend Space 1D | `LinearMixerState` | 一维相邻样本插值 |
| `FreeformCartesian2D` | Blend Space二维Cartesian | `CartesianMixerState` | 平移速度向量空间 |
| `FreeformDirectional2D` | Blend Space二维Directional | `DirectionalMixerState` | 方向与幅值空间 |
| `BlendStack` | Blend Stack / temporal transition | 不由Mixer负责 | 只保留跨来源时间历史 |
| `MarkerSync` | 显式source handoff同步 | 项目现有Marker算法 | 只处理来源间raw-to-effective phase |

名称只表达接近的学习概念，不宣称内部等同UE。特别是项目的world-aware phase仍叫FootPlacement阶段，不叫Post Process Anim Blueprint。

## Responsibility Model

| 层 | 输入 | 唯一职责 | 输出 |
|---|---|---|---|
| BTSMTL / MM | Gameplay事实或搜索结果 | 选择producer/source并提交raw clock | AnimationSelection |
| MarkerSync | source usage与source marker schema | 来源切换时映射canonical phase | effective Selection page |
| BlendSpace weight evaluator | 编译样本表与轴值 | 计算确定性normalized weights | weighted SampleId列表 |
| BlendSpace phase mapper | canonical phase与样本phase binding | 得到每个样本的effective clip time | ClipSamplePlan time |
| Animancer backend | ClipSamplePlan | 创建/复用Playable并采样骨骼 | sampled source pose |
| BlendSpacePlayer | 上述三项结果 | 聚合source-local Pose、curve、foot feature | Pose Value + discontinuity |
| Inertialization | 单Pose discontinuity | residual/rebase | 连续Pose Value |
| BlendStack | 多source history | CrossFade/Stored/release | 跨来源Pose Value |
| FootPlacement | composition后Pose与foot contribution | world query与脚部修正 | final pose |

## Decision 1: 新增BlendSpacePlayer，不复用BlendStack

`BlendSpacePlayer`是第三种显式Player，与`SelectedPosePlayer`和`BlendStack`并列。它消费Selection是为了沿用producer lifecycle、raw time、loop、play rate、source identity和Projection绑定；参数输入只决定同一BlendSpace资产内的样本贡献。

它不保存旧source entry。Selection source identity或generation发生离散变化时，节点发布typed discontinuity。作者需要平滑时显式连接局部`Inertialization`。需要保留多个旧来源并CrossFade时，仍使用独立BlendStack业务分支，不能要求BlendSpacePlayer暗中长成第二个Stack。

业务取舍：这样不能用一个节点同时表达任意BlendSpace来源历史，但移动主链通常长期保持同一个Locomotion Blend Space；职责清晰比一个全能播放器更重要。

## Decision 2: 资产引用只保存在Profile producer binding

`CharacterAnimationPresentationProfile`新增`AnimationPoseSourceKind.BlendSpace`。每条binding使用稳定producer identity指向一个`CharacterAnimationBlendSpaceAsset`。Pose Graph的BlendSpacePlayer不保存资产引用，只声明参数端口和节点策略。

Compiler从该节点可达的Selection endpoint解析全部producer binding，并要求：

- 全部source kind均为BlendSpace。
- 全部资产Rig与Pose Graph Rig完全一致。
- 全部资产拥有相同轴数量、ParameterId、值类型和单位。
- 同一个producer只有一个BlendSpace binding。

这样Profile是唯一资源绑定真相，节点负责“怎么求值”，不会出现节点选A资产而Profile又选B资产。

## Decision 3: 正式模式目录只有三种

### Linear1D

- 编译时按position升序保存稳定样本顺序。
- 重复position非法。
- 参数低于/高于范围时夹到首/尾样本。
- 区间内只激活相邻两个样本，按线性比例归一化。

### FreeformCartesian2D

- 使用Animancer `CartesianMixerState`可见语义作为行为基线。
- 输入被轴范围验证后进入编译好的Cartesian求解数据。
- 重合坐标、退化关系和非有限值编译失败。
- 输出只包含正权重样本并按稳定SampleId次序归一化。

### FreeformDirectional2D

- 使用Animancer `DirectionalMixerState`可见语义作为行为基线。
- 方向与向量长度共同参与贡献；零向量样本必须唯一。
- 同方向同半径重复样本、不可区分方向和非有限值编译失败。
- 输出只包含正权重样本并确定性归一化。

实现时把算法移入纯C# `BlendSpaceWeightEvaluator`，输入只允许编译数据和数值参数，输出只允许预分配weight page。Evaluator不创建Playable、不读取Time、不引用AnimationClip、不访问authoring asset。

不提供Direct，因为它不是参数空间求解，显式BlendPose已经能表达；不提供SimpleDirectional，因为当前移动业务和Animancer公开对应算法不要求第四种重叠模式。

## Asset Model

`CharacterAnimationBlendSpaceAsset`保存：

- `BlendSpaceId`、content revision、Rig identity。
- `BlendSpaceMode`。
- X轴和可选Y轴：稳定ParameterId、显示名、单位、最小值、最大值。
- `BlendSpacePhasePolicy`。
- 稳定Sample列表。
- source-local Pose Parameter policy表。
- Editor Preview配置，只用于authoring，不进入runtime选择逻辑。

每个Sample保存：

- 稳定`BlendSpaceSampleId`。
- AnimationClip精确引用和clip content identity。
- 1D或2D坐标。
- `DynamicCycle`或`StationaryPose`角色。
- StationaryPose使用的固定normalized sample time。
- DynamicCycle的canonical marker binding或normalized phase binding。
- 对应Foot Analysis source identity。

样本不保存Runtime weight、当前time、Playable、Transform或Animator state。

## Time And Phase

资产必须显式选择一种策略：

### SharedNormalizedPhase

- Selection raw continuous time、loop和play rate形成canonical normalized phase。
- 每个DynamicCycle样本按自己的clip length映射同一normalized phase。
- StationaryPose始终采样其固定normalized time。
- 适用于作者已经对齐周期、或中心静止姿势与循环样本混合的资产。

### MarkerSynchronizedPhase

- 资产指定一个稳定Phase Reference Sample；它必须是DynamicCycle。
- 参考样本的marker序列把raw/effective source time转换为canonical marker segment与segment fraction。
- 所有DynamicCycle样本必须提供相同MarkerId循环拓扑，但各marker时间可以不同。
- 每个样本按自己的marker segment映射effective clip time。
- StationaryPose不需要marker，始终采样固定时间，也不得成为Phase Reference。
- Marker缺失、顺序不一致或reference失效时编译失败，不回退SharedNormalizedPhase。

参数变化不会动态选最高权重样本作为phase leader。固定Phase Reference保证速度或方向跨区域时步态phase不因leader切换跳变。该选择少了UE式动态leader自由度，但更容易诊断，也与项目显式MarkerSync原则一致。

外部MarkerSync处理BlendSpace source与其它source之间的handoff；内部phase mapper处理一个BlendSpace source内的child sample。二者共享编译marker schema和phase数据类型，但状态所有者不同，不互相扫描。

## Pose Graph Contract

`BlendSpacePlayer`端口：

- 一个必需`AnimationSelection`输入。
- 一个必需typed X Parameter输入。
- 二维模式增加一个必需typed Y Parameter输入。
- 一个Pose输出。
- 一个typed Pose Discontinuity输出。

节点字段只保存稳定NodeId、availability policy和允许的参数越界策略。资产和轴ParameterId来自可达source binding，不能在节点重复填写。

Validator拒绝：

- 非BlendSpace selection。
- 缺失或多余的轴端口。
- 参数类型、单位或ParameterId不一致。
- 一个输出被当成Selection或普通标量连接。
- source资产Rig、模式接口或Projection revision不一致。
- 隐藏Player、隐式Inertialization或图外fallback。

## Compiler And Projection

Projection Compiler按以下顺序工作：

```text
reachable Selection endpoint
  -> Profile BlendSpace binding
  -> asset structural validation
  -> clip / marker / foot artifact resolution
  -> weight solver compilation
  -> phase table compilation
  -> pose parameter policy compilation
  -> fixed workspace allocation
  -> PosePlan BlendSpacePlayer instruction
```

Projection payload只保存Runtime需要的数据：

- BlendSpace identity/revision/Rig。
- mode和dense axis contract。
- stable sample table与clip resource binding。
- 预计算weight solver data。
- phase reference和dense marker phase table。
- Foot Analysis artifact bindings。
- Pose Parameter policy与dense channel mapping。
- 最大active sample数、ClipSamplePlan workspace offset和diagnostic source map。

Runtime不得读取ScriptableObject、AssetDatabase、Timeline authoring、AnimationTrack marker或Profile。

## Runtime Flow

每帧一个BlendSpacePlayer执行：

1. 从同帧Selection cache读取有效BlendSpace source、raw/effective phase与lifecycle。
2. 从compiled parameter page读取X/Y；参数不可用时按节点Require/AllowEmpty合同失败，不使用旧值。
3. Weight evaluator写入预分配weight page。
4. Phase mapper为正权重样本写入effective time。
5. 构造稳定次序的ClipSamplePlan并交给现有Animancer采样后端。
6. 用同一weight page聚合Pose Parameter、Foot Analysis feature和source contribution。
7. 发布普通Pose Value和source discontinuity。

权重必须满足：有限、非负、稳定次序、总和为1。数值误差在唯一normalization pass收口；结果为空、NaN或退化时发布typed runtime failure，不保留上一帧结果。

## Pose Parameter And Foot Feature Aggregation

资产为每个source-local ParameterId保存显式策略：

- `RequireAllSamplesWeighted`：全部正权重样本必须有值，然后按样本权重合成。
- `WeightedAvailableSamples`：只对有值样本重新归一化；必须在资产中逐ParameterId明确选择。
- `Unavailable`：该BlendSpace不发布该参数。

没有全局默认策略。未知ParameterId或policy缺失编译失败。

每个样本的Foot Analysis feature按其effective sample time读取，再按姿势相同权重聚合。进入后续Blend、LayeredBoneBlend或BlendStack时，现有source contribution继续乘上外层权重；FootPlacement只读取最终实际脚部贡献。BlendSpace不得自己执行射线、plant锁定、pelvis或IK。

## Authoring Workspace

Blend Space使用Character Animation Authoring Workspace的正式外壳：

- Navigator：资产、轴、Sample、编译产物层级。
- Canvas：1D刻度线或2D参数空间，显示采样点、当前preview落点和有效贡献连线。
- Details / Authoring：模式、轴范围、phase策略、sample clip、位置、角色、marker、Foot Analysis与参数策略。
- Details / Live：当前参数、SampleId、weight、phase、effective time、feature availability和runtime revision。
- Details / References：Profile producer binding、Pose Graph BlendSpacePlayer、Rig、clip、artifact与Projection引用。
- Bottom Dock：Preview controls、compile diagnostics、Pose Watch和reference problems。

拖动样本、编辑轴或marker只修改authoring并标记stale；只有显式Compile/Build发布Projection。Preview在stale时明确拒绝或显示旧revision，不自动build。

## Preview And Diagnostics

Preview必须构造正式AnimationSelection和typed Parameter page，执行相同CharacterPresentationPosePlan、BlendSpace weight evaluator、phase mapper、Animancer sampler与feature aggregator。

Diagnostics按NodeId和SampleId输出：

- selection/source identity与generation。
- asset identity/revision和Projection revision。
- X/Y原值、range处理结果和parameter availability。
- active sample、weight、canonical phase、effective time。
- Pose Parameter和foot feature来源。
- downstream discontinuity、Inertialization和FootPlacement状态。

Live Debug只读取Runtime Snapshot，不重新计算权重。

## Agent Boundary

Agent v17 CharacterController Snapshot的Presentation只读摘要增加：

- BlendSpace asset identity、revision、mode、axis ParameterId和sample count。
- producer到BlendSpace source binding。
- BlendSpacePlayer NodeId、输入ParameterId和Projection compile status。
- stale、missing artifact、marker topology、Rig或parameter mismatch诊断。

Snapshot不输出每个clip的generated Foot Analysis payload，不输出Runtime weight/time，也不把BlendSpace Sample伪装为Timeline Clip。Patch catalog、lowerer、handler、validator和MCP action不增加BlendSpace mutation。正式修改入口只有Character Animation Authoring Workspace和Presentation Authoring Service。

## Corin主图与独立演示边界

当前Corin主图目标：

```text
BaseLocomotion AnimationSelectionInput
  -> MarkerSync
  -> SelectedPosePlayer
  -> Inertialization
  -> Base pose composition
```

Idle、WalkStart、WalkLoop、RunStart、RunLoop、RunEnd与MovingTurn分别绑定自己的Timeline source。这样Gameplay状态选中的动作阶段能够直接进入表现链，当前双端帧同步场景不依赖尚未具备的八向样本。

后续Blend Space演示使用独立CharacterPipelineDefinition、AnimationPresentationProfile和Pose Graph：

```text
Demo BaseLocomotion AnimationSelectionInput
  -> MarkerSync
  -> BlendSpacePlayer(Speed，必要时LocalVelocityX/Y)
  -> Inertialization
  -> Demo base pose composition
```

演示图创建前必须具备完整样本、统一Rig、明确phase角色和正式Motor参数合同。演示图的全部可达producer必须一次绑定合法BlendSpace source；不得让同一BlendSpacePlayer接收Timeline或MotionMatching普通source，也不得把演示轴参数、资产或producer binding写回Corin主图。

## Failure Semantics

- Authoring结构错误：Compiler失败并定位AssetId/SampleId/NodeId/ParameterId。
- Artifact Missing/Stale/Corrupt：Projection不发布。
- Runtime parameter不可用：按节点availability合同输出NoPose或失败，不缓存旧参数。
- Runtime revision不匹配：角色Presentation创建失败，不读取旧Projection。
- 数值求解产生非有限或空weight：该节点typed failure，不选择最近样本作为fallback。
- Preview缺少正式Projection：停止预览并显示编译问题，不建立临时Mixer路径。

## Alternatives Considered

### 直接使用Unity BlendTree

优点是Inspector熟悉；代价是必须引入AnimatorController、另一套状态/时钟/同步与不可见Runtime算法，无法保持现有Pose Graph和Projection为唯一权威，因此不采用。

### 直接实例化Animancer MixerState作为Runtime权威

优点是代码少；代价是权重、child state和time进入Animancer对象，Preview/Projection/diagnostics和Foot feature必须反向猜状态，因此只复用算法行为和采样设施，不复用其状态所有权。

### 把Blend Space塞进BlendStack

优点是节点更少；代价是参数空间和跨来源历史共用同一个生命周期，未来任何算法或过渡变化都会修改同一模块，也无法向作者解释当前权重来自参数还是CrossFade，因此不采用。

### 让BlendSpacePlayer直接引用资产

优点是图上直观；代价是producer Profile binding和节点各有一份资源真相，Selection无法确定正式source identity，Agent Snapshot也会出现两条引用链，因此资产只由Profile绑定。

## 实施安装记录

以下记录以当前工作区实际合同为准，作为本change后续创建独立Blend Space演示配置时的唯一实施基线。

### Pose Graph与Selection合同

- `CharacterPresentationPoseOperation.PayloadVersion`为`8`，`CharacterPresentationPosePlan.SchemaVersion`为`character-presentation-pose-plan/v5`，`CharacterPresentationPosePlan.RuntimeAbi`为`character-presentation-pose-runtime/v9`。
- 阶段枚举固定为`Selection = 1`、`SourceAndNativePose = 2`、`WorldAwarePostProcess = 3`、`FinalPublication = 4`。`BlendSpacePlayer`编译到`SourceAndNativePose`。
- `BlendSpacePlayer`端口固定为必需`Selection`、必需`X`、可选`Y`输入，以及`Pose`、`Discontinuity`输出。X/Y只接受typed Parameter edge。
- `AnimationSelectionFrame.SchemaVersion`为`animation-selection-frame/v3`。`AnimationPoseSourceKind`固定为`Timeline = 1`、`MotionMatching = 2`、`BlendSpace = 3`；`AnimationPoseSourceId`由`PlaybackId + SourceKind + SelectionGeneration`组成，generation变化属于离散来源变化。

### MarkerSync与连续性职责

- `PlayerSourceUsageFrame.SchemaVersion`为`player-source-usage-frame/v1`，usage明确区分`Sample`、`HandoffReference`、`Retained`和`Release`，并保存实际Player `NodeId`；它不是按AnimationChannel猜测播放器的旁路索引。
- `AnimationMarkerSyncEffectiveSample.SchemaVersion`为`animation-marker-sync-effective-sample/v1`。MarkerSync消费Selection与精确Player source usage，只发布source级canonical/effective phase；`BlendSpacePlayer`再把该phase映射为各Sample的effective time。
- `SelectedPosePlayer`只采样单一已选择source；`BlendStack`唯一拥有多source retention、CrossFade、Stored Pose、per-bone transition和exact release；`Inertialization`唯一拥有单一最终Pose history、residual与rebase。`BlendSpacePlayer`不持有旧source、Stored Pose或inertial residual。

### Profile、Projection与采样入口

- `CharacterAnimationPresentationProfile`中的`AnimationProducerPresentationBinding`是producer资源绑定唯一入口；Blend Space只能经`CharacterAnimationPresentationAuthoringService.ConfigureBlendSpaceProducerBinding`调用`AnimationProducerPresentationBinding.ConfigureBlendSpace`写入。Pose Graph节点不保存资产引用。
- `CharacterPresentationSemanticContract.SchemaVersion`为`character-presentation-semantic-contract/v2`。Definition显式Build从semantic contract生成`ContractHash`，再把source revision、semantic hash、contract hash和独立`ProjectionRevision`原子发布到Projection；选择资产、打开窗口和编辑authoring都不得发布Projection。
- Runtime采样固定经过`ClipSamplePlan -> AnimancerPoseSamplingBackend -> ManualMixerState/ClipState`。Blend Space求解器和phase mapper先写稳定SampleId、最终weight与effective time；Animancer只应用这些结果并采样骨骼，不读取Mixer Parameter反算权重或选择phase leader。

### Foot Analysis入口

- 每个样本以`BlendSpaceBindingKey(BlendSpaceId, SampleId)`定位，expected artifact identity同时包含精确clip、analysis source、Rig和calibration身份。
- `AnimationFootAnalysisArtifactStore`是唯一artifact store；显式Foot Analysis命令生成artifact，Projection Compiler将其降低为`AnimationFootAnalysisProjectionBuildData`以及不可变Projection foot identity/feature payload。Runtime不读取artifact文件，也不按clip名或Timeline同名轨道推断样本。

### Authoring Workspace与Agent边界

- `CharacterAnimationBlendSpaceEditorWindow`复用`GraphAuthoringEditorShell`。正式页面为Navigator、1D/2D Canvas、Authoring、Live、References、Preview与Diagnostics；全部写操作进入`CharacterAnimationBlendSpaceAuthoringService`，Compile、Foot Analysis和Definition Build只由明确按钮触发。
- Preview必须选择精确场景`CharacterPipelineHost`与producer，构造正式Selection、typed Parameter page和clock，再调用同一`CharacterAnimationPlaybackRuntime`与Projection Pose Plan；不得创建临时PlayableGraph或临时编译Projection。
- Agent authoring schema为`agent-character-controller-synthesis.v17`。`AgentGraphSnapshotExporter`只导出`AgentSnapshotAnimationBlendSpace`和`AgentSnapshotAnimationBlendSpacePlayer`摘要；Patch DTO、lowerer、handler、validator和四个generic MCP action都不增加Presentation mutation。

### 分裂路径审计

- 项目正式代码与Corin controller中不存在为本能力创建的AnimatorController BlendTree或加载入口；第三方与原始美术包自带controller不属于Blend Space运行链，也没有被Projection引用。
- `AnimationBlendStackRuntime`没有Blend Space mode或axis合同；`BlendSpacePlayer`没有CrossFade、Stored Pose、旧source retention或per-bone transition字段。
- `AnimancerPoseSamplingBackend`只把`ClipSamplePlan`的clip binding、time和weight应用到`ManualMixerState` child，不读取Mixer `Parameter`。
- Runtime Blend Space resolver只从`CharacterPresentationProjection.BlendSpacePlayers`建立采样计划，不读取`AssetDatabase`、authoring `ScriptableObject`、clip名称或producer显示名。
- Marker topology缺失时返回`MissingMarkerSegment`并清空time page；数值求解失败返回typed failure，不切换到normalized phase或最近样本。
- 没有旧Blend Space Workbench、Agent Patch/MCP Presentation写入口或第二个临时PlayableGraph。
