## MODIFIED Requirements

### Requirement: Landing Prediction必须形成独立世界事实

每只脚 MUST按`正式Foot Motion Step Event -> committed Body Target世界速度 + Timeline段边界/Continuation -> 根Bank共享Prediction Motion State -> KCC Future Body Translation -> Raw Landing -> Future Landing SphereCast -> Accepted/Rejected Observation -> Landing Tracking/Commit`执行。Step Event MUST携带同Source、Cycle、Side与ordinal的稳定Landing Event identity，并使用正式Step Time作为预测时域、正式Step Distance作为相邻同脚Event与RootLocalLanding水平步长一致性证据。

Raw Landing MUST继续按`VisiblePosition + FutureBodyTranslation + VisibleRotation * RootLocalLanding`从本帧输入重新投影。Step Distance MUST不替代committed Body世界速度、Future Body Translation或世界地形；RootLocalLanding MUST只乘本帧Visible Rotation，不外推Future Body Yaw。

Foot根Bank MUST为每个Actor保存一份左右脚共享的Prediction Motion State，状态至少包含稳定当前速度、稳定Continuation速度、初始化事实、Timeline Generation、Body Reset Sequence与Prediction Source identity。Runtime MUST对当前与Continuation世界速度分别应用Profile显式`PredictionVelocityDeltaThreshold`、`PredictionVelocitySmoothSpeed`与`PredictionMaximumSpeed`：速度差未超过阈值时保持稳定速度，超过时按Presentation Delta执行有界EMA响应，并把结果限制在最大预测速度内。三个配置 MUST为有限正值、进入Profile Revision且由正式Corin Profile显式序列化；缺失或非法时 MUST发布typed invalid，不得使用代码默认值、旧配置或普通/预测回退路径。

committed Timeline当前/Continuation速度、Presentation Delta和所有中间Prediction量 MUST通过有限值与lineage校验后才能推进Prediction Motion State。输入非法时，Runtime MUST使当前Pending Foot事务失败并保持上一Committed Prediction状态不变，不得把NaN/Inf、错误Generation或部分更新结果送入EMA，也不得把上一输出改名为本帧成功结果。合法但幅度或方向急变的正式速度 MUST进入同一阈值/EMA/上限控制，不得由未经验证的PIK相对突变公式静默丢弃。

首次合法输入 MUST直接以正式速度初始化Prediction Motion State。Body Reset、Retarget、Timeline Generation变化或Prediction Source变化 MUST清空状态；普通Landing Event、Animation Source、左右脚Step或Source Sample变化 MUST不重置角色级稳定速度。唯一KCC Future Body Translation MUST消费稳定当前/Continuation速度并在同一Pending Workspace内服务左右脚；Prediction State与Workspace MUST使用根Bank预分配的固定布局，不得在表现帧热路径创建Trajectory对象、临时Sample数组或托管集合。Runtime MUST不复制KCC、在KCC结果后平滑世界位置或创建第二Trajectory Source。

Runtime MUST把上次真实查询使用的Side、Landing Event、Source Sample identity、Source Cycle、按1毫米量化的Raw Landing、按`1e-4`量化的Component Up、Profile Revision与非零World Revision保存在Committed Observation Page中。Landing处于Tracking时，除正式`Sliding`接触准入或强制lineage变化外，当前Raw Landing Candidate相对该查询快照的世界位移累计不超过Profile显式`PredictionInputAccumulationDistance`且Component Up夹角不超过`ComponentUpChangeAngleDegrees`时，Runtime MUST复用根事务已提交的不可变Accepted或Rejected Observation，不得更新查询快照或执行SphereCast。距离阈值 MUST为正且不得超过SphereCast半径。

