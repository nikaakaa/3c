# Design: 将Transition Routing模块接入现有动画管线

## Context

项目当前正式动画链已经完成：

```text
Gameplay / Motion Matching
  -> AnimationSelectionFrame
  -> explicit Player
  -> compiled Pose Plan
  -> Pose composition
  -> Foot Placement
  -> Output Pose
```

其中：

- `SelectedPosePlayer`和`BlendSpacePlayer`只保留当前source并发布`PoseDiscontinuity`。
- `BlendStack`拥有多source CrossFade、Stored Pose、Per-Bone Blend Profile、capacity和exact source release。
- `Inertialization`拥有previous/current completed Pose、速度、TRS residual、parameter filter、衰减clock和连续rebase。
- `CharacterPresentationInertializationPlanCompiler`要求Inertialization直接连接单source Player。
- Corin BaseLocomotion使用`MarkerSync -> BlendSpacePlayer -> Inertialization`。
- Corin FullBodyAction使用`BlendStack`后直接进入`LayeredBoneBlend`。

当前数学和运行资源已经满足两种过渡技术，缺口是作者模型和两个runtime owner之间的正式请求协议。

该请求协议的通用部分不在本change中重新设计。`add-animation-transition-routing-module`已经独立安装Blend Logic、exact rule、typed request、generation、capture/release握手、reset、snapshot和Frame Facts状态机。本设计只说明现有唯一Pose Plan怎样成为该模块的正式调用方，以及怎样把模块输出落实为现有BlendStack和Inertialization数学。

实施本change前，前置模块change必须已经由用户跑通并归档。未归档时停止，不允许复制其类型或先在Pose Runtime内部写第二份状态机。

UE公开工作流采用以下概念：

- State Machine Transition的Blend Logic为`Standard Blend`、`Inertialization`或`Custom`。
- 兼容Blend节点或State Machine只发出Inertialization request。
- 下游`Inertialization`或`Dead Blending`节点消费请求。
- 传统Blend期间source与target持续求值；Inertialization开始后旧source不再求值。
- Blend Stack用`Max Active Blends`限制历史，并可用`Store Blended Pose`压缩溢出来源。

参考：

- https://dev.epicgames.com/documentation/en-us/unreal-engine/transition-rules-in-unreal-engine
- https://dev.epicgames.com/documentation/en-us/unreal-engine/animation-blueprint-blend-nodes-in-unreal-engine
- https://dev.epicgames.com/documentation/en-us/unreal-engine/python-api/class/AnimNode_BlendStack?application_version=5.7

本设计复用这些用户可见概念，不复制UE的Animation Blueprint类层级、隐藏message propagation、Montage或State Machine runtime。

## Goals

- 把已归档模块的`Standard Blend`与`Inertialization`合同接入BlendStack exact source-target rule。
- 作者在请求节点下游放置显式`Inertialization`节点后，Pose Graph Compiler能把唯一静态route降低为模块可消费的Frame Facts。
- Standard Blend、Stored Pose和Inertial residual保持独立owner。
- FullBodyAction可以在同一AnimationChannel中让不同endpoint pair采用不同Blend Logic。
- Runtime在连续打断、source释放、Marker detach、Preview seek和reset时保持原子。
- UI尽量使用UE熟悉术语，同时保留项目真实Gameplay和Presentation边界。

## Non-Goals

- 不实现通用Animation Blueprint。
- 不实现Custom Blend Graph。
- 不实现Dead Blending。
- 不实现Pose Snapshot。
- 不让Animation Graph重新决定Gameplay winner。
- 不为任意组合节点自动传播request。
- 不让Inertialization读取Selection、Gameplay State或BlendStack私有entry。
- 不让BlendStack执行Quaternion residual或保存Inertial accumulator。

## Terminology

### Blend Logic

作者对一次source-target transition选择的表现技术：

```text
StandardBlend
Inertialization
```

`Custom`不安装。硬切表示为`StandardBlend + DurationSeconds = 0`。

### Standard Blend

