# Design: 生成式左右脚相位动画同步

## Context

当前跨source同步链已经完整存在：

```text
PoseState Transition / AnimationSlot relation
    -> compiled leader/follower与MarkerGroup
    -> MarkerSegmentRelationCursor定位有向segment occurrence
    -> leader segment fraction
    -> follower segment start + fraction * follower segment duration
    -> source effective time
    -> Animancer sample
    -> Pose与Foot Feature
```

问题集中在最后一条比例换算。`MarkerSegmentTimeMapper.MapDetailed`当前直接使用leader fraction计算follower time；`CharacterAnimationBlendSpacePhaseMapper`也把Reference Sample fraction线性插值到每个child sample。Marker保证区间语义相同，但没有描述区间内部两只脚真实运动的非线性关系。

现有Foot Analysis已经用精确Sampling Rig、Rig Calibration和独立PlayableGraph采样左右脚heel、toe、sole、高度、速度、Plant Confidence与Landing Event。缺少的是一份只服务动画时间对应的连续描述，以及在Character Build阶段把两份描述编译为Runtime可直接查表的关系计划。

## Goals

- Transition条件成立后立即混合，不等待下一次脚接触。
- 在同一Marker有向区间内让source与target的左右脚接触、位置、摆动高度和运动方向尽量接近。
- 同一算法服务Sequence、合法Action relation与Blend Space child phase。
- 运行时固定容量、无AnimationClip采样、无动态搜索、无GC分配。
- 保持Marker authoring、Foot Analysis、Projection、source clock、Pose、FootGrounding与IK的唯一owner。

## Non-Goals

- 不根据当前地形、IK结果或最终混合骨骼反向修改动画时间。
- 不推迟Gameplay状态、Movement或Transition Routing。
- 不解决缺失左右脚版本的Start、Stop、Turn素材。
- 不把Motion Matching改造成Marker follower。
- 不建立通用自动动画配对数据库。

## Data Ownership

| 数据 | 唯一owner | 输入 | 输出 |
| --- | --- | --- | --- |
| SyncMode、Group、Topology、Role、Point Marker、Time Mapping | 实际AnimationTrack或Profile Pose source binding | 作者配置 | 可编译同步语义 |
| 双脚同步描述 | 现有Animation Foot Analysis artifact | Clip、Rig v4、Calibration v4、Sampling Rig、Analysis Settings | Editor-only连续特征 |
| Foot Phase Time Warp | Presentation Projection Compiler | 两侧artifact、marker occurrence、relation direction | 固定Projection plan |
| relation cursor与effective time | source-local Marker Sync runtime | raw clocks、compiled plan | follower effective clock |
| 接触、Anchor、Pelvis、Clearance | FootGrounding与Predictive Modifier | 映射后Pose和Foot Feature、world query | typed Goal Set |
| 最终腿链姿势 | 唯一FullBodyIK | Component Pose与Goal Set | solved Component Pose |

不存在独立FootPhase资产、运行时pair cache、Transition上的同步开关或IK到source clock的反馈。

## Playback Clock Ownership And Frame Sync

项目只有一个权威simulation tick，但不同业务通道拥有不同局部播放时钟。权威tick负责确定“哪个模拟结果属于哪一步”，不直接成为所有动画的播放位置：

```text
authority simulation tick
    -> producer自己的deterministic lifecycle/playhead
    -> Motion Contribution + CommittedMovementPlaybackClock原子resolve
    -> Body/Trajectory/Presentation Fact committed history
    -> render frame在相邻committed锚点之间投影raw source time
    -> Marker/GeneratedFootPhase把raw time映射成effective time
    -> Sequence Player采样
```

`CommittedMovementPlaybackClock`是目标数值后端无关的整数合同：

```text
OwnerIdentity
Generation
AuthorityTick
ContinuousTicks
TickRate
```

