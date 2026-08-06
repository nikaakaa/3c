# Change: 用FinalIK Grounding与FBBIK替换分裂Pose IK

## Why

当前Foot Placement已经拥有动画Foot Feature、未来落点、当前/未来支撑、锁脚、移动表面锚点、Ground Envelope和骨盆计划，但正式链仍把“地面查询、脚掌对齐、骨盆补偿”和“骨骼如何满足目标”主要交给项目自有实现：

- `FootPlacement`查询世界、直接平移pelvis subtree并发布`component.biped-leg-targets`。
- `LegIK`调用`CharacterComponentPoseLimbSolver`分别求解左右腿。
- Corin再串联两个`TwoBoneIK`求解双臂。
- Foot Calibration还保存由预览姿势派生的膝盖方向，使鞋底作者工具承担求解器Rig职责。

这条链实际包含三次局部骨骼求解，而且项目自己的地面适配数学与已安装FinalIK Grounding重复。继续修补它们会把成本投入通用Grounding和通用IK，而不是本项目真正需要保留的业务：动画相位驱动的未来落点、支撑生命周期、移动表面锚定和Pose Graph事务。

UE的可借鉴点不是把多个IK节点并行执行，而是把职责和数据流分开：GASP基础Locomotion在AnimGraph末段用一个Leg IK把脚钉到已由Pose Warping调整的IK Foot Bone；UE Foot Placement独立拥有Trace、Plant、Pelvis与Interpolation设置；需要全身联动时，Control Rig FBIK把Root和多个Effectors交给同一个求解器。项目采用同样的职责边界，但不声称FinalIK FBBIK等同UE PBIK。

本地FinalIK源码还提供可直接借鉴的成熟Grounding链：`Grounding.Leg`执行Ray/Capsule查询、速度外推、脚高与坡面旋转；`GrounderFBBIK`提供pelvis-before-effectors应用顺序并触发一次FBBIK。`Grounding.Pelvis`只按腿offset计算lower/lift，缺少UE Foot Placement式逐腿compression/extension可达区间，因此仅作为审计对象，不作为正式骨盆高度权威。之前提案完全排除FinalIK Grounding，等于继续让项目自有实现承担全部空间查询和对齐，这与“成熟方案优先”不一致。

FinalIK Grounding也不是完整的预测式Foot Placement。Stock实现只提供基于脚当前速度的短时外推，不知道动画触地相位、Future Landing、Current/Future Support、Ground Envelope、moving surface anchor、Free/Locked/Sliding、source contribution或逐腿可达区间。因此本change保留这些无法由FinalIK提供的项目业务，把当前地面采样、命中到脚目标、坡面旋转与脚平滑下沉到FinalIK Grounding，并由独立Pelvis Reach Planner只消费最终Foot Goals和Rig腿长。实施审计若证明必须重写Grounding脚部或FBBIK核心方程才能接入Pose Buffer与显式预测输入，必须停止并报告，不得悄悄恢复项目自研通用Grounding、shadow skeleton或旧IK。

240帧Foot IK连续采集进一步证明当前实现存在一条独立于FinalIK求解质量的权重合同错误：左右脚全部成功进入FBBIK且满权重帧残差接近零，但运行时曾把`PlantConfidence`、拼接后的脚速或最终sole世界速度依次作为整个Foot Goal的总闸门。前者让Run混合权重长期偏低，后两者把Body或actor世界平移计入两脚速度，造成持续输入时Goal归零、松开输入后才恢复。surface distance门控还会在修正距离最大时关闭IK。因此本change最终拆开职责：合法Current Grounding Goal只由Placement Weight控制；烘焙Plant Confidence和Sole Local Velocity只维护Plant Contact；Plant Support只服务Pelvis；Contact Weight只拥有anchor、lock与slide。

## What Changes

- 把正式Component Pose DAG收敛为两个目标数据分支和一次骨骼求解：
  - `LocalToComponentPose`提供唯一Component Pose。
  - `PredictiveFootPlacement`读取Component Pose、表现帧和world context，通过FinalIK Grounding backend生成Foot Goals，并通过逐腿Pelvis Reach Planner生成pelvis pre-solve；它不输出Pose，也不是IK solver。
  - `PoseBoneIKGoals`从同一Component Pose读取左右手武器Virtual Bone并生成Hand Goals；它不执行IK。
  - 唯一`FullBodyIK`同时接收原始Component Pose与全部Goal Sets，在一次FinalIK FBBIK中处理Body、双手和双脚。
  - `ComponentToLocalPose`只消费FullBodyIK结果。
