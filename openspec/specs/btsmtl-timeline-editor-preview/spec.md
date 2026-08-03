# btsmtl-timeline-editor-preview Specification

## Purpose
定义BTSMTL有限Action Timeline编辑器预览的正式链路：`TimelinePreviewSession`通过typed Action adapter接入唯一`AnimationPreviewRuntime`，复用Action lifecycle、AnimationSlot、Transition Routing、source backend与Pose Plan；持续Locomotion由Pose Graph Fact Preview负责，不恢复旧`TimelinePlayer`、BaseLocomotion Timeline、共享Playback总管或独立PlayableGraph。
## Requirements
### Requirement: Source Time Authoring模块必须跨正式owner复用

Timeline Field的time ruler、marker、curve与analysis interaction、geometry和rendering MUST被抽象为不依赖Timeline数据类型的Source Time Authoring模块。Timeline AnimationTrack/Clip与Presentation Pose Source binding MUST分别通过typed owner adapter使用同一模块，并把Mutation提交给各自正式owner。模块 MUST不复制数据、不创建Locomotion Timeline、不提供任意自定义curve或SerializedProperty入口。

#### Scenario: Timeline与Pose Source编辑相同曲线类型

- **WHEN** 作者分别编辑Attack Clip和Run Pose Source的Foot Placement Weight
- **THEN** 两个页面 MUST共享key/tangent/selection/Undo交互实现
- **AND** 数据 MUST分别只写入Timeline Clip与Profile binding

#### Scenario: 提取模块后编辑Timeline marker

- **WHEN** 作者在原Timeline页面拖动Attack marker
- **THEN** Timeline AnimationTrack identity与Mutation语义 MUST保持不变
- **AND** Presentation Profile MUST不获得副本

### Requirement: Timeline Animation Analysis必须是按需领域工具

Timeline窗口 MAY为有限Action AnimationClip通过显式Character Editor provider提供Animation Analysis面板。面板 MUST默认关闭，不占Track行；打开后 MUST显式显示当前Action Clip、Analysis Source、artifact状态、Left/Right选择与单一metric选择。持续Locomotion Pose source的Analysis MUST属于Profile source editor，Timeline窗口不得创建或反向搜索对应Timeline。生成曲线 MUST只读且不得进入Timeline selection、Undo或Curve Channel Catalog。

#### Scenario: 查看Attack脚分析

- **WHEN** 作者选中Attack AnimationClip、选择匹配Analysis Source并打开Analysis
- **THEN** 面板 MUST允许选择一只脚和一个metric查看
- **AND** Timeline主时间轴 MUST不增加Sole Speed、Height、Plant或Landing行

#### Scenario: 查看Run Pose source脚分析

- **WHEN** 作者从Run Sequence source打开Analysis
- **THEN** 必须导航到Profile source editor
- **AND** MUST不创建RunLoop Timeline或反向搜索Definition

#### Scenario: 未选择Analysis Source

- **WHEN** 独立Timeline打开Analysis但没有显式Source
- **THEN** 面板 MUST显示Analysis Source Required
- **AND** MUST不搜索引用该Timeline的Definition或Graph

### Requirement: Timeline Analysis必须显示并显式应用脚接触候选

Animation Analysis面板 MUST在有限Action artifact Ready时显示由左右脚PlantConfidence实际采样推导的contact候选及目标frame。候选 MUST保持瞬时只读，不得自动保存或成为第二份运行真相。作者确认目标Action AnimationTrack后，Apply MUST重新校验artifact、AnimationClip dependency、Analysis Source、Sampling Rig、Calibration、采样参数、Timeline映射和candidate revision，并通过Timeline正式mutation生成已有的AnimationSyncMarker作者数据。持续Locomotion Pose source候选只能由Profile source editor写入其binding。

#### Scenario: 应用Action候选

- **WHEN** 作者选择完整覆盖MarkerGroup/Finite AnimationTrack的Action Clip并确认Apply
- **THEN** 面板 MUST把当前未过期候选写为正式LeftFootContact与RightFootContact Marker
- **AND** MUST只替换这两类脚接触Marker、尽量保留匹配的stable marker identity并保留其它业务Marker
- **AND** 写入 MUST进入既有Undo、dirty、validator、compiler与Agent v15链

#### Scenario: 候选已经过期

- **WHEN** 候选显示后artifact identity/content hash、Clip dependency或Timeline映射发生变化
- **THEN** Apply MUST拒绝旧candidate revision
- **AND** MUST不按缓存frame、clip名称或半周期假设继续写入Marker

#### Scenario: producer级映射不唯一

- **WHEN** 目标Track不是MarkerGroup/Cyclic，或存在多个AnimationClip，或单Clip没有完整覆盖Timeline
- **THEN** 面板 MAY显示只读候选但 MUST禁用Apply并说明映射不唯一
- **AND** MUST不选择权重最高Clip或按名称猜测Marker来源

### Requirement: Timeline Analysis工具不得伪造Foot Placement世界

Animation Analysis面板 MUST只显示离线AnimationClip局部特征。它 MUST不执行PhysicsScene查询、Foot Lock、Ground Envelope、Pelvis、Final IK或Camera，不得把离线plant confidence显示为Gameplay contact。

