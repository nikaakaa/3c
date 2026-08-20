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

把控制Curve搬进AnimationClip还会改变现有依赖边界：当前Foot Analysis Artifact使用完整Clip dependency hash，而注册Curve写回同一Clip会改变该hash。如果不拆分分析输入与表现控制依赖，应用Phase候选或修改Foot Weight会立即使生成候选所依据的Artifact过期，并与“Curve变化只使Projection stale”的目标冲突。当前Motion Matching又按Runtime参数名`animation.foot-placement-weight`直接搜索Clip Curve，而Sequence作者channel使用`presentation.foot-placement-weight`；不统一会在删除Sequence后留下第二条正式Curve链。

## Goals

- 让AnimationClip成为唯一素材时间真相。
- 让Unity Animation Window成为唯一素材时间编辑表面。
- 让Profile只保存角色级装配、Rig、Analysis和Sync Group。
- 分离骨骼/Root分析输入身份与注册表现Curve身份，阻止作者Curve反向污染Foot Analysis Artifact。
- 让全部Clip消费者通过唯一Curve catalog把作者channel降低为Runtime参数。
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
| 角色Rig、Foot Analysis Source、Pose Source Binding、Action producer引用 | CharacterAnimationPresentationProfile | Profile/Pose authoring与Agent Document | Projection dense binding |
| Locomotion Sync Group成员 | CharacterAnimationPresentationProfile | Profile Sync Group authoring与Agent Document | Projection relation inventory |
| PoseState Transition与Blend | PoseStateMachine edge | PoseStateMachine authoring | compiled Routing Plan |
| Action Start/End、ClipIn、Weight、Ease、Window、Cue、Motion、Warp | Gameplay Timeline | Timeline Editor | Program与Action Presentation计划 |
| Foot Analysis Artifact与Phase Validation Descriptor | Library generated product | 显式Analyzer Build | Projection Build校验与Foot Feature payload |
| effective source time与relation generation | source-local Animation Phase Runtime | 无 | raw clock + compiled phase plan |

Animation Editor是写入口，不是数据owner。AnimationClip保存时间数据；Profile保存角色装配。Blend Space与Motion Matching资源 MAY保存自身Artifact所需的Rig/Analysis兼容身份，但这些身份只作为资源准入约束，不能改写Profile的角色级选择，也不能由Binding或Action producer再保存一份可写配置。不存在Sequence、Marker资产、FootPhase资产或Editor session真相。

## Canonical Data Flow

```text
Unity Animation Window
  -> native AnimationClip registered curves
  -> registered curve catalog author-channel lowering
  -> Character Presentation Profile direct Clip bindings + Sync Groups
  -> AnimationClipAnalysisInputHash + Foot Analysis artifact
  -> Gameplay committed clock contract
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

注册表唯一规定channel id、精确Unity `EditorCurveBinding(path + type + property)`、秒域、值域、切线限制、是否必填和Projection降低方式。Animation Window通过正式Preview Target上的作者曲线接收器显示与编辑这些曲线；Production Prefab不安装该接收器，Runtime不从MonoBehaviour字段读取结果。Authoring、Document、Projection与Motion Matching resolver必须按完整binding匹配，不能只按`propertyName`搜索。

全部注册Curve的key time使用Clip秒域。基础素材时长`SourceDurationSeconds`只从排除注册表现Curve的骨骼/Root曲线与正式Clip设置计算；注册Curve不得延长或重新定义该时长。现有Sequence归一化key迁移时使用`key.time * SourceDurationSeconds`，切线按时间缩放保持原曲线形状。

`presentation.foot-placement-weight`值域固定为`[0,1]`，key coverage必须完整覆盖`[0, SourceDurationSeconds]`。全部可达Clip必须具有该曲线；没有隐式常量1。现有Sequence曲线迁入Clip后删除Sequence副本。唯一catalog把该作者channel降低为Runtime `animation.foot-placement-weight`参数；Direct Clip、Action、Blend Space与Motion Matching不得各自声明第二binding或第二channel。

`presentation.locomotion-phase`只要求于Locomotion Sync Group成员。它保存展开相位而不是归一化时间：

```text
phase整数       = RightFoot Landing / Plant onset
phase整数 + 0.5 = LeftFoot Landing / Plant onset
```

- 曲线在声明coverage内必须有限且连续严格单调递增；validator必须检查Hermite段内部导数与切线过冲，不能只比较key value。
- Cyclic Clip coverage固定为`[0, SourceDurationSeconds]`，运行时本地时钟使用半开区间`[0, SourceDurationSeconds)`；首尾Phase差必须为正整数，循环首尾模1相同。
- Finite Clip的Phase coverage只由首尾正式key时间定义，可以不足或超过一个周期，但每条可达Transition使用的实际业务秒域必须完整落在该coverage内；Unity Curve外推或末key保持不扩大coverage。
- Curve key可以非线性分布，用于表达脚步在区间内的真实快慢；Runtime不假设Marker间线性。
- 作者不得用Phase值伪造与Foot Analysis相反的接触侧或把整数/半整数放在支撑中段；Build必须校验对应脚的Landing/Plant onset时间误差、对侧脚状态与左右接触顺序。

AnimationClip的Loop事实从正式Clip设置解析。ClipPlayer、Binding、Document、Projection和Runtime不保存第二个Loop字段。DefaultPlayRate删除；持续Pose默认1倍，业务变速只能由明确Player参数或Timeline Segment时间变换拥有。

## AnimationClip Dependency Boundaries

同一原生Clip生成两种不同用途的身份：

```text
AnimationClipAnalysisInputHash
  = canonical bone/root curves
  + formal loop setting
  + SourceDurationSeconds

