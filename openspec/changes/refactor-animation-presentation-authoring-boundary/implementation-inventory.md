# Implementation Inventory

## 审计状态

- 审计日期：2026-07-13。
- 迁移前置审计阶段没有修改运行时代码、Editor 代码或 Unity 资产。
- Corin 的 31 条旧转场中有 20 条使用项目自有 `Inertialization`。已安装的 Animancer 8.2.2 没有原生惯性化 API。
- 用户已确认这些 transition、Driver 与 Inertialization 数据均由此前中间实现生成，不是需要保真的业务资产。apply 直接删除它们，并重新建立 Animancer 原生正式配置。

## Git 基线

当前工作树把以下相关目录整体报告为未跟踪目录，因此 Git 无法进一步区分目录内哪些文件是本轮之前新增、哪些是本轮之前修改：

| 状态 | 路径 |
| --- | --- |
| `??` | `3cDemo/Client/3C_Client/Assets/Configs/Character/Corin/Pipeline/` |
| `??` | `3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/BTSMTL/` |
| `??` | `3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/` |
| `??` | `3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Graph/` |
| `??` | `3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Presentation/` |
| `??` | `3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Runtime/` |
| `??` | `openspec/changes/refactor-animation-presentation-authoring-boundary/` |

本 change 涉及的现有实现面如下：

| 区域 | 当前主要文件 |
| --- | --- |
| Animation Contracts | `AnimationIdentity.cs`、`AnimationContribution.cs`、`AnimationLayerCandidate.cs`、`AnimationLayerDefinition.cs`、`AnimationLayerPlan.cs`、`AnimationLayerPlaybackOutput.cs`、`AnimationLifecycleCommand.cs`、`AnimationLifecycleInterfaces.cs`、`AnimationPresentationBindingIndex.cs`、`AnimationPresentationDefinition.cs` |
| Animation Lifecycle | `CharacterAnimationPresentationAdapter.cs`、`CharacterAnimationExecutionLineage.cs`、`CharacterAnimationLifecycleCommandQueue.cs`、`CharacterAnimationContributionRegistry.cs` |
| Animation Arbitration | `CharacterAnimationLayerArbitrator.cs`、`AnimationLayerHandoffResolver.cs`、`AnimationLayerCandidateAllocator.cs`、`AnimationHandoffLedger.cs`、`AnimationHandoffCausalGraphBuilder.cs` |
| Animation Playback | `CharacterAnimationLayerRuntime.cs`、`AnimationLayerPlaybackState.cs`、`IAnimationInertializationAdapter.cs` |
| Animation Diagnostics | `AnimationLayerFrameSnapshot.cs`、`CharacterAnimationTracePublisher.cs` |
| Pipeline 集成 | `CharacterPipeline.cs`、`CharacterGraphContext.cs`、`CharacterBTSMTLPhase.cs`、`TimelinePlaybackScheduler.cs`、`CharacterPresentationStage.cs`、`CharacterPipelineHost.cs` |
| Animancer 输出 | `AnimancerAnimationPresenter.cs`、`AnimancerInertializationOutput.cs` |
| BTSMTL 反向依赖 | `RunnableNode.cs`、`TreeExecutionLifecycle.cs`、StateMachine control-flow 与 presentation 字段相关实现 |
| Editor 与 Agent | `CharacterAnimationPresentationAuthoringService.cs`、`AgentAuthoringModels.cs`、`AgentGraphSnapshotExporter.cs`、`AgentPatchCompiler.cs` 及旧 Driver Inspector 路径 |
| Corin 资产 | `CorinPlayableRootTree.asset`、`CorinCharacterPipelineDefinition.asset` 及其 inline StateMachine/Timeline 数据 |

## 当前异常与合法 State.None

### 未知 activation

现有调用顺序为：

1. `RunnableNode.BeginActivation` 调用 `TreeExecutionContext.BeginActivation`。
2. `BeginActivation` 在目标 activation 的 `RunnableActivationEntered` 之前先提交 `TreeControlFlowCommitted`。
3. `CharacterAnimationPresentationAdapter` 立即把 committed fact 翻译成 topology。
4. `CharacterAnimationExecutionLineage.RetainTopologyRecord` 对 topology 端点调用 `AddReferenceChain`。
5. 目标 activation 尚未通过 entered fact 注册进 lineage，因而抛出 `Animation lineage reference targets unknown activation`。