#### Scenario: 预览Attack动画

- **WHEN** 作者查看Attack的plant或landing metric
- **THEN** 面板 MUST明确数据属于动画局部分析
- **AND** MUST不显示虚构地面、锁脚或运行时IK结果

### Requirement: Timeline 编辑器预览使用管线预览会话

系统 MUST 使用 editor-only TimelinePreviewSession 作为 TimelineEditorWindow 的播放、暂停、速度和游标预览控制器。TimelineEditorWindow MUST为当前绑定的 resolved TimelineData 建立唯一 preview session，并在窗口重绑或释放时正式释放旧 preview owner。TimelineEditorWindow MUST NOT直接控制 TimelinePlayer、PlayableGraph 或旧 Timeline autonomous playback。

#### Scenario: inline Timeline 窗口点击播放

- **WHEN** 用户从 TimelineNode 打开 inline Timeline 并点击播放
- **THEN** TimelineEditorWindow 的 TimelinePreviewSession MUST只读使用该节点的 resolved TimelineData
- **AND** session MUST NOT修改 TimelineNode 内的 authoring data
- **AND** page MUST NOT调用旧 TimelinePlayer

#### Scenario: shared Timeline root page 点击播放

- **WHEN** 用户直接打开 shared TimelineAsset 并点击播放
- **THEN** TimelinePreviewSession MUST只读使用 TimelineAsset.Data
- **AND** preview controls MUST与 inline TimelineEditorWindow 使用同一实现
- **AND** shared TimelineAsset MUST不保存 preview time 或 target

#### Scenario: TreeClip 跨窗口下钻

- **WHEN** 用户从 TimelineEditorWindow 打开 TreeClip Graph page或在 Graph 窗口返回
- **THEN** TimelineEditorWindow 的 preview session MUST保持归属当前 Timeline 窗口
- **AND** Graph 页面切换 MUST NOT创建、接管或释放 Timeline preview session

### Requirement: Timeline 编辑器预览目标来自正式管线预览目标

系统 MUST使用`TimelinePreviewTarget`作为Timeline编辑器可选择的预览目标抽象，并由`CharacterPipelineHost`或等价正式角色管线目标实现它。正式角色管线预览目标 MUST沿`CharacterPipelineDefinition.AnimationPresentationProfile`与匹配的`CharacterPresentationProjection`取得Action Playback Input、AnimationSlot、Transition Routing、完整Pose Plan、Rig与有限Action producer source binding，并使用`AnimancerComponent`和显式`CharacterAnimationRigBinding`。Timeline Preview MUST不直接预览持续Locomotion Pose source；该工作由同一Projection上的Pose Graph Fact Preview承担。系统 MUST不使用TimelinePlayer、场景搜索、fallback target、Definition内联Presentation或第二份动画拓扑配置作为预览目标。

#### Scenario: 选择预览目标

- **WHEN** 用户在 Timeline 编辑器 target field 选择场景对象
- **THEN** 可接受对象 MUST是 `TimelinePreviewTarget`
- **AND** 当前角色管线目标 MUST由 `CharacterPipelineHost` 实现
- **AND** `CharacterPipelineHost` MUST使用 Definition 引用的正式 CharacterAnimationPresentationProfile 与匹配 Projection
- **AND** `CharacterPipelineHost` MUST使用正式 `AnimancerComponent` 应用动画预览

#### Scenario: 未选择预览目标

- **WHEN** Timeline 编辑器没有有效 `TimelinePreviewTarget`
- **THEN** 用户 MAY继续编辑 Timeline 数据
- **AND** 播放、暂停、速度和可应用预览 MUST处于禁用状态
- **AND** 系统 MUST不自动查找场景中的 Host 或 TimelinePlayer

### Requirement: Timeline 资产不保存编辑器播放状态

系统 MUST 将编辑器预览播放状态保存在 TimelinePreviewSession 中。Inline TimelineData、shared TimelineAsset 及其持有的 TimelineData MUST只保存 authoring 数据，不得保存当前预览目标、session identity、playback generation、PlayableGraph 或预览播放状态。

#### Scenario: 两个页面预览同一个 shared Timeline

- **WHEN** 两个作者页面预览同一个 shared TimelineAsset
- **THEN** 每个页面 MUST拥有自己的 preview session 时间、playback generation 和播放状态
- **AND** 一个页面的播放、暂停、seek 或关闭 MUST NOT改写 TimelineAsset 或另一个页面状态

#### Scenario: 预览 inline Timeline

- **WHEN** TimelineNode inline TimelineData 被预览
- **THEN** preview session MUST从 authoring data 创建独立工作副本
- **AND** Track runtime、TreeClip runtime 和当前 time MUST NOT写回 RootTree asset

### Requirement: 旧 TimelinePlayer 预览路径必须删除
系统 MUST 删除 BTSMTL Timeline 编辑器对 `TimelinePlayer` autonomous playback 的依赖。旧 `TimelinePlayer`、`Timeline.Bind(TimelinePlayer)`、`Timeline.Unbind()`、`Timeline.TimelinePlayer` 和依赖这些字段的编辑器调用 MUST 删除或迁移到正式 preview session。系统 MUST NOT 保留兼容分支继续支持旧播放器预览。

