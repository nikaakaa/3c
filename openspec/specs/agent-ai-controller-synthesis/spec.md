# agent-ai-controller-synthesis Specification

## Purpose

定义Agent通过统一Document v3目录包、Mutation、Validator与固定生命周期工具读取、修改和验证BTSMTL AI Controller资产的正式合同。
## Requirements
### Requirement: Agent必须通过正式AI Authoring合同修改AI Controller

系统 MUST使用`btsmtl-agent-authoring-document.v3`目录包作为CharacterController与AIController唯一AI-facing编辑合同，并通过显式domain选择AI root。AI package MUST从已有合法`AIControllerDefinition`、AIControllerTree、Graph、AI Blackboard、Perception和受控Character input/request catalog checkout；editable分片 MUST表达AI Definition binding、Shared Flow/Value、Observation、Memory与Character input/request intent binding目标结构。Graph MUST使用稀疏Node kind、逻辑port、系统anchor和Edge完整目标集合。Document Reconciler MUST降低为统一immutable`AgentMutationPlan`，handler MUST调用正式BTSMTL与AI authoring API。系统 MUST不直接编辑YAML，不建立AI专用package解释器，不保留v1/v2、v15-v17 Patch、bootstrap或双写输出。

#### Scenario: Agent编辑AI接近与攻击结构

- **WHEN** AI package增加Configured Candidate、距离条件、MoveAxis与Attack request结构
- **THEN** Reconciler MUST从统一capability catalog生成immutable Mutation Plan
- **AND** handler MUST通过正式AI authoring API和Graph policy写入

#### Scenario: Agent尝试创建Timeline节点

- **WHEN** AI Graph文件包含Timeline或ActivateActionInstance kind
- **THEN** Reconciler MUST在mutation前拒绝整份package
- **AND** AI Tree MUST不发生部分修改

#### Scenario: Agent提交旧合同

- **WHEN** Document链收到v1单文件或v15-v17 Snapshot/Patch payload
- **THEN** 系统 MUST明确报告schema不匹配
- **AND** MUST不转换、双写或兼容解释

### Requirement: Agent Validator必须检查AI与Character分层

Agent Validator MUST复用AI Graph Role、Node Capability、AI Blackboard scope、Perception schema、Intent binding与AI Compiler正式校验。它 MUST确认AI Tree只输出CharacterSimulationInput，MUST报告任何Character execution、Transform副作用、自由字符串input或跨Program Blackboard引用。Validator MUST NOT复制节点白名单或运行AI Program。

#### Scenario: AI Intent绑定错误类型

- **WHEN** WriteContinuousInput绑定到类型不匹配的Character InputId
- **THEN** Validator MUST复用Character catalog校验报告错误
- **AND** MUST不按显示名替换InputId

### Requirement: Agent技能合同必须覆盖AI Tree工作流

BTSMTL Agent authoring技能与MCP bridge文档 MUST记录已有合法AI Definition的package checkout、AI通过宿主文件能力直接编辑JSON、dry-run、同document hash apply、整个package反向同步与validate流程。技能 MUST列出AI Graph、Blackboard、Perception、Observation、Memory和Character input/request intent binding正式schema，并指导AI读取本package内Node/Graph catalog。MCP MUST只暴露五个固定生命周期工具，MUST不增加AI专用action、Node级tool、Patch、bootstrap、YAML写入或按名称猜identity。

#### Scenario: Agent修改AI Controller

- **WHEN** Agent需要修改AI Blackboard或Intent分支
- **THEN** MUST先显式checkout或复用未冲突package
- **AND** MUST直接修改相关editable文件
- **AND** dry-run与apply MUST使用完全相同document hash
- **AND** apply成功后 MUST从最终AI Tree反向规范化整个package
