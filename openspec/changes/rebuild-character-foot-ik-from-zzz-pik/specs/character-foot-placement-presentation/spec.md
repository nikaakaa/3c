# character-foot-placement-presentation Specification Delta

## MODIFIED Requirements

### Requirement: Foot Placement必须是唯一Goal事务

唯一`CharacterPoseConstraintRuntime` MUST继续为每个Actor和表现帧建立匹配Frame、Completion、Program、Projection、Rig与Tuning Generation lineage的Pending Constraint Bank，并唯一拥有Foot持续状态、Resolved Foot Pair、Pelvis、Goal Contribution、唯一Goal Assembler、唯一Goal Set、FBBIK BendHistory与Solver Result。Final Pose物理页、Physical Writer和Physical Result MUST只属于`CharacterFinalPosePublication`。在线调参 MUST只进入actor-local Constraint Tuning Snapshot，不修改Program Image、Execution View或其它Actor。

`CharacterPoseProgramRuntime` MUST在Program Image中Foot Placement、PoseBone Contribution、Goal Assembler与FBBIK各自Operation的位置，通过对应typed编译Handle恰好调用一次Constraint Runtime入口并写入唯一per-operation completion。Constraint Module MUST不扫描Program、不维护第二份Stage Schedule、不接收NativeSlice、Goal offset/count、Operation index或call-site index，也不重新执行已完成Operation。Complete MUST只验证完整闭包并发布一个Constraint Result。

Foot Placement MUST在唯一深模块内执行完整的AMLegIK输入消费、普通／预测查询、脚目标、逐脚收尾和骨盆计算，发布一个Foot Placement Result。外部调用方 MUST不编排内部阶段或取得可变子状态。原逐脚控制参数写入 MUST先编码到Pending结果，全部脚、骨盆、Goal、Solver和Final Publication完成后再统一Seal；不得发布第二Goal Set、Pelvis、Solver、Final Pose或Writer。

本次完整复刻 MUST替换原3C预测、接触五态、世界残差和骨盆响应的行为保持承诺。保留的系统边界 MUST不被解释为保留原Foot数值算法；复刻未完成不得以旧算法补齐并标为成功。

#### Scenario: 正常生成Foot Placement结果

- **WHEN** 同一表现帧具有合法动画姿态、区域输入、Owner／World、Rig和原算法参数
- **THEN** 唯一Foot Operation MUST依原顺序生成左右脚及骨盆控制结果和唯一completion
- **AND** 后续Assembler与Solver MUST消费同一Bank的三个Goal Contribution

#### Scenario: 重复执行Foot Placement

- **WHEN** 同一Frame与Completion重复请求同一Foot Operation
- **THEN** Program Runtime MUST拒绝重复完成并阻止本帧Final Publication
- **AND** MUST不覆盖第一次结果或建立第二Foot事务

#### Scenario: Constraint完成后发布Final Pose

- **WHEN** Foot、Goal Assembler与Solver已形成合法Constraint Result
- **THEN** Program Output MUST只通过正式Publication binding写入唯一Pending物理Pose页
- **AND** Constraint Bank MUST不保存第二Final Pose副本或Physical Writer

#### Scenario: 当前IK仍有未完成改进

- **WHEN** 原算法仍有未完成翻译、输入恢复或消费对账
- **THEN** Runtime和实施记录 MUST保留精确未完成项，不以旧3C算法冒充该原函数
- **AND** MUST不恢复已否决的3C Reach硬夹紧、末端夹脚或无消费证据的SmoothKnee补丁

### Requirement: Foot Lifecycle必须生成唯一权威结果

每脚 MUST只拥有一份按原AMLegIK规则更新的PredictState、FootLockInfo和必要响应历史，全部属于同一根Bank。区域持久位、一次性事件、预测启用、脚锁记录、脚目标及骨盆状态 MUST保持各自原语义，不由旧Swing／Landing／Locked／Releasing状态重新裁决。

每个字段 MUST有唯一写入阶段。区域应用 MUST按原语句顺序处理状态和remainTime；查询与目标阶段 MUST只改写其原有记录；Finalize MUST只推进原计时、缓存和事件清理。历史换代标识只能用于lineage和诊断，不得形成原版之外的状态选择器。

#### Scenario: 内部责任拆分

