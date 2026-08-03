# Tasks

## 1. 基线与共同合同

- [x] 1.1 枚举Animancer state、Layer、FadeGroup、TransitionLibrary与最终Pose调用点。
- [x] 1.2 枚举Lifecycle Current、Outgoing、Retired、superseded和producer visual复用路径。
- [x] 1.3 枚举Marker Sync对Current/Outgoing和Animancer weight的读取点。
- [x] 1.4 枚举Foot Placement对Animancer scalar与visible contribution的读取点。
- [x] 1.5 枚举Preview、Trace与Debug重建fade/weight的路径。
- [x] 1.6 核对AnimationChannelId与PoseSlotId最终类型和Projection binding。
- [x] 1.7 核对Pose Graph change的PoseSlotFrame输入合同。
- [x] 1.8 核对Rig Definition与Pose Graph dense BoneId共同合同。
- [x] 1.9 建立旧Layer Stack、global compositor和Animancer transition待删除清单。
- [x] 1.10 标记并行Projection、Foot Analysis与Presentation Runtime改动边界。

## 2. Rig与Runtime Binding

- [x] 2.1 定义CharacterAnimationRigDefinition schema version。
- [x] 2.2 定义稳定RigId与content revision。
- [x] 2.3 定义稳定BoneId。
- [x] 2.4 定义父节点优先dense BoneId数组。
- [x] 2.5 定义ParentIndex约束。
- [x] 2.6 定义root motion exclusion。
- [x] 2.7 定义scale policy。
- [x] 2.8 定义LeftFoot语义BoneId。
- [x] 2.9 定义RightFoot语义BoneId。
- [x] 2.10 校验dense顺序、父索引、唯一根与无环。
- [x] 2.11 定义CharacterAnimationRigBinding组件。
- [x] 2.12 让Rig Binding按dense index显式保存Transform。
- [x] 2.13 校验Transform非空、唯一且属于同Animator。
- [x] 2.14 校验RigId/revision精确匹配。
- [x] 2.15 删除运行时Humanoid、名称、path和层级搜索补全。

## 3. Per-Bone Blend Profile

- [x] 3.1 定义CharacterAnimationBlendProfile schema。
- [x] 3.2 定义ProfileId。
- [x] 3.3 保存RigId/revision引用。
- [x] 3.4 保存显式global duration multiplier。
- [x] 3.5 保存按BoneId override列表。
- [x] 3.6 校验global multiplier有限且为正。
- [x] 3.7 校验override BoneId存在且唯一。
- [x] 3.8 校验override multiplier有限且为正。
- [x] 3.9 将Profile展开为dense multiplier数组。
- [x] 3.10 将dense Profile纳入ProjectionRevision。
- [x] 3.11 在Inspector显示Rig identity和最终multiplier。
- [x] 3.12 删除按骨骼名称或数组长度勉强应用路径。

## 4. Blend Library与Transition Matrix

- [x] 4.1 将Blend Library owner key定义为PoseSlotId。
- [x] 4.2 定义MaxActiveSourceEntries字段。
- [x] 4.3 定义MaxBlendInTimeToReplaceNewest字段。
- [x] 4.4 定义DepthBlendTimeMultiplier字段。
- [x] 4.5 校验Stack容量至少为2。
- [x] 4.6 校验阈值与倍率有限合法。
- [x] 4.7 定义CrossFade technique。
- [x] 4.8 删除Inertial technique并让BlendStack类型本身唯一表达CrossFade。
- [x] 4.9 定义transition duration。
- [x] 4.10 定义canonical transition curve引用。
- [x] 4.11 定义Blend Profile引用。
- [x] 4.12 定义每slot default transition rule。
- [x] 4.13 定义同slotsource-target exact override。
- [x] 4.14 校验override source/target属于同AnimationChannelId/PoseSlotId。
- [x] 4.15 枚举每slot全部可达producer与合法Empty组合。
- [x] 4.16 物化完整source-target transition matrix。
- [x] 4.17 拒绝duplicate、orphan、cross-slot与缺失pair。
- [x] 4.18 让Runtime只按stable producer index exact lookup。
- [x] 4.19 删除Runtime default rule与固定duration fallback。
- [x] 4.20 删除Animancer TransitionLibrary authoring引用。
- [x] 4.21 强制RequireOutput slot的Empty到producer exact transition为零时长。

## 5. Canonical Curve

