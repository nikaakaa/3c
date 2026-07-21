# Change: 新增并分离 Character Presentation Pose Graph

## Why

当前角色动画把三类不同职责压在同一个 `LayerId` 和同一条播放链上：

- BTSMTL、Timeline 与 Program 用 `LayerId` 表达逻辑侧“同一时刻谁可以成为该路输出”。
- `CharacterAnimationLayerDefinition` 又用同一个 `LayerId` 保存 AvatarMask、Override/Additive、输出策略和组合顺序。
- 未实施的 `refactor-animation-playback-to-blend-stack` 进一步让 Blend Stack 同时负责单路姿势的时间混合、所有 Layer 的骨骼空间合成和最终 Animator Pose。

这会让逻辑仲裁、时间混合和骨骼合成继续互相知道。当前 Corin 只有一个 Base Layer 时问题不明显；一旦同时保留 Locomotion、叠加 FullBody Action、加入 UpperBody/Equipment，或让 Motion Matching 高频替换某一路姿势，就无法只替换一个职责。任何新的分层都会被迫修改 BTSMTL 状态选择或在现有 compositor 外再套第二层混合。

UE 的 AnimGraph 分层价值不在“多一张蓝图”，而在于把职责分开：StateMachine、Montage 或 Motion Matching 产生姿势；Blend Stack处理单个姿势入口的时间历史；Slot与Layered Blend Per Bone按骨骼空间组合；最终 IK在结果之后处理。本项目已经有 BTSMTL、Timeline、Projection、Animancer和Foot Placement，因此只需要补上缺失的 Presentation Pose Graph，不需要复制 UE 的 Gameplay 状态机或 Animation Blueprint runtime。

本 change 将动画链正式拆成：

```text
BTSMTL / Program AnimationChannel selection
  -> Projection binding
  -> fixed PoseSlot Blend Stack
  -> PoseSlot pose
  -> Character Presentation Pose Graph
  -> Final Animation Pose
  -> Foot Placement Pose Post Process
```

## What Changes

- 将现有 `LayerId` 拆为两个互不替代的稳定身份：
  - `AnimationChannelId` 属于 BTSMTL、Timeline、Semantic IR、Program 和 command，唯一表达逻辑仲裁通道。
  - `PoseSlotId` 属于 Animation Presentation Profile、Blend Stack 与 Pose Graph，唯一表达表现姿势入口。
- 每个可达 `AnimationChannelId` 必须在 Projection 中一对一绑定一个 `PoseSlotId`。Presentation 不在多个 channel 之间重新选择赢家；Pose Graph只组合已经解析的 slot pose。
- 新增 `CharacterPresentationPoseGraphAsset`，保存独立 Pose Graph 数据、稳定节点/端口身份、Pose Slot声明、骨骼Mask引用、Pose Parameter声明和唯一 Output Pose。
- 新增 Editor-only `CharacterPresentationPoseGraphCompiler`，把authoring DAG、共享Rig dense bone数据、Pose Slot布局、曲线解析策略和固定workspace布局编译为 target-neutral `CharacterPresentationPoseProgram`，并嵌入 `CharacterPresentationProjection`。
- 新增固定节点集合：Runtime节点为`PoseSlotInput`、`LayeredBoneBlend`、`AdditivePose`、`PoseCurveResolve`与`OutputPose`；authoring另提供静态`PoseSubgraph`及compiler-only `GraphInput`/`GraphOutput`边界。边界端口使用独立稳定`InterfacePortId`，Compiler递归展开后这三类节点不得进入Runtime Program。节点只处理 Pose/Curve/Contribution，不读取 State、Action、Blackboard、Timeline Window、GameplayTag 或业务 Priority。
- 每个 `PoseSlotInput` 读取该 slot 唯一固定 `AnimationBlendStackRuntime` 的输出。Blend Stack不是作者可选节点，不能被绕过，也不再拥有跨 slot 合成和最终 Animator Pose。
- Pose值同时携带 dense local bone pose、命名 `PoseParameterId` 标量流、slot availability/weight 和可追踪 source contribution。每个合成节点必须显式声明骨骼Mask、Override/Additive语义与Curve解析策略。
- `CharacterPoseGraphEvaluator` 使用编译后的固定拓扑和预分配workspace完成跨slot骨骼合成、curve解析、公共子图缓存、最终source contribution和Animator AnimationStream写回。
- `CharacterAnimationPresentationProfile` 不再保存 Layer catalog。它改为唯一引用 Pose Graph、Blend Library、Animation Rig Definition、producer resource binding与Foot Analysis输入。
- 删除 `CharacterAnimationLayerDefinition`、Animancer layer index、Profile layer order、layer AvatarMask和layer blend mode旧数据；不提供旧Layer到Slot的运行时兼容或fallback。
- 抽取 `GraphAuthoringEditorShell`，复用现有BTSMTL节点编辑器的窗口、画布、搜索、复制粘贴、Undo、Inspector与只读diagnostics外壳。BTSMTL Graph与Pose Graph保留各自的数据、节点、端口、validator和compiler，不共享Gameplay `BaseNode`/`BaseEdge`语义。
- Corin正式迁移为两个逻辑通道和两个姿势入口：
  - `BaseLocomotion` / `BaseLocomotionSlot`，`RequireOutput`。
  - `FullBodyAction` / `FullBodyActionSlot`，`AllowEmpty`。
  Attack、Dodge与其它FullBody Action不再抢走Base Locomotion command；Pose Graph通过全身Mask覆盖Base，action退出时由slot Blend Stack连续淡回Base。
