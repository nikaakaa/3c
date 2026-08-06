## Context

当前系统已经完成动画Foot Feature贡献传播、未来落点与world contact lifecycle的基础类型，以及Animancer Evaluate Barrier后的空间化Pose Plan。问题有两层：骨盆、双腿和双臂曾由项目自研解析式算法分三次求解；当前脚地面采样、坡面对齐与脚平滑可复用FinalIK Grounding，但它的stock pelvis只按脚offset取lower/lift，不能表达UE Foot Placement式的逐腿compression/extension可达区间，因此会在同平面或离散台阶上产生悬空、过度下蹲和根运动补偿抖动。

当前普通基线还有第三个已由连续采集复现的问题：单Clip Analyzer烘焙的`PlantConfidence`在动画混合后被直接执行`InverseLerp(0.5, 1)`，同一个值同时承担源动画接触意图、Grounding输入权重、Foot Goal权重和Pelvis支撑权重。240帧采集内FinalIK全部成功，满权重残差接近零，但跑动平均Plant Confidence约为`0.48/0.51`，最终左右Goal平均权重仅约`0.43/0.45`。问题发生在FBBIK前，不是solver没有解到目标。

这次设计把FinalIK能够覆盖的当前脚Grounding交给FinalIK Grounding，把“骨骼如何满足全部目标”交给FinalIK FBBIK。项目只继续拥有FinalIK没有的动画相位Future Landing、Current/Future Support、Ground Envelope、moving surface anchor、Free/Locked/Sliding、source contribution，以及逐腿可达区间驱动的Pelvis Reach Planner。它们在同一个`PredictiveFootPlacement`目标生成节点中组合，不形成第二查询权威或第二骨骼求解器。

## Goals

- 让预测式Foot Placement成为FinalIK Grounding-backed纯目标生成节点，不直接写Pose或执行IK。
- 复用FinalIK Grounding的当前脚查询、命中到脚目标、坡面旋转和脚平滑数学。
- 以逐腿compression/extension区间生成唯一pelvis pre-solve translation，避免stock lower/lift导致的同平面下蹲与台阶悬空。
- 把动画相位预测明确限制为FinalIK缺失的Project Predictive Extension。
- 分离源动画接触意图、当前世界地面对齐强度与脚掌约束强度，禁止一个烘焙标量同时承担三种职责。
- 让双脚、双手与Body目标只经过一次Full Body IK求解。
- 让FinalIK在Component Pose Buffer上工作，不复制第二套Transform骨架。
- 保持唯一编译Pose Plan、一次Animancer Evaluate、一次Physical Transform final write。
- 删除项目自研TwoBone/LegIK数值求解和旧schema。
- 让人工UI、Document、Validator、Compiler、Preview、Runtime和Diagnostics使用同一Capability与目标合同。
- 明确FinalIK与UE PBIK的差距，不用项目私有参数伪装缺失能力。

## Non-Goals

- 不让FinalIK Grounder组件与PredictiveFootPlacement形成第二Foot Placement。
- 不把FullBodyBipedIK、LimbIK或GrounderFBBIK组件挂到角色Prefab。
- 不创建shadow skeleton、target GameObject或LateUpdate求解链。
- 不从UE复制PBIK、LegIK或FootPlacement源码。
- 不在本change补齐UE PBIK逐骨limits、stiffness、preferred angles或excluded bones。
- 不修改预测器的Gameplay隔离、Motion Matching选择、KCC或Network边界。
- 不把FinalIK的简单velocity prediction伪装成动画相位Future Landing。
- 不保留与FinalIK Grounding竞争的当前脚目标或坡面旋转算法；Pelvis Reach Planner只消费其正式脚目标与Rig腿长，不重新query或重新计算脚高。

## 当前实施阶段

当前普通Foot Placement基线已经接入Project Predictive Extension的接触约束层：FinalIK Grounding唯一根据当前Component Pose执行地面查询、脚高、坡面旋转与脚平滑；同一Goal Source消费最终动画Foot Feature、source contribution、Body可见速度和Current Support，维护Free、Locked与Sliding，并把锁点保存为命中Surface的局部锚点。Pelvis Reach Planner再从左右Hip、动画Ankle、最终Foot Goal、Goal权重、Rig腿长与min/max extension ratio计算每腿允许的骨盆高度区间，合并后只发布一个pelvis pre-solve translation。所有Goals仍只交给一次FBBIK。Future Landing、Path Sample与Ground Envelope尚未改变正式Goal输出，后续继续在同一个Goal Source内接入，不增加第二solver或backend选择。

