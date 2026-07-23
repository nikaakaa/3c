# Tasks

## 1. 基线与删除清单

- [x] 1.1 枚举全部`PoseSlotId`声明、序列化字段、Projection payload与Runtime lookup。
- [x] 1.2 枚举全部固定per-slot Stack创建、Advance、Push、Evaluate、Reset与Dispose调用点。
- [x] 1.3 枚举`ResolvedAnimationPoseRequest`的selection、sample与transition混合字段。
- [x] 1.4 枚举Pose Graph当前节点、端口、compiler operation和native workspace。
- [x] 1.5 枚举Timeline、MM、Preview、Replay与Live Debug的Pose Request生产入口。
- [x] 1.6 枚举Foot Placement在Pose Graph外的创建、Present、Solver与final frame顺序。
- [x] 1.7 建立旧PoseSlot、隐藏Stack、旧Blend Library与旧request待删除清单。
- [x] 1.8 核对Corin旧Layer、PoseSlot、Blend、Rig与generated Projection迁移范围。

## 2. Animation Selection合同

- [x] 2.1 定义`AnimationSelectionFrame`schema与version。
- [x] 2.2 定义AnimationChannel、producer、source和generation identity字段。
- [x] 2.3 定义raw visual sample time、continuous time、cycle、loop与play rate字段。
- [x] 2.4 定义source-local clip sample descriptor。
- [x] 2.5 定义Presentation Parameter page identity与只读布局。
- [x] 2.6 禁止Selection携带transition、entry、Bone Mask、IK或最终weight。
- [x] 2.7 定义Selection的RequireSelection与AllowEmpty availability。
- [x] 2.8 定义同source连续sample与新generation的identity规则。
- [x] 2.9 定义Selection batch completion与frame lease。
- [x] 2.9.1 让请求工作区在`BeginFrame`回收上一帧全部source row占用。
- [x] 2.9.2 让上一帧row lease在新completion开始时立即失效。
- [x] 2.10 删除`ResolvedAnimationPoseRequest`混合合同。

## 3. Pose Graph typed端口与输入

- [x] 3.1 新增`AnimationSelection`端口类型。
- [x] 3.2 新增typed Program Parameter端口类型。
- [x] 3.3 定义`AnimationSelectionInput`节点。
- [x] 3.4 让Selection Input显式绑定AnimationChannelId。
- [x] 3.5 定义`MotionMatchingSelectionInput`节点。
- [x] 3.6 定义Selection frame cache与fan-out规则。
- [x] 3.7 定义`ProgramParameterInput`节点。
- [x] 3.8 校验ParameterId、类型、默认值与Projection binding。
- [x] 3.9 禁止Pose Graph读取State、Action、Blackboard或Timeline authoring object。
- [x] 3.10 删除PoseSlotInput固定读取隐藏Stack的节点语义。
- [x] 3.11 允许Selection edge显式经过零个或一个MarkerSync节点。
- [x] 3.12 拒绝同一路Selection串联两个MarkerSync节点。

## 4. Selected Pose Player

- [x] 4.1 定义`SelectedPosePlayer`authoring节点。
- [x] 4.2 定义Player runtime operation与workspace。
- [x] 4.3 让Player消费Selection并输出普通Pose Value。
- [x] 4.4 让Player复用唯一Animancer source backend。
- [x] 4.5 让同source连续sample只更新时间。
- [x] 4.6 让Selection identity变化执行明确硬切。
- [x] 4.7 让AllowEmpty输出NoPose。
- [x] 4.8 让RequireSelection缺失时失败。
- [x] 4.9 禁止Player自动fade或创建隐藏Stack。
- [x] 4.10 接入Player diagnostics与continuity。
- [x] 4.11 让Player先发布本帧Sample、HandoffReference与Release usage再采样source。
- [x] 4.12 让Player在连接MarkerSync时只消费该节点的effective sample page。
- [x] 4.13 让Player未连接MarkerSync时只消费Selection raw visual time。

## 5. 显式Blend Stack节点