BlendStack同时保留和采样旧source与新source，按curve、duration和per-bone Blend Profile计算权重。

### Inertialization Request

上游节点在离散目标切换边界发布的typed事件。它要求下游Inertialization从已经保存的completed Pose history相对新输入建立残差。

### Stored Pose

BlendStack为限制live source数量捕获的内部Pose entry。它不是Blend Logic，不继续推进动画时间。

### Pose Discontinuity

Player观察到Selection identity、generation、source continuity或reset变化后发布的事实。它说明发生了离散切换，不决定使用哪种Blend Logic。

## Target Authoring Model

### Blend Stack Policy

```text
CharacterAnimationBlendPolicy
  Schema
  PolicyId
  Revision

  StackPolicy
    MaxActiveBlends
    StoreBlendedPose
    MaxBlendInTimeToOverrideAnimation
    DepthBlendTimeMultiplier

  DefaultTransition
    BlendLogic
    DurationSeconds
    BlendMode
    CustomCurve
    BlendProfile

  ExactOverrides[]
    SourceEndpoint
    TargetEndpoint
    Rule
```

项目内部字段可以继续使用canonical curve payload，但Inspector使用UE对应名称：

- `Max Active Blends`
- `Store Blended Pose`
- `Max Blend In Time To Override Animation`
- `Blend Logic`
- `Duration`
- `Mode`
- `Custom Blend Curve`
- `Blend Profile`

`Store Blended Pose`只配置Stack历史策略。

### Inertialization Policy

`CharacterPoseInertializationPolicy`不再保存source-target producer matrix。它只保存consumer本身的数学和过滤设置：

```text
CharacterPoseInertializationPolicy
  Schema
  PolicyId
  Revision
  DefaultBlendProfile
  DurationScale
  PositionResidualLimit
  RotationResidualLimit
  ScaleResidualLimit
  LinearVelocityLimit
  AngularVelocityLimit
  ParameterFilters[]
  ResetPolicy
```

Transition duration和具体source-target Blend Logic来自上游compiled request。consumer policy只约束如何处理请求，不重新选择业务技术。

## Target Graph

### Corin

```text
AnimationSelectionInput(BaseLocomotion)
  -> MarkerSync
  -> BlendSpacePlayer
  -> Inertialization(Locomotion)
  -> BasePose

AnimationSelectionInput(FullBodyAction)
  -> BlendStack(Action)
  -> Inertialization(Action)
  -> ActionPose

BasePose + ActionPose + ActionWeight
  -> Layered Blend Per Bone
  -> Pose Parameter Resolve
  -> Foot Placement
  -> Output Pose
```

Locomotion Player继续可以直接发布request；Action BlendStack新增request producer能力。两个Inertialization节点拥有独立history和作用域，避免Action请求修改未被Action mask覆盖的Locomotion分支。

### 不采用全局Output前Inertialization

一个Output前全局consumer看起来更接近UE常见示例，但会让FullBodyAction切换请求影响BaseLocomotion、Additive和ModifyBone后的全身结果。项目当前没有UE request group/tag体系，第一阶段保持branch-local consumer。

## Imported Module Contract And Integration Payload

本节中的Blend Logic、request identity、request generation和lifecycle来自已归档Transition Routing模块。当前change不得重新声明另一套serialized enum、runtime state machine或reason catalog；这里只定义Projection和Pose Plan如何引用这些合同。

### AnimationTransitionBlendLogic

该类型由前置模块唯一提供。角色动画资产只序列化它的稳定值，不声明角色专属副本。

```text
StandardBlend = 1
Inertialization = 2
```

不保留`Unknown`运行值，不安装`Custom`占位。

### PoseInertializationRequest

该类型由前置模块唯一提供。Pose Runtime只能附带编译route identity和completion facts，不能派生第二种request payload。

```text
PoseInertializationRequest
  RequestEventId
  CompletionIdentity
  ProducerNodeId
  ConsumerNodeId
  PreviousEndpoint
  CurrentEndpoint
  Reason
  ResetReason
  DurationSeconds
  BlendProfileIndex
  ParameterFilterSetIndex
  IsPresent
```

