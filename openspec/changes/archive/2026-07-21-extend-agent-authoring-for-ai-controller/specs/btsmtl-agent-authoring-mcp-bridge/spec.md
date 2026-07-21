## MODIFIED Requirements

### Requirement: MCP bridge 必须透传同一 v15 Character 与 AI 事务

BTSMTL Agent MCP bridge MUST接受并返回`agent-character-controller-synthesis.v15` Snapshot、Patch与Validation结果，并通过显式domain discriminator透传Character Controller或AI Controller generic事务。Bridge MUST只调用正式Agent Snapshot、lowerer、dry-run、apply和validator入口，不得新增AI专用action、SerializedProperty、YAML、反射、任意字段写入或旧v14转换工具。

#### Scenario: MCP提交AI Controller Patch

- **WHEN** 调用方通过MCP bridge提交合法v15 AI Controller Patch
- **THEN** Bridge MUST把同一请求交给AgentPatchAuthoringService
- **AND** MUST返回typed plan、事务与Validator产生的机器可读报告

