# 设计：有效Foot目标到共同Pelvis需求

## 已批准调用链

`原动画Pose -> 双脚唯一Resolved目标 -> 共同Pelvis期望 -> 统一Reach边界与一次响应 -> 唯一GoalSet/FBBIK`

只替换三个明确业务决定，不恢复212054卸载，不调整已认可的Foot位置、旋转或查询算法。

## 第1步：位置目标有效性与作者权重分离

State Target、Support Target、有限Sole和现有Sole→Ankle/Rotation解析负责目标合法性。只有解析成功才生成Ready Resolved Foot；Unavailable和Suppress继续发布零权重。

Ready时PositionWeight取frame.FootPlacementWeight。Correction=0表示本帧目标恰好与动画Sole相同，不表示放弃这份位置约束。Swing每帧仍使用新目标，不保存世界锁；正式作者权重0仍关闭可见约束。保留现有极小作者权重的编码数值边界，本步不修改小分母保护或RotationWeight=FormalWeight×LockWeight。

Runtime不新增字段；已有FormalWeight、ResolvedOutcome、SupportTarget、三层PositionWeight和最终Physical事实足够验证。Diagnostics升级facts59/diagnosis28，不复用历史失败facts58，不向旧包补值。

## 第2步：共同高度需求

输入为同一根事务的左右原动画Sole、左右Resolved有效目标Sole和有限单位Component Up。沿Up取高度后计算：

`requestedOffset = min(targetLeftHeight,targetRightHeight) - min(animatedLeftHeight,animatedRightHeight)`。

Resolved有效目标与现有Goal作者权重语义一致：作者0不让不可见修正偷偷影响Pelvis，不读取最终Physical Pose回推。公式允许负值，不保留旧max(0,...)或地形相对高度加项。Stride/Primary Support生产资格本步保持，几何与换代事实可保留，但旧目标字段必须按新含义删除/改名，不伪装成仍消费旧公式。

本步只替换目标生产，保留响应/Reach以便独立比较。下一步才改变硬边界职责。

Runtime用不可变`CharacterFootPelvisHeightTarget`保存本次真正消费的Component Up、左右动画Sole、左右按正式Goal权重解析的目标Sole、两份最低高度和有符号请求。输入仍来自本帧Resolved→既有Goal编码/基线混合，不来自最后Physical Pose；保留这一输入路径也保持作者0及极小权重的原编码语义。`HeightTargetAvailable=false`明确表示该帧未消费高度公式，零字段仅为无测量占位；后续Reach不能反写这些输入。旧RawPelvisDelta、RootRelativeGroundTargetAlongUp、SoleClearanceLiftAlongUp和UnclampedSpringTarget命名删除，以HeightTarget与RequestedOffsetAlongUp表达真实用途。只被旧下降地形公式使用的Stride私有SwingTimeToLanding传递同时删除，正式Foot Motion时钟不动。

## 第3步：一次处理可达性与响应

同帧typed Reach Request提供Hip、有效Ankle目标、真实腿长、正式安全余量和lineage。所有实际参与的腿先形成唯一硬区间，进入现有Pelvis模块后一次用于目标和响应合法性。原动画弯曲余量可形成目标偏好，但不再缩小另一份最终输出硬区间。

保留一份根Bank内的Spring状态、原频率与Handoff/Velocity Reset业务。不增加第二响应或新速率参数。最终输出若因几何必须触界，统一阶段记录夹紧并清除继续向外的速度；之后Module只消费结果，不再次改写Pelvis输出。无交集、横向本已不可达等情况继续使用明确typed拒绝及既有Foot Reach保护，不以FBBIK伸直或降低未授权权重掩盖。

此结构不能保证所有几何冲突都无突降；它先减少不必要目标上抬与动画姿态偏好造成的硬压，再用Replay评估剩余真实约束。

### 第3步的实际输入、数学和事实

Module只把原有IsLandingReachCandidate准入结果、Resolved的左右typed Reach Request、Primary Support、原Pose和第2步有效目标交给一次ResolvePelvis；不再调用ApplyLandingReach或另写Spring Output。Foot FinalizeLanding、不可达Goal夹紧、唯一Goal编码和FBBIK顺序保持。

