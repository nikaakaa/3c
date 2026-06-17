## Context
项目目标是保持唯一 `CharacterFramePipeline` 主线，并让 FullBody、Locomotion、Action 以及未来 UpperBody/LowerBody 都只作为 request 或 frame output 的提交者。

当前实现已经把 frame output side effects 收到 output composer / applier，但持有关系还没有闭环：

- `PlayerFullBodyActionController` 直接 `new CharacterFramePipeline()`。
- `FullBodyActionTickAdapter` 直接 `new CharacterFramePipeline()`。
- `CharacterFramePipeline` 直接 `new FullBodySubmissionBuilder()`。

这会让 MonoBehaviour 和 FullBody 具体实现继续像 pipeline owner。预测回滚语境下，这种持有关系会放大风险：live tick、simulation tick phase、replay/synctest 可能通过不同对象组合推进同一帧。

## Goals
- 每个角色只有一个纯 C# `CharacterFramePipelineHost` 持有角色帧运行时。
- MonoBehaviour 只作为 Unity Adapter，负责序列化引用、配置解析、生命周期入口和兼容 Tick。
- `CharacterFramePipeline` 只负责 phase order 和 phase 执行，不创建 FullBody 具体提交者。
- request submission 和 frame output submission 在 Interface 层拆分。
- `FullBodyActionTickAdapter` 的逐 phase 推进复用同一个 host，不创建第二个 pipeline。
- FullBody replay、synctest 和后续高延迟校正复用 host 的正式入口。

## Non-Goals
- 不在本变更中重命名 `PlayerFullBodyActionController` 为 `PlayerCharacterRuntimeAdapter`。
- 不在本变更中减少所有 MonoBehaviour 数量。
- 不迁移 ScriptableObject 配置资产结构。
- 不新增 UpperBody、LowerBody 或 Attack runtime。
- 不改变状态机配置语义、runner 状态推进语义、motion executor 语义或 animation presenter 语义。
- 不新增 fallback 配置。

## Decisions

### Decision: 使用 CharacterFramePipelineHost 作为深 Module
`CharacterFramePipelineHost` 放在 `Assets/Scripts/Character/Pipeline/Runtime/...`。它是纯 C# Module，提供一帧 Tick 和逐 phase 推进两个入口，内部持有同一个 `CharacterFramePipeline` 实例和提交者 Adapter。

这个 Module 的 Interface 要小：调用者只需要提供 `ICharacterFrameRuntimePort` 和 `CharacterFrameInput`，或在 simulation tick 中提供 phase 与已有 frame context。调用者不需要知道 FullBody request 如何仲裁、frame output 如何构建、output composer 如何应用。

### Decision: MonoBehaviour 可以创建 host，但不能创建 pipeline
短期内 `PlayerFullBodyActionController` 仍可作为 Unity Adapter 懒创建一个 `CharacterFramePipelineHost`。这是兼容现状的最小迁移。它不能再直接创建或持有 `CharacterFramePipeline`。

`FullBodyActionTickAdapter` 不再拥有自己的 host 或 pipeline；它通过 `PlayerFullBodyActionController` 或等价 Unity Adapter 取得同一个 host 的逐 phase 入口。

### Decision: Pipeline 依赖提交者 Interface
新增角色帧提交者 Interface：

- request submitter：在 `GameplayDecision` phase 写入 locomotion decision、action request submission、state decision 和 runtime facts trace 等纯数据。
- output submitter：在 `BuildMotion` phase 生成 `CharacterFrameSubmission` 纯数据。

`CharacterFramePipeline` 可以持有这些 Interface，但不能创建 FullBody 具体实现。FullBody 生产实现可以先从 `FullBodySubmissionBuilder` 适配出来，再按需要拆成 `FullBodyFrameRequestSubmitter` 和 `FullBodyFrameOutputSubmitter`。

### Decision: Output composer / applier 继续是唯一副作用应用点
本变更只收敛持有关系，不改变 output side effects 的权威位置。motion、animation、input consume、runtime facts、snapshot、diagnostics 仍只通过 `CharacterFrameOutputApplier` 触发。

### Decision: Replay 以 host 为正式入口
FullBody replay 可以从 `PredictionInputFrame` 构造 `CharacterFrameInput`，但进入点必须是 `CharacterFramePipelineHost`。Replay 不直接创建 `CharacterFramePipeline`，不直接调用 FullBody submitter，也不手工拼接 input buffer、state machine、motion 和 animation facts。

## Alternatives Considered
- 继续让 `PlayerFullBodyActionController` 直接持有 pipeline：实现最少，但 MonoBehaviour 仍是最高运行时 owner，prediction/replay 后续会绕回 controller 大类。
- 让 `FullBodyActionTickAdapter` 自己持有 host：会把 phase tick 变成第二个角色帧持有者，不利于一名角色一个 host。
- 把 host 做成新的 MonoBehaviour：会增加 Unity 生命周期对象数量，和减少 MonoBehaviour 的方向相反。
- 把 pipeline 做成 static/global：会破坏一名角色一个运行时状态，也不利于测试和多角色。

## Risks / Trade-offs
- 现有 EditMode 测试大量直接 `new CharacterFramePipeline()`，需要迁移到 host 或显式测试构造路径。
- 逐 phase tick 需要保存 frame context，host 必须提供清晰的 Begin/RunPhase/Complete 语义。
- `FullBodySubmissionBuilder` 当前同时覆盖 request 与 frame output 构建，拆分时要保持行为一致，不能引入第二套仲裁或运动解析。
- 当前还有多个 active changes，实施时必须只触碰持有关系，不顺手迁移配置或状态机模型。

## Migration Plan
1. 先更新静态边界测试，锁定 pipeline/host/controller/tick adapter 的持有关系。
2. 新增提交者 Interface 和 `CharacterFramePipelineHost`。
3. 让 `CharacterFramePipeline` 构造时接收提交者 Interface，移除 FullBody 具体实例化。
4. 将 FullBody 生产提交接到 request/output submitter Interface。
5. 将 `PlayerFullBodyActionController.Tick` 改为调用 host。
6. 将 `FullBodyActionTickAdapter` 改为复用同一个 host 的逐 phase 入口。
7. 将 replay/synctest 中直接创建 pipeline 的测试和工具迁移到 host。
8. 跑定向 EditMode 测试、C# build、OpenSpec strict validate。

## Open Questions
- `PlayerFullBodyActionController` 的正式改名和 MonoBehaviour 数量收敛放到后续 `refactor-character-unity-runtime-adapters` 或等价 change。
- ScriptableObject 配置资产是否迁移到更中性的 Character runtime config，不属于本变更。
