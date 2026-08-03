# btsmtl-timeline-editor-preview Specification

## MODIFIED Requirements

### Requirement: Timeline Animation Analysis必须是按需领域工具

Timeline窗口 MAY为有限Action Animation Clip通过显式Character Editor provider提供Animation Analysis面板。面板 MUST默认关闭，并显式显示当前Action Clip、Analysis Source、artifact状态、脚与metric。持续Locomotion Pose source的Analysis工具 MUST属于Profile source editor，Timeline窗口 MUST不要求或伪造对应Timeline。两种入口生成的artifact都 MUST只读展示且不得进入Timeline selection、Undo或Curve Channel Catalog。

#### Scenario: 查看Attack脚分析

- **WHEN** 作者选中Attack AnimationClip、选择匹配Analysis Source并打开Analysis
- **THEN** Timeline面板 MUST允许选择一只脚和一个metric查看
- **AND** Timeline主时间轴 MUST不增加生成feature行

#### Scenario: 查看Run Pose source脚分析

- **WHEN** 作者从Run Sequence source打开Analysis
- **THEN** 必须导航到Profile source editor
- **AND** MUST不创建RunLoop Timeline或反向搜索Definition

### Requirement: Timeline 编辑器预览目标来自正式管线预览目标

系统 MUST继续使用`TimelinePreviewTarget`作为Timeline编辑器可选择的预览目标抽象，并由正式CharacterPipelineHost实现。目标 MUST沿Definition、Presentation Profile与匹配Projection取得有限Action Playback Input、AnimationSlot、Transition Routing、完整Pose Plan、Rig与Action producer binding。Timeline Preview MUST不负责直接预览持续Locomotion Pose source；该工作由使用同一target-neutral Projection的Pose Graph Preview承担。系统 MUST不使用TimelinePlayer、场景搜索、fallback target、Definition内联Presentation或第二份动画拓扑。

#### Scenario: 选择Action Timeline预览目标

- **WHEN** 用户为Attack Timeline选择场景预览对象
- **THEN** 对象 MUST实现正式TimelinePreviewTarget
- **AND** Preview MUST通过FullBodyAction Slot进入同一Pose Plan

#### Scenario: 未选择预览目标

- **WHEN** Timeline Editor没有有效target
- **THEN** 用户 MAY继续编辑Timeline数据
- **AND** 播放与可应用预览 MUST禁用且不得自动搜索Host

### Requirement: 预览采样必须复用正式动画Selection与Pose Plan

有限Action Timeline的Authoring Preview MUST把当前Track/Clip时间降低为正式Action Animation Selection与Parameter page，并执行匹配Projection的Action Playback Input、AnimationSlot、Transition Routing、Player和Pose Plan。持续Locomotion Pose source、PoseStateMachine与Transition Rule的预览 MUST属于Pose Graph Workspace，Timeline Preview MUST不伪造BaseLocomotion Timeline或Presentation Fact。具备正式Body与PhysicsScene上下文时Preview MAY执行FootPlacement，否则 MUST标记world-aware阶段Unavailable。

#### Scenario: 当前时间采样Attack

- **WHEN** 作者把Timeline Preview游标移动到Attack clip中间
- **THEN** Preview MUST生成对应Action Selection并送入FullBodyAction Slot
- **AND** 最终路径 MUST与正式Pose Plan一致

#### Scenario: 尝试预览Walk到Run

- **WHEN** 作者需要预览Locomotion PoseState从Walk到Run
- **THEN** Timeline Editor MUST导航到Pose Graph Workspace
- **AND** MUST不创建临时Walk/Run Timeline preview command

#### Scenario: 非连续seek

- **WHEN** 作者从一个Action producer非连续seek到另一个producer
- **THEN** Slot、Routing与Inertialization MUST按正式seek/reset policy处理
- **AND** Preview MUST不创建额外fade或简化dispatcher

### Requirement: 动画预览必须使用一个正式Runtime和分离输入Adapter

Timeline Action Preview、Pose Graph Fact Preview与Motion Matching Query Fixture MUST复用同一个`AnimationPreviewRuntime`、Projection、source backend与Pose Plan，但 MUST通过不同typed input adapter提交数据。Timeline adapter MUST创建preview-session scoped非零ActionInstance并通过正式Action command inbox提交Select、Sample、Complete与Release；Pose Graph adapter MUST只提交Fact；MM Fixture adapter MUST只提交PoseState relevance与query fixture。后两者 MUST不创建Gameplay producer、AnimationChannel winner或`AnimationPlaybackId`。

#### Scenario: Timeline Preview播放Attack

- **WHEN** Preview session选择Attack Timeline并移动游标
- **THEN** 同一session的Select与Sample MUST保持相同非零ActionInstance
- **AND** command MUST经过正式Action inbox与Slot

#### Scenario: Pose Graph Preview没有Action

- **WHEN** 作者只修改Locomotion速度Fact
- **THEN** Preview MUST直接推进PoseState与Pose Plan
- **AND** MUST不创建空Action entry

#### Scenario: MM Query Fixture执行查询

- **WHEN** Fixture使一个MM PoseState provider relevant
- **THEN** 结果 MUST通过state-local provider sample进入绑定Player
- **AND** MUST不伪造Timeline producer或PlaybackId

