# Design: TreeClip Scope Variable Window 单一路径

## Context

当前同 Tick Window 决策存在两条完整链路：

```text
ActionWindowClip
  -> ActionWindowTrack.Sample
  -> TimelinePlaybackScheduler.m_PreparedDecisionWindows
  -> CharacterGraphContext.m_TimelineDecisionWindows
  -> ActionWindowActiveInfoNode
  -> Commit ActionWindowSample
```

```text
Decision TreeClip
  -> TimelineRunningTree
  -> ExposedPropertyNode Set
  -> PipelineBlackboardRuntime Frame value
  -> PipelineBlackboardValueInfoNode
```

`SubmitActionWindowSampleNode` 还可以从普通 Graph 或 Commit Tree 直接调用 `CharacterGraphContext.SubmitActionWindowSample`。因此同一种动作窗口事实拥有 Timeline Track、Graph ActionNode 和 TreeClip Blackboard 三个作者入口；同一种当前 Tick Bool 条件拥有专用 Window reader 与通用 Blackboard reader 两个消费入口。

目标不是删除动作窗口这个业务事实，而是删除它作为独立 Timeline authoring/runtime 分支。作者应只看到一个可组合模型：TreeClip 决定时间与逻辑，scope variable 决定输出，显式 projection 决定该输出是否需要成为 ActionWindowSample。

## Goals

- TreeClip 成为 Timeline 时间区间逻辑的唯一作者容器。
- Pipeline Blackboard scope variable 成为 Window 当前 Tick active 状态的唯一运行真值。
- Transition、OnExit 和普通 Graph 使用同一 Blackboard ValueNode 读取 Window。
- 需要 ActionInstance、policy、SyncFacts 和 Debug 的变量通过显式 projection 产出正式 ActionWindowSample。
- 保持 TimelinePlaybackScheduler 唯一逻辑时间权威和 Decision -> RootTree -> Commit 屏障。
- 保持 ActionProfile 为 window 网络策略主来源，不把策略复制到 TreeClip 或 declaration。
- 删除 ActionWindowTrack/Clip、专用 cache、reader、submit node 和旧资产，不保留兼容路径。

## Non-Goals

- 不删除 `ActionWindowSample`、ActionProfile window policy、ActionSyncDomain 或 GameplaySync adapter。
- 不把所有 Blackboard variable 自动转换成 SyncFact。
- 不新增通用 Blackboard key/value 网络包。
- 不让 Decision TreeClip 直接发送网络包、判定命中、扣血、移动 Transform 或驱动表现对象。
- 不实现 Begin/End 事件式 Window 生命周期；本 change 保持当前每个 active logic tick 产生一个 Window sample 的合同。
- 不修改 Animation、Motion、Cue 或 Camera Track 的作者模型。
- 不创建 WindowProfile、WindowRegistry、WindowAsset 或一次性 TimelineRunningTree asset。

## Decisions

### 1. TreeClip 是唯一 Timeline Window 作者容器

作者在 Timeline 中创建 Decision TreeClip，并通过 Clip StartFrame/EndFrame 定义时间范围。TreeClip 的 inline TimelineRunningTree 使用现有 ValueNode、条件组合和 ExposedPropertyNode 写入 scope variable。系统不再提供 ActionWindowTrack 或 ActionWindowClip。

业务取舍：作者只学习一种时间区间模型，并可在 inline Tree 中组合输入、条件和多个输出。代价是最简单的 Window 也需要一次 TreeClip 下钻，但现有 TreeClip 创建、命名、下钻和 Graph Data Catalog 已经提供正式编辑入口，不需要新编辑器体系。

### 2. Window active 真值统一为 Bool Frame/Frame variable

Timeline Window 输出 declaration 必须是 Bool、Frame scope、Frame lifetime。Decision TreeClip 在每个 active logic tick 写入 true；Clip 不 active 时本 Tick 不写，Frame cleanup 自动移除旧值。Transition 和 OnExit 只通过通用 Pipeline Blackboard Bool ValueNode 读取。

不使用 ActionInstance lifetime 表达 Timeline 时间区间，因为它会在 Clip 离开后残留 true；不依赖 OnExit 写 false，因为旧 playback 的清理可能覆盖同 Tick 新 playback 的写入。

