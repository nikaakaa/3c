## RENAMED Requirements

- FROM: `### Requirement: Foot Placement必须是Pose Graph中唯一world-aware postprocess节点`
- TO: `### Requirement: Foot Placement必须是Pose Graph中唯一有状态world-aware骨骼控制节点`

## MODIFIED Requirements

### Requirement: Rig Calibration必须在精确Sampling Rig上下文可视化编辑

Sole frame MUST只通过heel/toe接触点间接编辑，并由heel-to-toe平面投影与VisualRoot up自动派生。系统 MUST从`CharacterFootPlacementAnalysisSource`提供显式`Edit Rig Calibration`入口，并以该Source精确引用的Sampling Rig、Rig v3、Calibration v3与Calibration Preview建立唯一Editor session。Scene View MUST只允许作者编辑左右heel/toe contact；preferred bend direction MUST由当前预览姿势的`Hip -> Knee -> Ankle`弯曲方向自动派生并作为只读方向显示，MUST不提供手动Knee Bend位置、方向或pole override。Apply MUST同时获得左右腿有限非退化的自动bend direction并通过统一几何validator，随后以单次Undo只更新唯一Calibration资产；非法draft MUST保留旧正式数据。编辑、打开、selection、repaint与handle拖动 MUST不自动执行Foot Analysis、Projection Compile或Build。

`CharacterFootPlacementAnalysisSource` MUST显式配置持久化的Calibration Preview Clip与归一化预览时间。进入校准session时，Editor MUST在独立Animation Mode driver拥有的临时PlayableGraph中把该固定帧采样到Sampling Rig；退出、切换Prefab Stage或采样失败时 MUST恢复进入前姿势并释放preview graph。Preview Pose MUST只改变作者看到的姿势并作为自动派生输入；派生结果只有Apply后才可进入唯一Calibration资产。系统 MUST不生成第二套Calibration owner，也 MUST不让Preview Pose进入Runtime Foot Placement链路。

#### Scenario: 作者校准Corin右脚鞋底

- **WHEN** 作者从Corin Analysis Source进入精确Sampling Rig并调整右脚heel/toe
- **THEN** Scene View MUST只读显示由当前Calibration Preview自动派生的右腿bend direction
- **AND** Apply MUST把左右腿自动bend direction与heel/toe、sole frame原子写入唯一Calibration资产

#### Scenario: Calibration Preview腿部完全伸直

- **WHEN** 任一侧`Hip -> Knee -> Ankle`无法形成有限非退化的弯曲方向
- **THEN** 校准页面 MUST明确报告该侧Preview Pose无稳定膝弯曲并禁用Apply
- **AND** MUST不沿用旧direction、猜测VisualRoot轴或要求作者拖动pole位置

### Requirement: Foot Placement必须是Pose Graph中唯一有状态world-aware骨骼控制节点

启用Foot Placement的Character Presentation Pose Graph MUST显式包含一个接收并输出Component Pose的`FootPlacement`节点。Pose Graph Compiler MUST把该节点降低为DAG中对应位置的world-aware stage，复用正式Planner、PhysicsScene query、Rig v3 Calibration和解析式Limb Pose Solver，并只在节点output workspace中发布已修改pelvis与双腿的Component Pose。Runtime MUST允许后续Pose节点消费该输出，不得在图外追加Foot Placement Pass，不得由Final IK、Animator、MonoBehaviour或其它manager自主更新形成第二骨骼写入路径。每个最终Output路径 MUST最多包含一个有状态FootPlacement实例。

#### Scenario: 一个表现帧更新Corin

- **WHEN** Corin Pose Plan包含FootPlacement节点且上游Component Pose完成
- **THEN** Runtime MUST执行一次Planner、query与Pose solver并发布节点输出
- **AND** FinalAnimationPoseFrame MUST只在全部下游节点和final writer完成后发布

#### Scenario: Final IK组件仍存在

- **WHEN** rig validation发现旧Final IK Foot Placement solver或自主写骨骼组件
- **THEN** runtime创建 MUST失败
- **AND** 系统 MUST不接受同帧双求解

### Requirement: Foot Placement 必须只消费表现帧正式输入

Foot Placement MUST只读取同帧`CharacterBodyPresentationFrame`、带有效lease的上游Component Pose Value、最终Pose contribution与Foot Features、显式`CharacterFootPlacementProfile`、Rig v3、同identity Rig Calibration和当前Unity PhysicsScene查询结果。Profile构造runtime settings时 MUST从`Projection.PoseProgram.Parameters`一次性绑定唯一`animation.foot-placement-weight`的`PoseParameterId`、dense index与`PoseProgramHash`；operation MUST核对同帧Completion、Availability、ProgramHash和有限归一化Weight。若上游包含Inertialization或其它composition，Foot Placement MUST读取其实际输出，MUST不遍历source重新计算混合结果。它 MUST不读取visible playback列表、Layer、producer binding、AnimationClip、BTSMTL runtime、State、Action、Blackboard、GameplayTag、Marker语义、MotionWarp target、Network Model私有状态或logic Transform作为替代真相。

