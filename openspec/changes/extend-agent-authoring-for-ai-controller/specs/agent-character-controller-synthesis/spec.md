## RENAMED Requirements

- FROM: `### Requirement: Agent Snapshot schema v14 必须输出稳定 authoring identity`
- TO: `### Requirement: Agent Snapshot schema v15 必须输出稳定 authoring identity`

## MODIFIED Requirements

### Requirement: Patch IR 必须是确定性的 graph 编辑指令

系统 MUST定义schema v15 Agent Patch IR作为Character Controller与AI Controller唯一确定性graph编辑指令边界，并使用显式domain discriminator选择根合同。Patch IR MUST使用stable authoring id或前序operation output引用定位编辑目标，只能表达正式authoring操作。Character domain MUST保留现有Character、Timeline、MotionWarp、Marker与Curve typed operation；AI domain MUST只增加AI Definition、AI Graph、AI Blackboard、Configured Candidate、Observation、Memory与Intent operation。Patch IR MUST不直接写Unity YAML、GUID映射集合、runtime状态或旧配置路径。

#### Scenario: AI Patch创建Controller Tree

- **WHEN** schema v15 AI Controller Patch创建AIControllerTree和AI Blackboard declaration
- **THEN** Patch MUST使用AI domain与stable identity
- **AND** MUST不通过Character domain或通用字段写入表达

### Requirement: Agent Patch 编译必须维护 identity 生命周期

Agent Patch compiler MUST在更新现有元素时保持其authoring identity，在创建新元素时生成新identity，在复制元素时生成新identity。系统 MUST只接受schema v15，不得保留v14及更早兼容解析或按path、display name、Actor名称、Tag、列表index猜测identity。Typed command lowering MUST在mutation前验证domain、authoring identity格式、operation id唯一性和前序operation reference顺序。

#### Scenario: AI Patch引用创建结果

- **WHEN** Patch先创建AIControllerTree再引用其operation output创建节点
- **THEN** compiler MUST在同一typed plan中绑定稳定owner symbol
- **AND** handler MUST不重新按路径查找Tree

### Requirement: Agent Patch Compiler内部必须使用唯一类型化命令计划

系统 MUST将schema v15 `AgentPatchOperation`只作为editor-only JSON边界DTO，并通过唯一operation catalog与`AgentPatchCommandLowerer`一次降低为immutable typed command plan。Character Controller与AI Controller domain MUST复用同一lowering、planning symbol、preflight、asset transaction和handler catalog基础；领域handler只消费各自正式authoring API。Dry-run与apply MUST消费同一typed command plan，MUST不建立AI专用Patch compiler或第二事务。

#### Scenario: 同一AI Patch执行dry-run和apply

- **WHEN** AgentPatchAuthoringService收到合法v15 AI Controller Patch并请求apply
- **THEN** dry-run与apply MUST消费同一immutable command plan
- **AND** report MUST保留相同operation identity和planned owner集合

### Requirement: Agent Snapshot schema v15 必须输出稳定 authoring identity

Agent Snapshot MUST使用schema v15和显式domain discriminator。Character Controller Snapshot MUST继续输出v14已有全部正式作者数据。AI Controller Snapshot MUST输出AIControllerDefinition、AIControllerTree、Graph/Node/Edge、Node Capability、AI Blackboard declaration、显式候选Actor Perception binding、Character input/request binding与generated AI Program identity。Snapshot path和列表index MAY作为可读定位信息，但 MUST不取代identity。Snapshot MUST不输出runtime mutable state、AI candidate state或Perception缓存。v15 Snapshot MUST成为生成v15 Patch的唯一上下文，不提供v14镜像输出。

#### Scenario: 导出AI Controller Full Snapshot

- **WHEN** Agent exporter导出合法AI Controller Full Snapshot
- **THEN** Snapshot MUST标记v15和AI Controller domain
- **AND** MUST包含生成可重复typed Patch所需的稳定authoring identity

