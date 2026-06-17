## ADDED Requirements
### Requirement: 退役单一 FullBody frame submission 权威
系统 MUST 将 `CharacterFrameSubmissionSource.FullBody` 或等价单一 FullBody 来源从正式 output authority 中退役。迁移期可以继续用 FullBody submission 喂给 legacy adapter，但最终运动、动画、输入消费、runtime facts 和 diagnostics 的正式选择 MUST 来自 `CharacterFramePlan` 或等价角色级计划。

#### Scenario: Plan 是正式输出选择
- **GIVEN** Locomotion 和 FullBody Action 已提交候选输出或 occupancy claim
- **WHEN** output composer 生成本帧结果
- **THEN** composer MUST 以 `CharacterFramePlan` 或等价角色级计划表达最终选择
- **AND** MUST NOT 以 `CharacterFrameSubmissionSource.FullBody` 作为最终输出权威

#### Scenario: Legacy submission 只作为迁移输入
- **GIVEN** 当前实现仍需要 `CharacterFrameSubmission` 承载 FullBody 集成结果
- **WHEN** 该 submission 进入 output composer
- **THEN** composer MAY 将它转换为 `CharacterFramePlan`
- **AND** 该路径 MUST 被标记为 legacy 或 integrated adapter
- **AND** 后续新增身体域 MUST NOT 依赖该单一 FullBody source 参与正式仲裁

### Requirement: Output composer 不得长期保持 pass-through
系统 MUST 让角色级 output composer 承担 plan 合成或 plan 选择职责。若保留 `Compose(CharacterFrameSubmission)` 或等价 legacy overload，它 MUST 只作为迁移 Adapter，并且 MUST 有自动测试覆盖其删除条件。

#### Scenario: Composer 消费 plan
- **WHEN** 正式角色帧管线进入 BuildMotion 或等价 plan build 阶段
- **THEN** output composer MUST 能消费 `CharacterFramePlan` 或等价角色级计划
- **AND** MUST 保留 body occupancy、motion 选择、animation 选择、input consume 和 runtime facts 的最终选择结果

#### Scenario: Legacy overload 有删除条件
- **WHEN** 代码中仍存在从单个 FullBody submission 到 output 的 overload
- **THEN** 测试 MUST 标记该 overload 为 legacy adapter
- **AND** MUST 证明正式 plan path 已覆盖 Corin 当前 Locomotion 与 FullBody Action 主线
- **AND** 后续迁移完成后该 overload MUST 被删除或移出正式运行时路径

### Requirement: 角色级管线不承担身体域退役策略
`CharacterFramePipeline` MUST 继续只负责 phase 顺序、调用 submitter/composer/applier 和传播结果。FullBody 集成路径退役、Locomotion submitter 拆分、FullBody Action submitter 拆分和 body occupancy 规则 MUST 位于独立 Module 或 spec 约束中，不得写成 pipeline 本体的特殊分支。

#### Scenario: Pipeline 不硬编码退役分支
- **WHEN** 检查 `CharacterFramePipeline` 核心逻辑
- **THEN** pipeline MUST NOT 通过具体 `FullBodySubmissionBuilder` 类型判断退役路径
- **AND** MUST NOT 通过具体 `CharacterFrameSubmissionSource.FullBody` 判断最终输出
- **AND** MUST NOT 在 phase switch 中写入 UpperBody、FullBody Action 或 Locomotion 的业务优先级
