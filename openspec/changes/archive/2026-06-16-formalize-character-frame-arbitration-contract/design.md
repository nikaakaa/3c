## Context
现有运行时已经收口到单一 `CharacterFramePipeline`，这是正确方向。但第一阶段实现为了接住现有 Locomotion 和 Dodge，使用 `FullBodySubmissionBuilder` 集成 Locomotion frame runtime、状态机推进、Action motion 和输出提交。这种形态只能视为迁移期 integrated submitter。

如果继续把它解释成目标架构，下一步 UpperBody 很容易被塞进 FullBody controller 或 builder 内部，变成“FullBody 统管 Locomotion、Action、UpperBody”的大模块。这不符合角色级管线的目标，也会破坏后续扩展的 Locality。

## Goals
- 明确 Character 级别的一帧 owner。
- 明确 Locomotion、FullBody Action、UpperBody 等是 sibling submitters。
- 明确 FullBody Action 通过 body occupancy claim 和 arbitration result 压制 Locomotion，而不是拥有 Locomotion。
- 明确 `CharacterFramePipeline` 不内置具体业务层优先级。
- 落地 `BodyArbiter`、`CharacterFramePlan` 和 body occupancy 的最小纯 C# 契约。
- 保持现有 Corin playable 主线短期不破坏。

## Non-Goals
- 不在本 change 内拆 `FullBodySubmissionBuilder`。
- 不在本 change 内实现 UpperBody。
- 不改变当前 Dodge、TurnBack、Locomotion 的行为。
- 不新增运行时 fallback。
- 不新增第二套 runner、pipeline、presenter 或 motion executor。

## Terms
### Character Frame Owner
角色级运行时 owner。它拥有当前角色的一帧调度入口，负责收集所有 frame submitter 的请求和候选输出，并调用唯一 `CharacterFramePipeline`。

### Sibling Frame Submitter
Locomotion、FullBody Action、UpperBody、HitReact 或 Aim 等行为域的提交者。提交者只能提供 request、facts、candidate output 或 occupancy claim，不能直接成为最终输出 owner。

### BodyArbiter
纯逻辑仲裁模块。它读取 sibling submitters 的请求、事实和 occupancy claim，产出 body/layer 级别的决策，不执行运动、不播放动画、不推进状态机。

### CharacterFramePlan
角色级一帧计划。它保存本帧哪些 body domains 获胜、哪些输出被允许、哪些输出被压制，以及 output composer/applier 需要消费的纯数据。

### BodyOccupancyDecision
角色身体占用决策。它表达 FullBody、UpperBody、LowerBody、Locomotion base layer 或等价域的占用、叠加和互斥结果。

### Current FullBody Integrated Submitter
当前 `FullBodySubmissionBuilder` 或等价实现。它可以暂时把 Locomotion、状态机、Action motion 集成到一个提交结果中，但它不是长期 target owner，也不是新增 UpperBody 的接入点。

## Decisions
### Decision: Character owns the frame
正式目标中一帧由 Character 级 owner 触发。FullBody Action 不拥有 Locomotion，Locomotion 也不绕过 Character frame owner 自己提交最终输出。

### Decision: FullBody can claim, not own
FullBody Action 可以提交 full-body occupancy claim。仲裁结果可以让 Action 输出替代或压制 Locomotion 输出，但这个结果来自 BodyArbiter/CharacterFramePlan，不来自 FullBody 直接管理 Locomotion。

### Decision: Pipeline remains orchestration, not policy blob
`CharacterFramePipeline` 负责固定阶段顺序、请求汇集、计划执行和输出应用。具体身体域互斥、优先级和混合规则属于 BodyArbiter 或等价策略模块。

### Decision: UpperBody requires arbitration first
新增 UpperBody 正式 runtime 前，必须先落地角色级 arbitration contract。UpperBody 不得直接依赖 `PlayerFullBodyActionController`、`FullBodySubmissionBuilder` 或 FullBody 兼容 view 作为上级 owner。

### Decision: Transitional wording must be explicit
凡是规格中保留“FullBody 调度 Locomotion”或“Locomotion 作为 FullBody 子职责”的描述，必须标记为迁移期现状、兼容入口或已批准的短期 implementation detail，不得作为目标架构指导后续 change。

## Target Shape
1. Character runtime host 读取输入、时间和配置根。
2. Character runtime host 调用 sibling submitters 收集 requests/facts/candidate outputs。
3. BodyArbiter 根据请求、状态、occupancy claim 和策略生成 `CharacterFramePlan`。
4. `CharacterFramePipeline` 消费 plan 并进入 output composer。
5. Output applier 只通过正式 motion executor、Presenter、blackboard writer 和 camera/input adapters 执行副作用。

## Migration Strategy
1. 本 change 收口术语和规格。
2. 本 change 添加 `CharacterFramePlan`、`BodyOccupancyDecision`、`IBodyArbiter` 和默认 BodyArbiter 的纯 C# 最小实现。
3. 本 change 通过 EditMode 与静态边界测试锁定 FullBody claim 压制 Locomotion、UpperBody claim 不隐式压制 base Locomotion、pipeline 不内置具体身体域策略。
4. 后续 change 将当前 FullBody integrated submitter 拆成 Locomotion submitter、FullBody Action submitter 和 compatibility adapter。
5. 后续 change 将角色级 host 从 FullBody controller 中迁出。
6. 后续 change 才允许新增 UpperBody submitter。

## Test Surface
- OpenSpec strict validation。
- 静态测试证明正式规格不再把目标架构描述为 FullBody 拥有 Locomotion。
- 静态测试证明新增 UpperBody 前必须存在 BodyArbiter/CharacterFramePlan contract。
- EditMode tests 覆盖 BodyArbiter 纯数据仲裁结果。
- EditMode tests 覆盖 FullBody occupancy claim 压制 Locomotion output 的场景。
- 静态边界测试证明 BodyArbiter 不调用 motion executor、Presenter、state machine runner 或 Unity scene object。
- 静态边界测试证明 CharacterFramePipeline 不直接写具体 UpperBody/FullBody/Locomotion 优先级分支。

## Risks
### Risk: 短期命名与现有实现不一致
当前代码仍由 FullBody 兼容入口驱动部分角色帧，这会造成命名不完全一致。

Mitigation: 明确当前入口是迁移期 integrated submitter，后续分阶段迁出，不在本 change 内破坏 Corin 主线。

### Risk: 抽象过早
如果一次性实现完整 BodyArbiter 和 UpperBody，可能过度设计。

Mitigation: 本 change 只实现 FullBody、Locomotion 与 UpperBody claim 的最小纯数据仲裁，不新增 UpperBody runtime，不拆现有 playable 主线。

### Risk: Pipeline 变成策略集合
如果把仲裁直接写进 `CharacterFramePipeline`，会让 pipeline 再次变成大杂烩。

Mitigation: 规格要求 arbitration policy 位于独立 Module，pipeline 只消费 plan。
