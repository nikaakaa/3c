# character-foot-placement-presentation Specification

## Purpose

定义Corin Landing Prediction、Ground Path、Foot Lifecycle、Support、Pelvis、Goal Contribution与唯一FinalIK FBBIK之间的正式表现边界，不固定Foot模块内部类名和聚合方式。

## Requirements

### Requirement: Foot Placement必须是唯一Goal事务

唯一`CharacterPoseConstraintRuntime` MUST继续为每个Actor和表现帧建立匹配Frame、Completion、Program、Projection、Rig与Tuning Generation lineage的Pending Constraint Bank，并唯一拥有Foot Context、Resolved Foot Pair、Primary Support/Pelvis、Goal Contribution、唯一Goal Assembler、唯一Goal Set、FBBIK BendHistory与Solver Result。它 MUST不再拥有Final Pose物理页、Physical Writer或Physical Result；这些真相只属于`CharacterFinalPosePublication`。Foot Placement与FBBIK在线调参 MUST只进入actor-local Constraint Tuning Snapshot，不得修改Program Image、actor-local Execution View或其它Actor。

`CharacterPoseProgramRuntime` MUST在Program Image中Foot Placement、PoseBone Contribution、Goal Assembler与FBBIK各自Operation的位置，通过对应typed编译Handle恰好调用一次Constraint Runtime入口并写入唯一per-operation completion。Constraint Module MUST不扫描Program、不维护第二份Stage Schedule、不接收NativeSlice、Goal offset/count、Operation index或call-site index，也 MUST不重新执行已经完成的Operation。Constraint `Complete`只能验证完整闭包并发布一个`CharacterPoseConstraintResult`。

Foot Placement MUST继续由当前保留的深`CharacterFootPlacementModule`接收同帧不可变Frame Input并发布一个`CharacterFootPlacementResult`。调用方 MUST不知道或编排Landing Prediction、Ground Path、左右脚状态、Support、Pelvis与Goal编码顺序。本change MUST整体保留当前Lifecycle、Transition、State Target、Interpolation、Pelvis及Landing收口的算法、配置和执行顺序，不固定或恢复旧中央状态机类名，不引入新的Foot请求／最终结果流程。输入保持相同Pose、Foot Motion、Body／World、Rig与Profile时，Foot、Pelvis和三个Goal Contribution MUST保持指定提交ad3527e103cc3235a63e8a1c1dbd26df5155e0ba的结果；不得发布第二Goal Set、第二Pelvis、第二FBBIK、第二Final Pose页或第二Physical Writer。

#### Scenario: 正常生成Foot Placement结果

- **WHEN** 同一表现帧具有合法Component Pose、Step、Body、World Query、Profile、Program Operation与Pending Constraint Bank
- **THEN** Foot Placement Operation MUST生成同lineage的Resolved Foot Pair、Pelvis Result、三个Goal Contribution和唯一operation completion
- **AND** 后续Assembler与FBBIK MUST在各自Program Operation位置消费同一Bank结果，调用方不得取得或逐个提交Foot Context、Ground Path、Pelvis、Goal workspace或Solver状态

#### Scenario: 重复执行Foot Placement

- **WHEN** 同一Frame与Completion第二次请求执行同一个Foot Placement Operation
- **THEN** Program Runtime MUST使该Operation completion与整帧Invalid并阻止Final Publication
- **AND** Constraint Module MUST不覆盖第一次结果或建立第二Foot Placement事务

#### Scenario: Constraint完成后发布Final Pose

- **WHEN** Foot、Goal Assembler与FBBIK已经形成合法Constraint Result
- **THEN** Program Output MUST只通过actor-local Publication binding解析Program Image中的稳定layout handle并写入Final Publication唯一Pending Pose物理页
- **AND** Constraint Bank MUST不保存Physical Writer、Physical Result或第二Final Pose副本

#### Scenario: 当前IK仍有未完成改进

- **WHEN** Foot／IK其它change仍有未完成任务或尚未归档，但用户已确认当前保留实现作为重构基线
- **THEN** 本change MUST只迁移Constraint外部调用、存储归属与发布边界，不等待或实施那些剩余行为任务
- **AND** 已撤除的骨盆Reach硬夹紧、末端夹脚与已撤销SmoothKnee MUST不恢复；已知问题不得通过调整IK或评分隐藏

### Requirement: Landing Prediction必须形成独立世界事实

每只脚 MUST从typed Step Event、committed Body Target世界速度、Timeline段边界与Continuation、KCC Future Body Translation和本帧可见姿态生成Raw Landing。Raw Landing MUST从本帧输入重新投影，不得旋转或平移上一帧查询结果，也不得外推没有正式Plan的Future Body Yaw。

