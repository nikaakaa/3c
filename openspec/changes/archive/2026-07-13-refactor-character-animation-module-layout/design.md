## Context

动画播放权威重构后，正式运行时已经具备正确的阶段边界：逻辑 producer 发布有序事实，Registry 保存 producer lifecycle，Arbitrator 提交每层唯一 LayerPlan，LayerRuntime 保存视觉播放历史，Presenter 只写 Unity 动画状态。

当前剩余问题是实现结构仍沿用早期演化过程形成的大文件：

```text
Presentation/CharacterAnimationContributionLifecycle.cs
  identity + command + queue + registry

Presentation/CharacterAnimationLayerRuntime.cs
  layer contracts + candidate + plan + diagnostics
  + arbitrator + ledger + causal graph + resolver
  + playback runtime + inertialization contract

Presentation/CharacterPresentationStage.cs
  animation transaction orchestration
  + logic pose interpolation
  + trace publishing
```

其中 `CharacterAnimationLayerArbitrator` 是正确的单一业务权威，但“单一权威”被实现成了“单一巨型类”。它既持有跨帧 ledger 状态，又实现多个可独立推理的无状态算法。Graph 和 Logic 还因为 command contract 位于 Presentation namespace 而形成错误的源码依赖方向。

本设计只重构模块边界和内部所有权，不改变动画行为或作者配置。

## Goals / Non-Goals

### Goals

- 让目录、namespace 和运行时职责表达同一个架构。
- 让 Logic producer 只拥有动画事实写权限，不能消费或清理 batch。
- 让 PresentationStage 只拥有 batch 消费权限，不向 producer 暴露 Registry、Arbitrator 或 Runtime。
- 保持一个 Queue、一个 Registry、一个 Arbitrator、一个 LayerRuntime 和一个 Presenter 主链路。
- 保持 `CharacterAnimationLayerArbitrator` 为唯一对外 commit 入口，同时把内部状态和纯算法拆开。
- 让所有 layer 基于同一 ledger snapshot 完成单帧求解，再原子提交 dispositions。
- 让 LayerRuntime 的多层协调与单层播放状态分开。
- 让 Stage 只编排表现事务，插值与 trace 发布由独立实现负责。
- Preview 复用同一模块，不增加专用分支。
- 迁移后删除旧文件位置、旧 namespace 和旧聚合实现。

### Non-Goals

- 不增加可替换 Queue、Registry、Arbitrator 或 Resolver 实现。
- 不为内部单实现算法创建接口。
- 不改变 animation authoring、资产结构或序列化字段。
- 不改变任何仲裁、混合、插值、采样或 diagnostics 结果。
- 不通过 partial class 机械切割大类。
- 不增加 asmdef 或通用依赖注入框架。
- 不拆 `CharacterGraphContext` 和 `TimelinePlaybackScheduler` 的业务职责。

## Target Layout

```text
Character/Pipeline/
├─ Animation/
│  ├─ Contracts/
│  │  ├─ AnimationIdentity.cs
│  │  ├─ AnimationContribution.cs
│  │  ├─ AnimationLifecycleCommand.cs
│  │  ├─ AnimationLifecycleInterfaces.cs
│  │  ├─ AnimationLayerDefinition.cs
│  │  ├─ AnimationLayerCandidate.cs
│  │  ├─ AnimationLayerPlan.cs
│  │  └─ AnimationLayerPlaybackOutput.cs
│  ├─ Lifecycle/
│  │  ├─ CharacterAnimationLifecycleCommandQueue.cs
│  │  └─ CharacterAnimationContributionRegistry.cs
│  ├─ Arbitration/
│  │  ├─ CharacterAnimationLayerArbitrator.cs
│  │  ├─ AnimationLayerCandidateAllocator.cs
│  │  ├─ AnimationHandoffLedger.cs
│  │  ├─ AnimationHandoffCausalGraphBuilder.cs
│  │  └─ AnimationLayerHandoffResolver.cs
│  ├─ Playback/
│  │  ├─ CharacterAnimationLayerRuntime.cs
│  │  ├─ AnimationLayerPlaybackState.cs
│  │  └─ IAnimationInertializationAdapter.cs
│  └─ Diagnostics/
│     ├─ AnimationLayerFrameSnapshot.cs
│     └─ CharacterAnimationTracePublisher.cs
└─ Presentation/
   ├─ CharacterPresentationStage.cs
   ├─ CharacterPresentationInterpolator.cs
   ├─ CharacterPresentationPose.cs
   ├─ PresentationCue.cs
   └─ Animancer/
      ├─ AnimancerAnimationPresenter.cs
      └─ AnimancerInertializationOutput.cs
```