- Foot Placement继续是唯一固定Pose Post Process。它只消费Pose Graph完成后的最终未IK姿势和经过最终空间Mask合成的左右脚实际贡献，UpperBody或零脚权重slot不得错误稀释Foot Analysis。
- Runtime、Timeline Preview和Live Debug统一展示 `AnimationChannelId -> PoseSlotId -> Stack Entry -> Pose Graph Node -> Final Pose` 链路，不从Animancer state或作者图重新推导。

## Impact

### Specs

- 新增 `character-presentation-pose-graph`
- 新增 `graph-authoring-editor-shell`
- 修改 `btsmtl-graph-core`
- 修改 `btsmtl-compiled-simulation-program`
- 修改 `character-animation-layer-runtime`
- 修改 `character-animation-pipeline`
- 修改 `character-animation-presentation-authoring`
- 修改 `character-foot-placement-presentation`
- 修改 `character-pipeline-definition-authoring`
- 修改 `character-pipeline-runtime`
- 修改 `character-state-timeline-authoring-loop`
- 修改 `character-state-interruption-authoring`
- 修改 `character-presentation-interpolation`
- 修改 `btsmtl-timeline-editor-preview`
- 修改 `agent-character-controller-synthesis`
- 修改 `gameplay-tick-system`
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

- `refactor-animation-playback-to-blend-stack` 必须同步改为“每个Pose Slot唯一时间Blend Stack”。其Stored Pose、Inertial、Per-Bone transition和Animancer source采样保留；跨slot mask/additive组合、curve最终解析与Animator最终Pose移交本change。两个change不得先后安装出一条临时双compositor路径。
- `refactor-presentation-projection-target-boundary` 已完成target-neutral Projection边界。本change作为其后续破坏性schema迁移，把producer contract字段从`LayerId`改为`AnimationChannelId`，并让Projection保存Pose Slot、Pose Program、Blend Stack与Rig payload；不得恢复任何Numeric Target依赖。
- 已完成的Marker Sync、Foot Analysis和Foot Placement语义保留。Marker Sync只在同一Animation Channel/Pose Slot的live producer之间工作；Foot Analysis在Pose Graph最终空间合成后形成每脚输入。
- 本change不修改Motion Matching查询。以后Motion Matching只需成为某个Animation Channel的pose producer，继续进入同一Pose Slot Blend Stack和Pose Graph。

## Breaking Changes

- `LayerId` 从Timeline、Program producer contract、command、Projection binding与diagnostics删除，统一替换为`AnimationChannelId`。
- `CharacterAnimationLayerDefinition`和Profile Layer catalog删除，统一替换为Pose Graph Slot声明与图拓扑。
- Corin不再使用单一Base channel仲裁Locomotion与FullBody Action；现有Timeline channel数据必须正式迁移。
- Blend Stack不再写最终Animator Pose，也不再拥有跨slot Layer order、AvatarMask或Additive composition。
- Profile必须引用有效Pose Graph、Blend Library与Rig Definition；缺一项即编译或Runtime创建失败。
- Projection schema、Presentation ContractHash和ProjectionRevision提升；旧generated Projection与旧Profile数据直接失效并重建。
- BTSMTL Editor共享的是通用Editor Shell，不允许Pose Graph继承或复用Gameplay `BaseNode`、ConditionRule、BTAbortPolicy或runtime evaluation context。
- 不提供FormerlySerializedAs、旧Layer reader、默认Slot、自动Base Pose、按名称匹配channel/slot或Animancer Layer fallback。
