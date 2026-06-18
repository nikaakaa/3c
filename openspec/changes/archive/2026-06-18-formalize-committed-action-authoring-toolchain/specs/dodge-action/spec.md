## ADDED Requirements
### Requirement: Dodge 使用通用 Branch Authoring Tree
`Action.Dodge` MUST 作为通用 Committed Action branch authoring 的第一个 concrete instance。正式 Dodge action definition MUST 通过通用 branch authoring 表达 selector、Directional condition、Backstep condition、Directional TimelineNode、Backstep TimelineNode 和 FullBody claim。Dodge 专用 `DodgeCommittedActionBranchAuthoring` MAY 只作为一次性迁移输入或诊断输入存在，MUST NOT 作为正式 runtime 解析入口或 fallback。

#### Scenario: Dodge 节点树表达两个变体
- **WHEN** 设计者检查正式 `Action.Dodge` definition
- **THEN** branch authoring MUST 包含一个 selector root 或批准等价选择入口
- **AND** MUST 包含 Directional condition 到 Directional TimelineNode 的路径
- **AND** MUST 包含 Backstep condition 到 Backstep TimelineNode 的路径
- **AND** 两个 TimelineNode MUST 保存正式 Animation、Motion、Window 和 Cue payload

#### Scenario: Dodge 行为保持
- **GIVEN** Dodge branch authoring 已迁移到通用节点树
- **WHEN** 有移动意图的 Dodge 请求被接受并评估
- **THEN** selector MUST 选择 Directional TimelineNode
- **AND** Directional motion、animation key、window、cue 和 Run latch 行为 MUST 与迁移前等价
- **WHEN** 无移动意图的 Dodge 请求被接受并评估
- **THEN** selector MUST 选择 Backstep TimelineNode
- **AND** Backstep motion、animation key、window、cue 和不写 Run latch 行为 MUST 与迁移前等价

#### Scenario: 不保留 Dodge 专用 Fallback
- **GIVEN** 通用 Dodge branch authoring 缺失或非法
- **WHEN** 正式 gameplay 路径尝试处理 Dodge
- **THEN** 系统 MUST 报告配置错误或拒绝动作输出
- **AND** MUST NOT 从 `DodgeCommittedActionBranchAuthoring`、旧 Directional / Backstep variant、single timeline、Resources 或代码默认值继续运行