每腿硬半径严格为`LegLength-MinimumCompressionReserve`，不沿用旧辅助函数按水平距离放宽安全余量的做法。令`v=Hip-TargetAnkle`、`y=dot(v,up)`、`h2=|v-up*y|²`，若`radius²-h2<0`则为HorizontalUnreachable，否则沿Up的合法平移区间为`[-y-sqrt(radius²-h2), -y+sqrt(radius²-h2)]`。Primary只在原Accepted资格且Goal有效时参与；有同侧正式Foot Request则复用其几何，无第二份同腿硬区间。

Reach Role可同时为FootTarget与PrimarySupport，后者只标该腿的优先角色，不改写输入来源。复用Foot Request时EventIdentity属于该Request，公共Primary Event独立保留；两者不能强制同值。只有独立Primary输入才以其正式Event构造几何，不能把它反报成Foot Motion已请求的Landing Reach。

所有已请求腿可达且交集非空时选择AllRequestedLegs；否则明确发布LegUnreachable或NoCommonInterval，在原Primary存在且硬几何合法时选择PrimarySupportOnly。不存在合法Primary时不造可达区间；相关Foot Request继续Unavailable并走原Goal Reach保护。它是既有支撑优先政策的显式裁决，不是默认点/旧缓存或另一响应。正常交集Available后还核对实际加权Pelvis位移，不能只检查未乘权重的数值。

原动画压缩量仍为`LegLength-distance(AnimatedHip,AnimatedAnkle)`，原姿态区间计算只生成PosturePreference目标。它不可达时发布Evaluated=true/Available=false，共同HeightTarget仍是独立合法需求；不产生另一份输出夹紧。未消费姿态偏好的Release/Rejected帧为Evaluated=false，零字段不是测量。

唯一AdvancePelvisResponse先把preferred target限制到选中的硬边界，在原Handoff条件下保留或清除背离目标的旧速度，再按原频率执行一次Critical Spring，最后只对硬边界夹紧一次并清除朝外速度。区间先按正式Pelvis PositionWeight换算到Spring的未加权标量域；真正应用的位移仍为Output×PositionWeight。必须输出的安全平移不受5毫米显示门掩盖；非安全必需的小量仍保留原可见门，Release完成仍按既有GeometryEpsilon吸零。

Runtime发布完整左右Reach角色/输入/区间、公共交集与选择、PosturePreference实际输入、一次响应前后和硬夹紧事实。旧SupportReach字段与WithLandingReachOutput命名删除，不以旧字段代表新硬边界。ResponseEvaluated=false只表示本帧无需推进Spring，已计算Reach不因此变成无效；对未运行公式或响应不伪造零测量。非单位作者权重、横向不可达、无交集和无上一Spring等边界需按实际Replay覆盖报告，不由本轮全权重样本冒称全覆盖。

## 不变项

- Contact完整世界残差、capture同帧Advance、完成容差和Anchor不变。
- Swing动画XZ、FootHeight、Ground Path、Correction Response及既有旋转政策不变。
- 既有GoalSet/FBBIK、Rig、曲线、世界查询与Gameplay Body不变。
- 不添加未来Pose预测、卸载时钟、Pose后低通、默认地面或第二解释链。

## 验证顺序与失败界限

第1步覆盖L339/L515/R611及原193957的实际零权重/近零修正帧，证明目标持续有效但未锁住Swing；同时核对Goal新增覆盖造成的真实Physical变化。

第2步核对两份最低高度、signed requestedOffset、被替换字段和322/466/675及全包骨盆大步。第3步再核对每腿硬区间、偏好目标、响应前后、唯一夹紧与不相交路径。

每步保持原37项质量规则，只有正式合同随API改变；原始输入/时钟、固定接触帧、Ground Query、脚与骨盆/膝盖输出分别对照。没有动态样本的Action、作者极小权重、重入或退化几何明确标记未覆盖，不以编译或单个总分通过冒充效果完成。

## 持续Goal第一轮：补齐真实骨盆观测

