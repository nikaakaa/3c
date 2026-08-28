# Change: 稳定角色脚步Path换代与Landing可达

## Why

当前Corin Foot Placement已经完成唯一深Module、每脚分型Lifecycle Context、唯一Goal Set、唯一FBBIK和根Bank事务重构，但仍有两项行为问题：Ground Path更新时脚部Correction会出现可见跳变，Landing前脚目标可能超出腿链可达范围并把膝盖拉到接近完全伸直。

封口诊断包已经把问题分开，但当前Path结论需要收紧：`Releasing -> Swing`同帧Floor顺序修复和取消Ground Path identity单独触发Residual重置，清除了错误执行条件，却没有让用户观察到明显的整体抖动改善。最新`20260828-184607-138-d23494c9824a42a89c9973d567305442`诊断在1043帧内记录829次Path Revision输出跳变、411次稳定Swing输出跳变和814次临近Landing换点。左脚737到738帧的RootLocalLanding与Event不变、剩余Step Time只减少一帧，但720度/秒转向令Timeline世界速度方向剧变，KCC Future Body Translation单帧移动约36厘米，查询切换Surface，Next Landing移动约39.7厘米，Swing Target移动约14.9厘米；Interpolation先把Correction限制到约9.16厘米，随后Ground Path Envelope Hard Constraint为避免穿地立即抬到约21.08厘米，最终Correction单帧跳约17.5厘米。当前首要问题因此不是再缩短最终Goal HalfLife，而是Future Body Prediction缺少跨帧稳定状态、Landing在临近接触时仍可被普通预测重写。Landing腿伸直则继续伴随Foot Goal满权、Primary Support缺失和目标压缩余量归零。

`build-character-foot-motion-data-foundation`已经由用户验收并归档，现行spec已经安装可审查的Step Time/Distance、Foot Height、Contact、Lock Mode/Weight与Support作者数据，并继续禁止没有正式行为change时的Runtime消费。本change建立唯一正式消费者，按独立小步迁移行为，不恢复旧隐藏Foot Feature、旧PlantConfidence政策、第二状态机或Goal后处理器。

本change已经把旧`CharacterFootStateMachine`拆成唯一Transition Resolver/Runtime、纯State Target Resolver、统一Interpolation Runtime与Post-Interpolation Hard Constraint，并删除分散Residual与兼容入口。最新诊断证明这套分层能精确定位首次放大阶段，但结构清晰不等于控制政策已经完整：Prediction仍直接消费每帧瞬时世界速度，Observation阈值只控制查询频率，Landing接受只有小变化死区，Approach Contact以后没有正式提交语义。后续实施必须在现有Owner内补齐成熟政策，不得恢复中央状态机或增加第二Prediction、第二Interpolation、Goal后处理器。

## Current Diagnostic Record

