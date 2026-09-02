# Foot 规则迁移清单

## 1. 盘点边界

本清单以`b601d933b^`中的旧 Foot 离线诊断源码为算法事实来源，以当前工作树中的`character-foot-ik`多 Fact Root 声明和真实成员`DiagnosticField`为采样事实来源。它只回答三件事：旧规则实际检查什么、原始证据是什么、当前生成采样能否提供这些证据。

迁移只恢复领域算法语义、阈值、窗口、Coverage、分母和七维评分组合。以下旧类型明确不属于恢复目标：

- `CharacterFootLandingPredictionSampler`及旧领域采样生命周期。
- `CharacterFootCsvColumn`、`CharacterFootCsvBinding`、手写Header和旧CSV Reader。
- `FootFrame`及任何替代它的固定列诊断DTO。
- `CharacterFootDiagnosticStore`、旧Publisher、Query索引和第二Capture manifest。
- 旧`CharacterFootMotionDiagnosticAnalyzer`单体类本身。

历史源码定位：

| 领域 | `b601d933b^`源码 |
| --- | --- |
| 事件构造与派生事实 | `CharacterFootMotionDiagnosticAnalyzer.cs` |
| 穿透 | `CharacterFootContactPlanePenetration.cs`、`CharacterFootContactPlanePenetrationDiagnosis.cs` |
| 锁脚 | `CharacterFootLockedSoleMotionDiagnosis.cs` |
| Landing路径 | `CharacterFootLandingPathContinuityDiagnosis.cs`、`CharacterFootPathStageDiagnosis.cs` |
| Landing状态 | `CharacterFootLandingStateConsistencyDiagnosis.cs` |
| Swing抖动 | `CharacterFootSwingPathJitterDiagnosis.cs` |
| Step Time | `CharacterFootStepTimeCandidateSelectionDiagnosis.cs` |
| Leg／Pelvis／Reach | `CharacterFootLandingLegExtensionDiagnosis.cs` |
| Coverage、分母与评分 | `CharacterFootDiagnosisInfrastructure.cs` |

## 2. 当前证据分类

本清单使用以下状态：

- **可直接绑定**：当前真实成员已经有`DiagnosticField`，Full Sampler现有字段组能够选中；新Plan可以直接绑定Schema字段。
- **可离线派生**：当前Schema已经有全部原始值，差值、速度、窗口、事件段、clearance、比例或统计必须在Operator内计算，不补Player派生字段。
- **缺失原始事实**：真实运行事实存在，但当前成员没有`DiagnosticField`、不在Fact Root中，或只有不足以证明旧规则语义的近似值。实施时必须由唯一Owner补真实成员采样，不能绑定相似字段代替。

当前直接证据主要来自：

- `foot`：Motion Core、Lifecycle、Path Continuity、Output Stages、Correction Response、Landing Observation、Ground Path、Current Support和Resolved Foot。
- `formal-input`／`formal-output`：Foot Height、Contact、Lock Mode／Weight、Support、Time To Landing等已标成员。
- `input`：Presentation Delta、Grounded、Action Foot Weight、Body Tick和Body／Prediction Motion已标成员。
- `pelvis-goal`、`primary-support`和`stride`中已经标记的Core、Height Target及Spring输入。
- `ground-contacts`、`ground-envelope`和`ground-surfaces`三张真实子表。

当前确定缺口：

