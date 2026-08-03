## 1. Pose空间与Capability合同

- [x] 1.1 将Pose port稳定类型定义为`pose.local`与`pose.component`，删除通用`pose.value`。
- [x] 1.2 为Local Pose节点逐一声明typed input/output、availability与execution domain。
- [x] 1.3 为Component Pose节点逐一声明typed input/output、availability与execution domain。
- [x] 1.4 新增`LocalToComponentPose`作者payload、Capability、静态port与Details描述。
- [x] 1.5 新增`ComponentToLocalPose`作者payload、Capability、静态port与Details描述。
- [x] 1.6 让GraphInput与GraphOutput的动态port保存显式Pose空间并拒绝通配。
- [x] 1.7 让OutputPose只接受Local Pose。
- [x] 1.8 让Graph Canvas按Capability投影Pose空间颜色、标签与合法连接反馈。
- [x] 1.9 让clipboard、duplicate、Undo与layout往返保留转换节点和typed edge identity。
- [x] 1.10 删除旧通用Pose port枚举、catalog分支和隐式空间假设。

## 2. Pose authoring、Document与Validator闭合

- [x] 2.1 扩展Presentation authoring models以保存转换节点和Graph/Subgraph端口空间。
- [x] 2.2 扩展唯一Presentation Mutation以创建、修改、连接和删除空间化节点。
- [x] 2.3 扩展Capability驱动的edge validator，拒绝Local/Component错连。
- [x] 2.4 扩展Subgraph签名validator，拒绝调用点与定义空间不一致。
- [x] 2.5 扩展Document v3 Presentation models与strict codec。
- [x] 2.6 扩展Document exporter与package mapper，稀疏输出空间化port和转换节点。
- [x] 2.7 扩展Document reconciler与Mutation planner，复用人工编辑同一Mutation。
- [x] 2.8 扩展Agent validator与readonly context，输出Capability和Rig v3诊断。
- [x] 2.9 修正Marker Sync Document owner规则，允许Action Track与Pose Source binding各自唯一拥有。
- [x] 2.10 删除旧Document通用Pose port reader和Presentation兼容字段。

## 3. Rig v3与唯一腿部映射

- [x] 3.1 将Rig Definition升级为v3并增加Pelvis BoneId。
- [x] 3.2 为左腿增加Hip、Knee、Ankle、Toe Physical BoneId。
- [x] 3.3 为右腿增加Hip、Knee、Ankle、Toe Physical BoneId。
- [x] 3.4 删除旧LeftFootBoneId与RightFootBoneId。
- [x] 3.5 扩展Rig compiler生成dense pelvis与左右腿chain索引。
- [x] 3.6 扩展Rig validator检查Physical、唯一性、父子链、长度与同Rig revision。
- [x] 3.7 让CharacterAnimationRigBinding成为唯一Physical Bone Transform绑定。
- [x] 3.8 新增通用World-Aware Presentation Binding，只保存self-collider排除和world fixture绑定。
- [x] 3.9 删除CharacterFootPlacementRig及其Prefab腿骨字段。
- [x] 3.10 升级Sampling Rig工具的Rig Mapping页面。
- [x] 3.11 实现pelvis与左右腿chain的骨骼选择器和场景高亮。
- [x] 3.12 实现parent chain、leg length与duplicate bone即时诊断。
- [x] 3.13 让Rig Mapping与Calibration共用同一预览姿势和目标Prefab identity，并由该姿势自动派生左右腿preferred bend reference，删除手动Knee Bend位置控制。

## 4. Foot Analysis身份与Pose Source输入

- [x] 4.1 让Foot Analysis Source显式引用Rig Definition v3。
- [x] 4.2 将Rig revision加入Foot Analysis input snapshot与artifact key。
- [x] 4.3 让Analyzer按Rig v3 dense chain解析ankle/toe，不读取Prefab专用Foot rig。
- [x] 4.4 让Artifact validator核对Rig、Sampling Rig与Calibration三方identity/revision。
- [x] 4.5 让Projection依赖hash包含Rig v3与artifact版本。
- [x] 4.6 删除旧Foot Analysis对Left/Right Foot字段和Transform名称的解析路径。
- [x] 4.7 补齐Sequence Pose Source binding的SyncRole正式字段。
- [x] 4.8 让Pose Source marker、topology、SyncRole与typed curve进入同一validated binding schema。

