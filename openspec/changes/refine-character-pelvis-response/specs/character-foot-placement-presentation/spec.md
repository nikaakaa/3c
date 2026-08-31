## ADDED Requirements

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

### Requirement: 共同Pelvis目标必须来自同帧双脚几何候选

在现有Pelvis生产资格内，共同期望偏移 MUST由同帧原动画Ankle A、既有Foot Goal编码及PositionWeight产生的预Reach有效Ankle X、原动画Pelvis B、单位Component Up U与正式PelvisFootProximityRadius r生成。全部几何 MUST在同一表现世界米制域，脚修正 MUST为c=dot(X-A,U)。r MUST独立显式配置、有限且大于零，不得替代LegLength或CompressionReserve。系统 MUST删除旧两个Sole最低高度差及其字段，不保留旧目标开关，也不得读取最终Physical Pose或Foot内部状态补输入。

每脚 MUST按以下互斥条件生成typed候选：|A-B|<r取OriginalWithinRadius且value=c；否则C=|B-X|²-r²，C>=0取TargetOutsideRadius且无几何候选；C<0时令b=2dot(B-X,U)，b<=0取TargetAtOrAboveReference且value=c，b>0取TargetBelowReference且value=(-b+sqrt(b²-4C))/2。共同选择 MUST优先取OriginalWithinRadius项的MAX；不存在此类且恰一项几何候选可用时取该项；其余取MIN(cL,cR)。候选无独立历史，不使用匿名哨兵代替typed可用性。

项目c是实际加权脚踝修正，不声明等价ZZZ的post-g/k标量。系统 MUST不为本候选单独在Pelvis乘g、修改Contact Anchor/Residual或改变Foot Goal权重。原Support准入、Release首选0、中性软偏好和唯一响应 MUST保持，不在目标迁移中同时更改末端Reach投影。

#### Scenario: 原动画脚靠近共同参考点

- **WHEN** 一脚或两脚的原动画Ankle与B距离严格小于正式r
- **THEN** 共同目标 MUST只从这些OriginalWithinRadius项取最大c
- **AND** MUST不借此修改脚目标、Root或另一响应

#### Scenario: 只有一项目标几何候选

- **WHEN** 没有OriginalWithinRadius项且只有一脚产生合法目标几何候选
- **THEN** 系统 MUST选取该项value并发布SingleTargetCandidate
- **AND** MUST不改用两脚平均或旧双Sole最低差

#### Scenario: 没有原动画近邻且两脚目标候选可用性相同

- **WHEN** 没有OriginalWithinRadius项且两脚都产生几何候选或都未产生几何候选
- **THEN** 系统 MUST使用两脚正式c的最小值并发布CorrectionMinimum
- **AND** 无几何候选的value占位 MUST不作为实际零测量

#### Scenario: 本帧未消费共同高度

- **WHEN** 原Pelvis资格进入Release或明确不求值
- **THEN** HeightTarget MUST发布Available=false、Kind及Selection为None，其余占位不作为测量
- **AND** MUST不沿用上次候选、补默认参考点或生成第二套目标

### Requirement: 骨盆可达硬边界必须与姿态偏好分责

Pelvis MUST从同帧typed Reach Request的真实腿长与正式安全余量形成唯一双腿硬区间。原动画额外弯曲余量 MUST只属于目标姿态偏好，不得再形成另一份输出硬夹紧。完整边界 MUST交给同一骨盆响应阶段，外部Module不得在其后再次改写Pelvis输出。

系统 MUST继续只持有一份根Bank内Spring状态、使用现有正式响应配置和Handoff规则。输出触界时 MUST保留腿长安全并处理朝外速度；几何不可达 MUST通过既有typed Reach/Goal保护表达，不得以悬空、默认目标、未授权降权或FBBIK完全伸直代替明确政策。

当前下降响应候选 MUST保持原Handoff事件判定与原清速度分支。当旧速度沿Component Up向上且本帧合法目标低于旧输出超过GeometryEpsilon时，MUST在同一次Spring积分前清除该背向速度，不再要求Handoff。旧向下速度 MUST仍按原Handoff门处理。MUST不重置输出位置、扩大真实硬边界或修改下游Foot/Bend来补偿该变化。

软姿态偏好 MUST仅在本帧原动画零偏移与双脚共同请求之间选择目标。设共同请求为r、原姿态几何给出的偏好为p0，实际preferred MUST为`Clamp(p0,Min(0,r),Max(0,r))`。软偏好 MUST不得反向生成位移或扩大共同请求的幅度；后续真正Reach MUST继续独立按正式腿长及安全余量限制目标和输出。零点只表示同帧原动画基准，不是缺输入fallback；Posture几何可用性与其软偏好是否被采用 MUST分开解释。