- 3.1修正了`Releasing -> Swing`同帧漏过Swing Ground Floor的执行顺序；该修正属于地面安全一致性，不足以解释整体Path抖动。
- 3.2删除了Ground Path identity单独变化造成的Residual重置；用户端到端观察为抖动基本不变，因此不得把它记录为可见问题的主修复。
- 正式Step Time只提供Landing前Residual欠账的截止时间。它不能解释Raw Landing端点变化约1厘米而Correction同帧跳变约12厘米的放大，不得在首个不连续阶段尚未定位时作为当前抖动修复接入。
- 逐阶段诊断已经把`Raw Landing/Path Target -> Swing Target -> Captured Residual -> State Output -> Safety Floor -> Encoded Goal`放在同Frame与Event lineage下对账，并以typed失败阻止缺失阶段时继续生成伪结论。
- `20260826-231414`在右脚4367到4368帧证明旧Event的Promoted Contact Landing遮住同帧新Event的Accepted Swing Landing，使State Machine错误发布`PathAvailable 1 -> 0 -> 1`。Swing Path Landing与Contact Landing必须拆成两个typed输入；该修复不改变后续Residual截止衰减政策。
- canonical nearest对照已经证明，上一Committed `SurfaceIdentity`通过`PreferredSurfaceIdentity`参与下一帧候选选择时，相同查询几何会因历史不同得到不同Surface；超过5厘米的23次CurrentFloor catchup中18次、超过10厘米的14次中13次来自旧Surface覆盖nearest。FutureLanding必须把上次真实查询输入作为累计基准；同Source/Cycle/Event/Profile/World下Raw Landing累计位移不超过5厘米且Component Up变化不超过1度时复用不可变Observation，超过阈值或lineage变化才查询一次。正式`Sliding`接触准入窗口在输入identity变化时刷新Observation，避免缓存误差与动画脚底残差叠加后拒绝正式Lock。历史Surface不得参与查询；查询后的Surface变化必须在Current Anchor与Next Landing所有权闭合后只换代Next Landing。
- ZZZ PIK最新逆向已经补齐完整Plant边沿转移表、驻留计时、回弹强制刷新消费链、ManualWait固定步目标到渲染帧相位混合、Heel/Toe双点、高度变化率Clamp、Plant旋转冻结、Footprint与物理组件CrossCheck、Pelvis参数、SmoothKnee开关消费和LateIK通道驱动。设计按“算法已确认 / 能力存在但公式未闭合 / 不可作为实现合同”三档记录采用边界；该资料只作为实现参考和行为对照，不成为Runtime依赖。实施直接复用控制顺序和职责，不照搬混淆类型、匿名B/D输入、未激活实例参数、全局缓存、预测/回退双路径、Legacy路径或多个IK组件。
- `20260829-001204-882-bb3f766924fa416db136bb00cbf77b6d`基线与`20260829-003203-424-118a4d5bfb2e46cea36a2043bce007be`候选使用同一1044帧纯输入Record完成A/B Replay。2086条Foot主行的662个同名字段中，除Program、Projection、Unity实例及其派生Surface/Path identity外，其余646个行为字段逐行完全一致，31个正式诊断target的命中数、比率和峰值完全一致，证明Event Frame前置没有行为回归。候选新增34个事件字段，Formal/Input不一致、非法Frame和Approach阶段不一致均为0；但PreSwing有46/492行的旧Step Time为0而Next Landing仍有约0.6秒，且`Corin_Pipeline_WalkStart_Inplace`右脚在帧122与995出现旧Step Distance `2.29190016m`、Next Landing Event Distance `3.65770483m`的`1.36580467m`冲突。该冲突必须按Source/Cycle/Side/ordinal与RootLocalLanding查明并typed拒绝或修正正式来源；不得扩大容差、直接切Prediction或提前完成2.4/5.3。
- 后续使用正式`character.foot_motion_bake`分析同一WalkStart源查明：上述两个Distance分别属于右脚刚落地的ordinal 2和下一次Landing的ordinal 3，并非作者数据冲突。旧单值Step Curve在接触后的PreSwing仍发布Current Event的Distance，同时Time已经失去Next Landing语义；新的Event Frame按Current Contact与Next Landing分别发布，正确暴露了旧Current/Incoming猜测路径。实施必须以明确Event occurrence消费对应Distance与RootLocalLanding，删除旧单值Step消费者；不得把这次差异当成容差问题或拒绝新Event Frame。上一条保留为切换前的失败诊断经验。
- `20260829-011616-328-887e772da37043d9a4b2ccf2d537d9fe`记录了第一次直接删除Biomechanical Step输入、同时把正式Lock Weight映射进旧PlantConfidence生命周期的失败实验。Replay虽然完成1044/1044且Fixed输出与历史Proof一致，但Foot行为明确劣化：Locked事件`44 -> 0`、Release事件`46 -> 0`、Support换代`88 -> 0`、Contact Plane可用行`202 -> 0`、Ground Path拒绝行`324 -> 671`、Stable Swing大于2厘米`41/411 -> 83/437`、Path Revision大于2厘米`137/829 -> 154/824`。该实验已由后续提交回退，证明唯一Frame不能靠字段改名穿过旧生命周期；实施必须先让正式Contact/Lock、Support与Height各自在现有根事务内成为唯一消费者并同步删除对应PlantConfidence、Support-from-Lock与Constraint Weight语义，最后再删除只剩Prediction用途的旧Step输入。
- `20260829-013614-721-26afaab920904b94b1ec0afe2027e3e9`进一步验证“先迁移正式Contact/Lock、后补Landing Commit”同样不可接受。该候选已经按正式Contact、Lock Mode/Weight、Current Event边沿与同Event Reentry替换Plant准入，但旧Landing Context仍只做Tracking/Promotion，结果Locked事件`44 -> 21`、Landing Span`48 -> 24`、Locked漂移大于1厘米`15/44 -> 16/21`、Swing到Landing大于2厘米`3/48 -> 6/20`、Landing未闭合`1/48 -> 10/24`。候选已经回退。正式Contact不得先于同Event `Tracking -> Committed -> Promoted`所有权落地；下一实施步骤必须先单独闭合Landing Commit并保持旧Contact生命周期不变，Replay通过后才重做正式Contact切换。
- `20260829-014738-536-329421aa420f44c8989546e4e020710c`证明反向顺序“先Landing Commit、暂留旧Plant生命周期”也不可交付。候选产生1154个Committed主行和24次Formal Current Event与上一Committed Event连续的Promotion机会，Committed后的普通查询已经被停止；但正式接触时旧PlantConfidence仍接近0，旧生命周期不进入Landing、Locked或Release，三个事件覆盖全部归零。该候选已经回退。正式Foot Motion与旧隐藏Plant时间轴本来就不同，因此Landing Commit与正式Contact/Lock必须作为同一原子业务切换实施并共同Replay；不再制造任一半迁移的可运行中间态。
- `20260829-020405-247-f47057128ebe4d439588fafd008776cd`证明“Landing Commit与正式Contact/Lock原子切换、Prediction暂留旧Step”仍然不构成闭合业务切换。候选虽然保持1154个Committed主行并彻底删除Foot Placement正式链的PlantConfidence/PlantCycleConsumed，但Landing Span仅`48 -> 5`、Locked事件仅`44 -> 4`、54个正式落地边界仍有51个Missed Entry、Locked漂移大于1厘米变为`4/4`、Stable Swing大于2厘米由`41/411 -> 32/261`且比率`9.98% -> 12.26%`。499行正式Lock请求中有286行`ContactUnavailable`、172行`ContactOutOfLockRange`；Current Contact Event与Next Landing Event已经正确分离，但正式Contact仍在消费旧Step Time/Distance/RootLocalLanding算出的Landing，初始或持续接触Event没有自己的正式Landing，能找到上一Landing的请求又常因旧预测残差超过8厘米准入。该候选必须回退。下一次业务切换必须把唯一Foot Motion Frame、正式Current/Next Event驱动Prediction、Landing Commit和正式Contact/Lock放进同一原子迁移，不得让正式Contact继续消费旧Step Prediction。
- `20260829-030354-218-0805c6d38b0142faafad0fce4371e58f`证明把唯一Foot Motion Frame、正式Current/Next Prediction、Landing Commit和正式Contact/Lock一次原子切换仍缺少“接触边沿刷新已提交Landing”的闭环。候选恢复到31个Landing Span和19个Locked事件，但54个正式落地边界仍漏28个、提前进入5个；Landing不收敛由`1/48 -> 24/31`，Swing到Landing大于2厘米由`3/48 -> 21/30`，Stable Swing大于2厘米由`41/411 -> 64/345`，Locked漂移或下陷由`15/44 -> 13/19`，最终Heel穿透由`41/92 -> 35/50`。正式Lock窗口中有196行`ContactEventUnavailable`、223行`ContactOutOfLockRange`，而`ContactAcquired`只有31次：Next Event在接触前提交的Landing进入Current Event后若已经存在，当前实现不会用接触边沿的canonical当前脚底输入刷新它，残留预测误差直接越过8厘米Lock准入；少量准入后Landing又多在1至3帧内结束，统一Interpolation没有形成可见闭合。该候选必须回退。下一次实现必须先在现有正式Observation Page内安装同Event Contact Rising一次性刷新与提交规则，并证明它不会增加普通Swing查询或覆盖Next Event，再重新执行唯一Frame与正式Lock原子切换。
- `20260829-032215-058-391e4dfe6b0c4b18834eb9ce467ab359`证明Contact Rising一次性刷新能闭合准入，但同时暴露Formal Event `RootLocalLanding`的空间合同错误。候选把`ContactOutOfLockRange`从223行降到20行、`ContactAcquired`从31次增到54次、Missed Landing从`28/54`降到`4/54`；然而Landing不收敛达到`39/54`、Swing到Landing大于2厘米达到`39/54`、Locked漂移或下陷达到`28/39`、最终Heel穿透达到`64/93`。右脚187至188帧的同一Event在接触前预测到`Y=0`且水平落点偏离约1米，接触刷新后真实台阶为`Y=0.18m`，Acquire安全下界被迫单帧抬脚约9.9厘米。正式Bake确认事件时序正确，但Projection把Rootmotion Reference相对`Bip001` Motion Root的`(-0.549,-0.095,-0.544)`直接发布为Runtime RootLocalLanding；Predictor却按VisualRoot/KCC Body local解释，而同Event接触时实际需要约`(-0.285,0.0237,0.272)`。该候选必须回退，禁止通过减慢Acquire掩盖穿地。下一步必须让Formal Event时间与Distance继续来自Motion Reference，而RootLocalLanding来自Target Inplace Clip同Landing sample的VisualRoot-local位置，显式重建Corin产物并先证明旧行为不变，再重做正式消费者迁移。
- `20260829-035426-702-d8d0a4fd8b48434db82470bfd5308625`完成Formal Event空间合同修正后的无行为回归封口。Foot Motion analysis升级到v82：时间、Distance与运动学继续来自Rootmotion Reference，Landing Event RootLocalLanding改由Target Clip同采样点的VisualRoot-local脚底提供，并严格校验样本数量和有限值。Corin Float32与Fixed产品已显式重建，Program hash不变，Presentation Projection revision由`53566be8...`换代为`808cf24f...`；WalkLoop右脚同一Event字段由`(-0.549,-0.095,-0.544)`变为`(0.0058,0.0237,0.3939)`。同一1044帧Replay的八份诊断全部target eligible/matched与基线完全一致，旧Runtime Raw Landing逐行不变，证明该提交只修正式数据语义，没有改变现行行为或引入第二运行路径。
- `20260829-040230-586-de935a88206b4d9b9fed571ad6e298d1`证明v82已经修正正式Raw Landing，但Landing Context仍错误冻结跨Source的旧Committed Event。右脚187帧Formal Next与188帧Current Contact的Raw Landing只差约1厘米且都命中`Y=0.18m`踏面，`ContactAcquired`保持54次、`ContactOutOfLockRange`为25行、Missed Landing为`4/54`；但Landing不收敛仍为`37/54`、Swing到Landing大于2厘米为`31/54`、Locked漂移或下陷为`28/39`、最终Heel穿透为`63/93`。该事件窗口中Formal Next已经是`1797538807859757055`，Landing Context与Ground Path Next却持续保留前一Source的Committed Event `5184069796753506521`和`Y=0`端点，因为`CaptureNextSwing`对任意Committed状态无条件返回，不校验它是否仍为正式Current或Next Event。下一步只允许仍与正式Next Event一致的Commit冻结；Source/Event换代且旧Commit既未晋级Current、也不再是Next时必须清除并接纳新Event，不得把跨Source旧Commit当作Landing稳定性。
- `20260829-041159-640-4f58495f7cd24f0dae0e9d4ba09918d7`证明跨Source旧Commit已经清除：Tracking/Committed Event与Formal Next一致，右脚180至187帧Ground Path Next持续保持`Y=0.18m`，187至188帧最终Correction只由约8.57厘米变到9.13厘米。但候选整体仍未通过，Landing不收敛为`33/54`、Swing到Landing大于2厘米为`27/54`、Locked漂移或下陷为`27/39`、最终Heel穿透为`59/93`、Stable Swing大于2厘米为`101/548`。代表Stable Swing在Path、Event和Landing点均未变化时，Ground Envelope随Phase前进令Swing Target单帧变化7.8厘米；统一Interpolation输出只到5.3厘米，但旧高度链没有应用Formal Foot Height，Post-Interpolation Ground Envelope安全下界把最终输出直接夹到13厘米。下一步必须完成既定Foot Height迁移：目标固定为同一Envelope采样加Formal Foot Height，删除`LandingConstraintWeight * FormalTargetCorrection`与NextSwing Constraint Weight旧状态；不得放松安全下界或用更长HalfLife允许穿地。
- `20260829-042623-080-ef3822e052dd423f95ebf10cc49794a6`否定了“先单独接管Formal Foot Height即可修复上述跳变”的实施顺序。候选已经把Swing目标改成`Ground Envelope + Formal Foot Height`并删除全部`LandingConstraintWeight`与`NextSwingConstraintWeight`代码和诊断列；穿透只轻微改善，但Stable Swing大于2厘米由上一候选`101/548（18.43%）`恶化到`142/529（26.84%）`，Path Revision大于2厘米由`94/555（16.94%）`恶化到`133/538（24.72%）`，Swing到Landing大于2厘米由`27/54（50.00%）`恶化到`29/54（53.70%）`。左脚240至241帧中Envelope保持`Y=1.62m`、正式目标高度只由`1.657996m`变到`1.657673m`，但动画Sole由`1.413968m`升到`1.446352m`，Correction相应由`0.244028m`变到`0.211321m`；现有Continuity仍把正常Phase采样标为`SwingTargetChanged`并重建Residual，Interpolation输出单帧变化约4.34厘米。失败提交必须回退。正式高度政策本身不撤销，但再次接入前必须先让同一Event、同一Accepted Landing与同一Envelope的Phase推进不触发Path Revision或Residual重建，并由统一Interpolation连续跟踪时变目标；不得通过改诊断阈值、冻结Phase或放松Ground安全下界掩盖问题。
- `20260829-043549-433-cf93562c24314afa95b24e91411852bc`是上述失败实验的回退复验。1044帧Replay生成2086条Foot主行、54个Landing Span、39个Locked Event和52个Release Event；八份诊断中全部target的eligible、matched与rate均与`20260829-041159-640-4f58495f7cd24f0dae0e9d4ba09918d7`逐项完全一致，证明代码、采样schema和行为已经无残留地回到上一候选。
- `20260829-044710-010-d91eb0f5cd884418b391fe538cde6fcd`证明把每帧全部Phase目标变化都低通会积累额外滞后：Stable Swing与Swing到Landing分别减少2个大跳，但Path Revision大跳增加1个，右脚987帧因累计Residual令原本1.76厘米的Revision输出变化变成2.04厘米，仍不满足逐项无回归。后续`20260829-045916-613-03e2e955f615423e8b0cb99ceffea926`把正式Path Revision与同Path目标跟踪分型：Path原因只保留可用性、Landing Event和Accepted Landing点换代，Phase目标变化超过正式阈值时发布独立`TargetTrackingApplied`并沿用原连续接管数学，不再发布`SwingTargetChanged`或伪装成Path重建。该Replay有200行Target Tracking、0行旧原因；所有可见跳变、穿透、Landing、Lock与Release target的eligible、matched和rate均与回退候选一致，Path Revision合同仍为0命中。该步骤只完成职责拆分并证明零回归，不宣称已经改善现有抖动；Formal Foot Height再次接入前仍需对照ZZZ已确认的高度Delta变化率Clamp，将必要的速率政策放进唯一Interpolation Runtime。
- `20260829-052918-223-0e4f9a6d9b8e401fa84c4eddb7682225`否定了“Formal Foot Height接管后以ZZZ默认同型`1.2m/s`竖直Correction变化率限制即可闭合高度迁移”。候选把高度限速放入唯一Interpolation Runtime，391行触发限速，Safety Floor Clamp由235行降到85行，最终Heel穿透由`59/93`降到`48/93`，Swing到Landing大于2厘米由`27/54`降到`19/54`，FBBIK保持`2086/2086`成功且最终物理脚踝目标最大残差由4.35毫米降到2.45毫米；但Stable Swing大于2厘米由`101/548（18.43%）`恶化到`115/530（21.70%）`，Path Revision大于2厘米由`94/555（16.94%）`恶化到`110/537（20.48%）`，并新增`15/48`个Residual Deadline Miss。限速虽然减少末端Hard Floor补偿，却把未收完的Formal Height残差带过正式Landing边界，仍违反逐项无回归门槛。该候选必须整体回退；后续高度迁移不能只限制每帧Correction速度，必须先明确Formal Height时变目标在Swing剩余时域内的可达轨迹或Landing边界收敛政策，同时禁止降低安全下界、扩大诊断阈值或接受穿地换连续性。
- `20260829-053929-274-f9129235d0da4315ab5a36dc01931e74`是上述Formal Height速率限制失败实验的回退复验。Runtime与Editor工程重新编译、Unity显式Refresh并重启后，同一1044帧Record生成2086条Foot主行；八份诊断共25个target的eligible和matched与`20260829-045916-613-03e2e955f615423e8b0cb99ceffea926`逐项完全一致，FBBIK保持`2086/2086`成功、最终物理脚踝目标最大残差恢复为4.35085967毫米，Safety Floor Clamp恢复为235行。失败代码、配置和诊断schema已经无残留地回到已知候选，失败经验与原始采样仍保留在Git和持久Diagnostics目录中。
- `20260829-061422-001-6dfafda9b2e74a5486a5ff02576ff6b1`否定了“以Corin业务量级`0.2m/s`死区、`12/s`EMA响应和`8m/s`上限直接稳定committed Timeline速度即可改善Landing Prediction”。候选已经原子建立左右脚共享Prediction Motion State、正式Source/Reset lineage、KCC唯一稳定速度输入、缓存revision和facts v24；1043帧中Prediction可用944帧、Current响应851帧、最大速度Clamp为0，FBBIK保持`2086/2086`成功，Stable Swing大于2厘米由`101/548`降到`87/513`，新增穿透由`10/93`降到`9/93`。但Raw与Stable当前速度差的中位数达到0.838米/秒、P90达到4.079米/秒，Observation查询由1164次增加到1202次，Landing Point Revision行由518增到553；随后Path Revision大于2厘米由`94/555`恶化到`101/590`、Swing到Landing由`27/54`恶化到`29/54`、Landing Exit由`35/54`恶化到`38/54`、Locked漂移由`27/39`恶化到`28/39`、Safety Floor Clamp由235行增到245行。该控制把稳定Swing的变化延迟到Path换代和接触边界，不满足逐项无回归，必须整体回退。后续Prediction Motion不能只对速度做固定EMA；必须先闭合“稳定速度变化、5厘米累计Query Admission、Approach Commit时刻”三者的时域关系，禁止把更少的普通Swing变化换成更晚的Landing/Lock误差。
- `20260829-062306-770-7559feeb4c194dc9ab0c83021c329b6a`是上述Prediction Motion固定EMA实验的回退复验。删除新状态、配置、KCC输入切换和facts v24后，Runtime与Editor重新编译，Unity显式Refresh并重启；同一1044帧Record的八份诊断共25个target与`20260829-053929-274-f9129235d0da4315ab5a36dc01931e74`逐项完全一致，Observation查询同为1164次，FBBIK同为`2086/2086`成功，最终物理脚踝目标最大残差同为4.35085967毫米，Safety Floor Clamp同为235行。失败实验已经无残留回退，采样与实现仍可由Git历史追溯。
- `20260829-063342-672-691b29f9a8cc4397967de6ba17a06e0a`完成Landing提交与Rejected保留语义封口。Approach区间的`CommitAttempted`由376个逐帧重复事实收敛为51个predictive Event各一次，Committed持续行仍为376且CommitUnavailable为0；Tracking阶段同Event的新Observation不可用时保留既有Accepted Landing及原始lineage，Source/Event消失后的5行陈旧Tracking则显式清除。Observation查询仍为1164次、FBBIK仍为`2086/2086`成功、最终物理脚踝目标最大残差仍为4.35085967毫米、Safety Floor Clamp仍为235行，25个正式target的matched全部不变；两个Path Continuity target只因删除陈旧样本令eligible各减少1且matched继续为0。该步骤证明`Empty / Tracking / Committed`、Surface换代、一次性Approach提交与Rejected/Accepted分权已经闭合，不改变现有脚部行为。
- `20260829-064313-909-b751e00de849436db2d2edab2d5a2f77`否定了“只把KCC Future Body Translation的当前速度从`Body.TargetVelocity`切到`Timeline.CurrentVelocity`，Continuation保持Timeline不变”这一独立迁移步骤。候选没有增加Prediction状态、滤波、阈值或双路径；Observation查询由1164次降到1159次、Safety Floor Clamp由235行降到234行、Swing到Landing大于2厘米由`27/54`降到`26/54`，FBBIK仍为`2086/2086`成功且最终物理脚踝目标最大残差仍为4.35085967毫米。但Stable Swing大于2厘米由`101/548`恶化到`105/550`；其余24个正式target的matched不变，Path Revision大于2厘米仍为`94`。这证明当前`Timeline.CurrentVelocity`不能脱离共享Prediction Motion状态和Query Admission时域单独替换现有当前速度输入；正式迁移必须把输入接纳、稳定状态与5厘米累计查询合同作为同一可回退步骤闭合，不能先切输入源再等待后续滤波补救。该候选必须反向提交恢复。
- `20260829-065054-213-0746d3431e6c4cc3b28a999819e709ba`是上述Timeline当前速度独立迁移的回退复验。反向提交、Runtime与Editor编译、Unity显式Refresh并重启后，同一1044帧Record生成2086条Foot主行；25个正式target的eligible与matched逐项完全回到`20260829-063342-672-691b29f9a8cc4397967de6ba17a06e0a`，Observation查询恢复为1164次、Safety Floor Clamp恢复为235行、CommitAttempt恢复为51行且Committed持续行为376行，FBBIK保持`2086/2086`成功，最终物理脚踝目标最大残差为4.35085967毫米。失败代码已经无残留回退，失败采样和结论保留在Git及Diagnostics目录。
- `20260829-070138-754-a340e1b99d3e48809d2d8f395e34e3cd`完成Prediction Motion正式配置与状态合同的零行为落地。Profile v29新增并严格校验`PredictionVelocityDeltaThreshold`、`PredictionVelocitySmoothSpeed`和`PredictionMaximumSpeed`，Corin显式序列化`0.1m/s`、`30/s`与`8m/s`；Future Body Translation Source发布正式identity，并新增固定布局共享状态纯求值器，但尚未接入KCC请求。Runtime与Editor编译后，同一1044帧Record的25个正式target与`20260829-063342-672-691b29f9a8cc4397967de6ba17a06e0a`逐项完全一致，Observation查询1164次、Safety Floor Clamp235行、FBBIK`2086/2086`成功、最终物理脚踝目标最大残差4.35085967毫米。该步骤证明配置、Profile Revision、Source合同与未调用状态类型本身不改变行为，下一步可以单独归因唯一运行链接管。
- `20260829-071427-436-2f65452207d54e2d9f6a8fb6adb30cde`否定了以Corin`0.1m/s`速度差阈值、`30/s`EMA响应和`8m/s`上限接管共享Prediction Motion唯一运行链。候选已在根Pending Bank内闭合Timeline/Body Reset/Source lineage、稳定当前与Continuation速度、Prediction Revision、唯一KCC Future Translation及facts v24强校验；1043个Prediction帧中99帧Unavailable、2次Reset、793帧Current响应、118帧Continuation响应且没有最大速度Clamp。Stable Swing大于2厘米保持`101`，Swing到Landing由`27/54`降到`26/54`，最终Heel穿透由`59/93`降到`57/93`，新增穿透由`10/93`降到`9/93`，FBBIK保持`2086/2086`且最大物理脚踝目标残差由4.35085967毫米降到3.56052141毫米；但Raw与Stable当前速度差中位数仍为0.144米/秒、P90为1.409米/秒，Observation查询由1164增到1166，Path Revision大于2厘米由`94/555`恶化到`96/560`、Landing Exit由`35/54`恶化到`36/54`、Locked漂移由`27/39`恶化到`28/39`、Safety Floor Clamp由235行增到246行。30/s响应仍把一部分变化延迟到Path、Landing和Lock边界，不满足逐项无回归；运行链接管必须反向提交恢复，已验证零行为的配置、Source合同和未调用状态类型可保留为下一候选边界。
- `20260829-072044-676-c75998f39dc44695a238cfe91a7b4284`是上述30/s Prediction运行链接管的回退复验。移除Bank运行态接管、稳定速度KCC输入与facts v24行为诊断后，保留Profile v29配置、Source identity和未调用状态纯求值器；Runtime与Editor重新编译、Unity显式Refresh并重启。同一1044帧Record的25个正式target与零行为边界`20260829-070138-754-a340e1b99d3e48809d2d8f395e34e3cd`逐项完全一致，Observation查询1164次、Safety Floor Clamp235行、CommitAttempt51行、Committed持续376行、FBBIK`2086/2086`成功和4.35085967毫米最大物理脚踝目标残差全部恢复。失败运行代码无残留，下一候选必须先降低Raw到Stable的尾部误差，不能沿用30/s响应。

