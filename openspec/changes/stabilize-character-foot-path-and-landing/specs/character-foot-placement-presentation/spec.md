## MODIFIED Requirements

### Requirement: Landing Prediction必须形成独立世界事实

每只脚 MUST按`正式Foot Motion Step Event -> committed Body Target世界速度 + Timeline段边界/Continuation -> KCC Future Body Translation -> Raw Landing -> Future Landing SphereCast -> Accepted/Rejected Landing`执行。Step Event MUST携带同Source、Cycle、Side与ordinal的稳定Landing Event identity，并使用正式Step Time作为预测时域、正式Step Distance作为相邻同脚Event与RootLocalLanding水平步长一致性证据。

Raw Landing MUST继续按`VisiblePosition + FutureBodyTranslation + VisibleRotation * RootLocalLanding`从本帧输入重新投影。Step Distance MUST不替代committed Body世界速度、Future Body Translation或世界地形；RootLocalLanding MUST只乘本帧Visible Rotation，不外推Future Body Yaw。

Runtime MUST先把新Raw Landing与Committed Observation实际查询使用的输入快照比较。同Source/Cycle/Event/Profile/World Revision下，累计位移不超过5厘米且Component Up夹角不超过1度时 MUST复用根事务已提交的不可变Accepted或Rejected Observation，不得建立新Key或执行SphereCast。超过连续阈值，或Source/Cycle、Event、Profile Revision、World Revision变化时，Runtime MUST从Side、Landing Event、按1毫米量化的Raw Landing、按`1e-4`量化的Component Up、Profile Revision与非零World Revision构造canonical Landing Observation Key。SphereCast MUST从Key反量化后的canonical Raw Landing上方沿Component Down使用Profile半径和有限距离查询，并过滤自身Collider、初始重叠、非法点、非法法线与超坡度命中。新Key MUST恰好查询一次，并在固定容量合法候选中按距离与稳定identity选择canonical最近候选。容量溢出或没有合法命中 MUST发布typed拒绝，不得沿用另一Key、旧Landing、默认Surface或另一Event结果。

上一Committed Surface、Frame、Authority Tick、Trajectory Generation、Future Translation Source、Foot State、Residual与查询输出 MUST不进入Observation Key或候选选择。Pending Observation MUST随Foot根事务提交或丢弃；Reset、Retarget与World Query Backend重建 MUST清空Observation Page。当前静态FootPlacementSurface在Backend生命周期内使用固定非零World Revision；移动平台不属于本change。

#### Scenario: 正式Step预测命中

- **WHEN** 同一正式Event具有合法Step Time、Step Distance、RootLocalLanding、Future Body Translation和SphereCast命中
- **THEN** Runtime MUST发布唯一Accepted Landing、Surface、点、法线、查询距离及完整Event lineage
- **AND** Step Time变化 MUST只改变该Event预测时域，不得重建另一套Landing生命周期

#### Scenario: 相同Landing Observation Key

- **WHEN** 当前Foot帧生成的canonical Observation Key与上一Committed Page相同
- **THEN** Runtime MUST复用相同Observation identity、Surface、点、法线与Reject结果
- **AND** MUST不执行FutureLanding SphereCast或读取上一Surface重新选择候选

#### Scenario: 新Landing Observation Key

- **WHEN** canonical Raw Landing、Component Up、Event、Profile Revision或World Revision产生新Key
- **THEN** Runtime MUST执行一次FutureLanding SphereCast并提交canonical最近合法候选或typed拒绝
- **AND** Pending Foot事务失败时 MUST丢弃该Observation，不得污染上一Committed Page

#### Scenario: 预测输入变化低于查询阈值

- **WHEN** 同Source、Cycle、Event、Profile和World Revision的新Raw Landing相对上次实际查询输入累计位移不超过5厘米且Component Up变化不超过1度
- **THEN** Runtime MUST复用Committed Observation与查询结果
- **AND** MUST不执行SphereCast、替换NextSwingLanding或重建Ground Path

#### Scenario: Step Event与RootLocalLanding不一致

