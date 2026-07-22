## ADDED Requirements

### Requirement: 每个Pose Slot必须拥有唯一有序Blend Stack

每个编入Projection的PoseSlotId MUST拥有唯一`AnimationBlendStackRuntime`。Stack MUST按稳定push order保存active `AnimationBlendEntryId`，并将`AnimationPoseSourceId`与表现entry identity分离。同一SourceId连续sample MUST只更新source；同一SourceId在另一个target之后重新成为target MUST创建新EntryId与独立Fade Clock；同Playback不同SelectionGeneration MUST拥有独立source visual。Stack MUST只处理绑定到该slot的AnimationChannel producer，不读取State、Action、Pose Graph topology、Bone Mask、业务priority或Timeline authoring object。

#### Scenario: A淡出期间切换到B和C

- **WHEN** 同一Pose Slot内A到B尚未结束且C首样本到达
- **THEN** Stack MUST按稳定顺序保留A、B、C active entry
- **AND** A与B各自Fade Clock MUST不因C push重置

#### Scenario: 同producer不同generation重入

- **WHEN** producer P generation 10仍被entry引用且generation 11首样本到达
- **THEN** 两个AnimationPoseSourceId MUST拥有独立source visual与sample lifetime
- **AND** generation 11 MUST不覆盖generation 10

#### Scenario: 不同slot同时切换

- **WHEN** BaseLocomotionSlot与FullBodyActionSlot同帧各自push新target
- **THEN** 两个Stack MUST独立推进entry与clock
- **AND** 跨slot结果 MUST只由Pose Graph组合

### Requirement: CrossFade必须使用独立Clock、Curve与每骨骼规范化Weight

每个CrossFade entry MUST独立保存elapsed、base duration、canonical curve、Blend Profile与push depth。`AnimationBlendCurveEvaluator` MUST按`base duration * per-bone duration multiplier * depth multiplier`计算每根骨骼raw/eased alpha，并从最新到最旧使用nested residual计算slot内部最终weight。每根骨骼的live source与Stored Pose weight MUST规范化；项目 MUST不调用Animancer FadeGroup、easing或state weight作为结果。

#### Scenario: 腿比上半身更快完成切换

- **WHEN** Blend Profile给腿较小duration multiplier、脊柱和手臂较大multiplier
- **THEN** 同帧腿骨eased alpha MAY高于上半身
- **AND** 每根骨骼自己的全部entry weight MUST仍规范化

#### Scenario: A到B未完成时push C

- **WHEN** B clock未完成且C以alpha 0入栈
- **THEN** push边界PoseSlotFrame pose MUST等于push前A/B混合pose
- **AND** A、B、C MUST继续按各自clock参与nested residual

#### Scenario: AllowEmpty slot淡出

- **WHEN** AllowEmpty slot收到正式Empty target
- **THEN** Stack MUST以透明NoPose entry连续消耗slot output weight
- **AND** MUST不创建fallback clip或bind pose

#### Scenario: RequireOutput slot首次获得合法source

- **WHEN** RequireOutput slot从无Current pose的PendingFirstSample收到首个合法source pose
- **THEN** Projection Compiler MUST已将该slot的`Empty -> producer` exact transition物化为零时长
- **AND** Stack MUST在同一原子提交中以完整权重发布首个source pose
- **AND** Runtime MUST不新增Uninitialized混合状态、临时改写duration或使用bind pose、默认Idle与残留姿势补帧

### Requirement: Blend Stack容量必须通过Stored Pose连续压缩

每个Pose Slot MUST显式配置至少为2的`MaxActiveSourceEntries`。push超过容量或最新entry命中`MaxBlendInTimeToReplaceNewest`时，Evaluator MUST在切换前捕获当前完整slot local pose、pose velocity、Pose Parameter与Left/Right Foot Analysis aggregate为唯一Stored Pose，再原子移除被取代entry。Stored Pose MUST使用预分配slot，不引用AnimationClip、AnimationPoseSourceId、Marker或Gameplay事件。系统 MUST不提供直接丢弃entry或关闭Stored Pose的正式配置。

#### Scenario: 高频切换达到容量

- **WHEN** 新target push会让live source超过slot容量
- **THEN** capture边界每根骨骼输出 MUST与capture前PoseSlotFrame连续
- **AND** 新target MUST从Stored Pose之上以alpha 0开始

#### Scenario: 捕获后释放旧source

- **WHEN** Stored Pose完成pose、velocity、parameter与Foot aggregate捕获
- **THEN** 不再被entry或relation引用的旧AnimationPoseSourceId MUST由Stack发布带completion identity的release事实
- **AND** Stored Pose MUST不继续推进Timeline、Marker、Notify或root motion

