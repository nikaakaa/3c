## MODIFIED Requirements

### Requirement: TreeClip 编译为 TimelineBody 图 operation invocation

Logic TreeClip 与 DualProjection TreeClip 的 Logic 投影 MUST将其 AssetTree（TimelineBody 图）编译为正式 operations：TimelineClip caller MUST按 clip 声明 OnEnable、OnDisable、OnDestroy 三个边界 hook entry 与 Root（技能入口）entry，SourceMap MUST按 clipId 登记 Root handle 供运行时查询。Presentation TreeClip MUST NOT绑定、编译或执行 TimelineBody 图。系统 MUST不恢复 Timeline.Bind/Evaluate/Unbind 自主播放路径。

#### Scenario: Decision TreeClip 穿过 Loop 边界

- **WHEN** 一个 SimulationTick 穿过 Logic TreeClip 的 Timeline loop 边界
- **THEN** compiled evaluator MUST按尾段、中间 cycle 和头段顺序求值
- **AND** Frame Blackboard MUST保持唯一结果

## ADDED Requirements

### Requirement: Presentation TreeClip必须只以Marker产生表现事件

Presentation TreeClip MUST通过 typed Presentation Marker 产生特效、音效、相机或表现动画事件。Marker MUST保留稳定MarkerId、作者时间、`Pulse` 或 `Stateful` 生命周期类型与正式payload binding；它们 MUST由 PresentationFrame 的视觉游标跨越产生事件。DualProjection TreeClip 的 Marker 与其 Logic AssetTree 共用 Clip identity，但两者不得共享执行入口或互相写入状态。`TreeDecision` 与“结束片段”节点只属于 Logic AssetTree，不得成为 Presentation Marker 的结束或触发机制。

#### Scenario: Presentation TreeClip没有逻辑图

- **WHEN** 作者创建只包含表现事件的 Presentation TreeClip
- **THEN** 作者数据 MUST只保存 Marker 与表现 binding
- **AND** 准备阶段 MUST拒绝为该 Clip 绑定或执行 TimelineBody 图
- **AND** 表现事件 MUST不产生 Frame Blackboard、Gameplay fact 或 TreeClip Commit / Discard 候选

#### Scenario: DualProjection TreeClip同时含图与Marker

- **WHEN** 一个 Clip 同时拥有 Logic AssetTree 与 Presentation Marker
- **THEN** Logic Tick MUST只执行 AssetTree
- **AND** PresentationFrame MUST只遍历 Marker
- **AND** 同一图不得因多个PresentationFrame被重复执行
