# Change: 先复刻Lyra Foot Plant，再接预测与FinalIK全身求解


## Why

立项时本change已经安装FinalIK FBBIK Pose Buffer backend、Rig v4、typed Goal Set和一次全身求解，但普通脚步仍是过渡实现：`CharacterPredictiveFootPlacementGoalSource`把FinalIK Grounding、项目自有contact/anchor/pelvis规划和预测字段混在一起。它与用户认可的Lyra普通脚步不是同一套算法，所以继续在现有混合实现上调阈值不能得到Lyra效果。

本地Lyra 5.7内容资产已经给出可核对的真实链路：

- `ABP_Mannequin_Base`向`CR_Mannequin_FootPlant`暴露`UseFootPlacement`、`IsOnGround`、`GroundDistance`和`DisableLegIK`，但资产实际执行gate只由`DisableLegIK <= 0 && !UseFootPlacement`连线形成；`IsOnGround`、`GroundDistance`与`PelvisBlendSpeed`没有直接进入该执行gate。
- `CR_Mannequin_FootPlant`为左右`ik_foot`执行Sphere Trace，维护目标Z偏移、命中法线、当前脚偏移和当前骨盆Z偏移。
- Control Rig使用spring/alpha interpolation平滑脚偏移与法线，按命中法线调整脚旋转，再以两个`FRigUnit_TwoBoneIKSimplePerItem`完成左右腿Basic IK。
- Lyra内容没有引用`AnimNode_FootPlacement`。UE 5.7的`AnimNode_FootPlacement`是另一套更复杂的通用节点，包含Plant/Replant、Ball Pivot、Plant Plane和水平pelvis rebalance，不能再写成“Lyra原样实现”。

因此普通接地核心不再复用FinalIK Grounding，也不把UE Foot Placement冒充Lyra。先把Lyra Control Rig从输入、查询、目标计算、平滑、骨盆到脚旋转的顺序和参数完整映射到项目；唯一求解器替换是：Lyra最后的两次Basic IK不进入项目，改由现有唯一FinalIK FBBIK同时满足骨盆、双脚和可选双手目标。在Lyra当前目标之后，保留项目已经调出效果的contact判定、surface-local anchor、移动平台跟随和pelvis reach安全，但把它们迁进同一个`FootGrounding`节点，作为有界稳定阶段，而不是与Lyra并列的第二套当前接地算法。普通基线闭合后，预测只修改动画处于摆动阶段的脚，不改变stance脚、anchor、骨盆或另一只脚。

## What Changes

- 把正式普通脚步链固定为：
  - `LocalToComponentPose`提供唯一Component Pose。
  - `FootGrounding`按固定内部顺序生成唯一Baseline Foot Goal Set：`Lyra Current Grounding -> Stance Stabilization -> Pelvis Resolve`；它只生成值，不写Pose，也不是IK solver。
  - `PredictiveFootPlacementModifier`是后置可选阶段，只重写预测资格为Swing的单脚Goal。
  - `PoseBoneIKGoals`从同一Component Pose生成不重叠的手部Goals。
  - 唯一`FullBodyIK`在一个Pending Pose中先应用Lyra骨盆偏移，再用一次FinalIK FBBIK处理双腿和可选双手。
  - `ComponentToLocalPose`只消费FullBodyIK结果。
