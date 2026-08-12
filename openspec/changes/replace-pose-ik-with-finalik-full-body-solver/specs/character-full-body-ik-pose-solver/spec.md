## MODIFIED Requirements

### Requirement: FullBodyIK只消费统一Foot Placement最终Goal

`FullBodyIK` MUST消费统一`FootPlacement`发布的一个最终Foot Goal Set与任意不重叠的其它Goal Sets。Current Support、Predictive Swing、Stance、Anchor与Pelvis MUST在该Goal Set发布前由同一Foot Placement owner完成。Compiler、Build Validator与Runtime MUST拒绝第二Foot Goal producer、独立Predictive Modifier、重复Foot slot、隐式Goal Merge、按port顺序覆盖或运行时择优。

#### Scenario: 统一Foot Placement进入FullBodyIK

- **WHEN** 作者把FootPlacement Goals和不重叠Hand Goals连接到FullBodyIK
- **THEN** FullBodyIK MUST只合并这两个Goal Set并执行一次FBBIK
- **AND** MUST不启动第二Grounding、独立Predictive Modifier、FinalIK Grounding或第二腿solver

## ADDED Requirements

### Requirement: Full Body IK必须复用FinalIK FBBIK核心数学

正式`FullBodyIK` MUST直接复用本地FinalIK `IKSolverFullBody`、chain、effector、mapping与constraint核心数学。项目 MAY把Transform I/O替换为stable indexed Pose Buffer，把生命周期替换为Actor preparation和显式frame调用，但 MUST不复制、改写或重新命名核心求解方程到项目自有solver。若Pose Buffer接入无法在不重写核心数学的情况下成立，实施 MUST停止并报告。

#### Scenario: Pose Buffer接入要求重写FBIK方程

- **WHEN** 某项接入要求复制或改写FinalIK核心solver迭代
- **THEN** 实施 MUST停止并记录精确阻塞点
- **AND** MUST不悄悄恢复旧LegIK、TwoBoneIK或自研PBIK

### Requirement: FinalIK必须通过唯一Pose Buffer backend执行

正式Character Runtime MUST通过stable bone index从Pending Component Pose读取Physical与Virtual Pose，并只向独立Pending Component Pose写入Physical Bone结果。Backend MUST在Physical写入后按Rig依赖重建Virtual Bone。正式链 MUST不创建shadow skeleton、target GameObject、`FullBodyBipedIK`、`GrounderFBBIK`或`LimbIK`组件，也 MUST不在Update、LateUpdate、OnAnimatorIK或Animancer外部读取或写入Physical Transform。每Actor solver、chain、mapping、goal和scratch workspace MUST在Runtime preparation时预分配。

#### Scenario: 一个正式PresentationFrame执行FullBodyIK

- **WHEN** Pose与全部Goal Set完成且lineage合法
- **THEN** Backend MUST在一个Pending Pose中执行一次`ReadPose -> Solve -> WritePose`
- **AND** 正常帧 MUST不创建GameObject、Transform、solver对象或managed集合

#### Scenario: Adapter需要第二套Transform骨架

- **WHEN** 某项FinalIK调用只能通过shadow skeleton或target Transform运行
- **THEN** 项目接入 MUST失败
- **AND** MUST不把该层级隐藏在Preview、Prefab或Runtime Factory

### Requirement: FullBodyIK必须使用显式Rig v4 biped binding

Rig v4 MUST唯一声明Solver Root、Pelvis、ordered Spine、左右Arm、左右`Hip -> Knee -> Foot` Leg及可选Head/Clavicle Physical BoneId，并保存合法reference pose。Runtime MUST只从该Rig和FullBodyIK Profile建立FinalIK binding，不得使用BipedReferences自动检测、Humanoid Avatar、bone name、Prefab组件或默认chain。左右腿bend constraint MUST从reference Hip-Knee-Foot plane构造，作为Lyra Knee PV的唯一项目映射。

#### Scenario: Corin右腿参考平面退化

- **WHEN** Corin Rig右Hip-Knee-Foot无法形成有限平面
- **THEN** Build或Runtime preparation MUST失败并报告RightLeg语义
- **AND** MUST不使用世界前方或上一帧膝盖方向

### Requirement: FullBodyIK Profile必须只表达FinalIK真实能力

`CharacterFullBodyIkProfile` MUST只保存FinalIK FBBIK真实支持的iterations、FABRIK pass、spine stiffness、body pull、chain pin/pull/push/push-parent/reach、limb mapping、maintain rotation与bend constraint参数。它 MUST不保存UE PBIK Preferred Angle/Excluded Bone/Axis Limit，也 MUST不保存Lyra Sphere Trace、foot smoothing、pelvis blend或FinalIK Grounding字段。

#### Scenario: 作者编辑FullBodyIK Profile

- **WHEN** 作者选择Corin FullBodyIK Profile
- **THEN** Inspector MUST只显示FinalIK FBBIK真实参数
- **AND** Lyra Foot Plant参数 MUST只显示在Foot Placement Profile

### Requirement: FullBodyIK必须消费可组合typed Goal Sets

`component.full-body-ik-goals` MUST是同帧Component空间瞬时value。每个Goal Set MUST携带Frame Sequence、Completion Identity、Rig Id/Revision、Producer Node/Call Site及固定容量goal slice；每个goal MUST携带唯一Effector Slot、目标Component Position/Rotation、Position/Rotation Weight、Goal Application与Source Kind。Goal Application MUST显式区分普通绝对effector target、`FootPlacementEffectorTarget`与pelvis pre-solve translation，不得包含`GroundingEffectorTarget`、Toe Plant Pivot或PlantPivotWeight。