Contracts 文件按真正跨阶段使用的概念聚合，不要求每个 struct 单独一个文件。内部 helper 留在其状态 owner 内，不把目录扩张为大量微型文件。

## Dependency Direction

```text
Graph / StateMachine / Timeline
  -> Animation contracts
  -> IAnimationLifecycleFactSink

CharacterPresentationStage
  -> IAnimationLifecycleBatchSource
  -> Contribution Registry
  -> CharacterAnimationLayerArbitrator
  -> CharacterAnimationLayerRuntime
  -> Presentation.Animancer

CharacterAnimationLayerArbitrator
  -> CandidateAllocator
  -> HandoffLedger
  -> CausalGraphBuilder
  -> HandoffResolver

CharacterAnimationLayerRuntime
  -> AnimationLayerPlaybackState
```

Animation contracts 和 runtime 实现 MUST NOT依赖具体 Animancer adapter。Presentation 可以依赖 Animation，Animation 不得反向依赖 Presentation concrete types。

## Module Contracts

| 模块 | 持久状态 | 输入 | 输出 |
|---|---|---|---|
| Lifecycle Queue | ordered pending commands、sequence | producer facts | ordered batch |
| Contribution Registry | playback/contribution/owner membership | lifecycle batch | Registry snapshot |
| CandidateAllocator | 无 | layer definitions、contributions | DesiredCandidates |
| HandoffLedger | records、Ready、release、per-layer disposition | ordered lifecycle records、resolution commit | read-only ledger snapshot |
| CausalGraphBuilder | 无 | ledger snapshot、layer id | directed components/paths |
| HandoffResolver | 无 | layer、candidate、playback、causal graph、ready facts | AnimationLayerResolution |
| Arbitrator | 私有拥有上述模块 | batch、Registry snapshot、playback snapshots | one LayerPlan per layer |
| PlaybackState | Final/Held、ActiveHandoff、blend/inertialization | one LayerPlan、presentation delta | one PlaybackOutput |
| LayerRuntime | 每层 PlaybackState 集合 | all LayerPlans | final layer outputs |
| PresentationStage | 表现事务编排引用 | presentation frame、batch | Presenter apply、pipeline output |

## Decisions

### 1. 使用 Animation 业务域而不是继续扩张 Presentation

动画 contribution、lifecycle、仲裁与播放状态属于角色动画业务，不是 Unity 表现 adapter。它们迁入 `Pipeline.Animation`；Presentation 只保留 render-frame 聚合、root pose 插值和 Animancer 实现。

业务取舍：namespace 迁移会修改较多 using 和 editor 引用，但换来 Logic 不再依赖 Presentation，代码结构与运行时事实方向一致。只移动文件而保留旧 namespace 的风险更低，却只能改善浏览体验，不能形成真实模块边界，因此拒绝。

### 2. 公共合同使用 Animation 根 namespace

阶段间公共类型使用 `ThirdPersonCharacter.Pipeline.Animation`。Lifecycle、Arbitration、Playback、Diagnostics 的实现类型使用子 namespace。

业务取舍：调用方只需引用一个稳定合同 namespace；实现目录仍能表达所有权。把每个 DTO 放进不同子 namespace 会让 Graph、Timeline、Stage 出现大量 using，增加作者和调试代码负担。

### 3. Queue 提供两个窄接口但保持一个实现

`IAnimationLifecycleFactSink` 暴露 Sample、Complete、Release、Handoff、OwnerReady 等事实写入；`IAnimationLifecycleBatchSource` 暴露 pending batch copy、acknowledge 与 clear。`CharacterAnimationLifecycleCommandQueue` 同时实现二者并由 `CharacterPipeline` 唯一构造。

业务取舍：两个接口限制 producer 和 consumer 权限，是真实边界；但不增加第二个队列、事件总线或通用消息框架。为每种 command 创建单独 sink 会放大接口数量，因此不采用。

### 4. Arbitrator 是 façade，不是算法集合文件

`CharacterAnimationLayerArbitrator` 对外只保留 construction、Layers、Build 和 Reset。它私有构造并独占内部 concrete modules，外部不得直接访问 Ledger、GraphBuilder 或 Resolver。

业务取舍：唯一权威保持不变，内部算法可以分别理解和维护。将这些 helper 公开或注入可替换实现会制造多个可调用仲裁入口和不必要的策略组合，因此全部保持 internal concrete。

### 5. Candidate allocation 是纯转换

`AnimationLayerCandidateAllocator` 负责 layer 过滤、priority grouping、override 剩余覆盖、同组归一、additive 保留和非法 layer errors。它不读 ledger、不读 playback、不保存跨帧状态。

输入是 layer catalog 与 Registry contributions，输出是完整 DesiredCandidate 集合。它不得生成 HandoffPlan 或修改 Registry。

