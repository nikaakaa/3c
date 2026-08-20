# Design

## 1. 设计目标

预测式负责回答“脚下一次准备去哪里以及途中需要越过什么”；响应式负责回答“当前动画脚附近真实可接触表面在哪里”。两者都不能直接写骨骼，必须在同一 Foot Placement Pending Frame 中收敛为每脚唯一 Resolved Proposal。

最终运行链固定为：

```text
同帧原生 Component Pose + 每脚生物力学事实 + Body/PhysicsScene
├─ Predictive Proposal：Future Landing -> Ground Path -> Envelope -> 当前预测修正
└─ Reactive Proposal：Footprint Query -> 当前接触点/法线 -> 当前响应修正
                         ↓
             Per-Foot Proposal Arbiter
                         ↓
          Resolved Contact / Resolved Foot Goal
                         ↓
          Support Lock + 唯一 GoalTransition
                         ↓
             最终左右脚 Sole / Ankle
                         ↓
                  唯一 Pelvis
                         ↓
          一个 GoalSet -> 一个 FBBIK -> 一个 writer
```

响应模块是 Foot Placement 内部能力，不是独立 Pose Graph 节点、MonoBehaviour 或后处理组件。

## 2. iStep 的使用边界

本 change 直接修改并复用 `FootIK.findNewIKPos`，保留：

- 用脚掌长宽构造有面积的 BoxCast。
- 用有限 SphereCast 检查或修复 BoxCast 的坡面法线。
- 过滤非法法线和超过地面坡度的命中。
- 把脚底厚度、前后接触范围和表面法线纳入落脚几何。
- 对同一表面的小幅测量变化设置死区。

从正式调用路径中拆除：

- `OnAnimatorIK` 和 `Animator.Get/SetIK*`。
- iStep Grounded SphereCast 与 Grounded 生命周期。
- Body Placement、身体倾斜、肩膀和膝盖 Hint。
- glue、步事件、外推、Reset Lerp、脚目标与骨盆 SmoothDamp。
- Effects、Terrain Texture 与 Demo 内容。

修改后的唯一实现位于 `Assets/_HoaxGames/iStep/Scripts`：把原 `findNewIKPos` 和必要接触稳定计算移动到不依赖 Animator writer 的 Contact Solver。`FootIK` 与项目 Adapter 必须调用同一 Solver，GameScripts 不得复制或重新表达一份等价 BoxCast、SphereCast 和坡面数学。正式 Character Runtime 允许通过窄 Adapter 引用该 `HoaxGames` Solver，但不得调用 `FootIK.OnAnimatorIK` 或启用其 Body Placement。

## 3. 响应式模块合同

### 3.1 输入

每脚请求至少包含：

```text
Frame / Completion / Rig lineage
Foot Side
Original Ankle Position / Rotation
Original Heel / Toe / Sole Center
Sole Forward / Right / Up
Component Up
Sole Half Width
Cast Above / Cast Below / Footprint Half Thickness
Normal Repair Radius / Maximum Repair Separation
Ground Layer / Minimum Ground Normal Dot
Self Collider filter
```

Heel、Toe、Sole Frame 与 Sole Half Width 属于 Rig Calibration。查询距离、厚度、法线修复半径、坡度和测量死区属于 Foot Placement Profile。角色 Profile 不保存另一份脚长；脚长由同一 Calibration 的 Heel/Toe 几何得到。

### 3.2 输出

修改后的 iStep Solver 直接返回从现有 `IKResult` 收敛出的 Contact Result；项目 Adapter 将其映射为 `CharacterFootReactiveContactProposal`，至少包含：

```text
Availability / typed Reject Reason
Frame / Completion / Rig lineage
Foot Side / Measurement Revision
Surface Identity
Contact Point / Contact Normal / Query Distance
Original Sole / Original Ankle
Reactive Sole / Reactive Ankle
Position Correction Along Component Up
Penetration / Clearance
UsedNormalRepair
```

响应式目标保留动画脚的水平位置，只用合法接触面计算当前 Sole 沿 Component Up 的有符号接触修正；脚掌朝向继续由统一 Support Orientation 使用 Resolved Contact Normal 计算。模块不直接输出 FinalIK Goal，也不决定 Pelvis。

### 3.3 拒绝原因

拒绝必须 typed，至少区分：

- `InactivePhase`
- `WorldContextUnavailable`
- `InvalidRigGeometry`
- `InvalidRequest`
- `NoFootprintHit`
- `InitialOverlapOnly`
- `SelfColliderOnly`
- `InvalidSurfaceGeometry`
- `GroundAngleExceeded`
- `ContactOutsideAcquisitionRange`