- Locomotion Input Motion以自身operation identity和activation generation为owner，在产生本tick motion delta时同时提交本tick结束位置的continuous ticks。
- MovingTurn的`TimelineMotionCurve`以Timeline operation和activation generation为owner，使用Timeline连续playhead提交Movement clock。它虽然写入Locomotion motion channel，但不因此变成Locomotion Input Motion。
- Sprint、Attack、Dodge属于Action lifecycle，继续使用Action committed sample history和Action playback identity，不得写入Movement clock。
- Motion resolve只能携带获胜Movement contribution已经提交的clock，不得在resolve后从operation state反查elapsed或generation。
- Presentation只消费committed clock锚点；它不得直接查询simulation tick、Timeline state或Locomotion operation。相同owner与generation可以插值，identity变化必须按新锚点接管。
- rollback重放重新产生每个authority tick的Motion与clock。outer transaction只发布最终分支；Presentation workspace、Sequence Player、relation cursor与effective time不进入snapshot或network。
- retained outgoing source锁定进入relevance时的完整clock identity。新Movement owner出现时，不能仅因两者都属于Movement就让旧source改绑新owner。
- Marker sync与GeneratedFootPhase没有clock ownership。它们只读取各source raw clock，输出本次采样的effective time；任何relation reset都不得归零producer clock。

### Clock identity切换规则

同一`OwnerIdentity + Generation`内，`AuthorityTick`与`ContinuousTicks`必须单调。发生回退说明producer提交、rollback净分支或clock路由违反合同，Runtime必须报告typed failure。owner或generation改变不是回退，而是明确的新局部时钟；Player从当前可见连续时间接管新anchor，随后只消费该identity。

### 与表现插值的关系

Committed clock不是最终渲染时间。它是和Body/Intent同一simulation result提交的raw anchor。Presentation按照当前authority sample position在前后锚点之间求值；当没有新simulation tick时，可按既有连续identity用presentation delta投影，但不得越过已知terminal或改写committed anchor。这样帧率可以高于模拟频率，同时rollback仍以simulation branch为准。

## Authoring Model

现有`AnimationSyncMode`继续只表达：

```text
Unspecified
None
MarkerGroup
```

MarkerGroup新增必须显式填写的：

```text
AnimationSyncTimeMapping
    Unspecified
    MarkerSegmentFraction
    GeneratedFootPhase
```

约束如下：

- `None`必须清空Time Mapping、Group、Topology、Role与Marker。
- `MarkerGroup`不得保存`Unspecified`。
- relation两侧必须使用同一个canonical group和同一个Time Mapping。
- `MarkerSegmentFraction`只使用现有marker时间。
- `GeneratedFootPhase`额外要求两侧精确Foot Analysis artifact与可编译同步描述。
- 策略不写在Transition，因为同一source被PoseState、Action Slot或Blend Space使用时必须保持同一业务语义。

这两个正式策略不是fallback关系。非步态Action可以明确选择`MarkerSegmentFraction`；Locomotion gait选择`GeneratedFootPhase`。任一策略自己的输入损坏都直接失败。

## Foot Synchronization Descriptor

### Artifact payload

现有Analyzer完成全部采样帧后，额外构造每脚同步描述：

```text
AnimationFootSynchronizationDescriptor
    SampleRate
    Duration
    Left[]
    Right[]

AnimationFootSynchronizationSample
    NormalizedTime
    RootLocalSolePlanarPosition
    CalibratedSoleHeight
    SoleLocalVelocity
    PlantConfidence
```

`RootLocalSolePlanarPosition`来自同一帧heel/toe中点并转换到同一Visual Root局部空间。高度、速度和Plant Confidence复用同一Analyzer采样结果，不重新采样Clip。

描述保存在Editor-only artifact，不直接进入普通Runtime Foot Feature curve。这样Runtime不会为每个source携带只在Build时使用的完整位置序列。

Artifact identity必须覆盖同步描述算法、归一化规则、采样率与reduction。旧artifact没有该描述时直接Stale。

