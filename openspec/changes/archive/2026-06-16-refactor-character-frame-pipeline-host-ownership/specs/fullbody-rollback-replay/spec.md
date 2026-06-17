## MODIFIED Requirements
### Requirement: Replay 复用 FullBody Frame Pipeline
系统 MUST 让 FullBody replay、synctest 和本地高延迟校正复用 live gameplay 使用的 `CharacterFramePipelineHost -> CharacterFramePipeline` 主线。Replay adapter MAY 从 `PredictionInputFrame` 构造 pipeline 输入，但 MUST NOT 通过另一套手工顺序直接拼接 input buffer、controller Tick、状态恢复和动画播放事实。Replay adapter MUST NOT 直接创建 `CharacterFramePipeline`、FullBody submitter、第二 runner、第二 motion executor 或第二 animation presenter。

#### Scenario: PredictionInputFrame 进入 Host 和 Pipeline
- **GIVEN** replay adapter 收到 tick N 的 `PredictionInputFrame`
- **WHEN** replay 推进 tick N
- **THEN** 系统 MUST 将该输入帧作为 `CharacterFramePipelineHost` 的输入
- **AND** host MUST 通过同一个 `CharacterFramePipeline` 推进
- **AND** 离散按钮事实 MUST 在 `CharacterFramePipeline` 的输入缓冲步骤写入 `InputRequestBuffer`
- **AND** Move/Look/Run facts MUST 在 `CharacterFramePipeline` 的输入或 facts 步骤进入 Locomotion decision

#### Scenario: Replay 不绕过 GameplayDecision
- **WHEN** replay 推进 Dodge、TurnBack 或未来 Attack 输入
- **THEN** 请求 MUST 重新经过 `CharacterActionRequestSubmissionArbiter` 和统一状态机
- **AND** replay MUST NOT 直接写入“已进入某动作”的结果
- **AND** replay MUST NOT 直接调用 `BasicLocomotionPipeline` 作为 FullBody 最终路径

#### Scenario: Replay 快照来自 Pipeline 结果
- **WHEN** replay 推进 tick N 后捕获快照
- **THEN** 快照 MUST 来自同一 `CharacterFramePipeline` 写入的 FullBody 状态、runtime blackboard、input buffer restore state 和 motion executor restore state
- **AND** 快照 recorder MUST 不需要额外 enrich 一条独立 FullBody gameplay truth

#### Scenario: Replay 不创建分裂持有者
- **WHEN** FullBody replay 或 synctest 构造推进入口
- **THEN** replay MUST 使用角色正式 `CharacterFramePipelineHost`
- **AND** MUST NOT 为 replay 单独创建第二个 `CharacterFramePipeline`
- **AND** MUST NOT 直接调用 FullBody submitter 具体实现来绕过 host