- **WHEN** 输入、查询、脚响应、骨盆和收尾由不同内部模块实现
- **THEN** 相同输入与上一Committed状态 MUST只产生一份权威脚状态和一份最终输出
- **AND** 任一模块 MUST不能独立Seal、写另一模块的历史或绕过根事务

#### Scenario: 整帧Discard

- **WHEN** 脚状态和Finalize已在Pending页更新，但后续骨盆、Goal、Solver或Writer失败
- **THEN** 上一Committed状态、参数、计时、查询缓存和控制结果 MUST全部保持不变
- **AND** 下一帧 MUST不能读取失败帧的记录

### Requirement: Foot Placement配置与Rig必须显式

FootPlacement MUST显式引用唯一Profile、Rig Calibration、完整脚区域输入和Owner／World绑定。Projection、Profile、Calibration、Rig及Animation Rig Binding的identity和revision MUST一致；原版实际消费的默认参数、当前状态参数、静态配置和一次性命令 MUST有明确生产者。

原Foot／Toe／Pelvis坐标、绑定旋转、Owner Transform和Rigidbody MUST独立核对。配置缺失、原区段缺失或身份不匹配 MUST在Build／绑定边界报告具体缺项，不从旧分析曲线、对象名称、另一角色、默认值或旧Projection补齐。

#### Scenario: Projection与Calibration不匹配

- **WHEN** Projection与运行资源的identity或revision不一致
- **THEN** Runtime创建 MUST失败并报告具体资源
- **AND** MUST不继续使用旧Goal

#### Scenario: Corin原区段尚未恢复

- **WHEN** 已配置的Walk或Run状态只有区段列表头而没有完整内容
- **THEN** 编译 MUST报告该状态的原输入缺失
- **AND** MUST不生成推测区段并标为原版完整配置

### Requirement: Foot Placement诊断必须只显示正式结果

Runtime Result MUST与Diagnostics严格分型。Constraint Module MAY按Frame开始冻结的interest，从Pending Context、Observation、Resolved Result和Constraint阶段Result单向深冻结原区域输入、PIK模式权重、普通／预测查询、PredictState、FootLockInfo、方向／标量历史、脚控制量、骨盆、Goal与Solved结果；这些事实只能进入`CharacterPoseConstraintCommittedResult`。Physical Write与最终Physical Bone结果 MUST只由Final Publication冻结进`CharacterFinalPosePublicationCommittedResult`。

Gizmo、Trace与Pose Watch MUST只读取各自允许的Committed页。Foot采样 MUST在成功Seal后的同步Commit调用栈内，从相同Frame、Completion、Program、Projection、Rig和Actor lineage的Constraint、Final Publication与当前Source已提交状态直接取得Left／Right与公共Fact Root，并以`in`执行一行target-scoped `DiagnosticEvent` partial调用；MUST不先组合或消费`CharacterFootIkCommittedCaptureViewLease`、Runtime Snapshot、Dimension View、Consumer／Binding或第二事实页。帧开始的可选partial Query只在匹配target订阅时要求Physical Writer冻结真实Physical Ankle事实；未订阅和Disabled构建不得执行该读取。`character-foot-ik-diagnostic-sampling`只提供字段／Sampler／Program Definitions与Editor workflow；`generated-diagnostic-sampling-framework`只拥有生成程序、typed packet、Capability Session与Writer／Reader。PoseGraph与Constraint不得拥有任一下游编译器、Schema、packet或Host知识。旧Foot单体Analyzer／Publisher、Diagnosis Store、旧CSV与历史兼容Reader直接删除；独立Foot Analysis只在Completed Artifact之后执行当前Plan、Operator、评分和报告。Diagnostics MUST不查询世界、修改Context、选择Support、生成Goal、执行FBBIK、读取未冻结Physical Transform反推结果或把Constraint与Physical事实写回同一业务Bank。

#### Scenario: 捕获正式Foot事实

- **WHEN** Foot、Pelvis、Goal、FBBIK、Pending Pose与Physical Writer均成功提交
- **THEN** Foot采样 MUST从同一lineage的Constraint、Final Publication与当前Source已提交状态直接传入可对账冻结基线的正式事实；Live／Trace Projector MAY独立发布只读View
- **AND** Diagnostics页归属变化 MUST不改变Runtime Result、Final Pose或Physical Writer输入

#### Scenario: Writer失败

- **WHEN** Constraint Result已经完成但Final Publication在Physical Writer前或Writer中失败
- **THEN** Diagnostics MUST不发布本帧Pending Constraint或Physical结果
- **AND** Projector MUST不为Foot Capture借用上一帧或发布第二Snapshot；Live／Pose Watch只能按各自既有合同保留上一Committed事实或正式Actor Fault

