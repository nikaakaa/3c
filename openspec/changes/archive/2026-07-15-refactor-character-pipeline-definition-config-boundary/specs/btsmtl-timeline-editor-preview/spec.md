## MODIFIED Requirements

### Requirement: Timeline 编辑器预览目标来自正式管线预览目标

系统 MUST使用 `TimelinePreviewTarget` 作为 Timeline 编辑器可选择的预览目标抽象，并由 `CharacterPipelineHost` 或等价正式角色管线目标实现它。正式角色管线预览目标 MUST沿 `CharacterPipelineDefinition.AnimationPresentationProfile` 与匹配的正式 `CharacterPresentationProjection` 取得唯一 Animancer TransitionLibrary、producer bindings，并使用 `AnimancerComponent` 正式引用。系统 MUST不使用 TimelinePlayer、场景搜索、fallback target、Definition 内联 Presentation 或第二份 animation layer 配置作为预览目标。

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