这是动画拓扑投影对 Tree 事件顺序的错误假设，不是 Corin 动画资源缺失或单条资产配置错误。

### Driver 数量错误与动画丢失

`CharacterAnimationLayerRuntime` 要求 visible owner 切换时 topology 必须推导出恰好一个 Driver。分支延迟激活、退出或同帧重入时，现有链会得到零个或多个 Driver，进入 invalid hold，并停止产生正常动画输出。用户看到的动画丢失、攻击后卡住与该 invalid hold 一致。

### 合法空状态被误判

`RootNode.OnUpdate` 在没有可运行 child 或 Tree 已停止时允许返回 `State.None`。`StateBehaviorSubTree` 的空 enter/root/exit 结构会经过这条合法语义。当前 `RunnableNode.UpdateNode` 在每次 `OnUpdate` 后无条件调用 `PublishExecuted`，把 `State.None` 转成 `TreeExecutionResult.None`；`TreeExecutionContext.PublishExecuted` 又把该 fact 判为 `InvalidExecuted`。这是为了动画 owner 推断新增的通用 Runnable lifecycle 反向破坏 BTSMTL 语义。

## 当前运行链

| 顺序 | 输入 | 当前处理 | 输出与问题 |
| --- | --- | --- | --- |
| 1 | Runnable/StateMachine 执行 | `TreeExecutionLifecycle` 发布 entered、executed、released、committed | Tree 逻辑被迫提供动画 activation topology |
| 2 | Tree facts | `CharacterAnimationPresentationAdapter` | 翻译为 topology、ready、handoff command |
| 3 | activation 与 topology | `CharacterAnimationExecutionLineage` | 维护 owner membership 和引用链，产生未知 activation 异常 |
| 4 | Timeline animation sample | `TimelinePlaybackScheduler.ActiveTimeline` 注册 producer 并提交 contribution | sample 携带 owner、Priority 和 Driver 依赖 |
| 5 | lifecycle command | `CharacterAnimationLifecycleCommandQueue` | 跨帧保存 topology、ready、handoff、contribution |
| 6 | queue batch | `CharacterAnimationContributionRegistry` | 收集候选并依赖 lineage 判断 membership |
| 7 | registry snapshot | `CharacterAnimationLayerArbitrator` 与 `Animation/Arbitration` | 再按 Priority、Driver 和 causal component 选 visible owner |
| 8 | selected candidate | `CharacterAnimationLayerRuntime` | 自算 `LayerPlan`、`ActiveHandoff`、crossfade 与惯性化状态 |
| 9 | playback output | `AnimancerAnimationPresenter` | 手动写 state time/weight，并用 `Evaluate(0)` 输出；Animancer 未成为 fade 时间权威 |

该链是本 change 要整体替换的中间实现，不能局部修补 lineage 或 Driver 数量后继续保留。

## Corin 稳定身份

### Graph 身份

| Graph | AuthoringId |
| --- | --- |
| RootTree | `79647291-0c69-4e9e-9276-96a93c3647e7` |
| Locomotion StateMachine | `8968c5a0-f19d-487f-94cc-01cd191fee7d` |
| Action StateMachine | `fdfba4db-d919-460a-a2f3-eb8149c7610c` |
| Attack state body | `3ae19d5e-dd52-4f44-a80d-e32d2474e7ec` |
| Nested Attack StateMachine | `ba00b356-4bf5-4b38-9bec-7ac934b7a25c` |

### Timeline producer 身份

全部 producer 当前写入语义层 `Base`。`Priority` 是待删除的旧表现字段，只在这里作为迁移证据保存。

