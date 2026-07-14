## Context

当前 BTSMTL authoring 数据、运行工作副本和编辑器窗口已经分离：

```text
BaseTreeAsset / inline Graph / TimelineData
  -> Clone
  -> RunnableTree / StateMachineGraphRuntime / TimelinePlaybackScheduler
  -> CharacterPipeline outputs
```

这保证多个角色和多次状态激活不会污染作者资产，但也使旧 GraphView 直接读取 `BaseNode.State` 的调试方式失效。未来若 Graph 被编译为紧凑 IR，runtime 甚至不再存在 `BaseNode` 对象，因此 editor/runtime object binding 不能作为正式调试边界。

本设计只建立 diagnostics 与 source mapping 基础，不预设未来 compiler 的具体 IR。当前解释执行器和未来编译执行器都必须把执行语义投影为同一套 Trace。

## Goals / Non-Goals

### Goals

- Graph、StateMachine、ConditionRuleGraph、Timeline、TreeClip、Blackboard 和动画表现能在 Play Mode 下按真实 runtime instance 观察。
- Graph 与 Timeline 保持两个独立窗口，并共享同一个目标角色和 runtime Debug Session。
- 调试 UI 只依赖稳定 source identity、source map 和 Trace，不依赖 runtime Node/Graph/Track/Clip 对象布局。
- 多角色、多 State activation、多 Timeline playback、多 TreeClip cycle 能显式区分。
- 当前解释执行与未来编译执行复用同一 diagnostics contract。
- 关闭 diagnostics 时不改变 gameplay、动画、网络和生命周期结果。

### Non-Goals

- 本 change 不实现 runtime compiler。
- 本 change 不实现远程 Trace Server 或长期录制文件。
- 本 change 不把 Authoring Preview 扩展成完整 gameplay simulator。
- 本 change 不允许 runtime debug 修改变量、状态或作者资产。

## Decisions

### 1. 编辑器观察 source，不绑定 runtime clone

`BaseTreeWindow` 和 `TimelineEditorWindow` 继续持有 authoring data。Runtime 事件携带 source identity，Editor 依据 Debug Source Map 叠加状态。

原因：

- runtime clone 与 authoring asset 生命周期不同，直接绑定会混淆保存、Undo 和实例选择；
- 编译后不再保证存在 Node 对象；
- 同一 source 可能同时存在多个 runtime instance，单一 clone reference 无法表达选择。

代价：必须补齐稳定 identity、source map 和 view model，第一阶段工作量高于恢复旧高亮。

### 2. Graph 使用统一 `GraphAuthoringId`

重构前，`BlackboardOwnerId` 作为持久 Graph owner identity 被 Blackboard declaration 引用。本 change 将该身份正式提升并重命名为 `GraphAuthoringId`，Blackboard owner 继续引用同一值。

不新增 `DebugGraphId`，不同时保留 `BlackboardOwnerId` 与 `GraphAuthoringId`。现有资产一次性迁移后删除旧字段和命名。

原因：Graph identity 是 Graph、Blackboard、compiler source map 和 diagnostics 的共同基础，不应继续属于单一 Blackboard 模块。

### 3. Timeline identity 不使用列表下标

`TimelineData`、Track、Clip 分别持有稳定 authoring identity。Track/Clip 重排保持 identity；复制 authoring element 时生成新 identity；runtime clone 保留 identity。

当前 `TrackIndex/ClipIndex` 只允许作为 runtime 数组位置或 debug 辅助字段，不能作为 source identity。TreeClip runtime identity 由 Timeline playback instance、Track authoring id、Clip authoring id 和 cycle 组成。

### 4. Source identity 与 runtime instance identity 分离

Source identity 回答“作者资产里的哪一个元素”；runtime instance identity 回答“这次谁在执行”。

```text
Source:
  ProgramRevision + GraphAuthoringId + ElementAuthoringId

Runtime:
  CharacterRuntimeId + GraphRuntimeInstanceId
  + StateActivationGeneration / TimelinePlaybackId / TreeClipCycle
```

同一 source 可对应多个 runtime instance。UI 必须显式选择实例，不能默认取第一个。

### 5. Trace 使用事件流，Session 重建快照

Runtime 发布不可变结构化事件；`RuntimeDebugSession` 按 session、tick domain、tick/frame 和 sequence 排序，重建当前状态与有界历史。

事件流用于保留 Enter -> Running -> Exit、edge evaluated、transition waiting/commit 等过程。只轮询最终 Node 状态无法解释同一帧 catch-up tick 和短暂窗口。

Trace domain 至少区分：

- Logic：Graph、State、Condition、Timeline logic、TreeClip、Blackboard；
- Presentation：visual Timeline time、animation contribution、ordered handoff records、causal components、LayerPlan 与 playback lifecycle；
- Lifecycle：runtime instance create/destroy、target attach/detach、pipeline deactivate/dispose。

### 6. Channel 控制采集成本

第一阶段提供 `Graph`、`StateMachine`、`Timeline`、`Blackboard`、`Animation`、`Motion` 六类 channel。Graph/StateMachine 是节点高亮基础；其它 channel 可按 Session 开关。

Runtime 不默认序列化任意 managed object。Blackboard 和端口值使用受限 debug value snapshot，只允许已支持的基础类型和稳定 identity；未知类型显示类型名与不可展开状态，不调用任意 `ToString()` 作为逻辑合同。