约束：

- endpoint使用现有`PoseDiscontinuityEndpoint`。
- request不携带Pose数组；consumer从自己的completed history读取previous output，从当前input读取target。
- request不携带AnimationClip、Animancer state、Gameplay Action、StateMachine edge或Bone Mask。
- `RequestEventId`在同一Pose Plan实例内单调稳定，用于防止重复消费。
- reset request不允许伪造previous endpoint。

### Compiled Route

```text
PoseInertializationRouteDescriptor
  RouteIndex
  ProducerOperationIndex
  ConsumerOperationIndex
  ScopeId
  SupportedReasonMask
```

`CharacterPresentationPosePlan`保存固定route数组。Runtime只按operation index访问，不进行图搜索。

### Compiled Transition Rule

```text
CharacterPresentationBlendTransitionRuleDescriptor
  SourceProgramProducerIndex
  SourceEmpty
  TargetProgramProducerIndex
  TargetEmpty
  BlendLogic
  DurationSeconds
  CurveIndex
  BlendProfileIndex
  InertializationRouteIndex
```

规则约束：

- `StandardBlend`必须有合法duration、curve和Blend Profile；duration可以为0。
- `Inertialization`必须有正duration、Blend Profile和合法route。
- `Inertialization`不得以Empty为target。
- `StandardBlend`不得引用Inertialization route。
- 所有可达pair必须exact覆盖。

## Compiler

### Phase 1: Graph Topology

Pose Graph Compiler继续生成Selection、Player、BlendStack、Inertialization、composition与world-aware operation。

### Phase 2: Request Producer Discovery

Compiler枚举：

- `SelectedPosePlayer`
- `BlendSpacePlayer`
- `BlendStack`

只有它们的compiled rule中存在Inertialization时才需要route。

### Phase 3: Consumer Reachability

第一阶段route只允许：

```text
Request Producer
  -> Inertialization
```

允许中间出现明确声明`InertialRequestTransparent`的零状态identity节点；默认全部composition、Layered、Additive、ModifyBone、FootPlacement、Subgraph边界不透明。

每个producer必须解析到一个唯一consumer。以下情况构建失败：

- 没有consumer。
- 到达两个consumer。
- consumer位于producer上游。
- 经过不透明节点。
- route跨FootPlacement或Output。
- producer与consumer Rig或Pose scope不一致。
- consumer没有合法Policy。

### Phase 4: Exact Rule Materialization

Compiler枚举BlendStack可达endpoint完整矩阵，将default与override物化为exact rule。Runtime不知道某条规则来自default还是override。

### Phase 5: Cross-Artifact Validation

Projection Build验证：

- Blend Policy schema与Rig identity。
- route operation index。
- consumer descriptor和workspace layout。
- request、history、residual与source capacity。
- Corin Profile、Pose Graph和generated Projection revision一致。

## Runtime Ordering

每个PresentationFrame只执行一次正式completion：

```text
consume committed Selection
  -> resolve raw/effective sample
  -> prepare Player/BlendStack membership
  -> resolve exact Blend Logic
  -> prepare source sample usage
  -> prepare request routes
  -> schedule source capture
  -> evaluate Standard Blend raw Pose
  -> evaluate Inertialization consumers
  -> evaluate Pose composition
  -> evaluate Foot Placement
  -> publish Output Pose
  -> commit request consumption
  -> commit Stack entry/release
  -> publish diagnostics
```

任何阶段失败时，不提交半完成request、半释放source或半更新history。

## Standard Blend Path

```text
A live
  -> selection B
  -> exact rule StandardBlend
  -> retain A
  -> sample A and B
  -> BlendStack evaluates weights
  -> no inertial request
```

连续选择C时，BlendStack按现有replace/capacity规则保留或压缩历史。

超过`Max Active Blends`且`Store Blended Pose`开启时，Stack捕获当前完整Stack输出、parameter aggregate、contribution和foot feature，压缩旧entry后release无引用source。

## Inertialization Path

