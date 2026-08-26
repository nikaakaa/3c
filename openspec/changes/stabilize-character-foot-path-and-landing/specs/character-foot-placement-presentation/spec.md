## MODIFIED Requirements

### Requirement: Landing Prediction必须形成独立世界事实

每只脚 MUST按`正式Foot Motion Step Event -> committed Body Target世界速度 + Timeline段边界/Continuation -> KCC Future Body Translation -> Raw Landing -> Future Landing SphereCast -> Accepted/Rejected Landing`执行。Step Event MUST携带同Source、Cycle、Side与ordinal的稳定Landing Event identity，并使用正式Step Time作为预测时域、正式Step Distance作为相邻同脚Event与RootLocalLanding水平步长一致性证据。

Raw Landing MUST继续按`VisiblePosition + FutureBodyTranslation + VisibleRotation * RootLocalLanding`从本帧输入重新投影。Step Distance MUST不替代committed Body世界速度、Future Body Translation或世界地形；RootLocalLanding MUST只乘本帧Visible Rotation，不外推Future Body Yaw。

SphereCast MUST继续从Raw Landing上方沿Component Down使用Profile半径和有限距离查询，并过滤自身Collider、初始重叠、非法点、非法法线与超坡度命中。容量溢出或没有合法命中 MUST发布typed拒绝，不得沿用旧Landing、默认Surface或另一Event结果。

#### Scenario: 正式Step预测命中

- **WHEN** 同一正式Event具有合法Step Time、Step Distance、RootLocalLanding、Future Body Translation和SphereCast命中
- **THEN** Runtime MUST发布唯一Accepted Landing、Surface、点、法线、查询距离及完整Event lineage
- **AND** Step Time变化 MUST只改变该Event预测时域，不得重建另一套Landing生命周期

#### Scenario: Step Event与RootLocalLanding不一致

- **WHEN** 正式Step Distance、Event table与RootLocalLanding水平位移不满足编译容差或lineage不匹配
- **THEN** Projection Build或当前Foot帧 MUST发布typed invalid
- **AND** MUST不读取旧隐藏Step Event或重新编号继续预测

### Requirement: Foot Placement必须通过统一状态机生成双脚修正

每只脚 MUST继续只有一个固定typed `CharacterFootStateContext`、一个`CharacterFootStateMachine`、一个Effective Correction Owner和一个Anchor Owner。State Machine MUST使用正式Foot Motion Frame、不可变Ground Observation和上一Committed Context生成Swing、Landing、Locked、Releasing或UnlockedSupport；不得恢复旧Lifecycle对象、第二状态机或Goal后处理器。

Swing MUST使用Last Landing、Next Landing、Runtime Ground Envelope和正式Foot Height生成目标。Accepted Swing Motion MUST携带同Ground Path Event的typed Swing Path Landing Reference；Promoted Landing与按当前Step解析的Landing MUST只属于Contact/Anchor，不得门控Swing Path可用性或提供Swing Residual的Landing Point。Path Revision MUST由Event、可用性、Landing端点或实际Swing目标的有效变化触发，并通过唯一Swing Residual保持连续；Ground Path identity单独变化 MUST不每帧重置Residual。Raw Landing/Path Target、Swing Target、Captured Residual、State Output、Safety Floor Output与Encoded Goal之间 MUST保持同Frame可对账，后继阶段 MUST不把小幅输入变化无依据放大为更大的Correction跳变。Residual大于LandingUpdateDistance时 MUST按正式Step Time计算Landing截止收敛，但Step Time MUST只决定已经连续的Residual衰减，不得用于掩盖同帧不连续，也不得平滑穿过真实Envelope最低安全高度。

Landing Anchor MUST在同Event正式Lock Mode首次进入Sliding或Locked且Accepted Landing合法时建立。Acquire Residual MUST保存当前Output到Anchor的连续差，并由正式Lock Weight消退。正式Locked Mode与完成Lock Weight进入Locked FullAnchor；已锁脚返回Sliding Mode时保持同一Anchor和顶层Locked生命周期，只切换内部Sliding Response。正式Contact退出或Mode回到Unlocked时进入Releasing。

Releasing完成并回到Swing的同一帧 MUST先更新State，再执行新Swing Ground Floor与最终输出分类。迁移完成后State Machine MUST不读取旧PlantConfidence、PlantCycleConsumed或旧Constraint Weight决定Landing、Lock与Release。

#### Scenario: 同Event Path换代

- **WHEN** 同一Swing Event的Landing或Envelope目标发生正式Revision
- **THEN** State Machine MUST从上一Effective Correction连续接管新目标，并按Step Time在LandingUpdateDistance内收敛
- **AND** 只有真实Envelope高于连续输出时 MAY立即向上Clamp并发布Safety Floor事实

#### Scenario: 旧Contact Event与新Swing Event同帧交接

- **WHEN** 旧Event的Landing在当前帧成为Promoted Contact Landing，且下一Event已经具有Accepted Swing Motion与匹配Ground Path
- **THEN** State Machine MUST让旧Landing只服务Contact/Anchor，并让新Swing Path Landing继续服务Swing目标与Residual
- **AND** MUST不因两者Event不同把Swing Path发布为一帧不可用

#### Scenario: 正式Lock渐进接管

- **WHEN** 同Event Lock Mode从Unlocked进入Sliding且Lock Weight从0连续增加
- **THEN** 唯一State Machine MUST建立一次Anchor并在现有Landing状态中按Weight消退Acquire Residual
- **AND** MUST不新增固定Duration、第二Landing状态或第二Anchor

#### Scenario: Releasing完成进入Swing

- **WHEN** Releasing满足完成条件且当前帧具有合法Swing Envelope
- **THEN** State Machine MUST在同帧切换Swing并执行该Envelope最低安全Correction
- **AND** 发布为Swing的Corrected Sole MUST不因旧Releasing执行顺序留在Envelope下方

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

Pelvis Builder MUST同时读取Primary Support腿Reach与Landing Reach Request，并使用Foot Motion Profile的米制最小Landing腿压缩余量计算沿Component Up的可行交集。交集存在时，Pelvis Target与Spring Output MUST限制在交集内；Support换代、坡度变化和目标跨越仍必须保持现有显式Handoff与Velocity Reset事实。

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

封口Foot诊断 MUST在同Frame、Completion、Program、Projection、Rig、Event与Surface lineage下同时记录正式Step/Foot Height/Contact/Lock/Support输入、Path Revision原因、Raw Landing/Path Target、Swing Target、Captured Residual、State Output、Safety Floor Output、Encoded Goal、Residual基础与截止HalfLife、Safety Floor Clamp、Envelope clearance、Support与Landing Reach区间、Goal夹紧量、Target/Solved Extension Ratio、Compression Reserve和Physical结果。

Diagnostics MUST只读取Committed Source、Path、Context、Resolved、Goal、Solved与Final Publication结果，不得创建Anchor、选择Support、修改Reach、Clamp Goal或执行第二次World Query。

#### Scenario: Path Revision产生Safety Floor Clamp

- **WHEN** 新Ground Envelope最低安全高度高于连续Swing输出
- **THEN** 诊断 MUST记录Revision原因、Clamp前后Correction、Envelope clearance和对应Surface lineage
- **AND** MUST区分普通目标跟随与真实地面安全抬升

#### Scenario: Correction在Path后继阶段被放大

- **WHEN** Raw Landing、Path Target或Swing Target只有小幅单帧变化，但后继State Output、Safety Floor Output或Encoded Goal产生明显更大的Correction变化
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
