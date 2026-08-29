## 1. 固定依赖与唯一输入

- [ ] 1.1 确认`build-character-foot-motion-data-foundation`已经由用户验收并归档，记录正式Curve Catalog、Artifact format、algorithm version与Corin Registered Curve Hash
- [ ] 1.2 固定Foot模块的typed输入、唯一Result与根事务边界，让`refactor-character-pose-graph-architecture`只消费不透明Constraint合同而不规定Foot内部布局
- [ ] 1.3 固定Corin范围和TrainingEnemy禁区，确认本change内只有一套Recorder、Analyzer与Publisher

## 2. 发布唯一Foot Motion Runtime Frame

- [ ] 2.1 扩展Projection Compiler，从正式AnimationClip Curve组和匹配Artifact事件降低唯一Foot Motion payload与稳定Landing Event table
- [ ] 2.2 让选中Live Animation Source按同Contribution、Cycle、Normalized Time和Completion采样左右正式Foot Motion Sample
- [ ] 2.3 把唯一typed Foot Motion Frame接入Foot Placement Pose Input，并严格校验Source与Contribution lineage
- [ ] 2.4 对缺失、重复、旧binding、Event不一致和非有限值发布typed invalid，不读取旧Artifact或默认值补全
- [ ] 2.5 让稳定Landing Event table正式保存PreSwing、Swing、Approach Contact与Landing边界，并由同Source/Cycle/Side/ordinal的Runtime Frame发布`InApproachContactToLanding`供Plant目标准备消费

## 3. 收口Path换代与Floor顺序

- [x] 3.1 让Releasing完成先更新为Swing，再执行同帧Swing Ground Envelope保护和最终输出分类
- [x] 3.2 让Path Revision只由Event、可用性、Landing端点或实际Swing目标的有效变化触发，不因identity单独变化每帧重置Residual
- [x] 3.3 在同Frame、Side与Event lineage下补齐Raw Landing/Path Target、Swing Target、Captured Residual、State Output、Safety Floor Output和Encoded Goal逐阶段事实
- [x] 3.4 用最新代表事件定位Correction首次不连续或放大的正式阶段，区分Target换代、Residual Capture、State所有权、Safety Floor与Goal编码责任
- [x] 3.5 拆分Accepted Swing Path Landing与Promoted Contact Landing所有权，消除Event交接帧错误的Path不可用，不用更短HalfLife、Step Time截止、Goal低通或Solver后处理掩盖同帧跳变
- [x] 3.6 把Swing硬Floor收敛为同一Accepted Ground Path Envelope，删除逐帧CurrentSwingFloor Query，并区分普通目标追踪与Envelope Clamp
- [x] 3.7 扩展正式诊断事实，记录Revision原因、逐阶段Correction、Envelope clearance和Releasing到Swing转换结果
- [x] 3.8 把采样包迁移为项目本地持久`Diagnostics/FootPlacementRuns/<run-id>/`下的每Frame/Side唯一主行与独立Ground Path几何表，删除Unity Temp写入和每个Contact/Envelope重复整套阶段列的旧展开行
- [x] 3.9 让停止与队列失败统一进入后台Finalizing，排空Writer、封存双表并运行唯一Analyzer/Publisher后再发布结果
- [x] 3.10 为每脚建立根事务所有的Landing Observation Key、Committed/Pending Page与双页Pool，相同Key复用已提交Accepted或Rejected结果
- [x] 3.11 让超过正式累计阈值或Source/Cycle/Event/Profile/World lineage变化只执行一次canonical SphereCast并删除PreferredSurfaceIdentity选择行为
- [x] 3.12 把Observation identity、World revision、cache state、query executed与canonical Raw Landing接入唯一facts/diagnosis链并删除Preferred旧口径
- [x] 3.13 为Corin显式配置5厘米预测输入累计距离与1度Component Up变化阈值，阈值内复用Committed Observation，并在正式Sliding接触准入输入变化时刷新观测
- [x] 3.14 让PreSwing与Swing消费同一Accepted Swing Motion和Ground Envelope，消除进入Swing首帧才补Path的Correction跳变
- [x] 3.15 在Tracking阶段让新查询的Surface变化只无条件换代NextSwingLanding，不得覆盖Current Contact Anchor或受LandingAcceptanceDistance保留
- [x] 3.16 把现有Landing Context收敛为可并存的`NextSwing Empty/Tracking`、`Plant Target Tracking/Verified`与`Verified LastLanding` typed槽位，Prediction Landing、连续Plant目标与真实Plant事实分权但不建立第二状态机
- [x] 3.17 让正式`ApproachContactToLanding`保持同Event Prediction Tracking与Ground Path几何更新，同时由持久Plant Target隔离可见输出并作为唯一Interpolation的Plant目标准备区；不得在实际Contact Rising前冻结世界落点或把每次Path Revision直接写入可见Correction
- [x] 3.18 在Approach Contact没有同Event Accepted Landing时发布typed unavailable，不用Animated Sole、旧Event、Rejected Observation或默认Surface建立承诺
- [x] 3.19 让Tracking的新Rejected Observation保持自身Key和拒绝结果，同时允许既有同Event Accepted Landing继续保留原始lineage，禁止把保留Landing改名成本次查询命中
- [x] 3.20 让同Event首次正式Contact Rising恰好执行一次Current Contact Plant Verification，以该Verified Landing建立LastLanding与唯一Anchor；稳定Plant及同EventReentry期间冻结Anchor且不得再次查询或重定位

