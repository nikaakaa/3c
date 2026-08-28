# Design: 在唯一Foot事务内稳定Path并保证Landing可达

## Context

当前重构已经把Landing Lifecycle、Ground Path、Swing Residual、Anchor、Support、Pelvis与Goal收进唯一`CharacterFootPlacementModule -> Resolved Foot Pair`根事务，这个唯一生命周期继续保留。但事务内部仍由`CharacterFootStateMachine`同时决定离散状态、执行Transition、计算状态目标、推进多套Residual/Progress、写Anchor并执行Ground Floor。输入稳定时，任一状态分支仍可在同一方法中改变输出连续性；增加新状态也必须修改中央分支和分散的插值字段。需要拆掉的是这种传统中央状态机，不是再建第二套Foot链。

诊断显示两类问题具有不同边界。Path侧已经证明identity单独触发Residual重置不合理，但删除该触发后用户观察到的整体抖动基本不变；最新诊断还出现Landing端点只变化约1厘米而Correction同帧跳变约12厘米的反例，因此必须先定位Raw Target之后的第一个放大阶段，不能把Step Time截止收敛当成瞬时跳变修复。真正Envelope升高仍必须硬保护。Landing直腿不是Bend Solver随机反弯，而是Foot Goal在Pelvis无有效Reach协调时超过腿长，FBBIK只能把膝盖夹到伸展极限。设计必须同时保留Path阶段连续、地面安全和腿链可达，不能用同一个平滑器互相交换问题。

## Decision 1: 唯一正式Foot Motion Runtime Frame

Projection Compiler在`build-character-foot-motion-data-foundation`归档后，从原生AnimationClip Catalog和匹配Foot Analysis lineage降低唯一Foot Motion payload。Source Runtime按与Component Pose相同的Live Contribution、Source、Cycle、Normalized Time与Completion生成左右脚typed Sample；离散Lock Mode不得跨Source混合。

Foot Placement Pose Input只接受这一个Frame。缺失完整Curve、Event table、Source lineage或Contribution归属时整帧typed invalid，不读取旧Artifact字段、旧隐藏Feature、默认值或另一动画Source补全。

Step Event table由正式Step Time边界、Step Distance、匹配Artifact中的RootLocalLanding与稳定source/cycle/side ordinal共同编译。Runtime不读取Library Artifact；Editor Build只把已经严格对账的结果发布进Projection。

## Decision 2: 消费者按依赖顺序迁移

正式顺序固定为：

```text
Path逐阶段归因
-> 首个不连续阶段修复
-> 拆分State / Transition / Interpolation / Hard Constraint
-> Step Time/Distance接入Prediction
-> Foot Height接入Swing
-> Support接入Primary Support/Pelvis
-> Landing Reach闭合
-> Contact/Lock接入Transition、State Target与Interpolation
```

每一步只切换一个业务定义。对应旧字段在同一步删除；未轮到的正式字段可以存在于不可变Frame，但不得影响行为。架构拆分先保持当前已确认行为，只改变责任和数据流；旧`CharacterFootStateMachine`及旧Residual/Progress字段必须在同一步删除，不保留转发层。Step Time只在瞬时Correction链已经连续后负责Landing截止，不得作为隐藏上游或下游同帧跳变的滤波器。Contact/Lock必须最后迁移，因为延长Landing前必须先让Swing接近Anchor并让Pelvis拥有有效Support/Reach输入。

## Decision 3: State、Transition、Interpolation与Hard Constraint分层

每只脚继续由一个根事务处理，但帧内顺序固定为：

```text
Immutable Foot Input / Observation
-> Pre-Interpolation Transition
-> State Target
-> Unified Interpolation
-> Post-Interpolation Transition
-> Hard Constraint
-> Resolved Foot
```

`CharacterFootDiscreteState`只表达当前业务归属：`Swing / UnlockedSupport / Landing / Locked / Releasing`。Lock的`Sliding / FullAnchor`继续是Locked内部的typed响应模式，不扩展成第二套顶层状态。状态本身不得拥有`Enter/Exit`副作用、计时器、Residual或World Query。