业务取舍：变量生命周期与 Timeline active range 保持无状态重建，取消和 loop boundary 不需要额外恢复逻辑。代价是 active 区间每 Tick 都会执行一次 Decision Tree，但这是已有 Decision TreeClip 的正式语义。

### 3. Declaration 使用显式 fact projection，而不是 key 约定

Pipeline Blackboard declaration 增加可选 fact projection：

```text
ProjectionKind: None | ActionWindow
WindowType
WindowId
Digest
```

`ActionWindow` projection 只允许 Bool + Frame/Frame declaration，并要求 SyncPolicy 为 `SyncFact`。WindowType、WindowId 和 Digest 是输出身份，不包含 authority、history、replication、correction 或 packet policy；这些策略继续从 ActionInstance 对应 ActionProfile 解析。

普通 local gate 使用 `ProjectionKind=None` 和 `SyncPolicy=None`。系统不得根据 variable key、category、Bool 类型或 true 值猜测 projection。

业务取舍：作者仍只配置 TreeClip 和 scope variable，同时网络/debug 输出具有稳定业务身份。代价是需要在 Graph Data Catalog declaration 详情中多配置一个显式 output binding，但它避免隐式命名规则和通用 key/value 网络化。

### 4. Window projection 必须携带正式写入 provenance

Blackboard 写入 provenance 至少包含：

- declaration identity 与结构化 address；
- local logic tick；
- source Graph/runtime owner；
- TreeClip playback、clip、cycle identity（若来自 Timeline）；
- 显式 Action Context / ActionInstanceId（若 projection 为 ActionWindow）。

TimelineRunningTree 从 `TimelineTreeClipRuntimeContext.ActionContext` 提供动作归属。非 Timeline Graph 若写入 ActionWindow-bound variable，必须通过显式 Action Context 写入上下文提供归属。系统不得读取 ambient current active action，也不得从变量名称、State membership 或 Timeline asset 反推动作。

业务取舍：同一个 Timeline asset 可被不同 ActionInstance 复用，Window sample 仍归属正确实例。代价是 Blackboard write API 需要携带结构化 provenance，但该信息只存在于 runtime frame，不成为第二份 authoring data。

### 5. Fact projection 在 RootTree 决策后统一提交

运行顺序固定为：

```text
BeginFrame
  -> 清理上一 Tick Frame values 与 projection candidates

TimelinePlaybackScheduler.PrepareDecisionFacts
  -> 求值 active Decision TreeClip
  -> 写 Bool Frame variables
  -> 收集显式 ActionWindow projection candidates

BehaviorTreeRuntime.Tick
  -> Transition / OnExit 读取相同 Bool variables
  -> 可能取消 source Timeline

WindowFactProjection
  -> 校验 candidate provenance 与 Action Context
  -> declaration + ActionInstanceId + local tick 去重
  -> 生成 ActionWindowSample
  -> 写入 SyncFacts.Action.WindowSamples 与 ActionRuntime debug

TimelinePlaybackScheduler.Commit
  -> 只推进 retained playback 的非决策贡献与 Commit Tree

EndFrame
  -> 清理 Frame values 与 candidates
```

如果 Decision Window 在本 Tick active 并触发 source State 离开，该 candidate 仍代表本 Tick 已观察到的窗口并提交一次；被取消 playback 不再提交 Motion、Cue、Camera、Animation 或 Commit Tree 输出。local-only gate 没有 projection candidate，因此不会产生 ActionWindowSample。

业务取舍：Transition 和网络/debug 观察同一个 authored Window，不再各采样一次。代价是需要明确 WindowFactProjection barrier，但它是现有 Pipeline phase 内的正式事实投影，不是第二个 Timeline 时间推进器。

### 6. Projection candidate 是帧内输出队列，不是第二个 Blackboard

每次 true 写入显式 ActionWindow-bound declaration 时，runtime 记录一个只读 candidate。candidate 只保存生成正式 fact 所需的 declaration、provenance 和 Tick identity，并在本帧 projection 后清空。它不提供任意读写 API、不被 ConditionRuleGraph 查询、不跨 Tick持久化，也不拥有 authoring 配置。

