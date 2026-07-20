## ADDED Requirements

### Requirement: Pipeline Definition 必须引用唯一 Animation Presentation Profile

`CharacterPipelineDefinition` MUST引用唯一 `CharacterAnimationPresentationProfile`，不得内联保存 Animation Presentation 数据。该 Profile MUST只保存 Animation Layer catalog、正式 Animancer TransitionLibraryAsset 引用、稳定 producer presentation keys 与 producer-to-transition bindings。Graph、StateMachine、Timeline、Presenter、旧 SO 或独立 Pipeline transition table MUST不保存同一数据的第二份真相。

#### Scenario: 打开 Corin Definition

- **WHEN** 作者检查 Corin CharacterPipelineDefinition
- **THEN** Definition MUST只显示正式 CharacterAnimationPresentationProfile 引用
- **AND** Base layer、TransitionLibrary 引用与 producer bindings MUST来自该 Profile
- **AND** 系统 MUST不从 StateMachine edge 读取 AnimationTransitionDefinition

#### Scenario: shared Graph 被多个角色使用

- **WHEN** 两个 CharacterPipelineDefinition 引用同一个 shared Graph/Timeline
- **THEN** 两个角色 MAY引用不同 CharacterAnimationPresentationProfile
- **AND** 两个角色 MAY通过各自 Profile 绑定不同 Animancer TransitionLibrary 或 producer transition key
- **AND** shared Graph MUST不保存角色级 transition 策略

### Requirement: CharacterAnimationPresentationProfile Inspector 必须是唯一 Presentation 配置入口

系统 MUST在 `CharacterAnimationPresentationProfile` Inspector 中唯一编辑 Layer catalog、TransitionLibrary 引用与 producer presentation binding。CharacterPipelineDefinition Inspector MUST只编辑 Profile 引用并提供打开该资产的导航，不得保存或显示这些数据的可写副本。系统 MUST不提供独立 Animation Presentation 窗口；Graph Inspector、StateMachine Editor 和 Timeline Editor MUST不提供这些数据的可写副本。Timeline Editor 继续独占 LayerId、clip、time、loop、ease 与 producer 内部 Weight。

#### Scenario: 编辑 producer transition

- **WHEN** 作者在 CharacterAnimationPresentationProfile Inspector 选择一个 animation producer
- **THEN** 作者 MUST能查看其 layer、stable key 与 Animancer transition binding
- **AND** transition 细节 MUST通过 Animancer 正式 authoring API 或窗口编辑
- **AND** Graph/Timeline 逻辑资产 MUST保持不变

#### Scenario: 编辑 Timeline clip

- **WHEN** 作者需要修改 clip 时间、ease 或 Weight
- **THEN** CharacterAnimationPresentationProfile Inspector MUST导航到独立 Timeline Editor
- **AND** MUST不复制这些字段

#### Scenario: 同时观察逻辑与 Timeline

- **WHEN** 作者从 CharacterAnimationPresentationProfile Inspector 打开来源 Graph 和 Timeline
- **THEN** Graph 与 Timeline MUST保持两个可同时观察的独立窗口
- **AND** Timeline MUST不进入 Graph 页签栈
- **AND** 系统 MUST不创建第三个 Presentation 窗口

### Requirement: Profile Inspector 必须按正式 identity 显示 producer binding

`CharacterAnimationPresentationProfile` Inspector MUST在显式 Definition context 下，从该 Definition 的正式 Projection 读取 inline/shared Graph 与 Timeline 中的 animation producer 投影，并按 stable producer identity 显示 LayerId、来源 Timeline 与 binding。Inspector MUST不重新编译 Graph，不推导或显示 StateMachine producer flow，MUST不保存 Tree node/edge 副本、Driver site、ExecutionLineage、runtime activation 或第二张 Animation Graph。正式运行时 MUST不依赖该列表做 selection 或 transition。

#### Scenario: 查看 Attack1 到 Attack2

- **WHEN** 作者在包含 Attack1 与 Attack2 的 Definition context 下检查 Profile
- **THEN** Inspector MUST分别显示 Attack1 与 Attack2 的 producer identity、LayerId 与 binding
- **AND** 状态 edge MUST只保存 condition、priority 与 interruption
- **AND** Inspector MUST不复制 Attack1 到 Attack2 的逻辑 edge

#### Scenario: 查看 Action 覆盖 Locomotion

- **WHEN** Definition context 同时包含 Action 与 Locomotion producer
- **THEN** Inspector MUST只列出各自的稳定 identity 与 binding
- **AND** MUST不推断覆盖关系或创建 Driver、Priority

## MODIFIED Requirements

### Requirement: Animancer 原生 transition 数据必须是转场权威