010821的30条大步是相对修正量，不等于最终世界骨盆位移。旧CSV用当帧PoseRoot位置/旋转重建世界骨盆，在本组三包的单位scale条件下经跨阶段髋点微米对账成立，但Sampler没有冻结同Completion的完整变换，不能推广到任意scale/shear或采样延迟。另一个已证错误是Releasing结果没有发布实际AnimatedPelvis输入，而Sampler仍用其默认零点加Goal算PhysicalPelvisGoalResidual，制造约0.747米的假误差。

该轮只修观察合同，不改变响应或质量规则。唯一Physical Writer在最终骨骼写完后读取一次真实Pelvis世界点，并用同一点反算组件点；两者随原PhysicalWrite Completion一起冻结，经既有Snapshot发布。StrideHips增加PoseInputAvailable，Accepted、Releasing与仅LandingReach的合法输入直接来自同帧PelvisFrame；无Goal的Rejected保持明确不可用。HeightTarget/Posture未求值与原Pose不可用不得混同。

唯一Sampler/Analyzer新增最终世界骨盆点、源Pose有效性及PhysicalPelvisGoalResidual有效性。只有同Completion最终写回、真实源Pose与非零Pelvis Goal都成立才测该误差；缺失时标Unavailable，不把零占位算成零缺陷。世界运动、组件运动和相对修正量并列作为事实，不改原37个质量Target/阈值/分母或七维评分。旧61不得补新字段重发62；新录制必须证明实际Foot、Pelvis、Knee、Goal和Body与010821保持，仅观测字段修正。

## 持续Goal第二轮：2Hz参数实验已拒绝

ae10348只将Corin现有正式频率从3Hz改2Hz，并显式重建匹配的Float32/Fixed产品。023618按facts62/d31独立回放确认Foot保护保持、Correction超过50毫米30→23，但目标和硬边界允许回正时仍低于-5毫米的帧数208→270，实际Solved Knee超过10厘米的单步160→174。因此不按七维总分不变或R826单峰降低采纳，原参数3Hz和匹配产品恢复，失败原包/Proof保留。该实验不构成新默认值、分支、配置开关或ZZZ参数复原，完整结果见experiments/20260831-pelvis-frequency-2hz.md。

## 持续Goal第三轮：背向速度门实验未接纳

2edf9bc/586c828只移除清速度的Handoff前置，033902已实际出现30个无Handoff清速度帧，265/266靶点成立、Correction超过50毫米30→28、Foot保护与37项计数保持。但世界大步仍33个，R826仍626.929毫米，R996的168.357毫米Knee峰后移至R997的221.587毫米。全包Knee超过10厘米净减少不抵消该明确窗口代价，也不将其误报成全局新增问题类别。

根任务不把这条组合交付为可用改善，恢复原Handoff前置及facts62/d31，保留原始facts63/d32失败包和实验公式。Runtime当前不采用该新门，没有旧新并列分支。完整目标时序排除、运行证据、撤销与恢复结果见experiments/20260831-pelvis-velocity-direction.md。后续Bend处理需用户独立授权，不在这次骨盆实验中混入。

## 持续Goal第四轮：取消背离下降目标的旧向上速度

当前候选以54979a5和035643为直接前驱，固定193957仍为效果对照。保留原Handoff判定与原清速度分支，只对`previousVelocity > 0`且`target-previousOutput < -GeometryEpsilon`增加无Handoff清速度资格。正式式为`reset = (Handoff != None || previousVelocity > 0) && abs(target-previousOutput) > GeometryEpsilon && previousVelocity*(target-previousOutput) < 0`。

该区别来自业务方向，而非帧号或膝盖诊断：下降需求已明确时，不继续把骨盆抬离目标、增加之后真实腿长上界强压的幅度；目标回升时，旧向下速度仍只服从原Handoff政策。本轮不保证世界Y单调，因为原动画与Root继续运动。只改唯一响应的输入速度，不改频率、目标、硬区间、Foot、Bend或任何资产，不新增速度状态、参数或第二响应。