## 5. 有序staged Pose compiler

- [x] 5.1 扩展Pose IR operation以携带输入/输出Pose空间和execution domain。
- [x] 5.2 新增LocalToComponent lowering handler。
- [x] 5.3 新增ComponentToLocal lowering handler。
- [x] 5.4 实现Local Pose到Component Pose的全catalog转换operation。
- [x] 5.5 实现Component Pose到Local Pose的全catalog转换operation。
- [x] 5.6 将固定四阶段plan替换为按拓扑切分的ordered stage table。
- [x] 5.7 为每个stage生成固定operation range、workspace、completion和diagnostic layout。
- [x] 5.8 让pure-pose stage可在world-aware stage之后继续执行。
- [x] 5.9 保持每个source demand/capture每帧最多一次。
- [x] 5.10 保持PlayableGraph每帧最多Evaluate一次。
- [x] 5.11 保持最终Physical Transform writer每帧最多一次。
- [x] 5.12 实现stage失败时阻断后续stage与FinalPublication，并让同一Animation Presentation Runtime进入Faulted。
- [x] 5.13 删除旧`WorldAwarePostProcess`固定尾阶段假设。

## 6. Component Pose骨骼控制

- [x] 6.1 将ModifyBone输入输出迁为Component Pose。
- [x] 6.2 将TwoBoneIK输入输出迁为Component Pose。
- [x] 6.3 从现有TwoBoneIK实现提取可复用Physical chain解析式求解内核。
- [x] 6.4 让TwoBoneIK按Rig v3 chain/reference编译并只写目标Physical chain。
- [x] 6.5 在控制节点完成后重算受影响descendant与Virtual Bone依赖。
- [x] 6.6 为每个骨骼控制发布space、chain、reach与completion诊断。

## 7. Foot Placement真实Pose节点

- [x] 7.1 将FootPlacement Capability输入输出迁为Component Pose并声明world-aware domain。
- [x] 7.2 将FootPlacement compiler handler从透传边界改为正式world-aware operation。
- [x] 7.3 让operation读取同帧Body frame、上游Pose、参数、generated feature、Profile、Calibration和PhysicsScene。
- [x] 7.4 保持Planner只生成contact/support/lock/replant/foot target/pelvis plan。
- [x] 7.5 实现CharacterComponentPoseLimbSolver把pelvis计划应用到Component Pose workspace。
- [x] 7.6 实现左腿reach clamp、bend plane、near-extension与ankle semantic rotation求解。
- [x] 7.7 实现右腿reach clamp、bend plane、near-extension与ankle semantic rotation求解。
- [x] 7.8 让FootPlacement输出真实已修改Component Pose和有限completion。
- [x] 7.9 让Compiler限制每个最终Output路径最多一个有状态FootPlacement实例。
- [x] 7.10 删除Native Job中的FootPlacement输入复制实现。
- [x] 7.11 删除CharacterSimulationPresentationRuntime图外PresentPosePostProcess。
- [x] 7.12 删除ICharacterFootPlacementSolver。
- [x] 7.13 删除FinalIKLimbFootPlacementSolver和Foot Placement对RootMotion.FinalIK的程序集引用。
- [x] 7.14 删除CharacterFootPlacementComposition及其Host装配。
- [x] 7.15 删除Gameplay Lab对Final IK/旧composition的启动校验。
- [x] 7.16 让Foot Placement reset与Projection replacement进入Pose Plan统一事务。

## 8. Pose Source Editor

