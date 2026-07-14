# btsmtl-timeline-editor-preview Specification

## ADDED Requirements

### Requirement: Timeline、Track 和 Clip 必须拥有稳定 authoring identity

`TimelineData`、每个 Track 和每个 Clip MUST 持有稳定 authoring identity。authoring 重排 MUST 保持 identity，复制 Track/Clip MUST 生成新 identity，runtime clone MUST 保留 source identity。TrackIndex 和 ClipIndex MUST NOT 作为 Debug Source Map 的 source identity。

#### Scenario: 重排 Track

- **WHEN** 作者调整 Timeline Track 顺序
- **THEN** Track 和其 Clip authoring identity MUST 保持
- **AND** runtime debug source mapping MUST 不因 index 变化指向其它 Track

#### Scenario: 复制 Clip

- **WHEN** 作者复制一个 Clip
- **THEN** 新 Clip MUST 获得新 authoring identity
- **AND** 原 Clip identity MUST 保持

#### Scenario: runtime clone Timeline

- **WHEN** scheduler 从 TimelineData 创建 runtime clone
- **THEN** clone 中 Timeline、Track 和 Clip MUST 保留 authoring identity
- **AND** playback handle、cycle 和其它 runtime identity MUST 继续独立生成

### Requirement: Timeline Editor 必须分离 Authoring Preview 与 Live Debug

`TimelineEditorWindow` MUST 提供语义明确且互斥的 Authoring Preview 与 Live Debug 模式。Authoring Preview MUST 继续由 `TimelinePreviewSession` 驱动；Live Debug MUST 由 `RuntimeDebugSession` 观察真实 scheduler，不得调用 preview evaluator 或修改 runtime playback。

#### Scenario: Authoring Preview

- **WHEN** 用户选择 Authoring Preview
- **THEN** TimelineEditor MUST 使用显式 preview target、preview time 和 preview lifecycle
- **AND** UI MUST 不把结果标记为真实 gameplay runtime

#### Scenario: Live Debug

- **WHEN** 用户选择 Live Debug 并附着有效 Character runtime target
- **THEN** TimelineEditor MUST 显示真实 playback 的 logic time、visual time、cycle 和 lifecycle
- **AND** Timeline 编辑内容 MUST 只读
- **AND** `TimelinePreviewSession` MUST 不参与该模式

### Requirement: Timeline Live Debug 必须显示真实 runtime membership

Timeline Live Debug MUST 从 Trace 显示当前 playback instance、active Track/Clip、TreeClip phase/runtime、animation contribution 和 terminal state。它 MUST NOT 仅根据当前 authoring time 重新采样来猜测 membership。

#### Scenario: Decision TreeClip active

- **WHEN** scheduler 在某 logic tick 评估 Decision TreeClip
- **THEN** Timeline Live Debug MUST 在对应 Clip 上显示该 tick 的 Decision evaluation
- **AND** UI MUST 能关联写入的 Blackboard declaration identity

#### Scenario: visual time 位于两个 logic tick 之间

- **WHEN** PresentationFrame 以 interpolation alpha 计算 visual Timeline time
- **THEN** Timeline Live Debug MUST 分别显示 logic time 与 visual time
- **AND** animation playhead MUST 使用 visual time
- **AND** gameplay window/TreeClip decision 标记 MUST 使用 logic tick

#### Scenario: 多个 playback 使用同一 Timeline source

- **WHEN** 同一 Timeline source 同时存在多个 playback instances
- **THEN** Timeline Editor MUST 提供 playback instance 选择
- **AND** Follow Graph Selection 与 Pin Playback MUST 是显式模式