- Authoring Graph保存Pose edge和Goal value edge；generated Pose Plan只按依赖拓扑调度目标生产与求解。两个Goal Source MAY按生成计划有序执行，但它们都不修改骨骼，因此不得称为“串行IK”。
- execution domain新增`PureValue`与`WorldAwareValue`：`PoseBoneIKGoals`属于PureValue，`PredictiveFootPlacement`属于WorldAwareValue；不再借用带Pose写入含义的PurePose或WorldAwarePose描述Goal Source。
- 将FinalIK Grounding改造限定为中立I/O边界：
  - 以显式root/foot Component Transform、delta time和固定查询请求替代正式链中的`Transform`与`Time.time`读取。
  - 通过现有精确`PhysicsScene`、LayerMask和self-collider排除端口提供Ray/Sphere/Capsule命中，不让插件直接选择默认PhysicsScene。
  - 复用Grounding现有命中到脚目标、坡面rotation offset与foot interpolation数学；stock pelvis输出被禁用。
  - Transform backend继续服务FinalIK自带素材；项目Runtime只使用Pose Buffer/world-query adapter。
- `PredictiveFootPlacement`是唯一world query和Foot contact owner：
  - FinalIK Grounding负责其现有能力覆盖的当前脚采样、脚掌坡面对齐与脚平滑。
  - 项目扩展负责动画Foot Feature、source contribution、Placement/Plant Support/Contact职责、未来落点与路径采样、Current/Future Support、Ground Envelope、Free/Locked/Sliding、moving surface anchor，以及基于Rig腿长和最终Foot Goal的逐腿pelvis可达区间。
  - Foot Placement Weight是作者总开关；Body Grounded与合法Current Grounding命中生成Placement Weight并唯一控制普通Foot Goal；混合后的Plant Confidence与烘焙Sole Local Velocity只参与接触意图迟滞；Plant Support只参与Pelvis；Contact Weight只参与anchor、lock与slide。系统不得用脚速、surface distance或接触置信度连续衰减普通Foot Goal。
  - 当前脚Grounding与预测扩展共用同一个精确PhysicsScene查询端口和命中合同，但Future Landing与路径采样不得伪装成FinalIK Grounding能力；Pelvis Reach Planner也不得覆盖FinalIK产生的当前脚高与坡面旋转。
  - 同一节点内不得保留一套与FinalIK Grounding并行竞争的“当前脚目标/坡面旋转”实现；Pelvis Reach Planner不是第二Grounding或IK solver，只能发布一个pelvis pre-solve Goal。
- 新增唯一`CharacterFullBodyIkProfile`，只保存FinalIK FBBIK真实支持的iterations、FABRIK pass、spine stiffness、body pull、chain pin/pull/push/push-parent/reach、limb mapping与maintain rotation配置。
- 现有`CharacterFootPlacementProfile`继续作为唯一Foot Placement作者资产，明确区分FinalIK Grounding-backed设置与Project Predictive Extension设置；不得复制Grounder组件字段或保存backend选择/fallback。
- 将Animation Rig升级为v4，显式声明solver root、pelvis、ordered spine、左右arm chain、左右leg chain与可选head/clavicle；全部语义使用BoneId。
- 将Foot Calibration升级为v4，只保存heel、toe、sole frame及geometry validation identity；删除preferred bend、Knee Direction、pole和solver orientation。
- 新增统一FinalIK Pose Buffer backend：
  - 以stable bone index访问Component Pose与父子关系。
  - 每Actor初始化一次FBBIK chain、effector、mapping、Grounding state和固定容量workspace。
  - 每帧只读Pending Component Pose并写另一Pending Component Pose，不读写Animator Physical Transform。
  - 不创建`FullBodyBipedIK`、`GrounderFBBIK`、`LimbIK`组件、target GameObject或shadow skeleton。