- [x] 5.1 将现有Blend Stack算法封装为Pose Graph runtime node实例。
- [x] 5.2 让节点消费Animation Selection而不是PoseSlot request。
- [x] 5.3 让节点输出普通Pose Value。
- [x] 5.4 迁移active entry、push order与独立clock。
- [x] 5.5 迁移CrossFade并删除Stack内Inertial technique。
- [x] 5.5.1 删除Primitive contribution validator对旧`Inertial` kind的残留引用。
- [x] 5.5.2 让Slot Blend执行统一使用`AnimationPoseMath`，删除旧BlendStack私有数学类型引用。
- [x] 5.6 迁移Stored Pose与容量压缩。
- [x] 5.6.1 让BlendStack的live-to-Empty过渡强制捕获上一已完成输出为Stored Pose。
- [x] 5.7 迁移Per-Bone Blend Profile。
- [x] 5.8 迁移source retention与exact release。
- [x] 5.8.1 让live-to-Empty捕获完成后exact release全部旧source，不再要求当前帧采样。
- [x] 5.9 让每个节点拥有独立workspace与diagnostics identity。
- [x] 5.10 禁止节点读取Gameplay winner或下游Pose拓扑。
- [x] 5.11 让BlendStack发布当前与Retained source usage但不解析marker time。
- [x] 5.12 禁止BlendStack扫描或修改MarkerSync relation。

## 6. Node-local Blend Policy

- [x] 6.1 定义`CharacterAnimationBlendPolicy`identity与schema。
- [x] 6.2 保存MaxActiveSources与Stored Pose policy。
- [x] 6.3 保存canonical curves与Blend Profile引用。
- [x] 6.4 保存authoring default rule。
- [x] 6.5 保存exact source-target override。
- [x] 6.6 让Blend Stack节点唯一引用Blend Policy。
- [x] 6.7 枚举节点可达Selection endpoints。
- [x] 6.8 为节点物化完整exact transition table。
- [x] 6.9 拒绝duplicate、orphan与缺失pair。
- [x] 6.10 删除按PoseSlot保存的全局Blend Library payload。

## 7. Pose组合节点升级

- [x] 7.1 将现有Pose buffer统一为普通Pose Value布局。
- [x] 7.2 新增`BlendPose`标量混合节点。
- [x] 7.3 迁移`LayeredBoneBlend`。
- [x] 7.4 迁移`AdditivePose`。
- [x] 7.5 将`PoseCurveResolve`统一命名为`PoseParameterResolve`。
- [x] 7.6 让所有weight来自typed input或显式常量。
- [x] 7.7 保持source contribution按节点拓扑传播。
- [x] 7.7.1 让Layered与Additive组合后的Pose Value保留base分支的typed discontinuity。
- [x] 7.8 保持Foot Feature按最终骨骼贡献传播。
- [x] 7.9 保持公共子图frame cache只求值一次。
- [x] 7.10 删除PoseSlotFrame专属输入布局。

## 8. Bone Modify与Rig边界

- [x] 8.1 定义`ModifyBone`节点schema。
- [x] 8.2 让节点只引用稳定AnimationBoneId。
- [x] 8.3 定义Local与Mesh reference space。
- [x] 8.4 定义Position、Rotation与Scale有限操作。
- [x] 8.5 定义typed weight输入。
- [x] 8.6 编译dense Bone index与父节点依赖。
- [x] 8.7 拒绝未知BoneId、Rig revision与非有限值。
- [x] 8.8 禁止运行时名称、path或Humanoid查找。

## 9. Foot Placement与IK图阶段

- [x] 9.1 定义`FootPlacement`authoring节点。
- [x] 9.2 定义ComposedAnimationPoseFrame阶段输出。
- [x] 9.3 让Compiler把FootPlacement降低为world-aware Phase D。
- [x] 9.4 让节点显式引用Foot Placement Profile、Rig与Calibration。
- [x] 9.5 让节点消费typed FootPlacementWeight。
- [x] 9.6 复用唯一Planner与PhysicsScene query workspace。
- [x] 9.7 复用唯一IK Solver adapter。
- [x] 9.8 让OutputPose等待Solver exact completion。
- [x] 9.9 将FinalAnimationPoseFrame改为Solver完成后发布。
- [x] 9.10 删除图外自动追加Foot Placement Pass。
- [x] 9.11 无FootPlacement节点时不构造Planner或Solver。
- [x] 9.12 缺少正式world context时发布typed Unavailable而不伪造地面。