AnimationClipRegisteredCurveHash
  = exact registered bindings
  + canonical key/tangent/weight/wrap payload
```

Foot Analysis Artifact identity只使用`AnimationClipAnalysisInputHash`、Analysis Source、Rig、Sampling Rig、Calibration与Geometry Validation。Phase候选同时锁定Artifact identity、AnalysisInputHash与候选生成时的RegisteredCurveHash；Apply前任一基线变化都拒绝。Apply成功只改变RegisteredCurveHash并使相关Projection stale，Artifact继续Ready。骨骼、Root、Loop或基础素材时长变化才使Artifact stale。

Projection dependency继续包含完整Unity dependency hash、AnalysisInputHash、RegisteredCurveHash与Artifact validation identity，用于区分资产并发变化和正式运行输入。Document v4的Clip分片使用完整dependency baseline做乐观并发锁，但不得把该baseline误当成Foot Analysis输入身份。

## Direct Clip References

Profile-owned Pose Source Binding直接保存AnimationClip对象引用。Blend Space sample和Timeline Animation Segment同样直接保存AnimationClip对象引用。三者不共享可写对象，但引用同一Clip时共享同一素材曲线真相。

AnimationClip持久身份使用`assetPath + assetGuid + signed non-zero localFileId`。Projection依赖使用完整Unity dependency hash、`AnimationClipAnalysisInputHash`与注册曲线canonical hash；不创建Sequence AuthoringId或ContentRevision。

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
- phase leader从Transition两侧正式raw clock authority和整个Blend可见窗口coverage按固定规则解析，并在一个relation generation内保持不变。
- 同步策略只有Locomotion Phase，不存在同组内策略分叉。

一个Clip最多属于一个Locomotion Sync Group。没有加入Group的Clip保持自己的raw time，不参与相位关系。

## Phase Compilation

Projection Compiler首先为每个Group成员编译per-clip计划：

```text
AnimationClipPhasePlan
  ClipIdentity
  FullClipDependencyHash
  AnalysisInputHash
  RegisteredCurveHash
  SourceDurationSeconds
  CurveCoverageSeconds
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
  ActualCoverageSeconds
```

Direct Clip的clock carrier就是自身。Blend Space必须显式指定Dynamic Phase Reference Sample作为clock carrier；其它Dynamic Sample只消费同一个unwrapped phase，不保存reference-to-sample配对warp。随后只为PoseState实际可达edge建立relation：

```text
AnimationPhaseRelationPlan
  RelationIdentity
  TransitionId
  LeaderSourcePhasePlanIndex
  FollowerSourcePhasePlanIndex
  LeaderClockAuthority
  LeaderActualCoverage
  FollowerActualCoverage