`CharacterFootTransitionResolver`是纯决策器，只读取不可变Frame、Observation与上一Committed typed Context，返回固定`CharacterFootTransitionDecision`。Decision至少包含Source、Target、Reason、Event lineage、执行Phase、Anchor Command与Interpolation Policy identity。允许边固定为：

```text
Swing -> Landing | UnlockedSupport
UnlockedSupport -> Landing | Swing
Landing -> Locked | Releasing
Locked -> Releasing
Releasing -> Swing
```

输入驱动的边在Pre-Interpolation阶段执行；只有依赖本帧插值完成事实的边在Post-Interpolation阶段执行。`Releasing -> Swing`属于Post阶段，完成后必须用Swing输出分类执行同帧Hard Constraint。系统不得循环求Transition直到稳定，也不得让状态目标或插值器暗中改State。唯一Transition Runtime负责应用Decision、改写离散State、执行Anchor Create/Retain/Release命令并记录原因；其他模块不得写这些字段。

`CharacterFootStateTargetResolver`按已经确定的State纯计算本帧目标，输出目标Correction、Reference、Contact/Support/Reach意图和固定typed Interpolation Request。它不得读取或推进Delta Time、Residual、Progress与上一输出，也不得查询世界。Swing/UnlockedSupport目标只来自Ground Path、Envelope与正式Foot Height；Landing/Locked目标来自唯一Anchor和正式Contact/Lock；Releasing只回到原始动画Swing目标。

`CharacterFootInterpolationRuntime`是Effective Correction连续性的唯一所有者。它只接受`Previous Effective Correction + State Target + typed Policy + Delta Time`，持有一份统一Interpolation State并发布Output、Residual和Completion。现有Swing Residual、Acquire Residual、Release Residual、Contact Progress与散落的HalfLife推进必须迁入这里；政策固定为直接跟随、Residual Half-Life与正式Weight接管等有业务含义的typed策略，不提供string key、字典注册、任意曲线回调或项目级通用Tween。统一的是执行生命周期和状态所有权，不是强迫Swing、Landing与Release使用同一条曲线。

根`CharacterFootStateContext`收敛为一组分型数据块：`Discrete State Context`只存当前State与最近Transition，`Contact Context`只存Anchor和Lock响应，`Interpolation State`只存上一目标、Effective Correction、统一Residual与完成事实，Landing与Observation继续使用各自typed Page。所有数据块仍由同一个Pending/Committed根事务一次Seal或Discard，不建立独立生命周期。

Hard Constraint只在插值后消费结果并立即执行不可违反的物理边界。Swing Hard Constraint复用State Target所属的同一Accepted Ground Path Envelope，不执行当前脚逐帧World Query；Landing Reach负责双腿可达和最小压缩余量。两者不得回写State Target、Residual或Transition，也不得被插值延迟。这样预测输入稳定时Swing连续目标和安全下界来自同一事实，已知Path穿地和超长Goal不会因为追求平滑被保留。

## Decision 4: 先定位Path同帧放大，再分离连续目标与Envelope安全

FutureLanding世界事实固定拆成`Raw Landing -> canonical Landing Observation -> Acceptance`。Raw Landing仍从每帧不可变Frame Input重新投影；Observation Key由Side、Landing Event、按1毫米量化的Raw Landing、按`1e-4`量化的Component Up、Profile Revision与World Revision组成。SphereCast必须使用Key反量化后的canonical几何，相同Key复用同一Committed Observation Page且不得查询，新Key恰好查询一次并只选择canonical最近合法Surface。Accepted与Rejected查询结果都属于不可变Observation；Pending根事务失败时不得提交新Page。

上一Committed Surface、Frame、Authority Tick、Trajectory Generation、Future Translation Source、Foot State、Residual与查询输出不得进入Observation Key。上一Surface不得传入World Query或改变候选选择；历史只在Observation之后通过`LandingUpdateDistance`决定是否替换NextSwingLanding。1毫米Key量化定义世界查询输入是否相同，5毫米死区定义新Observation是否改变正式Landing与Ground Path，两者不得合并。

