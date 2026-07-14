## 1. 基线与迁移清单

- [x] 1.1 确认 `refactor-animation-layer-playback-authority` 已归档并作为 current baseline
- [x] 1.2 列出 `CharacterAnimationContributionLifecycle.cs` 中所有 public/internal 类型及其目标 owner
- [x] 1.3 列出 `CharacterAnimationLayerRuntime.cs` 中所有 public/internal 类型及其目标 owner
- [x] 1.4 列出 `CharacterPresentationStage.cs` 中 animation transaction、interpolation 与 diagnostics 方法边界
- [x] 1.5 使用 `rg` 列出所有 `ThirdPersonCharacter.Pipeline.Presentation` 动画类型调用方
- [x] 1.6 建立旧文件、目标文件、目标 namespace 与 `.meta` 的一一迁移表
- [x] 1.7 确认迁移不包含 Corin 资产、Timeline 数据、StateMachine 数据或序列化字段修改

## 2. Animation 公共合同

- [x] 2.1 创建 `Pipeline/Animation/Contracts` 目录
- [x] 2.2 将 playback、contribution 与 owner identity 提取到 `AnimationIdentity.cs`
- [x] 2.3 将 `AnimationContribution` 及其来源合同提取到 `AnimationContribution.cs`
- [x] 2.4 将 command kind、handoff intent、owner ready 与 ordered command 提取到 `AnimationLifecycleCommand.cs`
- [x] 2.5 定义 `IAnimationLifecycleFactSink`
- [x] 2.6 定义 `IAnimationLifecycleBatchSource`
- [x] 2.7 将两个 lifecycle 接口放入 `AnimationLifecycleInterfaces.cs`
- [x] 2.8 将 layer definition、output policy、blend mode 与 resolved layer 提取到 `AnimationLayerDefinition.cs`
- [x] 2.9 将 priority group、state plan 与 DesiredCandidate 提取到 `AnimationLayerCandidate.cs`
- [x] 2.10 将 plan kind、causal disposition、causal record、HandoffPlan 与 LayerPlan 提取到 `AnimationLayerPlan.cs`
- [x] 2.11 将 playback status、handoff lifecycle 与 PlaybackOutput 提取到 `AnimationLayerPlaybackOutput.cs`
- [x] 2.12 将公共合同 namespace 统一为 `ThirdPersonCharacter.Pipeline.Animation`
- [x] 2.13 保持所有合同字段、identity equality 与 runtime validation 语义不变

## 3. Lifecycle Queue 与 Registry

- [x] 3.1 创建 `Pipeline/Animation/Lifecycle` 目录
- [x] 3.2 将 `CharacterAnimationLifecycleCommandQueue` 移入独立文件
- [x] 3.3 让 Queue 实现 `IAnimationLifecycleFactSink`
- [x] 3.4 让 Queue 实现 `IAnimationLifecycleBatchSource`
- [x] 3.5 保持 Queue 的 tick、phase、sequence 排序不变
- [x] 3.6 保持 Queue acknowledge 只删除已提交 sequence 范围
- [x] 3.7 将 Registry debug entry 与 Registry snapshot 迁入 Registry 所有权文件
- [x] 3.8 将 `CharacterAnimationContributionRegistry` 移入独立文件
- [x] 3.9 保持 Registry 只处理 Sample、Complete、Release 与 owner membership
- [x] 3.10 确认 Registry 不引用 HandoffLedger、LayerPlan、PlaybackState 或 Animancer
- [x] 3.11 保持 Registry reset/dispose 清理全部 producer lifecycle 状态

## 4. Producer 与 Consumer 权限边界