| 缺失事实族 | 当前原因 | 唯一业务Owner |
| --- | --- | --- |
| 最终Physical Ankle世界位姿 | 已由Final Pose Publication按target interest冻结为`physical` Fact Root，包含write availability、Ankle世界位置与旋转；Heel／Toe必须用Source Ankle到Probe的局部偏移在Operator离线刚体重建 | Final Pose Publication／Physical Writer |
| 最终Physical Ankle组件位姿、Pelvis世界／组件位置、Physical Write Completion | 真实成员仍在`AnimationPhysicalBoneWriteDiagnostics`，后续需要它们的Operator再由同一Owner补Key，不复制进Foot DTO | Final Pose Publication／Physical Writer |
| Pose Root世界位置与旋转 | `CharacterFootIkPhysicalCapture`已有真实值，但未进入Foot Capability | Final Pose Publication |
| FBBIK Effector目标、Solved组件位姿、Residual与可用性 | `CharacterFullBodyIkEffectorDiagnostics`是Fact Root但成员无`DiagnosticField` | Final IK Solver Diagnostics |
| Leg原始／目标／Solved Hip-Knee-Ankle、Bend、Extension、Compression Reserve和稳定方向 | `CharacterFullBodyIkLimbDiagnostics`与嵌套LegPose是Fact Root但成员无`DiagnosticField` | Final IK Solver Diagnostics |
| Solver完成／失败／迭代／Pelvis Pre-Solve等 | `CharacterFullBodyIkSolverDiagnostics`是Fact Root但成员无`DiagnosticField` | Final IK Solver Diagnostics |
| Formal Source identity／cycle／completion、normalized time、contribution continuity | 真实`AnimationFootMotionRuntimeSample`成员存在，但当前只标运动值和部分Event值 | Animation Pose Source／Foot Observation |
| Step候选`SourceLandingCycleOffset`及完整Formal候选身份 | 当前`CharacterFootStepCandidateDiagnostics`没有完整暴露并标记旧规则所需字段 | Foot Step Selection |
| Source Ankle位置 | `CharacterFootLandingPredictionFootDiagnostics.SourceAnklePosition`已有值但未标记 | Foot Landing Prediction |
| Pelvis Posture、双腿Reach交集、Spring当前Output／Target／Velocity、same-level限速事实 | `CharacterFootStrideHipsDiagnostics`已有只读投影，但对应成员没有完整字段标记 | Foot Stride／Pelvis Resolver |

## 3. 固定阈值、窗口和统计合同

这些值来自旧算法，迁移时先原样形成默认Foot Plan参数；它们以后可以由当前Plan修改，但不得在迁移时无证据改写。

| 合同 | 原值 |
| --- | ---: |
| 几何数值epsilon | 0.00001 m（穿透责任分类）／0.0001 m（运行几何与Path clearance） |
| 位置噪声下限／Contact touch tolerance | 0.001 m |
| 一般穿透、锁脚水平漂移、Contact gap阈值 | 0.01 m |
| 锁脚向下excursion证据阈值 | 0.005 m |
| Swing／Contact／Plant可见输出主跳变阈值 | 0.02 m |
| Release flyback correction excursion | 0.01 m且速度方向反转次数大于0 |
| Landing leg target extension ratio增量 | 0.02 |
| Landing solved bend下降 | 5° |
| Contact持续未贴合窗口 | gap大于0.01 m持续至少0.1 s |
| Contact短暂大间隙 | 持续不足0.1 s但峰值大于0.1 m |
| Stable Swing cadence Hold／Advance | 小于0.005 m／大于0.02 m的三帧切换 |
| Step Time差异观察 | 大于0.001 s |
| Same-level双脚高度差及Pelvis向下步长 | 高度差不超过0.01 m；向下单帧超过0.01 m |
| Same-level预期最大向下速度 | 0.6 m/s |
| Landing Reach候选压缩储备 | 0.02 m |
| 低表现采样分类 | `deltaSeconds >= 1/30 s` |
| Swing速度异常观察 | 5 m/s |
| 每个诊断上下文代表事件上限 | 5 |
| Step Time每个原因候选上限 | 5，最终仍受全局代表事件上限5约束 |
| Health最小eligible分母 | 10 |
| 满Evidence分母 | 50 |
| 米制严重度档 | 0.01、0.02、0.05、0.10、0.20、0.30 m |
| 严重度罚权 | 0、0.1、0.35、0.7、0.85、0.95、1 |

连续窗口必须满足同Side、相邻Frame、相同Program／Projection／PoseGraph／Profile运行身份且没有Body Reset或Source断裂。旧代码用Presentation Delta累计时长；新Operator必须绑定当前显式时间字段，不能把Simulation Tick等同于Presentation Frame。

## 4. 逐规则迁移

### 4.1 Contact Plane Penetration

| Target | 旧规则、窗口与分母 | 原始事实依赖 | 当前分类 |
| --- | --- | --- | --- |
| `final-contact-plane-penetration` | 同一Contact Plane连续段的最终Heel／Toe最大深度大于0.01 m；每段只计一次；同时统计时间积分、穿透帧率和脚长覆盖系数 | Source Ankle位姿与Heel／Toe、最终Physical Ankle位姿、Contact Anchor Point／Normal、Event／Surface lineage、Contact状态、delta time | 当前Plan已完整绑定；Operator先离线刚体重建最终Heel／Toe，再计算clearance、深度、积分和段聚合 |
| `contact-plane-penetration-contribution` | 分类Source残留、Foot引入、放大、部分消解、完全消解或Baseline Residual；只作责任证据，不与最终穿透重复扣分 | 与上一项相同，逐probe比较`sourceDepth`和`finalDepth` | 当前Plan已完整绑定并复用同一刚体重建，不再受Physical probe缺口阻塞 |