同一 declaration、ActionInstanceId 和 local tick 最多生成一个 sample。不同 ActionInstance 的相同 declaration 不互相覆盖。Blackboard value 继续是条件读取真值，candidate 只是该次写入的输出记录。

业务取舍：既避免多个 playback 写同一 Frame variable 时丢失 ActionInstance provenance，也不建立第二套决策状态存储。

### 7. 删除专用 Window producer 和 reader

迁移完成后删除：

- `ActionWindowTrack`、`ActionWindowClip`、`TimelineActionWindowSample`；
- `ActionWindowClipInspectorView`；
- Scheduler 的 ActionWindow Track 扫描、prepared samples、keys 和 submit helper；
- `BeginTimelineDecisionFacts`、`AddTimelineDecisionWindow`、`IsCurrentTickActionWindowActive`；
- `ActionWindowActiveInfoNode`；
- `SubmitActionWindowSampleNode`；
- Agent 对专用 Window reader/submit 节点的 emitter、macro、patch 和 validator 分支。

`CharacterGraphContext.SubmitActionWindowSample` 可以收口为 projection stage 的内部正式提交函数，但不得继续作为 Graph/Timeline 作者 API 暴露。

业务取舍：阻止后续功能绕过 scope variable 再建立快捷路径。代价是非 Timeline Window 也必须通过 scope variable 与 projection 表达，但这正好复用同一 Blackboard owner/scope 生命周期模型。

### 8. Graph Data Catalog 是唯一 projection 编辑入口

Blackboard declaration 的展开详情在现有 Graph Data Catalog 中显示 Projection。选择 `ActionWindow` 后显示 WindowType、WindowId、Digest，并即时校验 Bool、Frame/Frame、SyncFact 和当前 owner 可见性。继承 declaration 只读显示 projection，并提供定位 owner；不在 TreeClip Inspector 复制完整 projection 编辑器。

TreeClip Inspector 只显示该 Clip 的 Decision/Commit、ownership、Graph 名称和引用到的输出 declaration 摘要。作者下钻后通过同一个 Catalog 创建或选择变量。

业务取舍：变量身份、scope 和输出映射保持在 declaration 唯一来源，TreeClip 只引用它。代价是编辑 Window 身份需要展开 declaration 详情，但统一目录已经支持该交互。

### 9. ActionProfile 和网络边界保持不变

Projection 生成的 `ActionWindowSample` 必须携带 ActionInstanceId、WindowType、WindowId、local tick 和 Digest。ActionRuntime 继续记录输出；CharacterNetworkSendStage 继续只收集 SyncFacts；BehaviorNetworkPolicyResolver 继续通过 ActionInstance -> ActionProfile -> WindowType 解析策略。

Declaration projection 不保存完整网络 policy，也不直接选择 SyncDomain 或 packet。缺失 ActionProfile window policy 按现有正式错误/过滤语义处理，不创建默认 policy。

业务取舍：作者路径统一不会迫使网络层改成同步 Blackboard。代价是 window variable 与 ActionProfile 的 WindowType 必须一致，Validator 和 Debug 必须把断裂关系明确报告。

### 10. Corin 迁移必须原子替换

Corin 当前六个 ActionWindowClip：

- Attack1Hit
- Attack1Cancel
- Attack2Hit
- Attack2Cancel
- DodgeForwardIFrame
- DodgeBackIFrame

必须转换为六个 Decision TreeClip 和对应 Bool Frame/Frame declaration。Hit、Cancel、IFrame declaration 使用 ActionWindow projection；现有 `CanDodgeMoveCancel` 保持 None projection。Attack Cancel 的 Transition 与 OnExit 条件改读同一个 Blackboard declaration。

迁移后删除四个 Timeline 中全部 ActionWindowTrack/Clip managed-reference 数据、四个 ActionWindowActiveInfoNode 数据和 ActionWindowTrack 对应 editor/runtime 类型。ActionProfile 中 Hit/Cancel/IFrame policy 保留，因为它们属于下游策略，不是 Window 作者真相。