#### Scenario: 搜索旧播放器入口
- **WHEN** 实现完成后搜索 Timeline 编辑器代码
- **THEN** 不应存在 `typeof(TimelinePlayer)`、`Timeline.TimelinePlayer`、`TimelinePlayer.RunningTimelines` 或 `TimelinePlayer.IsPlaying` 作为预览入口
- **AND** Timeline 编辑器的播放入口 MUST 指向 `TimelinePreviewSession`

### Requirement: Timeline preview session 必须隔离动画生命周期状态

每个`TimelinePreviewSession` MUST拥有独立session identity、非零ActionInstance、playback generation、Action command inbox、session-local `CharacterActionPlaybackRuntime`、匹配Projection的AnimationSlot/Pose Plan workspace、Animancer source backend与snapshot。它 MUST不读取角色runtime Action lifecycle、不与其它窗口共享command batch/state，也 MUST不把lifecycle、Slot、Player或Pose Graph状态写入Timeline asset。

#### Scenario: 两个 Preview 窗口

- **WHEN** 两个窗口预览同一 Timeline
- **THEN** 两个session MUST拥有独立playback generation、queue、channel lifecycle、Player、source与Pose Plan workspace

#### Scenario: 两个 Preview session 绑定同一物理目标

- **WHEN** 两个 Preview session 尝试同时绑定同一个 CharacterPipelineHost 与 AnimancerComponent
- **THEN** 目标 MUST明确拒绝第二个 session
- **AND** 系统 MUST不让两个 session 共享、重复推进或竞争同一 Animancer Graph 输出
- **AND** 两个页面 MAY通过不同 Preview target 分别建立完整动画预览

#### Scenario: 切换 target

- **WHEN** session 切换 Preview target
- **THEN** 旧target queue、channel lifecycle、Player、source与Pose Plan workspace MUST清理
- **AND** 新 target MUST使用新 session identity

#### Scenario: Dispose

- **WHEN** Preview stop 或 dispose
- **THEN** pending commands、每channel Lifecycle、Player、source、Pose Plan workspace与native state MUST释放
- **AND** Timeline asset MUST不保存 runtime state

### Requirement: Timeline Preview 必须按正式阶段展示 TreeClip

Timeline Editor MUST显示 TreeClip的 Decision/Commit阶段、inline/shared ownership和 Blackboard输出摘要。Authoring Preview MUST NOT执行 TreeClip、Program operation、SimulationKernel、Action、Blackboard、GameplayEffect、Motion 或 WorldSolver。TreeClip 的真实 Decision/Commit、输出与终止事实 MUST只由正式运行 Session产生，并通过 Live Debug显示。系统 MUST NOT创建 Preview Simulation Session、临时 `CharacterGraphContext`、`TimelineRunningTree` clone、写入 authoring默认值或形成第二套 TreeClip执行语义。

#### Scenario: Authoring Preview 打开含 TreeClip 的 Timeline

- **WHEN** 用户选择 Authoring Preview
- **THEN** TimelineEditor MUST使用显式 preview target、preview time 与独立 animation lifecycle
- **AND** MUST不创建 Simulation Session、输入、logic Tick、Action target或WorldSolver
- **AND** UI MUST不把结果标记为真实 gameplay runtime

#### Scenario: Preview target 缺少正式上下文

- **WHEN** 作者打开含 TreeClip 的 Timeline 但没有绑定完整 preview target
- **THEN** Timeline Editor MUST 继续显示 Clip、阶段、Graph 和声明摘要
- **AND** Preview MUST 不执行 TreeClip
- **AND** 系统 MUST NOT 创建 fallback context 或解释器路径

#### Scenario: 只预览动画资源

- **WHEN** 作者只请求纯表现动画采样且不执行 TreeClip Gameplay
- **THEN** Timeline Editor MAY 使用 CharacterPresentationProjection 采样表现资源
- **AND** MUST 不产生 Motion、Window、Blackboard、Action 或 GameplayEffect 事实

### Requirement: Timeline、Track 和 Clip 必须拥有稳定 authoring identity

`TimelineData`、每个 Track 和每个 Clip MUST 持有稳定 authoring identity。authoring 重排 MUST 保持 identity，复制 Track/Clip MUST 生成新 identity，Program operation、Projection producer 与 Debug Source Map MUST保留对应 source identity。TrackIndex 和 ClipIndex MUST NOT 作为 Debug Source Map 的 source identity。

#### Scenario: 重排 Track

- **WHEN** 作者调整 Timeline Track 顺序
- **THEN** Track 和其 Clip authoring identity MUST 保持
- **AND** runtime debug source mapping MUST 不因 index 变化指向其它 Track

#### Scenario: 复制 Clip

- **WHEN** 作者复制一个 Clip
- **THEN** 新 Clip MUST 获得新 authoring identity
- **AND** 原 Clip identity MUST 保持

#### Scenario: 编译 Timeline

- **WHEN** Compiler 从 TimelineData 生成 Program operation 与 Projection producer
- **THEN** Timeline、Track 和 Clip authoring identity MUST进入正式 Source Map
- **AND** runtime activation、EventId、cycle 和 playback generation MUST独立生成

