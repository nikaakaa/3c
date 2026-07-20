## MODIFIED Requirements

### Requirement: Agent Snapshot schema v11 必须输出稳定 authoring identity

Agent Snapshot MUST使用schema v11，并为Graph、Node、Edge、Timeline、Track、Clip、Blackboard declaration、CharacterInputProfile request timing与Timeline animation producer输出正式稳定authoring identity。Snapshot MUST额外输出Blackboard InputDerived InputValueId、`ActionTargetSnapshot` declaration类型、`CanActivateAction` target declaration、`ActivateActionInstance` target declaration与ActionProfile target requirement。Snapshot path和列表index MAY作为可读定位信息，但 MUST不取代identity。Snapshot MUST不输出Tree animation Driver、ExecutionLineage、LayerPlan或runtime playback lifecycle。schema v11 Snapshot MUST成为生成v11 Patch的唯一上下文，不提供旧schema镜像输出。

#### Scenario: 导出Full Snapshot

- **WHEN** Agent exporter导出`CharacterPipelineDefinition` Full Snapshot
- **THEN** 每个Graph、Node、Edge、Timeline、Track、Clip和animation producer MUST包含稳定authoring identity
- **AND** Snapshot MUST标记schema v11
- **AND** Snapshot MUST输出当前source revision所需的逻辑、Timeline、request timing与Action target内容

#### Scenario: Timeline Track重排后导出

- **WHEN** 作者重排Track或Clip后重新导出Snapshot
- **THEN** 对应元素和producer identity MUST保持
- **AND** index/path MAY更新

#### Scenario: 导出目标攻击调用点

- **WHEN** `CanActivateAction`与`ActivateActionInstance`引用ActionTargetSnapshot declaration
- **THEN** Snapshot MUST输出两个调用点的稳定declaration identity与key
- **AND** MUST同时输出所属ActionProfile的typed target requirement

## ADDED Requirements

### Requirement: Agent 必须完整修改 Action target authoring

Agent schema v11 Patch MUST提供类型化operation创建或配置`ActionTargetSnapshot` Blackboard declaration、保存InputDerived InputValueId、绑定准入与激活节点，以及设置ActionProfile的`None`、`OptionalSnapshot`或`SnapshotRequired`。Lowerer、Handler与Validator MUST调用正式authoring API，MUST不直接编辑YAML、不按显示名猜引用，也 MUST不形成第二个Action target配置入口。

#### Scenario: 为攻击建立目标链

- **WHEN** Patch创建InputDerived ActionTargetSnapshot declaration并绑定Attack Profile、CanActivate与Activate
- **THEN** dry-run MUST验证所有引用属于当前Definition且类型匹配
- **AND** apply MUST通过同一immutable typed plan原子写入正式资产

#### Scenario: 查询与激活引用不同目标变量

- **WHEN** reachable `CanActivateAction`与`ActivateActionInstance`引用不同declaration
- **THEN** Validator MUST报告准确Graph、Node与declaration identity
- **AND** artifact MUST不发布

#### Scenario: 可选目标攻击配置 MotionWarp

- **WHEN** ActionProfile声明`OptionalSnapshot`且Timeline MotionWarp配置完整
- **THEN** Agent Validator MUST接受该组合
- **AND** Snapshot MUST完整投影requirement、target references与Warp source

### Requirement: Agent 技能合同必须记录现行 MotionWarp 与 Action target 操作

`btsmtl-agent-authoring` current contract MUST列出Agent v11已经支持的MotionWarp Track/Clip创建、source绑定、参数配置，以及本change增加的Action target declaration、InputDerived binding、admission、activation与profile requirement操作。技能合同 MUST与operation catalog、lowerer和handler一一对应，MUST不描述不存在的操作或旧schema。

#### Scenario: Agent准备修改Corin目标攻击

- **WHEN** Agent读取current contract准备生成Patch
- **THEN** 它 MUST能从合同确认每个目标与MotionWarp操作的字段和identity要求
- **AND** MUST不需要读取Graph YAML或猜测未记录的宽DTO字段