普通约束层还负责四个有限安全边界：锁点与当前Support法线超过Replant角度时释放；锁点相对当前Grounding姿势产生过度Ankle Twist时释放；左右Foot Goal小于最小脚距时只移动Free脚，双脚均受约束则释放次要支撑脚；单腿目标超出允许pelvis区间或双腿区间无交集时释放不可满足的次要Foot Goal。它们只选择或约束Goal，不重新查询地面、不重算坡面rotation，也不执行第二次IK。

Corin普通基线必须显式关闭FinalIK `Overstep Falls Down`。该stock策略在脚查询无命中时会用`Max Step`向下构造overstep位移，而项目合同禁止把无合法Surface Identity的fallback脚点发布为Foot Goal。Corin `Foot Radius`还必须小于Calibration左右鞋底中较短一侧的半长，避免Best质量的Capsule在鞋底已经越过台阶边缘后仍取得宽于真实鞋底的支撑。FinalIK stock pelvis lower/lift/damper全部退出正式配置与输出；Corin使用`AllPlantedFeet`、`FollowBody`、最大升降范围、插值速度与dead zone配置唯一Pelvis Reach Planner。有效Plant Support或Contact支撑脚目标与动画Ankle没有共同竖直变化时骨盆保持动画高度；目标共同抬高或降低时骨盆按支撑权重跟随，避免把相同平台高度差全部转成膝盖压缩；未进入Plant Contact且无Contact约束的摆动脚不参与高度规划。

有限Heel Lift与Toe Pivot已经沿同一成熟边界闭合。FinalIK Grounding的`Best`质量仍以stock heel Ray与foot-center Capsule唯一计算脚高与坡面rotation；同一个`Grounding.Leg`可额外发布一个typed Toe Ray命中，只作为secondary plant point，不参加或覆盖上述stock结果。Project Predictive Extension据此提供`Unlocked`、`PivotAroundToe`、`PivotAroundAnkle`与`LockRotation`四种plant policy，并把选中的toe plant point、完整ankle目标与`HeelLiftRatio`写入同一个Foot Goal。唯一FinalIK FBBIK在`ReadPose`前按Goal权重绕toe point应用ankle rotation和position offset，再执行原有一次solve。这里没有第二当前Grounding owner、第二LegIK、第二骨架或重复坡面对齐数学。

Corin普通基线不接入当前`VB_Weapon_LeftHand/RightHand` Goal。现有Virtual Bone在Component Pose中等于对应当前手骨姿势，若把它以1.0权重作为固定Component目标，会在pelvis pre-solve translation之后把双手拉回移动前的位置并迫使FBBIK扭曲脊柱和双腿。双手必须等到存在不等于当前手骨Pose的真实武器目标语义后再接入同一个FullBodyIK。

## Evidence Audit

### UE/GASP

官方GASP文档把Motion Matching作为基础Pose选择，并在AnimGraph中组合Root Offset、Orientation Warping、Leg IK等节点。它证明的是“表现功能进入图、按Pose流组合”，不是“所有locomotion都应使用FBIK”。

UE的Component Space Conversion文档明确说明Skeletal Control工作于Component Space，并建议把这些节点集中在一次Local-to-Component与Component-to-Local之间。本项目现有空间合同已经与此一致。

UE Control Rig FBIK使用Root、多Effectors和Bone Settings完成多目标全身求解。它的PBIK后端包含逐骨position/rotation stiffness、轴limit、Preferred Angle、Excluded Bone等FinalIK FBBIK没有的一等合同。因此本设计只对齐目标/求解分层、图内执行和多effector模型，不宣称算法或参数等价。

### FinalIK

本地源码显示：

- `Grounding.Leg.Process`按Quality执行单Ray、heel/toe/side Ray或heel Ray加CapsuleCast，并以`foot velocity * prediction`生成短时查询偏移。
- `Grounding.Leg`把命中点/平面转换为脚高和rotation offset，并使用`footSpeed`与`footRotationSpeed`平滑。
- `Grounding.Pelvis.Process`从左右腿offset生成lower/lift pelvis结果，并使用`pelvisSpeed`与`pelvisDamper`平滑。
- 该stock pelvis只聚合foot offset，不表达每腿minimum/maximum extension interval，也会把root delta damper与角色移动混入骨盆高度；因此只作为审计证据，不进入正式输出。
- `Grounding`允许替换Raycast、CapsuleCast与SphereCast委托，但stock leg仍直接读取`Transform`和`Time.time`。
- `IKSolverFullBodyBiped.SetToReferences`固定建立Root、左右臂、左右腿五条chain，九个effectors和四个limb mappings。
- `IKSolverFullBody.OnUpdate`固定执行`ReadPose -> Solve -> WritePose`。
- `FBIKChain`负责pin/pull/push/reach、FABRIK与trigonometric pass。
- `IKConstraintBend`从输入姿势、effector rotation和bend constraint计算四肢弯曲方向。
- `IKMappingSpine`、`IKMappingLimb`和`BoneMap`把solver node结果映回骨架。
- `GrounderFBBIK`先移动pelvis，再向左右脚effector写offset，之后由FBBIK统一求解。