Tracking阶段超过任一累计阈值，或Landing Event、Source Sample、Source Cycle、Profile Revision、World Revision变化时，Runtime MUST从当前Candidate生成新的canonical Landing Observation Key并恰好查询一次。SphereCast MUST从Key反量化后的canonical Raw Landing上方沿Component Down使用Profile半径和有限距离查询，并过滤自身Collider、初始重叠、非法点、非法法线与超坡度命中，在固定容量合法候选中按距离与稳定identity选择canonical最近候选。容量溢出或没有合法命中 MUST发布typed拒绝；该Rejected Observation MUST保持自己的Key和结果，不得改名为另一Key、旧Landing、默认Surface或另一Event结果。Landing Tracking MAY继续持有同Event此前已经Accepted的NextSwingLanding，但 MUST保留其原始Observation lineage并同时发布本次Rejected事实，不得把保留Landing描述为本次查询命中。

上一Committed Surface、Frame、Authority Tick、Trajectory Generation、Future Translation Source、Foot State、Residual与查询输出 MUST不进入Observation Key或候选选择。Pending Observation和Prediction Motion State MUST随Foot根事务提交或丢弃；Reset、Retarget与World Query Backend重建 MUST清空Observation Page和Landing承诺，Prediction Motion State再按自身重置规则处理。当前静态FootPlacementSurface在Backend生命周期内使用固定非零World Revision；移动平台不属于本change。

#### Scenario: 正式Step预测命中

- **WHEN** 同一正式Event具有合法Step Time、Step Distance、RootLocalLanding、Future Body Translation和SphereCast命中
- **THEN** Runtime MUST发布唯一Accepted Landing、Surface、点、法线、查询距离及完整Event lineage
- **AND** Step Time变化 MUST只改变该Event预测时域，不得重建另一套Landing生命周期

#### Scenario: 高角速度下稳定Prediction速度

- **WHEN** 同一Timeline Generation内角色急转导致相邻表现帧的committed世界速度方向大幅变化
- **THEN** 根Bank MUST先按正式阈值、EMA响应和最大速度更新共享Prediction Motion State，再用稳定速度生成唯一KCC Future Body Translation
- **AND** 左右脚 MUST读取同一Workspace在各自正式Step Time的Sample，不得各自滤波或直接使用瞬时世界速度建立第二轨迹

#### Scenario: Prediction Motion状态重置

- **WHEN** Body Reset、Retarget、Timeline Generation或Prediction Source发生正式变化
- **THEN** Runtime MUST清空旧稳定速度并以新lineage首个合法正式速度重新初始化
- **AND** MUST不把另一Generation、Source或被Discard帧的速度历史带入新Prediction

#### Scenario: Prediction输入非法

- **WHEN** committed当前/Continuation速度、Presentation Delta或Prediction lineage缺失、非有限或不匹配
- **THEN** 当前Pending Foot事务 MUST失败且上一Committed Prediction Motion State MUST保持不变
- **AND** Runtime MUST不执行部分EMA、不发布伪Stable速度或把上一帧结果标记为本帧成功

#### Scenario: 预测输入累计变化未超过阈值

- **WHEN** 当前Candidate与上一真实查询属于同Source、Cycle、Event、Profile、World，累计世界位移不超过正式距离阈值且Up变化不超过正式角度阈值
- **THEN** Runtime MUST复用相同Observation identity、Surface、点、法线与Reject结果
- **AND** MUST不更新查询快照、不执行FutureLanding SphereCast或读取上一Surface重新选择候选

#### Scenario: 预测输入累计变化超过阈值

- **WHEN** 当前Candidate相对上一真实查询的世界位移或Component Up夹角超过正式阈值
- **THEN** Runtime MUST执行一次FutureLanding SphereCast并提交canonical最近合法候选或typed拒绝
- **AND** Pending Foot事务失败时 MUST丢弃该Observation，不得污染上一Committed Page

#### Scenario: 预测lineage变化

- **WHEN** Landing Event、Source Sample、Source Cycle、Profile Revision或World Revision变化
- **THEN** Runtime MUST不受距离或角度阈值限制并执行一次新查询
- **AND** 新查询 MUST建立新的Observation identity，不得把旧结果改名继续使用

#### Scenario: Sliding接触准入刷新

- **WHEN** Landing仍处于Tracking、正式Foot Lock Mode为`Sliding`且当前canonical预测输入identity不同于上一真实查询输入
- **THEN** Runtime MUST执行一次新查询并发布`ContactAcquisitionRefresh`原因
- **AND** canonical输入identity未变时 MUST复用Committed Observation；Landing已经Committed时 MUST使用承诺落点做Lock准入，不得以Sliding为由晚期重查

