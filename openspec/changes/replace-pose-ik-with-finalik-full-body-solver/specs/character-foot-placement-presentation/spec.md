## RENAMED Requirements

- FROM: `### Requirement: Foot Placement规划与Leg IK求解必须在Pose Graph中显式分段`
- TO: `### Requirement: Lyra Foot Grounding与Full Body IK必须在Pose Graph中显式分段`
- FROM: `### Requirement: Foot Placement Planner与Leg IK Solver必须使用typed目标合同`
- TO: `### Requirement: Lyra Foot Grounding与Full Body IK必须使用typed目标合同`
- FROM: `### Requirement: Leg IK必须保持Physical腿链长度`
- TO: `### Requirement: Full Body IK必须由成熟后端保持biped约束`
- FROM: `### Requirement: Foot Placement与Leg IK必须提供分层诊断且保持热路径有界`
- TO: `### Requirement: Lyra Foot Grounding与Full Body IK必须提供分层诊断且保持热路径有界`
- FROM: `### Requirement: Foot Rotation必须应用语义foot frame差值`
- TO: `### Requirement: Foot Rotation必须复刻Lyra ProcessFootOffset语义`
- FROM: `### Requirement: 地面查询必须形成有限连续 Support Envelope`
- TO: `### Requirement: 当前Grounding与未来Support Envelope必须显式分离`
- FROM: `### Requirement: Pelvis 必须由支撑腿和腿长约束统一求解`
- TO: `### Requirement: Pelvis必须使用Lyra期望值与唯一Reach安全夹紧`
- FROM: `### Requirement: 每只脚必须使用有限约束生命周期`
- TO: `### Requirement: 普通FootGrounding必须统一维护Lyra与Stance运行状态`
- FROM: `### Requirement: Locked Foot 必须支持移动 Surface`
- TO: `### Requirement: Stance Anchor必须支持移动Surface`

## ADDED Requirements

### Requirement: 普通FootGrounding必须以本地Lyra Control Rig作为Current Grounding行为权威

正式`FootGrounding`的`Lyra Current Grounding`阶段 MUST逐项对照本地`ABP_Mannequin_Base -> CR_Mannequin_FootPlant`的资产gate事实、每脚Sphere Trace、目标Z偏移、命中法线、脚偏移平滑、法线平滑、Pelvis Z期望值及`ProcessFootOffset`目标形成顺序。项目没有可正式映射的独立`UseFootPlacement`或`DisableLegIK`参数，因此节点存在即执行，Body Grounded只作为诊断事实。每个Current Grounding计算和Profile字段 MUST能够回指Lyra资产中的变量、函数、节点连线或常量。系统 MAY执行UE厘米到Unity米、Bone Name到Rig BoneId、Control Rig写骨到typed Goal、Two Bone IK到唯一FinalIK FBBIK四种表示映射，但 MUST不以FinalIK Grounding、UE `AnimNode_FootPlacement`或项目旧planner替换Lyra current算法。

普通`FootGrounding` MUST在同一节点内按`Lyra Current Grounding -> Stance Stabilization -> Pelvis Resolve`生成唯一Baseline Goal Set。它 MUST保留有界contact滞回、surface-local anchor、移动surface跟随与pelvis reach安全，但 MUST把这些能力诊断为项目稳定层，不得伪装成Lyra原生。它 MUST不包含FinalIK Grounding Quality/Ray/Capsule current target、Plant Plane、Ball Pivot、secondary Toe Trace、脚间分离、水平Pelvis Rebalance、Future Landing或与上述三阶段竞争的第二控制路径。

Stance Stabilization MUST把现有最大合法surface坡度应用到同一次Current Grounding SphereCast的命中选择，并使用Calibration Heel/Toe几何计算唯一`Sole Clearance Target`。该目标 MUST作为非负增量并入既有Foot Offset `SpringInterpV2`目标。Plant Contact MUST对求值后的同一spring候选执行向上鞋底安全约束。非Plant脚 MAY只在Current Surface identity与上一帧相同、上一帧约束后Heel/Toe对当前平面均不低于面且本帧候选首次进入面下时执行同一约束；该历史 MUST保存在现有Stance `FootState`内且不得形成第二spring或第二clearance owner。正修正 MUST写回该spring的同一个Value并取消向下Velocity。新surface首次命中的Swing MUST只追踪同一spring target，`Sole Constraint Offset` MUST为零；提前跨级由显式`PredictiveFootPlacementModifier`处理。系统 MUST不在spring状态之外直接平移Ankle，不得使用`AnchorBlendWeight`充当清障资格，也不得创建第二current query、第二clearance状态、第二rotation目标、第二配置或第二solver。