Heel／Toe clearance必须按`dot(probe - anchorPoint, normalizedAnchorNormal)`计算。不能用`PlantPenetrationDepth`、`CurrentSupport.RequiredDisplacement`或单个Sole点近似整脚穿透。

### 4.2 Locked Sole Motion

| Target | 旧规则、窗口与分母 | 原始事实依赖 | 当前分类 |
| --- | --- | --- | --- |
| `locked-horizontal-drift` | 连续FullAnchor段，只统计Physical Anchor可用且Anchor稳定的段；最终Physical Sole相对Anchor的水平距离最大值大于0.01 m | Constraint State、Lock Response、Event／Surface、Anchor Point／Normal、Source与Physical Ankle位姿、Source Heel／Toe、Corrected Sole、delta time | 当前Plan已完整绑定；Operator离线重建Physical Heel／Toe／Sole并计算水平投影和段最大值 |
| `locked-vertical-anchor-evidence` | FullAnchor与Sliding段的Sole向下excursion大于0.005 m；只作证据，最终穿透由统一穿透规则计分 | 同上，加Lock Response分类和Anchor轨迹 | 当前Plan已完整绑定；分类、刚体重建和excursion都在Operator离线完成 |

Sliding允许水平移动，不能进入FullAnchor水平漂移分母。Anchor变化、Event变化或Surface变化必须切段。

### 4.3 Landing Path Continuity

| Target | 旧规则 | 原始事实依赖 | 当前分类 |
| --- | --- | --- | --- |
| `identity-only-residual-rebuild` | 只有Ground Path输入identity变化时不得重建Swing Residual | Path前后availability、Landing Event、Ground Path Input identity、Revision Reason、Residual Rebuilt、前后Target Correction | 可直接绑定；identity-only变化与correction step离线派生 |
| `path-revision-contract-mismatch` | `pathRevisionExpected`必须等于`pathResidualRebuilt`且Reason必须匹配 | Path availability、Landing Point／Target delta、Revision Distance／Reason、Tracking Applied | 可直接绑定并离线派生expected／reason match |
| `releasing-to-swing-envelope-violation` | Releasing完成进入Swing同帧的Safety Floor clearance after不得小于-0.0001 m | Output Stage状态边界、Safety Floor before／after／clamp、Final Correction | 可直接绑定 |
| `residual-deadline-miss` | Time To Landing到截止点时Residual After Decay不得超过Tolerance加0.0001 m | Residual before／after、Tolerance、Time To Landing、base／deadline／applied half-life | 可直接绑定；向量模和deadline判定离线派生 |
| `residual-growth-without-revision` | 没有Path Revision时Residual不得增长 | Residual before／after、Rebuilt、applied half-life | 可直接绑定并离线派生 |
| `late-approach-landing-revision` | 同Event进入Approach Contact后，Consumed NextSwing Landing不得跨Surface或移动超过Landing Acceptance Distance | Formal／Selected阶段、Observed与Consumed Event／Surface／Point、Acceptance Distance、Final Correction、Physical Ankle／Sole前后步长 | 规则主判定所需Landing、Ground Path和阶段可直接绑定；delta与Physical步长可离线派生，Operator尚未迁移 |

旧Path Stage attribution还会按`State Target -> Interpolation -> Safety Floor -> Final Effective Correction -> Goal Target -> Final Physical`寻找首个放大阶段，噪声下限0.001 m。Final Physical Ankle与Foot probes现可离线重建；完整归因仍需逐Stage绑定当前Schema。

### 4.4 Landing State Consistency

