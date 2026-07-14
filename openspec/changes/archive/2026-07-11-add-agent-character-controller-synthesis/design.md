# Design: Agent 生成角色动作控制器 authoring 编译链路

## 设计目标

这条链路服务两个业务目标：

- 短期：让 Codex/LLM 能稳定生成 2v2vE demo 需要的角色动作控制器，而不是靠人手动从零拼 graph。
- 长期：让通用编辑器拥有 Agent-native authoring 能力，人负责描述和微调，Agent 负责生成、修复和解释结构。

设计上要把“不稳定的自然语言生成”和“严格的 BTSMTL asset 结构”隔开。LLM 可以犯错，但错误必须停在 schema、macro 或 compiler report 中，不能污染正式 graph。

## 总体链路

```text
CharacterPipelineDefinition / BTSMTL Asset
-> AgentGraphSnapshotExporter
-> Agent Snapshot JSON
-> LLM 生成 Intent 或 Patch IR
-> AgentMacroLibrary
-> Agent Patch IR
-> AgentPatchCompiler
-> BTSMTL 正式 authoring API
-> AgentGraphValidator
-> AgentCompileReport
-> LLM 修复或作者微调
```

## 模块划分

### `AgentGraphSnapshotExporter`

输入：

- `CharacterPipelineDefinition`
- Root `BaseTreeAsset`
- RootTree 下钻的 `StateMachineGraph`
- `StateNode` 状态行为图
- Transition edge 的 `TransitionRuleGraph`
- `CharacterInputProfile`
- `ActionProfile` 列表
- Timeline asset 引用摘要

输出：

- 面向 Agent 的只读 JSON snapshot。
- snapshot 只描述当前可编辑事实和可引用资产，不成为正式配置。

处理重点：

- 导出 graph kind、节点类型、节点显示名、stable authoring id、flow 边、property 边、inline/shared ownership。
- 导出可用输入 request/value、ActionProfile action id、Timeline asset identity。
- 不导出 Unity YAML、私有字段布局或运行时临时状态。

### `AgentControllerIntent`

输入：

- 人类自然语言或 Agent 分析后的业务描述。

输出：

- 受限业务意图，例如：

```json
{
  "target": "ActionStateMachine",
  "macro": "two_hit_combo",
  "request": "Attack",
  "steps": [
    { "state": "Attack1", "actionProfile": "Attack.Light.01", "timeline": "Attack1" },
    { "state": "Attack2", "actionProfile": "Attack.Light.02", "timeline": "Attack2" }
  ],
  "cancel": [{ "request": "Dodge", "reason": "DodgeCancel" }]
}
```

业务取舍：

- 选择 Intent 层可以让人和 Agent 用角色动作语言沟通，减少心智负担。
- 不直接让作者维护 Patch IR，因为 Patch IR 更接近 compiler 指令，不适合长期设计讨论。

### `AgentMacroLibrary`

第一阶段宏：

- `locomotion_state_machine`
- `single_timeline_action`
- `two_hit_combo`
- `dodge_cancel`
- `hit_reaction`

职责：

- 将业务意图展开为 Patch IR。
- 统一生成状态、状态行为、transition、rule graph、TimelineNode、ActionActivation、ActionLifecycleTransition 等结构。
- 维护宏版本，保证同一个意图在同一版本下可重复生成。

业务取舍：

- 宏优先牺牲一部分自由度，换来 demo 阶段的稳定生成和可评估。
- 自由节点生成可以后置，因为当前最大风险是结构不合法，而不是表达力不足。

### `AgentPatchIR`

Patch IR 是 Agent 和 compiler 之间的机器指令层，典型操作包括：

- `ensure_state_machine`
- `ensure_state`
- `ensure_transition`
- `ensure_transition_rule`
- `ensure_state_behavior_node`
- `ensure_timeline_node`
- `ensure_action_activation`
- `ensure_action_lifecycle_transition`
- `bind_asset_reference`
- `link_flow`
- `link_property`

约束：

- Patch 必须引用 stable authoring id 或 snapshot 中存在的资产身份。
- Patch 不能写 Unity YAML、不能写 `m_Nodes`、`m_Edges` 等内部集合。
- Patch 不能携带 runtime-only 数据。
- Patch 应用必须可生成结构 diff 和 compile report。

业务取舍：

- Patch IR 比直接 JSON graph 更啰嗦，但能支持增量生成和人类微调后的二次修改。
- Patch IR 作为中间层可以让 Codex、外部模型或后续 UI 都复用同一 compiler。

### `AgentPatchCompiler`

职责：

- 解析 Patch IR。
- 通过 `AssetResolver` 解析 ActionProfile、Timeline、InputProfile 条目和目标 RootTree。
- 通过 `NodeEmitterRegistry` 创建并配置正式节点。
- 所有结构变更必须调用：
  - `BaseGraph.CreateNode(Type)`
  - `BaseGraph.Link(...)`
  - `BaseGraph.LinkProperty(...)`
  - 节点/模块正式配置方法或受控 emitter
- 不允许 compiler 自己维护第二套 graph 数据。

业务取舍：

- 选择 compiler 写 BTSMTL，而不是 LLM 写 BTSMTL，可以把小众语料问题转化为确定性工程问题。
- compiler 越硬，LLM 输出越容易自动修复；但第一版必须控制节点范围，避免把所有节点都纳入。