## What Changes

- 从当前选中动画Source的原生Foot Motion Curve生成唯一typed Runtime Foot Motion Frame，并保持Source、Cycle、Contribution、Completion、Clip与Landing Event lineage。
- 保留已经完成的`Releasing -> Swing`顺序修正和identity触发清理，先对Ground Path到Encoded Goal的逐阶段Correction做同帧归因，修复首个已证明的不连续阶段；之后才接入Residual截止收敛与真实Envelope安全边界。
- 只把Step Time/Distance接入Landing Prediction，保持世界落点仍由正式Future Body Translation、RootLocalLanding与唯一SphereCast生成。
- 在现有Foot根Bank内增加左右脚共享的Prediction Motion State。它只消费committed Timeline当前/Continuation世界速度、正式Step Time、Presentation Delta、Trajectory Generation与Body Reset，并按ZZZ同型的速度差阈值、EMA响应和最大预测速度生成稳定速度，再由唯一KCC Future Body Translation同时服务左右脚；不建立低速回退路径或第二Trajectory Source。
- 为每脚FutureLanding建立根事务所有的Committed/Pending Observation Page；每帧继续重新投影Raw Landing，但只在相对上次真实查询输入累计超过正式距离/Up角度阈值、Source/Cycle/Event/Profile/World lineage变化或正式`Sliding`接触准入输入identity变化时执行一次SphereCast，其余帧复用Committed Observation。新查询始终选择canonical最近合法Surface并删除历史Surface偏好。
- 把每脚Landing所有权收敛为同一Context中的`Tracking -> Committed -> Promoted`：PreSwing与早期Swing允许稳定Prediction更新NextSwingLanding；正式Foot Motion进入Approach Contact后，最新Accepted Landing成为该Event的Committed承诺，普通预测只保留诊断而不得重查或换点；成为Current Contact Event后再晋级Contact Landing。新Rejected Observation不得伪装成Accepted，但Tracking可以保留同Event已经Accepted的事件Landing，不把它改名为Rejected Key的结果。
- 把每脚约束执行固定为`不可变输入与Observation -> Pre-Interpolation Transition -> State Target -> 统一Interpolation -> Post-Interpolation Transition -> Hard Constraint -> Resolved Foot`。同一根事务按固定顺序执行一次，不建立第二状态机或第二输出路径。
- 用独立typed `CharacterFootTransitionResolver`声明固定Transition边、判定阶段和优先级。Resolver只生成不可变Decision；唯一Transition Runtime应用State与Anchor命令，不执行插值、不查询世界、不写Goal。
- 用纯`CharacterFootStateTargetResolver`按Transition后的离散State生成Correction Target、接触引用、Goal/Ownership目标和Interpolation Policy Request。State Target不得保存时间状态、推进Residual或跳转到另一State。
- 用唯一typed `CharacterFootInterpolationRuntime`拥有Effective Correction、唯一Residual、上一Target与Completion。Swing Path换代、Landing Acquire和Release都提交固定Policy Request给它执行；删除分散在State分支中的`SwingResidual`、`AcquireResidual`、`ReleaseResidual`、`ContactProgress`与重复`Advance`数学。
- Swing/UnlockedSupport的State Target只使用正式Ground Path、Envelope与Foot Height；插值后的Hard Constraint复用同一Ground Path Envelope作为安全下界。删除逐帧CurrentSwingFloor Query及全部结果、诊断和Owner语义。Releasing继续只回到原始Swing目标。
- 只把Foot Height接入Swing，使动画抬脚高度叠加到Runtime Ground Envelope，删除由`LandingConstraintWeight`乘`BaselineHeightError`或`FormalTargetCorrection`提前改脚目标的旧政策。
- 只把Support接入Resolved Foot、Primary Support与Pelvis，使承重意图不再依赖Lock资格，并为Landing腿提供独立Reach请求。
- 增加必须显式序列化的米制最小Landing腿压缩余量；缺失即typed invalid，不提供默认值。Pelvis优先求双腿可达交集，无法同时满足时夹紧Foot Goal并发布typed不可达结果，不允许完全伸直后继续进入Full Lock。
- 保留现有唯一Pelvis Critical Spring并参照ZZZ的非对称速度边界，增加必须显式序列化的最大上升、下降速度；Spring积分后先限制速度，再把Target与Output限制在双腿Reach交集，撞到边界时清除继续向外的速度。
- 最后用Contact、Lock Mode和Lock Weight替换旧PlantConfidence的Landing、Locked、Sliding与Release生命周期；由独立Transition、State Target与统一Interpolation链执行，Anchor与Interpolation各自只有一个typed Owner。
- 保持`Swing / UnlockedSupport / Landing / Locked / Releasing`五个顶层状态不变，在同一Foot根Bank内增加分型Contact Transition Context，只保存上一正式Lock请求、距最近边沿时间、最近与最近释放Contact Event identity。Resolver生成Contact Rising/Falling/Same-Event Reentry Refresh事实，唯一Transition Runtime更新Context；Releasing期间同Event快速重入必须复用仍保留的Anchor与Committed Landing执行`Releasing -> Landing`强制刷新，不重查、不重建Anchor、不把Interpolation清零。Release完成后不复活旧Event，新Event不受上一Event回弹事实阻断。删除旧`PlantCycleConsumed`布尔，不新增Rebound或Grounded顶层状态。
- 每迁移一个消费者就删除对应旧输入与旧解释，不保留新旧双读、fallback、运行开关或兼容reader。
- Foot诊断采样包把每Frame/Side唯一阶段事实与一对多Ground Contact/Envelope几何拆成正式主表和几何表；停止录制后由唯一后台Finalizer排空、封存、分析并发布，不在Unity主线程等待Writer或扫描CSV。