### 与contact candidate的区别

现有contact Marker candidate仍是Editor session中的应用建议，不写artifact。同步描述只是按时间排列的数值特征：

- 不保存MarkerId。
- 不修改Profile或Timeline。
- 不声明世界接触。
- 不被FootGrounding直接消费。
- 不进入Gameplay Program、Snapshot或Network。

## Warp Compilation

### Relation inventory

Projection Compiler只为实际可达关系编译计划：

- 每条PoseState Transition解析出的source/target relation。
- 每个AnimationSlot中编译可达且明确选择`GeneratedFootPhase`的source pair。
- 每个Blend Space固定Phase Reference到各DynamicCycle sample的关系。

不生成全动画库N×N pair table，也不按名称扫描目录。

### Segment boundaries

Marker仍负责把时间划分为稳定有向区间，例如：

```text
RightFootContact -> LeftFootContact
LeftFootContact  -> RightFootContact
```

每张warp表只处理一个精确leader occurrence与一个精确follower occurrence：

```text
leader segment fraction [0, 1]
    -> follower segment fraction [0, 1]
```

端点固定：

```text
Warp(0) = 0
Warp(1) = 1
```

因此相邻segment在Marker边界连续，Cyclic wrap仍由现有cycle展开处理。

### Feature normalization

编译器在每个segment内分别规范化左右脚描述，避免Walk与Run的绝对步幅和速度量级直接支配成本：

- root-local平面位置转换为该segment起点到终点的相对轨迹，并按Calibration heel-to-toe尺度与segment有效行程归一化。
- sole height相对各自segment基线并按Calibration尺度归一化。
- local velocity分成方向和该segment内的归一化运动强度。
- Plant Confidence保持`[0, 1]`。

有效行程不足、非有限值、左右脚样本数不足或Calibration尺度非法时拒绝生成计划。

### Alignment cost

每个leader sample `i`与follower sample `j`的成本由两只脚同时组成：

```text
Cost(i, j) =
    left plant mismatch
  + right plant mismatch
  + left normalized planar position mismatch
  + right normalized planar position mismatch
  + left normalized height mismatch
  + right normalized height mismatch
  + left velocity direction mismatch
  + right velocity direction mismatch
  + time-warp regularization
```

权重与归一化常量属于versioned compiler algorithm，不成为每个Transition或每对动画的作者配置。需要调整时提升algorithm identity并使旧artifact/Projection失效。

### Deterministic monotonic alignment

Editor-only compiler使用确定性单调动态规划：

- 路径从首样本到末样本。
- leader与follower索引都只能前进，不得倒退。
- 固定邻接集合、成本计算顺序与tie-break。
- 对连续停滞、过大局部斜率和无法覆盖末端的路径判为非法。
- 结果被重采样并确定性reduction为固定容量严格单调knot table。
- reduction误差或容量超限时Build失败，不提高容差、不切线性映射。

输出计划包含：

```text
AnimationFootPhaseTimeWarpPlan
    PlanIdentity
    AlgorithmIdentity
    LeaderArtifactHash
    FollowerArtifactHash
    LeaderSourceIdentity
    FollowerSourceIdentity
    SegmentPlans[]

AnimationFootPhaseWarpSegmentPlan
    LeaderOccurrenceIndex
    FollowerOccurrenceIndex
    PreviousMarkerId
    NextMarkerId
    Knots[] { LeaderFraction, FollowerFraction }
```

有限source存在重复有向pair时，Compiler为实际可选occurrence组合生成有界表。Runtime仍按当前raw target time和稳定authoring identity选择follower occurrence，然后读取该精确组合的warp。

## Runtime Mapping

现有relation cursor保留，但raw clock owner改为上节的精确producer identity。运行步骤调整为：