#### Scenario: Swing脚首次查询到高一级踏面

- **WHEN** Current SphereCast命中合法高踏面且Foot Offset spring候选仍让Heel或Toe低于该面
- **THEN** Stance Stabilization MUST保留完整`Sole Clearance Target`作为同一脚现有spring target
- **AND** MUST不直接改写该Swing脚的Value或Velocity，`Sole Constraint Offset` MUST为零
- **AND** 图中显式存在的Predictive Modifier MAY依据Future Landing提前改写该Swing Goal

#### Scenario: 非Plant脚在同一支撑面上连续越界

- **WHEN** Current Surface identity与上一帧相同，上一帧约束后Heel/Toe对当前支撑平面均不低于面，且本帧spring候选第一次使Heel或Toe进入面下
- **THEN** Stance MUST只把本帧最小Component Up缺口写回同一Foot Offset spring Value
- **AND** MUST发布连续接触证据，且不得创建anchor、第二clearance状态或spring状态外Ankle平移

#### Scenario: 实现者准备添加Plant Plane

- **WHEN** Lyra资产对照中没有Plant Plane节点、状态或参数
- **THEN** 本change实现 MUST拒绝把Plant Plane加入普通FootGrounding
- **AND** MUST不以“提升Lyra质量”为理由保留旧项目路径

#### Scenario: 普通基线执行一个接地帧

- **WHEN** FootGrounding节点、节点总weight、Rig、Calibration与PhysicsScene均合法
- **THEN** FootGrounding MUST先按Lyra顺序生成current目标，再由Stance Stabilization与Pelvis Resolve生成唯一Baseline Goals
- **AND** 最后 MUST只由唯一FullBodyIK写入骨骼Pose
- **AND** Body Grounded MUST只作为同帧诊断事实而不关闭普通Goal

### Requirement: 普通基线与预测扩展必须是有序Goal阶段

`FootGrounding` MUST输出完整Baseline Goal Set。可选`PredictiveFootPlacementModifier` MUST消费该Goal Set并只重写Foot Analysis明确标记为Swing且未由stance anchor拥有的单个Foot slot；stance/anchored脚、另一只脚、Pelvis slot、Lyra current trace结果、Stance状态和Lyra平滑状态 MUST逐值保持。普通Goal与Modifier最终Goal MUST不作为两个并行Foot输入同时连接FullBodyIK。Compiler、Build Validator与Runtime MUST拒绝同slot竞争、隐式Goal Merge、最后写入获胜或运行时择优。

#### Scenario: 普通基线不接预测

- **WHEN** Pose Graph连接`FootGrounding -> FullBodyIK`
- **THEN** 一个表现帧 MUST执行完整Lyra普通脚步目标生成和一次FBBIK
- **AND** MUST不创建Future Landing query或预测状态

#### Scenario: 预测修改摆动脚

- **WHEN** Pose Graph连接`FootGrounding -> PredictiveFootPlacementModifier -> FullBodyIK`且左脚具有Swing资格
- **THEN** Modifier MAY重写LeftFoot slot
- **AND** RightFoot与Pelvis slot MUST逐值保持Baseline结果

#### Scenario: Swing脚进入contact

- **WHEN** 被预测改写的脚在当前帧进入合法stance contact
- **THEN** Modifier MUST停止改写该Foot slot并逐值传递Baseline Goal
- **AND** 只有FootGrounding MAY根据current hit建立或维持surface-local anchor

#### Scenario: 预测失去合法落点

- **WHEN** Swing脚Future Landing或Ground Envelope无合法结果
- **THEN** Modifier MUST原样输出该脚Baseline Goal
- **AND** MUST不调用第二Grounding算法或第二IK solver

