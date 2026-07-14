# character-pipeline-runtime Specification

## ADDED Requirements

### Requirement: CharacterPipeline 必须作为显式 diagnostics target

每个 active `CharacterPipeline` MUST 能通过 `CharacterPipelineHost` 注册为独立 diagnostics target，并提供 session identity、source/program revision、Trace Buffer 和只读 target metadata。Host 或 Pipeline MUST NOT 向 editor 暴露 runtime Graph/Node/Timeline 对象作为正式调试 API。

#### Scenario: Host 激活 Pipeline

- **WHEN** `CharacterPipelineHost` 激活一个有效 Pipeline
- **THEN** diagnostics target registry MUST 注册该 runtime target
- **AND** target MUST 提供稳定 session identity 和 definition/source revision

#### Scenario: Host 禁用或销毁

- **WHEN** Host deactivate 或 dispose Pipeline
- **THEN** diagnostics target MUST 注销
- **AND** attached Debug Session MUST 收到正式 detach lifecycle
- **AND** editor MUST 不继续持有 Pipeline runtime 对象

### Requirement: Pipeline domain debug 必须进入统一 Trace

Action、Blackboard、Motion、Timeline、Animation Registry、ordered handoff records、causal components、LayerPlan、playback lifecycle、Presentation 和 Camera 的 runtime debug MUST 投影到统一 Trace/view model。`CharacterPipelineHostEditor` MUST 消费该 view model，不得继续各自遍历 runtime service 私有 debug collections 形成平行调试链。

#### Scenario: 查看 Pipeline Inspector

- **WHEN** 用户选择附着 active Debug Session 的 Host
- **THEN** Inspector MUST 显示 Session 当前 frame 的 Action、Blackboard、Motion、Animation 和 Camera snapshot
- **AND** Graph/Timeline 窗口 MUST 能引用同一 event identity

#### Scenario: 持续运行

- **WHEN** Play Mode 中 runtime target 持续产生 Trace
- **THEN** Inspector MUST 按统一 editor update schedule 刷新
- **AND** MUST NOT 依赖鼠标事件触发数据更新