- **WHEN** 正式Step Distance、Event table与RootLocalLanding水平位移不满足编译容差或lineage不匹配
- **THEN** Projection Build或当前Foot帧 MUST发布typed invalid
- **AND** MUST不读取旧隐藏Step Event或重新编号继续预测

### Requirement: Ground Path必须使用上一已提交落点与下一事件落点

每只脚 MUST按Landing Event identity缓存Accepted Landing。PreSwing或Swing阶段的每个有效表现帧 MUST重新投影Raw Landing并先执行正式Prediction Input Gate；只有超过5厘米累计位移、超过1度Component Up变化或离散lineage变化时 MUST构造新canonical Landing Observation Key并执行一次正式Landing SphereCast。新Observation与同Surface、同Event NextSwingLanding的距离小于2厘米时 MUST保留原落点并复用Ground Path；Surface变化、Event变化或距离达到2厘米时 MUST提交新的NextSwingLanding。该事件实际落地后最新NextSwingLanding MUST晋级为LastLanding，之后才为新的Swing事件建立下一落点。

Ground Path MUST只使用LastLanding与NextSwingLanding构造查询输入。没有LastLanding时 MUST发布`CurrentLandingUnavailable`；不得用Animated Sole、Transform、固定高度或默认地面补起点。

#### Scenario: 同一Landing Observation Key持续多个表现帧

- **WHEN** PreSwing或Swing连续帧产生相同canonical Observation Key
- **THEN** Runtime MUST复用Committed Observation、NextSwingLanding与Committed Ground Path
- **AND** MUST不执行新的Landing SphereCast或Capsule Ground Detection

#### Scenario: 新Observation超过更新死区

- **WHEN** 新Key产生的Accepted Observation与同Event NextSwingLanding距离达到正式更新死区
- **THEN** Runtime MUST提交新的NextSwingLanding并重建同一Foot事务中的Ground Path
- **AND** Ground Path重建 MUST消费该新Observation，不得执行第二次Landing查询

#### Scenario: 新Observation低于更新死区

- **WHEN** 新Key产生的Accepted Observation与同Event NextSwingLanding距离小于正式更新死区
- **THEN** Runtime MUST提交新的Observation Page但保留NextSwingLanding与Committed Ground Path
- **AND** MUST不因毫米级预测误差执行Capsule Ground Detection

#### Scenario: 下一Swing Event完成

- **WHEN** NextSwingLanding对应的事件成为已完成Swing Event
- **THEN** Runtime MUST把该Accepted Landing晋级为新的LastLanding
- **AND** MUST只为新的PreSwing或Swing Event建立新的NextSwingLanding

### Requirement: Foot Lifecycle必须生成唯一权威结果

每只脚 MUST在同一根事务内按固定顺序执行`不可变输入与Observation -> Pre-Interpolation Transition -> State Target -> Interpolation -> Post-Interpolation Transition -> Hard Constraint -> Resolved Foot`。这些阶段 MUST每帧各执行一次，并只发布一份离散State、一份Effective Correction和一个Resolved Foot。

唯一typed `CharacterFootTransitionResolver` MUST只读取正式Foot Motion Frame、不可变Ground Observation、上一Committed离散State和当前阶段事实，并发布不可变Transition Decision。唯一Transition Runtime MUST应用Decision中的State与Anchor命令；Resolver和Runtime MUST不推进Residual、计算State Target、查询世界或写Goal。Pre与Post阶段允许的Transition边、优先级和输入集合 MUST固定且可编译校验。

纯`CharacterFootStateTargetResolver` MUST按Transition后的离散State生成Correction Target、Contact Reference、Goal与Ownership目标及typed Interpolation Policy Request。Swing与UnlockedSupport的Target MUST只使用正式Ground Path、Envelope与Foot Height；Releasing MUST只回到原始Swing Target。Resolver MUST不保存跨帧时间状态、不推进Residual、不改写State、不得执行World Query，也不得执行Hard Constraint。

