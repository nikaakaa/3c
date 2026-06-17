# Change: 明确角色级帧仲裁契约

## Why
当前主线已经有 `CharacterFramePipeline`，但规格和实现口径仍混有“FullBody 调度 Locomotion”或“Locomotion 作为 FullBody 子职责”的过渡描述。这会把后续 UpperBody、HitReact、Aim、Attack 等层继续推向 FullBody 内部扩展，形成新的大杂烩和分裂路径。

目标架构必须更硬：角色级运行时拥有一帧，Locomotion、FullBody Action、UpperBody 等行为域只提交请求、事实或输出候选。是否占用全身、是否压制基础移动、是否允许上半身叠加，必须由角色级 `BodyArbiter` 或等价仲裁模块产出 `CharacterFramePlan`，再交给 `CharacterFramePipeline` 的 output composer/applier 执行。

## What Changes
- **BREAKING**：目标架构中 FullBody 不再作为 Locomotion 的上级 owner。FullBody Action 可以声明全身占用或压制 Locomotion，但只能通过角色级仲裁结果生效。
- **BREAKING**：后续新增 UpperBody、HitReact、Aim 或 Attack layer 前，必须先有角色级仲裁契约，不能直接读取 FullBody runtime 状态或挂到 FullBodySubmissionBuilder 内部。
- 将当前 `FullBodySubmissionBuilder`、FullBody host adapter 或等价实现定义为迁移期 integrated submitter，允许它暂时收集 Locomotion 与 Action 数据，但不得作为长期目标仲裁模型。
- 引入规格级术语：Character frame owner、sibling frame submitter、BodyArbiter、CharacterFramePlan、BodyOccupancyDecision。
- 明确 `CharacterFramePipeline` 只负责一帧顺序和输出应用，不把 FullBody 优先级、UpperBody 混合规则或 Locomotion 压制规则写死在 pipeline 本体。
- 新增 `CharacterFramePlan`、`BodyOccupancyDecision`、`IBodyArbiter` 与默认 BodyArbiter 的纯 C# 契约和实现。
- 让 `CharacterFrameOutputComposer` 先消费角色级 plan，再产出 `CharacterFrameOutput`，pipeline 本体不承载具体身体域优先级。
- 增加 EditMode 与静态边界测试，覆盖 FullBody claim 压制 Locomotion、UpperBody claim 不隐式压制 base Locomotion、BodyArbiter 不依赖 Presenter/motion executor/runner。
- 本变更不修改 `.asset`、`.prefab`、`.unity`，不新增 UpperBody runtime，不迁移 Corin Prefab 或 Scene。

## Impact
- Affected specs:
  - `character-frame-pipeline`
  - `character-runtime-ports`
  - `fullbody-action-framework`
  - `wasd-locomotion-pipeline`
- Affected implementation:
  - `CharacterFramePipelineHost` 或等价角色级 runtime host
  - `FullBodySubmissionBuilder` 的迁移边界
  - Locomotion frame request/output submitter
  - Future UpperBody submitter
  - Body arbitration model and tests

## Out of Scope
- 不新增 UpperBody runtime。
- 不重命名或拆分 `PlayerFullBodyActionController`。
- 不改 Corin 配置资产、Prefab 或 Scene。
- 不改现有 Presenter、motion executor、state machine runner。
- 不把当前 `FullBodySubmissionBuilder` 一次性拆成正式 sibling submitters。
- 不把当前所有历史规格一次性改写为最终实现描述。

## 用户验证
- 阅读本 change 的 `design.md`，确认目标架构中 Locomotion、FullBody Action、UpperBody 是 Character frame owner 下的兄弟提交者。
- 运行 `openspec validate formalize-character-frame-arbitration-contract --strict --no-interactive`。
- 运行 `CharacterFrameArbitrationTests` 和 `FullBodyRollbackReplayTests` 中的管线定向测试，确认没有新增 FullBody 内部 UpperBody 分支、第二 pipeline 或绕过 output applier 的执行路径。