## 10. Pose Plan Compiler与Runtime

- [x] 10.1 升级Pose Graph authoring schema。
- [x] 10.2 升级Pose Program schema与runtime ABI。
- [x] 10.3 编译Selection、Parameter与Pose typed edges。
- [x] 10.4 编译Player与Blend Stack state layout。
- [x] 10.5 编译Player source membership与Marker time resolve阶段。
- [x] 10.6 编译native pose composition阶段。
- [x] 10.7 编译world-aware postprocess阶段。
- [x] 10.8 编译final publication阶段。
- [x] 10.9 校验cycle、dangling edge、非法fan-in与重复Output。
- [x] 10.10 校验每个stateful node拥有唯一runtime identity。
- [x] 10.11 按Projection容量预分配全部workspace。
- [x] 10.12 让Runtime只执行不可变CharacterPresentationPosePlan。
- [x] 10.13 禁止Runtime解释authoring asset或动态创建节点。

## 11. Timeline与Animation Lifecycle迁移

- [x] 11.1 让Program Finalize继续提交每AnimationChannel唯一winner。
- [x] 11.2 让Timeline sampling生成只含raw visual time的Animation Selection。
- [x] 11.3 将Projection marker binding identity写入Selection。
- [x] 11.4 删除Timeline写入Marker Sync effective time的路径。
- [x] 11.5 将Timeline registered curve写入Parameter page。
- [x] 11.6 从Timeline resolver删除transition identity查询。
- [x] 11.7 从Lifecycle删除固定PoseSlot Stack引用。
- [x] 11.8 让Player节点报告正式source retention demand。
- [x] 11.9 让Lifecycle按全部stateful Player需求保留source。
- [x] 11.10 删除按PoseSlot推送Empty的旧命令。
- [x] 11.11 保持logic release与presentation retention分离。
- [x] 11.12 让Rollback replacement重新产生Animation Selection identity。
- [x] 11.13 让SelectedPosePlayer与BlendStack分别处理重入语义。
- [x] 11.14 将表现插值调试从PoseSlotId迁移为PoseNodeId与Player source usage。
- [x] 11.15 删除Rollback Pipeline中的第二套CrossFade或动画时间轴假设。

## 12. Motion Matching迁移

- [x] 12.1 让MM Module输出Animation Selection batch。
- [x] 12.2 让MM Continue保持source identity并更新时间。
- [x] 12.3 让MM Jump提升SelectionGeneration。
- [x] 12.4 删除MM生成ResolvedAnimationPoseRequest。
- [x] 12.5 删除MM对固定PoseSlot Stack的直接引用。
- [x] 12.6 让MM Selection Input绑定正式producer output。
- [x] 12.7 让MM与BTSMTL Selection复用同一Player节点合同。
- [x] 12.8 让Pose History读取匹配节点的Composed Pose completion。
- [x] 12.9 保持MM无私有fade、player或retention。
- [x] 12.10 更新MM Replay与diagnostics selection identity。

## 13. Preview、Live Debug与Diagnostics

- [x] 13.1 让Timeline Preview生成正式Selection。
- [x] 13.2 让Preview编译并执行匹配Projection的Pose Plan。
- [x] 13.3 让MM Query Fixture进入正式Selection Input。
- [x] 13.4 删除Preview固定per-slot Stack装配。
- [x] 13.5 按PoseNodeId发布Player、Stack与Pose operation状态。
- [x] 13.6 发布Selection到Player的source map。
- [x] 13.7 分别发布Blend Stack entry/clock/Stored与Inertialization residual。
- [x] 13.8 发布MarkerSync raw/effective time、relation、leader、segment与fraction。
- [x] 13.9 发布Composed、PostProcess与Final completion。
- [x] 13.10 让Live Debug按图拓扑显示最终贡献。
- [x] 13.11 禁止Debug从Animancer weight重建第二份事实。

