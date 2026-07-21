# Design: Character Presentation Pose Graph

## Context

当前正式链路把`LayerId`贯穿Gameplay与Presentation：

```text
State / Action / Timeline ownership
  -> Program Finalize per LayerId selection
  -> AnimationPlaybackLifecycle
  -> Animancer Layer / Fade
  -> Final Animator Pose
  -> Foot Placement
```

未实施的完整Blend Stack提案进一步计划：

```text
per Layer stack
  -> Stored / Inertial / Per-Bone transition
  -> Layer mask / Override / Additive composition
  -> Final Animator Pose
```

其中“一个producer入口连续切换时如何保留历史”和“多个姿势入口如何按骨骼组合”是两种不同问题。前者按时间组织source，后者按空间组织slot。把它们放进同一个固定compositor，会让Motion Matching、FullBody Action、UpperBody、Equipment和IK都只能继续修改同一大模块。

本设计把本项目缺失的UE AnimGraph职责定义为`Character Presentation Pose Graph`。它只负责Presentation pose composition，不复制UE Animation Blueprint中的Gameplay event graph、StateMachine选择、Montage业务触发或Motion Matching搜索。

## Goals

- 让BTSMTL逻辑仲裁、每路时间混合、跨路骨骼合成和最终IK各有唯一权威。
- 保留当前Program Finalize“Presentation不重新选择逻辑赢家”的边界。
- 让每个Pose Slot自动经过固定Blend Stack，支持高频重选、Stored Pose、Inertial与Per-Bone transition。
- 让Pose Graph唯一拥有跨slot拓扑、Bone Mask、Override/Additive、Pose Parameter curve解析和最终动画pose。
- 复用现有节点编辑体验，不把BTSMTL Gameplay节点模型复制到Presentation。
- 让当前ALS式Corin马上获得有业务意义的BaseLocomotion与FullBodyAction分层。
- 保持未来Motion Matching只替换上游pose producer，不替换Blend Stack、Pose Graph或Foot Placement。
- 所有Runtime拓扑、buffer与node dispatch均由Projection编译固定，表现热路径不解释authoring graph且不动态分配。

## Non-Goals

- 不实现Motion Matching database、Pose Search、trajectory query、candidate cost或search cadence。
- 不实现UE Animation Blueprint Event Graph、AnimInstance class、Montage Section、Slot Group arbitration或Notify Gameplay事件。
- 不把BTSMTL StateMachine搬进Pose Graph，也不让Pose Graph读取Input、Blackboard、Action或GameplayTag。
- 不实现运行时动态Linked Anim Layer class替换。当前只提供静态inline/shared PoseSubgraph；Equipment需要动态替换时必须用独立change定义正式业务输入和生命周期。
- 不实现动画重定向。所有source必须已经适配正式Rig。
- 不把Foot Placement改成可选Pose Graph节点；它继续是固定、唯一的Pose Post Process。
- 不提供旧Layer配置兼容、隐式Base Pose、缺失mask默认全身或缺失curve policy默认规则。

## Responsibility Model

| 层 | 输入 | 唯一职责 | 输出 |
|---|---|---|---|
| BTSMTL / Program | State、Action、Timeline、ownership | 每个Animation Channel选择至多一个producer | committed producer command |
| Presentation Projection | producer contract、Profile、Pose Graph、Blend/Rig authoring | 绑定channel、slot、资源并编译target-neutral payload | Projection |
| Pose Slot Blend Stack | 一个slot的resolved source request历史 | 时间混合、clock、curve、Stored Pose、Inertial、source retirement | PoseSlotFrame |
| Presentation Pose Graph | 全部PoseSlotFrame、bone masks、curve policy | 空间合成、additive、curve解析、贡献传播 | FinalAnimationPoseFrame |
| Foot Placement | final pose、final per-foot contribution、Body与world query | 最终脚部约束与pelvis修正 | post-processed pose |
| Animancer | resolved clip/mixer sample | source Playable采样和寿命 | source pose sample |

任何一层不得重新承担上一层的决策。

## Target Architecture