## Dependency And Ordering

- `build-character-foot-motion-data-foundation`已经由用户验收并归档；本change不得反向修改Analyzer候选、Motion Reference或AnimationClip作者数据。
- `refactor-character-pose-graph-architecture`只改变Program Operation、Constraint Bank与Final Publication所有权；本change先固定Foot模块的typed输入、唯一Result和根事务边界，Pose Graph重构把Foot视为不透明Constraint能力，不规定其内部Transition与Interpolation布局。
- 行为迁移固定按`Path逐阶段归因 -> 首个不连续阶段修复 -> 拆分State/Transition/Interpolation/Hard Constraint -> Step Time/Distance -> 共享Prediction Motion稳定 -> Landing提交 -> Foot Height -> Support/Pelvis与非对称速度边界 -> Landing Reach -> Contact/Lock`执行。Step Time不得用来掩盖同帧Correction放大；后一步不得在前一步仍有双主线时开始。

## Impact

- Affected specs: `character-foot-placement-presentation`、`character-animation-pipeline`
- Affected runtime: AnimationClip/Source Foot Motion采样、Presentation Projection、Foot Placement Pose Input、共享Prediction Motion State、Landing Prediction/Tracking/Commit、Ground Path、Foot Discrete State、内部Contact Transition Context、Transition/State Target/Interpolation/Hard Constraint、Resolved Foot、Primary Support、Pelvis与Goal编码
- Affected profile: Prediction速度差阈值、EMA响应速度、最大预测速度、Pelvis最大上升速度与最大下降速度必须正式序列化、严格校验并进入Profile Revision；不提供默认值或旧配置补全
- Affected editor/build: Projection Compiler、正式Curve/Event lineage校验、Corin Float32/Fixed产品显式重建
- Affected diagnostics: 正规化采样包、后台Finalizing生命周期、Raw Landing/Path Target、Swing Target、Residual Capture、State Output、Safety Floor、Encoded Goal、Envelope clearance、Landing Reach、Support来源、Solved/Physical结果与typed拒绝原因
- 不改变Gameplay State、World State、KCC、网络packet、rollback snapshot或Simulation Program行为

