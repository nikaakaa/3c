## ADDED Requirements

### Requirement: 可靠动画膝盖方向必须保留有符号侧向

唯一FBBIK的Animation Bend Direction Owner MUST从本帧同一Component Pose的Hip、Knee、Ankle判断可靠弯曲方向。动画几何可靠时，运行时 MUST保留该方向的符号，并按本帧原Hip-Ankle轴到加权Target Hip-Ankle轴的`FromToRotation`运输实际请求，不得只为使它与上一Stable或Applied方向的dot非负而取反。Stable MUST保存运输前的可靠动画方向，Applied MUST保存本帧实际请求，两者仍属于同一Pending BendHistory。可靠动画运输分支的零Target腿轴 MUST明确拒绝，不用默认Up、旧Goal或匿名轴补齐。动画几何退化时 MUST保留原Stable历史、Target平面投影与Applied保留政策，不用当前退化腿轴冒充旧Stable方向的来源轴。不得新增第二pole owner、Solver、Frame后膝盖修正或平滑器。

方向与其反向 MUST视为不同膝盖侧。相邻方向dot MUST反映实际向量，不得以Abs隐藏变化。Profile权重、Goal、Reach与Vendor求解保持各自职责；请求方向变化不等于实际Solved Knee翻侧，零权重也不得被宣称为方向约束已参与。

#### Scenario: 可靠动画方向与历史不在同一半球

- **WHEN** 当前动画几何可靠，但原动画或其Target腿轴运输后的方向与上一已应用方向dot为负
- **THEN** Runtime MUST保留本帧动画有符号方向，不因历史半球检查取反
- **AND** Diagnostics MUST记录真实dot并与实际Solved Knee分开解释

#### Scenario: 动画腿近似伸直而不能提供可靠弯曲方向

- **WHEN** 当前Hip、Knee、Ankle按现有几何门判定无法提供可靠动画方向
- **THEN** Runtime MUST继续既有BendHistory保留分支并发布Retained Previous事实
- **AND** 不得伪造当前动画方向、强制权重1或增加第二求解路径

### Requirement: Animation Pipeline必须发布唯一正式Foot Motion Runtime Frame

在`build-character-foot-motion-data-foundation`归档后，Projection Compiler MUST从原生AnimationClip Catalog的完整Foot Motion Curve组和匹配Foot Analysis lineage生成唯一typed Runtime payload。Payload MUST包含左右脚Step Time、Step Distance、Foot Height、Contact、Lock Mode、Lock Weight、Support及稳定Landing Event table；Event table MUST把Contact-only Landing与Predictive Landing显式分型。Contact-only Landing只可成为Current Contact且三个Swing提前时间 MUST全部为0；Predictive Landing才可成为Next Landing，并 MUST保存与同Source/Cycle/Side/ordinal一致的PreSwing、Swing、Approach Contact与Landing边界，使Runtime可以发布typed `InApproachContactToLanding`与归一化`ApproachContactToLandingProgress`事实。非零Approach区间的进度 MUST以同一Event的Approach Contact边界为0、Landing边界为1，并随正式时间单调推进；Approach时长为0时，Landing前 MUST继续发布Swing且进度为0，不得利用时间容差提前发布满权Approach。该进度只表达Prediction准备区间和诊断时钟，不得成为Foot Placement Position、Normal、Residual、PlantBlend或Goal权重；它不得从Contact Curve、Lock Weight、固定秒数或运行时累计值重新推导。Approach Contact边界 MUST取同一Foot Motion Artifact在正式LiftOff之后、Landing之前最后一次Contact由零进入正值的首个正值采样；缺少该Contact边沿时Projection Build MUST拒绝该Source，不得用LiftOff、旧Feature Phase或固定提前时间补全。非循环片段的首个Landing没有前置LiftOff时，0秒正式Contact为零才可把片段起点声明为被裁剪Swing的明确起点；0秒已经Contact时该Landing MUST作为Contact-only Event，不能进入Next Landing。若Landing本身就在0秒，同样只建立起始Current Contact。Foot Motion Curve中的Toe、Ground Pose证据 MAY进入只读诊断字段，但不得形成第二Foot Motion行为输入；Current Support使用的是同一`FinalAnimationPoseFrame`和Rig Calibration发布的Heel/Toe世界脚掌几何，不得从Curve payload或另一Animation Source重复生产。

