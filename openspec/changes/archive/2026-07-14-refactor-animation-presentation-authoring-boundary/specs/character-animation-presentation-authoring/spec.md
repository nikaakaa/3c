# character-animation-presentation-authoring Specification

## ADDED Requirements

### Requirement: Pipeline Definition 必须拥有唯一 Animation Presentation Definition

CharacterPipelineDefinition MUST内联拥有唯一 CharacterAnimationPresentationDefinition。该定义 MUST只保存 Animation Layer catalog、正式 Animancer TransitionLibraryAsset 引用、稳定 producer presentation keys 与 producer-to-transition bindings。Graph、StateMachine、Timeline、Presenter、旧 SO 或独立 Pipeline transition table MUST不保存同一数据的第二份真相。

#### Scenario: 打开 Corin Definition

- **WHEN** 作者检查 Corin CharacterPipelineDefinition
- **THEN** Animation Presentation Definition MUST与 Definition 一起序列化
- **AND** Base layer、TransitionLibrary 引用与 producer bindings MUST来自该定义
- **AND** 系统 MUST不从 StateMachine edge 读取 AnimationTransitionDefinition

#### Scenario: shared Graph 被多个角色使用

- **WHEN** 两个 CharacterPipelineDefinition 引用同一个 shared Graph/Timeline
- **THEN** 两个角色 MAY绑定不同 Animancer TransitionLibrary 或 producer transition key
- **AND** shared Graph MUST不保存角色级 transition 策略

### Requirement: Animation producer 必须拥有稳定 presentation identity

每个可被 AnimationLayerSelection 引用的 Timeline animation producer MUST拥有稳定 presentation identity。identity MUST由正式 authoring identity 与 runtime playback generation 组合，不得使用显示名、数组 index、asset path、breadcrumb 或当前 Tree activation 作为 fallback。inline/shared Timeline 重排后 producer identity MUST保持；复制 producer 时 MUST生成新 identity。

#### Scenario: Timeline Track 重排

- **WHEN** 作者重排 AnimationTrack 或 Clip
- **THEN** 原 producer presentation identity MUST保持
- **AND** 绑定 MUST不因列表 index 变化而 orphan

#### Scenario: 复制 inline Timeline producer

- **WHEN** 作者复制一个 inline TimelineNode 或 animation producer
- **THEN** 新 producer MUST获得新 identity
- **AND** 系统 MUST不让两个 producer 共用同一 runtime state key

#### Scenario: binding 指向未知 producer

- **WHEN** Presentation Definition 中的 binding 无法解析到正式 producer identity
- **THEN** Validator MUST报告 orphan binding
- **AND** runtime MUST不按名称或 clip 猜测目标

### Requirement: Animancer 原生 transition 数据必须是转场权威

系统 MUST使用项目已安装的 Animancer TransitionLibraryAsset、ITransition、FadeMode、source-to-target fade duration modifier 与 FadeGroup easing 作为转场播放权威。CharacterAnimationPresentationDefinition MAY保存 producer 到 Animancer transition key/source 的绑定，但 MUST不再保存 Pipeline 自有 Layer + SourceProducer + TargetProducer -> Duration + Curve 表，也 MUST不实现自定义 crossfade weight 求值。

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

### Requirement: CharacterPipelineDefinition Inspector 必须是唯一 Presentation 配置入口

系统 MUST在 CharacterPipelineDefinition Inspector 中唯一编辑 Layer catalog、TransitionLibrary 引用与 producer presentation binding，并 MUST不提供独立 Animation Presentation 窗口。Graph Inspector、StateMachine Editor 和 Timeline Editor MUST不提供这些数据的可写副本。Timeline Editor 继续独占 LayerId、clip、time、loop、ease 与 producer 内部 Weight。

#### Scenario: 编辑 producer transition

- **WHEN** 作者在 CharacterPipelineDefinition Inspector 选择一个 animation producer
- **THEN** 作者 MUST能查看其 layer、stable key 与 Animancer transition binding
- **AND** transition 细节 MUST通过 Animancer 正式 authoring API 或窗口编辑
- **AND** Graph/Timeline 逻辑资产 MUST保持不变

#### Scenario: 编辑 Timeline clip