拒绝时不得沿用上一帧 Proposal 作为当前事实。Committed 测量只能在同 Surface 且本帧已经获得合法新测量、变化位于正式死区内时复用。

## 4. Footprint 查询

### 4.1 定向 BoxCast

原 `findNewIKPos` 的 footprint 中心、前向偏移、BoxCast half extents、查询起点、查询距离、命中点回算和脚底高度补偿 MUST原样移动到 Contact Solver，再把 Animator、`m_transform` 和组件字段改成显式 Request 参数。项目 Adapter 用同帧 Heel/Toe、Sole Frame、Calibration 和 Profile 建立这些参数，不重新计算另一套落脚数学。

Contact Solver 改为接收显式 PhysicsScene、Layer、Trigger policy 与自碰撞判断，并沿用 iStep 的主 BoxCast 结果。它必须过滤：

- 自身 Collider。
- 初始重叠与负距离。
- 非有限点、法线和距离。
- 零法线与小于 `MinimumGroundNormalDot` 的表面。

响应式查询不按名称、Tag、Transform 层级或 `FootIK` 组件启用状态选择表面。若为自碰撞过滤而需要完整命中集合，只允许在修改后的同一 Solver 内扩展原查询，不得在 GameScripts 再写第二个 Physics 查询实现。

### 4.2 SphereCast 法线修复

原 `findNewIKPos` 的 SphereCast、`alpha/beta` 比较、两命中点叉乘法线修复和坡度限制继续由同一 Contact Solver执行。项目集成只增加以下正式限制：

- Surface Identity 与主命中一致。
- 点位与主命中不超过 `MaximumRepairSeparation`。
- 法线合法且坡度可接受。

满足时，Solver 继续使用原 iStep 几何选择更可靠的法线；不满足时保持主命中法线。SphereCast 不得选择另一踏面、替代 BoxCast 接触点或在 BoxCast 失败时生成默认接触。

因此一次 Reactive Contact Query 可以包含一个 footprint cast 和一个受约束的 normal-repair cast，但只发布一个 Surface、一个点和一个法线。

## 5. 测量稳定与最终运动平滑分离

响应模块每脚拥有唯一 Measurement Pending/Committed 页：

```text
MeasurementRevision
SurfaceIdentity
ContactPoint
ContactNormal
QueryDistance
```

Pending 从 Committed 初始化，但只有本帧合法查询才可发布 Proposal：

- 同 Surface 且点位差不超过 `ContactPointDeadZone`、法线夹角不超过 `ContactNormalDeadZoneDegrees` 时，复用 Committed 测量及 revision。
- 同 Surface 但超出死区时提交新测量 revision。
- Surface 改变时立即提交新测量 revision，不用死区冻结旧表面。
- 查询失败时当前 Proposal 为 Rejected；旧 Committed 只保留事务历史，不驱动当前 Arbiter。

这层只抑制物理查询的毫米级噪声。最终脚目标的连续性仍由裁决之后的唯一 `CharacterFootGoalTransition` 负责；Pelvis 仍只使用现有临界弹簧。不得在响应模块增加脚目标 Lerp、SmoothDamp、外推或 Reset Blend。

## 6. 每脚响应所有权曲线

现有 Foot Analysis 已提供每个 Step 的：

```text
EventPhase
ReleasePhase
LiftOffPhase
ApproachContactPhase
LandingPhase
```

先用现有生物力学相位得到每脚原始接触权重：

```text
phase < ReleasePhase:
    support = 1
ReleasePhase <= phase < LiftOffPhase:
    support = 1 - inverseLerp(ReleasePhase, LiftOffPhase, phase)
LiftOffPhase <= phase < ApproachContactPhase:
    support = 0
phase >= ApproachContactPhase:
    support = inverseLerp(ApproachContactPhase, LandingPhase, phase)
```

随后分别为左右脚采样同一条作者曲线：

```text
reactiveOwnership = ReactiveOwnershipCurve.Evaluate(support)
predictiveOwnership = 1 - reactiveOwnership
```

曲线输入与输出都限制在 `[0,1]`。Build 必须要求曲线端点为 `(0,0)` 与 `(1,1)`、键值有限、输出不越界并单调不减。左右脚共享曲线形状，但各自使用自己的 Step 相位，因此一只脚可以完全响应式支撑，另一只脚同时保持预测式摆动。

`animation.foot-placement-weight` 继续表达最终 Foot Placement 总强度。`reactiveOwnership` 只用于决定目标来源，不能直接作为 FinalIK Position Weight，否则中间混合阶段会无故降低总体约束强度。现有 Lock Preparation 与 Unlock 生命周期保持独立，不从该曲线重新计时。

## 7. Predictive 与 Reactive Proposal

最终接入前，现有预测式计算必须明确输出 `CharacterFootGoalProposal`，至少包含：

