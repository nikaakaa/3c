# Change: 收束 Character Submitter Chain 边界

## Why
当前 `CharacterFrameSubmitterGraph` 名称暗示它是行为图，但源码职责只是顺序调用 Locomotion submitter 与 FullBody Action submitter。这个误导会让后续 behavior tree 入口和现有 submitter chain 混淆。本变更只收束当前链路命名和职责，不改变运行语义。

## What Changes
- 将 `CharacterFrameSubmitterGraph` 的正式语义收束为 submitter chain / composite / collection。
- 固定当前 request pass 与 output pass 中 Locomotion、Action 的顺序依赖。
- 明确 Locomotion 当前先填充 context，Action 后续消费 context 的依赖关系。
- 不接入新的 behavior execution tree，不改变 `CharacterFramePipeline` phase 顺序。
- 增加测试证明重命名 / 边界收束不改变 Locomotion、Dodge 和 frame plan 行为。

## Implementation Slices
1. **Inventory slice**：列出现有 submitter chain 调用顺序和 context dependency。
2. **Rename slice**：将误导性的 graph 命名收束为 chain / composite。
3. **Boundary slice**：用测试固定 Locomotion 先准备 facts、Action 后消费 facts 的顺序。
4. **Compatibility slice**：确认 default runtime host 行为不变。

## Acceptance Criteria
- 生产代码中 submitter 顺序组合不再叫 `Graph`。
- Locomotion request/output 准备仍先于 Action 消费 context。
- `CharacterFramePipeline` phase 顺序不变。
- Directional Dodge、Backstep Dodge、基础移动相关测试保持通过。
- 没有新增 behavior tree runner 或 default entry replacement。

## Stop Conditions
- 如果实现需要引入 behavior tree runner，必须停止，移到 `add-character-behavior-submission-entry`。
- 如果实现需要改变 Locomotion / Action 调用顺序，必须停止并重审 context dependency。
- 如果实现需要迁移 Dodge timeline 权威，必须停止，移到 `migrate-dodge-to-behavior-timeline`。

## Non-Goals
- 不新增 behavior submission 数据模型。
- 不替换默认 submitter 入口。
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
  - `Assets/Scripts/Character/Pipeline/Runtime/CharacterFrameSubmitterGraph.cs`
  - `Assets/Scripts/Character/Pipeline/Runtime/CharacterRuntimeCore.cs`
  - `Assets/Tests/Editor/Character/*`
