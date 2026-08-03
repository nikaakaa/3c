# Tasks

## 1. 实施基线与共同合同收口

- [x] 1.1 枚举当前`LayerId`在Timeline、Semantic IR、Program、command、Projection、Runtime、Preview、Trace和Agent Snapshot中的全部字段与构造入口。
- [x] 1.2 枚举`CharacterAnimationLayerDefinition`的全部authoring、serialized、Projection和Runtime调用方。
- [x] 1.3 枚举当前Animancer Layer、FadeGroup、TransitionLibrary和最终Pose写入调用方。
- [x] 1.4 枚举批准基线中尚未实施的Blend Stack提案所含per-layer Stack、global compositor和final Animator output职责。
- [x] 1.5 枚举Foot Placement读取Animancer、Layer scalar和visible contribution的全部入口。
- [x] 1.6 枚举BTSMTL Graph Editor中窗口、GraphView、搜索、clipboard、Undo、Inspector和diagnostics实现。
- [x] 1.7 枚举BTSMTL Graph Editor中BaseNode、BaseEdge、ConditionRule、BTAbortPolicy、InputAction与runtime context特判。
- [x] 1.8 核对`refactor-animation-playback-to-blend-stack`最终per-slot输出合同与本change一致。
- [x] 1.9 核对`refactor-presentation-projection-target-boundary`最终producer contract和Projection payload与本change一致。
- [x] 1.10 核对当前Corin全部AnimationTrack、producer、LayerId、Action/Locomotion ownership和Marker Group。
- [x] 1.11 建立待删除旧Layer数据、旧Animancer fade路径、旧snapshot字段和generated asset清单。
- [x] 1.12 标记并行active change已经修改且本change不得回退的Projection、Foot Analysis、Marker Sync和Presentation Runtime字段。

## 2. Animation Channel身份迁移

- [x] 2.1 定义稳定`AnimationChannelId`值类型及空值规范。
- [x] 2.2 定义AnimationChannel canonical序列化格式。
- [x] 2.3 将Timeline `AnimationTrack.LayerId`替换为`AnimationChannelId`。
- [x] 2.4 将AnimationTrack authoring validator切换到AnimationChannelId。
- [x] 2.5 将Timeline复制与粘贴保持channel identity引用而不生成新channel。
- [x] 2.6 将Semantic producer contract的LayerId替换为AnimationChannelId。
- [x] 2.7 将Semantic emitter输出切换到AnimationChannelId。
- [x] 2.8 将Float32 Program producer descriptor切换到AnimationChannelId。
- [x] 2.9 将Fixed Program producer descriptor切换到AnimationChannelId。
- [x] 2.10 将Program canonical codec与hash输入切换到AnimationChannelId。
- [x] 2.11 提升受影响Program ABI与operation-set identity。
- [x] 2.12 将Program Finalize按AnimationChannelId建立唯一selection。
- [x] 2.13 将Presentation command schema切换到AnimationChannelId。
- [x] 2.14 将command queue排序与冲突诊断切换到AnimationChannelId。
- [x] 2.15 将Playback Lifecycle key切换到AnimationChannelId。
- [x] 2.16 将Marker Sync同组约束切换为同AnimationChannelId。
- [x] 2.17 将Trace、Snapshot与diagnostic字段切换到AnimationChannelId。
- [x] 2.18 删除全部运行时代码中的LayerId类型、字段、转换和字符串标签。

## 3. Pose Slot与Profile合同

- [x] 3.1 定义稳定`PoseSlotId`值类型及空值规范。
- [x] 3.2 定义`PoseSlotOutputPolicy`的RequireOutput与AllowEmpty值。
- [x] 3.3 定义Pose Slot authoring declaration schema。
- [x] 3.4 让Pose Slot declaration显式保存AnimationChannelId binding。
- [x] 3.5 实现channel-to-slot一对一校验。
- [x] 3.6 实现可达Animation Channel完整slot coverage校验。
- [x] 3.7 实现一个Pose Slot最多一个channel校验。
- [x] 3.8 实现一个Pose Slot恰好一个根PoseSlotInput校验。
- [x] 3.9 将`CharacterAnimationPresentationProfile`增加唯一Pose Graph引用。
- [x] 3.10 保留Profile的Blend Library与Rig Definition正式引用。
- [x] 3.11 从Profile删除Layer catalog字段。
- [x] 3.12 从Profile删除Layer order、AvatarMask、BlendMode和Animancer layer index数据。
- [x] 3.13 从Profile Inspector删除旧Layer列表UI。
- [x] 3.14 在Profile Inspector增加Pose Graph、Blend Library与Rig identity摘要。
- [x] 3.15 在Profile Inspector增加显式Open Pose Graph入口。
- [x] 3.16 在Profile Inspector按producer identity显示AnimationChannelId和PoseSlotId。
- [x] 3.17 让Profile validation拒绝缺失Pose Graph、Blend Library或Rig。
- [x] 3.18 删除`CharacterAnimationLayerDefinition`类型与全部serialized使用点。

