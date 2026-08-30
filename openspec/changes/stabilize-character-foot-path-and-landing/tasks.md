## 1. 固定依赖与唯一输入

- [x] 1.1 确认`build-character-foot-motion-data-foundation`已经由用户验收并归档，记录正式Curve Catalog、Artifact format、algorithm version与Corin Registered Curve Hash
- [x] 1.2 固定Foot模块的typed输入、唯一Result与根事务边界，让`refactor-character-pose-graph-architecture`只消费不透明Constraint合同而不规定Foot内部布局
- [x] 1.3 固定Corin范围和TrainingEnemy禁区，确认本change内只有一套Recorder、Analyzer与Publisher

## 2. 发布唯一Foot Motion Runtime Frame

- [x] 2.1 扩展Projection Compiler，从正式AnimationClip Curve组和匹配Artifact事件降低唯一Foot Motion payload与稳定Landing Event table
- [x] 2.2 让选中Live Animation Source按同Contribution、Cycle、Normalized Time和Completion采样左右正式Foot Motion Sample
- [x] 2.3 把唯一typed Foot Motion Frame接入Foot Placement Pose Input，并严格校验Source与Contribution lineage
- [x] 2.4 对缺失、重复、旧binding、Event不一致和非有限值发布typed invalid，不读取旧Artifact或默认值补全
- [x] 2.5 让稳定Landing Event table正式保存PreSwing、Swing、Approach Contact与Landing边界，并由同Source/Cycle/Side/ordinal的Runtime Frame发布`InApproachContactToLanding`供Plant目标准备消费；全接触循环在sample 0生成零Swing提前时间的正式Contact-only Event
- [x] 2.6 由同一Event table非零Approach区间发布归一化`ApproachContactToLandingProgress`，以Approach Contact为0、Landing为1且同Event单调；零时长Approach在Landing前保持Swing与进度0，不得从Contact Curve、Lock Weight、时间容差或运行时累计值重算

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
- [x] 3.21 当旧Current Contact Prepared Target与下一Swing Event同帧并存时，下一Event只更新Prediction/Landing Context/Ground Path，当前State Target选择本帧Current Support且Prepared Target只拥有Post Constraint测量；禁止下一Event进入当前Interpolation、Rotation、Reach Goal或硬最低约束

## 4. 拆分State、Transition、Interpolation与Post Constraint

- [x] 4.1 对账当前每个Foot State、合法Transition边、Anchor命令、目标Correction、Residual/Progress、完成条件与Post Constraint，固定迁移前业务映射
- [x] 4.2 把根Context拆成离散State、Contact/Anchor、统一Interpolation、Landing与Observation分型数据块，并保持一次Begin、Seal或Discard的唯一根事务
- [x] 4.3 实现纯`CharacterFootTransitionResolver`与固定typed Decision，显式区分输入驱动的Pre-Interpolation边和完成驱动的Post-Interpolation边
- [x] 4.4 实现唯一Transition Runtime，只允许它应用Decision、写离散State、执行Anchor Create/Retain/Release并发布Transition事实
- [x] 4.5 实现纯`CharacterFootStateTargetResolver`，按已确定State生成目标Correction、Reference、Contact/Support/Reach意图与typed Interpolation Request，不推进时间和Context
- [x] 4.6 实现唯一`CharacterFootInterpolationRuntime`，迁移Swing/Acquire/Release Residual、旧Contact Progress、HalfLife与Effective Correction，只保留一份统一Interpolation State和固定typed Policy
- [x] 4.7 把Ground Path Envelope与Landing Reach放在Interpolation之后执行，禁止Post Constraint回写State Target、Residual或Transition
- [x] 4.8 让Resolved Foot只消费Post-Transition、Post-Interpolation和Post-Constraint结果，并补齐Transition、Target、Interpolation与Constraint逐阶段事实
- [x] 4.9 删除旧`CharacterFootStateMachine`、旧分散Residual/Progress字段、重复Advance方法和全部兼容入口，确认State/Anchor与Effective Correction各自只有一个写入者
- [x] 4.10 把查询后Landing接受距离剥离为独立`LandingAcceptanceDistance`正式配置，保持Corin现行2厘米行为不变
- [x] 4.11 把Landing端点与Swing Target的Path Revision距离剥离为独立`PathRevisionDistance`正式配置，保持Corin现行2厘米行为不变
- [x] 4.12 把Swing截止残差与Release完成距离拆成独立`SwingResidualTolerance`和`ReleaseCompletionTolerance`正式配置，删除旧复用字段并保持Corin现行2厘米行为不变

