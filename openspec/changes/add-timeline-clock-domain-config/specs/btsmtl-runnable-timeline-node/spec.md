## MODIFIED Requirements

### Requirement: TreeClip 编译为 TimelineBody 图 operation invocation

Logic TreeClip 与 DualProjection TreeClip 的 Logic 投影 MUST将其 AssetTree（TimelineBody 图）编译为正式 operations：TimelineClip caller MUST按 clip 声明 OnEnable、OnDisable、OnDestroy 三个边界 hook entry 与 Root（技能入口）entry，SourceMap MUST按 clipId 登记 Root handle 供运行时查询。Presentation TreeClip MUST NOT绑定、编译或执行 TimelineBody 图。系统 MUST不恢复 Timeline.Bind/Evaluate/Unbind 自主播放路径。

#### Scenario: Decision TreeClip 穿过 Loop 边界

- **WHEN** 一个 SimulationTick 穿过 Logic TreeClip 的 Timeline loop 边界
- **THEN** compiled evaluator MUST按尾段、中间 cycle 和头段顺序求值
- **AND** Frame Blackboard MUST保持唯一结果

## ADDED Requirements

### Requirement: Marker 是与 Clip 同级的点触发实体且触发图仅暴露 OnEnable

Timeline Marker MUST是与 Clip 同级的一等内容实体：单帧点、稳定 MarkerId、触发帧与触发图引用。Marker 的触发图 MUST只暴露 OnEnable 一个回调：触发时 MUST只执行该入口一次，MUST NOT进入 Update / Exit / Disable 生命周期。Marker 的推进者由其域归属决定：Logic 域 Marker MUST经 SimulationTick Advance / Commit 确定性触发；Presentation 域 Marker MUST经 PresentationFrame 游标跨点触发并只改变表现状态。`TreeDecision` 与“结束片段”节点只属于 Logic TreeClip 的 AssetTree，Marker 触发图 MUST NOT包含它们。

#### Scenario: 表现域 Marker 触发

- **WHEN** 表现游标跨过 Presentation Marker 的触发点
- **THEN** 其触发图 MUST只执行 OnEnable 入口一次
- **AND** MUST NOT产生 Gameplay fact 或 Frame Blackboard

#### Scenario: 逻辑域 Marker 触发

- **WHEN** SimulationTick 跨过 Logic Marker 的触发点
- **THEN** 其触发图 MUST经 Advance / Commit 协议执行 OnEnable 入口一次
- **AND** 同一经过 Discard 后 MUST不产生执行残留

#### Scenario: 与 Clip 同级共存

- **WHEN** 作者在同一 Timeline 使用 Clip 与 Marker
- **THEN** Marker MUST作为与 Clip 同级的内容实体创建、存储与推进
- **AND** Marker MUST NOT作为 Clip 的子列表存在