## 4. Graph Authoring Editor Shell抽取

- [x] 4.1 定义`IGraphAuthoringDocument`接口。
- [x] 4.2 定义`IGraphAuthoringNodeCatalog`接口。
- [x] 4.3 定义`IGraphAuthoringPortPolicy`接口。
- [x] 4.4 定义`IGraphAuthoringMutationAdapter`接口。
- [x] 4.5 定义`IGraphAuthoringInspectorAdapter`接口。
- [x] 4.6 定义`IGraphAuthoringDiagnosticsAdapter`接口。
- [x] 4.7 把窗口生命周期抽到`GraphAuthoringEditorShell`。
- [x] 4.8 把GraphView画布与selection协调抽到Shell。
- [x] 4.9 把节点搜索与创建菜单抽到Shell。
- [x] 4.10 把clipboard domain envelope抽到Shell。
- [x] 4.11 把复制、粘贴和identity remap协调抽到Shell。
- [x] 4.12 把Undo、dirty owner和serialized owner协调抽到Shell。
- [x] 4.13 把Inspector宿主抽到Shell。
- [x] 4.14 把breadcrumb与document navigation抽到Shell。
- [x] 4.15 把只读diagnostics overlay宿主抽到Shell。
- [x] 4.16 建立BTSMTL document adapter。
- [x] 4.17 建立BTSMTL node catalog adapter。
- [x] 4.18 建立BTSMTL port policy adapter。
- [x] 4.19 建立BTSMTL mutation与Inspector adapter。
- [x] 4.20 建立BTSMTL diagnostics adapter。
- [x] 4.21 将现有BaseTree资产打开入口迁移到Shell。
- [x] 4.22 删除Shell已接管的旧window/view/clipboard/Undo重复实现。
- [x] 4.23 拒绝跨Graph domain clipboard粘贴。
- [x] 4.24 删除Shell中的BaseNode subtype、ConditionRule和InputAction特判。

## 5. Pose Graph authoring数据

- [x] 5.1 定义`CharacterPresentationPoseGraphAsset`。
- [x] 5.2 定义普通CSharp `CharacterPoseGraphData`。
- [x] 5.3 定义稳定PoseNodeId。
- [x] 5.4 定义稳定PosePortId。
- [x] 5.5 定义PoseEdge schema。
- [x] 5.6 定义Pose graph content revision。
- [x] 5.7 定义Pose node base authoring合同且不包含runtime evaluation字段。
- [x] 5.8 定义PoseSlotInput authoring node。
- [x] 5.9 定义LayeredBoneBlend authoring node。
- [x] 5.10 定义AdditivePose authoring node。
- [x] 5.11 定义PoseCurveResolve authoring node。
- [x] 5.12 定义PoseSubgraph authoring node与typed接口。
- [x] 5.12.1 定义独立稳定`PoseInterfacePortId`且与node-local `PosePortId`分离。
- [x] 5.12.2 定义compiler-only `GraphInput`与`GraphOutput` authoring node kind。
- [x] 5.12.3 让PoseSubgraph调用点本地端口显式保存`InterfacePortId` binding。
- [x] 5.13 定义OutputPose authoring node。
- [x] 5.14 定义owner-private inline PoseSubgraph reference。
- [x] 5.15 定义显式shared PoseGraph asset reference。
- [x] 5.16 实现inline/shared互斥所有权。
- [x] 5.17 实现Create Inline、Extract Shared与Clear Shared正式mutation。
- [x] 5.18 实现Pose Graph stable identity生成与复制remap。
- [x] 5.19 实现Pose Graph asset直接打开入口。
- [x] 5.20 建立Pose Graph document、node catalog、port policy、mutation、Inspector与diagnostics adapters。

## 6. Pose Parameter与Mask authoring

