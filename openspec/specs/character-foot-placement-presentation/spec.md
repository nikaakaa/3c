# character-foot-placement-presentation Specification

## Purpose

定义Corin Landing Prediction、Ground Path、双脚状态、Support、Pelvis、Goal Contribution与唯一FinalIK FBBIK之间的正式表现边界。
## Requirements
### Requirement: Foot Placement必须是唯一Goal事务

唯一`CharacterPoseConstraintRuntime` MUST为每个Actor和表现帧建立匹配Frame、Completion与Rig lineage的Pending根Bank。根Runtime MUST只管理阶段顺序、lineage、页所有权、Seal/Discard/Invalidate和失败传播，不得实现Foot、Pelvis、Goal或Solver数学。

Foot Placement MUST形成一个深`CharacterFootPlacementModule`，其外部Interface只接收同帧不可变Frame Input并发布一个`CharacterFootPlacementResult`。调用方 MUST不知道或编排Landing Prediction、Ground Path、左右脚状态、Support、Pelvis与Goal编码顺序。

Module Implementation MUST先通过World Query Adapter生成不可变Observation Page，再为左右脚各执行一次`CharacterFootStateMachine`并生成唯一Resolved Foot Pair，最后计算Primary Support、Pelvis与三个typed Goal Contribution。State Machine MUST不直接调用SphereCast、访问Collider或保存Unity查询对象；不得发布第二Goal Set、第二Pelvis、第二FBBIK或第二Physical Writer。

#### Scenario: 正常生成Foot Placement结果

- **WHEN** 同一表现帧具有合法Component Pose、Step、Body、World Query、Profile和根Pending Bank
- **THEN** Foot Placement MUST生成同Frame、Completion与Rig lineage的Resolved Foot Pair、Pelvis Result和三个Goal Contribution
- **AND** 调用方 MUST不取得或逐个提交Foot Context、Ground Path、Pelvis或Solver状态

#### Scenario: 重复执行Foot Placement

- **WHEN** 同一Frame与Completion第二次请求Foot Placement Prepare
- **THEN** 根Runtime MUST报告非法调用顺序并阻止整帧发布
- **AND** MUST不建立第二Foot Placement事务

### Requirement: Landing Prediction必须形成独立世界事实

每只脚 MUST按`Current/Incoming Step -> committed Body Target世界速度 + Timeline段边界/Continuation -> KCC Future Body Translation -> Raw Landing -> Future Landing SphereCast -> Accepted/Rejected Landing`执行。Step必须携带稳定Landing Event identity；Raw Landing必须按`VisiblePosition + FutureBodyTranslation + VisibleRotation * RootLocalLanding`从本帧输入重新投影，不得旋转旧查询结果。

Future Body Translation的当前平面速度 MUST来自同帧committed Body Target世界速度；Timeline只提供当前有限段剩余时间和显式Continuation世界速度。KCC MUST在原世界空间积分并裁剪平移，不得按输入方向、速度方向、Body Yaw、相邻表现速度方向差或任何推导曲率旋转世界速度。`RootLocalLanding` MUST只乘本帧已经显示的`VisibleRotation`；当前阶段没有正式未来朝向Plan，因此 MUST不外推Future Body Yaw，不得把瞬时Yaw Velocity维持到Landing时刻。

SphereCast MUST从Raw Landing上方沿Component Down使用Profile声明的半径和有限距离查询。查询 MUST过滤自身Collider、初始重叠、非法点、非法法线与超坡度命中，并在固定容量返回集合中按距离和稳定identity选择最近合法命中。命中数量达到固定缓冲容量时 MUST发布`GroundQueryCapacityExceeded`并拒绝整次查询，不得从截断集合选择落点；没有合法命中时 MUST发布`GroundQueryMissed`，不得创建默认Surface。

#### Scenario: Future Landing命中

- **WHEN** SphereCast返回合法Surface
- **THEN** diagnostics MUST发布唯一Accepted Landing、Surface identity、点、法线与实际查询距离

#### Scenario: Landing输入不可用