```

实际coverage统一使用秒域，来自Gameplay committed movement clock、有限state lifecycle、Timeline frame到秒的正式换算、ClipIn和Player使用方式的联合结果。MovingTurn的Gameplay Timeline以正式Timeline frame rate把0-28帧换算为秒，因此只使用该区间，不再按Clip 0-71帧假设业务出口。

Foot Analysis Artifact删除旧pairwise warp descriptor，但保留新的Editor-only `AnimationFootPhaseValidationDescriptor`。Descriptor按`AnimationClipAnalysisInputHash`保存左右脚随规范化素材时间采样的root-local平面位置、calibrated height、local velocity、Plant confidence与Landing/Plant onset事件；它只服务候选生成与Projection Build质量门槛，不进入Projection或Runtime。

Foot Analysis对每条relation执行Build质量校验：

- Phase整数和半整数的inverse时间分别与右/左脚Landing/Plant onset位于固定误差内，对侧脚状态与接触顺序正确。
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

## Phase Relation Lifetime

Compiler使用固定leader规则，不保留作者SyncRole：

1. 若两侧clock authority不同，`CommittedMovement`优先于`PresentationDelta`，前提是其Phase coverage覆盖完整Blend可见窗口。
2. 若两侧authority相同，Transition建立时的outgoing source优先。
3. 优先候选coverage不足时可选择另一侧；两侧都不能覆盖完整窗口则Build失败。
4. leader一旦写入`AnimationPhaseRelationPlan`，同一runtime generation内不得按weight、sample、clock进度或有限端点动态更换。

Runtime用`RelationIdentity + TransitionId + TransitionGeneration`建立唯一relation generation。Transition replacement先释放旧generation，再建立新generation；反向边拥有自己的plan与generation。正常release时follower从最后effective time建立自己的raw/effective continuation anchor并删除relation generation；State `AlwaysResetOnEntry`先重置Player raw clock，再建立新relation；Projection replacement、Presentation Reset与Dispose直接清除generation和continuation。旧relation不得跨reset、branch replacement或Projection identity复用。

## Blend Space

Blend Space sample直接引用AnimationClip。Phase policy收敛为：

```text
SharedNormalizedPhase
LocomotionPhase
```

`SharedNormalizedPhase`只允许作者明确保证全部Dynamic sample按相同normalized time创作。`LocomotionPhase`要求资产显式选择一个Dynamic Phase Reference Sample作为source raw clock carrier，全部Dynamic sample属于同一Profile Sync Group并具有合法per-clip Phase plan。Reference只通过自己的forward plan产生canonical unwrapped phase；全部Dynamic sample分别通过自己的inverse plan采样。Stationary sample继续使用固定normalized time，不参加Phase inverse。

Blend Space内部child时间与PoseState edge外部source关系复用同一per-clip plan，不保存reference-to-sample pairwise warp，也不动态选择最高权重sample作为leader。

Motion Matching不加入Locomotion Sync Group，也不创建Phase follower，但其Clip Foot Placement Weight必须通过同一注册Curve catalog解析。`presentation.foot-placement-weight`只在Editor/Document作者层存在，Projection compiler把它降低为既有`animation.foot-placement-weight`Runtime参数；MM resolver不得再仅按Runtime参数名或`propertyName`搜索Curve。

## Action Timeline

Timeline Animation Segment直接引用AnimationClip，只保存编排字段。Action Timeline没有素材Marker、Phase、Foot Weight、Foot Analysis identity或Notify副本。Profile的Action producer binding只保存producer到Timeline/Track的正式引用；Projection从Profile Analysis Source与Clip身份解析Artifact并从Clip提取Foot Weight curve。Action不参加Locomotion Sync Group。

当前没有正式Action Marker Sync或Sequence Notify内容，因此对应authoring、Projection、Runtime、Snapshot和Preview全部删除。未来出现明确业务消费方时必须新建proposal，不能保留空能力。

## Animation Authoring Surface

Unity Animation Window是素材时间编辑器。项目提供薄入口完成：

1. 解析精确Character Definition、Profile、AnimationClip和Preview Target。
2. 确保Preview Target包含正式作者曲线接收器，并校验Production Prefab不安装该接收器。
3. 选择Preview Target并把Animation Window切换到精确Clip。
4. 显示Clip注册曲线缺失、Artifact状态和Build诊断摘要。
5. Foot Analysis候选只有在作者显式应用时写入Locomotion Phase曲线。

项目不注入Animation Window内部lane，不保存它的播放、selection或viewport状态，不通过反射访问`AnimationWindowState`。注册binding属于Clip内真实float curve，因此Build审计必须证明Animancer/Native Pose只消费骨骼Pose与Projection参数，不读取接收器字段、不让该字段成为第二Runtime状态。

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
- 完整Clip dependency baseline与只读`AnimationClipAnalysisInputHash`。
- 两项注册Curve中该Clip正式允许的秒域完整canonical key payload。

不包含骨骼曲线、AnimationEvent、Loop import配置、Rig、Artifact或generated plan。Agent不能创建AnimationClip；新Clip只从`context/asset-catalog.json`选择现有原生`.anim`。

`editable/presentation/profile.json`保存直接Clip Binding与Locomotion Sync Group；`editable/timelines/**/timeline.json`保存Segment到Clip的结构化引用。Reconciler将Clip curve、Profile、Pose Graph、PoseStateMachine和Timeline变化降低为同一immutable Mutation Plan，并由现有Application Service在首次Mutation前锁定全部Clip/Profile/Timeline owner，执行一个Undo事务、保存与canonical reverse export。AI domain继续使用同一包模型但不能出现Character Clip分片。apply不Build；注册Curve Mutation只使Projection stale，不直接修改或重建Foot Analysis Artifact。

旧v3、Sequence package字段、reader、writer、reconciler与Mutation全部删除，不提供迁移reader。正式内容通过一次显式仓库迁移与Document重新checkout进入v4。

## Corin Migration

1. 先升级Foot Analysis Artifact identity与`AnimationFootPhaseValidationDescriptor`，按不含注册Curve的`AnimationClipAnalysisInputHash`显式重建Corin分析产物。
2. 把19个Sequence的归一化Foot Placement Weight曲线按`SourceDurationSeconds`无损换算成秒域并写入对应19个原生`.anim`唯一注册channel。
3. 让全部Pose Binding、Blend Space Sample和Action Timeline Segment直接引用对应Clip，并删除Action producer的Foot Analysis identity副本。
4. 删除全部Sequence资产、Notify数据、DefaultPlayRate、Player Loop、Rig/Analysis副本和Document Sequence分片。
5. 为Walk/Run循环和合法Start素材作者正式Locomotion Phase曲线；Curve写回不使步骤1的Artifact过期。
6. MovingTurn不迁移当前错误0-71 Marker计划；先把Gameplay 0-28帧按正式Timeline frame rate换算为秒域coverage，再作者Phase并由Foot Analysis质量门槛检查。
7. 当前MovingTurn内容无法接入RunLoop时保持Build失败，直到动画内容或Gameplay生命周期被正式重做；不得发布旧Projection。
8. Corin Locomotion Transition全部迁移为Standard Blend并删除未引用Inertialization配置。
9. 显式重建Presentation Projection、Float32/Fixed Program与Native Pose产品；只有分析输入真实变化时才再次重建Foot Analysis。

## Deletion Inventory

- `AnimationSequenceAsset`、`CharacterAnimationSequenceAsset`与analysis reference接口。
- Sequence asset inspector、Timeline Sequence mode、Sequence time adapter、Sequence Preview session。
- `CharacterSequencePoseSourceBinding`、Sequence Blend Space sample与Timeline Sequence Segment引用。
- Sequence Notify kind、payload、authoring、Projection、snapshot、preview与runtime。
- Animation Marker Sync authoring、group、role、topology、point marker和time mapping枚举。
- `AnimationFootSynchronizationDescriptor`与pairwise warp payload；质量校验所需数据迁入新的Editor-only `AnimationFootPhaseValidationDescriptor`，不保留旧类型。
- `AnimationFootPhaseTimeWarpCompiler`、pairwise plan、knot、Projection payload和validation。
- `MarkerSegmentTimeMapper`、relation cursor、occurrence runtime和相关diagnostics。
- Document v3、Sequence schema、fragment、codec、exporter、reconciler、mutation与manifest discovery。
- Corin 19个Sequence资产及全部旧generated Projection payload。

## Failure Policy

以下情况在authoring或Document apply失败：

- Clip不是可写原生`.anim`。
- 注册Curve完整binding、秒域、基础时长、coverage、值域、切线或key payload非法。
- Profile Group引用重复、不可达或不属于当前Definition闭包的Clip。
- Timeline/Profile/Blend Space引用旧Sequence或未知Clip对象。

以下情况在Projection Build失败：

- 必填Foot Weight或Group成员Phase曲线缺失。
- Analysis Input Hash、Artifact validation identity或Registered Curve Hash不匹配。
- Phase非有限、Hermite段不严格单调、循环跨度非法或reduction超限。
- 实际业务coverage超出Phase coverage。
- Foot Analysis Landing/Plant onset、对侧脚状态或接触顺序与作者Phase不一致。
- 可达Transition没有相容目标Phase或有限出口质量超限。

以下情况在Runtime进入typed invalid：

- Projection relation与Clip/curve identity不匹配。
- forward/inverse plan无序、越界或求值非有限。
- follower cycle展开回退或超出有限coverage。
- relation generation、TransitionGeneration或continuation anchor与当前branch不匹配。

所有失败都不读取旧Sequence、不回退normalized time、不禁用同步继续播放、不保留上一帧effective time。

## Decisions And Tradeoffs

### 选择Clip内注册曲线，而不是Profile内AnimationCurve副本

Clip内曲线能直接使用Unity Animation Window，并与骨骼动画共享时间、frame rate和Undo对象。代价是修改表现曲线会dirty原生动画资产，且导入FBX子Clip不能直接作为作者目标。项目正式内容已经归一化为原生`.anim`，用这一限制换取删除Sequence资产和第二素材编辑器更符合当前demo范围。

### 选择Analysis Input Hash，而不是把完整Clip dependency作为Foot Analysis身份

完整Clip dependency适合并发锁和Projection失效，但注册表现Curve也会改变它；继续复用会让Phase候选写回立即使自己的Artifact过期。独立Analysis Input Hash只覆盖骨骼、Root、Loop与基础时长，代价是需要canonical过滤和单独版本身份，但能让分析产物与作者控制Curve保持正确单向依赖。

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
3. Character Build对缺失Curve、Phase非单调、onset错误、coverage越界、identity不匹配和MovingTurn到RunLoop不相容分别返回typed failure，且不发布旧Projection。
4. Unity人工作者链从Profile、Blend Space和Action Workspace打开同一原生AnimationClip，确认都进入Animation Window且读取同一注册Curve。
5. 用户在真实Gameplay中验证`TurnBack -> RunLoop`、`MovingTurn -> RunLoop`、Walk/Run切换和Start进入Loop；观察左右脚接触顺序、腿部混合和Standard Blend，不使用离线重放。
6. Document v4执行checkout、修改Clip Curve/Profile Group/Timeline Clip引用、dry-run、同hash apply、reverse export与validate；注册Curve apply后Artifact保持Ready，v3 package必须在Mutation前拒绝。

## OpenSpec Reconciliation

- 早期`separate-animation-sequence-authoring`与`add-generated-foot-phase-animation-sync`已经不在active清单；只删除它们遗留在实现、current specs或文字中的Sequence/warp口径。
- active `add-character-presentation-blend-space`文档已经使用直接Clip与LocomotionPhase；本change迁移其仍为Sequence的实现并对账剩余任务，不建立第二phase模型。
- 修改current specs中全部`SequencePlayer`、Sequence Binding、Marker Group、AnimationTrack Marker Sync和Marker effective time要求。
- 把Agent Character、Agent AI、Document Sync、MCP Bridge、Graph Authoring和相关Presentation能力统一提升到Document v4。
- `character-foot-placement-presentation`继续只消费Projection中的Foot Feature与最终Pose，不读取Phase curve或Group。
- Motion Matching继续不参与Locomotion Phase relation，但其Foot Weight resolver改用唯一Curve catalog；Gameplay Program ABI、Snapshot、Network协议、Root Motion、MotionWarp、FullBodyIK和WorldSolver保持原owner数量与输入输出。
