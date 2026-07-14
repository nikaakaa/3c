# Change: 以 TreeClip 与 Scope Variable 统一 Timeline Window 作者路径

## Why

当前角色管线对同一种“Timeline 某个时间区间内条件成立”语义保留了两套正式作者与运行路径：

- `ActionWindowTrack / ActionWindowClip` 由 `TimelinePlaybackScheduler` 专门预采样到 `m_TimelineDecisionWindows`，ConditionRuleGraph 再通过 `ActionWindowActiveInfoNode` 读取；
- Decision `TreeClip` 在相同的 Prepare 阶段写入 Frame scope Pipeline Blackboard，ConditionRuleGraph 通过通用 Blackboard ValueNode 读取。

两条路径拥有不同的 Track、Clip、临时存储、reader、Inspector、Agent authoring 和资产配置，但作者表达的都是“在这段 Timeline 时间内发布一个当前 Tick 有效的值”。此外，`SubmitActionWindowSampleNode` 又允许 Graph/Commit Tree 直接提交同一个 `ActionWindowSample`，形成第三个动作窗口生产入口。Corin 当前已经同时使用 Attack Cancel 的 ActionWindow 路径、Dodge IFrame 的 ActionWindow 路径和 DodgeMoveCancel 的 TreeClip Blackboard 路径，证明这种分层并没有减少作者心智，反而把时间区间语义按下游用途拆成了多套数据源。

本变更删除 ActionWindow 专用作者模型，统一由 TreeClip 表达时间范围、inline TimelineRunningTree 表达区间逻辑、具有 owner 的 scope variable 表达区间输出。需要 ActionInstance、策略解析、SyncFacts 和 Runtime Debug 身份的变量通过 declaration 上的显式 fact projection 配置转换为正式 `ActionWindowSample`；本地状态门不配置 projection，因此不会产生动作事实。

## What Changes

- 将 Decision `TreeClip + Bool Frame/Frame Pipeline Blackboard variable` 定义为 Timeline 时间窗口的唯一正式作者模型。
- 在现有 `BaseExposedProperty` / Pipeline Blackboard declaration 上增加可选的显式 fact projection 描述；本 change 只增加 `ActionWindow` projection kind，并保存稳定 WindowType、WindowId 和 Digest，不引入第二个 profile、registry 或 asset。
- 只有显式配置 `ActionWindow` projection 且 SyncPolicy 为 `SyncFact` 的 true 写入，才能在当前 Tick 投影为 `ActionWindowSample`；普通 Blackboard key 或 local gate 不会自动产生事实。
- 为 Blackboard 写入记录正式 source provenance，包括 declaration、Graph/runtime owner、logic tick、TreeClip/playback identity 和显式 Action Context；缺失 Action Context 时拒绝 action-scoped projection，不读取 ambient current action。
- 在 RootTree/StateMachine 完成本 Tick 决策后、SyncFacts 收集前统一投影本 Tick 的 window candidates；Transition 在此前只读取同一个 Blackboard variable。
- 删除 `ActionWindowTrack`、`ActionWindowClip`、`ActionWindowClipInspectorView` 和 Scheduler 的专用 ActionWindow 采样、prepared window、dedupe 与 decision cache。
- 删除 `CharacterGraphContext.BeginTimelineDecisionFacts/AddTimelineDecisionWindow/IsCurrentTickActionWindowActive`、`ActionWindowActiveInfoNode` 和对应 Agent emitter/compiler 路径。
- 删除 `SubmitActionWindowSampleNode` 作为并行动作窗口生产入口；非 Timeline 动作同样通过有显式 Action Context provenance 的 scope variable 与相同 projection 产出窗口事实。
- 保留 `ActionWindowSample`、`SyncFacts.Action.WindowSamples`、ActionProfile window policy、BehaviorNetworkPolicyResolver 和网络 adapter 合同；它们是下游事实与策略边界，不再是 Timeline 作者入口。
- 将 Graph Data Catalog 作为 projection 配置的唯一编辑入口：本地 Blackboard declaration 详情可选择 None/ActionWindow，并在合法组合下编辑 WindowType、WindowId、Digest。
- 原子迁移 Corin Attack1/Attack2 Hit/Cancel、DodgeForward/DodgeBack IFrame 为 Decision TreeClip + scope variable，复用现有 TreeClip authoring service，不创建一次性 Tree asset。
- 将 Attack Cancel、Dodge Move Cancel 和后续 window 条件统一改为通用 Pipeline Blackboard ValueNode；删除 Corin 所有 ActionWindowTrack/Clip 和 ActionWindow reader 数据。

