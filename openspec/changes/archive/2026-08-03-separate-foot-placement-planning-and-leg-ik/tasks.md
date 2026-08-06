# Tasks

## 1. 固定GASP参考与当前错误闭包

- [x] 1.1 固定本地GASP版本、`FootPlacementMode`默认值和三种互斥模式。
- [x] 1.2 固定GASP默认AnimBP中Foot Placement与Leg IK的执行顺序。
- [x] 1.3 固定GASP Foot Placement读取的FK foot、IK foot、ball bone与foot speed曲线合同。
- [x] 1.4 固定GASP Animation Modifier生成IK foot轨迹与左右FootSpeed曲线的离线边界。
- [x] 1.5 固定GASP Control Rig只作为互斥替代方案而非默认叠加路径。
- [x] 1.6 枚举当前Corin FootPlacement作者节点、port、payload、operation和generated plan条目。
- [x] 1.7 枚举`CharacterFootPlacementPlan`到native control再到Limb Solver的全部字段映射。
- [x] 1.8 固定BendPlaneNormal被当作KneeDirection的精确调用点。
- [x] 1.9 固定Foot Placement Weight在Planner与staged executor重复应用的精确调用点。
- [x] 1.10 固定当前partial position blend改变腿长的精确数学路径。
- [x] 1.11 枚举Calibration完整geometry validator在Apply、Analyzer、Build与Runtime的真实调用点。
- [x] 1.12 对账现行spec与实际实现中复合节点、single-weight和Build validation的差异。

## 2. 定义typed双腿目标合同

- [x] 2.1 定义稳定`component.biped-leg-targets`Graph value type identity。
- [x] 2.2 定义targets的表现帧identity。
- [x] 2.3 定义targets的CompletionIdentity。
- [x] 2.4 定义targets的RigId与RigRevision。
- [x] 2.5 定义每脚Target Ankle Component Position。
- [x] 2.6 定义每脚Target Ankle Component Rotation。
- [x] 2.7 定义每脚Animated Bend Plane Normal。
- [x] 2.8 定义每脚Preferred Bend Plane Normal。
- [x] 2.9 定义每脚Bend Stabilization Weight。
- [x] 2.10 定义每脚Position Weight与Rotation Weight。
- [x] 2.11 定义每脚Leg Extension Ratio。
- [x] 2.12 定义每脚Constraint State与Decision Reason。
- [x] 2.13 定义targets有限性与归一化验证。
- [x] 2.14 定义targets reset、invalid与unavailable状态。
- [x] 2.15 定义targets只存在于同帧固定workspace的寿命。
- [x] 2.16 禁止targets进入Snapshot、Network、Gameplay State或Animator writer。
- [x] 2.17 删除旧`CharacterFootPlacementNativeControl`合同。
- [x] 2.18 删除旧Left/Right Bend Component Direction命名。

## 3. 扩展Graph Capability与作者节点

- [x] 3.1 为FootPlacement Capability增加typed targets输出。
- [x] 3.2 保持FootPlacement Component Pose输入与输出。
- [x] 3.3 保持FootPlacement唯一normalized Weight输入。
- [x] 3.4 将FootPlacement execution domain固定为WorldAwarePose。
- [x] 3.5 新增LegIK typed payload。
- [x] 3.6 为LegIK声明Component Pose输入。
- [x] 3.7 为LegIK声明typed targets输入。
- [x] 3.8 为LegIK声明Component Pose输出。
- [x] 3.9 将LegIK execution domain固定为PurePose。
- [x] 3.10 禁止LegIK声明第二Weight输入。
- [x] 3.11 将FootPlacement与LegIK加入唯一创建菜单和搜索目录。
- [x] 3.12 为targets port提供区别于Pose端口的稳定标签与颜色。
- [x] 3.13 让Details只显示FootPlacement的Profile、Calibration与Weight依赖。
- [x] 3.14 让LegIK Details只读显示Rig v3左右腿链与解析式solver身份。
- [x] 3.15 禁止LegIK Details显示Knee Direction、Pole Transform或默认轴字段。
- [x] 3.16 更新clipboard codec往返FootPlacement与LegIK节点及targets edge。
- [x] 3.17 更新Document v3 exporter输出新节点和typed edge。
- [x] 3.18 更新Document v3 strict parser接受新节点和typed edge。
- [x] 3.19 更新Reconciler通过唯一Presentation Mutation创建或修改新链。
- [x] 3.20 删除Document与UI中的复合solver隐式口径。