| Producer | NodeId | TimelineId | TrackId | ClipId | Animation GUID | Priority | Frame |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Idle | `29d4be7b-7bf2-537e-81d4-683c052ad71b` | `c6e83124-336e-4405-8d9d-0e6b28fd8103` | `dc73675d-ae72-48f0-9e57-edcbd0bb55ad` | `a99cfc56-8257-4b39-9b23-7e895467491e` | `7dbd59f643bcfbe4495990d45ad359ef` | 0 | 0-161 |
| WalkStart | `1b3bbcbe-98f5-533c-9449-876b7f3a8882` | `fd83f793-f3c3-4d28-abec-ccbef3f0f3d5` | `9f166e87-b3bc-48ca-a90a-7ef476cfe3bd` | `aacb9d6f-a1eb-4287-8e3e-0b83b1ed7dbc` | `90d528fc935ce2e43b4b301c8c2b98db` | 0 | 0-75 |
| WalkLoop | `e24c7688-1a49-5987-b13f-20a0371e3a9d` | `e7ea1649-1085-47f2-b9ab-4b013ecb78b3` | `762b4d39-92af-42f3-b8c0-7ac9be15afb7` | `6094493b-d4e3-4730-b7eb-7eb7d7c3565f` | `a42a3b1b15a10b44f9892c728d72b74f` | 0 | 0-36 |
| RunStart | `b9a7efff-6ab0-5c28-8f94-58ddc2dc7d75` | `4e832b73-5961-4eb3-875d-d729555d1aa9` | `7e3c2795-f922-44d1-bb5b-b1dbe5baf99a` | `e42ddb7b-d061-4976-ae7e-8c2599777546` | `c3f6deae23e56064fb4b8938601e35e5` | 0 | 0-63 |
| RunLoop | `c9d0f6dd-f690-5496-964d-782800ff3270` | `68286aed-b84f-4b77-8906-a43806c41bfa` | `e9e8b58c-4813-4b9f-9e32-8b9f2b57d3f9` | `69bab49f-2e55-44e2-a2a5-c6403981d8bb` | `3747a185f0711e842b4df7c03fa2cfac` | 0 | 0-30 |
| RunEnd | `672bd4bb-258c-5eb0-ba4b-e6b8e0cf16c4` | `1fd891af-e93c-4208-b2d3-7f448e87d97a` | `681cd134-2253-4956-8ffb-55a893974192` | `f1f2d891-95c3-4049-b954-bb8d2c7fb2c3` | `9654782e7ebbcd14bab93c76dc248019` | 0 | 0-136 |
| MovingTurn | `47e8c663-5df9-53bb-9a62-c67a2dd00c8e` | `04758f73-9b79-4142-ab05-5c4159ea6ba8` | `e1e9df79-5033-4856-857e-0060a28517f2` | `0c9d8066-371a-4731-8304-acf54f8b76d2` | `e569e81bd2858154b9bf4f2e660cf981` | 0 | 0-71 |
| Attack1 | `35f64fa2-0ff9-5fcc-a8e6-393fb9c61782` | `10f4cb90-8b9a-4944-b77c-14efc9a3124d` | `0811fba7-c4c7-4cc3-9714-f93b9da4d4ab` | `e34b7999-c4fa-4f8b-8425-5d7ed8de8159` | `1085463043eec484c9173b05b7037f92` | 100 | 0-49 |
| Attack2 | `f7e0a35f-8383-5c84-b7d3-0409f82e6f59` | `40908a3b-5568-459b-b4f0-b871155dc226` | `321b312e-7450-4482-b10e-11066d42129c` | `f9929a1c-362c-4ec2-a908-b80ee39c9ab8` | `4ef82b85767cffe4883132371252902e` | 100 | 0-48 |
| DodgeBack | `2379310b-cbad-44e2-9f3b-398442659c85` | `1ec9175b-a959-4960-af6b-4177f601425f` | `ec241d60-b292-47bf-8d33-f1bccb68521b` | `044298f5-fe4d-4496-a75c-512d33f6c37e` | `edd7c59a8dbf622489dcbd2692cd2ecd` | 100 | 0-141 |
| DodgeForward | `3f4f4ef4-8a13-430c-afc4-c772e6612e72` | `86e3cd9c-eab8-41b0-b2f5-e9a73c3cfa27` | `ddd84d90-c3aa-4944-8fc0-10527e379910` | `354d0739-cd16-4a89-867d-066bfc98caf7` | `6f9dc7e23ba63244e95d8f8d7142c5a2` | 100 | 0-141 |

`WalkEnd` 当前没有动画 producer，不能在迁移时生成默认动画或隐式复用其它 producer。

## 旧 transition 完整导出

来源：`Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset`。

Layer catalog 当前只有一层：`Base`，Animancer layer index 为 0，`OutputPolicy=1`，`ApplyToAnimancer=1`。旧 `m_DriverBindings` 共 31 条，每条只有一个 Base layer Driver。

