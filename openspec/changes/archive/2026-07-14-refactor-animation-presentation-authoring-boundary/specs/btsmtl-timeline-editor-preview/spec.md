# btsmtl-timeline-editor-preview Specification

## ADDED Requirements

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

## MODIFIED Requirements

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

### Requirement: Timeline Live Debug 必须显示真实 runtime membership

Timeline Live Debug MUST从 Trace 显示当前 playback instance/generation、active Track/Clip、TreeClip phase/runtime、AnimationProducerSample、PendingFirstSample/Current/Outgoing/Retired 与 terminal state。它 MUST不根据当前 authoring time 重新采样来猜测 membership。

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
- **THEN** Timeline Editor MUST提供 playback instance 选择
- **AND** Follow Graph Selection 与 Pin Playback MUST是显式模式

## REMOVED Requirements

### Requirement: 预览采样复用正式动画贡献链路

**Reason**: Registry、Arbitrator、LayerPlan 与 custom LayerRuntime 已删除；预览必须复用 selection/sample/lifecycle/Animancer 正式链路。

#### Scenario: 删除旧 Preview 仲裁

- **WHEN** Timeline Preview 采样动画
- **THEN** MUST不创建 Registry、Arbitrator、LayerPlan 或 ActiveHandoff
- **AND** MUST不使用动画 Priority 选择 producer