- [x] 8.1 抽取Timeline Field的时间尺接口，不依赖Timeline Track/Clip owner。
- [x] 8.2 抽取Marker interaction、geometry与rendering接口。
- [x] 8.3 抽取Curve interaction、geometry与rendering接口。
- [x] 8.4 抽取Analysis候选显示与Apply接口。
- [x] 8.5 保持Timeline通过原typed adapter使用抽取后的唯一模块。
- [x] 8.6 新增Presentation Profile `Open Source`入口和source-kind路由。
- [x] 8.7 实现Sequence Pose Source时间页和source信息区。
- [x] 8.8 接入Sync Marker lane的新增、删除、拖动、分组、循环闭合和Undo。
- [x] 8.9 接入typed Foot Placement Weight Curve lane。
- [x] 8.10 接入curve key多选、框选、精确值、切线、weighted tangent、复制粘贴和Undo。
- [x] 8.11 接入Foot Analysis generated channel与候选过期诊断。
- [x] 8.12 实现左右脚接触候选到Pose Source binding marker的显式Apply。
- [x] 8.13 让BlendSpace编辑器复用sample Marker/Curve/Analysis模块。
- [x] 8.14 让Motion Matching入口导航到正式Source Set/Database与artifact上下文。
- [x] 8.15 删除CharacterPresentationPoseSourceBindingEditor中的Marker文本字段与CurveField写入口。
- [x] 8.16 保持打开、编辑、Undo和Preview不自动Build。

## 9. Pose Graph Preview、Pose Watch与Live Debug

- [x] 9.1 将Pose Graph Preview面板的数据源从Action producer改为Fact fixture。
- [x] 9.2 实现Grounded、Speed、Acceleration和Vertical Speed编辑控件。
- [x] 9.3 实现Movement Direction、Desired Direction、Facing Error和Motion Phase编辑控件。
- [x] 9.4 按Capability生成typed parameter fixture控件。
- [x] 9.5 让面板调用EvaluatePoseGraphPreview而不是EvaluateTimelinePreview。
- [x] 9.6 扩展Preview target解析以获取精确Definition、Rig、Body与World-Aware Binding。
- [x] 9.7 从target所在Scene获取实际PhysicsScene并核对identity。
- [x] 9.8 让AnimationPreviewRuntime执行同一staged Pose Plan和FootPlacement operation。
- [x] 9.9 在world context不完整时发布首个world-aware节点typed Unavailable。
- [x] 9.10 删除Preview假地面、默认Rig和历史Pose替代路径。
- [x] 9.11 让Pose Watch复制节点完成后的真实Pose与空间。
- [x] 9.12 让FootPlacement Pose Watch显示已求解pelvis和左右腿结果。
- [x] 9.13 扩展Live Debug显示stage、space、completion与world capability。
- [x] 9.14 扩展Foot Placement Trace显示Planner与解析式solver结果。
- [x] 9.15 删除Action producer型Pose Preview UI和Final IK mutable-state诊断。

## 10. Corin迁移与正式发布

- [x] 10.1 在精确Corin Sampling Rig上下文修正剩余Calibration几何错误。
- [x] 10.2 将Corin Rig Definition迁移为v3 pelvis与左右腿chain。
- [x] 10.3 将Corin Foot Analysis Source绑定到同一Rig v3、Sampling Rig与Calibration。
- [x] 10.4 将Corin Sequence Pose Source补齐SyncRole和规范marker/curve数据。
- [x] 10.5 将Corin Pose Graph全部edge迁为Local或Component Pose。
- [x] 10.6 在Corin图中显式添加LocalToComponentPose。
- [x] 10.7 将ModifyBone、TwoBoneIK与FootPlacement置于Component Pose链。
- [x] 10.8 在Corin图中显式添加ComponentToLocalPose并连接OutputPose。
- [x] 10.9 从Corin与Gameplay Lab Prefab删除FootPlacementRig、Composition与Final IK组件。
- [x] 10.10 删除旧Rig v2、旧port schema和旧generated Presentation资产。
- [x] 10.11 通过正式Foot Analysis命令生成Corin受影响clip artifacts。
- [x] 10.12 通过显式Float32 Character Build发布新Projection与Native Pose Program。
- [x] 10.13 通过显式Fixed Character Build发布新Projection与Native Pose Program。
- [x] 10.14 对账Definition、Profile、Rig、Calibration、Artifact、Projection和Pose Program identity/revision。

## 11. 文档与规格收口

- [x] 11.1 更新`openspec/project.md`中的Pose runtime、Rig、Foot Placement与Preview真相。
- [x] 11.2 更新`btsmtl-agent-authoring` current contract的Pose空间、Rig v3与Marker owner规则。
- [x] 11.3 将活跃Foot Placement change未发布任务转记到本change并移除旧Final IK发布边界。
- [x] 11.4 删除仍描述图外FootPlacement、Rig v2、通用Pose port或Final IK solver的现行文档。
