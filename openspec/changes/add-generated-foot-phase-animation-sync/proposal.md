# Change: 新增生成式左右脚相位动画同步

## Why

当前动画同步已经具备完整的MarkerGroup、leader/follower、Finite/Cyclic拓扑、source-local relation与共同可见期持续求值，但真正的时间映射仍然是线性的：Runtime先计算leader在当前有向Marker区间中的时间比例，再把同一比例直接乘到follower区间时长。

这能保证`RightFootContact -> LeftFootContact`不会映射到相反脚区间，却默认Walk与Run在区间内按相同速度完成压脚、离地、前摆、越身和落地。Corin当前Walk Loop与Run Loop各只有左右脚接触锚点，实际脚轨迹、支撑时长和摆动速度并不按相同时间比例推进，因此Transition当帧开始后仍会混合到左右脚位置、速度或接触状态差异较大的姿势。

给PoseState Transition增加“等待下一次落脚”条件只能推迟问题并增加输入响应延迟；Predictive Foot Placement与FullBodyIK也不应修复基础动画时间不匹配。需要在现有source-local Marker同步链内建立真正的时间对应关系：以Marker确定左右脚语义区间，以现有Foot Analysis生成的双脚运动数据编译leader时间到follower时间的单调映射，Transition仍在业务条件成立时立即开始。

## What Changes

- 在现有MarkerGroup作者合同中新增显式`AnimationSyncTimeMapping`：`MarkerSegmentFraction`用于通用Marker线性同步，`GeneratedFootPhase`用于基于左右脚分析数据的时间扭曲。`None`不携带时间映射；MarkerGroup不得保留`Unspecified`。
- `GeneratedFootPhase`继续使用现有SyncGroupId、Finite/Cyclic topology、SyncRole与ordered Point Marker。Marker只确定有向区间、occurrence与左右脚顺序，不直接成为plant/contact运行时真相。
- 现有Animation Foot Analysis artifact增加Editor-only同步描述，保存同一采样时钟下左右脚root-local sole平面位置、校准高度、局部速度与Plant Confidence。它不成为可编辑FootPhase资产，也不作为Runtime FootGrounding的第二份contact来源。
- Character Build为每个实际可达的PoseState source relation、Action source relation和Blend Space固定Phase Reference到Dynamic Sample关系编译`AnimationFootPhaseTimeWarpPlan`。每个计划按精确leader/follower artifact、marker segment occurrence与算法身份保存固定容量单调映射表。
- Editor-only warp compiler在对应Marker区间内对双脚描述执行确定性单调序列对齐。映射固定`0 -> 0`与`1 -> 1`，不得倒退、跨Marker区间或交换左右脚；无法得到合法映射时Build失败，不得退回线性segment fraction。
- Runtime保留现有relation cursor、cycle展开、Finite occurrence选择、source retention与continuation anchor。`MarkerSegmentTimeMapper`先定位leader/follower occurrence，再按显式时间映射策略得到follower fraction；共同可见期间每帧继续求值。
- 持续Pose source不再从Motion resolve后的获胜operation反查Locomotion elapsed。每个实际Movement producer必须从同一权威simulation tick派生并随Motion Contribution原子提交`CommittedMovementPlaybackClock`；Locomotion Input Motion使用自身operation生命周期，MovingTurn Timeline Motion使用Timeline owner、generation与连续playhead。Sprint、Attack、Dodge继续使用独立Action playback clock。
- Presentation只消费已提交Movement clock锚点并在相邻authority tick之间投影，不读取Gameplay状态或权威tick服务，不把Sequence Player、Marker relation或effective time写入snapshot/network。rollback替换分支时按完整owner、generation与authority tick重新锚定。
- Sequence Player进入source relevance时锁定精确clock owner与generation；保留的outgoing source继续消费自己已锁定的clock identity。`GeneratedFootPhase`只在raw source clock之上生成effective time，不拥有、归零或改写Movement clock。
- PoseState Transition Rule、Routing、blend duration与blend curve不增加脚步条件。Transition target Ready后仍立即提交，Gameplay movement不等待Marker边界。
- Blend Space保留固定Phase Reference。`MarkerSegmentPhase`使用现有线性marker fraction；`GeneratedFootPhase`使用Reference到各Dynamic Sample的编译warp plan；参数权重变化不得动态更换phase leader。
- Corin Walk Loop与Run Loop的`Locomotion.Gait`迁移到`GeneratedFootPhase`，继续复用各自现有左右脚接触Marker和唯一Foot Analysis Source。Idle、Start、End与MovingTurn没有完整同步覆盖时保持明确`None`，不伪造Marker或左右脚版本。
- Runtime、Pose Graph Preview、Timeline Preview、Blend Space Preview与Live Debug显示mapping policy、leader/follower occurrence、leader segment fraction、warped follower fraction、warp plan identity和最终effective time，并继续只读取正式Projection。
- 删除把`leader.Fraction`无条件直接用于follower的单一路径。旧Foot Analysis artifact、旧Projection payload与缺少显式time mapping的MarkerGroup authoring直接失效，不保留兼容reader、默认策略或运行时补建。