Runtime MUST从Side、Landing Event、量化Raw Landing、量化Component Up、Profile Revision与World Revision构造canonical Landing Observation Key。相同Key MUST复用根事务已提交的不可变Accepted或Rejected Observation；新Key MUST恰好查询一次，并在固定容量合法候选中按距离与稳定identity选择canonical最近命中。上一Committed Surface和其它历史状态 MUST不进入Key或候选选择。

查询 MUST过滤自身Collider、初始重叠、非法点、非法法线与超坡度命中。容量溢出或没有合法命中 MUST发布typed拒绝，不得沿用另一Key、旧Landing或默认Surface。

#### Scenario: 相同Landing Observation Key

- **WHEN** 当前帧生成的canonical Landing Observation Key与上一Committed Page相同
- **THEN** Runtime MUST复用相同Observation identity、Surface、点、法线或Reject结果
- **AND** MUST不执行SphereCast或读取上一Surface重新选择候选

#### Scenario: 新Landing Observation Key

- **WHEN** canonical Raw Landing、Component Up、Event、Profile Revision或World Revision产生新Key
- **THEN** Runtime MUST执行一次SphereCast并产生canonical最近合法候选或typed拒绝
- **AND** Pending事务失败时 MUST丢弃该Observation，不得污染上一Committed Page

### Requirement: Foot Lifecycle必须生成唯一权威结果

每只脚 MUST在同一根事务内只有一份权威离散State、一份权威连续Correction和一个最终Resolved Foot。Foot模块内部 MAY把typed持久状态、Transition判定、State Target、时间连续化、Anchor和Hard Constraint拆成独立组件；spec不固定类名、对象数量或Context聚合形态。

拆分后的每项持久字段和每项Decision MUST只有一个写入Owner。Transition判定 MUST不推进Residual或Interpolation时间；时间连续化 MUST不选择离散State；Hard Constraint MUST不反向改写Transition。所有内部组件 MUST共享根Bank的Prepare、Seal和Discard，不得形成第二生命周期、第二输出路径或图外Goal后处理。

#### Scenario: 内部责任拆分

- **WHEN** Foot实现把Transition、Target和时间连续化拆成独立组件
- **THEN** 相同Frame Input和上一Committed状态 MUST只产生一份离散State、一份Effective Correction和一个Resolved Foot
- **AND** 任一内部组件 MUST不能独立提交或绕过根事务

#### Scenario: 整帧Discard

- **WHEN** Foot内部Pending状态已经更新但后续Goal或Solver阶段失败
- **THEN** 上一Committed Foot状态、Correction、Anchor和Path MUST保持不变
- **AND** 下一帧 MUST不读取被丢弃的内部结果

### Requirement: Foot Placement配置与Rig必须显式

FootPlacement节点 MUST显式引用唯一Profile与Calibration。Projection、Profile、Calibration、Rig v4和Animation Rig Binding的identity与revision MUST精确匹配；PhysicsScene、World-Aware Binding或正式Future Body Translation source缺失时 MUST报告不可用，不得从Transform名称、Animator Avatar、旧Prefab组件或默认配置补全。

#### Scenario: Projection与Calibration不匹配

- **WHEN** Projection保存的Calibration identity或revision与Runtime资产不同
- **THEN** Runtime创建 MUST失败并报告stale identity
- **AND** MUST不继续使用旧Goal

### Requirement: Foot Placement必须与Gameplay和Network隔离

Landing、Goal、查询命中和diagnostics只属于Presentation。它们 MUST不进入Character State、World State、Gameplay Fact、Blackboard、Snapshot、Hash或网络packet，也 MUST不写VisualRoot或Gameplay Body。

#### Scenario: 两端显示同一角色

- **WHEN** 两个客户端以不同Presentation时刻显示同一committed Body
- **THEN** 两端 MAY独立计算Landing diagnostics
- **AND** 结果 MUST不改变Gameplay或网络确认

### Requirement: Foot Placement诊断必须只显示正式结果

Runtime Result MUST与Diagnostics严格分型。Constraint Module MAY按Frame开始冻结的interest，从Pending Context、Observation、Resolved Result和Constraint阶段Result单向深冻结Phase Progress、Baseline、Envelope、Swing Correction、Residual、Anchor、Contact Progress、Ownership、Support Eligibility、Support、Pelvis、Goal与Solved结果；这些事实只能进入`CharacterPoseConstraintCommittedResult`。Physical Write与最终Physical Bone结果 MUST只由Final Publication冻结进`CharacterFinalPosePublicationCommittedResult`。