## 5. 单独接入Step Time与Step Distance

- [x] 5.1 用正式Step Time替换Landing Prediction时域、Current/Incoming选择和Future Body Translation请求时长
- [x] 5.2 在Path瞬时Correction链连续后，用正式Step Time、SwingResidualTolerance和基础HalfLife计算统一Interpolation State中Swing政策的Landing截止收敛
- [x] 5.3 用正式Step Distance对账同脚相邻Motion-space Landing水平距离，并校验RootLocalLanding的同Event ordinal/sample lineage；循环展开上一周期，有限首段使用素材起点，不改变世界速度或地形查询数学
- [ ] 5.4 删除旧隐藏Step Time/Distance/Event消费者及其Projection字段，不保留双读或fallback
- [x] 5.5 对账Raw Landing、Future Translation、Landing Event和Surface lineage诊断，阻止事件边界造成水平偏移
- [x] 5.6 在Foot根Bank增加左右脚共享的Prediction Motion State，保存稳定当前/Continuation速度、初始化事实、移动计划Generation、Body Reset与Prediction Source lineage，并随同一事务Seal或Discard
- [x] 5.7 在Foot Motion Profile增加必须显式序列化的`PredictionVelocityDeltaThreshold`、`PredictionVelocitySmoothSpeed`与`PredictionMaximumSpeed`，纳入Profile Revision并严格拒绝缺失、非有限和非正值
- [x] 5.8 按本项目Replay证明的阈值、EMA与上限控制顺序分别稳定committed Body Target当前世界速度与移动计划Continuation，只把稳定速度交给唯一KCC Future Body Translation；Corin保持已证明零回归的`60/s`，`4/s`因增加Query与Path Revision已由134944 Replay否决；ZZZ主求解的未命名标量响应不得作为世界速度算法证据，不增加移动计划Current替代路径、普通/预测双路径或KCC后位置低通
- [x] 5.9 让Body Reset、Retarget、移动计划Generation与Prediction Source变化重置Prediction Motion State，普通Landing Event、Animation Source、Source Sample与左右脚Step换代不得重置角色级稳定速度
- [x] 5.10 发布Raw/Stable当前与Continuation速度、速度差、EMA响应、最大速度Clamp、Prediction初始化/重置原因、KCC Future Translation与晚期Candidate消费结果诊断
- [x] 5.11 对Prediction输入执行有限值和lineage接纳；非法或缺失移动计划时发布typed unavailable、不得推进稳定状态或生成Future Translation，合法急转只进入同一EMA控制，不套用语义未确认的PIK相对突变公式；停止边界的正式零速度计划生产后续单独处理
- [x] 5.12 让Prediction Motion State与Future Translation Workspace保持根Bank预分配固定布局，热路径不创建Trajectory对象、临时Sample数组或托管集合

## 6. 连续接管Foot Height与Landing/Lock垂直误差

