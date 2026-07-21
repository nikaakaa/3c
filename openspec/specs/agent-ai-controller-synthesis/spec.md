# agent-ai-controller-synthesis Specification

## Purpose

定义Agent通过统一v15 Snapshot、Patch、Validator与MCP事务读取、修改和验证BTSMTL AI Controller资产的正式合同。

## Requirements

### Requirement: Agent必须通过正式AI Authoring合同修改AI Controller

系统 MUST将唯一Agent Snapshot、Patch、Intent与Validation根schema原子提升为`agent-character-controller-synthesis.v15`，使用显式domain discriminator区分Character Controller与AI Controller根。AI Snapshot MUST输出AIControllerDefinition、AI Graph role、Graph/Node/Edge identity、AI Blackboard declaration、显式候选Actor Perception binding、Character input/request binding与generated AI Program identity。Patch MUST通过唯一schema、typed command lowerer与handler catalog创建或修改AI Tree、AI declaration、Observation、Memory与Intent节点。Handler MUST调用正式BTSMTL与AI Definition authoring API，MUST NOT直接编辑YAML或建立AI专用宽DTO解释器。系统 MUST删除v14及更早reader、converter与双写输出。

#### Scenario: Agent创建AI接近与攻击结构

- **WHEN** v15 AI Controller Patch创建Configured Candidate、距离条件、MoveAxis与Attack request节点
- **THEN** lowerer MUST生成immutable typed command plan
- **AND** handler MUST通过正式AI authoring API和统一Graph policy写入

#### Scenario: Agent尝试创建Timeline节点

- **WHEN** AI Graph Patch包含TimelineNode或ActivateActionInstanceNode
- **THEN** preflight MUST拒绝整次事务
- **AND** AI Tree MUST不发生部分修改

#### Scenario: Agent提交旧v14 Patch

- **WHEN** v15安装后Agent收到v14 Patch
- **THEN** 系统 MUST明确报告版本不匹配
- **AND** MUST不转换、双写或兼容解释

### Requirement: Agent Validator必须检查AI与Character分层

Agent Validator MUST复用AI Graph Role、Node Capability、AI Blackboard scope、Perception schema、Intent binding与AI Compiler正式校验。它 MUST确认AI Tree只输出CharacterSimulationInput，MUST报告任何Character execution、Transform副作用、自由字符串input或跨Program Blackboard引用。Validator MUST NOT复制节点白名单或运行AI Program。

#### Scenario: AI Intent绑定错误类型

- **WHEN** WriteContinuousInput绑定到类型不匹配的Character InputId
- **THEN** Validator MUST复用Character catalog校验报告错误
- **AND** MUST不按显示名替换InputId

### Requirement: Agent技能合同必须覆盖AI Tree工作流

BTSMTL Agent authoring技能与MCP bridge文档 MUST记录AI Definition发现、Snapshot导出、Patch dry-run、同Patch apply、重新导出与validate流程，并列出AI Graph、Blackboard、Perception和Intent正式operation。MCP bridge MUST只透传同一v15 generic transaction，MUST NOT增加AI专用action、YAML写入或按名称猜identity。

#### Scenario: Agent修改AI Controller

- **WHEN** Agent需要修改AI Blackboard或Intent分支
- **THEN** MUST先导出当前AI Snapshot并锁定source revision
- **AND** dry-run与apply MUST使用完全相同Patch