```text
SourceKind = Predictive
Availability / Reject Reason
Landing Event / Surface / Ground Path lineage
Original Ankle / Sole
Target Ankle / Sole
Position / Rotation intent
```

响应模块使用相同外层 Proposal 合同，但 `SourceKind = Reactive`，lineage 指向 Measurement Revision。两者都只表达当前候选，不写 GoalSet。

混合时先换成相对同帧 Original Ankle/Sole 的 Component 空间修正：

```text
predictiveCorrection = predictiveTarget - original
reactiveCorrection = reactiveTarget - original
resolvedCorrection = lerp(
    predictiveCorrection,
    reactiveCorrection,
    reactiveOwnership)
resolvedTarget = original + resolvedCorrection
```

不得直接混合来自不同帧、不同 Original Pose 或不同 Rig lineage 的绝对世界点。

## 8. Surface 兼容与 typed handoff

两个 Proposal 只有满足下列条件才可连续混合：

- Frame、Completion、Rig、Foot Side 和 Landing Event 上下文一致。
- Surface Identity 相同，或接触点沿 Component Up 与水平面的差都位于正式兼容阈值内。
- 法线点积不小于 `MinimumCompatibleNormalDot`。
- 两个目标都位于当前腿部和 Foot Placement 的正式可达合同内。

兼容时按曲线混合修正。不兼容时不得把两个点做中间插值：

1. Arbiter 延续上一 Committed Owner，只要该 Owner 本帧仍合法。
2. 当响应权重到达曲线终点、Reactive Proposal 位于正式接触获取范围且 lineage 完整时，执行 `PredictiveToReactiveSurfaceHandoff`。
3. Handoff 当帧只切换 Raw Resolved Proposal；最终骨骼连续性由裁决后的唯一 GoalTransition 从上一 Committed correction 向新 correction 收敛。
4. 原 Owner 已失效而另一 Proposal 又不满足接管合同时，本帧发布 typed rejection，不用旧目标、默认地面或无条件 fallback 补洞。

Arbiter 每脚拥有一份 Pending/Committed Owner 状态，至少记录 SourceKind、Landing Event、Surface、Proposal Revision 与 handoff reason；只有外层 Seal 才提交。

## 9. 落地、支撑锁定与 Ground Path

`NextSwingLanding` 继续是预测世界事实。接近落地时，Arbiter 可以把预测修正逐渐交给响应修正。事件完成时：

- 若本事件存在合法 Resolved Contact，Landing 生命周期把该点、法线、Surface、来源和 Proposal Revision 晋级为 `LastLanding`。
- 若 Reactive Proposal 不可用但 Predictive Proposal 按相位与 lineage 仍是合法 Owner，则 Resolved Contact 仍可来自预测；这是显式所有权裁决，不是默认地面或隐藏 fallback。
- 没有合法 Resolved Contact 时不得晋级虚构 LastLanding。

下一次 Ground Path 的起点使用已提交 `LastLanding`，终点仍是下一事件的预测 `NextSwingLanding`。这样路径从真实支撑点出发，又不让响应式模块预测未来。

支撑锁定后，Support Lock 拥有固定 Contact Anchor。响应查询可以验证当前 Surface，但不得按当前动画脚位置逐帧搬动 Anchor；Surface 身份改变或接触失效时进入正式 typed unlock/reacquire，不建立 iStep glue 状态。

## 10. 骨盆与最终 Goal 顺序

左右脚必须先完成：

```text
Proposal query
-> ownership arbitration
-> support acquisition / lock
-> final GoalTransition
```

之后才能把最终 Position Weight 投影到 Sole，并送入唯一 Pelvis Builder。Pelvis 只能看到最终两只脚结果，不得读取 Predictive 与 Reactive 两套原始目标分别计算高度。

最终顺序为：

1. Pending 从 Committed 初始化。
2. 计算预测提案与响应提案。
3. 分别计算左右脚生物力学接触权重和曲线权重。
4. 每脚执行唯一 Arbiter。
5. 落地完成时提交 Resolved Contact；支撑阶段进入现有 Lock/Slide/Unlock。
6. 每脚执行一次最终 GoalTransition。
7. 用最终左右脚 Sole 计算唯一 Pelvis。
8. 写 Pelvis、LeftFoot、RightFoot 三个 Goal。
9. 执行一次 FullBodyIK 和一次 final writer。
10. 外层 Seal 或 Discard 全部 Landing、Measurement、Owner、Lock、Transition 与 Pelvis Pending 状态。

## 11. Profile 与 Calibration

Rig Calibration 增加每脚 `SoleHalfWidth`，并升级 schema、revision 和 geometry validation lineage。Heel/Toe 继续定义脚掌前后范围，不新增重复 FootLength。