- [x] 5.1 定义normalized curve schema。
- [x] 5.2 校验首key为0/0。
- [x] 5.3 校验末key为1/1。
- [x] 5.4 校验key time严格递增。
- [x] 5.5 校验value位于0到1。
- [x] 5.6 校验value单调不减。
- [x] 5.7 校验key与tangent有限。
- [x] 5.8 将curve编入Projection canonical payload。
- [x] 5.9 实现唯一AnimationBlendCurveEvaluator。
- [x] 5.10 删除Animancer easing与FadeGroup curve读取。

## 6. Entry与Request合同

- [x] 6.1 定义AnimationBlendEntryId。
- [x] 6.2 将PoseSlotId纳入EntryId。
- [x] 6.3 将完整AnimationPoseSourceId纳入EntryId。
- [x] 6.4 将PresentationRequestSequence纳入EntryId。
- [x] 6.5 定义Live Source Entry状态。
- [x] 6.6 定义Stored Pose Entry状态。
- [x] 6.7 删除每slot Inertial Accumulator并由局部Inertialization节点独占Accumulator。
- [x] 6.8 定义ResolvedAnimationPoseRequest中的AnimationChannelId。
- [x] 6.9 定义ResolvedAnimationPoseRequest中的PoseSlotId。
- [x] 6.10 保存resolved visual time、cycle和scale。
- [x] 6.11 保存producer内部clip samples。
- [x] 6.12 保存exact matrix source/target identity。
- [x] 6.13 保存Pose Parameter samples。
- [x] 6.14 保存Foot Analysis samples。
- [x] 6.15 禁止request携带State、Action、Priority、Pose Graph或Bone Mask。
- [x] 6.16 让PendingFirstSample首个合法request以完整权重原子初始化RequireOutput slot。

## 7. Per-Slot Stack Runtime

- [x] 7.1 让每个Projection PoseSlot创建唯一AnimationBlendStackRuntime。
- [x] 7.2 按stable push order保存active entries。
- [x] 7.3 让同AnimationPoseSourceId连续sample只更新source。
- [x] 7.4 让同Playback不同SelectionGeneration创建新EntryId。
- [x] 7.5 让不同generation拥有独立source visual。
- [x] 7.6 为每entry保存独立elapsed clock。
- [x] 7.7 为每entry保存base duration。
- [x] 7.8 为每entry保存canonical curve index。
- [x] 7.9 为每entry保存Blend Profile index。
- [x] 7.10 为每entry保存push depth。
- [x] 7.11 实现每骨骼duration计算。
- [x] 7.12 实现从新到旧nested residual。
- [x] 7.13 实现每骨骼weight规范化。
- [x] 7.14 让重复引用同SourceId的entry复用唯一固定source capture slice并保留各自贡献身份。
- [x] 7.15 实现AllowEmpty透明NoPose transition。
- [x] 7.16 实现RequireOutput拒绝Empty。
- [x] 7.17 实现entry归零与retirement条件。
- [x] 7.18 禁止Stack读取Pose Graph topology或跨slot状态。

## 8. Stored Pose压缩

- [x] 8.1 预分配每Pose Slot Stored Pose buffer。
- [x] 8.2 捕获当前dense local pose。
- [x] 8.3 捕获previous/current pose velocity。
- [x] 8.4 捕获Pose Parameter aggregate。
- [x] 8.5 捕获source contribution aggregate。
- [x] 8.6 捕获LeftFoot feature aggregate。
- [x] 8.7 捕获RightFoot feature aggregate。
- [x] 8.8 实现容量溢出capture条件。
- [x] 8.9 实现最新entry快速替换capture条件。
- [x] 8.10 在新entry push前完成capture。
- [x] 8.11 原子移除被Stored取代的entries。
- [x] 8.12 在capture引用结束且同completion成功后发布完整SourceId release。
- [x] 8.13 阻止Stored Pose推进Timeline或Marker。
- [x] 8.14 阻止Stored Pose伪造PlaybackId或AnimationClip。
- [x] 8.15 删除达到容量后直接丢entry路径。

## 9. Inertial数学迁移到局部节点

- [x] 9.1 预分配每Inertialization节点唯一workspace。
- [x] 9.2 捕获切换前current/previous slot pose。
- [x] 9.3 捕获新target pose与velocity。
- [x] 9.4 计算position residual。
- [x] 9.5 计算shortest-arc rotation residual。
- [x] 9.6 计算scale residual并应用Rig scale policy。
- [x] 9.7 计算linear/angular velocity residual。
- [x] 9.8 捕获Pose Parameter residual。
- [x] 9.9 捕获每脚feature transition端点。
- [x] 9.10 按duration、curve与Blend Profile衰减。
- [x] 9.11 让SelectedPosePlayer在handoff完成后立即退出旧source。
- [x] 9.12 让新target成为Player唯一live source。
- [x] 9.13 实现连续中断从当前修正结果rebase。
- [x] 9.14 禁止叠加第二个Accumulator。
- [x] 9.15 在residual完成后清除Accumulator。
- [x] 9.16 拒绝非有限residual和非法四元数。