## MODIFIED Requirements

### Requirement: Rig Calibration必须同时约束Editor分析与Runtime Solver

`CharacterFootPlacementCalibration` MUST只保存左右脚contact距离、anchor surface distance与鞋底间隙及未来预测共用的heel/toe几何、sole frame及geometry validation identity。Rig v4 MUST显式保存Pelvis、左右Hip/Knee/Foot及FBBIK biped映射。Lyra Current Grounding rotation MUST不依赖Calibration重建脚朝向；Stance Stabilization MAY使用Heel/Toe接触几何重建最终Ankle目标下的鞋底两点，但 MUST只产生同一个Ankle Goal的平移修正。Calibration MUST不保存preferred bend、Knee Direction、pole、Plant Pivot或FinalIK Grounding参数；左右Knee PV的项目等价物 MUST由Rig reference pose编译成FBBIK bend constraint。

#### Scenario: Corin膝盖参考平面退化

- **WHEN** Rig reference pose无法形成合法Hip-Knee-Foot平面
- **THEN** Build MUST失败并报告对应Leg BoneId
- **AND** Runtime MUST不使用世界前方、旧PV Transform或上一帧方向

### Requirement: Foot Rotation必须复刻Lyra ProcessFootOffset语义

每脚最终rotation MUST从Lyra平滑后的Hit Normal与Control Rig `AimBoneMath`/rotation连线形成：以Component上方向到Hit Normal的最短旋转乘回输入动画Ankle Rotation，等价于`AimBoneMath`对`ik_foot_root` Primary Axis的旋转再乘`IKFoot`相对`root`的动画旋转。系统 MUST保持Lyra先更新trace/normal、再更新foot offset、最后形成rotation/position目标的顺序。FootGrounding MUST不按投影forward重新构造朝向，不调用FinalIK Grounding rotation offset，也不得增加Maximum Angle、Ankle Twist Reduction、Ball Pivot或toe-preserving rotation。

鞋底间隙 MAY只改变该rotation对应的Ankle竖直状态。它 MUST先计算使Calibration Heel/Toe到同一支撑平面的最小有符号距离回到零的完整`Sole Clearance Target`，再把该值并入既有Foot Offset spring；求值后 MAY只通过上述单向约束提高同一spring Value。最终Goal MUST发布相对有效支撑面的`Residual Sole Penetration`。它 MUST不修改rotation、不在spring状态之外直接平移Ankle，也不得引入第二rotation owner。

#### Scenario: 平地动画脚处于摆动姿势

- **WHEN** 平滑Hit Normal等于Component上方向而动画Ankle保留抬脚pitch
- **THEN** FootGrounding MUST保留输入动画Ankle Rotation
- **AND** MUST不把sole up强制重建为地面法线

### Requirement: 当前Grounding与未来Support Envelope必须显式分离

普通`FootGrounding` MUST为每脚从同一输入Component Pose的Rig Foot BoneId执行一次Lyra参数的NonAlloc SphereCast，并输出`DidTraceHit`、Hit Location、Hit Normal与Target Foot Offset Z。Lyra `Hit Location` MUST按UE 5.7 Control Rig源码解释为`HitResult.ImpactPoint`转换到VM/Component空间后的点；Target Foot Offset Z MUST取该点的Component绝对竖直坐标，不得减去动画Ankle高度，也不得使用swept sphere中心。它 MUST使用精确PhysicsScene、正式Foot Placement LayerMask、self-collider过滤、固定容量workspace和由Stance最大坡度换算的minimum ground normal dot。超过该坡度的楼梯立面、锐边与近竖直命中 MUST在同一命中page选择阶段被拒绝；该命中同时是Stance Stabilization建立anchor和鞋底间隙的唯一current surface证据。系统 MUST不执行heel/toe双查询、Ray/Capsule Quality组合、Root Cast、velocity prediction或第二套Current Support查询择优。

可选`PredictiveFootPlacementModifier` MAY只为Swing脚执行Future Landing、Ground Envelope与Swing Clearance查询。Future结果 MUST不覆盖普通current hit、current normal或Pelvis Goal。全部命中 MUST来自合法有限Collider，不得使用默认平面、隐藏Collider或Gameplay `CharacterTraversal` Ramp。