- **WHEN** Step、Motion Timeline、Body Target、Future Body Translation或合法Surface不可用
- **THEN** 该脚 MUST发布明确Rejected原因
- **AND** MUST不沿用上一帧Landing或生成替代落点

#### Scenario: Landing命中容量溢出

- **WHEN** SphereCast返回的命中数量达到固定缓冲容量
- **THEN** 该脚 MUST发布`GroundQueryCapacityExceeded`
- **AND** MUST不接受截断命中集合中的任何Surface

### Requirement: Foot Placement必须通过统一状态机生成双脚修正

每只脚 MUST只有一个固定typed `CharacterFootStateContext`、一个`CharacterFootStateMachine`和一个Effective Correction Owner。State Machine MUST用以下确定映射重新解释`8fc704a74ed3548c3357eff5c2d45f52d8366a4b`行为：

```text
None且PlantCycle未消费 -> Swing
None且PlantCycle已消费 -> UnlockedSupport
Acquiring -> Landing
Locked -> Locked / FullAnchor Response
Sliding -> Locked / Sliding Response
Releasing -> Releasing
```

Sliding Response MUST只属于Locked内部计算事实，不得拥有独立Event、Anchor、Transition或Output。系统 MUST保持8fc的状态条件、比较顺序、公式、阈值和逐帧结果，不得因新命名改变行为。

Swing MUST继续使用动画Phase、Last Landing、Next Landing与Ground Envelope：

```text
phase = InverseLerp(LiftOffPhase, LandingPhase, EventPhase)
progress = SmoothStep(0, 1, phase)
baseline = Lerp(LastLanding, NextLanding, progress)
envelope = SampleEnvelopeByArcLength(progress)
vertical = max(0, dot(envelope - baseline, ComponentUp))
         + LandingConstraintWeight * dot(baseline - AnimatedSole, ComponentUp)
```

同Event Path Revision MUST继续按LandingUpdateDistance捕获`PreviousOutput - NewSwingCorrection`残差，并按EffectiveCorrectionHalfLife衰减。Swing、Landing与Locked MUST继续执行8fc向上RaiseToFloor。

PlantConfidence首次达到0.5 MUST消费本轮Plant；只有合法Landing且水平误差不超过LockDistance才进入Landing并使用Landing Point创建Anchor。Landing Contact Progress MUST保持历史最大`InverseLerp(0.5, 0.75, PlantConfidence)`；达到1进入Locked。Landing、Locked、Sliding Response与Releasing的准入、释放和完成条件 MUST逐值保持8fc。

Locked FullAnchor Response MUST使用完整Anchor修正。Sliding Response MUST保持8fc的水平权重公式和进入首帧保留Output行为。Releasing MUST保持移动Swing Target残差与HalfLife衰减。Contact Ownership、Support Weight和Goal Position Weight MUST逐值保持8fc。

#### Scenario: Acquiring重新解释为Landing

- **WHEN** PlantConfidence首次达到0.5且Landing与LockDistance准入合法
- **THEN** 新State Machine MUST进入Landing并产生与8fc Acquiring相同的Anchor、Acquire Residual、Progress和Output
- **AND** MUST不引入新的LandingStarted、固定Duration或动画Contact Plan

#### Scenario: Sliding重新解释为Locked Response

- **WHEN** Locked脚水平误差大于LockDistance且不超过SlideDistance
- **THEN** State Machine MUST保持Locked生命周期并使用与8fc Sliding相同的水平修正、首帧保留和HalfLife追踪
- **AND** Sliding Response MUST不形成第二状态机或第二Anchor Owner

#### Scenario: 相同基线输入

- **WHEN** 新Module收到与8fc相同的Frame Input和映射后的上一帧Context
- **THEN** Swing、Anchor、Correction、Ownership、Support、Pelvis与Goal结果 MUST逐帧等价
- **AND** 任一差异 MUST作为重构回归而不是行为优化

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

Runtime Result MUST与Diagnostics严格分型。Diagnostics MUST从Pending Context、Observation、Resolved Result和后续阶段Result单向深冻结Phase Progress、Baseline、Envelope、Swing Correction、Residual、Anchor、Contact Progress、Ownership、Support Eligibility、Support、Pelvis与Goal/Solved/Physical结果，并可发布新五状态和Lock Response。

