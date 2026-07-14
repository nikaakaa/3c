## MODIFIED Requirements

### Requirement: 预览采样复用正式动画贡献链路

Timeline Preview MUST 复用正式 AnimationTrack sampling、私有 Registry、私有 Arbitrator、每层唯一 LayerPlan、持久 LayerRuntime 与 Presenter。Preview contribution MUST 使用独立 playback/contribution/owner identity，并生成 `AnimationLayerPlaybackOutput`。Preview MUST NOT直接播放 clip、把 DesiredCandidate 直接交给 Runtime或实现第二套 layer mixing。

#### Scenario: 当前时间采样

- **WHEN** preview time 位于 AnimationTrack clip 范围
- **THEN** Sample MUST 进入 session 私有 Registry
- **AND** Arbitrator MUST 生成对应 LayerPlan
- **AND** LayerRuntime MUST 生成最终 layer output
- **AND** Presenter MUST 只消费该 output

#### Scenario: 多 contribution

- **WHEN** 多个 preview contributions 写入同一 layer
- **THEN** Preview MUST 使用正式 priority、weight 与 blend mode 规则生成一个 LayerPlan

#### Scenario: 非连续 seek

- **WHEN** preview time 非连续跳转
- **THEN** session MUST 清理 Registry、Arbitrator ledger 与 Final/Held/Active layer state
- **AND** 目标时间 MUST 通过新的 InitialSeed plan建立输出

#### Scenario: 连续播放

- **WHEN** session 连续播放
- **THEN** 同 owner sample MUST 通过 Update plan更新持久 layer output
- **AND** session MUST NOT每帧重新 InitialSeed

### Requirement: Timeline preview session 必须隔离动画生命周期状态

每个 `TimelinePreviewSession` MUST 拥有独立 Registry、Arbitrator、LayerRuntime 与 outputs。它 MUST NOT读取角色 runtime layer state、与其它窗口共享 ledger/playback state或把 lifecycle 写入 Timeline asset。

#### Scenario: 两个 Preview 窗口

- **WHEN** 两个窗口预览同一 Timeline
- **THEN** 两个 session MUST 拥有独立 owner、Registry、Arbitrator ledger 与 layer state

#### Scenario: 切换 target

- **WHEN** session 切换 Preview target
- **THEN** 旧 target outputs、ledger 与 layer state MUST 清理
- **AND** 新 target MUST 使用新 session identity

#### Scenario: Dispose

- **WHEN** Preview stop 或 dispose
- **THEN** pending records、active handoff、held output 与 native state MUST 释放
- **AND** Timeline asset MUST 不保存 runtime state