### Requirement: Timeline Editor 必须分离 Authoring Preview 与 Live Debug

`TimelineEditorWindow` MUST 提供语义明确且互斥的 Authoring Preview 与 Live Debug 模式。Authoring Preview MUST 继续由 `TimelinePreviewSession` 驱动；Live Debug MUST 由 `RuntimeDebugSession` 的共享增量 provider current state 或显式 Capture history 和 Timeline 窗口本地 runtime binding 观察真实 Program/Session trace，不得调用 preview evaluator、修改 runtime playback 或改写其它 Graph / Timeline 窗口的 binding。

#### Scenario: Authoring Preview

- **WHEN** 用户选择 Authoring Preview
- **THEN** TimelineEditor MUST 使用显式 preview target、preview time 和 preview lifecycle
- **AND** UI MUST 不把结果标记为真实 gameplay runtime

#### Scenario: Live Debug

- **WHEN** 用户选择 Live Debug
- **THEN** TimelineEditor MUST 以当前 Timeline identity/content hash 请求正式 target 解析
- **AND** 成功附着时 MUST 使用该窗口本地 binding 观察真实 playback
- **AND** Timeline 编辑内容 MUST 只读
- **AND** `TimelinePreviewSession` MUST 不参与该模式

#### Scenario: Play Mode domain reload 保持 Live Debug

- **WHEN** TimelineEditorWindow 在 Live Debug 下经历 Play Mode domain reload
- **THEN** 窗口 MUST 从已序列化 Timeline owner/path 恢复相同 authoring Timeline 与 Live Debug mode
- **AND** MUST 创建新的本地 runtime binding 并重新解析共享 Session
- **AND** locator 无效时 MUST 停止恢复，不得改用 Authoring Preview 或猜测其它 Timeline

### Requirement: Timeline Live Debug 必须显示真实 runtime membership

Timeline Live Debug MUST从共享provider的current Action playback summary显示当前playback instance/generation、发起Graph/Node source、ActionInstance、active Track/Clip、TreeClip phase/runtime、Action Selection、PendingFirstSample/Selected/Retained/Retired、AnimationSlot/PoseNode identity与terminal state。PoseState source usage属于Pose Graph Live Debug；Timeline MAY提供只读导航但不得伪装成Timeline playback。停止Capture后，它 MUST在共享Capture history position显示对应历史事实，不得根据当前authoring time重新采样来猜测membership。

#### Scenario: Decision TreeClip active

- **WHEN** Program 在某 SimulationTick 评估 Decision TreeClip operation
- **THEN** Timeline Live Debug MUST在对应 Clip 上显示该 tick 的 Decision evaluation
- **AND** UI MUST能关联写入的 Blackboard declaration identity

#### Scenario: visual time 位于两个 logic tick 之间

- **WHEN** PresentationFrame 以 interpolation alpha 计算 visual Timeline time
- **THEN** Timeline Live Debug MUST分别显示 logic time 与 visual time
- **AND** animation playhead MUST使用 visual time
- **AND** gameplay window/TreeClip decision 标记 MUST使用 logic tick

#### Scenario: 多个 playback 使用同一 Timeline source

- **WHEN** 同一 Timeline source 同时存在多个 playback instances
- **THEN** Timeline Editor MUST 为每个 playback 显示 playback id、来源 Graph / Node、activation context 与 terminal / lifecycle 摘要
- **AND** Timeline 窗口 MUST 要求作者在本地 binding 中 Pin 其中一个，或显式保持 Follow
- **AND** 系统 MUST NOT 按列表顺序静默选择赢家

#### Scenario: 当前 Timeline 未执行

- **WHEN** 已附着 target 的共享 current state 不包含当前 Timeline 的 playback
- **THEN** Timeline Editor MUST 显示当前角色未执行该 Timeline 的状态
- **AND** MUST NOT 调用 TimelinePreviewSession、preview evaluator 或 authoring time 重采样

### Requirement: 预览采样必须复用正式动画Selection与Pose Plan

有限Action Timeline Authoring Preview MUST把当前Track/Clip时间降低为正式Action Selection与Parameter page，并通过session-local Action command inbox执行匹配Projection的Action Playback Input、AnimationSlot、Transition Routing、Player和`CharacterPresentationPosePlan`。持续Locomotion Pose source、PoseStateMachine与Transition Rule预览 MUST属于Pose Graph Workspace，Timeline Preview不得伪造BaseLocomotion Timeline或Presentation Fact。具备正式Body与PhysicsScene上下文时 MAY执行FootPlacement，否则 MUST标记world-aware阶段Unavailable。Preview MUST不创建隐藏MarkerSync、固定per-slot Stack、隐藏Inertialization、简化PoseGraph、Animancer direct Play、假Foot Physics或自动全局平滑。

#### Scenario: 当前时间采样

- **WHEN** 作者把Preview游标移动到Attack clip中间
- **THEN** Preview MUST生成对应Attack Selection并送入FullBodyAction Action Playback Input与AnimationSlot
- **AND** 最终路径 MUST与正式Pose Plan一致

