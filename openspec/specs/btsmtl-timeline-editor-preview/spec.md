# btsmtl-timeline-editor-preview Specification

## Purpose
定义BTSMTL有限Action Timeline编辑器预览的正式链路：`TimelinePreviewSession`通过typed Action adapter接入唯一`AnimationPreviewRuntime`，复用Action lifecycle、AnimationSlot、Transition Routing、source backend与Pose Plan；持续Locomotion由Pose Graph Fact Preview负责，不恢复旧`TimelinePlayer`、BaseLocomotion Timeline、共享Playback总管或独立PlayableGraph。
## Requirements

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

有限Action Timeline Authoring Preview MUST把当前Track/Clip时间降低为正式Action Selection与Parameter page，并通过session-local Action command inbox执行匹配Projection的Action Playback Input、AnimationSlot、Transition Routing、Player和`CharacterPresentationPosePlan`。持续Locomotion Pose source、PoseStateMachine与Transition Rule预览 MUST属于Pose Graph Workspace，Timeline Preview不得伪造BaseLocomotion Timeline或Presentation Fact。具备正式Body与PhysicsScene上下文时 MAY执行FootPlacement，否则 MUST标记world-aware阶段Unavailable。Preview MUST不创建隐藏素材同步节点、固定per-slot Stack、隐藏Inertialization、简化PoseGraph、Animancer direct Play、假Foot Physics或自动全局平滑。

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

### Requirement: Timeline Live Debug 必须显示 MotionWarp 正式运行事实

Live Debug MUST从正式runtime trace显示MotionWarp window首尾、source MotionCurve首尾与当前累计pose、ActionInstance、target snapshot、Translation/Rotation mode、未限制Target Pose、有效Target Pose、Limit结果、position/yaw progress、previous/current Warped Cumulative Pose、当前correction、final Action channel request和actual solver result。Live Debug MUST不重新计算Warp，也 MUST不读取mutable accumulator或scene target。

#### Scenario: Warp 请求被墙阻挡

- **WHEN** Live Debug观察到Warp final request与actual result不同
- **THEN** UI MUST同时显示请求修正和Solver实际结果
- **AND** 作者 MUST能区分clamp与collision造成的未到达

### Requirement: Timeline Editor 必须按时间语义抽象作者内容

Timeline Editor MUST将本地作者内容明确分为占据起止区间的`Span Clip`和沿时间连续求值的`Continuous Curve`。两类内容 MUST共享Timeline frame geometry、选择、帧吸附、pointer capture、本地草稿、单次Undo提交和提交后刷新合同，但 MUST继续使用各自正式数据所有权与authoring API。该抽象 MUST只属于Editor交互层，不得创建统一宽序列化DTO、新Runtime Track、第二份`TimelineData`或运行时按类型反射分派。AnimationClip注册Curve与Locomotion Phase MUST不进入该抽象。

#### Scenario: 同一AnimationTrack展开作者内容

- **WHEN** 作者展开包含Animation Segment、Weight和Ease的AnimationTrack
- **THEN** Segment MUST显示在Span Clip行，Weight与Ease MUST显示在Continuous Curve行
- **AND** Foot Placement Weight与Locomotion Phase MUST不显示为Timeline lane

#### Scenario: 编辑器提交一次拖动

- **WHEN** 作者拖动Continuous Curve key并释放指针
- **THEN** pointer capture期间 MUST只更新当前元素的本地草稿
- **AND** Pointer Up或意外Capture Out MUST只产生一个正式Undo事务
- **AND** Pointer Cancel MUST丢弃草稿且不得修改资产

### Requirement: Timeline Editor 必须完整编辑显式注册的 Continuous Curve Channel