唯一typed `CharacterFootInterpolationRuntime` MUST拥有上一Target、Effective Position/Rotation Correction、唯一Residual与Completion。Swing Path换代、Landing Acquire和Release MUST只通过固定typed Policy Request连续化；迁移完成后 MUST删除分散的`SwingResidual`、`AcquireResidual`、`ReleaseResidual`、`ContactProgress`和重复Advance数学。Residual大于5毫米`ResidualLandingTolerance`时，Interpolation Runtime MUST按正式Step Time计算Landing截止收敛；Release MUST只使用独立5毫米`ReleaseCompletionDistance`判断完成。Step Time只决定Residual衰减，不得改变Raw Target、重选State或掩盖同帧不连续。

Ground Path Envelope和Reach MUST在Interpolation之后作为Hard Constraint执行。Swing Hard Constraint MUST复用本帧Accepted Swing Motion已经采样的同一Envelope Point与Path identity，不得执行Raycast、SphereCast或读取另一Surface；只有连续输出低于Envelope时 MAY立即Clamp。Hard Constraint MUST不修改State Target、不触发Residual Revision，也不得写回Interpolation历史；它 MAY限制已知不可达Goal，但 MUST不反向修改State、Transition Decision或Target。全部分型状态 MUST由同一根Bank统一Seal或Discard，不得形成第二状态机、第二生命周期或第二输出路径。

Swing Target MUST只使用Last Landing、Next Landing、Runtime Ground Envelope与正式Foot Height。Accepted Swing Motion MUST携带同Ground Path Event的typed Swing Path Landing Reference；Promoted Contact Landing MUST只服务Contact与Anchor。Residual Revision MUST只由Event、可用性、Landing端点或正式Swing Target的有效变化触发，Ground Path identity单独变化 MUST不重置Residual。Diagnostics MUST分别发布原始Builder Swing Target与State Target，不得用State Target覆盖Builder事实。

Landing Anchor MUST在同Event正式Lock Mode首次进入Sliding或Locked且Accepted Landing合法时由唯一Transition Runtime建立，并同时保存Contact Point与Normal。正式Lock Weight MUST通过Interpolation Policy Request渐进接管Anchor位置与旋转。旋转目标 MUST保留动画脚踝Yaw并把Pitch/Roll对齐Contact Normal；正式Rotation Weight MUST由Foot Placement Weight与Lock Weight共同决定。正式Contact退出或Mode回到Unlocked时 MUST进入Releasing；Releasing必须通过同一Interpolation连续回到动画位置与旋转。Releasing完成进入Swing的同一帧 MUST先应用Post-Interpolation Transition，再按新State执行Ground Path Envelope Hard Constraint和最终输出分类，不得重跑State Target或Interpolation。迁移完成后全部阶段 MUST不读取旧PlantConfidence、PlantCycleConsumed或旧Constraint Weight决定Landing、Lock与Release。

#### Scenario: 同Event Path换代

- **WHEN** 同一Swing Event的Landing或Envelope Target发生正式Revision
- **THEN** State Target Resolver MUST发布新Target，Interpolation Runtime MUST从上一Effective Correction连续接管并按Step Time在LandingUpdateDistance内收敛
- **AND** 只有同一Accepted Ground Path Envelope高于连续输出时 Hard Constraint MAY立即向上Clamp并发布Safety Floor事实

#### Scenario: 旧Contact Event与新Swing Event同帧交接

- **WHEN** 旧Event的Landing在当前帧成为Promoted Contact Landing，且下一Event已经具有Accepted Swing Motion与匹配Ground Path
- **THEN** Transition与State Target MUST让旧Landing只服务Contact与Anchor，并让新Swing Path Landing继续服务Swing Target与Interpolation
- **AND** MUST不因两者Event不同把Swing Path发布为一帧不可用

#### Scenario: 正式Lock渐进接管

- **WHEN** 同Event Lock Mode从Unlocked进入Sliding且Lock Weight从0连续增加
- **THEN** Transition Runtime MUST建立一次Anchor，State Target MUST发布Anchor目标，Interpolation Runtime MUST按Weight连续接管
- **AND** MUST不新增固定Duration、第二Landing状态、第二Anchor或状态私有Residual

#### Scenario: 正式Lock接管脚掌旋转

