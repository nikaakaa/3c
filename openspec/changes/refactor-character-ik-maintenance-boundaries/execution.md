# 串行执行记录

## 固定对照与接入

- 总源码及行为基线：`ad3527e103cc3235a63e8a1c1dbd26df5155e0ba`，不得随HEAD更新。
- 主证据：`Diagnostics/FootPlacementRuns/20260831-233436-894-d1564c7fa0b442f6aef02bb470ca0b1b`；独立交叉证据：`20260831-205014-114-dc157fde9c004846a72e9cd1fa1b5b01`。
- 唯一正式Record：`43357ff3cd384e5cba75d2c31175b116`，1044 Tick、60Hz，使用原`logic-locked`与`one-fixed-tick-per-presentation-frame`驱动。
- 已确认原始samples、geometry、analysis与持久replay-proof均存在。两包各1043表现帧、2086脚行、1215列、67186几何行；旧逐列结果为1191业务列一致、24身份列双向映射。候选仍需实际回放，不复用旧结论冒充新验证。
- 第二阶段`refactor-character-pose-graph-architecture`的串行接入文档已提交`8cb2eef`；仅第一阶段全部通过后启动。Reset修正单独提交、单独验证并作为第二阶段保留成果。
- `ad5f6f9`为共享工作区独立GM任务的正确改动，不回退。IK核心在本次开始时与固定基线相同。

## 第一个闭环：请求生产与最终发布

状态：候选`2a6fe3309ddbaf2906ee88ef6758aa077aa47da8`已通过本Record范围的编译与正式回放对账，可作为下一小步对照；不代表整体IK质量通过。Editor第一次使用no-restore时因GM新增工程缺少project.assets.json失败；按正式构建完成依赖还原后成功。最终构建只有既有InputValueNodes未使用字段警告，0错误；每次均按规则关闭build server。

| 原读取或决定 | 当前唯一位置 | 保持的业务语义 |
|---|---|---|
| Module混读Step、Motion.State和Resolved决定Reach准入 | `CharacterFootLifecycle.BuildRequest/AdmitLandingReach` | 原Grounded、事件身份、作者权重阈值、Contact或预测Landing条件；不改变逐腿可达观察 |
| 初步和最终均叫Resolved | 请求`CharacterFootPlacementRequest/Pair`；完成后`Publish`发布`CharacterResolvedFootResult/Pair` | Pelvis读请求；Goal只读完成结果，不增加插值或Pelvis响应次数 |
| 平铺重复的脚几何、支撑权重 | 只读`CharacterFootPlacementPose`与`CharacterFootSupportFacts` | 两个阶段复用同一组值；SupportWeight/SupportIntentWeight内部只有一个Weight，外部旧列继续投影 |
| 临时Foot Goal编码后反解Pelvis脚掌位置 | Foot生产`CharacterFootGoalTarget`；Pelvis直接读其EffectiveSole | 原world→component→world、归一化、Lerp/Slerp及Heel/Toe合成顺序不变 |
| Goal Encoder重新决定有效性与权重 | Foot生产GoalTarget，Encoder仅组装正式Goal | 原Ready、权重阈值及失效时动画姿态保留 |

`Pose.EffectiveAnkle/EffectiveSole`表达原脚目标求解所用的加权规划几何；`GoalTarget.EffectiveSole`表达按正式component目标和权重还原出的脚掌位置。基线中Pelvis姿态偏好使用前者的Ankle，共同高度使用后者的Sole，不能因同名Vector3而互换。本次明确这两个阶段的含义并保留原消费链；没有创建可任选的兼容读取。

本闭环不改变Profile、GroundPath、Contact、Interpolation、Pelvis公式、FBBIK、Bend历史、动画时序或CSV列格式。后续Stride最小输入、完成凭据封装、方向历史、Reset及Editor列绑定仍未完成。

## 验证约束

每个闭环由“采样数据自动测试”任务读取候选提交、上一通过提交和固定总基线，使用同一正式Record。先核对输入、Body、时序与版本身份，再对比实际Foot、Pelvis、Knee、Goal、动画状态、Solved和可用Physical输出；规则及总分只作辅助。不提高容差、改评分、删差异列或重造数据。原包未覆盖的输入、最终Physical Knee和Reset边界不能称为通过。