冻结035643原输入的递推复现原Output最大误差5.04e-8米，候选预测266/789的Correction大步减小，R996/997模型不再改变；全包实际Foot、骨盆和Knee必须由新Replay判决。模型的负偏移积分略增且允许回正时低于-5毫米多一帧，是必须保留的风险，不能把模型当真实物理结果或提前通过。详见experiments/20260831-pelvis-upward-velocity.md。

075917正式Replay及独立验收后，本轮保留为局部改进：266/789实际修正大步减少、Foot与37质量计数保持、R994–997没有重演前次移帧。世界Y大步仍33，回正迟滞多1帧、少数Knee步长增加，R826峰未解；这些代价和待办不从完整Goal移除。当前唯一Runtime采用这一正速度资格，没有并列旧门/配置开关，193957仍为固定效果对照。

## 平地下压第一步：软偏好只在原Pose与共同请求之间选择

本轮以4404a79/075917为直接前驱，保留193957固定效果对照。平地179帧的只读分解区分真实腿长、20毫米余量、原动画偏好和历史迟滞；不是全部下压都由相同原因产生。

保持原Posture几何，先得到p0，再使用`preferred=Clamp(p0,Min(0,requested),Max(0,requested))`。这是降低额外弯腿偏好的目标优先级，不是增加可见输出夹紧；原动画零偏移为已知本帧基准，不是fallback。真Reach及20毫米安全余量继续位于后续唯一响应中并可覆盖软目标范围。脚目标、支撑资格、原Spring、Bend、Body和作者配置均不动。

Available/Reserve/Min/Max继续描述原姿态几何，OffsetAlongUp与TargetAdjusted描述实际采用的软目标；不要求软目标必须满足姿态区间，只要求真正硬Reach仍有效。既有事实足够重算，不新增CSV列。该步可能少保留一部分原动画弯腿程度，必须由同Record检查脚部、膝盖和台阶，不能用175单窗改善宣布平地整体修好。详细分解与模型边界见experiments/20260831-pelvis-neutral-posture-preference.md。

085223已完成正式Replay与独立验收并保留该局部修正：移动平地平均额外下压约减少1毫米，175减少21.515毫米，Foot及37质量计数保持；静止83帧、最深值和主要硬约束问题不变，R826峰与部分Knee步长略增。该结果不替代193957质量基线，不宣称平地下陷全部修复，也不取消后续原目标/硬边界排查。

## 平地下压第二步：正式压缩余量单变量候选

以09094b3/085223为直接前驱，2023条有效脚行的正式Rig腿长与实际两段骨长最大差不足0.4微米，排除厘米级长度缩短。平地原动画可只剩约12毫米弯曲余量，固定20毫米硬余量本身会要求额外下蹲；贴地目标与世界锁的几何需求仍须独立保留。

本轮只将Corin既有MinimumLandingLegCompressionReserve从0.02米试验为0.01米，保留唯一LegLength-reserve硬区间及Foot Reach保护。它不改真实腿长、脚目标、Bend、3Hz、软目标或速度门，也不把余量改成状态开关或软约束。10毫米是项目实验参数，不是ZZZ值或质量阈值；取舍是允许腿更直以减少不必要下压，是否引入反弯和脚部回归须由同Record裁决。

现有Diagnostics从实际Request/每行reserve复算，保持facts65/Analyzer65/diagnosis34、37项质量规则及独立2厘米只读反事实不变。Profile与Float32/Fixed/Projection身份通过正式构建同步，不能沿用旧产品或改Proof放行。冻结输入模型只作为准入，真实结果及保留/撤销记录见experiments/20260831-pelvis-compression-reserve.md。

092855真实Replay及独立验收后拒绝该候选：静止/移动平均下压显著减轻，但Knee超过10厘米157→166，R826最大峰增大且954/996旧翻侧后移放大；不能以Foot规则保持或骨盆均值改善采纳。a0aea66已只恢复20毫米Profile与匹配产品，唯一Runtime算法及Diagnostics不变。并行GroundEnvelope改动尚未隔离，恢复Replay未执行，不声称动态恢复完成。该实验确定了余量贡献，也说明降低下压前需独立解决膝盖选边稳定性；不在本change偷偷加Bend修改。