当前FootPlacementSurface在World Query Backend生命周期内视为静态，Backend发布固定非零World Revision；Reset、Retarget或Backend重建必须清空每脚Observation Page。移动平台和运行时Surface变更不在本change范围。

Ground Path Input identity只表示查询输入lineage，不单独触发Residual重置。Interpolation Runtime只有在Event、Path可用性、Landing端点或正式Swing目标变化超过现有`LandingUpdateDistance`时捕获`PreviousOutput - NewTarget`。原始Builder目标与State Target继续分列诊断，不得互相改名覆盖。

Accepted Swing Motion必须携带与同一Ground Path Event匹配的typed Swing Path Landing Reference。Promoted Landing与按当前Step解析的Landing只属于Contact/Anchor准入，不得门控Swing Path可用性或提供Swing Residual的Landing Point。同帧旧Event完成并Promote、下一Swing Event已经Accepted时，Foot根事务必须同时保留旧Contact Landing和新Swing Path Landing，不得把Path发布为一帧不可用。

Path诊断必须先在同Frame、Side与Event lineage下记录`Raw Landing/Path Target -> Swing Target -> Captured Residual -> State Output -> Safety Floor Output -> Encoded Goal`。任一后继阶段的单帧Correction变化明显大于直接输入变化时，必须先修复第一个产生不连续或放大的阶段；不得通过更短HalfLife、Goal低通或Step Time截止把该跳变摊到后续帧。

在上述Correction链已经连续后，普通Swing目标使用统一Interpolation State中的Residual。基础半衰期仍来自Profile；当Residual大于`LandingUpdateDistance`时，Interpolation Runtime按剩余Step Time计算保证在Landing前收敛到容差所需的半衰期，并取它与基础半衰期的较小值。没有有效Step Time时不得猜测截止时间，只能发布明确输入不可用。Step Time只解决Landing前仍有Residual欠账，不负责改变Raw Target、重选State Output或修正同帧放大。

Swing的Ground Path Envelope同时服务连续轨迹目标和插值后的安全下界。Hard Constraint MUST消费本帧Accepted Swing Motion已经采样的同一Envelope Point和Path identity，不得重新Raycast、SphereCast或读取另一Surface。Envelope随Swing Progress连续采样；只有正式Path Revision才能改变其几何。若Interpolation Output低于Envelope，系统允许立即抬升并必须诊断为Ground Path Envelope Clamp；Hard Constraint输出不得写回Interpolation历史。

`Releasing -> Swing`完成必须由Post-Interpolation Transition先更新顶层State，再按新State执行Ground Floor和最终输出分类，避免同一帧发布Swing却跳过Swing Envelope保护。

## Decision 5: Foot Height只定义Swing动画高度

Swing的世界目标沿Component Up固定为：

```text
DesiredSoleHeight = RuntimeGroundEnvelopeHeight + FormalFootHeight
DesiredCorrection = DesiredSoleHeight - AnimatedSoleHeight
```

Formal Foot Height只表达动画脚高于动画Foot Path的高度。它不包含Runtime Landing、Anchor、Pelvis或世界修正。Runtime Ground Envelope只表达地面下界。两者组合后删除由`LandingConstraintWeight`乘`BaselineHeightError`或`FormalTargetCorrection`的旧高度/目标政策；Foot XZ继续来自动画骨骼，不创建Foot Forward曲线或空间位置双写。

## Decision 6: Support与Lock解耦并先进入Pelvis

Resolved Foot把正式Support写入`SupportIntentWeight`。Primary Support的Acquire/Retain资格由Support Intent、稳定Event lineage和有效`PelvisReachReference`共同决定，不要求Lock Mode为Locked，也不把Support反写为Foot Goal约束。