- [x] 6.1 定义稳定`PoseParameterId`。
- [x] 6.2 定义有限标量Pose Parameter declaration。
- [x] 6.3 让每个Pose Parameter declaration保存显式default。
- [x] 6.4 定义`Base` resolve policy。
- [x] 6.5 定义`Overlay` resolve policy。
- [x] 6.6 定义`Weighted` resolve policy。
- [x] 6.7 定义`Max` resolve policy。
- [x] 6.8 定义`Min` resolve policy。
- [x] 6.9 让LayeredBoneBlend保存每参数完整policy。
- [x] 6.10 让AdditivePose保存每参数完整policy。
- [x] 6.11 让PoseCurveResolve保存显式source与policy。
- [x] 6.12 定义Pose Graph Bone Mask authoring引用。
- [x] 6.13 让Bone Mask只使用稳定BoneId。
- [x] 6.14 定义Additive reference pose identity。
- [x] 6.14.1 安装唯一公开`AnimationAdditiveReferencePoseIds.RigReference` identity且不建立Reference catalog或fallback。
- [x] 6.14.2 让AdditivePose Node字段默认与构造默认都使用Rig Reference identity。
- [x] 6.15 定义Additive reference space与scale policy。
- [x] 6.16 在Inspector显示Mask对应Rig identity与revision。
- [x] 6.17 在Inspector显示每个Pose Parameter缺失policy状态。
- [x] 6.18 删除Runtime字符串曲线查找与同名覆盖路径。

## 7. Pose Graph Validator

- [x] 7.1 建立唯一`CharacterPresentationPoseGraphValidator`。
- [x] 7.2 校验node identity非空且唯一。
- [x] 7.3 校验port identity非空且在node内唯一。
- [x] 7.4 校验edge端点存在且domain正确。
- [x] 7.5 校验typed port兼容。
- [x] 7.6 校验非法fan-in。
- [x] 7.7 校验dangling required input。
- [x] 7.8 校验graph cycle并保留完整source chain。
- [x] 7.9 校验根图恰好一个OutputPose。
- [x] 7.10 校验每个Pose Slot恰好一个PoseSlotInput。
- [x] 7.11 校验所有Pose Slot被最终Output可达路径消费。
- [x] 7.12 校验RequireOutput slot完整性。
- [x] 7.13 校验AllowEmpty传播到Output的合法性。
- [x] 7.14 校验Mask Rig identity和BoneId coverage。
- [x] 7.15 校验Additive source/reference兼容。
- [x] 7.15.1 让Validator精确拒绝Rig Reference之外的任意Additive reference字符串。
- [x] 7.16 校验全部可达PoseParameter policy coverage。
- [x] 7.16.1 让PoseCurveResolve authoring合同要求两个有序Pose输入和一个Pose输出。
- [x] 7.17 校验PoseSubgraph typed接口。
- [x] 7.17.1 校验根图禁止边界节点且恰好一个OutputPose，子图恰好一个GraphInput/GraphOutput且禁止OutputPose。
- [x] 7.17.2 校验接口identity唯一、调用点完整一对一coverage、kind/direction/required一致及required边界悬空。
- [x] 7.18 校验inline/shared互斥与inline/shared递归cycle。
- [x] 7.19 拒绝BTSMTL节点、Blackboard端口和Unity Object runtime字段。
- [x] 7.20 让Profile、Projection Compiler与Editor Diagnostics复用同一Validator。

## 8. Pose Graph Compiler与Program