这些Grounding和FBBIK数学及应用顺序成熟可复用；但stock入口直接保存或访问`Transform`、`Time`、`Physics`和MonoBehaviour回调，没有项目的Component Pose、显式delta time、精确PhysicsScene或fixed workspace入口。

FinalIK Grounding的能力边界也必须明确：它没有动画Foot Feature、触地相位、Future Landing delay/local offset、Current/Future Support区分、Ground Envelope、surface identity、moving surface anchor或Free/Locked/Sliding。它不能原样替换完整预测业务，只能成为唯一PredictiveFootPlacement中的成熟Grounding kernel。

## Selected Architecture

```text
Local Pose
  -> LocalToComponentPose
       -> Component Pose ---------------------------------------------+
            |                                                        |
            |-- read only --> PredictiveFootPlacement                |
            |                   -> FinalIK Grounding kernel           |
            |                   -> Project Predictive Extension       |
            |                   -> Pelvis Reach Planner               |
            |                   -> Body/Feet Goals --------+          |
            |                                             |          |
            |-- read only --> PoseBoneIKGoals             |          |
            |                   -> Hand Goals -------------+          |
            |                                             |          |
            +---------------------------------------------|----------+
                                                          v
                                               FullBodyIK (only solver)
                                                          |
                                               Solved Component Pose
  -> ComponentToLocalPose
  -> OutputPose
```

这是一张Pose与Goal value共同组成的DAG，不是三个IK节点串行。`PredictiveFootPlacement`和`PoseBoneIKGoals`只读取同一个Component Pose并生产目标；generated stage table MAY按拓扑有序调用它们，但两者都不改骨骼。`FullBodyIK`是唯一写骨骼Pose的IK solver。

## Decision: PredictiveFootPlacement是FinalIK Grounding-backed Goal Source

当前FootPlacement先调用项目Support Query和Planner，再以`TranslateSubtree`写pelvis并让LegIK解腿。改造后`PredictiveFootPlacement`仍是唯一world-aware owner，但内部职责固定为：

1. 从Component Pose和Calibration构造FinalIK Grounding实际支持的当前脚采样输入。
2. 通过FinalIK Grounding处理stock Ray/Sphere/Capsule组合、velocity prediction、命中到脚高、坡面rotation与脚平滑；stock pelvis权重固定为零，不发布stock pelvis结果。
3. 从Foot Feature构造动画相位Future Landing与路径采样请求，通过同一个PhysicsScene查询端口执行Project Predictive Extension，并补充Current/Future Support区分、Ground Envelope、surface identity/anchor、Free/Locked/Sliding和source contribution。
4. Pelvis Reach Planner按每腿Hip、动画Ankle、最终Foot Goal、Goal权重、Rig腿长与min/max extension ratio求允许高度区间，同时从贡献脚相对动画Ankle的竖直Goal变化求支撑权重首选高度；区间相交时把首选高度夹入共同区间，区间冲突时保留主要支撑脚并释放不可满足的次要Goal。
5. 把最终结果发布为Goal value，不写Pose。

输出包括：

- `PelvisPreSolveTranslation`；
- 左右脚目标Component Position/Rotation；
- position/rotation weight；
- constraint、confidence、support与completion诊断元数据。

FullBodyIK按FinalIK GrounderFBBIK已有顺序在自己的Pending output中应用pelvis pre-solve translation，然后设置effectors并求解。PredictiveFootPlacement不再拥有任何Physical Bone写入，也不调用任何IK solver。

业务收益是PredictiveFootPlacement可以分别解释“FinalIK Grounding给出了什么当前地面结果”和“项目预测扩展为什么选择这个未来落点”，FullBodyIK只解释“骨架如何满足全部目标”。

### FinalIK Grounding复用边界

必须复用而不得在项目代码中复制的数学：

- stock Quality对应的cast组合与velocity prediction基线；
- 命中点/平面到脚高与rotation offset的转换；
- Calibration semantic sole frame与ankle frame之间的输入输出变换不改变上述rotation数学；
- 最大脚旋转限制与rotation interpolation；
- foot vertical interpolation；
- GrounderFBBIK的pelvis-before-effectors应用顺序。

只允许项目扩展的内容：

