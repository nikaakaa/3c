# character-foot-placement-presentation Specification

## Purpose

定义Corin当前Landing Prediction、Foot Placement Goal事务与唯一FinalIK FBBIK之间的正式表现边界。
## Requirements
### Requirement: Foot Placement必须是唯一Goal事务

`CharacterFootPlacementRuntime` MUST只消费同帧Component Pose、左右原子Biomechanical Step Read Page、Body Presentation、Locomotion Motion Timeline、正式Future Body Translation与当前PhysicsScene，并只输出Pelvis、LeftFoot、RightFoot三个Goal。一次Frame只能拥有一个Pending结果，并且必须由外层表现事务`Seal`或`Discard`。

系统 MUST不提供第二Grounding、第二Pelvis、LegIK、TwoBoneIK、默认地面、固定高度、fallback、兼容Goal链或FBBIK后处理。

#### Scenario: 一帧完成

- **WHEN** Foot Placement完成左右脚Landing判断
- **THEN** Runtime MUST发布同Frame、Completion与Rig identity的三个Goal
- **AND** 外层事务 MUST对该Pending结果执行一次`Seal`或`Discard`

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

### Requirement: 当前阶段必须只生成Swing脚垂直Goal

Foot Placement MUST只在Current Step权威且处于Swing、Landing Event identity与该脚NextSwingLanding一致、Ground Path全部Edge通过Reachability、状态为Accepted、Ground Envelope端点合法且垂直增量严格大于几何容差时，为该脚生成非零位置Goal。PreSwing、支撑脚、Landing完成帧、`UnreachableEdge`、其它Ground Path Rejected、身份不一致和垂直增量处于容差内的脚 MUST继续发布原生Ankle位置与旋转，但位置和旋转权重都为零。Pelvis Goal MUST继续保持零位置和旋转权重。

Swing Foot Motion MUST使用同帧Original Component Pose中的Animated Sole计算`LastLanding -> NextSwingLanding`水平纵向进度，并按该进度分别采样Ground Envelope和两个Landing端点之间的直线基线。最终Ankle与Sole MUST只沿`Component Up`增加`Ground Envelope Sample高度 - Baseline Sample高度`，该增量 MUST在数值容差外保持非负。系统 MUST保留原生动画的水平位置、抬脚高度和旋转，不得把NextSwingLanding直接作为Ankle目标，不得从输入方向、速度方向或旧IK Pose重建脚轨迹。

具有有效非零垂直增量的Swing脚Position Weight MUST只使用同帧现有`animation.foot-placement-weight`作为上限；Rotation Weight MUST为零。通过输入合同但垂直增量为零的Foot Motion MUST保持Accepted诊断并发布零权重Goal，使FullBodyIK跳过无意义的FBBIK Update。系统 MUST不叠加Landing Confidence、摆动相位、预测误差、跨帧Goal平滑、Spring、Pelvis、Foot Lock、Constraint、Anchor、脚底旋转或FBBIK后处理。

同一`LandingEventIdentity`的Accepted落点 MUST在PreSwing或Swing阶段接受实时权威预测更新。更新距离小于正式Profile的死区时 MUST复用当前落点。预测点漂移 MUST不降低Position Weight。事件完成时 MUST使用最后一个Accepted落点晋升为LastLanding，支撑脚不得继续追逐新路径。不可走 MUST只由Ground Path typed rejection表达，不得用预测误差权重假装拒绝。

#### Scenario: Swing脚经过台阶包络

- **WHEN** Current authoritative Swing Step与全部Edge可达的Accepted Ground Path属于同一Landing Event且Ground Envelope高于Landing基线
- **THEN** Foot Placement MUST把原生Ankle沿Component Up抬高对应的包络增量
- **AND** MUST保持原生Ankle在垂直于Component Up平面内的位置不变
- **AND** 唯一FullBodyIK MUST消费该同帧Goal并执行一次FBBIK

#### Scenario: Swing脚经过平地包络

- **WHEN** Ground Envelope与LastLanding到NextSwingLanding基线重合
- **THEN** Vertical Correction MUST为零
- **AND** Foot Motion MUST保持Accepted且Foot Goal Position Weight MUST为零
- **AND** 唯一FullBodyIK MUST验证Goal lineage后跳过FBBIK Update

#### Scenario: Ground Path不可用

- **WHEN** Current Step处于Swing但Ground Path为`UnreachableEdge`、其它Rejected、Envelope非法或Landing Event identity不一致
- **THEN** 该脚 MUST发布明确Foot Motion rejection和零权重Goal
- **AND** MUST不沿用上一帧Goal、默认Envelope或LastLanding到NextSwingLanding直线