### 7. Source revision 必须精确匹配

Debug Source Map 和每条 Trace Session 携带 `ProgramId`、`CompilationRevision` 和 `SourceContentHash`。当前解释执行准备阶段生成等价 revision；未来 compiler 在产物中生成。

Editor 当前 source 与 Trace revision 不一致时停止 overlay。不得按名称、当前 index、近似 path 或旧 GUID fallback。

### 8. Authoring Preview 与 Live Debug 是两个模式

`TimelinePreviewSession` 继续负责离线 authoring preview，只采样作者当前编辑内容并驱动显式 preview target。

Live Debug 由 `RuntimeDebugSession` 驱动，观察真实 `TimelinePlaybackScheduler`，不设置 preview target，不调用 preview evaluator，不修改 runtime time。

Timeline 窗口使用显式模式控制器切换二者；进入 Live Debug 时编辑区只读。Graph 窗口同样以只读 overlay 显示 runtime 状态。

### 9. 本地有界 Buffer，不先造 Trace Server

每个 Character runtime 拥有有界 in-process Trace Buffer。Editor Session 可实时跟随、暂停观察和在 buffer 范围内 scrub。

第一阶段不实现独立进程、网络 transport 或文件存储。Trace contract 不依赖 in-process transport，后续可以增加 transport/store，而不改变 producer 和 view。

### 10. Pipeline Inspector 复用统一 view model

`CharacterPipelineHostEditor` 不再直接遍历 runtime service 的私有 debug list。它选择/附着 `RuntimeDebugSession`，并使用与 Graph/Timeline 相同的 snapshot view model。

Layer 调试按以下层次展示：

```text
Registry contribution
  -> ordered handoff record
  -> causal component disposition
  -> Layer priority allocation
  -> LayerPlan
  -> final playback output
```

这避免 Inspector、Graph 和 Timeline 各自解释一次 runtime 状态。

## Module Boundaries

### Runtime-safe contracts

建议位于独立 runtime assembly：

- source/runtime identity；
- Trace event、category、domain 和 debug value；
- Debug Source Map 只读合同；
- Trace sink 和有界 buffer；
- 不引用 UnityEditor、GraphView 或 EditorWindow。

### Runtime producers

- TreeDesigner：Graph/Node/Edge/StateMachine/Condition lifecycle；
- Timeline：playback、time、Track/Clip、TreeClip lifecycle；
- CharacterPipeline：Blackboard、Motion、Animation、Presentation 和 target lifecycle。

Producer 只发布事件，不维护 editor selection、颜色、breadcrumb 或 UI 文本。

### Editor analysis

- Target registry；
- Debug Session 与 instance selection；
- Trace analyzer/snapshot builder；
- source revision resolver；
- Graph、Timeline、Host Inspector view model。

### Editor views

- `BaseTreeWindow` runtime overlay；
- `TimelineEditorWindow` Live Debug mode；
- `CharacterPipelineHostEditor` session summary；
- views 不直接持有 runtime Graph/Node/Timeline 对象。

## Runtime Event Ordering

事件至少携带：

```text
SessionId
ProgramRevision
Domain
LocalLogicTick or PresentationFrame
Sequence
RuntimeInstanceKey
SourceElementHandle
EventKind
Payload
```

同一 logic tick 使用单调 sequence 保序；catch-up tick 不合并。Presentation event 使用 render frame 与自身 sequence，不伪装成 logic fact。

## Migration

1. 为 Graph、Timeline、Track 和 Clip 生成/迁移稳定 authoring identity。
2. 将 Blackboard declaration owner reference 迁移到 `GraphAuthoringId`。
3. 升级 Agent Snapshot schema v4 和所有 emitter/resolver。
4. 接入 Trace contract 与 runtime producers。
5. 接入 Debug Session，再替换 Graph/Timeline/Inspector 旧调试读取。
6. 删除 `BaseNodeView` 直接读取 runtime Node state 的旧路径及 Host Inspector 平行读取逻辑。

迁移不保留旧字段、旧 schema、按 index source mapping 或 runtime clone editor binding。

## Risks / Trade-offs

- 事件过多会影响 Editor Play Mode 性能：通过 channel、有界 buffer、结构化值范围和无 UI 反向调用控制。
- authoring identity 迁移错误会导致 source map 断裂：Validator 必须在运行前拒绝缺失或重复 identity。
- 多实例选择增加 UI 心智：Session 默认跟随当前目标的最近活跃实例，但选择结果必须显式显示；不能静默猜测同 source 的唯一实例。
- source revision 严格匹配会在运行中编辑资产后中断 overlay：这是可信调试的必要代价，不提供模糊匹配。
- 第一阶段没有持久 trace 文件，只能回看 buffer 范围：先满足本地动作调试，远程与长录制作为后续独立能力。

## Resolved Questions

- 有界历史由每个 `RuntimeTraceBuffer` 持有，当前正式容量为 512 个完整 segment；容量不进入 gameplay authoring 数据，本 change 不增加 Editor Preferences 或 Session 临时容量配置。
- Timeline playback 选择同时提供 Follow Graph 与显式 Pin；Follow 选择最近活跃实例，Pin 保持用户选定实例。