Gizmo、Trace与Pose Watch MUST只读取各自允许的Committed页。Foot采样 MUST在成功Seal后的同步Commit调用栈内，从相同Frame、Completion、Program、Projection、Rig和Actor lineage的Constraint、Final Publication与当前Source已提交状态直接取得Left／Right与公共Fact Root，并以`in`执行一行target-scoped `DiagnosticEvent` partial调用；MUST不先组合或消费`CharacterFootIkCommittedCaptureViewLease`、Runtime Snapshot、Dimension View、Consumer／Binding或第二事实页。帧开始的可选partial Query只在匹配target订阅时要求Physical Writer冻结真实Physical Ankle事实；未订阅和Disabled构建不得执行该读取。`character-foot-ik-diagnostic-sampling`只提供字段分类、Sampler／Program Definitions和Editor workflow；`generated-diagnostic-sampling-framework`只拥有生成程序、typed packet、Capability Session与Writer／Reader。PoseGraph与Constraint不得拥有任一下游编译器、Schema、packet或Host知识。旧Foot单体Analyzer／Publisher、Diagnosis Store、旧CSV与历史兼容Reader直接删除；独立`character-foot-diagnostic-analysis`只在通用Host完成封存后读取生成Artifact并拥有Operator、当前Plan、七维评分和报告，Foot Runtime不维护其输入绑定或结果。固定输入回放 MUST通过同一Artifact Reader读取所需证据。Diagnostics MUST不查询世界、修改Context、选择Support、生成Goal、执行FBBIK、读取未冻结Physical Transform反推结果或把Constraint与Physical事实写回同一业务Bank。

#### Scenario: 捕获正式Foot事实

- **WHEN** Foot、Pelvis、Goal、FBBIK、Pending Pose与Physical Writer均成功提交
- **THEN** Foot采样 MUST从同一lineage的Constraint、Final Publication与当前Source已提交状态直接传入可对账冻结基线的正式事实；Live／Trace Projector MAY独立发布只读View
- **AND** Diagnostics页归属变化 MUST不改变Runtime Result、Final Pose或Physical Writer输入

#### Scenario: Writer失败

- **WHEN** Constraint Result已经完成但Final Publication在Physical Writer前或Writer中失败
- **THEN** Diagnostics MUST不发布本帧Pending Constraint或Physical结果
- **AND** Projector MUST不为Foot Capture借用上一帧或发布第二Snapshot；Live／Pose Watch只能按各自既有合同保留上一Committed事实或正式Actor Fault

#### Scenario: 离线诊断Foot Artifact

- **WHEN** 通用Host已经完成Foot Capability manifest、Schema和CSV封存
- **THEN** 独立Foot诊断器 MAY读取这些不可变Artifact执行当前Plan
- **AND** Foot Runtime、Constraint和Final Publication MUST不持有Analyzer、Plan、评分或报告引用

#### Scenario: 增加响应解释字段

- **WHEN** 仅增加本帧响应原因或前后数值的诊断记录
- **THEN** 运行状态、脚目标、Pelvis、Bend与最终骨骼 MUST保持不变
- **AND** MUST不要求修改Goal Assembler、Solver算法或质量评分政策

### Requirement: Ground Path必须使用上一已提交落点与下一事件落点

每只脚 MUST按Landing Event identity维护LastLanding与NextSwingLanding。PreSwing或Swing阶段每帧 MUST重新投影Raw Landing并构造canonical Observation Key；只有新Key执行一次SphereCast，相同Key复用Committed Observation。新Observation低于正式更新死区时 MUST保留NextSwingLanding与Ground Path；达到死区时 MUST提交新落点并重建Path。事件完成后最新NextSwingLanding MUST晋级为LastLanding。

Ground Path MUST只使用LastLanding与NextSwingLanding构造查询输入。没有LastLanding时 MUST发布`CurrentLandingUnavailable`，不得用Animated Sole、Transform、固定高度或默认地面补起点。

#### Scenario: 相同Observation持续多个表现帧

- **WHEN** PreSwing或Swing连续帧产生相同canonical Observation Key
- **THEN** Runtime MUST复用Committed Observation、NextSwingLanding与Committed Ground Path
- **AND** MUST不执行新的SphereCast或Capsule Ground Detection

#### Scenario: 新Observation达到更新死区

- **WHEN** 新Key产生的Accepted Observation与同Event NextSwingLanding距离达到正式更新死区
- **THEN** Runtime MUST提交新NextSwingLanding并重建同一Foot事务中的Ground Path
- **AND** Ground Path MUST消费该Observation，不得执行第二次Landing查询

