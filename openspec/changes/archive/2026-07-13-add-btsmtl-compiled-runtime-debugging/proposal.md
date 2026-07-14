# Change: 增加 BTSMTL 编译无关运行时调试

## Why

提案基线中，`CharacterPipeline` 从 authoring Graph 创建隔离运行工作副本，`BaseTreeWindow` 和 `TimelineEditorWindow` 仍绑定作者数据。旧节点高亮依赖窗口直接读取运行 Node 对象，已经无法观察 `CharacterPipeline` 内部 clone，也无法承受后续把 Graph/Timeline 编译成数组、指令流或其它 runtime IR。

提案基线中，`CharacterPipelineHostEditor` 能直接读取 Action、Blackboard、Motion、Animation Registry 和部分动画交接即时数值，但没有统一目标会话、实例选择、稳定源码映射、逐帧刷新和历史回看。Timeline 编辑器的 `TimelinePreviewSession` 只表达 authoring preview，进入 Play Mode 后不会观察真实 `TimelinePlaybackScheduler`。这些入口不是同一条运行时调试链路。

需要建立类似 StateTree Debugger 的正式边界：authoring 元素拥有稳定源码身份，执行后端只发布结构化 Trace，Debug Session 依据严格匹配的 Source Map 将 Trace 投影到 Graph、Timeline 和 Pipeline 调试 UI。当前解释执行与未来编译执行必须使用同一合同，不能恢复 runtime clone 直绑或按名称/下标猜测作者元素。

## What Changes

- 将 Graph 的持久 owner identity 收敛为通用 `GraphAuthoringId`，Pipeline Blackboard declaration owner 同步引用该身份，不保留第二个调试专用 Graph ID。
- 为 `TimelineData`、Track 和 Clip 增加持久稳定 authoring identity；重排保持 identity，复制产生新 identity，runtime clone 保留 source identity。
- 将 Agent Snapshot 合同升级到 schema v4，显式输出 Graph、Node、Edge、Timeline、Track 和 Clip authoring identity，并要求 Patch 编译与资产生成维护这些身份。
- 新增纯运行时 diagnostics contract，表达 source element、program/source revision、runtime session、runtime instance、logic/presentation domain、tick、sequence、category 和结构化事件 payload。
- 新增 Debug Source Map，将紧凑 execution handle 映射到 authoring element；当前解释执行准备阶段与未来编译器产物必须生成同一种 map。
- 让 Graph、StateMachine、ConditionRuleGraph、Timeline scheduler、Pipeline Blackboard、Animation Registry、ordered handoff records、causal components、LayerPlan 与 playback lifecycle 在正式生命周期边界发布 Trace；关闭 diagnostics 时不得改变执行结果。
- 新增每个 Character runtime 独立的有界 Trace Buffer，以及 editor-only `RuntimeDebugSession`；Session 显式选择目标角色、runtime instance 和 channel，并从事件重建当前快照与短期历史。
- `BaseTreeWindow` 保持绑定 authoring Graph，只通过 source identity 叠加当前 runtime instance 的只读节点、边、状态机与生命周期状态；删除读取 authoring node `State` 的旧运行高亮路径。
- `TimelineEditorWindow` 明确区分 Authoring Preview 和 Live Debug。Live Debug 读取真实 Timeline playback 的 logic time、visual time、cycle、active Track/Clip、TreeClip 和 animation contribution，不调用 `TimelinePreviewSession`。
- `CharacterPipelineHostEditor` 改为消费统一 Debug Session view model，持续刷新并展示各 layer 的输入 contribution、causal disposition、最终 LayerPlan、playback output 和 identity，不再直接拼装一套平行调试读取链。
- Source revision、Debug Source Map 与运行 Trace 不一致时必须停止 overlay 并显示明确错误；不得按显示名、序号、旧路径或当前资产结构 fallback。
- 第一阶段只提供 Unity Editor/Development 本地 in-process Trace、实时观察、暂停和有界历史回看；不实现独立 Trace Server、远程 transport、持久 trace 文件、断点、单步执行或真正的 BTSMTL runtime compiler。

## Capabilities

### New Capabilities

- `btsmtl-runtime-diagnostics`: 定义稳定源码身份、Debug Source Map、结构化 Trace、runtime instance、channel、buffer、Debug Session 和只读 editor projection。

### Modified Capabilities

- `btsmtl-graph-core`: 增加统一 Graph authoring identity 和编译无关 Graph runtime overlay。
- `btsmtl-timeline-editor-preview`: 增加 Timeline/Track/Clip 稳定身份，并分离 Authoring Preview 与 Live Debug。
- `agent-character-controller-synthesis`: Snapshot schema v4 输出并维护稳定 authoring identity。
- `character-pipeline-runtime`: Character runtime 作为显式 diagnostics target 接入统一 Trace Session，不暴露 runtime Graph 对象给 editor。
- `character-presentation-interpolation`: 表现帧 Trace 必须暴露 logic/visual 时间、动画贡献、ordered handoff records、causal components、LayerPlan 和 playback 结果。

## Impact

- 影响 Graph、Timeline、Agent authoring、CharacterPipeline、StateMachine、Blackboard、动画表现和对应 Editor assembly。
- Graph owner identity 字段需要正式迁移，现有 Blackboard declaration owner reference 必须同步更新，旧字段和旧命名迁移后删除。
- Timeline 数据结构新增稳定 identity，现有 Corin inline Timeline 和 shared Timeline 必须一次性补齐并验证唯一性。
- Agent Snapshot schema 从 v3 破坏性升级到 v4，旧 schema 不保留兼容解析。
- Graph 与 Timeline 编辑器增加只读 Live Debug 模式，但不改变原有 authoring 数据 ownership、页面栈和双窗口关系。
- runtime diagnostics 是观察链路，不得写 StrictGameplay、Blackboard、SyncFacts、Motion、Action 或 Presentation 决策输入。

## Current Spec Comparison

- `btsmtl-graph-core` 要求 runtime 使用隔离工作副本且页面栈不参与 runtime；本 change 保持该约束，但明确编辑器通过 Trace overlay 观察 source，不绑定或修改工作副本。
- `btsmtl-timeline-editor-preview` 只规定 authoring preview；本 change 不替换该预览，而是增加语义独立的 Live Debug，避免 preview session 冒充真实 scheduler。
- `character-presentation-interpolation` 目前只以 SHOULD 要求调试可追踪性，且现有 Inspector 未展示完整 layer arbitration；本 change 将编译无关 Trace 和精确 identity 提升为 MUST。
- current `agent-character-controller-synthesis` 和 `openspec/project.md` 已统一声明 schema v4，且实现中已删除 v3 parser。本 change 继续在 v4 中补齐 Graph/Node/Edge/Timeline/Track/Clip 稳定 authoring identity 和 source revision，不引入新 schema 分支。
- 当前 specs 没有允许 editor 直接读取 runtime clone 的要求，因此删除旧 `BaseNodeView` direct-state 高亮不会与 current spec 冲突。

## Out of Scope

- BTSMTL runtime IR、bytecode、Burst executor 或代码生成器本身。
- 独立 Trace Server、跨进程远程调试和持久 trace 文件格式。
- Runtime Simulation 的 fake world、fake input 和完整非 Play Mode 角色执行沙箱。
- 断点、暂停 gameplay tick、单步执行和运行时变量改写。
- 新增自动化测试或把人工验证写入 tasks。
