## Context

项目当前已经有一次FinalIK FBBIK求解，但普通脚步目标不是Lyra算法。现有`CharacterPredictiveFootPlacementGoalSource`把FinalIK Grounding、项目自有接触状态、锚点、pelvis reach和未接回输出的预测查询放在同一个类里。FBBIK能把收到的目标解到很小残差，普通效果仍不如Lyra，说明首先要替换的是目标生成，不是继续调solver。

本地Lyra内容审计纠正了旧文档的来源判断：Lyra实际使用`ABP_Mannequin_Base -> CR_Mannequin_FootPlant`，不是UE `AnimNode_FootPlacement`。Control Rig中可以直接确认左右脚Sphere Trace、目标/当前Z偏移、命中法线、骨盆Z、spring interpolation、Aim Bone和两个Basic IK节点。UE通用Foot Placement节点的Plant/Replant、Ball Pivot、Plant Plane和水平pelvis属于另一套实现。

## Goals

- 普通`FootGrounding`按本地Lyra Control Rig的输入、执行顺序、计算和常量生成同语义目标。
- FinalIK只承担最后一次FBBIK骨骼求解，不承担Grounding、trace、foot smoothing或pelvis planning。
- 不恢复Lyra原始两个Basic IK节点；用唯一FBBIK消费等价pelvis/foot目标。
- 普通基线在预测关闭时独立成立。
- 预测只在基线完成后改写Swing脚，不改变Lyra当前脚链。
- 旧FinalIK Grounding与重复目标路径一次删除；有效contact、anchor和pelvis reach安全能力迁入唯一`FootGrounding` owner，不保留开关、reader或fallback。

## Non-Goals

- 不照搬UE `AnimNode_FootPlacement`的Ball Pivot、Plant Plane、secondary Toe Trace、脚间分离、水平pelvis rebalance或复杂extension finalizer。
- 不保留与Lyra current target并列的第二套Grounding、脚rotation/smoothing或pelvis resolver；contact/anchor/reach只能作为同一节点中的后置有界阶段。
- 不盲删Corin当前已调好的有效行为；每项有效参数和状态必须先映射到新owner，旧combined容器才可删除。
- 不让预测修改pelvis、命中法线、另一只脚或FBBIK参数。
- 不要求项目与Lyra逐浮点或逐骨完全相同；允许厘米到米、Bone Name到BoneId、Control Rig写骨到typed Goal、Two Bone IK到唯一FBBIK四种明确表示替换。
- 不修改Gameplay KCC、Body同步、Network、Camera或动画选择。

## Evidence Audit

### Lyra内容资产

`ABP_Mannequin_Base.uasset`可确认以下输入和装配：

- `CR_Mannequin_FootPlant` Control Rig class；
- `UseFootPlacement`；
- `IsOnGround` / `IsMovingOnGround`；
- `GroundDistance`；
- `DisableLegIK`；
- `LeftKneePVCtrl`、`RightKneePVCtrl`与`PelvisCtrl`。

资产连线进一步确认：真实执行gate是`DisableLegIK <= 0 && !UseFootPlacement`；`IsOnGround`与`GroundDistance`没有直接连接该gate，`PelvisBlendSpeed=0.5`也没有进入实际执行图。项目没有可正式映射的独立`UseFootPlacement`或`DisableLegIK`参数，因此不能伪造同名运行参数或用Body Grounded替代它们。

`CR_Mannequin_FootPlant.uasset`可确认以下计算单元和状态：

- 左右`ik_foot_l`、`ik_foot_r`；
- `ProcessFootTrace`和`SphereTraceByTraceChannel`；
- `TargetLeftFootOffsetZ`、`TargetRightFootOffsetZ`；
- `CurrentLeftFootOffsetZ`、`CurrentRightFootOffsetZ`；
- `CurrentLeftFootHitNormal`、`CurrentRightFootHitNormal`；
- `DidLeftFootTraceHit`、`DidRightFootTraceHit`；
- `CurrentPelvisOffsetZ`与`PelvisBlendSpeed`；
- `ProcessFootOffset`、`AimBoneMath`、`OffsetTransformForItem`与`SetRotation`；
- `SpringInterpV2`、`SpringInterpVectorV2`和`AlphaInterp`；
- 两个`FRigUnit_TwoBoneIKSimplePerItem` Basic IK。