Foot Placement Profile 增加 `ReactiveContact` 设置：

```text
CastAbove
CastBelow
FootprintHalfThickness
NormalRepairRadius
MaximumRepairSeparation
MinimumGroundNormalDot
ContactPointDeadZone
ContactNormalDeadZoneDegrees
MaximumCompatibleHorizontalDistance
MaximumCompatibleVerticalDistance
MinimumCompatibleNormalDot
ReactiveOwnershipCurve
```

所有米制、角度和曲线必须严格校验并进入 Profile Revision。Corin 与 TrainingEnemy 必须使用同一正式 Profile 值；不得在 Prefab 上增加 iStep 参数副本或运行时 override。

## 12. Foot Placement面板对比模式

现有Foot Placement调试面板增加一项session-local `Proposal Mode`：

```text
Predictive Only
Reactive Only
Hybrid
```

三种模式使用同一套Predictive Proposal、修改后iStep Reactive Proposal、Arbiter状态和最终Goal链：

- `Predictive Only`：Arbiter有效响应权重固定为0，只让Predictive Proposal拥有Raw Resolved Goal；Reactive Proposal仍可作为只读对照显示。
- `Reactive Only`：响应相位有效时Arbiter有效响应权重固定为1，Predictive Proposal只作只读对照；Reactive Proposal无效时发布typed rejection，不回退预测目标。
- `Hybrid`：使用左右脚各自的生物力学接触权重和正式`ReactiveOwnershipCurve`。

模式切换 MUST形成typed Debug Proposal Mode Handoff，并继续从上一Committed最终修正经过唯一GoalTransition收敛；不得重置角色Pose、启动第二Solver或创建旧目标队列。Support Lock、Pelvis和FinalIK只消费切换后的唯一Resolved Proposal。

该选择只存在于Editor/Development diagnostics session，面板关闭、Runtime重建或diagnostics interest释放时恢复`Hybrid`。它不得写入Foot Placement Profile、Projection、Prefab、Character State、Snapshot或网络，也不得进入正式Player业务UI。正式角色只有Hybrid配置真相。

面板同时只读显示：

```text
左右脚Biomechanical Support Weight
曲线原始Reactive Ownership
模式覆盖后的Effective Ownership
Predictive / Reactive Proposal状态
Surface兼容性
Committed / Pending Owner
Resolved Source
```

## 13. 诊断

每脚只读摘要和 CSV 增加：

```text
ReactiveQueryExecuted / RejectReason
Footprint Origin / HalfExtents / Rotation / Cast Distance
Box Surface / Point / Normal / Distance
NormalRepair Surface / Point / Normal / Used
Committed / Pending Measurement Revision
BiomechanicalSupportWeight
ReactiveOwnershipCurveValue
Predictive / Reactive Proposal availability and correction
Compatibility result / reason
Committed / Pending Owner
Resolved Source / Surface / Contact / Correction
Handoff reason
Promoted LastLanding source and revision
```

Gizmo 只显示成功 Seal 的 footprint、响应点、预测点和 Resolved 点，不重新查询或裁决。诊断必须继续记录同一 Frame、Completion 与 Rig lineage，并与最终 Goal、FBBIK solved position 和 Physical Bone writer 对账。

## 14. 失效与事务

- PhysicsScene、Rig、Calibration 或 Profile 不完整时，响应模块发布 `WorldContextUnavailable` 或 `InvalidRigGeometry`，不得构造平面。
- Body 离地、有限 Action 占脚、Pose discontinuity、Reset、Retarget 或 Dispose 时，响应 Measurement 与 Owner Pending 状态按正式硬失效规则清理；不得让响应 Goal 淡出到空中。
- Barrier 前失败由外层 Discard 恢复全部 Committed 状态；Barrier 内或之后失败继续遵守现有 Animation Runtime Faulted 规则。
- 响应事实只属于 Presentation，不进入 Character State、World State、Snapshot、Hash 或网络 packet。

## 15. 实施顺序

前四段可以在不修改预测最终组装的情况下并行完成：

```text
直接拆分 iStep Contact Solver
-> Profile / Calibration
-> 项目 Reactive Adapter
-> Measurement state / diagnostics
```

最后统一接入按以下顺序完成：

```text
预测结果降为 Proposal
-> Arbiter
-> Resolved LastLanding / Support Lock
-> 唯一 GoalTransition
-> 唯一 Pelvis
-> 唯一 GoalSet / FBBIK / writer
```

最终接入前不得把响应模块挂到 Prefab、Animator IK Pass 或临时 MonoBehaviour 上验证；需要观察模块时只通过 Foot Placement 的只读 diagnostics interest 输出，不增加第二运行入口。