#### Scenario: 尝试预览Walk到Run

- **WHEN** 作者需要预览Locomotion PoseState从Walk到Run
- **THEN** Timeline Editor MUST导航到Pose Graph Workspace
- **AND** MUST不创建临时Walk/Run Timeline preview command

#### Scenario: 非连续seek

- **WHEN** 作者从一个producer非连续seek到另一个producer
- **THEN** AnimationSlot MUST按正式Action seek/reset policy重建source usage
- **AND** 连接Inertialization时 MUST按正式seek/reset policy处理history与residual
- **AND** 连接BlendStack时 MUST按正式node reset/seek policy处理而不创建额外fade

#### Scenario: Preview非连续拖动时间

- **WHEN** 作者把预览时间从一个不连续位置跳到另一个位置
- **THEN** Preview MUST重置Inertialization history
- **AND** MUST不把seek解释为可惯性化的连续切换

### Requirement: Timeline Editor 必须编辑 AnimationTrack Marker Sync

Timeline Editor MUST在AnimationTrack Inspector与同一track时间轴中编辑SyncMode、SyncGroupId、Finite/Cyclic topology、SyncRole和marker。每个AnimationTrack MUST拥有一个固定存在、可折叠的`SYNC MARKERS`子轨；该子轨 MUST只是父Track作者数据的编辑投影，不得加入`TimelineData.Tracks`、获得独立AuthoringId、接受Clip或执行Tick。折叠状态 MUST只改变显示高度并保留group、topology、role和marker数量摘要。`None`子轨 MUST显示禁用摘要，`MarkerGroup`子轨 MUST按稳定MarkerAuthoringId显示、选择和拖动Point Marker，并按整数Timeline frame吸附。

作者 MUST能在子轨空白帧通过右键菜单新增Marker。菜单 MUST从当前正式Definition authoring context内同AnimationChannelId、同显式MarkerSync可达集合、同canonical SyncGroup的AnimationTrack动态投影已使用MarkerId候选，并 MUST允许显式输入新的合法MarkerId。候选索引 MUST只读且不得序列化为全局catalog、Profile或Track副本。Marker右键菜单 MUST提供选择、定位、重命名与删除；Inspector MUST继续提供精确MarkerId和frame输入。新增、重命名、移动、删除与模式切换 MUST通过Timeline正式authoring API进入Undo、dirty、identity、唯一校验、RebindTimeline和Authoring Preview刷新链，不得使用YAML、SerializedProperty任意写入或独立FootPhase资产。

#### Scenario: 拖动一个marker

- **WHEN** 作者在AnimationTrack marker lane拖动RightPlant
- **THEN** 编辑器 MUST保持该marker的AuthoringId
- **AND** pointer capture期间 MUST只更新本地整数frame预览
- **AND** 释放或意外失去capture时 MUST以一个Undo事务提交最后frame并触发正式validation、Projection stale状态与Preview刷新
- **AND** Pointer Cancel MUST恢复原frame且不得写入资产

#### Scenario: 在空白帧新增同组marker

- **WHEN** 作者在Attack AnimationTrack的SYNC MARKERS子轨空白帧打开右键菜单
- **THEN** 菜单 MUST显示同Action AnimationChannelId、同Slot可达集合其它有限Action Track已经使用的MarkerId候选
- **AND** 选择候选 MUST通过正式authoring API在当前frame创建具有新AuthoringId的Marker

#### Scenario: 为组创建首个marker名称

- **WHEN** 当前Sync Group尚无MarkerId候选
- **THEN** 作者 MUST能显式输入新的合法MarkerId
- **AND** Editor MUST不创建独立Marker catalog作为先决条件

#### Scenario: 删除marker点

- **WHEN** 作者右键一个Marker并选择删除
- **THEN** Editor MUST按MarkerAuthoringId调用正式删除API
- **AND** Timeline、Inspector、pair coverage与Preview MUST在同一提交后刷新

#### Scenario: 查看循环闭合

- **WHEN** 作者展开Cyclic MarkerGroup子轨
- **THEN** 子轨 MUST明确显示末Marker到下一周期首Marker的有向闭合关系
- **AND** Preview游标 MUST突出当前有向Marker Pair与fraction
- **AND** Finite子轨 MUST不显示回绕

#### Scenario: 切换为None

- **WHEN** 作者把MarkerGroup track切换为None
- **THEN** authoring API MUST原子清空group、topology、SyncRole和markers
- **AND** Undo MUST能恢复完整旧配置

#### Scenario: shared producer调用拓扑冲突

- **WHEN** 当前shared AnimationTrack同时被Once与Loop节点调用
- **THEN** Timeline Editor MUST显示全部冲突call site的stable identity与来源定位
- **AND** MUST不提供call site override作为修复

#### Scenario: None动画轨仍显示固定子轨

- **WHEN** 作者打开SyncMode为None的AnimationTrack
- **THEN** clip row下方 MUST显示固定Marker Sync子轨和None摘要
- **AND** 子轨 MUST不创建Timeline运行时Track

#### Scenario: 折叠Marker子轨

