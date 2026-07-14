## ADDED Requirements

### Requirement: TimelinePlaybackScheduler 必须分阶段推进 TreeClip

`TimelinePlaybackScheduler` MUST 将 Decision TreeClip 求值放在 `PrepareDecisionFacts`，将 Commit TreeClip 生命周期放在 `Commit`。Scheduler MUST 使用同一 active playback time、cycle、owner 和 cancel 状态解释两种阶段，并保证每个 TreeClip 在对应阶段每 Tick最多执行一次。PresentationFrame MUST NOT 执行任何 TreeClip 逻辑。

#### Scenario: Decision 后 source Timeline 被取消

- **WHEN** Decision TreeClip 在 Prepare 阶段写入 Frame Blackboard
- **AND** RootTree 随后通过 State Transition 取消 source Timeline
- **THEN** source State.OnExit MUST 仍能读取本 Tick Decision 值
- **AND** Scheduler MUST 丢弃该 playback 尚未提交的 Commit Tree 输出

#### Scenario: Loop Timeline 跨越 TreeClip 边界

- **WHEN** Loop Timeline 在一个 logic tick 跨过 duration 边界
- **THEN** Scheduler MUST 按尾段和头段解释 TreeClip active range
- **AND** 同一 clip/cycle 的 Decision MUST NOT 重复执行
- **AND** Commit Tree runtime MUST 按 cycle identity 正确退出和进入

#### Scenario: Pipeline deactivate

- **WHEN** Character pipeline deactivate 或 dispose
- **THEN** Scheduler MUST ForceStop 并释放全部 active 和 terminal-pending Tree runtime
- **AND** TreeClip MUST NOT 在表现收尾阶段继续 Tick

### Requirement: TreeClip 运行不得恢复 Timeline 双权威

角色管线模式下 TreeTrack MUST 使用 Scheduler 管理的专用 Tree runtime 解释入口。实现 MUST NOT 为 TreeTrack 调用旧 Timeline `Bind`、`Evaluate`、`SetTime` 或 `Unbind` 来旁路显式轨道采样。

#### Scenario: 迁移 TreeTrack

- **WHEN** TreeTrack 被接入角色 Timeline playback
- **THEN** TimelineNode MUST 继续只提交播放请求
- **AND** Scheduler MUST 继续是唯一逻辑时间权威
- **AND** Animation、Motion、Window、Cue、Camera 和 Tree MUST NOT 形成两个推进时钟