- **WHEN** 同Event具有合法Contact Normal、Sliding或Locked Mode和非零Lock Weight
- **THEN** Foot Goal MUST保留动画Yaw并按Lock Weight把Pitch/Roll连续对齐Contact Normal
- **AND** FBBIK之外 MUST不存在第二旋转目标、图外骨骼修正或固定旋转权重

#### Scenario: Releasing完成进入Swing

- **WHEN** Post-Interpolation Transition判定Releasing完成且当前帧具有合法Swing Envelope
- **THEN** Transition Runtime MUST在同帧应用Swing，随后按新State执行Ground Path Envelope Hard Constraint和最终输出分类
- **AND** 发布为Swing的Corrected Sole MUST不因旧Releasing顺序留在真实地面安全高度下方

### Requirement: Resolved Foot必须形成紧凑下游合同

`CharacterResolvedFootResult` MUST继续包含Frame、Completion、Rig、Side、Final Sole/Ankle、Effective Correction、Goal Weight、Contact Reference/Ownership、Support Eligibility、Support Weight、Support Intent Weight、Support Horizontal Error、Support Event lineage、Pelvis Reach Reference与Outcome，并新增typed Landing Reach Request及其Event lineage。

Support Intent Weight MUST逐值来自正式Support，不得从Lock Mode、Lock Weight、Contact Ownership或Foot State复制。Support Eligibility MUST由正式Support Intent、稳定Event lineage与有效Pelvis Reach Reference生成：可获取且可保留时发布`AcquireAndRetain`，只能延续已选Primary时发布`RetainOnly`，没有正式Support或Reach时发布`None`。Landing或Sliding MAY在未Full Lock时参与Pelvis Support，但 MUST不因此建立Foot Contact Anchor或增加Foot Goal锁定权重。

Contact Reference MUST只属于脚锁；Pelvis Reach Reference MUST只属于Support/Pelvis；Landing Reach Request MUST只表达目标腿可达。三者不得互相作为默认值或让下游读取Foot State、Lock Mode、Path Residual与Context内部字段。

#### Scenario: Landing脚具有正式Support但尚未Full Lock

- **WHEN** Landing脚Support Intent非零、Event匹配且具有合法Pelvis Reach Reference，但Lock Mode仍为Sliding
- **THEN** Resolved Foot MAY发布`AcquireAndRetain`供Primary Support与Pelvis消费
- **AND** Foot Contact Ownership和Goal Lock Weight MUST继续只由正式Contact/Lock决定

#### Scenario: 无正式Support

- **WHEN** 双脚正式Support均为0或没有合法Pelvis Reach Reference
- **THEN** Resolved Foot MUST发布Support Eligibility None和零Support Intent
- **AND** MUST不把相对更高的一只脚归一成Support 1

### Requirement: Pelvis必须只消费Resolved Foot Pair并保持双腿可达

Primary Support MUST只读取Resolved Pair中的Support Eligibility、Support Intent Weight、Support Event lineage、Support Horizontal Error和Pelvis Reach Reference。Selector MUST不读取Foot State、Lock Mode、Contact Ownership或Context；正式Support为0时不得按相对大小生成支撑。

Pelvis Builder MUST同时读取Primary Support腿Reach与Landing Reach Request，并使用Foot Motion Profile中必须显式序列化的米制最小Landing腿压缩余量计算沿Component Up的可行交集。该配置缺失、非有限或越界时 MUST发布typed invalid，不得使用代码默认值或旧配置补全。交集存在时，Pelvis Target与Spring Output MUST限制在交集内；Support换代、坡度变化和目标跨越仍必须保持现有显式Handoff与Velocity Reset事实。

交集不存在时，系统 MUST优先保持Primary Support腿安全，把Landing Foot Goal夹紧到保留最小压缩余量的最大可达点，发布`LandingReachUnavailable`并禁止该脚进入Full Lock。FBBIK MUST不接收已知超出可达区间的Landing目标后仅靠膝盖完全伸直夹紧。

#### Scenario: 双腿Reach存在交集