### 6. Ledger 是唯一仲裁持久状态

`AnimationHandoffLedger` 独占 ordered handoff records、Ready leaf、released owner 和 per-layer disposition。它负责 ingest、去重、排序、snapshot、commit、prune 与 reset，但不判断哪条路径应该胜出。

业务取舍：状态生命周期集中后，reset、deactivate、Preview seek 与 dispose 能统一清理；Ledger 不再同时承担图算法，避免状态管理和业务选择互相污染。

### 7. 因果建图与业务选择分开

`AnimationHandoffCausalGraphBuilder` 只依据 command order、logical/resolved owner equality 和 activation generation 构造有向组件、邻接关系与可达路径。它不读取 contribution priority，也不选择 Driver。

`AnimationLayerHandoffResolver` 使用 causal graph、当前 playback、DesiredCandidate 与 ready facts判断 relevance、authority、唯一末端、Selected/Coalesced/Retired/Conflict、Hold/Invalid 和 selected Driver，输出 internal `AnimationLayerResolution`。

业务取舍：图连通性错误和 authority 错误可以分别定位。合并为一个 Resolver 文件改动更少，但会把当前千行 Arbitrator 变成下一个大 Resolver，因此不采用。

### 8. 所有 layer 基于同一只读 Ledger Snapshot

Arbitrator 单帧执行顺序固定为：

1. Queue 已按 tick、phase、sequence 提供完整 batch。
2. Ledger ingest 当前 batch。
3. CandidateAllocator 生成全部 layer candidates。
4. Arbitrator 捕获一次 ledger snapshot 与全部 playback snapshots。
5. 每个 layer 只读这些快照并生成 resolution。
6. Arbitrator 收集全部 resolution 后一次提交 dispositions。
7. Ledger 在所有 layer commit 后 prune records 与 released ready facts。
8. Arbitrator 输出每层唯一 LayerPlan。

业务取舍：比逐层边求解边修改 ledger 多一个短生命周期 resolution 集合，但消除了 layer iteration order 对其它 layer 观察状态的影响。

### 9. Resolution 不是第二套 Plan

`AnimationLayerResolution` 只包含 plan kind、message、selected Driver、from/to owners、supersede 和 record dispositions。它是 internal 单帧值，Arbitrator 立即把它组装为正式 `AnimationLayerPlan`，不得缓存、暴露给 Presenter 或进入 diagnostics 第二数据源。

### 10. 多层 Runtime 与单层 PlaybackState 分开

`CharacterAnimationLayerRuntime` 负责按正式 layer catalog 找到 plan、调用每层 state、收集 outputs 和 frame snapshot。`AnimationLayerPlaybackState` 独占该层 FinalOutput、HeldOutput、DesiredCandidate、ActiveHandoff、blend weights 与 inertialization session。

`ActiveHandoff` 和 `WeightedPlan` 继续作为 PlaybackState 私有 helper，不继续拆分。LayerRuntime 和 PlaybackState 都不得读取 raw lifecycle records 或重新仲裁。

### 11. Stage 继续是聚合根

`CharacterPresentationStage` 继续唯一组织：SamplePresentation 后读取 batch、Registry Apply、Arbitrator Build、LayerRuntime Apply、Presenter Apply、diagnostics publish、acknowledge。

`CharacterPresentationInterpolator` 负责 previous/current logic sample、InterpolationAlpha、logic root 与 visual root pose。`CharacterAnimationTracePublisher` 只读取正式 batch、snapshot、plan 与 playback diagnostics，不参与决策。

业务取舍：Stage 代码缩小但事务顺序仍集中可见；不把每个调用包装成通用 stage interface，避免角色管线变成框架。

### 12. Preview 复用同一 concrete modules

Preview session 使用同一 Queue、Registry、Arbitrator、LayerRuntime 与 Animancer Presenter。非连续 seek、target switch 和 dispose 同时 reset这些模块。Preview 不直接调用内部 Allocator、Ledger、GraphBuilder 或 Resolver。

### 13. Namespace 与文件迁移原子完成

所有 `.cs` 与 `.meta` 成对移动。调用方、Editor、Preview 和 diagnostics 在同一个 apply 中改到新 namespace。完成后删除旧目录文件，不提供 `[Obsolete]` wrapper、type forwarder、namespace alias 或双定义。

业务取舍：迁移期间代码不能保持每一步都可编译，但最终只有一条干净路径；兼容层会把本次物理收口变成长期双命名，因此不接受。

### 14. 本 change 不增加 asmdef

当前 `Assets/GameScripts/Main/Runtime` 没有按 Pipeline 业务域建立 asmdef。直接增加会同时牵涉 BTSMTL、TreeDesigner、Animancer、Editor 和 TEngine 依赖图。