- 把`component.biped-leg-targets`替换为固定容量`component.full-body-ik-goals`。Goal显式携带Effector Slot、Component Transform、position/rotation weight、source/completion/rig lineage；FullBodyIK按编译顺序合并，重复Slot或lineage不一致直接失败。
- 从Capability Catalog、Document v3、Mutation、Validator、Compiler、Projection、Native Pose Program、Workspace、Preview、Pose Watch、Live Debug与Trace中删除`TwoBoneIK`、`LegIK`、旧target ABI和旧solver字段，新增`PredictiveFootPlacement`、`PoseBoneIKGoals`、`FullBodyIK`及typed端口。
- 迁移Corin：
  - 两个手臂`TwoBoneIK`迁为一个`PoseBoneIKGoals`。
  - `FootPlacement + LegIK`迁为Goal-only `PredictiveFootPlacement + FullBodyIK`。
  - 配置Rig v4、Foot Placement Profile、FullBodyIK Profile并重建Foot Analysis artifact、Float32/Fixed Program、Presentation Projection和Native Pose Program。
- 删除`CharacterComponentPoseLimbSolver`、`CharacterTwoBoneIkPoseSolver`、旧LegIK诊断、旧TwoBone/LegIK payload及重复的当前脚对齐/坡面旋转算法；删除FinalIK stock pelvis作者字段与正式输出，保留唯一逐腿Pelvis Reach Planner；预测扩展保留Future Landing与Ground Envelope请求种类，但不保留第二套当前脚Grounding结果或运行时择优路径。
- 保持动画帧事务不变：Barrier后任何Grounding query、predictive extension、Goal validation或FullBodyIK失败都阻断后续stage和FinalPublication，并使对应Actor Animation Runtime进入Faulted。

## Impact

- 新增current capability候选：`character-full-body-ik-pose-solver`。
- 修改`character-foot-placement-presentation`、`character-presentation-pose-graph`、`graph-authoring-domain-framework`、`character-animation-pipeline`、`character-animation-layer-runtime`、`character-pipeline-runtime`、`character-pipeline-definition-authoring`、`character-animation-presentation-authoring`与`character-animation-foot-analysis-artifact`。
- 影响FinalIK Grounding与FBBIK的I/O边界、`ThirdPersonClient.Runtime`装配、Pose Graph authoring/compiler/runtime、Rig/Calibration schema、Foot Analysis、Projection、Preview、diagnostics与Corin内容资产。
- 不修改Gameplay KCC、Simulation状态、Network packet、Body root motion、Motion Matching查询、Foot Analysis曲线payload、Camera、Motion Warping或Timeline事件；只收紧`PlantConfidence`的Runtime消费语义。
- 不安装Unity Animation Rigging并行约束链，不从UE复制PBIK/FootPlacement源码，不在Prefab挂FinalIK组件。

## 成熟方案对照与无法承诺的边界

- GASP基础Locomotion使用Leg IK钉住已Warp的IK Foot Bone，不是FBIK全身落脚示例。本change对齐其图内执行和IK末端职责，但选择FinalIK FBBIK满足项目双手、双脚与Body联动需求。
- UE Foot Placement具有Plant、Trace、Pelvis、Interpolation和动画曲线/Root Motion速度输入。项目的Predictive Foot Placement仍需对应业务层；FBBIK不能替代world query和plant决策。
- FinalIK Grounding成熟覆盖当前脚Ray/Capsule查询、简单速度预测、坡面rotation与脚平滑，但不覆盖动画相位Future Landing、Ground Envelope、多命中surface identity、移动表面锚定、项目Foot Feature贡献或逐腿pelvis可达区间。这些是明确的Project Predictive Extension，不得假装来自FinalIK。
- FinalIK公开的Raycast、SphereCast与CapsuleCast delegate只是stock Grounding内部的Physics入口，不是任意预测路径查询器。项目可以让当前Grounding和预测扩展共用同一个PhysicsScene adapter与命中页，但不能把Future Landing/路径采样命名成FinalIK查询能力。
- FinalIK Grounding和FBBIK stock源码都以`Transform`、`Time`和MonoBehaviour回调为入口。无shadow skeleton接入必然需要维护vendor I/O改造面；只能抽象输入输出和生命周期，不能承诺未来插件升级无冲突合并。
- UE Control Rig FBIK是PBIK，具有逐骨刚度、轴limit、Preferred Angle、Excluded Bone等能力。FinalIK FBBIK没有一一对应合同，UI和spec不得伪装具备。
- 显式toe plant point只能作为同一个FinalIK Grounding owner中的次级支撑查询发布，不得参与或覆盖stock Best质量的heel Ray、foot-center Capsule、脚高、rotation与foot interpolation。Future Landing或world fixture若要求复制/重写这些核心数学，实施必须先停止并报告可复用边界；Pelvis Reach Planner必须明确标记为Project Predictive Extension，不得包装成FinalIK能力。