- 从动画Foot Feature和source contribution产生landing time/local offset；
- 为FinalIK当前脚Grounding提供显式pose、time与world-query输入；
- 为Future Landing和路径采样建立项目预测请求；
- 让两类请求共用精确PhysicsScene、LayerMask、自碰撞排除、稳定surface identity与固定容量命中页；
- Current/Future Support、Ground Envelope、moving surface anchor与Free/Locked/Sliding生命周期；
- 基于Rig腿长和Foot Goal权重的逐腿compression/extension区间、pelvis height mode、最大升降范围、dead zone、显式actor movement compensation与pelvis interpolation；
- 同一Grounding owner中的typed secondary Toe plant query，以及不改变stock脚高、rotation和pelvis结果的plant point发布；
- `Unlocked`、`PivotAroundToe`、`PivotAroundAnkle`与`LockRotation`策略、`AdjustHeelBeforePlanting`和`HeelLiftRatio`；
- 将Grounding输出与预测状态降低为Body/Foot Goals及typed diagnostics。

如果实施发现“复用”实际要求把FinalIK Grounding公式复制到新项目类、用另一套公式重算当前脚目标/rotation，或两套查询结果运行时择优，必须停止。Pelvis Reach Planner不得改写Grounding命中、脚高或rotation，只能消费最终Foot Goal与Rig几何合同。允许修改vendor代码的范围只包括显式pose/time/query输入、fixed workspace、命中数据identity和输出结构，不得悄悄改变核心数学语义。

## Decision: 使用可合并的typed Goal Set

`component.full-body-ik-goals`是瞬时、同帧、Component空间value，不是Pose。一个set包含固定容量goal slice和统一lineage：

- Frame Sequence；
- Source Node/Call Site；
- Completion Identity；
- Rig Id/Revision；
- 每项Effector Slot；
- target Component Position/Rotation；
- position/rotation weight；
- Goal Application：普通Pose目标使用绝对effector target，Grounding脚目标使用`GroundingEffectorTarget`成熟应用语义，pelvis使用`PelvisPreSolveTranslation`；
- source kind与只读diagnostic metadata。

FullBodyIK提供稳定动态输入`goals:<local-id>`。Compiler把全部输入降低为有序value index列表，并在Build时拒绝重复Effector Slot和超出容量。Runtime只验证同帧lineage并按已编译顺序拷入预分配effector page，不分配集合、不按字符串查找、不做最后写入获胜。

`GroundingEffectorTarget`仍保存FinalIK Grounding算出的绝对Component目标，但FullBodyIK MUST按stock `GrounderFBBIK.SetLegIK`语义应用：pelvis pre-solve平移后，用`target - 当前foot bone position`写`IKEffector.positionOffset`，并在FBBIK `ReadPose`前把目标rotation差值按权重预乘到foot bone。Grounding脚 MUST不把`IKEffector.positionWeight`或`rotationWeight`设为Foot Placement总权重；否则会改变`IKConstraintBend.LimitBend`和effector rotation对bend plane的语义。`PoseBoneIKGoals`等通用目标继续使用绝对effector position/rotation weight。

第一份正式Effector Slot集合与FinalIK FBBIK保持一致：Body、LeftShoulder、RightShoulder、LeftThigh、RightThigh、LeftHand、RightHand、LeftFoot、RightFoot。Corin首版只使用Body、双手和双脚。

## Decision: 手部目标也进入同一个FullBodyIK

Corin现有两个TwoBoneIK权重为1，目标分别是`VB_Weapon_LeftHand`和`VB_Weapon_RightHand`，不是可直接删除的空节点。

新增`PoseBoneIKGoals`节点：

- 读取同一Component Pose中的Physical或Virtual Pose Bone；
- 按node-local binding生成一个或多个typed goals；
- 保留现有effector offset、rotation policy与weight语义；
- 不写肩、肘、腕，不执行IK。

Corin把两只手合成一个Goal Source，并与PredictiveFootPlacement的Body/Feet goals一起送入唯一FullBodyIK。这样不会为了脚IK牺牲武器持握，也不会保留手臂自研TwoBone fallback。

## Decision: Rig v4 owns biped semantics

Rig v3已经包含完整Physical Bone catalog，但只把pelvis与双腿提升为语义。FinalIK FBBIK需要完整biped references，因此Rig v4增加：

- Solver Root Bone；
- Pelvis Bone；
- ordered Spine Bones；
- 左右Arm：可选Clavicle、Upper Arm、Forearm、Hand；
- 左右Leg：Hip、Knee、Ankle、Toe；
- 可选Head。

Validator要求：

- 所有语义BoneId存在于同一Physical catalog；
- solver root只能是pelvis或spine成员；
- spine、arm和leg父子关系有效且无重复语义；
- 四肢参考姿势能产生有限非退化bend plane；
- 参考segment长度有限且为正；
- Virtual Bone依赖仍只读Physical Pose。

FinalIK的`BipedReferences.AutoDetectReferences`、Humanoid Avatar和骨骼命名搜索不进入正式链。Editor MAY提供一次显式迁移命令，把当前已知Corin BoneId写入v4，但Build只认可资产中的明确结果。