### Requirement: Ground Detection必须发布原始Capsule接触集合

Ground Detection MUST沿LastLanding到NextSwingLanding构造唯一Capsule请求。请求 MUST显式携带轴端点、Component Down、半径、查询距离、最大轴段长度、Ground Layer、分段命中容量与整条路径Contact容量。Backend MUST按最大轴段长度确定性切分轴并执行真实Capsule Cast，过滤自身Collider、初始重叠、非法几何和同分段重复命中，并发布原始位置、法线、Surface、分段索引、查询距离和稳定candidate identity。

Backend MUST不把接触集合预先压成单个落点，不得改用Raycast、SphereCast或第二种查询算法。没有合法接触或固定容量溢出 MUST发布typed rejection，不得生成默认地面。

#### Scenario: Capsule命中多个表面

- **WHEN** 分段Capsule Cast命中多个合法表面
- **THEN** Backend MUST在固定容量页中保留各接触的位置、法线和identity
- **AND** MUST不先压成中心线或单一Surface

### Requirement: Ground Envelope必须来自可达Edge与上侧凸包

Ground Envelope Builder MUST把Raw Contacts投影到脚步纵向与Component Up组成的二维平面，稳定生成Edge候选并在同一路径距离保留最高候选。Path Start与Target Landing MUST作为首尾端点保留；`CastAbove`和`CastBelow`只属于查询范围，不得成为Reachability限值。

正式Profile MUST提供米制`MaximumReachableVerticalEdge`。任一Edge超过限值时 MUST发布`UnreachableEdge`与首个Invalid Segment，不得删除障碍后继续构造Hull、沿用旧Envelope或借用KCC Step高度和腿长替代。全部Edge合法时，Builder MUST输出位于所有保留候选上侧或与其重合的连续上侧Convex Hull。Envelope只表达feet-only地面下界，不改变Foot XZ或驱动Pelvis。

#### Scenario: 路径经过不可达垂直面

- **WHEN** 任一Edge沿Component Up的高度超过`MaximumReachableVerticalEdge`
- **THEN** Ground Path MUST发布`UnreachableEdge`且Accepted Envelope为空
- **AND** Raw Contacts与Edge事实 MUST保留在同一成功Seal的只读诊断页

### Requirement: Ground Path与Foot持续状态必须保持抽象和实现分离

Foot核心 MUST只依赖World Query合同、Ground Envelope Builder和预分配Observation页。Unity Adapter只执行查询与固定容量写入，不得选择Foot State、保存持续状态、推进Interpolation、创建Anchor、构造Pelvis或写Goal。Foot业务只消费不可变Observation，不得直接访问Unity查询对象。

跨帧状态 MUST保存在固定布局typed记录中，并 MAY按Transition、Interpolation、Anchor和Path责任分区；每个字段 MUST只有一个写入Owner。系统 MUST不使用字符串Key、共享Dictionary、Gameplay Blackboard、动态字段或可变Diagnostics保存Foot状态。全部状态页 MUST由根Bank统一提交或丢弃，任一内部组件不得拥有独立Committed/Pending生命周期。

#### Scenario: 分型状态共同提交

- **WHEN** Transition、Interpolation、Anchor与Path分别产生Pending typed状态
- **THEN** 根Bank MUST在完整Foot、Goal和Solver闭包合法后一次Seal
- **AND** 任一分区 MUST不能单独提交、回退到旧Context或从Diagnostics恢复状态

### Requirement: Future Body Translation必须写入固定Workspace

Foot Placement MUST为每个根Bank预分配固定容量Future Body Translation Workspace，并把它交给正式Translation Source写入。Translation Source MUST只更新有效Sample数量和内容，不得为每次活跃预测新建Trajectory对象、临时Sample数组或复制Sample集合。

#### Scenario: 同一帧左右脚请求未来Body平移

- **WHEN** 左右脚需要同一Body、Timeline与Duration范围的未来平移
- **THEN** Foot模块 MUST在本帧只填充一次Pending Workspace并让两脚读取同一只读结果
- **AND** 预测不得产生托管堆分配

### Requirement: Resolved Foot必须形成紧凑下游合同

`CharacterResolvedFootResult` MUST只表示当前Foot流程完成既有Landing资格判断后的最终Goal输入。它 MUST发布下游实际消费的Frame、Completion、Rig、Side、Final Sole/Ankle/Rotation、有效Sole/Ankle/Rotation、Correction、作者位置/旋转权重、Contact Reference与Ownership、Support Eligibility、Support Intent与Weight、Support Error、Event lineage、所需typed Reach观察和Outcome。提供给Pelvis的初步需求 MUST使用不同的内部类型，不得把初步Resolved当作最终结果；迁移 MUST不复制两套同义字段或为已删除的夹脚建立受限输出合同。

