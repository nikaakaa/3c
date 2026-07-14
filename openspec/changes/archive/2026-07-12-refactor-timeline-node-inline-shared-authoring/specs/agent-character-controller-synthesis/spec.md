## MODIFIED Requirements

### Requirement: Compiler 必须调用 BTSMTL 正式 authoring API

系统 MUST 通过 AgentPatchCompiler 将 Patch IR 应用到 BTSMTL graph。Compiler MUST调用现有正式 authoring API、节点/模块配置入口和 Timeline ownership authoring service，至少包括 BaseGraph.CreateNode(Type)、BaseGraph.Link(...)、BaseGraph.LinkProperty(...)、TimelineNode inline/shared 切换和 TimelineData clone。Compiler MUST尊重 CanCreateNodeType(Type)、PropertyPort PortId、Graph 与 Timeline inline/shared ownership 和 graph 类型规则。Compiler MUST NOT自己维护第二套节点、边、端口、Timeline 数据或 Workbench 数据。

#### Scenario: 创建非法节点

- **WHEN** Patch IR 尝试在 StateMachineGraph 中创建 TimelineNode
- **THEN** compiler MUST拒绝该操作并输出 compile report
- **AND** 系统 MUST NOT把非法节点加入正式 graph

#### Scenario: 默认创建 inline TimelineNode

- **WHEN** Patch IR 为状态行为 Graph 创建 TimelineNode 且未显式请求 Shared
- **THEN** compiler MUST创建正式 TimelineNode
- **AND** compiler MUST通过正式 ownership API 创建 inline TimelineData
- **AND** compiler MUST NOT要求或保留外部 TimelineAsset 引用

#### Scenario: 从 template asset 导入 inline Timeline

- **WHEN** Patch IR 为 Inline TimelineNode 提供 TimelineAsset template path
- **THEN** compiler MUST将 template data 克隆到节点 inline TimelineData
- **AND** template path MUST只作为编译期输入
- **AND** 生成节点 MUST NOT保存该 asset 为 runtime source

#### Scenario: 显式绑定 shared Timeline

- **WHEN** Patch IR 明确设置 Timeline ownership 为 Shared 并提供 TimelineAsset path
- **THEN** compiler MUST通过正式 ownership API绑定 shared TimelineAsset
- **AND** 节点 inline TimelineData MUST被清理
- **AND** compiler MUST NOT创建 TimelineStateNode 或旧播放器引用

### Requirement: Validator 必须检查 Agent 生成 graph 的 BTSMTL 语义

系统 MUST提供 Agent graph validator，在 apply 前后检查 Agent 生成结构。Validator MUST检查 graph 类型规则、ConditionRuleGraph 纯条件语义、TimelineNode 位置、Timeline inline/shared ownership、TimelineData serialized owner/path、TreeClip graph ownership、ActionProfile 引用、Input request 引用、Action Context 链路和 AnyState 条件。Validator MUST输出机器可读错误路径和建议修复。

#### Scenario: TimelineNode 位于错误图层

- **WHEN** 生成结果中 TimelineNode 位于 StateMachineGraph
- **THEN** validator MUST报告 graph kind 错误
- **AND** report MUST指出 TimelineNode 应位于 StateNode 的状态行为 Graph

#### Scenario: TimelineNode 存在双真相

- **WHEN** TimelineNode 同时保存 inline TimelineData 和 shared TimelineAsset
- **THEN** validator MUST报告 ownership 冲突
- **AND** validator MUST NOT按优先级静默选择其中一份

#### Scenario: inline Timeline owner path 断裂

- **WHEN** TimelineNode inline TimelineData 无法绑定到 RootTree serialized owner/path
- **THEN** validator MUST报告稳定 node path 与断裂字段
- **AND** 系统 MUST NOT把数据保存到临时 Timeline asset

#### Scenario: Action Context 断链

- **WHEN** 攻击状态播放带 projected Window TreeClip 的 Timeline 但没有 Action Context 来源
- **THEN** validator MUST报告 Action Context 缺失
- **AND** report MUST建议在状态进入或动作开始处创建 action activation 并把 context 传给 TimelineNode

#### Scenario: TreeClip owner path 断裂

- **WHEN** resolved TimelineData 中的 TreeClip inline TimelineRunningTree 缺少稳定 owner/path
- **THEN** validator MUST报告 TimelineNode、track、clip 和 graph identity
- **AND** validator MUST拒绝该 authoring 结果