本地Lyra内容中没有`AnimNode_FootPlacement`引用。旧文档里“Lyra式Plant Plane/Ball Pivot”这一说法没有本地资产依据，必须删除。

### FinalIK

FinalIK现有FBBIK Pose Buffer改造已经提供：

- indexed Physical Pose读取和Pending Pose写入；
- pelvis-before-effectors应用顺序；
- spine、arm、leg chain和bend constraint；
- 单次`ReadPose -> Solve -> WritePose`；
- 固定workspace与typed failure。

FinalIK Grounding提供的是插件自己的Ray/Capsule、foot offset/rotation和pelvis逻辑。它与Lyra Sphere Trace Control Rig不是同一算法，因此不再进入正式普通脚步链。

## Selected Architecture

```text
                               +------------------------+
                               | PoseBoneIKGoals        |
                               | optional hand goals    |
                               +-----------+------------+
                                           |
Component Pose ----------------------------+--------------------+
      |                                                         |
      v                                                         v
+-----------------------------+                     +----------------------+
| FootGrounding               | Baseline Goal Set   | FullBodyIK           |
| Lyra -> Stance -> Pelvis    +-------------------->| FinalIK FBBIK once   |
+--------------+--------------+                     +----------+-----------+
               |                                                   |
               | optional                                          v
               v                                            Solved Component Pose
+-----------------------------+
   | PredictiveFootPlacementModifier |
| Modifier: Swing foot only   |
+-----------------------------+
```

普通图是`FootGrounding -> FullBodyIK`。预测图是`FootGrounding -> PredictiveFootPlacementModifier -> FullBodyIK`。两个Goal阶段都不写骨骼；只有FullBodyIK是IK solver。

## Lyra到项目的唯一映射

| Lyra来源 | Lyra职责 | 项目映射 | 不允许的扩展 |
|---|---|---|---|
| `UseFootPlacement` / `DisableLegIK` | 资产内真实执行gate | 项目没有独立正式参数；节点拓扑存在即执行 | 伪造同名参数或按动画名猜测 |
| `IsOnGround` / `GroundDistance` | AnimBP可观察输入，但未直接连接该gate | `CharacterBodyPresentationFrame`只读ground诊断事实 | 把Body Grounded变成项目伪gate |
| `ik_foot_l/r` | 每脚trace起点与目标 | Rig v4 Left/Right Foot BoneId的Component Transform | heel/toe两套当前查询 |
| `ProcessFootTrace` | Sphere Trace与目标Z/法线 | 精确PhysicsScene NonAlloc SphereCast | FinalIK Quality、Ray/Capsule择优 |
| `SpringInterpVectorV2` | hit normal平滑 | 每脚固定normal spring state | Plant Plane第二状态 |
| `SpringInterpV2` | foot offset平滑 | 每脚固定vertical offset spring state | 第二套current offset filter |
| `CurrentPelvisOffsetZ` | pelvis竖直补偿 | 唯一Pelvis期望值 | 并列pelvis target |
| Pelvis `SpringInterpV2`连线 | pelvis平滑 | 唯一pelvis vertical spring state | 使用未接入执行图的`PelvisBlendSpeed=0.5` |
| `AimBoneMath` / foot offset | 角色上方向到命中法线的最短旋转、动画脚相对旋转与竖直偏移 | 最终Foot Goal Component Transform | 重建朝向、Ball Pivot或toe-preserving offset |
| Two Bone IK + Knee PV | 满足每腿目标 | Rig reference bend constraint + 一次FBBIK | 恢复LegIK/TwoBoneIK |
| 项目Foot Analysis | stance证据 | Lyra目标后的contact滞回 | 把普通Goal总权重归零 |
| 项目surface anchor | stance稳定 | 当前连续目标与surface-local anchor连续混合；anchor只表达站稳所有权 | Swing脚anchor、第二clearance状态或第二current target |
| 项目Calibration Heel/Toe | 鞋底几何 | 从唯一Current Surface计算非负Sole Clearance Target，并入现有Foot Offset spring目标；Plant Contact持续执行单向安全约束，同一surface上上一帧鞋底仍在面上、本帧候选刚越到面下时也只消除本帧连续越界 | 新surface首次命中的Swing大缺口Value teleport、spring状态外硬平移、第二查询或第二spring |
| 项目pelvis reach | 可达保护 | 对Lyra Pelvis期望值做有界安全夹紧 | 第二Pelvis Goal或水平重平衡 |