## Decision: Calibration v4只拥有鞋底

Foot Calibration v4保留：

- 左右heel contact offset；
- 左右toe contact offset；
- 自动sole frame；
- Sampling Rig、Preview Clip、geometry validation identity。

删除：

- preferred bend direction；
- Knee Bend作者/诊断字段；
- bend plane geometry error；
- Runtime target中的preferred bend plane。

四肢bend constraint由Rig v4参考Component Pose初始化，并在每帧使用输入动画pose和effector rotation更新。这更接近FinalIK真实工作方式，也避免“调鞋底时还要理解膝盖箭头”。若Rig参考姿势退化，Rig v4 Apply/Build直接失败，不能由世界前方、角色前方或旧Calibration补值。

## Decision: FinalIK Grounding与FBBIK使用中立Backend

### Allowed Modification Surface

Grounding允许修改或扩展的内容仅限：

- root、heel、toe、ankle与foot pose改为显式value/handle输入；
- `Time.time`改为调用方提供的frame delta和history；
- 每脚只接收显式当前Component Pose并生成未按Plant Confidence缩放的Grounding结果；最终Foot Goal权重在Project Predictive Extension中根据当前世界运动学与合法Current Support独立计算；
- Physics查询改为显式world-query port和fixed hit workspace；
- `RaycastHit.collider`补充稳定surface identity与self-collider裁决结果；
- FinalIK Grounding只接受其stock语义覆盖的当前脚采样输入；
- Future Landing和路径采样通过同一world-query port执行，但归属Project Predictive Extension而非Grounding kernel；
- Grounding state的初始化、reset、workspace和只读diagnostic output；
- vendor Transform调用链继续由Transform backend提供同等输入。

FBBIK允许修改或扩展的内容仅限：

- bone handle从`Transform`identity抽象为backend handle；
- position/rotation/local/parent读写；
- Biped reference建立；
- chain/effector/mapping初始化；
- mapping读Pose与写Pose；
- 预分配workspace和backend lifecycle。

Grounding不得重新实现或修改语义的内容：

- stock Ray/heel-toe-side/Capsule query组合；
- hit point/plane到height offset的数学；
- slope rotation offset、maximum rotation和插值数学；
- foot vertical interpolation；
- stock pelvis lower/lift、interpolation和damper数学；正式adapter必须把其权重固定为零，不能把stock pelvis结果与Pelvis Reach Planner择优混合。

FBBIK不得重新实现或修改语义的内容：

- FBIKChain Push/Reach/Stage1/Stage2；
- FABRIK iteration；
- trigonometric pass；
- child constraints；
- effector solve；
- bend constraint数学；
- spine/limb mapping数学。

如果为了显式预测请求或Pose Buffer接入必须复制这些Grounding/FBBIK数学到项目新类、改变核心方程、运行两套查询择优或保留Transform shadow，实施必须停止并报告。

### Dependency Direction

FinalIK core不能引用`ThirdPersonClient.Runtime`。中立bone backend、ground query backend、frame time input和indexed pose handle放在RootMotion assembly可见的扩展边界；项目adapter只把`CharacterPoseBuffer`、Rig v4、精确PhysicsScene、self-collider filter与FinalIK request连接起来。

正式角色Runtime只引用Grounding adapter与Pose Buffer adapter，不引用`FullBodyBipedIK`、`GrounderFBBIK`或IK MonoBehaviour。vendor Transform backend可继续服务插件自带素材，但不进入Character Capability、Prefab、Projection或runtime switch，不是项目fallback。

### Actor Workspace

每Actor在Runtime preparation时按Rig v4与FullBodyIK Profile创建一次：

- indexed biped binding；
- FinalIK Grounding root/legs state；
- Project Predictive Extension的Pelvis Reach Planner state；
- fixed FinalIK current-foot query state；
- fixed predictive future/path request与共享hit pages；
- Project Predictive Extension contact、anchor与envelope state；
- FBBIK chains/effectors/mappings；
- input component pose view；
- output component pose view；
- fixed goal merge page；
- solver node/mapping scratch；
- diagnostic snapshot page。

正常PresentationFrame不创建GameObject、Transform、array、list、delegate、Grounding或solver对象。Reset清空Grounding、Project Predictive Extension和solver帧状态，不重建骨架。

## Decision: Foot Placement Profile区分成熟Grounding与预测扩展

现有`CharacterFootPlacementProfile`继续是唯一Foot Placement配置，不新建Grounder组件配置资产。作者表面分为两组：