```text
CharacterSimulationProgram
  AnimationChannelId selections
              |
              v
CharacterPresentationProjection
  channel -> PoseSlot binding
  source resources / marker / foot feature
  compiled stack data / compiled pose program / dense rig
              |
              v
AnimationPlaybackLifecycle per channel
              |
              v
AnimationBlendStackRuntime per PoseSlot
  + AnimancerPoseSamplingBackend
  + AnimationSlotBlendPoseEvaluator
              |
              v
PoseSlotFrame[]
  pose + parameters + availability + contribution
              |
              v
CharacterPoseGraphEvaluator
  PoseSlotInput
  LayeredBoneBlend
  AdditivePose
  PoseCurveResolve
  static PoseSubgraph
  OutputPose
              |
              v
FinalAnimationPoseFrame
              |
              v
Foot Placement -> Camera
```

## Identity Model

### AnimationChannelId

`AnimationChannelId`属于Gameplay Semantic与Program contract。Timeline `AnimationTrack`明确声明channel；Program Finalize在State、Action、interruption和Timeline ownership结束后为每个channel最多提交一个producer。

它回答：“逻辑上这一类动画输出当前是谁？”

它不保存Bone Mask、Additive、slot order、Stack容量或fade策略。

### PoseSlotId

`PoseSlotId`属于Presentation。Pose Graph声明slot；Projection要求每个可达Animation Channel精确绑定一个slot，并要求每个slot至多绑定一个channel。

它回答：“这个已经解析好的pose从哪里进入空间合成？”

它不参与State/Action优先级，也不能在多个channel之间选winner。

### PoseNodeId / PosePortId

Pose Graph节点和端口拥有稳定authoring identity。重排节点或改变显示名不得改变identity。复制节点必须生成新identity；shared subgraph引用保留被引用图identity，但每个调用site拥有独立call-site identity。

### PoseParameterId

从Animation source进入Pose Graph的曲线参数使用稳定`PoseParameterId`，不按字符串名称在Runtime查找。声明至少包含value kind、显式default和每个合成site的resolve policy。当前只安装有限标量参数；不为未定义的Vector/Transform参数留动态object口。

## Authoring Model

### CharacterPresentationPoseGraphAsset

资产保存：

- PoseGraphId与content revision。
- 有序Pose Slot声明：PoseSlotId、绑定AnimationChannelId、`RequireOutput | AllowEmpty`。
- Pose Parameter声明。
- Pose节点、typed port、独立`InterfacePortId`与普通edge。
- inline/shared PoseSubgraph reference。
- 根图精确一个`OutputPose`且禁止`GraphInput`/`GraphOutput`；子图精确一个`GraphInput`和一个`GraphOutput`且禁止`OutputPose`。

资产不保存：

- BTSMTL Graph、StateMachine、ConditionRule或Blackboard。
- producer选择、Action ownership或Timeline window。
- AnimationClip强引用、Animancer state或runtime weight。
- runtime debug、preview cursor或workspace。

### Pose Value Contract

每条Pose edge传递一个不可变逻辑值：

```text
PoseValue
  Availability
  Dense local TRS pose
  PoseParameter buffer
  SourceContribution map
  PoseContinuity identity
```

Authoring graph不保存Runtime buffer。Compiler为每个edge/value slot分配固定workspace index。

### Fixed Node Set

#### PoseSlotInput

- 精确引用一个PoseSlotId。
- 读取该slot固定Blend Stack已经完成的`PoseSlotFrame`。
- 不创建Stack、不选择producer、不改变clock。
- 每个slot在根图中必须只有一个authoring input节点；分支复用通过edge fan-out和编译缓存完成。

#### LayeredBoneBlend

- 一个Base Pose和一个Overlay Pose输入。
- 显式dense Bone Mask来源。
- 使用Overlay availability/slot output weight和mask逐骨骼Override。
- Overlay为空时保持Base，不生成bind pose。
- 每个PoseParameter必须显式选择Base、Overlay、Weighted、Max或Min策略。

#### AdditivePose

- 一个Base Pose和一个Additive Pose输入。
- 显式Bone Mask、weight source和reference pose identity。
- Compiler校验additive source与reference space兼容。
- 不把任意普通clip静默解释为additive。