## ADDED Requirements

### Requirement: Foot结果必须形成紧凑的原控制输出

最终Resolved Foot MUST表达同帧原脚目标计算和控制参数编码完成后的结果，只发布下游实际消费的Frame、Completion、Rig、Side、位置、旋转、明确分型的附加标量／可见权重、输出有效性及来源identity。原始命中、预测中间量、离散状态与内部历史 MUST留在所属模块，不复制为第二业务输入。

原Foot控制位置与项目Sole／Ankle几何 MUST通过显式Rig绑定转换，不能把原footprint、控制参数位置、Weighted Goal或最终物理脚底混称同一个点。CalculateFootTarget已应用的基准混合 MUST不在Goal编码或Solver输入重复应用。

最终Pair MUST只组合相同Frame、Completion与Rig的两脚；Foot Result MUST在原骨盆阶段完成后发布。旧3C Landing完成资格、233436连续性及Contact Ownership进度 MUST不再作为新输出准入或数值保持条件。

#### Scenario: 初步脚结果尚未完成整帧计算

- **WHEN** 两脚控制目标已形成但骨盆及模块完成检查尚未结束
- **THEN** Runtime MUST只保留内部Pending脚结果
- **AND** 外部Goal消费者 MUST不能取得它作为正式Foot Result

#### Scenario: 原算法产生超出当前Solver可达范围的目标

- **WHEN** 原输入与计算合法而项目Solver观察到目标不可达
- **THEN** 系统 MUST分别保留原目标、编码结果和Solver事实
- **AND** MUST不恢复旧末端夹脚、额外骨盆位移或隐藏权重来制造等价

#### Scenario: 正常输出保持

- **WHEN** 相同原输入、历史、查询记录和参数重复求值
- **THEN** 输出和状态更新 MUST遵循原算法；只有原算法与未修改的系统边界要求保持一致
- **AND** MUST不再以旧3C目标／连续性数值不变作为完整复刻条件

### Requirement: 完整复刻必须具有逐函数与逐输入证据

完整复刻 MUST以同构建原始指令、真实元数据和已核验输入为依据，覆盖AMLegIK的核心算法与必要直接输入／消费者。所有原方法 MUST登记为算法迁移、正式宿主适配、只读诊断或明确非业务运行壳，并记录来源与验证状态。原始函数体已存在但尚未翻译 MUST标为未完成，不得以项目近似函数替代。

样本的字段身份、采样范围与可复算范围 MUST分别保存。轮询行、单帧快照、原始未定型块和完整调用输入 MUST不能混作同一种证据。物理输出等价 MUST另行核对原生控制语义与Solver配置。

#### Scenario: 原函数尚未完成离线翻译

- **WHEN** HitGroundImpl、PredictIkHitGround、DoCalculateTarget或Pelvis计算的指令已存在
- **THEN** 实施 MUST继续完成该函数分支、常量、操作数和输出对账
- **AND** MUST不把旧3C几何／预测／Spring标为它的完成实现

#### Scenario: 采样存在但输入不同步

- **WHEN** 多列来自字段轮询且缺少同次调用的全部操作数
- **THEN** 结果 MUST只声称可证明的字段／窗口关系
- **AND** MUST不把未覆盖分支或最终骨骼视为已通过

### Requirement: 区域输入必须复刻原区段选择和时间生产

脚区域 MUST使用原状态、区段表、帧数、时长、播放进度、滑动位、GroundPositionL和回调顺序。当前区段 MUST按原列表顺序和闭区间选择，保留零长度区段；未来边界 MUST同时比较起点与终点的循环距离，相同距离保留原枚举顺序。

remainTime MUST按当前／下一状态及停止帧的原公式生成。OnFootPlant MUST先消费进入时的旧remainTime和time，随后同一区域求值才能写新remainTime及脚点。多个状态或区域的命令 MUST由原区域选择和顺序规则解决，不把两个布尔输入插值或任意选dominant source。

#### Scenario: 单帧区段

- **WHEN** StartFrame等于EndFrame且当前原归一化进度命中该端点
- **THEN** 该段 MUST作为合法区段参与原选择
- **AND** MUST不因持续时间为零而删除

#### Scenario: 快速重入比较