#### Scenario: 读取CrossFade后的最终姿态

- **WHEN** Outgoing与Current source经Blend Stack和上游Pose节点共同形成Component Pose
- **THEN** Foot Placement MUST只消费该次Completion对应的Pose、Foot Features和`animation.foot-placement-weight`
- **AND** MUST不遍历source重新计算一次混合结果

#### Scenario: Runtime Projection缺少生成特征

- **WHEN** Foot Placement需要的dense Foot Feature未被Projection发布
- **THEN** world-aware stage MUST失败并报告确切缺失字段
- **AND** MUST不从AnimationClip或Transform现场重建特征

#### Scenario: 左脚分支正在惯性衰减

- **WHEN** 上游Local Pose惯性化后转换为Component Pose且左脚贡献正在衰减
- **THEN** Foot Placement MUST使用最终传播到节点输入的左脚Feature与Weight
- **AND** MUST不读取Inertialization私有Accumulator决定接触

### Requirement: Foot Placement Planner与骨骼Solver必须分离

`CharacterFootPlacementPlanner` MUST只根据正式输入和world query生成vendor-neutral`CharacterFootPlacementPlan`，不得写Pose或Transform。`CharacterComponentPoseLimbSolver` MUST只根据上游Component Pose、Rig v3 chain、Calibration与Plan修改pelvis和双腿Pose，不得查询world、读取AnimationClip或决定contact lifecycle。两者 MUST由同一个FootPlacement operation原子调用，Plan MAY进入diagnostics但 MUST不成为作者Graph port。Core runtime MUST不依赖`ICharacterFootPlacementSolver`、Final IK或MonoBehaviour solver。

#### Scenario: 解析式solver应用一帧计划

- **WHEN** Planner发布左右脚目标与pelvis offset
- **THEN** CharacterComponentPoseLimbSolver MUST在FootPlacement output workspace应用该计划
- **AND** final writer之前 MUST不存在Transform写入

#### Scenario: 后续替换Solver实现

- **WHEN** 后续引入保持同一Component Pose solver contract的新数值实现
- **THEN** Planner、Profile、Calibration、Pose Graph节点和Document MUST保持不变
- **AND** 实现替换 MUST不恢复vendor adapter或第二作者配置

### Requirement: Foot Placement 配置和Rig必须显式且可验证

FootPlacement节点 MUST显式引用Profile与Calibration；Definition MUST显式引用Rig v3与唯一Animation Rig Binding；Foot Analysis Source MUST显式引用同一Rig v3、Sampling Rig与Calibration。Rig v3 MUST唯一声明pelvis及左右Hip、Knee、Ankle、Toe Physical BoneId。Build与runtime create MUST校验全部identity/revision、Physical chain、父子关系、腿长、sole frame、preferred bend和world binding。系统 MUST不按名字、Humanoid Avatar、Prefab旧组件或默认轴猜测配置。

#### Scenario: Corin Runtime与Projection使用不同Calibration

- **WHEN** Runtime节点、Foot Analysis artifact或Projection引用不同Calibration revision
- **THEN** Character Build或runtime create MUST失败
- **AND** MUST报告三方identity而不是使用任一默认值

#### Scenario: sole frame或腿部校准退化

- **WHEN** sole frame不正交、bend reference退化或腿链长度非法
- **THEN** Calibration/Rig validator MUST阻止Apply与Build
- **AND** runtime MUST不归一化为猜测方向

### Requirement: Preview 必须遵守正式世界上下文边界

Foot Placement Preview MUST通过共享AnimationPreviewRuntime执行同一staged Pose Plan。只有精确CharacterPipelineHost提供匹配Definition、Rig v3、Animation Rig Binding、World-Aware Binding、Body fixture与实际PhysicsScene时，Preview才可执行query与solver。上下文缺失时 MUST在FootPlacement节点报告typed Unavailable，MUST不创建假地面、默认solver或历史Pose。

#### Scenario: 纯动画预览攻击clip

- **WHEN** Timeline或Pose Source预览只有动画资源而没有精确Host world context
- **THEN** 动画source与pure-pose阶段 MAY继续显示
- **AND** FootPlacement输出与FinalAnimationPoseFrame MUST明确Unavailable
