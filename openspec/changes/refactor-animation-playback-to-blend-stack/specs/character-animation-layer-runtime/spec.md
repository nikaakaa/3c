## RENAMED Requirements

- FROM: `### Requirement: Animancer 必须是实际动画混合权威`
- TO: `### Requirement: Pose Slot Blend Stack必须是时间混合权威`
- FROM: `### Requirement: Animancer 必须继续独占 Fade 与最终 Pose`
- TO: `### Requirement: Stack与Pose Graph必须分别独占时间混合和最终Pose`

## MODIFIED Requirements

### Requirement: 角色管线不依赖旧动画播放路径

角色管线和Timeline Preview MUST共用`AnimationChannel selection -> Lifecycle -> PoseSlot Blend Stack -> Animancer source backend -> Slot Evaluator -> Pose Graph`。系统 MUST不读取旧AnimationPresentationPolicySO、旧locomotion/action SO、旧bodyclaim、Animancer TransitionLibrary、旧Layer compositor，也 MUST不依赖TimelinePlayer autonomous playback、Animator.Play/CrossFade、Animancer FadeGroup或独立PlayableGraph作为另一权威。

#### Scenario: 发现旧直接fade入口

- **WHEN** 实施发现角色路径仍调用Animancer Layer Play/Fade或旧global compositor
- **THEN** 该引用 MUST删除并迁移到正式source backend/Stack/Pose Graph
- **AND** MUST不保留兼容分支

#### Scenario: Timeline Preview

- **WHEN** Editor预览角色动画
- **THEN** Preview MUST复用正式Projection、Lifecycle、Stack、Slot Evaluator、Rig和Pose Graph
- **AND** MUST不创建scalar fade或简化composition

### Requirement: 循环动画必须由连续 visual Timeline time 重采样

循环producer的continuous visual time MUST由committed Timeline logic sample、cycle identity与PresentationFrame interpolation计算。AnimationPlaybackLifecycle MUST只关联selected/pending和仍被PoseSlot Stack引用的producer，不得推进CharacterSimulationState Timeline clock。逻辑producer release后，只要Stack entry、Marker relation或PresentationRetention仍持有该Playback，sampler MUST继续animation-only sampling，且 MUST不执行Gameplay operation。

#### Scenario: 循环回绕

- **WHEN** committed loop sample从末尾回绕到开头
- **THEN** AnimationTrack MUST使用连续visual time重采样同一Playback generation

#### Scenario: Source已停止

- **WHEN** producer Gameplay ownership已release且其source仍被PoseSlot Stack entry引用
- **THEN** Presentation MAY继续animation-only sample
- **AND** TreeClip、Motion、Window与Cue fact MUST不再产生

### Requirement: 动画播放生命周期必须只管理可见 producer 寿命

每个AnimationChannelId MUST拥有一个Lifecycle并绑定唯一PoseSlot Stack。Lifecycle MUST只保存selection、PendingFirstSample、PresentationRetention和命令协调；Stack MUST唯一保存entry、clock、Stored/Inertial和source retirement；Pose Graph MUST只消费PoseSlotFrame。三者 MUST不复制彼此状态，也 MUST不解释State、Action、业务Priority或跨层winner。

#### Scenario: target首样本延迟

- **WHEN** Stack已有Current A且选择B但首样本未到
- **THEN** Lifecycle MUST记录Pending B并继续输出A slot pose
- **AND** MUST不选择默认Idle或当前clip副本

#### Scenario: target首样本到达

- **WHEN** Pending B收到匹配generation的合法sample
- **THEN** Lifecycle MUST原子创建resolved request并push到slot Stack
- **AND** Stack MUST按exact matrix创建EntryId与clock

#### Scenario: source视觉贡献完成

- **WHEN** Playback无entry、relation、capture、selected或pending引用
- **THEN** Lifecycle MUST标记Retired并释放retention/source visual
- **AND** Pose Graph MUST不参与该判断

### Requirement: Pose Slot Blend Stack必须是时间混合权威

`AnimationBlendStackRuntime`与`AnimationSlotBlendPoseEvaluator` MUST负责同PoseSlot entry order、Fade Clock、curve、Per-Bone transition weight、Stored Pose、Inertial、slot source retirement与PoseSlotFrame。Animancer MUST只负责source采样。Character Pose Graph MUST负责跨slotMask、Override/Additive、Parameter最终解析和Animator最终Pose。项目 MUST不保留Animancer FadeGroup、Animancer Layer weight、旧global Stack compositor或第二套crossfade evaluator。

#### Scenario: producer包含多个clip

- **WHEN** 同producer采样多个重叠clip
- **THEN** Animancer backend MUST在同Playback ManualMixer内表达child weight
- **AND** Stack MUST把混合后的producer pose作为一个entry source

#### Scenario: fade期间再次切换

- **WHEN** A到B未完成又选择C
- **THEN** Stack MUST保留entry或捕获Stored Pose后push C
- **AND** 已有clock MUST不被Animancer或Pose Graph重建

#### Scenario: transition使用Inertial

- **WHEN** exact pair声明Inertial
- **THEN** Slot Evaluator MUST从当前slot pose/velocity建立唯一Accumulator
- **AND** Animancer MUST只采样新target