最终Resolved Pair MUST只组合同Frame、Completion与Rig的两脚结果，不重新选择State、Support、Reach或Goal。内部State、Transition Decision、Path、Anchor历史与Interpolation过程 MUST不进入最终下游合同。Primary Support与Pelvis MUST只消费本模块内部的初步请求视图；Goal编码 MUST只读取最终Resolved与Pelvis Result，不得新增业务层Reach夹紧。必要的身份和数值检查 MUST复用现有生产/消费边界，不在每个内部阶段重复验证相同字段。

最终Sole、Ankle、Rotation、有效目标与Correction MUST保持当前Foot/Heel/Toe几何和权重规则。未加权Goal、加权目标与实际Solved/Physical Pose MUST保持不同含义，不得把最终Goal输入称为已写入的物理脚底或保证它必然可达。原目标不可达时 MUST保留真实观察和原Landing资格结果，不硬改目标、权重或骨盆来制造成功。

#### Scenario: 初步脚结果尚未完成Landing判断

- **WHEN** Foot已完成本帧目标与Interpolation但Pelvis响应及其后的原Landing完成判断尚未结束
- **THEN** Foot MUST只产生内部typed脚需求和完成凭据，不发布最终Resolved
- **AND** 根Runtime与Goal消费者 MUST不能取得这份未完成结果作为正式输出

#### Scenario: 原Landing资格不满足但目标保持

- **WHEN** Foot进入原Landing完成检查且本腿在当前加权Pelvis位移下不满足可达资格
- **THEN** 现有Transition MUST保留原未完成结果，不因此允许Full Lock
- **AND** Foot目标、作者权重和Pelvis响应 MUST保持原行为，不补回末端夹脚或硬压骨盆

#### Scenario: 正常输出保持

- **WHEN** 相同输入进入基于233436保留行为整理后的内部阶段
- **THEN** 分型迁移 MUST保持Goal的位置、旋转、权重和原连续性处理
- **AND** MUST不新增一次Interpolation、Pelvis响应或FBBIK

### Requirement: Pelvis必须只消费typed脚需求并保留可达观察

Primary Support MUST只读取同Frame、Completion、Rig与Side的typed请求中正式Support Eligibility、Support Intent、Support Error、Event lineage与Pelvis Reach Reference。正式Support为零或Reference无效时 MUST按现有业务发布不可用，不得按相对权重归一制造支撑。Contact Reference、Pelvis Reach Reference和Landing Reach Request MUST保持独立含义。

Pelvis MUST只消费请求中所需的目标与Reach视图、Primary Support Result、同帧动画/Body输入和显式设置，不得读取Foot State、Lock Mode、Anchor历史、Path Residual、Interpolation内部状态或Diagnostics。请求的未加权与有效目标 MUST明确分型，权重不得重复应用。

Pelvis MUST继续使用233436组合中用户已接受的共同目标、软姿态偏好、一次Spring及Handoff/背向速度规则，并保留逐腿和交集的typed Reach观察。Reach MUST不夹取骨盆目标或输出、不清边界速度、不阻止Release回零、不强开骨盆权重；Primary Support不得作为例外。末端Foot径向夹脚和公共硬执行边界 MUST保持删除，不以重构之名恢复。

原Landing完成可达资格 MUST继续使用本腿请求与当前实际加权Pelvis位移判断。该结果只作为现有Transition Resolver的准入输入，State仍由唯一Transition Runtime更新；Pelvis和Ground Constraint MUST不能直接反写离散State。删除硬Reach MUST不被扩大为删除原完成资格、改变作者权重或新增一个状态选择器。

#### Scenario: 下游选择Support

- **WHEN** Primary Support收到合法的两脚请求
- **THEN** 它 MUST仅按请求的正式Support与Event字段执行原有获取/保留选择
- **AND** MUST不读取Foot State、Lock Mode或Interpolation历史

#### Scenario: 可达观察参与原Landing完成

- **WHEN** 唯一Pelvis响应已产生本帧实际加权位移，原Foot流程请求检查Landing完成
- **THEN** 本腿typed观察 MUST按当前位移计算原可达资格
- **AND** Foot Lifecycle MUST按原政策消费该结果完成准入，不修改骨盆响应或脚目标

#### Scenario: 主支撑观察不可达

