## MODIFIED Requirements

### Requirement: 动画层预览只读取调试 Snapshot

系统 MUST从正式 `AnimationPlaybackLifecycle` 与 Animancer adapter导出只读 `AnimationPlaybackFrameSnapshot` 或等价数据。Snapshot MAY包含每层selection、sample time、PendingFirstSample、Current、Outgoing、Retired、Animancer state key与fade progress，MUST不参与gameplay决策或最终播放。Timeline Authoring Preview MUST使用与正式链路相同的sampling、lifecycle和Animancer adapter，但 MUST不创建Preview Simulation Session；Timeline Live Debug MUST只读取正式运行Snapshot与trace。

#### Scenario: 生成每帧预览数据

- **WHEN** Timeline Authoring Preview采样AnimationTrack
- **THEN** 它 MUST复用正式动画播放链并导出只读Snapshot
- **AND** MAY只读投影已创作的单来源MotionCurve轨迹
- **AND** MUST不执行Program operation、Motion arbitration、MotionWarp或WorldSolver

#### Scenario: Live Debug观察正式动画

- **WHEN** 正式Character runtime生成动画生命周期Snapshot
- **THEN** Timeline Live Debug MUST只读显示该Snapshot
- **AND** MUST不通过Authoring Preview重算正式结果