#### Scenario: 左脚站在斜坡

- **WHEN** 左Foot SphereCast命中合法斜坡
- **THEN** FootGrounding MUST按Lyra计算Left Target Offset Z和Hit Normal
- **AND** MUST先通过Lyra normal/offset平滑形成Left Current Goal，再由Stance Stabilization形成唯一Left Baseline Goal

#### Scenario: SphereCast同时接触楼梯立面与踏面

- **WHEN** 同一次Current Grounding命中page包含超过最大坡度的立面和合法踏面
- **THEN** 查询 MUST拒绝立面并选择最近合法踏面
- **AND** MUST不启动第二条toe、heel或edge查询

#### Scenario: 稳定stance脚在斜坡旋转后Heel进入支撑面

- **WHEN** 唯一Current Surface和目标rotation使Calibration Heel或Toe低于支撑平面
- **THEN** Stance Stabilization MUST把完整非负Sole Clearance Target加入现有Foot Offset spring目标
- **AND** Plant Contact成立时 MUST把求值后仍存在的最小正缺口写回同一spring Value
- **AND** Pelvis Reach MUST消费该连续状态形成的Goal

#### Scenario: Swing脚的Current hit切换到高一级踏面

- **WHEN** 同一脚没有anchor所有权且唯一Current SphereCast从低踏面切到高踏面
- **THEN** Stance Stabilization MUST记录新支撑面的完整`Sole Clearance Target`
- **AND** MUST把它并入既有Lyra Foot Offset spring目标
- **AND** MUST不因`AnchorBlendWeight=0`关闭该目标或把离散间隙直接写入Swing脚Ankle Goal

#### Scenario: 当前SphereCast未命中

- **WHEN** Lyra current SphereCast没有合法命中
- **THEN** FootGrounding MUST按Lyra Control Rig未命中分支更新目标与状态
- **AND** MUST不调用FinalIK Grounding或默认地面补命中

#### Scenario: Swing脚跨越楼梯边缘

- **WHEN** Modifier为Swing脚取得多个合法Future Envelope segment
- **THEN** Modifier MAY提高该脚clearance或调整Future Landing
- **AND** 当前SphereCast的Hit Normal与另一只脚Baseline Goal MUST不变

### Requirement: 普通FootGrounding必须统一维护Lyra与Stance运行状态

每脚普通状态 MUST包含Lyra等价的current foot offset、target foot offset、current hit normal、trace hit、normal spring与offset spring，以及唯一contact滞回、surface identity、surface-local anchor、anchor blend/release、上一帧约束后鞋底的surface identity与Heel/Toe世界位置及明确失效原因。该鞋底历史 MUST只供同surface连续越界判断，reset或无合法surface时 MUST清除。Pelvis状态 MUST包含Lyra vertical期望/平滑和唯一reach安全夹紧状态。Initialization、Body reset、branch replacement、Projection replacement、invalid pose与dispose MUST从当前输入Pose和Lyra默认值原子重建Lyra状态，并清除contact、anchor、鞋底连续性、release与reach历史。

动画Foot Feature中的Plant Confidence、sole speed与显式Swing/stance特征 MAY形成唯一contact进入/退出滞回。运行时`surface distance` MUST在同一帧Lyra Foot Offset spring求值后，以其候选Ankle和Rotation重建Calibration Heel/Toe，并取两点到唯一Current Surface的最大绝对平面距离；它 MUST不读取IK前动画鞋底到高踏面的高度差，也 MUST不使用角色零高度判断锁脚。
contact MUST只决定anchor建立、维持与释放，不得连续缩放或归零普通Foot Goal总weight。
FootGrounding MAY迁移Free/Locked/Sliding语义，但 MUST不保留第二状态机、Plant Plane、heel lift或toe pivot。

