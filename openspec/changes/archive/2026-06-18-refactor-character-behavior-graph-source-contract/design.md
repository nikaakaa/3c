# Design: Character Behavior Graph Source 合同收敛

## Context
当前主线使用六层语义：Source、Action、Claim、Slot、Channel、Presentation Layer。

这次变更只修改 Source 与 ActionDefinition 的数据源归属：

- Source：Behavior Graph 表达哪些行为来源参与角色帧，例如 Locomotion leaf、CommittedAction leaf、未来 UpperBody leaf。
- Action：ActionDefinition 表达动作语义、selector、timeline、track、clip、motion payload、animation key、window 和 cue。

## Goals
- 让 Behavior Graph 成为 source topology authoring Module，而不是动作内容 authoring Module。
- 让 `CharacterActionDefinitionSO` 或 action catalog 成为 Committed Action 数据源。
- 让 compiler、editor 和测试只通过正式 Interface 验证数据归属。
- 保留 Graph Editor 打开或定位 Timeline Editor 的能力，但不复制 timeline 数据。

## Non-Goals
- 不实现新的 Timeline Editor 交互。
- 不迁移 Ref timeline UI。
- 不新增 UpperBody、Cue 或 Facial runtime source。
- 不改 runtime evaluator 的执行语义。
- 不把 Dodge timeline 数据迁回 Behavior Graph。

## Decisions
### Decision: Graph Interface 只表达 Source 拓扑
`CharacterBehaviorGraphDefinition` 或等价 authoring asset 只保存 root、composite、source leaf、port、edge、editor position、schema version 和必要的 source reference。

它不保存 Dodge selector、Directional timeline、Backstep timeline、track、clip、motion payload、animation key、window 或 cue。

### Decision: ActionDefinition 持有 Action 内容
`CharacterActionDefinitionSO`、action catalog 或批准的等价 ActionDefinition 持有 committed action branch、selector、timeline、track、clip 和 payload。

Behavior graph 可以引用或定位 ActionDefinition，但不能在缺少 ActionDefinition 时生成隐藏默认 branch。

### Decision: Compiler 职责分离
Behavior compiler 只把 source graph 编译为 source runtime definition、execution topology 或等价提交结构。

ActionDefinition compiler/validator 负责生成 `CommittedActionBranchDefinition`、`ActionTimelineDefinition` 和对应校验结果。

组合层可以同时读取两者，但不得把 Action timeline payload 塞回 graph compiler。

## Risks / Trade-offs
- 风险：正式 spec 与已完成 source-boundary change 语义重复。
  - 缓解：本 change 直接修改 `character-behavior-graph-contracts`，不新增平行 capability。
- 风险：Graph Editor 仍需要提供打开 Dodge Timeline 的入口。
  - 缓解：Graph Editor 只传递或选择正式 ActionDefinition，不复制 timeline 数据。
- 风险：legacy embedded Dodge 字段仍存在于旧 asset。
  - 缓解：legacy 字段只允许产生迁移诊断或一次性迁移，不作为正式 fallback。

## Validation Strategy
- OpenSpec 严格校验本 change。
- EditMode 测试覆盖 Graph compiler 不输出 timeline payload。
- EditMode 测试覆盖 ActionDefinition 编译 Dodge selector/timeline。
- EditMode 测试覆盖 Graph Editor 保存 topology/editor position 不修改 action timeline。
- 静态边界测试覆盖 behavior graph authoring schema 不暴露 timeline/track/clip 字段作为正式数据源。