共享Unity、构建和回放一次只交给一个任务。测试期间停止相关代码写入；另一个产品任务使用Unity时等待明确释放，不抢占或并发Refresh。只提交本任务文件，原始证据不随代码提交。

## 持久字段与Reset清单

以下字段随唯一根Bank由Committed复制到Pending；各业务Owner只写Pending，Root只Seal/Discard，不参与其数学。下表从`2a6fe33`逐字段盘点，并随当前闭环更新；未验证的变更状态以闭环记录为准。

| 记录与字段 | 唯一写入者 | 运行消费者 | 初始化／清理及证据边界 |
|---|---|---|---|
| Landing：LastLanding、NextSwingLanding、PlantTarget、NextSwingReferencePoint、NextSwingPredictionError、TrackedEventIdentity、NextTrackingState、PlantTargetState | LandingRuntime及其Context方法 | 下一帧Prediction、Transition、StateTarget和Support引用 | FootPlacementBank.Reset清空；正式跟踪失效清NextSwing，已Verified的Plant目标不得误清。PromotedLanding、PlantTargetUpdated、PlantVerificationAttempted/Unavailable由BeginFrame清空，是本帧过程证据 |
| Discrete：State、LockResponse | TransitionRuntime.Apply，决定来自TransitionResolver | StateTarget、Interpolation政策及Foot输出生产 | Bank.Reset清零；Pre/Post转换按原序应用。未消费的LastTransitionPhase/Reason已删除，实际转换证据仍由本帧Transition Fact发布 |
| Contact：HasContact、EventIdentity、AcquiredFrameSequence/CompletionIdentity、WorldRevision、SurfaceIdentity、Anchor、Normal | TransitionRuntime按AnchorCommand创建／释放 | StateTarget、Ground约束、Foot输出和下次Transition | Create从正式ContactLanding取值；Release与Bank.Reset清零，不从诊断或最终骨骼反建 |
| ContactTransition：HasPreviousRequest、PreviousRequestedLock/EventIdentity/Mode/Weight、SecondsSinceEdge、LatestContactEventIdentity、LatestReleasedContactEventIdentity、CompletedLockWeightEventIdentity | TransitionRuntime.UpdateContactTransition | 下一帧Contact边沿、完成Lock门控 | 首帧为default；边沿重置计时、事件换代清完成标记；Bank.Reset清零。未消费的LastEdge字段已删除，本帧边沿仍从实际Transition Decision发布 |
| Interpolation路径与连续量：HasOutput、HasSwingPath、SwingLandingEventIdentity、SwingGroundPathInputIdentity、SwingLandingPoint、PreviousTargetCorrection、PreviousSwingTargetCorrection、EffectiveCorrection、SwingResidual、Residual、Progress、StartResidual、Completed、Policy | InterpolationRuntime | 下次插值、HardConstraint读取连续输出、Foot完成资格 | ResetInterpolation与Bank.Reset清空；ApplyPostTransition仅保留原规定的Correction、Response、PreviousOutput与lineage，不增一次推进 |
| Interpolation高度：HasTargetHeight、TargetHeightEventIdentity、FilteredTargetHeightAlongUp、TargetHeightRetargetActive | InterpolationRuntime的高度求值 | 后续Plant目标与连续输出 | ClearPlant／完整Reset按原政策处理；不得被Pelvis、Goal或Ground约束反写 |
| Interpolation接触：HasPlantTarget、PlantTargetEventIdentity/Kind、PlantLockResponse、PlantTargetVerified、PlantDirectFollow、PlantDesiredPoint、PlantFilteredPoint、PreviousPlantSelectedWorldTarget、SelectedSupportTarget、PlantWorldResidual、PlantWorldResidualTransitionActive | InterpolationRuntime | 下一帧目标切换、世界残差与响应 | ClearPlant及ResetInterpolation按原分域清理；PlantFact已从持久State移出，只随本帧InterpolationResult发布 |
| Interpolation响应：HasPreviousResponseOutputPoint、PreviousResponseOutputPoint、ResponseHistory.HasValue/Scalar/Domain/AppliedDirection、HasCorrectionResponseLineage、CorrectionResponseSourceLineage/ProfileRevision/WorldRevision、PendingCorrectionResponseInitializationReason | InterpolationRuntime | 下一帧响应、lineage失效与初始化 | ClearCorrectionResponse清响应及前输出；UpdateCorrectionResponseLineage清失效域；PostTransition保留指定字段。方向与标量归ResponseHistory；Fact仅随本帧结果发布，运行不从Fact读取有效性或方向 |
| PrimarySupport：HasValue、Side、LandingEventIdentity | StrideHipsBuilder.ResolvePrimarySupport | 下一帧支撑保留及本帧Pelvis准备 | 无候选及Bank.Reset调用Clear；Retained已移出持久State，只在本帧Result发布 |
| PelvisSpring：HasValue、SupportSide、SupportLandingEventIdentity、Slope、TargetAlongUp、OutputAlongUp、VelocityAlongUp | StrideHipsBuilder唯一AdvancePelvisResponse | 下一帧响应、支撑交接和释放回零 | Bank.Reset或不产出Pelvis Goal的既有分支Clear；保持3Hz配置及一次积分。Reach观察不得清速度、夹输出或写Spring |
| BendHistory：Left/RightStableDirection、Left/RightAppliedDirection及四个Has标记、SourceCompletionIdentity、Revision | FinalIkFullBodySolver正式求值 | 下一帧退化膝向、有效性与lineage | Root初始化／ResetSolvers及原调参清历史路径清零；参考方向准备不得把Has提前置true。当前空历史仍读Vendor bend.direction，是任务4独立修正点 |
| 帧结果：ResolvedFeet、StrideHips、Pelvis/Left/RightGoal、Diagnostics、FrameSequence、CompletionIdentity | 各阶段唯一生产者，Bank组织发布 | Encoder、Assembler、Solver及Seal后诊断 | Begin清输出，不把前帧结果当新请求。IsPendingFrameOpen只表达Pending开放，Seal置false；Committed可读性继续由根Bank与结果lineage表达 |

