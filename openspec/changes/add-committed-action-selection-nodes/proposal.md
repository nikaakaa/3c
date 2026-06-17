# Change: 增加 Committed Action 选择节点

## Why
当前 `CommittedActionBranch` 第一版只支持单个 TimelineNode，Dodge 的 Directional / Backstep 仍由旧 variant 字段和请求解析决定。要让提交型行为内部成为可编辑的技能节点树，必须先支持最小选择语义：根据只读 facts / request context 选择一个 timeline，而不是继续把 variant 逻辑写死在 Dodge resolver。

## What Changes
- 扩展 Action node 合同，新增 Selector、Condition 和 Timeline 三类最小节点。
- 扩展 CommittedActionBranch evaluator，使其能在确定性顺序中选择一个 timeline。
- 定义 Action condition 只读输入范围：request facts、locomotion facts、blackboard snapshot、action context 或批准的等价纯数据。
- 保持 ActionTimeline 作为 leaf 内部时序数据，不直接执行副作用。
- 增加测试覆盖 selected timeline 输出、未选中 timeline 不输出、condition 不写状态、selector 顺序确定。

## Implementation Slices
1. **Model slice**：扩展 CommittedActionNodeDefinition，使它能表达 selector、condition、timeline 和 child 顺序。
2. **Context slice**：定义 CommittedActionBranchEvaluationContext，只暴露只读 facts/snapshot/request context。
3. **Condition slice**：实现最小 condition evaluator，先满足 Dodge Directional / Backstep 的选择需要。
4. **Selector slice**：按稳定 child 顺序选择一个 child，并保证未选中 child 无输出。
5. **Compatibility slice**：保持现有单 Timeline root 的 CommittedActionBranch 行为不变。

## Acceptance Criteria
- 现有单 timeline CommittedActionBranch 测试继续通过。
- Selector 可基于只读 context 选择 Directional 或 Backstep timeline。
- 未选中 timeline 的 motion、animation、fact、cue 均不进入 outcome。
- Condition evaluator 不写 blackboard、不消费 input、不改 lifecycle。
- Action request / interrupt 仲裁仍在 Action resolver / arbiter，不被 selector 取代。

## Stop Conditions
- 如果 selector 需要决定一个 action request 是否 accepted，必须停止；那属于 Action request arbitration。
- 如果 condition 需要写黑板或消费输入，必须停止；那属于 frame output apply。
- 如果要支持 decorator abort、service、parallel action subgraph，必须另开 proposal。

## Non-Goals
- 不实现完整通用行为树。
- 不实现 editor UI。
- 不迁移 Dodge 资产权威。
- 不实现 combo、cooldown、cost、damage、hitbox physics 或 presentation cue runtime。
- 不改变 `CharacterFramePipeline` 和 behavior submission entry 的职责。

## Dependencies
- MUST 在 `add-character-behavior-submission-contracts` 后实施。
- SHOULD 在 `refactor-character-graph-naming-boundaries` 后实施，以使用统一命名。
- SHOULD 先于 `migrate-dodge-to-behavior-timeline` 实施。

## Impact
- Affected specs:
  - `committed-action-node-selection`
  - related: `action-domain-runtime`
  - related: `character-action-catalog`
  - related: `dodge-action`
- Affected code:
  - `Assets/Scripts/Character/Action/Branch/Model/*`
  - `Assets/Scripts/Character/Action/Branch/Solver/*`
  - `Assets/Scripts/Character/Action/Timeline/*`
  - `Assets/Tests/Editor/Character/Action/*`
