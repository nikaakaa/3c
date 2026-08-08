# Lyra Foot Plant迁移清单

## 文档结论

普通脚步的current grounding唯一行为参考改为本地Lyra `ABP_Mannequin_Base -> CR_Mannequin_FootPlant`。项目现有有效contact、surface-local anchor、移动surface跟随和pelvis reach安全保留，但迁入同一`FootGrounding`节点的后置稳定阶段。FinalIK只保留FBBIK求解器；UE `AnimNode_FootPlacement`和FinalIK Grounding不再是普通基线组成部分。

## 本地Lyra证据

### `ABP_Mannequin_Base`

路径：`D:/UE_Project/LyraStarterGame/Content/Characters/Heroes/Mannequin/Animations/ABP_Mannequin_Base.uasset`

已确认：

- 引用`CR_Mannequin_FootPlant`；
- 暴露`UseFootPlacement`；
- 读取`IsOnGround` / `IsMovingOnGround`；
- 保存`GroundDistance`；
- 读取`DisableLegIK`；
- 连接`LeftKneePVCtrl`、`RightKneePVCtrl`与`PelvisCtrl`。

资产连线对账结论：

- 实际执行gate是`DisableLegIK <= 0 && !UseFootPlacement`；
- `IsOnGround`与`GroundDistance`没有直接连接该gate；
- `PelvisBlendSpeed=0.5`没有进入实际执行图；
- 项目没有可正式映射的独立`UseFootPlacement`或`DisableLegIK`参数，因此不建立伪gate。

### `CR_Mannequin_FootPlant`

路径：`D:/UE_Project/LyraStarterGame/Content/Characters/Heroes/Mannequin/Rig/CR_Mannequin_FootPlant.uasset`

已确认：

- 左右`ik_foot_l/r`与`ball_l/r`骨骼；
- `ProcessFootTrace`；
- `SphereTraceByTraceChannel`；
- `TargetLeftFootOffsetZ` / `TargetRightFootOffsetZ`；
- `CurrentLeftFootOffsetZ` / `CurrentRightFootOffsetZ`；
- `CurrentLeftFootHitNormal` / `CurrentRightFootHitNormal`；
- `DidLeftFootTraceHit` / `DidRightFootTraceHit`；
- `CurrentPelvisOffsetZ`与`PelvisBlendSpeed`；
- `ProcessFootOffset`；
- `SpringInterpV2`、`SpringInterpVectorV2`与`AlphaInterp`；
- `AimBoneMath`、`OffsetTransformForItem`与`SetRotation`；
- 两个`FRigUnit_TwoBoneIKSimplePerItem` Basic IK。

已确认参数和连线语义：

- 每脚Sphere Trace从脚上方0.5米扫到下方0.5米，半径0.05米；
- `SphereTraceByTraceChannel`输出名为`Hit Location`，但UE 5.7 Control Rig源码实际把`HitResult.ImpactPoint`转换到VM空间后写入该字段；
- normal spring为`8 / 1`；
- foot offset spring为`2.5 / 1 / 0.2`；
- pelvis target为左右Target Offset最小值，pelvis spring为`2.5 / 1 / 0.2`；
- 未命中时normal target回到世界上方向；
- `ProcessFootOffset`使用`TargetOffsetZ - CurrentPelvisOffsetZ`形成相对脚目标。

本地Lyra Content静态搜索没有发现`AnimNode_FootPlacement`引用。Manny/Quinn Post Process AnimBP负责额外姿势修正，但普通Foot Plant装配来自基础AnimBP与上述Control Rig。

## 当前项目实现

代码正式链已经收口为：