```text
1. 定位leader当前segment occurrence与linear leader fraction
2. 根据现有规则建立或推进follower occurrence
3. 读取relation的显式Time Mapping
4a. MarkerSegmentFraction: follower fraction = leader fraction
4b. GeneratedFootPhase: follower fraction = WarpPlan.Evaluate(leader fraction)
5. follower time = cycle * duration + segment start + follower fraction * segment duration
6. 只写入本次follower effective sample time，不改写producer raw clock
```

`GeneratedFootPhase`查表失败属于typed invalid。Runtime不得读取artifact、重新计算cost、尝试其它策略或保留上一帧effective time。

Transition建立relation时执行同一流程，之后共同可见的每帧继续执行。有限leader到达最后marker coverage后已经没有新的相位可供映射；Runtime必须提交一次终点映射并保留follower continuation anchor，后续共同可见帧让follower按自己的raw delta连续推进，不得重复把follower压回终点。outgoing source的Pose retention与同步时间relation分开结束，Transition clock和blend weight不参与时间映射。

## Blend Space Integration

active Blend Space当前有：

```text
SharedNormalizedPhase
MarkerSynchronizedPhase
```

本change收敛为：

```text
SharedNormalizedPhase
MarkerSegmentPhase
GeneratedFootPhase
```

- `SharedNormalizedPhase`只适用于明确按同一normalized time创作的通用动态样本。
- `MarkerSegmentPhase`保留当前Reference Marker区间线性比例。
- `GeneratedFootPhase`以固定Phase Reference为leader，为每个DynamicCycle sample编译warp plan。
- StationaryPose继续使用固定normalized time，不参加foot warp。
- 参数权重变化不更换Reference，不改变warp identity。

Blend Space只复用同一计划格式和求值函数，不复制动态规划算法。外层PoseState source relation与Blend Space内部child relation仍有不同owner，但共享Projection数据结构。

## Action Integration

Action Marker Sync继续由Timeline AnimationTrack与AnimationSlot relation拥有。现有Action默认迁移为`MarkerSegmentFraction`。只有明确配置`GeneratedFootPhase`、两侧均具备完整marker coverage与Foot Analysis artifact的Action pair才编译warp。

这允许未来有限RunEnd或动作衔接使用同一能力，但不会把当前无完整Marker的Attack、Dodge、Start、Stop或Turn自动加入脚步同步。

## Corin Migration

当前Corin循环配置：

```text
Walk Loop: RightFootContact frame 0, LeftFootContact frame 18, 36 frames
Run Loop:  RightFootContact frame 0, LeftFootContact frame 15, 30 frames
```

迁移结果：

```text
SyncMode = MarkerGroup
SyncGroup = Locomotion.Gait
Topology = Cyclic
TimeMapping = GeneratedFootPhase
```

两对half-cycle marker仍是硬边界；区间内部由双脚warp决定。Walk→Run与Run→Walk分别编译自己的有向relation plan。

Walk/Run迁移为`GeneratedFootPhase`。Start、End与MovingTurn保留当前正式authoring和Marker覆盖，不由本change自动改写Time Mapping；其中MovingTurn的raw Movement clock明确来自Timeline Motion producer，与是否配置Marker relation无关。

## Diagnostics

Runtime snapshot与Preview必须显示：

- RelationId、Time Mapping与Warp Plan identity。
- leader/follower source与artifact hash摘要。
- marker pair和两侧occurrence ordinal。
- leader linear fraction。
- warped follower fraction。
- follower effective time与cycle。
- typed failure reason：missing plan、identity mismatch、missing occurrence、invalid knot、finite coverage exceeded。

Diagnostics只从成功Seal的Committed page复制，不参与source选择或时间计算。

## Failure Policy

以下情况在Projection Build失败：

- MarkerGroup保留`Unspecified` Time Mapping。
- relation两侧Time Mapping不同。
- `GeneratedFootPhase`缺少或引用Stale/Corrupt artifact。
- marker区间没有足够同步样本。
- 对齐路径非单调、复杂度超限或reduction超限。
- Blend Space Reference或Dynamic Sample缺少精确warp输入。