Timeline Editor MUST通过显式typed Curve Channel Catalog显示和编辑Timeline owner已经正式拥有的Continuous Curve。每个descriptor MUST声明稳定ChannelId、owner类型、显示名、颜色、time domain、value domain、单位、完整curve读取、正式owner mutation和领域validator。Catalog MUST覆盖Animation Segment的Weight、Ease In和Ease Out，MotionCurve Clip的Weight、Position X/Y/Z、Yaw和Ease In/Out，MotionWarp Clip的Position Progress与Yaw Progress，以及CameraStateClip与CameraResponseClip的Weight和Ease In/Out。`presentation.locomotion-phase`、`presentation.foot-placement-weight`、AnimationClip骨骼曲线和其它Clip内注册Curve MUST只由Unity Animation Window编辑，不得进入Timeline Catalog。

每个具有registered Timeline channel的Track MUST显示可折叠`CURVES`分组，展开后每个ChannelId拥有独立lane。每个Clip或Segment MUST只在自己的StartFrame..EndFrame范围显示自己的Timeline-local curve、key与边界；重叠内容 MUST不在作者层合并curve。Curve Lane MUST按完整`AnimationCurve.Evaluate`结果绘制插值，显示原始key、tangent handle、当前游标time/value、value reference与单位。

#### Scenario: 展开Animation Track曲线

- **WHEN** 作者展开包含Animation Segment的CURVES分组
- **THEN** Editor MUST只显示Weight、Ease In与Ease Out三个Timeline-local typed channel
- **AND** Foot Placement Weight与Locomotion Phase MUST不出现在该分组

#### Scenario: 展开MotionCurve Track曲线

- **WHEN** 作者展开MotionCurve Clip的CURVES分组
- **THEN** Editor MUST显示Weight、Position X/Y/Z、Yaw与Ease In/Out
- **AND** Position与Yaw MUST使用unbounded value view及明确单位

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

Timeline Curve Editor MUST只提供Timeline-local作者投影与正式mutation，不得创建`GenericTimelineCurveRuntime`。Animation Segment Weight/Ease MUST继续进入Action Presentation计划；MotionCurve和MotionWarp曲线 MUST继续进入Semantic IR、Numeric Program与各自evaluator/modifier；Camera曲线 MUST继续进入既有Camera compile/presentation链。AnimationClip注册表现Curve MUST由Clip Curve catalog、Animation Window入口与Character Presentation Projection链拥有，MUST不经过Timeline Curve MutationAdapter。RootMotionCurveAsset、导入AnimationClip骨骼/BlendShape/属性曲线和没有正式consumer的任意Float Curve MUST不进入Timeline Curve Channel Catalog。

#### Scenario: 编辑MotionWarp progress

- **WHEN** 作者修改MotionWarp Position Progress channel
- **THEN** Timeline只通过MotionWarpClip正式mutation保存curve
- **AND** 后续Compiler MUST沿既有MotionWarp semantic operation编译

#### Scenario: 请求Clip注册Curve

- **WHEN** Timeline Curve Editor收到`presentation.locomotion-phase`或`presentation.foot-placement-weight`
- **THEN** Catalog MUST拒绝该channel并提供Open Animation Clip导航
- **AND** MUST不在Segment或Timeline创建Curve副本

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

### Requirement: Timeline Editor必须只编辑Action Timeline并导航直接AnimationClip

Timeline Editor MUST只拥有有限Action的Track、Segment、Window、Motion、MotionWarp、Decision、Cue与Timeline-local Curve。Animation Segment MUST直接引用精确AnimationClip并保存Start/End、ClipIn、Extrapolation、Weight与Ease。作者双击Segment或执行Open Clip时，Editor MUST通过精确Character Definition、Profile、Clip与Preview Target打开Unity Animation Window。Timeline Editor MUST不提供Sequence模式、素材Marker lane、素材Curve lane、Foot Analysis面板或Sequence Preview。

#### Scenario: 双击Attack Segment

- **WHEN** 作者双击Attack Timeline中的Animation Segment
- **THEN** 系统 MUST打开精确AnimationClip和正式Preview Target
- **AND** Timeline selection与AnimationClip Curve MUST继续由各自owner保存

#### Scenario: Timeline没有Character上下文

- **WHEN** 作者独立打开shared Timeline
- **THEN** Timeline本地Action编排 MUST保持可编辑
- **AND** Open Clip需要的Preview Target不可解析时 MUST显示typed Unavailable而不搜索任意Definition