- [x] 8.1 定义`CharacterPresentationPoseProgram` schema version。
- [x] 8.1.1 将Pose Program schema与runtime ABI激进提升为v2并拒绝v1。
- [x] 8.2 定义Pose operation code enum。
- [x] 8.3 定义版本化Pose operation payload。
- [x] 8.3.1 将Pose operation payload提升为v2并让PoseCurveResolve layout精确要求Input A与Input B。
- [x] 8.4 定义stable slot index table。
- [x] 8.5 定义channel-to-slot compiled binding table。
- [x] 8.6 定义dense Bone Mask table。
- [x] 8.7 定义Additive reference descriptor table。
- [x] 8.7.1 从Rig父节点优先ReferenceLocal TRS编译唯一Rig Reference的Local或Mesh dense descriptor。
- [x] 8.8 定义Pose Parameter index与default table。
- [x] 8.9 定义每operation Parameter policy table。
- [x] 8.10 定义pose value workspace layout。
- [x] 8.11 定义parameter workspace layout。
- [x] 8.12 定义source contribution workspace layout。
- [x] 8.13 定义operation source map。
- [x] 8.14 建立Editor-only`CharacterPresentationPoseGraphCompiler`。
- [x] 8.15 生成稳定topological operation顺序。
- [x] 8.16 静态展开inline PoseSubgraph。
- [x] 8.16.1 将外部输入edge重接GraphInput内部消费者并将GraphOutput内部source重接外部消费者。
- [x] 8.17 静态展开shared PoseSubgraph call site。
- [x] 8.17.1 为递归展开的内部node/port生成call-site-scoped稳定identity与完整source-map call chain。
- [x] 8.17.2 保证PoseSubgraph、GraphInput、GraphOutput不进入Runtime Program或动态dispatch。
- [x] 8.18 生成公共子图frame cache与liveness layout。
- [x] 8.18.1 要求FrameCacheCount精确等于Operations数量并让operation index成为唯一frame-cache index。
- [x] 8.19 将BoneId与Mask展开为Rig dense index。
- [x] 8.20 将PoseParameterId展开为dense index。
- [x] 8.20.1 将PoseCurveResolve的Base Pose与Parameter Source Pose依序降低为operation Input A与Input B并纳入canonical hash。
- [x] 8.21 生成唯一Output operation index。
- [x] 8.22 计算Pose Program canonical hash与content revision。
- [x] 8.23 拒绝未知operation、port和payload version。
- [x] 8.24 删除Runtime authoring graph解释入口。

## 9. Projection与Build链迁移

- [x] 9.1 将Presentation Semantic Contract producer字段改为AnimationChannelId。
- [x] 9.2 更新Frontend Contract builder canonical hash输入。
- [x] 9.3 更新Float32 Presentation Contract Adapter。
- [x] 9.4 更新Fixed Presentation Contract Adapter。
- [x] 9.5 更新Remote Presentation semantic manifest adapter。
- [x] 9.6 让Projection compile request接收Pose Graph、Blend Library与Rig Definition。
- [x] 9.7 编译channel-to-PoseSlot binding payload。
- [x] 9.8 编译Pose Slot output policy payload。
- [x] 9.9 嵌入per-slot Stack policy与transition matrix。
- [x] 9.10 嵌入dense Rig与Mask payload。
- [x] 9.11 嵌入CharacterPresentationPoseProgram。
- [x] 9.12 将Pose Graph dependency纳入ProjectionRevision。
- [x] 9.13 将Blend Library dependency纳入ProjectionRevision。
- [x] 9.14 将Rig/Mask/Parameter dependency纳入ProjectionRevision。
- [x] 9.15 保持纯Presentation变化不改变Numeric ProgramHash。
- [x] 9.16 让Projection payload validation校验slot/stack/pose program cross-reference。
- [x] 9.17 更新Projection Asset Inspector identity摘要。
- [x] 9.18 提升Projection schema并拒绝旧Layer payload。

## 10. Blend Stack职责收窄

- [x] 10.1 将`AnimationBlendStackRuntime` owner从LayerId切换为PoseSlotId。
- [x] 10.2 将每Layer Stack policy切换为每Pose Slot policy。
- [x] 10.3 将transition matrix key约束为同AnimationChannelId/PoseSlotId producer。
- [x] 10.4 定义不可变`PoseSlotFrame`合同。
- [x] 10.5 让PoseSlotFrame保存availability和slot output weight。
- [x] 10.6 让PoseSlotFrame保存dense local pose。
- [x] 10.7 让PoseSlotFrame保存Pose Parameter buffer。
- [x] 10.8 让Player输出保存live/Stored contribution，并让局部Inertialization输出普通Pose contribution。
- [x] 10.9 让PoseSlotFrame保存continuity identity。
- [x] 10.10 将旧`AnimationBlendPoseEvaluator`拆为`AnimationSlotBlendJob`与独立Pose Graph Job。
- [x] 10.11 保留per-slot独立clock、curve和Per-Bone transition。
- [x] 10.12 保留显式BlendStack容量与Stored Pose，并把Accumulator唯一迁入局部Inertialization节点。
- [x] 10.13 保留per-slotMarker relation detach和source retirement。
- [x] 10.14 从Stack evaluator删除跨slotMask composition。
- [x] 10.15 从Stack evaluator删除global Layer order。
- [x] 10.16 从Stack evaluator删除跨slotOverride/Additive。
- [x] 10.17 从Stack evaluator删除最终Animator AnimationStream写回。
- [x] 10.18 让Stack只向PoseSlotInput发布完成frame。