```text
A or Blend(A, B) currently visible
  -> selection C
  -> exact rule Inertialization
  -> prepare C sample
  -> publish request
  -> consumer reads previous completed corrected Pose
  -> consumer reads current C Pose
  -> create residual
  -> commit C as current live target
  -> release old Stack history after capture completion
```

旧source不再用于后续残差求值。

## Interruption Semantics

### Standard到Standard

继续使用BlendStack现有多source history、replace、Stored Pose和retirement。

### Standard到Inertialization

consumer的previous history是上一帧最终Stack输出。当前帧新target准备完成后建立残差，旧Stack history在同一completion后release。

### Inertialization到Inertialization

consumer从上一份corrected completed output相对新target rebase，替换唯一accumulator。不得恢复最初source或叠加第二残差。

### Inertialization期间上游Standard Blend

上游从当前live target向新target执行Standard Blend。既有consumer residual继续按原时钟衰减并叠加在新的raw input上，不产生新request、不捕获下游输出回写Stack，也不创建第二accumulator。

### Pose到Empty

只允许Standard Blend。BlendStack降低branch output weight并在exact completion释放source。Inertialization不得对NoPose、Invalid或Bind Pose建立残差。

### Reset与Seek

Initialization、Body reset、Presentation reset、非连续Preview seek、Rig revision变化和invalid input清除request queue、history和accumulator，并按typed reset语义传播。不得用上一帧缓存伪造新history。

## Marker Sync And Source Lifetime

Standard Blend期间，BlendStack全部live source继续发布`Sample` usage，MarkerSync持续解析共同可见期。

Inertialization切换边界：

1. outgoing source在边界帧仍是合法handoff/sample reference。
2. incoming target生成合法effective sample。
3. request与consumer capture plan准备完成。
4. 单次Evaluate产生新target和consumer corrected Pose。
5. completion提交后detach outgoing Marker relation并release旧source。

Stored Pose不加入Marker relation。Inertial residual不伪装成Animation source。

## Parameter And Foot Feature

- Standard Blend继续按per-bone weight混合Pose Parameter和左右脚feature。
- Inertialization按consumer Policy对每个Parameter执行`Inertialize`或`Snap`。
- 每脚feature按实际脚Bone residual envelope从previous aggregate过渡到target aggregate。
- 最终Foot Placement只消费Layered/Additive合成和全部Inertialization完成后的Final Pose输入。

## Preview And Diagnostics

Preview执行同一Projection和Pose Plan，不创建简化request dispatcher。

Live snapshot增加：

```text
BlendLogic
RequestEventId
RequestProducerNodeId
RequestConsumerNodeId
RequestState
PreviousEndpoint
CurrentEndpoint
Duration
CaptureCompletion
SourceReleaseCompletion
ResidualMagnitude
RebaseCount
ResetReason
```

Pose Graph画布显示：

- BlendStack当前exact rule的Blend Logic。
- Standard Blend live entry、weight和Stored状态。
- Inertialization request流向和consumer高亮。
- consumer residual、progress、rebase和completion。

Editor不根据authoring rule伪造Live状态。

## Corin Migration

1. 将`CorinActionBlendPolicy`升级到新schema。
2. 为现有可达FullBodyAction endpoint逐pair保存Blend Logic。
3. `Empty -> Action`、`Action -> Empty`和需要共同可见期的Action pair使用Standard Blend。
4. 需要立即响应且目标非Empty的现有Action pair可以显式使用Inertialization；不得按Attack、Dodge显示名自动分类。
5. 在Action BlendStack后增加`Action Inertialization`节点和consumer Policy。
6. 保持Locomotion现有`BlendSpacePlayer -> Locomotion Inertialization`，但迁移到同一request route合同。
7. 重建Profile、Projection与Float32/Fixed wrappers的共同Presentation identity。
8. 删除旧Policy v1、直接Player endpoint matrix和旧generated payload。

未来Hit或Death producer由独立Gameplay change加入FullBodyAction可达集合后，现有Compiler会要求Action Blend Policy补齐对应exact pair；本change不提前创建占位producer或按名称保留规则。

