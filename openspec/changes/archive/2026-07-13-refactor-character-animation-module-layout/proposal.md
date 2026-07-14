# Change: 重构角色动画模块布局与内部职责

## Why

`refactor-animation-layer-playback-authority` 已经把正式运行链路收敛为：

```text
ordered lifecycle records
  -> Contribution Registry
  -> CharacterAnimationLayerArbitrator
  -> one AnimationLayerPlan per layer
  -> CharacterAnimationLayerRuntime
  -> AnimancerAnimationPresenter
```

这条行为链路已经分离了逻辑事实、动画仲裁和播放执行，但代码的物理所有权仍没有跟上运行时语义：

- `Presentation/CharacterAnimationLayerRuntime.cs` 同时保存 layer 定义、contribution、candidate、plan、diagnostics、千行级 Arbitrator、inertialization adapter 和播放 Runtime；
- `CharacterAnimationLayerArbitrator` 同时处理 candidate allocation、ledger ingestion、Ready/release retention、因果建图、路径搜索、authority、冲突、plan kind 和清理，单个类超过一千行；
- `Presentation/CharacterAnimationContributionLifecycle.cs` 同时保存 runtime identity、lifecycle command、Queue、Registry snapshot 和 Registry；
- `CharacterGraphContext`、`TimelinePlaybackScheduler` 等逻辑 producer 为提交动画事实而依赖 `Pipeline.Presentation` 命名空间，代码依赖方向仍错误地表现为“逻辑依赖表现”；
- `CharacterPresentationStage` 同时承担表现聚合、logic pose 插值和大量 animation trace 发布，排查问题时难以快速区分事实、仲裁、播放与 Unity adapter。

继续在这些大类内增加规则，会让后续受击、上半身层、motion matching 或其它动画能力继续堆进同一实现，增加回归定位和代码讲解成本。需要在不改变当前动画行为、不增加第二套权威的前提下，按状态所有权和数据转换边界完成物理模块重构。

## What Changes

- 在 `Character/Pipeline` 下建立正式 `Animation` 业务模块，并按 `Contracts`、`Lifecycle`、`Arbitration`、`Playback` 与 `Diagnostics` 分区。
- 将公共动画事实和阶段间 DTO 从 `Pipeline.Presentation` 迁移到 `ThirdPersonCharacter.Pipeline.Animation`；实现类型使用对应子命名空间。
- 将 `Pipeline.Presentation` 收敛为表现帧聚合、logic pose 插值、presentation cue 和具体 Animancer adapter，不再承载动画业务仲裁合同。
- 定义 `IAnimationLifecycleFactSink` 与 `IAnimationLifecycleBatchSource` 两个窄合同：
  - Graph、StateMachine、Timeline 等 producer 只能获得 FactSink并提交事实；
  - PresentationStage 只能获得 BatchSource并复制、确认或清理批次；
  - `CharacterAnimationLifecycleCommandQueue` 仍是唯一具体队列并同时实现两个合同，不新增 event bus 或第二个 command buffer。
- 将 `CharacterAnimationContributionRegistry` 与 lifecycle queue 拆为独立文件和职责；Registry 继续只拥有 producer membership，不吸收 handoff ledger。
- 保留 `CharacterAnimationLayerArbitrator` 为唯一动画 commit 仲裁入口，但将内部实现拆为由它私有拥有的 concrete modules：
  - `AnimationLayerCandidateAllocator`：无状态完成 contribution priority allocation 与 DesiredCandidate；
  - `AnimationHandoffLedger`：唯一持有 ordered records、Ready/release 和 per-layer disposition；
  - `AnimationHandoffCausalGraphBuilder`：无状态构造有向因果组件与路径；
  - `AnimationLayerHandoffResolver`：无状态完成 relevance、authority、Driver、Hold/Invalid 与临时 resolution；
  - `AnimationLayerResolution`、ledger snapshot 与 causal graph 都是 internal、只读、单帧中间结果，不成为第二套 runtime。
- 将 Arbitrator 单帧顺序固定为：摄入 commands、分配 candidates、捕获一份只读 ledger snapshot、全部 layer 基于同一 snapshot 求解、一次提交全部 disposition、最后 prune。
- 将 `CharacterAnimationLayerRuntime` 收敛为多 layer 播放协调器，将每层 Final/Held output、唯一 ActiveHandoff、blend elapsed 与 inertialization session 迁入独立 `AnimationLayerPlaybackState`。
- 将 `AnimationLayerPlan`、`AnimationLayerPlaybackOutput`、frame snapshot 与 diagnostics 类型分别迁入 Contracts/Diagnostics，不再与算法实现混在同一个文件。
- 将 `AnimancerAnimationPresenter` 与 `AnimancerInertializationOutput` 放入 `Presentation/Animancer`；抽取 `CharacterPresentationInterpolator` 和 animation trace publisher，使 Stage 只组织正式顺序。
- 将 `CharacterPresentationDebug.cs` 按真实语义迁移为 `CharacterPresentationPose.cs`；不保留旧文件名、旧命名空间转发类型或兼容 alias。
- 所有 `.cs` 迁移 MUST 同步迁移 `.meta`，保持 Unity script GUID；所有 namespace 和引用一次完成迁移，旧位置完成后直接删除。
- Timeline Preview 继续实例化同一 Queue、Registry、Arbitrator、LayerRuntime 与 Presenter；不新增 preview 专用实现。
- 动画优先级、因果连接、Driver 选择、Hold/Invalid、CrossFade、Inertialization、Timeline visual time、Corin 资产与 diagnostics 语义保持不变。