- [x] 6.1 在Foot Motion Profile新增必须显式序列化的`GroundPenetrationTolerance`与`LandingLockCompletionTolerance`，纳入Profile Revision并严格拒绝缺失、非有限与非正值；Corin首个候选均使用`0.01m`
- [x] 6.2 在3.17持续准备Plant目标、3.20建立Verified Anchor后，删除`AcquireByWeight`进入帧对Contact Anchor的立即`RaiseToMinimum`；保留普通Swing/UnlockedSupport对Accepted Ground Envelope的硬最低约束，确认Effective Correction仍只有唯一Interpolation Owner
- [x] 6.3 在唯一Interpolation内建立同Event持久Prepared Plant Target、上一Selected World Target、上一实际World Output Point与完整Vector Plant World Residual；删除raw Contact累计max与全部Prepared Plant可见混合，Approach Progress只更新准备事实，首次Contact Verification选择Verified Target时才重基Residual并在同帧Advance
- [x] 6.4 让Post Constraint对普通Swing/UnlockedSupport执行Accepted Ground Envelope硬最低约束，对Approach Plant Target和Landing/Locked Contact Anchor只测量穿透并发布容差、追赶与Full Lock门控；继承的超预算Plant误差由同一PlantBlend连续追赶且不得Full Lock，Reach不可达仍可硬夹紧Goal
- [x] 6.5 让Landing只有在正式Lock Weight完成、位置残差不超过`LandingLockCompletionTolerance`、穿透不超过`GroundPenetrationTolerance`且Reach允许时进入Locked；Landing完成Decision延后到双脚/Pelvis Reach求解之后，未满足时保留同Anchor Landing继续接管
- [x] 6.6 用`Runtime Ground Envelope + Formal Foot Height`生成Swing Raw Height，保持Foot XZ来自动画骨骼；唯一Target Height历史保存Accepted Landing沿Up高度，Swing按`Raw Height + Filtered Landing Height - Current Landing Height`输出，正常Phase直接通过，同Event Landing高度有效换代才限速，Plant接管时Swing发布Held并由Plant继续同一历史；记录Raw、History Before、Delta、Applied Delta、Held、Rate Limited、Clamp与Filtered Height
- [x] 6.7 删除由`LandingConstraintWeight`乘`BaselineHeightError`或`FormalTargetCorrection`的旧高度/目标政策、`NextSwingConstraintWeight`状态及对应代码和诊断列
- [x] 6.8 发布Formal Foot Height、Target Height前后与Update Reason、Plant Mixed World Target、World Residual捕获前后与衰减、当时Output Point、Vertical Continuity Owner、Correction Stage Disposition、Effective Correction前后、Envelope/Anchor穿透、Ground Catchup、Full Lock门控和最终Correction诊断事实，删除把同帧抬升描述为Safety Floor成功的旧口径；最新Correction Response事实和旧Disposition替换由6.20完成
- [x] 6.9 在Foot Motion Profile新增必须显式序列化的`MaximumVerticalTargetSpeed`，纳入Profile Revision并严格拒绝缺失、非有限与非正值；它只供6.16显式`RateLimited` Target Height模式控制同Event Landing高度换代，不限制正常动画Phase、不影响`Direct`模式且不提供共享默认值
- [x] 6.10 闭合到Desired Output的`Swing/Current Support Selected Target -> Contact Verification时一次换为Verified Position+SupportNormal -> 捕获完整WorldResidual -> 同帧衰减`，并用Target Kind、Lock Response、Verification、State/Response与Target Revision定义Capture；不得让Formal Approach Progress、raw Contact、Lock Weight或dominant Source Weight改变Position Target。最新ZZZ一手Trace证明后续仍需6.16至6.20的独立Correction Response，不能把本项解释为最终输出已经完成
- [x] 6.11 删除已经被Replay否决的旧单档`MaximumVerticalCorrectionSpeed`、上一世界输出重表达链及全部旧CSV/Analyzer字段；现有World Target加Residual形成Desired Output的职责保留，最新一手Trace证明的独立Correction Response由6.16至6.20在同一Interpolation内正式补回，不能复用本项已删除实现
- [x] 6.12 在Foot Motion Profile新增独立`TargetHeightForceRefreshDistance`并纳入Revision与严格校验；首个候选为`0.30m`。`RateLimited`模式下同Event Current Landing或Plant Target与Filtered Landing历史的累计沿Up差达到该值时强制刷新内部高度，小于该值且超过`PathRevisionDistance`的换代才走`MaximumVerticalTargetSpeed`；`Direct`模式不以该阈值拖延合法目标采用，后级连续性由6.17至6.19承担
- [x] 6.13 把既有Swing到Landing Floor交接、Actual Foot Envelope反事实、Plant Interpolation和表现采样节奏事实接入唯一Analyzer/Publisher正式Target，不执行第二次World Query
- [x] 6.14 为可判定质量Target发布独立Health Score与Evidence Score，保留次数、分母、严重度档位、扣分构成和代表帧；零eligible或纯候选比较发布typed Unavailable。用户后续授权的去重7维浅层加权摘要由`consolidate-foot-diagnostic-scoring`统一定义，不沿用旧文件平均分
- [x] 6.15 把Observation Query Purpose/Refresh Mode、首次Forced Plant Verification例外和Plant Target/Weight/Height/Mixed World Target/Output Point/Vector Residual/Owner/Disposition接入唯一facts/Analyzer/Publisher，升级唯一schema并删除旧WeightChanged与第二Correction限速列
- [x] 6.16 在Foot Motion Profile增加显式`TargetHeightAdoptionMode`、`SupportDirectionMaximumChangeDegrees`、`CorrectionResponseIncreaseSpeed`与`CorrectionResponseDecreaseSpeed`，纳入Profile Revision并严格拒绝缺失、非有限、非正值、超过180度和未知Mode；Corin使用实测`Direct`模式、每次`10°`Direction上限及`1.8m/s`、`1.5m/s`两档，不读取旧`MaximumVerticalCorrectionSpeed=0.6m/s`
- [x] 6.17 在唯一`CharacterFootInterpolationState/Runtime`增加每脚Applied Direction History与标量Correction Response History、Requested/Previous/Applied Direction、角限制事实、初始化事实、增减方向与上一Committed Response Output；保持Target Height、Plant World Residual、Direction History和Correction Response四个typed Owner分离，不新建第二Interpolation或组件
- [ ] 6.18 在6.24提供正式Requested Support Direction后，固定实现`Target Height Adoption(Component Up) -> Selected Position+RequestedDirection Target -> Plant World Residual -> Desired Output -> Direction History -> PoseRoot-Y Correction Response -> Existing Goal Baseline Mix`；Direction History每次最多朝Requested转Profile角度且不重投影Correction scalar，标量再按Desired Response相对Previous Response的增减方向选择速率。删除`BasisTransferred`及世界输出到新Direction scalar投影，只允许正式Position Target Capture执行一次typed Visible Output重基；禁止恢复稳定帧逐帧上一世界输出重表达、无条件全Plant单档限速、Goal后处理或Final Pose低通
- [x] 6.19 让首次合法输入及Reset、Retarget、Source/Profile/World lineage失效后的首次输入同步Correction Response；普通动画目标变化、同Event换点、Contact Verification、Action Pose Contribution、攻击、Lock Response换代和Same-Event Reentry继续上一History，不按动作类型切换路径或清零
- [x] 6.20 升级唯一facts/Analyzer/Publisher，发布Target Height Mode、Desired/Previous/Current Correction Response、增减方向、选中速率、Applied Delta、初始化/重置原因、Desired/Response Output和五类连续Owner，删除“World Residual取代Correction历史”的旧Disposition口径
- [x] 6.21 从同一`FinalAnimationPoseFrame`和Rig Calibration读取每脚Heel/Toe接触点、Foot Rotation、Sole Forward、Component Up与脚掌尺寸，作为Current Support唯一Pose输入；不得读取Foot Motion Toe曲线、另一Source或LateUpdate骨骼
- [x] 6.22 在现有World Query Backend内为Heel/Toe建立固定容量Current Support Observation，复用正式距离、半径、坡度、Layer、自身Collider排除、有限值与World Revision合同，并分别保存Accepted/Rejected/NotExecuted、SphereCastExecuted、Hit Capacity、位置、法线、距离、Surface identity和拒绝原因
- [ ] 6.23 删除022607失败的`OriginalSole + ComponentUp × max displacement`与同一selected SphereCast raw Normal组合；在现有Backend和Rig几何内建立固定容量多点Current Support事务，从明确记录lineage解析一个完整XYZ Foot Target Position和一个Requested Support Direction。查询形状与位置组合必须先形成项目typed合同，不猜ZZZ外部重载、六次常量或裸HitPoint；不得以调低Slope、偏好Up、多法线平均、单点降级、旧结果或默认Up掩盖
- [ ] 6.24 让State Target从Swing Ground/Current Support或Verified Anchor中选择一个Position+RequestedDirection完整Target，显式发布Target Kind以及Position/Direction分型来源lineage；Requested Direction进入唯一Direction History，Applied Direction只由Rig Sole Forward生成同一Foot Goal Rotation，Correction Response位置使用独立PoseRoot basis，按实际Position/Rotation Weight反解Ankle。Target Height继续沿Component Up，Position、Rotation、Applied Direction、Goal Weight、lineage与Writer保持单一。当前Final Component Pose已经完成Pose Graph混合，不得用Approach、Contact、Lock或dominant Source补造第二State Blend；删除或拒绝Toe Goal、Toe Writer、第二Grounder和Pose后Rotation低通
- [ ] 6.25 把Foot/Toe Pose输入、多点Observation、完整XYZ Position与Direction来源、Direction History Requested/Previous/Applied、角上限/实际变化、Correction Response scalar与Rotation Goal接入唯一诊断链；Ground Path Up、Target Height Up、Requested Direction和Applied Direction分列且不得互相fallback，不为诊断执行第二次World Query
- [x] 6.26 在同Event、同Source/Cycle、稳定Ground Path的Swing三帧域发布Contact、PlantBlend、Source/Physical/Offset速度与新增加速度，结构化识别AdvanceToHold、HoldToAdvance和连续推进；旧202551包确认19个AdvanceToHold
- [x] 6.27 把正式`ApproachContactToLandingProgress`、Approach Target Preparation、Selected Target Kind与Lock Weight分列接入唯一Sampler/Analyzer/Publisher，删除PlantBlend、Takeover Weight Delta/Advanced及旧`WeightStarted/WeightCompleted`；强校验Approach Progress变化不改变可见Position/Normal/Residual/Goal权重，首次Contact Verification换代才Capture完整Vector Residual并同帧Advance
- [x] 6.28 删除Action occupancy对Foot Hard Ownership Loss、Anchor Release、Suppress+Reset与Landing Reach的参与；Hard Ownership Loss只保留`!Grounded || !CurrentStep.IsAuthoritative`，`animation.foot-placement-weight`只控制Goal可见权重，Action Pose贡献期间继续同一Interpolation与Reach链