```text
CharacterAnimationPresentationRuntime
  -> Current Grounding坡度过滤与Sole Clearance Target
  -> PosePlanExecutionRuntime
  -> CharacterFootGroundingGoalSource
       -> CharacterLyraCurrentGroundingSolver单一Foot Offset spring
       -> Stance在Plant Contact或同surface连续跨面时把单向鞋底约束写回同一spring Value
       -> contact / surface-local anchor / moving surface
       -> CharacterFootPlacementPelvisPlanner
       -> Baseline Pelvis + LeftFoot + RightFoot Goals
  -> optional CharacterPredictiveFootPlacementModifier
       -> one selected Swing Foot rewrite
       -> unchanged Pelvis / stance Foot / other Foot
  -> CharacterFinalIkFullBodySolver
       -> pelvis subtree translation
       -> foot pre-rotation
       -> absolute foot effector position after pre-rotation
       -> one FinalIK FBBIK Pose Buffer solve
  -> final writer
```

代码中已经完成：

- Profile schema v12只保存`Lyra Current Grounding`、`Stance Stabilization`和`Predictive Extension`；
- Pose Plan schema v22、Runtime ABI v25、operation payload v23使用独立`FootGrounding`和`PredictiveFootPlacementModifier`；
- 每脚一次SphereCast使用Impact Point，并按现有55度最大坡度过滤同一命中页中的楼梯立面和锐边；
- UE 5.7 SpringInterpV2数学、normal/offset/pelvis状态和reset顺序进入唯一solver；
- contact只控制anchor生命周期：唯一Current Surface和Calibration Heel/Toe生成`Sole Clearance Target`并加入既有Foot Offset spring target；contact的surface distance在spring求值后由候选Ankle/Rotation重建Heel/Toe并取到同一支撑面的最大绝对平面距离，不再读取IK前动画脚相对高踏面的高度差；Plant Contact时，Stance把候选鞋底的向上缺口写回同一spring Value并取消向下Velocity；非Plant脚只有在上一帧约束后鞋底位于同一surface面上、本帧候选刚进入面下时执行同一单向约束，新surface首次命中的大缺口只消费原spring target；不存在第二状态、spring状态外Ankle硬平移或Anchor清障资格；
- Pelvis Reach失败日志包含Render Frame、Lyra target/current、左右Hip/Goal、Goal Weight、腿长、左右区间、全局升降范围与最终交集；
- pelvis reach只夹紧Lyra竖直target，没有脚选择、水平重平衡或第二pelvis owner；
- Modifier严格对账Baseline header、Goal内容、workspace、Rig、producer lineage和slot顺序；
- Modifier每帧最多选择一只Swing脚，并发布selected side、Envelope、query/reject和是否rewrite；
- FBBIK只保留一次Pose Buffer solve，不查询world、不拥有Grounding；Foot Placement先在Pending Pose应用旋转，再把Component Position作为绝对effector position交付；满位置权重Foot residual超过`0.001m`时返回typed failure并阻断最终Pose发布；
- 统一Diagnostics、Inspector和CSV发布Presentation Delta、PoseRoot竖直delta、动画Ankle Component Y、minimum ground normal dot、鞋底支撑面、上一帧鞋底surface/平面距离、连续跨面判定、`Target Offset`、`Sole Clearance Target`、合成`Offset Target`、`Unconstrained Offset`、`Sole Constraint Offset`、约束后`Current Offset`、最终Heel/Toe平面距离与`Residual Sole Penetration`；

## 已删除代码路径

- `CharacterFinalIkGroundingAdapter`及runtime state；
- `CharacterPredictiveFootPlacementGoalSource`和旧combined diagnostics；
- Grounding Quality、Overstep、Root Cast、Ray/Capsule择优、secondary Toe query与velocity prediction；
- Plant Plane、Ball Pivot、脚间分离、重复Replant、toe plant point和PlantPivotWeight；
- `GroundingEffectorTarget`、旧combined operation/payload/descriptor、旧reader和backend badge；
- `RejectLeftGoal`、`RejectRightGoal`与不再拥有决策的`PlantSupportWeight`诊断；
- Directional/AllPlantedFeet模式、Horizontal Rebalance、Actor Movement Compensation与三维pelvis字段；
- 旧TwoBoneIK、LegIK与正式FinalIK MonoBehaviour组件路径。

## 作者资产状态

