# Design: 在唯一Foot事务内稳定Path并保证Landing可达

## Context

当前重构已经把Landing Lifecycle、Ground Path、Swing Residual、Anchor、Support、Pelvis与Goal收进唯一`CharacterFootPlacementModule -> CharacterFootStateMachine -> Resolved Foot Pair`链。这条所有权不需要再次拆分；需要替换的是8fc行为政策和旧动画数据输入。

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
-> Step Time/Distance接入Prediction
-> Foot Height接入Swing
-> Support接入Primary Support/Pelvis
-> Landing Reach闭合
-> Contact/Lock接入State Machine
```

每一步只切换一个业务定义。对应旧字段在同一步删除；未轮到的正式字段可以存在于不可变Frame，但不得影响行为。Step Time只在瞬时Correction链已经连续后负责Landing截止，不得作为隐藏上游或下游同帧跳变的滤波器。Contact/Lock必须最后迁移，因为延长Landing前必须先让Swing接近Anchor并让Pelvis拥有有效Support/Reach输入。

## Decision 3: 先定位Path同帧放大，再分离连续目标与Envelope安全

FutureLanding世界事实固定拆成`Raw Landing -> canonical Landing Observation -> Acceptance`。Raw Landing仍从每帧不可变Frame Input重新投影；Observation Key由Side、Landing Event、按1毫米量化的Raw Landing、按`1e-4`量化的Component Up、Profile Revision与World Revision组成。SphereCast必须使用Key反量化后的canonical几何，相同Key复用同一Committed Observation Page且不得查询，新Key恰好查询一次并只选择canonical最近合法Surface。Accepted与Rejected查询结果都属于不可变Observation；Pending根事务失败时不得提交新Page。

上一Committed Surface、Frame、Authority Tick、Trajectory Generation、Future Translation Source、Foot State、Residual与查询输出不得进入Observation Key。上一Surface不得传入World Query或改变候选选择；历史只在Observation之后通过`LandingUpdateDistance`决定是否替换NextSwingLanding。1毫米Key量化定义世界查询输入是否相同，5毫米死区定义新Observation是否改变正式Landing与Ground Path，两者不得合并。

当前FootPlacementSurface在World Query Backend生命周期内视为静态，Backend发布固定非零World Revision；Reset、Retarget或Backend重建必须清空每脚Observation Page。移动平台和运行时Surface变更不在本change范围。

Ground Path Input identity只表示查询输入lineage，不单独触发Residual重置。State Machine只有在Event、Path可用性、Landing端点或实际Swing目标变化超过现有`LandingUpdateDistance`时捕获`PreviousOutput - NewTarget`。

Accepted Swing Motion必须携带与同一Ground Path Event匹配的typed Swing Path Landing Reference。Promoted Landing与按当前Step解析的Landing只属于Contact/Anchor准入，不得门控Swing Path可用性或提供Swing Residual的Landing Point。同帧旧Event完成并Promote、下一Swing Event已经Accepted时，State Machine必须同时保留旧Contact Landing和新Swing Path Landing，不得把Path发布为一帧不可用。

Path诊断必须先在同Frame、Side与Event lineage下记录`Raw Landing/Path Target -> Swing Target -> Captured Residual -> State Output -> Safety Floor Output -> Encoded Goal`。任一后继阶段的单帧Correction变化明显大于直接输入变化时，必须先修复第一个产生不连续或放大的阶段；不得通过更短HalfLife、Goal低通或Step Time截止把该跳变摊到后续帧。

在上述Correction链已经连续后，普通目标继续使用唯一`SwingResidual`。基础半衰期仍来自Profile；当Residual大于`LandingUpdateDistance`时，State Machine按剩余Step Time计算保证在Landing前收敛到容差所需的半衰期，并取它与基础半衰期的较小值。没有有效Step Time时不得猜测截止时间，只能发布明确输入不可用。Step Time只解决Landing前仍有Residual欠账，不负责改变Raw Target、重选State Output或修正同帧放大。

Swing的未来Ground Path Envelope只服务连续轨迹目标，不得作为当前脚硬Floor。硬Floor只等于正式`CurrentSwingFloor`查询命中的真实Surface Point相对`AnimatedSole`沿Component Up所需的最低安全Correction；查询必须复用唯一Foot World Query和正式Sphere、Layer、坡度与Cast配置。Query Miss、Capacity或Invalid时发布typed unavailable，不得回读未来Envelope补全。Foot Height、Landing目标和Residual属于连续目标，不得作为硬Floor。若CurrentSwingFloor在当前帧高于输出，系统允许立即抬升并必须诊断为Safety Floor Clamp；不得为了平滑把脚留在真实地面下方。

`Releasing -> Swing`完成必须先更新顶层State，再按新State执行Ground Floor和最终输出分类，避免同一帧发布Swing却跳过Swing Envelope保护。

## Decision 4: Foot Height只定义Swing动画高度

Swing的世界目标沿Component Up固定为：

```text
DesiredSoleHeight = RuntimeGroundEnvelopeHeight + FormalFootHeight
DesiredCorrection = DesiredSoleHeight - AnimatedSoleHeight
```

Formal Foot Height只表达动画脚高于动画Foot Path的高度。它不包含Runtime Landing、Anchor、Pelvis或世界修正。Runtime Ground Envelope只表达地面下界。两者组合后删除旧`LandingConstraintWeight * BaselineHeightError`高度政策；Foot XZ继续来自动画骨骼，不创建Foot Forward曲线或空间位置双写。

## Decision 5: Support与Lock解耦并先进入Pelvis

Resolved Foot把正式Support写入`SupportIntentWeight`。Primary Support的Acquire/Retain资格由Support Intent、稳定Event lineage和有效`PelvisReachReference`共同决定，不要求Lock Mode为Locked，也不把Support反写为Foot Goal约束。

`ContactReference`继续只属于脚锁；`PelvisReachReference`可以来自同Event已经Accepted的Landing/Ground事实。这样Sliding或暂时不可锁的承重脚可以协调Pelvis，但不能因此把脚固定到世界Anchor。

Primary Support只消费Resolved字段，不读取Foot State、Lock Mode或Context。Support曲线为0时不得由相对大小归一成1；双脚都无正式Support时Pelvis进入现有typed Release，而不是猜一只脚承重。

## Decision 6: Landing Reach先协调Pelvis，再限制Foot Goal

Foot Motion Profile新增米制`MinimumLandingLegCompressionReserve`。State Machine为Landing脚发布typed Reach Request：Hip、目标Ankle、Leg Length、最小压缩余量、Landing Event和有效世界Reference。它不是第二Support、第二Anchor或第二状态机。

Pelvis Builder同时计算Primary Support腿和Landing腿允许的Pelvis沿Up区间：

```text
FeasiblePelvisInterval = SupportReachInterval ∩ LandingReachInterval
```

交集存在时，Pelvis Target与Spring必须限制在交集内。交集不存在时，系统先保持Primary Support安全，再把Landing Foot Goal夹紧到保留最小压缩余量的最大可达点，发布`LandingReachUnavailable`，并禁止该脚进入Full Lock。它可以保持Landing、Sliding或进入Releasing，但不得把超长目标交给FBBIK后仅靠腿伸直夹紧。

该政策的业务取舍是：不可同时满足双腿时允许短暂未完全踩实，换取不出现明显直腿、骨盆瞬移或关节奇异。

## Decision 7: 正式Contact与Lock驱动现有状态

首次出现同Event的正式Sliding或Locked Mode且Accepted Landing合法时，唯一State Machine建立Anchor并保存当前Output到Anchor的Acquire Residual。Landing阶段直接使用正式Lock Weight消退该Residual；Mode、Weight和Event不一致时发布typed invalid，不按旧PlantConfidence继续。

正式Locked Mode和完成的Lock Weight进入现有`Locked / FullAnchor Response`。已锁脚回到Sliding Mode时保持同一顶层Locked生命周期和同一Anchor，只切换内部Sliding Response。Mode回到Unlocked或Contact正式退出时进入现有Releasing；Release仍由唯一Effective Correction Owner处理。

迁移完成后删除旧PlantCycleConsumed、旧PlantConfidence状态准入、旧Constraint Weight接触政策及相应Projection字段。Foot Placement Weight继续只表达整个Foot IK作者权重，不替代Contact、Lock或Support。

## Decision 8: 诊断证明阶段责任，不决定行为

封口诊断必须继续按同Frame、Completion、Program、Projection、Rig、Event和Surface lineage组合Source、Path、Context、Goal、Solved和Physical结果，并至少发布：

```text
Path Revision原因与前后目标
Raw Landing/Path Target、Swing Target、Captured Residual、State Output、Safety Floor Output与Encoded Goal的逐阶段Correction
Residual基础/截止半衰期与剩余距离
Safety Floor Clamp、CurrentSwingFloor查询事实及Safety Floor clearance
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
- Path identity每帧变化就无条件重置Residual。
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
4. 发布唯一Foot Motion Runtime Frame，只让Step Time/Distance进入Prediction和已经连续的Residual截止，并删除旧Step消费者。
5. 让Foot Height进入Swing并删除旧Baseline Height Error政策。
6. 让Support进入Resolved Foot、Primary Support和Pelvis，但保持Lock生命周期不变。
7. 增加双腿Reach交集、最小Landing压缩余量、Goal夹紧与typed拒绝。
8. 用Contact、Lock Mode与Lock Weight替换旧PlantConfidence生命周期并删除旧字段。
9. 显式重建Corin Projection、Float32与Fixed产品，完成编译、诊断重放和严格OpenSpec校验。