## 4. 建立Graph完整性与Rig验证

- [x] 4.1 拒绝targets连接非LegIK输入。
- [x] 4.2 拒绝LegIK targets来自非FootPlacement节点。
- [x] 4.3 拒绝LegIK Pose与targets来自不同FootPlacement call-site。
- [x] 4.4 拒绝targets跨subgraph边界而未显式声明同类型GraphInput/GraphOutput。
- [x] 4.5 拒绝targets跨RigId或RigRevision连接。
- [x] 4.6 拒绝同一targets输出扇出到多个LegIK实例。
- [x] 4.7 拒绝可达Output路径存在未消费FootPlacement targets。
- [x] 4.8 拒绝LegIK缺少Pose或targets任一输入。
- [x] 4.9 保持每个最终Output路径最多一个有状态FootPlacement实例。
- [x] 4.10 验证FootPlacement与LegIK使用同一Rig v3 pelvis及左右腿链。
- [x] 4.11 验证LegIK只写Rig v3声明的左右Physical chain。
- [x] 4.12 验证LegIK不写Virtual Bone或其它Physical Bone。
- [x] 4.13 验证FootPlacement与LegIK之间不存在ComponentToLocal转换。
- [x] 4.14 删除Compiler自动补建或隐藏调用LegIK的可能入口。

## 5. 扩展Pose IR、Projection与固定workspace

- [x] 5.1 为Pose IR增加LegIK operation code。
- [x] 5.2 为Pose IR增加typed targets value descriptor。
- [x] 5.3 为FootPlacement operation增加targets output value index。
- [x] 5.4 为LegIK operation增加targets input value index。
- [x] 5.5 编译FootPlacement为WorldAwarePose stage。
- [x] 5.6 编译LegIK为FootPlacement之后的PurePose stage。
- [x] 5.7 为targets分配固定Committed/Pending workspace页。
- [x] 5.8 让targets completion与同call-site FootPlacement Pose completion一致。
- [x] 5.9 让LegIK验证Pose与targets CompletionIdentity一致。
- [x] 5.10 让LegIK验证targets表现帧与当前frame一致。
- [x] 5.11 让LegIK验证targets Rig identity与Native Pose Program一致。
- [x] 5.12 让FootPlacement失败阻断LegIK和FinalPublication。
- [x] 5.13 让LegIK失败阻断后续stage和FinalPublication。
- [x] 5.14 保持每个source每帧最多capture一次。
- [x] 5.15 保持Animancer PlayableGraph每帧最多Evaluate一次。
- [x] 5.16 保持Physical Transform final writer每帧最多执行一次。
- [x] 5.17 更新Native Pose Program schema与content hash覆盖LegIK和targets layout。
- [x] 5.18 删除旧复合FootPlacement native operation payload。

## 6. 拆分Foot Placement运行阶段

- [x] 6.1 保持FootPlacement读取同帧最终Component Pose与Foot Features。
- [x] 6.2 保持FootPlacement读取唯一Foot Placement Weight。
- [x] 6.3 保持FootPlacement读取Body、PhysicsScene、Profile、Rig与Calibration。
- [x] 6.4 保持FootPlacement拥有Free、Locked与Sliding生命周期。
- [x] 6.5 保持FootPlacement拥有Current/Future Support与Ground Envelope。
- [x] 6.6 保持FootPlacement拥有footprint prediction与swing clearance。
- [x] 6.7 保持FootPlacement拥有surface anchor和moving surface rebuild。
- [x] 6.8 保持FootPlacement生成目标ankle位置与旋转。
- [x] 6.9 保持FootPlacement生成animated/preferred bend plane normal和独立稳定权重。
- [x] 6.10 保持FootPlacement生成pelvis component offset。
- [x] 6.11 让FootPlacement只把pelvis offset应用到其Component Pose输出。
- [x] 6.12 让FootPlacement把左右腿目标发布到typed targets workspace。
- [x] 6.13 删除FootPlacement stage内左右腿Physical chain求解。
- [x] 6.14 删除FootPlacement stage内对共享Limb Solver的直接调用。
- [x] 6.15 删除FootPlacement输出完成前的隐藏Transform写入可能路径。
- [x] 6.16 保持Reset原子清除constraint、prediction与pelvis历史。

