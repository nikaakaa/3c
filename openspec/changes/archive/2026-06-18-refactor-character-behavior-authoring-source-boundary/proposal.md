# Change: 收敛 Character Behavior Authoring 数据源边界

## 背景
当前项目已经有 `CharacterBehaviorSubmissionRunner`、`CharacterBehaviorAuthoringAsset`、`CharacterActionDefinitionSO`、Dodge selector/timeline 和 Editor 窗口，但数据源边界仍然混杂：

- Character Behavior Graph 既像 source graph，又曾携带 Dodge branch/timeline 数据。
- Committed Action Timeline Editor 已经改为编辑正式 `CharacterActionDefinitionSO`。
- `CharacterBehaviorAuthoringCompiler` 仍然能从 behavior authoring asset 内部编译 Dodge branch。
- Graph Editor 和 Timeline Editor 之间缺少明确关系，容易再次出现 sample-only asset、重复 Dodge 数据或 Editor 视图变成 gameplay 权威。

本变更只收敛 authoring / compiler / editor 数据源边界，不重写 runtime evaluator，不实现新的 timeline UI，不新增角色帧入口。

## 目标
- 明确 Character Behavior Authoring Graph 只表达角色行为提交 source 拓扑：root、ordered composite、Locomotion leaf、CommittedAction leaf。
- 明确 Dodge selector、Directional timeline、Backstep timeline 的正式数据源只属于 `CharacterActionDefinitionSO` 或批准的 action catalog/config。
- 拆分 compiler 职责：Behavior compiler 只产出 `CharacterBehaviorRuntimeDefinition` / execution tree；Action definition compiler/validator 产出 `CommittedActionBranchDefinition` / `ActionTimelineDefinition`。
- 让 Graph Editor 可以定位或打开 Committed Action Timeline Editor，但不能复制、保存或拥有 Dodge timeline 数据。
- 给迁移期 legacy field、sample asset 和双数据源提供明确删除或报错策略。
- 增加测试证明没有第二份 Dodge branch authoring 数据源。

## 非目标
- 不迁移 Ref timeline 交互；该部分由 `migrate-ref-timeline-editor-to-formal-action-config` 负责。
- 不修改 `CharacterFramePipeline` 的 phase 顺序。
- 不新增第二 motion executor、第二 animation presenter、第二 blackboard writer 或第二角色控制入口。
- 不实现 UpperBody runtime source、Facial slot、FaceBody 或新的身体域。
- 不引入 fallback 配置。缺正式 action definition、behavior graph 或 catalog reference 时必须报正式错误。

## 影响范围
- Affected specs:
  - `character-behavior-authoring-source-boundary`
  - `character-behavior-editor-adapters`
- Affected code:
  - `Assets/Scripts/Character/Behavior/Authoring/...`
  - `Assets/Scripts/Character/Behavior/Config/...`
  - `Assets/Scripts/Character/Action/Config/...`
  - `Assets/Editor/Character/Graph/...`
  - `Assets/Editor/Character/Action/Timeline/...`
  - `Assets/Tests/Editor/Character/Behavior/...`
- Related active changes:
  - `refactor-character-behavior-graph-source-contract`
  - `formalize-character-behavior-submission-runtime-chain`
  - `migrate-ref-timeline-editor-to-formal-action-config`

## 验证
- `openspec validate refactor-character-behavior-authoring-source-boundary --strict --no-interactive`
- EditMode 测试覆盖：
  - Behavior graph 只编译 source topology。
  - Dodge branch 只从正式 `CharacterActionDefinitionSO` 获取。
  - Graph Editor 不写入 Dodge timeline。
  - 缺失 action definition / catalog reference 报正式错误。
  - runtime / editor 静态边界不引用 sample-only 配置作为正式入口。