## 14. Corin正式资产迁移

- [x] 14.1 保持BaseLocomotion与FullBodyAction AnimationChannelId。
- [x] 14.2 创建两个AnimationSelectionInput节点。
- [x] 14.3 为BaseLocomotion创建唯一MarkerSync节点。
- [x] 14.4 将BaseLocomotion MarkerSync一对一连接SelectedPosePlayer。
- [x] 14.5 为BaseLocomotion创建SelectedPosePlayer与局部Inertialization节点。
- [x] 14.6 为FullBodyAction创建显式Blend Stack节点。
- [x] 14.7 创建Locomotion Inertialization Policy。
- [x] 14.8 创建Action Blend Policy。
- [x] 14.9 配置两个连续性节点的完整exact transition table。
- [x] 14.10 创建Base与Action LayeredBoneBlend。
- [x] 14.11 配置全身Action Mask与typed ActionWeight。
- [x] 14.12 迁移Pose Parameter Resolve。
- [x] 14.13 创建FootPlacement节点并绑定正式Profile/Rig/Calibration。
- [x] 14.14 连接唯一OutputPose。
- [x] 14.15 更新Animation Presentation Profile引用。
- [x] 14.16 更新Prefab Rig与IK Solver绑定。
- [x] 14.17 显式重建Projection与Float32/Fixed wrapper。

## 15. 激进删除与规格收口

- [x] 15.1 删除PoseSlotId类型与序列化字段。
- [x] 15.2 删除channel-to-PoseSlot binding。
- [x] 15.3 删除固定Stack数组与自动构造。
- [x] 15.4 删除PoseSlotFrame合同与workspace。
- [x] 15.5 删除旧CharacterAnimationBlendLibrary。
- [x] 15.6 删除旧ResolvedAnimationPoseRequest。
- [x] 15.7 删除Timeline transition identity传递。
- [x] 15.8 删除图外Foot Placement自动Pass。
- [x] 15.9 删除旧Projection schema与reader。
- [x] 15.10 删除FormerlySerializedAs、converter与fallback。
- [x] 15.11 同步更新全部相关active change文档。
- [x] 15.12 实施完成后更新openspec/project.md正式链路。
- [x] 15.13 对比current specs并删除旧隐藏Stack与图外IK口径。
- [x] 15.14 严格校验本change和全部OpenSpec文档。

## 16. 显式Marker Sync节点

- [x] 16.1 定义`MarkerSync` authoring node kind与schema version。
- [x] 16.2 定义节点稳定PoseNodeId与Selection输入输出端口。
- [x] 16.3 保持SyncGroup、Topology、SyncRole和Point Marker只属于AnimationTrack。
- [x] 16.4 让Compiler把节点可达producer marker binding编入Projection。
- [x] 16.5 校验节点输出精确连接一个stateful Player。
- [x] 16.6 拒绝MarkerSync输出fan-out到多个Player。
- [x] 16.7 拒绝MarkerSync串联MarkerSync。
- [x] 16.8 定义版本化`PlayerSourceUsageFrame`及Sample、HandoffReference、Release kind布局。
- [x] 16.9 定义版本化effective sample page布局。
- [x] 16.10 让SelectedPosePlayer在切换边界声明旧source HandoffReference与新source Sample。
- [x] 16.10.1 让SelectedPosePlayer在Marker映射后立即release旧source且不保留旧Pose。
- [x] 16.11 让BlendStack声明current、Retained与exact release usage。
- [x] 16.12 在source采样前解析默认outgoing leader。
- [x] 16.13 实现incoming AlwaysLeader方向反转。
- [x] 16.14 实现outgoing AlwaysFollower方向反转。
- [x] 16.15 实现同有向Marker pair与occurrence fraction映射。
- [x] 16.16 让None、不同组和无共同usage返回typed NotApplicable并保持raw time。
- [x] 16.17 让角色冲突、缺失segment与损坏Projection返回typed Invalid。
- [x] 16.18 在Player exact release时建立continuation anchor并detach relation。
- [x] 16.19 禁止MarkerSync读取blend weight、Stored Pose或per-bone contribution。
- [x] 16.20 禁止MarkerSync保留playable或延长source lifecycle。
- [x] 16.21 删除图外`AnimationMarkerSyncRuntime`自动装配入口。
- [x] 16.22 删除Lifecycle和Timeline对隐藏Stack entry的Marker扫描。
- [x] 16.23 删除Runtime normalized-time与名称推断路径。
- [x] 16.24 将Preview、Replay与Live Debug绑定到PoseNodeId和正式relation snapshot。

