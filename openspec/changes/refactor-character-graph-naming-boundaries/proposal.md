# Change: 收束 Character Graph 与 Submitter 命名边界

## Why
当前源码和 proposal 语言里同时存在 `CharacterGraphDefinition`、`CharacterExecutionNodeTree`、`CharacterFrameSubmitterGraph`、`ActionBranch` 等名称。它们职责不同，但都容易被理解成“正式行为图”。这会让后续目标混淆：submitter chain 不是图，authoring graph 不是 runtime runner，ActionBranch 也不是顶层行为二分。

本变更只做命名与边界收束，使源码名称能反映真实职责，并为后续行为树、Action selection nodes 和编辑器 adapter 提供清晰语言。

## What Changes
- 将 authoring graph 与 runtime execution tree 的命名边界写清。
- 将 `CharacterGraphDefinition` 正式重命名为 `CharacterBehaviorGraphDefinition`。
- 将 `CharacterExecutionNodeTree` 正式重命名为 `CharacterBehaviorExecutionTree`。
- 将 `ActionBranch` 正式重命名为 `CommittedActionBranch`。
- 将 `CommittedActionBranch` 的语义限定为 Action module 内部 committed behavior 分支，不作为顶层行为分类。
- 增加 rename / compatibility / static boundary 测试，避免旧名称继续作为正式扩展入口。

## Naming Decisions To Apply
本变更不再保留候选名，正式映射如下：

```text
CharacterGraphDefinition -> CharacterBehaviorGraphDefinition
CharacterExecutionNodeTree -> CharacterBehaviorExecutionTree
ActionBranch -> CommittedActionBranch
```

`CharacterFrameSubmitterGraph -> CharacterFrameSubmitterChain` 归属 `refactor-character-submitter-chain-boundary`，本变更只消费其结果。

## Acceptance Criteria
- 生产代码中不再把 submitter chain 命名为 graph。
- 文档和测试中能明确区分 authoring graph 与 runtime execution tree。
- Action 不再被描述为“行为树之外的另一半”，只作为 committed behavior 领域实现。
- `ActionBranch` 旧名不再作为正式类型名进入新扩展点。
- Rename 不改变 Locomotion、Dodge、ActionTimeline 和 frame pipeline 的行为测试结果。
- 旧名称若短期保留，必须有迁移用途或兼容 adapter 说明，不能作为新增功能入口。

## Stop Conditions
- 如果 rename 需要改变 runtime 语义，必须停止并拆回对应功能 proposal。
- 如果发现某旧名称仍被 prefab、scene 或 config 作为正式入口引用，必须先补迁移计划。
- 如果 rename 触碰 Action/Dodge 具体行为逻辑，必须停止并移入后续 action/dodge proposal。

## Non-Goals
- 不新增行为树运行能力。
- 不实现节点编辑器 UI。
- 不迁移 Dodge runtime 语义。
- 不重写 `CharacterFramePipeline` phase 顺序。

## Dependencies
- SHOULD 在 `refactor-character-submitter-chain-boundary` 后实施，避免重复处理 submitter chain 命名。
- SHOULD 在 `add-character-behavior-submission-contracts` 后实施，确保 behavior submission 语言已固定。
- MAY 与 `add-character-behavior-submission-entry` 相邻实施，但 MUST 独立验证。

## Impact
- Affected specs:
  - `character-behavior-boundaries`
  - related: `character-frame-pipeline`
  - related: `project-structure`
  - related: `action-domain-runtime`
- Affected code:
  - `Assets/Scripts/Character/Graph/*`
  - `Assets/Scripts/Character/Pipeline/Runtime/CharacterFrameSubmitterGraph.cs`
  - `Assets/Scripts/Character/Action/Branch/*`
  - `Assets/Tests/Editor/Character/*`
