# Change: 建立 Character Behavior Submission 合同

## Why
当前系统需要统一“行为节点只提交本帧意图”的语言，但不能一开始就替换 `CharacterFramePipeline` 或包装 Locomotion / Action。先建立最小纯数据合同和 fake runner 测试，可以验证 submission 的形状、pass 边界和状态所有权，不把数据模型错误和生产链路迁移混在一起。

## What Changes
- 新增 typed behavior submission 合同，区分 request、output、cue、diagnostic 和 state write。
- 新增 `CharacterBehaviorSubmissionSet` 或等价聚合模型，只作为纯数据容器。
- 新增 `CharacterBehaviorEvaluationPass` 或等价 pass 标记，明确 RequestPass 与 OutputPass 的职责。
- 新增 submission consumer / owner 映射，明确每类 submission 只能被哪些后续阶段消费，禁止无声丢弃或跨层偷用。
- 新增 `CharacterBehaviorStateOwnership` 文档化模型或测试 fixture，列清 node state、Locomotion state、Action lifecycle state、blackboard facts、animation playback state 和 rollback restore state 的 owner。
- 新增 fake behavior runner / fake leaf evaluator 测试，仅用于验证同帧收集顺序和边界，不接生产 pipeline。

## Implementation Slices
1. **Pass contract slice**：定义 RequestPass / OutputPass，明确各自能产生什么 submission。
2. **Typed submission slice**：拆分 request、output、cue、diagnostic、state write，不做一个万能大 struct。
3. **State ownership slice**：用表、测试或模型固定每类状态 owner。
4. **Fake runner slice**：只用 fake leaf 评估，验证排序、聚合、不可副作用。
5. **Boundary test slice**：静态检查合同层不引用 Unity runtime object、Editor 类型或正式 applier。

## Acceptance Criteria
- `BehaviorSubmission` 不成为垃圾桶；request、output、cue、diagnostic、state write 必须有类型化边界。
- 每类 submission 必须有明确 owner、consumer 和非法 consumer 测试。
- RequestPass 不能产出最终 motion / animation apply 意图；OutputPass 不能重新决定 action request 是否 accepted。
- Fake runner 能稳定收集多个 fake leaf submission，并保留 source step、node id 和 pass。
- 合同层不引用 `MonoBehaviour`、`Transform`、`Animator`、`CharacterController`、`InputAction`、GraphView、`TreeRunner` 或 `TimelinePlayer`。
- 没有任何生产 runtime 默认入口被替换。

## Stop Conditions
- 如果实现需要接入 `CharacterRuntimeCore` 默认 host，必须停止，移到 `add-character-behavior-submission-entry`。
- 如果实现需要包装现有 Locomotion 或 Action submitter，必须停止，移到后续 wrapper / golden line proposal。
- 如果 typed submission 需要直接调用 applier、blackboard writer、motion executor 或 animation presenter，必须停止。
- 如果某类 submission 没有明确 consumer 或错误时只能静默丢弃，必须停止并补合同。

## Non-Goals
- 不恢复 `CharacterFrameSubmitterGraph` / `CharacterFrameSubmitterChain`。
- 不包装 Locomotion / Action。
- 不迁移 Dodge。
- 不实现正式 behavior execution runtime。
- 不做节点编辑器 UI。

## Dependencies
- SHOULD 在 `add-character-graph-contracts` 后实施。
- MUST 先于 `add-dodge-behavior-submission-golden-line` 和 `add-character-behavior-submission-entry`。

## Impact
- Affected specs:
  - `character-behavior-submission-contracts`
  - related: `character-frame-pipeline`
  - related: `character-runtime-blackboard`
- Affected code:
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Behavior/Model/*`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Behavior/Solver/*`
  - `3cDemo/Client/3C_Client/Assets/Tests/Editor/Character/Behavior/*`
