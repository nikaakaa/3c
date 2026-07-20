# btsmtl-timeline-editor-preview Specification

## MODIFIED Requirements

### Requirement: Timeline Editor 必须分离 Authoring Preview 与 Live Debug

TimelineEditorWindow MUST 提供语义明确且互斥的 Authoring Preview 与 Live Debug 模式。Authoring Preview MUST 继续由 TimelinePreviewSession 驱动；Live Debug MUST 由 RuntimeDebugSession 的真实 Trace snapshot 和 Timeline 窗口本地 runtime binding 观察真实 scheduler，不得调用 preview evaluator、修改 runtime playback 或改写其它 Graph / Timeline 窗口的 binding。

#### Scenario: Authoring Preview

- **WHEN** 用户选择 Authoring Preview
- **THEN** TimelineEditor MUST 使用显式 preview target、preview time 和 preview lifecycle
- **AND** UI MUST 不把结果标记为真实 gameplay runtime

#### Scenario: Live Debug

- **WHEN** 用户选择 Live Debug
- **THEN** TimelineEditor MUST 以当前 Timeline identity/content hash 请求正式 target 解析
- **AND** 成功附着时 MUST 使用该窗口本地 binding 观察真实 playback
- **AND** Timeline 编辑内容 MUST 只读
- **AND** TimelinePreviewSession MUST 不参与该模式

#### Scenario: Play Mode domain reload 保持 Live Debug

- **WHEN** TimelineEditorWindow 在 Live Debug 下经历 Play Mode domain reload
- **THEN** 窗口 MUST 从已序列化 Timeline owner/path 恢复相同 authoring Timeline 与 Live Debug mode
- **AND** MUST 创建新的本地 runtime binding 并重新解析共享 Session
- **AND** locator 无效时 MUST 停止恢复，不得改用 Authoring Preview 或猜测其它 Timeline

### Requirement: Timeline Live Debug 必须显示真实 runtime membership

Timeline Live Debug MUST 从 Trace 显示当前 playback instance/generation、发起 Graph / Node source、可用的 activation context、active Track/Clip、TreeClip phase/runtime、AnimationProducerSample、PendingFirstSample/Current/Outgoing/Retired 与 terminal state。它 MUST 不根据当前 authoring time 重新采样来猜测 membership。

#### Scenario: 一个匹配 playback

- **WHEN** 当前 Timeline source 在本地 binding 对应的 target 中只有一个可跟随 playback
- **THEN** Timeline 窗口 MUST Follow 该 playback
- **AND** MUST 显示 logic time、visual time、cycle 和 lifecycle
- **AND** Graph 窗口的本地 binding MUST 不被改变

#### Scenario: 多个 playback 使用同一 Timeline source

- **WHEN** 同一 Timeline source 同时存在多个 playback instances
- **THEN** Timeline Editor MUST 为每个 playback 显示 playback id、来源 Graph / Node、activation context 与 terminal / lifecycle 摘要
- **AND** Timeline 窗口 MUST 要求作者在本地 binding 中 Pin 其中一个，或显式保持 Follow
- **AND** 系统 MUST NOT 按列表顺序静默选择赢家

#### Scenario: 当前 Timeline 未执行

- **WHEN** 已附着 target 的共享 snapshot 不包含当前 Timeline 的 playback
- **THEN** Timeline Editor MUST 显示当前角色未执行该 Timeline 的状态
- **AND** MUST NOT 调用 TimelinePreviewSession、preview evaluator 或 authoring time 重采样
