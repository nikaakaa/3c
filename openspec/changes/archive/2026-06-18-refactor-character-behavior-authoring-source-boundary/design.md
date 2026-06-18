# Design: Character Behavior Authoring Source Boundary

## 问题
现在最危险的问题不是单个类写错，而是同一份语义在多个地方有影子：

- Behavior graph 表示 root / leaf 顺序。
- Action definition 表示 action catalog、Dodge selector 和 timeline。
- Timeline editor 正在编辑正式 action definition。
- Behavior authoring compiler 仍然能从 behavior asset 里编出 Dodge branch。

如果继续这样做，后续会出现三个后果：

1. 编辑器里改了 timeline，但 runtime 读的是另一份 branch。
2. Graph Editor 看起来能编辑 Dodge，但实际不影响正式 action definition。
3. 测试只证明 sample asset 可编译，不能证明正式角色资产可运行。

## 六层边界
本变更按项目现有六层术语收边界：

- Source：Behavior graph 负责，例如 LocomotionSource、CommittedActionSource。
- Action：Action definition 负责，例如 `Action.Dodge`。
- Claim：Action definition / branch outcome 负责，例如 FullBody claim。
- Slot：BodyArbiter / CharacterFramePlan 负责，例如 BaseSlot、UpperBodySlot。
- Channel：Action timeline 负责，例如 Motion、Animation、Window、Cue。
- Presentation Layer：Editor view、Animancer layer、Timeline view 只展示或消费结果。

Graph Editor 的 node 只能表达 Source 层，不表达 Action / Claim / Slot / Channel 权威。

## 决策
### Decision: Behavior Authoring Graph 只产出 Source Runtime Definition
`CharacterBehaviorAuthoringAsset` 或继任资产只保存：

- stable asset id
- schema version
- root node
- ordered composite / parallel node
- Locomotion leaf
- CommittedAction leaf
- editor position
- source edge

它不保存 Dodge selector、Directional timeline、Backstep timeline、track、clip 或 motion payload。

### Decision: ActionDefinition 是 Committed Action 数据源
`CharacterActionDefinitionSO` / action catalog 是 action 数据源。Dodge branch 通过正式 action definition 构建：

- `DodgeCommittedActionBranchAuthoring`
- `CommittedActionBranchTimelineAuthoring`
- `ActionTimelineTrackAuthoring`
- `ActionTimelineClipAuthoring`

如果 Graph Editor 要展示 Dodge，只能读取 action definition 的摘要或打开 timeline editor。

### Decision: Compiler 拆分
Compiler 分成两条逻辑：

- Behavior compiler：graph -> execution tree / `CharacterBehaviorRuntimeDefinition`。
- Action compiler/validator：action definition -> `CharacterActionDefinition` -> `CommittedActionBranchDefinition` -> `ActionTimelineDefinition`。

组合点只能是正式角色配置或 runtime composition root，不允许 behavior graph 偷带 action branch。

### Decision: Legacy 字段必须有退役策略
如果现有 `CharacterBehaviorAuthoringAsset` 上已经有 Dodge branch 字段，迁移期只能用于诊断或一次性迁移。正式编译不得继续消费它。发现 legacy field 中仍有数据时，工具可以报告迁移提示，但不得把它当 fallback。

## 迁移步骤
1. 列出现有 behavior authoring asset 和 compiler 中所有 Dodge branch/timeline 字段。
2. 将 behavior compiler 输出收敛到 source graph runtime definition。
3. 新增 action definition reference / catalog reference 的正式校验。
4. 让 Graph Editor 保存时只写 graph topology 和 editor position。
5. Timeline Editor 继续写正式 action definition。
6. 删除 sample-only compiled runtime definition 的正式入口。
7. 用测试确认双数据源消失。

## 风险
- 现有测试可能依赖 behavior asset 内嵌 Dodge branch，需要改成正式 action definition fixture。
- 如果直接删除字段，Unity serialized asset 可能留下 orphaned yaml；实现时应使用兼容读取和明确迁移，不依赖隐藏 fallback。
- Graph Editor 用户可能误以为能编辑 Dodge，必须通过 UI 文案和菜单边界说明其职责是 source graph。

## 与其它 change 的关系
- `formalize-character-behavior-submission-runtime-chain` 消费本变更产生的清晰 source runtime definition。
- `migrate-ref-timeline-editor-to-formal-action-config` 消费本变更确立的 action definition 数据源。