### Requirement: 同组 producer handoff 必须按 Marker Segment 映射

当同AnimationChannelId/PoseSlotId内live Current切换到incoming target，且两者为同canonical Marker Group时，Runtime MUST只从Stack push前Current与Pending取得真实PlaybackId并按SyncRole解析leader/follower。Marker relation MUST不跨channel/slot，不得使用Stored Pose、Inertial、Pose Graph、Bone Mask、名称、Action或entry weight推导方向。

#### Scenario: Walk切换Run

- **WHEN** BaseLocomotionSlot从WalkLoop切到同组RunLoop
- **THEN** Runtime MUST先映射Run effective time再push Stack
- **AND** Pose Graph可见性 MUST不改变relation

#### Scenario: 跨slot同组

- **WHEN** Attack与Run声明同SyncGroup但分属不同slot
- **THEN** Projection Build MUST失败
- **AND** Runtime MUST不建立跨slotrelation

### Requirement: Marker Sync 必须在共同可见期间持续求值

Marker Sync MUST在同PoseSlot source与target共同为live entry的每个PresentationFrame重新使用source effective marker segment求target effective time，不得只在首样本保存固定phase offset。target MUST以mapped time重新采样完整producer，包括AnimationClip membership、ClipIn、ease和内部weight。Gameplay raw Timeline sample、cycle与logic completion MUST保持不变。Stored Pose、Inertial和Pose Graph共同可见期 MUST不延长Marker relation。

#### Scenario: 不同时长循环动画blend

- **WHEN** 1.0秒WalkLoop与0.6秒RunLoop在BaseLocomotionSlot共同可见
- **THEN** RunLoop MUST在每个PresentationFrame持续对齐WalkLoop marker fraction
- **AND** MUST不因各自速度不同在transition后半段漂移

#### Scenario: 多clip target producer

- **WHEN** mapped time使target AnimationTrack采样到两个重叠Clip
- **THEN** 两个clip的membership、time与内部weight MUST由同一effective time重采样
- **AND** Marker Sync MUST不选择某个clip作为第二phase authority

#### Scenario: Gameplay状态立即切换

- **WHEN** Program在logic tick把状态从Walk切换到Run
- **THEN** Gameplay state、Motion与World request MUST在该tick推进
- **AND** Presentation MUST不等待marker边界才提交状态切换

### Requirement: Sync relation 必须服从播放生命周期并连续脱离

Sync relation MUST以完整PlaybackId为key并限制在同AnimationChannelId/PoseSlotId，只依赖Lifecycle selected/pending/retention与Stack live source/capture/retired事实。source因CrossFade完成、Stored capture或Inertial capture退役时，Runtime MUST以target最后effective/raw time建立continuation anchor后删除relation。Stored Pose、Inertial和Pose Graph MUST不成为relation节点。

#### Scenario: 连续A到B到C

- **WHEN** B仍跟随A且B又成为C source
- **THEN** 求值顺序 MUST为A effective到B effective到C effective
- **AND** 三者 MUST属于同channel/slot

#### Scenario: source被Stored捕获

- **WHEN** source capture后无live entry引用
- **THEN** target MUST建立continuation anchor
- **AND** 下一帧不得跳回raw Timeline time

### Requirement: outgoing producer 必须使用纯表现 retention

逻辑producer release后，只要Playback仍为Selected、Pending、被active entry引用、参与capture或Marker relation，Lifecycle MUST持有只读PresentationRetention并继续animation-only sample。Retention MUST不恢复Program membership，也 MUST不运行TreeClip、Motion、Window或Cue。Stored Pose不继续运行旧source。

#### Scenario: 攻击逻辑结束但仍CrossFade

- **WHEN** Attack Gameplay已停止但source仍被entry引用
- **THEN** sampler MUST继续animation-only sample
- **AND** 直到entry与relation释放

#### Scenario: Stored捕获完成

- **WHEN** pose/velocity/parameter/feature capture完成
- **THEN** Lifecycle MAY释放不再被引用的source retention
- **AND** Stored Pose MUST不推进旧Timeline

### Requirement: Stack与Pose Graph必须分别独占时间混合和最终Pose

MarkerSyncRuntime MUST只提供effective sample time；Animancer backend MUST只提供source pose；PoseSlot Stack MUST独占同slottransition、clock、curve、Per-Bone weight、capacity、Stored、Inertial与retirement；Character Pose Graph MUST独占跨slotMask、Override/Additive、Parameter resolve和最终Animator Pose。项目 MUST不保留Animancer automatic sync/fade、Animator crossfade、第二动画时钟或另一pose compositor。

#### Scenario: 同步target开始混合

- **WHEN** matched target首样本进入Lifecycle
- **THEN** Marker Sync MUST先给出effective time
- **AND** Stack MUST按exact matrix建立transition
- **AND** Pose Graph MUST只读取完成PoseSlotFrame

#### Scenario: 最终动画Pose完成

- **WHEN** 全部PoseSlotFrame完成且Pose Graph写回Animator AnimationStream
- **THEN** Runtime MUST发布唯一FinalAnimationPoseFrame
- **AND** Foot Placement MUST只在该completion后运行
