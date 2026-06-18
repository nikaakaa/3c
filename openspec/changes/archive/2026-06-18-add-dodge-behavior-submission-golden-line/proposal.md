# Change: 建立 Dodge Behavior Submission 金线

## Why
如果先实现通用 behavior entry，再找业务实例验证，很容易搭出一层漂亮但无业务压力的新抽象。Dodge 是当前最完整的承诺型行为实例，必须提前成为金线：旧生产路径输出与新 behavior submission 表达逐项对比，证明 submission 合同能承载真实动作，而不替换默认入口。

## What Changes
- 新增 Dodge golden line 测试 fixture，使用当前正式 Dodge 路径作为基准。
- 将旧路径的 accepted request、body claim、motion、animation、input consume、window facts、cue/diagnostics、Run latch 相关输出映射为 behavior submission。
- 对比旧路径输出与 behavior submission 表达是否等价。
- 覆盖 Directional、Backstep、rejected request、animation-end 等待、Run latch、动作结束后再次触发和 restore 一致性。
- 不替换默认 runtime host，不改变生产路径。

## Implementation Slices
1. **Baseline capture slice**：捕获当前 Dodge 旧路径输出。
2. **Submission mapping slice**：将旧路径输出映射成 typed behavior submissions。
3. **Comparison slice**：逐项比较旧输出与 submission 表达。
4. **Regression slice**：覆盖 Directional、Backstep、rejected、Run latch 和 restore。
5. **Boundary slice**：确认 golden line 只在测试或 adapter 层，不成为第二生产路径。

## Acceptance Criteria
- Directional Dodge 旧路径输出能被 behavior submission 无损表达。
- Backstep Dodge 旧路径输出能被 behavior submission 无损表达。
- Rejected request 不产生 output submission，也不消费 input。
- Run latch 语义能被 submission / frame output candidate 明确表达。
- Golden line 不替换 `CharacterRuntimeCore` 默认入口。
- Golden line 不引入第二 motion executor、第二 animation presenter 或第二 blackboard write path。

## Stop Conditions
- 如果 behavior submission 合同无法表达 Dodge 的某个必要输出，必须停止并回到 `add-character-behavior-submission-contracts` 修改合同。
- 如果测试需要修改 Dodge 生产行为才能通过，必须停止。
- 如果需要迁移 Dodge timeline 权威，必须停止，移到 `migrate-dodge-to-behavior-timeline`。

## Non-Goals
- 不正式迁移 Dodge 到 selector + timeline。
- 不替换 submitter chain 默认入口。
- 不新增 Action selection nodes。
- 不做编辑器 UI。

## Dependencies
- MUST 在 `add-character-behavior-submission-contracts` 后实施。
- SHOULD 在 `refactor-character-submitter-chain-boundary` 后实施，以便基线顺序明确。
- MUST 先于 `add-character-behavior-submission-entry`。

## Impact
- Affected specs:
  - `dodge-behavior-submission-golden-line`
  - related: `dodge-action`
  - related: `character-behavior-submission-contracts`
  - related: `character-frame-pipeline`
- Affected code:
  - `Assets/Tests/Editor/Character/Action/*`
  - `Assets/Tests/Editor/Character/Behavior/*`
  - possible test adapters under `Assets/Scripts/Character/Behavior/Solver/*`
