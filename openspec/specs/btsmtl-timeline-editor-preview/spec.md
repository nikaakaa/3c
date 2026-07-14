# btsmtl-timeline-editor-preview Specification

## Purpose
定义 BTSMTL Timeline 编辑器预览的正式链路：编辑器预览由 `TimelinePreviewSession` 控制播放状态，预览目标通过 `TimelinePreviewTarget` 接入正式角色管线，复用 Timeline sampling、AnimationPlaybackLifecycle 和 AnimancerPlaybackAdapter，不恢复旧 `TimelinePlayer`、Registry/Arbitrator 或独立 PlayableGraph 预览权威。
## Requirements

### Requirement: Timeline 编辑器预览使用管线预览会话

系统 MUST 使用 editor-only TimelinePreviewSession 作为 TimelineEditorWindow 的播放、暂停、速度和游标预览控制器。TimelineEditorWindow MUST为当前绑定的 resolved TimelineData 建立唯一 preview session，并在窗口重绑或释放时正式释放旧 preview owner。TimelineEditorWindow MUST NOT直接控制 TimelinePlayer、PlayableGraph 或旧 Timeline autonomous playback。

#### Scenario: inline Timeline 窗口点击播放

- **WHEN** 用户从 TimelineNode 打开 inline Timeline 并点击播放
- **THEN** TimelineEditorWindow 的 TimelinePreviewSession MUST使用该节点的 resolved TimelineData clone
- **AND** session MUST NOT修改 TimelineNode 内的 authoring data
- **AND** page MUST NOT调用旧 TimelinePlayer

#### Scenario: shared Timeline root page 点击播放

- **WHEN** 用户直接打开 shared TimelineAsset 并点击播放
- **THEN** TimelinePreviewSession MUST使用 TimelineAsset.Data 的 runtime clone
- **AND** preview controls MUST与 inline TimelineEditorWindow 使用同一实现
- **AND** shared TimelineAsset MUST不保存 preview time 或 target

#### Scenario: TreeClip 跨窗口下钻

- **WHEN** 用户从 TimelineEditorWindow 打开 TreeClip Graph page或在 Graph 窗口返回
- **THEN** TimelineEditorWindow 的 preview session MUST保持归属当前 Timeline 窗口
- **AND** Graph 页面切换 MUST NOT创建、接管或释放 Timeline preview session

### Requirement: Timeline 编辑器预览目标来自正式管线预览目标

系统 MUST使用 `TimelinePreviewTarget` 作为 Timeline 编辑器可选择的预览目标抽象，并由 `CharacterPipelineHost` 或等价正式角色管线目标实现它。正式角色管线预览目标 MUST使用 `CharacterPipelineDefinition.AnimationPresentation`、其唯一 Animancer TransitionLibrary、producer bindings 与 `AnimancerComponent` 正式引用。系统 MUST不使用 `TimelinePlayer`、场景搜索、fallback target 或第二份 animation layer 配置作为预览目标。

#### Scenario: 选择预览目标

- **WHEN** 用户在 Timeline 编辑器 target field 选择场景对象
- **THEN** 可接受对象 MUST是 `TimelinePreviewTarget`
- **AND** 当前角色管线目标 MUST由 `CharacterPipelineHost` 实现
- **AND** `CharacterPipelineHost` MUST使用正式 `CharacterPipelineDefinition.AnimationPresentation`
- **AND** `CharacterPipelineHost` MUST使用正式 `AnimancerComponent` 应用动画预览

#### Scenario: 未选择预览目标

- **WHEN** Timeline 编辑器没有有效 `TimelinePreviewTarget`
- **THEN** 用户 MAY继续编辑 Timeline 数据
- **AND** 播放、暂停、速度和可应用预览 MUST处于禁用状态
- **AND** 系统 MUST不自动查找场景中的 Host 或 TimelinePlayer

### Requirement: Timeline 资产不保存编辑器播放状态

系统 MUST 将编辑器预览播放状态保存在 TimelinePreviewSession 中。Inline TimelineData、shared TimelineAsset 及其持有的 TimelineData MUST只保存 authoring 数据，不得保存当前预览目标、session identity、runtime clone、PlayableGraph 或预览播放状态。

#### Scenario: 两个页面预览同一个 shared Timeline

- **WHEN** 两个作者页面预览同一个 shared TimelineAsset
- **THEN** 每个页面 MUST拥有自己的 preview session 时间、runtime clone 和播放状态
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

每个 `TimelinePreviewSession` MUST拥有独立 session identity、playback generation、CharacterAnimationPlaybackCommandQueue、AnimationPlaybackLifecycle、Animancer preview states 与 snapshots。它 MUST不读取角色 runtime playback lifecycle、不与其它窗口共享 command batch/state，也 MUST不把 lifecycle 写入 Timeline asset。

#### Scenario: 两个 Preview 窗口

- **WHEN** 两个窗口预览同一 Timeline
- **THEN** 两个 session MUST拥有独立 playback generation、queue、lifecycle 与 Animancer state

#### Scenario: 两个 Preview session 绑定同一物理目标

- **WHEN** 两个 Preview session 尝试同时绑定同一个 CharacterPipelineHost 与 AnimancerComponent
- **THEN** 目标 MUST明确拒绝第二个 session
- **AND** 系统 MUST不让两个 session 共享、重复推进或竞争同一 Animancer Graph 输出
- **AND** 两个页面 MAY通过不同 Preview target 分别建立完整动画预览

#### Scenario: 切换 target

