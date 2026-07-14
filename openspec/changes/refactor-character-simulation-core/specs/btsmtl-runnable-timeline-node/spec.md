# btsmtl-runnable-timeline-node Specification

## MODIFIED Requirements

### Requirement: TimelineNode 生命周期映射 Timeline 播放

TimelineNode MUST编译为 Runnable operation 与 Timeline playback data。Enter、play/update、loop、complete、stop 和 release MUST映射到 SimulationState slot，MUST不由 TimelineNode 或 TimelineRunningTree clone 持有。

#### Scenario: TimelineNode 完成

- **WHEN** compiled Timeline 到达请求终点且 commit lifecycle 完成
- **THEN** Kernel MUST从 state slot 产生 Runnable completion

### Requirement: TimelineNode 播放状态隔离

每个 Actor、Graph activation、Timeline activation 和 loop cycle MUST使用独立 compiled identity/state slot。Shared Timeline authoring MUST不导致不同角色或不同 activation 共享播放状态。

#### Scenario: 两个角色播放同一 Timeline

- **WHEN** 两个 Actor 使用同一 Program 中的 Timeline data
- **THEN** 它们 MUST使用各自 SimulationState 中的 playback slot

### Requirement: 保留 Timeline 驱动 Tree 链路

TreeTrack/TreeClip MUST编译为 Timeline decision/commit operation。Decision MUST在 RootTree operation 前只写 Frame Blackboard，Commit MUST在 RootTree operation 后执行 Enter/Update/Exit/Destroy 生命周期。系统 MUST不恢复 Timeline.Bind/Evaluate/Unbind 自主播放路径。

#### Scenario: Decision TreeClip 穿过 Loop 边界

- **WHEN** 一个 SimulationTick 穿过 Timeline loop 边界
- **THEN** compiled evaluator MUST按尾段、中间 cycle 和头段顺序求值
- **AND** Frame Blackboard MUST保持唯一结果

### Requirement: Timeline 动作事实必须来自 Timeline 轨道采样

Compiled Timeline gameplay segment MUST产生 ActionWindow、MotionContribution 和 typed facts。Animation/Cue resource MUST通过 Presentation command 与 Projection 定位，MUST不进入 gameplay state。

#### Scenario: Attack Cancel Window

- **WHEN** compiled Decision TreeClip 命中 Cancel Window segment
- **THEN** MUST写入正式 Frame Blackboard declaration 并投影 ActionWindow fact