#### PoseCurveResolve

- 只解析已声明PoseParameterId。
- 每个参数有完整source和policy。
- 不修改骨骼pose，不读取Gameplay curve或Timeline Window。

#### PoseSubgraph

- 可以inline保存私有Pose Graph或显式引用shared Pose Graph asset。
- 调用点的每个本地端口用独立稳定`InterfacePortId`一对一绑定子图边界端口，不复用node-local `PosePortId`冒充接口身份。
- 边界支持Pose与Parameter typed port；`GraphInput`只含output port，`GraphOutput`只含input port并至少导出一个Pose。
- Validator拒绝接口identity重复、coverage缺失、重复binding、kind/direction/required不一致、required边界悬空和inline/shared cycle。
- Compiler递归静态展开call site，把外部输入重接到`GraphInput`内部消费者，把`GraphOutput`内部source重接到外部消费者，并为内部node/port生成call-site-scoped稳定identity及完整source-map call chain。
- `PoseSubgraph`、`GraphInput`、`GraphOutput`在编译后全部消失，Runtime Program不安装对应operation或动态dispatch。
- Runtime不动态替换实现，不通过反射加载Graph class。

#### GraphInput / GraphOutput

- 只属于PoseSubgraph authoring与Compiler边界，不是Runtime operation。
- 子图必须各有且仅有一个；根图禁止出现。
- 子图Parameter声明必须与根图dense Parameter catalog一致，数据通过显式Parameter接口传递，不允许子图创建第二套Parameter identity/default。

#### OutputPose

- 根图必须恰好一个。
- 输入必须在所有合法slot availability组合下产生有效最终pose。
- 输出最终参数和source contribution。
- 不包含Foot Placement、Camera或Gameplay root motion。

## Graph Editor Reuse

现有`BaseTreeWindow`、`BaseTreeView`和节点UI同时混有两类东西：

- 可复用的Editor交互：窗口、GraphView、搜索、selection、clipboard、Undo、Inspector、breadcrumb和只读overlay。
- BTSMTL领域语义：`BaseGraph`运行上下文、`BaseNode.OutputValue`、`BaseEdge` transition priority、`BTAbortPolicy`、`ConditionRuleGraph`与Input Action兼容规则。

本change抽取`GraphAuthoringEditorShell`，通过窄adapter接入领域：

```text
IGraphAuthoringDocument
IGraphAuthoringNodeCatalog
IGraphAuthoringPortPolicy
IGraphAuthoringMutationAdapter
IGraphAuthoringInspectorAdapter
IGraphAuthoringDiagnosticsAdapter
```

BTSMTL继续使用`BaseGraph`、`BaseNode`、`PropertyPort`与自身compiler。Pose Graph使用`CharacterPoseGraphData`、Pose Node、Pose Port与Pose Compiler。两者共用外壳，不共用领域数据或runtime evaluator。

原`BaseTreeWindow`迁移为BTSMTL domain adapter上的正式入口；不得保留旧window实现与新shell两套交互路径。

## Compiler

`CharacterPresentationPoseGraphCompiler`只在Editor运行，输入：

- validated Pose Graph authoring。
- `CharacterAnimationRigDefinition`。
- Profile中的channel/slot binding。
- Blend Library编译后的slot layout。
- Pose Parameter catalog。

输出不可变`CharacterPresentationPoseProgram`：

- schema与PoseGraph identity。
- stable slot index和channel binding。
- topological node operation array。
- fixed pose/parameter/contribution workspace layout。
- dense Bone Mask和additive reference descriptor。
- static subgraph call-site source map。
- output operation index。
- required runtime capability与diagnostic source map。

Compiler必须：

- 拒绝cycle、dangling edge、重复Output、缺失Output和非法port kind。
- 拒绝未消费或重复的slot声明。
- 拒绝channel/slot非一对一、未知channel、未知slot和output policy冲突。
- 对所有`AllowEmpty`组合验证Output可达且Base要求得到满足。
- 计算公共子图的固定frame cache，不要求作者创建Save/Use Cached Pose节点。
- 将Rig BoneId、Mask和Parameter解析为dense index。
- 将Pose Program纳入ProjectionRevision与Presentation dependency，不进入Numeric Program。