## Baseline Execution

### 1. 执行资格与总Alpha

项目没有可正式映射的独立`UseFootPlacement`、`DisableLegIK`或单腿禁用输入，因此`FootGrounding`节点存在即执行Current Grounding。它读取同一PresentationFrame的最终Foot Placement Weight、Body ground诊断事实和Rig/Calibration identity；Foot Placement Weight只在最终Pelvis/Foot Goal alpha应用一次。缺失正式Body、Rig、Calibration或PhysicsScene时返回typed Unavailable，不构造默认平面。

Foot Placement Weight只作为Lyra Control Rig节点alpha的项目等价物应用一次。Plant Confidence、sole speed与surface distance可以在后置`Stance Stabilization`中形成contact滞回，但不能再次连续缩放整个Goal，也不能让stance判定失败等价于关闭普通Foot Placement。

### 2. 每脚当前查询

每只脚只从同一输入Component Pose的IK Foot位置构造一次Sphere Trace。Start、End、Radius、Trace Channel和命中后的Z偏移公式必须从本地`ProcessFootTrace`资产逐项记录并按厘米到米转换。UE 5.7 `FRigUnit_SphereTraceByTraceChannel`源码把`HitResult.ImpactPoint`转换到VM空间后写入名为`HitLocation`的输出，因此Lyra的`TargetFootOffsetZ = HitLocation.Z`是命中接触点在Control Rig VM空间中的绝对竖直坐标，不是球心停止位置，也不是相对动画脚踝的差值。项目映射为`PoseRoot.InverseTransformPoint(RaycastHit.point).y`，同一Impact Point同时作为surface anchor证据。项目只替换查询入口：

- 使用角色所属的精确PhysicsScene；
- 使用正式Foot Placement LayerMask；
- 排除actor自身Collider；
- 使用`Stance Stabilization.MaximumSurfaceSlopeDegrees`换算minimum normal dot，在命中选择阶段拒绝楼梯立面、近竖直圆角和超过正式坡度上限的伪支撑；
- 使用固定容量命中workspace；
- 只接受有限、合法、可走表面命中。

不得加第二条heel/toe trace、Capsule quality、velocity prediction、Root Cast或“最佳命中”选择。未命中行为必须按Lyra graph的原连线输出，不用FinalIK Grounding或默认地面补结果。

### 3. 目标与平滑

每脚保存Lyra等价的`DidTraceHit`、`TargetOffsetZ`、`CurrentOffsetZ`与`CurrentHitNormal`。更新严格遵守Control Rig数据依赖：先trace并得到target/normal，再更新normal spring和offset spring，最后形成Lyra current位置/旋转目标。Reset从当前输入Pose和Lyra默认状态重新初始化，同时清除contact、anchor与release状态，不能从上一分支恢复旧surface所有权。

所有spring/alpha常量必须来自Lyra资产。代码不得用当前FinalIK Grounding的Foot Speed/Rotation Speed或UE通用Foot Placement的stiffness替代。

### 4. Stance Stabilization

Lyra current目标形成后，Foot Analysis的Plant Confidence、sole speed、surface distance和显式Swing/stance特征进入唯一contact滞回。contact只决定能否建立、维持或释放anchor，不决定普通Foot Goal是否整体存在。没有contact时，Foot Goal继续使用Lyra current目标与节点总weight。

合法stance脚可以把同一current hit的surface identity、point与normal保存为surface-local anchor。最终脚目标在Lyra current目标与重建后的anchor目标之间连续混合；移动surface只通过该局部anchor更新世界目标。Swing、surface失效、reset、不可达、超过释放界限或contact释放时必须退出anchor。Swing脚永不创建anchor，预测也不拥有anchor。