- `FinalIK Grounding`：Quality、Ground Layer、Max Step、Foot Radius、velocity prediction基线、Foot Height/Rotation Speed、Foot Rotation Weight/Maximum Angle、Root Cast Radius和Overstep policy；Corin正式Profile MUST显式选择`Best`，不得由Runtime按性能或命中结果降级Quality。stock Pelvis Speed/Damper/Lower/Lift不再属于正式作者数据。
- `Predictive Extension`：动画相位look-ahead、future distance、path samples、surface continuity、contact/lock/slide、moving anchor、leg reach、source contribution、plant lock type、plant前heel调整、heel lift ratio，以及Pelvis Height Mode、Actor Movement Compensation Mode、最大升降范围、插值速度、dead zone与最大水平脚调整。

Project Predictive Extension不得重新声明Foot Rotation Speed、坡面rotation算法或Foot Height interpolation。Pelvis Reach Planner配置描述逐腿可达性与整体验高，不复刻FinalIK stock lower/lift公式。旧stock Pelvis Speed/Damper/Lower/Lift字段直接删除，不做双写。两组配置共同形成一个Profile identity并进入Projection依赖，Profile不保存backend选择或fallback。

普通基线阶段，Corin显式使用`Best`、`velocity prediction = 0`、`Unlocked`、`AdjustHeelBeforePlanting = false`、`AllPlantedFeet`与`FollowBody`。唯一Foot Placement Weight是作者控制的总开关。每脚运行时信号拆成五层：

- `PlacementWeight`：Body Grounded、Current Grounding命中合法且作者Foot Placement Weight有效时等于作者权重，否则为0。它唯一控制未约束FinalIK Grounding Foot Goal的Position/Rotation Weight，不读取脚速、Plant Confidence或surface distance。
- `PlantConfidence`：单Clip烘焙并随最终Pose contribution混合的源动画接触意图。它只通过enter/exit迟滞决定Plant Contact，不连续缩放Foot Goal或Pelvis贡献。
- `AnimationFootSpeed`：最终Pose contribution中已经按source权重与visual time scale混合的烘焙`SoleLocalVelocity.magnitude`。它不拼接Body世界平移、Body可见速度或yaw点速度，只用于Plant Contact进入、退出和Contact约束渐退。
- `PlantSupportWeight`：Plant Contact成立时等于`PlacementWeight`，否则为0。它只表达Pelvis Reach Planner的普通支撑腿选择，不控制普通Foot Goal。
- `ContactWeight`：只有Plant Contact成立、Plant Policy允许约束且surface anchor有效时才存在，按`PlantSpeedThreshold -> UnalignmentSpeedThreshold`连续渐退，只控制anchor、lock与slide。`Unlocked`下固定为0。

正式Profile只保留`PlantSpeedThreshold`与`UnalignmentSpeedThreshold`两个严格有序阈值。Corin与TrainingEnemy使用`0.6m/s`和`2.0m/s`，对应UE 5.7默认`60cm/s`与`200cm/s`的职责：低速允许进入Plant，达到Unalignment阈值退出，区间内只让锁脚约束连续渐退。旧`Alignment Full/Zero` planar/vertical阈值、descending tolerance、Plant/Release速度距离字段和兼容别名全部删除。

FinalIK Grounding必须先基于当前脚Pose产生完整命中、脚高、rotation与插值结果，`GroundingFootInput`不携带Plant或速度权重。`Grounding.Leg`的`rootYOffset`会从脚到命中面的高度差中扣除动画脚到Root参考平面的高度，所以`IKPosition`表达的是“动画Ankle加地形相对Root的高度差”，不是把摆动脚绝对压到地面。Project Predictive Extension因此可让合法Current Grounding Goal始终使用`PlacementWeight`；脚速只决定支撑/约束状态，不能关闭普通跑动Foot IK。

Pelvis Reach Planner继续从最终Foot Goal计算逐腿区间，支撑权重为`max(PlantSupportWeight, ContactWeight)`。这样普通跑动支撑脚不因烘焙置信度连续降权，受约束脚在释放前仍能维持支撑，摆动脚即使保留地形相对动画高度的Foot Goal也不会参与`AllPlantedFeet`骨盆规划。Foot Goal没有相对动画Ankle竖直变化时骨盆不偏移，双脚目标共同抬高或降低时骨盆按支撑权重跟随该变化。高低踏面区间有交集时先求支撑权重首选高度再夹入共同可达区间，无交集时保留主要支撑脚并释放次要Goal。

这与UE Foot Placement的职责边界对齐：动画/Root Motion空间脚速决定Plant意图，Alignment Alpha参与plant plane过渡与有限roll/hyperextension行为，`DisableLeg`才是整腿回到FK的独立权重。当前后端仍是FinalIK Grounding加唯一FBBIK，不复制UE实现，也不新增第二solver或backend路径。

## Decision: FullBodyIK Profile只暴露真实FinalIK能力

Profile保存：

