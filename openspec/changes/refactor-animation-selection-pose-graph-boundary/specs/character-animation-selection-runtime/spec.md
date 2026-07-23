## ADDED Requirements

### Requirement: Gameplay与搜索层必须只提交Animation Selection

BTSMTL Program与Motion Matching Module MUST只向Presentation提交版本化`AnimationSelectionFrame`与typed Presentation Parameter page。Selection MUST保存AnimationChannel、producer、source、generation、sample time、continuous time、cycle、loop、play rate和source-local clip sample；MUST不保存transition rule、Blend entry、Bone Mask、IK plan或最终weight。BTSMTL MUST继续唯一仲裁每AnimationChannel的Gameplay winner，Motion Matching MUST只替换明确绑定的selection provider。

#### Scenario: BTSMTL状态机切换移动producer

- **WHEN** Program将BaseLocomotion channel从Run切换为Stop
- **THEN** Presentation MUST收到Stop的Animation Selection
- **AND** Program MUST不创建Blend entry或计算Run到Stop的表现权重

#### Scenario: Motion Matching跳到新姿势

- **WHEN** Motion Matching选择同一clip中的新pose time
- **THEN** Module MUST发布提升generation的新Selection
- **AND** MUST不直接创建私有播放器或crossfade

### Requirement: Selection请求工作区必须按frame lease复用容量

Animation Selection请求工作区的source row MUST只属于当前表现帧。`BeginFrame` MUST使上一帧全部row lease失效并回收占用，Projection容量 MUST表示单帧最大并发source数量；Runtime MUST不按历史playback generation累积row，也 MUST不通过扩大容量掩盖未回收的旧generation。

#### Scenario: 连续动作产生多个playback generation

- **WHEN** 多个动作在不同表现帧依次创建新的source identity
- **THEN** 每帧 MUST只占用该帧实际解析的source row
- **AND** 上一帧lease MUST不能在新completion中读取

### Requirement: Pose Graph必须显式选择Animation Player

每个Animation Selection MUST只通过Pose Graph中的`SelectedPosePlayer`或`BlendStack`节点降低为Pose Value。`SelectedPosePlayer` MUST只采样当前Selection并发布typed discontinuity；没有下游Inertialization时执行明确硬切。`BlendStack` MUST保存该节点自己的多source历史并执行已编译CrossFade。Compiler与Runtime MUST不在Selection Input、AnimationChannel或OutputPose背后自动插入Player、Stack、Inertialization或fade。

#### Scenario: 稳定动作使用直接Player

- **WHEN** 作者把Action Selection连接到SelectedPosePlayer
- **THEN** Selection变化 MUST直接替换当前source
- **AND** Runtime MUST不创建隐藏Blend Stack

#### Scenario: 状态机输出使用Blend Stack

- **WHEN** 作者把BaseLocomotion Selection连接到BlendStack
- **THEN** Selection变化 MUST由该节点保存旧player并连续过渡
- **AND** 其它未连接该节点的Selection MUST不承担其workspace或transition

### Requirement: Marker Sync必须是显式Selection节点

Timeline与Motion Matching MUST只提交raw visual sample；只有Pose Graph中的`MarkerSync`节点 MAY在source采样前把raw time解析为effective time。节点 MUST位于Selection Input与一个stateful Player之间，唯一拥有marker relation、leader/follower、segment fraction和continuation anchor；MUST不采样Pose、不计算blend weight、不保存playable、不延长source lifetime。没有MarkerSync节点时Player MUST直接使用raw visual time，Runtime与Preview MUST不自动补建同步。

#### Scenario: BaseLocomotion显式启用步态同步

- **WHEN** BaseLocomotion Selection依次连接MarkerSync与SelectedPosePlayer
- **THEN** Player MUST先声明正式source usage
- **AND** MarkerSync MUST在Player采样source前生成对应effective sample page

#### Scenario: Action分支没有MarkerSync

- **WHEN** FullBodyAction Selection直接连接BlendStack
- **THEN** BlendStack MUST按各source raw visual time采样
- **AND** Timeline、Lifecycle与Runtime MUST不在图外应用marker mapping

### Requirement: Marker Sync与Player必须通过正式source usage合同配对

每个MarkerSync输出 MUST精确连接一个`SelectedPosePlayer`或`BlendStack`。Compiler MUST生成一对一`PlayerSourceUsage`合同，并显式区分`Sample`、`HandoffReference`与`Release`。SelectedPosePlayer MUST在切换边界把旧source声明为一次性HandoffReference、把新source声明为Sample，完成映射后立即release旧source且不得保留旧Pose；BlendStack MUST把当前与尚未exact release的历史source声明为Sample。MarkerSync只为该集合解析时间，随后Player完成source sample与Pose求值。MarkerSync MUST不扫描BlendStack entry或读取weight；Player MUST不复制marker relation算法。fan-out到多个Player、串联两个MarkerSync或缺少Player consumer MUST编译失败。