Stance Stabilization必须使用Calibration已有Heel/Toe接触几何，在唯一Current Surface、Lyra Target Offset和目标Hit Normal形成的目标Ankle Transform下计算沿Component Up的非负`Sole Clearance Target`。该值必须与Lyra基础Target Offset相加后进入现有Foot Offset `SpringInterpV2`状态；spring只更新一次，Pelvis target仍只使用Lyra左右Target Offset最小值。

`0ef04`采样证明只有目标进入spring仍不足以保护已经承重的离散楼梯：高踏面首次进入Current Query时，spring候选Value可落后安全目标`0.17m`以上。后续`17359`又证明把该安全约束无条件用于Swing会把离散踏面高度直接写入Value：左右全部大于`0.05m`的`Sole Constraint Offset`都发生在Swing，最大单帧写回分别达到`0.134998m`与`0.128789m`。因此Plant Contact仍用同一Current Surface、当前平滑Ankle Rotation和Calibration Heel/Toe测量候选鞋底；若候选穿入支撑平面，才沿Component Up把最小正修正写回同一个offset spring Value，并把小于零的Velocity归零。

`410f`继续证明“非Plant Contact一律不约束”过度扩大了这一保护边界：88个显著穿透脚帧发生在同一Current Surface已经稳定8至14帧之后，候选从`0m`至`0.003733m`附近开始小越界，再扩大到`0.124862m`和`0.135886m`。Stance因此在同一个`FootState`内保存上一帧支撑surface identity与约束后Heel/Toe世界位置；只有surface identity不变、上一帧两点对当前平面仍不低于面、当前候选首次进入面下时，才把本帧正向缺口写回原offset spring Value。新surface首次命中且上一帧鞋底不在该面上方时仍只追踪原spring target，不恢复任意Swing大缺口瞬移。该历史不是第二clearance滤波或第二接触owner，只是同一单向碰撞边界的一帧连续性证据。

Anchor捕获必须从Plant Contact约束后的同一Current Grounding结果出发，只保存surface-local位置。稳定anchor随surface刚体变换保持该局部鞋底间隙；释放或不可达后，旧anchor MAY只作为既有pose blend来源退场，鞋底支撑权威必须立即回到Current Surface并继续使用同一Foot Offset spring，不建立独立clearance blend。`AnchorDistanceExceeded`不得在已经释放Plant Contact后逐帧重复成为新释放原因。

诊断中的`Sole Clearance Target`表达唯一Current Surface对目标Ankle的完整Component Up增量，`Offset Target`表达它与Lyra Target Offset、Current Pelvis Offset合成后的唯一spring目标，`Unconstrained Offset`表达SpringInterpV2本帧候选，`Sole Constraint Offset`表达Plant Contact或同surface连续跨面写回同一状态的向上修正，`Current Offset`表达约束后的唯一状态Value，`Residual Sole Penetration`表达anchor混合后最终Goal的剩余量。`Previous Sole Surface/Heel/Toe Plane Distance`和`Continuous Sole Contact`必须说明非Plant修正是否来自同一平面的连续越界；新surface首次命中的Swing不得产生该修正。预测改写必须在后置Modifier诊断中单独出现。它们不得改成两套查询、两套spring或spring状态外修正路径。

现有Free/Locked/Sliding实现可作为迁移来源，但最终语义必须收敛为一份明确的contact/anchor lifecycle。
不得保留旧状态机与新状态机并行。普通Goal总权重不得由contact或anchor状态切换。

### 5. Pelvis Resolve

Pelvis先按`CR_Mannequin_FootPlant`左右Target Offset最小值形成唯一竖直期望值，再用资产中的`SpringInterpV2`参数更新Current Pelvis Offset。
现有逐腿reach计算只作为最终安全夹紧。它根据最终双脚目标与Rig腿长限制共同可达区间，不生成第二目标或水平分量。
未进入执行图的`PelvisBlendSpeed=0.5`不映射为项目参数。