- [ ] 6.31 从CaptureFoot与Goal编码共同使用的实际PoseRoot矩阵捕获不可变PositionResponseBasis，发布单位WorldAxis、未归一化dual HeightProjection和WorldUnitsPerPoseUnit，拒绝非有限、不可逆与退化矩阵；141256候选已因接触回归撤销，保留实验记录，不计为现行实现
- [ ] 6.32 在唯一Interpolation以位置basis处理Desired scalar与正式VisibleOutputTransfer，并沿WorldAxis输出；Support Direction只保留现有角历史和Rotation职责，保持完整XYZ Capture/Decay、权重、Anchor、Goal和两档速率，角配置激进改名且不兼容旧名；候选撤销后本项重新打开
- [ ] 6.33 完成上述坐标候选的唯一Diagnostics迁移与Corin正式产物重建，用既有Replay核对稳定Swing的额外XZ及全包接触/穿透/Reach/骨盆/脚锁；记录身份差异和失败经验，不以局部靶点通过或总分上涨代替完整结果
- [ ] 6.34 用带Frame/Completion/Side/WorldSole与Goal权重的唯一Committed Weighted Goal Sole参考替换旧Visible松散字段，修正把Pending开放标志当历史有效性的读取门；不修改SealFrame事务语义，不把Goal参考叫作Final Physical Sole；150516组合已因持续离面与穿透回归撤销，首次激活证据保留
- [ ] 6.35 正式Capture接入合法上一Goal参考和同一dual重基，公开上一/当前参考与采用事实并迁移唯一Diagnostics；保持完整XYZ Capture、同帧Advance及所有既有质量公式
- [ ] 6.36 使用既有Replay同时对比141256实验前驱与130545原基线，核对Contact穿透/FullAnchor、零权重后Capture、Swing与全部诊断，记录失败或可保留结论而不替换原质量门
- [x] 6.37 在唯一Correction Response内实现LockedSliding完整世界误差域，闭合进入、正常推进、退出到原Plant/Release残差与Reset，不改变五态/目标/旋转/查询/权重
- [x] 6.38 发布分域响应的唯一Runtime事实并迁移Sampler/Analyzer/Publisher，严格区分未执行scalar与WorldError推进，保留原质量规则；9f5b539/facts56完成，后续由6.40替换位置域
- [x] 6.39 用原Record验证Sliding再离地、完整交接、水平轨迹、Heel/Toe穿透、Release、Pelvis与Reach；173423靶点成立但新增穿透及Release大步，拒绝质量通过，原始包与持久Proof保留，后续改同一实验而不视作好基线
- [x] 6.40 将VerifiedSupport统一为ContactWorldResidual单一位置历史，删除Sliding第二世界误差与Contact动画相对scalar消费，保留原XYZ Capture/Decay和退出Release的完整移交
- [ ] 6.41 迁移唯一Diagnostics到ContactWorldResidual分域合同，删除废弃Sliding误差字段，保持原37项质量公式并用原Record记录与03:54历史及近期控制的可比结果

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
- [x] 8.5 发布Target/Solved Extension Ratio、Compression Reserve、Reach区间、交集和Goal夹紧量诊断事实
- [ ] 8.6 在唯一FBBIK中让可靠的本帧动画弯曲向量保留符号，历史只在现有动画退化分支接管；保持Target投影、权重、根Bank、Goal与Vendor算法，发布真实相邻方向dot
- [ ] 8.7 用既有Record对账有符号动画方向候选、Solved Knee与全部Foot质量，单列大腿轴角投影/运输差异、零权重和退化输入，记录保留或拒绝结论