- Iterations；
- FABRIK Pass；
- Spine Stiffness；
- Pull Body Vertical/Horizontal；
- 每chain Pin/Pull/Push/Push Parent/Reach及smoothing；
- 每limb mapping Weight/Maintain Rotation；
- bend constraint Weight和clamp；
- 全局node weight。

Profile不保存：

- UE PBIK Position/Rotation Stiffness；
- 任意逐骨XYZ Rotation Limit；
- UE Preferred Angle；
- Excluded Bones；
- Stretch；
- Root Behavior枚举。

UI以“FinalIK FBBIK Profile”命名，并在References显示backend identity。不能用UE字段名包装FinalIK近似参数。

## Execution Domains and Transaction

- `PredictiveFootPlacement`：`WorldAwareValue`，执行FinalIK Grounding-backed query与Project Predictive Extension，只发布goal value completion。
- `PoseBoneIKGoals`：`PureValue`，只读取Component Pose并发布goal value completion。
- `FullBodyIK`：`PurePose`，消费Component Pose和全部goal values，发布Solved Component Pose。

Compiler根据Pose和goal edges生成拓扑顺序，不保存作者stage index。`PredictiveFootPlacement`与`PoseBoneIKGoals`是从同一Component Pose扇出的两个只读producer；它们在stage table中的先后只表示调度顺序，不表示前一个IK结果输入后一个IK。只有`FullBodyIK`是solver。Grounding query、预测扩展、goal lineage或FBBIK失败都会阻断FullBodyIK后续stage与FinalPublication。Animancer Evaluate Barrier之后失败时Actor Animation Runtime进入Faulted，不恢复状态或Physical Bone快照。

FullBodyIK只写Pending Dense Pose page。唯一Physical Transform writer在全部stage成功并Seal后发布最终Pose。

## Preview, Live Debug and Authoring

### Canvas

- `Predictive Foot Placement`显示Component Pose输入、Weight输入和Full Body IK Goals输出，不显示Pose输出；标题下方显示`Goal Source · FinalIK Grounding`，不得显示IK solver badge。
- `Pose Bone IK Goals`显示Component Pose输入与Goals输出，Details以可重排binding列表配置Effector Slot、目标Pose Bone、offset和weights。
- `Full Body IK`显示Component Pose输入、动态`Goals`输入和Solved Component Pose输出。
- `Two Bone IK`与`Leg IK`从创建菜单、clipboard、Document schema和详情中删除。
- Canvas把Component Pose扇出edge与Goal value edge完整画出，不把两个Goal Source自动排成Pose backbone，也不隐藏Goal merge。

### Profile and Rig

- Presentation Profile新增唯一FullBodyIK Profile引用。
- Rig v4编辑器按Root/Spine/Arms/Legs/Head分组，只允许从Physical Bone catalog选择。
- Foot Calibration Scene只显示和编辑Heel/Toe/Sole，不再显示Knee Direction。
- 显式`Validate Rig for FinalIK FBBIK`命令输出缺失语义、父子关系、segment length与reference bend plane，不在Inspector repaint运行。

### Diagnostics

IK现象、运行证据、UE源码对照、已踩坑与固定排查顺序统一维护在`ik-diagnostics.md`；后续修正不得只改代码或阈值而不更新该记录。

- Grounding Watch：FinalIK Grounding backend identity、current query requests/hits、stock velocity prediction与foot height/rotation。
- Predictive Extension Watch：动画Plant Confidence、Plant Contact迟滞、Animation Foot Speed、surface distance、Placement/Plant Support/Contact weights、current/future support、Ground Envelope、surface anchor、lock状态、左右腿pelvis允许区间、目标/平滑pelvis offset、冲突释放与Body/Feet goals。
- Goal Source Watch：Virtual target和生成的hand goals。
- FullBodyIK Watch：输入/输出Pose、每effector权重/残差、chain reach、iterations、bend constraint和typed failure。
- Diagnostics只复制已完成固定workspace，不第二次调用FinalIK、不读取Transform反推。

## Migration and Cleanup

迁移只允许一次正式schema跳变：

1. 审计并安装FinalIK Grounding中立time/pose/query backend，通过Grounding数学复用门禁。
2. 审计并安装FinalIK FBBIK Pose Buffer backend，通过solver数学复用门禁。
3. 安装Rig v4、Calibration v4、Foot Placement Profile分组、FullBodyIK Profile与新goal ABI。
4. 同步Capability、Document、Compiler、Projection和Runtime。
5. 迁移Corin full-biped mapping、Grounding配置、手部goal bindings和Pose Graph。
6. 重建Foot Analysis artifact与全部generated products。
7. 删除旧TwoBone/LegIK及重复Grounding数据、代码、诊断、workspace和authoring能力。
8. 更新current truth与仍引用旧节点的active change文档。

不提供Rig v3 reader、Calibration v3 reader、旧target codec、节点自动替换fallback或运行时双solver开关。旧资产在迁移完成前应明确Build失败。