PredictionMotion、BodyTrajectory及其Tick/Generation/ResetSequence/AuthorityTick/PredictionMotionRevision/RequestedDuration/Attempt标记继续由既有预测生产者拥有；GroundPath、LandingObservation、CurrentSupport页继续沿正式池Acquire/Release与根事务管理。本次不创建第二缓存、不改变世界查询和Body预测次数。

## 第一个闭环的验证封口

- 候选与下一步接入：`2a6fe3309ddbaf2906ee88ef6758aa077aa47da8`；上一通过／总基线：`ad3527e103cc3235a63e8a1c1dbd26df5155e0ba`。
- 原包：`Diagnostics/FootPlacementRuns/20260901-012513-283-0a4830a755b64a5b9a57fbcd6e8fb32b`。正式samples、geometry、diagnoses沿原发布；仅将会被Temp清理的Proof复制为同目录`replay-proof.json`并核对原字节。
- 官方proof/4直接对233436匹配1044输入；独立对233436与205014的runtime identity、起始Body、Trace/input/body hash、调度和全部1044输入帧均相同。Profile与Program/Projection identity未改变。
- 对两个基线各比较2086×1215：1191业务列逐格字符串完全相同，无浮点容差放宽；23身份列均双向映射无冲突，StartedUtc单列归运行元数据。67186×27几何中22业务列完全相同，5身份列双向映射无冲突。
- Body、正式输入、Foot、Resolved、Pelvis/Stride、Goal、Knee/Bend、实际采到的Solved/Physical及动画时序状态无业务差异。facts71/Analyzer71/d40和42个Target的规则、eligible、matched、occurrence、measurements、coverage与七维评分对象保持，总分仍为61.9，既有问题未被掩盖。
- 覆盖限制：此Record的Action ownership、Contact reentry geometry、BodyReset均为0 eligible，窄Landing腿窗口仅2。CSV有Solved Knee，没有最终Physical Knee；未覆盖入口、路线、调度及最终Physical Knee不能冒充已通过。Reset独立修正尚未实施。
- 测试任务已退出Play，Edit/Idle、未暂停、非编译、Console0，并明确交回Unity独占。共享HEAD后来出现的其它提交不替换候选与总基线；测试确认相关Runtime/Character与Diagnostics仍与候选一致。