## 9. 单独接入Contact与Lock

- [x] 9.1 用正式Contact与Lock Mode通过Pre-Interpolation Transition建立同Event唯一Anchor并进入Landing
- [x] 9.2 让正式Lock Weight只负责Contact后的Rotation可见响应、Release与完成资格，用Locked/Sliding Mode选择FullAnchor或Sliding State Target；Contact晋升Anchor时Position Target一次换为Verified Anchor，Weight完成不得绕过6.5的位置、穿透与Reach门控；完成资格按Contact Event持久到Event换代或Anchor释放，不要求满权峰值与几何闭合同帧巧合
- [x] 9.3 用正式Contact退出、Lock Mode与Weight产生Releasing Transition；同Event合法重入在Pre-Interpolation执行`Releasing -> Landing`，Interpolation Completion只在Post-Interpolation执行`Releasing -> Swing`
- [ ] 9.4 删除旧PlantConfidence、无identity的PlantCycleConsumed布尔和Constraint Weight状态准入消费者及其Projection字段
- [x] 9.5 让Contact Anchor只消费同Event首次Contact Rising产生的Verified Plant Landing；稳定Plant阶段LockDistance或Reach不可用时拒绝Full Lock，不以重复查询移动Anchor
- [x] 9.6 在同一Foot根Bank增加唯一Contact Transition Context，保存上一正式Lock请求、距最近边沿秒数、最近与最近释放Contact Event identity及同Event Lock Weight完成资格，不新增Rebound、Blocked或Grounded顶层状态
- [x] 9.7 让唯一Transition Resolver生成Contact Rising/Falling/Same-Event Reentry Refresh Decision事实，唯一Transition Runtime随根事务更新Context；Pending失败或Discard不得推进边沿历史
- [x] 9.8 Releasing期间同Event合法重入时发布`SameEventContactReentryRefresh`并执行`Releasing -> Landing`，只Retain原Verified Anchor并从当前Effective Correction连续接管，不查询、不Create、不清零Interpolation
- [x] 9.9 Release完成或Anchor清除后阻止旧Event复活；新Event紧接上一边沿时必须执行自己的首次Plant Verification
- [x] 9.10 发布上一/当前Lock请求、边沿、距边沿秒数、最近/最近释放Event、Reentry Refresh/Unavailable、Retained Anchor完整几何/获取身份与连续接管诊断；Suppress帧同样发布真实Pre/Post Decision和正式FootPlacementWeight，确认下游Resolved Foot、Pelvis和Goal不读取内部Context