| Target | 旧规则 | 当前证据与结论 |
| --- | --- | --- |
| `missed-landing-entry` | Formal落地边界时既未Landing也未Locked | Formal Event、Constraint State和correction/final sole step可直接绑定；边界离线派生 |
| `early-landing-entry` | Landing入口没有对应Formal落地边界 | 同上，可直接绑定并离线分段 |
| `landing-without-contact-plane` | Landing状态段并非全程有同Event Contact Plane | Lifecycle、Contact Plane availability／Event／Surface可直接绑定 |
| `landing-not-closing` | 多帧Landing段的Corrected Sole到Anchor closure小于等于0 | Corrected Sole与Anchor可直接绑定；距离和closure离线派生 |
| `landing-wrong-exit` | 连续Landing退出既未进入Locked也未进入Releasing | Constraint State／Event可直接绑定 |
| `landing-exit-jump` | 退出额外输出步长大于0.01 m | Source／Corrected／Final Sole与Interpolation可直接绑定，Physical Ankle／Heel／Toe可用相同刚体重建补充 |
| `landing-persists-after-formal-unlock` | Landing段内出现Formal Unlocked | Formal Lock Mode与Constraint State可直接绑定 |
| `release-flyback` | correction excursion大于0.01 m且速度方向反转 | Final Effective Correction、Frame和delta可直接绑定；excursion与反转次数离线派生 |
| `swing-to-landing-floor-handoff` | Swing进入Landing额外输出步长大于0.02 m | Floor owner／clamp、Residual、Formal Height、Progress、Time To Landing和Final Sole可直接绑定；Physical Ankle／Sole可离线补充 |
| `plant-interpolation-output-jump` | 连续Plant帧Foot Placement output offset step大于0.02 m | Plant target、Desired／Response、Residual、Correction Response、Final Correction、delta可直接绑定；步长／速度离线派生 |
| `contact-acquisition-continuity` | 非Idle接触建锚首帧，previous visible到final output额外步长大于0.02 m | Lifecycle acquisition、Anchor、Original／Final Sole、Response和Residual可直接绑定；Physical补充距离可离线重建 |
| `lock-weight-completion-by-contact-event` | 满Lock Weight后在窗口内必须几何闭合并进入Locked；未满权Event不得进入Locked | Formal Lock Weight、Current Event、Plant latch、Output Distance、Penetration、completion tolerance、Landing Reach、transition reason | 大部分可直接绑定；窗口和完成资格离线派生；完整Landing Reach availability依赖未标Leg／Reach事实 |
| `approach-progress-ownership` | Approach progress单调，且同Event不得提前Plant interpolation、Residual capture或可见位置接管 | Formal阶段／progress、Plant／Residual、Final Correction、Motion／Formal权重、Goal权重 | 可直接绑定并离线派生 |
| `action-hard-ownership` | Action Foot Weight不得改变Hard Ownership Loss；所有权只由Grounded和Current Step Authority决定 | Grounded、Action identity／weight、Current Step authority、Hard Ownership Loss、Formal／Motion／Resolved weight | 可直接绑定 |
| `contact-transition-context` | Previous／Current Lock request、Contact edge seconds、Event历史和Verified Anchor必须与上一Committed帧一致 | Lifecycle Previous／Current快照、Frame连续性 | 可直接绑定并离线对账 |
| `formal-goal-weight-policy` | Ready目标位置权重必须等于Formal Foot Placement Weight；Unavailable／Suppress为零；最终Goal与Contact／Lock旋转政策一致 | Formal／Motion／Resolved／Final Goal权重、Outcome、Suppress、Contact状态 | 可直接绑定 |
| `contact-reentry-output-geometry` | 同Event重入时展示previous response、Residual捕获衰减、Desired、Response和Final Sole的真实移动 | Lifecycle Reentry、Response、Residual、Desired、Final Sole | 可直接绑定并离线派生；Informational |
| `contact-support-gap` | 满位置权重Contact episode：FullAnchor／Sliding gap大于0.01 m，或Landing gap大于0.01 m持续0.1 s；按episode计一次 | 最终Physical Heel／Toe／Sole、Anchor Plane、Constraint／Lock状态、Formal／Goal position weight、delta | 状态／权重／Anchor与Physical Ankle已可绑定；gap、积分和episode可离线派生，Operator尚未迁移 |
| `full-anchor-contact-gap` | FullAnchor同政策段Whole Foot Gap大于0.01 m | 同上一项 | Physical Probe已闭合，待迁移Operator |
| `sliding-contact-gap` | Sliding同政策段Whole Foot Gap大于0.01 m | 同上一项 | Physical Probe已闭合；实现时不得把合法水平滑动算作此规则 |
| `landing-contact-convergence` | Landing gap持续大于0.01 m至少0.1 s | 同上一项 | Physical Probe已闭合，待迁移Operator |
| `contact-release-separation` | Release gap仅作退出证据，不参与Health | 同上一项 | Physical Probe已闭合，待迁移Operator |
| `contact-transient-large-gap` | 不足0.1 s的Contact gap段峰值大于0.1 m | 同上一项 | Physical Probe已闭合，待迁移Informational Operator |
| `contact-state-output-jump` | 涉及Landing／Locked／Releasing连续帧，不能由正常动画移动解释的最终Physical脚额外步长大于0.02 m | Source与最终Physical Ankle／Heel／Toe、Final Sole、Constraint State、delta | Source Ankle／Heel／Toe与Physical Ankle现已可直接绑定，Heel／Toe离线重建；完整Operator尚未迁移 |