- **WHEN** 作者折叠SYNC MARKERS子轨
- **THEN** Track Handle MUST保留SyncMode、Group、Topology、Role和Marker数量摘要
- **AND** 折叠 MUST不修改Marker数据、Track组合顺序或运行时Projection

#### Scenario: Track重排保持组合行

- **WHEN** 作者拖动重排带Marker Sync子轨的AnimationTrack
- **THEN** clip row、Marker子轨和左侧Track Handle MUST作为一个组合行移动
- **AND** 不得与相邻Track重叠

#### Scenario: Marker与Foot Placement曲线同时存在

- **WHEN** AnimationTrack同时启用MarkerGroup且Clip拥有Foot Placement Weight曲线
- **THEN** Marker MUST只显示在SYNC MARKERS子轨
- **AND** Foot Placement Weight MUST只显示在独立CURVES子轨
- **AND** 两者 MUST不共享key、contact、phase或运行时状态

### Requirement: Authoring Preview 必须复用正式 Marker Sync 表现链

有限Action Timeline Authoring Preview MUST通过CharacterPresentationProjection解析producer marker binding、Action AnimationChannelId与Slot/Player PoseNodeId，并复用session-local `CharacterActionPlaybackRuntime`、AnimationSlot、source backend与Pose Plan。单producer预览 MUST从正式relation snapshot显示raw/effective time和当前marker segment。持续Locomotion Sequence/BlendSpace source marker预览 MUST由Pose Graph Workspace从Profile binding执行，Timeline Editor不得复制或编辑这些marker。

#### Scenario: 单producer预览marker

- **WHEN** 作者拖动一个MarkerGroup AnimationTrack的Timeline游标
- **THEN** Preview MUST显示该时间所在的marker pair与segment fraction
- **AND** 在没有source handoff时effective time MUST等于raw time

#### Scenario: 比较Locomotion Walk与Run handoff

- **WHEN** 作者选择Walk与Run Presentation Pose source
- **THEN** Pose Graph Workspace MUST通过正式source-local marker映射与PoseState transition执行比较
- **AND** Timeline Preview MUST不生成BaseLocomotion Selection

#### Scenario: Preview包含TreeClip或MotionWarp

- **WHEN** 当前Timeline还包含TreeClip、MotionCurve或MotionWarp
- **THEN** Authoring Preview MUST只显示并编辑这些track
- **AND** MUST不创建Simulation Source、Pipeline、WorldSolver或Action target输入来执行它们

### Requirement: Timeline Live Debug 必须显示正式 Sync Relation

Timeline Live Debug MUST从共享RuntimeDebugSession的正式Animation trace显示有限Action source/target PlaybackId、AnimationChannelId、Slot/Player PoseNodeId、canonical SyncGroupId、有向marker pair、source fraction、target occurrence、raw/effective time、effective cycle、relation depth、lifecycle phase与detach/failure reason。PoseState Source Sync relation MUST在Pose Graph Live Debug显示PoseState、transition generation与Presentation source usage；Timeline Live Debug MAY提供只读跨工作区导航，但 MUST不把Pose relation伪装为Timeline playback。两者 MUST不按authoring游标重新采样、不推导State transition、不求值Pose Graph、不从Animancer weight重建贡献或维护第二份relation状态。

#### Scenario: 观察连续切换

- **WHEN** runtime发生`Attack1 -> Attack2 -> Dodge`且存在Action relation chain
- **THEN** Live Debug MUST按playback generation显示每条source-target relation与depth
- **AND** 显示值 MUST来自当帧正式runtime snapshot

#### Scenario: source退休

- **WHEN** source fade完成并触发target continuation rebase
- **THEN** Live Debug MUST显示`SourceRetiredRebased`及最后raw/effective anchor
- **AND** MUST不把该事件显示为Gameplay State transition

#### Scenario: 观察Walk到Run

- **WHEN** runtime发生PoseState Source Sync
- **THEN** Timeline Live Debug MUST提供Open Pose Graph Live导航
- **AND** MUST不生成虚假AnimationPlaybackId

#### Scenario: target显式None

- **WHEN** incoming target未参与Marker Sync
- **THEN** Live Debug MUST显示`TargetExplicitNone`
- **AND** MUST继续显示target原始Timeline采样与普通Animancer lifecycle

### Requirement: Timeline Live Debug 必须显示 MotionWarp 正式运行事实

Live Debug MUST从正式runtime trace显示MotionWarp window首尾、source MotionCurve首尾与当前累计pose、ActionInstance、target snapshot、Translation/Rotation mode、未限制Target Pose、有效Target Pose、Limit结果、position/yaw progress、previous/current Warped Cumulative Pose、当前correction、final Action channel request和actual solver result。Live Debug MUST不重新计算Warp，也 MUST不读取mutable accumulator或scene target。

#### Scenario: Warp 请求被墙阻挡

- **WHEN** Live Debug观察到Warp final request与actual result不同
- **THEN** UI MUST同时显示请求修正和Solver实际结果
- **AND** 作者 MUST能区分clamp与collision造成的未到达

### Requirement: Timeline Editor 必须按时间语义抽象作者内容