## 10. 清理、构建与严格校验

- [ ] 10.1 删除全部旧Foot Motion Runtime payload、旧隐藏Feature reader、旧配置字段和失去消费者的诊断列
- [ ] 10.2 使用精确Corin Definition显式重建Presentation Projection、Float32 Program与Fixed Program，不修改TrainingEnemy
- [ ] 10.3 使用规定参数编译Runtime与Editor工程，并在每次构建后立即关闭dotnet build server
- [ ] 10.4 对封口诊断包重新生成facts/diagnosis，对账Raw/Stable Prediction速度、KCC Future Translation、NextSwing Tracking、Approach Plant目标准备、Contact Verification、Contact边沿、同EventReentry Refresh/Unavailable、Transition、Target Height Component Up、Selected Support Normal/Position Response Basis、Interpolation、Ground穿透与Catchup、Action occupancy/Goal Weight/Hard Ownership Loss、Full Lock门控、Path、Envelope、Pelvis速度边界、Landing Reach、Support、Goal、Solved和Physical阶段责任
- [ ] 10.5 执行`git diff --check`、本change严格校验和全量严格OpenSpec校验，清除旧spec冲突和失效任务引用
- [ ] 10.6 按design中的ZZZ P0/P1精确结论、37个CSV全量复盘、最新Raw、可琳楼梯/攻击Trace与P2/P3边界逐项核对实现，分别记录直接采用、项目输入差异、Replay否决、后续补证和明确不照搬；必须核对`0x60/0x64`方向、`f54`下降沿从属、`f58`比较、`arr230=arr228+arr130×arr128`与`0x278→0x274`的138次`5×dt`响应，不得把匿名B/D/`0x54`/`0x58`/`0x64`/`0x199`输入或`0x274`外部触发猜成正式业务状态、算法开关与默认值
- [ ] 10.7 确认新增Prediction、Observation、Landing、Interpolation与Pelvis路径具有固定容量、有限值校验、数组边界、确定性tie-break和typed容量失败，且热路径没有每帧托管分配
- [ ] 10.8 确认不存在独立PIK组件、预测/普通fallback、全局Foot缓存、第二Landing生命周期、第二Interpolation、第二IK、第二Writer、Action专用Foot链、LateUpdate骨骼旁路或常驻Final Pose低通