#### Scenario: 支撑脚与Pelvis参与同帧GoalSet

- **WHEN** 另一只脚拥有有效Swing Foot Goal
- **THEN** 支撑脚和Pelvis Goal权重 MUST保持为零
- **AND** 本阶段 MUST不根据Swing脚高度移动Pelvis

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

### Requirement: Foot Placement诊断必须只显示当前事实

Scene诊断 MUST保留上一已提交Accepted Landing、下一Landing Event的Cached Accepted Landing、左右脚Ground Envelope和上游Invalid Segment，并从最近一次成功Seal的只读摘要显示当前Swing脚的Original Animated Sole、Corrected Sole及二者之间的实际垂直修正。Original Sole MUST使用白色小标记；Corrected Sole MUST使用对应脚颜色；修正 MUST使用细线；Active Swing的Foot Motion rejection MUST在Original Sole位置显示红色线框标记。

只读摘要与CSV MUST记录Foot Motion State、typed Reject Reason、Landing Event、Ground Path identity、Reachability状态、路径distance与progress、Original Sole与Ankle、Baseline Sample、Envelope Sample、Vertical Correction、Corrected Sole、最终Component Ankle Goal和实际Goal权重。Diagnostics与Gizmo MUST不重新采样动画、查询世界、计算Reachability、采样Envelope、计算Foot Motion或执行FBBIK，也 MUST不显示文字、伪路径或Pelvis结果。

Foot Placement Scene诊断与CSV MUST区分Ground Path、Foot Motion、Goal、FullBodyIK结果和最终物理骨骼写入。Scene Gizmo不得把Goal存在或画面抖动描述为最终骨骼已经改变；CSV除Goal与FullBodyIK字段外，MUST记录唯一final writer写入后的物理脚踝组件位置、写入Completion identity及相对Goal残差。最终骨骼消费仍 MUST通过现有同帧FootPlacement Goal Target Watch与FullBodyIK Pose Watch验证；两者 MUST具有相同Frame、Completion和Rig lineage，FullBodyIK effector diagnostics MUST记录对应脚的目标、solved position和residual，物理脚踝字段 MUST来自final writer写入后的Transform而不是Goal或Solver缓存。

#### Scenario: 查看有效Swing Foot Motion

- **WHEN** 用户查看最近一次成功Seal且具有有效Swing Foot Goal的Scene诊断
- **THEN** Corrected Sole与Original Sole的差 MUST逐值等于Component Up乘Vertical Correction
- **AND** CSV中的最终Goal、Position Weight和Pelvis Weight MUST逐值等于同一GoalSet事实

#### Scenario: 查看失败Swing Foot Motion

- **WHEN** 当前Swing脚因Ground Path或Foot Motion合同失败而发布零权重Goal
- **THEN** Scene诊断 MUST在Original Sole显示红色失败标记
- **AND** CSV MUST记录对应typed Reject Reason且不得保留上一帧Corrected Sole或Goal

#### Scenario: 验证Goal已经改变最终脚骨骼

- **WHEN** 当前Swing脚发布非零位置Goal且唯一FullBodyIK成功完成同帧求解
- **THEN** FootPlacement Goal Target Watch与FullBodyIK Pose Watch MUST具有相同Frame、Completion和Rig lineage
- **AND** FullBodyIK对应脚effector diagnostics MUST记录该Goal与最终solved position
- **AND** CSV中的FinalPhysicalWriteCompletionIdentity MUST与该帧Completion一致，FinalPhysicalAnkleComponentPosition MUST来自final writer之后的Transform
- **AND** 用户 MUST不从Scene Gizmo或抖动单独推断骨骼已经消费Goal

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

Foot Placement Runtime MUST只依赖World Query合同、Ground Envelope Builder和预分配结果页。纯Builder MUST不引用`PhysicsScene`、`Collider`、`RaycastHit`、Gizmo或Editor类型；Unity Backend MUST不选择Step、构造Hull或写Goal；Gizmo MUST不重新查询或重算算法。

Raw Contacts、Builder workspace和Envelope顶点 MUST预分配。左右脚 MUST各自只有一个Committed Page和一个Pending Page，并随外层Foot Placement事务执行`Seal`或`Discard`。

#### Scenario: 提交Foot Placement Frame

- **WHEN** 外层Frame成功Seal
- **THEN** Raw Contacts与Ground Envelope MUST作为同一Foot Placement结果原子提交
- **AND** Debug读取 MUST不改变下一帧状态