Gizmo、CSV、Trace与Pose Watch MUST只读取相同Frame、Completion、Rig和Bank identity的Committed页。Diagnostics MUST不查询世界、修改Context、选择Support、生成Goal或执行FBBIK。

#### Scenario: 捕获重构后基线事实

- **WHEN** Foot、Pelvis、Goal、FBBIK和Pending Pose完成验证
- **THEN** Diagnostics MUST发布可对账8fc的同帧正式事实和新状态映射
- **AND** Diagnostics命名变化 MUST不改变Runtime Result

### Requirement: Ground Path必须使用上一已提交落点与下一事件落点

每只脚 MUST按Landing Event identity缓存Accepted Landing。PreSwing或Swing阶段的每个有效表现帧 MUST执行一次且仅一次正式Landing SphereCast；同一事件的后续权威Accepted结果 MUST允许更新NextSwingLanding，不能把首次预测永久冻结。更新距离小于正式Foot Motion Profile的死区时 MUST保留原落点并复用Ground Path，但 MUST不停止下一表现帧的正式Landing预测。该事件实际落地后最新NextSwingLanding MUST晋级为LastLanding，之后才为新的Swing事件建立下一落点。

Ground Path MUST只使用LastLanding与NextSwingLanding构造查询输入。没有LastLanding时 MUST发布`CurrentLandingUnavailable`；不得用Animated Sole、Transform、固定高度或默认地面补起点。

#### Scenario: 同一Landing Event持续多个表现帧

- **WHEN** NextSwingLanding Event identity没有变化且新的Accepted Landing移动超过更新死区
- **THEN** Runtime MUST提交新的NextSwingLanding并重建同一Foot Placement事务中的Ground Path
- **AND** Ground Path重建 MUST消费该表现帧已经产生的唯一SphereCast结果，不得为重建再执行第二次Landing查询

#### Scenario: 同一Landing Event的小幅预测误差

- **WHEN** 新的Accepted Landing与缓存点的距离小于正式更新死区
- **THEN** Runtime MUST复用缓存落点与Committed Ground Path
- **AND** MUST继续执行下一表现帧的唯一Landing预测，但不得因毫米级误差触发新的Capsule Ground Detection

#### Scenario: 下一Swing Event完成

- **WHEN** NextSwingLanding对应的事件成为已完成Swing Event
- **THEN** Runtime MUST把该Accepted Landing晋级为新的LastLanding
- **AND** MUST只为新的PreSwing或Swing Event建立新的NextSwingLanding

### Requirement: Ground Detection必须发布原始Capsule接触集合

Ground Detection MUST沿LastLanding到NextSwingLanding构造唯一Capsule请求。两个轴端点 MUST分别为`LastLanding + ComponentUp * CastAbove`与`NextSwingLanding + ComponentUp * CastAbove`，查询方向 MUST为`-ComponentUp`，距离 MUST为`CastAbove + CastBelow`。请求 MUST显式携带半径、最大轴段长度、Ground Layer和固定命中容量；Capsule只表示路径采集包络，不表示鞋底或最终Ground Envelope。

Unity World Query Backend MUST按最大轴段长度确定性切分Capsule轴并对每段执行真实Capsule Cast。每段Physics命中缓冲容量 MUST使用`SegmentHitCapacity`，整条路径Raw Contact页容量 MUST使用独立的`ContactCapacity`；两者都必须由同一个Ground Detection Profile正式配置并预分配。Backend MUST过滤自身Collider、初始重叠、非法几何和同分段重复命中，并发布分段索引、Surface、位置、法线、查询距离和稳定candidate identity。Backend不得改用Raycast、Sphere Cast或第二种查询算法。

#### Scenario: Capsule命中多个表面

- **WHEN** 分段Capsule Cast命中多个合法表面
- **THEN** Backend MUST在固定容量页中保留各接触的位置和法线
- **AND** MUST不先压成单个落点或中心线

#### Scenario: Capsule没有合法命中