- **WHEN** session 切换 Preview target
- **THEN** 旧 target queue、lifecycle 与 Animancer state MUST清理
- **AND** 新 target MUST使用新 session identity

#### Scenario: Dispose

- **WHEN** Preview stop 或 dispose
- **THEN** pending commands、Pending/Current/Outgoing playback 与 native state MUST释放
- **AND** Timeline asset MUST不保存 runtime state

### Requirement: Timeline Preview 必须按正式阶段展示 TreeClip

Timeline Editor MUST 显示 TreeClip 的 Decision/Commit 阶段、inline/shared ownership 和 Blackboard 输出摘要。Preview 只有在正式 preview target 提供所需 Pipeline Context 时才 MAY 执行 TreeClip；缺少上下文时 MUST 显示不可执行状态。Preview MUST NOT 创建临时 CharacterGraphContext、写入 authoring 默认值或形成第二套 TreeClip Tick 权威。

#### Scenario: Preview target 提供 Pipeline Context

- **WHEN** Timeline Preview target 提供正式 Pipeline Blackboard 和 Graph runtime context
- **THEN** Preview MAY 按正式 Prepare/Commit 顺序执行 TreeClip
- **AND** Preview MUST 使用与 runtime 相同的阶段和节点能力校验

#### Scenario: Preview target 缺少 Pipeline Context

- **WHEN** 作者打开含 TreeClip 的 Timeline 但没有绑定正式 preview target
- **THEN** Timeline Editor MUST 继续显示 Clip、阶段、Graph 和声明摘要
- **AND** Preview MUST 不执行 TreeClip
- **AND** 系统 MUST NOT 创建 fallback context

### Requirement: Timeline、Track 和 Clip 必须拥有稳定 authoring identity

`TimelineData`、每个 Track 和每个 Clip MUST 持有稳定 authoring identity。authoring 重排 MUST 保持 identity，复制 Track/Clip MUST 生成新 identity，runtime clone MUST 保留 source identity。TrackIndex 和 ClipIndex MUST NOT 作为 Debug Source Map 的 source identity。

#### Scenario: 重排 Track

- **WHEN** 作者调整 Timeline Track 顺序
- **THEN** Track 和其 Clip authoring identity MUST 保持
- **AND** runtime debug source mapping MUST 不因 index 变化指向其它 Track

#### Scenario: 复制 Clip

- **WHEN** 作者复制一个 Clip
- **THEN** 新 Clip MUST 获得新 authoring identity
- **AND** 原 Clip identity MUST 保持

#### Scenario: runtime clone Timeline

- **WHEN** scheduler 从 TimelineData 创建 runtime clone
- **THEN** clone 中 Timeline、Track 和 Clip MUST 保留 authoring identity
- **AND** playback handle、cycle 和其它 runtime identity MUST 继续独立生成

### Requirement: Timeline Editor 必须分离 Authoring Preview 与 Live Debug

`TimelineEditorWindow` MUST 提供语义明确且互斥的 Authoring Preview 与 Live Debug 模式。Authoring Preview MUST 继续由 `TimelinePreviewSession` 驱动；Live Debug MUST 由 `RuntimeDebugSession` 的共享增量 provider current state 或显式 Capture history 和 Timeline 窗口本地 runtime binding 观察真实 scheduler，不得调用 preview evaluator、修改 runtime playback 或改写其它 Graph / Timeline 窗口的 binding。

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

Timeline Live Debug MUST 从共享 provider 的 current playback summary 显示当前 playback instance/generation、发起 Graph / Node source、可用的 activation context、active Track/Clip、TreeClip phase/runtime、AnimationProducerSample、PendingFirstSample/Current/Outgoing/Retired 与 terminal state。停止 Capture 后，它 MUST 在共享 Capture history position 显示对应历史事实。它 MUST 不根据当前 authoring time 重新采样来猜测 membership。

#### Scenario: Decision TreeClip active

- **WHEN** scheduler 在某 logic tick 评估 Decision TreeClip
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

### Requirement: 预览采样必须复用正式动画播放链路

Timeline Preview MUST复用正式 AnimationTrack sampling、CharacterAnimationPlaybackCommandQueue、AnimationPlaybackLifecycle 与 AnimancerPlaybackAdapter。每个 Timeline AnimationTrack MUST映射稳定 producer identity，preview session MUST为每层提交零或一个 AnimationLayerSelection，并使用匹配 generation 的 AnimationProducerSample。Preview MUST不直接播放 clip、不比较 Priority，也 MUST不实现第二套 layer mixing。

#### Scenario: 当前时间采样

- **WHEN** preview time 位于 AnimationTrack clip 范围
- **THEN** session MUST提交该 producer 的唯一 layer selection 与 sample
- **AND** AnimationPlaybackLifecycle MUST完成 PendingFirstSample/Current 提交
- **AND** AnimancerPlaybackAdapter MUST应用正式 producer binding

#### Scenario: 同层多个 producer

- **WHEN** 一次 preview evaluation 发现多个 producer 选择同一 LayerId
- **THEN** session MUST明确拒绝该 evaluation
- **AND** MUST不按 Priority 或 Track 顺序选择赢家

#### Scenario: 非连续 seek

- **WHEN** preview time 非连续跳转
- **THEN** session MUST重置 command queue、playback lifecycle 与 Animancer state
- **AND** 目标时间 MUST使用新的 playback generation 建立正式 selection/sample

#### Scenario: 连续播放

- **WHEN** session 连续播放
- **THEN** 同一 playback generation MUST持续更新 producer sample time
- **AND** session MUST不在每个表现帧重新创建隐藏 producer