- **WHEN** 作者需要修改 clip 时间、ease 或 Weight
- **THEN** CharacterPipelineDefinition Inspector MUST导航到独立 Timeline Editor
- **AND** MUST不复制这些字段

#### Scenario: 同时观察逻辑与 Timeline

- **WHEN** 作者从 CharacterPipelineDefinition Inspector 打开来源 Graph 和 Timeline
- **THEN** Graph 与 Timeline MUST保持两个可同时观察的独立窗口
- **AND** Timeline MUST不进入 Graph 页签栈
- **AND** 系统 MUST不创建第三个 Presentation 窗口

### Requirement: Definition Inspector 必须按正式 identity 显示 producer binding

CharacterPipelineDefinition Inspector MUST从 RootTree 递归发现 inline/shared Graph 与 Timeline 中的正式 animation producer，并按 stable producer identity 显示 LayerId、来源 Timeline 与 binding。Inspector MUST不推导或显示 StateMachine producer flow，MUST不保存 Tree node/edge 副本、Driver site、ExecutionLineage、runtime activation 或第二张 Animation Graph。正式运行时 MUST不依赖该列表做 selection 或 transition。

#### Scenario: 查看 Attack1 到 Attack2

- **WHEN** 作者检查包含 Attack1 与 Attack2 的 Corin Definition
- **THEN** Inspector MUST分别显示 Attack1 与 Attack2 的 producer identity、LayerId 与 binding
- **AND** 状态 edge MUST只保存 condition、priority 与 interruption
- **AND** Inspector MUST不复制 Attack1 到 Attack2 的逻辑 edge

#### Scenario: 查看 Action 覆盖 Locomotion

- **WHEN** Definition 同时包含 Action 与 Locomotion producer
- **THEN** Inspector MUST只列出各自的稳定 identity 与 binding
- **AND** MUST不推断覆盖关系或创建 Driver、Priority

### Requirement: 播放生命周期调试必须只保留统一视图

RuntimeDebugSession 与 CharacterPipelineHost 调试视图 MUST作为 AnimationLayerSelection、Timeline sample、PendingFirstSample、Current、Outgoing、Retired、Animancer state key 与 fade progress 的唯一生命周期调试入口。CharacterPipelineDefinition Inspector MUST不复制该 Trace UI。Editor MUST不重新运行 Graph、重采样 Timeline、重建 selection 或自行混合。

#### Scenario: 排查攻击切换

- **WHEN** Base selection 从 Locomotion 变为 Attack1
- **THEN** Host Live Debug MUST显示逻辑 selection、Attack1 首样本、Animancer state 与 outgoing Locomotion fade
- **AND** 数据 MUST来自正式 Trace

### Requirement: 迁移必须一次性删除旧动画表现 authoring

迁移 MUST先识别旧 Animation Layers、StateMachine AnimationTransitionDefinition、HandoffRole、external exit transition 与 Driver binding 的完整删除边界，再创建正式 Layer catalog、producer identity、Animancer TransitionLibrary binding 和 Corin logic selection。旧自定义 strategy、duration、curve 与 Inertialization 不需要保真。全部新资产保存并静态校验后，系统 MUST删除旧字段、旧 Inspector、旧 Agent operation 与任何一次性 migrator，不保留 FormerlySerializedAs、runtime lazy migration、兼容 parser 或双写。

#### Scenario: 迁移旧 Layer

- **WHEN** CharacterPipelineDefinition 仍保存旧 m_AnimationLayers
- **THEN** migrator MUST将其迁入内联 Animation Presentation Definition
- **AND** 保存成功后旧字段 MUST删除

#### Scenario: 迁移旧 transition

- **WHEN** 旧 edge 保存 strategy、duration 与 curve
- **THEN** 迁移 MUST删除该旧表现数据
- **AND** MUST不把旧 Inertialization 或 Pipeline curve 搬入正式配置

#### Scenario: 迁移完成

- **WHEN** 全部正式资产完成新配置
- **THEN** 项目 MUST不再包含 m_AnimationTransitionDefinitions、m_HandoffRole、m_ExternalExitTransition、Driver binding 或旧 m_AnimationLayers 路径
- **AND** 一次性 migrator MUST从正式代码删除