以下情况在Runtime进入typed invalid并阻止正式动画帧发布：

- Projection plan identity与source binding不匹配。
- relation选择到没有编译warp的occurrence组合。
- knot非有限、无序或映射结果越界。
- Finite follower coverage耗尽。

所有失败都不回退normalized time、linear marker fraction、Animancer sync、旧Projection或上一帧结果。

## Decisions And Tradeoffs

### 选择relation-local pairwise warp，而不是单一全局GaitPhase曲线

pairwise warp能直接针对Walk/Run、Run/Walk以及具体有限occurrence最小化双脚差异，避免假设所有动作都能投影到一条完全相同的通用相位曲线。代价是Projection需要为实际可达relation保存更多表。

项目只为PoseState edge、Action可达pair和Blend Space固定Reference关系编译数据，规模由正式内容图决定，不生成全库N×N表。对当前求职demo，这个存储成本换来更稳定、可诊断的动画结果更有价值。

### 保留显式MarkerSegmentFraction，而不是让所有MarkerGroup强制使用脚数据

Marker Sync也服务攻击、连段和其它非步态语义。这些动画没有合理的左右脚相位合同，强制Foot Analysis会错误扩大业务含义。保留显式通用线性策略可以继续支持这些关系；它不是Generated失败时的fallback，Compiler不会自动切换。

### 编译期动态规划，而不是Runtime pose search

编译期能使用完整Clip数据并产出确定、固定容量的查表计划。Runtime成本低且Preview可完全复用。代价是Clip、Calibration、分析算法或Marker变化后必须显式重建artifact和Projection；这与项目现有generated product边界一致。

### Marker继续作为硬边界，而不是完全由脚数据自动发现关系

Marker保留作者对动作语义、循环拓扑和左右脚顺序的控制，避免算法把错误的周期或脚侧匹配起来。代价是作者仍需保证Marker覆盖完整；Foot Analysis只细化区间内部，不替作者猜测组关系。

### 不等待下一次脚接触

立即映射目标时间可以保持移动响应，脚步质量由目标时间选择解决。等待接触更容易实现，但最坏会延迟接近半个步态周期，并把动画表现约束泄漏到Gameplay响应。

### 选择producer随Motion原子提交clock，而不是Presentation读取权威tick

Presentation读取权威tick只能知道模拟走到哪一步，无法知道当前motion来自Locomotion Input还是MovingTurn Timeline，也无法知道各自的局部起点和generation。让producer原子提交clock会增加Contribution合同字段，但能保证位移和播放位置来自同一业务owner，并让Fixed、Float、rollback和remote presentation共用同一提交边界。

### 选择精确owner锁定，而不是Movement通道级共享clock

通道级共享clock实现简单，但transition期间outgoing source和incoming source可能属于不同producer；用新owner覆盖旧source会把保留源突然改绑到另一条时间线。精确identity锁定需要Player保存owner与generation，却能保持source retention、continuation anchor和rollback重基线可解释。

## OpenSpec Reconciliation

- 本change修改current specs中所有“直接映射segment fraction”的要求，使其改为显式Time Mapping。
- `character-foot-placement-presentation`不修改；它继续只消费映射后的effective sample与最终Foot Feature。
- active IK change拥有Rig v4与当前Foot Analysis动作级artifact升级；本change只在同一artifact上增加同步描述。
- active Blend Space change必须删除“只有两种phase policy”和`MarkerSynchronizedPhase`等同线性脚步同步的口径，改为三种显式策略并复用本change的warp plan。
- Motion Matching specs保持不变，MM不创建Marker relation。
- current `character-presentation-interpolation`中“持续Pose source直接使用presentation-owned clock”的旧口径改为：Presentation拥有渲染投影过程，但raw Movement anchor必须来自simulation producer随Motion原子提交；两者不是第二套Gameplay时间线。
