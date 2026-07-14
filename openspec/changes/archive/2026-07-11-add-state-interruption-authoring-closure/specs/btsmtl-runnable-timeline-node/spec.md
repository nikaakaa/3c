## MODIFIED Requirements

### Requirement: TimelineNode 完成状态必须保持请求语义

系统 MUST 保持 `TimelineNode` 通过正式 Timeline playback request 获取播放状态，并在播放成功时返回 `Success`。TimelineNode MUST NOT 直接驱动 StateMachine transition，也 MUST NOT 在自身内部解释 action lifecycle。自然播放完成、graceful stop 和 ForceStop MUST 使用 RunnableNode 的正式分层生命周期，不得共用无原因 OnStop 路径。

#### Scenario: Timeline 播放完成

- **WHEN** Timeline playback request 返回 `Succeeded`
- **THEN** TimelineNode MUST 返回 `Success`
- **AND** MUST 进入自然完成回调而不是 cancel 回调
- **AND** 状态机 transition 是否发生 MUST 由 StateMachine condition rule 决定

#### Scenario: Timeline 被 graceful stop

- **WHEN** Self、LowerPriority、Parent abort 或 State exit 请求停止正在运行的 TimelineNode
- **THEN** TimelineNode MUST 通过正式 playback request 取消未完成 Timeline
- **AND** Node stop status MUST 在取消请求建立后返回 Completed
- **AND** TimelineNode MUST NOT 提交 Action lifecycle transition

#### Scenario: Timeline 被 ForceStop

- **WHEN** Pipeline Shutdown、Dispose 或强制 Reset 释放 TimelineNode
- **THEN** TimelineNode MUST 立即取消并释放 active playback handle
- **AND** MUST NOT 等待 Timeline 完成、动画 blend 或网络确认