Compiler MUST拒绝重复Effector Slot、超出容量、跨Rig或无法建立唯一producer的连接；Runtime MUST拒绝跨帧或lineage不匹配，不得使用最后写入获胜、字符串查找或旧Goal。

#### Scenario: 最终Foot与Hand Goals来自两个Goal Source

- **WHEN** FootPlacement发布最终Pelvis/Feet goals且PoseBoneIKGoals发布Hands
- **THEN** Compiler MUST按稳定动态port顺序形成唯一Goal merge plan
- **AND** FullBodyIK MUST在一次solve中消费全部不重叠slots

#### Scenario: 两个Goal Set同时写LeftFoot

- **WHEN** Graph把FootPlacement Goal和另一个Foot Goal producer同时连接
- **THEN** Build MUST报告LeftFoot重复producer
- **AND** MUST不按port顺序覆盖

### Requirement: FullBodyIK必须按Lyra可见顺序应用pelvis与feet

FullBodyIK MUST先复制输入Component Pose到独立Pending output，按最终Foot Goal Set中的唯一Component空间`PelvisPreSolveTransform`及其显式pivot调整Pelvis subtree；该Transform只能由现有Stance Stabilization owner发布，包含经过双腿reach夹紧的竖直translation与有限body/pelvis rotation。随后 MUST把`FootPlacementEffectorTarget`的旋转作为foot pre-rotation写入Pending Pose，并把同一Goal的Component Position作为绝对effector position交付，使它在FinalIK `ReadPose`与`LimitBend`之后仍由Solve按绝对目标消费；再设置普通Body/Hand effectors，最后只执行一次FBBIK `ReadPose -> Solve -> WritePose`。Foot Position MUST不在FinalIK内部`LimitBend`修改腿链参考Pose之前预先降低成相对`positionOffset`；未提供的effectors MUST在本帧明确归零。

FullBodyIK MUST不查询world、不读取AnimationClip或Foot Placement Profile、不决定trace/plant/swing、不平滑foot/pelvis，也 MUST不调用FinalIK Grounding、`GrounderFBBIK`、TwoBoneIK或LegIK。

#### Scenario: 左脚接地且双手跟随武器目标

- **WHEN** Pelvis、LeftFoot、RightFoot与双手Goals合法
- **THEN** FullBodyIK MUST先应用Pelvis translation，再在同一个FBBIK solve中满足全部effectors
- **AND** MUST不为Lyra左右腿分别执行Basic IK

### Requirement: FullBodyIK失败必须服从动画帧事务

Goal lineage错误、Rig mapping错误、非有限输入、FinalIK mapping/solver失败、非有限输出，或满位置权重`FootPlacementEffectorTarget`对应的FinalIK内部end-effector solver node位置残差超过`0.001m`，MUST产生typed failure，阻断ComponentToLocalPose、后续stage与FinalPublication。solver残差失败 MUST保留对应Goal Set、Foot Slot、绝对目标、solver node、映射后Physical Foot及两层残差诊断。若已跨过Animancer Evaluate Barrier，同一Actor Animation Runtime MUST进入Faulted，不得逆序恢复Player状态或Physical Bone快照，也不得发布只完成pelvis、单腿或手臂的部分Pose。映射后的Physical Foot位置残差只要有限，MUST不单独升级为solver失败。

#### Scenario: FinalIK映射输出非有限旋转

- **WHEN** WritePose检测到任一Physical Bone rotation非有限
- **THEN** FullBodyIK MUST失败并阻断FinalPublication
- **AND** MUST不沿用上一帧Solved Pose

#### Scenario: 满权重Foot目标未被FBBIK内部节点满足

- **WHEN** LeftFoot或RightFoot使用满位置权重`FootPlacementEffectorTarget`且对应end-effector solver node的求解后位置残差超过`0.001m`
- **THEN** FullBodyIK MUST返回`FootEffectorSolverResidualExceeded`并报告对应Goal Set与Foot Slot
- **AND** MUST阻断FinalPublication，不得把明显偏离目标的脚Pose作为成功帧发布

#### Scenario: Rig映射产生有限Physical Foot残差

- **WHEN** 满权重Foot的end-effector solver node残差不超过`0.001m`，但映射回真实Rig层级后的Physical Foot存在有限非零位置残差
- **THEN** FullBodyIK MUST允许该完整Pose继续提交，并发布目标、solver node、Physical Foot与两层残差诊断
- **AND** MUST不运行第二solver、后处理回拉或兼容路径

### Requirement: FullBodyIK必须提供分层只读诊断

Diagnostics MUST只读暴露backend source identity、Rig binding、Profile revision、Goal Set lineage、每effector目标与权重、最终pelvis pre-solve transform及其pivot、chain pull/reach、iterations、bend constraint、输入/输出Pose completion、residual与typed failure。Target Watch MUST观察统一FootPlacement Final Goals与Hand Goals；Current Support、Prediction、Stance与Pelvis细节只从同一FootPlacement完成快照展开。Diagnostics MUST从已完成固定workspace复制，不得第二次调用FinalIK、重新执行world query、创建Transform或遍历Animator反推。

#### Scenario: 排查右手与左脚互相拉扯

- **WHEN** 同一FBBIK solve中RightHand与LeftFoot residual异常
- **THEN** Live Debug MUST显示两个Goal、对应chain参数、pelvis pre-solve和solver residual
- **AND** MUST不通过关闭某个隐藏第二solver来排查
