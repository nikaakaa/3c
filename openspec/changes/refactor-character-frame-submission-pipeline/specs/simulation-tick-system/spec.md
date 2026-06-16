## MODIFIED Requirements
### Requirement: FullBody Gameplay Phase 接入
系统 MUST 让当前 FullBody gameplay 主线通过唯一 Character frame pipeline 接入 `SimulationTickPhase` 的固定顺序，而不是将输入缓冲更新、玩法判定、运动构建、运动执行和表现提交整包放入单个 `ExecuteMotion` handler。tick runner 仍只负责调度，具体玩法逻辑必须位于 Character frame pipeline、FullBody 提交职责或其 adapter 中。

#### Scenario: 输入缓冲早于玩法判定
- **WHEN** tick runner 执行 tick N
- **THEN** FullBody 输入请求缓冲更新 MUST 发生在 `UpdateInputBuffer` phase
- **AND** FullBody Action 请求仲裁 MUST 发生在 `GameplayDecision` phase 或之后
- **AND** Action 仲裁 MUST 能看到同 tick 写入的输入请求
- **AND** 这些 phase MUST 由 Character frame pipeline 统一推进

#### Scenario: 状态决策早于运动执行
- **WHEN** tick runner 执行 tick N
- **THEN** FullBody 统一状态机推进 MUST 发生在 `GameplayDecision` phase
- **AND** 运动命令构建 MUST 发生在 `BuildMotion` phase
- **AND** motion executor 调用 MUST 只发生在 `ExecuteMotion` phase
- **AND** motion executor 调用 MUST 来自 Character frame pipeline 的统一 output applier

#### Scenario: 表现提交不早于运动执行
- **WHEN** tick runner 执行 tick N
- **THEN** FullBody base layer 动画命令提交 MUST 发生在运动命令已构建之后
- **AND** 动画播放事实写入 MUST 不作为同 tick 状态进入的前置权威
- **AND** 动画提交 MUST 来自 Character frame pipeline 的统一 output applier

#### Scenario: 快照晚于 Character 输出
- **WHEN** `WriteSnapshotAndEvents` phase 捕获角色快照
- **THEN** 本 tick 的 FullBody 状态、输入消费、运动执行结果和 runtime facts 写入 MUST 已完成
- **AND** 快照 recorder MUST NOT 需要主动重跑 gameplay 逻辑来补齐状态
- **AND** snapshot/events commit MUST 发生在 Character frame pipeline 的统一提交阶段

### Requirement: Phase Handler 不形成旁路
系统 MUST 防止 FullBody phase handler、Locomotion-only phase handler、rollback/debug phase handler 和未来身体域 handler 形成多条 gameplay 推进路径。保留的 handler MUST 明确标识用途，并且不得在同一角色同一 tick 中重复推进状态机或重复执行运动。正式 gameplay handler MUST 进入唯一 Character frame pipeline。

#### Scenario: Character handler 是动作 demo 主路径
- **GIVEN** 当前 Sandbox 使用 FullBody 动作 demo
- **WHEN** tick driver 推进角色
- **THEN** Character frame pipeline handler MUST 是 Move、TurnBack、Dodge 和后续 Attack 的主 gameplay 推进路径
- **AND** FullBody MUST 作为该管线下的提交来源
- **AND** locomotion-only handler MUST 不同时推进同一角色

#### Scenario: Locomotion-only handler 明确窄用途
- **GIVEN** 测试或诊断需要 locomotion-only replay
- **WHEN** 使用 locomotion-only handler
- **THEN** handler MUST 明确标识为 locomotion-only
- **AND** MUST NOT 被作为 FullBody 动作 demo 的完整验收路径
- **AND** MUST NOT 与 Character frame pipeline 同 tick 推进同一角色

#### Scenario: Debug handler 不推进 gameplay
- **WHEN** rollback debug runner、snapshot recorder 或 presentation probe 注册到 tick phase
- **THEN** 它们 MUST 只记录、恢复或比较数据
- **AND** MUST NOT 调用状态机 Tick 或 motion executor 作为正常 gameplay 推进
- **AND** MUST NOT 绕过 Character frame pipeline 提交角色 gameplay 副作用