#### Scenario: 快速替换最新entry

- **WHEN** 最新entry elapsed未超过正式replace阈值且新target到达
- **THEN** Evaluator MUST捕获当前完整slot输出并替换短命历史
- **AND** MUST不按动画名称、帧率或任意weight阈值猜测

### Requirement: Inertial Blend必须保持Pose与Velocity连续

Inertial transition MUST从切换前current/previous PoseSlotFrame与新target pose计算每骨骼position、rotation、scale和velocity residual，并由该slot唯一Inertial Accumulator按duration、curve和Blend Profile衰减。旧CrossFade entry与Stored Pose MUST在capture完成后退出，新target MUST成为唯一live animation source。Rotation MUST使用最短弧且保持单位四元数；Inertial residual MUST不成为第二组entry或Pose Graph节点。

#### Scenario: Attack动作被替换

- **WHEN** FullBodyActionSlot的Attack1到Attack2 rule为Inertial
- **THEN** 切换首帧slot pose与velocity MUST从Attack1当前输出连续
- **AND** 后续只采样Attack2并衰减residual

#### Scenario: Inertial尚未结束时再次切换

- **WHEN** accumulator仍有residual且新target到达
- **THEN** Runtime MUST先求当前修正pose/velocity再相对新target重建同一Accumulator
- **AND** MUST不叠加第二个Accumulator

#### Scenario: residual完成

- **WHEN** 全部骨骼clock完成且residual归零
- **THEN** Runtime MUST清除Accumulator
- **AND** PoseSlotFrame MUST完全来自当前target source

### Requirement: Per-Bone Blend Profile必须依赖稳定Rig Identity

Profile MUST引用唯一`CharacterAnimationRigDefinition`；Definition MUST以稳定BoneId与父节点优先顺序定义dense skeleton。每个Blend Profile MUST匹配同一RigId/revision，由global duration multiplier和按BoneId override构成。Compiler MUST展开与Rig顺序一致的dense数组；Prefab MUST通过显式Rig Binding绑定全部Runtime Transform。系统 MUST不使用Humanoid、骨骼名称、Transform path或层级搜索补全。

#### Scenario: Profile只覆盖腿部差异

- **WHEN** Profile global multiplier为1并覆盖左右腿BoneId
- **THEN** 未覆盖骨骼 MUST使用正式global值
- **AND** 腿骨 MUST使用dense override

#### Scenario: Runtime缺少骨骼绑定

- **WHEN** Rig Definition包含某BoneId但Prefab Binding缺失
- **THEN** Presentation Runtime创建 MUST失败并报告BoneId
- **AND** MUST不按名称搜索或复制父骨姿势

#### Scenario: Profile来自另一Rig

- **WHEN** transition引用不匹配RigId/revision的Profile
- **THEN** Projection Build MUST失败
- **AND** Runtime MUST不截断或重排数组

### Requirement: Animancer必须只作为Source Pose采样后端

`AnimancerPoseSamplingBackend` MUST只为完整`AnimationPoseSourceId`创建AnimationClip state或producer内部ManualMixerState、应用resolved sample time/loop/child weight并管理source playable寿命。Timeline控制state MUST保持Speed为0且child MUST保持DontSynchronize；request的VisualTimeScale MUST继续表达有效视觉时间推进率而不是state Speed。Backend MUST不调用AnimancerLayer.Play、StartFade、FadeGroup、自动Layer weight或transition lookup，MUST不决定entry weight、slot composition、retirement或最终Animator Pose。

#### Scenario: producer包含两个重叠clip

- **WHEN** request包含两个合法clip sample
- **THEN** Backend MUST在同一Playback source ManualMixer内按Timeline child weight采样
- **AND** Stack MUST把该producer pose作为一个slot source entry

#### Scenario: source不再被引用

- **WHEN** Stack、relation、selection与capture均不再引用某Playback source
- **THEN** Backend MUST精确释放该AnimationPoseSourceId state/mixer
- **AND** MUST不读取Animancer FadeGroup决定时机

### Requirement: 每个Pose Slot必须由固定Animation Job输出PoseSlotFrame

Runtime MUST按Projection Rig bone count、slot count和每slot容量预分配source、Stored、pose history、Inertial、parameter、feature与weight Native workspace。每个Pose Slot MUST由唯一`AnimationBlendStackRuntime`拥有source workspace、双页不可变`AnimationSlotBlendFramePlan`与slot workspace；Runtime MUST把完整SourceId降低为frame-local capture index和带generation的physical source identity后原子提交inactive page。Source capture MUST把Animancer source AnimationStream写入独立buffer；唯一`AnimationSlotBlendJob` MUST按相同非零exact completion求值该slot的CrossFade、Stored与Inertial，写完整Native Pose Slot buffer并最后写`CompletedAt`。Source playable、capture job、slot blend job、Pose Graph job与最终writer MUST位于同一PlayableGraph并在一次Evaluate中顺序完成；Runtime MUST不在两次Evaluate之间回到托管代码逐骨复制，也 MUST不保留managed pose evaluator作为第二路径。Slot job MUST不读取跨slotBone Mask、执行Override/Additive、写VisualRoot/Gameplay Body或写最终Animator Pose。