- **WHEN** 单脚重新进入区域
- **THEN** OnFootPlant MUST先比较进入时已有的remainTime和time
- **AND** 本次随后计算的新remainTime MUST不能倒填为该比较的输入

#### Scenario: PIK状态之间发生动画过渡

- **WHEN** 两状态均为PIK但同脚区段不一致
- **THEN** Runtime MUST继续执行正式区域交接与记录更新规则
- **AND** MUST不以pIkWeight恒为1声称区域交接已完成

### Requirement: 每脚接触状态必须按原单脚转移更新

每次FootPlant命令 MUST只更新被指定的一脚，并计算lockNow=isInZone且非isSliding，最终写isMoving=非isInZone、isLocking=lockNow。各一次性事件 MUST只在原分支规定的位置写入；未写字段保留到原消费者清理。原enablePIK、区域锁定位和FootLockInfo锁定结果 MUST分开。

#### Scenario: 当前脚从区外进入非滑动区域

- **WHEN** 上一合法状态isMoving为真，当前isInZone为真且isSliding为假
- **THEN** 当前脚 MUST产生原进入区域／锁区事件并清time；旧remainTime大于旧time时置快速重入标记
- **AND** 另一脚以及非本分支字段 MUST保持不变

#### Scenario: 稳定处于区外

- **WHEN** 上一isMoving为真且当前isInZone为假
- **THEN** 当前调用 MUST只执行原稳定区外分支的写入
- **AND** MUST不每帧伪造leaveGroundedZone或清空全部事件

### Requirement: 支撑混合必须区分PIK模式与动画权重

普通非平台分支 MUST从当前／下一状态的PIK标记a／b与原过渡量t产生pIkWeight=(1-clamp01(t))×a+clamp01(t)×b；平台和其它原分支 MUST照原规则处理。needPIK、pIkWeight、逐脚enablePIK、IKWeight、pelvisIkWeight和控制附加标量 MUST保持独立。

每次Foot求值 MUST先取得普通与预测支撑，用clamp01(pIkWeight)线性组合位置和法线，再进入原目标处理。CalculateFootTarget的动画基准混合 MUST仍由其自身原公式负责，不混同为第二次Pose动画过渡。

#### Scenario: 从非PIK进入PIK

- **WHEN** 原过渡量从0推进到1
- **THEN** 普通／预测支撑的组合权重 MUST按原PIK标记产生t
- **AND** MUST不把该权重当作Contact、脚锁权重或Final Pose低通系数

#### Scenario: 预测记录未启用

- **WHEN** 逐脚enablePIK为假或EnablePIKWarp关闭
- **THEN** Runtime MUST执行原查询／记录及返回分支
- **AND** MUST不跳到旧3C预测Runtime，也不跳过应执行的普通查询和目标处理

### Requirement: 落点与法线历史必须按原分支重写或承接

每脚 MUST使用current／next footprint、current／next footnormal、两份高度历史及原组件空间脚点。PredictIkHitGround的接触记录判定 MUST位于非isMoving分支；比较普通位置与nextFootprint的XZ距离，进入区域事件使原距离阈值乘3。

距离严格超阈值、needRaycast、原平台离开条件或快速重入标记满足原分支时，current／next位置 MUST一起采用普通位置，两份法线一起采用普通法线，两份高度一起采用普通位置Y。未重写时 MUST按原进入区域事件和赋值顺序承接，不添加世界位置残差低通或第二纪元状态机。

#### Scenario: 进入区域且没有触发重写

- **WHEN** 非isMoving分支中距离未超原有效阈值，其他刷新条件为假且enterGroundedZone为真
- **THEN** current位置／法线 MUST先承接旧next，再由current写next
- **AND** MUST不使用抬脚事件替代进入事件

#### Scenario: 新记录直接覆盖

- **WHEN** 原接触重写条件成立
- **THEN** 原位置、法线和两份高度历史 MUST按该分支一次覆盖
- **AND** 输出 MUST不继续消费已删除的3C PlantWorldResidual

### Requirement: 世界查询必须复刻原几何与更新条件

普通／预测查询 MUST保留原调用顺序、是否重新查询的判据、质量模式、脚／Toe几何、Rigidbody差量、所有实际查询参数、候选过滤、组合公式和无命中出口。Query adapter MUST只执行请求和记录结果，候选采用与缓存状态只由Foot内核更新。