## Current Spec Comparison

- current `character-foot-placement-presentation`已经删除`8fc704a`公式、PlantConfidence生命周期和单体`CharacterFootStateMachine/Context`实现约束，只保留一个权威Foot结果、typed状态单一写入、根事务和下游隔离边界。本change通过delta把内部实现收紧为独立Transition、纯State Target、统一Interpolation与Hard Constraint固定顺序。
- current spec只要求时间连续化、Transition和Hard Constraint不能互相越权，不规定具体策略。本change安装正式Foot Motion输入、Transition边、Interpolation Policy和迁移完成后的旧字段删除要求。
- current Resolved Foot与Pelvis合同只规定下游不得读取Foot内部状态。本change进一步安装与Lock分离的正式Support Intent、Landing Reach和无交集时的Goal夹紧政策。
- current `character-animation-pipeline`规定新增22条Curve在没有正式消费者时不进入Runtime；本change通过新的Animation Pipeline requirement建立唯一Runtime Frame，并在消费者迁移时取代对应的旧Runtime输入。
- active `refactor-character-pose-graph-architecture`对Foot的delta只调整Constraint/Final Publication所有权，与本change不矛盾；实施时仍需按其最终Program lineage重新对账。
- current Landing Prediction与Ground Path要求PreSwing/Swing持续重新投影并在新Observation达到死区后换代，但尚未表达Prediction速度状态和Approach Contact后的Landing承诺。本change修改为Tracking阶段继续canonical更新、Committed阶段停止普通预测消费；这不是旧Key fallback，而是同Event Landing所有权从可修改预测变成已提交承诺。