- [x] 4.1 将 `CharacterGraphContext` 的 Queue 依赖改为 `IAnimationLifecycleFactSink`
- [x] 4.2 保持 StateMachine handoff 只通过 FactSink 提交
- [x] 4.3 保持 AnimationOwnerReady 只通过 FactSink 提交
- [x] 4.4 将 `TimelinePlaybackScheduler` 的 Queue 依赖改为 `IAnimationLifecycleFactSink`
- [x] 4.5 将 `TimelineTreeRuntimeSet` 的 animation lifecycle 依赖改为 FactSink
- [x] 4.6 将 `CharacterBTSMTLPhase` 向下传递的依赖收敛为 FactSink
- [x] 4.7 将 `CharacterPresentationStage` 的 Queue 依赖改为 `IAnimationLifecycleBatchSource`
- [x] 4.8 保持 Stage 在 plan/output 成功提交后才 acknowledge batch
- [x] 4.9 让 `CharacterPipeline` 继续唯一构造具体 Queue
- [x] 4.10 让 `CharacterPipeline` 分别以 FactSink 和 BatchSource 视图装配 producer 与 Stage
- [x] 4.11 保持 deactivate/dispose 只清理同一个具体 Queue
- [x] 4.12 确认没有新增第二个 command list、event bus 或 producer 专用 Queue

## 5. Arbitrator 内部职责拆分

- [x] 5.1 创建 `Pipeline/Animation/Arbitration` 目录
- [x] 5.2 将 layer catalog 解析保留为 Arbitrator 私有或 internal 单一实现
- [x] 5.3 提取无状态 `AnimationLayerCandidateAllocator`
- [x] 5.4 将 layer 过滤与非法 layer error 迁入 CandidateAllocator
- [x] 5.5 将 priority grouping 与 override 剩余权重分配迁入 CandidateAllocator
- [x] 5.6 将同优先级归一与 additive 保留迁入 CandidateAllocator
- [x] 5.7 确认 CandidateAllocator 不读写 ledger、playback 或 LayerPlan
- [x] 5.8 提取唯一有状态 `AnimationHandoffLedger`
- [x] 5.9 将 ordered handoff ingest 与 instance 去重迁入 Ledger
- [x] 5.10 将 Ready leaf 与 released owner retention 迁入 Ledger
- [x] 5.11 将 per-layer disposition 保存迁入 Ledger
- [x] 5.12 定义只读 `AnimationHandoffLedgerSnapshot`
- [x] 5.13 将 disposition commit 与 record/ready prune 迁入 Ledger
- [x] 5.14 将 reset/deactivate/seek 所需的 ledger 清理收敛到 Ledger.Reset
- [x] 5.15 提取无状态 `AnimationHandoffCausalGraphBuilder`
- [x] 5.16 将严格 command order 连接规则迁入 GraphBuilder
- [x] 5.17 将 logical/resolved owner 精确连接与 activation generation 规则迁入 GraphBuilder
- [x] 5.18 将 component union、adjacency 与 path enumeration 迁入 GraphBuilder
- [x] 5.19 确认 GraphBuilder 不读取 priority、不选择 Driver且不修改 Ledger
- [x] 5.20 定义 internal 只读 causal graph/component/path 结果
- [x] 5.21 提取无状态 `AnimationLayerHandoffResolver`
- [x] 5.22 将 source/target relevance 与 Ready 判断迁入 Resolver
- [x] 5.23 将 component authority 计算迁入 Resolver
- [x] 5.24 将 Selected、Coalesced、Retired 与 Conflict 选择迁入 Resolver
- [x] 5.25 将最后 Driver、Hold、Invalid、Update、Empty 与 Handoff 决策迁入 Resolver
- [x] 5.26 定义单帧 internal `AnimationLayerResolution`
- [x] 5.27 确认 Resolution 不持久化、不进入 Presenter且不成为 diagnostics 第二数据源
- [x] 5.28 将 `CharacterAnimationLayerArbitrator` 收敛为唯一 façade
- [x] 5.29 让 Arbitrator 私有构造并独占 Allocator、Ledger、GraphBuilder 与 Resolver
- [x] 5.30 让 Arbitrator 每帧只 ingest 一次 command batch
- [x] 5.31 让 Arbitrator 每帧只捕获一次 ledger snapshot
- [x] 5.32 让所有 layer 基于同一 ledger/playback batch 生成 resolution
- [x] 5.33 让 Arbitrator 在全部 layer resolve 后一次 commit dispositions
- [x] 5.34 让 Arbitrator 在 commit 后统一 prune Ledger
- [x] 5.35 让 Arbitrator 为每个正式 LayerId 输出且只输出一个 LayerPlan
- [x] 5.36 删除 Arbitrator 内已经迁移的 allocation、ledger、graph 与 resolver 旧方法