## Capabilities

### Added

- 无新增独立能力目录；生成式相位同步进入现有Animation Foot Analysis、Marker Sync、PoseState source与Projection能力。

### Modified

- `character-animation-foot-analysis-artifact`：增加只用于时间对齐的双脚同步描述，以及从精确artifact编译单调warp plan的合同。
- `character-animation-presentation-authoring`：MarkerGroup owner必须显式选择时间映射策略，Projection保存对应固定计划。
- `character-animation-layer-runtime`：source-local relation按显式策略持续映射effective time。
- `character-animation-selection-runtime`：PoseState与Action source同步只消费编译计划，不在Runtime搜索脚姿势。
- `character-presentation-pose-graph`：Transition继续从两侧source binding推导同步，同时引用编译warp plan。
- `character-animation-pipeline`：source-local effective time解析升级为marker区间加可选生成式warp。
- `character-presentation-interpolation`：Movement producer随motion原子提交权威tick派生的source-owned播放时钟，Presentation只重采样并按完整identity处理rollback重基线。
- `character-state-timeline-authoring-loop`：Corin Walk/Run正式改用生成式左右脚相位同步。
- `btsmtl-timeline-editor-preview`：AnimationTrack编辑、Authoring Preview与Live Debug显示同一显式Time Mapping和运行时warp结果。
- `agent-character-controller-synthesis`：Agent Document分别在有限Action Track与持续Pose source binding维护唯一Marker Sync owner，并完整读写Time Mapping。
- `btsmtl-agent-authoring-document-sync`：Document v3通过共享Capability读写Profile source binding同步字段，禁止把策略复制到Transition或暴露generated warp payload。

## Current Spec Comparison

- current `character-animation-layer-runtime`、`character-animation-selection-runtime`、`character-animation-pipeline`与`character-presentation-pose-graph`都明确要求按有向Marker pair和`segment fraction`映射。该线性比例正是本change要替换的步态同步行为；通用非步态Marker关系仍可显式选择`MarkerSegmentFraction`。
- current `character-animation-layer-runtime`已经要求Walk切Run时Gameplay movement不等待Marker边界。本change保留该要求，不新增Transition脚步门禁。
- current `character-animation-presentation-authoring`禁止把Marker写入Transition、Rule、Blackboard、ActionProfile、独立FootPhase资产或Pose Graph MarkerSync节点。本change保留唯一owner，只在同一MarkerGroup owner增加时间映射策略；生成描述进入现有Foot Analysis artifact，编译结果进入Projection。
- current `agent-character-controller-synthesis`仍把全部Marker Sync可写数据限定在AnimationTrack，和已生效的持续Pose source由Profile binding拥有的合同冲突。本change明确修正该旧口径：有限Action由AnimationTrack拥有，持续Pose由Profile binding拥有；两者都由同一typed capability表达，Transition与generated Projection不可编辑。
- current `btsmtl-timeline-editor-preview`与Document v3只表达mode、group、topology、role、marker和单一segment fraction。本change补齐Time Mapping、leader fraction与warped follower fraction，避免人工编辑、AI编辑和运行时诊断看到不同合同。
- current `character-animation-foot-analysis-artifact`把contact Marker candidate定义为Editor session瞬时数据。本change不把候选Marker写入artifact；新增的是无作者语义的连续双脚同步描述，二者不能混为一份数据。
- current `character-foot-placement-presentation`要求FootGrounding在最终effective sample time读取Foot Feature，且不得把MarkerId作为plant/contact真相。本change只改变effective time如何得到，不改变FootGrounding、PredictiveFootPlacementModifier或FullBodyIK的输入与owner。
- active `replace-pose-ik-with-finalik-full-body-solver`正在把Foot Analysis升级到Rig v4、Calibration v4和动作级Landing Event。本change必须在其唯一artifact schema与Analyzer上继续增加同步描述，不能建立第二Analyzer、第二artifact或旧格式reader。
- active `replace-pose-ik-with-finalik-full-body-solver`已经为Start、End与MovingTurn补充正式Marker覆盖；本change不得继续把这些资源描述为“无Marker”。它们是否参与哪条relation必须读取当前authoring，Movement clock所有权不得由MarkerGroup或资源显示名推断。
- active `add-character-presentation-blend-space`当前把`MarkerSynchronizedPhase`定义为线性segment fraction。该定义和已实现`CharacterAnimationBlendSpacePhaseMapper`与本change冲突；应用本change时必须把该active change重基线为显式`MarkerSegmentPhase | GeneratedFootPhase`，共用同一warp compiler和Projection plan，不能保留另一套Blend Space脚步同步算法。
- active Motion Matching能力已经按Pose、Trajectory和Foot Feature成本选帧，不参加Marker relation。本change不把MM source接入Marker Foot Phase，也不修改其Selection或Database职责。