## 4. 拆分State、Transition、Interpolation与Post Constraint

- [x] 4.1 对账当前每个Foot State、合法Transition边、Anchor命令、目标Correction、Residual/Progress、完成条件与Post Constraint，固定迁移前业务映射
- [x] 4.2 把根Context拆成离散State、Contact/Anchor、统一Interpolation、Landing与Observation分型数据块，并保持一次Begin、Seal或Discard的唯一根事务
- [x] 4.3 实现纯`CharacterFootTransitionResolver`与固定typed Decision，显式区分输入驱动的Pre-Interpolation边和完成驱动的Post-Interpolation边
- [x] 4.4 实现唯一Transition Runtime，只允许它应用Decision、写离散State、执行Anchor Create/Retain/Release并发布Transition事实
- [x] 4.5 实现纯`CharacterFootStateTargetResolver`，按已确定State生成目标Correction、Reference、Contact/Support/Reach意图与typed Interpolation Request，不推进时间和Context
- [x] 4.6 实现唯一`CharacterFootInterpolationRuntime`，迁移Swing/Acquire/Release Residual、Contact Progress、HalfLife与Effective Correction，只保留一份统一Interpolation State和固定typed Policy
- [x] 4.7 把Ground Path Envelope与Landing Reach放在Interpolation之后执行，禁止Post Constraint回写State Target、Residual或Transition
- [x] 4.8 让Resolved Foot只消费Post-Transition、Post-Interpolation和Post-Constraint结果，并补齐Transition、Target、Interpolation与Constraint逐阶段事实
- [x] 4.9 删除旧`CharacterFootStateMachine`、旧分散Residual/Progress字段、重复Advance方法和全部兼容入口，确认State/Anchor与Effective Correction各自只有一个写入者
- [x] 4.10 把查询后Landing接受距离剥离为独立`LandingAcceptanceDistance`正式配置，保持Corin现行2厘米行为不变
- [x] 4.11 把Landing端点与Swing Target的Path Revision距离剥离为独立`PathRevisionDistance`正式配置，保持Corin现行2厘米行为不变
- [x] 4.12 把Swing截止残差与Release完成距离拆成独立`SwingResidualTolerance`和`ReleaseCompletionTolerance`正式配置，删除旧复用字段并保持Corin现行2厘米行为不变

## 5. 单独接入Step Time与Step Distance

