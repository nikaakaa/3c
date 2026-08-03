# Change: 新增并分离 Character Presentation Pose Graph

## 重新基线

`refactor-animation-selection-pose-graph-boundary`把本change从“隐藏per-slot Stack之后的空间合成图”升级为完整表现图。最终Pose Graph必须显式包含Selection Input、可选`MarkerSync`、`SelectedPosePlayer`或`BlendStack`、可选局部`Inertialization`、Blend/Layered/Additive、参数解析、Modify Bone、Foot Placement和Output。固定`PoseSlotInput`、隐藏Marker Sync、隐藏Stack前置条件以及图外默认Foot Placement不再是目标架构。

本change继续拥有Pose Graph authoring、typed port、validator、compiler、runtime plan、diagnostics和Corin图资产迁移；新change拥有跨模块的Selection/Player/FootPlacement边界。两者必须按同一节点集完成，不能分别归档成两套管线。

## Why

本change批准时，角色动画把三类不同职责压在同一个 `LayerId` 和同一条播放链上：

- BTSMTL、Timeline 与 Program 用 `LayerId` 表达逻辑侧“同一时刻谁可以成为该路输出”。
- `CharacterAnimationLayerDefinition` 又用同一个 `LayerId` 保存 AvatarMask、Override/Additive、输出策略和组合顺序。
- 当时并行设计的 `refactor-animation-playback-to-blend-stack` 还计划让 Blend Stack 同时负责单路姿势的时间混合、所有 Layer 的骨骼空间合成和最终 Animator Pose。

这会让逻辑仲裁、时间混合和骨骼合成继续互相知道。批准基线中的Corin只有一个Base Layer时问题不明显；一旦同时保留Locomotion、叠加FullBody Action、加入UpperBody/Equipment，或让Motion Matching高频替换某一路姿势，就无法只替换一个职责。任何新的分层都会被迫修改BTSMTL状态选择或在现有compositor外再套第二层混合。

UE 的 AnimGraph 分层价值不在“多一张蓝图”，而在于把职责分开：StateMachine、Montage 或 Motion Matching 产生姿势；Blend Stack处理单个姿势入口的时间历史；Slot与Layered Blend Per Bone按骨骼空间组合；最终 IK在结果之后处理。本项目已经有 BTSMTL、Timeline、Projection、Animancer和Foot Placement，因此只需要补上缺失的 Presentation Pose Graph，不需要复制 UE 的 Gameplay 状态机或 Animation Blueprint runtime。

本 change 将动画链正式拆成：

```text
BTSMTL / Motion Matching Animation Selection
  -> Character Presentation Pose Graph
  -> 可选MarkerSync
  -> SelectedPosePlayer 或显式 BlendStack
  -> Blend / Layered / Additive / ModifyBone
  -> FootPlacement world-aware phase
  -> Final Animation Pose
```

## 当前实施基线

- AnimationChannel、Selection Input、显式Player、node-local Blend/Inertialization Policy、Pose Graph Plan、Rig、Projection payload、FinalAnimationPoseFrame与GraphAuthoringEditorShell已经进入正式代码链。
- Pose Graph编辑器已经按匹配ProjectionRevision读取正式runtime operation trace、source usage、Marker relation与阶段completion，不从Animancer weight反推图状态。
- Corin Animation Presentation Profile、Pose Graph、Rig Binding、Float/Fixed Program wrapper与generated Projection已经由正式authoring与Build链整体重建；旧Layer、PoseSlot、全局Blend Library与TransitionLibrary payload不是兼容输入。
- Corin资产迁移仍由本change统一记录；`refactor-animation-playback-to-blend-stack`不复制第二份迁移清单。
- 已经归档的`refactor-presentation-projection-target-boundary`只作历史追溯，不回改其proposal、design、tasks或spec delta。

## What Changes