系统 MUST使用项目已安装的 Animancer TransitionLibraryAsset、ITransition、FadeMode、source-to-target fade duration modifier 与 FadeGroup easing 作为转场播放权威。`CharacterAnimationPresentationProfile` MAY保存 producer 到 Animancer transition key/source 的绑定，但 MUST不再保存 Pipeline 自有 Layer + SourceProducer + TargetProducer -> Duration + Curve 表，也 MUST不实现自定义 crossfade weight 求值。

#### Scenario: 播放目标 producer

- **WHEN** selected producer 收到第一份合法 sample
- **THEN** AnimancerPlaybackAdapter MUST通过正式 transition key/source 调用 TransitionLibrary.Play 或 AnimancerLayer.Play
- **AND** fade MUST由 Animancer state graph 推进

#### Scenario: source-target duration modifier

- **WHEN** TransitionLibrary 为当前 source key 到 target key 配置 modifier
- **THEN** adapter MUST使用 Animancer 原生解析结果
- **AND** Pipeline MUST不复制同一 pair 到另一张表

#### Scenario: 删除旧自定义 transition

- **WHEN** 旧 AnimationTransitionDefinition 保存此前中间实现生成的 Inertialization、strategy、duration 或 curve
- **THEN** 迁移 MUST删除该自定义数据而不建立兼容映射
- **AND** 正式转场 MUST重新由 Animancer TransitionLibrary authoring 提供

### Requirement: 播放生命周期调试必须只保留统一视图

RuntimeDebugSession 与 CharacterPipelineHost 调试视图 MUST作为 committed producer command、Timeline visual sample、PendingFirstSample、Current、Outgoing、Retired、Animancer state key 与 fade progress 的唯一生命周期调试入口。CharacterPipelineDefinition Inspector 与 CharacterAnimationPresentationProfile Inspector MUST不复制该 Trace UI。Editor MUST不重新运行 Graph、重建 Program command、重采样 Gameplay Timeline 或自行混合。

#### Scenario: 排查攻击切换

- **WHEN** Base committed producer 从 Locomotion 变为 Attack1
- **THEN** Host Live Debug MUST显示 Program command EventId、Attack1 首样本、Animancer state 与 outgoing Locomotion fade
- **AND** 数据 MUST来自正式 Trace

### Requirement: 迁移必须一次性删除旧动画表现 authoring

迁移 MUST先识别当前内联 CharacterAnimationPresentationDefinition 与更早的 Animation Layers、StateMachine AnimationTransitionDefinition、HandoffRole、external exit transition、Driver binding 的完整删除边界，再创建正式 CharacterAnimationPresentationProfile、producer identity、Animancer TransitionLibrary binding 和 Corin logic selection。旧自定义 strategy、duration、curve 与 Inertialization 不需要保真。全部新资产保存并静态校验后，系统 MUST删除旧字段、旧类型、旧 Inspector、旧 Agent operation 与任何一次性 migrator，不保留 FormerlySerializedAs、runtime lazy migration、兼容 parser 或双写。

#### Scenario: 迁移当前内联 Presentation

- **WHEN** CharacterPipelineDefinition 仍保存内联 m_AnimationPresentation
- **THEN** 迁移 MUST将 Layer、TransitionLibrary 与全部 producer bindings 原样写入正式 Profile asset
- **AND** 保存成功后内联字段与旧类型 MUST删除

#### Scenario: 迁移旧 transition

- **WHEN** 旧 edge 保存 strategy、duration 与 curve
- **THEN** 迁移 MUST删除该旧表现数据
- **AND** MUST不把旧 Inertialization 或 Pipeline curve 搬入正式配置

#### Scenario: 迁移完成

- **WHEN** 全部正式资产完成新配置
- **THEN** 项目 MUST不再包含 m_AnimationPresentation 内联块、m_AnimationTransitionDefinitions、m_HandoffRole、m_ExternalExitTransition、Driver binding 或旧 m_AnimationLayers 路径
- **AND** 一次性 migrator MUST从正式代码删除

## REMOVED Requirements

### Requirement: Pipeline Definition 必须拥有唯一 Animation Presentation Definition

**Reason**: Animation Presentation 改为独立 Profile asset，Definition 只负责引用，不再内联拥有表现配置。

**Migration**: 将每个 Definition 的内联 Layer、TransitionLibrary 与 producer bindings 原样迁入 CharacterAnimationPresentationProfile，并把 Definition 改为正式 Profile 引用。

### Requirement: CharacterPipelineDefinition Inspector 必须是唯一 Presentation 配置入口

**Reason**: Definition Inspector 收敛为配置装配清单，Presentation 的唯一写入口迁到 Profile Inspector。

**Migration**: 删除 Definition Inspector 的 Layer、Library 与 binding 写 UI，改为 Profile 引用和打开 Profile 的导航。

### Requirement: Definition Inspector 必须按正式 identity 显示 producer binding

**Reason**: producer binding 属于 Profile，不属于 Definition 默认表单。

**Migration**: 将 binding 列表和 Graph/Timeline 导航迁到 Profile Inspector，并使用显式 Definition context 读取正式 Projection。