- [ ] 5.1 用正式Step Time替换Landing Prediction时域、Current/Incoming选择和Future Body Translation请求时长
- [ ] 5.2 在Path瞬时Correction链连续后，用正式Step Time、SwingResidualTolerance和基础HalfLife计算统一Interpolation State中Swing政策的Landing截止收敛
- [ ] 5.3 用正式Step Distance与Event table校验RootLocalLanding的同脚相邻事件和水平步长，不改变世界速度或地形查询数学
- [ ] 5.4 删除旧隐藏Step Time/Distance/Event消费者及其Projection字段，不保留双读或fallback
- [ ] 5.5 对账Raw Landing、Future Translation、Landing Event和Surface lineage诊断，阻止事件边界造成水平偏移
- [x] 5.6 在Foot根Bank增加左右脚共享的Prediction Motion State，保存稳定当前/Continuation速度、初始化事实、移动计划Generation、Body Reset与Prediction Source lineage，并随同一事务Seal或Discard
- [x] 5.7 在Foot Motion Profile增加必须显式序列化的`PredictionVelocityDeltaThreshold`、`PredictionVelocitySmoothSpeed`与`PredictionMaximumSpeed`，纳入Profile Revision并严格拒绝缺失、非有限和非正值
- [ ] 5.8 按本项目Replay证明的阈值、EMA与上限控制顺序分别稳定committed Body Target当前世界速度与移动计划Continuation，只把稳定速度交给唯一KCC Future Body Translation；`60/s`在60FPS下等于业务级变化直通，Corin新候选`4/s`必须证明转向时Prediction Landing连续且不增加稳定Swing、Landing或穿透回归；ZZZ主求解的未命名标量响应不得作为世界速度算法证据，不增加移动计划Current替代路径、普通/预测双路径或KCC后位置低通
- [x] 5.9 让Body Reset、Retarget、移动计划Generation与Prediction Source变化重置Prediction Motion State，普通Landing Event、Animation Source、Source Sample与左右脚Step换代不得重置角色级稳定速度
- [x] 5.10 发布Raw/Stable当前与Continuation速度、速度差、EMA响应、最大速度Clamp、Prediction初始化/重置原因、KCC Future Translation与晚期Candidate消费结果诊断
- [x] 5.11 对Prediction输入执行有限值和lineage接纳；非法或缺失移动计划时发布typed unavailable、不得推进稳定状态或生成Future Translation，合法急转只进入同一EMA控制，不套用语义未确认的PIK相对突变公式；停止边界的正式零速度计划生产后续单独处理
- [x] 5.12 让Prediction Motion State与Future Translation Workspace保持根Bank预分配固定布局，热路径不创建Trajectory对象、临时Sample数组或托管集合

## 6. 连续接管Foot Height与Landing/Lock垂直误差