- **WHEN** Primary Support腿和Landing腿沿Up可达区间存在交集
- **THEN** Pelvis Target与Spring Output MUST位于该交集内
- **AND** Landing Foot Goal MUST保持至少Profile声明的最小压缩余量

#### Scenario: 双腿Reach没有交集

- **WHEN** 保持Primary Support安全与Landing腿最小压缩余量无法同时满足
- **THEN** Runtime MUST夹紧Landing Foot Goal并发布`LandingReachUnavailable`
- **AND** 该脚 MUST保持Landing、Sliding或进入Releasing，不得进入Full Lock或输出已知超长Goal

## ADDED Requirements

### Requirement: Foot诊断必须证明Path安全与Landing可达责任

封口Foot诊断 MUST在同Frame、Completion、Program、Projection、Foot Profile、Rig、Event与Surface lineage下同时记录正式Step/Foot Height/Contact/Lock/Support输入、Prediction Input Snapshot与阈值判定、Path Revision原因、Raw Landing/Path Target、Pre/Post Transition Decision、State Target、Interpolation Position/Rotation Policy、Residual、Output与Completion、Hard Constraint前后Correction、Encoded Position/Rotation Goal、Residual基础与截止HalfLife、Ground Path Envelope Clamp与clearance、Support与Landing Reach区间、Goal夹紧量、Target/Solved Extension Ratio、Compression Reserve和Physical结果。

Diagnostics MUST只读取Committed Source、Path、Context、Resolved、Goal、Solved与Final Publication结果，不得创建Anchor、选择Support、修改Reach、Clamp Goal或执行第二次World Query。

#### Scenario: Path Revision产生Ground Path Envelope Clamp

- **WHEN** Accepted Ground Path Envelope的最低安全Correction高于连续Swing输出
- **THEN** 诊断 MUST记录Path identity、Envelope Point、Clamp前后Correction和Safety Floor clearance
- **AND** MUST区分普通目标跟随与真实地面安全抬升

#### Scenario: Correction在Path后继阶段被放大

- **WHEN** Raw Landing、Path Target或State Target只有小幅单帧变化，但后继Interpolation Output、Hard Constraint Output或Encoded Goal产生明显更大的Correction变化
- **THEN** 诊断 MUST定位第一个产生不连续或放大的阶段并记录其直接输入、输出和所有权状态
- **AND** Runtime MUST不把该现象归类为Step Time截止欠账或通过缩短Residual HalfLife隐藏

#### Scenario: Landing Goal不可达

- **WHEN** Landing Reach与Primary Support Reach没有交集
- **THEN** 诊断 MUST记录两侧区间、最小压缩余量、Goal夹紧量和`LandingReachUnavailable`
- **AND** MUST能区分动画Source已伸直、Foot Placement引入超长目标与FBBIK最终夹紧

### Requirement: Foot诊断采样必须正规化并由后台唯一封口

Foot诊断Recorder MUST为每个Frame、Completion与Side只写一条包含Source、Path、State、Goal、Solved和Physical阶段事实的`samples.csv`主行。一对多Ground Contact与Envelope顶点 MUST写入同目录唯一`ground-path-geometry.csv`，并通过Sample、Frame、Completion、Side与Ground Path identity连接主行；不得为每个几何项重复整套主行列。

停止采样、捕获队列失败和自动路线结束 MUST统一进入`Finalizing`。Unity主线程 MUST停止捕获并立即返回；唯一后台Finalizer MUST排空现有Writer、封存双表、运行同一Analyzer与Publisher并原子发布facts和diagnoses。程序集重载 MAY等待同一Finalizer完成以保护包完整性，但不得建立同步Analyzer、Python Reporter、第二输出schema或仅扩大队列的替代路径。

#### Scenario: 停止包含大量Ground Path几何的录制

- **WHEN** 当前录制已经积累大量Ground Contact与Envelope顶点且作者点击停止
- **THEN** Editor MUST进入Finalizing而不在停止回调中等待Writer或扫描CSV
- **AND** `samples.csv` MUST保持每Frame/Side一条主行，几何表 MUST只保存紧凑几何记录
- **AND** Finalizer完成后 MUST由同一Analyzer与Publisher生成facts和独立diagnoses
