# Design: GASP式Foot Placement与Leg IK显式分段

## Context

当前作者与执行链为：

```text
LocalToComponentPose
  -> Left Arm TwoBoneIK
  -> Right Arm TwoBoneIK
  -> FootPlacement(world query + pelvis + left/right limb solve)
  -> ComponentToLocalPose
```

当前`CharacterFootPlacementRuntime`已经在实现上包含Planner、query、constraint、pelvis和target计算，`CharacterComponentPoseLimbSolver`负责解析式两骨链；但是`CharacterFootPlacementNativeControl`把二者重新压成同一个operation输入，作者图和运行诊断无法观察中间合同。

本地GASP 5.7提供三种互斥模式：

| Mode | GASP链路 | 本change结论 |
|---|---|---|
| 0 | Off | 不创建项目运行模式；作者通过正式图与Weight决定是否存在/介入 |
| 1，默认 | Foot Placement node -> Leg IK | 采用职责和顺序 |
| 2 | CR_Biped_FootPlacement | 只作为算法参考，不引入第二runtime |

GASP的动画离线阶段通过`AM_Copy_IKFootRoot`和`AM_FootSpeed_L/R`准备IK foot与速度曲线。运行时Foot Placement根据脚速、脚高和trace处理plant、ground alignment与pelvis，Leg IK再把腿链固定到IK foot位置。项目已有更丰富的sole feature、future landing与Ground Envelope，因此不删除预测能力，只重建清楚的生产边界。

## Goals

- 作者图能明确看见Foot Placement规划与Leg IK骨骼求解是两个阶段。
- world query、contact lifecycle和骨骼数学拥有不同输入输出、execution domain和诊断。
- 平面法线、膝盖方向和joint target术语不再混用。
- 任何position weight下都保持上下腿物理长度。
- Foot Placement Weight在最终链只应用一次。
- 保持Rig v3、Foot Analysis artifact、Pose workspace、单次Animancer Evaluate和单次final writer。
- 迁移结束后只保留一条正式Foot Placement与Leg IK链。

## Non-Goals

- 不复制GASP Control Rig、Chooser、Motion Matching数据库或完整Animation Blueprint。
- 不向Corin Skeleton添加UE风格`ik_foot_root/ik_foot_l/ik_foot_r`物理骨骼。
- 不把Foot contact改成Timeline手工区间或Marker语义。
- 不让LegIK读取PhysicsScene、Body、AnimationClip、Profile或constraint state。
- 不让FootPlacement写Animator Transform或在图外执行solver。
- 不新增测试；端到端验收由用户执行。
- 不自动Build、Compile、分析或发布资产。

## Decision 1: 采用GASP职责分层，不复制IK骨骼数据载体

目标作者链为：

```text
LocalToComponentPose
        |
        v
FootPlacement [WorldAwarePose]
  input : pose.component
          animation.foot-placement-weight
  output: pose.component            -- 只应用pelvis component offset
          component.biped-leg-targets
        |                 |
        +--------+--------+
                 v
LegIK [PurePose]
  input : pose.component
          component.biped-leg-targets
  output: pose.component             -- 只解左右Physical腿链
        |
        v
ComponentToLocalPose
```

GASP用IK骨骼在Pose内部携带foot target，因为UE Mannequin Skeleton已经标准化拥有这些骨骼。Corin当前没有该标准，Rig v3的Virtual Bone又是由Physical Pose派生的只读数据。把Virtual Bone改成可写控制骨会同时改变source capture、BlendStack、Mask、Additive、Virtual dependency和final writer语义。

项目改用稳定`component.biped-leg-targets`端口。它只存在于编译Pose Plan的同帧固定workspace，不序列化每帧值，也不进入Animator、Snapshot或Network。这样保留GASP的可组合边界，同时比隐藏IK骨骼更明确。

### Tradeoff