## Tradeoffs

### 选择FinalIK FBBIK Pose Buffer改造

收益：复用成熟多链约束、effector、FABRIK/trigonometric和mapping数学；统一脚、手和Body；保留图内Pose事务。

代价：必须维护第三方源码I/O改造面；FinalIK升级会产生合并成本；功能不等价UE PBIK。

### 选择FinalIK Grounding作为当前脚Grounding基线

收益：复用插件已有Ray/Capsule组合、velocity prediction、坡面rotation和foot interpolation；项目不再维护第二套通用当前脚对齐数学。骨盆则使用FinalIK缺失的逐腿可达区间，行为更接近UE Foot Placement。

代价：stock Grounding没有动画相位Future Landing、Ground Envelope、surface identity、移动平台锚定或逐腿compression/extension pelvis区间，必须保留范围明确的Project Predictive Extension与Pelvis Reach Planner；同时需要把Transform、Time与Physics入口抽象为显式value和world-query backend。

### 不选择Stock GrounderFBBIK组件直挂

收益是接入最快，查询与FBBIK调用顺序由插件组件负责。

代价是它依赖Physical Transform、MonoBehaviour回调和目标组件，无法进入同一Pose Buffer事务，还会与Pose Graph形成第二生命周期，因此不作为正式方案或fallback。

### 不选择Stock FinalIK + Shadow Skeleton

收益是接入快且不改vendor数学。

代价是每Actor多一套Transform层级、Pose->Transform和Transform->Pose两次复制、额外生命周期与潜在LateUpdate顺序；这会破坏唯一Pose Buffer/唯一writer并使Preview与Runtime难以统一，因此不作为正式方案或fallback。

### 不选择保留TwoBoneIK并只替换双腿

收益是迁移小。

代价是手和脚继续走不同solver，骨盆/脊柱无法统一响应，项目仍有两套IK参数和诊断；与用户要求的单路径不一致。

### 不选择Unity Animation Rigging组合约束

它提供成熟Animation Job约束和TwoBoneIK，但没有直接等价于UE PBIK/FinalIK FBBIK的单一多目标全身求解器。接入它会保留多constraint编排或需要另一全身后端，形成第二路径。

### 不选择自研PBIK

它最接近UE功能表，但会把范围扩大到迭代约束、limits、stiffness、preferred angles和稳定性工具，违背“优先成熟方案、项目只自有预测业务”的目标。

### 不选择把Plant Confidence阈值从0.5改成0

收益是只改一行映射，跑动Foot Goal数值会立刻升高。

代价是摆动脚的任意非零烘焙置信度都会直接进入IK，动画混合仍让一个标量同时承担接触判断和地面对齐强度，容易出现拖脚、提前吸附与骨盆误参与。它没有修复职责错误，因此不作为正式方案或调试开关。

## Hard Stop Gates

实施必须按顺序通过以下门禁：

1. 生成FinalIK Grounding与FBBIK参与文件清单、内容hash及Transform/Time/Physics依赖矩阵。
2. 证明Grounding seam可以接受显式current-foot pose/time、精确PhysicsScene和fixed hit page，且不复制命中到目标、rotation、foot/pelvis interpolation数学。
3. 证明Future Landing与路径采样只复用同一world-query port和命中合同，不冒充FinalIK Grounding能力，也不生成第二套当前脚Grounding结果。
4. 明确列出FinalIK Grounding无法覆盖、必须由Project Predictive Extension拥有的字段和状态；不存在运行时两套结果择优。
5. 证明FBBIK backend seam可以覆盖全部read/write/init/mapping，而不改核心求解方程。
6. 证明正式Runtime不需要shadow skeleton、target GameObject、FinalIK组件或Physical Transform中间写入。
7. 证明每Actorworkspace可在preparation时完整预分配，正常帧不创建managed集合、Grounding或solver对象。
8. 证明Rig v4能为Corin提供完整spine/arms/legs/root语义和非退化reference bend plane。

任一门禁失败时，停止实施并向用户报告失败文件、依赖和可选成熟替代方向；不得继续创建临时adapter、保留自研solver fallback或悄悄降低为shadow rig。

## Open Questions Deferred by Evidence

- FinalIK FBBIK与UE PBIK逐骨limits/stiffness的功能差距不在本change解决。
- FinalIK Grounding的简单velocity prediction不等同动画相位Future Landing；Project Predictive Extension的必要范围必须以源码缺口逐项证明。
- 第三方插件源码公开分发许可不由本技术提案判断。
- 如果未来需要非biped、额外触手链或任意effectors拓扑，FinalIK FBBIK固定五chain模型可能不够；届时必须另立成熟solver提案，不能扩写本节点为自研通用PBIK。
