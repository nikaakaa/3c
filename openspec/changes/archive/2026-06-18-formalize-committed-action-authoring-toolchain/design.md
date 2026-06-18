## Context
现在的运行时链路是：`CharacterConfigSO -> CharacterActionCatalogSO -> CharacterActionDefinitionSO -> DodgeCommittedActionBranchAuthoring -> CommittedActionBranchDefinition -> CommittedActionBranchEvaluator -> ActionTimelineEvaluator`。这条链路已经能让运行时从 SO 得到 Dodge selector 和两个 timeline，但作者侧不是通用节点树：Dodge branch 是代码固定结构，Timeline Editor 只暴露 Directional / Backstep 两个 timeline，Character Behavior Editor 仍然编辑 behavior source graph。

`refactor-action-timeline-time-authority` 已完成但未归档；实现本 proposal 时必须使用它定义的 seconds authoring、compile context 和 runtime local tick 口径，不能再以 frame 字段作为正式权威。

## Goals / Non-Goals
- Goals: 让设计者从 `CharacterActionDefinitionSO` 进入一个正式 Action branch 工具，能编辑 selector / condition / timeline 节点树，选择 TimelineNode 后编辑它的 track / clip / payload，并通过同一 compiler 生成运行时 branch。
- Goals: 将 Dodge 从专用固定 branch authoring 迁移为通用 branch authoring 的第一个 concrete instance。
- Goals: 保持 Source、Action、Claim、Slot、Channel、Presentation Layer 六层清楚分离。
- Non-Goals: 不实现通用 Skill Editor 宣称，不新增 Attack / Block 第二条金线，不引入新的 runtime runner、motion executor、animation presenter、blackboard writer 或角色控制入口。
- Non-Goals: 不让 Behavior Graph Editor 成为 Action branch 编辑器。

## Layer Mapping
- Source: 仍由 CommittedAction source 或批准等价 source 提交 action 输出；authoring 工具只配置该 source 消费的 action definition。
- Action: `Action.Dodge` 等动作语义位于 `CharacterActionDefinitionSO` 和 Action Catalog。
- Claim: branch authoring 保存默认 body claim，Dodge 继续是 FullBody claim 语义。
- Slot: `BaseSlot` / `UpperBodySlot` 仲裁合同不变；branch editor 不创建新 slot，也不使用 `FullBody` 作为 slot。
- Channel: TimelineNode 输出 Motion、Animation、Window、Cue 和 facts 这类 channel 数据。
- Presentation Layer: Timeline panel 和 preview 只是 Editor-only 表现；正式运行时仍由 motion executor、Animancer presenter 和 output applier 消费 frame output。

## Decisions
- Decision: 新增通用 `CommittedActionBranchAuthoring` 或批准等价数据模型，而不是继续扩展 `DodgeCommittedActionBranchAuthoring`。
  - Reason: Dodge 特例无法表达后续 action 的 selector/condition/timeline 组合，也让 editor 和 runtime 之间存在隐藏结构。
- Decision: Committed Action Branch Editor 以 `CharacterActionDefinitionSO` 为根对象。
  - Reason: Action Catalog 已经把 action definition 定为动作逻辑入口，branch authoring 应是 action definition 的子模块。
- Decision: Timeline Editor 变成 TimelineNode adapter，可嵌入 branch editor；独立窗口只作为快捷入口。
  - Reason: Timeline 数据属于选中的 timeline node，独立窗口不能拥有第二套 action branch selection 语义。
- Decision: Character Behavior Editor 只负责 behavior source topology。
  - Reason: Source graph 和 Action branch graph 处在不同语义层，混在一个 authoring asset 中会让 behavior compiler 编译 action payload，产生分裂路径。
- Decision: 迁移采取激进策略。
  - Reason: 当前用户要求迁移/重构不要保留废弃配置和 fallback；实现完成后 `Action.Dodge` 正式路径应只使用通用 branch authoring。

## Alternatives Considered
- 保留 Dodge 两个固定 tab 并继续扩展 Timeline Editor：拒绝，因为它仍不是节点树工具链，后续动作会继续堆特例。
- 把 Character Behavior Editor 扩展成一切图编辑入口：拒绝，因为它会混淆 behavior source graph 与 committed action branch graph。
- 保留 `DodgeCommittedActionBranchAuthoring` 作为 fallback：拒绝，因为项目规则要求迁移保持干净统一，不保留废弃配置。

## Risks / Trade-offs
- Risk: 资产迁移可能改变 Dodge selector 或 timeline payload。
  - Mitigation: 添加资产迁移/编译 EditMode 测试，断言迁移前后的 Directional / Backstep evaluator outcome 等价。
- Risk: Editor GraphView 与 Timeline panel 一次性做完范围偏大。
  - Mitigation: 第一版只支持最小 selector、condition、timeline 节点类型和已存在 condition kind；节点视觉可简洁，数据闭环和验证优先。
- Risk: active specs 尚未吸收时间权威变更。
  - Mitigation: 本 change 明确依赖 `refactor-action-timeline-time-authority`，实现时按该 change 的 seconds/tick delta 编写测试。

## Migration Plan
1. 引入通用 branch authoring 数据模型和 compiler，先支持 Selector、Condition、Timeline。
2. 为现有 Dodge asset 生成等价通用 branch authoring：selector -> directional condition -> directional timeline，selector -> backstep condition -> backstep timeline。
3. 将 `CharacterActionDefinitionSO.ToDefinition()` 切到通用 branch authoring。
4. 删除或降级 Dodge 专用 branch authoring 的正式 runtime 用途，不保留 fallback。
5. 让 Timeline Editor 通过 selected TimelineNode adapter 编辑同一份 serialized timeline 数据。
6. 通过测试证明 Dodge 行为、runtime boundary、editor writeback 和 preview evaluator 一致。

## Open Questions
- 独立 `Tools/3C/Committed Action Timeline Editor` 菜单是否保留为打开 Dodge branch editor 并自动选中默认 TimelineNode 的快捷入口，默认决策是保留快捷入口但不保留独立数据权威。
- 第一版 condition kind 默认只支持已有 `RequestVariantEquals` 和 `HasMoveIntent`，新增 condition kind 另走 proposal。
