# Change: 按 Unity 2022 兼容方式迁移 Ref Timeline UI

## Why
当前 Committed Action Timeline Editor 已经能以正式 `CharacterActionDefinitionSO` 作为入口，并能读写 Dodge 的 Directional / Backstep timeline 数据。但实际 UI 仍是早期轻量仿制：组件边界不清、交互不完整、视觉质量离 `Ref/wly970123` 的 timeline editor 还有距离。

之前直接迁移 Ref UXML 的尝试暴露了新的风险：Ref 项目资源来自较新 Unity 版本，原样导入 Unity 2022 可能触发 UI Toolkit importer 崩溃。因此本变更只规划和实现 Unity 2022 兼容的 Timeline UI 迁移路径，不改变正式 gameplay runtime。

## What Changes
- 将 `Ref/wly970123` 的 Timeline field、track handle、track view、clip view、inspector 和核心交互按 Unity 2022 兼容方式迁移到本项目 Editor-only 路径。
- 建立 `CharacterActionDefinitionSO -> editor timeline model -> serialized writeback` 的清晰边界，避免 UI 直接散落操作深层 `SerializedProperty`。
- 保持 Committed Action Timeline Editor 的唯一正式数据源为 `CharacterActionDefinitionSO`、`DodgeCommittedActionBranchAuthoring`、`CommittedActionBranchTimelineAuthoring`、`ActionTimelineTrackAuthoring` 和 `ActionTimelineClipAuthoring`。
- 让 Directional / Backstep timeline 支持稳定查看、选择、添加、删除、移动、缩放、保存、校验和重新打开。
- 用自动测试和静态边界验证证明迁移后的 UI 不引入 Ref runtime runner、`TimelinePlayer`、Taco tree、`PlayableGraph` 或第二套 gameplay 输出路径。

## Non-Goals
- 不修改 `CharacterFramePipeline`、`CharacterBehaviorSubmissionRunner`、BodyArbiter、OutputApplier、motion executor、Animancer presenter 或 blackboard writer。
- 不让 Ref `TimelinePlayer`、Taco `BaseTree` / `RunnableTree` / `RunnableNode` / `TreeRunner` 或 Ref gameplay `PlayableGraph` 进入正式 gameplay。
- 不实现通用技能编辑器，工具命名仍为 `Committed Action Timeline Editor`。
- 不新增 fallback 配置；缺正式 action definition、timeline、track、clip 或 preview binding 时必须报告正式错误。
- 不在本变更内实现完整动画 / motion 视觉预览；本阶段 preview 以正式 evaluator 的数据结果为主。

## Impact
- Affected specs:
  - `character-behavior-editor-adapters`
- Related active changes:
  - `migrate-ref-timeline-editor-to-formal-action-config`
  - `refactor-character-behavior-authoring-source-boundary`
  - `formalize-character-behavior-submission-runtime-chain`
- Affected editor code and resources:
  - `3cDemo/Client/3C_Client/Assets/Editor/Character/Action/Timeline/...`
  - `Ref/wly970123/taco-editor/Assets/Addon/Taco/Timeline/Editor/...`
  - `3cDemo/Client/3C_Client/Assets/Tests/Editor/Character/Action/...`
- Affected formal data:
  - `CharacterActionDefinitionSO`
  - `DodgeCommittedActionBranchAuthoring`
  - `CommittedActionBranchTimelineAuthoring`
  - `ActionTimelineTrackAuthoring`
  - `ActionTimelineClipAuthoring`

## Validation
- `openspec validate port-ref-timeline-ui-to-unity-2022-compatible-editor --strict --no-interactive`
- 定向 EditMode 测试覆盖：
  - Unity 2022 兼容 UI 资源导入边界。
  - editor timeline model 与 serialized adapter 的读写一致性。
  - Directional / Backstep track 和 clip 的添加、删除、移动、缩放、保存、重新加载。
  - preview adapter 输出与正式 evaluator 一致。
  - runtime 静态边界不引用 UnityEditor、GraphView、TimelinePlayer、PlayableGraph、Taco runner 或 Ref runtime tree。