- typed targets：依赖可见、跨Rig可验证、无需修改Skeleton；需要Graph Framework、Compiler和workspace支持非Pose瞬时value。
- UE式IK骨骼：可只用Pose edge串联，但会给没有IK骨的角色引入第三类可写骨骼和更大的Rig ABI，不采用。
- 继续复合节点：Graph简单，但Planner/Solver错误继续隐藏，无法得到GASP式独立Pose Watch，不采用。

## Decision 2: FootPlacement应用Pelvis，LegIK只解腿

FootPlacement生成完整`CharacterFootPlacementPlan`后，把`PelvisComponentOffset`应用到节点Component Pose输出的pelvis subtree。它同时把左右脚目标降低为`CharacterBipedLegTargets`：

```text
FrameIdentity
CompletionIdentity
RigId / RigRevision
Left / Right:
  TargetAnkleComponentPosition
  TargetAnkleComponentRotation
  AnimatedBendPlaneNormal
  PreferredBendPlaneNormal
  BendStabilizationWeight
  PositionWeight
  RotationWeight
  ExtensionRatio
  ConstraintState / DecisionReason
```

LegIK必须同时消费该FootPlacement输出Pose和targets。Compiler验证两个edge来自同一个FootPlacement call-site；targets输出只能有一个LegIK consumer。LegIK不得重新应用pelvis，也不得重新读取Foot Placement Weight。

这与GASP的结果边界一致：Foot Placement完成pelvis与目标准备，Leg IK只把Physical腿链固定到目标。区别只是GASP把目标写进IK bones，本项目写进typed workspace。

### Tradeoff

- Pelvis由FootPlacement应用：FootPlacement Pose Watch能直接显示地形高度补偿，LegIK保持无状态单一职责。
- Pelvis由LegIK应用：Plan求解更原子，但FootPlacement Pose输出成为纯透传，作者无法区分pelvis错误和腿链错误，不采用。

## Decision 3: LegIK使用bend plane normal ABI

Planner保留动画弯曲平面：

```text
animatedNormal = normalize(cross(knee - hip, ankle - knee))
finalNormal = normalize(lerp(animatedNormal, preferredNormal, stabilizationWeight))
```

LegIK不把`finalNormal`当作膝盖方向。它以应用Pelvis后的当前Hip和目标Ankle计算：

```text
targetAxis = normalize(targetAnkle - hip)
planeNormal = normalize(projectOnPlane(finalNormal, targetAxis))
kneeDirection = normalize(cross(targetAxis, planeNormal))
```

Planner生成preferred normal时继续使用固定同侧约定；LegIK只在normal退化时返回typed failure，不猜测默认轴。所有公开、native、diagnostic和UI字段统一使用`BendPlaneNormal`或`KneeDirection`，不得使用含糊`BendDirection`。

### Tradeoff

- normal ABI：与Planner现有数学一致，接近伸直时仍能表达稳定平面；solver必须显式做normal到direction转换。
- knee direction ABI：solver更直接，但Planner需要在pelvis应用前提前选定方向，目标轴变化后可能失真，不采用。
- joint target position：接近UE TwoBoneIK通用接口，但会重新引入作者可拖动pole和第二几何真相，不采用。

## Decision 4: 先混合目标再完整解算，禁止组件位置后混合

当前solver先求完整目标，再分别对joint/end Component Position执行`Lerp`。在`0 < PositionWeight < 1`时，这会改变Hip-Knee和Knee-Ankle距离。

LegIK改为：

```text
effectiveTarget = lerp(animatedAnklePosition, targetAnklePosition, positionWeight)
solveDistance = clamp(distance(hip, effectiveTarget), minimumReach, maximumReach)
solve full knee and ankle positions with fixed upper/lower lengths
apply hip and knee rotations from solved chain
ankleRotation = slerp(animatedAnkleRotation, targetRotation, rotationWeight)
rebuild descendants
```

当position weight为零时节点保持输入Pose；大于零时任何输出都必须保持Rig链长。目标越界由Planner先拒绝，solver仍保留typed reach结果作为完整性保护，但不得静默硬拉到错误方向。

## Decision 5: Foot Placement Weight只有FootPlacement拥有