## 10. Animancer Source Sampling Backend

- [x] 10.1 将AnimancerPlaybackAdapter替换为AnimancerPoseSamplingBackend。
- [x] 10.2 按完整AnimationPoseSourceId保存source visual。
- [x] 10.3 让单Clip source使用唯一ManualMixerState单child，避免同SourceId运行时更换拓扑。
- [x] 10.4 为多Clip producer创建ManualMixerState。
- [x] 10.5 应用resolved visual sample time。
- [x] 10.6 应用loop/cycle状态。
- [x] 10.7 应用producer内部child weight。
- [x] 10.8 保持Timeline source Speed为0。
- [x] 10.9 保持child DontSynchronize。
- [x] 10.10 删除AnimancerLayer.Play调用。
- [x] 10.11 删除StartFade和FadeGroup调用。
- [x] 10.12 删除Animancer layer weight写入。
- [x] 10.13 删除transition lookup。
- [x] 10.14 删除按ProducerId覆盖旧generation路径。
- [x] 10.15 只在无entry/relation/retention引用后释放source。

## 11. Slot Pose Workspace与PoseSlotFrame

- [x] 11.1 定义PoseSlotFrame schema。
- [x] 11.2 定义Pose/NoPose/Invalid availability。
- [x] 11.3 定义slot output weight。
- [x] 11.4 定义dense local pose buffer。
- [x] 11.5 定义Pose Parameter buffer。
- [x] 11.6 定义BlendStack的live/Stored contribution，并让Inertialization独立发布普通Pose contribution。
- [x] 11.7 定义左右脚feature aggregate。
- [x] 11.8 定义continuity identity。
- [x] 11.9 定义completion identity。
- [x] 11.10 按Rig bone count预分配source capture buffer。
- [x] 11.11 按slot容量预分配weight buffer。
- [x] 11.12 预分配pose history与velocity buffer。
- [x] 11.13 预分配parameter与feature buffer。
- [x] 11.14 建立唯一AnimationSlotBlendJob并删除managed pose evaluator路径。
- [x] 11.15 生成双页不可变per-slot frame plan并原子提交active page。
- [x] 11.16 让source capture job写入独立Native buffer slice。
- [x] 11.17 让Slot Job发布完整Native Pose Slot输出并最后写completion identity。
- [x] 11.18 禁止Slot Job读取跨slotMask和Additive。
- [x] 11.19 禁止Slot Job写最终Animator Pose。
- [x] 11.20 禁止表现帧动态扩容或Transform逐骨写入。

## 12. Marker Sync与Retention迁移

- [x] 12.1 将Marker relation约束为同AnimationChannelId/PoseSlotId。
- [x] 12.2 在Stack push前完成target effective time解析。
- [x] 12.3 只使用push前live Current和Pending target建立relation。
- [x] 12.4 禁止Stored Pose成为Marker source。
- [x] 12.5 禁止局部Inertialization的Accumulator成为Marker source。
- [x] 12.6 禁止Pose Graph共同可见关系建立Marker relation。
- [x] 12.7 在CrossFade source退出时建立continuation anchor。
- [x] 12.8 在Stored capture后建立continuation anchor。
- [x] 12.9 让SelectedPosePlayer handoff完成后释放旧source，局部Inertialization只保留Pose history。
- [x] 12.10 让relation拓扑按PlaybackId稳定求值。
- [x] 12.11 保持logic release后的animation-only retention。
- [x] 12.12 在Stack/relation/pending均释放后Retire source。
- [x] 12.13 在Reset与Dispose原子清理relation、anchor与retention。

## 13. Projection、Foot Feature与Pose Graph接缝

- [x] 13.1 将Blend Library per-slot policy编入Projection。
- [x] 13.2 将完整transition matrix编入Projection。
- [x] 13.3 将Rig dense payload编入Projection。
- [x] 13.4 将Blend Profile dense payload编入Projection。
- [x] 13.5 将Stack workspace需求编入Projection。
- [x] 13.6 将PoseSlotFrame schema identity编入Projection。
- [x] 13.7 按effective time采样live source Foot Analysis。
- [x] 13.8 按LeftFoot transition weight生成slot aggregate。
- [x] 13.9 按RightFoot transition weight生成slot aggregate。
- [x] 13.10 让Stored Pose保留每脚aggregate。
- [x] 13.11 让局部Inertialization按实际脚Bone envelope传播每脚feature。
- [x] 13.12 把PoseSlotFrame交给唯一PoseSlotInput。
- [x] 13.13 禁止Foot Placement直接读取PoseSlot scalar。
- [x] 13.14 让Foot Placement只读取Pose Graph最终每脚贡献。
- [x] 13.15 让Pose Graph Invalid阻止Pose Post Process。