最终只有一个`PelvisPreSolveTranslation`和一份vertical interpolation state。FullBodyIK先在Pending Pose中应用该translation，再把双脚完整Component目标作为FinalIK绝对effector position交付。这样保留Lyra“pelvis先动、腿再解”的顺序，并避免FinalIK在`ReadPose`内部执行`LimitBend`后让预先计算的相对offset失去参考系。

### 6. Foot Goal

`ProcessFootOffset`的脚Z offset、hit normal、Aim Bone和rotation顺序先形成Lyra current Component Transform，再由Stance Stabilization有界混合为最终Foot Goal。Calibration的Sole Frame继续只表达语义脚方向；Heel/Toe contact只在后置鞋底间隙中重建接触几何。Calibration不能改变trace、Lyra smoothing、contact或pelvis算法。

Goal Application使用`FootPlacementEffectorTarget`，保存完整目标Transform和最终weight。它不再使用`GroundingEffectorTarget`、toe plant point或PlantPivotWeight。FullBodyIK在pelvis pre-solve后先应用foot pre-rotation，再把Component Position直接设置为FinalIK绝对effector position，随后只求解一次；满权重脚目标若在求解后仍超过`0.001m`残差，返回typed failure并阻断该错误Pose发布。

### 7. Knee PV到FBBIK

Lyra Basic IK的Left/Right Knee PV只负责腿弯曲方向。项目不创建PV Transform，而是在Rig v4 reference pose中编译左右腿bend constraint。Build必须确认参考膝盖平面有效；退化时失败，不使用世界前方或旧帧方向。

这一步是后端表示映射，不允许改变Lyra脚目标或追加第二腿solver。

## Prediction Extension

预测在普通基线之后工作。它的输入是Baseline Goal Set、最终动画Foot Feature、Body future transform和预测world query。只有Foot Analysis明确处于Swing且不由stance anchor拥有的脚具有rewrite资格。

Modifier可以：

- 查询Future Landing；
- 构造Ground Envelope；
- 提高Swing Clearance；
- 修改该Swing Foot slot的最终目标。

Modifier不可以：

- 改写另一只脚；
- 改写Pelvis Goal；
- 改写Lyra current trace hit/normal或spring state；
- 创建contact、anchor或stance所有权；
- 调用Grounding或IK solver；
- 把Baseline Goal与修改后Goal同时送入FullBodyIK。

预测失败或无合法未来命中时，Modifier原样传递Baseline Goal。脚从Swing交接为contact时，Modifier在该帧停止改写，后续anchor只能由FootGrounding基于current hit接管。这里的“原样传递”是同一正式Goal value的确定结果，不是fallback算法或运行时择优。

## Profile and Runtime State

`CharacterFootPlacementProfile`只包含三组：

- `Lyra Current Grounding`：Sphere Trace参数、foot offset smoothing、normal smoothing、pelvis smoothing和明确Lyra来源标识；不保存项目无法正式映射的伪gate；
- `Stance Stabilization`：最大合法surface坡度、contact进入/退出滞回、anchor blend/release、surface跟随、由Calibration几何确定的无参数鞋底间隙与pelvis reach安全界限；
- `Predictive Extension`：Future Landing horizons、Ground Envelope、Swing Clearance和未来查询约束。

删除FinalIK Grounding Quality、Max Step、Foot Radius prediction、Foot Height Speed、Foot Rotation Speed、Maximum Angle、Root Cast Radius、Overstep、Plant Plane、Toe Pivot、Pelvis Height Mode、Horizontal Rebalance和Actor Movement Compensation配置。当前Corin中已调好的contact/anchor/reach字段必须先逐项决定迁入`Stance Stabilization`、改名或确认为重复后删除，不能按旧字段名批量清空。

每Actor固定状态只保留：

- 左右脚Lyra current offset、normal spring和trace结果；
- 左右脚contact滞回、surface identity、surface-local anchor、anchor blend/release状态；
- Lyra pelvis vertical interpolation；
- pelvis reach安全夹紧状态；
- 可选预测的future landing/envelope/swing state；
- Goal Set workspace；
- FBBIK workspace。