## 6. Playback Runtime 拆分

- [x] 6.1 创建 `Pipeline/Animation/Playback` 目录
- [x] 6.2 将 `IAnimationInertializationAdapter` 与 inertialization debug 合同迁入 Playback
- [x] 6.3 提取每层 `AnimationLayerPlaybackState`
- [x] 6.4 将 FinalOutput、HeldOutput 与 DesiredCandidate 状态迁入 PlaybackState
- [x] 6.5 将唯一 ActiveHandoff 与 elapsed/duration 状态迁入 PlaybackState
- [x] 6.6 将 Immediate、CrossFade、Inertialization 与 supersede 执行迁入 PlaybackState
- [x] 6.7 将 blend weight composition 迁入 PlaybackState
- [x] 6.8 将 ActiveHandoff 与 WeightedPlan 保持为 PlaybackState 私有 helper
- [x] 6.9 将 `CharacterAnimationLayerRuntime` 收敛为多 layer 协调器
- [x] 6.10 保持 Runtime 每层每帧只 Apply 一个 LayerPlan
- [x] 6.11 保持 Runtime 只向 Arbitrator 暴露只读 playback snapshots
- [x] 6.12 确认 Runtime 与 PlaybackState 不引用 raw lifecycle command、Ledger 或 causal graph
- [x] 6.13 保持 Hold/Invalid 维持最后合法 output且不产生 fallback
- [x] 6.14 保持 reset/dispose 退休全部 active inertialization session

## 7. Diagnostics 与 Presentation

- [x] 7.1 创建 `Pipeline/Animation/Diagnostics` 目录
- [x] 7.2 将 `AnimationLayerSnapshot` 与 `AnimationLayerFrameSnapshot` 迁入独立 diagnostics 文件
- [x] 7.3 保持 snapshot 只读取正式 candidate、causal records、LayerPlan 与 playback lifecycle
- [x] 7.4 提取 `CharacterAnimationTracePublisher`
- [x] 7.5 将 Registry lifecycle trace 发布迁入 TracePublisher
- [x] 7.6 将 arbitration/plan/playback trace 发布迁入 TracePublisher
- [x] 7.7 确认 TracePublisher 不保存第二份 Ledger 或 LayerPlan 状态
- [x] 7.8 创建 `Presentation/Animancer` 目录
- [x] 7.9 移动 `AnimancerAnimationPresenter.cs` 及其 `.meta`
- [x] 7.10 移动 `AnimancerInertializationOutput.cs` 及其 `.meta`
- [x] 7.11 将具体 adapter namespace 收敛到 `ThirdPersonCharacter.Pipeline.Presentation.Animancer`
- [x] 7.12 将 `CharacterPresentationDebug.cs` 与 `.meta` 重命名为 `CharacterPresentationPose.cs`
- [x] 7.13 保持 CharacterPresentationRootPose 与 CharacterVisualPose 合同不变
- [x] 7.14 提取 `CharacterPresentationInterpolator`
- [x] 7.15 将 previous/current logic sample 与 visual-root bind pose 状态迁入 Interpolator
- [x] 7.16 将 interpolation alpha、logic root pose 与 visual pose 计算迁入 Interpolator
- [x] 7.17 将 `CharacterPresentationStage` 收敛为表现事务聚合根
- [x] 7.18 保持 Sample、batch copy、Registry、Arbitrator、Runtime、Presenter、acknowledge 顺序不变
- [x] 7.19 保持 Stage 每个表现帧只调用一次 Presenter final apply

## 8. Preview、Runtime 与 Editor 引用迁移