Contact gap分母只包含measurement applicable且reference available的事实；满位置权重才进入Health。缺失Anchor／Physical事实必须输出`MissingEvidence`，不能归为`NotApplicable`。

### 4.5 Swing Path Jitter

| Target | 旧规则、窗口 | 原始事实依赖 | 当前分类 |
| --- | --- | --- | --- |
| `stable-swing-output-jump` | 同Source／Cycle／Event、Accepted、Unanchored、Stable Path连续Swing帧对，Foot Placement相对动画新增的最终Physical输出步长大于0.02 m | Source与Physical Ankle／Heel／Toe、Frame／delta、State／Event／Path、Final Correction | Source与Physical Probe现已闭合并可离线重建；仍待迁移Operator本身 |
| `path-revision-output-jump` | 相同连续资格，但Path Revision当帧额外输出步长大于0.02 m | 上述事实，加Path delta／reason／residual／stage输出 | Physical Probe不再缺失，其余Stage归因按当前Schema继续对账 |
| `path-revision-amplification-evidence` | 同一Revision样本寻找首个放大Stage；Informational，不重复计分 | State Target、Interpolation、Safety Floor、Final Correction、Goal、Physical output | Final Physical现可重建；其余可直接绑定 |
| `swing-actual-foot-envelope-counterfactual` | 实际脚水平位置在corridor内有唯一Envelope候选，且相对Builder target需要提前抬升大于0.02 m | Ground Envelope子表、Ground Path轴、实际Physical脚位置、Builder target、Component Up | 子表／Path／Builder目标与Physical Foot均可绑定或离线派生；Operator尚未迁移 |
| `stable-swing-correction-response-cadence` | 同Source／Cycle／Event／Path连续三帧，在小于0.005 m Hold与大于0.02 m Advance间切换 | Desired／Response、Final Correction、Formal Height、Envelope sample、Original Sole、Observation状态、delta | 可直接绑定并离线派生；Informational |

速度、加速度、jerk、低采样率分类和Body Tick span都是离线派生。不得把它们重新写入Player Schema。

### 4.6 Step Time Candidate Selection

该规则是Informational，不参与Health。它记录Formal Step Time与Current／Incoming候选的选择事实，统计：

- observation、formal available、current eligible、incoming eligible分母。
- selected source与formal closer candidate分类。
- Formal到Current／Incoming／Selected的绝对时间差分布。
- Normalized Time wrap、Selected Source变化、Selected Landing Event变化，以及Formal到Selected差异大于0.001 s的代表事件。

Current／Incoming的authority、阶段、event identity、time、root-local landing和最大预测时间大部分已可直接绑定；候选eligibility与时间差可离线派生。Formal Source identity／cycle／completion／normalized time／contribution continuity以及候选`SourceLandingCycleOffset`缺失，Owner分别为Animation Pose Source／Foot Observation和Foot Step Selection。未补齐前该Operator只能`MissingEvidence`，不能只输出部分统计冒充完整迁移。

### 4.7 Landing Leg、Pelvis与Reach

| Target | 旧规则、窗口 | 原始事实依赖 | 当前分类 |
| --- | --- | --- | --- |
| `landing-leg-extension` | Runtime Landing同Event段相对入口前一帧：Target Extension Ratio增量大于0.02、Solved Bend下降大于5°或Bend Direction反转 | Leg原始／目标／Solved Hip-Knee-Ankle、Bend Degrees、Extension Ratio、Compression Reserve、稳定方向dot；Landing状态／Event；Pelvis goal与Reach区间 | Landing状态和Pelvis Goal可直接绑定；全部Leg Pose字段未标记，Owner为Final IK Solver Diagnostics；Reach细节缺失，Owner为Foot Stride／Pelvis Resolver |
| `same-level-feet-pelvis-descent` | 双脚最终Physical高度差不超过0.01 m时，Physical Pelvis单帧下降大于0.01 m；分类same-level限速、共同高度请求、Release、Response History、Animation／Root Motion | 左右Physical脚、Physical／Animated Pelvis、Pose Root、Pelvis Final Goal与Spring Response、Height Target、Posture、Reach交集、delta | 左右Physical脚已可重建；Physical Pelvis、Pose Root及完整Response／Posture／Reach仍缺失，由Final Pose Publication和Foot Stride／Pelvis Resolver分别补齐；Informational |