业务取舍：本 change 通过 namespace、internal visibility 和窄接口建立代码边界。编译程序集隔离具有长期价值，但应在目录和依赖方向稳定后单独规划。

## Key Scenarios

### Rapid Locomotion Chain

同一批 records 先进入 Ledger，GraphBuilder 构造一条连续有向路径，Resolver 选择最后 Driver 并返回 Coalesced/Selected dispositions。Arbitrator 在其它 layer 也完成 resolution 后统一 commit，并只生成一个 Base LayerPlan。拆分类不得改变现有结果。

### True Parallel Conflict

GraphBuilder 返回两个互不连通组件，Resolver 计算出相同最高 authority并返回 Conflict/Invalid resolution。Arbitrator 不按调用顺序或文件模块顺序选择其中之一。

### Producer And Consumer Permissions

StateMachine 只持有 FactSink，能够提交 Handoff 和 OwnerReady，但不能读取、acknowledge 或 clear pending batch。PresentationStage 只持有 BatchSource，不能伪造 producer runtime owner 或直接调用 StateMachine。

### Preview Reset

Preview seek 时由 Preview 聚合根 reset Queue、Registry、Arbitrator 和 LayerRuntime。内部 Ledger 随 Arbitrator.Reset 清理，不由 Editor 单独访问。

## Migration Plan

1. 以 playback-authority change 的最终代码和 delta 为基线建立类型 ownership 清单。
2. 建立 Animation 目录和公共合同 namespace，先迁移 identity、contribution、command、layer、candidate、plan 与 output DTO。
3. 增加 FactSink/BatchSource，并让 CharacterPipeline 按窄视图装配 Graph/Timeline 与 PresentationStage。
4. 拆出 Queue 和 Registry，不改变 command order、acknowledge 和 membership 行为。
5. 从 Arbitrator 提取 CandidateAllocator、Ledger、CausalGraphBuilder、HandoffResolver 与 internal resolution。
6. 将 Arbitrator 改为 snapshot -> resolve all -> commit all -> prune 的 façade。
7. 拆出单层 PlaybackState，保持 plan-only Runtime 接口和 ActiveHandoff 行为。
8. 迁移 diagnostics contracts、trace publisher、Interpolator 与 Animancer concrete adapters。
9. 更新 Runtime、Graph、Logic、Camera、Unity Host、Preview、Agent authoring 和 Editor 引用。
10. 删除旧文件位置、旧 namespace 和所有过渡代码。
11. 更新 `openspec/project.md`，执行静态编译与 strict validation。

## Risks / Trade-offs

- namespace 迁移影响面大，遗漏 using 会造成编译错误；必须一次更新所有 runtime、preview 和 editor 调用方。
- `.meta` 若未随文件移动可能改变 Unity script GUID；迁移任务必须显式成对处理。
- Ledger snapshot 与统一 commit 是内部顺序调整；必须保持所有 layer 使用同一 batch，并确认 disposition 只在全部 resolution 后写回。
- 拆出 GraphBuilder 后如果复制而非移动旧 CanFollow/path 逻辑，会形成两个因果算法；旧方法必须随迁移删除。
- internal resolution 若进入长期缓存或 diagnostics，会形成第二套 Plan 真相；它只能在单次 Build 调用内存在。
- Stage 拆分不能改变 Sample、Registry、Arbitrator、Runtime、Presenter、acknowledge 的事务顺序。
- 不设置机械行数阈值；目标是单一状态 owner 和单一转换职责。禁止为了满足文件大小继续拆出无业务边界的 helper。

## Rejected Alternatives

### 只移动文件并保留 Presentation namespace

改动较小，但 Logic 仍依赖 Presentation，职责错误只被目录掩盖。

### 把所有算法继续留在 Arbitrator partial class

能缩短单文件，却共享同一批可变字段，状态所有权和算法耦合没有改变。

### 给 Allocator、Ledger、GraphBuilder 和 Resolver 都定义接口

当前每项只有一个正式实现。接口会增加组合方式和测试替身心智，但不会增加业务能力；只有跨 producer/consumer 权限边界的 Queue 需要接口。

### 让 Stage 直接调用内部 Resolver

会绕过唯一 Arbitrator 并产生第二个 plan 生成入口，违反播放权威收口。

### 同时拆 CharacterGraphContext

GraphContext 的大体积来自 State、Timeline、Action、Blackboard 等不同服务，需要独立业务重构。与动画文件迁移同时进行会扩大回归面，也容易用 partial 文件制造表面模块化。

### 同时增加 asmdef

编译期边界更强，但会把本次任务扩张到全项目程序集依赖迁移，且无法直接改善当前仲裁算法所有权。