## Non-Goals

- 不实现Heel/Toe双点IK、脚掌旋转、移动平台、Reactive Foot或第二种Ground Query。
- 不在本change实现ZZZ中的Foot Pitch/Roll、SmoothKnee、Stride Wrapping、Moving Platform Delta、Time Scale逐骨骼快照、楼梯专用Knee速度或动画后Bone Adjust；这些能力在后续正式change中继续以ZZZ成熟实现为行为参考，但必须进入现有Resolved Foot、Pelvis、FBBIK或Pose Graph Owner，不得增加第二IK、第二Writer或隐藏组件。
- 不增加“无论输入与状态如何，每帧覆盖最终全身Pose”的常驻滤波。ZZZ现有证据只确认预测滤波、位置/姿态混合、骨骼快照和分责任稳定，没有确认一个无条件最终Pose低通；当前`CharacterFootInterpolationRuntime`也只连续化Foot Effective Correction，不承担全身Pose、Pelvis或Knee平滑。
- 不增加第二Foot State Machine、第二Goal Set、第二FBBIK、全局Goal低通或图外骨骼修正。
- 不把ZZZ的Rebound、Blocking或Grounded位复制成新的顶层Foot状态；边沿、驻留时间和同Event重入只属于现有Transition Resolver的内部typed事实。
- 不创建字符串Channel、Dictionary字段、任意Tween注册表或跨Foot/PoseState/AnimationSlot共享的全局插值服务。统一Interpolation只服务Foot Constraint的固定typed通道，不复用Pose Transition Routing。
- 不用膝盖最小角度直接覆盖FBBIK结果，不用降低Goal Weight掩盖不可达。
- 不修改Foot Motion Analyzer算法、正式AnimationClip曲线或Motion Reference绑定。
- 不处理TrainingEnemy，不顺手迁移其Pose Graph、曲线、Projection或Foot配置。
- 不新增自动测试；实施只复用现有编译、严格OpenSpec校验和封口诊断重放。