#### Scenario: Step Event与RootLocalLanding不一致

- **WHEN** 正式Step Distance、Event table与RootLocalLanding水平位移不满足编译容差或lineage不匹配
- **THEN** Projection Build或当前Foot帧 MUST发布typed invalid
- **AND** MUST不读取旧隐藏Step Event或重新编号继续预测

### Requirement: Ground Path必须使用上一已提交落点与下一事件落点

每只脚 MUST按Landing Event identity在同一Landing Context中维护`Empty / Tracking / Committed`所有权；Promotion只作为事件成为Current Contact时的当帧输出事实，不得形成第二状态机。PreSwing与早期Swing MUST进入Tracking并重新投影Raw Landing Candidate；只有累计输入或强制lineage触发Query Admission时 MUST执行一次正式Landing SphereCast，其余帧 MUST复用Committed Observation。Tracking中新Observation命中不同Surface时 MUST无条件提交新的NextSwingLanding；同Surface新点与NextSwingLanding的距离小于正式`LandingAcceptanceDistance`时 MUST保留原落点并复用Ground Path，达到阈值时 MUST提交新点。

正式Foot Motion进入`ApproachContactToLanding`且存在同Event Accepted NextSwingLanding时，Runtime MUST把该Landing原子提交为Committed。Committed阶段 MAY继续计算Raw Candidate供诊断，但普通速度、角度、Source Sample或Surface变化 MUST不创建新Observation Key、执行SphereCast、换点或重建Ground Path。该Event成为Current Contact Event时，Committed Landing MUST晋级为LastLanding并发布Promoted Contact Landing，之后才为新的Swing Event建立Tracking。

Ground Path MUST只使用LastLanding与NextSwingLanding构造查询输入。没有LastLanding时 MUST发布`CurrentLandingUnavailable`；不得用Animated Sole、Transform、固定高度或默认地面补起点。

#### Scenario: 查询前累计输入持续低于阈值

- **WHEN** PreSwing或Swing连续帧相对上次真实查询的累计位移和Up变化持续不超过正式阈值且lineage稳定
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

#### Scenario: 新Observation切换Surface

- **WHEN** Tracking阶段一次正式新查询命中与同Event NextSwingLanding不同的Surface
- **THEN** Runtime MUST无条件提交新NextSwingLanding并重建Ground Path
- **AND** MUST不以LandingAcceptanceDistance保留旧Surface

#### Scenario: Approach Contact提交Landing

- **WHEN** 正式Foot Motion首次进入`ApproachContactToLanding`且Tracking已经持有同Event Accepted NextSwingLanding
- **THEN** Runtime MUST在同一根事务内把该Landing提交为Committed并保留其Surface、点、法线、Observation与Event lineage
- **AND** 后续普通Prediction变化 MUST不再查询、换点、切Surface或重建该Event的Ground Path

#### Scenario: Approach Contact没有可提交Landing

- **WHEN** 正式Foot Motion进入`ApproachContactToLanding`但同Event从未产生Accepted NextSwingLanding
- **THEN** Runtime MUST发布typed unavailable并保持该Event没有Committed Landing
- **AND** MUST不使用Animated Sole、旧Event、默认Surface或Rejected Observation建立承诺

#### Scenario: Committed阶段出现晚期Candidate变化

- **WHEN** Landing已经Committed且后续Raw Candidate因急转、速度或Source Sample变化超过查询阈值
- **THEN** Runtime MAY记录晚期Candidate与忽略原因，但 MUST继续消费原Committed Landing和Ground Path
- **AND** MUST不创建普通Observation Key、执行SphereCast或把晚期Candidate传给Contact Anchor

#### Scenario: Tracking查询拒绝后保留事件Landing

- **WHEN** Tracking已经持有同Event Accepted NextSwingLanding，而后续新Key查询产生typed拒绝
- **THEN** Runtime MUST提交Rejected Observation事实并 MAY继续持有原NextSwingLanding及其原始lineage
- **AND** MUST不把原Landing改名为Rejected Key的结果或声称本次查询成功