#### Scenario: 同帧求值两个slot

- **WHEN** BaseLocomotion与FullBodyAction Stack均有合法frame plan
- **THEN** 两个Slot Evaluator MUST分别发布PoseSlotFrame
- **AND** Character Pose Graph MUST在两者完成后唯一合成最终pose

#### Scenario: workspace容量不匹配

- **WHEN** Projection要求的source/Stored slot超过创建时workspace
- **THEN** Runtime创建 MUST失败
- **AND** 表现帧 MUST不动态扩容或关闭Per-Bone能力

#### Scenario: Clip包含root motion

- **WHEN** source Clip带root transform曲线
- **THEN** Slot Evaluator MUST遵守Rig root exclusion且不修改VisualRoot/Gameplay Body
- **AND** Gameplay motion MUST继续只来自Program与WorldSolver

### Requirement: Blend Stack Transition Matrix必须是同Pose Slot唯一转场权威

Projection MUST为每个PoseSlotId保存显式Stack Policy、canonical curves、dense Blend Profiles及该slot绑定AnimationChannel全部可达source-target/Empty组合的完整matrix。Runtime MUST只按稳定producer index exact lookup。Animancer TransitionLibrary、Pose Graph edge、BTSMTL State edge、动画名称或缺失pair默认值 MUST不参与transition解析。

#### Scenario: source-target存在精确override

- **WHEN** Library为同slot Attack1到Attack2配置Inertial override
- **THEN** Compiler MUST物化exact matrix entry
- **AND** Runtime MUST不查询default rule或Animancer library

#### Scenario: override跨slot

- **WHEN** source属于FullBodyActionSlot而target属于BaseLocomotionSlot
- **THEN** Projection Build MUST拒绝该pair
- **AND** MUST不使用Pose Graph共同可见关系修正

#### Scenario: 可达pair无法物化

- **WHEN** 任一合法pair缺少duration、curve、technique或Blend Profile
- **THEN** Projection Build MUST失败并报告producer identity
- **AND** Runtime MUST不使用固定0.2秒或Linear fallback

### Requirement: PoseSlotFrame必须完整表达Stack输出

每个完成slot evaluation MUST发布不可变PoseSlotFrame，包含PoseSlotId、completion identity、Pose/NoPose/Invalid availability、output weight、dense local pose、Pose Parameter buffer、live/Stored/Inertial contribution、左右脚feature aggregate与continuity identity。PoseSlotFrame MUST不声称已经经过跨slotBone Mask，也 MUST不携带Pose Graph authoring node或Gameplay state。

#### Scenario: Optional slot完成到Empty

- **WHEN** FullBodyActionSlot完成source-Empty transition
- **THEN** PoseSlotFrame MUST为NoPose、零output weight和零最终slot contribution
- **AND** MUST不保留上一帧action pose

#### Scenario: Stored Pose参与输出

- **WHEN** live source与Stored Pose共同贡献
- **THEN** Frame MUST按稳定identity列出两类贡献与per-bone weight
- **AND** Pose Graph MUST能在不读取旧Playback的情况下消费该frame

### Requirement: Blend Stack调试必须完整解释Slot Pose来源

正式snapshot MUST按AnimationChannelId/PoseSlotId显示selection、Pending、EntryId、AnimationPoseSourceId及其Playback/SourceKind/SelectionGeneration、push order、source reference、technique、duration、elapsed、raw/eased alpha、选定BoneId weight、Stored capture、Inertial residual、PoseSlotFrame、retirement与workspace状态。Preview和Live Debug MUST只读取该snapshot，不得重新求值curve、weight、pose、capacity或最终Pose Graph贡献。

#### Scenario: 排查三次连续切换

- **WHEN** A到B未完成又切到C并触发Stored capture
- **THEN** Debug MUST显示capture前entry、capture原因、Stored Pose与C clock
- **AND** MUST区分source retirement与Stored继续贡献

#### Scenario: 查看左脚与脊柱进度

- **WHEN** transition使用Per-Bone Blend Profile
- **THEN** Debug MUST按BoneId显示两处slot内部actual weight
- **AND** MUST不只显示Animancer scalar或冒充最终跨slotweight