## Success Criteria

```text
每帧只有一个正式Foot Motion Runtime Frame且lineage匹配选中动画贡献
Step、Foot Height、Support、Contact/Lock各自只有一个正式消费者
Releasing完成并回到Swing的同一帧执行新Swing Envelope保护
普通Path Revision保持Correction连续且向上/向下响应使用同一Residual政策
Raw Landing或Swing Target的小变化不得在State Output、Safety Floor或Encoded Goal阶段被无依据放大
720度/秒急转等高角速度输入下，KCC Future Body Translation必须消费同一根Bank提交的稳定Prediction速度，不得直接把单帧瞬时速度方向变化变成大幅Landing换点
PreSwing与早期Swing只在Tracking阶段允许更新NextSwingLanding；Approach Contact后同Event Landing必须Committed且普通预测不得重查、切Surface或重建Ground Path
Rejected Observation不得冒充Accepted；Tracking保留的同Event已接受Landing必须继续保持自身原始Observation lineage
真实Ground Envelope最低安全高度不被平滑穿过
首个同帧不连续阶段修复后，Landing前Residual才按剩余时间收敛到SwingResidualTolerance以内或发布明确不可达
Swing目标使用Runtime Envelope加正式Foot Height
Support Intent不由Contact或Lock门控且Pelvis不出现无依据支撑空洞
Pelvis上升与下降速度分别受Profile正式上限约束，Reach边界不得积累继续向外的Spring Velocity
Landing腿与支撑腿Reach区间有交集时Pelvis输出位于交集内
无交集时Foot Goal夹紧并拒绝Full Lock，不产生超长腿目标
Contact/Lock生命周期只读取正式Contact、Lock Mode与Lock Weight
顶层Foot状态仍只有Swing、UnlockedSupport、Landing、Locked与Releasing
Contact Rising/Falling与Same-Event Reentry Refresh只存在于同一根Bank内部Transition Context和Decision事实
Releasing期间同Event重入只可Retain原Anchor并连续执行Releasing到Landing；Release完成后不得复活旧Event，新Event必须正常准入
每条Foot Transition只由唯一typed Resolver判定并由唯一Transition Runtime应用
State Target不拥有Transition、Residual、HalfLife、时间推进或Hard Constraint
Swing、Landing Acquire与Release只通过一个Interpolation State、一个Residual和一个Effective Correction Owner连续化
Pre/Post Transition、Interpolation与Hard Constraint顺序固定且每帧各执行一次
Ground Path Envelope与Reach只约束插值后的结果，不反向修改State、Transition、Target或Residual
Swing Hard Constraint只消费已接受预测Path；预测输入不变时不得执行实时地面查询或逐踏面切换输出
旧隐藏Step、Constraint、PlantConfidence、PlantCycleConsumed布尔和Support消费者被删除
不存在fallback、旧新双读、第二状态机、第二Goal链或TrainingEnemy变化
```