Timeline Editor MUST将作者内容明确分为占据起止区间的`Span Clip`、位于单一整数帧的`Point Marker`和沿时间连续求值的`Continuous Curve`。三类内容 MUST共享Timeline frame geometry、选择、帧吸附、pointer capture、本地草稿、单次Undo提交和提交后刷新合同，但 MUST继续使用各自正式数据所有权与authoring API。该抽象 MUST只属于Editor交互层，不得创建统一宽序列化DTO、新Runtime Track、第二份`TimelineData`或运行时按类型反射分派。

#### Scenario: 同一AnimationTrack展开全部作者内容

- **WHEN** 作者展开同时包含Animation Clip、Marker Sync和Foot Placement Weight的AnimationTrack
- **THEN** Clip MUST显示在Span Clip行，Sync Marker MUST显示为Point Marker，Foot Placement Weight MUST显示在Continuous Curve行
- **AND** 三类内容 MUST使用同一时间轴缩放与滚动坐标
- **AND** Marker MUST不以曲线key或连续phase值显示

#### Scenario: 编辑器提交一次拖动

- **WHEN** 作者拖动Point Marker或Continuous Curve key并释放指针
- **THEN** pointer capture期间 MUST只更新当前元素的本地草稿
- **AND** Pointer Up或意外Capture Out MUST只产生一个正式Undo事务
- **AND** Pointer Cancel MUST丢弃草稿且不得修改资产

#### Scenario: Editor抽象进入Runtime

- **WHEN** Timeline编译或运行
- **THEN** Runtime MUST继续消费现有Track、Clip、Projection和Program合同
- **AND** MUST不存在Span、Point、Curve统一运行时解释器或第二条Tick路径

### Requirement: Timeline Editor 必须完整编辑显式注册的 Continuous Curve Channel

Timeline Editor MUST通过显式typed Curve Channel Catalog显示和编辑Timeline Clip已经正式拥有的Continuous Curve。每个descriptor MUST声明稳定ChannelId、owner类型、显示名、颜色、time domain、value domain、单位、完整curve读取、正式owner mutation和领域validator。Catalog MUST首批覆盖Animation Clip的Weight、Ease In、Ease Out和Foot Placement Weight，MotionCurve Clip的Weight、Position X/Y/Z、Yaw和Ease In/Out，MotionWarp Clip的Position Progress与Yaw Progress，以及CameraStateClip与CameraResponseClip的Weight和Ease In/Out。Editor MUST不通过反射、字段名、SerializedProperty path或任意字符串发现和修改curve。

每个具有registered channel的Track MUST显示可折叠`CURVES`分组，展开后每个ChannelId拥有独立lane。每个Clip MUST只在自己的StartFrame..EndFrame范围显示自己的curve、key与边界；重叠Clip MUST不在作者层合并curve。Curve Lane MUST按完整`AnimationCurve.Evaluate`结果绘制插值，显示原始key、tangent handle、当前游标time/value、value reference与单位。Bounded channel MUST使用声明范围；unbounded channel MUST提供独立vertical fit、scale和zero line，不得Clamp到`[0,1]`。

#### Scenario: 展开Animation Track曲线

- **WHEN** 作者展开包含Animation Clip的CURVES分组
- **THEN** Editor MUST显示Weight、Ease In、Ease Out与Foot Placement Weight四个typed channel
- **AND** Foot Placement Weight MUST只是其中一个channel，不得拥有专用Curve View实现
- **AND** Sync Marker MUST继续只显示在独立SYNC MARKERS子轨

#### Scenario: 展开MotionCurve Track曲线

- **WHEN** 作者展开MotionCurve Clip的CURVES分组
- **THEN** Editor MUST显示Weight、Position X/Y/Z、Yaw与Ease In/Out
- **AND** Position与Yaw MUST使用unbounded value view及明确单位
- **AND** Editor MUST不把它们Clamp到权重范围

#### Scenario: 展开MotionWarp与Camera曲线

- **WHEN** 作者展开MotionWarp或Camera Clip的CURVES分组
- **THEN** Editor MUST只显示Catalog为该owner登记的channel
- **AND** MotionWarp progress MUST继续接受其单调与端点领域校验
- **AND** Camera曲线 MUST继续由Camera领域消费者拥有运行语义

#### Scenario: 绘制实际插值

- **WHEN** channel包含非线性或weighted tangent key
- **THEN** Curve Lane MUST按完整AnimationCurve插值绘制
- **AND** MUST显示原始key与tangent handle
- **AND** MUST不使用key之间的直线替代实际曲线

### Requirement: Curve Key编辑必须无损且原子

Curve Lane MUST支持单选、Shift追加、框选、双击或右键新增、一个或多个key拖动、Delete或右键删除、复制粘贴、数值Inspector以及Auto、Clamped Auto、Linear、Constant、Free和Weighted tangent编辑。横轴 MUST通过descriptor在Timeline frame与curve local time之间映射，并按整数Timeline frame吸附；纵轴 MUST按typed value domain处理。一次手势或Inspector提交 MUST只修改本地完整curve草稿并通过descriptor MutationAdapter生成一个Undo事务。Pointer Cancel MUST丢弃草稿；Pointer Up或意外Capture Out MUST提交最后草稿。提交后 MUST重新读取owner并刷新Timeline、Inspector、领域validation、Projection stale状态和可用Authoring Preview。