HitGroundSimpleImpl与HitGroundImpl MUST作为原配置的明确算法分支完成，不以旧双SphereCast、最近Surface、Capsule路径、Convex Hull或平均法线代替。方法名称 MUST不作为推定物理重载的证据。

#### Scenario: 多个子查询共同生成支撑

- **WHEN** 原几何要求多点输入与多个命中记录
- **THEN** Runtime MUST保留每项实际结果并按原公式分别生成位置和支撑方向
- **AND** MUST不把任意单个raw normal或hit point冒称完整输出

#### Scenario: 合法无命中

- **WHEN** 查询正常完成但原过滤后没有可用候选
- **THEN** Runtime MUST执行原算法规定的无命中出口并记录原因
- **AND** MUST不借旧Surface、默认地面或另一脚生成虚假命中

### Requirement: 脚目标必须保留原坐标响应和权重回归

脚目标 MUST按原Owner／Foot绑定坐标及原高度区间计算。输入方向限制、每次方向历史限角、修正标量历史、原实例升降选速率、预测／disableDamping旁路与最终动画基准混合 MUST保持分型和原执行顺序。

当前有效footUpVelocityLimit／footDownVelocityLimit MUST按原AMLegIK实例isRaising选档；原按dt积分的量只积分一次。已由CalculateFootTarget应用的基准混合与历史回归 MUST不再次由Goal或Final Pose平滑执行。

#### Scenario: 身体升降与目标修正方向不同

- **WHEN** Owner运动产生的isRaising与目标修正增减方向不同
- **THEN** 选速率 MUST以原实例判据和当前参数为准
- **AND** MUST不继续使用旧3C目标差符号选档

#### Scenario: 本次disableDamping启用

- **WHEN** 原命令使disableDamping在本轮有效
- **THEN** 双脚 MUST按原目标分支消费它，随后骨盆按原顺序消费并清理
- **AND** MUST不把它扩大成永久配置或强制鞋底贴地命令

### Requirement: LockFoot必须完整保留原每脚状态和门控

LockFoot MUST保留LockGoal、LocalPos、LocalDiff、WorldPos、LastFrameOriginW、LockState与IsSetLockGoal，按原脚高、速度、范围、阻塞、PIK、CrossCheck及平台规则推进。实际启用getter MUST包含EnableLockFoot与非移动平台条件。

锁脚结果 MUST只进入原PreprocessAnimPos及其下游算法，不直接成为第二Goal、骨盆或Writer。原开关关闭 MUST停止应跳过的锁脚更新，同时保留其它普通Foot计算。

#### Scenario: Corin基准关闭锁脚

- **WHEN** 正式配置与原可琳快照一致，EnableLockFoot为假
- **THEN** Runtime MUST执行原关闭分支
- **AND** MUST不删除该算法实现，也不为了制造视觉改善强制开启

#### Scenario: 原锁脚条件失效

- **WHEN** 当前输入不再满足原门控
- **THEN** LockFoot MUST执行对应原退出／重置分支
- **AND** MUST不靠旧3C Anchor维持已失效目标

### Requirement: Pelvis必须复刻原双脚候选与完整响应

Pelvis MUST只在原逐脚计算和Finalize完成后读取必要的脚结果、动画骨盆、原参数与世界输入，完整执行原PelvisAdjustmentAdvance模式选择、Min／Max候选、腿距条件、上下速率、PD、查询、旋转和权重。

原骨盆历史、一次性disableDamping和当前／目标权重 MUST由唯一骨盆责任模块管理并随根事务提交。旧3C Primary Support目标、233436 Spring、同层下沉限速、硬Reach区间与Landing完成门 MUST不叠加于原骨盆结果。

#### Scenario: 两种原骨盆模式

- **WHEN** Profile显式选择原PelvisAdjustmentAdvance的任一值
- **THEN** Runtime MUST执行对应原候选和外层响应
- **AND** 每只脚的Finalize MUST在两种模式下均按原顺序执行

#### Scenario: 骨盆响应已完成

- **WHEN** 原候选、响应、查询和权重均已产生合法结果
- **THEN** Foot MUST发布唯一骨盆控制量
- **AND** MUST不额外执行旧3C Spring或以Reach观察改写该控制量

### Requirement: Foot核心与宿主实现必须保持分离

Foot业务 MUST只依赖不可变输入、明确参数、固定容量状态和查询结果。Animator／Source／World适配 MUST只发布实际数据或执行原请求；不得在适配器内选择脚状态、推进响应或补业务输出。