- **WHEN** 查询没有合法接触或固定容量溢出
- **THEN** Runtime MUST发布对应typed rejection
- **AND** MUST不生成默认地面或替代查询

### Requirement: Ground Envelope必须来自可达Edge与上侧凸包

Ground Envelope Builder MUST把Raw Contacts投影到脚步纵向与Component Up组成的二维平面，按Near/Far、Bottom/Top和candidate identity稳定排序。Builder MUST在法线有效时用相邻接触的位置与法线定义地面平面；法线无效不得丢弃有效碰撞位置，只有位于两接触距离和高度范围内的平面交点 MAY成为Edge候选。

同一路径距离 MUST保留最高候选，Path Start与Target Landing MUST作为首尾端点保留。`CastAbove`和`CastBelow` MUST只用于Capsule查询范围，不得作为Reachability限值。

正式Ground Path Profile MUST提供米制`MaximumReachableVerticalEdge`。Builder MUST在同路径距离折叠前保留每个Edge的Bottom与Top，并检查全部Edge沿Component Up的垂直距离。任一Edge超过限值时，Ground Path MUST发布`UnreachableEdge`与首个Invalid Segment，不得删除障碍点后继续构造Hull，不得沿用旧Envelope，也不得把KCC Step高度、Cast范围或腿长作为替代限值。

只有全部Edge通过Reachability时，全部合法候选才 MUST形成二维上侧Convex Hull，输出从Path Start到Target Landing的连续折线。该折线 MUST位于全部保留候选的Component Up上侧或与其重合，并且只属于feet-only地面下界；它 MUST不携带Animation Clearance、不改变Foot XZ、不驱动Pelvis。

#### Scenario: 路径经过台阶

- **WHEN** 合法接触与法线定义出台阶边缘且全部Edge不超过正式Reachability限值
- **THEN** Ground Envelope MUST保留上侧Hull关键转折点
- **AND** MUST不退化为LastLanding到NextSwingLanding中心直线

#### Scenario: 路径经过不可达垂直面

- **WHEN** 任一Edge的Bottom到Top垂直距离超过`MaximumReachableVerticalEdge`
- **THEN** Ground Path MUST发布`UnreachableEdge`并记录首个Invalid Segment
- **AND** Accepted Ground Envelope MUST为空
- **AND** Raw Contacts与Edge事实 MUST保留在同一成功Seal的只读诊断页

### Requirement: Ground Path模块必须保持抽象与实现分离

Foot Placement核心 MUST只依赖现有World Query合同、Ground Envelope Builder和预分配Observation页。Unity Adapter只执行8fc已有查询与固定容量写入；不得选择Step、保存Foot状态、平滑Correction、创建Anchor、构造Pelvis或写Goal。State Machine MUST只消费不可变Observation，不得直接访问Unity查询对象。

Landing Lifecycle的Previous/Next Landing、更新死区、晋升、Prediction Error和Constraint Weight MUST迁入Foot Context并保持8fc行为。Ground Path payload与Foot Context MUST属于同一根Bank，只有State Machine可以写Context。

#### Scenario: 整帧Discard

- **WHEN** Pending Ground Path与Foot Context已生成但后续阶段失败
- **THEN** Committed Context、Path、Correction、Anchor与Pelvis状态 MUST保持上一成功帧
- **AND** 下一帧 MUST不读取被丢弃的事实

### Requirement: Foot Constraint必须由显式typed State Context驱动

每只脚 MUST使用一个固定布局`CharacterFootStateContext`集中保存Landing Event、PlantCycleConsumed、Path Residual、Contact Anchor/Progress、唯一Effective Correction、Acquire/Release Residual、顶层State与Lock Response。系统 MUST不使用字符串Key、共享Dictionary、Gameplay Blackboard、动态字段或可变Diagnostics保存Foot状态。

State Machine MUST是Context唯一写入者，并在一次Evaluate中生成Pending Context和Resolved Foot。调用方、Pelvis、Goal与Diagnostics MUST不能直接写Context。

