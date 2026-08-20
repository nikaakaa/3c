# Design: 原生AnimationClip作者链与Locomotion Phase同步减法

## Context

当前作者和运行链包含三层本可合并的数据：

```text
AnimationClip
  -> CharacterAnimationSequenceAsset
  -> Profile Binding / Blend Space Sample / Timeline Segment
  -> Projection Clip Plan
```

Sequence保存Clip引用、Loop、DefaultPlayRate、Marker、Curve、Notify、Rig与Analysis。Corin全部19个Sequence均一对一包装原生`.anim`；默认倍率、Rig和Analysis没有素材级差异，Notify没有正式内容。Projection最终仍把Sequence降低成AnimationClip、curve、marker和source index，Runtime没有消费Sequence对象。Sequence因此是宽接口、浅实现。

生成式脚相同步继续扩大了这层接口：Marker划分segment，Foot Analysis增加同步描述，Editor-only DP为每对可达source编译warp knots，Runtime再通过relation cursor求effective time。该链能证明计划结构合法，却不能证明实际有限业务出口与目标步态相容。

## Goals

- 让AnimationClip成为唯一素材时间真相。
- 让Unity Animation Window成为唯一素材时间编辑表面。
- 让Profile只保存角色级装配、Rig、Analysis和Sync Group。
- 用一条作者可见Phase曲线取代Marker点、生成式pairwise warp和Runtime segment relation。
- 让Projection按实际业务播放覆盖区间拒绝不相容关系。
- 保持Authoring -> Projection -> Runtime单向链，不新增fallback或第二播放器。
- 删除当前没有业务内容的Sequence Notify和Action Marker Sync。

## Non-Goals

- 不修改Unity Animation Window内部UI。
- 不让AnimationClip拥有Gameplay State、Transition、Blend或Timeline逻辑。
- 不把Foot Analysis曲线、IK结果或Foot Placement世界结果写回Phase。
- 不为未来可能需要的通用Notify、Montage Marker或动画事件预留未使用Runtime。
- 不把Motion Matching改为Clip Phase follower。

## Ownership

| 数据 | 唯一owner | 作者入口 | Runtime输入 |
| --- | --- | --- | --- |
| 骨骼动画、Root曲线、注册表现曲线 | 原生AnimationClip | Unity Animation Window | Projection中的Clip与canonical curve plan |
| Rig、Foot Analysis Source、Pose Source Binding、Action producer binding | CharacterAnimationPresentationProfile | Profile/Pose authoring与Agent Document | Projection dense binding |
| Locomotion Sync Group成员 | CharacterAnimationPresentationProfile | Profile Sync Group authoring与Agent Document | Projection relation inventory |
| PoseState Transition与Blend | PoseStateMachine edge | PoseStateMachine authoring | compiled Routing Plan |
| Action Start/End、ClipIn、Weight、Ease、Window、Cue、Motion、Warp | Gameplay Timeline | Timeline Editor | Program与Action Presentation计划 |
| Foot Analysis Artifact | Library generated product | 显式Analyzer Build | Projection Build校验与Foot Feature payload |
| effective source time | source-local Animation Phase Runtime | 无 | raw clock + compiled phase plan |

Animation Editor是写入口，不是数据owner。AnimationClip保存时间数据；Profile保存角色装配。不存在Sequence、Marker资产、FootPhase资产或Editor session真相。

## Canonical Data Flow

```text
Unity Animation Window
  -> native AnimationClip registered curves
  -> Character Presentation Profile direct Clip bindings + Sync Groups
  -> Foot Analysis artifact and Gameplay committed clock contract
  -> Character Presentation Projection Build
  -> per-clip forward/inverse Phase plan + reachable relation plan
  -> Runtime raw source clock
  -> source phase
  -> target effective Clip time
  -> ClipPlayer sample
  -> Standard Blend / Action Slot / Pose Plan
```

任何阶段输入缺失或不相容都失败，不读取旧Sequence、不退回normalized time、不现场运行Foot search。

## AnimationClip Registered Curves

项目只注册当前正式消费的两项Float Curve：

```text
presentation.locomotion-phase
presentation.foot-placement-weight
```