#### Scenario: BlendStack保留Walk并接收Run

- **WHEN** BlendStack source usage同时包含Retained Walk与incoming Run
- **THEN** 配对MarkerSync MUST按Track marker binding解析两者effective time
- **AND** BlendStack MUST独立计算两者CrossFade weight

#### Scenario: SelectedPosePlayer从Walk切换到Run

- **WHEN** SelectedPosePlayer不保留旧Pose但在边界帧把Walk声明为HandoffReference
- **THEN** MarkerSync MUST能用Walk最后effective segment映射Run起始effective time
- **AND** 映射完成后Walk MUST立即release且后续平滑只属于Inertialization

#### Scenario: 同一Selection进入两个Player

- **WHEN** 作者需要两个Player各自保留独立播放状态
- **THEN** 作者 MUST为需要同步的每条Player路径分别创建MarkerSync
- **AND** Compiler MUST不共享隐藏relation state

### Requirement: Blend Stack节点必须独占自身时间连续性

每个编译后的Blend Stack节点 MUST拥有唯一runtime identity、active player顺序、CrossFade clock、Stored Pose、Per-Bone Blend Profile、source retention与exact release。节点 MUST只消费Animation Selection与node-local Blend Policy，输出普通Pose Value；MUST不读取或执行Inertial residual、Gameplay State、Motion Matching query、下游Bone Mask、Foot Placement或Output topology。

#### Scenario: A到B尚未完成又选择C

- **WHEN** 同一Blend Stack节点在A到B过渡期间收到C Selection
- **THEN** 节点 MUST按编译Policy保留或压缩当前历史并开始到C的连续过渡
- **AND** 不得要求BTSMTL重新提交A或B的Gameplay逻辑

#### Scenario: Selection转为Empty

- **WHEN** AllowEmpty Blend Stack从live Selection转为Empty并且旧source不再提供当前帧sample
- **THEN** 节点 MUST用上一已完成Pose捕获Stored Pose并执行编译后的live-to-Empty过渡
- **AND** MUST不再请求旧source采样，并在捕获帧完成后exact release旧source

### Requirement: Blend Policy必须按节点物化完整transition

每个Blend Stack节点 MUST引用唯一`CharacterAnimationBlendPolicy`。Compiler MUST枚举该节点全部可达Selection endpoint，将authoring default与exact override物化为完整source-target/Empty table，并把canonical curve与dense Blend Profile编入Projection。Runtime MUST只按稳定identity exact lookup；缺失pair、重复override、未知source或Rig不匹配 MUST失败且不得fallback。

#### Scenario: Action Stack缺少Attack到Empty规则

- **WHEN** Compiler发现Action Blend Stack可达Attack与Empty但没有可物化的合法pair
- **THEN** Projection Build MUST失败并定位该Blend Stack节点和endpoint

### Requirement: Animancer必须只负责source采样

Animancer source backend MUST只按完整source identity创建或复用Sequence/ManualMixer playable、应用Selection sample time、loop、play rate与source-local clip weight并提供pose capture。它 MUST不仲裁AnimationChannel winner、不查询Blend Policy、不拥有跨source weight、不执行Layer composition、不追加Foot Placement，也 MUST不发布最终Pose。

#### Scenario: Blend Stack切换source

- **WHEN** Blend Stack要求同时采样旧source与新source
- **THEN** Animancer MUST分别提供两个source pose capture
- **AND** source间weight MUST只由Blend Stack节点计算

### Requirement: Selection Preview必须执行正式Pose Plan

Timeline Preview与Motion Matching Query Fixture MUST把Editor输入降低为正式Animation Selection并执行匹配Projection的`CharacterPresentationPosePlan`。图中只使用直接Player时Preview MUST显示硬切；图中使用Blend Stack时 MUST复用正式entry、CrossFade clock与Stored语义；图中使用Inertialization时 MUST复用正式history、residual与rebase。Preview不得创建简化player、固定per-slot Stack、全局惯性器、临时PlayableGraph或Animancer direct Play路径。

#### Scenario: Timeline Preview seek到另一个producer

- **WHEN** 作者在Preview中非连续seek并且图中Selection连接SelectedPosePlayer
- **THEN** Preview MUST按Player hard-cut语义更新pose
- **AND** MUST不为了平滑预览而后台插入Blend Stack