### Requirement: Timeline Editor 必须编辑 AnimationTrack Marker Sync

Timeline Editor MUST只为有限Action Timeline中的AnimationTrack编辑SyncMode、SyncGroupId、Finite/Cyclic topology、SyncRole和marker。每个Action AnimationTrack MUST拥有固定可折叠`SYNC MARKERS`子轨；该子轨 MUST只是父Track作者数据投影，不得成为独立runtime Track。持续Locomotion Pose source marker MUST由Profile source editor编辑，Timeline Editor MUST不显示其副本或把Pose source加入同AnimationChannel候选集合。所有Action marker mutation MUST继续通过正式Timeline authoring API进入Undo、dirty、identity、validation、RebindTimeline和Preview刷新。

#### Scenario: 编辑Attack marker

- **WHEN** 作者在Attack AnimationTrack的SYNC MARKERS子轨拖动marker
- **THEN** Editor MUST保持MarkerAuthoringId并以一个Undo事务提交
- **AND** 必须触发正式validation、Projection stale和Action Preview刷新

#### Scenario: 尝试从Timeline编辑Run marker

- **WHEN** 作者从Action Timeline上下文搜索Locomotion.Gait候选
- **THEN** 候选 MUST只来自同一Action Slot同步可达的有限Action producer
- **AND** Run Pose source MUST只提供Open Profile Source导航而不成为可写Track

#### Scenario: Marker与Action Foot Placement曲线同时存在

- **WHEN** Action AnimationTrack启用MarkerGroup且Clip拥有Foot Placement Weight
- **THEN** Marker MUST只显示在SYNC MARKERS子轨
- **AND** Foot Placement Weight MUST只显示在CURVES子轨

### Requirement: Timeline Editor 必须完整编辑显式注册的 Continuous Curve Channel

Timeline Editor MUST通过显式typed Curve Channel Catalog显示和编辑Timeline Clip正式拥有的Continuous Curve，并继续覆盖Action Animation Clip、MotionCurve、MotionWarp、CameraState与CameraResponse的已注册channel。持续Locomotion Pose source的Foot Placement Weight MUST由Profile source curve editor使用同一typed descriptor语义编辑，但不得序列化为Timeline Clip或出现在Timeline CURVES分组。Editor MUST不通过反射、字段名、SerializedProperty path或任意字符串发现和修改curve。

#### Scenario: 展开Action Animation Track曲线

- **WHEN** 作者展开包含Action Animation Clip的CURVES分组
- **THEN** Editor MUST显示该Clip注册的Weight、Ease In、Ease Out与Foot Placement Weight
- **AND** Sync Marker MUST继续只显示在独立SYNC MARKERS子轨

#### Scenario: 编辑Run Pose source曲线

- **WHEN** 作者从Run Sequence source打开Foot Placement Weight
- **THEN** Profile source curve editor MUST编辑Run binding的typed curve
- **AND** Timeline Editor MUST不创建Run Clip或CURVES分组

### Requirement: Authoring Preview 必须复用正式 Marker Sync 表现链

有限Action Timeline Authoring Preview MUST通过CharacterPresentationProjection解析producer marker binding、Action AnimationChannelId与Slot/Player PoseNodeId，并复用正式MarkerSync、session-local `CharacterActionPlaybackRuntime`、Slot、source backend与Pose Plan。持续Locomotion Sequence/BlendSpace source marker的预览 MUST由Pose Graph Workspace从Profile binding执行，Timeline Editor MUST不复制或编辑这些marker。

#### Scenario: 单Action producer预览marker

- **WHEN** 作者拖动MarkerGroup Action AnimationTrack游标
- **THEN** Preview MUST显示该时间所在marker pair与segment fraction
- **AND** 没有source handoff时effective time MUST等于raw time

#### Scenario: 比较Locomotion Walk与Run handoff

- **WHEN** 作者选择Walk与Run Presentation Pose source
- **THEN** Pose Graph Workspace MUST通过正式MarkerSync与PoseState transition执行比较
- **AND** Timeline Preview MUST不生成BaseLocomotion Selection

### Requirement: Timeline Live Debug 必须显示正式 Sync Relation

Timeline Live Debug MUST从共享RuntimeDebugSession显示有限Action Timeline source/target PlaybackId、AnimationChannelId、MarkerSync PoseNodeId、SyncGroup、有向pair、fraction、occurrence、raw/effective time、cycle、relation depth、lifecycle与detach/failure reason。PoseState Source Sync relation MUST在Pose Graph Live Debug显示PoseState、Transition generation与Presentation source usage；Timeline Live Debug MAY提供只读跨工作区导航，但 MUST不把Pose relation伪装为Timeline playback。两者 MUST不重新采样、推导State transition、求值Pose Graph或维护第二份relation状态。

#### Scenario: 观察Action连续切换

- **WHEN** runtime发生`Attack1 -> Attack2 -> Dodge`并存在Action relation chain
- **THEN** Timeline Live Debug MUST按playback generation显示relation与depth
- **AND** 数据 MUST来自当帧正式snapshot

#### Scenario: 观察Walk到Run

- **WHEN** runtime发生PoseState Source Sync
- **THEN** Timeline Live Debug MUST提供Open Pose Graph Live导航
- **AND** MUST不生成虚假AnimationPlaybackId