- `FootGrounding`逐项复刻Lyra内容行为：
  - 项目没有可正式映射的独立`UseFootPlacement`或`DisableLegIK`参数，因此`FootGrounding`节点存在即执行；`animation.foot-placement-weight`只作为最终Pelvis/Foot Goal alpha应用一次，Body Grounded只保留为诊断事实。
  - 每脚从Rig声明的IK Foot位置执行一次同参数Sphere Trace；查询只走项目精确PhysicsScene、正式Foot Placement layer和self-collider过滤。
  - 明确Control Rig `SphereTraceByTraceChannel.HitLocation`在UE 5.7源码中实际由`HitResult.ImpactPoint`转换到VM空间；项目以同一Impact Point的Component竖直坐标形成`TargetFootOffsetZ`并建立surface anchor，同时输出`DidTraceHit`与`HitNormal`，不增加heel/toe双查询、Capsule quality或运行时择优。
  - Current Grounding查询使用现有Stance最大坡度换算的minimum normal dot拒绝楼梯立面和锐边伪支撑；这属于项目合法Foot Surface边界，不增加第二查询或新的作者配置。
  - 按Control Rig相同顺序和常量平滑法线、脚Z偏移与骨盆Z偏移。
  - 按Lyra `ProcessFootOffset`的Aim Bone/offset语义形成最终脚位置和旋转目标。
  - 使用Calibration已有Heel/Toe几何和唯一Current Surface计算沿Component Up的鞋底最小间隙目标；该目标作为非负增量进入现有Lyra Foot Offset spring。Stance对Plant Contact持续执行单向鞋底安全；非Plant脚只有在同一surface上从上一帧非穿透连续跨到本帧穿透时才提高原spring Value并取消向下Velocity。新surface首次命中的Swing只追踪同一spring target，不允许离散踏面高度直接改写Value。Predictive Modifier保留为可选能力，但当前Corin正式图不接入；不存在spring状态之外的Ankle硬平移、第二查询、第二clearance状态或第二IK。
  - Lyra骨盆竖直偏移是唯一期望目标；现有逐腿reach逻辑只迁为最终有界安全夹紧，不能生成第二个pelvis目标或水平重平衡。
- 保留并收口项目已经有效的普通站立稳定能力：
  - Foot Analysis的Plant Confidence、sole speed、surface distance与滞回继续判断stance contact，但不能再把整个普通Foot Goal权重归零。
  - 有效stance脚可以在当前Lyra目标上建立surface-local anchor；当前目标与anchor连续混合，Swing脚永不建立anchor。
  - anchor保存surface identity与局部位置/法线，移动surface时从同一surface重建世界目标；失效、reset、不可达或转入Swing时显式释放。
  - Lyra Pelvis Z先形成期望值，保留的腿长可达逻辑只对该值做有界安全夹紧；最终仍只有一个Pelvis Goal和一份平滑状态。
- 建立一份可追踪的Lyra资产对照表。每个项目计算必须对应Control Rig函数、变量、节点或AnimBP门控；空间从UE厘米换算为Unity米、骨骼名字换成Rig BoneId、Control Rig写骨改成typed Goal是允许的表示映射，算法顺序和默认值不允许自行替换。
- FinalIK只保留FBBIK求解职责：
  - 删除正式链中的FinalIK Grounding adapter、`Grounding.Leg`状态、Grounding Quality/Overstep/Root Cast等Profile字段和`GroundingEffectorTarget`语义。
  - `FullBodyIK`消费普通绝对Goal、Lyra Foot Placement Goal与Pelvis Pre-Solve Translation；Foot Placement Position直接作为FinalIK绝对effector position交付，不在FinalIK内部`LimitBend`前预先换算一次性position offset，也不调用Grounding或`GrounderFBBIK`。
  - 满权重Foot Placement Goal求解后若位置残差超过`0.001m`，返回typed failure并阻断错误Pose发布，不用第二solver或后处理回拉。
  - 不挂`FullBodyBipedIK`、`GrounderFBBIK`、`LimbIK`组件，不创建target GameObject或shadow skeleton。
- 不保留与上述单一路径竞争的额外算法：FinalIK Grounding的Ray/Capsule current target、第二套脚rotation/smoothing、`hasAnchor ? contactWeight : placementWeight`式总权重切换、并列pelvis resolver、Plant Plane、Ball Pivot、secondary Toe Trace与普通阶段的未来落点。现有contact/anchor/reach能力必须先逐项映射到`Stance Stabilization`或`Pelvis Resolve`，确认正式owner后才删除combined旧容器；不保留旧新切换、兼容reader或fallback。
- 预测扩展只能在普通基线完成后接入：
  - 只消费最终动画Foot Feature中的Swing资格、Next Landing、Body future transform、Future Support、Ground Envelope与Swing Clearance。
  - 只改写Swing脚同一个Goal slot；stance/anchored脚、另一只脚、当前命中法线、Lyra平滑状态和Pelvis Goal逐值原样传递。
  - 脚从Swing交接为contact后，Modifier停止改写，由`FootGrounding`在当前Lyra目标上接管contact与anchor；预测不得创建anchor。
  - 预测失效时输出原始Baseline Goal，不建立第二普通Grounding或第二solver。