#### Scenario: 下一Swing Event完成

- **WHEN** NextSwingLanding对应的事件成为已完成Swing Event
- **THEN** Runtime MUST把该Accepted Landing晋级为新的LastLanding
- **AND** MUST只为新的PreSwing或Swing Event建立新的NextSwingLanding

### Requirement: Foot Lifecycle必须生成唯一权威结果

每只脚 MUST在同一根事务内按固定顺序执行`不可变输入与Observation -> Pre-Interpolation Transition -> State Target -> Interpolation -> Post-Interpolation Transition -> Hard Constraint -> Resolved Foot`。这些阶段 MUST每帧各执行一次，并只发布一份离散State、一份Effective Correction和一个Resolved Foot。

顶层离散State MUST继续只包含`Swing / UnlockedSupport / Landing / Locked / Releasing`，不得增加Rebound、Blocked、Grounded或第二套状态枚举。根Bank内部 MUST为每脚保存唯一typed Contact Transition Context，至少包含上一Committed正式Lock请求、距最近Contact边沿的秒数、最近Contact Event identity和最近释放Contact Event identity。Contact Rising、Contact Falling与Same-Event Reentry Refresh MUST只作为本帧Transition事实或Reason发布，不得成为新的顶层State、Anchor Owner或Interpolation Owner。

唯一typed `CharacterFootTransitionResolver` MUST只读取正式Foot Motion Frame、不可变Ground Observation、上一Committed离散State和当前阶段事实，并发布不可变Transition Decision。唯一Transition Runtime MUST应用Decision中的State与Anchor命令；Resolver和Runtime MUST不推进Residual、计算State Target、查询世界或写Goal。Pre与Post阶段允许的Transition边、优先级和输入集合 MUST固定且可编译校验。允许边固定为`Swing -> Landing | UnlockedSupport`、`UnlockedSupport -> Landing | Swing`、`Landing -> Locked | Releasing`、`Locked -> Releasing`、`Releasing -> Landing | Swing`；其中`Releasing -> Landing`只能由同Event Reentry Refresh在Pre阶段触发，`Releasing -> Swing`只能由Interpolation Completion在Post阶段触发。

纯`CharacterFootStateTargetResolver` MUST按Transition后的离散State生成Correction Target、Contact Reference、Goal与Ownership目标及typed Interpolation Policy Request。Swing与UnlockedSupport的Target MUST只使用正式Ground Path、Envelope与Foot Height；Releasing MUST只回到原始Swing Target。Resolver MUST不保存跨帧时间状态、不推进Residual、不改写State、不得执行World Query，也不得执行Hard Constraint。

唯一typed `CharacterFootInterpolationRuntime` MUST拥有上一Target、Effective Correction、唯一Residual与Completion。Swing Path换代、Landing Acquire和Release MUST只通过固定typed Policy Request连续化；迁移完成后 MUST删除分散的`SwingResidual`、`AcquireResidual`、`ReleaseResidual`、`ContactProgress`和重复Advance数学。Residual大于`SwingResidualTolerance`时，Interpolation Runtime MUST按正式Step Time计算Landing截止收敛；Releasing完成 MUST只读取独立`ReleaseCompletionTolerance`。Step Time只决定Residual衰减，不得改变Raw Target、重选State或掩盖同帧不连续。

Ground Path Envelope和Reach MUST在Interpolation之后作为Hard Constraint执行。Swing Hard Constraint MUST复用本帧Accepted Swing Motion已经采样的同一Envelope Point与Path identity，不得执行Raycast、SphereCast或读取另一Surface；只有连续输出低于Envelope时 MAY立即Clamp。Hard Constraint MUST不修改State Target、不触发Residual Revision，也不得写回Interpolation历史；它 MAY限制已知不可达Goal，但 MUST不反向修改State、Transition Decision或Target。全部分型状态 MUST由同一根Bank统一Seal或Discard，不得形成第二状态机、第二生命周期或第二输出路径。