### Route 分类

| 行号 | Route |
| --- | --- |
| 1-21 | Root `79647291-0c69-4e9e-9276-96a93c3647e7` -> owner `be1b96f8-b19f-5d57-a23a-bc7877164a00` -> Locomotion `8968c5a0-f19d-487f-94cc-01cd191fee7d` |
| 22-27 | Root -> owner `44f5b1d2-416a-5c09-ac71-92cf97fa622b` -> Action `fdfba4db-d919-460a-a2f3-eb8149c7610c` |
| 28-29 | Root 直接 site，`m_Segments` 为空 |
| 30-31 | Root -> Action owner `44f5b1d2-416a-5c09-ac71-92cf97fa622b` -> state body owner `ff371195-3c10-4c47-9f93-440accbee2c3` -> body graph `3ae19d5e-dd52-4f44-a80d-e32d2474e7ec` -> nested SM owner `1afcf514-7458-4e28-aaba-10b485528f91` -> graph `ba00b356-4bf5-4b38-9bec-7ac934b7a25c` |

### Curve payload

28 条非 Immediate 配置使用完全相同的线性曲线：

```text
key 0: time=0, value=0, inSlope=0, outSlope=1, tangentMode=0,
       weightedMode=0, inWeight=0, outWeight=0
key 1: time=1, value=1, inSlope=1, outSlope=0, tangentMode=0,
       weightedMode=0, inWeight=0, outWeight=0
preInfinity=2, postInfinity=2, rotationOrder=4
```

3 条 Immediate 配置的 curve key 列表为空，`preInfinity=2`、`postInfinity=2`、`rotationOrder=4`。没有第三种曲线签名。

### 数量统计

| Strategy | Duration | Curve | 数量 |
| --- | --- | --- | --- |
| Immediate | 0 | Empty | 3 |
| ContributionCrossFade | 0.08 | Linear01 | 3 |
| ContributionCrossFade | 0.1 | Linear01 | 5 |
| Inertialization | 0.06 | Linear01 | 3 |
| Inertialization | 0.08 | Linear01 | 11 |
| Inertialization | 0.1 | Linear01 | 4 |
| Inertialization | 0.12 | Linear01 | 2 |

### 逐项原生映射

`Site=1` 表示 node site，`Site=2` 表示 transition edge site。`可映射` 只表示 Animancer 原生播放语义具备等价表达能力，尚未创建或写入新资产。

表中的 `阻塞` 记录 API 审计结果，即 Animancer 没有原生惯性化等价物；用户确认旧数据可删除后，这些行的正式处置是删除而不是迁移，不再阻塞 apply。