## 11. Character Pose Graph Runtime

- [x] 11.1 定义`CharacterPoseGraphNativeJob`创建合同。
- [x] 11.2 定义Pose Program ABI校验。
- [x] 11.3 定义fixed pose workspace。
- [x] 11.4 定义fixed parameter workspace。
- [x] 11.5 定义fixed contribution workspace。
- [x] 11.6 定义frame cache与completion identity。
- [x] 11.7 绑定全部PoseSlotFrame输入index。
- [x] 11.8 实现PoseSlotInput operation。
- [x] 11.9 实现LayeredBoneBlend TRS operation。
- [x] 11.10 实现LayeredBoneBlend source contribution传播。
- [x] 11.11 实现LayeredBoneBlend Pose Parameter解析。
- [x] 11.12 实现Additive position规则。
- [x] 11.13 实现Additive shortest-arc rotation规则。
- [x] 11.14 实现Additive scale policy。
- [x] 11.14.1 实现Mesh reference delta到local pose的Rig parent-index转换。
- [x] 11.15 实现Additive source contribution传播。
- [x] 11.16 实现PoseCurveResolve operation。
- [x] 11.16.1 实现PoseCurveResolve保持Base骨骼/贡献/foot、只解析Parameter Source且区分NoPose与Invalid的完整runtime语义。
- [x] 11.17 实现OutputPose operation。
- [x] 11.18 实现AllowEmpty NoPose传播。
- [x] 11.19 实现RequireOutput invalid状态。
- [x] 11.20 实现非有限pose/parameter拒绝。
- [x] 11.21 将最终Evaluator接入唯一PlayableGraph/Animation Job拓扑。
- [x] 11.22 让最终Job唯一写回Animator AnimationStream。
- [x] 11.23 禁止表现帧动态扩容和逐骨骼Transform写入。
- [x] 11.24 定义`FinalAnimationPoseFrame`发布合同。

## 12. Animancer source后端与Presentation编排

- [x] 12.1 保持Animancer source visual按完整AnimationPoseSourceId隔离。
- [x] 12.2 保持Timeline source state Speed为零。
- [x] 12.3 保持producer内部ManualMixer child使用DontSynchronize。
- [x] 12.4 删除AnimancerLayer.Play调用。
- [x] 12.5 删除Animancer StartFade与FadeGroup调用。
- [x] 12.6 删除Animancer layer weight写入。
- [x] 12.7 删除Animancer transition lookup。
- [x] 12.8 让Animancer backend只输出source pose sample和playable寿命。
- [x] 12.9 更新CharacterAnimationPlaybackRuntime创建顺序。
- [x] 12.10 更新PresentationFrame command消费顺序。
- [x] 12.11 同帧推进全部AnimationChannel Lifecycle。
- [x] 12.12 同帧生成全部PoseSlot Stack frame plan。
- [x] 12.13 在全部slot完成后只执行一次Pose Graph。
- [x] 12.14 在Pose Graph完成后只执行一次Pose Post Process。
- [x] 12.15 在Pose Post Process完成后推进Camera。
- [x] 12.16 更新Reset、Dispose与job completion顺序。

## 13. Foot Placement输入迁移

- [x] 13.1 将Foot Placement pose输入切换为FinalAnimationPoseFrame。
- [x] 13.2 将LeftFoot actual contribution切换为Pose Graph最终输出。
- [x] 13.3 将RightFoot actual contribution切换为Pose Graph最终输出。
- [x] 13.4 传播Base slot live source Foot Analysis。
- [x] 13.5 传播Action slot live source Foot Analysis。
- [x] 13.6 传播Stored Pose每脚feature aggregate。
- [x] 13.7 由局部Inertialization按脚Bone envelope传播每脚feature transition。
- [x] 13.8 按最终dense Bone Mask组合每脚feature贡献。
- [x] 13.9 让零脚mask overlay不稀释Base feature。
- [x] 13.10 让全身overlay按实际脚贡献替换Base feature。
- [x] 13.11 将final PoseParameter映射到Foot Placement正式输入。
- [x] 13.12 删除Animancer state weight读取。
- [x] 13.13 删除单一Layer/slot scalar替代每脚贡献路径。
- [x] 13.14 让Pose Graph Invalid触发Foot Placement正式reset。
- [x] 13.15 保持Foot Placement固定在Pose Graph之后且不成为Graph节点。