每个表现帧 MUST从与Component Pose相同的选中Live Contribution采样一个`Foot Motion Runtime Frame`，并携带Program、Projection、Completion、Node、Source、Contribution Continuity、Clip、Cycle、Normalized Time、Event lineage与按Event table求出的PreSwing/Swing/Approach Contact/Landing阶段事实。离散Lock Mode、Landing Event与Approach Contact阶段 MUST不跨Source混合；多Source混合时 MUST使用Pose贡献链已经选定的同一正式Source，而不是按Foot字段另行择优。

Action Slot的`SourceActionInstanceId`与左右脚Live Pose Contribution Weight MUST只表示动画Source provenance和Action Pose对Original Sole基线的贡献，不得成为Foot Goal ownership token。`animation.foot-placement-weight` MUST继续是作者控制现有Foot Goal可见权重的唯一Action边界；它 MAY把Goal权重降到0，但 MUST不使Foot Motion Runtime Frame失效、不触发Foot Anchor释放或Interpolation reset。Action开始、结束与crossfade MUST继续使用同一Foot Placement Target Height、World Residual、Correction Response、Reach和Goal链，不得创建Action专用Foot路径。

Foot Placement MUST只消费这一份Frame。缺失完整Curve、Event table、Contribution归属、非有限值或stale lineage时 MUST使依赖Foot Placement的当前Pose帧typed invalid；不得读取Library Artifact、旧隐藏Foot Feature、默认Curve或另一Source作为fallback。

对应消费者迁移完成时，旧Step、Constraint、PlantConfidence和Support Projection字段及reader MUST删除。系统 MUST不长期保存新旧Foot Motion Frame并在运行时选择输出。

#### Scenario: 混合中的正式Foot Motion Source

- **WHEN** Pose由多个Live Animation Source贡献且Foot Placement需要正式Foot Motion Frame
- **THEN** Runtime MUST使用Pose贡献链选定的同一Source、Cycle、Normalized Time和Completion采样完整左右脚Frame
- **AND** MUST不分别混合Step Time、Lock Mode或Support生成不存在于任一AnimationClip的组合

#### Scenario: 正式曲线或Event table缺失

- **WHEN** 选中Source缺少任一必需Foot Motion Curve、稳定Event table或匹配Registered Curve Hash
- **THEN** Projection Build或Runtime准备 MUST拒绝该Source
- **AND** MUST不回退旧Foot Analysis Feature、PlantConfidence、默认值或另一Source

#### Scenario: Approach Contact边界驱动Plant目标准备

- **WHEN** 选中Source的同一Landing Event从Swing进入Event table声明的Approach Contact区间
- **THEN** Foot Motion Runtime Frame MUST发布同Source、Cycle、Side、ordinal与Event identity的`InApproachContactToLanding`及从0到1的`ApproachContactToLandingProgress`
- **AND** Runtime MUST只用该进度准备同Event Prediction、Prepared Target与诊断时钟，不得让它改变Position、Support Normal、World Residual、Correction Response或Goal权重；Contact Curve、Lock Weight、固定秒数、脚高、PlantConfidence或另一Source也不得重算可见接管权重

#### Scenario: 全接触循环没有Contact边沿

- **WHEN** 循环Foot Motion的某只脚在全部Active Sample中均为正式Contact，且没有LiftOff或Contact Rising边沿
- **THEN** Foot Motion Artifact MUST在sample 0生成该脚唯一Contact-only Landing Event，三个Swing提前时间全部为0
- **AND** Runtime MUST把它只发布为Current Contact，不得生成Predictive Next Landing、PreSwing、Swing或Approach Contact阶段

#### Scenario: Action Slot改变脚部动画基线

- **WHEN** Action Slot对脚骨骼具有非零Live Pose Contribution，且`animation.foot-placement-weight`在动作crossfade中改变Goal可见权重
- **THEN** Animation Pipeline MUST继续发布同一正式Foot Motion Runtime Frame与Action Pose后的Original Sole基线
- **AND** Foot Placement MUST不把Action occupancy解释为第二Goal Owner、Hard Ownership Loss或Correction Response reset