| # | Source Graph | Source Element | Site | Strategy | Duration | Curve | Animancer 原生映射 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | Locomotion | `cefb032a-559d-5b49-a828-636ab8675b57` | 2 | ContributionCrossFade | 0.1 | Linear01 | 可映射：TransitionLibrary fade duration + linear FadeGroup |
| 2 | Locomotion | `75d831fc-f4bf-5b7c-a943-7db96ae35107` | 2 | ContributionCrossFade | 0.1 | Linear01 | 可映射：TransitionLibrary fade duration + linear FadeGroup |
| 3 | Locomotion | `cfd47800-17b5-5bdf-b52f-05186263aae0` | 2 | ContributionCrossFade | 0.08 | Linear01 | 可映射：TransitionLibrary fade duration + linear FadeGroup |
| 4 | Locomotion | `286730dd-a1eb-520e-b7da-20fe5deeed66` | 2 | ContributionCrossFade | 0.1 | Linear01 | 可映射：TransitionLibrary fade duration + linear FadeGroup |
| 5 | Locomotion | `088613ee-853b-5c84-80f8-bcc7a36589dc` | 2 | ContributionCrossFade | 0.08 | Linear01 | 可映射：TransitionLibrary fade duration + linear FadeGroup |
| 6 | Locomotion | `1de2c561-6b9e-5443-9d1e-e949c7228ed1` | 2 | ContributionCrossFade | 0.08 | Linear01 | 可映射：TransitionLibrary fade duration + linear FadeGroup |
| 7 | Locomotion | `5a7962f8-6408-591f-b85a-6f5bae17a85d` | 2 | Inertialization | 0.1 | Linear01 | 阻塞：Animancer 无原生惯性化 |
| 8 | Locomotion | `7494ee92-1226-533f-9ce9-ee03dca3c69b` | 2 | Inertialization | 0.1 | Linear01 | 阻塞：Animancer 无原生惯性化 |
| 9 | Locomotion | `fd23e77d-55d9-5d98-ae48-0afa84f3314c` | 2 | Inertialization | 0.08 | Linear01 | 阻塞：Animancer 无原生惯性化 |
| 10 | Locomotion | `8a410586-1246-5d8f-a3fb-2d7030477dfc` | 2 | Inertialization | 0.08 | Linear01 | 阻塞：Animancer 无原生惯性化 |
| 11 | Locomotion | `241d4915-d761-45db-bba8-76307ab3d710` | 2 | Immediate | 0 | Empty | 可映射：AnimancerLayer.Play，零时长 |
| 12 | Locomotion | `a2c2dac7-2c1e-4b74-be8e-58180e79d475` | 2 | ContributionCrossFade | 0.1 | Linear01 | 可映射：TransitionLibrary fade duration + linear FadeGroup |
| 13 | Locomotion | `2cf4ef14-7cf8-4476-8e30-c8e34633ffa9` | 2 | ContributionCrossFade | 0.1 | Linear01 | 可映射：TransitionLibrary fade duration + linear FadeGroup |
| 14 | Locomotion | `bc0e0840-8938-4217-818d-c82fb5efcfc5` | 2 | Inertialization | 0.08 | Linear01 | 阻塞：Animancer 无原生惯性化 |
| 15 | Locomotion | `002e71e1-58d5-46ff-81b6-5d0bf9a79ed1` | 2 | Immediate | 0 | Empty | 可映射：AnimancerLayer.Play，零时长 |
| 16 | Locomotion | `be84cfaa-ae4a-4594-853c-fdb72e670795` | 2 | Immediate | 0 | Empty | 可映射：AnimancerLayer.Play，零时长 |
| 17 | Locomotion | `5bef5235-5d04-4e45-ac33-3decd967d385` | 2 | Inertialization | 0.08 | Linear01 | 阻塞：Animancer 无原生惯性化 |
| 18 | Locomotion | `f9842f44-15dc-49a9-a7eb-68e4206ca5fb` | 2 | Inertialization | 0.08 | Linear01 | 阻塞：Animancer 无原生惯性化 |
| 19 | Locomotion | `8b001fc1-ec21-48ae-bc95-a11be3cc47e5` | 2 | Inertialization | 0.08 | Linear01 | 阻塞：Animancer 无原生惯性化 |
| 20 | Locomotion | `a720ebce-57a2-427d-b372-358458f7da9b` | 2 | Inertialization | 0.08 | Linear01 | 阻塞：Animancer 无原生惯性化 |
| 21 | Locomotion | `ef8602ca-593f-4fb9-95cf-232919542f0b` | 2 | Inertialization | 0.08 | Linear01 | 阻塞：Animancer 无原生惯性化 |
| 22 | Action | `8895a79e-15d9-5de4-a114-9657aadfed55` | 2 | Inertialization | 0.06 | Linear01 | 阻塞：Animancer 无原生惯性化 |
| 23 | Action | `bea2f4da-77b5-4c69-8717-0043041d724e` | 2 | Inertialization | 0.08 | Linear01 | 阻塞：Animancer 无原生惯性化 |
| 24 | Action | `7abaec8f-be58-4231-aad3-b45d5cfaa2a5` | 2 | Inertialization | 0.08 | Linear01 | 阻塞：Animancer 无原生惯性化 |
| 25 | Action | `9be1f7c4-e18d-4962-9841-3eb2094ab2ce` | 2 | Inertialization | 0.1 | Linear01 | 阻塞：Animancer 无原生惯性化 |
| 26 | Action | `d25c2b4b-aeb1-4407-8499-762b4ec66b07` | 2 | Inertialization | 0.1 | Linear01 | 阻塞：Animancer 无原生惯性化 |
| 27 | Action | `30624ac2-3e3f-482a-aa8e-af6bce4a1ec6` | 2 | Inertialization | 0.08 | Linear01 | 阻塞：Animancer 无原生惯性化 |
| 28 | Root | `be1b96f8-b19f-5d57-a23a-bc7877164a00` | 1 | Inertialization | 0.12 | Linear01 | 阻塞：Animancer 无原生惯性化 |
| 29 | Root | `44f5b1d2-416a-5c09-ac71-92cf97fa622b` | 1 | Inertialization | 0.12 | Linear01 | 阻塞：Animancer 无原生惯性化 |
| 30 | Nested Attack | `abcae7cb-bf6d-512d-8fab-02d0259853c7` | 2 | Inertialization | 0.06 | Linear01 | 阻塞：Animancer 无原生惯性化 |
| 31 | Nested Attack | `dd1defb3-47dd-47b7-91ab-f9702630fca9` | 2 | Inertialization | 0.06 | Linear01 | 阻塞：Animancer 无原生惯性化 |