Swing Target MUST只使用Last Landing、Next Landing、Runtime Ground Envelope与正式Foot Height。Accepted Swing Motion MUST携带同Ground Path Event的typed Swing Path Landing Reference；Promoted Contact Landing MUST只服务Contact与Anchor。Path Residual Revision MUST只由Event、可用性或Accepted Landing端点变化触发；Ground Path identity单独变化和同一Path内的Phase目标推进 MUST不发布Path Revision。正式Swing目标的有效变化 MAY通过独立typed Target Tracking事实连续接管，但 MUST不伪装成Path Residual重建。Diagnostics MUST分别发布原始Builder Swing Target、State Target、Path Revision与Target Tracking，不得互相改名覆盖。

Landing Anchor MUST在正式Contact有效、同Event Lock Mode首次从Unlocked进入Sliding或Locked、Committed Landing合法且该Event没有Active或Retained Anchor时由唯一Transition Runtime建立。正式Lock Weight MUST通过Interpolation Policy Request渐进接管Anchor。正式Contact退出或Mode回到Unlocked时 MUST进入Releasing、记录Contact Falling与最近释放Event并继续Retain原Anchor；只有Releasing完成进入Swing后该Event才闭合并清除Anchor。完成帧 MUST先应用Post-Interpolation Transition，再按新State执行Ground Path Envelope Hard Constraint和最终输出分类，不得重跑State Target或Interpolation。

Releasing期间同Event再次出现Sliding或Locked请求，且原Anchor仍保留、Committed Landing、Lock距离和Reach继续合法时，Resolver MUST发布typed `SameEventContactReentryRefresh`并在Pre-Interpolation阶段执行`Releasing -> Landing`。Transition Runtime MUST只Retain原Anchor，State Target MUST立即重新计算同Anchor目标，Interpolation Runtime MUST从当前Effective Correction连续接管；系统 MUST不创建Anchor、不执行Landing Query、不移动Committed Landing，也不得把Interpolation清零。Release已经完成或Anchor已经清除时，旧Event MUST不复活；不同Event即使紧接上一Contact边沿也 MUST按自己的Committed Landing正常准入。Contact Transition Context MUST只由唯一Transition Runtime随根Pending Bank更新；Pending失败或Discard MUST保持上一Committed边沿历史不变。迁移完成后全部阶段 MUST不读取旧PlantConfidence、PlantCycleConsumed布尔或旧Constraint Weight决定Landing、Lock与Release。

#### Scenario: 同Event Path换代

- **WHEN** 同一Swing Event的Landing或Envelope Target发生正式Revision
- **THEN** State Target Resolver MUST发布新Target，Interpolation Runtime MUST从上一Effective Correction连续接管并按Step Time在SwingResidualTolerance内收敛
- **AND** 只有同一Accepted Ground Path Envelope高于连续输出时 Hard Constraint MAY立即向上Clamp并发布Safety Floor事实

#### Scenario: 旧Contact Event与新Swing Event同帧交接

- **WHEN** 旧Event的Landing在当前帧成为Promoted Contact Landing，且下一Event已经具有Accepted Swing Motion与匹配Ground Path
- **THEN** Transition与State Target MUST让旧Landing只服务Contact与Anchor，并让新Swing Path Landing继续服务Swing Target与Interpolation
- **AND** MUST不因两者Event不同把Swing Path发布为一帧不可用

#### Scenario: 正式Lock渐进接管

- **WHEN** 同Event Lock Mode从Unlocked进入Sliding且Lock Weight从0连续增加
- **THEN** Transition Runtime MUST建立一次Anchor，State Target MUST发布Anchor目标，Interpolation Runtime MUST按Weight连续接管
- **AND** MUST不新增固定Duration、第二Landing状态、第二Anchor或状态私有Residual

#### Scenario: Releasing期间同Event Contact重新请求Lock