Reset、branch replacement、Projection replacement、invalid pose和dispose必须原子清除上述状态。普通source crossfade只继续按最终输入Pose和权重求值，不创建第二生命周期。

## Execution and Transaction

`FootGrounding`与Modifier属于`WorldAwareValue`，`PoseBoneIKGoals`属于`PureValue`，`FullBodyIK`属于`PurePose`。Compiler按typed依赖生成stage table；value阶段不得持有Pose output page或写骨集合。

world query、Goal validation或FBBIK在Animancer Evaluate Barrier后失败时，阻断后续stage和FinalPublication，并把Actor Animation Runtime置为Faulted。系统不沿用上一帧Goal或部分发布pelvis/单腿Pose。

## Diagnostics

诊断按同一PresentationFrame分层：

- 执行与观察：节点存在、节点总weight、Body Grounded诊断事实和reset sequence；
- 每脚trace：start/end/radius/channel、hit、Control Rig Hit Location、Unity Impact Point、normal、target offset Z；
- 每脚平滑：current offset Z、current normal、spring velocity/state identity；
- Stance：contact证据/滞回、surface identity、anchor local/world target、blend/release和不可达原因；
- Pelvis：Lyra target、reach夹紧前后、current offset Z与spring参数；Reach失败必须包含Render Frame、左右Hip/Goal、Goal Weight、腿长、全局升降范围与最终交集；
- Goal：Lyra Target Offset、Sole Clearance Target、合成Offset Target、Current Grounding、anchor混合后的最终Ankle、鞋底支撑面、Heel/Toe平面距离、Residual Sole Penetration、最终Baseline、总weight、预测rewrite前后与Swing资格；
- FBBIK：completion、effector目标、bend constraint、iterations、residual和failure。

Diagnostics只从已完成固定workspace复制，不重新query、重新平滑、重新求解或遍历Animator Transform。Canvas和Trace必须分别显示`Lyra Current Grounding`、`Stance Stabilization`与`Predictive Extension`来源，不再显示FinalIK Grounding badge，也不得把anchor结果标成Lyra原生。

## Migration and Cleanup

1. 固化Lyra资产函数、变量、节点连线和常量清单。
2. 盘点Corin当前全部contact/anchor/pelvis配置与运行状态，逐项标记迁移owner、改名或重复删除依据。
3. 用Lyra Current Grounding payload/profile/runtime替换FinalIK Grounding payload和adapter。
4. 把有效contact/anchor/reach能力迁入FootGrounding的Stance Stabilization与Pelvis Resolve。
5. 把combined Goal Source拆成`FootGrounding`与后置Modifier。
6. 把Goal Application改为通用Foot Placement target，移除Grounding/toe pivot ABI。
7. 通过BTSMTL Document checkout、dry-run、apply与validate迁移Corin和TrainingEnemy Profile/Pose Graph；不得直接修改Unity YAML。
8. 删除旧FinalIK Grounding、重复脚目标/平滑、并列pelvis resolver、Plant Plane和已完成迁移的旧字段。
9. 普通基线完成后保持Corin不连接预测；只有用户以后明确授权时，才通过同一Document生命周期接入可选Swing预测Modifier。
10. 删除combined operation、旧reader、旧diagnostic列和兼容枚举。
11. Document apply成功后等待用户明确触发精确Character Build发布Projection、Program和Native Pose Program；不自动构建。

## Tradeoffs

### 选择真实Lyra Control Rig而不是UE AnimNode FootPlacement

业务收益是先得到用户实际认可、可回指Lyra的current grounding，同时不丢失Corin已经有用的站立稳定和移动平台表现；代价是最终普通基线不是“逐节点纯Lyra”，而是明确的`Lyra current target + 项目有界稳定层`。诊断必须把两层分开显示，不能把anchor效果伪称为Lyra原生。

### 选择FBBIK替换Lyra Basic IK

业务收益是保留项目双手、双脚和骨盆一次联动，避免恢复LegIK/TwoBoneIK分裂链；代价是最终骨骼分配不会与Lyra Two Bone IK逐浮点一致。验收关注相同输入产生相同脚/骨盆目标与同等可见接地效果，不宣称solver bitwise一致。