`ContactReference`继续只属于脚锁；`PelvisReachReference`可以来自同Event已经Accepted的Landing/Ground事实。这样Sliding或暂时不可锁的承重脚可以协调Pelvis，但不能因此把脚固定到世界Anchor。

Primary Support只消费Resolved字段，不读取Foot State、Lock Mode或Context。Support曲线为0时不得由相对大小归一成1；双脚都无正式Support时Pelvis进入现有typed Release，而不是猜一只脚承重。

## Decision 7: Landing Reach先协调Pelvis，再限制Foot Goal

Foot Motion Profile新增必须显式序列化的米制`MinimumLandingLegCompressionReserve`并纳入Profile Revision。缺失、非有限或越界时整项typed invalid，不提供代码默认值或旧配置补全。State Target Resolver与Resolved Foot为Landing脚发布typed Reach Request：Hip、目标Ankle、Leg Length、最小压缩余量、Landing Event和有效世界Reference。它不是第二Support、第二Anchor或第二状态机。

Pelvis Builder同时计算Primary Support腿和Landing腿允许的Pelvis沿Up区间：

```text
FeasiblePelvisInterval = SupportReachInterval ∩ LandingReachInterval
```

交集存在时，Pelvis Target与Spring必须限制在交集内。交集不存在时，系统先保持Primary Support安全，再把Landing Foot Goal夹紧到保留最小压缩余量的最大可达点，发布`LandingReachUnavailable`，并禁止该脚进入Full Lock。它可以保持Landing、Sliding或进入Releasing，但不得把超长目标交给FBBIK后仅靠腿伸直夹紧。

该政策的业务取舍是：不可同时满足双腿时允许短暂未完全踩实，换取不出现明显直腿、骨盆瞬移或关节奇异。

## Decision 8: 正式Contact与Lock驱动Transition与统一插值

首次出现同Event的正式Sliding或Locked Mode且Accepted Landing合法时，Pre-Interpolation Transition Resolver发布`Swing/UnlockedSupport -> Landing`与Create Anchor命令。Transition Runtime只建立一次Anchor；State Target Resolver以该Anchor生成Landing目标，Interpolation Runtime保存当前Output到Anchor的Residual，并按正式Lock Weight推进接管。Mode、Weight和Event不一致时发布typed invalid，不按旧PlantConfidence继续。

正式Locked Mode和完成的Lock Weight触发`Landing -> Locked`，并使用`FullAnchor Response`目标。已锁脚回到Sliding Mode时保持同一顶层Locked生命周期和同一Anchor，只切换内部Sliding Response目标。Mode回到Unlocked或Contact正式退出时触发`Landing/Locked -> Releasing`；Release仍由Interpolation Runtime这个唯一Effective Correction Owner处理。任何State Target都不得直接写Anchor、State或插值进度。

迁移完成后删除旧PlantCycleConsumed、旧PlantConfidence状态准入、旧Constraint Weight接触政策及相应Projection字段。Foot Placement Weight继续只表达整个Foot IK作者权重，不替代Contact、Lock或Support。

## Decision 9: 诊断证明阶段责任，不决定行为

封口诊断必须继续按同Frame、Completion、Program、Projection、Rig、Event和Surface lineage组合Source、Path、Context、Goal、Solved和Physical结果，并至少发布：

```text
Path Revision原因与前后目标
Raw Landing/Path Target、Swing Target、Captured Residual、State Output、Safety Floor Output与Encoded Goal的逐阶段Correction
Transition Decision、State Target、Interpolation Request/Output/Completion与Hard Constraint前后值
Residual基础/截止半衰期与剩余距离
Ground Path Envelope Clamp、Path identity及Safety Floor clearance
Formal Step/Foot Height/Contact/Lock/Support输入
Support与Landing Reach区间及交集
Foot Goal夹紧量与LandingReachUnavailable
Target/Solved Extension Ratio与Compression Reserve
```

Diagnostics不得创建Anchor、选择Support、改变Reach、Clamp Goal或执行第二次Query。