## 7. 修正Leg IK解析式求解

- [x] 7.1 将solver输入字段统一命名为BendPlaneNormal。
- [x] 7.2 对最终animated/preferred normal混合结果执行有限归一化。
- [x] 7.3 将bend plane normal投影到目标腿轴正交平面。
- [x] 7.4 由目标腿轴与bend plane normal计算Knee Direction。
- [x] 7.5 删除把bend plane normal直接作为Knee Direction的路径。
- [x] 7.6 让退化plane normal返回typed solver failure。
- [x] 7.7 让position weight先混合动画Ankle与Target Ankle。
- [x] 7.8 按effective target执行完整两骨解析式求解。
- [x] 7.9 保持Upper Leg长度不变。
- [x] 7.10 保持Lower Leg长度不变。
- [x] 7.11 删除解算后分别Lerp Knee与Ankle Component Position的路径。
- [x] 7.12 让Ankle Rotation只读取rotation weight。
- [x] 7.13 让bend stabilization只读取Plan的bend weight。
- [x] 7.14 让position weight为零时保持输入腿链Pose。
- [x] 7.15 让rotation weight为零时保持输入Ankle Rotation。
- [x] 7.16 重建左右腿受影响Physical descendants。
- [x] 7.17 重算受影响Virtual Bone依赖。
- [x] 7.18 输出reach state、solve distance、fixed lengths与residual诊断。
- [x] 7.19 保持通用TwoBoneIK现有显式joint target语义独立。
- [x] 7.20 禁止LegIK query world或修改contact lifecycle。

## 8. 消除Foot Placement Weight重复应用

- [x] 8.1 固定唯一Weight owner为FootPlacement operation。
- [x] 8.2 保持Weight参与contact最低准入。
- [x] 8.3 保持Weight参与constraint position结果一次。
- [x] 8.4 保持Weight参与rotation结果一次。
- [x] 8.5 保持Weight参与free clearance结果一次。
- [x] 8.6 保持Weight参与pelvis结果一次。
- [x] 8.7 删除staged executor对Pelvis Offset的第二次nodeWeight乘法。
- [x] 8.8 删除staged executor对左右Position Weight的第二次nodeWeight乘法。
- [x] 8.9 删除staged executor对左右Rotation Weight的第二次nodeWeight乘法。
- [x] 8.10 删除LegIK中的Foot Placement Weight读取。
- [x] 8.11 保持Weight为零时FootPlacement发布无介入Pose与零targets weight。
- [x] 8.12 更新diagnostics显示唯一作者Weight与最终各通道weight。

## 9. 闭合Calibration与Artifact发布验证

- [x] 9.1 为Geometry Validation Result定义稳定identity与content hash。
- [x] 9.2 让Calibration Apply发布匹配当前Rig、Sampling Rig与Preview Pose的validation identity。
- [x] 9.3 让Foot Analyzer拒绝缺失或过期validation identity。
- [x] 9.4 让Foot Analysis artifact identity包含geometry validation identity。
- [x] 9.5 让Artifact payload保存已验证Calibration revision和geometry hash。
- [x] 9.6 让Definition Build拒绝Artifact validation identity不匹配。
- [x] 9.7 让Definition Build拒绝当前Calibration revision与Artifact不一致。
- [x] 9.8 让Projection发布Calibration geometry validation identity。
- [x] 9.9 让Runtime create核对Projection、Calibration、Rig与Artifact identity。
- [x] 9.10 保持Runtime不读取Sampling Rig、Preview Clip或Editor validator。
- [x] 9.11 删除仅以`Calibration.RequireValid()`代替几何发布验证的创建路径。
- [x] 9.12 保持非法Calibration明确阻止正式发布而不猜测默认axis。