## 第二个闭环：收窄Pelvis输入并清除阶段混用

状态：候选`48d7bbcf2d321e4983269f8d40ca828468ae814f`已通过Runtime/Editor编译和本Record正式回放；上一通过为`2a6fe33`，总基线不变。

- `CharacterFootStrideRequest`只保存正式Swing资格、Step Event、可用Landing的点／Event和Path接受事实；原始Step与GroundPathLanding不再进入Stride求值。删除恒等于CurrentStep的SelectedStep副本，Reach仍读取同一正式CurrentStep。
- `PreparePelvis`只投影原有Support可用条件、加权踝点、位置权重、两腿Reach和既有帧输入；`ResolvePelvis`仅接收`CharacterFootPelvisInput`，不再访问完整脚请求。拒绝顺序、支撑选择、姿态区间和唯一Spring数学不变。
- 完成凭据迁入`CharacterFootLifecycle.Completion`，过程字段全部私有；它自己消费本腿可达结果并执行原完成步骤。Module不能读取LandingCompletionPending、PreTransition或Interpolation等凭据内部数据，也不再取得随后会被覆盖的初步Motion。
- Prediction结果不再携带Goal或最终Motion。删除无消费者的WithLiveStep及重复WithFootMotion拷贝；诊断分别读取同一帧的Prediction、最终Motion、Resolved与正式Goal。只读记录的GroundPath更新用值复制，保留原内容。
- 删除初始零权重Foot/Pelvis Goal及其跨PredictFootPair/PredictEvent/RejectedEvent的传递；Encoder只在Foot完成后从最终Resolved编码。删除未进入插值的PreviousVisibleOutput参数、可见Sole Bank字段及其读取函数，不启用旧Goal Sole接管。
- Editor及其Runtime依赖按规定构建成功、0错误，build server立即关闭；包及既有工程警告不在本次范围。未改Profile、Solver、历史响应、世界查询、诊断列或评分。

## 第二个闭环的验证封口

- 原包：`Diagnostics/FootPlacementRuns/20260901-014135-409-d6745dcc70e54adb87ec807c6594b566`，正式输出与同目录字节一致的`replay-proof.json`已保留，数据未提交Git。
- 官方proof/4直接对012513匹配1044输入；独立对012513、233436、205014的输入、起始Body、runtime identity、时序及全部1044帧均无差异。
- 三组对照均2086×1215：1191业务列逐格字符串相同，23身份列双向映射无冲突、StartedUtc归运行元数据。67186×27几何的22业务列相同，5身份列双向映射无冲突；Foot/Pelvis/Knee/Goal/动画时序和实际采到的Solved/Physical无业务差异。
- facts71/Analyzer71/d40、42个Target的规则/eligible/matched/occurrence/measurements/coverage及七维评分保持，61.9不变；没有调整容差、评分或列。Action/reentry/BodyReset零覆盖、窄Landing腿窗口仅2、未采最终Physical Knee的限制不变。
- 测试任务已退出Play并交回Unity，Edit/Idle、未暂停、非编译。下一步以48d7bbc作为上一通过提交，固定总基线仍是ad3527e。

## 合同同步

首两个闭环通过后，current Foot的Resolved/Pelvis条款已同步请求→Pelvis→原完成→最终结果顺序，故本change删除重复RENAMED动作而保留同名MODIFIED差量。project与stabilize取消旧硬Reach/夹脚保证，stabilize删除重复的Resolved/Pelvis差量。可靠有符号膝向与其它未完成Contact/Goal Sole工作保留，没有将未完成的Reset或诊断绑定提前写成current事实；也没有自动归档。

## 第三个闭环：运行历史与本帧证据分离

状态：候选`514d9b5d21a86aa74c0a6a94653d576e95e22bcf`已通过Runtime/Editor编译及本Record正式回放；上一通过为`48d7bbc`，固定总基线不变。