唯一Current Surface、Lyra Target Offset和目标Hit Normal形成的目标Ankle MUST通过Calibration Heel/Toe重建目标鞋底点并计算`Sole Clearance Target = max(0, -minimumDistance) / Dot(ComponentUp, SupportNormal)`。FootGrounding MUST把该值加入同一脚既有Foot Offset spring的target，再由该spring的唯一value/velocity/previous-target状态形成候选Current Grounding Goal。Stance MUST用当前平滑Rotation和同一Current Surface复核候选Heel/Toe；Plant Contact成立时，或非Plant脚满足同surface上一帧非穿透、本帧首次穿透的连续边界时，正向安全缺口 MUST累加到同一value，且向下velocity MUST归零。新Current Surface首次命中的Swing MUST保留spring候选，不得把离散高度直接改写value。Pelvis target MUST继续只使用Lyra左右Target Offset最小值，不能把鞋底目标变成第二Pelvis owner。

Anchor捕获 MUST从Plant Contact约束后的同一连续Current Grounding Goal出发并保存surface-local位置。最终Current/anchor Goal MUST再次通过Calibration Heel/Toe计算`Residual Sole Penetration`用于诊断，但 MUST不建立spring状态外硬平移。Anchor释放后旧anchor MAY只在既有blend退场期间继续提供pose来源；鞋底支撑与安全平面权威 MUST立即回到唯一Current Surface，且同一释放原因 MUST不逐帧重复触发。Swing、无anchor、anchor不可达释放以及重新使用current surface时 MUST继续使用同一Foot Offset spring，不得建立独立clearance blend。没有合法支撑面时 MUST不伪造平面或固定高度补偿。

#### Scenario: 高台阶上的当前脚已贴住踏面

- **WHEN** Plant Confidence和sole speed满足contact进入条件，且Lyra spring候选重建的Heel/Toe均处于唯一Current Surface的contact进入距离内
- **THEN** Stance MUST允许该脚进入Plant Contact并从同一候选Goal捕获anchor
- **AND** MUST不因IK前动画Ankle低于该踏面或Target Offset高于角色零高度而继续判为Swing

#### Scenario: Swing脚刚查询到高踏面但当前候选尚未到达

- **WHEN** Current Surface已切到高踏面，但Lyra spring候选Heel或Toe仍超出contact进入距离
- **THEN** Stance MUST保持Swing且不得建立anchor
- **AND** MUST继续由唯一Foot Offset spring向该面收敛，不得直接吸附到高一级踏面

#### Scenario: Rollback替换表现分支

- **WHEN** Body ResetSequence变化
- **THEN** 左右脚与Pelvis Lyra spring state MUST在应用新Goal前重建
- **AND** MUST不存在旧surface anchor、release或contact权重可带入新分支

### Requirement: Pelvis必须使用Lyra期望值与唯一Reach安全夹紧

Pelvis期望值 MUST按`CR_Mannequin_FootPlant`中左右Target Foot Offset与Current Pelvis Offset Z的原始连线和运算生成，并使用资产实际连接的`SpringInterpV2`参数更新唯一current value；未进入执行图的`PelvisBlendSpeed=0.5` MUST不映射为项目配置。随后Pelvis Resolve MUST根据最终双脚Goals与Rig腿长把该值有界夹入共同可达区间。FootGrounding MUST输出一个Component空间竖直`PelvisPreSolveTranslation`，且 MUST不存在第二Pelvis Goal、AllPlantedFeet/Directional目标模式、水平分量、Heel Lift或Actor Movement Compensation。

FullBodyIK MUST先在Pending Component Pose应用Pelvis translation，再把完整Foot Component Position作为FinalIK绝对effector目标交付，最后执行一次FBBIK。FootGrounding与FullBodyIK都 MUST不写VisualRoot。

#### Scenario: 左脚目标低于右脚目标

- **WHEN** Lyra Pelvis graph根据左右Target Offset得到新的Target Pelvis Z
- **THEN** FootGrounding MUST用相同运算和平滑更新Lyra Current Pelvis Offset Z
- **AND** MUST只允许唯一reach安全阶段在必要时夹紧该值，不得调用旧AllPlantedFeet或Directional目标resolver

### Requirement: Animation Clip Foot Placement曲线必须沿正式表现投影采样

每个Pose Source MUST在同一effective sample time提供唯一Foot Placement Weight与生成Foot Features。Foot Placement Weight MUST映射Lyra Control Rig节点总alpha并只应用一次；项目 MUST不伪造独立`DisableLegIK`或单腿关闭参数。Plant Confidence与sole speed MAY供FootGrounding的唯一contact滞回使用，但 MUST不再次连续缩放整个Goal；Next Landing只可由显式PredictiveFootPlacementModifier消费，Swing feature同时定义stance释放和预测改写资格。