Runtime不得解释authoring节点类型、ScriptableObject、GUID path或Editor graph。

## Runtime Evaluation

### Creation

创建顺序固定：

```text
Projection/Contract validation
  -> Rig Binding validation
  -> Pose Program validation
  -> fixed Slot Stack workspaces
  -> Animancer source backend
  -> fixed Pose Graph workspace/output job
  -> Foot Placement
```

任一步失败时不发布半初始化Runtime。

### PresentationFrame

```text
Body frame
  -> consume channel commands
  -> sample retained producers / Marker Sync
  -> update every slot Blend Stack frame plan
  -> sample source poses
  -> evaluate each PoseSlotFrame
  -> evaluate compiled Pose Program once
  -> publish FinalAnimationPoseFrame
  -> Foot Placement once
  -> Camera
```

`CharacterPoseGraphEvaluator`按编译operation顺序执行。每个operation只读输入workspace并写唯一输出slot。fan-out只读同一缓存；不得重复采样source或重复推进Stack clock。

### Empty Slot

`AllowEmpty` slot输出typed `NoPose`和零贡献。能够接受Optional overlay的节点必须显式定义NoPose行为；`OutputPose`不能输出NoPose。`RequireOutput` slot没有Current/Pending或合法Pose时Runtime进入typed invalid，不得使用bind pose、默认Idle或上一帧残留姿势。

## Pose Parameter Curves

ALS式动画经常用动画曲线控制骨骼混合、Foot IK权重或局部姿态强度。它们不能在骨骼层混合后由同名Timeline曲线偷偷覆盖。

本设计要求：

- 每个source sample把已编译、已声明的PoseParameter值与pose一起输出。
- Slot Blend Stack按同一entry clock与对应参数policy生成slot参数。
- Pose Graph每个空间合成节点显式解析参数。
- OutputPose发布唯一final parameter stream。
- Foot Placement只读取正式映射给它的final parameter或final per-foot contribution。
- 未声明参数、重复参数、缺失policy或非有限值直接失败。

这样骨骼Mask不会被动画曲线反向改变；曲线也不会在多个slot之间靠名称碰撞决定结果。

## Blend Stack Boundary

每个Pose Slot自动拥有一个固定`AnimationBlendStackRuntime`。它负责：

- ordered entry与独立Fade Clock。
- canonical curve和Per-Bone transition profile。
- capacity、Stored Pose与Inertial accumulator。
- 同slot source retirement和Marker relation detach。
- `PoseSlotFrame`及slot内部source contribution。

它不负责：

- 跨slot Bone Mask。
- slot composition order。
- Base/Overlay/Additive拓扑。
- final Pose Parameter解析。
- Animator最终Pose写回。

Blend Stack不是Pose Graph节点。Pose Graph只通过`PoseSlotInput`读取它的完成结果，因此作者无法绕过Stack，也不会为不同graph topology创建多个stack实例。

## Foot Placement and IK Boundary

Pose Graph输出`FinalAnimationPoseFrame`：

- 最终未IK骨骼pose。
- final PoseParameter stream。
- 按最终空间Mask传播后的source contribution。
- Left/Right语义foot的实际贡献与连续性identity。

Foot Placement只在该frame completion之后运行。BaseLocomotion的脚在FullBodyAction全身覆盖时可以被action贡献替换；未来UpperBody mask若脚权重为0，则不得降低Base脚贡献。Stored Pose与Inertial feature先在slot内部形成，再由Pose Graph按最终脚Bone Mask传播。

Foot Placement仍不成为Graph节点，原因是它依赖Body、PhysicsScene、surface生命周期和pelvis约束，不是纯Animation Pose composition。

## Corin Formal Topology

```text
Animation Channels
  BaseLocomotion    -> BaseLocomotionSlot    RequireOutput
  FullBodyAction    -> FullBodyActionSlot    AllowEmpty

Pose Graph
  BaseLocomotionSlot
      |
      +---- LayeredBoneBlend(full body mask) <---- FullBodyActionSlot
                         |
                     OutputPose
                         |
                    Foot Placement
```

