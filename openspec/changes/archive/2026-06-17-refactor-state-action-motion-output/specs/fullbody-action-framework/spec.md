## ADDED Requirements
### Requirement: FullBody 使用 Action Motion Resolver
FullBodySubmissionBuilder 或等价提交构建器 MUST 在状态机输出 action motion spec 后，通过 Action motion resolver 生成本帧动作运动命令，并把 resolver result 纳入 `CharacterFrameSubmission` 或等价角色级提交结果。运动执行仍 MUST 由现有 motion executor 经 Character output applier 执行。

#### Scenario: Resolver 位于执行前
- **WHEN** FullBodySubmissionBuilder 处理 Action 状态输出
- **THEN** 提交构建器 MUST 先读取 action motion spec
- **AND** MUST 调用 Action motion resolver 生成 `ActionMovementCommand`
- **AND** MUST 再把 resolver result 交给 Character output applier 提交 `IActionMovementExecutor`

#### Scenario: 单一运动执行路径保持
- **WHEN** Action motion resolver 产出动作运动命令
- **THEN** Character output applier MUST 继续通过统一 motion executor 执行
- **AND** resolver MUST NOT 直接移动角色
- **AND** 状态机 runner MUST NOT 直接移动角色

#### Scenario: Action facts 来自 resolver result
- **WHEN** Character output applier 写入 runtime blackboard action facts
- **THEN** action movement distance、completed 和 exited-to-locomotion 派生 MUST 来自状态机 frame 与 Action motion resolver result
- **AND** MUST NOT 从 output resolver 中重复计算动作完成状态

#### Scenario: Resolver 结果进入帧结果
- **WHEN** FullBodySubmissionBuilder 完成 Action motion submission 构建
- **THEN** `CharacterFrameSubmission` 或等价角色级提交结果 MUST 能暴露 action motion resolver result
- **AND** 测试 MUST 能通过该结果验证 motion command、completed 和 run latch 派生