Landing Reach报告还保留0.02 m候选压缩储备、可用性／分类分母、Minimum Correction、Original／Target／Solved Extension和Compression Reserve分布，以及与Pelvis Reach交集冲突证据。它不应恢复成Player派生DTO；全部由Leg、Pelvis、Goal及当前状态原始字段在离线Operator中计算。

## 5. Coverage、分母和七维评分

### 5.1 目标级Coverage

- `eligibleEventCount`由满足规则适用域的连续帧、帧对、三帧窗口、Contact policy segment或event episode产生。
- `matchedEventCount`只计算证据完整且违反规则的eligible事件。
- `matchedEventRate = matched / eligible`；eligible为0时Health不可用。
- 任一必需事实缺失时必须进入`MissingEvidence`，不得减少分母后继续给Health。
- Health至少需要10个eligible事件；10至49仍可给Health，但Evidence不足100；50个及以上才达到完整样本Evidence。
- Path Stage需要`availableEventCount == eligibleEventCount`才允许对应Health。
- Evidence分数为`100 * min(1, eligible / 50) * stageCoverage`。

### 5.2 严重度Health

带米制occurrence的Target按互斥严重度档统计。每档事件率乘以对应罚权，`health = 100 * (1 - burden)`，结果按一位小数四舍五入。无occurrence的Health规则使用违反率，`health = 100 * (1 - matched / eligible)`。

Health评级：

- `Stable`：大于等于90。
- `Attention`：大于等于75且小于90。
- `Degraded`：大于等于50且小于75。
- `Severe`：小于50。

Evidence评级：大于等于90为`Strong`，大于等于60为`Moderate`，其余为`Limited`。

### 5.3 七维评分

| 维度 | 权重 | 唯一Health Target | 当前状态 |
| --- | ---: | --- | --- |
| 下陷／穿透 | 0.20 | `final-contact-plane-penetration` | 已闭合当前Operator与Plan |
| 接触未贴合 | 0.20 | `contact-support-gap` | 原始Physical证据已闭合，Operator待迁移 |
| 普通Swing平顺度 | 0.15 | `stable-swing-output-jump` | 原始Physical证据已闭合，Operator待迁移 |
| Path变化连续性 | 0.15 | `path-revision-output-jump` | 原始Physical证据已闭合，完整stage归因待迁移 |
| 接触状态交接 | 0.15 | `contact-state-output-jump` | 原始Physical证据已闭合，Operator待迁移 |
| 腿部姿态／可达性 | 0.10 | `landing-leg-extension` | 缺Leg Pose与Reach事实 |
| 锁脚水平稳定性 | 0.05 | `locked-horizontal-drift` | 已闭合当前Operator与Plan |

权重总和固定为1。缺失维度不补0或100，也不重分配权重；只报告已知加权贡献、可用权重，以及未知维度按0／100形成的最低／最高可能分。只有七维全部有Health时才发布`totalScore`。任何`MissingEvidence`必须令相关维度和总分不完整。

## 6. 实施顺序与Owner闭合

1. Final Pose Publication已先暴露Physical Write availability与每脚Physical Ankle世界位姿，Foot Source Ankle／Heel／Toe也已补Key；穿透与锁脚Operator已闭合，其余Contact／Swing规则复用相同离线刚体重建。Pose Root、Pelvis和组件位姿按后续Operator的真实需要再补。
2. Final IK Solver Diagnostics标记Effector、Leg Pose和Solver现有成员；这是Leg／Reach及Solved证据前置。
3. Animation Pose Source／Foot Observation补Formal identity、cycle、completion、normalized time和continuity；Foot Step Selection补候选cycle offset。
4. Foot Stride／Pelvis Resolver标记现有Posture、Reach与完整Spring结果；Foot Landing Prediction补Source Ankle Position。
5. 重新生成Full Schema后逐条编译Plan绑定；只有本表列出的必需输入全部存在，Operator才从`MissingEvidence`转为可执行。
6. 先迁纯离线Operator，再恢复当前Core／Full Plan和七维评分组合；不恢复任何旧采样、列绑定、DTO、Store或Publisher路径。

当前结论是：旧算法大部分中间量可以从当前事实离线派生，但七维Health的七个维度目前全部至少有一个真实原始证据缺口。只恢复规则代码而不补Owner字段，会生成看似完整但实际缺证据的错误报告。