producer迁移：

- Idle、WalkStart、WalkLoop、RunStart、RunLoop、RunEnd、MovingTurn进入`BaseLocomotion`。
- Attack1..5、Dodge和其它明确全身动作进入`FullBodyAction`。
- WalkEnd没有producer时不创建fallback；BaseLocomotion继续保持正式selection或由逻辑选择目标producer。
- Action退出由`FullBodyAction`提交None；BaseLocomotion从未被表现层停止，Pose Graph只让action slot淡出。

这改变的是动画表现并行性，不改变Attack Window、damage、IFrame、Action Context或World motion。

## Projection and Identity

`CharacterPresentationSemanticContract` producer entry保存`AnimationChannelId`。`CharacterPresentationProjection`保存：

- ordered producer resource binding。
- AnimationChannelId到PoseSlotId的一对一binding。
- compiled slot stack policy与transition matrix。
- Rig Definition dense payload。
- compiled CharacterPresentationPoseProgram。
- Marker Sync与Foot Analysis payload。

Pose Graph、Blend Library、Rig、Mask、Parameter catalog或source binding变化只改变ProjectionRevision；Gameplay channel或producer contract变化还会改变SemanticHash、ContractHash和各Numeric Target ProgramHash。Projection仍不保存NumericProfile、Target ABI或ProgramHash。

## Diagnostics and Preview

统一snapshot必须能沿一条链解释：

```text
AnimationChannelId
  -> committed PlaybackId
  -> PoseSlotId
  -> Stack Entry / Stored / Inertial
  -> PoseSlotFrame
  -> Pose Graph node contribution
  -> OutputPose / Final foot contribution
```

Timeline Authoring Preview继续只采样Presentation动画，不运行Gameplay StateMachine。它为每个channel最多生成一个preview command，复用正式Projection、Stack、Pose Program、Rig和Evaluator。Live Debug只读正式snapshot，不重新执行Graph或curve。

## Migration and Deletion

迁移必须形成一次最终切换，不发布临时双拓扑：

1. 抽取Editor Shell并把BTSMTL现有入口切换到domain adapter。
2. 建立Pose Graph authoring、validator、compiler和Projection schema，但不接入第二条Runtime输出。
3. 将Blend Stack change收窄到per-slot输出合同，建立Pose Graph final evaluator。
4. 将`LayerId`全链替换为`AnimationChannelId`，将Profile Layer catalog替换为Pose Graph Slot声明。
5. 在同一Runtime迁移中切换Presentation、Preview、Debug和Foot Placement到`Stack -> PoseGraph -> PostProcess`。
6. 迁移Corin两个channel、两个slot、正式Pose Graph与Blend Library。
7. 重建Projection和目标Program wrapper。
8. 删除旧Layer definition、Animancer layer/fade、旧Blend Stack global compositor、旧snapshot和旧serialized字段。

不存在“旧Layer compositor先运行，再由Pose Graph二次合成”的中间正式路径。

## Tradeoffs

### 选择：分离AnimationChannelId与PoseSlotId

业务收益：攻击是否激活仍由Gameplay逻辑决定，攻击如何覆盖基础移动由Presentation Graph决定。以后改变UpperBody mask或把Locomotion改成Motion Matching，不需要改Action ownership。

技术代价：Timeline、Program contract、Projection、command、trace和资产都要做一次破坏性重命名与迁移。

### 选择：Blend Stack固定在每个Pose Slot之前

业务收益：所有ALS、Action和未来Motion Matching source都天然获得同一套高频切换连续性，不会因为作者忘放节点而失效。

技术代价：作者不能在图中任意决定某一路是否绕过Stack。当前项目没有需要绕过时间连续性的业务，因此不暴露该自由度。

### 选择：Pose Graph拥有最终Pose，Animancer只采样source

业务收益：Bone Mask、Additive、curve与Foot contribution都基于同一个最终合成事实，调试可以回答每根骨骼来自哪里。