## 10. 拆分Pose Watch、Live Debug与Scene诊断

- [x] 10.1 为FootPlacement Watch显示输入动画脚位置。
- [x] 10.2 为FootPlacement Watch显示应用Pelvis后的Component Pose。
- [x] 10.3 显示左右constraint state与transition reason。
- [x] 10.4 显示heel、toe、current support与future support。
- [x] 10.5 显示目标Ankle位置、旋转和surface normal。
- [x] 10.6 显示prediction、clearance与pelvis offset。
- [x] 10.7 显示FootPlacement最终position、rotation和bend weights。
- [x] 10.8 为LegIK Watch显示输入与输出Physical腿链。
- [x] 10.9 显示Bend Plane Normal。
- [x] 10.10 显示转换后的Knee Direction。
- [x] 10.11 显示Upper/Lower Leg固定长度。
- [x] 10.12 显示target、effective与solve distance。
- [x] 10.13 显示reach state、residual与typed failure。
- [x] 10.14 让Scene gizmo区分动画输入、FootPlacement目标与LegIK结果。
- [x] 10.15 保持Watch只复制同帧已完成workspace。
- [x] 10.16 禁止diagnostics重新query、重新求解或遍历Transform反推。
- [x] 10.17 保持无diagnostic interest时不复制大Pose或targets snapshot。

## 11. 迁移Corin正式资产与发布产物

- [x] 11.1 通过唯一Presentation Mutation在Corin Pose Graph创建LegIK节点。
- [x] 11.2 将Right Arm TwoBoneIK Component Pose连接到FootPlacement。
- [x] 11.3 将FootPlacement Component Pose输出连接到LegIK Pose输入。
- [x] 11.4 将FootPlacement targets输出连接到LegIK targets输入。
- [x] 11.5 将LegIK Component Pose输出连接到ComponentToLocalPose。
- [x] 11.6 删除旧FootPlacement到ComponentToLocalPose直连edge。
- [x] 11.7 删除Corin旧复合FootPlacement payload数据。
- [x] 11.8 对账Corin最终Output路径只有一个FootPlacement与一个LegIK。
- [x] 11.9 对账Corin左右手臂继续只使用通用TwoBoneIK。
- [x] 11.10 通过显式命令重建受影响Foot Analysis artifact。
- [x] 11.11 通过显式Character Build发布Presentation Projection。
- [x] 11.12 通过同一次显式Character Build发布Native Pose Program。
- [x] 11.13 对账生成Plan中FootPlacement stage先于LegIK stage。
- [x] 11.14 对账生成Plan不含旧复合FootPlacement operation。
- [x] 11.15 对账Runtime Prefab不新增LegIK MonoBehaviour或target Transform。

## 12. 激进删除旧路径与文档收口

- [x] 12.1 删除`CharacterFootPlacementNativeControl`及其workspace。
- [x] 12.2 删除旧Bend Component Direction字段和diagnostic标签。
- [x] 12.3 删除旧FootPlacement operation内嵌双腿solve分支。
- [x] 12.4 删除重复Foot Placement Weight乘法。
- [x] 12.5 删除旧复合FootPlacement Graph capability合同。
- [x] 12.6 删除任何自动插入隐藏LegIK的Compiler路径。
- [x] 12.7 搜索确认不存在第二Foot Placement planner。
- [x] 12.8 搜索确认不存在第二LegIK solver或Final IK adapter。
- [x] 12.9 搜索确认不存在运行时IK target Transform。
- [x] 12.10 搜索确认不存在可写IK控制骨兼容层。
- [x] 12.11 更新`openspec/project.md`最终Pose链与Foot Placement边界。
- [x] 12.12 更新current `character-foot-placement-presentation`。
- [x] 12.13 更新current `character-presentation-pose-graph`。
- [x] 12.14 更新current `character-animation-foot-analysis-artifact`。
- [x] 12.15 更新current `graph-authoring-domain-framework`。
- [x] 12.16 更新current `character-animation-pipeline`与`character-pipeline-runtime`。
- [x] 12.17 对账全部current spec不再要求复合FootPlacement原子调用solver。
- [x] 12.18 执行严格OpenSpec校验并修复全部错误。
