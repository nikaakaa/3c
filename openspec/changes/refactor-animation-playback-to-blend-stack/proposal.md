# Change: 重构动画播放为每Pose Slot完整Blend Stack

## Why

当前动画运行时把逻辑选择、Timeline表现采样和Animancer淡入淡出分开，但真正的时间混合权威仍属于Animancer：

- `AnimationPlaybackLifecycle`只记录Current与Outgoing，`AnimancerPlaybackAdapter`按producer复用visual state，同producer不同generation不能稳定共存。
- 高频A到B到C切换时没有每entry独立Fade Clock、Curve、稳定顺序和固定容量。
- 没有Stored Pose或Inertial Blend，Motion Matching式高频pose jump只能无限保留source或提前丢pose。
- Animancer scalar fade无法表达同一次transition中脚、骨盆、脊柱和手臂使用不同速度。
- Foot Analysis、Marker Sync、Preview和Debug把Animancer state weight当成最终可见贡献，无法覆盖Stored/Inertial或后续空间Bone Mask。

原提案把“每一路姿势的时间历史”和“多路姿势的空间合成”一起放进Blend Stack，并让它写最终Animator Pose。`add-character-presentation-pose-graph`已经把UE式职责正式分层：Blend Stack必须只处理每个Pose Slot内部的时间连续性；Pose Graph唯一处理跨slot Bone Mask、Override/Additive、Pose Parameter和最终Animator Pose。

本change因此收窄为一次完成项目正式的per-slot Blend Stack核心：项目拥有entry、独立clock、curve、per-bone transition weight、容量压缩、Stored Pose、Inertial Blend和slot source retirement；Animancer只负责Clip/ManualMixer source采样；Blend Stack输出`PoseSlotFrame`，由Character Presentation Pose Graph完成空间合成。

## What Changes

- 每个编译后的`PoseSlotId`固定装配唯一`AnimationBlendStackRuntime`。它不是BTSMTL节点，也不是作者可选Pose Graph节点。
- 分离`AnimationPlaybackId`、`AnimationBlendEntryId`与Animancer source visual。同producer不同generation拥有独立source；同一Playback连续sample不重启clock；重新成为target时创建新entry。
- `CharacterAnimationBlendLibrary`按Pose Slot唯一保存Stack Policy、显式default rule与source-target override；Compiler物化所有可达producer/Empty组合的完整transition matrix，Runtime不fallback。
- transition rule显式声明CrossFade或Inertial、duration、canonical curve和`CharacterAnimationBlendProfile`。
- `CharacterAnimationRigDefinition`保存稳定dense BoneId；Blend Profile按BoneId保存transition duration multiplier；Prefab通过`CharacterAnimationRigBinding`显式绑定Runtime骨骼。
- 每个CrossFade entry拥有独立Fade Clock，并按每骨骼duration multiplier和push depth计算nested residual weight。
- 每Pose Slot显式配置`MaxActiveSourceEntries`、`MaxBlendInTimeToReplaceNewest`与`DepthBlendTimeMultiplier`。容量或快速替换触发时捕获当前完整slot pose、velocity、Pose Parameter和每脚feature aggregate为Stored Pose，再原子释放不再需要的source。
- Inertial transition从当前slot pose/velocity相对新target建立每骨骼residual；旧source退出，单一accumulator衰减；连续中断从当前修正结果rebase。
- `AnimancerPoseSamplingBackend`只创建AnimationClip state/producer内部ManualMixer、写sample time和child weight、管理playable寿命，不调用Layer Play/Fade或transition lookup。
- `AnimationSlotBlendPoseEvaluator`使用预分配Native workspace完成source capture、CrossFade、Stored Pose、Inertial和slot参数/贡献输出；它不做跨slot mask、additive、curve最终解析或Animator最终写回。
- 新增不可变`PoseSlotFrame`：Availability、slot output weight、dense local pose、Pose Parameter、live/Stored/Inertial contribution、每脚feature aggregate和continuity identity。
- Marker Sync只在同`AnimationChannelId/PoseSlotId`内于target入栈前解析effective time。Stored Pose和Inertial不冒充producer或relation节点。
- Foot Placement实际输入改为Pose Graph最终空间合成后的左右脚贡献。Blend Stack只提供slot内部贡献与feature aggregate，不声称它已经是最终可见结果。
- Preview、Runtime和Live Debug复用同一Stack与slot evaluator，显示entry order、clock、curve、selected BoneId weight、Stored capture、Inertial residual、PoseSlotFrame和退役原因。
- 原子迁移Profile、Blend Library、Rig、Projection与Corin资产；删除Animancer fade/TransitionLibrary、旧Layer Stack、旧global compositor与兼容字段。

## Impact

### Specs

- 新增`character-animation-blend-stack`
- 修改`character-animation-layer-runtime`
- 修改`character-animation-presentation-authoring`
- 修改`character-animation-pipeline`
- 修改`character-foot-placement-presentation`
- 实施完成后同步更新`openspec/project.md`

### Code

- Animation playback、Lifecycle、Marker Sync与PresentationRetention合同
- PoseSlot Blend Stack、curve/weight evaluator、Stored Pose、Inertial与slot pose workspace
- Animancer source sampling backend
- Profile、Blend Library、Rig、Projection Compiler与generated Projection schema
- Character Presentation Pose Graph输入、Preview、Trace和Foot Placement feature链
- Corin Blend Library、Rig Binding与正式Profile资产

### Active Change 关系

- 本change与`add-character-presentation-pose-graph`形成同一最终动画管线。Blend Stack输出PoseSlotFrame，Pose Graph消费全部slot并写最终Animator Pose；两者不得分别安装出两个compositor或临时直通输出。
- 本change使用`AnimationChannelId -> PoseSlotId`一对一binding，不再使用旧LayerId或Profile Layer catalog。
- `refactor-presentation-projection-target-boundary`完成后，Rig、slot Stack policy、transition matrix与PoseSlotFrame所需feature继续属于同一target-neutral Projection，不进入Numeric Program。
- Marker Sync、Foot Analysis与Foot Placement既有Gameplay/Presentation边界不变，只迁移贡献来源。
- 后续Motion Matching位于ResolvedAnimationPoseRequest之前，必须复用同一slot Stack，不建立私有crossfade。

## Breaking Changes

- Animancer不再拥有fade、Layer weight、transition easing或最终pose；TransitionLibraryAsset、FadeMode与FadeGroup从正式链删除。
- Blend Stack不再拥有跨slotLayer order、AvatarMask、Override/Additive或最终Animator Pose。
- Stack owner从LayerId改为PoseSlotId；transition pair必须属于同AnimationChannelId/PoseSlotId。
- `AnimationBlendPoseEvaluator`职责拆为per-slot `AnimationSlotBlendPoseEvaluator`和由另一change提供的`CharacterPoseGraphEvaluator`。
- visible contribution schema升级为PoseSlotFrame内部贡献；最终Foot contribution只由Pose Graph输出。
- Profile、Projection与Blend Library schema提升，旧Layer配置和旧generated Projection直接失效。
- 不提供Animancer fade、旧Layer compositor、默认transition、bind pose或旧single-scalar weight fallback。