- **WHEN** Primary Support腿的几何观察范围不包含当前Pelvis输出
- **THEN** 系统 MUST保留真实不可达事实，不以Primary身份强制夹取骨盆或脚目标
- **AND** MUST不新增公共硬区间、边界清速度或权重补偿

#### Scenario: 请求身份混杂

- **WHEN** 请求与其对应观察或结果的Frame、Completion、Rig、Side或Event不匹配
- **THEN** 现有唯一交接校验入口 MUST在正式发布前拒绝，不将同一检查复制到每个内部方法
- **AND** MUST不借用上一帧结果、默认脚需求或另一只脚的裁决补全

### Requirement: Foot阶段数据必须具有唯一权威来源和消费权限

正式Pose/Foot Motion/Body输入、世界Observation、选中Target、Interpolation输出、Ground Constraint输出、初步脚请求、Pelvis结果、Landing完成后的Resolved、Goal、Solved Pose和Physical写入 MUST按阶段分型，明确空间、权重是否已应用、生产Owner及合法消费者。中间事实可以是本阶段权威，但 MUST不冒充后续已完成结果，不得从同名诊断值、临时编码或另一阶段的近似值补齐。

唯一Foot请求生产者 MUST按基线实际规则发布Support事实、Landing Reach观察准入、正式权重及加权脚几何。Module与Pelvis消费者 MUST不混读过程Motion.State、Step和Resolved再次决定同一准入，不通过临时Goal编码后反算另一份Pelvis脚底输入。Stride/Pelvis所需的步态、落点与可用性 MUST通过最小typed请求视图进入唯一准备阶段，不直接取得Foot Context、完整Path页或原始Landing历史。

迁移 MUST以用户指定源码基线中实际被Pelvis、Goal和Writer消费的条件与数值为依据，保持空间换算、权重及数值顺序。同义字段不一致时 MUST明确唯一生产者并报告差异，不凭名字认定权威，不保留两份可选择的正式真相。

实际消费链 MUST只作为回归对照，不能替代正式业务Owner定义。纠正非权威来源若产生行为变化，MUST独立记录来源、数值和业务影响并交由用户决策；不得为保留旧结果建立双读，也不得将行为修正伪装成机械迁移。

#### Scenario: 下游需要判断本腿是否进入可达观察

- **WHEN** Foot请求已发布正式Reach观察准入
- **THEN** Module与Pelvis准备 MUST只消费该决定，不读取过程Motion.State或原始Step重判
- **AND** 请求生产者 MUST保留基线的全部实际权重、事件和可用性条件

#### Scenario: Pelvis需要有效脚底

- **WHEN** 请求已包含按正式权重和空间规则得到的有效Sole
- **THEN** Pelvis MUST直接读取，不建立临时Goal再反解另一份脚底
- **AND** 迁移 MUST保持原来真正被消费的求值顺序，不直接采用未经对账的同义字段

### Requirement: Foot业务控制权必须由唯一Owner执行

Transition Resolver MUST拥有离散变化判定，Transition Runtime MUST唯一应用State、Contact边沿与Anchor命令，Landing Runtime MUST拥有正式Landing记录。State Target MUST只选择请求目标，Interpolation MUST唯一推进残差、响应和Applied Direction历史，Ground Constraint MUST只发布原阶段输出，不倒写Interpolation历史。

Foot输出Owner MUST按作者输入和既有Ready/Suppress/Contact规则解析权重；Primary Selector MUST唯一选择主支撑并写入其历史；Pelvis Owner MUST唯一产生目标、响应和Pelvis权重。本腿可达观察到原Landing完成资格 MUST是唯一反馈，不允许反复重算Foot/Pelvis、反写Spring或建立另一份权重控制器。

Root MUST只调度和提交，不执行业务数学；Encoder、Assembler、Solver和Diagnostics MUST不反写Foot状态、请求、权重或已发布Goal数据。权限 MUST通过收窄输入视图、职责和可变状态可见性落实，不能以多层重复检查代替所有权分离。

#### Scenario: 当前Pelvis位移使Landing不能完成

- **WHEN** 本腿可达观察不满足原Landing完成条件
- **THEN** 现有Foot Transition MUST决定并应用未完成状态
- **AND** Pelvis、Module、Goal层 MUST不另写State、不夹脚、不再次积分或修改原权重

#### Scenario: 诊断读取过程事实

- **WHEN** CSV、Trace或Watch读取目标、权重、Primary或可达性证据
- **THEN** 它们 MUST只展示生产Owner已经作出的决定
- **AND** 运行Owner MUST不反读这些证据控制下一帧