### 先迁移增强再接预测

业务收益是现有调参不会被无理由抹掉，且每个视觉差异仍能归因于Lyra current grounding、Stance Stabilization、Pelvis Resolve或预测改写；代价是迁移前必须逐项对账，不能直接删除combined类。项目禁止兼容和fallback，因此迁移以单次owner转移完成，不提供旧新运行时切换。

### 严格Lyra Ankle目标与Corin鞋底最终防穿透

严格停在Lyra Ankle目标的收益是逐项对照简单，代价是Corin鞋底具有实际长度，斜坡旋转后Heel或Toe仍可能进入支撑平面。项目选择把鞋底间隙放进同一Stance Stabilization owner：Lyra current仍保持可观察，最终Baseline明确记录额外平移，不把该结果冒充Lyra原生。

### Plant Contact防穿透、Swing连续性与提前跨级

只把鞋底间隙作为spring target会让已经承重的脚在高踏面首次出现时留下可达`0.18m`的穿透；把安全缺口无条件写回Value又会让Swing脚在离散踏面间单帧吸附。Current-only查询没有未来落点信息，因此不能把“同帧清除任意Swing穿透”伪装成Lyra响应式目标。当前Corin按用户业务顺序只启用普通响应式图：Plant Contact持续约束同一spring Value；非Plant脚只在同一surface上由上一帧非穿透到本帧穿透的连续边界执行单向约束；新surface首次出现的大缺口只追踪同一spring target。Predictive Modifier作为后续可选能力保持未接线。业务收益是已知支撑面上的小越界不会再积累成深下陷，同时不恢复跨级吸附；代价是未启用预测时仍不承诺在Current查询尚未看到下一踏面前提前抬脚。

`17359`进一步证明此前Plant Contact本身判错了owner输入：左脚45帧的Plant Confidence为1、动画鞋底速度低于进入阈值、最终Heel/Toe距踏面仅约`0m`至`0.002m`，但旧`surface distance`仍约为`0.289m`，因此状态一直是Swing且没有anchor。原因是contact在Lyra Foot Offset spring之前用动画鞋底量距离；高一级踏面的绝对高度被误当成脚未接触。修复后contact只读取同一帧Lyra spring候选Ankle重建的Heel/Toe到唯一支撑面的最大绝对平面距离，再决定Plant Contact和anchor。这样高台阶不再以角色零高度为锁脚条件，同时Swing仍须先由现有spring靠近踏面，避免一命中高面就硬吸附。

### 拒绝楼梯立面与保留Lyra任意命中

保留任意命中更接近Control Rig原始Trace，但Unity楼梯Collider的立面和圆角会产生低Normal Y，脚掌会朝墙面旋转。项目选择复用现有最大坡度作为查询合法性边界，使SphereCast在同一命中page中选择合法踏面；没有合法踏面时进入Lyra未命中分支，不新增第二查询。

### 沿Component Up与沿Surface Normal抬脚

沿Surface Normal位移最短，但会改变脚步X/Z落点并在楼梯边缘产生水平滑动。沿Component Up保留动画水平步幅和anchor落点，代价是斜坡越陡抬升越大。最大坡度门槛保证`Dot(ComponentUp, Normal)`保持有限，项目选择Component Up。

## Hard Stop Gates

- 无法从Lyra资产确认的常量、分支或公式不得凭经验填写；必须先补资产证据。
- 如果普通基线仍调用FinalIK Grounding、`GrounderFBBIK`，或旧contact/anchor planner与新Stance Stabilization并行拥有同一状态，停止实施。
- 如果需要恢复LegIK/TwoBoneIK、shadow skeleton、target GameObject或第二Physical writer，停止实施。
- 如果预测需要改写pelvis、stance/anchored脚、contact/anchor状态或Lyra current trace状态，停止实施并另提设计。
- 如果正式PhysicsScene、Body Frame、Rig Foot BoneId或Calibration不完整，返回typed failure，不创建默认配置。
- 任何构建和发布只能由用户明确触发；Inspector、OnValidate、selection和Preview不得自动执行。