- `CharacterFootCorrectionResponseHistory`只保存HasValue、Scalar、Domain、AppliedDirection。旧HasCorrectionResponse与Fact.Evaluated的所有生产／清理入口原本始终同写同清，现在由一份有效性表达，不新增状态判定或方向限制。
- `ApplyCorrectionResponse`返回typed位置／方向结果及只读Fact；Plant、Release、Swing只读正式结果的AppliedDirection，不再从解释记录反读。CorrectionResponseFact与PlantFact从Interpolation持久State删除，仅随本帧InterpolationResult返回。
- PostTransition继续保留同一响应历史、前输出及lineage；ResetInterpolation、ClearCorrectionResponse与根Bank Reset按原边界清空。未执行响应的内部路径只返回未求值证据，仍保留实际历史；原来这些路径携带的旧Fact没有成为正式输出，不新增可见行为。
- 删除三个调用恒为false的旧VisibleOutputTransfer参数及不可达分支，诊断原有Transferred列仍按原语义为false，不启用Goal Sole历史接管。
- PrimarySupport改为State命名，只持久化HasValue/Side/Event；Retained只随本帧选择结果发布。删除无任何消费者的LastTransitionPhase/Reason与LastEdge字段。Pending标志改名IsPendingFrameOpen，Root仍只关闭和提交，不新增数学。
- 本步未触碰FBBIK、Vendor方向、Profile或算法常量，普通Reset语义保持；Solver空历史方向修正仍作为后续独立行为提交。

## 第三个闭环的验证封口

- 原包：`Diagnostics/FootPlacementRuns/20260901-015751-281-77de9a9ff4d74a97a47e922ebb4666bb`，正式数据与同目录持久Proof已保留，未提交原包。
- 官方proof/4直接对014135匹配1044输入；独立对014135、233436、205014的runtime identity、起始Body、输入／Body hash、驱动和全部1044帧无差异。
- 三组对照均2086×1215，1191业务列逐格字符串相同，23身份列双向映射无冲突，StartedUtc为运行元数据。几何67186×27的22业务列完全相同，5身份列双向映射无冲突。
- Response初始化／分域／Previous／Current／方向／连续历史，以及Foot、Pelvis、Knee、Goal、全部已采Solved/Physical和时序状态均无差异；facts71/Analyzer71/d40、42个Target的规则和统计、coverage与quality保持，61.9不变。
- Action/reentry/BodyReset零覆盖、窄Landing仅2、未采最终Physical Knee及完全Reset配对未覆盖的限制继续保留。此次只证明历史分型不改变已覆盖行为，不证明后续Solver Reset修正。
- 测试任务已回Edit/Idle、未暂停、非编译、Console0并归还Unity；下一小步的上一通过提交为514d9b5。

## 第四个闭环：独立修正Solver空历史方向

状态：候选fc00789的实现及Runtime/Editor编译、普通Record回归已通过；完全Reset配对尚未验证，4.4保持未完成。

- 在现有Rig参考姿态`PrepareReferencePose → Prepare → SetToIndexedReferences`中，Vendor已经按原`IKConstraintBend.Initiate`算法生成精确方向后，捕获只读`CharacterFullBodyIkBendReference`。它携带Rig Id/Revision与左右方向，替代原分散的Rig身份字段；初始化合法性只在此处检查。
- `ResetLegBendState`统一从正式参考记录恢复Vendor方向及原权重，初始化、Reset与清历史调参共用该入口；不重新构造第二个Solver，不新增默认轴、方向估算或Profile配置。
- 空Stable历史时只用传入的正式参考方向；每帧计算后再写Vendor工作字段。可靠动画的有符号方向运输、Stable/Applied含义、退化投影、历史翻号条件和Bend权重表达式保持。
- 参考记录不设置任何BendHistory Has标志。没有改变Root清历史的触发范围，也没有把Foot Reset扩大为Solver Reset。未消费的public Prepare入口收回私有，参考准备只从同一Rig入口发生。
- 已知验证缺口：现有Record只新建Runtime，BodyReset为0；现成MCP没有同一Solver预热→完整Reset→同一完整Pose/Goal输入配对能力。已有Preview Seek会走正式Reset，正在核对其Watch/Trace能否证明完整输入与空历史，未增加测试、反射或第二驱动。正常回放通过也不代表该边界已覆盖。