- `AnimationChannelId`继续属于BTSMTL、Timeline、Semantic IR、Program和command，唯一表达逻辑仲裁通道；删除作为隐藏表现入口的`PoseSlotId`，Pose Graph用稳定`PoseNodeId`表达Selection Input与全部运行节点。
- 每个可达`AnimationChannelId`必须在Projection中绑定到显式`AnimationSelectionInput`；Motion Matching output绑定到`MotionMatchingSelectionInput`。Presentation不在多个channel之间重新选择赢家。
- `CharacterPresentationPoseGraphAsset`保存独立Pose Graph数据、稳定节点/端口身份、Selection Input、Player、连续性、骨骼Mask引用、Pose Parameter声明、FootPlacement与唯一OutputPose。
- Editor-only `CharacterPresentationPoseGraphCompiler`把authoring DAG、共享Rig dense bone数据、node-local Blend/Inertialization Policy、曲线解析策略和固定workspace布局编译为target-neutral `CharacterPresentationPosePlan`，并嵌入`CharacterPresentationProjection`。
- 正式Runtime节点集合为`AnimationSelectionInput`、`MotionMatchingSelectionInput`、`ProgramParameterInput`、`MarkerSync`、`SelectedPosePlayer`、`BlendStack`、`Inertialization`、`BlendPose`、`LayeredBoneBlend`、`AdditivePose`、`PoseParameterResolve`、`PoseSubgraph`、`ModifyBone`、`FootPlacement`与`OutputPose`；compiler-only `GraphInput`/`GraphOutput`不得进入Runtime Plan。节点只处理Selection、source usage、Pose、Discontinuity、Parameter与Output，不读取State、Action、Blackboard、Timeline Window、GameplayTag或业务Priority。
- `MarkerSync`只把Selection raw visual time映射为与一个Player source usage对应的effective sample page。它不计算权重、不保留source、不复制AnimationTrack上的SyncGroup/Role/Point Marker；图中没有该节点时必须使用raw time。
- `SelectedPosePlayer`只采样当前Selection并发布typed discontinuity；`BlendStack`只拥有多source CrossFade、Stored Pose、Per-Bone Blend Profile与source release；局部`Inertialization`只拥有单Pose history、residual与rebase。Runtime和Preview不得自动补建任何节点。
- Pose值同时携带dense local bone pose、命名`PoseParameterId`标量流、typed availability、continuity和可追踪source contribution。每个合成节点必须显式声明骨骼Mask、Override/Additive语义与Parameter解析策略。
- `AnimationPosePlayableGraphRuntime`使用编译后的固定拓扑和预分配workspace安排source capture、显式Player、native composition、world-aware FootPlacement与final writer，在同一正式Plan中完成实际source contribution和Animator AnimationStream写回；`FinalAnimationPoseFrame`只在FootPlacement/IK完成后发布。
- `CharacterAnimationPresentationProfile`不再保存Layer catalog、PoseSlot catalog或全局per-slot Blend Library。它唯一引用Pose Graph、node-local Policy、Animation Rig Definition、producer resource binding与Foot Analysis输入。
- 删除 `CharacterAnimationLayerDefinition`、Animancer layer index、Profile layer order、layer AvatarMask和layer blend mode旧数据；不提供旧Layer到Slot的运行时兼容或fallback。
- 抽取 `GraphAuthoringEditorShell`，复用现有BTSMTL节点编辑器的窗口、画布、搜索、复制粘贴、Undo、Inspector与只读diagnostics外壳。BTSMTL Graph与Pose Graph保留各自的数据、节点、端口、validator和compiler，不共享Gameplay `BaseNode`/`BaseEdge`语义。
- Corin正式迁移为两个逻辑通道和两条显式姿势分支：
  - `BaseLocomotion -> AnimationSelectionInput -> MarkerSync -> SelectedPosePlayer -> Inertialization`，要求合法Pose。
  - `FullBodyAction -> AnimationSelectionInput -> BlendStack`，允许Empty。
  Attack、Dodge与其它FullBody Action不再抢走Base Locomotion command；Pose Graph通过全身Mask覆盖Base，action退出时由Action BlendStack连续淡回Base。
- FootPlacement作为作者图中的显式节点降低为唯一world-aware阶段，只消费composition完成的未IK姿势和经过最终空间Mask合成的左右脚实际贡献；UpperBody或零脚权重分支不得错误稀释Foot Analysis。
- Runtime、Timeline Preview和Live Debug统一展示`AnimationChannelId -> Selection Input -> Player -> composition -> FootPlacement -> Final Pose`链路，不从Animancer state或作者图重新推导。

## Impact

### Specs

- 新增 `character-presentation-pose-graph`
- 新增 `graph-authoring-editor-shell`
- 修改 `btsmtl-graph-core`
- 修改 `btsmtl-compiled-simulation-program`
- 修改 `btsmtl-node-interruption-lifecycle`
- 修改 `character-animation-layer-runtime`
- 修改 `character-animation-pipeline`
- 修改 `character-animation-presentation-authoring`
- 修改 `character-foot-placement-presentation`
- 修改 `character-equipment-presentation`
- 修改 `character-pipeline-definition-authoring`
- 修改 `character-pipeline-runtime`
- 修改 `character-state-timeline-authoring-loop`
- 修改 `character-state-interruption-authoring`
- 修改 `character-presentation-interpolation`
- 修改 `btsmtl-timeline-editor-preview`
- 修改 `agent-character-controller-synthesis`
- 修改 `gameplay-tick-system`
- 删除或改写current specs中仍把TransitionLibrary、Animancer fade、LayerId或Equipment Required Layer视为正式权威的旧要求
- 实施完成后同步更新 `openspec/project.md`