- `CharacterFootPlacementProfile`收敛为`Lyra Current Grounding`、`Stance Stabilization`与`Predictive Extension`三组。第一组保存从Lyra资产确认的trace、offset/normal/pelvis spring参数与来源identity；第二组保存contact滞回、anchor混合/释放、surface跟随和pelvis reach安全界限；第三组只保存未来落点与摆脚clearance参数。不得保存伪造gate、backend选择、FinalIK Grounding字段、并列pelvis模式或UE `AnimNode_FootPlacement`字段。
- 保持Rig v4、Calibration v4、Foot Analysis、typed Goal Set、Pose/Value DAG、唯一Animancer Evaluate Barrier、唯一Physical final writer和动画帧事务不变。
- 先盘点并记录Corin与TrainingEnemy当前已调参数与运行语义，再将每项有效contact/anchor/reach能力迁到唯一新owner。作者资产必须通过BTSMTL Document checkout、dry-run、apply与validate生命周期迁移为`FootGrounding -> FullBodyIK`或显式Modifier拓扑；不得直接改Unity YAML。所有有效字段完成映射后，删除combined `CharacterPredictiveFootPlacementGoalSource`、旧FinalIK Grounding配置、重复target/smoothing/pelvis路径和废弃reader，不删除尚未迁移的有效调参，也不保留兼容枚举。Generated Projection、Program与Native Pose Program只在Document apply成功且用户显式触发Character Build后发布。

## 实施前代码对账与正式目标

实施前代码已经证明FBBIK本身不是主要差距：脚Goal成功进入solver时残差接近零。差距发生在solver之前：当时Goal Source使用FinalIK Grounding的Ray/Capsule组合生成当前目标，再把项目接触权重、anchor、可达区间和pelvis策略混在同一个类中；Corin还保存过`Unlocked`等配置。这里的问题是当前目标来源和控制权混杂，不是contact、anchor与reach能力本身没有价值。预测查询类型虽已存在，但当时尚未形成显式Goal Modifier。

本次文档把现有combined实现定义为迁移来源，不再整体判定为垃圾，也不再把它描述成“Lyra质量基线”。正式完成条件是：当前脚查询、基础位置/旋转和平滑能回指本地Lyra资产；contact/anchor/reach安全各自只有一个明确owner并在Lyra目标之后有界工作；FinalIK只出现在最后的FBBIK backend。预测没有接入时，项目已经拥有Lyra思路的普通接地效果并保留现有站立稳定能力；接入预测后，只多一段Swing脚Goal改写。

## 鞋底防穿透取舍

- 严格Lyra只约束Ankle Goal，无法保证Corin有长度和厚度的Heel/Toe在斜坡旋转后都位于支撑面上；项目选择在同一FootGrounding稳定层增加鞋底最小间隙，因此Current Grounding公式仍可对账Lyra，但最终Baseline Goal不再宣称逐值等于Lyra Ankle Goal。
- 拒绝超过现有最大坡度的Current hit可以避免楼梯立面和锐边法线把脚掌转向墙面；代价是没有合法踏面命中时按Lyra未命中分支处理，而不是把任意Collider命中都当作支撑。
- 间隙沿Component Up应用，保留动画脚步的水平落点和楼梯前后位置；代价是在陡坡上位移会比沿surface normal更长。现有最大坡度门槛同时给出有限分母，不允许近竖直支撑进入该计算。
- 单次Ankle Sphere Trace只能把命中面当作当前鞋底支撑平面，能避免Heel/Toe穿入该平面，但脚掌跨台阶边缘时可能比heel/toe双查询略保守。项目继续禁止第二Current Grounding查询，避免恢复另一套Support owner。
- 把完整鞋底间隙作为spring外输出直接应用到Swing脚会单帧吸到高一级；只乘`AnchorBlendWeight`又会让无anchor的Swing失去清障。`0ef04`证明安全目标只进spring时可落后`0.17m`以上，`17359`反证任意Swing无条件写回Value，`410f`又证明非Plant一律不约束会让同一踏面上从小越界积累到`0.124862m/0.135886m`。项目最终选择“同一spring target + Plant Contact持续约束 + 同surface连续跨面单向约束 + 新surface大缺口不瞬移”。代价是需要在同一Stance `FootState`保留上一帧鞋底边界证据；收益是不接预测时也能阻止已知踏面上的连续下陷，同时不恢复跨级吸附。