## 第四个闭环的普通帧回归与未完成边界

- 原包：`Diagnostics/FootPlacementRuns/20260901-021236-922-e66abf5ba5c347168d24274f131593c9`，同目录Proof已原字节保留。
- 官方Proof对015751匹配1044输入；独立对015751、233436、205014输入、起始Body、runtime identity、时序及全部1044帧相同。CSV1191业务列逐格相同、23身份列双向映射无冲突、StartedUtc归元数据；几何22业务列相同、5身份列映射无冲突。42个Target及全部规则／统计／coverage和七维评分对象保持，总分61.9。
- 此结论仅证明fc00789没有改变原Record所覆盖的普通帧；没有执行额外Reset/调参/Preview。4.4仍未完成，原有未覆盖限制不变。测试任务已回Edit/Idle并归还Unity。
- 输入取证审计已更正：已有Pose Watch可以读取完整FBBIK前Pose与正式GoalSet，不需要重复生产它们。缺少Bend入口before标记、参考来源和Reset代次，以及现有Preview观察组合的操作/导出接入。通用脚本工具Roslyn不可用、CodeDom命令过长，未做替代编译或私有反射。待决定方案见reset-observation-gap.md。

## 第五个闭环：先统一Resolved字段组的读写绑定

状态：候选bb25738已通过Unity／Editor编译和原Record回放；没有改Runtime、列格式或版本。

- 新建唯一Editor typed列绑定基础，字段声明同时提供原始列名、CLR值类型、单位、业务组、可用性引用、Runtime证据getter和解析记录setter。标量／Vector／Quaternion沿同一CSV codec读写；配置完整性在初始化检查，文件列索引在读文件时绑定一次。
- Resolved Foot先迁移57个typed binding／98个原始CSV列，静态逐项核对顺序与原Header完全一致。删除该组旧Header字面量、AddResolvedFoot写行、ParseFrame赋值与RequireColumns清单；没有同组新旧读写并存。
- Editor解析记录将Resolved聚合为一份CharacterFootResolvedSample，SupportTarget记录提取为共享typed记录；诊断公式只改字段路径，规则／评分表达式不动。全部字符串转义、数值R格式和解析失败规则从原实现原样迁入唯一共享CSV Values。
- 仍使用原Sampler/Analyzer/Publisher和原始帧字节索引；几何仍为独立表，未恢复facts.json。facts71/Analyzer71/d40保持。其它尚未迁移字段组仍在原位置，后续逐组替换并删除各自旧映射；任务5整体尚未完成。

## 首组列绑定的验证封口

- 原包：`Diagnostics/FootPlacementRuns/20260901-024845-141-603a49a924ca4398aa453f9aaa230e8b`，候选bb25738，上一通过fc00789/021236，总基线ad3527e/233436不变。
- 官方Proof对021236匹配1044输入；独立对021236、233436、205014输入及完整1044帧相同。三组CSV2086×1215列名／顺序相同，1191业务列逐格一致，23身份列双向映射无冲突、StartedUtc归元数据。Resolved98列全部维持原格式与值；几何22业务列及5身份映射保持。
- facts71/Analyzer71/d40、42个Target及规则／统计／coverage、七维评分61.9保持。20447条details的连续offset/length、SHA及id/family逐条通过；samples2086段与geometry1787段字节长度和SHA通过。
- 正式MCP summary、contact-support-gap events、detail3033与实际JSONL一致；3L/3R/476R/746L/1045L/1045R的98列帧查询，以及geometry92L的27列24行，与独立原始数据逐值一致。没有重分析或第二报告。
- 测试已回Edit/Idle并归还Unity。这里只确认合法原包与查询链，未制造坏CSV负样本；Reset4.4和已有未覆盖输入限制仍保留。

## 下一组：Current Support与共享支撑目标