## Impact

- Affected specs:
  - `btsmtl-runnable-timeline-node`
  - `btsmtl-graph-data-catalog-authoring`
  - `character-pipeline-blackboard`
  - `character-action-authoring-closure`
  - `character-action-activation-flow`
  - `character-state-interruption-authoring`
  - `character-state-timeline-authoring-loop`
  - `character-gameplay-pipeline-closure`
  - `character-pipeline-runtime`
- Retained downstream contracts:
  - `ActionWindowSample` 与 `SyncFacts.Action.WindowSamples`
  - ActionProfile window policy 与 Transaction BehaviorId 解析
  - ActionSyncDomain、history、digest、packet mapping 和 Runtime Debug
- Affected runtime areas:
  - Timeline TreeClip Decision 调度与写入上下文
  - Pipeline Blackboard declaration、value provenance 与 frame cleanup
  - CharacterGraphContext 与 CharacterBTSMTLPhase 的 fact projection barrier
  - TimelinePlaybackScheduler ActionWindow 专用采样删除
- Affected editor areas:
  - Graph Data Catalog Blackboard declaration 详情
  - TreeClip Inspector、下钻与 Decision output 摘要
  - ActionWindow Clip Inspector 删除
  - Agent snapshot、validator、patch compiler 与节点 emitter
- Affected Corin assets:
  - `CorinAttack1Timeline.asset`
  - `CorinAttack2Timeline.asset`
  - `CorinDodgeForwardTimeline.asset`
  - `CorinDodgeBackTimeline.asset`
  - `CorinPlayableRootTree.asset`
  - ActionProfile 只保留策略，不再对应 ActionWindowClip 作者数据
- Breaking authoring change:
  - Timeline 不再提供 ActionWindowTrack/Clip。
  - ConditionRuleGraph 不再提供 ActionWindowActiveInfoNode。
  - Graph 不再提供 SubmitActionWindowSampleNode。
  - 旧 ActionWindow 资产必须在同一迁移中全部转换，运行时不保留反序列化兼容或 fallback。

## Dependencies And Conflicts

- 本 change 依赖 `refactor-pipeline-blackboard-owned-scopes` 的最终 declaration owner、scope/lifetime、结构化 address 和显式 reference 实现。
- 本 change 依赖 `restore-timeline-treeclip-pipeline-runtime` 的 Decision/Commit Scheduler、inline TimelineRunningTree、Clip runtime context 和 TreeClip authoring service；但会明确推翻该 change 中“本地 gate 使用 TreeClip、Action window 继续使用 ActionWindowTrack”的临时分层结论。
- 本 change 依赖 `unify-graph-data-catalog-authoring` 的唯一 Graph Data Catalog；projection 编辑必须接入该目录，不恢复旧 ExposedProperty panel 或新增 Window 配置面板。
- 现行 `btsmtl-runnable-timeline-node` 要求第一阶段必须保留 `ActionWindowTrack`，与本 change 直接冲突，本 change 通过 MODIFIED delta 删除该要求。
- 现行 `character-action-authoring-closure` 要求 Timeline window Inspector 和 Corin Attack1 ActionWindowTrack，必须移除并替换为 TreeClip + scope variable。
- 现行 `character-state-interruption-authoring` 与 active TreeClip delta 同时允许 ActionWindow reader 和 Blackboard reader，必须收口为 Blackboard reader。
- 现行 `character-pipeline-blackboard` 与 `character-network-sync-domain-contract` 禁止 Blackboard key 自动成为网络事实。本 change 保持该边界：只有 declaration 上显式、类型校验通过的 fact projection 才产生 `ActionWindowSample`，NetworkSendStage 仍只消费 SyncFacts。
- 若按 archive 顺序处理，必须先归档三个依赖 change 和 `unify-graph-data-catalog-authoring`，再归档本 change，避免旧 requirement 在后归档时重新覆盖统一口径。