唯一`animation.foot-placement-weight`进入FootPlacement节点。Planner以该值决定contact准入、constraint target、free clearance、pelvis和最终每脚weights。FootPlacement输出Pose与targets已经是最终权重结果。

LegIK没有第二Weight端口；staged executor不得把同一参数再次乘到pelvis、position、rotation或bend。需要逐动画控制时，作者继续在Pose Source或有限Action Timeline的正式typed curve中编辑唯一Weight。

### Tradeoff

- 单owner：曲线响应与诊断可预测，不会出现平方权重。
- LegIK再提供Alpha：能够单独调solver，但会形成第二Foot Placement控制曲线和容易误接的半完成pelvis状态，不采用。

## Decision 6: Calibration合法性通过发布identity进入Runtime

完整几何validator需要Sampling Rig和Calibration Preview姿势，只能在Editor作者与Build边界执行。正式链为：

```text
Calibration Apply
  -> Geometry Validation Result
  -> Foot Analysis Artifact(identity包含validation hash)
  -> Explicit Character Build
  -> Projection Foot Calibration Validation Identity
  -> Runtime exact identity match
```

Runtime不得访问Sampling Rig，也不得只调用数值级`Calibration.RequireValid()`就声称几何合法。Runtime创建必须核对Projection发布的validation identity、Calibration revision、Rig revision和artifact identity；不匹配直接失败。

## Decision 7: Graph与stage约束防止半链路

Capability注册：

- `FootPlacement`：`pose.component + parameter.float.normalized -> pose.component + component.biped-leg-targets`，execution domain为`WorldAwarePose`。
- `LegIK`：`pose.component + component.biped-leg-targets -> pose.component`，execution domain为`PurePose`。

Validator要求：

- 每个到达OutputPose的FootPlacement targets必须由一个LegIK消费。
- LegIK的Pose与targets必须来自同一个FootPlacement call-site。
- targets不得连接TwoBoneIK、ModifyBone、GraphOutput、OutputPose或另一个FootPlacement。
- 每个最终Output路径最多一个有状态FootPlacement实例。
- targets的Rig和Completion必须匹配LegIK当前输入Pose。

Compiler生成FootPlacement world-aware stage，再生成LegIK pure pose stage。FootPlacement失败阻断LegIK与FinalPublication；LegIK失败同样阻断后续stage。两者都已经位于Animancer Evaluate Barrier后，失败使Actor Animation Runtime进入Faulted。

## Decision 8: 诊断按规划与求解分层

FootPlacement Pose Watch显示：

- 输入动画脚和应用Pelvis后的Pose；
- Free/Locked/Sliding；
- heel/toe/current/future support；
- ankle target、surface normal、prediction、pelvis offset；
- 最终position/rotation/bend weights。

LegIK Pose Watch显示：

- 输入腿链和最终腿链；
- bend plane normal与转换后的knee direction；
- upper/lower length、target/effective/solve distance；
- reach state、residual和失败原因。

Scene颜色固定为动画输入、FootPlacement目标和LegIK结果三类，不从Transform反推，不重新query或求解。

## Migration

1. 先安装typed target value、capability、validator、compiler和workspace，但不发布Corin资产。
2. 把现有复合operation拆成FootPlacement world-aware operation与LegIK pure operation，并删除旧ABI；中间提交不得保留两套可选执行路径。
3. 修正共享解析式Limb Solver数学和术语，通用TwoBoneIK继续使用其明确joint-target ABI，不与Foot Placement normal ABI混用。
4. 迁移Corin Pose Graph，删除复合节点运行payload并新增唯一LegIK节点与targets edge。
5. 显式重建Foot Analysis artifact并执行一次Character Build，发布Projection与Native Pose Program。
6. 删除旧control、旧重复weight、旧diagnostics字段和旧文档口径。

迁移阶段若Corin Graph与Projection schema不匹配，正式Runtime创建必须失败；不得自动插入LegIK、透传旧FootPlacement结果或继续使用旧Projection。