### Code

- BTSMTL Graph Editor窗口、GraphView、搜索、Clipboard、Undo、Inspector和diagnostics overlay的通用Editor Shell抽取
- 新的Pose Graph authoring data、asset、node catalog、port policy、validator、compiler、Projection payload与Inspector
- `AnimationTrack.LayerId`、Program producer contract、presentation command、trace与snapshot迁移为`AnimationChannelId`
- `CharacterAnimationPresentationProfile`、Projection Compiler、Binding Index和generated Projection schema迁移
- `AnimationBlendStackRuntime`、Pose workspace、Animation Jobs与Animancer source sampling链路重新分责
- `CharacterSimulationPresentationRuntime`、Timeline Preview、Foot Placement输入和Live Debug链路
- Corin Graph/Timeline/Profile/Pose Graph/Blend Library/Rig Binding/generated Projection资产

### Active Change 关系

- `refactor-animation-playback-to-blend-stack`保留Stored Pose、Per-Bone CrossFade和Animancer source采样算法并迁入显式Blend Stack节点；现有Inertial数学迁入`refactor-inertial-blending-to-local-pose-node`的局部节点。跨分支mask/additive、参数解析、ModifyBone、FootPlacement与最终Pose属于本change编译的同一Pose Plan。
- `refactor-presentation-projection-target-boundary`已完成target-neutral Projection边界。本change作为其后续破坏性schema迁移，把producer contract字段从`LayerId`改为`AnimationChannelId`，并让Projection保存Selection Input binding、Pose Plan、node-local Policy与Rig payload；不得恢复任何Numeric Target依赖。
- 已完成的Marker Sync、Foot Analysis和FootPlacement算法语义保留。Marker segment算法迁入显式`MarkerSync`节点，只对其一对一Player声明的同一Animation Channel和SyncGroup真实source usage工作；Foot Analysis按effective sample并沿实际Pose贡献进入显式FootPlacement节点。
- 本change不拥有Motion Matching查询。`add-character-motion-matching-pose-source`只提交正式Animation Selection；图上的`MotionMatchingSelectionInput`决定接入位置，显式节点决定直接播放、局部Inertialization或BlendStack。MM不得建立私有fade、Stack、惯性器或第二Pose输出。

## Breaking Changes

- `LayerId` 从Timeline、Program producer contract、command、Projection binding与diagnostics删除，统一替换为`AnimationChannelId`。
- `CharacterAnimationLayerDefinition`、Profile Layer catalog、PoseSlot声明和固定Stack装配删除，统一替换为Pose Graph Selection Input、Player节点与图拓扑。
- Corin不再使用单一Base channel仲裁Locomotion与FullBody Action；现有Timeline channel数据必须正式迁移。
- Blend Stack不再写最终Animator Pose，也不再拥有跨slot Layer order、AvatarMask或Additive composition。
- Profile必须引用有效Pose Graph、节点所需全部Policy与Rig Definition；缺一项即编译或Runtime创建失败。
- Projection schema、Presentation ContractHash和ProjectionRevision提升；旧generated Projection与旧Profile数据直接失效并重建。
- BTSMTL Editor共享的是通用Editor Shell，不允许Pose Graph继承或复用Gameplay `BaseNode`、ConditionRule、BTAbortPolicy或runtime evaluation context。
- 不提供FormerlySerializedAs、旧Layer reader、默认Slot、自动Base Pose、隐藏Player、按名称匹配channel/node或Animancer Layer fallback。

## 后续动画职责重构关系

本change建立Pose Graph、typed ports、Projection和完整Pose Plan基座，但其交付拓扑仍以Animation Selection Input为持续Locomotion入口。后续`refactor-animation-control-boundaries`在该基座内增加Presentation Fact、PoseStateMachine、SequencePlayer和AnimationSlot，并删除BaseLocomotion Selection Input。若本change晚于该后续change归档，归档时 MUST按新current model合并，不得恢复“Pose Graph不含Animation State Machine”或“基础Pose必须来自AnimationChannel”的旧结论。