状态：候选a9105e3已通过Unity／Editor编译与原Record回放。

- Current Support新增102列完整typed绑定，脚掌两Probe复用同一形状；Selected Support Target的22列也改由同一绑定读写，三个SupportTarget场景共享一个字段定义。
- 增加静态typed投影来组合父记录与子记录，不复制字段清单、不重新查询或生成支撑决定。删除相应旧Header、写入helper、局部解析函数和必需列字符串。
- Analyzer的Current Support聚合为一个记录，诊断规则只改读取路径。全局共222列已迁入唯一绑定，剩余字段仍按组继续迁移；不提前完成任务5。Runtime、格式identity、规则与评分未变。

## Current Support列绑定的验证封口

- 原包：`Diagnostics/FootPlacementRuns/20260901-030817-973-b2daa7ebff2746e4918c8360da6edbc9`，候选`a9105e3dd6d8a82bf484e74092bd389e343e0606`，上一通过bb25738/024845，固定总基线不变。
- 官方Proof对024845匹配1044 Tick；独立对024845与233436的runtime identity、起始Body、输入／Body hash、时钟及全部1044帧无差异。
- CSV2086×1215列名与顺序保持，1191业务列逐格一致、23身份映射无冲突、StartedUtc归元数据；几何67186×27的22业务列一致、5身份映射无冲突。CurrentSupport102、SelectedSupportTarget22与Resolved98合计222列保持。
- 42个Target、规则／统计／coverage／quality对象保持；20447条明细索引与全部2086 samples段、1787 geometry段字节校验通过。正式summary、events、detail3033、六个222列帧查询与geometry92L查询均与原始数据一致。
- 原始包及字节一致的同目录Proof已保留，无重复报告或Git数据提交；测试已回Edit/Idle、Console0并归还Unity。仅确认本Record合法输入范围，Reset4.4与坏CSV等未覆盖项保持未完成。

## 当前接续点

运行与Editor普通Record的上一通过提交为a9105e3；总基线仍为ad3527e。剩余工作为：完成其余993列的唯一绑定和诊断分组、统一现有格式identity、清理文件与剩余旧字段并逐闭环回放；Reset取证扩展等待用户决定。第一阶段未完成前不启动Pose Graph。工作区原有.gitignore、ProjectSettings、project.md与stabilize提案的未提交变更继续保留，不夹带提交。

## 脚步候选、阶段与正式事件列

状态：候选8026fcb已完成实际Replay及全部列／查询验证；最初问答中断后的turn完成不算通过，以下033553才是本候选真实证据。

- Current/Incoming候选各19列及Selected Phase的5列由共享阶段定义驱动；保留原Selected Source条件，没有改变选择规则。
- 输入与输出Formal Event各20列共用一次正式事件准备和同一事件字段形状。保留原available与IsValid条件、未绑定Event的零Identity和无效时的空/零输出；以前未被规则使用的列也具备typed读写绑定。
- 删除这83列的旧Header、写入helper、局部解析函数和必需列清单。读取端用按文件创建的CharacterFootSampleReadBindings汇聚一次列索引绑定，不再随分组增加ParseFrame参数。
- 累计305/1215列已迁移，Runtime、格式identity和评分规则未改。下一验证同时比较a9105e3/030817与固定ad3527e/233436；Reset4.4继续未完成。

## 候选／正式事件列的验证封口

- 原包：`Diagnostics/FootPlacementRuns/20260901-033553-950-371118784cce4c25812efa74b4b15a57`，候选8026fcb；一次精确Record回放完成1044输入。最初验证任务因诊断问答结束时尚未发出Replay，已显式纠正，没有借用旧包。
- 对上一a9105e3/030817与固定233436：全部1215列顺序一致，1191业务列逐格一致、23身份列双向映射无冲突、UTC独列；新83列全部值一致，累计305列为298精确列和7既有身份列。几何22业务列及5身份映射保持，Proof输入／Body／时钟及1044帧一致。
- 42Target、规则／统计／coverage与quality保持；305列的六个实际帧查询、geometry查询及全部20447明细索引、原始分段校验通过。普通Record下没有非身份差异，未补Reset4.4或其它未覆盖输入。
- 测试任务已明确归还Unity；下一闭环普通行为对照为8026fcb，固定总基线仍是ad3527e。