### Requirement: Foot运行历史不得借用过程证据保存

下一帧必须读取的方向、响应、残差和有效性 MUST保存在固定布局typed运行状态中，每项字段具有唯一写入Owner及明确初始化/Reset语义。过程Fact MUST只表达本帧前值、采用值、结果与理由，不能成为隐藏的跨帧状态容器。

Foot运行状态和过程证据 MUST仍属于同一根帧事务下的Constraint Bank；拆分不得创建独立Committed/Pending生命周期、全局缓存、字符串状态Key或新的外部可变Context。Pending事务开放与Committed结果可读 MUST分别判断，不得以已经关闭的Pending标志否定正式历史。其它active拥有的连续性参考不得由本change新增旁路消费。

#### Scenario: 从过程记录移出上一帧方向

- **WHEN** Interpolation需要上一帧实际应用方向限制本帧方向变化
- **THEN** 它 MUST读取唯一正式方向历史并由同一Owner写入Pending新值
- **AND** 诊断Fact只能单向记录该变化，删除诊断投影不得改变方向计算

#### Scenario: 后续阶段丢弃本帧

- **WHEN** Pending Foot历史已更新但完整帧未成功Seal
- **THEN** 新历史与过程证据 MUST共同丢弃，Committed历史保持上一成功帧
- **AND** 任一内部记录 MUST不能单独提交或从未提交Fact恢复状态

### Requirement: Foot诊断字段与分析消费必须只有一个Schema Owner

每个Foot诊断字段 MUST只声明一次稳定identity、类型、单位、业务分组、availability与提取语义；主表与真实子表 MUST分别拥有明确布局。Runtime Result与正式Fact Root MUST不引用列名、CSV、Analyzer或Publisher。采样迁移 MAY把已验证typed列绑定替换为通用Attribute、Schema descriptor、Generated Capture Program和通用Schema Reader，但 MUST同时删除被替换的Header、Column getter/setter、CSV Reader与必需列手工清单，不得让二者并存为两份Schema真相。

Schema MUST在编译或明确初始化入口完成闭包校验；sealed输入 MUST只在框架Reader边界核对identity、布局与hash，不在每次字段搬运或记录转交时重复重检，也不在OnInspectorGUI进行重操作。主表与真实子表 MUST沿同一Foot Capability packet流分别投影，保留紧凑明细和随机查询业务；Extractor与Reader不得执行第二份Foot数学或生成评分。

字段布局或含义变化 MUST显式升级版本，缺列、重复列、非法类型或不匹配版本 MUST拒绝，不建立旧reader、别名或默认值补全。历史原包及其旧结果 MUST保留为证据，不自动覆盖或用新语义重新解释。现有评分维度、权重、分母和Unavailable规则 MUST保持原Owner。

仅修改内部记录组织或采样映射且列名、顺序、类型和含义均不变时，版本 MUST保持；不得为已经删除的Reach夹紧虚构一组新旧字段或强制ABI迁移。

#### Scenario: 新增普通证据字段

- **WHEN** 当前版本新增一个正式响应证据字段
- **THEN** 字段identity、Generated写入位置、Schema驱动读取和必需字段校验 MUST由同一Schema descriptor得到
- **AND** 不改变质量规则时 MUST不新增评分Target或修改报告业务规则

#### Scenario: Schema字段不完整或重复

- **WHEN** 当前Schema存在重复identity、缺失Extractor／Schema Reader或类型不一致
- **THEN** 编译或Capability preflight MUST明确失败，不开始生成看似合法的packet或采样文件
- **AND** MUST不靠空值、零值或忽略该列继续运行

#### Scenario: 从旧typed列绑定迁移到Generated Schema

- **WHEN** 已验证的旧typed列绑定被Generated Schema与Schema Reader整体替换且字段业务含义未变
- **THEN** 新Schema MUST使用新的正式identity，逐字段业务值、availability、离线诊断规则和评分语义 MUST保持
- **AND** 原有紧凑分析存储和只读查询业务 MUST由唯一离线Foot Analysis继续提供，旧Reader MUST不兼容解释新Schema

### Requirement: 有效Foot位置目标不得由修正幅度撤销

同帧正式State/Support Target与Sole/Ankle目标解析成功的Ready Foot MUST按正式FootPlacementWeight发布PositionWeight。Correction为零或小于GeometryEpsilon MUST不撤销该位置约束。Unavailable与Suppress MUST继续零权重；作者权重为零 MUST保持原关闭语义。Rotation MUST继续使用现有正式Contact与LockWeight政策。

