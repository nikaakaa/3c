# Change: 退役 Character Submitter Graph/Chain 边界

## Why
旧 `CharacterFrameSubmitterGraph` / `CharacterFrameSubmitterChain` 都是迁移期顺序组合，容易被误读为正式 behavior graph 或长期 chain 扩展点。当前目标架构已经收口到 `CharacterBehaviorSubmissionRunner`：Locomotion 与 Committed Action 是 Character frame owner 下的 sibling submitters，FullBody 只表达 body/channel claim 语义。

## What Changes
- 删除 `CharacterFrameSubmitterGraph` / `CharacterFrameSubmitterChain` 作为正式类型或扩展入口。
- 将正式提交组合收束到 `CharacterBehaviorSubmissionRunner` 或批准的等价 behavior submission runner。
- 固定当前 request pass 与 output pass 中 Locomotion、Action 的顺序依赖。
- 明确 Locomotion 当前先填充 context，Action 后续消费 context 的依赖关系。
- 不接入新的 behavior execution tree，不改变 `CharacterFramePipeline` phase 顺序。
- 增加测试证明退役旧 graph/chain 后不改变 Locomotion、Dodge 和 frame plan 行为。

## Implementation Slices
1. **Inventory slice**：列出现有 submission runner 调用顺序和 context dependency。
2. **Retire slice**：删除旧 graph/chain 类型和正式引用。
3. **Boundary slice**：用测试固定 Locomotion 先准备 facts、Action 后消费 facts 的顺序。
4. **Compatibility slice**：确认 default runtime host 行为不变。

## Acceptance Criteria
- 生产代码中 submitter 顺序组合不再叫 `Graph` 或 `Chain`。
- 正式入口使用 `CharacterBehaviorSubmissionRunner` 或批准等价 runner。
- Locomotion request/output 准备仍先于 Action 消费 context。
- `CharacterFramePipeline` phase 顺序不变。
- Directional Dodge、Backstep Dodge、基础移动相关测试保持通过。
- 没有新增 behavior tree runner 或 default entry replacement。

## Stop Conditions
- 如果实现需要引入 behavior tree runner，必须停止，移到 `add-character-behavior-submission-entry`。
- 如果实现需要改变 Locomotion / Action 调用顺序，必须停止并重审 context dependency。
- 如果实现需要迁移 Dodge timeline 权威，必须停止，移到 `migrate-dodge-to-behavior-timeline`。

## Non-Goals
- 不新增 authoring graph 数据模型。
- 不重写 Locomotion / Action submitter。
- 不迁移 ActionBranch / Dodge。

## Dependencies
- MAY 在 `add-character-behavior-submission-contracts` 前后实施。
- SHOULD 先于 `add-character-behavior-submission-entry`。

## Impact
- Affected specs:
  - `character-submitter-chain-boundary`
  - related: `character-frame-pipeline`
- Affected code:
  - `Assets/Scripts/Character/Pipeline/Runtime/CharacterFrameSubmitterChain.cs`
  - `Assets/Scripts/Character/Behavior/Runtime/CharacterBehaviorSubmissionRunner.cs`
  - `Assets/Scripts/Character/Pipeline/Runtime/CharacterRuntimeCore.cs`
  - `Assets/Tests/Editor/Character/*`