## 14. Preview、Trace与Editor诊断

- [x] 14.1 将Timeline Preview command key切换到AnimationChannelId。
- [x] 14.2 让Preview通过Projection解析PoseSlotId。
- [x] 14.3 让Preview复用正式per-slot Blend Stack。
- [x] 14.4 让Preview复用正式Pose Program和Evaluator。
- [x] 14.5 拒绝Preview同channel多个producer。
- [x] 14.6 更新Preview非连续seek的channel/slot reset。
- [x] 14.7 扩展Animation snapshot保存AnimationChannelId和PoseSlotId。
- [x] 14.8 扩展snapshot分别保存Stack entry/Stored与局部Inertialization。
- [x] 14.9 扩展snapshot保存PoseNodeId与operation availability。
- [x] 14.10 扩展snapshot保存Pose Parameter最终值。
- [x] 14.11 扩展snapshot保存per-bone/per-foot final contribution。
- [x] 14.12 扩展snapshot保存OutputPose completion identity。
- [x] 14.13 将Timeline Live Debug Marker relation限制为同channel/slot。
- [x] 14.14 把Pose diagnostics的Live入口从authoring validation-only状态切换到正式runtime snapshot读取。
- [x] 14.14.1 让Pose diagnostics取得当前显式runtime diagnostics target与最新AnimationPlaybackFrameSnapshot。
- [x] 14.14.2 校验snapshot的PoseGraph identity、ProjectionRevision与当前打开文档精确匹配。
- [x] 14.14.3 通过compiled Pose Program source map把operation snapshot映射到稳定PoseNodeId。
- [x] 14.14.4 只读显示node availability、final contribution与OutputPose completion identity。
- [x] 14.14.5 在缺少target、缺少snapshot或ProjectionRevision不匹配时分别显示Unavailable或Stale。
- [x] 14.14.6 禁止Pose diagnostics临时编译Program、运行Pose Evaluator或读取authoring默认值冒充Live结果。
- [x] 14.15 删除按Animancer state重建fade或最终贡献的debug路径。
- [x] 14.16 删除旧LayerId、Current/Outgoing和Animancer fade snapshot字段。

## 15. Corin正式资产迁移（唯一实施归属）

