# character-submitter-chain-boundary Specification

## Purpose
记录旧 CharacterFrameSubmitterGraph / Chain 的退役边界，以及 LocomotionSource 与 CommittedActionSource 在 behavior submission runner 中的顺序依赖。
## Requirements
### Requirement: 旧 Submitter Graph/Chain 退役
系统 MUST 删除旧 `CharacterFrameSubmitterGraph` / `CharacterFrameSubmitterChain` 迁移结构。正式角色帧提交组合 MUST 由 `CharacterBehaviorSubmissionRunner` 或批准的等价 behavior submission runner 收敛 Locomotion submitter 与 Committed Action submitter。该结构 MUST NOT 使用 Graph 命名，也 MUST NOT 被当作 authoring graph。

#### Scenario: Submitter 组合不再叫 Graph 或 Chain
- **WHEN** 检查正式 frame submitter 组合类型
- **THEN** 名称 MUST 表达 behavior submission runner 职责
- **AND** MUST NOT 继续叫 `CharacterFrameSubmitterGraph`
- **AND** MUST NOT 继续保留 `CharacterFrameSubmitterChain` 作为正式类型

#### Scenario: Runner 不冒充 authoring graph
- **WHEN** 后续 proposal 接入 behavior execution tree
- **THEN** 它 MUST 通过独立 behavior entry 接入
- **AND** MUST NOT 将旧 submitter graph/chain 直接声明为行为树 runtime

### Requirement: Locomotion 到 Action 的 Context Dependency 明确
当前 behavior submission runner MUST 明确 Locomotion 先准备本帧 movement facts、timeline facts、state frame 或 locomotion frame，Committed Action 后续消费这些 context 来解析 request、推进 lifecycle 和构建最终输出。该顺序 MUST 有自动测试覆盖。

#### Scenario: Request stage 顺序
- **WHEN** frame pipeline 进入 GameplayDecision 或等价 request stage
- **THEN** Locomotion submitter MUST 先准备本帧 locomotion decision / facts
- **AND** Committed Action submitter MUST 后消费这些 facts 解析 action request

#### Scenario: Output stage 顺序
- **WHEN** frame pipeline 进入 BuildMotion 或等价 output stage
- **THEN** Locomotion submitter MUST 先写入 state frame / locomotion frame context
- **AND** Committed Action submitter MUST 后基于该 context 构建 final frame submission 或等价 output

### Requirement: Submission Runner 收束不改变运行语义
Behavior submission runner 的命名和边界收束 MUST NOT 改变 Locomotion、Dodge、Action lifecycle、ActionTimeline outcome、BodyArbiter 或 output applier 的运行语义。

#### Scenario: Dodge 行为保持
- **WHEN** behavior submission runner 收束完成
- **THEN** Directional Dodge 与 Backstep Dodge 的 accepted/rejected、motion、animation、input consume 和 Run latch 行为 MUST 保持不变

#### Scenario: Pipeline phase 保持
- **WHEN** behavior submission runner 收束完成
- **THEN** `CharacterFramePipeline` phase 顺序 MUST 保持不变
- **AND** output applier MUST 仍是唯一副作用出口

### Requirement: Submission Boundary 可测试和可验证
系统 MUST 提供自动测试和静态边界验证，证明正式 behavior submission runner 不恢复旧 graph/chain 迁移链路，也不是 authoring graph runtime。

#### Scenario: 自动测试覆盖链路顺序
- **WHEN** 运行 behavior submission boundary EditMode 测试
- **THEN** 测试 MUST 覆盖 request stage 顺序、output stage 顺序和缺失 context 的明确失败

#### Scenario: 静态边界验证
- **WHEN** 检查正式 runtime 源码
- **THEN** 静态测试 MUST 确认旧 `CharacterFrameSubmitterGraph` 名称不再作为正式类型或新扩展入口存在
- **AND** MUST 确认旧 `CharacterFrameSubmitterChain` 名称不再作为正式类型或新扩展入口存在
