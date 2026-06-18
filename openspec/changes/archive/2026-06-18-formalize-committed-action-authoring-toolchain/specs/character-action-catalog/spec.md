## ADDED Requirements
### Requirement: Action Definition 使用通用 Branch Authoring
`CharacterActionDefinitionSO` 或批准等价动作定义 MUST 使用通用 Committed Action branch authoring 作为正式 branch 配置来源。Action definition compiler MUST 将该 branch authoring 编译为 `CommittedActionBranchDefinition` 或批准等价 runtime model。Dodge 专用 branch authoring、旧 variant 字段、single timeline authoring 或代码默认值 MUST NOT 作为正式 runtime branch fallback。

#### Scenario: Catalog 编译通用 Branch
- **GIVEN** Action Catalog 包含一个带通用 branch authoring 的 `Action.Dodge` definition
- **WHEN** runtime 构建 action catalog definition
- **THEN** `Action.Dodge` runtime definition MUST 包含从通用 branch authoring 编译出的 `CommittedActionBranchDefinition`
- **AND** selector、condition、timeline 和 body claim MUST 来自该通用 branch authoring
- **AND** runtime MUST NOT 根据 action id 特判读取 Dodge 专用 branch 字段

#### Scenario: 缺失 Branch 不 Fallback
- **GIVEN** action definition 缺失通用 branch authoring 或 branch authoring 非法
- **WHEN** action definition compiler 或 validator 运行
- **THEN** 系统 MUST 报告配置错误
- **AND** MUST NOT 从旧 Directional / Backstep variant、single timeline authoring、Resources、sample asset 或代码默认值补齐 branch