- [x] 8.1 更新 `CharacterPipeline` 的 Animation 与 Presentation namespace 引用
- [x] 8.2 更新 `CharacterPipelineDefinition` 的 layer contract 引用
- [x] 8.3 更新 `CharacterPipelineFrame` 与 `CharacterPipelineOutput` 的 plan/output/snapshot 引用
- [x] 8.4 更新 `CharacterPipelineHost` 正式 runtime 引用
- [x] 8.5 更新 `CharacterPipelineHost` Preview runtime 使用同一模块链路
- [x] 8.6 保持 Preview 连续播放、seek、target switch 与 dispose reset 语义不变
- [x] 8.7 更新 Camera 对 CharacterPresentationPose 的正式引用
- [x] 8.8 更新 Agent snapshot/exporter 的 layer contract 引用
- [x] 8.9 更新 Agent compiler/validator 的 layer contract 引用
- [x] 8.10 更新 Host Inspector 与 runtime diagnostics 的 snapshot 引用
- [x] 8.11 更新 Timeline Editor Preview 的 Presenter 与 playback 引用
- [x] 8.12 确认 Preview 不直接调用 Ledger、GraphBuilder 或 Resolver

## 9. 旧路径清理与静态审计

- [x] 9.1 删除旧 `Presentation/CharacterAnimationContributionLifecycle.cs` 位置及迁移后空实现
- [x] 9.2 删除旧 `Presentation/CharacterAnimationLayerRuntime.cs` 聚合文件及迁移后空实现
- [x] 9.3 删除旧 `CharacterPresentationDebug.cs` 文件名
- [x] 9.4 删除旧 `ThirdPersonCharacter.Pipeline.Presentation` 下的动画业务类型定义
- [x] 9.5 使用 `rg` 确认不存在旧 namespace forwarding、alias 或兼容 wrapper
- [x] 9.6 使用 `rg` 确认只有一个 `CharacterAnimationLifecycleCommandQueue` 实现
- [x] 9.7 使用 `rg` 确认只有一个 `CharacterAnimationContributionRegistry` 实现
- [x] 9.8 使用 `rg` 确认只有一个 `CharacterAnimationLayerArbitrator` 对外 commit 入口
- [x] 9.9 使用 `rg` 确认 LayerPlan 只由 Arbitrator 正式提交
- [x] 9.10 使用 `rg` 确认 ActiveHandoff 只由 PlaybackState 持有
- [x] 9.11 使用 `rg` 确认 Graph/Logic 不再为动画事实依赖具体 Presentation 类型
- [x] 9.12 使用 `rg` 确认 Animation 模块不依赖具体 Animancer adapter
- [x] 9.13 使用 `rg` 确认不存在 preview 专用 Registry、Arbitrator、Resolver 或 Runtime
- [x] 9.14 使用 `rg` 确认没有新增 fallback、默认 Idle、旧 SO 或第二套播放路径
- [x] 9.15 确认所有移动的 Unity 脚本均保留原 `.meta` GUID

## 10. 文档与静态校验

- [x] 10.1 更新 `openspec/project.md` 的 Animation 与 Presentation 目录归属
- [x] 10.2 更新 `openspec/project.md` 的 producer/consumer 窄接口和内部仲裁步骤
- [x] 10.3 更新仍 active diagnostics change 中受文件与 namespace 迁移影响的引用
- [x] 10.4 在 `3cDemo/Client/3C_Client` 运行 `dotnet build 3C_Client.sln --disable-build-servers /nr:false /p:UseSharedCompilation=false`
- [x] 10.5 编译命令结束后立即运行 `dotnet build-server shutdown`
- [x] 10.6 处理编译发现的旧类型、旧 namespace 与可见性引用
- [x] 10.7 再次使用 required flags 编译受影响 assemblies
- [x] 10.8 再次编译结束后立即运行 `dotnet build-server shutdown`
- [x] 10.9 运行 `openspec validate refactor-character-animation-module-layout --strict --no-interactive`