## Dependencies And Sequencing

1. 以active `replace-pose-ik-with-finalik-full-body-solver`当前Rig v4、Calibration v4、Foot Analysis artifact和Projection payload为唯一基线；实现时只提升一次最终artifact format、algorithm identity与Projection schema。
2. 在修改Blend Space phase mapper前，先重基线active `add-character-presentation-blend-space`的时间策略文档与代码，确保Reference Sample到child sample复用同一warp plan，不并列保留线性脚步专用实现。
3. 先完成authoring schema和旧数据原子迁移，再生成Foot Analysis artifact与warp plan；Runtime不得在缺少新Projection时运行时补建。
4. Runtime、Preview和Diagnostics最后统一切换到新plan，删除无条件线性follower fraction路径后再发布Corin生成产物。

## Deliberate Scope

- 不增加Transition等待脚落地、Foot Notify门禁或Gameplay状态延迟。
- 不改变PoseState业务条件、Transition Routing、Standard Blend、Inertialization或Blend Profile数学。
- 不让FootGrounding、PredictiveFootPlacementModifier、FullBodyIK、Motion Matching或Animancer选择同步时间。
- 不创建独立FootPhase ScriptableObject、Timeline FootPhase Track、Blackboard脚变量、pair table作者资产或Pose Graph MarkerSync节点。
- 不通过本change补造缺失的LeftLead/RightLead Start、Stop或Turn动画。单一有限动画缺少相反支撑脚内容时仍是素材覆盖问题。
- 不把`GeneratedFootPhase`失败回退到`MarkerSegmentFraction`、normalized time、Animancer自动同步或上一帧effective time。
- 不运行Unity batchmode，不在proposal阶段修改实现代码或生成产品。

## Breaking Changes

- 所有`MarkerGroup` owner必须显式保存`AnimationSyncTimeMapping`。旧序列化数据中的缺省值视为`Unspecified`并阻止发布；迁移必须把每个现有owner明确写为`MarkerSegmentFraction`或`GeneratedFootPhase`。
- Animation Foot Analysis artifact format与algorithm identity提升；旧artifact直接Stale，不提供兼容reader。
- Presentation Projection schema、ContractHash与ProjectionRevision提升。旧Projection不能被Runtime或Preview消费。
- Corin Walk/Run由线性Marker fraction迁移到`GeneratedFootPhase`；Start、End、MovingTurn与Action保持当前显式authoring，不得因存在Marker而自动加入该策略。
- `SimulationLocomotion`时钟枚举与Motion resolve后反查Locomotion operation状态的字段删除，统一迁移为`CommittedMovement`与原子`CommittedMovementPlaybackClock`。缺少合法clock的Movement contribution直接失败，不保留presentation delta或零值回退。
- `MarkerMappedTime`与相关diagnostics必须区分leader fraction和warped follower fraction；旧只含单一segment fraction的快照合同删除。
- Blend Space现有`MarkerSynchronizedPhase`名称和“唯一线性marker策略”口径必须清理为显式通用线性策略与生成式脚相位策略，不保留旧枚举兼容值。

## Success Criteria

- Walk→Run与Run→Walk在Transition条件成立的当帧开始混合，同时target effective time由同一Marker区间内的双脚生成式warp决定，而不是直接复制leader时间比例。
- 共同可见期间每帧持续映射，target time保持有限、单调、marker边界连续，并保持Finite/Cyclic occurrence与cycle语义。
- 同一warp compiler和plan格式服务PoseState relation、合法Action relation与Blend Space固定Reference关系，没有第二套脚步对齐算法。
- Runtime不读取Library artifact、不采样AnimationClip、不执行动态规划，也不根据当前骨骼或IK结果重新搜索时间。
- FootGrounding、PredictiveFootPlacementModifier和FullBodyIK继续在映射后的同一effective sample time消费原有Pose与Foot Feature，不拥有同步决策。
- Corin作者数据、Foot Analysis artifact、Projection、Runtime、Preview与Diagnostics形成一条正式链，旧线性Locomotion配置和旧generated产品被删除。
- Locomotion Input Motion与MovingTurn Timeline Motion都能从同一权威simulation tick提交各自连续Movement clock；Sprint/Attack/Dodge不进入Movement clock。Sequence Player只在owner或generation真正改变时重基线，同一identity内不再出现elapsed回退。
- rollback重放只重新生成并提交最终simulation分支的Movement clock锚点；Presentation状态不进入rollback snapshot/network，渲染帧只在相邻committed锚点之间连续投影。