注册表唯一规定channel id、Unity EditorCurveBinding、值域、切线限制、是否必填和Projection降低方式。Animation Window通过正式Preview Target上的作者曲线接收器显示与编辑这些曲线；曲线接收器只服务AnimationMode作者预览，Runtime不从MonoBehaviour字段读取结果。

`presentation.foot-placement-weight`值域固定为`[0,1]`。全部可达Clip必须具有该曲线；没有隐式常量1。现有Sequence曲线迁入Clip后删除Sequence副本。

`presentation.locomotion-phase`只要求于Locomotion Sync Group成员。它保存展开相位而不是归一化时间：

```text
phase整数       = RightFootContact
phase整数 + 0.5 = LeftFootContact
```

- 曲线在实际使用区间内必须有限且严格单调递增。
- Cyclic Clip首尾Phase差必须为正整数，循环首尾模1相同。
- Finite Clip可以覆盖不足或超过一个周期，但每条可达Transition使用的实际业务区间必须完整落在曲线coverage内。
- Curve key可以非线性分布，用于表达脚步在区间内的真实快慢；Runtime不假设Marker间线性。
- 作者不得用Phase值伪造与Foot Analysis相反的接触侧；Build质量门槛会拒绝。

AnimationClip的Loop事实从正式Clip设置解析。DefaultPlayRate删除；持续Pose默认1倍，业务变速只能由明确Player参数或Timeline Segment时间变换拥有。

## Direct Clip References

Profile-owned Pose Source Binding直接保存AnimationClip对象引用。Blend Space sample和Timeline Animation Segment同样直接保存AnimationClip对象引用。三者不共享可写对象，但引用同一Clip时共享同一素材曲线真相。

AnimationClip持久身份使用`assetPath + assetGuid + signed non-zero localFileId`。Projection依赖使用Unity dependency hash与注册曲线canonical hash；不创建Sequence AuthoringId或ContentRevision。

同一Character Profile内一个Clip只能有一套注册曲线。若业务确实需要同一骨骼内容的不同Phase或Foot Weight，作者必须创建另一份明确原生`.anim`，不能在Binding、Sample或Segment增加override。

## Locomotion Sync Group

Profile中的Locomotion Sync Group只保存：

```text
GroupId
Member AnimationClip references
```

Phase channel固定为`presentation.locomotion-phase`，不成为每组可调字符串。Group不保存Marker、Time Mapping、Topology或SyncRole：

- Topology从AnimationClip Loop事实解析。
- source/target关系从PoseState可达edge解析。
- phase leader从Transition时两侧正式raw clock authority和有限生命周期解析。
- 同步策略只有Locomotion Phase，不存在同组内策略分叉。

一个Clip最多属于一个Locomotion Sync Group。没有加入Group的Clip保持自己的raw time，不参与相位关系。

## Phase Compilation

Projection Compiler首先为每个Group成员编译per-clip计划：

```text
AnimationClipPhasePlan
  ClipIdentity
  ClipDependencyHash
  CurveHash
  Duration
  Loop
  ForwardKnots(time -> unwrapped phase)
  InverseKnots(unwrapped phase -> time)
```

Compiler按固定误差和容量约束将作者曲线降低为严格单调knots。误差或容量超限直接失败，不改用原曲线Runtime求值。

随后把每个可达state-local source降低为统一endpoint：

```text
AnimationSourcePhasePlan
  SourceIndex
  SourceKind = DirectClip | BlendSpace
  ClockCarrierClipPlanIndex
  DynamicSampleClipPlanIndices
  ActualCoverage
```

Direct Clip的clock carrier就是自身。Blend Space必须显式指定Dynamic Phase Reference Sample作为clock carrier；其它Dynamic Sample只消费同一个unwrapped phase，不保存reference-to-sample配对warp。随后只为PoseState实际可达edge建立relation：

```text
AnimationPhaseRelationPlan
  RelationIdentity
  LeaderSourcePhasePlanIndex
  FollowerSourcePhasePlanIndex
  LeaderClockAuthority
  LeaderActualCoverage
  FollowerActualCoverage
```