## 17. 双端帧同步动画链路收口

- [x] 17.1 从双端运行日志锁定首个动画异常与后续stale lease级联。
- [x] 17.2 区分异常前Run缺失和异常后动画停摆两条独立故障链。
- [x] 17.3 让BlendStack仅保留的有限Timeline在退出混合期间冻结末端Pose。
- [x] 17.4 让新表现帧在回收workspace前清除上一失败帧的frame-local Pose命令。
- [x] 17.5 为Presentation正式MCP入口增加完整producer source binding配置能力。
- [x] 17.6 让Idle、WalkStart、WalkLoop、RunStart、RunLoop、RunEnd与MovingTurn分别绑定正式Timeline source。
- [x] 17.7 让BaseLocomotion统一通过SelectedPosePlayer消费纯Timeline Selection。
- [x] 17.8 删除Corin临时BlendSpace资产、速度轴节点、参数声明和策略引用。
- [x] 17.9 重建并核对Projection中全部BaseLocomotion producer的Timeline映射。
- [x] 17.10 显式重建Projection、Float32与Fixed target产物。
- [x] 17.11 严格校验本change与OpenSpec文档。
- [x] 17.12 将每个Timeline producer唯一可达调用点的PlaybackMode编入Presentation Projection。
- [x] 17.13 拒绝同一Timeline producer同时被Once与Loop调用点消费。
- [x] 17.14 让Presentation采样只使用编译PlaybackMode判断Once与Loop。
- [x] 17.15 删除通过BlendSpace类型或Marker topology推断循环的表现路径。
- [x] 17.16 在SelectedPosePlayer完成Marker映射后、复用source workspace槽位前释放旧物理source。
- [x] 17.17 在新source采样前断开旧CapturePlayable，禁止两个CaptureJob写入同一复用槽位。
- [x] 17.18 重建并核对Projection、Float32与Fixed target中的PlaybackMode。

## 18. Terminal 与有效 Selection 收口

- [x] 18.1 将普通 Complete 与 Release 保存为可按 EventId 回滚的活跃 terminal。
- [x] 18.2 让 AllowEmpty 通道在当前 playback 终止后向 PoseGraph 提交正式 Empty。
- [x] 18.3 让 RequireSelection 通道在 terminal 后继续等待逻辑层下一位正式 winner。
- [x] 18.4 让 Player source usage、Marker sample 与选中判断统一消费 effective Selection。
- [x] 18.5 保留 raw Selection 与 sampling history，支持 terminal Retire 后恢复。
- [x] 18.6 保留同一 playback 的 Complete 与 Release 历史，避免部分回滚丢失较早 terminal。
- [x] 18.7 通过 Unity 脚本刷新确认编译无错误。
- [x] 18.8 通过 BTSMTL Agent Validator 校验 Corin 正式 CharacterController 资产。
- [x] 18.9 让 terminal 后仍被 Required channel 选中的有限 source 转为 Retained 并保持末帧。
- [x] 18.10 让 Camera 在 Body ResetSequence 变化时清除旧 Cinemachine tracking history。
- [x] 18.11 核对五条 Attack Timeline 的片段范围、重叠与 Ease 配置。
- [x] 18.12 对照 Agent v17 与 Document v1 change，拒绝用整体平移、SelfEase 或 YAML 修改伪造攻击片段 overlap。
- [x] 18.13 使用正式 Presentation 入口让 Projection、Float32和Fixed target追上当前Definition revision。
