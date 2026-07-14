# btsmtl-timeline-editor-preview Specification

## ADDED Requirements

### Requirement: Timeline 编辑器预览使用管线预览会话
系统 MUST 使用 editor-only 的 `TimelinePreviewSession` 作为 BTSMTL Timeline 编辑器的播放、暂停、速度和游标预览控制器。Timeline 编辑器 MUST NOT 直接控制 `TimelinePlayer`、`PlayableGraph` 或 `Timeline.TimelinePlayer`。

#### Scenario: 点击播放按钮
- **WHEN** 用户在 Timeline 编辑器点击播放
- **THEN** 编辑器 MUST 将 `TimelinePreviewSession.IsPlaying` 设置为 true
- **AND** 编辑器 MUST NOT 调用 `Timeline.TimelinePlayer.IsPlaying`

#### Scenario: 拖拽时间游标
- **WHEN** 用户拖拽 Timeline 时间游标
- **THEN** 编辑器 MUST 调用 `TimelinePreviewSession.SetTime(...)`
- **AND** 编辑器 MUST NOT 调用 `Timeline.TimelinePlayer.Evaluate(...)`

### Requirement: Timeline 编辑器预览目标来自正式管线预览目标
系统 MUST 使用 `TimelinePreviewTarget` 作为 Timeline 编辑器可选择的预览目标抽象，并由 `CharacterPipelineHost` 或等价正式角色管线目标实现它。正式角色管线预览目标 MUST 使用 `CharacterPipelineDefinition` 和 `AnimancerComponent` 的正式引用。系统 MUST NOT 使用 `TimelinePlayer`、场景搜索、fallback target 或第二份 animation layer 配置作为预览目标。

#### Scenario: 选择预览目标
- **WHEN** 用户在 Timeline 编辑器 target field 选择场景对象
- **THEN** 可接受对象 MUST 是 `TimelinePreviewTarget`
- **AND** 当前角色管线目标 MUST 由 `CharacterPipelineHost` 实现
- **AND** `CharacterPipelineHost` MUST 使用正式 `CharacterPipelineDefinition.AnimationLayers`
- **AND** `CharacterPipelineHost` MUST 使用正式 `AnimancerComponent` 应用动画预览

#### Scenario: 未选择预览目标
- **WHEN** Timeline 编辑器没有有效 `TimelinePreviewTarget`
- **THEN** 用户 MAY 继续编辑 Timeline 数据
- **AND** 播放、暂停、速度和可应用预览 MUST 处于禁用状态
- **AND** 系统 MUST NOT 自动查找场景中的 Host 或 TimelinePlayer

### Requirement: 预览采样复用正式动画贡献链路
系统 MUST 让 Timeline 编辑器预览复用正式 Timeline 采样和动画层链路。动画预览 MUST 从 `AnimationTrack.Sample(...)` 产生 `TimelineAnimationContribution`，转换为 `AnimationContribution`，再由 `CharacterAnimationLayerRuntime` 生成播放计划，并由 `AnimancerAnimationPresenter` 应用。系统 MUST NOT 通过 Timeline track 直接播放 AnimationClip。

#### Scenario: 动画轨道处于当前时间
- **WHEN** preview session 时间落在某个 AnimationTrack clip 范围内
- **THEN** 正式管线预览目标 MUST 采样该 clip 并生成动画贡献
- **AND** 动画贡献 MUST 经过角色动画层运行时仲裁
- **AND** Animancer adapter MUST 只消费仲裁后的播放计划

#### Scenario: 多轨道贡献同一 layer
- **WHEN** 同一帧预览存在多个动画贡献指向同一 layer
- **THEN** 正式管线预览目标 MUST 使用与角色管线相同的 priority、weight 和 blend mode 规则生成播放计划
- **AND** 系统 MUST NOT 在 Timeline 编辑器里实现第二套混合规则

### Requirement: Timeline 资产不保存编辑器播放状态
系统 MUST 将编辑器预览播放状态保存在 `TimelinePreviewSession` 中。`Timeline` 资产 MUST 保持为可复用数据资产，不保存当前预览目标、`TimelinePlayer`、PlayableGraph 或预览播放状态。

#### Scenario: 同一 Timeline 被两个窗口预览
- **WHEN** 两个编辑器窗口预览同一个 Timeline 资产
- **THEN** 每个窗口 MUST 拥有自己的 preview session 时间和播放状态
- **AND** 一个窗口的播放、暂停或拖拽 MUST NOT 改写 Timeline 资产上的播放状态

### Requirement: 旧 TimelinePlayer 预览路径必须删除
系统 MUST 删除 BTSMTL Timeline 编辑器对 `TimelinePlayer` autonomous playback 的依赖。旧 `TimelinePlayer`、`Timeline.Bind(TimelinePlayer)`、`Timeline.Unbind()`、`Timeline.TimelinePlayer` 和依赖这些字段的编辑器调用 MUST 删除或迁移到正式 preview session。系统 MUST NOT 保留兼容分支继续支持旧播放器预览。

#### Scenario: 搜索旧播放器入口
- **WHEN** 实现完成后搜索 Timeline 编辑器代码
- **THEN** 不应存在 `typeof(TimelinePlayer)`、`Timeline.TimelinePlayer`、`TimelinePlayer.RunningTimelines` 或 `TimelinePlayer.IsPlaying` 作为预览入口
- **AND** Timeline 编辑器的播放入口 MUST 指向 `TimelinePreviewSession`
