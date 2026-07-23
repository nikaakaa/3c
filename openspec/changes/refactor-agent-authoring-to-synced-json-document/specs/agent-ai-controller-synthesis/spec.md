## MODIFIED Requirements

### Requirement: Agent必须通过正式AI Authoring合同修改AI Controller

系统 MUST使用`btsmtl-agent-authoring-document.v1`作为CharacterController与AIController唯一AI-facing编辑合同，并通过显式domain discriminator选择AI root。AI Document MUST从已有合法`AIControllerDefinition`、AIControllerTree、Graph、AI Blackboard、Perception、受控Character input/request catalog和generated AI Program只读身份checkout；editable正文 MUST表达AI Definition binding、Shared Flow/Value、Observation、Memory与Intent目标结构。Document Reconciler MUST降低为统一immutable `AgentMutationPlan`，handler MUST调用正式BTSMTL与AI authoring API。系统 MUST不直接编辑YAML，不建立AI专用Document解释器，不保留v15-v17 Patch reader、converter、bootstrap action或双写输出。

#### Scenario: Agent编辑AI接近与攻击结构

- **WHEN** AI Document增加Configured Candidate、距离条件、MoveAxis与Attack request结构
- **THEN** Reconciler MUST生成统一immutable Mutation Plan
- **AND** handler MUST通过正式AI authoring API和统一Graph policy写入

#### Scenario: Agent尝试创建Timeline节点

- **WHEN** AI Document editable正文包含TimelineNode或ActivateActionInstanceNode
- **THEN** Reconciler MUST在mutation前拒绝整份Document
- **AND** AI Tree MUST不发生部分修改

#### Scenario: Agent提交旧Patch合同

- **WHEN** Document链收到v15、v16或v17 Snapshot/Patch payload
- **THEN** 系统 MUST明确报告schema不匹配
- **AND** MUST不转换、双写或兼容解释

### Requirement: Agent技能合同必须覆盖AI Tree工作流

BTSMTL Agent authoring技能与MCP bridge文档 MUST记录已有合法AI Definition的Document checkout、AI直接编辑JSON、dry-run、同document hash apply、反向canonical同步与validate流程，并列出AI Graph、Blackboard、Perception和Intent正式document entity能力。MCP bridge MUST只透传同一Document事务，MUST不增加AI专用action、Patch JSON、bootstrap、YAML写入或按名称猜identity。

#### Scenario: Agent修改AI Controller

- **WHEN** Agent需要修改AI Blackboard或Intent分支
- **THEN** MUST先显式checkout或复用未冲突的Document并锁定live source revision
- **AND** dry-run与apply MUST使用完全相同document hash
- **AND** apply成功后 MUST从最终AI Tree反向规范化Document

