# Change: 退役 FullBody 集成帧路径

## Why
当前主线已经开始具备 `BodyArbiter`、`CharacterFramePlan` 和唯一 `CharacterFramePipeline` 的角色级形态，但正式路径里仍保留若干迁移期身份：`FullBodySubmissionBuilder` 同时承担 request/output submitter，`ICharacterFrameRuntimePort` 仍继承 FullBody runtime ports，`PlayerFullBodyActionController` 仍持有 pipeline host，`CharacterFrameSubmissionSource.FullBody` 仍像唯一正式来源。

这些身份如果不明确降级和退役，后续 UpperBody、HitReact、Aim、Attack 等身体域会继续被塞进 FullBody 集成路径，重新形成大 Module 和分裂入口。

## What Changes
- **BREAKING**：`FullBodySubmissionBuilder` 不再作为长期正式角色帧 submitter；它只能作为迁移期 integrated adapter，后续必须被 sibling submitters 和角色级 plan composer 替代。
- **BREAKING**：`ICharacterFrameRuntimePort` 的正式目标 Interface 不得继续通过继承 FullBody runtime ports 暴露 FullBody 操作面板。
- **BREAKING**：`PlayerFullBodyActionController` 不得长期拥有正式 `CharacterFramePipelineHost`；它必须降级为 Unity 装配/兼容 tick adapter。
- **BREAKING**：`CharacterFrameSubmissionSource.FullBody` 不得继续作为正式 output authority；如保留，只能作为迁移期诊断或兼容标记。
- 将旧规格中的“FullBody 主调度入口”和“Locomotion 作为 FullBody 子职责”从目标架构要求中移除或降级为历史/迁移期描述。
- 定义退役顺序：先补角色级 host/submitter/plan 测试，再迁移调用点，再删除或降级旧正式入口。

## Impact
- Affected specs:
  - `character-frame-pipeline`
  - `character-runtime-ports`
  - `fullbody-action-framework`
- Depends on:
  - `formalize-character-frame-arbitration-contract`
- Affected future implementation:
  - `CharacterFramePipeline`
  - `CharacterFramePipelineHost`
  - `CharacterFrameOutputComposer`
  - `ICharacterFrameRuntimePort`
  - `FullBodySubmissionBuilder`
  - `FullBodyRuntimePortAdapter`
  - `PlayerFullBodyActionController`
  - `CharacterFrameSubmission`
  - `CharacterFramePlan`
  - `DefaultBodyArbiter`

## Out of Scope
- 不在 proposal 阶段写代码。
- 不新增 UpperBody、HitReact、Aim 或 Attack runtime。
- 不新增第二 pipeline、第二 runner、第二 motion executor、第二 Presenter 或 fallback 配置。
- 不改 `.asset`、`.prefab`、`.unity`。
- 不清理全项目历史资产，只处理 Corin playable 主线相关正式运行时路径。

## 用户验证
- 阅读 `design.md` 的退役矩阵，确认每个废弃身份都有“保留/降级/删除”的目标状态和顺序。
- 运行 `openspec validate retire-fullbody-integrated-frame-paths --strict --no-interactive`。
- 后续实现完成后，运行本 change `tasks.md` 中列出的 EditMode 测试、C# build 和 GitNexus `detect_changes()`。