#### Scenario: Swing修正趋零但目标仍有效

- **WHEN** Swing目标合法、正式FootPlacementWeight非零，Correction从0.1毫米以上变为更小值或零
- **THEN** PositionWeight MUST继续等于正式作者权重，目标仍随本帧Swing更新
- **AND** MUST不因Pelvis先平移而默许脚脱离这个有效目标，也不得冻结为世界Anchor

#### Scenario: 作者关闭或目标不可用

- **WHEN** 正式作者权重为零，或本帧目标解析失败/被Suppress
- **THEN** 系统 MUST保持原零权重/不可用语义
- **AND** MUST不使用上一目标、默认点或第二Goal补全

### Requirement: 共同Pelvis目标必须来自同帧双脚高度需求

在现有Pelvis生产资格内，共同期望偏移 MUST由同帧原动画双脚Sole与Resolved有效目标Sole沿同一Component Up的最低高度差产生：`min(targetL,targetR)-min(animatedL,animatedR)`。该偏移 MUST保留正负号，替换旧地形相对高度加正向抬脚补偿，不与其叠加。目标 MUST不读取上一帧最终Physical脚底反推，也不得产生第二目标Owner。

#### Scenario: 较低目标脚位于较低动画脚之下

- **WHEN** 有效目标脚最低高度小于原动画脚最低高度
- **THEN** 共同期望偏移 MUST为负值，并交给同一Pelvis响应
- **AND** MUST不因旧正向max门丢弃下降需求

#### Scenario: 最低脚身份不同

- **WHEN** 原动画最低脚与有效目标最低脚属于不同Side
- **THEN** 系统 MUST分别取各对双脚的最低高度后相减
- **AND** MUST不暗改为同脚修正最小值、平均值或旧Stride地形高度

### Requirement: 骨盆可达性观察不得硬改骨盆与脚目标

系统 MUST保留同帧typed Reach Request的逐腿几何与交集观察，以及原Landing完成可达资格。Reach MUST不再夹取骨盆响应目标或输出、清边界速度、阻止骨盆Release回零或强开骨盆权重，Primary Support MUST不作为例外。Module MUST删除末端Foot径向夹脚，原Resolved目标与正式权重直接进入唯一GoalSet。

系统 MUST只持有原根Bank内Spring，沿原频率及Handoff／背向速度规则积分preferredTarget。软姿态偏好 MUST只在原动画零偏移与共同请求之间选择，不附加Reach硬边界。MUST删除硬执行选择、公共执行上下界、动作事实和夹脚API，不保留恒值兼容分支。

#### Scenario: 主支撑可达上界低于响应输出

- **WHEN** 主支撑几何观察给出低于Spring输出的上界
- **THEN** MUST保留观察，不通过它硬改骨盆或Foot Goal
- **AND** 实际求解不足 MUST照实保留，不将Solver成功等同最终骨骼准确到位

#### Scenario: Release沿原响应回零

- **WHEN** 骨盆处于Release
- **THEN** 原Spring MUST追零，完成只由原输出／速度容差决定
- **AND** MUST保留真实Up及逐腿观察，不产生无历史Reach启动

#### Scenario: Landing完成与骨盆执行分开

- **WHEN** Foot到达原Landing完成检查阶段
- **THEN** MUST按本腿实际加权位移可达性执行原资格检查
- **AND** MUST不据此后置夹脚、硬压骨盆或修改作者权重

### Requirement: 骨盆观测必须区分原Pose、修正量和最终世界写回

最终Physical Pelvis世界点 MUST由唯一Physical Writer完成本次骨骼写入后取得，并与组件点及同一Completion一起冻结。Sampler MUST只消费这份正式结果，不通过采样时live Root变换冒充同Completion世界事实。原Pose输入有效性 MUST与HeightTarget/Posture求值有效性分离；产生合法Pelvis Goal的Releasing也 MUST发布真实源Pose。

#### Scenario: Release阶段检查最终骨盆Goal残差

- **WHEN** Releasing仍有非零Pelvis Goal且同Completion的最终Physical写回有效
- **THEN** 残差 MUST使用本帧真实源Pelvis组件点加正式加权Goal作为期望
- **AND** MUST不把未执行HeightTarget的默认零点当原Pose

#### Scenario: 最终世界运动与额外修正不同

- **WHEN** Root、原动画和Pelvis修正同时变化
- **THEN** Diagnostics MUST分别表达最终世界点、组件点和相对修正，不把其中一项直接命名成另一项的跳变
- **AND** 必要阶段缺失 MUST标Unavailable，不改质量规则或用占位零值宣布通过
