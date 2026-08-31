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

以下字段随唯一根Bank由Committed复制到Pending；各业务Owner只写Pending，Root只Seal/Discard，不参与其数学。名称以首个候选`2a6fe33`为准，迁移后同步本表。

| 记录与字段 | 唯一写入者 | 运行消费者 | 初始化／清理及证据边界 |
|---|---|---|---|
| Landing：LastLanding、NextSwingLanding、PlantTarget、NextSwingReferencePoint、NextSwingPredictionError、TrackedEventIdentity、NextTrackingState、PlantTargetState | LandingRuntime及其Context方法 | 下一帧Prediction、Transition、StateTarget和Support引用 | FootPlacementBank.Reset清空；正式跟踪失效清NextSwing，已Verified的Plant目标不得误清。PromotedLanding、PlantTargetUpdated、PlantVerificationAttempted/Unavailable由BeginFrame清空，是本帧过程证据 |
| Discrete：State、LockResponse | TransitionRuntime.Apply，决定来自TransitionResolver | StateTarget、Interpolation政策及Foot输出生产 | Bank.Reset清零；Pre/Post转换按原序应用。LastTransitionPhase/Reason说明最近转换，不作为第二个State |
| Contact：HasContact、EventIdentity、AcquiredFrameSequence/CompletionIdentity、WorldRevision、SurfaceIdentity、Anchor、Normal | TransitionRuntime按AnchorCommand创建／释放 | StateTarget、Ground约束、Foot输出和下次Transition | Create从正式ContactLanding取值；Release与Bank.Reset清零，不从诊断或最终骨骼反建 |
| ContactTransition：HasPreviousRequest、PreviousRequestedLock/EventIdentity/Mode/Weight、SecondsSinceEdge、LatestContactEventIdentity、LatestReleasedContactEventIdentity、CompletedLockWeightEventIdentity | TransitionRuntime.UpdateContactTransition | 下一帧Contact边沿、完成Lock门控 | 首帧为default；边沿重置计时、事件换代清完成标记；Bank.Reset清零。LastEdge是本帧证据 |
| Interpolation路径与连续量：HasOutput、HasSwingPath、SwingLandingEventIdentity、SwingGroundPathInputIdentity、SwingLandingPoint、PreviousTargetCorrection、PreviousSwingTargetCorrection、EffectiveCorrection、SwingResidual、Residual、Progress、StartResidual、Completed、Policy | InterpolationRuntime | 下次插值、HardConstraint读取连续输出、Foot完成资格 | ResetInterpolation与Bank.Reset清空；ApplyPostTransition仅保留原规定的Correction、Response、PreviousOutput与lineage，不增一次推进 |
| Interpolation高度：HasTargetHeight、TargetHeightEventIdentity、FilteredTargetHeightAlongUp、TargetHeightRetargetActive | InterpolationRuntime的高度求值 | 后续Plant目标与连续输出 | ClearPlant／完整Reset按原政策处理；不得被Pelvis、Goal或Ground约束反写 |
| Interpolation接触：HasPlantTarget、PlantTargetEventIdentity/Kind、PlantLockResponse、PlantTargetVerified、PlantDirectFollow、PlantDesiredPoint、PlantFilteredPoint、PreviousPlantSelectedWorldTarget、SelectedSupportTarget、PlantWorldResidual、PlantWorldResidualTransitionActive | InterpolationRuntime | 下一帧目标切换、世界残差与响应 | ClearPlant及ResetInterpolation按原分域清理；PlantFact只解释本帧，不能反向控制状态 |
| Interpolation响应：HasPreviousResponseOutputPoint、PreviousResponseOutputPoint、HasCorrectionResponse、CorrectionResponse、CorrectionResponseDomain、HasCorrectionResponseLineage、CorrectionResponseSourceLineage/ProfileRevision/WorldRevision、PendingCorrectionResponseInitializationReason | InterpolationRuntime | 下一帧响应、lineage失效与初始化 | ClearCorrectionResponse清响应及前输出；UpdateCorrectionResponseLineage清失效域；PostTransition保留指定字段。当前CorrectionResponseFact.Evaluated/ResponseDirection仍被运行读取，后续任务3将其迁为正式方向历史，不能直接删除 |
| PrimarySupport：HasValue、Side、LandingEventIdentity | StrideHipsBuilder.ResolvePrimarySupport | 下一帧支撑保留及本帧Pelvis准备 | 无候选及Bank.Reset调用Clear；Retained仅解释本帧选择，仍随Result发布 |
| PelvisSpring：HasValue、SupportSide、SupportLandingEventIdentity、Slope、TargetAlongUp、OutputAlongUp、VelocityAlongUp | StrideHipsBuilder唯一AdvancePelvisResponse | 下一帧响应、支撑交接和释放回零 | Bank.Reset或不产出Pelvis Goal的既有分支Clear；保持3Hz配置及一次积分。Reach观察不得清速度、夹输出或写Spring |
| BendHistory：Left/RightStableDirection、Left/RightAppliedDirection及四个Has标记、SourceCompletionIdentity、Revision | FinalIkFullBodySolver正式求值 | 下一帧退化膝向、有效性与lineage | Root初始化／ResetSolvers及原调参清历史路径清零；参考方向准备不得把Has提前置true。当前空历史仍读Vendor bend.direction，是任务4独立修正点 |
| 帧结果：ResolvedFeet、StrideHips、Pelvis/Left/RightGoal、Diagnostics、FrameSequence、CompletionIdentity | 各阶段唯一生产者，Bank组织发布 | Encoder、Assembler、Solver及Seal后诊断 | Begin清输出，不把前帧结果当新请求。HasFrame当前表达Pending开放；Seal置false，不能复用为Committed有效性 |
| Visible Sole：HasVisibleFootOutputs、Left/RightVisibleSole | Foot最终GoalTarget几何发布 | 旧PreviousVisibleOutput路径，当前插值实际不接管 | Begin/Reset清；本change不因清理HasFrame而启用旧历史接管，后续删除无消费参数或明确结果有效性 |

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
