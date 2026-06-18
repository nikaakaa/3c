# Change: 重构 Character Body Slot 结果模型

## Why
当前问题不是“大家不懂术语”，而是 runtime 数据模型仍把 gameplay slot、body claim、旧 FullBody 命名和 animation layer 口径混在一起。`BodyOccupancyDecision.BaseLayerOwner`、旧 action-side owner 命名这类口径会继续把后续 Editor、Timeline、compiler 和 runtime 引回旧 FullBody 主线语义。

因此本 change 不再以解释文档为目标，而是把角色身体仲裁结果改成明确的 slot contract：谁提交 claim 是一层，claim 占用什么身体资源是一层，最终哪个 slot 被谁拥有又是另一层。后续 Character Behavior Editor 和 Committed Action Timeline Editor 只能消费这个 slot contract，不能再把 FullBody 当节点或表现层。

## What Changes
- 正式引入角色身体 slot 结果模型，用 `BaseSlot`、`UpperBodySlot` 或批准的等价类型表达 frame plan 中的身体资源位置。
- 正式区分 claim 和 slot owner：`FullBody` 是 claim；Dodge 被采纳后产生的是 CommittedAction 对 `BaseSlot` 的 ownership，并压制 `UpperBodySlot`。
- `BodyOccupancyDecision` 和 `CharacterFramePlan` MUST 暴露 slot 口径结果；`BaseLayerOwner` / `UpperBodyOwner` 等 layer 口径不得作为新代码、新测试、新 editor/compiler 的正式契约，并从当前正式代码面删除。
- 旧 action-side owner 名称 MUST 收敛为 `CharacterBodyDomain.CommittedAction` 或批准的等价名称；正式输出 MUST 使用 CommittedAction / Action-side owner 口径。
- `UpperBody` 仍只作为 claim/slot 扩展边界存在，本 change 不实现 UpperBody runtime source、masked layer 或并行 gameplay tick。
- `Facial` / `FaceBody` 不进入当前 BodyArbiter、FramePlan 或 rollback snapshot；后续必须先决定它是 channel、presentation slot 还是 gameplay slot。
- 更新自动测试，证明 Dodge、Locomotion、UpperBody suppression、非法 Facial 字段和旧 FullBody 节点边界都由 slot contract 约束。

## Impact
- Affected specs:
  - `character-body-slot-data-model`
  - `action-domain-runtime`
  - `character-frame-pipeline`
  - `dodge-action`
- Affected docs:
  - `docs/Goal/character-body-slot-data-model-goal.md`
  - `AGENTS.md`
  - `openspec/project.md`
- Affected runtime code after approval:
  - `Assets/Scripts/Character/Pipeline/Model/CharacterBodyArbitration.cs`
  - `Assets/Scripts/Character/Pipeline/Model/CharacterFramePlan.cs`
  - `Assets/Scripts/Character/Pipeline/Runtime/DefaultBodyArbiter.cs`
  - consumers of `BaseLayerOwner` / `UpperBodyOwner`
- Affected tests:
  - `Assets/Tests/Editor/CharacterFrameArbitrationTests.cs`
  - related behavior submission / dodge golden-line tests if they assert old owner names

## Not Included
- 不实现 UpperBody runtime source。
- 不实现 Facial slot 或 facial runtime。
- 不实现 Editor UI、Timeline UI、preview 或 compiler。
- 不重写 `CharacterRuntimeCore`。
- 不改变 `CharacterFramePipeline` phase 顺序。
- 不引入第二 motion executor、第二 animation presenter、第二 blackboard writer 或第二角色控制入口。