技术代价：项目必须维护编译拓扑、pose workspace、Animation Job和Quaternion/Additive规则；Animancer Layer系统不能继续代劳。

### 选择：复用Editor Shell，不复用BTSMTL数据模型

业务收益：编辑体验一致，同时不会让Pose节点携带State transition、Blackboard或BT字段。两个领域各自保持清楚、可编译的数据合同。

技术代价：需要先把当前窗口中的领域特判抽成adapter，不能只继承`BaseNode`快速堆节点。

### 选择：编译器自动缓存公共Pose子图

业务收益：作者不需要理解Save/Use Cached Pose节点，也不会因为忘记缓存而重复采样。fan-out仍然有固定成本和稳定调试identity。

技术代价：Compiler要做liveness与workspace分配。当前图是静态DAG，这个复杂度比把缓存生命周期暴露给作者更可控。

### 选择：当前只安装静态PoseSubgraph

业务收益：可以拆分和复用Base/Action组合逻辑，但不会为尚不存在的武器动态层建立空接口和生命周期。

技术代价：未来Equipment若需要运行时替换整段Pose实现，需要新的change扩展Projection与Runtime binding；不会假装当前已经等价于UE Linked Anim Layer。

### 未选择：继续用Profile有序Layer数组

它实现简单，但组合关系只能是固定线性栈，无法清楚表达full-body、upper-body、additive、curve resolve和以后子图复用。更重要的是它继续把slot声明、mask和blend order压在一个浅配置表里。

### 未选择：把StateMachine和Montage做成Pose Graph节点

UE这样做是因为Animation Blueprint自己承担一部分状态选择。本项目已有BTSMTL Program作为唯一Gameplay语义，复制状态机或Montage业务分段会恢复双权威，因此不做。

## Spec Conflicts and Resolution

- `character-animation-layer-runtime`和`character-animation-pipeline`中的`LayerId`改为`AnimationChannelId`；空间Layer语义迁移到Pose Graph。
- `character-state-timeline-authoring-loop`中的Corin单一Base layer改为BaseLocomotion与FullBodyAction两个channel/slot；逻辑仍为每channel唯一selection。
- `character-animation-presentation-authoring`与`character-pipeline-definition-authoring`中的Layer catalog/Animancer TransitionLibrary改为Pose Graph、Blend Library与Rig引用。
- `btsmtl-graph-core`的一套Graph规则限定为BTSMTL领域；跨领域只共享Editor Shell，Pose Graph不得成为第二套BTSMTL runtime graph。
- `character-presentation-interpolation`和`gameplay-tick-system`中的Animancer fade时钟改为slot Blend Stack clock；Animancer只推进source sampling graph。
- `character-foot-placement-presentation`的最终输入改为Pose Graph输出后的per-foot contribution，而不是Animancer或单个slot scalar。
- `agent-character-controller-synthesis`继续不写Presentation配置，但Snapshot只读术语改为Animation Channel、Pose Slot、Pose Graph、Blend Library与producer binding。
- `refactor-animation-playback-to-blend-stack`删除global Layer compositor职责；已完成的`refactor-presentation-projection-target-boundary`继续提供target-neutral边界，本change负责其后续AnimationChannelId与Pose payload schema升级。

## Risks

- 当前Graph Editor领域特判比类名显示得更深。抽取时若adapter仍通过类型判断回调BTSMTL节点，会形成伪通用Shell；任务要求逐一收口搜索、端口兼容、Inspector与diagnostics seam。
- Animation Jobs中source capture、slot stack和final graph output必须在同一PlayableGraph内拥有固定顺序。不能用两个独立PlayableGraph或Transform回写连接它们。
- Corin从单Base仲裁改成两个channel后，Locomotion必须在Action期间继续拥有合法selection。Program compiler和authoring inventory必须验证这一点，不能由Presentation补Idle。
- Pose Parameter curve策略若遗漏会产生难以察觉的ALS行为差异，因此Compiler对每个合成site要求完整policy，不允许运行时按名称默认。
- 多active change都修改Projection和Animation specs。实施前必须以这三个提案的最终共同合同为基线，不能按旧任务顺序分别落地出临时双路径。