### `AgentGraphValidator`

职责：

- 在 apply 前检查 Patch IR 的 schema、引用和宏约束。
- 在 apply 后检查生成 graph 的 BTSMTL 语义。
- 输出机器可读 report，供 LLM 下一轮修复。

第一阶段必查规则：

- `StateMachineGraph` 只能包含 Enter、AnyState、Exit、StateNode。
- `TimelineNode` 只能位于状态行为图或普通行为图，不能位于 `StateMachineGraph` 或 `TransitionRuleGraph`。
- `TransitionRuleGraph` 只能包含纯 value/条件节点和唯一结果节点。
- Action 宏必须引用存在且合法的 `ActionProfile`。
- Timeline 动作必须引用存在的 Timeline asset。
- `Action Context` 必须从 action activation 传到 Timeline 或 lifecycle 节点。
- request 查询不能在 `TransitionRuleGraph` 中消费输入。
- inline/shared graph ownership 不能同时作为真数据存在。
- AnyState transition 必须有非默认条件。

业务取舍：

- Validator 会让部分生成失败更早暴露，但这比生成一个看似能打开、运行时才错的 graph 更适合 demo 迭代。
- Validator 报错必须面向 Agent 修复，不只面向人类阅读。

### `AgentCompileReport`

Report 至少包含：

- schema 错误。
- 引用解析错误。
- graph 语义错误。
- 已应用或计划应用的操作摘要。
- 生成/修改的 state、transition、Timeline、ActionProfile 引用。
- 建议修复指令。
- 评估指标。

Report 是 Agent 自修复循环的接口。业务上它降低人类介入次数，让“AI 生成 -> 编译失败 -> AI 修复”成为可重复流程。

## 评估设计

第一阶段评估不评“像不像某个标准答案”，而评估“是否稳定生成可用动作控制器”。

指标：

- `schema_valid_rate`：LLM 输出是否符合 Intent/Patch schema。
- `compile_success_rate`：Patch 是否能通过 compiler 应用。
- `semantic_valid_rate`：生成 graph 是否满足 BTSMTL/CharacterPipeline 规则。
- `repair_iterations`：从首次输出到合法 graph 的修复轮数。
- `asset_resolution_rate`：ActionProfile、Timeline、Input request 引用解析成功率。
- `business_coverage`：目标需求中的状态、transition、action、timeline、cancel、hit reaction 是否被覆盖。
- `diff_size`：每次生成修改的节点/边数量，帮助发现过度生成。

评估样例集：

- Idle/Walk/Run locomotion。
- 单段轻攻击。
- 二连击。
- 攻击中闪避取消。
- 受击打断攻击。

业务取舍：

- 选择结构和语义指标，能在不运行完整 Unity 场景的情况下快速评估 Agent 生成稳定性。
- 运行时手感仍由用户端到端验证负责，不写入 OpenSpec task。

## 路径建议

泛用 BTSMTL 部分：

- `Assets/GameScripts/Main/Runtime/BTSMTL/TreeDesigner/Editor/Scripts/AgentAuthoring`

角色动作语义部分：

- `Assets/GameScripts/Main/Runtime/Character/Pipeline/Editor/AgentAuthoring`

业务取舍：

- 分开后，BTSMTL compiler 可以服务未来通用编辑器；Character macro 不污染基础图层。
- 如果全部放进 Character，会短期更快，但后续通用编辑器复用成本高。
- 如果全部放进 BTSMTL，会让基础图层知道 ActionProfile、Timeline 角色语义，边界变脏。

## 与现有 spec 的关系

- 依赖 `btsmtl-graph-core` 的唯一图数据和正式编辑入口。
- 依赖 `btsmtl-sm-node-authoring` 的状态机层级规则。
- 依赖 `btsmtl-runnable-timeline-node` 的 Timeline 播放请求语义。
- 依赖 `character-input-node-authoring` 的输入 request/value 节点。
- 依赖 `character-action-authoring-closure` 的 ActionProfile 和 Action Context 口径。
- 依赖 `character-state-timeline-authoring-loop` 的 Action StateMachine + Timeline authoring 模式。

当前发现的文档口径差异：

- `openspec/project.md` 写 `add-pipeline-blackboard-authoring` 未完成，但 `openspec list` 显示该 change 已 Complete。本变更不依赖该差异；后续整理项目文档时应同步修正。

## 风险和缓解

- 风险：LLM 直接生成 Patch IR 仍可能过大或错误。
  缓解：第一阶段优先生成 Intent，再由宏展开 Patch。

- 风险：节点私有字段没有正式 setter，compiler 可能需要反射。
  缓解：优先为纳入 Agent 生成范围的节点补正式 authoring 方法；反射只能封装在明确 emitter 内，不能扩散到业务代码。

- 风险：自动应用失败后污染 graph。
  缓解：第一阶段先实现 dry-run 和 validator，apply 前必须有可报告的计划；后续再做事务回滚。

- 风险：评估指标只看结构，不代表手感。
  缓解：结构评估只负责 AI 生成稳定性；手感、窗口时间和动画表现仍由 Timeline 预览和用户端到端调试闭环负责。