#### Scenario: 原动画弯曲余量大于安全余量

- **WHEN** 动画姿态希望保留更多弯曲，但实际腿长与正式安全余量允许更高骨盆
- **THEN** 动画弯曲要求 MAY影响响应目标，MUST不单独强压最终骨盆输出
- **AND** 只有统一硬区间可裁决必要的输出夹紧

#### Scenario: 当前几何要求必要下蹲

- **WHEN** 双脚目标与身体位置使当前骨盆输出超过真实硬可达边界
- **THEN** 唯一Pelvis响应阶段 MUST保持腿长安全并发布必要的边界调整
- **AND** MUST不承诺在不变脚目标、身体和腿长的同时绝对连续，也不得另加后置平滑绕过限制

#### Scenario: 姿态偏好试图把抬升请求反转为下压

- **WHEN** 双脚共同请求为正、原动画弯腿偏好给出负偏移，且本帧原Pose与几何输入合法
- **THEN** 软偏好 MUST选取原动画零偏移，不得仅为维持额外弯腿程度制造反向下压
- **AND** 后续真实Reach若仍要求负偏移 MUST照常执行，不得放宽安全区间或关闭Foot Goal

#### Scenario: 姿态偏好试图加深已有下降请求

- **WHEN** 双脚共同请求为负且原姿态偏好要求更低位置
- **THEN** 软偏好 MUST不得低于该共同请求，允许在共同请求与零偏移之间选取
- **AND** Posture的原始几何区间 MUST继续作为事实保留，不冒充真正硬边界

#### Scenario: 同一支撑下目标已降低但旧速度仍向上

- **WHEN** Handoff为None、target低于previousOutput超过GeometryEpsilon且previousVelocity大于零
- **THEN** 唯一响应 MUST以零输入速度执行原频率的一次Spring，再按原真实硬边界处理结果
- **AND** 目标、上一输出、Handoff事实和全部Foot目标 MUST不因此换代

#### Scenario: 同一支撑下目标回升但旧速度仍向下

- **WHEN** Handoff为None、target高于previousOutput且previousVelocity小于零
- **THEN** 本候选 MUST保留原输入速度和Spring规律
- **AND** MUST不复活已否决的所有方向无Handoff清速度政策

### Requirement: 骨盆观测必须区分原Pose、修正量和最终世界写回

最终Physical Pelvis世界点 MUST由唯一Physical Writer完成本次骨骼写入后取得，并与组件点及同一Completion一起冻结。Sampler MUST只消费这份正式结果，不通过采样时live Root变换冒充同Completion世界事实。原Pose输入有效性 MUST与HeightTarget/Posture求值有效性分离；产生合法Pelvis Goal的Releasing或仅LandingReach也 MUST发布真实源Pose。

#### Scenario: Release阶段检查最终骨盆Goal残差

- **WHEN** Releasing仍有非零Pelvis Goal且同Completion的最终Physical写回有效
- **THEN** 残差 MUST使用本帧真实源Pelvis组件点加正式加权Goal作为期望
- **AND** MUST不把未执行HeightTarget的默认零点当原Pose

#### Scenario: 最终世界运动与额外修正不同

- **WHEN** Root、原动画和Pelvis修正同时变化
- **THEN** Diagnostics MUST分别表达最终世界点、组件点和相对修正，不把其中一项直接命名成另一项的跳变
- **AND** 必要阶段缺失 MUST标Unavailable，不改质量规则或用占位零值宣布通过

## MODIFIED Requirements

### Requirement: Pelvis必须只消费Resolved Foot Pair

Primary Support MUST只读取Resolved Pair公开的Support Eligibility、Support Intent、Support Error、Event lineage和Pelvis Reach Reference。Stride与Pelvis MUST只消费这些正式下游结果、同帧原动画Pose与Component Up，以及由Resolved Foot编码为Goal后按正式权重解析的有效Sole/Ankle；不得读取Foot State、Transition Decision、Anchor、Path或Interpolation内部状态。

Pelvis与Foot Goal发生可达冲突时，决定依据、限制结果和失败原因 MUST通过明确typed Reach合同表达；系统不得通过降低未授权Goal权重、修改内部Foot状态或让FBBIK隐式夹紧来补全缺失政策。原动画几何 MUST来自本帧Pose输入，不能使用已被骨盆平移后的Solver Original或最终Physical结果反推。

#### Scenario: Pelvis处理本帧目标与Reach输入

- **WHEN** Resolved Pair、原Pose和有效Goal输入具有相同Frame、Completion与Rig来源
- **THEN** Pelvis MUST在唯一模块内生成一个共同Target及一份响应结果
- **AND** MUST不反向改变Foot State、Transition或Interpolation，也不得另行查询世界