## Animancer 8.2.2 能力核对

已从项目内 `Packages/com.kybernetik.animancer` 源码核对，不依赖外部文档：

| 能力 | 结论 | 证据 |
| --- | --- | --- |
| `TransitionLibraryAsset` | 存在 | `Runtime/Utilities/Transitions/Transition Libraries/TransitionLibraryAsset.cs` |
| `TransitionLibrary.Play` | 存在 | 可按当前 source state 与 target transition 解析 fade duration |
| source-to-target modifier | 存在 | `GetFadeDuration`、`SetFadeDuration` 与 `TransitionModifierDefinition` |
| `ITransition` | 存在 | `Runtime/Interfaces/ITransition.cs` |
| `FadeMode` | 存在 | `Runtime/Data Types/FadeMode.cs` |
| `FadeGroup.Easing` | 存在 | 可接收线性或其它 easing delegate |
| `ManualMixerState` | 存在 | 可承载同一 Timeline producer 内多个 clip child 及其权重 |
| manual evaluate | 存在 | `AnimancerComponent.Evaluate(float)` 与 `AnimancerGraph.Evaluate(float)` |
| Timeline 外部 pose time + Animancer fade time | 可表达 | clip/mixer `Speed=0`，表现帧写入 sample time，再以 presentation delta 调用 `Evaluate`；FadeGroup 使用 graph delta 推进 |
| 原生惯性化 | 不存在 | 在整个 Animancer Runtime C# 源码中搜索 `inertialization`、`inertialisation`、`inertial` 均无匹配 |

该包是 8.2.2 源码版，`Strings.ProOnlyTag` 为空，TransitionLibrary、ManualMixerState 和自定义 easing 的 Pro 代码存在，不是 Lite DLL 缺功能造成的假阴性。

现有 `AnimancerAnimationPresenter` 把 clip time 与权重手工写入后调用 `Evaluate(0)`。这能采样姿势，但不能让 Animancer 的 FadeGroup 使用真实表现帧 delta 推进，必须在目标实现中改为正交的两个时钟。

## 迁移门禁结论

API 审计结果仍是：3 条 Immediate 与 8 条 ContributionCrossFade 有原生对应；20 条 Inertialization 没有原生对应。

业务处置已经明确：31 条旧 transition 全部属于此前中间实现，不迁移 strategy、duration 或 curve。正式配置重新使用 Animancer TransitionLibrary 与原生 fade authoring；项目自有 `AnimancerInertializationOutput`、`IAnimationInertializationAdapter` 和全部 Inertialization session 直接删除。

因此迁移门禁已通过，可以继续实施。该决定不等于在 runtime 中把旧 Inertialization 自动降级为 CrossFade，而是删除非权威旧数据后创建唯一正式 Animancer 配置，不保留旧 parser、转换规则或兼容路径。

## 最终删除检查词

- `AnimationTransitionDefinition`
- `HandoffRole`
- `AnimationOwnerReady`
- `AnimationTopologyRecord`
- `AnimationHandoffIntent`
- `CharacterAnimationPresentationAdapter`
- `CharacterAnimationExecutionLineage`
- `DriverBinding`
- `CausalGraph`
- `Arbitrator`
- `LayerPlan`
- `ActiveHandoff`
- `MissingDriver`
- `AnimationContribution.Priority`

以上名称目前必须保留在旧实现和旧资产中，只有完成正式迁移后才能从 runtime、Editor、Agent 和 current specs 删除；迁移 inventory 与 OpenSpec 历史说明可以继续引用。