- [x] 6.1 在Foot Motion Profile新增必须显式序列化的`MaximumVerticalCorrectionSpeed`、`GroundPenetrationTolerance`与`LandingLockCompletionTolerance`，纳入Profile Revision并严格拒绝缺失、非有限与非正值；Corin首个候选分别使用`0.6m/s`、`0.01m`与`0.01m`
- [x] 6.2 在3.17持续准备Plant目标、3.20建立Verified Anchor后，删除`AcquireByWeight`进入帧对Contact Anchor的立即`RaiseToMinimum`；保留普通Swing/UnlockedSupport对Accepted Ground Envelope的硬最低约束，确认Effective Correction仍只有唯一Interpolation Owner
- [x] 6.3 在唯一Interpolation内建立同Event持久Plant Target高度历史与单调`PlantBlend`权重，让Approach、Landing与Locked共用同一Policy；该步骤只确认持久槽、目标高度历史和单调权重已经进入唯一Owner，不代表状态切换混合、门控、Correction历史与完成条件已经闭合
- [x] 6.4 让Post Constraint对普通Swing/UnlockedSupport执行Accepted Ground Envelope硬最低约束，对Approach Plant Target和Landing/Locked Contact Anchor只测量穿透并发布容差、追赶与Full Lock门控；继承的超预算Plant误差由同一PlantBlend连续追赶且不得Full Lock，Reach不可达仍可硬夹紧Goal
- [x] 6.5 让Landing只有在正式Lock Weight完成、位置残差不超过`LandingLockCompletionTolerance`、穿透不超过`GroundPenetrationTolerance`且Reach允许时进入Locked；Landing完成Decision延后到双脚/Pelvis Reach求解之后，未满足时保留同Anchor Landing继续接管
- [x] 6.6 用`Runtime Ground Envelope + Formal Foot Height`生成Swing Raw Height，保持Foot XZ来自动画骨骼；唯一Target Height历史保存Accepted Landing沿Up高度，Swing按`Raw Height + Filtered Landing Height - Current Landing Height`输出，正常Phase直接通过，同Event Landing高度有效换代才限速，Plant接管时Swing发布Held并由Plant继续同一历史；记录Raw、History Before、Delta、Applied Delta、Held、Rate Limited、Clamp与Filtered Height
- [x] 6.7 删除由`LandingConstraintWeight`乘`BaselineHeightError`或`FormalTargetCorrection`的旧高度/目标政策、`NextSwingConstraintWeight`状态及对应代码和诊断列
- [ ] 6.8 发布Formal Foot Height、目标高度、限速前后Correction、竖直速率、Envelope/Anchor穿透、Ground Catchup、Full Lock门控和最终Correction诊断事实，删除把同帧抬升描述为Safety Floor成功的旧口径
- [x] 6.9 在Foot Motion Profile新增必须显式序列化的`MaximumVerticalTargetSpeed`，纳入Profile Revision并严格拒绝缺失、非有限与非正值；它只控制同Event Ground Path换代且Landing沿Up有效变化及Approach/Plant目标接管，不限制正常动画Phase，现有`MaximumVerticalCorrectionSpeed`只控制状态混合后的Effective Correction历史，不提供共享默认值
- [ ] 6.10 按ZZZ已确认顺序闭合`当前态到目标态混合 -> 目标高度历史限速 -> typed状态权重混合 -> Correction历史限速 -> 既有Foot Goal权重基准混合`，并用项目正式State、Response、Event与边沿定义各历史的更新、冻结、强制刷新和Reset门；同Event换点、Contact Verification、Lock Response切换与Same-Event Reentry不得跳过状态混合或同时清零两份历史
- [ ] 6.11 删除Correction限速之前或之后重复修改可见Correction的Plant、Ground或Goal混合路径，确认基准混合只由既有Foot Goal/Position Weight执行一次，并把两次限速的输入、历史、输出、Clamp与Reset原因纳入6.8诊断

## 7. 单独接入Support与Pelvis

- [x] 7.1 把正式Support写入Resolved Foot的Support Intent，并建立与Lock分离的Pelvis Reach Reference
- [x] 7.2 让Primary Support按Support Intent、Event lineage和Reach Reference获取/保留，不读取Foot State或Lock Mode
- [x] 7.3 让Pelvis消费正式Support Presence/Share并保持双脚都无Support时的typed Release
- [x] 7.4 删除由旧Lock状态推导Support Weight、Intent和Eligibility的消费者，不把弱单侧Support归一成1
- [ ] 7.5 在Foot Motion Profile增加必须显式序列化的`PelvisMaximumUpVelocity`与`PelvisMaximumDownVelocity`，纳入Profile Revision并拒绝默认值或旧配置补全
- [ ] 7.6 按本项目Landing Reach业务在唯一Critical Spring积分后限制非对称Velocity、再限制Reach Output，并在撞到区间边界时清除继续向外的速度；ZZZ Pelvis字段与上下速度语义未闭合，不作为公式或参数来源
- [ ] 7.7 发布Pelvis原始Target、Spring输入/输出/Velocity、上下速度上限、Reach边界Clamp与向外Velocity清除诊断

## 8. 闭合Landing腿可达

- [x] 8.1 在Foot Motion Profile新增必须显式序列化的米制最小Landing腿压缩余量，纳入Profile revision和严格校验，缺失时typed invalid且不提供默认值
- [x] 8.2 让State Target与Resolved Foot发布Landing Reach Request，包含Event、世界Hip、目标Ankle、腿长与最小压缩余量
- [x] 8.3 让Pelvis Builder求Primary Support腿与Landing腿Reach区间交集，并限制Target与Spring Output
- [x] 8.4 在Reach无交集时保持支撑腿安全、按最小压缩余量夹紧Landing Foot Goal、发布`LandingReachUnavailable`并禁止Full Lock
- [ ] 8.5 发布Target/Solved Extension Ratio、Compression Reserve、Reach区间、交集和Goal夹紧量诊断事实