Foot Module MUST由根Bank显式取得Committed与Pending页并执行一次Evaluate；不得保存第二套Committed/Pending指针或公开Begin、Complete、Discard生命周期。State Machine的一次Evaluate MUST内部完成Landing晋升、Next Swing捕获与Constraint解析，外层Implementation MUST不编排三个独立入口。

#### Scenario: Action硬失去脚所有权

- **WHEN** Action占用该脚且当前PlantConfidence不低于0.5
- **THEN** State Machine MUST按8fc清空输出与接触状态，并把本轮Plant保持为已消费的UnlockedSupport映射
- **AND** Action、调用方和Diagnostics MUST不直接修改Context字段

### Requirement: Future Body Translation必须写入固定Workspace

Foot Placement MUST为每个根Bank预分配固定容量Future Body Translation Workspace，并把它交给正式Translation Source写入。Translation Source MUST只更新有效Sample数量和内容，不得为每次活跃预测新建Trajectory对象、临时Sample数组或复制Sample集合。

#### Scenario: 同一帧左右脚请求未来Body平移

- **WHEN** 左右脚需要同一Body、Timeline与Duration范围的未来平移
- **THEN** Foot Module MUST在本帧只填充一次Pending Workspace并让两脚读取同一只读结果
- **AND** 预测不得产生托管堆分配

### Requirement: Resolved Foot必须形成紧凑下游合同

`CharacterResolvedFootResult`正式下游合同 MUST包含Frame/Completion/Rig/Side、Final Sole/Ankle、Effective Correction、Goal Weight、Contact Reference/Ownership、Support Eligibility、Support Weight、Support Intent Weight、Support Horizontal Error、Support Event lineage、typed Pelvis Reach Reference与Outcome。Support Eligibility MUST只包含`None`、`RetainOnly`与`AcquireAndRetain`。

State Machine MUST按8fc映射发布Eligibility：Swing、Landing和UnlockedSupport为None，Releasing为RetainOnly，Locked无论FullAnchor或Sliding Response均为AcquireAndRetain。重构阶段Support Intent Weight MUST逐值等于8fc Support Weight；Pelvis Reach Reference MUST只在现有Contact可参与Pelvis时指向与Contact Reference相同的点，其余状态为Unavailable。State、Lock Response、Path、Anchor内部历史、Acquire/Release Residual和其它Context字段 MUST不进入正式Resolved Result；它们 MAY只进入Diagnostics。

Resolved Foot Pair MUST只组合同Frame/Completion/Rig的左右Result，不重新计算状态、Support或Goal，不得成为第二Blackboard。

#### Scenario: Locked使用Sliding Response

- **WHEN** Locked脚当前使用Sliding Response
- **THEN** Resolved Foot MUST发布AcquireAndRetain及与8fc相同的Support Weight、Horizontal Error和Contact Reference
- **AND** MUST不向Primary Support暴露Lock Response

#### Scenario: Releasing只能保留支撑

- **WHEN** 脚处于Releasing
- **THEN** Resolved Foot MUST发布RetainOnly及与8fc相同的残余Support Weight
- **AND** Primary Support MUST不能从无Primary状态新获取该脚

### Requirement: Pelvis必须只消费Resolved Foot Pair并保持基线结果

Primary Support MUST只读取Resolved Pair中的Support Eligibility、Support Weight、Support Horizontal Error、Support Event identity和Contact Reference：AcquireAndRetain可获取并保留，RetainOnly只能保留，None不能参与。Selector MUST不读取Foot State、Lock Response或Context。

Stride与Pelvis MUST只读取Primary Support Result及Resolved Pair中的Final Sole、Pelvis Reach Reference和lineage。Stride端点、支持腿可达区间、Pelvis Target、Handoff与Spring MUST保持8fc逐值结果。本change MUST不读取Foot State/Lock Response、不提前接入Landing腿或读取新的Path Proposal改变Pelvis。

#### Scenario: Sliding Response脚成为Primary Support

- **WHEN** 一只Resolved Foot发布AcquireAndRetain且其内部脚处于Sliding Response
- **THEN** Primary Support MUST按8fc把它视为可获取且可保留候选
- **AND** Primary Support与Pelvis MUST不需要读取Sliding Response并产生与旧Sliding状态相同的结果