- **WHEN** 某Contact Event已经进入Releasing、原Anchor仍保留，之后同Event再次出现合法正式Contact与Sliding或Locked请求
- **THEN** Transition Resolver MUST发布`SameEventContactReentryRefresh`并执行`Releasing -> Landing`，Transition Runtime MUST Retain原Anchor
- **AND** State Target MUST重新计算同Anchor目标，Interpolation MUST从当前Effective Correction连续接管，不得查询Landing、创建Anchor或清零历史

#### Scenario: Release完成后旧Event再次请求Lock

- **WHEN** Releasing已经完成且原Anchor已经清除，旧Event再次出现Sliding或Locked请求
- **THEN** Runtime MUST发布typed旧Event重入不可用并保持没有Contact Anchor
- **AND** MUST不复活旧Committed Landing、创建新Anchor或沿用已清除的Interpolation接管事实

#### Scenario: 新Event紧接上一Contact边沿

- **WHEN** 上一Contact Event刚进入Releasing或已经完成，而新的Event已经具有Committed Landing、正式Contact和Sliding或Locked请求
- **THEN** 新Event MUST按自己的Event identity正常执行Contact Rising与Anchor准入
- **AND** MUST不因上一Event的驻留时间、回弹事实或已消费identity被错误抑制

#### Scenario: Contact边沿事实保持内部

- **WHEN** 正式Contact或Lock请求发生上升沿、下降沿或同Event Reentry Refresh
- **THEN** Runtime MUST只更新同一根Bank内的Contact Transition Context并发布typed Decision事实
- **AND** Resolved Foot、Primary Support、Pelvis和Goal MUST不接收Rebound状态、边沿计时器或可变Context

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

Pelvis Builder MUST同时读取Primary Support腿Reach与Landing Reach Request，并使用Foot Motion Profile中必须显式序列化的米制最小Landing腿压缩余量计算沿Component Up的可行交集。Profile还 MUST显式序列化有限正值`PelvisMaximumUpVelocity`与`PelvisMaximumDownVelocity`并纳入Revision；任一配置缺失、非有限或越界时 MUST发布typed invalid，不得使用代码默认值或旧配置补全。交集存在时，Pelvis Target与Spring Output MUST限制在交集内；唯一Critical Spring积分后的Velocity MUST限制在`[-PelvisMaximumDownVelocity, PelvisMaximumUpVelocity]`，Output撞到Reach边界且Velocity继续朝外时 MUST清除对应方向速度。Support换代、坡度变化和目标跨越仍必须保持现有显式Handoff与Velocity Reset事实。

交集不存在时，系统 MUST优先保持Primary Support腿安全，把Landing Foot Goal夹紧到保留最小压缩余量的最大可达点，发布`LandingReachUnavailable`并禁止该脚进入Full Lock。FBBIK MUST不接收已知超出可达区间的Landing目标后仅靠膝盖完全伸直夹紧。

#### Scenario: 双腿Reach存在交集

- **WHEN** Primary Support腿和Landing腿沿Up可达区间存在交集
- **THEN** Pelvis Target与Spring Output MUST位于该交集内
- **AND** Landing Foot Goal MUST保持至少Profile声明的最小压缩余量

#### Scenario: 双腿Reach没有交集

- **WHEN** 保持Primary Support安全与Landing腿最小压缩余量无法同时满足
- **THEN** Runtime MUST夹紧Landing Foot Goal并发布`LandingReachUnavailable`
- **AND** 该脚 MUST保持Landing、Sliding或进入Releasing，不得进入Full Lock或输出已知超长Goal

#### Scenario: Pelvis非对称速度边界

- **WHEN** 合法Pelvis Target要求Spring分别向上或向下移动
- **THEN** Runtime MUST使用Profile对应方向的最大速度限制Spring Velocity，并把Output保持在双腿Reach交集内
- **AND** MUST不以单一共享速度、隐藏默认值或Final Pose低通替代正式上下行边界

#### Scenario: Pelvis撞到Reach边界

- **WHEN** Pelvis Spring Output到达可行交集上界或下界且Velocity仍朝区间外
- **THEN** Runtime MUST把Output限制在边界并清除继续向外的Velocity分量
- **AND** 后续Target回到区间内时 MUST从同一唯一Spring State继续响应，不建立第二Pelvis平滑器

