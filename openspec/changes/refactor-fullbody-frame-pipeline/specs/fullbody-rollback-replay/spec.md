## ADDED Requirements
### Requirement: Replay 复用 FullBody Frame Pipeline
系统 MUST 让 FullBody replay、synctest 和本地高延迟校正复用 live gameplay 使用的 FullBody frame pipeline。Replay adapter MAY 从 `PredictionInputFrame` 构造 pipeline 输入，但 MUST NOT 通过另一套手工顺序直接拼接 input buffer、controller Tick、状态恢复和动画播放事实。

#### Scenario: PredictionInputFrame 进入 Pipeline
- **GIVEN** replay adapter 收到 tick N 的 `PredictionInputFrame`
- **WHEN** replay 推进 tick N
- **THEN** 系统 MUST 将该输入帧作为 FullBody frame pipeline 的输入
- **AND** 离散按钮事实 MUST 在 pipeline 的输入缓冲步骤写入 `InputRequestBuffer`
- **AND** Move/Look/Run facts MUST 在 pipeline 的输入或 facts 步骤进入 Locomotion decision

#### Scenario: Replay 不绕过 GameplayDecision
- **WHEN** replay 推进 Dodge、TurnBack 或未来 Attack 输入
- **THEN** 请求 MUST 重新经过 FullBody Action request gate 和统一状态机
- **AND** replay MUST NOT 直接写入“已进入某动作”的结果
- **AND** replay MUST NOT 直接调用 `BasicLocomotionPipeline` 作为 FullBody 最终路径

#### Scenario: Replay 快照来自 Pipeline 结果
- **WHEN** replay 推进 tick N 后捕获快照
- **THEN** 快照 MUST 来自同一 pipeline 写入的 FullBody 状态、runtime blackboard、input buffer restore state 和 motion executor restore state
- **AND** 快照 recorder MUST 不需要额外 enrich 一条独立 FullBody gameplay truth

### Requirement: Pipeline Replay 可诊断
系统 MUST 为 pipeline replay 提供字段级 diagnostics，使 replay mismatch 能区分输入回灌、Action 仲裁、统一状态机、运动执行、动画事实和 snapshot capture 的差异。

#### Scenario: Replay mismatch 标记阶段
- **WHEN** FullBody synctest 发现原始运行和 replay 不一致
- **THEN** diagnostics MUST 能标记差异发生在输入、状态、运动、动画事实或 snapshot 字段
- **AND** MUST 输出 restore tick、end tick 和当前 pipeline step 或等价阶段信息

#### Scenario: 同输入序列收敛
- **WHEN** 使用相同 `PredictionInputFrame` 序列、相同配置和相同 tick rate 重放 Move、Run、TurnBack 和 Dodge
- **THEN** replay 后的 FullBody 状态、运动根位置/朝向、输入消费状态和 runtime blackboard facts MUST 在定义容差内收敛