## Capabilities

### Modified Capabilities

- `character-animation-layer-runtime`：明确唯一 Arbitrator 内部按 candidate、ledger、causal graph、resolution 分层，并将多层播放协调与单层播放状态分开。
- `character-animation-pipeline`：明确 lifecycle producer/consumer 使用不同窄合同，并建立 Animation 到 Presentation 的单向模块依赖。

## Current Spec Comparison

- `refactor-animation-layer-playback-authority` 已于 2026-07-12 归档，current `character-animation-layer-runtime` 已正式使用 Arbitrator、LayerPlan 和持久 LayerRuntime，不再包含 `CharacterAnimationTransitionRuntime` 或“LayerRuntime 自行仲裁”的旧语义。
- current `character-animation-layer-runtime` 要求 Arbitrator 独占 transition ledger。本 change 将 ledger 提取为 Arbitrator 私有构造、私有生命周期管理的 internal concrete object，仍由 Arbitrator 独占，不把 ledger 暴露给 Stage、Runtime、Graph 或 Preview，因此不产生第二个仲裁权威。
- current `character-animation-pipeline` 要求 `CharacterPresentationStage` 聚合 Queue、Registry、Arbitrator、LayerRuntime、Presenter 与 output job。本 change 只让 Stage 通过 BatchSource 使用同一个 Queue，并把算法实现移动到各自模块；Stage 的聚合根地位和每帧调用顺序不变。
- current `character-animation-pipeline` 已要求 StateMachine/Timeline 只发布事实且不能直接写 Animancer。本 change 用 FactSink 和 BatchSource 在代码权限上落实这一要求，不改变 handoff fact 内容。
- current `character-presentation-interpolation` 已要求 logic pose 插值与动画 visual playback 分离。本 change 只提取 `CharacterPresentationInterpolator`，不改变插值算法、InterpolationAlpha 或 correction 行为，因此不修改该 capability。
- `add-btsmtl-compiled-runtime-debugging` 仍是未完成的 active change。本 change 已更新其已实施部分的代码引用和文件归属；其剩余 Graph/Timeline overlay 任务继续复用正式 ordered record、causal component、LayerPlan 与 playback trace 合同，不创建第二套 diagnostics 数据源。
- 现行 specs 没有规定 asmdef 边界，也没有要求拆分 `CharacterGraphContext`。本 change 不借目录迁移扩大为全项目程序集或 Graph 服务重构。

## Dependency And Apply Order

`refactor-character-animation-module-layout` 依赖已归档的 `refactor-animation-layer-playback-authority` 最终实现和 current delta。实际串行顺序为：

1. `refactor-animation-layer-playback-authority` 已归档为 current baseline。
2. 本 change 已一次迁移类型、命名空间、内部算法模块与调用方。
3. `openspec/project.md` 和仍 active 的 diagnostics 引用已按新模块边界更新。
4. 归档前重新 strict validate 本 change，归档后再 strict validate 全部 current specs。

不得在两个 change 之间保留旧 namespace wrapper、并行 Queue、旧 Arbitrator 路径或 Preview 特例。

## Out of Scope

- 不修改 Corin RootTree、StateMachine、Timeline、TreeClip、Blackboard、transition edge 或 animation clip 资产。
- 不改变 contribution priority、override/additive 权重、因果连接、authority、Driver 选择、Hold/Invalid 或 OutputPolicy 行为。
- 不改变 CrossFade、Inertialization、ActiveHandoff supersede 或 presentation delta 语义。
- 不拆分 `CharacterGraphContext` 的 State、Timeline、Action、Blackboard 服务；该问题需要单独 proposal，不能用 partial 文件伪装模块化。
- 不重新设计 `TimelinePlaybackScheduler` 或移动 BTSMTL authoring 类型。
- 不新增 asmdef、DI 容器、event bus、service locator、通用 pipeline framework 或可插拔仲裁器接口。
- 不改变 runtime diagnostics payload、Editor overlay、Host Inspector 或 Timeline Preview 的业务语义。
- 不新增测试，不运行 Unity batchmode。

## Impact

- 主要目录：`Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation` 与 `.../Presentation`。
- 主要拆分来源：`CharacterAnimationLayerRuntime.cs`、`CharacterAnimationContributionLifecycle.cs`、`CharacterPresentationStage.cs`。
- 主要调用方：`CharacterPipeline`、`CharacterGraphContext`、`CharacterBTSMTLPhase`、`TimelinePlaybackScheduler`、`TimelineTreeRuntimeSet`、`CharacterPipelineHost`、Camera、runtime output 与 Agent authoring/editor diagnostics。
- Unity 资产数据不迁移；仅代码文件与 namespace 迁移，`.meta` 必须随文件保留。
- 这是中等风险的内部破坏性重构：正式输入输出和运行行为不变，旧文件布局、旧 namespace 与大类内部结构一次性删除。