## 14. Runtime编排、Preview与Debug

- [x] 14.1 更新Presentation Runtime创建顺序。
- [x] 14.2 更新每帧command消费与slot plan顺序。
- [x] 14.3 在全部slot plan后采样Animancer sources。
- [x] 14.4 在全部source capture后求值PoseSlotFrame。
- [x] 14.5 在全部PoseSlotFrame完成后调用Pose Graph。
- [x] 14.6 在FinalAnimationPoseFrame后调用Foot Placement。
- [x] 14.7 更新Reset分别清理BlendStack/Stored与局部Inertialization顺序。
- [x] 14.8 更新Dispose等待job并释放workspace顺序。
- [x] 14.9 让Timeline Preview复用正式per-slot Stack。
- [x] 14.10 让Timeline Preview复用正式Slot Evaluator。
- [x] 14.11 扩展snapshot保存channel、slot与entry顺序。
- [x] 14.12 扩展snapshot保存clock、curve与per-bone weight。
- [x] 14.13 扩展snapshot分别保存Stack Stored与局部Inertialization详情。
- [x] 14.14 扩展snapshot保存PoseSlotFrame与continuity。
- [x] 14.15 明确snapshot中的weight尚未经过Pose Graph最终Mask。
- [x] 14.16 删除按Animancer state重建weight的Debug路径。

## 15. 旧路径删除与Corin资产单一归属

- [x] 15.1 删除旧Animancer fade与Layer weight代码。
- [x] 15.2 删除旧LayerId Stack key与Layer compositor。
- [x] 15.3 删除旧AnimationBlendPoseEvaluator global output职责。
- [x] 15.4 删除旧single-scalar visible contribution schema。
- [x] 15.5 删除旧Projection Layer/transition字段与reader。
- [x] 15.6 删除FormerlySerializedAs、fallback与兼容转换。
- [x] 15.7 更新`openspec/project.md`中Animancer、Blend Stack与Pose Graph职责。
- [x] 15.8 将Corin通道、Pose Graph、Rig、Blend Profile、Blend Library、Runtime Binding、Projection与Program wrapper资产迁移统一归属到`add-character-presentation-pose-graph`第15章。
- [x] 15.9 从本change删除重复的Corin资产创建与重建任务，不建立第二份迁移清单。
- [x] 15.10 在proposal与design记录两个change不得分开形成Corin临时配置或独立归档闭包。

## 16. 显式Blend Stack节点重新基线

- [x] 16.1 将Stack owner从PoseSlotId迁移为稳定PoseNodeId。
- [x] 16.2 让Stack节点消费AnimationSelectionFrame。
- [x] 16.3 让Stack节点输出统一Pose Value。
- [x] 16.4 将per-slot policy迁移为node-local CharacterAnimationBlendPolicy。
- [x] 16.5 让Compiler只物化该节点可达selection的exact transition table。
- [x] 16.6 把entry、clock、CrossFade、Stored Pose与Per-Bone算法迁入BlendStack节点实例。
- [x] 16.6.1 把既有Inertial数学、history、rebase与workspace整体迁移给局部Inertialization节点。
- [x] 16.6.2 删除BlendStack中的Inertial technique、rule、state、contribution与diagnostics字段。
- [x] 16.7 把source retention与release迁入节点生命周期。
- [x] 16.7.1 让Stack membership预阶段发布current与Retained PlayerSourceUsage。
- [x] 16.7.2 让Stack exact release发布匹配source usage release。
- [x] 16.7.3 让Stack在连接MarkerSync时消费effective sample page。
- [x] 16.7.4 让Stack未连接MarkerSync时消费Selection raw visual time。
- [x] 16.7.5 删除Stack对MarkerId、SyncRole、relation与effective time的所有权。
- [x] 16.7.6 删除Lifecycle对隐藏Stack entry的Marker扫描。
- [x] 16.8 删除固定per-slot Stack创建与workspace分配。
- [x] 16.9 删除PoseSlotFrame专用输出和隐藏PoseSlotInput接线。
- [x] 16.10 更新Preview、Live Debug与snapshot为PoseNodeId、source usage和Pose Value口径。
- [x] 16.11 更新本change全部spec delta为显式节点口径。
- [x] 16.12 确认本change只作为新边界change的算法迁移输入，不独立形成旧链归档闭包。