## Pelvis字段组

状态：候选6ba9ec3已通过实际Replay、133列及1043个完整Pelvis事实投影对账。

- 将Pelvis的133列收进87个typed绑定，包含原Height Target、Posture、Reach、Response及对应Goal/Physical观察；额外保留过去未供规则使用的五个原始Stride字段，没有新增业务规则。
- 原解析记录移出巨型Analyzer并保持分组，Height Target的语义检查和事实投影仍由Analyzer执行，原公式、顺序与容差不变。StrideSpringOutput的重复解析存储已删除，全部消费者读取同一Response.Output。
- 字符串枚举／flags检查由共享CSV Values提供，保留原ToString、分隔符及失败语义。累计438/1215列迁入绑定，仍不代表任务5完成。

## Pelvis列绑定的验证封口

- 原包：`Diagnostics/FootPlacementRuns/20260901-034923-038-6b0960f4ec434f93b0ae44f2f91f4ab0`，候选6ba9ec3；一次原Record完成1044输入，独立对033553和固定233436的输入／Body／时钟及帧数组无差异。
- CSV1215列顺序相同、1191业务列逐格相同，身份映射无冲突。133个Pelvis列全部精确一致，累计438列为431精确与7身份列。几何22业务列、5身份映射保持。
- 特别对账1043个pelvisFrames的完整Observation/HeightTarget/Posture/Reach/Response JSON，两个基线均differentFrames=0。42Target、规则、统计、coverage与quality保持，61.9不变。
- 六个438列真实帧查询、geometry查询、20447条明细索引、原始文件全部分段校验通过。原始数据与持久Proof保留，测试已归还Unity。Reset与原有未覆盖限制不变。

## Solver与Physical列绑定

状态：候选ee0d95b已完成实际Replay与全部正式明细投影对账。

- 将尾部Solver/Physical的93列接入55项typed绑定；捕获来源仍是原FootIkCapture与原世界坐标/Heel/Toe/残差计算。代码只改变数据投影与读写位置，不新增求解、查询或骨骼写入。
- 解析记录独立保存Solver要求／求解结果和Physical实际写入字段，原有规则只调整读取路径；以前仅写入而未参与规则的原始列也保留typed解析，不新增评分规则。
- 删除对应旧Header、写行和解析/必需列映射。累计531/1215列迁入绑定，格式identity不变，任务5仍未完成。

## Solver与Physical列的验证封口

- 原包：`Diagnostics/FootPlacementRuns/20260901-040120-840-fb1fb97ba68148b09864a59c80b8eed8`。测试任务明确通过并归还Unity；1044输入、2086脚行和1215列保持，对上一6ba9ec3/034923与固定233436比较。
- 新93列全部精确相同，累计531列为524精确列及7既有身份列。全部1191业务列、几何业务列、42项诊断、覆盖与评分保持；20447条明细仅21条既有身份字段路径换代，实际帧与明细查询保持一致。Reset4.4及原有未覆盖项不变。
- 根任务另读040120与233436原始CSV核对：只有原23身份列和UTC不同；samples SHA256为`16bbe67187b2e7e412868237b4e427e50febcd8f7fcd5a777592c45f0b29986a`，geometry为`dd50143eddfa258079bfacf458fc32ccac7b8faf7f3dc3d5fe6a0e358acb1e8b`。

## 响应与接触列绑定

状态：实现、Unity与Editor构建通过，0错误，build server已关闭；等待实际回放。

- 将81个响应与接触列收进55个typed绑定，包含Plant目标高度、前后输出、世界残差及方向响应。过去的本帧证据读取改为同一Response记录，未修改运行历史、算法、规则或格式identity。
- Header、CSV写入、解析和必需列检查共用该组；PreviousResponseOutput和DeadlineHalfLife明确关联原有效性列，读写不屏蔽原值。
- 累计612/1215列迁入绑定；比较点为上一ee0d95b/040120和固定ad3527e/233436，任务5尚未完成。
