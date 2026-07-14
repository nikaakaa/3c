# btsmtl-runnable-timeline-node Specification

## MODIFIED Requirements

### Requirement: TimelineNode 是普通可执行节点

系统 MUST继续提供普通 TimelineNode authoring，默认拥有 inline TimelineData，并 MAY显式引用 shared TimelineAsset。Compiler MUST把 resolved Timeline source 编译为 Program playback request、Timeline table 和 state layout；TimelineNode authoring MUST不直接成为播放器、runtime object 或 Network Model node，也 MUST不新增 model-specific TimelineNode。

#### Scenario: 编译 inline TimelineNode

- **WHEN** State body 中的 inline TimelineNode 被编译
- **THEN** Compiler MUST生成稳定 playback operation 与 source mapping
- **AND** authoring TimelineData MUST不在 runtime 被修改或 clone

### Requirement: TimelineNode 生命周期映射 Timeline 播放

TimelineNode 的 Runnable 生命周期 MUST映射为 compiled Timeline request lifecycle。SimulationState MUST保存 playback handle、generation、time、cycle、completion 和 cancel state；SimulationKernel MUST推进 Decision/Commit operation。TimelineNode MUST不通过 BaseGraph.User service 查询 runtime object，不创建 TimelineData clone，不绑定旧播放器，也不直接释放 Animancer lifecycle。

#### Scenario: 两个节点复用 shared Timeline

- **WHEN** 两个 compiled operation 引用同一 shared Timeline source
- **THEN** 两个 activation MUST拥有独立 playback state slots/generation
- **AND** 一个 activation 的 time/cancel MUST不污染另一个

#### Scenario: rollback 恢复 TimelineNode

- **WHEN** snapshot 恢复 active playback
- **THEN** handle、generation、time、cycle 和 TreeClip state MUST恢复
- **AND** runtime MUST不重新提交一次新的 authoring request

### Requirement: Timeline 请求入口来自正式执行上下文

正式 Timeline gameplay request MUST在 SimulationKernel Program execution context 内创建和查询。Presentation projection MAY消费对应 playback identity 和 visual sample request，但 Timeline operation MUST不持有 Presentation adapter、旧播放器、scene object 或 fallback service。

#### Scenario: Program 缺失 Timeline table

- **WHEN** Timeline operation 引用不存在的 table/index
- **THEN** Program load/compile MUST失败
- **AND** runtime MUST不现场读取 TimelineAsset 补齐

### Requirement: TimelineNode 播放状态隔离

多个 Timeline activation MUST通过 Program state layout、playback generation 和 owner scope 隔离。隔离 MUST不依赖 TimelineData runtime clone；Capture/Restore MUST包含全部 playback state。

#### Scenario: 同一节点再次激活

- **WHEN** 同一 TimelineNode source 在新 State activation 中进入
- **THEN** Kernel MUST分配新的 deterministic generation/slot owner
- **AND** 前一次 playback state MUST不被复用