Curve mutation MUST原子保存pre/post wrap mode及每个key的time、value、in/out tangent、in/out weight和WeightedMode。Curve key不获得持久AuthoringId；Editor MAY在当前owner revision内使用临时key index选择，Agent与持久Patch MUST以`OwnerAuthoringId + ChannelId + Full Curve`替换完整channel，不得按key index跨revision修改。

#### Scenario: 拖动多个curve key

- **WHEN** 作者框选多个key并拖动
- **THEN** pointer capture期间 MUST只更新本地curve草稿
- **AND** 所有key MUST按相同Timeline frame delta和值delta移动并保持合法顺序
- **AND** 释放时 MUST只产生一个Undo事务

#### Scenario: 精确编辑weighted tangent

- **WHEN** 作者在Inspector修改一个key的in/out tangent、weight与WeightedMode
- **THEN** MutationAdapter MUST原子保存完整Keyframe字段
- **AND** 未修改的key与wrap mode MUST无损保留

#### Scenario: 复制到不兼容channel

- **WHEN** 作者把unbounded Position key粘贴到bounded Weight channel
- **THEN** Editor MUST拒绝该操作并说明time/value domain不兼容
- **AND** MUST不Clamp、不换算单位也不部分写入

#### Scenario: 外部修改使key选择过期

- **WHEN** owner curve revision在编辑手势外被Agent或其它正式入口替换
- **THEN** Editor MUST使临时key选择失效并重新读取完整curve
- **AND** MUST不按旧key index写入新revision

### Requirement: Curve Editor必须保持领域运行链唯一

通用Curve Editor MUST只提供作者投影与正式mutation，不得创建`GenericTimelineCurveRuntime`。Animation控制曲线 MUST继续进入CharacterPresentationProjection与现有Presentation consumer；MotionCurve和MotionWarp曲线 MUST继续进入Semantic IR、Numeric Program与各自evaluator/modifier；Camera曲线 MUST继续进入既有Camera compile/presentation链。RootMotionCurveAsset、导入AnimationClip骨骼/BlendShape/属性曲线和没有正式consumer的任意Float Curve MUST不进入Timeline Curve Channel Catalog。

#### Scenario: 编辑MotionWarp progress

- **WHEN** 作者修改MotionWarp Position Progress channel
- **THEN** Timeline只通过MotionWarpClip正式mutation保存curve
- **AND** 后续Compiler MUST沿既有MotionWarp semantic operation编译
- **AND** Editor MUST不直接执行MotionWarp或创建第二runtime sampler

#### Scenario: 请求任意自定义curve

- **WHEN** 某功能只提供显示名但没有registered ChannelId、owner mutation、validator或runtime consumer
- **THEN** Curve Editor MUST拒绝创建该channel
- **AND** MUST不保存未知AnimationCurve字段或fallback数据

### Requirement: Timeline Field内部交互、几何与渲染必须分属明确模块

Timeline Editor MUST保留现有TimelineEditorWindow、TimelineField、Inspector和Authoring Preview/Live Debug入口，但selection/drag/move/resize交互状态、time/frame/clip geometry与hit-test、track/clip/playhead/overlay rendering、preview/live binding MUST由职责独立的内部模块拥有。selection MUST只读暴露，外部 MUST通过selection命令修改；interaction MUST只依赖窄host port，不得持有完整TimelineField；rendering MUST显式消费frame range、viewport、playhead或overlay输入，不得反向读取完整TimelineField。interaction模块 MUST通过唯一authoring mutation/Undo入口修改Timeline；geometry与rendering MUST是输入驱动且不得写asset；preview/live binding MUST继续由窗口本地session adapter拥有。拆分 MUST不改变Timeline/Track/Clip identity、Source Map、右侧Inspector selection或双窗口页签行为。

#### Scenario: Resize一个Animation Clip

- **WHEN** 作者拖动Clip边缘改变范围
- **THEN** interaction模块 MUST使用geometry模块的frame结果创建唯一mutation
- **AND** mutation MUST在一个Undo边界更新原Clip identity对应的数据
- **AND** rendering模块 MUST只根据新数据重绘

#### Scenario: 点击右侧Inspector设置

- **WHEN** 作者选择Clip后操作右侧Inspector字段
- **THEN** selection owner MUST在字段提交期间保持同一Clip authoring identity
- **AND** TimelineField重绘 MUST不把selection清空或切换到其它Clip

#### Scenario: Authoring Preview切换Live Debug

- **WHEN** TimelineEditor从Authoring Preview切换到Live Debug
- **THEN** window/session adapter MUST停止preview binding并建立该窗口本地runtime binding
- **AND** interaction模块 MUST进入只读状态
- **AND** geometry与rendering模块 MUST复用同一authoring Timeline identity显示真实overlay

#### Scenario: 多个playback overlay

- **WHEN** 同一Timeline source存在多个runtime playback
- **THEN** runtime overlay模块 MUST呈现各playback identity并服从Follow/Pin选择
- **AND** rendering模块 MUST不按列表顺序静默选择赢家或调用preview evaluator