第25节曾把Corin迁为预测拓扑；该接线未经当前业务目标授权，现作为错误历史保留。第26节已通过正式Document checkout、dry-run与apply删除Corin的Predictive Modifier节点和三条相关edge，恢复`FootGrounding goals -> FullBodyIK foot-goals`；apply返回`applied=true`、`saved=true`、`syncState=Clean`，正式Source Pose Graph和Document editable均已不含该节点。Generated Presentation Projection仍是上次Character Build产品；按项目规则不得自动构建，只有用户明确触发Character Build后才发布与响应式Source Graph匹配的Float32/Fixed Program、Presentation Projection与Native Pose Program。TrainingEnemy本轮不改图。

## Current Spec覆盖与冲突

- current `character-foot-placement-presentation`仍把普通链写成`FootPlacement -> LegIK`，并保留Directional/AllPlantedFeet、Actor Movement Compensation与旧contact总权重口径；本change的同名RENAMED/MODIFIED Requirements完整替换这些职责。
- current `character-presentation-pose-graph`与`graph-authoring-domain-framework`仍保存Rig v3、`component.biped-leg-targets`、TwoBoneIK/LegIK和旧FootPlacement端口；本change以Rig v4、typed Goal Set与唯一FullBodyIK覆盖。
- current `character-animation-pipeline`、`character-animation-layer-runtime`和`character-pipeline-runtime`仍描述FootPlacement Pose stage后串联LegIK；本change改为WorldAwareValue Goal阶段与一次PurePose FBBIK。
- current Foot Analysis相关文字曾把Body Grounded写成普通Goal gate；本change明确Body Grounded只读诊断，节点存在即执行且总alpha只应用一次。

这些冲突是active change尚未归档造成的delta覆盖关系，不允许据此恢复旧Runtime。归档时应把上述delta合并进current specs并删除被替换的旧Requirement正文。

## 唯一有意差异

Lyra Control Rig最后分别执行左右Basic IK；项目不恢复这两个节点。项目把最终Pelvis/Foot目标交给一次FinalIK FBBIK，并用Rig reference bend constraint表达Lyra Knee PV方向。项目还明确保留Stance Stabilization与Pelvis reach安全；`Target Offset`逐值保留Lyra语义，但Foot Offset spring target额外包含项目Sole Clearance Target，Plant Contact持续执行单向鞋底安全，同surface上由上一帧非穿透到本帧穿透的非Plant脚也只消除当前连续越界，因此不能宣称最终Goal逐值等于Lyra。Swing在新踏面突然升高时优先保持连续，不承诺Current-only同帧零穿透；Corin当前不接Predictive Modifier。诊断必须分别观察Lyra输入、spring候选、连续接触或Plant Contact约束写回、Baseline Goal与FBBIK结果。

## 发布边界

第26节源码、文档、Corin Source Graph与生成产品已经更新；`ThirdPersonClient.Runtime.csproj`编译为0 error、1个无关既有warning，随后已关闭.NET build server。本change strict OpenSpec validate通过。已打开Unity完成强制Asset refresh和脚本编译，未发现项目C#或AssetDatabase错误。

用户明确授权Character Build后，正式菜单重建了2个Foot Placement geometry validation资产，消除了19个Foot Analysis binding的过期identity。Document随后把遗留edge identity从`corin.pose.local-to-component-predictive-foot-placement`迁为`corin.pose.local-to-component-foot-grounding`，apply返回`applied=true`、`saved=true`、`syncState=Clean`，source revision更新为`3b1e74baca51290ab2901ff42fb309880a57865770159a0785af336dd338520f`。精确Float32与Fixed Character Build最终发布Program、Presentation Projection与Native Pose Program；重新checkout为Clean，`btsmtl.validate` compile/semantic均成功。Source、Document与Generated Projection静态搜索均无Predictive Modifier。GameplayLab Live Snapshot对两个Fixed Actor均报告`ModifierNotCompiled`、左右脚Anchored、Sole Residual与FBBIK Residual为零；持续运行检查Console为0 error。未执行batchmode；Editor保持GameplayLab Play Mode供直接测试。Inspector、OnValidate、selection、Preview和运行时不得自动构建或修复产品。