- [x] 15.1 在Corin authoring catalog创建`BaseLocomotion` AnimationChannelId。
- [x] 15.2 在Corin authoring catalog创建`FullBodyAction` AnimationChannelId。
- [x] 15.3 将Idle producer迁移到BaseLocomotion。
- [x] 15.4 将WalkStart producer迁移到BaseLocomotion。
- [x] 15.5 将WalkLoop producer迁移到BaseLocomotion。
- [x] 15.6 将RunStart producer迁移到BaseLocomotion。
- [x] 15.7 将RunLoop producer迁移到BaseLocomotion。
- [x] 15.8 将RunEnd producer迁移到BaseLocomotion。
- [x] 15.9 将MovingTurn producer迁移到BaseLocomotion。
- [x] 15.10 将Attack1 producer迁移到FullBodyAction。
- [x] 15.11 将Attack2 producer迁移到FullBodyAction。
- [x] 15.12 将Attack3 producer迁移到FullBodyAction。
- [x] 15.13 将Attack4 producer迁移到FullBodyAction。
- [x] 15.14 将Attack5 producer迁移到FullBodyAction。
- [x] 15.15 将DodgeBack producer迁移到FullBodyAction。
- [x] 15.16 将DodgeForward producer迁移到FullBodyAction。
- [x] 15.17 枚举并迁移其它明确全身Action producer到FullBodyAction。
- [x] 15.18 保持WalkEnd无producer且不创建Timeline、默认Idle或其它fallback。
- [x] 15.19 让Locomotion与Action所有权分别只写各自AnimationChannel。
- [x] 15.20 让Program Finalize能在同一Tick同时提交BaseLocomotion与FullBodyAction最终selection。
- [x] 15.21 重新导出Corin Snapshot并确认全部可达AnimationTrack只使用两个正式AnimationChannelId。
- [x] 15.22 创建Corin Pose Graph asset并建立稳定PoseGraph identity。
- [x] 15.23 创建绑定BaseLocomotion/RequireSelection的唯一AnimationSelectionInput节点。
- [x] 15.24 创建绑定FullBodyAction/AllowEmpty的唯一AnimationSelectionInput节点。
- [x] 15.25 为BaseLocomotion创建唯一MarkerSync节点。
- [x] 15.25.1 将BaseLocomotion MarkerSync一对一连接唯一SelectedPosePlayer节点。
- [x] 15.26 为BaseLocomotion创建SelectedPosePlayer后的唯一局部Inertialization节点。
- [x] 15.26.1 为FullBodyAction创建唯一显式BlendStack节点。
- [x] 15.27 创建覆盖全身的稳定Rig Bone Mask。
- [x] 15.28 创建以Base Inertialization输出为Base、FullBodyAction BlendStack输出为Overlay的LayeredBoneBlend节点。
- [x] 15.29 创建根图唯一OutputPose节点并连接最终Pose路径。
- [x] 15.30 为全部已声明PoseParameter配置LayeredBoneBlend完整resolve policy。
- [x] 15.31 校验RequireOutput、AllowEmpty与全部合法availability组合。
- [x] 15.31.1 创建Corin CharacterAnimationRigDefinition并建立稳定Rig identity。
- [x] 15.31.2 配置Corin父节点优先dense BoneId与ParentIndex。
- [x] 15.31.3 配置Corin root exclusion、scale policy与左右脚语义BoneId。
- [x] 15.31.4 创建Corin CharacterAnimationBlendProfile资产。
- [x] 15.31.5 配置每个Blend Profile的Rig identity、global duration multiplier与BoneId override。
- [x] 15.32 创建Corin BaseLocomotion节点唯一CharacterPoseInertializationPolicy。
- [x] 15.33 创建Corin FullBodyAction BlendStack节点唯一CharacterAnimationBlendPolicy与容量。
- [x] 15.34 为BaseLocomotion全部可达discontinuity endpoint配置完整Inertialization exact table。
- [x] 15.35 为FullBodyAction全部可达source pair及source-to-Empty配置完整CrossFade exact table。
- [x] 15.36 更新Corin Profile引用唯一Pose Graph。
- [x] 15.37 更新Corin Profile引用Pose Graph节点所需唯一Blend与Inertialization Policy。
- [x] 15.38 更新Corin Profile引用唯一Rig Definition。
- [x] 15.39 为全部BaseLocomotion producer配置稳定source resource binding。
- [x] 15.40 为全部FullBodyAction producer配置稳定source resource binding。
- [x] 15.41 保持Corin Foot Analysis Mode与Analysis Source GUID指向正式输入。
- [x] 15.42 更新Corin Prefab CharacterAnimationRigBinding的dense Transform绑定。
- [x] 15.42.1 接入Corin唯一final output job装配。
- [x] 15.43 删除Corin RootTree与shared Timeline中的旧`LayerId` serialized payload。
- [x] 15.44 删除Corin Profile中的旧Layer catalog、Animancer layer index与TransitionLibrary serialized payload。
- [x] 15.44.1 将Corin producer binding重写为正式SourceKind与Source字段并删除旧m_Transition、m_Easing序列化数据。
- [x] 15.45 通过显式Build请求发布Corin target-neutral Projection。
- [x] 15.46 通过显式Build请求发布Corin Float32 Program artifact与wrapper。
- [x] 15.47 通过显式Build请求发布Corin Fixed Program artifact与wrapper。
- [x] 15.48 让新generated Projection只保存AnimationChannel、SelectionInput、PoseNode、node-local Policy、Rig与Pose Plan payload。
- [x] 15.49 删除旧generated Projection中的Layer、TransitionLibrary与Animancer layer payload。

## 16. 旧路径删除与规格统一