采样包固定由同一Recorder发布`每Frame/Side一行的samples.csv + 只保存Ground Contact/Envelope数组项的ground-path-geometry.csv`。几何表必须按Sample、Frame、Completion、Side与Ground Path identity连接主表，不得为每个几何项重复整套Source、State、Goal和Solver列。

停止录制必须进入唯一`Finalizing`生命周期。Unity主线程只停止捕获并冻结最后一批不可变Frame；后台Finalizer继续排空同一Writer、先封存几何表再以`samples.csv`作为包完成标志、运行同一C# Analyzer与Publisher，最后把Completed或Failed状态发布回Editor。不得增加Python Reporter、同步停止分析路径或仅扩大队列掩盖持续吞吐不足。

## Rejected Alternatives

- 恢复旧Goal Transition或在FBBIK之后加全局平滑。
- 只把传统State Machine拆成多个State类，但继续让State自己执行Enter/Exit、改Anchor和推进插值；文件变多但责任没有分开，新Transition仍需改多个State。
- 建立项目级通用Tween Manager、string channel或字典注册插值；Foot的Event lineage、Anchor接管、Landing截止和Release完成无法由无业务语义的Tween可靠表达。
- 复用Animation Pose Graph的Transition Routing；Pose Source权重换代与Foot接触所有权是两种业务事务，共用路由会让Foot状态依赖动画图内部生命周期。
- 保留旧`CharacterFootStateMachine`作为新模块外的转发或兼容入口；这会让离散状态和Effective Correction继续存在两个可写位置。
- Path identity每帧变化就无条件重置Residual。
- 把每帧CurrentSwingFloor命中接进Swing State Target并以`ContinuousTargetChanged`重建Residual；这会让预测输入稳定时悬空脚仍追逐实时地面查询。
- 把完整Swing目标当作Ground Floor，或为了连续性允许脚穿过真实Envelope。
- 把当前脚水平投影到一维Ground Path上包络并取同距离最高点；该做法无法区分竖直边两侧的真实Surface。
- 直接给膝盖设置最小角度而不处理Foot Goal与Pelvis可达。
- 只降低Foot Goal Weight掩盖超长目标。
- 在Support和Foot Height未迁移前单独延长Landing或接入Lock Weight。
- 让Primary Support/Pelvis读取Foot State、Lock Mode或可变Context。
- 同时保留旧PlantConfidence和正式Contact/Lock并择优输出。
- 增加第二Landing状态、第二Anchor、第二Goal Set或第二FBBIK。

## Migration

1. Foot Motion数据change已经完成用户验收并归档，记录当前Curve/Event identity。
2. 保留已经完成的Releasing到Swing顺序修正和identity触发清理，记录它们没有明显改善整体Path抖动。
3. 对最新代表事件逐阶段发布Raw Target、Residual、State Output、Floor与Goal事实，修复第一个已证明的不连续阶段。
4. 固定当前State、Transition边、目标、Residual和Hard Constraint映射；建立分型Context、纯Transition Resolver、State Target Resolver与唯一Interpolation Runtime，在根事务内逐项迁移等价行为。
5. 切换固定帧内管线并删除旧`CharacterFootStateMachine`、三套Residual、Contact Progress、分散HalfLife推进与所有兼容入口；从此只有Transition Runtime写离散State/Anchor，只有Interpolation Runtime写Effective Correction。
6. 发布唯一Foot Motion Runtime Frame，只让Step Time/Distance进入Prediction和已经连续的Residual截止，并删除旧Step消费者。
7. 让Foot Height进入Swing并删除旧Baseline Height Error政策。
8. 让Support进入Resolved Foot、Primary Support和Pelvis，但保持Lock生命周期不变。
9. 增加双腿Reach交集、最小Landing压缩余量、Goal夹紧与typed拒绝。
10. 用Contact、Lock Mode与Lock Weight替换旧PlantConfidence生命周期并删除旧字段。
11. 显式重建Corin Projection、Float32与Fixed产品，完成编译、诊断重放和严格OpenSpec校验。