## Impact

- 修改`character-foot-placement-presentation`、`character-full-body-ik-pose-solver`、`character-presentation-pose-graph`、`graph-authoring-domain-framework`、`character-animation-pipeline`、`character-animation-layer-runtime`、`character-pipeline-runtime`、`character-pipeline-definition-authoring`、`character-animation-presentation-authoring`与`character-animation-foot-analysis-artifact`。
- 影响Pose Graph authoring/compiler/runtime、Foot Placement Profile、world query输入、Goal ABI、FinalIK adapter边界、Preview、diagnostics与Corin内容资产。
- 不修改Gameplay KCC、Simulation状态、Network packet、Body root motion、Motion Matching查询、Camera、Motion Warping或Timeline事件。
- 不恢复Lyra的两个Basic IK节点；项目继续只执行一次FinalIK FBBIK。这是唯一有意的后端差异。

## 与Current Spec及Active Change对比

- current `character-foot-placement-presentation`仍要求旧`FootPlacement -> LegIK`、Free/Locked/Sliding、moving anchor、Support Envelope和复杂pelvis策略。本change以Lyra current grounding替换旧current target，保留并收口contact/anchor/moving surface和pelvis reach安全，只把Future Support/Envelope留给后置预测扩展。
- current `character-presentation-pose-graph`仍包含旧FootPlacement/LegIK/Rig v3合同。本change继续使用已定义的Rig v4、Goal Source与唯一FullBodyIK拓扑，但把普通Goal Source的行为权威改为Lyra Control Rig。
- 本change前一版把FinalIK Grounding与UE `AnimNode_FootPlacement`混合作为普通基线；本次明确删除该目标，避免三套当前脚逻辑继续叠加。
- active `add-discrete-stair-presentation`同步为：普通FootGrounding每脚一次Lyra Sphere Trace，预测Modifier才拥有Future Support/Envelope；楼梯Surface与KCC边界不变。
- active Motion Matching与BlendSpace change只提供最终Pose和Foot Feature，不读取FootGrounding、预测或FBBIK结果，边界不变。

## References

- 本地Lyra基础AnimBP：`D:/UE_Project/LyraStarterGame/Content/Characters/Heroes/Mannequin/Animations/ABP_Mannequin_Base.uasset`
- 本地Lyra Foot Plant Control Rig：`D:/UE_Project/LyraStarterGame/Content/Characters/Heroes/Mannequin/Rig/CR_Mannequin_FootPlant.uasset`
- 本地Manny Post Process：`D:/UE_Project/LyraStarterGame/Content/Characters/Heroes/Mannequin/Rig/ABP_Manny_PostProcess.uasset`
- 本地Quinn Post Process：`D:/UE_Project/LyraStarterGame/Content/Characters/Heroes/Mannequin/Rig/ABP_Quinn_PostProcess.uasset`
- UE 5.7通用Foot Placement源码，仅作明确反例和可选后续能力参考：`C:/Program Files/Epic Games/UE_5.7/Engine/Plugins/Animation/AnimationWarping/Source/Runtime/Private/BoneControllers/AnimNode_FootPlacement.cpp`
- 本地FinalIK FBBIK核心：`Assets/Plugins/RootMotion/FinalIK/IK Solvers/IKSolverFullBody.cs`
