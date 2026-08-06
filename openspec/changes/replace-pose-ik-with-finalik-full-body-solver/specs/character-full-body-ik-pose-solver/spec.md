## ADDED Requirements

### Requirement: Full Body IK必须复用FinalIK FBBIK核心数学

正式`FullBodyIK`节点 MUST使用项目已安装FinalIK `IKSolverFullBodyBiped`的chain、effector、FABRIK、trigonometric pass、bend constraint与mapping数学。项目 MAY把bone identity、Pose读写、初始化和mapping I/O抽象为indexed Pose Buffer backend，但 MUST不复制或重写FBIKChain Push、Reach、Stage1、Stage2、FABRIK iteration、trigonometric solve、effector solve或bend constraint方程。Backend MUST发布稳定source identity与参与源码hash；身份不匹配 MUST使Projection Build或Runtime preparation失败。

#### Scenario: Pose Buffer接入需要改写FBIK方程

- **WHEN** 实施审计发现无Transform接入必须复制或改变FinalIK核心求解方程
- **THEN** 本change实施 MUST停止并报告精确依赖
- **AND** MUST不以项目自研solver、shadow skeleton或旧LegIK继续实施

### Requirement: FinalIK必须通过唯一Pose Buffer backend执行

正式Character Runtime MUST通过stable bone index从Pending Component Pose读取Physical与Virtual Pose，并只向独立Pending Component Pose写入Physical Bone结果。Backend MUST在Physical写入后按Rig依赖重建Virtual Bone。正式链 MUST不创建shadow skeleton、target GameObject、`FullBodyBipedIK`组件、`GrounderFBBIK`组件或`LimbIK`组件，也 MUST不在Update、LateUpdate、OnAnimatorIK或Animancer外部读取或写入Physical Transform。每Actor solver、chain、mapping、goal和scratch workspace MUST在Runtime preparation时预分配，正常PresentationFrame MUST不创建solver对象或managed集合。

#### Scenario: 一个正式PresentationFrame执行FullBodyIK

- **WHEN** FullBodyIK收到合法Component Pose与Goal Sets
- **THEN** FinalIK MUST只在Pose Buffer backend中完成ReadPose、Solve与WritePose
- **AND** Physical Transform MUST仍只由最终Physical Transform writer写一次

#### Scenario: Adapter需要第二套Transform骨架

- **WHEN** backend无法在不创建shadow skeleton的情况下提供FinalIK所需Pose访问
- **THEN** Runtime preparation MUST失败且本change实施 MUST停止
- **AND** MUST不把Transform复制作为隐藏或可选fallback

### Requirement: FullBodyIK必须使用显式Rig v4 biped binding

Animation Rig v4 MUST在同一Physical Bone catalog中显式声明Solver Root、Pelvis、ordered Spine、左右Arm chain、左右Leg chain与可选Head/Clavicle。Solver Root MUST是Pelvis或Spine成员；全部chain MUST有合法父子关系、有限正segment length与非退化reference bend plane。FullBodyIK binding MUST只按Rig BoneId和dense index建立，不得调用FinalIK `BipedReferences.AutoDetectReferences`、Humanoid Avatar、Transform名称搜索或默认biped补全。

#### Scenario: Corin右臂语义缺失

- **WHEN** Rig v4没有Right Upper Arm、Forearm或Hand之一
- **THEN** Rig validation与Projection Build MUST失败并报告缺失slot
- **AND** Runtime MUST不通过Animator Humanoid mapping或名称搜索补全

#### Scenario: Rig参考膝盖完全退化

- **WHEN** Rig v4参考Pose无法为一条腿生成有限bend plane
- **THEN** Rig Apply或Build MUST失败
- **AND** FullBodyIK MUST不使用世界前方、角色前方、旧Calibration或上一帧bend方向补值

### Requirement: FullBodyIK Profile必须只表达FinalIK真实能力