原状态、缓存、控制量与运行参数 MUST由根Bank统一准备、提交和丢弃；字段名称、布局和来源要可对账，但不照抄内存地址或把共享可变字典作为历史。正常每帧计算 MUST不新增托管堆分配。

#### Scenario: 两脚消费同轮输入

- **WHEN** 两脚需要同一Owner、状态过渡、参数和世界绑定
- **THEN** 它们 MUST读取同一冻结输入，分别更新本脚固定状态
- **AND** MUST不在两脚之间重复采集不同时间的共享输入

### Requirement: 原控制语义与物理输出必须分别对账

脚与骨盆位置、旋转、InScale、Goal权重、Solver结果和物理骨骼 MUST分别保留来源。原InScale MUST先通过实际原生消费者核对，再接入项目正式目标合同；类型名或相似数值不能证明其等同权重。

原AMLegIK目标等价 MUST不能替代最终物理效果验收。最终姿态不一致时 MUST定位输入绑定、原配置、控制编码或Solver差异，沿唯一Solver模块解决；不得再加骨骼后处理。

#### Scenario: 原生求解配置缺页

- **WHEN** 已恢复FBIKSetting身份但必要数据缓冲不可读
- **THEN** 实施记录 MUST保留资源ID、缺页范围和待恢复来源
- **AND** MUST不填项目默认参数并宣布最终骨骼完全等价

#### Scenario: 脚目标一致但骨骼仍有差异

- **WHEN** 原目标和正式编码目标一致而Solver／物理输出不一致
- **THEN** 诊断 MUST把问题保留在实际消费边界
- **AND** MUST不反向改变正确的目标算法来消除差异

## REMOVED Requirements

### Requirement: Landing Prediction必须形成独立世界事实

**Reason**: 原要求固定3C的KCC未来平移、canonical Landing Key、最近合法Surface与查询频率，不等于原AMLegIK预测算法。
**Migration**: 用本delta的原区域输入、预测记录、世界查询和普通／预测混合合同替换；删除Foot专属旧消费者，保留其它模块实际使用的公共世界服务。

### Requirement: Ground Path必须使用上一已提交落点与下一事件落点

**Reason**: 完整复刻不再以3C的LastLanding／NextSwingLanding路径作为脚目标权威来源。
**Migration**: 迁移为原current／next footprint与相应法线／高度历史；旧路径状态及无消费者字段直接删除。

### Requirement: Ground Detection必须发布原始Capsule接触集合

**Reason**: 原算法查询形状与子查询集合由同构建指令决定，不能强制追加Capsule路径扫描。
**Migration**: Query adapter按原请求实现固定容量真实查询；删除Foot专属Capsule路径配置和诊断输入。

### Requirement: Ground Envelope必须来自可达Edge与上侧凸包

**Reason**: 3C Convex Hull及MaximumReachableVerticalEdge会额外改变原脚目标。
**Migration**: 删除该Foot目标层及其输出夹取，用原多点地面组合和高度处理替换。

### Requirement: Ground Path与Foot持续状态必须保持抽象和实现分离

**Reason**: Ground Path不再属于新Foot运行链；抽象分离与事务存储要求仍然有效。
**Migration**: 使用新增Foot核心与宿主分离合同，保留固定状态、唯一写入和统一Seal／Discard，删除Ground Path专属部分。

### Requirement: Future Body Translation必须写入固定Workspace

**Reason**: Foot完整预测改为原AMLegIK输入与算法，不再要求KCC未来轨迹作为固定前置依赖。
**Migration**: 删除Foot对Future Body Translation的调用、缓存和配置；固定容量／无每帧分配约束迁入新核心合同，KCC自身保持原职责。

### Requirement: Pelvis必须只消费typed脚需求并保留可达观察

**Reason**: 原要求还绑定233436骨盆Spring、Primary Support和旧Landing完成语义，与完整原骨盆复刻冲突。
**Migration**: 由新增原骨盆候选与响应合同替换；保留输入分型和只读可达观察，删除旧业务响应及其配置。

### Requirement: Resolved Foot必须形成紧凑下游合同

**Reason**: 原结果合同绑定3C Landing完成资格、Contact Ownership进度和233436连续性，无法保留为完整原算法输出的前置条件。
**Migration**: 使用新增紧凑原控制输出合同；保留Frame／Completion／Rig分型、单一Pair与唯一Goal编码，移除旧准入及数值保持要求。