#### Scenario: Foot Placement Weight为一半

- **WHEN** 最终`animation.foot-placement-weight`为0.5
- **THEN** FootGrounding Baseline Pelvis与Foot Goals MUST只应用一次0.5总权重
- **AND** FullBodyIK MUST不再次乘相同作者权重

### Requirement: Lyra Foot Grounding与Full Body IK必须在Pose Graph中显式分段

Pose Graph MUST显式表达`Component Pose -> FootGrounding -> 可选PredictiveFootPlacementModifier -> FullBodyIK -> Solved Component Pose`。`FootGrounding`和Modifier MUST只输出Goal value；唯一FullBodyIK MUST是唯一biped Pose solver。Runtime MUST不恢复Lyra Control Rig中的两个Basic IK节点，不串联LegIK/TwoBoneIK，也不挂FinalIK组件。

#### Scenario: 一个表现帧更新Corin

- **WHEN** FootGrounding与可选Hand Goal Source完成且lineage匹配
- **THEN** FullBodyIK MUST在一个Pending Pose中应用Pelvis、Feet和Hands后执行一次FBBIK
- **AND** final writer之前 MUST不存在Transform写入

### Requirement: Lyra Foot Grounding与Full Body IK必须使用typed目标合同

`FootGrounding` MUST发布同帧`component.full-body-ik-goals`，包含唯一Pelvis Pre-Solve Translation、LeftFoot与RightFoot Goal。每个Foot Goal MUST携带完整Component Transform、position/rotation weight、`FootPlacementEffectorTarget` application、producer/completion/rig lineage及分层Baseline diagnostics。Modifier MUST消费并发布同一类型，不得扇出第二Foot Goal Set。FullBodyIK MUST只消费最终Goal value，不读取Foot Placement Profile、PhysicsScene或动画曲线。

#### Scenario: Pose与Goal lineage不同

- **WHEN** FullBodyIK输入Pose与Foot Goal Set不共享Frame、Completion或Rig revision
- **THEN** Runtime MUST明确失败并阻断FinalPublication
- **AND** MUST不使用上一帧Goal或按节点顺序猜测配对

### Requirement: Full Body IK必须由成熟后端保持biped约束

FullBodyIK MUST通过现有FinalIK FBBIK Pose Buffer backend求解Rig v4 Physical biped。它 MUST先应用Pelvis Pre-Solve Translation和Foot pre-rotation，再把Lyra最终Foot Component Position直接设置为FBBIK绝对effector position，并只执行一次`ReadPose -> Solve -> WritePose`。它 MUST不在FinalIK内部`LimitBend`执行前按旧Foot Transform预计算一次性position offset。左右腿bend constraint MUST来自Rig reference pose。满权重Foot Placement Goal求解后位置残差超过`0.001m`时 MUST返回typed failure并阻断FinalPublication。FullBodyIK MUST不调用FinalIK Grounding、`GrounderFBBIK`、LegIK或TwoBoneIK，也 MUST不重新计算trace、smoothing或pelvis。

#### Scenario: 左脚目标与双手目标同时存在

- **WHEN** 最终Foot Goal Set和不重叠Hand Goal Set合法
- **THEN** 一个FBBIK solve MUST同时处理Pelvis、双腿和双手
- **AND** 未提供的effector MUST在本帧明确归零

### Requirement: Foot Placement配置和Rig必须显式且通过发布验证

`CharacterFootPlacementProfile` MUST只包含`Lyra Current Grounding`、`Stance Stabilization`与`Predictive Extension`三组。Lyra字段 MUST逐项保存来源资产标识、Sphere Trace、foot offset/normal smoothing、pelvis spring与alignment参数；资产gate事实只进入来源对账，不得成为项目Profile字段。Stance字段 MUST只保存contact滞回、anchor blend/release、surface跟随和pelvis reach安全界限；Predictive字段 MUST只保存Future Landing、Envelope与Swing Clearance参数。Profile MUST不保存FinalIK Grounding、Grounder、UE AnimNode FootPlacement、并列Pelvis模式或重复current target字段。