## 9. 单独接入Contact与Lock

- [x] 9.1 用正式Contact与Lock Mode通过Pre-Interpolation Transition建立同Event唯一Anchor并进入Landing
- [x] 9.2 用正式Lock Weight选择typed接管政策并驱动统一Interpolation State，用Locked/Sliding Mode选择FullAnchor或Sliding State Target；Weight完成不得绕过6.5的位置、穿透与Reach门控
- [x] 9.3 用正式Contact退出、Lock Mode与Weight产生Releasing Transition；同Event合法重入在Pre-Interpolation执行`Releasing -> Landing`，Interpolation Completion只在Post-Interpolation执行`Releasing -> Swing`
- [ ] 9.4 删除旧PlantConfidence、无identity的PlantCycleConsumed布尔和Constraint Weight状态准入消费者及其Projection字段
- [x] 9.5 让Contact Anchor只消费同Event首次Contact Rising产生的Verified Plant Landing；稳定Plant阶段LockDistance或Reach不可用时拒绝Full Lock，不以重复查询移动Anchor
- [ ] 9.6 在同一Foot根Bank增加唯一Contact Transition Context，保存上一正式Lock请求、距最近边沿秒数、最近与最近释放Contact Event identity，不新增Rebound、Blocked或Grounded顶层状态
- [ ] 9.7 让唯一Transition Resolver生成Contact Rising/Falling/Same-Event Reentry Refresh Decision事实，唯一Transition Runtime随根事务更新Context；Pending失败或Discard不得推进边沿历史
- [x] 9.8 Releasing期间同Event合法重入时发布`SameEventContactReentryRefresh`并执行`Releasing -> Landing`，只Retain原Verified Anchor并从当前Effective Correction连续接管，不查询、不Create、不清零Interpolation
- [x] 9.9 Release完成或Anchor清除后阻止旧Event复活；新Event紧接上一边沿时必须执行自己的首次Plant Verification
- [ ] 9.10 发布上一/当前Lock请求、边沿、距边沿秒数、最近/最近释放Event、Reentry Refresh/Unavailable、Retained Anchor与连续接管诊断，确认下游Resolved Foot、Pelvis和Goal不读取内部Context

## 10. 清理、构建与严格校验

- [ ] 10.1 删除全部旧Foot Motion Runtime payload、旧隐藏Feature reader、旧配置字段和失去消费者的诊断列
- [ ] 10.2 使用精确Corin Definition显式重建Presentation Projection、Float32 Program与Fixed Program，不修改TrainingEnemy
- [ ] 10.3 使用规定参数编译Runtime与Editor工程，并在每次构建后立即关闭dotnet build server
- [ ] 10.4 对封口诊断包重新生成facts/diagnosis，对账Raw/Stable Prediction速度、KCC Future Translation、NextSwing Tracking、Approach Plant目标准备、Contact Verification、Contact边沿、同EventReentry Refresh/Unavailable、Transition、Interpolation、竖直限速、Ground穿透与Catchup、Full Lock门控、Path、Envelope、Pelvis速度边界、Landing Reach、Support、Goal、Solved和Physical阶段责任
- [ ] 10.5 执行`git diff --check`、本change严格校验和全量严格OpenSpec校验，清除旧spec冲突和失效任务引用
- [ ] 10.6 按design中的ZZZ P0/P1精确结论与P2/P3边界逐项核对实现，分别记录直接采用、项目输入差异、Replay否决、后续补证和明确不照搬，不把匿名B/D输入、推断名词或未激活实例参数写成正式算法与默认值
- [ ] 10.7 确认新增Prediction、Observation、Landing、Interpolation与Pelvis路径具有固定容量、有限值校验、数组边界、确定性tie-break和typed容量失败，且热路径没有每帧托管分配
- [ ] 10.8 确认不存在独立PIK组件、预测/普通fallback、全局Foot缓存、第二Landing生命周期、第二Interpolation、第二IK、第二Writer、LateUpdate骨骼旁路或常驻Final Pose低通