唯一`CharacterFullBodyIkProfile` MUST保存Iterations、FABRIK Pass、Spine Stiffness、Pull Body Vertical/Horizontal、每chain Pin/Pull/Push/Push Parent/Reach与smoothing、每limb mapping Weight/Maintain Rotation、bend constraint Weight/Clamp及全局Node Weight。Profile identity/revision MUST进入Projection依赖。Profile MUST不保存FinalIK FBBIK未直接提供的UE PBIK逐骨Position/Rotation Stiffness、任意XYZ Rotation Limit、Preferred Angle、Excluded Bone、Stretch或Root Behavior，也 MUST不保存solver backend选择或fallback枚举。

#### Scenario: 作者编辑FullBodyIK Profile

- **WHEN** 作者修改Left Leg Pull或Spine Stiffness
- **THEN** Profile revision与Projection source revision MUST改变
- **AND** Runtime MUST只在显式Build后消费新值

#### Scenario: Document提交UE Preferred Angle字段

- **WHEN** FullBodyIK Profile或节点payload包含未声明的Preferred Angle
- **THEN** strict parser与Mutation MUST拒绝该字段
- **AND** MUST不把它近似映射为FinalIK bend weight

### Requirement: FullBodyIK必须消费可组合typed Goal Sets

`component.full-body-ik-goals` MUST是同帧Component空间瞬时value。每个Goal Set MUST携带Frame Sequence、Completion Identity、Rig Id/Revision、Producer Node/Call Site及固定容量goal slice；每个goal MUST携带唯一Effector Slot、目标Component Position/Rotation、Position/Rotation Weight、Goal Application、Source Kind与只读diagnostic metadata。Goal Application MUST显式区分普通绝对effector target、FinalIK Grounding effector target与pelvis pre-solve translation，不得由Source Kind或节点类型暗中推断。`FullBodyIK` MUST通过stable动态Goal输入port消费一个或多个Goal Set，并按编译顺序合并。Compiler MUST拒绝重复Effector Slot、超出容量、跨Rig或无法建立唯一producer的连接；Runtime MUST拒绝跨帧或lineage不匹配，不得使用最后写入获胜、字符串查找或旧Goal。

#### Scenario: 双脚和双手来自两个Goal Source

- **WHEN** PredictiveFootPlacement发布Body/Feet goals且PoseBoneIKGoals发布Hand goals
- **THEN** FullBodyIK MUST在同一次solve中消费五个effectors
- **AND** 两个Goal Set MUST共享当前Frame与Rig lineage

#### Scenario: 两个Goal Set同时写LeftHand

- **WHEN** FullBodyIK的两个动态输入都声明LeftHand Effector Slot
- **THEN** Graph Validator与Build MUST拒绝该拓扑
- **AND** Runtime MUST不按port顺序覆盖其中一个目标

### Requirement: FullBodyIK必须按成熟FBBIK顺序应用pelvis与effectors

FullBodyIK MUST先复制输入Component Pose到独立Pending output，按PredictiveFootPlacement goal中的唯一`PelvisPreSolveTranslation`调整Pelvis subtree。普通Body与Hand Goal MUST按绝对position/rotation weight写入FinalIK effectors；FinalIK Grounding Foot Goal MUST按stock `GrounderFBBIK.SetLegIK`语义，以目标和pelvis平移后foot bone的Component位置差写入`positionOffset`，并在FBBIK `ReadPose`前把目标rotation差值按Goal weight预乘到foot bone。带`Toe Plant Pivot`的Foot Goal MUST同时携带完整ankle目标、Component空间toe plant point与`PlantPivotWeight`；FullBodyIK MUST按相同Goal position/rotation weight插值plant point和rotation，并从该toe point反推出唯一ankle position offset，`PlantPivotWeight`只在普通ankle offset与toe-preserving offset之间混合。Grounding Foot Goal MUST保持对应effector的`positionWeight`与`rotationWeight`为零，最后只执行一次FBBIK `ReadPose -> Solve -> WritePose`。未提供的effector MUST在本帧明确归零。FullBodyIK MUST不查询world、不读取AnimationClip或Foot Placement Profile、不决定contact lifecycle、不再次计算pelvis plan，也 MUST不调用FinalIK Grounding或GrounderFBBIK。