实际coverage来自Gameplay committed movement clock、有限state lifecycle、ClipIn和Player使用方式的联合结果。MovingTurn因此只使用0-28帧，不再按Clip 0-71帧假设业务出口。

Foot Analysis对每条relation执行Build质量校验：

- Phase整数和半整数附近的左右脚Plant侧正确。
- 实际Transition建立时刻与整个Blend可见窗口存在目标Phase coverage。
- 有限source终点的左右脚接触、位置、高度与速度能由目标Clip候选Phase表达。
- Phase局部速度、inverse斜率和跨循环展开不超过versioned算法门槛。
- 关系不存在只能靠双脚错误混合才能连接的区间。

质量门槛属于versioned compiler algorithm，不成为每条Transition的可调容差。

## Runtime Mapping

Runtime只执行：

```text
leader source raw time
  -> leader clock carrier forward phase
  -> 按follower source raw continuation选择最近合法unwrapped cycle
  -> Direct Clip follower: 自身inverse time
  -> Blend Space follower: 每个正权重Dynamic Sample各自inverse time
  -> follower effective sample
```

Runtime不读取AnimationClip曲线、Profile、Foot Analysis artifact或AssetDatabase；不搜索Pose、不计算cost、不选择其它策略。Plan identity、curve hash、coverage或数值无效时进入typed invalid并阻止本帧正式Pose发布。

删除Marker occurrence、segment ordinal、SyncRole、warp plan identity和follower Marker cursor。保留的continuation只属于Clip raw/effective clock，不形成第二生命周期。

## Blend Space

Blend Space sample直接引用AnimationClip。Phase policy收敛为：

```text
SharedNormalizedPhase
LocomotionPhase
```

`SharedNormalizedPhase`只允许作者明确保证全部Dynamic sample按相同normalized time创作。`LocomotionPhase`要求资产显式选择一个Dynamic Phase Reference Sample作为source raw clock carrier，全部Dynamic sample属于同一Profile Sync Group并具有合法per-clip Phase plan。Reference只通过自己的forward plan产生canonical unwrapped phase；全部Dynamic sample分别通过自己的inverse plan采样。Stationary sample继续使用固定normalized time，不参加Phase inverse。

Blend Space内部child时间与PoseState edge外部source关系复用同一per-clip plan，不保存reference-to-sample pairwise warp，也不动态选择最高权重sample作为leader。

## Action Timeline

Timeline Animation Segment直接引用AnimationClip，只保存编排字段。Action Timeline没有素材Marker、Phase、Foot Weight或Notify副本。Projection从Clip提取Foot Weight curve；Action不参加Locomotion Sync Group。

当前没有正式Action Marker Sync或Sequence Notify内容，因此对应authoring、Projection、Runtime、Snapshot和Preview全部删除。未来出现明确业务消费方时必须新建proposal，不能保留空能力。

## Animation Authoring Surface

Unity Animation Window是素材时间编辑器。项目提供薄入口完成：

1. 解析精确Character Definition、Profile、AnimationClip和Preview Target。
2. 确保Preview Target包含正式作者曲线接收器。
3. 选择Preview Target并把Animation Window切换到精确Clip。
4. 显示Clip注册曲线缺失、Artifact状态和Build诊断摘要。
5. Foot Analysis候选只有在作者显式应用时写入Locomotion Phase曲线。

项目不注入Animation Window内部lane，不保存它的播放、selection或viewport状态，不通过反射访问`AnimationWindowState`。

Timeline Editor只编辑Action Timeline。它可以双击Segment打开精确AnimationClip，但不显示Sequence tab、素材Marker lane、素材Curve lane或Sequence Preview。

## Agent Document v4

Document删除：

```text
editable/animation-sequences/**
```

并对当前Character Definition闭包内的现有原生AnimationClip导出：

```text
editable/animation-clips/<stable-segment>/curves.json
```

分片只包含：

- 精确AnimationClip结构化对象引用。
- Clip dependency baseline。
- 两项注册Curve中该Clip正式允许的完整canonical key payload。

不包含骨骼曲线、AnimationEvent、Loop import配置、Rig、Artifact或generated plan。Agent不能创建AnimationClip；新Clip只从`context/asset-catalog.json`选择现有原生`.anim`。