## 与Current Spec及Active Change对比

- current `character-foot-placement-presentation`要求`FootPlacement`先写pelvis Pose、`LegIK`再用自研解析式求解，并把preferred bend存在Calibration；它还规定项目自有foot rotation、Directional Pelvis与pelvis smoothing。本change删除Pose写入、LegIK、preferred bend和重复的当前脚数学，保留Calibration语义鞋底frame转换、未来路径预测与支撑生命周期，并以UE式逐腿可达区间替换旧Directional/stock pelvis，把全部结果放进唯一Goal Source。
- current `character-presentation-pose-graph`把`TwoBoneIK`、`FootPlacement`、`LegIK`、Rig v3与`component.biped-leg-targets`列为正式合同。本change升级为Rig v4、两个Goal Source分支、唯一`FullBodyIK`和`component.full-body-ik-goals`。
- current pipeline specs以固定职责顺序描述FootPlacement与LegIK。本change改为依赖DAG：目标生产可被有序调度，但只有FullBodyIK执行骨骼求解。
- active `add-discrete-stair-presentation`要求heel/toe、Current/Future Support、Ground Envelope和`Ground | FootPlacementSurface`查询。这些属于Project Predictive Extension；实施时必须把其中LegIK旧措辞改为Grounding Goals与FullBodyIK，但不得删除楼梯真实踏面合同。
- active `add-discrete-stair-presentation`仍明确保留旧Directional Pelvis和项目自有当前heel/toe query，与本change的FinalIK Grounding owner冲突。实施必须同步删除旧当前脚口径：当前脚采样归FinalIK Grounding，Future/Path Envelope归Predictive Extension，pelvis归唯一Pelvis Reach Planner；Actor Movement Compensation保留为显式`FollowBody`或`HoldWorldDuringInterpolation`模式，不再隐含在stock damper中。
- active `add-character-presentation-blend-space`和`add-character-motion-matching-pose-source`只把最终Foot Feature贡献交给唯一Foot Placement节点。本change保持该输入与MM History边界，只同步节点职责名称。
- active KCC和楼梯Gameplay change不读取IK Goals或结果，边界不变。
- 本change早先要求Plant Confidence连续门控Foot Goal与Planner贡献，这与240帧运行证据冲突，并把“源动画接触意图”和“当前世界地面对齐强度”错误合成同一个标量。本次修订删除该口径，不保留旧`PlantWeight`字段、诊断列或兼容计算路径。

## References

- Epic Game Animation Sample Project: https://dev.epicgames.com/documentation/en-us/unreal-engine/game-animation-sample-project-in-unreal-engine
- Epic AnimNode Foot Placement: https://dev.epicgames.com/documentation/en-us/unreal-engine/python-api/class/AnimNode_FootPlacement?application_version=5.4
- Epic Control Rig Full-Body IK: https://dev.epicgames.com/documentation/en-us/unreal-engine/control-rig-full-body-ik-in-unreal-engine
- 本地FinalIK `Grounding.Leg.Process`：`Assets/Plugins/RootMotion/FinalIK/Grounder/GroundingLeg.cs`
- 本地FinalIK `Grounding.Pelvis.Process`：`Assets/Plugins/RootMotion/FinalIK/Grounder/GroundingPelvis.cs`
- 本地FinalIK `GrounderFBBIK.OnSolverUpdate`：`Assets/Plugins/RootMotion/FinalIK/Grounder/GrounderFBBIK.cs`
- 本地FinalIK `IKSolverFullBody.OnUpdate`：`Assets/Plugins/RootMotion/FinalIK/IK Solvers/IKSolverFullBody.cs`