- [x] 16.1 删除主代码中的旧LayerId字段、类型与reader；Corin资产serialized残留由15.43清理。
- [x] 16.2 删除Profile代码合同与Inspector中的旧Layer catalog；Corin Profile serialized残留由15.44清理。
- [x] 16.3 删除旧CharacterAnimationLayerDefinition资产和引用。
- [x] 16.4 删除旧Animancer TransitionLibrary正式引用。
- [x] 16.5 删除旧Animancer layer index与AvatarMask runtime路径。
- [x] 16.6 删除旧global Blend Stack Layer compositor。
- [x] 16.7 删除旧Lifecycle Current/Outgoing并行weight事实。
- [x] 16.8 删除Projection代码schema中的旧Layer payload与revision token；Corin旧generated artifact由15.49清理。
- [x] 16.9 删除旧Preview简化播放链。
- [x] 16.10 删除旧Trace LayerId与Animancer fade字段。
- [x] 16.11 删除FormerlySerializedAs、兼容converter和fallback配置。
- [x] 16.12 更新`openspec/project.md`的动画模块职责。
- [x] 16.13 更新`openspec/project.md`的Profile、Projection与Editor代码组织。
- [x] 16.14 更新`refactor-animation-playback-to-blend-stack`的proposal、design、tasks和spec deltas。
- [x] 16.15 明确`refactor-presentation-projection-target-boundary`已经归档且只作历史追溯，不回改其proposal、design、tasks或spec delta。
- [x] 16.16 清理current specs中旧LayerId、Animancer fade/TransitionLibrary权威、旧preview lifecycle和Equipment Required Layer口径。
- [x] 16.16.1 更新`character-animation-presentation-authoring`的Blend Library、Profile bootstrap与Equipment边界。
- [x] 16.16.2 更新`btsmtl-timeline-editor-preview`的session、Marker Sync preview与Live Debug术语。
- [x] 16.16.3 更新`character-pipeline-runtime`的PresentationFrame时钟与完整动画合成结果。
- [x] 16.16.4 更新`character-presentation-interpolation`的source sampling、Stack/Pose Graph时钟与diagnostics术语。
- [x] 16.16.5 更新`character-equipment-presentation`，删除Feature Required Layer与旧Animation Profile Layer所有权。
- [x] 16.16.6 更新`btsmtl-node-interruption-lifecycle`，把每层producer术语改为每AnimationChannelId唯一输出。
- [x] 16.16.7 复查其它current specs只允许在禁止旧路径的语境中保留`LayerId`、TransitionLibrary或Animancer fade字样。
- [x] 16.17 记录最终`AnimationSelection -> SelectedPosePlayer或BlendStack -> 可选局部Inertialization -> Pose Plan -> FootPlacement`业务链路。
- [x] 16.18 记录Pose Graph与未来Motion Matching、Equipment动态层的正式扩展边界。
- [x] 16.19 在design记录唯一Rig Reference、Local/Mesh编译、PoseCurveResolve双输入与v2 frame-cache合同。
- [x] 16.20 在Pose Graph spec记录唯一Rig Reference、Local/Mesh编译、PoseCurveResolve双输入与v2 frame-cache合同。

## 17. 完整显式Pose Graph重新基线

- [x] 17.1 将图输入从PoseSlotFrame迁移为AnimationSelection与typed Program Parameter。
- [x] 17.2 新增AnimationSelectionInput与MotionMatchingSelectionInput节点合同。
- [x] 17.3 新增MarkerSync Selection节点合同。
- [x] 17.3.1 编译MarkerSync与stateful Player一对一source-usage合同。
- [x] 17.3.2 将Timeline Marker effective time解析迁入MarkerSync节点。
- [x] 17.3.3 删除图外Marker Sync自动装配与隐藏Stack entry扫描。
- [x] 17.3.4 新增SelectedPosePlayer节点合同。
- [x] 17.4 将Blend Stack算法装配为显式Player节点。
- [x] 17.4.1 将Inertialization装配为SelectedPosePlayer后的显式局部Pose节点。
- [x] 17.4.2 禁止Blend Stack与Inertialization同时拥有残差算法。
- [x] 17.5 新增普通BlendPose节点。
- [x] 17.6 保留并迁移LayeredBoneBlend与AdditivePose节点。
- [x] 17.7 将PoseCurveResolve迁移为PoseParameterResolve合同。
- [x] 17.8 新增受Rig BoneId约束的ModifyBone节点。
- [x] 17.9 新增显式FootPlacement作者节点。
- [x] 17.10 将FootPlacement降低为唯一world-aware执行阶段。
- [x] 17.11 让OutputPose只发布完成FootPlacement/IK后的FinalAnimationPoseFrame。
- [x] 17.12 删除固定PoseSlotInput与隐藏Stack依赖。
- [x] 17.13 删除图外自动追加Foot Placement的路径。
- [x] 17.14 升级validator、compiler、runtime plan和typed source map。
- [x] 17.15 升级Preview与Live Debug以显示Selection raw time、MarkerSync relation/effective time、Player source usage、Stack和阶段completion。
- [x] 17.16 按显式节点集重建Corin Pose Graph和Presentation Profile。
- [x] 17.17 更新本change全部spec delta为完整显式图口径。
- [x] 17.18 与`refactor-animation-selection-pose-graph-boundary`共同完成后再收口归档。