`editable/presentation/profile.json`保存直接Clip Binding与Locomotion Sync Group；`editable/timelines/**/timeline.json`保存Segment到Clip的结构化引用。Reconciler将Clip curve、Profile、Pose Graph、PoseStateMachine和Timeline变化降低为同一immutable Mutation Plan，并由现有Application Service执行一个Undo事务、保存与canonical reverse export。AI domain继续使用同一包模型但不能出现Character Clip分片。apply不Build。

旧v3、Sequence package字段、reader、writer、reconciler与Mutation全部删除，不提供迁移reader。正式内容通过一次显式仓库迁移与Document重新checkout进入v4。

## Corin Migration

1. 把19个Sequence的Foot Placement Weight曲线无损写入对应19个原生`.anim`注册channel。
2. 让全部Pose Binding、Blend Space Sample和Action Timeline Segment直接引用对应Clip。
3. 删除全部Sequence资产、Notify数据、DefaultPlayRate、Rig/Analysis副本和Document Sequence分片。
4. 为Walk/Run循环和合法Start素材作者正式Locomotion Phase曲线。
5. MovingTurn不迁移当前错误0-71 Marker计划；先按实际0-28业务coverage作者Phase并由Foot Analysis质量门槛检查。
6. 当前MovingTurn内容无法接入RunLoop时保持Build失败，直到动画内容或Gameplay生命周期被正式重做；不得发布旧Projection。
7. Corin Locomotion Transition全部迁移为Standard Blend并删除未引用Inertialization配置。
8. 显式重建Foot Analysis、Presentation Projection、Float32/Fixed Program与Native Pose产品。

## Deletion Inventory

- `AnimationSequenceAsset`、`CharacterAnimationSequenceAsset`与analysis reference接口。
- Sequence asset inspector、Timeline Sequence mode、Sequence time adapter、Sequence Preview session。
- `CharacterSequencePoseSourceBinding`、Sequence Blend Space sample与Timeline Sequence Segment引用。
- Sequence Notify kind、payload、authoring、Projection、snapshot、preview与runtime。
- Animation Marker Sync authoring、group、role、topology、point marker和time mapping枚举。
- `AnimationFootSynchronizationDescriptor`与artifact同步payload。
- `AnimationFootPhaseTimeWarpCompiler`、pairwise plan、knot、Projection payload和validation。
- `MarkerSegmentTimeMapper`、relation cursor、occurrence runtime和相关diagnostics。
- Document v3、Sequence schema、fragment、codec、exporter、reconciler、mutation与manifest discovery。
- Corin 19个Sequence资产及全部旧generated Projection payload。

## Failure Policy

以下情况在authoring或Document apply失败：

- Clip不是可写原生`.anim`。
- 注册Curve binding、值域、切线或key payload非法。
- Profile Group引用重复、不可达或不属于当前Definition闭包的Clip。
- Timeline/Profile/Blend Space引用旧Sequence或未知Clip对象。

以下情况在Projection Build失败：

- 必填Foot Weight或Group成员Phase曲线缺失。
- Phase非有限、非严格单调、循环跨度非法或reduction超限。
- 实际业务coverage超出Phase coverage。
- Foot Analysis与作者Phase接触侧不一致。
- 可达Transition没有相容目标Phase或有限出口质量超限。

以下情况在Runtime进入typed invalid：

- Projection relation与Clip/curve identity不匹配。
- forward/inverse plan无序、越界或求值非有限。
- follower cycle展开回退或超出有限coverage。

所有失败都不读取旧Sequence、不回退normalized time、不禁用同步继续播放、不保留上一帧effective time。

## Decisions And Tradeoffs

### 选择Clip内注册曲线，而不是Profile内AnimationCurve副本

Clip内曲线能直接使用Unity Animation Window，并与骨骼动画共享时间、frame rate和Undo对象。代价是修改表现曲线会dirty原生动画资产，且导入FBX子Clip不能直接作为作者目标。项目正式内容已经归一化为原生`.anim`，用这一限制换取删除Sequence资产和第二素材编辑器更符合当前demo范围。

### 选择Profile-owned Sync Group，而不是Clip自带Group字段