业务取舍：迁移后资产只剩一个时间区间模型，Runtime Debug 仍能看到同样的 Window facts。代价是 Timeline managed-reference 资产改写范围较大，必须通过正式 TreeClip authoring service 和 Agent validator 执行，不能手工伪造不安全 inline Graph YAML。

## Alternatives Considered

### 方案 A：保留 ActionWindowTrack，只把本地 gate 统一到 TreeClip

优点是现有攻击和 IFrame 资产无需迁移。缺点是同一种 Timeline 区间仍有两个 Track、两个 reader 和两套运行时缓存，正是本 change 要删除的分裂，因此不采用。

### 方案 B：根据 Blackboard key 或 category 自动生成 Window fact

优点是作者配置最少。缺点是命名成为隐藏协议，重命名可能改变网络事实，并违反 Blackboard key 不自动成为 SyncFact 的现行边界，因此不采用。

### 方案 C：在 TreeClip 中继续使用 SubmitActionWindowSampleNode

优点是可以复用现有 ActionWindowSample 提交代码。缺点是 local gate 使用 ExposedProperty、正式 window 使用 Submit 节点，TreeClip 内仍有两套输出作者语义；Commit 阶段还无法供同 Tick Transition 读取，因此不采用。

### 方案 D：保留 ActionWindowActiveInfoNode 作为 Blackboard reader 的快捷别名

优点是旧 ConditionRuleGraph 迁移较少。缺点是节点继续依赖专用 cache/查询合同并允许未来绕开 declaration reference，因此不采用。

## Risks And Mitigations

- **Projection 让 Blackboard 看起来自动联网**：只有显式 ActionWindow projection + SyncFact policy 才生成正式 fact；NetworkSendStage 仍只消费 SyncFacts，UI 明确区分 variable 与 fact projection。
- **多个 playback 写同一 declaration 丢失 Action Context**：candidate 按每次写入保存 provenance，并按 declaration + ActionInstance + tick 去重，不从最终单一 Bool value 反推来源。
- **无 Action Context 的普通 Timeline 误产动作事实**：Validator 报告缺失 authoring context，runtime 拒绝 candidate，不使用 ambient action fallback。
- **Decision purity 被事实输出破坏**：Tree 只做声明式 Blackboard 写入；fact projection 在 RootTree 后统一发生，Decision 节点不直接调用网络、ActionRuntime 或 SyncFacts。
- **ActionProfile policy 与 projection WindowType 不一致**：Agent validator 和 Runtime Debug 显示 projection identity、ActionProfile 解析结果及缺失原因，不注入默认 policy。
- **managed-reference 迁移损坏 Timeline**：迁移必须调用现有 TreeClipAuthoringService；若当前序列化无法安全创建全部 inline TreeClip，apply 必须停止说明缺口，不创建一次性 asset。
- **active changes 归档覆盖新口径**：固定依赖与 archive 顺序，实施和归档前检查相关 requirement 的最终合并结果。

## Migration

1. 固定四个依赖 change 的最终实现与 spec delta 作为基线。
2. 扩展 Blackboard declaration 的显式 ActionWindow projection 与校验。
3. 扩展 Blackboard write provenance 和本帧 projection candidate 收集。
4. 建立 RootTree 后的统一 WindowFactProjection，并接回现有 ActionWindowSample/SyncFacts/Debug。
5. 让 Decision TreeClip 写入绑定变量时提供 Timeline playback Action Context provenance。
6. 在 Graph Data Catalog 接入 projection 编辑、只读继承与错误展示。
7. 使用正式 TreeClip authoring service 原子迁移 Corin 六个 ActionWindowClip。
8. 将 Attack Cancel Transition/OnExit 条件迁移到 Blackboard reader。
9. 删除 ActionWindow Track/Clip、专用 cache、reader、submit node、Inspector 和 Agent 分支。
10. 更新 project context、snapshot/export、validator 和所有冲突 spec。

## Open Questions

无阻塞业务决策。`ActionWindowSample` 继续保持当前每 active Tick 一个 sample 的输出合同；若未来需要 Begin/End/Interrupted 区间事件，应另立 change，不在本次统一中同时扩张生命周期模型。