## ADDED Requirements

### Requirement: Foot诊断必须证明Path安全与Landing可达责任

封口Foot诊断 MUST在同Frame、Completion、Program、Projection、Rig、Event与Surface lineage下同时记录正式Step/Foot Height/Contact/Lock/Support输入、上一与当前Lock请求、Contact Rising/Falling、距最近边沿秒数、最近与最近释放Contact Event、Same-Event Reentry Refresh/Unavailable结果、Retained Anchor与连续接管事实、Raw Timeline当前/Continuation速度、稳定Prediction速度、速度差阈值、EMA响应、最大速度Clamp、Prediction状态初始化/重置原因、KCC Future Translation、Prediction Candidate与上次查询快照、累计位移、Up夹角、两个查询阈值、Query Reason、Landing Tracking/Committed状态、Commit Frame/Reason、晚期Candidate忽略原因、Path Revision原因、Raw Landing/Path Target、Pre/Post Transition Decision、State Target、Interpolation Policy/Residual/Output/Completion、Hard Constraint前后Correction、Encoded Goal、Residual基础与截止HalfLife、Ground Path Envelope Clamp与clearance、Support与Landing Reach区间、Pelvis上下速度边界、Goal夹紧量、Target/Solved Extension Ratio、Compression Reserve和Physical结果。

Diagnostics MUST只读取Committed Source、Path、Context、Resolved、Goal、Solved与Final Publication结果，不得创建Anchor、选择Support、修改Reach、Clamp Goal或执行第二次World Query。

#### Scenario: Path Revision产生Ground Path Envelope Clamp

- **WHEN** Accepted Ground Path Envelope的最低安全Correction高于连续Swing输出
- **THEN** 诊断 MUST记录Path identity、Envelope Point、Clamp前后Correction和Safety Floor clearance
- **AND** MUST区分普通目标跟随与真实地面安全抬升

#### Scenario: Prediction速度稳定阻止晚期Landing甩动

- **WHEN** committed世界速度方向单帧大幅变化但Event、RootLocalLanding与正式Step lineage保持一致
- **THEN** 诊断 MUST并列记录Raw/Stable速度、KCC Future Translation、Raw Landing、Tracking/Committed状态与最终Landing消费结果
- **AND** MUST能区分Prediction输入断点、Observation换代、Landing提交、Interpolation响应和Hard Constraint抬升

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

每个Foot诊断包 MUST写入项目本地持久目录`Diagnostics/FootPlacementRuns/<run-id>/`，MUST NOT写入Unity `Temp`。该目录 MUST只保存本地原始诊断，Recorder MUST NOT自动复制、晋升或加入版本控制；需要提交的诊断基线由作者明确选择后另行归档。

停止采样与捕获队列失败 MUST统一进入`Finalizing`。Unity主线程 MUST停止捕获并立即返回；唯一后台Finalizer MUST排空现有Writer、封存双表、运行同一Analyzer与Publisher并原子发布facts和diagnoses。程序集重载 MAY等待同一Finalizer完成以保护包完整性，但不得建立同步Analyzer、Python Reporter、第二输出schema或仅扩大队列的替代路径。

#### Scenario: Unity清理临时目录后保留诊断包

- **WHEN** 一次Foot诊断已经完成且Unity随后清理项目临时目录或重新启动
- **THEN** 完整诊断包 MUST仍保留在`Diagnostics/FootPlacementRuns/<run-id>/`
- **AND** Recorder查找最近一次采样 MUST只使用该持久目录
- **AND** 系统 MUST NOT自动移动、覆盖或删除已有诊断包

#### Scenario: 停止包含大量Ground Path几何的录制

- **WHEN** 当前录制已经积累大量Ground Contact与Envelope顶点且作者点击停止
- **THEN** Editor MUST进入Finalizing而不在停止回调中等待Writer或扫描CSV
- **AND** `samples.csv` MUST保持每Frame/Side一条主行，几何表 MUST只保存紧凑几何记录
- **AND** Finalizer完成后 MUST由同一Analyzer与Publisher生成facts和独立diagnoses
