## ADDED Requirements
### Requirement: Locomotion Frame Pipeline 不重写动画播放权威
系统 MUST 在抽出 `LocomotionFramePipeline` 时保持基础移动动画播放和播放进度 rollback 权威不变。Pipeline MAY 读取已批准的纯数据 playback progress 或 motion facts，但 MUST NOT 调用 Animancer play API、不得执行 `RestorePlaybackProgress`、不得重置 playback window，也不得把 Animator runtime delta 恢复为正式 movement facts source。

#### Scenario: Presenter 仍负责播放
- **WHEN** Locomotion 本帧需要提交基础移动动画
- **THEN** `PlayerLocomotionController` 或等价正式 Runtime Adapter MUST 继续调用 `BasicLocomotionAnimancerPresenter`
- **AND** `LocomotionFramePipeline` MUST NOT 直接播放动画

#### Scenario: Playback restore 不被抢权
- **WHEN** rollback restore 后继续推进 Locomotion
- **THEN** playback restore 和 sampling window 语义 MUST 继续遵守 `formalize-animation-playback-rollback-authority`
- **AND** 本变更 MUST NOT 将同 alias 恢复播放重新归零

#### Scenario: Motion source 不回退
- **WHEN** TurnBack 或 RunEnd 需要 animation motion contribution
- **THEN** Pipeline MUST 只消费已批准的 profile-driven motion facts
- **AND** MUST NOT 恢复 Animator runtime pending delta 或新增 root motion fallback