## Ownership

| 模块 | 唯一职责 |
|---|---|
| BTSMTL Program | 每AnimationChannel唯一Gameplay winner |
| Timeline/MM | Animation Selection与表现参数 |
| MarkerSync | raw到effective sample time |
| Source Backend | source playable与Pose采样 |
| BlendStack | Standard Blend、live history、Stored Pose、capacity、release、request发布 |
| Inertialization | completed history、residual、衰减、rebase、request消费 |
| Pose Graph | Layered/Additive/ModifyBone/Parameter composition |
| Foot Placement | 世界表面约束与IK |
| Output Pose | 唯一最终发布 |

## Rejected Alternatives

### 新增SelectionTransitionPlayer

它把UE已有的Blend Logic、BlendStack和Inertialization重新包装成项目专有节点，增加学习成本并复制Player语义。拒绝。

### 恢复BlendStack内部Inertial算法

实现简单，但会恢复已经删除的第二份history、residual、workspace和rebase owner。拒绝。

### 保持直接Player与BlendStack二选一

实现改动最小，但FullBodyAction同一通道无法按endpoint选择Standard Blend或Inertialization，作者模型继续偏离UE。拒绝。

### Output前自动插入全局Inertialization

能减少图节点，但Action request会影响Locomotion和其它分支，且形成隐藏拓扑。拒绝。

### 用Stored Pose表示Attack到Hit

只能保证姿势值连续，不能保留运动趋势，也把容量策略误当业务Blend Logic。拒绝。

### 实现Custom Blend和Dead Blending

当前业务没有明确使用者，会扩大Policy、Compiler、Runtime和调参面。拒绝。

## Spec Conflicts And Resolution

- `character-animation-selection-runtime`的CrossFade-only BlendStack要求改为Standard Blend owner与Inertialization request producer。
- `character-animation-layer-runtime`的直接Player Inertialization限制改为compiled request route。
- `character-animation-presentation-authoring`的两张endpoint matrix改为Blend Policy唯一选择Blend Logic、consumer Policy只保存数学配置。
- `character-presentation-pose-graph`增加request route与UE术语显示。
- `character-animation-pipeline`增加request/capture/release的同completion顺序。
- `btsmtl-timeline-editor-preview`从三选一拓扑描述改为正式Blend Logic与consumer request链。
- `refactor-inertial-blending-to-local-pose-node`保留算法所有权，删除直接单Player限制。
- `refactor-animation-selection-pose-graph-boundary`保留显式节点和无隐藏装配，替换“Player、Inertialization或BlendStack三选一”的作者口径。
- `add-character-presentation-blend-space`保留BlendSpacePlayer的source-local参数混合与Pose Discontinuity，补上按compiled Blend Logic发布request的条件。
- `add-character-motion-matching-pose-source`保留Selection Generation和source-neutral输出，删除CrossFade matrix与直接Player Inertialization matrix并列的连续性口径。
- `add-character-animation-virtual-bones`保留完整Pose Bone page流经Stored Pose与Inertialization的要求，不新增request专属Pose格式。

## Open Questions Resolved

### 是否把HardCut做成独立Blend Logic

不做。对齐UE习惯，使用Standard Blend Duration为0。

### 是否把Stored Pose做成独立节点

不做。它只属于BlendStack的内部历史策略。

### 是否让Inertialization读取Blend Policy

不读取资产。Compiler把上游exact rule需要的duration、profile和route写入request payload，consumer只读取compiled request与自己的consumer Policy。

### 是否允许一个consumer接多个producer

允许Compiler静态证明的多个上游producer汇入同一branch-local consumer；同一completion若收到多个请求，按稳定operation order收集并使用最短合法duration，reset优先于普通request。第一阶段Corin每个consumer只有一个直接producer。

### 是否允许request跨Layered Blend Per Bone

第一阶段不允许。需要全身合成后惯性化时，必须由后续change定义typed composite discontinuity与明确scope，不能放宽为隐式传播。
