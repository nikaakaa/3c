## REMOVED Requirements

### Requirement: Agent必须通过正式AI Authoring合同修改AI Controller

**Reason**: BTSMTL 的游戏 AIController domain 和对应作者资产已退出维护范围。

**Migration**: 删除 AI package DTO、解析、导出、对账、Mutation 和根路由；旧 AI 文档包不迁成插件图，也不保留兼容读写。

### Requirement: Agent Validator必须检查AI与Character分层

**Reason**: 旧 AI Graph/Program 作者 Validator 随该能力删除。

**Migration**: 插件游戏任务的角色观察/输入权限由其正式接入校验负责；剩余 BTSMTL 领域继续使用唯一原有 Validator，不增加插件图编译器。

### Requirement: Agent技能合同必须覆盖AI Tree工作流

**Reason**: Agent 不再通过 BTSMTL Document 维护游戏 AI 行为图。

**Migration**: 从技能说明和 MCP 描述中删除旧 AI 工作流及字段地图，保留其它正式领域和五个生命周期工具；本变更不新增插件 AI 自动化工具。