Definition Build MUST验证Profile、Rig v4、Calibration v4、Foot Analysis与Projection identity，并拒绝旧combined节点、旧Goal Application或旧Profile schema。任何生成产品只能由用户显式Character Build发布；Inspector、OnValidate、selection和Preview MUST不自动构建。

#### Scenario: Corin仍保存FinalIK Grounding Quality

- **WHEN** Corin Profile包含旧Grounding Quality、Overstep或Root Cast字段
- **THEN** Build MUST失败并报告旧字段
- **AND** MUST不忽略字段或通过兼容reader继续发布

### Requirement: Lyra Foot Grounding与Full Body IK必须提供分层诊断且保持热路径有界

Diagnostics MUST只读暴露节点执行状态、总alpha、Body Grounded诊断事实、Presentation Delta、PoseRoot竖直delta、每脚动画Ankle Component Y、每脚Sphere Trace输入/命中、minimum ground normal dot、Hit Location、Impact Point、Lyra Target Offset、Sole Clearance Target、合成Offset Target、Unconstrained Offset、Sole Constraint Offset、约束后Current Offset、Current Hit Normal、spring state、contact证据/滞回、surface identity、anchor local/world与blend/release、鞋底支撑面、上一帧鞋底surface identity及Heel/Toe对当前平面距离、连续越界是否成立、最终Ankle与Heel/Toe平面距离、Residual Sole Penetration、Pelvis Lyra target/reach夹紧前后、Baseline Goals、可选Swing rewrite、最终Goals、FBBIK completion/residual与typed failure。Pelvis Reach失败 MUST包含Render Frame、Lyra target/current、全局升降范围、左右Hip/Goal、Goal Weight、腿长、左右可达区间与最终交集。Canvas、Pose Watch、Target Watch和Trace MUST分别显示`Lyra Current Grounding`、`Stance Stabilization`和`Predictive Extension`，不得显示FinalIK Grounding backend badge，也不得把anchor或鞋底间隙结果标为Lyra原生。Diagnostics MUST复用固定workspace，不得重新query、平滑、求解或遍历Transform反推。

#### Scenario: 排查左脚悬空

- **WHEN** 最终Left Foot Pose高于预期
- **THEN** Live Debug MUST在同一Frame显示Left Sphere Trace、Target/Current Offset、Baseline/Final Goal和FBBIK residual
- **AND** 必须能区分问题发生在Lyra目标生成、预测rewrite还是solver

### Requirement: Preview 必须遵守正式世界上下文边界

Preview MUST通过共享AnimationPreviewRuntime执行同一Pose/Value Plan。只有精确Host提供匹配Definition、Rig v4、Calibration、Body ground输入、World-Aware Binding与PhysicsScene时，Preview才可执行Lyra Sphere Trace、可选预测和FBBIK。上下文缺失时 MUST报告typed Unavailable，不创建假地面、默认Profile或旧Grounding adapter。

#### Scenario: 纯动画预览缺少PhysicsScene

- **WHEN** Preview只有动画资源和Pose而没有正式world context
- **THEN** pure-pose阶段 MAY继续显示
- **AND** FootGrounding及其依赖的FullBodyIK输出 MUST明确Unavailable

### Requirement: Stance Anchor必须支持移动Surface

合法stance脚 MAY把当前Lyra SphereCast命中的support point与normal保存为命中Collider Transform的局部anchor，并在后续表现帧从同一surface重建世界目标。最终Foot Goal MUST在Lyra current目标与anchor目标之间连续混合。Surface被销毁、禁用、移出合法layer、不再满足坡度/reach或脚转入Swing时 MUST以明确原因释放。Surface引用和局部anchor MUST只属于Presentation runtime；预测Modifier MUST不创建、维持或改写它们。

#### Scenario: 角色站在移动平台

- **WHEN** stance anchor所在平台Transform在下一表现帧移动
- **THEN** 该脚Baseline世界目标 MUST由原局部anchor随平台更新并与Lyra current目标连续混合
- **AND** Network、Snapshot和WorldState MUST不保存该脚anchor
