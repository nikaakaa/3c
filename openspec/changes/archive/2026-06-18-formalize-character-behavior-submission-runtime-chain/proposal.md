# Change: 正式化 Character Behavior Submission 运行时闭环

## 背景
当前 `CharacterBehaviorSubmissionRunner` 已经存在，并且 `CharacterFramePipeline` 可以通过 request submitter / output submitter 接入它。但当前 leaf 主要包裹旧 `LocomotionFrameSubmitter` 和 `CommittedActionFrameSubmitter`，`CharacterBehaviorSubmissionComposer` 也依赖“最后一个 required output”选择最终提交。这证明入口已经迁移，但 typed source submission、channel 映射和 composer 规则还没有完全正式化。

本变更负责把 behavior submission 从“包一层旧 submitter”推进到“LocomotionSource / CommittedActionSource 提交明确 typed submissions，Composer 明确合成本帧 CharacterFrameSubmission”的运行时闭环。

## 目标
- 保持最高入口：`CharacterRuntimeCore -> CharacterFramePipeline`。
- 保持固定帧 phase：`CharacterFramePipeline` 不新增第二套 phase。
- 明确 `CharacterBehaviorSubmissionRunner` 是 pipeline 内部正式 request/output submitter 组合入口。
- 将 Locomotion 和 CommittedAction 的 request/output 拆成可审计 typed source submissions。
- 明确 Dodge timeline outcome 到 Motion、Animation、Window、Cue channel 的映射。
- 让 composer 显式消费 LocomotionSource 与 CommittedActionSource，而不是长期依赖 pass-through 或最后 required output。
- 保持 `DefaultBodyArbiter` / `CharacterFramePlan` / output applier 为唯一仲裁与副作用出口。

## 非目标
- 不改变 Editor timeline UI；该部分由 `migrate-ref-timeline-editor-to-formal-action-config` 负责。
- 不改变 authoring 数据源；该部分由 `refactor-character-behavior-authoring-source-boundary` 负责。
- 不实现 UpperBody runtime source。
- 不新增第二 motion executor、第二 animation presenter、第二 blackboard writer、第二角色控制入口。
- 不让 Ref runner、Taco tree、TimelinePlayer、PlayableGraph 进入正式 gameplay。

## 影响范围
- Affected specs: `character-behavior-submission-runtime-chain`。
- Affected code:
  - `Assets/Scripts/Character/Behavior/Model/...`
  - `Assets/Scripts/Character/Behavior/Runtime/...`
  - `Assets/Scripts/Character/Behavior/Solver/...`
  - `Assets/Scripts/Character/Pipeline/Runtime/...`
  - `Assets/Scripts/Character/Action/Runtime/...`
  - `Assets/Scripts/Character/Action/Branch/...`
  - `Assets/Scripts/Character/Action/Timeline/...`
  - `Assets/Tests/Editor/Character/Behavior/...`
- Related active changes:
  - `refactor-character-behavior-authoring-source-boundary`
  - `migrate-ref-timeline-editor-to-formal-action-config`

## 验证
- `openspec validate formalize-character-behavior-submission-runtime-chain --strict --no-interactive`
- EditMode 测试覆盖：
  - runtime core / pipeline 使用 behavior runner。
  - RequestPass 和 OutputPass 顺序。
  - LocomotionSource typed submission。
  - CommittedActionSource typed submission。
  - Dodge timeline outcome channel 映射。
  - Composer 显式合成规则。
  - runtime 不引用 Editor、Ref runner、Taco tree、TimelinePlayer、PlayableGraph。