#### Scenario: 左脚锁定且双手跟随武器Virtual Bone

- **WHEN** Foot goals包含Locked LeftFoot且Hand goals包含左右武器目标
- **THEN** FullBodyIK MUST在同一Pending Pose中先应用pelvis offset再一次求解全部effectors
- **AND** MUST不存在单独LegIK、TwoBoneIK或第二次FinalIK solve

#### Scenario: 左脚以脚尖为支点抬起脚跟

- **WHEN** LeftFoot Goal声明合法Toe Plant Pivot、完整ankle目标与0.5 Goal weight
- **THEN** FullBodyIK MUST在一次FBBIK求解前把toe plant point和ankle rotation都按0.5权重推进
- **AND** MUST从该加权toe point反推出ankle position offset而不是围绕ankle原地旋转
- **AND** MUST不创建Toe Effector、第二LegIK或第二world query

### Requirement: FullBodyIK失败必须服从动画帧事务

Invalid Rig、invalid Profile、goal lineage mismatch、duplicate effector、non-finite input、mapping failure或FinalIK solver failure MUST产生稳定typed failure并阻断后续Pose stage与FinalPublication。失败发生在Animancer Evaluate Barrier后时，对应Actor Animation Runtime MUST进入Faulted；系统 MUST不逆序恢复状态或Physical Bone快照，也 MUST不发布只应用pelvis、只完成手臂或只完成一条腿的部分Pose。

#### Scenario: FinalIK映射输出非有限旋转

- **WHEN** FullBodyIK WritePose得到non-finite Physical Bone rotation
- **THEN** executor MUST阻断ComponentToLocalPose与FinalPublication
- **AND** Actor Animation Runtime MUST进入正式Faulted路径

### Requirement: FullBodyIK必须提供分层只读诊断

Diagnostics MUST只读暴露backend source identity、Rig binding、Profile revision、Goal Set lineage、每effector目标与权重、pelvis pre-solve translation、chain pull/reach、iterations、bend constraint、输入/输出Pose completion、residual与typed failure。Pose Watch MUST观察FullBodyIK完成后的Component Pose；Target Watch MUST分别观察PredictiveFootPlacement与PoseBoneIKGoals发布的Goal Sets。Diagnostics MUST从已完成固定workspace复制，不得第二次调用FinalIK、重新执行world query、创建Transform或遍历Animator反推。

#### Scenario: 排查右手与左脚互相拉扯

- **WHEN** 作者同时观察Hand Goal Set、Foot Goal Set与FullBodyIK Pose
- **THEN** Live Debug MUST显示同一Frame的effectors、chain weights与最终residual
- **AND** Debug读取 MUST不改变下一帧solver状态或目标

#### Scenario: 连续排查跑动时脚权重偏低

- **WHEN** 作者以Continuous级别采集Foot Placement Trace
- **THEN** 每个PresentationFrame MUST在同一typed payload中记录左右Plant Confidence、Placement/Plant/Contact Weight、最终Goal Weight、grounding hit、constraint、pelvis plan与FullBodyIK residual
- **AND** Capture MUST能够直接导出连续帧数据
- **AND** `CharacterPipelineHost`与`FixedCharacterHost` Inspector MUST复用同一Runtime Diagnostics入口，并按各自Host Instance Id附着同一运行时目标
- **AND** `FixedCharacterHost`脚IK入口 MUST只申请Foot Placement Live/Capture通道，Inspector预览 MAY降频但Capture MUST保留逐PresentationFrame数据，并 MUST在固定有界segment数量后自动结束
- **AND** Trace发布与导出 MUST只消费已完成Presentation snapshot，不得读取FinalIK mutable solver、重新查询world或再次求解