Group表达角色PoseState之间的装配关系，不是动画素材本体。同一个Clip在不同角色上的Rig、Analysis与可达关系可能不同。把Group放在Profile会保留角色局部性，代价是单独打开Clip无法看到完整组关系；Animation入口必须携带精确Definition context。

### 选择作者Phase曲线，而不是GeneratedFootPhase pairwise warp

作者Phase可见、可审查、每Clip只保存一份，Projection只编译forward/inverse plan；pairwise warp能自动细化每对动画，但数据量与失败面随可达关系增长，而且当前实现会强制拟合不相容素材。项目选择更少的自动猜测和更强的Build拒绝，代价是动画作者必须调整Phase曲线及不相容的Turn内容。

### 选择Unity Animation Window，而不是继续深化Sequence Editor

Animation Window已经负责骨骼曲线、关键帧、切线和Preview交互。继续维护Sequence时间轴会复制成熟编辑能力。代价是项目不能注入自定义Marker lane；正式模型因此使用Float Phase曲线而不是离散Marker UI。

### 删除Notify，而不是迁移到AnimationEvent

当前19个Sequence没有Notify内容，也没有业务消费者。迁移到AnimationEvent会保留未使用接口并引入Unity回调副作用。删除后系统更小；未来真实脚步声或VFX需求必须以明确消费方重新设计。

### 拒绝导入子Clip，而不是自动生成可写副本

自动复制会制造隐藏资产、identity漂移和Build副作用。显式归一化让作者知道正式素材是什么，代价是新增外部动画时多一步内容准备。

### 提升Document v4，而不是原地改变v3

删除Marker/Sequence字段并新增Clip Curve分片会让旧package的同名字段产生不同语义。提升到v4能让schema门禁在Mutation前解释失败原因；代价是现有v3工作目录必须重新checkout。项目不需要长期兼容AI工作副本，因此选择一次性失效，而不是维护升级器。

## Verification Design

提案阶段只执行OpenSpec strict validation，不修改实现或Unity资产。实施阶段按依赖顺序验证：

1. 静态删除审计确认代码、资产和Document schema中不存在Sequence、Marker Sync、GeneratedFootPhase和Corin Locomotion Inertialization正式引用。
2. 使用禁用共享编译服务的参数构建受影响Runtime与Editor工程，并立即关闭.NET build server。
3. Character Build对缺失Curve、Phase非单调、错误接触侧、coverage越界和MovingTurn到RunLoop不相容分别返回typed failure，且不发布旧Projection。
4. Unity人工作者链从Profile、Blend Space和Action Workspace打开同一原生AnimationClip，确认都进入Animation Window且读取同一注册Curve。
5. 用户在真实Gameplay中验证`TurnBack -> RunLoop`、`MovingTurn -> RunLoop`、Walk/Run切换和Start进入Loop；观察左右脚接触顺序、腿部混合和Standard Blend，不使用离线重放。
6. Document v4执行checkout、修改Clip Curve/Profile Group/Timeline Clip引用、dry-run、同hash apply、reverse export与validate；v3 package必须在Mutation前拒绝。

## OpenSpec Reconciliation

- 删除active `separate-animation-sequence-authoring`完整目录；其Sequence owner、双文档Editor与Document fragment不再成立。
- 删除active `add-generated-foot-phase-animation-sync`完整目录；其同步descriptor、pairwise warp与Runtime mapper不再成立。
- 修改active `add-character-presentation-blend-space`中的Sequence、MarkerSynchronizedPhase、GeneratedFootPhase和Marker source sync口径。
- 修改current specs中全部`SequencePlayer`、Sequence Binding、Marker Group、AnimationTrack Marker Sync和Marker effective time要求。
- 把Agent Character、Agent AI、Document Sync、MCP Bridge、Graph Authoring和相关Presentation能力统一提升到Document v4。
- `character-foot-placement-presentation`继续只消费Projection中的Foot Feature与最终Pose，不读取Phase curve或Group。
- Motion Matching、Gameplay Program ABI、Snapshot、Network协议、Root Motion、MotionWarp、FullBodyIK和WorldSolver保持原owner数量与输入输出。
