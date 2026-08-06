# Change: 分离Foot Placement规划与Leg IK求解

## Why

当前Corin Pose Graph把world-aware Foot Placement规划、Pelvis补偿和双腿解析式求解压在一个`FootPlacement`作者节点及一个运行operation中。这个封装让作者看不到“地面目标是否正确”和“腿链是否正确到达目标”之间的边界，也已经掩盖了两个真实实现错误：

- Planner输出`AnimatedBendNormal`与`PreferredBendNormal`，但运行控制把平面法线作为膝盖弯曲方向直接交给`CharacterComponentPoseLimbSolver`。法线与膝盖方向在合法两骨链中相差九十度，导致膝盖侧翻和腿部扭曲。
- `animation.foot-placement-weight`已经在Planner的contact、target、pelvis与solve weight中生效，staged executor又把同一参数乘到Pelvis和双腿结果，形成平方响应，违反现行“每条最终求解链只应用一次”的合同。

本地UE 5.7 Game Animation Sample Project默认`DDCvar.FootPlacementMode=1`，使用`Foot Placement node + Leg IK`；`mode=2`的`CR_Biped_FootPlacement`只是互斥替代方案。GASP通过Animation Modifier离线生成IK foot轨迹和`FootSpeed_L/R`，运行时由Foot Placement负责plant、ground trace、IK foot target与pelvis，再由Leg IK把Physical腿链解到IK foot位置。项目不应照搬UE可写IK骨骼作为隐藏数据载体，但应保留这一职责与执行阶段分离。

现行spec明确要求Planner与Solver实现分离，却又要求它们由同一个作者`FootPlacement` operation原子调用，并禁止Plan成为Graph port。该封装与项目“节点表达真实组合、显式空间和有序stage”的方向矛盾，也直接导致错误语义无法在作者图、Pose Watch和Live Debug中定位。

## What Changes

- 将作者图中的复合`FootPlacement`破坏性替换为显式两节点链：
  - `FootPlacement`继续是唯一有状态`WorldAwarePose`节点，读取Component Pose、最终Foot Features、唯一Foot Placement Weight、Body、PhysicsScene、Profile、Rig v3与Calibration；它负责contact、prediction、support、constraint、foot target、rotation target与pelvis计划，并只把pelvis补偿应用到其Component Pose输出。
  - `LegIK`是无状态`PurePose`节点，同时消费同一`FootPlacement`输出的Component Pose与typed `component.biped-leg-targets`，只负责把Rig v3左右Physical腿链解到目标。
- 让`CharacterFootPlacementPlan`降低为同帧固定workspace中的typed target value，并通过稳定Graph port表达依赖。Pose与targets共享call-site、CompletionIdentity、Rig identity/revision和表现帧寿命；targets不得跨帧缓存、跨Rig连接、扇出给多个solver或进入FinalPublication。
- 保留通用`TwoBoneIK`节点处理手臂或单肢显式effector；`LegIK`只消费Foot Placement生成的双腿目标，不取代通用TwoBoneIK，也不创建第二planner。
- 修正解析式腿部求解数学：
  - 运行ABI显式传递`BendPlaneNormal`，不得再命名或解释为`BendDirection`。
  - solver以当前`Hip -> TargetAnkle`轴和最终bend plane normal计算膝盖方向，再求解关节位置。
  - position weight先混合动画ankle与目标ankle，再执行保持上下腿长度的完整解析式求解；不得在解算后分别线性插值knee与ankle组件位置而改变骨长。
  - ankle rotation单独按rotation weight混合，并在同一Component Pose workspace重建受影响descendant。
- Foot Placement Weight只在Foot Placement规划阶段应用一次。LegIK消费Plan中的最终position、rotation与bend stabilization weight，不再拥有第二个Foot Placement Weight端口或重复乘法。
- 让Calibration几何验证进入正式发布边界：Apply和artifact rebuild继续执行统一validator；Definition Build必须消费匹配当前Calibration revision且携带合法geometry validation identity的Foot Analysis artifact；Runtime只匹配Projection发布的验证identity，不读取Sampling Rig或在Player中即时重建Editor几何。
- 将Corin正式图迁移为`LocalToComponentPose -> Left/Right Arm TwoBoneIK -> FootPlacement -> LegIK -> ComponentToLocalPose`，同时连接FootPlacement targets到LegIK。删除旧复合operation、旧`CharacterFootPlacementNativeControl`、旧bend direction命名与旧重复weight应用，不保留兼容reader、隐藏LegIK或模式开关。
- 拆分诊断与Pose Watch：FootPlacement显示contact、support、pelvis与目标；LegIK显示bend plane、转换后的knee direction、reach、residual与最终已求解Pose。

## Impact

- 影响Pose Graph node/port capability、Document v3、Presentation Mutation、Validator、Pose IR、Projection、stage compiler、固定workspace、Foot Placement runtime、解析式Limb Solver、Preview、Pose Watch、Live Debug与Corin正式资产。
- `CharacterPresentationPoseGraphAsset`和generated Native Pose Program schema发生破坏性变化；旧复合FootPlacement payload与operation直接删除。
- Foot Analysis artifact与Projection增加Calibration geometry validation identity；现有相关artifact与Corin Projection需要通过显式Character Build重新发布。
- 不改变Gameplay Body、KCC、Timeline Action、Motion Matching选择、网络、Rollback或AnimationClip资源所有权。
- 不引入UE Control Rig、Unity Animation Rigging、Final IK、图外MonoBehaviour solver、可写Transform target、IK骨骼兼容层或第二套Foot Placement实现。

## 与GASP、现行Spec及Active Change对比

- GASP默认用Foot Placement和Leg IK两个连续AnimGraph节点；本change采用同样职责分层和执行顺序，但用typed target port代替UE Skeleton中的`ik_foot_root/ik_foot_l/ik_foot_r`。Corin没有标准IK骨骼，新增可写控制骨会扩大Rig、source capture、blend、mask和final writer ABI；typed target value能在不污染骨架的情况下表达相同依赖。
- GASP离线烘焙foot speed与IK foot轨迹；本项目继续由Foot Analysis artifact烘焙sole速度、高度、plant confidence和下一落地特征。地面高度、surface normal、移动平台与最终pelvis仍只能运行时求解。
- `character-foot-placement-presentation`现行“Foot Placement是唯一world-aware骨骼控制节点”和“Plan不得成为Graph port”将被修改为“Foot Placement是唯一有状态world-aware规划节点，LegIK是唯一消费其targets的pure pose求解节点”。
- `character-presentation-pose-graph`现行节点集合没有`LegIK`与typed targets port，将同步扩展；通用`TwoBoneIK`合同保持。
- `complete-composable-pose-graph-editor-workflow`已完成的复合FootPlacement实现是本change的迁移前基线，不会与新链并存。
- `repair-foot-placement-calibration-and-limb-solving`任务声称bend语义、单次weight与Build验证已经闭合，但当前代码证明仍存在语义错配、重复乘法和验证边界缺口。本change以新ABI和显式节点边界完成纠正，不修改其历史任务为第二套实现。

